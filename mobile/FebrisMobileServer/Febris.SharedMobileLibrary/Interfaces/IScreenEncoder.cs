// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Interfaces
{
    /// <summary>
    /// Whole-screen capture and H.264 encode.
    ///
    /// The Android implementation uses MediaProjection plus a VirtualDisplay whose output
    /// Surface IS the MediaCodec input surface, so frames go compositor to encoder with no
    /// bitmap ever allocated. This replaces the old Screenshot.CaptureAsync loop, which
    /// captured only the app's own view hierarchy rather than the device screen, and which
    /// pushed whole PNGs at roughly 10fps.
    ///
    /// PRIMITIVES ONLY. This assembly is netstandard2.0 and cannot name Android types, so
    /// nothing here exposes a Surface, a MediaFormat or a MediaProjection.
    ///
    /// PARAMETERLESS CONSTRUCTOR REQUIRED. DependencyService.Get&lt;T&gt;() activates through one,
    /// so the implementation cannot be constructor-injected. Use explicit Start/Stop instead.
    /// Do not copy the old static VideoStream() shape: a static entry point cannot be
    /// injected or substituted under any future DI.
    ///
    /// Encoded access units leave via the existing IWiFiP2pServer.SocketSender, not across
    /// this interface, which keeps the socket as the single send path.
    ///
    /// See docs/MOBILE_P2P_VIDEO.md section 4.
    /// </summary>
    public interface IScreenEncoder
    {
        /// <summary>True while a capture session is live.</summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts capture and encode. Idempotent: if a session is already live this returns
        /// true without spawning a second one. Returns false when consent was denied or the
        /// platform refused, which the caller must treat as "no video", not as a crash.
        /// </summary>
        Task<bool> StartAsync(int maxWidth, int maxHeight, int bitrateBps, int frameRate);

        /// <summary>Deterministic teardown. Safe to call when not running.</summary>
        Task StopAsync();

        /// <summary>
        /// Forces the next output access unit to be an IDR, via
        /// MediaCodec.PARAMETER_KEY_REQUEST_SYNC_FRAME. Used when a viewer joins late, after
        /// a decoder error, or after frames were dropped under pressure, since every frame
        /// after a dropped non-IDR is corrupt until the next keyframe.
        /// </summary>
        void RequestKeyFrame();

        /// <summary>
        /// Cached SPS/PPS for the current session, or null before the encoder has emitted
        /// its BUFFER_FLAG_CODEC_CONFIG output. The receiver cannot configure its decoder
        /// without this, so it is re-sent to any late-joining viewer.
        /// </summary>
        byte[] CodecConfig { get; }

        /// <summary>
        /// The cached SPS/PPS packaged as a complete, sendable <c>_videoCodecConfig</c> frame, or
        /// null when there is nothing to resend.
        ///
        /// <para><b>Why this exists rather than callers building the frame themselves.</b> A codec
        /// config is useless to the receiver without the ENCODED dimensions: the decoder builds its
        /// MediaFormat from them, and <c>MediaFormat.CreateVideoFormat</c> with a zero width or
        /// height makes <c>MediaCodec.Configure</c> throw <c>IllegalArgumentException</c>. Those
        /// dimensions are negotiated inside the encoder and were not on this interface, so the
        /// resync path hand-rolled its own header without them and every RESENT config was
        /// unconfigurable. That defeated the entire point of the recovery loop, which exists
        /// precisely for the case where the one-shot config was lost, and it was observed on device
        /// as a repeating resync answered by a config the decoder then rejected.</para>
        ///
        /// <para>Returning the finished frame rather than the raw dimensions keeps frame
        /// construction in the packetizer, where the primary emit path already does it, so the two
        /// senders cannot drift apart again.</para>
        /// </summary>
        byte[] BuildCodecConfigFrame();

        /// <summary>
        /// How many frame sends in a row failed to reach the socket. Zero while the link is
        /// healthy, because any successful send resets it.
        ///
        /// <para><b>Why the session loop needs this.</b> Capture must stop when nobody is
        /// receiving, and the loop's existing guards cannot tell. <c>StreamVideo</c> only reports
        /// what this device decided, and <c>WiFiConnected</c> only reports that the WiFi Direct
        /// GROUP is up. If the Server application dies while the group stays up, both remain true
        /// and the Companion keeps capturing a learner's screen with nobody watching. That is a
        /// privacy failure, not a performance one.</para>
        ///
        /// <para>Send failures are the only liveness evidence available to this side. "No traffic
        /// FROM the Server" is not usable, because a healthy Server legitimately sends nothing for
        /// long stretches: it only speaks to request a resync or to stop the stream.</para>
        /// </summary>
        int ConsecutiveSendFailures { get; }
    }
}
