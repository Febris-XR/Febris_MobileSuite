// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.DataLogic;
using Febris.MobileCompanionV3.P2pCommunication.WiFi;
using Febris.MobileCompanionV3.Resources;
using Febris.MobileCompanionV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using Febris.SharedMobileLibrary.Utilites;
using Febris.ModelLibrary.Models.XApiModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Febris.MobileCompanionV3.BusinessLogic
{
    public class LoopLogic
    {
        private IDevice _uniqueId = DependencyService.Get<IDevice>();
        private IWiFiService wifiService = DependencyService.Get<IWiFiService>();
        private IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
        private ICompanionDataCollection _companionData = DependencyService.Get<ICompanionDataCollection>();
        ISharedFileSystem _externalPlatformFileSystem = DependencyService.Get<ISharedFileSystem>();
        //StatementFileContext _statementContext = new StatementFileContext();
        VideoFileContext _videoContext = new VideoFileContext();
        ModulePackageLogic _moduleFileContext = new ModulePackageLogic();
        // MP2P-2: `WiFiP2pRequestCreation _requestCreation` field removed -- all callers now use
        // the static `new FebrisP2pFrameBuilder().Build(...)` API, and the legacy class is on the
        // deletion path for MP2P-9. The two commented-out `WiFiP2pRequestCreation.RequestBuilder`
        // references later in this file (~lines 365, 434) sit inside `#region Pre-Android 11` /
        // `#region Not updated after Android 11` dead blocks and will be removed by MP2P-9.
        FileManager _fileManager = new FileManager();
        StatementLogic _statementContext = new StatementLogic();

        public LoopLogic()
        {
            // _uniqueId = DependencyService.Get<IDevice>();
            //wifiService = DependencyService.Get<IWiFiService>();
        }
        //IWiFiService wifiService = DependencyService.Resolve<IWiFiService>();
        //WiFiService wifiService = LocalHardwareStaticDetails.;


        #region Ping loop
        /// <summary>0 until the upload loops have been started, then 1 for the life of the process.</summary>
        private static int _loopsStarted;

        /// <summary>
        /// Announces this device and, the FIRST time only, starts the two upload loops.
        ///
        /// <para><b>Why starting is now once-per-process.</b> The loops no longer exit when the
        /// link drops, they idle, which is what makes a reconnect recover instead of stranding
        /// every unsent statement (issue 14). The consequence is that this method must stop
        /// spawning a fresh pair each time it runs, or every reconnect would leave another pair
        /// behind, all uploading the same files concurrently.</para>
        ///
        /// <para><c>InitalizationRepeater</c> still runs on every call, because re-announcing this
        /// device to a Server that has just come back is exactly what should happen.</para>
        /// </summary>
        public async Task PingStarter()
        {
            //LoopLogic looper = new LoopLogic();
            StatusUpdateHelper.ServerConnectionInitalizing();
            bool complete = await InitalizationRepeater();
            if (!complete)
            {
                return;
            }

            if (System.Threading.Interlocked.CompareExchange(ref _loopsStarted, 1, 0) != 0)
            {
                Console.WriteLine("upload loops already running, not starting a second pair");
                return;
            }

            Console.WriteLine("*************Upload loops starting*************");
            Task.Run(() => FileUploadLoop());
            //Task.Run(() => wifiService.PingRepeater());
            //Task.Run(() => wifiService.SenderLoop());
            Task.Run(() => StatusUpdateLoop());
        }

        public async Task<bool> InitalizationRepeater()
        {
            bool complete = false;
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    await Ping_InitalizeRequest();
                    await Task.Delay(2000);
                }
                complete = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //_InitalizationComplete = complete;
            return complete;
        }

        private async Task Ping_InitalizeRequest()
        {
            byte[] tempData = { };
            bool fileSent = false;
            var BTName = await _companionData.GetStatusInformation();
            //UniqueIdentifier _uniqueId = new UniqueIdentifier();
            PacketHeaderModel tempHeader = new PacketHeaderModel()
            {
                //DeviceUniqueIdentifier = GetWiFiDeviceName(),
                //WiFiMacAddress = GetWiFiMacAddress(),

                DeviceUniqueIdentifier = _uniqueId.GetIdentifier(),
                //BlueToothDeviceMacAddress = LocalHardwareStaticDetails._thisDevice.BlueToothMacAddress,
                BlueToothDeviceName = BTName.BlueToothDeviceName,
                BlueToothDeviceAlias = "",
                BlueToothDeviceType = "Classic",
                BodyType = BodyType._initalize,
                PacketName = "initalizer"
            };

            try
            {
                tempData = new byte[1];
                // MP2P-2: shared FebrisP2pFrameBuilder. Parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(tempHeader, tempData);
                //fileSent = await WiFiP2pServer.wiFiP2pServer.SocketSender(dataPackage, tempHeader);//.Result;
                fileSent = await _wiFiP2PServer.SocketSender(dataPackage);//.Result;
                Console.WriteLine("File Sent: " + tempData + " || Header: " + tempHeader);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ping_InitalizeRequest Error:" +ex.StackTrace);
                Console.WriteLine("Ping_InitalizeRequest Error:" + ex.Message);
            }
            finally
            {
                tempHeader = null;
            }
        }
        #endregion



        #region Data sending loop
        /// <summary>
        /// Loop for sending status updates
        /// </summary>
        //internal async void SenderLoop()
        //{
        //    await Task.Delay(5000);
        //    while (true)
        //    {
        //        try
        //        {
        //            if (WiFiStaticDetails.networkInfo != null
        //                && WiFiStaticDetails.networkInfo.IsConnected
        //                && WiFiStaticDetails.HostInfo.GroupFormed)
        //            {
        //                bool upToDate = StatusUpdate();
        //            }
        //            else
        //            {
        //                RestartConnectionDiscovery();
        //                break;
        //            }
        //        }
        //        catch { }
        //        finally
        //        {
        //            await Task.Delay(5000);
        //        }
        //    }
        //}

        private async Task StatusUpdateLoop()
        {
            //await Task.Delay(LocalHardwareStaticDetails.StatusUpdateFrequency);
            await Task.Delay(500);
            while (true)
            {
                try
                {
                    // IDLE, DO NOT EXIT. This used to `break` out of the while(true), which is
                    // permanent, for a condition that is temporary by nature: being back in
                    // discovery means the link dropped, not that this device is finished. Nothing
                    // restarted the loop afterwards, so a single Server restart silently disabled
                    // status updates for the life of the process. See issue 14.
                    if (LocalHardwareStaticDetails.RunPeerDiscovery && LocalHardwareStaticDetails.RunServiceListener)
                    {
                        continue;   // the finally below still paces the loop
                    }
                    await StatusUpdate();
                }
                catch (Exception ex)
                {
                    LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                }
                finally
                {
                    await Task.Delay(LocalHardwareStaticDetails.StatusUpdateFrequency);
                }
            }
        }

        private async Task StatusUpdate()
        {
            StatusUpdateHelper.UploadingData();
            try
            {


                //DeviceStatusModel statusData=await _companionData.GetStatusInformation();
                // MP2P-2: removed `byte[] dataBody = { };` -- legacy `String2Bytes` -> `RequestBuilder`
                // pipeline is collapsed into a single `Build(header, string)` below.

                string uniqueId = _uniqueId.GetIdentifier();// Utilites.UniqueIdentifier.GetIdentifier();

                //list of statements
                List<string> statementList = await _statementContext.GetUnsentNameList() ?? new List<string>();
                List<string> oldStatementList = await _statementContext.GetSentNameList() ?? new List<string>();
                //list of videos
                List<string> videoList = await _videoContext.GetUnsentNameList() ?? new List<string>();
                List<string> oldVideoList = await _videoContext.GetSentNameList() ?? new List<string>();
                //module list
                List<string> moduleList = await _moduleFileContext.GetUncompressedModuleNameIndex() ?? new List<string>(); //.GetNameIndex() ?? new List<string>();//.GetNameList() ?? new List<string>();GetUncompressedPackageNameIndex();
                //List<string> moduleList = await _moduleFileContext.GetUncompressedPackageNameIndex() ?? new List<string>();
                List<string> zippedModuleList = await _moduleFileContext.GetCompressedModuleNameIndex() ?? new List<string>();//.GetZippedNameList() ?? new List<string>();                
                List<string> appList = await _moduleFileContext.GetInstalledPackageNameIndex() ?? new List<string>();//.GetAppList()??new List<string>();

                HardwareStatusUpdate hardwareStatus = new HardwareStatusUpdate()
                {
                    StatementFileList = statementList,
                    VideoFileList = videoList,
                    OldStatementFileList = oldStatementList,
                    OldVideoFileList = oldVideoList,
                    ClientUniqueId = uniqueId,
                    BatteryCharge = Battery.ChargeLevel * 100,
                    //StorageSpaceRemaining = ,
                    ModuleFileList = moduleList,
                    ZippedFileList = zippedModuleList,
                    AppList = appList
                };

                //create data package body
                string databodyString = JsonConvert.SerializeObject(hardwareStatus);

                //Build Header
                PacketHeaderModel tempHeader = new PacketHeaderModel()
                {
                    DeviceUniqueIdentifier = uniqueId,
                    BodyType = BodyType._statusUpdate,
                    PacketName = "Hardware Status Update"
                };

                //Build Data package (MP2P-2: `String2Bytes` + `RequestBuilder` collapsed into a single
                // `Build(header, string)` call. Legacy used `Encoding.ASCII`; new path uses UTF-8.)
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(tempHeader, databodyString);

                //Send Data package
                bool complete = await _wiFiP2PServer.SocketSender(dataPackage);

                //if (LocalHardwareStaticDetails.networkInfo != null
                //    && WiFiStaticDetails.networkInfo.IsConnected
                //    && WiFiStaticDetails.HostInfo.GroupFormed)
                //{
                //    bool upToDate = StatusUpdate();
                //}
                //else
                //{
                //    RestartConnectionDiscovery();
                //    break;
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            //StatusUpdateHelper.Connected();
        }
        #endregion

        #region File Update Loop
        public async Task FileUploadLoop()
        {
            await Task.Delay(500);
            //may need to change this one
            while (true)
            {
                try
                {
                    // IDLE, DO NOT EXIT. Same correction as StatusUpdateLoop, and this one is the
                    // more damaging of the two: exiting here stranded every unsent statement,
                    // because this is the only thing that ever retries them. A statement correctly
                    // preserved by FailAll was then never sent again. See issue 14.
                    if (LocalHardwareStaticDetails.RunPeerDiscovery && LocalHardwareStaticDetails.RunServiceListener)
                    {
                        continue;   // the finally below still paces the loop
                    }
                    // DO NOT UPLOAD A STATEMENT WHILE ITS SIMULATION IS STILL RUNNING.
                    //
                    // A run emits statements through the STATEMENT_CREATE broadcast and can emit
                    // more than one. Sending mid-run ships a partial record, and because a
                    // successful send marks it Uploaded, the finished version would never be sent
                    // at all. So the upload waits for the run to be over or closed.
                    //
                    // Nothing is lost while held: statements are already durable in sqlite and this
                    // loop simply skips a pass. The hold also self-expires, so a simulation that
                    // never signals completion delays uploads instead of stranding them. See
                    // LocalHardwareStaticDetails.SimulationUploadHoldActive.
                    if (LocalHardwareStaticDetails.SimulationUploadHoldActive())
                    {
                        continue;   // the finally below still paces the loop
                    }

                    if (!LocalHardwareStaticDetails.ActionProccessingBlocker)
                    {
                        await FileUpload();
                    }
                }
                catch (Exception ex)
                {
                    LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                    //throw;

                }
                finally
                {
                    await Task.Delay(LocalHardwareStaticDetails.FileUploadFrequency);

                }
            }
        }

        private async Task FileUpload()
        {
            StatusUpdateHelper.UploadingData();
            #region tends to send files into the void and lost
            string tempFilePath = string.Empty;
            bool complete = false;
            //bool fileSent = false;
            string uniqueId = _uniqueId.GetIdentifier();
            string BTThisDeviceName = string.Empty;
            DeviceStatusModel gatheredDeviceInfo = await _companionData.GetStatusInformation();

            //list of statements
            List<string> statementList = await _statementContext.GetUnsentNameList();
            //List<string> statementList = _fileManager.GetDirectoryContentNames(FileSystem.StatementPath);
            //list of videos
            List<string> videoList = await _videoContext.GetUnsentNameList();
            //List<string> videoList = _fileManager.GetDirectoryContentNames(FileSystem.VideoPath);

            foreach (var i in statementList)
            {
                bool fileSent = false;
                PacketHeaderModel tempHeader = null;
                byte[] tempData = { };

                #region Post-Android 11
                tempFilePath = string.Empty;
                tempFilePath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.StatementPath, i);
                #endregion
                #region Pre-Android11
                //PacketHeaderModel tempHeader = null;
                //byte[] tempData = { };
                //tempFilePath = string.Empty;
                //tempFilePath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.StatementPath, i);
                #endregion
                //UniqueIdentifier _uniqueId = new UniqueIdentifier();
                tempHeader = new PacketHeaderModel()
                {
                    //DeviceUniqueIdentifier = GetWiFiDeviceName(),
                    //WiFiMacAddress = GetWiFiMacAddress(),
                    //
                    DeviceUniqueIdentifier = uniqueId,//.GetIdentifier(),
                    BlueToothDeviceName = gatheredDeviceInfo.BlueToothDeviceName,
                    BlueToothDeviceAlias = "",
                    BlueToothDeviceType = "Classic",
                    BodyType = BodyType._statement,
                    PacketName = i,
                    // FIX (MOB-B1 blocker 3 / MP2P-8): stamp a per-send MessageId so the
                    // Server can echo an _acknowledge and we only mark the statement
                    // Uploaded on CONFIRMED delivery (see the ack-gated send below). A
                    // fresh Guid per poll iteration is required -- FebrisP2pAckTracker
                    // .RegisterPending throws on Guid.Empty or a duplicate id.
                    MessageId = Guid.NewGuid()
                };


                try
                {
                    #region Post-Android 11
                    ///Statements are now stored in the DB instead of the file system
                    RawStatement rawStatement = await _statementContext.GetByUUID(i);
                    //RawStatement rawStatement = await _statementContext.GetByRef(i);
                    // FIX (MDM-B3): use the UTF-8 string overload like the StatusUpdate path (line 238), not the legacy ASCII String2Bytes, so non-ASCII statement JSON is not silently lost on the v2 wire. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
                    //tempData = await DataConversionUtility.String2Bytes(rawStatement.JsonStatementData);
                    // MP2P-2: shared FebrisP2pFrameBuilder. Parameter order flipped from legacy (header, body).
                    byte[] dataPackage = new FebrisP2pFrameBuilder().Build(tempHeader, rawStatement.JsonStatementData);

                    // FIX (MOB-B1 blocker 3 / MP2P-8): ack-gated send. Register the pending
                    // ack BEFORE writing the frame (so a fast ack can't race ahead of
                    // registration), then flip the statement to Uploaded only when the
                    // Server confirms it DURABLY persisted (AckStatus.Success). This
                    // replaces the old "Uploaded on socket write-success" behavior (the
                    // MDM-B1 note below) that lost statements written to the socket buffer
                    // but never delivered/persisted.
                    //
                    // SOURCE-ONLY (Xamarin can't build/test on the current host). On device
                    // verify: (a) a normal send gets Success + marks Uploaded exactly once;
                    // (b) killing the Server mid-send leaves the statement UNSENT so the next
                    // poll retries it; (c) against a pre-MP2P-8 Server that never acks, the
                    // Failure_Timeout branch degrades to today's best-effort mark (no stall
                    // pile-up, no worse than current). See docs/BUGS.md MOB-B1.
                    //fileSent = WiFiP2pServer.wiFiP2pServer.SocketSender(dataPackage, tempHeader).Result;
                    Task<AckStatus> ackTask = FebrisP2pAckTracker.Instance.RegisterPending(
                        tempHeader.MessageId,
                        FebrisP2pAckTracker.DefaultTimeoutFor(BodyType._statement));

                    fileSent = await _wiFiP2PServer.SocketSender(dataPackage);//.Result;
                    // NOTE (MDM-B1): superseded by the ack-gating above/below -- Uploaded is
                    // no longer set on mere socket write-success. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
                    if (!fileSent)
                    {
                        // The socket write itself failed -- the frame never left. Drain the
                        // pending entry now (don't leave it to time out) and stop this pass;
                        // the link is likely down and the next poll retries from the unsent set.
                        FebrisP2pAckTracker.Instance.TryResolve(tempHeader.MessageId, AckStatus.Failure_Other);
                        break;
                    }

                    // Frame is on the wire -- wait for the Server's _acknowledge (or timeout).
                    AckStatus ackStatus = await ackTask;
                    if (ackStatus == AckStatus.Success)
                    {
                        rawStatement = await _statementContext.UploadComplete(rawStatement);
                        // BOTH IDENTIFIERS, because they are not interchangeable and logging only
                        // the external one made this line unmatchable against the file it describes.
                        // UUID is the name the file carries on both tiers; ExternalReferance is the
                        // caller's ReferenceUUID and correlates back to the training module.
                        Console.WriteLine("Statement: " + rawStatement.UUID + " (ref " +
                            rawStatement.ExternalReferance + ") has been uploaded");
                        StatusUpdateHelper.General("Statement: " + rawStatement.UUID + " has been uploaded");
                    }
                    else if (ackStatus == AckStatus.Failure_Timeout)
                    {
                        // No ack heard within the timeout -- "delivery uncertain." This is the
                        // exact pre-MP2P-8 risk window, so degrade to the OLD behavior (mark
                        // Uploaded best-effort) rather than stall/pile up forever against a
                        // Server that doesn't speak MP2P-8. When both sides speak MP2P-8 on a
                        // healthy link this branch is rare. A deployment that GUARANTEES a new
                        // Server can make delivery strict by removing this best-effort mark so
                        // a timeout leaves the statement unsent for retry instead.
                        rawStatement = await _statementContext.UploadComplete(rawStatement);
                        Console.WriteLine("Statement: " + rawStatement.UUID + " (ref " +
                            rawStatement.ExternalReferance +
                            ") ack timed out (delivery uncertain), marked Uploaded best-effort.");
                        StatusUpdateHelper.General("Statement: " + rawStatement.UUID +
                            " delivery unconfirmed (ack timeout).");
                    }
                    else
                    {
                        // Server explicitly reported a processing failure (e.g. Failure_DiskFull,
                        // Failure_HandlerThrew). Do NOT mark Uploaded -- leave it in the unsent set
                        // so the next poll retries it. Strictly better than the old code, which
                        // marked Uploaded on write-success even when the Server failed to persist.
                        Console.WriteLine("Statement: " + i + " NOT uploaded -- Server ack status " +
                            ackStatus + "; will retry on next poll.");
                        StatusUpdateHelper.General("Statement " + i + " retry pending (ack " + ackStatus + ").");
                    }
                    Console.WriteLine("File Sent: " + tempData + "||Header: " + tempHeader);
                    #endregion
                    #region Pre-Android 11
                    //tempData = await _externalPlatformFileSystem.OutgoingFileData(tempFilePath);
                    ////tempData = FileManager.OutgoingFileData(tempFilePath);
                    //byte[] dataPackage = WiFiP2pRequestCreation.RequestBuilder(tempData, tempHeader);
                    ////fileSent = WiFiP2pServer.wiFiP2pServer.SocketSender(dataPackage, tempHeader).Result;
                    //fileSent = await _wiFiP2PServer.SocketSender(dataPackage);//.Result;
                    //if (!fileSent) { break; }
                    //Console.WriteLine("File Sent: " + tempData + "||Header: " + tempHeader);
                    #endregion

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                    //return complete;
                    StatusUpdateHelper.Error(ex.Message);
                }
                finally
                {
                    await Task.Delay(500);
                    tempHeader = null;
                }

                #region pre Android 11
                //if (fileSent)
                //{
                //    //extract name
                //    string tempFileName = Path.GetFileName(tempFilePath);
                //    //get current path
                //    string tempCurrentPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.StatementPath, tempFilePath);
                //    //get new path
                //    string tempNewPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.OldStatementPath, Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.OldStatementPath, tempFileName));
                //    //move file
                //    bool moved = await _externalPlatformFileSystem.FileMover(tempCurrentPath, tempNewPath);
                //    //bool moved = _fileManager.MoveFolder(tempCurrentPath, tempNewPath);
                //    StatusUpdateHelper.ProcessingComplete();
                //}
                //else
                //{
                //    StatusUpdateHelper.FileNotSent();
                //    //else throw and error and keep in the current folder
                //}
                //fileSent = false;
                #endregion
            }

            #region Not updated after Android 11. Needs to be changed over to the public media files. 
            //foreach (var i in videoList)
            //{
            //    bool fileSent = false;
            //    PacketHeaderModel tempHeader = null;
            //    byte[] tempData = { };
            //    tempFilePath = string.Empty;
            //    tempFilePath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.RecordingsFilePath, i);
            //    //UniqueIdentifier _uniqueId = new UniqueIdentifier();
            //    tempHeader = new PacketHeaderModel()
            //    {
            //        //DeviceUniqueIdentifier = GetWiFiDeviceName(),
            //        //WiFiMacAddress = GetWiFiMacAddress(),
            //        DeviceUniqueIdentifier = uniqueId,//.GetIdentifier(),
            //        BlueToothDeviceName = gatheredDeviceInfo.BlueToothDeviceName,
            //        BlueToothDeviceAlias = "",
            //        BlueToothDeviceType = "Classic",
            //        BodyType = BodyType._video,
            //        PacketName = i
            //    };

            //    try
            //    {
            //        tempData = await _externalPlatformFileSystem.OutgoingFileData(tempFilePath);
            //        //tempData = FileManager.OutgoingFileData(tempFilePath);
            //        byte[] dataPackage = WiFiP2pRequestCreation.RequestBuilder(tempData, tempHeader);
            //        //fileSent = WiFiP2pServer.wiFiP2pServer.SocketSender(dataPackage, tempHeader).Result;
            //        fileSent = await _wiFiP2PServer.SocketSender(dataPackage);//.Result;
            //        if (!fileSent) { break; }
            //        Console.WriteLine("File Sent: " + tempData + " || Header: " + tempHeader);
            //        //LocalHardwareStaticDetails.StaticMainVM.
            //        //tempData = FileManager.OutgoingFileData(tempFilePath);
            //        //fileSent = WiFiP2pServer.wiFiP2pServer.SocketSender(tempData, tempHeader).Result;
            //        ////fileSent = await WiFiP2pServer.wiFiP2pServer.SocketSender(tempData, tempHeader);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.StackTrace);
            //        Console.WriteLine(ex.Message);
            //        //return complete;
            //        StatusUpdateHelper.Error(ex.Message);
            //    }
            //    finally
            //    {
            //        await Task.Delay(500);
            //        tempHeader = null;
            //    }

            //    if (fileSent)
            //    {
            //        //extract name
            //        string tempFileName = Path.GetFileName(tempFilePath);
            //        //get current path
            //        string tempCurrentPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.RecordingsFilePath, tempFilePath);
            //        //get new path
            //        string tempNewPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.OldStatementPath, Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.zipFolderPath, tempFileName));
            //        //move file
            //        bool moved = await _externalPlatformFileSystem.FileMover(tempCurrentPath, tempNewPath);
            //        //bool moved = _fileManager.ZipVideoFile(tempCurrentPath, tempFileName);
            //        StatusUpdateHelper.ProcessingComplete();
            //    }
            //    else
            //    {
            //        //else throw and error and keep in the current folder
            //        StatusUpdateHelper.FileNotSent();
            //    }
            //    fileSent = false;
            //}
            #endregion

            #endregion

            //return complete;

        }

        #endregion


        public static void EndConnectionDiscovery()
        {            
            LocalHardwareStaticDetails.RunPeerDiscovery = false;
            LocalHardwareStaticDetails.RunServiceListener = false;
        }
        public static void RestartConnectionDiscovery()
        {
            LocalHardwareStaticDetails.RunPeerDiscovery = true;
            LocalHardwareStaticDetails.RunServiceListener = true;
        }



    }
}
