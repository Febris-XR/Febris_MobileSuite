// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.Resources;
using Febris.MobileCompanionV3.Utilities;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Febris.MobileCompanionV3.P2pCommunication.WiFi
{
    public class VideoStreamProcessing
    {
        #region variables
        IWiFiService wifi = DependencyService.Get<IWiFiService>();
        IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
        string _uniqueId = string.Empty;
        public VideoStreamProcessing()
        {
            _uniqueId = DependencyService.Get<IDevice>().GetIdentifier();
        }
        
        #endregion

        // Encode settings, retuned 2026-07-28 against the ACTUAL purpose of this stream.
        //
        // WHAT THIS FEED IS FOR. An instructor watching a learner in order to give them
        // instructions. It is a monitoring view, not a recording and not a demonstration.
        // That fixes the priority order, and it is not the usual one:
        //
        //   1. Do not interfere with the Companion. It is running the learner's simulation,
        //      and every pixel this encoder touches is battery, heat and GPU time taken from
        //      that. A stream that degrades the session it exists to observe is worse than no
        //      stream at all.
        //   2. Legible enough to see what the learner is doing.
        //   3. Latency. Explicitly NOT important. An instructor speaking to a learner does
        //      not react frame by frame, and a second of lag does not change the instruction
        //      they give.
        //
        // The previous values were the design doc's proposed starting point (3.2), chosen
        // before that purpose was pinned down and never run on hardware. 720p30 at 4 Mbps is
        // a fidelity-first configuration and this is not a fidelity-first feature.
        //
        // 540p15 at 1.2 Mbps cuts the encoder's pixel rate by roughly 3.5x (27.6 Mpx/s down
        // to 7.8) and the radio duty cycle by more than 3x. Those are the terms the Companion
        // actually pays in. Nothing about reading a learner's screen needs 30fps.
        //
        // These are MAXIMA, and the capture preserves aspect ratio inside the box, so a
        // portrait Companion is letterboxed rather than stretched.
        private const int MaxCaptureWidth = 960;
        private const int MaxCaptureHeight = 540;
        private const int BitrateBps = 1_200_000;
        private const int FrameRate = 15;

        /// <summary>
        /// Consecutive failed frame sends before capture stops on its own.
        ///
        /// <para>At <see cref="FrameRate"/> this is roughly three seconds of a link that accepts
        /// nothing. Long enough that a brief stall cannot trip it, short enough that a learner's
        /// screen is not captured for long after the last person watching it has gone.</para>
        ///
        /// <para>Set generously ON PURPOSE. The failure this guards against is capture continuing
        /// with nobody receiving, and stopping a second late is a far smaller harm than a stream
        /// that ends every time the radio hiccups.</para>
        /// </summary>
        private const int MaxConsecutiveSendFailures = 45;

        /// <summary>
        /// Supervises a screen-stream session.
        ///
        /// This used to BE the stream: a loop calling Screenshot.CaptureAsync and pushing a
        /// whole PNG every 100ms. That had two independent defects. It captured only the
        /// app's own view hierarchy, never the headset display, so it showed the wrong thing
        /// entirely. And a 1080p PNG at 10fps demands roughly 80-240 Mbps against WiFi
        /// Direct's real 20-50, so it oversubscribed the link and stalled.
        ///
        /// Capture and encode now live behind IScreenEncoder, which pushes H.264 access
        /// units through the socket itself. This method only owns the session LIFETIME:
        /// start, wait, stop. It no longer touches frames.
        /// </summary>
        public static async Task VideoStream()
        {
            StatusUpdateHelper.VideoStreamRunning();
            Console.WriteLine("start screen stream");

            IScreenEncoder encoder = DependencyService.Get<IScreenEncoder>();
            if (encoder == null)
            {
                // No platform implementation registered on this head.
                Console.WriteLine("no IScreenEncoder is registered, cannot stream");
                LocalHardwareStaticDetails.StreamVideo = false;
                StatusUpdateHelper.VideoStreamEnded();
                return;
            }

            try
            {
                bool started = await encoder.StartAsync(MaxCaptureWidth, MaxCaptureHeight, BitrateBps, FrameRate);
                if (!started)
                {
                    // The usual cause is the user declining the capture-consent dialog,
                    // which is an ordinary outcome rather than a failure.
                    Console.WriteLine("screen capture did not start (consent declined or unavailable)");
                    LocalHardwareStaticDetails.StreamVideo = false;
                    return;
                }

                // Supervise only. Frames leave via the encoder, so this loop must not do any
                // per-frame work, and a longer poll here costs nothing.
                //
                // THREE INDEPENDENT REASONS TO STOP, and the third is the one that protects a
                // learner. StreamVideo covers an orderly stop from either side. WiFiConnected
                // covers the WiFi Direct group going away, which is the walk-out-of-range case.
                // Neither notices a Server APPLICATION that died while the group stayed up: both
                // stay true, and capture would continue indefinitely with nobody receiving it.
                string stopReason = null;
                while (true)
                {
                    if (!LocalHardwareStaticDetails.StreamVideo)
                    {
                        stopReason = "stream was stopped";
                        break;
                    }

                    if (!(LocalHardwareStaticDetails.StaticMainVM?.HomeVM?.WiFiConnected ?? false))
                    {
                        stopReason = "WiFi Direct connection lost";
                        break;
                    }

                    if (encoder.ConsecutiveSendFailures >= MaxConsecutiveSendFailures)
                    {
                        stopReason = "no frame has reached the Server in "
                            + MaxConsecutiveSendFailures + " consecutive sends, assuming nobody is receiving";
                        break;
                    }

                    await Task.Delay(250);
                }

                Console.WriteLine("screen stream stopping: " + stopReason);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // Deterministic teardown on every path, including the WiFi drop above.
                // Leaving MediaProjection running would keep capturing a learner's screen
                // after the session ended, and keep the foreground notification up.
                try { await encoder.StopAsync(); }
                catch (Exception ex) { Console.WriteLine("encoder teardown: " + ex.Message); }

                await AnnounceStreamEnded();

                LocalHardwareStaticDetails.StreamVideo = false;
                StatusUpdateHelper.VideoStreamEnded();
                Console.WriteLine("end screen stream");
            }
        }

        /// <summary>
        /// Tell the Server the stream is over.
        ///
        /// WHY THIS DID NOT EXIST AND HAD TO. `_endVideoStream` is documented in the BodyType
        /// enum as "the stream STOP in both directions", and the Server has a full inbound
        /// handler for it that tears down the decoder and cancels the frame pump. The Companion
        /// had no send site anywhere, so that handler was unreachable from the wire and every
        /// Companion-side ending was invisible to the Server: consent revoked, encoder failure,
        /// WiFi drop, app backgrounding. The Server kept a scarce hardware MediaCodec session
        /// and its Surface binding alive for a dead stream, and the operator's status line still
        /// read connected over a frozen last frame.
        ///
        /// Best-effort by design. This runs in a finally block on paths that include "the WiFi
        /// went away", so the send will often fail, and that must never mask the teardown that
        /// already happened above.
        ///
        /// Stamped with this device's identifier because the Server routes the stop through
        /// IsForDisplayedDevice, which compares exactly that field.
        /// </summary>
        private static async Task AnnounceStreamEnded()
        {
            try
            {
                IWiFiP2pServer sender = DependencyService.Get<IWiFiP2pServer>();
                if (sender == null)
                {
                    return;
                }

                var header = new PacketHeaderModel
                {
                    BodyType = BodyType._endVideoStream,
                    PacketName = "End Video Stream",
                    DeviceUniqueIdentifier = DependencyService.Get<IDevice>()?.GetIdentifier()
                };

                await sender.SocketSender(new FebrisP2pFrameBuilder().Build(header, new byte[0]));
            }
            catch (Exception ex)
            {
                // The link is frequently already gone on this path. Log and move on.
                Console.WriteLine("could not announce stream end: " + ex.Message);
            }
        }
    }
}
