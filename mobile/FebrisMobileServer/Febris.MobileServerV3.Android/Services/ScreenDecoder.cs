// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Media;
using Android.Views;
using Febris.MobileServerV3.Droid.Services;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using Xamarin.Forms;

[assembly: Dependency(typeof(ScreenDecoder))]

namespace Febris.MobileServerV3.Droid.Services
{
    /// <summary>
    /// Decodes the Companion's H.264 stream directly onto a display Surface.
    ///
    /// The old receive path built an Image, a MemoryStream and an ImageSource and fully
    /// decoded a PNG for every frame. This allocates nothing per frame: MediaCodec renders
    /// into the Surface itself and ReleaseOutputBuffer(index, true) is what puts a picture
    /// on screen.
    ///
    /// Registered through DependencyService, so it is a single instance shared by the
    /// renderer (which supplies the Surface) and the receive path (which supplies frames).
    /// That sharing is the reason for the explicit AttachSurface/DetachSurface pair: the
    /// two arrive independently and in no guaranteed order.
    ///
    /// NOT VERIFIED ON A DEVICE.
    /// </summary>
    public class ScreenDecoder : IScreenDecoder
    {
        private const string MimeType = "video/avc";
        private const int DequeueTimeoutUs = 10000;

        private readonly object _gate = new object();

        private MediaCodec _codec;
        private Surface _surface;
        private byte[] _pendingConfig;
        private int _pendingWidth;
        private int _pendingHeight;

        public bool IsReady
        {
            get { lock (_gate) { return _surface != null && _surface.IsValid; } }
        }

        public bool IsRunning
        {
            get { lock (_gate) { return _codec != null; } }
        }

        /// <summary>
        /// Called by the renderer when the SurfaceView's Surface becomes available. Not on
        /// IScreenDecoder because that assembly is netstandard2.0 and cannot name a Surface.
        ///
        /// If configuration already arrived (frames can beat the view onto the screen) the
        /// decoder is started here instead, which is why the config is cached.
        /// </summary>
        public void AttachSurface(Surface surface)
        {
            byte[] config;
            int width, height;

            lock (_gate)
            {
                _surface = surface;
                config = _pendingConfig;
                width = _pendingWidth;
                height = _pendingHeight;
            }

            if (config != null && surface != null && surface.IsValid)
            {
                Configure(config, width, height);
            }
        }

        /// <summary>Called by the renderer when the Surface goes away. The codec cannot
        /// outlive it, so this tears down rather than merely forgetting the reference.</summary>
        public void DetachSurface()
        {
            Stop();
            lock (_gate) { _surface = null; }
        }

        public bool Configure(byte[] codecConfig, int width, int height)
        {
            if (codecConfig == null || codecConfig.Length == 0)
            {
                return false;
            }

            lock (_gate)
            {
                // Cache unconditionally. A resync re-sends this, and if the Surface is not
                // up yet AttachSurface replays it.
                _pendingConfig = codecConfig;
                _pendingWidth = width;
                _pendingHeight = height;

                if (_surface == null || !_surface.IsValid)
                {
                    return false; // not an error, the view simply is not ready
                }
            }

            // Reconfiguring means a resolution change or a resync, so the previous session
            // is torn down first rather than reused.
            Stop();

            try
            {
                MediaFormat format = MediaFormat.CreateVideoFormat(MimeType, width, height);

                // csd-0 carries SPS+PPS. Without it the decoder cannot start, which is why
                // the codec-config frame is never dropped by the queue.
                format.SetByteBuffer("csd-0", Java.Nio.ByteBuffer.Wrap(codecConfig));

                MediaCodec codec = MediaCodec.CreateDecoderByType(MimeType);
                lock (_gate)
                {
                    codec.Configure(format, _surface, null, MediaCodecConfigFlags.None);
                    codec.Start();
                    _codec = codec;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScreenDecoder.Configure failed: " + ex.Message);
                Stop();
                return false;
            }
        }

        public bool SubmitFrame(byte[] accessUnit, long ptsMicros, bool isKeyFrame)
        {
            if (accessUnit == null || accessUnit.Length == 0)
            {
                return false;
            }

            MediaCodec codec;
            lock (_gate) { codec = _codec; }
            if (codec == null)
            {
                return false; // not configured yet, caller counts it as a drop
            }

            try
            {
                int inputIndex = codec.DequeueInputBuffer(DequeueTimeoutUs);
                if (inputIndex < 0)
                {
                    // No input buffer free. Refusing is correct: blocking here would stall
                    // the receive loop and back-pressure into the socket.
                    return false;
                }

                Java.Nio.ByteBuffer input = codec.GetInputBuffer(inputIndex);
                if (input == null)
                {
                    return false;
                }
                input.Clear();
                input.Put(accessUnit);

                codec.QueueInputBuffer(
                    inputIndex, 0, accessUnit.Length, ptsMicros,
                    isKeyFrame ? MediaCodecBufferFlags.KeyFrame : 0);

                DrainOutput(codec);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScreenDecoder.SubmitFrame failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Releases every ready output buffer TO THE SURFACE. The `true` argument is what
        /// actually renders the picture, and is the whole reason nothing is allocated here.
        /// </summary>
        private void DrainOutput(MediaCodec codec)
        {
            MediaCodec.BufferInfo info = new MediaCodec.BufferInfo();
            while (true)
            {
                int outputIndex = codec.DequeueOutputBuffer(info, 0);
                if (outputIndex < 0)
                {
                    return; // TryAgainLater or a format change, both benign
                }
                codec.ReleaseOutputBuffer(outputIndex, true);
            }
        }

        public void Stop()
        {
            MediaCodec codec;
            lock (_gate)
            {
                codec = _codec;
                _codec = null;
            }
            if (codec == null)
            {
                return;
            }

            try { codec.Stop(); } catch (Exception) { /* already stopped */ }
            try { codec.Release(); }
            catch (Exception ex) { Console.WriteLine("ScreenDecoder teardown: " + ex.Message); }
        }
    }
}
