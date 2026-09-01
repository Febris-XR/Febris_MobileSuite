// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.P2pCommunication.WiFi
{
    public class VideoStreamProcessing
    {
        private readonly IConfiguration _config;
        private ILogger _log;

        public VideoStreamProcessing()
        {
        }

        public VideoStreamProcessing(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public (bool videoNeeded, string videoName) VideoNeeded(JToken attachmentList)
        {
            try
            {
                bool videoNeeded = false;
                string videoName = string.Empty;
                foreach (var item in attachmentList)
                {
                    string contentType = (string)item["ContentType"].ToString().Replace("\r\n", string.Empty);
                    if (contentType == "video/mp4")
                    {
                        videoNeeded = true;
                        videoName = GetVideoName((string)item["Display"].ToString());
                    }
                }
                return (videoNeeded, videoName);
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                return (false, null);
            }
        }

        private string GetVideoName(string display)
        {
            try
            {
                string videoName = string.Empty;
                videoName = display.Split(':')[1];
                videoName = videoName.Replace("}", string.Empty).Replace(" ", string.Empty).Replace(@"\r\n", string.Empty).Replace("\"", string.Empty).Trim();
                return videoName;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                Guid fallbackName = Guid.NewGuid();
                _log.Error("video has been named: " + fallbackName.ToString());
                return fallbackName.ToString();
            }
        }

        #region H.264 receive path

        // The old path built an Image, a MemoryStream and an ImageSource per frame and let
        // Xamarin fully decode a PNG for each one. This one hands access units to MediaCodec,
        // which renders straight into the page's Surface. See docs/MOBILE_P2P_VIDEO.md 3.
        //
        // Frames are enqueued on the socket thread and drained by ONE pump. That split is the
        // point: submitting from the receive thread would let a slow decoder back-pressure
        // into the socket, and submitting via Task.Run per frame would decode them out of
        // order, which H.264 cannot survive.

        private static readonly object _pumpGate = new object();
        private static readonly VideoFrameQueue _frames = new VideoFrameQueue(FrameQueueCapacity);
        private static Task _pump;
        private static CancellationTokenSource _pumpCancel;
        private static DateTime _lastResyncRequest = DateTime.MinValue;

        /// <summary>Minimum gap between resync requests. Longer than a round trip plus the
        /// encoder's keyframe latency, so a request has time to take effect before the next.</summary>
        private static readonly TimeSpan ResyncRequestInterval = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Consecutive resync requests with no successfully decoded frame in between. Reset by
        /// the pump the moment anything decodes.
        /// </summary>
        private static int _unansweredResyncs;

        /// <summary>
        /// How many unanswered resyncs before the stream is declared lost.
        ///
        /// <para><b>Why a limit is needed at all.</b> Without one the Server asks forever. A
        /// Companion killed mid-stream was observed drawing 61 consecutive requests from a Server
        /// that never concluded anything was wrong, while the operator watched a frozen frame under
        /// a status line still reading connected. Nothing distinguished "the peer will answer
        /// shortly" from "the peer is gone", so the UI could only ever be optimistic.</para>
        ///
        /// <para>Five at a two second interval is roughly ten seconds of silence. That is
        /// comfortably longer than a resync legitimately takes (a round trip plus the encoder's
        /// keyframe latency) and short enough that an instructor is not misled for long.</para>
        /// </summary>
        private const int MaxUnansweredResyncs = 5;

        /// <summary>When a frame last ARRIVED, regardless of whether it decoded.</summary>
        private static DateTime _lastFrameArrival = DateTime.MinValue;

        /// <summary>
        /// How long the wire may be silent before an active stream is declared lost.
        ///
        /// <para><b>This is the case a resync budget cannot catch.</b> Resyncs are only requested
        /// in reaction to a frame being refused, so a peer that stops sending ENTIRELY, by being
        /// killed, crashing or walking out of range, produces no reaction at all: nothing arrives,
        /// nothing is refused, and the Server sits on a frozen last frame under a status line
        /// still reading connected. Silence has to be measured directly.</para>
        ///
        /// <para>Six seconds is comfortably longer than any legitimate gap. Even a completely
        /// static screen emits a keyframe on the encoder's two second interval, so three missed
        /// keyframe periods means the peer is not talking.</para>
        /// </summary>
        private static readonly TimeSpan VideoSilenceTimeout = TimeSpan.FromSeconds(6);

        private static DateTime _lastDiscardLog = DateTime.MinValue;

        /// <summary>Gap between display-gate discard warnings. The condition holds for every
        /// frame once it starts, so this is the difference between one useful line and 30 a
        /// second.</summary>
        private static readonly TimeSpan DiscardLogInterval = TimeSpan.FromSeconds(5);

        /// <summary>Roughly half a second at 30fps. Deep enough to ride out a scheduling
        /// hiccup, shallow enough that recovery is a stutter rather than a growing delay.</summary>
        private const int FrameQueueCapacity = 16;

        private static IScreenDecoder Decoder
        {
            get { return DependencyService.Get<IScreenDecoder>(); }
        }

        /// <summary>
        /// SPS/PPS from a _videoCodecConfig packet. Arrives before the first frame and again
        /// on every resync, so reconfiguring must be safe to repeat.
        /// </summary>
        internal static void ProcessCodecConfig(PacketHeaderModel header, byte[] body)
        {
            if (!IsForDisplayedDevice(header) || body == null || body.Length == 0)
            {
                return;
            }

            // ROUTED THROUGH THE QUEUE, not applied here.
            //
            // Configuring on this thread meant ScreenDecoder.Configure called Stop() and
            // released the MediaCodec while the pump could be inside SubmitFrame holding a
            // reference to it, which it captures under lock and then uses unlocked. That is a
            // use-after-release on a native object, and it presented as a caught-and-logged
            // "submit failed" rather than as the race it was. Going through the queue means
            // configure and submit both happen on the pump thread and cannot overlap at all.
            //
            // This is also the first production use of isCodecConfig: true, so the queue's
            // never-evict-codec-config protection stops being unreachable.
            lock (_pumpGate)
            {
                // A new configuration invalidates everything queued against the old one, and
                // the clear happens in the same critical section as the enqueue so the pump
                // cannot observe the gap between them.
                _frames.Clear();
                _frames.TryEnqueue(new VideoQueueItem(
                    body,
                    isKeyFrame: false,
                    isCodecConfig: true,
                    sequenceNumber: 0,
                    ptsMicros: 0,
                    width: header.Width ?? 0,
                    height: header.Height ?? 0));
            }

            StartPump();
        }

        /// <summary>
        /// One access unit from a _videoFrame packet. Runs on the socket thread and must not
        /// block, so it only enqueues.
        /// </summary>
        internal static void ProcessVideoFrame(PacketHeaderModel header, byte[] body)
        {
            if (!IsForDisplayedDevice(header) || body == null || body.Length == 0)
            {
                return;
            }

            VideoQueueItem item = new VideoQueueItem(
                body,
                header.IsKeyFrame ?? false,
                isCodecConfig: false,
                sequenceNumber: header.SequenceNumber ?? 0,
                ptsMicros: header.PtsMicros ?? 0);

            bool accepted;
            int dropped;
            lock (_pumpGate)
            {
                accepted = _frames.TryEnqueue(item);
                dropped = _frames.DroppedFrames;

                // Stamped on ARRIVAL, not on decode. The question this answers is whether the
                // peer is still talking, which is independent of whether we can decode what it
                // says. A stream that arrives but never decodes is the resync budget's problem.
                _lastFrameArrival = DateTime.UtcNow;
            }

            // RECOVERY. Every frame after a dropped non-IDR is corrupt until the next
            // keyframe, and the encoder only emits one every two seconds on its own. The queue
            // has counted drops since it was written and nothing ever read the counter, so the
            // stream degraded silently and stayed degraded.
            if (!accepted || dropped > 0)
            {
                lock (_pumpGate) { _frames.ClearDropCount(); }
                RequestResync(header, "dropped " + dropped + " frame(s)");
            }

            StartPump();
        }

        /// <summary>
        /// Ask the Companion to resend its codec config and force an IDR.
        ///
        /// Rate limited, because the conditions that trigger it are exactly the conditions
        /// that repeat: a decoder with no config refuses EVERY frame, so an unthrottled
        /// request would put one message on the wire per frame at 30fps, on a link already
        /// established to be struggling. One request per window, and the window is longer than
        /// a round trip plus the encoder's own keyframe latency so a request has time to work
        /// before another is sent.
        /// </summary>
        /// <summary>
        /// Marks the stream dead and makes the UI say so.
        ///
        /// <para>Clears <c>VideoStreamConnected</c>, which is what the operator-facing indicator
        /// binds to, and tears the local decoder down so a scarce hardware MediaCodec session is
        /// not held open for a stream nobody is receiving.</para>
        ///
        /// <para>Deliberately does NOT try to tell the peer. This path exists precisely for the
        /// case where the peer cannot be reached, so a send here would be the thing most likely to
        /// fail and it would buy nothing. The orderly stop already has its own path through
        /// <c>_endVideoStream</c>.</para>
        /// </summary>
        private static void DeclareStreamLost(string reason)
        {
            ConsoleFebrisP2pLogger.Instance.Log(
                FebrisP2pLogLevel.Warn, "video: stream declared lost (" + reason + ")");

            try
            {
                CompanionDeviceViewModel target = LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.DisplayedItem;
                if (target != null)
                {
                    target.VideoStreamConnected = false;
                }
            }
            catch (Exception ex)
            {
                ConsoleFebrisP2pLogger.Instance.Log(
                    FebrisP2pLogLevel.Warn, "video: could not clear the connected indicator: " + ex.Message);
            }

            // Local teardown, same reasoning as VideoStreamPage.OnDisappearing: releasing our own
            // resources must not depend on reaching anyone else.
            try { StopStream(); }
            catch (Exception) { }
        }

        /// <summary>
        /// Called by the pump whenever something actually decodes. Clears the unanswered-resync
        /// count so a stream that recovers is not eventually declared dead by an aging tally.
        /// </summary>
        private static void NoteStreamHealthy()
        {
            lock (_pumpGate)
            {
                _unansweredResyncs = 0;
            }
        }

        /// <summary>
        /// Whether the UI currently claims a stream is connected. This is the thing the silence
        /// check exists to correct, so it is also what decides whether the check applies: after an
        /// orderly stop the flag is already false and silence is simply the expected state.
        /// </summary>
        private static bool IsStreamMarkedConnected()
        {
            try
            {
                CompanionDeviceViewModel target = LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.DisplayedItem;
                return target != null && target.VideoStreamConnected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void RequestResync(PacketHeaderModel header, string reason)
        {
            DateTime now = DateTime.UtcNow;
            int unanswered;
            lock (_pumpGate)
            {
                if (now - _lastResyncRequest < ResyncRequestInterval)
                {
                    return;
                }
                _lastResyncRequest = now;
                unanswered = ++_unansweredResyncs;
            }

            // GIVE UP RATHER THAN ASK FOREVER. Past this point the peer has had ten seconds of
            // opportunity and produced nothing decodable, so the honest reading is that the
            // stream is gone rather than slow. Saying so is the whole point: an indicator that
            // can only ever say "connected" tells the operator nothing.
            if (unanswered > MaxUnansweredResyncs)
            {
                DeclareStreamLost("no decodable frame after " + MaxUnansweredResyncs + " resync requests");
                return;
            }

            ConsoleFebrisP2pLogger.Instance.Log(
                FebrisP2pLogLevel.Warn, "video: requesting resync (" + reason + ")");

            CompanionDeviceViewModel target = null;
            try { target = LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.DisplayedItem; }
            catch (Exception) { }

            if (target == null)
            {
                return;
            }

            var request = new PacketHeaderModel
            {
                BodyType = BodyType._videoResyncRequest,
                PacketName = "Video Resync Request",
                DeviceUniqueIdentifier = header?.DeviceUniqueIdentifier
            };

            byte[] frame = new FebrisP2pFrameBuilder().Build(request, new byte[0]);

            // Fire and forget deliberately: this runs on the socket receive thread and must
            // not block it, and a failed resync request is self-correcting because the next
            // dropped frame asks again.
            Task.Run(async () =>
            {
                try
                {
                    IWiFiP2pServer sender = DependencyService.Get<IWiFiP2pServer>();
                    if (sender != null) { await sender.SocketSender(frame, target); }
                }
                catch (Exception ex)
                {
                    ConsoleFebrisP2pLogger.Instance.Log(
                        FebrisP2pLogLevel.Warn, "video: resync request failed: " + ex.Message);
                }
            });
        }

        /// <summary>Clears any backlog from a previous session so a new stream does not begin
        /// by decoding stale frames against fresh parameters.</summary>
        internal static void ResetStream()
        {
            lock (_pumpGate)
            {
                _frames.Clear();
                _frames.ClearDropCount();

                // A fresh stream must not inherit the previous one's tally, or a session that
                // ended by being declared lost would immediately declare its successor lost too,
                // before that one had a chance to send anything.
                _unansweredResyncs = 0;
                _lastResyncRequest = DateTime.MinValue;
            }
        }

        /// <summary>Stops decoding and discards the backlog. Called when the Companion signals
        /// the end of a stream.</summary>
        internal static void StopStream()
        {
            CancellationTokenSource cancel;
            lock (_pumpGate)
            {
                cancel = _pumpCancel;
                _pumpCancel = null;
                _pump = null;
                _frames.Clear();
                _frames.ClearDropCount();
            }

            if (cancel != null)
            {
                try { cancel.Cancel(); } catch (ObjectDisposedException) { }
            }

            IScreenDecoder decoder = Decoder;
            if (decoder != null)
            {
                decoder.Stop();
            }
        }

        private static void StartPump()
        {
            lock (_pumpGate)
            {
                if (_pump != null && !_pump.IsCompleted)
                {
                    return;
                }
                _pumpCancel = new CancellationTokenSource();
                CancellationToken token = _pumpCancel.Token;
                _pump = Task.Run(() => PumpAsync(token));
            }
        }

        /// <summary>
        /// The single consumer. Exactly one of these may run: order is the only thing keeping
        /// the stream decodable, and two pumps would interleave access units.
        /// </summary>
        private static async Task PumpAsync(CancellationToken token)
        {
            int idleTicks = 0;

            while (!token.IsCancellationRequested)
            {
                VideoQueueItem item;
                bool got;
                lock (_pumpGate) { got = _frames.TryDequeue(out item); }

                if (!got)
                {
                    // SILENCE IS ITSELF A SIGNAL. Checked before retiring, because a peer that
                    // vanished produces exactly the same "no frames" condition as a stream that
                    // ended cleanly, and only one of those should leave the operator looking at a
                    // connected indicator.
                    if (IsStreamMarkedConnected())
                    {
                        DateTime lastArrival;
                        lock (_pumpGate) { lastArrival = _lastFrameArrival; }

                        if (lastArrival != DateTime.MinValue
                            && DateTime.UtcNow - lastArrival > VideoSilenceTimeout)
                        {
                            DeclareStreamLost("no frame for " + (int)VideoSilenceTimeout.TotalSeconds + "s");
                            lock (_pumpGate) { _pump = null; }
                            return;
                        }
                    }

                    // Retire after a couple of seconds of silence rather than spinning for the
                    // life of the app. Any later frame restarts the pump.
                    //
                    // NOTE this retirement is deliberately LONGER-LIVED than it looks while a
                    // stream is connected: the branch above keeps the pump alive past the idle
                    // budget until the silence timeout has had a chance to fire, so a vanished
                    // peer is reported rather than quietly forgotten.
                    if (++idleTicks > 400 && !IsStreamMarkedConnected())
                    {
                        lock (_pumpGate)
                        {
                            if (_frames.Count == 0) { _pump = null; return; }
                        }
                        idleTicks = 0;
                    }
                    await Task.Delay(5, token).ConfigureAwait(false);
                    continue;
                }

                idleTicks = 0;

                IScreenDecoder decoder = Decoder;
                if (decoder == null)
                {
                    continue;
                }

                try
                {
                    if (item.IsCodecConfig)
                    {
                        // Configure ON THE PUMP THREAD, in stream order with the frames around
                        // it. Returns false when the view has no Surface yet, which is normal
                        // rather than an error: the decoder caches the config and starts itself
                        // once the Surface arrives.
                        decoder.Configure(item.Payload, item.Width, item.Height);
                        continue;
                    }

                    if (decoder.SubmitFrame(item.Payload, item.PtsMicros, item.IsKeyFrame))
                    {
                        // Something decoded, so the stream is alive. This is the ONLY evidence of
                        // health that matters: frames arriving is not it, since a decoder with no
                        // configuration refuses every one of them.
                        NoteStreamHealthy();
                    }
                    else
                    {
                        // Refused. The important case is a decoder that was never configured,
                        // which happens whenever the one-shot codec config was lost: the
                        // oversize check rejected it, the display gate discarded it, or the
                        // Server attached mid-session and it had already gone by. Every frame
                        // is refused from then on and the screen stays black for the rest of
                        // the session, with no log and no recovery. This return value was
                        // discarded before, so nothing noticed.
                        RequestResync(null, decoder.IsRunning
                            ? "decoder refused a frame"
                            : "decoder has no codec configuration");
                    }
                }
                catch (Exception ex)
                {
                    // A single bad access unit must not kill the pump, or the stream stops
                    // permanently on one recoverable decoder complaint.
                    ConsoleFebrisP2pLogger.Instance.Log(
                        FebrisP2pLogLevel.Warn, "video pump: submit failed: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Only the device currently on screen is decoded. There is one decoder bound to one
        /// Surface, so accepting a second Companion's frames would interleave two unrelated
        /// H.264 streams into it and render garbage.
        /// </summary>
        private static bool IsForDisplayedDevice(PacketHeaderModel header)
        {
            if (header == null)
            {
                return false;
            }

            try
            {
                CompanionDeviceViewModel displayed =
                    LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.DisplayedItem;
                bool match = displayed?.CompanionDevice != null
                    && displayed.CompanionDevice.UniqueIdentifier == header.DeviceUniqueIdentifier;

                if (!match)
                {
                    LogDiscard(displayed == null
                        ? "no device is currently displayed"
                        : "frames are from '" + (header.DeviceUniqueIdentifier ?? "<null>") +
                          "' but the displayed device is '" +
                          (displayed.CompanionDevice?.UniqueIdentifier ?? "<null>") + "'");
                }
                return match;
            }
            catch (Exception ex)
            {
                LogDiscard("display lookup failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Say something when the display gate throws a stream away.
        ///
        /// The gate itself is correct: there is one decoder bound to one Surface, so a second
        /// Companion's frames would interleave two unrelated H.264 streams. Discarding
        /// SILENTLY is what was wrong. DisplayedItem has a non-UI writer, so the render target
        /// can detach with no operator action at all, and the symptom is a stream that simply
        /// stops with no log line anywhere while the Companion keeps capturing a learner's
        /// screen. That is the difference between a diagnosable drop and an invisible one.
        ///
        /// Rate limited for the same reason as the resync request: this condition is true for
        /// EVERY frame once it starts, so an unthrottled log would emit at 30fps.
        /// </summary>
        private static void LogDiscard(string reason)
        {
            DateTime now = DateTime.UtcNow;
            lock (_pumpGate)
            {
                if (now - _lastDiscardLog < DiscardLogInterval)
                {
                    return;
                }
                _lastDiscardLog = now;
            }

            ConsoleFebrisP2pLogger.Instance.Log(
                FebrisP2pLogLevel.Warn,
                "video: discarding inbound frames, " + reason +
                ". The Companion is still capturing; nothing has told it to stop.");
        }

        #endregion
    }
}
