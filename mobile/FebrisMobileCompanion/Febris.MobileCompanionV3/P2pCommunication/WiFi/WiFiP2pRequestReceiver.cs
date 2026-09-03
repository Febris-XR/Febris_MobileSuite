// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.BusinessLogic;
using Febris.MobileCompanionV3.DataLogic;
using Febris.MobileCompanionV3.Resources;
using Febris.MobileCompanionV3.Utilities;
using Febris.SharedMobileLibrary.Enums;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileCompanionV3.P2pCommunication.WiFi
{
    public class WiFiP2pRequestReceiver
    {
        IWiFiService wifi = DependencyService.Get<IWiFiService>();
        DataProtection _dataProtection = new DataProtection();
        ServerDeviceContext _context = new ServerDeviceContext();
        public bool _wifiIsEnabled = false;
        private LoopLogic _looper = new LoopLogic();

        //public WiFiP2pRequestReceiver()
        //{
        //    wifi = DependencyService.Get<IWiFiService>();
        //}

        public async void P2PWifiDownloadTransfer(object sender, DownloadEventArgs e)
        {
            PacketHeaderModel header = new PacketHeaderModel();
            byte[] body = { };
            bool processed = false;

            try
            {
                if (e.DataPackage.Length != 0)
                {
                    // MP2P-2: shared FebrisP2pFrameParser. ParseBytes returns (PacketHeaderModel, byte[])
                    // directly -- legacy `SeperateHeaderAndBody` + `ParsingJsonStringToHeaderModel`
                    // 2-call sequence collapses to 1. The `if (header == default)` check is no longer
                    // needed because ParseBytes either returns a valid header or throws.
                    try
                    {
                        (header, body) = new FebrisP2pFrameParser().ParseBytes(e.DataPackage);
                    }
                    catch (FebrisP2pFrameException ex)
                    {
                        // MP2P-7: structured exception. Logging via IFebrisP2pLogger lands with that tier.
                        Console.WriteLine("P2P frame parse failed: " + ex.Message);
                        return;
                    }
                    // DIRECTION GATE, and the first frame-level check this tier has ever had.
                    //
                    // Deliberately NOT P2pPeerAuthorization.Authorize, which is what the
                    // Server uses. That call requires a non-empty DeviceUniqueIdentifier for
                    // every non-pre-authentication type, and the Server does not populate it
                    // on most of its outbound frames: RequestHelper, StatementLogic and
                    // CompanionModuleLogic all construct headers without it. Adding the peer
                    // gate here would therefore deny nearly all legitimate Server traffic.
                    //
                    // The Companion also has no peer allowlist to consult: it never stores a
                    // server identity to resolve against, and the identifier the Server DOES
                    // stamp on its handshake replies and acks is the COMPANION's own, not its
                    // own. Connection-level identity is the anchor on this side, and
                    // ClientSocketThread already gates on the handshake coordinator.
                    //
                    // What is genuinely missing here is direction, so that is what this adds.
                    if (!P2pPeerAuthorization.IsLegalInbound(
                            header.BodyType, P2pPeerAuthorization.P2pTier.Companion))
                    {
                        Console.WriteLine("P2P frame rejected: body type " + header.BodyType +
                            " is not legal inbound at the Companion.");
                        return;
                    }

                    // Before dispatch and for EVERY body type, mirroring the Mobile Server, which
                    // calls NotePeerIsTalking at the same point for the same reason. Reaching here
                    // means the frame parsed and passed the direction gate above, so the far end is
                    // demonstrably a Febris peer and not merely an open socket.
                    StatusUpdateHelper.ServerResponding();

                    //process data
                    processed = await WiFiP2pRequestProcessing.ProcessDownloadedData(header, body);
                    //update ip addres of view model
                    WiFiP2pRequestProcessing.UpdateIPAddress(e, header);
                }
            }
            catch (Exception ex)
            {
                // MP2P-6: GC.Collect + GC.WaitForPendingFinalizers were removed here. A forced full
                // GC inside a catch handler buys nothing -- the exception path doesn't free more than
                // the runtime would on its own next collection.
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// DISABLED 2026-07-29. Commented out rather than deleted at the owner's instruction.
        ///
        /// This wrote the RAW unparsed wire package straight into the statement directory under
        /// a fresh <c>Guid.NewGuid()</c>, with no BodyType inspection and no peer check. It is
        /// the ONLY place in either tier that invents a file name, which matters because a
        /// mismatch between invented and real names is exactly what sent the owner looking for
        /// fabricated data in the first place.
        ///
        /// The Server's identical twin was hardened behind
        /// <c>AllowRawUploadPersistence = false</c>; this copy never received that gate. Inert
        /// today only because <c>WifiP2pCompanionUploadAction</c> has no raiser anywhere, which
        /// is a property of the current tree rather than a guarantee. Wiring that event for any
        /// unrelated reason would have started persisting whatever arrived.
        ///
        /// Deletion is tracked in the roadmap.
        /// </summary>
        //public async void P2PWifiUploadTransfer(object sender, UploadEventArgs e)
        //{
        //    string filename = Guid.NewGuid().ToString();
        //    string filePath = Path.Combine(FileSystem.StatementPath, filename);
        //    try
        //    {
        //        if (e.DataPackage.Length != 0)
        //        {
        //            File.WriteAllBytes(filePath, e.DataPackage);
        //        }
        //    }
        //    catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
        //}

        internal async void OnClientSocketCreation(object sender, ClientSocketCreationCheckEventArgs e)
        {
            //Task.Run(() => _looper.PingStarter());
        }

        public async void OnReceivedStatusUpdate(object sender, ConnectionStatusCheckEventArgs e)
        {
            // MP2P-7: replaced `throw new NotImplementedException()`. This is an async-void event
            // handler wired into EventHandlerHelper.WifiP2pCompanionStatusCheckAction via
            // App.xaml.cs. The event has no live raiser today (grep finds no `?.Invoke` or `(this,`
            // call sites) -- but if any future code raises it, throwing from an async-void handler
            // crashes the entire app (unhandled exception bubbling out of the async state machine).
            // Stub to a logged no-op so the handler is safe to invoke; real behavior can land when
            // we know what the Companion should do with a status update from the Server.
            ConsoleFebrisP2pLogger.Instance.Log(
                FebrisP2pLogLevel.Info,
                "Companion OnReceivedStatusUpdate invoked (no behavior wired yet).");
            await Task.CompletedTask;
        }

        internal async void ServiceDiscoveryToggleAction(object sender, CompanionDiscoveryEventArgs e)
        {
            Console.WriteLine("*************PassedBack ServiceDiscoveryToggleAction Value*************");
            Console.WriteLine("Service Discovery toggled to: " + e.StartDiscoveryLoops.ToString());
            try
            {
                LocalHardwareStaticDetails.RunPeerDiscovery = e.StartDiscoveryLoops;
                LocalHardwareStaticDetails.RunServiceListener = e.StartDiscoveryLoops;
                if (e.StartDiscoveryLoops)
                {
                    StatusUpdateHelper.Disconnected();
                    //LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = false;
                }
                else
                {
                    _looper.PingStarter();
                    StatusUpdateHelper.Connected();
                    //LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("*************Error in ServiceDiscoveryToggleAction*************");
                Console.WriteLine(ex.StackTrace);
                throw;
            }

            Console.WriteLine("************************************************************************");
        }


        #region for wifi broadcast reciever

        #region State Changed
        /// <summary>
        /// Add a button that changes when to a different color to indicate if the wifi connection is enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal async void OnStateChanged(object sender, CompanionDeviceEventArgs e)
        {
            if (e.WiFiEnable)
            {
                _wifiIsEnabled = true;

                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Wifi Enabled";
            }
            else
            {
                _wifiIsEnabled = false;
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Wifi Disabled";
            }
        }
        #endregion

        #region  Connection Success Changed
        internal async void OnConnectSuccessChanged(object sender, CompanionDeviceEventArgs e)
        {
            try
            {
                //#############################Issue here because null is handed back from calling thing################################
                //CompanionDeviceViewModel connectedDevice = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.WiFiDeviceName == e.ConnectDeviceName).FirstOrDefault();
                //if (e.ConnectSuccess)
                //{
                //Console.WriteLine("Companion device address: " + e.ConnectDeviceName);
                //Console.WriteLine("Connect Sucess");

                //CompanionDeviceViewModel temp = null;
                try
                {
                    //if (e.ConnectionStatus==ConnectionStatus.Connected)
                    if (e.ConnectSuccess == true)
                    {
                        if (e.WifiMacAddress != LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerAddress)
                        {
                            GroupOwnerDevice groupOwner = new GroupOwnerDevice()
                            {
                                Name = e.Name,
                                WifiMacAddress = e.WifiMacAddress,
                                WiFiDeviceName = e.WiFiDeviceName,
                                WiFiPrimaryDeviceType = e.WiFiPrimaryDeviceType,
                                WiFiSecondaryDeviceType = e.WiFiSecondaryDeviceType
                            };
                            groupOwner = await _context.Post(groupOwner);
                        }
                        LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerAddress = e.WifiMacAddress;
                        LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerDeviceName = e.Name;
                        LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.GroupInterface = e.WiFiGroupInterface;
                        LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.NetworkName = e.WiFiNetworkName;
                    }


                    //if (e.ConnectionStatus == ConnectionStatus.Connected)
                    //if (e.ConnectSuccess == true)
                    //{
                    //    LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = true;
                    //}
                    //else
                    //{
                    //    LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = false;
                    //}

                    LocalHardwareStaticDetails.StaticMainVM.HomeVM.P2pServerInfo = new P2pServerInfo()
                    {
                        GroupOwnerAddress = e.IPAddress,
                        GroupOwnerMac = e.ConnectedDeviceMacAddress,
                    };

                    ///Move this to an area that fires only after the socket is created
                    //_looper.PingStarter(); 

                }
                catch
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        //internal async void AfterClientSocketCreation(object sender, CompanionDeviceEventArgs e)
        //{
        //    try
        //    {
        //        //#############################Issue here because null is handed back from calling thing################################
        //        //CompanionDeviceViewModel connectedDevice = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.WiFiDeviceName == e.ConnectDeviceName).FirstOrDefault();
        //        //if (e.ConnectSuccess)
        //        //{
        //        //Console.WriteLine("Companion device address: " + e.ConnectDeviceName);
        //        //Console.WriteLine("Connect Sucess");

        //        //CompanionDeviceViewModel temp = null;
        //        try
        //        {
        //            if (e.ConnectSuccess)
        //            {
        //                if (e.WifiMacAddress != LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerAddress)
        //                {
        //                    GroupOwnerDevice groupOwner = new GroupOwnerDevice()
        //                    {
        //                        Name = e.Name,
        //                        WifiMacAddress = e.WifiMacAddress,
        //                        WiFiDeviceName = e.WiFiDeviceName,
        //                        WiFiPrimaryDeviceType = e.WiFiPrimaryDeviceType,
        //                        WiFiSecondaryDeviceType = e.WiFiSecondaryDeviceType
        //                    };

        //                    groupOwner = _context.Post(groupOwner).Result;



        //                }
        //                LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerAddress = e.WifiMacAddress;
        //                LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerDeviceName = e.Name;
        //                LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.GroupInterface = e.WiFiGroupInterface;
        //                LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.NetworkName = e.WiFiNetworkName;
        //            }

        //            //LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = e.ConnectSuccess;
        //            LocalHardwareStaticDetails.StaticMainVM.HomeVM.P2pServerInfo = new P2pServerInfo()
        //            {
        //                GroupOwnerAddress = e.IPAddress,
        //                GroupOwnerMac = e.ConnectedDeviceMacAddress,
        //            };

        //        }
        //        catch { return; }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //}

        #endregion

        #region On connection changed
        //This read the "I am client and host" thing
        internal async void OnConnectionChanged(object sender, CompanionDeviceEventArgs e)
        {
            Console.WriteLine(e.ConnectedDeviceName);

            //LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = e.ConnectSuccess;
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.P2pServerInfo = new P2pServerInfo()
            {
                GroupOwnerAddress = e.IPAddress,
                GroupOwnerMac = e.ConnectedDeviceMacAddress,
            };

            if (e.ConnectSuccess)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Connect Success";
                //connectedDevice.WiFiConnected = true;
                //change button colors on device list
            }
            else
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "No Connection";
            }
        }
        #endregion

        #region on recieved message changed        
        internal async void OnReceivedMessageChanged(object sender, CompanionDeviceEventArgs e)
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "ReceievedMessage: " + e.ReceivedMessage;
        }
        #endregion

        //#region on recieved status update
        //private void OnReceivedStatusUpdate(object sender, StatusCheckEventArgs e)
        //{

        //}
        //#endregion

        #region Getting Peers list
        internal async void OnPeersChanged(object sender, CompanionDeviceEventArgs e)
        {
            //Console.WriteLine(e.CompanionDeviceNameList);
            //Console.WriteLine(e.);
            try
            {
                foreach (var j in e.CompanionDeviceNameList)
                {
                    //if (LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    //    .Where(i => j == i.CompanionDevice.WiFiDeviceName).Any())
                    //{
                    //    //Console.WriteLine("Peer " + j + " Changed");
                    //}
                }
            }
            catch { }
        }
        #endregion

        #region Loop service - this is what is constantly looking for messages
        internal async void LoopForServerReceivedMessage()
        {
            while (true)
            {
                try
                {
                    //testMessage = wifi.GetServerReceivedMessage();
                    //Console.WriteLine(testMessage.ReceivedMessage);
                    //Console.WriteLine("ReceievedMessage: " + testMessage.ReceivedMessage);
                    //lblServerReceived.Text = testMessage.ReceivedMessage;
                }
                catch { }
                await Task.Delay(1000);
            }
        }
        #endregion

        #endregion
    }
}
