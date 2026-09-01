// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Constants + framing metadata for the Febris Mobile Server &lt;-&gt; Companion
    /// wire protocol (v2). Single source of truth -- replaces the byte-for-byte
    /// duplicated <c>WiFiP2pRequestCreation</c> / <c>WiFiP2pRequestProcessing</c>
    /// constants previously living in each project.
    ///
    /// <para>
    /// <b>Frame layout (v2):</b>
    /// <code>
    /// Offset  Size  Field
    /// ------  ----  ----------------------------------------
    /// 0       4     Magic bytes: 0x46 0x42 0x50 0x32 ("FBP2")
    /// 4       1     Version: 0x02
    /// 5       4     Header length (UInt32, big-endian)
    /// 9       8     Body length (UInt64, big-endian)
    /// 17      H     Header bytes (UTF-8 JSON of PacketHeaderModel)
    /// 17+H    B     Body bytes (raw)
    /// Total: 17 + H + B bytes
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <b>What the new format fixes:</b>
    /// <list type="bullet">
    ///   <item>Body length is explicit. A mid-stream pause no longer truncates the
    ///         message; the receiver reads exactly <c>H + B</c> bytes after the fixed
    ///         17-byte preamble.</item>
    ///   <item>Magic + version up front. Garbage on the wire fails fast with a clear
    ///         exception instead of yielding an undefined parse.</item>
    ///   <item>Big-endian lengths. Network byte order is explicit, not host-byte-
    ///         order-dependent like the legacy <c>BitConverter</c> calls.</item>
    ///   <item>UTF-8 header. The legacy frame encoded the JSON header as ASCII,
    ///         silently corrupting device names with non-Latin characters.</item>
    ///   <item>Size caps. Header capped at <see cref="MaxHeaderBytes"/>; body at
    ///         <see cref="MaxBodyBytes"/>. Rejects malicious / corrupted lengths
    ///         that would otherwise OOM the receiver.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class FebrisP2pFrame
    {
        /// <summary>Fixed 4-byte magic at the start of every frame. ASCII "FBP2".</summary>
        public static readonly byte[] MagicBytes = new byte[] { 0x46, 0x42, 0x50, 0x32 };

        /// <summary>Wire-format version. v1 was the legacy unframed format
        /// (<c>[4-byte header length][header][body]</c>); v2 is the format
        /// described in this class. Bump on any breaking layout change.</summary>
        public const byte CurrentVersion = 0x02;

        /// <summary>Fixed preamble size: 4 magic + 1 version + 4 header length +
        /// 8 body length = 17 bytes.</summary>
        public const int PreambleByteLength = 17;

        /// <summary>Maximum permitted header length in bytes. A header is
        /// JSON-serialized <see cref="Models.PacketHeaderModel"/>; 64 KB is
        /// well above any legitimate size and well below any DoS threshold.
        /// Receivers that see a larger header reject with
        /// <see cref="OversizedFrameException"/>.</summary>
        public const int MaxHeaderBytes = 64 * 1024;

        /// <summary>Maximum permitted body length in bytes. 256 MB accommodates
        /// realistic module ZIP sizes; payloads larger than this should be
        /// rejected at the API boundary, not framed. Receivers that see a
        /// larger body reject with <see cref="OversizedFrameException"/>.</summary>
        public const long MaxBodyBytes = 256L * 1024L * 1024L;

        /// <summary>Maximum body length for the video body types
        /// (<see cref="Models.BodyType._videoFrame"/>,
        /// <see cref="Models.BodyType._videoCodecConfig"/>).
        ///
        /// The 256 MB global ceiling is right for a module ZIP and far too generous
        /// for a single H.264 access unit. Without a tighter per-type cap, one
        /// corrupted length prefix would make a headset allocate 256 MB before any
        /// other validation could reject it. 512 KB is comfortably above a keyframe
        /// at the proposed 1280x720 / 4 Mbps profile and comfortably below anything
        /// that would hurt.</summary>
        public const long MaxVideoBodyBytes = 512L * 1024L;

        /// <summary>True for the body types that carry encoded video and are therefore
        /// bounded by <see cref="MaxVideoBodyBytes"/> rather than
        /// <see cref="MaxBodyBytes"/>.</summary>
        public static bool IsVideoPayload(Models.BodyType bodyType)
        {
            return bodyType == Models.BodyType._videoFrame
                || bodyType == Models.BodyType._videoCodecConfig;
        }
    }

    /// <summary>Base type for all framing errors. Catch this when you want to
    /// handle "frame is unrecoverable, close the connection and re-sync."</summary>
    public class FebrisP2pFrameException : Exception
    {
        public FebrisP2pFrameException(string message) : base(message) { }
        public FebrisP2pFrameException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>The first 4 bytes of the frame aren't the expected magic. Wire
    /// is out-of-sync, garbage, or a non-Febris protocol. Receiver should close
    /// the connection.</summary>
    public class InvalidFrameMagicException : FebrisP2pFrameException
    {
        public InvalidFrameMagicException(string message) : base(message) { }
    }

    /// <summary>Magic was correct but the version byte is unrecognized. Probably
    /// indicates a sender running a newer protocol than the receiver supports.</summary>
    public class UnsupportedFrameVersionException : FebrisP2pFrameException
    {
        public byte ReceivedVersion { get; }

        public UnsupportedFrameVersionException(byte receivedVersion)
            : base("Unsupported Febris P2P frame version: 0x" + receivedVersion.ToString("X2") +
                   ". Receiver supports up to 0x" + FebrisP2pFrame.CurrentVersion.ToString("X2") + ".")
        {
            ReceivedVersion = receivedVersion;
        }
    }

    /// <summary>Header or body length exceeds the configured cap. Receiver should
    /// reject the frame; if this happens often, investigate whether a sender is
    /// trying to push oversized payloads or a corrupted length prefix slipped
    /// through magic / version validation.</summary>
    public class OversizedFrameException : FebrisP2pFrameException
    {
        public OversizedFrameException(string message) : base(message) { }
    }

    /// <summary>Header bytes don't deserialize as a <see cref="Models.PacketHeaderModel"/>.
    /// Either the sender shipped malformed JSON or the byte stream is corrupted
    /// after magic/version validation passed (rare -- usually means the length
    /// prefix is also wrong).</summary>
    public class MalformedFrameHeaderException : FebrisP2pFrameException
    {
        public MalformedFrameHeaderException(string message, Exception inner)
            : base(message, inner) { }
    }

    /// <summary>Stream ended before the expected number of bytes was read. Usually
    /// means the peer disconnected mid-frame. Receiver should close the connection
    /// and the higher-level retry mechanism (Tier 3 distribution / Tier 1 MP2P-8
    /// ack) decides whether to re-request.</summary>
    public class TruncatedFrameException : FebrisP2pFrameException
    {
        public TruncatedFrameException(string message) : base(message) { }
    }
}
