// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Text;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>Encodes a <see cref="PacketHeaderModel"/> + body bytes into a
    /// v2 framed byte array (see <see cref="FebrisP2pFrame"/>). Single
    /// allocation per frame -- header + body are <see cref="Buffer.BlockCopy"/>'d
    /// into a pre-sized output buffer (no O(n^2) byte-by-byte combiner like the
    /// legacy <c>ArrayCombinerBuilder</c>).</summary>
    public interface IFebrisP2pFrameBuilder
    {
        /// <summary>Build a frame with a binary body. The most common case for
        /// modules / videos / raw payloads.</summary>
        byte[] Build(PacketHeaderModel header, byte[] body);

        /// <summary>Build a frame with a UTF-8 string body. Convenience for
        /// statement / status / generic-string body types -- the string is
        /// encoded as UTF-8 (unlike the legacy ASCII path).</summary>
        byte[] Build(PacketHeaderModel header, string body);

        /// <summary>Build a frame with an empty body. Used for body types like
        /// <c>BodyType._acknowledge</c> where the header carries all meaning.</summary>
        byte[] Build(PacketHeaderModel header);
    }

    public class FebrisP2pFrameBuilder : IFebrisP2pFrameBuilder
    {
        // Reused; encoding instances are thread-safe by spec.
        private static readonly Encoding HeaderEncoding = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        // MP2P-10: optional logger so every Build emits a structured "send" line
        // with direction / bodyType / headerLen / bodyLen / messageId fields. The
        // default null means "no logging" -- callers that want observability pass
        // an IFebrisP2pLogger (the Android shells pass the Log-forwarder; tests
        // pass a CapturingFebrisP2pLogger to assert the right lines fire). Kept
        // optional + nullable so MP2P-2's existing 21 call sites stay
        // backward-compatible -- they continue to work with no edits.
        private readonly IFebrisP2pLogger _logger;

        public FebrisP2pFrameBuilder()
        {
            // Defaults to the console logger rather than null. Every construction site in both
            // tiers uses this parameterless form, so a null default meant the per-frame markers
            // (`frame send: bodyType=`, `read frame totalLen=`) could NEVER appear. The device
            // test plan was written around grepping for exactly those, and their absence was read
            // as evidence during bring-up when it only ever meant "no logger was attached".
            _logger = ConsoleFebrisP2pLogger.Instance;
        }

        public FebrisP2pFrameBuilder(IFebrisP2pLogger logger)
        {
            _logger = logger;
        }

        public byte[] Build(PacketHeaderModel header)
        {
            // Empty body is a real case -- acknowledgement frames, status pokes.
            // Allocate a 0-byte body and run through the common path so the
            // length prefix is consistently set to 0.
            return Build(header, Array.Empty<byte>());
        }

        public byte[] Build(PacketHeaderModel header, string body)
        {
            // UTF-8 body. Legacy used Encoding.ASCII for the matching string
            // path on send + Encoding.Default on receive -- mismatched and
            // silently lossy for any non-ASCII character. v2 settles on UTF-8
            // both ways.
            byte[] bodyBytes = body == null
                ? Array.Empty<byte>()
                : Encoding.UTF8.GetBytes(body);
            return Build(header, bodyBytes);
        }

        public byte[] Build(PacketHeaderModel header, byte[] body)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));
            body = body ?? Array.Empty<byte>();

            // Serialize the header to UTF-8 JSON bytes. Newtonsoft.Json's default
            // serialization for a POCO with [JsonProperty] attributes is exactly
            // what the legacy code produced; the difference is the byte
            // encoding (UTF-8 here, ASCII before).
            string headerJson = JsonConvert.SerializeObject(header);
            byte[] headerBytes = HeaderEncoding.GetBytes(headerJson);

            // Cap enforcement BEFORE allocating the output buffer -- prevents a
            // malicious / buggy header from forcing a 4 GB allocation.
            if (headerBytes.Length > FebrisP2pFrame.MaxHeaderBytes)
            {
                throw new OversizedFrameException(
                    "Frame header is " + headerBytes.Length + " bytes; maximum is " +
                    FebrisP2pFrame.MaxHeaderBytes + " bytes. Header content: " +
                    (headerJson.Length > 200 ? headerJson.Substring(0, 200) + "..." : headerJson));
            }

            if ((long)body.Length > FebrisP2pFrame.MaxBodyBytes)
            {
                throw new OversizedFrameException(
                    "Frame body is " + body.Length + " bytes; maximum is " +
                    FebrisP2pFrame.MaxBodyBytes + " bytes. Split large payloads into chunks " +
                    "(see Tier 3 distribution roadmap) or raise MaxBodyBytes for this deployment.");
            }

            // Single allocation. Layout: [4 magic][1 version][4 header len][8 body len][header][body].
            int totalLength = FebrisP2pFrame.PreambleByteLength + headerBytes.Length + body.Length;
            byte[] output = new byte[totalLength];

            // Magic.
            Buffer.BlockCopy(FebrisP2pFrame.MagicBytes, 0, output, 0, FebrisP2pFrame.MagicBytes.Length);

            // Version.
            output[4] = FebrisP2pFrame.CurrentVersion;

            // Header length: UInt32 big-endian at offset 5.
            WriteUInt32BigEndian(output, 5, (uint)headerBytes.Length);

            // Body length: UInt64 big-endian at offset 9.
            WriteUInt64BigEndian(output, 9, (ulong)body.Length);

            // Header bytes at offset 17.
            Buffer.BlockCopy(headerBytes, 0, output, FebrisP2pFrame.PreambleByteLength, headerBytes.Length);

            // Body bytes immediately after.
            if (body.Length > 0)
            {
                Buffer.BlockCopy(body, 0, output, FebrisP2pFrame.PreambleByteLength + headerBytes.Length, body.Length);
            }

            // MP2P-10: one structured "send" line per frame. Fields match the
            // canonical observability format described in the audit doc + tier
            // roadmap: direction (send|recv), bodyType, headerLen, bodyLen,
            // messageId. peerId isn't known to the framer; the dispatch layer
            // logs that separately when it knows which Companion it's writing to.
            _logger?.Log(FebrisP2pLogLevel.Info,
                "frame send: bodyType=" + header.BodyType +
                " headerLen=" + headerBytes.Length +
                " bodyLen=" + body.Length +
                " messageId=" + header.MessageId);

            return output;
        }

        // netstandard2.0 lacks System.Buffers.Binary.BinaryPrimitives. Manual
        // big-endian writes keep the protocol byte-order explicit and
        // platform-independent (BitConverter is host-byte-order, which is
        // little-endian on Android-ARM but the dependency was undocumented).
        internal static void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        internal static void WriteUInt64BigEndian(byte[] buffer, int offset, ulong value)
        {
            buffer[offset + 0] = (byte)(value >> 56);
            buffer[offset + 1] = (byte)(value >> 48);
            buffer[offset + 2] = (byte)(value >> 40);
            buffer[offset + 3] = (byte)(value >> 32);
            buffer[offset + 4] = (byte)(value >> 24);
            buffer[offset + 5] = (byte)(value >> 16);
            buffer[offset + 6] = (byte)(value >> 8);
            buffer[offset + 7] = (byte)value;
        }
    }
}
