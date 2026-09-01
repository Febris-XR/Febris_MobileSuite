// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>Decodes a v2 framed byte stream / array into a
    /// <see cref="PacketHeaderModel"/> + body bytes. Stream-based parsing
    /// reads exactly the bytes declared by the length prefixes -- no more
    /// "read until <c>IsDataAvailable()</c> says false," which was the
    /// legacy approach that corrupted on mid-stream pauses.</summary>
    public interface IFebrisP2pFrameParser
    {
        /// <summary>Parse exactly one frame from a stream. Reads the preamble
        /// first to learn header + body sizes, then reads each section in
        /// turn. Returns the deserialized header and the raw body bytes.</summary>
        Task<(PacketHeaderModel Header, byte[] Body)> ParseAsync(
            Stream input,
            CancellationToken cancellationToken = default);

        /// <summary>Parse exactly one frame from an in-memory byte array.
        /// Convenience for callers that already have the full frame (e.g.,
        /// test fixtures, batched dispatchers). For wire-side parsing prefer
        /// <see cref="ParseAsync"/>.</summary>
        (PacketHeaderModel Header, byte[] Body) ParseBytes(byte[] input);

        /// <summary>Read exactly one complete v2 frame (preamble + header + body)
        /// off a stream and return its RAW, still-framed bytes. Returns
        /// <c>null</c> on a clean end-of-stream at a frame boundary, so a normal
        /// peer disconnect is not an error.
        ///
        /// This is the wire-side entry point for a per-connection read loop
        /// (MDM-B2). It makes the length prefix authoritative for framing, which
        /// is the whole point of the v2 format, while deliberately NOT parsing:
        /// existing receivers are handed raw frame bytes and call
        /// <see cref="ParseBytes"/> themselves, so cutting the socket layer over
        /// to this method fixes coalescing and truncation without disturbing that
        /// downstream contract.</summary>
        Task<byte[]> ReadFrameBytesAsync(
            Stream input,
            CancellationToken cancellationToken = default);
    }

    public class FebrisP2pFrameParser : IFebrisP2pFrameParser
    {
        private static readonly Encoding HeaderEncoding = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        // MP2P-10: optional logger -- symmetric to FebrisP2pFrameBuilder. Emits
        // one Info line per successful parse + one Warn line per failure path
        // (oversized / malformed / truncated). Null logger means no logging,
        // preserving backward compat for the original constructor.
        private readonly IFebrisP2pLogger _logger;

        public FebrisP2pFrameParser()
        {
            // Defaults to the console logger rather than null. Every construction site in both
            // tiers uses this parameterless form, so a null default meant the per-frame markers
            // (`frame send: bodyType=`, `read frame totalLen=`) could NEVER appear. The device
            // test plan was written around grepping for exactly those, and their absence was read
            // as evidence during bring-up when it only ever meant "no logger was attached".
            _logger = ConsoleFebrisP2pLogger.Instance;
        }

        public FebrisP2pFrameParser(IFebrisP2pLogger logger)
        {
            _logger = logger;
        }

        public async Task<(PacketHeaderModel Header, byte[] Body)> ParseAsync(
            Stream input,
            CancellationToken cancellationToken = default)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // 1. Read the fixed 17-byte preamble.
            byte[] preamble = new byte[FebrisP2pFrame.PreambleByteLength];
            await ReadExactAsync(input, preamble, 0, FebrisP2pFrame.PreambleByteLength, cancellationToken).ConfigureAwait(false);

            // 2. Validate magic.
            for (int i = 0; i < FebrisP2pFrame.MagicBytes.Length; i++)
            {
                if (preamble[i] != FebrisP2pFrame.MagicBytes[i])
                {
                    throw new InvalidFrameMagicException(
                        "Frame does not start with FBP2 magic bytes. First 4 bytes: 0x" +
                        preamble[0].ToString("X2") + " 0x" + preamble[1].ToString("X2") +
                        " 0x" + preamble[2].ToString("X2") + " 0x" + preamble[3].ToString("X2") +
                        ". Wire is out-of-sync or peer is speaking a different protocol.");
                }
            }

            // 3. Validate version.
            byte version = preamble[4];
            if (version != FebrisP2pFrame.CurrentVersion)
            {
                throw new UnsupportedFrameVersionException(version);
            }

            // 4. Decode lengths.
            uint headerLength = ReadUInt32BigEndian(preamble, 5);
            ulong bodyLength = ReadUInt64BigEndian(preamble, 9);

            // 5. Enforce caps BEFORE allocating the read buffers.
            if (headerLength > FebrisP2pFrame.MaxHeaderBytes)
            {
                throw new OversizedFrameException(
                    "Frame declares header length of " + headerLength + " bytes; maximum is " +
                    FebrisP2pFrame.MaxHeaderBytes + " bytes. Possible corruption or attack.");
            }

            if (bodyLength > (ulong)FebrisP2pFrame.MaxBodyBytes)
            {
                throw new OversizedFrameException(
                    "Frame declares body length of " + bodyLength + " bytes; maximum is " +
                    FebrisP2pFrame.MaxBodyBytes + " bytes. Possible corruption or attack.");
            }

            // 6. Read header bytes + deserialize.
            byte[] headerBytes = new byte[headerLength];
            if (headerLength > 0)
            {
                await ReadExactAsync(input, headerBytes, 0, (int)headerLength, cancellationToken).ConfigureAwait(false);
            }

            PacketHeaderModel header;
            try
            {
                string headerJson = HeaderEncoding.GetString(headerBytes);
                header = JsonConvert.DeserializeObject<PacketHeaderModel>(headerJson);
                if (header == null)
                {
                    // Pass a dummy inner so we hit the (message, inner) overload
                    // rather than synthesizing yet another exception ctor.
                    throw new MalformedFrameHeaderException(
                        "Header JSON deserialized to null. Raw header bytes: " + headerLength + ".",
                        inner: new InvalidOperationException("DeserializeObject returned null"));
                }
            }
            catch (MalformedFrameHeaderException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MalformedFrameHeaderException(
                    "Failed to deserialize header JSON. Raw header bytes: " + headerLength + ".",
                    ex);
            }

            // 6b. Per-BodyType body cap, now that BodyType is known. Same reasoning as
            //     ReadFrameBytesAsync: the 256 MB global ceiling is sized for module
            //     ZIPs and is far too generous for a single access unit, and this is
            //     the earliest point the type is available.
            if (FebrisP2pFrame.IsVideoPayload(header.BodyType) && bodyLength > (ulong)FebrisP2pFrame.MaxVideoBodyBytes)
            {
                throw new OversizedFrameException(
                    "Frame declares a " + header.BodyType + " body of " + bodyLength +
                    " bytes; maximum for a video payload is " + FebrisP2pFrame.MaxVideoBodyBytes +
                    " bytes. Possible corruption or attack.");
            }

            // 7. Read body bytes.
            byte[] bodyBytes;
            if (bodyLength == 0)
            {
                bodyBytes = Array.Empty<byte>();
            }
            else
            {
                bodyBytes = new byte[bodyLength];
                await ReadExactAsync(input, bodyBytes, 0, (int)bodyLength, cancellationToken).ConfigureAwait(false);
            }

            // MP2P-10: one structured "recv" line per successfully-parsed frame.
            // Mirror of FebrisP2pFrameBuilder's "send" line so the two endpoints
            // can be correlated by messageId in log search. Failure paths are
            // logged as Warn at their respective throw sites above (oversized
            // header / body), or surface as exceptions for the dispatcher to log.
            _logger?.Log(FebrisP2pLogLevel.Info,
                "frame recv: bodyType=" + header.BodyType +
                " headerLen=" + headerLength +
                " bodyLen=" + bodyLength +
                " messageId=" + header.MessageId);

            return (header, bodyBytes);
        }

        public (PacketHeaderModel Header, byte[] Body) ParseBytes(byte[] input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // Build a MemoryStream over the input and reuse the async parser.
            // ParseAsync against a MemoryStream is synchronous in practice
            // (no real I/O); .GetAwaiter().GetResult() is safe and avoids
            // duplicating the parse logic.
            using (MemoryStream ms = new MemoryStream(input, writable: false))
            {
                return ParseAsync(ms, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public async Task<byte[]> ReadFrameBytesAsync(
            Stream input,
            CancellationToken cancellationToken = default)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // 1. Preamble, EOF-tolerant. A peer closing cleanly BETWEEN frames is
            //    normal shutdown, not truncation, so that case returns null rather
            //    than throwing. Closing part-way into a preamble is still an error.
            byte[] preamble = new byte[FebrisP2pFrame.PreambleByteLength];
            bool gotPreamble = await TryReadExactAsync(
                input, preamble, 0, FebrisP2pFrame.PreambleByteLength, cancellationToken).ConfigureAwait(false);
            if (!gotPreamble)
            {
                return null;
            }

            // 2. Magic, 3. version, 4. lengths, 5. caps. Same order and the same
            //    constants as ParseAsync, so both entry points reject identically.
            for (int i = 0; i < FebrisP2pFrame.MagicBytes.Length; i++)
            {
                if (preamble[i] != FebrisP2pFrame.MagicBytes[i])
                {
                    throw new InvalidFrameMagicException(
                        "Frame does not start with FBP2 magic bytes. First 4 bytes: 0x" +
                        preamble[0].ToString("X2") + " 0x" + preamble[1].ToString("X2") +
                        " 0x" + preamble[2].ToString("X2") + " 0x" + preamble[3].ToString("X2") +
                        ". Wire is out-of-sync or peer is speaking a different protocol.");
                }
            }

            byte version = preamble[4];
            if (version != FebrisP2pFrame.CurrentVersion)
            {
                throw new UnsupportedFrameVersionException(version);
            }

            uint headerLength = ReadUInt32BigEndian(preamble, 5);
            ulong bodyLength = ReadUInt64BigEndian(preamble, 9);

            if (headerLength > FebrisP2pFrame.MaxHeaderBytes)
            {
                throw new OversizedFrameException(
                    "Frame declares header length of " + headerLength + " bytes; maximum is " +
                    FebrisP2pFrame.MaxHeaderBytes + " bytes. Possible corruption or attack.");
            }

            if (bodyLength > (ulong)FebrisP2pFrame.MaxBodyBytes)
            {
                throw new OversizedFrameException(
                    "Frame declares body length of " + bodyLength + " bytes; maximum is " +
                    FebrisP2pFrame.MaxBodyBytes + " bytes. Possible corruption or attack.");
            }

            // 6. Read the header on its own FIRST. The header is already bounded at
            //    64 KB by the check above, and reading it here is what lets step 7
            //    pick the right body cap before a single body byte is allocated.
            byte[] headerBytes = new byte[headerLength];
            if (headerLength > 0)
            {
                await ReadExactAsync(input, headerBytes, 0, (int)headerLength, cancellationToken).ConfigureAwait(false);
            }

            // 7. Per-BodyType body cap. The 256 MB global ceiling is sized for module
            //    ZIPs. Applying only that to video would let one corrupted length
            //    prefix make a headset allocate 256 MB for what should be a few tens
            //    of KB. This has to happen after the header is decoded, because
            //    BodyType is not knowable at the preamble.
            BodyType peekedBodyType = PeekBodyType(headerBytes);
            if (FebrisP2pFrame.IsVideoPayload(peekedBodyType) && bodyLength > (ulong)FebrisP2pFrame.MaxVideoBodyBytes)
            {
                throw new OversizedFrameException(
                    "Frame declares a " + peekedBodyType + " body of " + bodyLength +
                    " bytes; maximum for a video payload is " + FebrisP2pFrame.MaxVideoBodyBytes +
                    " bytes. Possible corruption or attack.");
            }

            // 8. Reassemble the frame verbatim. The caps above bound this well inside
            //    int range, so the casts are safe.
            byte[] frame = new byte[FebrisP2pFrame.PreambleByteLength + (int)headerLength + (int)bodyLength];
            Buffer.BlockCopy(preamble, 0, frame, 0, FebrisP2pFrame.PreambleByteLength);
            if (headerLength > 0)
            {
                Buffer.BlockCopy(headerBytes, 0, frame, FebrisP2pFrame.PreambleByteLength, (int)headerLength);
            }
            if (bodyLength > 0)
            {
                await ReadExactAsync(
                    input,
                    frame,
                    FebrisP2pFrame.PreambleByteLength + (int)headerLength,
                    (int)bodyLength,
                    cancellationToken).ConfigureAwait(false);
            }

            _logger?.Log(FebrisP2pLogLevel.Info,
                "FebrisP2pFrameParser.ReadFrameBytesAsync: read frame totalLen=" + frame.Length +
                " headerLen=" + headerLength + " bodyLen=" + bodyLength);

            return frame;
        }

        // Reads ONLY the BodyType out of the header bytes, so the body cap can be
        // chosen before the body is allocated. Deliberately forgiving: a header that
        // will not deserialize returns _junk, which is not a video type, so the frame
        // falls back to the global cap and the real MalformedFrameHeaderException is
        // raised later by the full parse. This helper must not change which exception
        // a malformed header produces.
        private static BodyType PeekBodyType(byte[] headerBytes)
        {
            if (headerBytes == null || headerBytes.Length == 0)
            {
                return BodyType._junk;
            }
            try
            {
                string headerJson = HeaderEncoding.GetString(headerBytes);
                PacketHeaderModel header = JsonConvert.DeserializeObject<PacketHeaderModel>(headerJson);
                return header?.BodyType ?? BodyType._junk;
            }
            catch (Exception)
            {
                return BodyType._junk;
            }
        }

        // Like ReadExactAsync but tolerates a clean EOF before the FIRST byte,
        // returning false instead of throwing. Used only for the preamble, where
        // "nothing more is coming" is a normal disconnect rather than a truncation.
        private static async Task<bool> TryReadExactAsync(
            Stream stream,
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    if (totalRead == 0)
                    {
                        return false;
                    }
                    throw new TruncatedFrameException(
                        "Stream ended " + totalRead + " bytes into a " + count +
                        "-byte frame preamble. Peer disconnected mid-frame.");
                }
                totalRead += read;
            }
            return true;
        }

        // Stream.Read / ReadAsync may return fewer bytes than requested even
        // when the stream isn't at EOF. This helper loops until count bytes
        // have been read or the stream is exhausted.
        private static async Task ReadExactAsync(
            Stream stream,
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new TruncatedFrameException(
                        "Stream ended after " + (offset + totalRead) +
                        " bytes; expected " + count + " more bytes. Peer likely disconnected mid-frame.");
                }
                totalRead += read;
            }
        }

        // Mirror of FebrisP2pFrameBuilder.Write*BigEndian. Same reason: avoid
        // host-byte-order dependency from BitConverter.
        internal static uint ReadUInt32BigEndian(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset + 0] << 24)
                 | ((uint)buffer[offset + 1] << 16)
                 | ((uint)buffer[offset + 2] << 8)
                 |  (uint)buffer[offset + 3];
        }

        internal static ulong ReadUInt64BigEndian(byte[] buffer, int offset)
        {
            return ((ulong)buffer[offset + 0] << 56)
                 | ((ulong)buffer[offset + 1] << 48)
                 | ((ulong)buffer[offset + 2] << 40)
                 | ((ulong)buffer[offset + 3] << 32)
                 | ((ulong)buffer[offset + 4] << 24)
                 | ((ulong)buffer[offset + 5] << 16)
                 | ((ulong)buffer[offset + 6] << 8)
                 |  (ulong)buffer[offset + 7];
        }
    }
}
