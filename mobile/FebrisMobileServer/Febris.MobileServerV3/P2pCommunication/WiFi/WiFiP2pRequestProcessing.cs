// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.DataLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Crypto = Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using Febris.SharedMobileLibrary.Utilites;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.P2pCommunication.WiFi
{
    public class WiFiP2pRequestProcessing
    {
        #region variables
        IWiFiService wifi = DependencyService.Get<IWiFiService>();
        #endregion

        #region constructor
        public WiFiP2pRequestProcessing()
        {
            //wifiService = 

        }
        #endregion

        #region Helpers

        #region encoding/decoding
        #region encoding 
        //public static byte[] String2Bytes(string input)
        //{
        //    return Encoding.ASCII.GetBytes(input);
        //}
        #endregion

        #region decoding
        //public static int Bytes2Int(byte[] input)
        //{
        //    return BitConverter.ToInt32(input, 0);
        //}
        //public static string Bytes2String(byte[] input)
        //{
        //    return Encoding.Default.GetString(input);
        //}

        #endregion

        #region Header
        public static async Task<PacketHeaderModel> ParsingJsonStringToHeaderModel(string input)
        {
            PacketHeaderModel output = new PacketHeaderModel();
            try
            {
                // MP2P-9: dead no-op left as a marker. `string.Replace` returns a new
                // string (C# strings are immutable); the return value here was never
                // assigned back, so this call did nothing. The original intent appears
                // to have been input sanitization to strip stray forward-slashes from
                // the JSON header before parsing, but the no-op meant any such characters
                // sailed through to the deserializer. Today's framer (MP2P-1/2) emits
                // clean UTF-8 JSON, so the sanitization isn't needed. Kept as a marker
                // so future readers don't reintroduce the same bug.
                // input.Replace(@"/", string.Empty);
                output = JsonConvert.DeserializeObject<PacketHeaderModel>(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return output;
        }

        public static async Task<PacketHeaderModel> ParsingByteArrayToHeaderModel(byte[] input)
        {
            PacketHeaderModel output = new PacketHeaderModel();
            string jsonString = string.Empty;
            try
            {
                jsonString = await DataConversionUtility.Bytes2String(input);
                output = (PacketHeaderModel)JsonConvert.DeserializeObject(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return output;
        }
        #endregion
        #endregion

        public async static Task<(string Header, byte[] Body)> SeperateHeaderAndBody(byte[] input)
        {
            string header = string.Empty;
            byte[] body = { };
            byte[] headerArray = { };
            byte[] headerSize = { };
            StringBuilder sb = new StringBuilder();
            int bytesRead = 0;
            int headerByteLength = 0;

            try
            {
                //get number of bytes in the header
                headerSize = input.Take(LocalHardwareStaticDetails.ExpectedHeaderLength).ToArray();
                headerByteLength = await DataConversionUtility.Bytes2Int(headerSize);

                //seperate header               
                headerArray = input
                    .Skip(LocalHardwareStaticDetails.ExpectedHeaderLength)
                    .Take(headerByteLength)
                    .ToArray();
                header = await DataConversionUtility.Bytes2String(headerArray);

                //seperate body
                body = input.Skip(headerByteLength + LocalHardwareStaticDetails.ExpectedHeaderLength).ToArray();
                Console.WriteLine(header + "/n" + await DataConversionUtility.Bytes2String(body));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (header, body);
        }
        #endregion

        #region Process downloaded data
        /// <summary>
        /// Marks the device that sent this frame as connected, keyed on the identifier in the
        /// header.
        ///
        /// <para><b>Any frame counts, not just <c>_initalize</c>.</b> A frame arriving from a known
        /// device IS the proof that it is connected, and it is the only proof that does not depend
        /// on the WiFi MAC. That matters because the MAC is derived by ELIMINATION from the group's
        /// client list, which cannot separate two devices that are both unresolved, so with two
        /// Companions paired neither ever gets one (issue 16).</para>
        ///
        /// <para>This was originally hooked to <c>_initalize</c> alone, which was not enough: a
        /// Companion sends that once at connect time, BEFORE the pairing ceremony creates its
        /// record, so the lookup found nothing and nothing ever re-marked it. Both devices then sat
        /// on "Trying To Connect..." with grey WiFi icons while visibly sending status updates
        /// every sixty seconds.</para>
        /// </summary>
        /// <summary>When each device last sent ANY accepted frame, keyed by its identifier.</summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _lastControlArrival
            = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();

        private static int _silenceWatchStarted;

        /// <summary>
        /// How long a CONNECTED device may be silent before the flag clears.
        ///
        /// <para>A healthy paired Companion sends a status update every sixty seconds, so 150
        /// seconds means two consecutive updates missed plus margin. This is the missing half of
        /// <see cref="NotePeerIsTalking"/>: marking on traffic without unmarking on silence
        /// produced a flag that could only ever say connected, observed as the Server claiming
        /// connected while both Companions correctly said disconnected (issue 20).</para>
        /// </summary>
        private const int ControlSilenceTimeoutSeconds = 150;

        /// <summary>
        /// Clears <c>WiFiConnected</c> on devices that have gone silent. Deliberately conservative,
        /// it only clears a device it has an arrival stamp for, so devices marked connected by
        /// other paths are left alone until they talk at least once.
        /// </summary>
        private static async Task ControlSilenceWatchAsync()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                    List<CompanionDeviceViewModel> list = LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.ItemList;
                    if (list == null) continue;

                    foreach (CompanionDeviceViewModel device in list.ToList())
                    {
                        string id = device?.CompanionDevice?.UniqueIdentifier;
                        if (string.IsNullOrWhiteSpace(id) || device.WiFiConnected == false) continue;

                        DateTime last;
                        if (_lastControlArrival.TryGetValue(id, out last)
                            && DateTime.UtcNow - last > TimeSpan.FromSeconds(ControlSilenceTimeoutSeconds))
                        {
                            Console.WriteLine("peer '" + id + "' silent for " + ControlSilenceTimeoutSeconds
                                + "s, marking disconnected");
                            device.WiFiConnected = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log and keep going. A watchdog that dies on its first error is the
                    // service-listener-loop mistake again (issue 14 fault C).
                    Console.WriteLine("control silence watch: " + ex.Message);
                }
            }
        }

        private static void NotePeerIsTalking(PacketHeaderModel header)
        {
            try
            {
                string id = header?.DeviceUniqueIdentifier;
                if (string.IsNullOrWhiteSpace(id)) return;

                _lastControlArrival[id] = DateTime.UtcNow;
                if (System.Threading.Interlocked.CompareExchange(ref _silenceWatchStarted, 1, 0) == 0)
                {
                    Task.Run(() => ControlSilenceWatchAsync());
                }

                CompanionDeviceViewModel device = LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.ItemList?
                    .Where(i => i?.CompanionDevice != null && i.CompanionDevice.UniqueIdentifier == id)
                    .FirstOrDefault();

                if (device != null && !device.WiFiConnected)
                {
                    Console.WriteLine("peer '" + id + "' is talking, marking connected");
                    device.WiFiConnected = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("NotePeerIsTalking failed: " + ex.Message);
            }
        }

        internal static async Task<bool> ProcessDownloadedData(PacketHeaderModel header, byte[] body)
        {
            //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Processing Downloaded Data";
            bool processed = false;

            // Before dispatch, and for EVERY body type. Reaching here means the frame already
            // passed the direction gate and the peer gate, so it is genuine traffic from a peer we
            // accept, which is exactly the condition "connected" is meant to describe.
            NotePeerIsTalking(header);

            switch (header.BodyType)
            {
                case BodyType._junk:
                    {
                        // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers (per-packet anti-pattern).
                        body = null;
                        return false;
                    }
                case BodyType._initalize:
                    {
                        await ProcessInitalization (header);
                        //processed = ProcessAcknowledge(header, body);
                        break;
                    }
                case BodyType._acknowledge:
                    {
                        processed = await ProcessAcknowledge(header, body);
                        break;
                    }
                // DISABLED 2026-07-29. The Companion sends only statements; there is no live
                // sender for _fileUpload on either tier. Commented out rather than deleted at
                // the owner's instruction. P2pPeerAuthorization now refuses the type outright,
                // so this case is doubly unreachable. Deletion is tracked in the roadmap.
                //case BodyType._fileUpload:
                //    {
                //        processed = await ProcessFileUpload(header, body);
                //        break;
                //    }
                case BodyType._statusUpdate:
                    {
                        processed = await ProcessStatusUpdate(header, body);
                        break;
                    }
                case BodyType._statement:
                    {
                        processed = await ProcessStatement(header, body);
                        break;
                    }
                // DISABLED 2026-07-29. Recorded-video FILE upload, distinct from the live screen
                // stream (_videoFrame / _videoCodecConfig), which is untouched. No live sender
                // exists on either tier. It also resolved against StatementPath rather than a
                // recordings path, so anything that did arrive landed among the statements.
                //case BodyType._video:
                //    {
                //        processed = await ProcessVideo(header, body);
                //        break;
                //    }
                case BodyType._module:
                    {
                        processed = await ProcessModule(header, body);
                        break;
                    }
                // DISABLED 2026-07-29. Handled on both tiers with no single owner and no live
                // sender anywhere.
                //case BodyType._genericString:
                //    {
                //        processed = await ProcessGenericString(header, body);
                //        break;
                //    }
                case BodyType._videoStream:
                    {
                        processed = await ProcessStartVideoStream(header, body);
                        break;
                    }
                case BodyType._endVideoStream:
                    {
                        processed = await ProcessEndVideoStream(header, body);
                        break;
                    }
                // NO CASE FOR _videoStreamStart. It was added here during the step 6 decode
                // work and was dead the moment it was written: the enum documents 671 as
                // "Server to Companion. Start capture", so the Server SENDS it and never
                // receives it. The direction gate now refuses it inbound, which would have
                // made this case unreachable anyway.
                //
                // CORRECTED 2026-07-28. This used to add that "the Server cannot yet command
                // capture to start", which does not follow from the premise and is wrong.
                // Starting a stream works, over the DEPRECATED _videoStream (667), which the
                // direction table classifies as bidirectional and which the Companion
                // dispatches to StartVideoStream: DeviceInfoModal ->
                // RequestHelper.VideoStreamRequest -> 667 -> Companion _videoStream ->
                // VideoStreamProcessing -> IScreenEncoder.
                //
                // What IS true is that 671 is dead contract in the other direction: legal
                // inbound at the Companion, no case there, so the gate admits it and the
                // default arm drops it silently. Either wire it and move the send onto it, or
                // delete it. Tracked in docs/MOBILE_P2P_VIDEO.md 7.6.
                case BodyType._pairingResponse:
                    {
                        // The Companion answered our pairing request with its ephemeral public
                        // key. Both sides can now derive the six-digit code.
                        processed = Crypto.P2pPairingCoordinator.CompleteExchange(body, header?.DeviceUniqueIdentifier);
                        break;
                    }
                case BodyType._videoCodecConfig:
                    {
                        processed = ProcessVideoCodecConfig(header, body);
                        break;
                    }
                case BodyType._videoFrame:
                    {
                        processed = ProcessVideoFrame(header, body);
                        break;
                    }
                default:
                    {
                        // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers (per-packet anti-pattern).
                        body = null;
                        return false;
                    }

                    #region tried to convert everything over not sure if I got everything
                    //case BodyType._video:
                    //    processed = ProcessVideo(header, body);
                    //    break;
                    //case BodyType._genericString:
                    //    processed = ProcessGenericString(header, body);
                    //    break;
                    //case BodyType._videoStream:
                    //    processed = StartVideoStream(header, body);
                    //    break;
                    //case BodyType._endVideoStream:
                    //    processed = ProcessEndVideoStream(header, body);
                    //    break;
                    //case BodyType._module:
                    //    processed = ProcessModule(header, body);
                    //    break;
                    //case BodyType._removeModule:
                    //    processed = ProcessModuleRemoval(header, body);
                    //    break;
                    //case BodyType._removeZippedModule:
                    //    processed = ProcessModuleZipRemoval(header, body);
                    //    break;
                    //case BodyType._removeApp:
                    //    processed = ProcessAppRemoval(header, body);
                    //    break;
                    //case BodyType._installModule:
                    //    processed = ProcessModuleInstall(header, body);
                    //    break;
                    //case BodyType._uninstallModule:
                    //    processed = ProcessModuleUninstall(header, body);
                    //    break;
                    //case BodyType._reinstallModule:
                    //    processed = ProcessModuleReinstall(header, body);
                    //    break;
                    //case BodyType._statusUpdate:
                    //    {
                    //        processed = ProcessStatusUpdate(header, body);
                    //        break;
                    //    }
                    //case BodyType._statement:
                    //    {
                    //        processed = ProcessStatement(header, body);
                    //        break;
                    //    }
                    #endregion

            }
            #region bunch of if statements
            //bool processed = false;
            //if (header.BodyType == BodyType._junk)
            //{
            //    body = null;
            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    return false;
            //}
            //else if (header.BodyType == BodyType._initalize)
            //{
            //    //PairDevice.TestHeader(header);
            //    ProcessInitalization(header);
            //}
            //else if (header.BodyType == BodyType._acknowledge)
            //{
            //    processed = ProcessAcknowledge(header, body);
            //    break;
            //}
            //else if (header.BodyType == BodyType._fileUpload)
            //{
            //    processed = ProcessFileUpload(header, body);
            //}
            //else if (header.BodyType == BodyType._statusUpdate)
            //{
            //    processed = ProcessStatusUpdate(header, body);
            //}
            //else if (header.BodyType == BodyType._statement)
            //{
            //    processed = ProcessStatement(header, body);
            //}
            //else if (header.BodyType == BodyType._video)
            //{
            //    processed = ProcessVideo(header, body);
            //}
            //else if (header.BodyType == BodyType._module)
            //{
            //    processed = ProcessModule(header, body);
            //}
            //else if (header.BodyType == BodyType._genericString)
            //{
            //    processed = ProcessGenericString(header, body);
            //}
            //else if (header.BodyType == BodyType._videoStream)
            //{
            //    processed = ProcessStartVideoStream(header, body);
            //}
            //else if (header.BodyType == BodyType._endVideoStream)
            //{
            //    processed = ProcessEndVideoStream(header, body);
            //}
            //default:
            //{
            //    body = null;
            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    return false;
            //}

            #endregion            
            return processed;
        }


        //internal static bool ProcessDownloadedData(PacketHeaderModel header, byte[] body)
        //{

        //    return processed;
        //}

        #region ProcessFileUpload 
        private static async Task<bool> ProcessFileUpload(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                //use the header to find the device
                string data = await DataConversionUtility.Bytes2String(body);
                //parse json
                LocalJSONHandler _jSONHandler = new LocalJSONHandler();
                _jSONHandler.DeserialiseHardwareStatusUpdate(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return processed;
        }
        #endregion
        #region ProcessStatusUpdate
        private static async Task<bool> ProcessStatusUpdate(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                //use the header to find the device
                string data = await DataConversionUtility.Bytes2String(body);
                //parse json
                LocalJSONHandler _jSONHandler = new LocalJSONHandler();
                _jSONHandler.DeserialiseHardwareStatusUpdate(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return processed;
        }
        #endregion
        #region statement
        private static async Task<bool> ProcessStatement(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            string data = await DataConversionUtility.Bytes2String(body);

            // EVERY TERMINAL PATH BELOW MUST ACK, INCLUDING THE FAILURES. Silence is not a
            // neutral outcome here: the Companion's ack timeout treats it as "delivery
            // uncertain" and marks the statement Uploaded best-effort (LoopLogic.cs:402-416),
            // which removes it from the unsent set and queues it for deletion. An explicit
            // failure status takes the other branch and leaves it for retry. So a NACK
            // preserves the statement and silence destroys it.

            // PacketName is an attacker-authored JSON string, and Path.Combine discards its
            // first argument when the second is rooted, so this used to write wherever the
            // sender asked. Refusing is correct; refusing SILENTLY was not, and that was a
            // regression introduced with the path-escape fix.
            if (!P2pSafeFileName.TryResolveWithin(FileSystem.StatementPath, header?.PacketName, out string path))
            {
                Console.WriteLine(P2pSafeFileName.RefusalMessage(header?.PacketName, "ProcessStatement"));
                await EmitStatementAck(header, AckStatus.Failure_BadHeader);
                return false;
            }

            try
            {
                File.WriteAllText(path, data);
            }
            catch (Exception ex)
            {
                // Disk full or permission denied. Unguarded, this threw straight out of
                // ProcessStatement, past the ack, and unwound into the swallow-all catch at
                // the receive entry, so the Companion heard nothing and destroyed the
                // statement 30 seconds later.
                ConsoleFebrisP2pLogger.Instance.Log(FebrisP2pLogLevel.Error,
                    "ProcessStatement failed to persist '" + header?.PacketName + "': " + ex.Message);
                await EmitStatementAck(header, AckStatus.Failure_DiskFull);
                return false;
            }
            // FIX (MDM-B11): Remove redundant file re-read and weak integrity check. File.WriteAllText throws on failure, so reaching here means the write succeeded. The File.Exists guard is kept so the success output (processed = true) is preserved. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
            // Old weak check (re-read entire file and compared string lengths):
            // if (File.Exists(path) && File.ReadAllText(path).Length == data.Length)
            if (File.Exists(path))
            {
                processed = true;
            }
            //using (FileStream fileStream = new FileStream(path, FileMode.Open))
            //{
            //    //File file = File.WriteAllBytes(path, body);
            //    fileStream.Write(body,0,body.Length);
            //    //if (fileStream.)
            //}

            // FIX (MOB-B1 blocker 3 / MP2P-8): the statement is now durably on disk
            // (File.WriteAllText throws on failure; the File.Exists guard confirms it),
            // so acknowledge it back to the Companion. The Companion's ack-gated send
            // then marks the statement Uploaded on CONFIRMED delivery instead of on mere
            // socket write-success (the old MDM-B1 data-loss window). Backward-compatible:
            // legacy Companions send MessageId == Guid.Empty and get no ack (they never
            // registered one). PacketName is the statement's unique UUID, so a retried
            // duplicate overwrites its own identical file (idempotent) -- no separate dedup
            // needed. SOURCE-ONLY (Xamarin can't build/test here).
            await EmitStatementAck(header, processed ? AckStatus.Success : AckStatus.Failure_Other);
            return processed;

        }

        /// <summary>
        /// Emits an <c>_acknowledge</c> frame back to the Companion that sent a statement,
        /// correlating on the original <see cref="PacketHeaderModel.MessageId"/> via
        /// <c>InResponseTo</c>. No-op for legacy senders (MessageId == Guid.Empty) or when
        /// the sender's device can't be resolved. Never throws into the caller -- the
        /// statement is already persisted, so a failed ack must not fail processing (the
        /// Companion simply times out and retries / best-effort marks).
        /// </summary>
        private static async Task EmitStatementAck(
            PacketHeaderModel statementHeader,
            Febris.SharedMobileLibrary.P2pNetworking.AckStatus status)
        {
            try
            {
                if (statementHeader == null || statementHeader.MessageId == Guid.Empty)
                {
                    // Legacy Companion -- no ack semantics requested.
                    return;
                }

                // Resolve which Companion to reply to (same lookup UpdateIPAddress uses).
                CompanionDeviceViewModel device = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    .FirstOrDefault(i => i.CompanionDevice.UniqueIdentifier == statementHeader.DeviceUniqueIdentifier);
                if (device == null)
                {
                    ConsoleFebrisP2pLogger.Instance.Log(FebrisP2pLogLevel.Warn,
                        "Cannot emit statement ack: no Companion device matches DeviceUniqueIdentifier=" +
                        (statementHeader.DeviceUniqueIdentifier ?? "<null>") +
                        " (MessageId=" + statementHeader.MessageId + ").");
                    return;
                }

                var ackHeader = new PacketHeaderModel
                {
                    BodyType = BodyType._acknowledge,
                    PacketName = statementHeader.PacketName,
                    DeviceUniqueIdentifier = statementHeader.DeviceUniqueIdentifier,
                    // The ack itself is NOT tracked (no ack-of-ack) -- MessageId stays Guid.Empty
                    // so the Companion's _acknowledge handler won't try to ack it back.
                    InResponseTo = statementHeader.MessageId,
                    AckStatus = status
                };

                byte[] ackFrame = new FebrisP2pFrameBuilder().Build(ackHeader);
                await DependencyService.Get<IWiFiP2pServer>().SocketSender(ackFrame, device);
            }
            catch (Exception ex)
            {
                // Statement is already persisted; a failed ack just means the Companion will
                // time out and retry (or best-effort mark). Never propagate to the caller.
                ConsoleFebrisP2pLogger.Instance.Log(FebrisP2pLogLevel.Error,
                    "Failed to emit statement ack for MessageId=" + statementHeader.MessageId + ": " + ex.Message);
            }
        }
        #endregion
        #region generic String
        //can use this for string readouts on screen
        private static async Task<bool> ProcessGenericString(PacketHeaderModel header, byte[] body)
        {
            // MP2P-7: replaced `throw new NotImplementedException()` with a logged no-op. There is
            // no business case wired to consume generic-string payloads on the Server today; the
            // body type exists so a future feature (debug readouts, status messages from Companion)
            // can light up without re-touching the dispatcher. If a frame ever routes here today,
            // log the receipt and return false so the upstream caller doesn't mistake silence for
            // success.
            string text = body == null
                ? "<null body>"
                : System.Text.Encoding.UTF8.GetString(body);
            ConsoleFebrisP2pLogger.Instance.Log(
                FebrisP2pLogLevel.Info,
                "ProcessGenericString received (not consumed): packet=" + (header?.PacketName ?? "<null>") +
                " bytes=" + (body?.Length ?? 0) +
                " text=" + (text.Length > 200 ? text.Substring(0, 200) + "..." : text));
            return false;
        }
        #endregion
        #region Video 
        private static async Task<bool> ProcessVideo(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;

            // See ProcessStatement: attacker-authored name, and Path.Combine honours a rooted
            // second argument by discarding the first.
            if (!P2pSafeFileName.TryResolveWithin(FileSystem.StatementPath, header?.PacketName, out string path))
            {
                Console.WriteLine(P2pSafeFileName.RefusalMessage(header?.PacketName, "ProcessVideo"));
                return false;
            }

            File.WriteAllBytes(path, body);
            if (File.Exists(path))
            {
                processed = true;
            }
            //using (FileStream fileStream = new FileStream(path, FileMode.Open))
            //{
            //    //File file = File.WriteAllBytes(path, body);
            //    fileStream.Write(body,0,body.Length);
            //    //if (fileStream.)
            //}            
            return processed;
        }
        #endregion
        #region Module
        private static async Task<bool> ProcessModule(PacketHeaderModel header, byte[] body)
        {
            // MP2P-7: replaced `throw new NotImplementedException()` with a logged no-op + false
            // return. The Server doesn't INSTALL modules -- that's the Companion's job (see the
            // Companion-side ProcessModule which unzips + invokes the Android PackageInstaller).
            // The Server receiving a _module body type is a routing error, not a normal case. Log
            // it loudly so the operator can investigate, but don't crash; just return false.
            ConsoleFebrisP2pLogger.Instance.Log(
                FebrisP2pLogLevel.Warn,
                "Server-side ProcessModule was invoked -- this is a routing error, modules install on " +
                "the Companion, not the Server. packet=" + (header?.PacketName ?? "<null>") +
                " bodyBytes=" + (body?.Length ?? 0));
            return false;
        }
        #endregion
        #region acknowledge
        private static async Task<bool> ProcessAcknowledge(PacketHeaderModel header, byte[] body)
        {
            // MP2P-7: replaced `throw new NotImplementedException()` with a logged stub.
            // The real implementation is gated by MP2P-8 (end-to-end acknowledgement), which
            // will add MessageId / InResponseTo correlation + AckStatus enum to PacketHeaderModel
            // and route this back to the sender's pending-ack ConcurrentDictionary. Until then
            // any received ack is just logged so we can see something arrived; returning true
            // tells the dispatch loop the frame was handled cleanly.
            ConsoleFebrisP2pLogger.Instance.Log(
                FebrisP2pLogLevel.Info,
                "ProcessAcknowledge received (MP2P-8 will wire the real correlation): packet=" +
                (header?.PacketName ?? "<null>") + " bodyBytes=" + (body?.Length ?? 0));
            return true;
        }
        #endregion
        #region IP address update
        internal static async Task UpdateIPAddress(DownloadEventArgs e, PacketHeaderModel header)
        {
            try
            {
                // Second line of defence. The caller now gates on P2pAuthorizationResult
                // .PeerResolved, but this method rewrites the Server's outbound routing from a
                // peer-authored string, so it refuses to run on a blank identifier on its own
                // account. Without this, empty-matches-empty in the comparison below selects
                // whichever device is mid-onboarding with no identifier stored yet.
                if (string.IsNullOrWhiteSpace(header?.DeviceUniqueIdentifier))
                {
                    return;
                }

                CompanionDeviceViewModel vmDevice = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    .Where(i => i.CompanionDevice != null
                             && !string.IsNullOrWhiteSpace(i.CompanionDevice.UniqueIdentifier)
                             && i.CompanionDevice.UniqueIdentifier == header.DeviceUniqueIdentifier)
                    .SingleOrDefault();
                if (vmDevice != null)
                {
                    vmDevice.WiFiIPAddress = e.CompanionIPAddress;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }
        #endregion
        #region Initalize
        /// <summary>
        /// Fills in a device's WiFi identity the first time it announces itself.
        ///
        /// <para><b>This is the only way a record ever gets a WifiMacAddress</b>, and a great deal
        /// hangs off that field. Both writers of <c>WiFiConnected</c> find their device by matching
        /// it, and <c>WiFiConnected</c> is the only writer of <c>VideoStreamAvailable</c>, which is
        /// what makes the Start Video Stream button visible at all.</para>
        ///
        /// <para><b>Why the guard no longer mentions Bluetooth.</b> It used to require
        /// <c>i.CompanionDevice.BlueToothName == input.BlueToothDeviceName</c>, which no
        /// numerically-paired device can satisfy: pairing records only Name and UniqueIdentifier,
        /// and the Companion reports an empty Bluetooth name because it holds no Bluetooth
        /// permission. A null compared against an empty string is false, so this returned early on
        /// every single announcement and the WiFi identity was never filled in. That left the
        /// device permanently unable to show its video button, which is what blocked Tier 1. See
        /// docs/MOBILE_KNOWN_ISSUES.md issue 10.</para>
        ///
        /// <para>Keying on <c>DeviceUniqueIdentifier</c> instead is not a new convention. It is the
        /// identifier the Server already uses for a Companion everywhere else, the one pairing
        /// settled on for the responder side (docs/MOBILE_AUTH.md 4.4), and the one the status
        /// update already matches on. The Bluetooth display name was never a sound key: it is
        /// user-editable and collides between two phones of the same model (MOBILE_AUTH 1.6).</para>
        ///
        /// <para>The empty-WifiMacAddress condition is deliberately KEPT, so this runs once per
        /// device rather than on every <c>_initalize</c> ping.</para>
        /// </summary>
        internal static async Task ProcessInitalization(PacketHeaderModel input)
        {
            try
            {
                if (input.BodyType != BodyType._initalize) return;

                // MARK THIS SPECIFIC DEVICE CONNECTED, keyed on the identifier it just announced.
                //
                // An `_initalize` from a peer is the only unambiguous, per-device evidence the
                // Server gets that a particular Companion is talking to it. Everything else that
                // set WiFiConnected matched on WifiMacAddress, and the code that fills that MAC in
                // can only resolve ONE unknown device at a time (issue 16), so with two Companions
                // connected the second stayed blank and therefore permanently "disconnected", with
                // no video button, even though its traffic was arriving normally.
                //
                // Keying on DeviceUniqueIdentifier removes that limit entirely. It scales to any
                // number of peers, and it is the identifier the Server already uses for a Companion
                // everywhere else. The MAC-based lookups only ever existed because identity used to
                // rest on device NAMES, which collide between identical handsets.
                CompanionDeviceViewModel announcing = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    .Where(i => i.CompanionDevice.UniqueIdentifier == input.DeviceUniqueIdentifier)
                    .FirstOrDefault();

                if (announcing != null && !announcing.WiFiConnected)
                {
                    Console.WriteLine("initalize: marking '" + input.DeviceUniqueIdentifier + "' connected");
                    announcing.WiFiConnected = true;
                }

                bool needsWiFiIdentity = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    .Where(i =>
                        string.IsNullOrEmpty(i.CompanionDevice.WifiMacAddress)
                        && (i.CompanionDevice.UniqueIdentifier == input.DeviceUniqueIdentifier
                            || string.IsNullOrEmpty(i.CompanionDevice.UniqueIdentifier)))
                    .Any();

                if (!needsWiFiIdentity) return;

                Console.WriteLine("initalize: resolving WiFi identity for '" + input.DeviceUniqueIdentifier + "'");
                await PairDevice.WiFiInformationRequest(input);
            }
            catch (Exception ex)
            {
                // Was a bare catch{}. This method is the sole entry point to the WiFi identity
                // refresh, so swallowing silently meant the one failure that mattered most on this
                // path could not be seen at all.
                Console.WriteLine("ProcessInitalization failed: " + ex.Message);
            }
        }
        #endregion
        #region Start Video Stream
        private static async Task<bool> ProcessStartVideoStream(PacketHeaderModel header, byte[] body)
        {
            // Legacy request to begin streaming. The frames themselves no longer arrive under
            // this body type: the Companion now sends _videoCodecConfig once and then
            // _videoFrame per access unit. Clearing here means a restart cannot decode a
            // backlog left by the previous session.
            VideoStreamProcessing.ResetStream();
            return await Task.FromResult(true);
        }
        #endregion
        #region H.264 stream
        /// <summary>SPS/PPS. Must reach the decoder before any frame, and again after a resync.
        ///
        /// This also carries the stale-state reset that ProcessVideoStreamStart used to do.
        /// That handler is gone with its dispatch case, and losing the reset with it would
        /// have been a real regression: a resync would decode a backlog captured under the
        /// previous configuration. ProcessCodecConfig clears the queue itself, so the
        /// behaviour survives its removal.</summary>
        private static bool ProcessVideoCodecConfig(PacketHeaderModel header, byte[] body)
        {
            VideoStreamProcessing.ProcessCodecConfig(header, body);
            return true;
        }

        /// <summary>
        /// One access unit. Deliberately synchronous and not wrapped in Task.Run: this only
        /// enqueues, and dispatching each frame onto the thread pool would decode them out of
        /// order, which H.264 cannot recover from.
        /// </summary>
        private static bool ProcessVideoFrame(PacketHeaderModel header, byte[] body)
        {
            VideoStreamProcessing.ProcessVideoFrame(header, body);
            return true;
        }
        #endregion
        #region End Video Stream
        private static async Task<bool> ProcessEndVideoStream(PacketHeaderModel header, byte[] body)
        {
            // MP2P-7 left this as a logged stub because nothing consumed the signal. There is
            // a consumer now: the decoder holds a MediaCodec session that should not outlive
            // the stream that configured it.
            ConsoleFebrisP2pLogger.Instance.Log(
                FebrisP2pLogLevel.Info,
                "ProcessEndVideoStream received: packet=" + (header?.PacketName ?? "<null>"));
            VideoStreamProcessing.StopStream();

            // Clear the operator-visible flag. Nothing anywhere set it false, so the status
            // line read "Video Stream Is Available" over a frozen last frame for the rest of
            // the session.
            //
            // This is also what finally reports a REFUSED capture start. A learner declining
            // the MediaProjection dialog returns early on the Companion, but that return is
            // inside the try whose finally announces the stream ended, so a decline now
            // arrives here as an ordinary stop rather than as silence. The operator sees the
            // stream go inactive instead of watching a blank page that claims to be running.
            try
            {
                CompanionDeviceViewModel device = LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.ItemList
                    ?.FirstOrDefault(i => i?.CompanionDevice != null
                                       && !string.IsNullOrWhiteSpace(i.CompanionDevice.UniqueIdentifier)
                                       && i.CompanionDevice.UniqueIdentifier == header?.DeviceUniqueIdentifier);
                if (device != null)
                {
                    device.VideoStreamConnected = false;
                }
            }
            catch (Exception ex)
            {
                ConsoleFebrisP2pLogger.Instance.Log(
                    FebrisP2pLogLevel.Warn, "could not clear VideoStreamConnected: " + ex.Message);
            }

            return await Task.FromResult(true);
        }
        #endregion

        #endregion


    }
}
