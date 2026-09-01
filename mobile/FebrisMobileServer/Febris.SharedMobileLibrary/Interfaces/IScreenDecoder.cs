// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
namespace Febris.SharedMobileLibrary.Interfaces
{
    /// <summary>
    /// Decodes the Companion's H.264 stream straight onto a display Surface.
    ///
    /// Replaces the old per-frame path, which built an Image, a MemoryStream and an
    /// ImageSource and fully decoded a PNG for every frame received. Decoding to a Surface
    /// allocates nothing per frame: MediaCodec renders directly into the view.
    ///
    /// PRIMITIVES ONLY, because this assembly is netstandard2.0 and cannot name a Surface.
    /// The platform implementation obtains its Surface from the renderer rather than across
    /// this interface, which is why <see cref="IsReady"/> exists separately from
    /// <see cref="IsRunning"/>: frames can arrive before the view has a Surface to draw on.
    ///
    /// See docs/MOBILE_P2P_VIDEO.md section 3.
    /// </summary>
    public interface IScreenDecoder
    {
        /// <summary>True once a display Surface is attached. Frames submitted before this
        /// cannot be rendered and are refused rather than queued indefinitely.</summary>
        bool IsReady { get; }

        /// <summary>True once configured and started.</summary>
        bool IsRunning { get; }

        /// <summary>
        /// Configures the decoder from the SPS/PPS carried by a _videoCodecConfig frame.
        /// Safe to call again on a resync or a resolution change, which tears the previous
        /// session down first. Returns false when the decoder could not be started.
        /// </summary>
        bool Configure(byte[] codecConfig, int width, int height);

        /// <summary>
        /// Submits one access unit for display. Returns false when it could not be accepted,
        /// which the caller should treat as a dropped frame rather than an error.
        /// </summary>
        bool SubmitFrame(byte[] accessUnit, long ptsMicros, bool isKeyFrame);

        /// <summary>Deterministic teardown. Safe to call when not running.</summary>
        void Stop();
    }
}
