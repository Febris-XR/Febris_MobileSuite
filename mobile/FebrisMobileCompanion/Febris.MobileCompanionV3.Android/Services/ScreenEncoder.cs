// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Hardware.Display;
using Android.Media;
using Android.Media.Projection;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Febris.MobileCompanionV3.Droid.Services;
using Febris.MobileCompanionV3.P2pCommunication.WiFi;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.P2pNetworking;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(ScreenEncoder))]

namespace Febris.MobileCompanionV3.Droid.Services
{
    /// <summary>
    /// Whole-screen H.264 capture, replacing the old Screenshot.CaptureAsync loop.
    ///
    /// The old path captured only the app's own view hierarchy, never the headset display,
    /// and pushed a whole PNG per frame at roughly 10fps, which demanded far more bandwidth
    /// than WiFi Direct actually delivers. This path captures the real screen and never
    /// allocates a bitmap at all: the VirtualDisplay renders straight into the MediaCodec
    /// input Surface, so pixels go compositor to encoder without touching managed memory.
    ///
    /// DEVICE-VERIFIED 2026-07-29. This header used to read "NOTHING IN THIS FILE HAS RUN. It
    /// compiles." That was true when written on 2026-07-26 and stopped being true three days later,
    /// and it was still here on 2026-08-26, where it caused a reader to conclude the whole pipeline
    /// was unrun. Tier 1 video ran end to end (fdd60f0): consent, 3,963 frames received, the Server
    /// rendering this device's real screen, and an orderly stop that released the VirtualDisplay
    /// with this process still alive. Tests 1.1, 1.2, 1.3, 1.4 and 1.6 pass.
    ///
    /// The encoder settings were retuned for monitoring rather than fidelity in 2498b2f. They are
    /// still the design doc's starting point in shape (docs/MOBILE_P2P_VIDEO.md 3.2) and are tuned
    /// against one bench, not characterised across devices.
    ///
    /// Registered through DependencyService, which activates a parameterless constructor, so
    /// configuration arrives via StartAsync rather than injection.
    /// </summary>
    public class ScreenEncoder : IScreenEncoder
    {
        private const string MimeType = "video/avc";
        private const int IFrameIntervalSeconds = 2;
        private const int DequeueTimeoutUs = 10000;

        private readonly object _gate = new object();

        private MediaProjection _projection;
        private VirtualDisplay _virtualDisplay;
        private MediaCodec _encoder;
        private Surface _inputSurface;
        private CancellationTokenSource _drainCts;
        private VideoFramePacketizer _packetizer;

        private int _width;
        private int _height;

        public bool IsRunning { get; private set; }

        /// <summary>SPS + PPS for the current session. Null until the encoder emits its
        /// codec-config output, which is normally the first buffer.</summary>
        public byte[] CodecConfig { get; private set; }

        private int _consecutiveSendFailures;

        /// <summary>See <see cref="IScreenEncoder.ConsecutiveSendFailures"/>. Read without a lock
        /// because it is a single int written through Interlocked and the session loop only needs
        /// it to be eventually right.</summary>
        public int ConsecutiveSendFailures
        {
            get { return Volatile.Read(ref _consecutiveSendFailures); }
        }

        /// <summary>
        /// Packages the cached SPS/PPS into a sendable frame, using the SAME packetizer call and
        /// the SAME negotiated dimensions as the primary emit path in <see cref="Emit"/>.
        ///
        /// The resync path used to build its own header and omitted the width and height, so the
        /// decoder on the far side got a MediaFormat of 0x0 and threw. Routing both senders through
        /// one construction point is what stops that recurring.
        /// </summary>
        public byte[] BuildCodecConfigFrame()
        {
            byte[] config = CodecConfig;
            if (config == null || config.Length == 0) return null;

            // _width and _height are the dimensions the encoder actually negotiated, which are not
            // necessarily the maxWidth/maxHeight that were requested.
            return _packetizer.BuildCodecConfig(config, _width, _height, 0);
        }

        public async Task<bool> StartAsync(int maxWidth, int maxHeight, int bitrateBps, int frameRate)
        {
            lock (_gate)
            {
                if (IsRunning)
                {
                    return true; // documented as idempotent
                }
            }

            try
            {
                // 1. The foreground service must already be running. From API 29
                //    CreateVirtualDisplay throws without one, and it must be a
                //    mediaProjection-typed service specifically.
                Android.App.Activity activity = Xamarin.Essentials.Platform.CurrentActivity;
                if (activity == null)
                {
                    Console.WriteLine("ScreenEncoder: no current activity, cannot request consent");
                    return false;
                }
                ScreenCaptureService.Start(activity);

                // 2. Consent. Declining is an ordinary outcome, not an error.
                Android.Content.Intent consent = await ScreenCaptureConsent.RequestAsync(activity);
                if (consent == null)
                {
                    Console.WriteLine("ScreenEncoder: screen capture consent was declined");
                    ScreenCaptureService.Stop(activity);
                    return false;
                }

                MediaProjectionManager manager = (MediaProjectionManager)
                    activity.GetSystemService(Android.Content.Context.MediaProjectionService);
                _projection = manager?.GetMediaProjection((int)Android.App.Result.Ok, consent);
                if (_projection == null)
                {
                    Console.WriteLine("ScreenEncoder: MediaProjection was not granted");
                    ScreenCaptureService.Stop(activity);
                    return false;
                }

                // 3. Resolve the capture size, capped and forced even. H.264 in yuv420p
                //    requires even dimensions on both axes.
                DisplayMetrics metrics = activity.Resources.DisplayMetrics;
                ComputeSize(metrics.WidthPixels, metrics.HeightPixels, maxWidth, maxHeight, out _width, out _height);

                // 4. Encoder, configured for Surface input.
                MediaFormat format = MediaFormat.CreateVideoFormat(MimeType, _width, _height);
                format.SetInteger(MediaFormat.KeyColorFormat, (int)MediaCodecCapabilities.Formatsurface);
                format.SetInteger(MediaFormat.KeyBitRate, bitrateBps);
                format.SetInteger(MediaFormat.KeyFrameRate, frameRate);
                format.SetInteger(MediaFormat.KeyIFrameInterval, IFrameIntervalSeconds);
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    // VBR suits screen content, which is mostly static then suddenly not.
                    format.SetInteger(MediaFormat.KeyBitrateMode, (int)BitrateMode.Vbr);
                }

                _encoder = MediaCodec.CreateEncoderByType(MimeType);
                _encoder.Configure(format, null, null, MediaCodecConfigFlags.Encode);
                _inputSurface = _encoder.CreateInputSurface();
                _encoder.Start();

                // 5. The VirtualDisplay renders DIRECTLY into the encoder's input surface.
                //    This is the zero-copy part: no bitmap is ever created.
                _virtualDisplay = _projection.CreateVirtualDisplay(
                    "FebrisCompanionCapture",
                    _width, _height, (int)metrics.DensityDpi,
                    DisplayFlags.Presentation,
                    _inputSurface, null, null);

                _packetizer = new VideoFramePacketizer(
                    DependencyService.Get<IDevice>()?.GetIdentifier());

                _drainCts = new CancellationTokenSource();
                CancellationToken token = _drainCts.Token;
                IsRunning = true;

                // 6. Drain on a background thread. A polling loop is used rather than
                //    MediaCodec.SetCallback because it keeps the lifetime explicit and
                //    avoids a Java callback object outliving teardown.
                _ = Task.Run(() => DrainLoop(token), token);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScreenEncoder.StartAsync failed: " + ex);
                await StopAsync();
                return false;
            }
        }

        public Task StopAsync()
        {
            lock (_gate)
            {
                IsRunning = false;
            }

            SafeDispose("drain", () => { _drainCts?.Cancel(); _drainCts?.Dispose(); _drainCts = null; });
            SafeDispose("virtual display", () => { _virtualDisplay?.Release(); _virtualDisplay = null; });
            SafeDispose("encoder", () =>
            {
                if (_encoder != null)
                {
                    try { _encoder.Stop(); } catch (Exception) { /* already stopped */ }
                    _encoder.Release();
                    _encoder = null;
                }
            });
            SafeDispose("input surface", () => { _inputSurface?.Release(); _inputSurface = null; });
            SafeDispose("projection", () => { _projection?.Stop(); _projection = null; });

            CodecConfig = null;
            _packetizer = null;

            Android.App.Activity activity = Xamarin.Essentials.Platform.CurrentActivity;
            if (activity != null)
            {
                ScreenCaptureService.Stop(activity);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Forces the next output to be an IDR. Needed whenever a viewer joins late or the
        /// send side has dropped frames, because every frame after a dropped non-IDR is
        /// corrupt until the next keyframe.
        /// </summary>
        public void RequestKeyFrame()
        {
            try
            {
                if (_encoder == null || Build.VERSION.SdkInt < BuildVersionCodes.M)
                {
                    return;
                }
                Bundle parameters = new Bundle();
                parameters.PutInt(MediaCodec.ParameterKeyRequestSyncFrame, 0);
                _encoder.SetParameters(parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScreenEncoder.RequestKeyFrame failed: " + ex.Message);
            }
        }

        private void DrainLoop(CancellationToken token)
        {
            MediaCodec.BufferInfo info = new MediaCodec.BufferInfo();

            while (!token.IsCancellationRequested)
            {
                int index;
                try
                {
                    index = _encoder.DequeueOutputBuffer(info, DequeueTimeoutUs);
                }
                catch (Exception ex)
                {
                    // Teardown races the drain: the encoder can be released underneath us.
                    if (!token.IsCancellationRequested)
                    {
                        Console.WriteLine("ScreenEncoder drain stopped: " + ex.Message);
                    }
                    return;
                }

                if (index < 0)
                {
                    continue; // TryAgainLater or a format change, both benign here
                }

                try
                {
                    Java.Nio.ByteBuffer buffer = _encoder.GetOutputBuffer(index);
                    if (buffer != null && info.Size > 0)
                    {
                        byte[] payload = new byte[info.Size];
                        buffer.Position(info.Offset);
                        buffer.Get(payload, 0, info.Size);
                        Emit(payload, info);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ScreenEncoder failed to read an output buffer: " + ex.Message);
                }
                finally
                {
                    try { _encoder.ReleaseOutputBuffer(index, false); } catch (Exception) { }
                }
            }
        }

        private void Emit(byte[] payload, MediaCodec.BufferInfo info)
        {
            bool isConfig = (info.Flags & MediaCodecBufferFlags.CodecConfig) != 0;
            bool isKeyFrame = (info.Flags & MediaCodecBufferFlags.KeyFrame) != 0;

            byte[] frame;
            try
            {
                if (isConfig)
                {
                    // SPS + PPS. Cached because a late-joining viewer cannot configure its
                    // decoder without it and must be able to ask for it again.
                    CodecConfig = payload;
                    frame = _packetizer.BuildCodecConfig(payload, _width, _height, 0);
                }
                else
                {
                    frame = _packetizer.BuildVideoFrame(
                        payload, isKeyFrame, info.PresentationTimeUs, _width, _height, 0);
                }
            }
            catch (ArgumentException ex)
            {
                // The packetizer rejects an oversize access unit rather than letting the
                // receiver drop the connection. Losing one frame is the right trade.
                Console.WriteLine("ScreenEncoder dropped a frame: " + ex.Message);
                return;
            }

            // Still fire and forget: blocking the drain thread on the socket would stall the
            // encoder and back pressure into the compositor. A bounded send queue with an
            // explicit drop policy is the follow-up (docs/MOBILE_P2P_VIDEO.md 3.4).
            //
            // The RESULT is no longer discarded, though. It is the only evidence this side has
            // that anyone is still receiving, and without it a dead Server leaves the Companion
            // capturing indefinitely. See ConsecutiveSendFailures.
            IWiFiP2pServer sender = DependencyService.Get<IWiFiP2pServer>();
            if (sender == null) return;
            _ = TrackSendAsync(sender, frame);
        }

        /// <summary>
        /// Sends a frame and records whether it got out, without making the caller wait.
        /// </summary>
        private async Task TrackSendAsync(IWiFiP2pServer sender, byte[] frame)
        {
            bool sent;
            try
            {
                sent = await sender.SocketSender(frame).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A throw is a failure like any other here. The session loop decides what a run
                // of them means, and it must not be denied that information by an exception.
                sent = false;
            }

            if (sent)
            {
                Interlocked.Exchange(ref _consecutiveSendFailures, 0);
            }
            else
            {
                Interlocked.Increment(ref _consecutiveSendFailures);
            }
        }

        /// <summary>Caps the capture size and forces both axes even, which yuv420p requires.</summary>
        internal static void ComputeSize(int sourceWidth, int sourceHeight, int maxWidth, int maxHeight, out int width, out int height)
        {
            double scale = Math.Min(
                maxWidth <= 0 ? 1.0 : (double)maxWidth / sourceWidth,
                maxHeight <= 0 ? 1.0 : (double)maxHeight / sourceHeight);
            if (scale > 1.0 || double.IsNaN(scale) || double.IsInfinity(scale))
            {
                scale = 1.0; // never upscale
            }

            width = MakeEven((int)Math.Round(sourceWidth * scale));
            height = MakeEven((int)Math.Round(sourceHeight * scale));
        }

        private static int MakeEven(int value)
        {
            if (value < 2) return 2;
            return value - (value % 2);
        }

        private static void SafeDispose(string what, Action action)
        {
            try { action(); }
            catch (Exception ex) { Console.WriteLine("ScreenEncoder teardown (" + what + "): " + ex.Message); }
        }
    }
}
