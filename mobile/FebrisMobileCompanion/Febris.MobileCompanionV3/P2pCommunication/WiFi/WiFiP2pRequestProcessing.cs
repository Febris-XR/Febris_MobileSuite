// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.BusinessLogic;
using Febris.MobileCompanionV3.Resources;
using Febris.MobileCompanionV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.SharedMobileLibrary.P2pNetworking;
using Crypto = Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Febris.MobileCompanionV3.P2pCommunication.WiFi
{
    public class WiFiP2pRequestProcessing
    {
        #region variables
        IWiFiService wifi = DependencyService.Get<IWiFiService>();
        ModulePackageLogic _moduleUtility = new ModulePackageLogic();
        ISharedFileSystem _externalPlatformFileSystem = DependencyService.Get<ISharedFileSystem>();
        //private static object sessionCreationLock = new object();
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
        public static byte[] String2Bytes(string input)
        {
            return Encoding.ASCII.GetBytes(input);
        }
        #endregion

        #region decoding
        public static int Bytes2Int(byte[] input)
        {
            return BitConverter.ToInt32(input, 0);
        }
        public static string Bytes2String(byte[] input)
        {
            return Encoding.Default.GetString(input);
        }

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
                // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers from exception handler.
                // Forcing a blocking full GC inside a parse-failure catch buys nothing and stalls
                // every managed thread.
            }
            return output;
        }
        public static async Task<PacketHeaderModel> ParsingByteArrayToHeaderModel(byte[] input)
        {
            PacketHeaderModel output = new PacketHeaderModel();
            string jsonString = string.Empty;
            try
            {
                jsonString = Bytes2String(input);
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
                headerByteLength = Bytes2Int(headerSize);

                //seperate header               
                headerArray = input
                    .Skip(LocalHardwareStaticDetails.ExpectedHeaderLength)
                    .Take(headerByteLength)
                    .ToArray();
                header = Bytes2String(headerArray);

                //seperate body
                body = input.Skip(headerByteLength + LocalHardwareStaticDetails.ExpectedHeaderLength).ToArray();
                //Console.WriteLine(header);// + "/n" + Bytes2String(body));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return (header, body);
        }
        #endregion

        #region Process downloaded data       
        internal static async Task<bool> ProcessDownloadedData(PacketHeaderModel header, byte[] body)
        {
            try
            {


                //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Processing Downloaded Data";
                StatusUpdateHelper.ProcessingData();
                bool processed = false;
                switch (header.BodyType)
                {
                    case BodyType._junk:
                        // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers; setting `body = null`
                        // is enough of a hint to the runtime, and forcing a full GC on every junk packet
                        // is the per-packet anti-pattern this pass is removing.
                        body = null;
                        return false;
                    case BodyType._initalize:
                        //processed = await ProcessAcknowledge(header, body);
                        break;
                    case BodyType._pairingRequest:
                        {
                            processed = ProcessPairingRequest(body);
                            break;
                        }
                    case BodyType._videoResyncRequest:
                        {
                            processed = ProcessVideoResyncRequest();
                            break;
                        }
                    case BodyType._acknowledge:
                        processed = await ProcessAcknowledge(header, body);
                        break;
                    case BodyType._fileUpload:
                        processed = await ProcessFileUpload(header, body);
                        break;
                    case BodyType._statement:
                        processed = await ProcessStatement(header, body);
                        break;
                    case BodyType._oldStatements:
                        processed = await ProcessOldStatements(header, body);
                        break;
                    case BodyType._video:
                        processed = await ProcessVideo(header, body);
                        break;
                    case BodyType._oldVideos:
                        processed = await ProcessOldVideos(header, body);
                        break;
                    case BodyType._genericString:
                        processed = await ProcessGenericString(header, body);
                        break;
                    case BodyType._statusUpdate:
                        processed = await ProcessStatusUpdate(header, body);
                        break;
                    case BodyType._videoStream:
                        processed = await StartVideoStream(header, body);
                        break;
                    case BodyType._endVideoStream:
                        processed = await ProcessEndVideoStream(header, body);
                        break;
                    case BodyType._module:
                        processed = await ProcessModule(header, body);
                        break;
                    case BodyType._removeModule:
                        processed = await ProcessModuleRemoval(header, body);
                        break;
                    case BodyType._removeZippedModule:
                        processed = await ProcessModuleZipRemoval(header, body);
                        break;
                    //case BodyType._removeApp:
                    //    processed = await ProcessAppRemoval(header, body);
                    //    break;
                    case BodyType._installModule:
                        processed = await ProcessModuleInstall(header, body);
                        break;
                    case BodyType._uninstallModule:
                        processed = await ProcessModuleUninstall(header, body);
                        break;
                    case BodyType._reinstallModule:
                        processed = await ProcessModuleReinstall(header, body);
                        break;


                    default:
                        // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers (per-packet anti-pattern).
                        body = null;
                        break;

                }
                #region bunch of if statements
                //if (header.BodyType == BodyType._junk)
                //{
                //    body = null;
                //    return false;
                //}
                //else if (header.BodyType == BodyType._initalize)
                //{
                //    //PairDevice.TestHeader(header);
                //}
                //else if (header.BodyType == BodyType._acknowledge)
                //{
                //    processed = await ProcessAcknowledge(header, body);
                //}
                //else if (header.BodyType == BodyType._fileUpload)
                //{
                //    processed = await ProcessFileUpload(header, body);
                //}
                //else if (header.BodyType == BodyType._statement)
                //{
                //    processed = await ProcessStatement(header, body);
                //}
                //else if (header.BodyType == BodyType._video)
                //{
                //    processed = await ProcessVideo(header, body);
                //}
                //else if (header.BodyType == BodyType._module)
                //{
                //    processed = await ProcessModule(header, body);
                //}
                //else if (header.BodyType == BodyType._genericString)
                //{
                //    processed = await ProcessGenericString(header, body);
                //}
                //else if (header.BodyType == BodyType._statusUpdate)
                //{
                //    processed = await ProcessStatusUpdate(header, body);
                //}
                //else if (header.BodyType == BodyType._videoStream)
                //{
                //    //processed = await ProcessStartVideoStream(header, body);
                //    processed = StartVideoStream(header, body);
                //}
                //else if (header.BodyType == BodyType._endVideoStream)
                //{
                //    processed = await ProcessEndVideoStream(header, body);
                //}
                //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Processing Complete";
                #endregion
                StatusUpdateHelper.ProcessingComplete();
                return processed;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Processing P2P request: " + ex.Message);
                // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers from top-level dispatch
                // exception handler (per-packet anti-pattern).
                return false;
            }
        }

        




        #region Process routing
        #region Status 
        private async static Task<bool> ProcessFileUpload(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                string data = Bytes2String(body);
                //use the header to find the device
                //string data = Bytes2String(body);
                ////parse json
                //LocalJSONHandler _jSONHandler = new LocalJSONHandler();
                //_jSONHandler.DeserialiseHardwareStatusUpdate(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return processed;
        }
        #endregion
        #region Hardware Status 
        private async static Task<bool> ProcessStatusUpdate(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                string data = Bytes2String(body);
                //use the header to find the device
                //string data = Bytes2String(body);
                ////parse json
                //LocalJSONHandler _jSONHandler = new LocalJSONHandler();
                //_jSONHandler.DeserialiseHardwareStatusUpdate(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return processed;
        }
        #endregion
        #region statement
        private async static Task<bool> ProcessStatement(PacketHeaderModel header, byte[] body)
        {

            try
            {
                bool processed = false;
                string data = Bytes2String(body);
                //string launchPathString = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModuleLinkPath, header.PacketName);
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.ProgressBarActive = true;
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Prepairing to Launch: " + header.PacketName;

                ModulePackageLogic _logic = new ModulePackageLogic();
                processed = _logic.LaunchModule(header.PacketName, data).Result;

                //IModulePackageUtility moduleUtility = DependencyService.Get<IModulePackageUtility>();
                //moduleUtility.RunModule(launchPathString, data);
                //System.Diagnostics.Process process = new System.Diagnostics.Process();
                //process.StartInfo = new System.Diagnostics.ProcessStartInfo(launchPathString, arguments: data);
                //process.StartInfo.UseShellExecute = true;
                //process.Start();
                //processed = true;
                return processed;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Statement Processing Error: " + ex.Message);
                Console.WriteLine("Statement Processing Header: " + header + "Statement Processing Data: ");// + data);
                return false;
            }
            finally
            {
                // Every one of the three writers of this flag set it TRUE and none ever set it
                // false, so the first unit of work left the spinner running forever. In a finally
                // rather than beside each return, because two of these three catches rethrow.
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.ProgressBarActive = false;
            }


        }
        /// <summary>
        /// Delete old statements
        /// </summary>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        private async static Task<bool> ProcessOldStatements(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.ProgressBarActive = true;
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Deleting old statements";
            try
            {
                StatementLogic _logic = new StatementLogic();
                processed = await _logic.DeleteOldStatements();
                return processed;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            finally
            {
                // Every one of the three writers of this flag set it TRUE and none ever set it
                // false, so the first unit of work left the spinner running forever. In a finally
                // rather than beside each return, because two of these three catches rethrow.
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.ProgressBarActive = false;
            }
        }

        #endregion
        #region generic String
        //can use this for string readouts on screen
        private async static Task<bool> ProcessGenericString(PacketHeaderModel header, byte[] body)
        {
            // MP2P-7: replaced `throw new NotImplementedException()` with a logged no-op. No live
            // consumer of generic-string payloads on the Companion today; logged for visibility +
            // returns false so the dispatch loop doesn't mistake silence for success. Future
            // string-readout features can wire a real handler here without re-touching the
            // dispatcher.
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
        private async static Task<bool> ProcessVideo(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;

            // PacketName is an attacker-authored JSON string, and Path.Combine discards its
            // first argument when the second is rooted, so this used to write wherever the
            // sender asked. Reachable on this tier before any peer check existed at all.
            if (!P2pSafeFileName.TryResolveWithin(
                    SharedMobileLibrary.FileSystem.FileSystem.StatementPath, header?.PacketName, out string path))
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
        /// <summary>
        /// Delete old videos
        /// </summary>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        private async static Task<bool> ProcessOldVideos(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.ProgressBarActive = true;
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Deleting old videos";
            try
            {
                VideoLogic _logic = new VideoLogic();
                processed = await _logic.DeleteOldVideos();
                return processed;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            finally
            {
                // Every one of the three writers of this flag set it TRUE and none ever set it
                // false, so the first unit of work left the spinner running forever. In a finally
                // rather than beside each return, because two of these three catches rethrow.
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.ProgressBarActive = false;
            }
        }

        #endregion
        #region Module
        private static async Task<bool> ProcessModule(PacketHeaderModel header, byte[] body)
        {
            // One gate for the whole method. PacketName feeds BOTH the zip destination
            // (below, with ".zip" appended) and the unzip target directory, so validating it
            // once here covers both rather than leaving whichever one a later edit forgets.
            // A rooted or traversing name would otherwise place an attacker-supplied archive
            // anywhere on the device and then unpack it somewhere else again.
            if (!P2pSafeFileName.IsPlainFileName(header?.PacketName))
            {
                Console.WriteLine(P2pSafeFileName.RefusalMessage(header?.PacketName, "ProcessModule"));
                return false;
            }

            FileManager _fileManager = new FileManager();
            ModulePackageLogic _logicContext = new ModulePackageLogic();
            //ISharedFileSystem _externalPlatformFileSystem = DependencyService.Get<ISharedFileSystem>();
            try
            {
                bool processed = false;


                #region post-Android 11
                string compressedFilePath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ZippedModulePath, header.PacketName + ".zip");
                File.WriteAllBytes(compressedFilePath, body);
                if (!File.Exists(compressedFilePath))
                {
                    // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers (per-module anti-pattern).
                    Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = false;
                    return processed;
                }
                ModulePackageModel data = await _logicContext.CreateCompressedModulePackage(header, compressedFilePath);


                ///Write to zip file
                //string zippedPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ZippedModulePath, header.PacketName + ".zip");
                //_externalPlatformFileSystem.WriteAllBytes(zippedPath, body);
                //if (!_externalPlatformFileSystem.FileExists(zippedPath).Result)
                //{
                //    return processed;
                //}
                //Module module = new Module
                //{

                //};
                //ModuleProvider moduleProvider = new ModuleProvider()
                //{
                //    TimeStamp = DateTime.UtcNow,
                //    LastUpdateTimeStamp = DateTime.UtcNow,
                //    UUID = new Guid(),
                //    LocationPath = zippedPath,
                //    Compressed = true,
                //    Module = module
                //};
                //var generatedId = App.DbContext.Create<ModuleProvider>(moduleProvider);
                #endregion
                #region pre-Android 11
                /////Write to zip file
                //string zippedPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ZippedModulePath, header.PacketName + ".zip");
                //File.WriteAllBytes(zippedPath, body);
                //if (!File.Exists(zippedPath))
                //{
                //    return processed;
                //}
                #endregion


                #region post-Android 11
                /// unpack zip file
                /// string newFileNameandPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModulePath, header.PacketName);

                ////processed = _fileManager.FileUnzipper(newFileNameandPath, header.PacketName, Path.GetFileNameWithoutExtension(header.PacketName)).Result;

                string newFileNameandPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModulePath, header.PacketName);
                processed = await _fileManager.FileUnzipper(compressedFilePath, newFileNameandPath, Path.GetFileNameWithoutExtension(header.PacketName));
                if (!processed)
                {
                    if (File.Exists(compressedFilePath))
                    {
                        await FileManager.DeleteFile(compressedFilePath);
                        //File.Delete(compressedFilePath);
                    }
                    await _logicContext.DeleteDBEntry(data);
                    // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers (per-module anti-pattern).
                    Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = false;
                    return processed;
                }

                //data = await _logicContext.DecompressedModuleDBUpdate(newFileNameandPath, data);
                data = await _logicContext.CreateDecompressedModuleFile(newFileNameandPath, data);

                //processed = _externalPlatformFileSystem.FileUnzipper(zippedPath, newFileNameandPath, Path.GetFileNameWithoutExtension(header.PacketName)).Result;




                ///delete zipped file
                //bool removeZippedFile = _fileManager.DeleteFolders(zippedPath);
                //bool removeZippedFile = FileManager.DeleteFile(zippedPath);

                //bool removeZippedFile = _externalPlatformFileSystem.DeleteFile(zippedPath);
                ///if it could not be unzipped before just end the cycle

                //moduleProvider.LastUpdateTimeStamp = DateTime.UtcNow;
                //moduleProvider.Compressed = false;
                //moduleProvider.LocationPath = newFileNameandPath;
                //generatedId = App.DbContext.Update<ModuleProvider>(moduleProvider);
                //bool removeZippedFile = _externalPlatformFileSystem.DeleteFile(zippedPath);


                #endregion
                #region pre-Android 11
                ///// unpack zip file
                //string newFileNameandPath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModulePath, header.PacketName);
                //FileManager _fileManager = new FileManager();
                ////processed = _fileManager.FileUnzipper(newFileNameandPath, header.PacketName, Path.GetFileNameWithoutExtension(header.PacketName)).Result;
                //processed = _fileManager.FileUnzipper(zippedPath, newFileNameandPath, Path.GetFileNameWithoutExtension(header.PacketName)).Result;


                /////delete zipped file
                ////bool removeZippedFile = _fileManager.DeleteFolders(zippedPath);
                //bool removeZippedFile = FileManager.DeleteFile(zippedPath);
                /////if it could not be unzipped before just end the cycle
                //if (!processed)
                //{
                //    return processed;
                //}

                #endregion

                #region post-Android 11
                ///install apk module
                //ModulePackageLogic _moduleUtility = new ModulePackageLogic();
                processed = await _logicContext.InstallModule(header.PacketName, newFileNameandPath, data);
                if (!processed)
                {
                    await _logicContext.DeleteModule(data.ModuleDirectoryName);
                    // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers (per-module anti-pattern).
                    Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = false;
                    return processed;
                }
                //moduleProvider.LastUpdateTimeStamp = DateTime.UtcNow;
                //moduleProvider.Installed = true;                
                //generatedId = App.DbContext.Update<ModuleProvider>(moduleProvider);


                #endregion
                #region pre-Android 11
                /////install apk module
                //ModuleUtility _moduleUtility = new ModulePackageLogic();
                //processed = _moduleUtility.InstallModule(header.PacketName, newFileNameandPath).Result;
                #endregion

                return processed;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers (per-module anti-pattern).
                Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = false;
                return false;
            }
        }
        #endregion
        #region Reinstall Module
        private async static Task<bool> ProcessModuleReinstall(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                ModulePackageLogic _moduleUtility = new ModulePackageLogic();
                processed = _moduleUtility.ReinstallApplication(header.PacketName).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
            return processed;
        }
        #endregion 
        #region install Module
        private async static Task<bool> ProcessModuleInstall(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                //lock (sessionCreationLock)
                //{
                ModulePackageLogic _moduleUtility = new ModulePackageLogic();
                processed = await _moduleUtility.InstallModule(header.PacketName);
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
            return processed;
        }
        #endregion 
        #region Remove App
        private async static Task<bool> ProcessModuleUninstall(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                ModulePackageLogic _moduleUtility = new ModulePackageLogic();
                processed = await _moduleUtility.UninstallApplication(header.PacketName);
                if (processed)
                {
                    //ModuleProvider moduleData = 

                    //App.DbContext.Update()
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
            return processed;
        }
        #endregion 
        #region Delete Module
        private async static Task<bool> ProcessModuleRemoval(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                ModulePackageLogic _moduleUtility = new ModulePackageLogic();
                processed = await _moduleUtility.DeleteModule(header.PacketName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
            return processed;
        }
        #endregion
        #region Delete Module Zip
        private async static Task<bool> ProcessModuleZipRemoval(PacketHeaderModel header, byte[] body)
        {
            bool processed = false;
            try
            {
                ModulePackageLogic _moduleUtility = new ModulePackageLogic();
                processed = await _moduleUtility.DeleteCompressedModule(header.PacketName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
            return processed;
        }
        #endregion
        #region Remove App
        //private static bool ProcessAppRemoval(PacketHeaderModel header, byte[] body)
        //{
        //    bool processed = false;
        //    try
        //    {
        //        ModulePackageLogic _moduleUtility = new ModulePackageLogic();
        //        processed = _moduleUtility.UninstallApplication(header.PacketName).Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        //throw;
        //    }
        //    return processed;
        //}
        #endregion 
        #region acknowledge
        private async static Task<bool> ProcessAcknowledge(PacketHeaderModel header, byte[] body)
        {
            // FIX (MOB-B1 blocker 3 / MP2P-8): resolve the pending send this ack answers.
            // The Server echoes InResponseTo == the statement's original MessageId plus an
            // AckStatus; hand both to the SHARED tracker (FebrisP2pAckTracker.Instance -- the
            // same instance the LoopLogic send loop registered against) so the awaiting send
            // unblocks and marks the statement Uploaded (Success) or retries (failure).
            //
            // We deliberately do NOT emit an ack for an ack (no InResponseTo set on our side)
            // to avoid a ping-pong. Legacy / malformed acks (no InResponseTo) are ignored
            // gracefully. `await Task.CompletedTask` keeps this method async without a warning
            // now that the body is synchronous. SOURCE-ONLY (Xamarin can't build/test here).
            await Task.CompletedTask;

            Guid correlationId = header.InResponseTo ?? Guid.Empty;
            AckStatus status = header.AckStatus ?? AckStatus.Failure_Other;
            if (correlationId != Guid.Empty)
            {
                bool resolved = FebrisP2pAckTracker.Instance.TryResolve(correlationId, status);
                if (!resolved)
                {
                    // No pending entry -- the send likely already timed out and was resolved,
                    // or this is a duplicate/late ack. Harmless; log for diagnostics.
                    Console.WriteLine("ProcessAcknowledge: no pending send for InResponseTo=" +
                        correlationId + " (already resolved/timed out?). Status=" + status + ".");
                }
            }
            else
            {
                Console.WriteLine("ProcessAcknowledge: received an ack with no InResponseTo -- ignoring.");
            }

            //read acknowledgement as
            //"success"
            //    "failure"

            return true;
            //throw new NotImplementedException();
        }
        #endregion
        #region IP address update
        internal async static void UpdateIPAddress(DownloadEventArgs e, PacketHeaderModel header)
        {
            //CompanionDeviceViewModel vmDevice = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
            //    .Where(i => i.CompanionDevice.UniqueIdentifier == header.DeviceUniqueIdentifier)
            //    .Single();
            //if (vmDevice != null)
            //{
            //    vmDevice.WiFiIPAddress = e.CompanionIPAddress;
            //}
        }
        #endregion
        #region Initalize
        internal static async Task ProcessInitalization(PacketHeaderModel input)
        {
            try
            {
                //run through bluetooth mac address and wifimacaddresses
                //if (input.BodyType == BodyType._initalize)
                //{
                //    if (LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                //        .Where(i =>
                //        i.CompanionDevice.BlueToothAlias == input.BlueToothDeviceAlias
                //        && i.CompanionDevice.BlueToothName == input.BlueToothDeviceName
                //        && i.CompanionDevice.BlueToothType == input.BlueToothDeviceType
                //        && (i.CompanionDevice.WifiMacAddress == null || i.CompanionDevice.WifiMacAddress == string.Empty)
                //        && (i.CompanionDevice.UniqueIdentifier == input.DeviceUniqueIdentifier || i.CompanionDevice.UniqueIdentifier == null || i.CompanionDevice.UniqueIdentifier == string.Empty))
                //        .Any())
                //    {
                //        CompanionDeviceViewModel device = await PairDevice.WiFiInformationRequest(input);
                //        Console.WriteLine(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList);
                //        PairDevice.SaveCompanionDeviceList();
                //    }
                //}
            }
            catch { }
        }
        #endregion
        #region Pairing
        /// <summary>
        /// The Server opened a numeric-comparison pairing ceremony (docs/MOBILE_AUTH.md 4.1).
        /// Answer with our ephemeral public key; both sides then derive the same six digits and
        /// a human confirms they match.
        ///
        /// Deliberately accepts from an UNPAIRED peer, because pairing is what creates the
        /// pairing. That is safe here in a way it is not for other body types: the ceremony's
        /// security rests on the out-of-band human comparison, and an attacker who sends this
        /// gains only a code that will not match the Server's.
        /// </summary>
        private static bool ProcessPairingRequest(byte[] serverPublicKey)
        {
            try
            {
                byte[] ours = Crypto.P2pPairingCoordinator.AcceptRequest(serverPublicKey);
                if (ours == null)
                {
                    // Either a ceremony is already running, in which case replacing it would let
                    // anyone cancel an operator's pairing mid-comparison, or the key was
                    // unusable.
                    Console.WriteLine("pairing request refused: busy or malformed key");
                    return false;
                }

                var header = new PacketHeaderModel
                {
                    BodyType = BodyType._pairingResponse,
                    PacketName = "Pairing Response",
                    DeviceUniqueIdentifier = DependencyService.Get<IDevice>()?.GetIdentifier()
                };

                byte[] frame = new FebrisP2pFrameBuilder().Build(header, ours);
                Task.Run(async () =>
                {
                    try
                    {
                        IWiFiP2pServer sender = DependencyService.Get<IWiFiP2pServer>();
                        if (sender != null) { await sender.SocketSender(frame); }
                    }
                    catch (Exception ex) { Console.WriteLine("pairing response send failed: " + ex.Message); }
                });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProcessPairingRequest failed: " + ex.Message);
                Crypto.P2pPairingCoordinator.Abort();
                return false;
            }
        }
        #endregion
        #region Video resync
        /// <summary>
        /// The Server has lost decodability and is asking us to rebuild it.
        ///
        /// Does BOTH halves, because the two failure modes need different things and the
        /// Server cannot tell them apart from its side:
        ///   - the codec config was lost, so the decoder was never configured and refuses
        ///     every frame. Resending the cached SPS/PPS is the only fix.
        ///   - a non-IDR was dropped, so every frame is corrupt until the next keyframe.
        ///     Forcing an IDR now is the only fix.
        /// Doing both costs one small frame and removes the guesswork.
        ///
        /// Until this existed, IScreenEncoder.RequestKeyFrame and the cached CodecConfig were
        /// both implemented and both had zero callers anywhere in the repo. The recovery loop
        /// was built on both sides and joined by nothing.
        /// </summary>
        private static bool ProcessVideoResyncRequest()
        {
            try
            {
                IScreenEncoder encoder = DependencyService.Get<IScreenEncoder>();
                if (encoder == null || !encoder.IsRunning)
                {
                    // Nothing to resync. Not an error: the Server may be reacting to frames
                    // that were already in flight when capture stopped.
                    return true;
                }

                // BUILT BY THE ENCODER, not here. This used to hand-roll its own header with only
                // BodyType, PacketName and DeviceUniqueIdentifier, omitting the Width and Height
                // that the primary emit path sends. The receiver builds its MediaFormat from those,
                // so a zero-sized config made MediaCodec.Configure throw IllegalArgumentException
                // and the decoder never started.
                //
                // That made the recovery loop self-defeating: the Server asks for a resync
                // precisely because it has no usable config, and the answer it got could never be
                // one. Observed on device as repeating "video: requesting resync" answered by
                // repeating "ScreenDecoder.Configure failed".
                byte[] frame = encoder.BuildCodecConfigFrame();
                if (frame != null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            IWiFiP2pServer sender = DependencyService.Get<IWiFiP2pServer>();
                            if (sender != null) { await sender.SocketSender(frame); }
                        }
                        catch (Exception ex) { Console.WriteLine("resend codec config failed: " + ex.Message); }
                    });
                }

                // Force an IDR. Without a fresh keyframe the resent config still has nothing
                // decodable to attach to until the encoder's own two-second interval elapses.
                encoder.RequestKeyFrame();
                Console.WriteLine("video resync honoured: codec config resent, keyframe requested");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProcessVideoResyncRequest failed: " + ex.Message);
                return false;
            }
        }
        #endregion
        #region Start Video Stream
        private async static Task<bool> StartVideoStream(PacketHeaderModel header, byte[] body)
        {
            LocalHardwareStaticDetails.StreamVideo = true;
            Task.Run(() => VideoStreamProcessing.VideoStream());
            return true;
        }
        //private static bool ProcessStartVideoStream(PacketHeaderModel header, byte[] body)
        //{            
        //    Task.Run(() => VideoStreamProcessing.VideoStream());
        //    return true;
        //}
        #endregion
        #region End Video Stream
        private async static Task<bool> ProcessEndVideoStream(PacketHeaderModel header, byte[] body)
        {
            LocalHardwareStaticDetails.StreamVideo = false;
            return true;
            //throw new NotImplementedException();
        }
        #endregion
        #endregion
        #endregion



        ///pulled directly from mobile companion application
        ///
        //public static void P2PWifiDownloadTransfer(string receivedFrom, byte[] input)
        //{
        //    //string filename = Guid.NewGuid().ToString();
        //    //string filePath = Path.Combine(FileSystem.FileSystem.StatementPath, filename);
        //    PacketHeaderModel header = new PacketHeaderModel();
        //    string headerString = string.Empty;
        //    byte[] body = { };
        //    bool processed = false;

        //    //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Downloading Data From Mobile Server";
        //    StatusUpdateHelper.DownloadingData();

        //    try
        //    {
        //        if (input.Length != 0)
        //        {
        //            (headerString, body) = SeperateHeaderAndBody(input).Result;
        //            //use header to route rest of data
        //            header = ParsingJsonStringToHeaderModel(headerString);
        //            //process data
        //            processed = ProcessDownloadedData(header, body);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    StatusUpdateHelper.Blank();
        //    //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Downloading Complete";
        //}




    }
}
