// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.P2pCommunication.WiFi
{
    public class WiFiP2pRequestReceiver
    {
        IWiFiService wifi = DependencyService.Get<IWiFiService>();
        DataProtection _dataProtection = new DataProtection();
        public bool _wifiIsEnabled = false;
        
        //public WiFiP2pRequestReceiver()
        //{
        //    wifi = DependencyService.Get<IWiFiService>();
        //}

        public async void P2PWifiDownloadTransfer(object sender, DownloadEventArgs e)
        {
            //string filename = Guid.NewGuid().ToString();
            //string filePath = Path.Combine(FileSystem.StatementPath, filename);
            PacketHeaderModel header = new PacketHeaderModel();
            byte[] body = { };
            bool processed = false;

            try
            {
                if (e.DataPackage.Length != 0)
                {
                    // MP2P-2: shared FebrisP2pFrameParser. ParseBytes returns (PacketHeaderModel, byte[])
                    // directly -- legacy `SeperateHeaderAndBody` + `ParsingJsonStringToHeaderModel`
                    // 2-call sequence collapses to 1. `headerString` intermediate is gone.
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
                    // PEER GATE (docs/MOBILE_P2P_VIDEO.md 6.3). This is the single receive
                    // entry, so it is the one place worth gating. It must run BEFORE both
                    // calls below, and especially before UpdateIPAddress: that method
                    // resolves a device purely by the identifier in this frame and
                    // overwrites its WiFiIPAddress with the connecting socket's source
                    // address, and that field is what SocketSender uses to pick a
                    // destination. Ungated, one frame carrying an observed identifier
                    // re-pointed the Server's OUTBOUND channel, stream control and module
                    // pushes included.
                    P2pAuthorizationResult auth = P2pPeerAuthorization.Authorize(header, IsKnownPairedPeer(header));
                    if (!auth.IsAuthorized)
                    {
                        Console.WriteLine("P2P frame rejected from " + e.CompanionIPAddress + ": " + auth.DenyReason);
                        await NackRefusedFrame(e, header, AckStatus.Failure_BadHeader);
                        return;
                    }

                    // DIRECTION GATE. Separate from the peer gate above, which only answers
                    // "is this peer known" and says nothing about a known peer sending
                    // something only this side has any business sending. The Server had a
                    // live dispatch case for _videoStreamStart, which the enum documents as
                    // Server-to-Companion, so a Companion could drive it.
                    if (!P2pPeerAuthorization.IsLegalInbound(
                            header.BodyType, P2pPeerAuthorization.P2pTier.MobileServer))
                    {
                        Console.WriteLine("P2P frame rejected from " + e.CompanionIPAddress +
                            ": body type " + header.BodyType + " is not legal inbound at the Mobile Server.");
                        await NackRefusedFrame(e, header, AckStatus.Failure_BadHeader);
                        return;
                    }

                    // REFRESH THE ADDRESS BEFORE DISPATCHING, not after.
                    //
                    // This ran after ProcessDownloadedData, and it is the only writer of
                    // CompanionDeviceViewModel.WiFiIPAddress. So any reply generated DURING
                    // dispatch was routed to the address recorded by a PREVIOUS frame:
                    // EmitStatementAck resolves the device and sends on that stale field. On a
                    // cold Server process the field is null and the send throws; if the peer's
                    // address had changed it goes nowhere. Either way no ack arrives, and a
                    // missing ack is what makes the Companion mark a statement Uploaded and
                    // delete it.
                    //
                    // The security ordering that matters is unchanged: this still runs after
                    // both gates, which is what the original comment was protecting.
                    //
                    // ONLY refresh the routing address when the peer actually RESOLVED.
                    //
                    // UpdateIPAddress finds a device purely by the identifier in this header
                    // and overwrites the WiFiIPAddress that SocketSender routes on. It used to
                    // run for every authorized frame, including ones waved through by the
                    // pre-authentication exemption, which runs BEFORE both the empty-identifier
                    // check and the known-peer check. So an unpaired device could re-point
                    // another device's outbound channel with a single _initalize frame, and an
                    // _initalize carrying NO identifier could match a device mid-onboarding
                    // through the empty-to-empty comparison in that lookup.
                    //
                    // Allowing a frame to proceed and trusting its identifier are two different
                    // decisions. P2pPeerAuthorization's own docblock claimed the gate closed
                    // this hijack; it did not, because the gate returned a bare "authorized"
                    // that could not express the difference. It can now.
                    if (auth.PeerResolved)
                    {
                        await WiFiP2pRequestProcessing.UpdateIPAddress(e, header);
                    }

                    //process data
                    processed = await WiFiP2pRequestProcessing.ProcessDownloadedData(header, body);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// The second receive door, and a latent leak. It writes the RAW unparsed wire
        /// package straight to disk under FileSystem.StatementPath with no BodyType
        /// inspection and no peer check. It is subscribed at App.xaml.cs:81 to an event
        /// that is never raised, so it is inert today, but wiring that event for any
        /// unrelated reason would start persisting whatever arrives. Once the screen
        /// stream exists, "whatever arrives" includes H.264 of a learner's session, and
        /// the live stream is meant to be ephemeral by construction
        /// (docs/MOBILE_P2P_VIDEO.md 2.7).
        ///
        /// Gated off rather than deleted: the repo convention is to disable dead code in
        /// place, and deleting the handler would also require unpicking the App.xaml.cs
        /// subscription. Flip AllowRawUploadPersistence only alongside a real peer check
        /// and an explicit BodyType allowlist.
        /// </summary>
        internal static bool AllowRawUploadPersistence = false;

        public void P2PWifiUploadTransfer(object sender, UploadEventArgs e)
        {
            if (!AllowRawUploadPersistence)
            {
                Console.WriteLine("P2PWifiUploadTransfer: raw upload persistence is disabled, dropping " +
                    (e?.DataPackage?.Length ?? 0) + " bytes");
                return;
            }

            string filename = Guid.NewGuid().ToString();
            string filePath = Path.Combine(FileSystem.StatementPath, filename);
            //string filePath = FileSystem.StatementPath;
            //if (e.ReceivedInformation != null) 
            //{
            //    using (MemoryStream memoryStream = new MemoryStream())
            //    {
            //        e.ReceivedInformation.CopyTo(memoryStream);
            //        byte[] fileData = memoryStream.ToArray();
            //        File.WriteAllBytes(filePath,fileData);
            //    }
            //}
            //else
            try
            {
                if (e.DataPackage.Length != 0)
                {
                    File.WriteAllBytes(filePath, e.DataPackage);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
        }

        /// <summary>
        /// The Server's trust anchor: a device counts as known only if its identifier
        /// already appears in the paired-device list. That list is the same one
        /// UpdateIPAddress resolves against, so gating on it means an unpaired device
        /// cannot reach the code that re-points the outbound channel.
        ///
        /// Empty stored identifiers are excluded deliberately. A device paired over
        /// Bluetooth but not yet associated over WiFi has an empty UniqueIdentifier, and
        /// treating empty as a match would let any frame with a blank identifier pass.
        /// That association step is BodyType._initalize, which is allowed through
        /// separately by P2pPeerAuthorization because its handler enforces the Bluetooth
        /// pairing itself.
        /// </summary>
        /// <summary>
        /// Tell a refused sender that its frame was refused.
        ///
        /// WHY A REFUSAL NEEDS A REPLY AT ALL. The Companion arms an ack timeout for every
        /// statement, and its timeout branch marks the statement Uploaded best-effort, which
        /// removes it from the unsent set and queues it for deletion. Silence therefore
        /// DESTROYS the statement, while an explicit failure status takes the retry branch and
        /// preserves it. Refusing the frame is correct; refusing it silently converts a
        /// security decision into permanent data loss, and that was net-new harm from the peer
        /// gate: before it, an unknown-peer statement was still persisted.
        ///
        /// Routed on the CONNECTION, not on the header. The device lookup is the thing that
        /// just failed, and trusting the identifier here to pick a destination would reopen
        /// exactly the outbound-hijack the gate exists to close.
        ///
        /// Only frames that asked for an ack get one (MessageId != Guid.Empty). A refused peer
        /// cannot use this as an amplifier or a probe: it learns only that a frame it already
        /// sent was rejected, which it could infer from the missing ack anyway.
        /// </summary>
        private static async Task NackRefusedFrame(DownloadEventArgs e, PacketHeaderModel header, AckStatus status)
        {
            try
            {
                if (header == null || header.MessageId == Guid.Empty)
                {
                    return;   // legacy sender, or a frame with no ack semantics
                }

                var nack = new PacketHeaderModel
                {
                    BodyType = BodyType._acknowledge,
                    PacketName = header.PacketName,
                    DeviceUniqueIdentifier = header.DeviceUniqueIdentifier,
                    InResponseTo = header.MessageId,
                    AckStatus = status
                };

                byte[] frame = new FebrisP2pFrameBuilder().Build(nack);
                await DependencyService.Get<IWiFiP2pServer>().SocketSender(frame, e?.CompanionIPAddress);
            }
            catch (Exception ex)
            {
                // Never let the courtesy reply break the refusal itself.
                Console.WriteLine("Failed to NACK a refused frame: " + ex.Message);
            }
        }

        private static bool IsKnownPairedPeer(PacketHeaderModel header)
        {
            try
            {
                string claimed = header?.DeviceUniqueIdentifier;
                if (string.IsNullOrWhiteSpace(claimed))
                {
                    return false;
                }

                return LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.ItemList
                    ?.Any(i => i?.CompanionDevice != null
                            && !string.IsNullOrWhiteSpace(i.CompanionDevice.UniqueIdentifier)
                            && i.CompanionDevice.UniqueIdentifier == claimed) ?? false;
            }
            catch (Exception ex)
            {
                // Fail CLOSED. An unreadable device list must not become an open door.
                Console.WriteLine("IsKnownPairedPeer check failed, denying: " + ex.Message);
                return false;
            }
        }

        public void OnReceivedStatusUpdate(object sender, ConnectionStatusCheckEventArgs e)
        {
            //get list of client devices - see if wifimacaddress exists in paired list 
            List<CompanionDeviceViewModel> companionList = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList;
            List<string> unknownList = new List<string>();

            List<CompanionDeviceViewModel> CompanionsWithUIdsList = new List<CompanionDeviceViewModel>();
            List<CompanionDeviceViewModel> CompanionsWithWifiMacList = new List<CompanionDeviceViewModel>();
            List<CompanionDeviceViewModel> CompanionsWithBlueToothList = new List<CompanionDeviceViewModel>();

            int groupMemberCount = 0;

            //sort them into catagories
            foreach (var i in companionList)
            {
                if (string.IsNullOrEmpty(i.CompanionDevice.UniqueIdentifier))
                {
                    CompanionsWithUIdsList.Add(i);
                }

                if (string.IsNullOrEmpty(i.CompanionDevice.WifiMacAddress))
                {
                    CompanionsWithWifiMacList.Add(i);
                }
                else if (string.IsNullOrEmpty(i.CompanionDevice.BlueToothMacAddress))
                {
                    CompanionsWithBlueToothList.Add(i);
                }
            }



            if (e.ClientUniqueIdList != null)
            {
                groupMemberCount = e.ClientUniqueIdList.Count;

                if (groupMemberCount > (CompanionsWithUIdsList.Count + CompanionsWithBlueToothList.Count))
                {
                    //check Ids and if a mac addess is not recognized scrutinize it
                }
            }
            else if (e.WifiMacAddressList != null)
            {
                groupMemberCount = e.WifiMacAddressList.Count;

                if (groupMemberCount > (CompanionsWithWifiMacList.Count + CompanionsWithBlueToothList.Count))
                {
                    //check Ids and if a mac addess is not recognized scrutinize it
                }
            }
            else if (e.BlueToothMacAddressList != null)
            {
                groupMemberCount = e.BlueToothMacAddressList.Count;

                if (groupMemberCount > (CompanionsWithWifiMacList.Count + CompanionsWithBlueToothList.Count))
                {
                    //check Ids and if a mac addess is not recognized scrutinize it
                }
            }

            foreach (var i in unknownList)
            {

            }

            //wifi.RemoveClient();
            //throw new NotImplementedException();
        }


        #region for wifi broadcast reciever

        #region State Changed
        /// <summary>
        /// Add a button that changes when to a different color to indicate if the wifi connection is enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnStateChanged(object sender, CompanionDeviceEventArgs e)
        {
            if (e.WiFiEnable)
            {
                _wifiIsEnabled = true;
                Console.WriteLine("Wifi Enabled");
            }
            else
            {
                _wifiIsEnabled = false;
                Console.WriteLine("Wifi Diabled");
            }
        }
        #endregion

        #region  Connection Success Changed
        internal void OnConnectSuccessChanged(object sender, CompanionDeviceEventArgs e)
        {
            try
            {
                //#############################Issue here because null is handed back from calling thing################################
                //CompanionDeviceViewModel connectedDevice = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.WiFiDeviceName == e.ConnectDeviceName).FirstOrDefault();
                //if (e.ConnectSuccess)
                //{
                //Console.WriteLine("Companion device address: " + e.ConnectDeviceName);
                //Console.WriteLine("Connect Sucess");
                CompanionDeviceViewModel temp = null;
                try
                {
                    if (LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                        .Where(j => j.CompanionDevice.WifiMacAddress != string.Empty
                        && j.CompanionDevice.WifiMacAddress != null
                        && j.CompanionDevice.WifiMacAddress == e.ConnectedDeviceMacAddress)
                        .Any())
                    {
                        temp = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                       .Where(i => i.CompanionDevice.WifiMacAddress == e.ConnectedDeviceMacAddress)
                        .Single();
                    }
                    else 
                    { 
                        return; 
                    }
                    //else if (LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    //    .Where(j => e.BlueToothMacAddress == j.CompanionDevice.BlueToothMacAddress)
                    //    .Any())
                    //{
                    //    if (temp == null && e.ConnectSuccess)
                    //    {
                    //        temp = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    //        .Where(j => e.BlueToothMacAddress == j.CompanionDevice.BlueToothMacAddress)
                    //        .Single();

                    //        if (temp != null)
                    //        {
                    //            //Task.Run(() => PairDevice.UpdateCompanionDevice(e, temp.CompanionDevice));
                    //            //Task.Run(() => PairDevice.UpdateCompanionDeviceViewModel(e, temp));//.ConfigureAwait(false);
                    //        }
                    //    }
                    //}


                    //if (LocalHardwareStaticDetails.PairedDeviceViewModelList
                    //    .Where(j => j.CompanionDevice.WifiMacAddress != string.Empty
                    //    && j.CompanionDevice.WifiMacAddress != null
                    //    && j.CompanionDevice.WifiMacAddress == e.ConnectedDeviceMacAddress)
                    //    .Any())
                    //{
                    //    temp = LocalHardwareStaticDetails.PairedDeviceViewModelList
                    //   .Where(i => i.CompanionDevice.WifiMacAddress == e.ConnectedDeviceMacAddress)
                    //    .Single();
                    //}
                    //else if (LocalHardwareStaticDetails.PairedDeviceList
                    //    .Where(j => e.BlueToothMacAddress==j.BlueToothMacAddress)
                    //    .Any())
                    //{
                    //    if (temp == null && e.ConnectSuccess)
                    //    {
                    //        temp = LocalHardwareStaticDetails.PairedDeviceViewModelList
                    //        .Where(j => e.BlueToothMacAddress == j.CompanionDevice.BlueToothMacAddress)                            
                    //        .Single();

                    //        if (temp != null)
                    //        {
                    //            //Task.Run(() => PairDevice.UpdateCompanionDevice(e, temp.CompanionDevice));
                    //            //Task.Run(() => PairDevice.UpdateCompanionDeviceViewModel(e, temp));//.ConfigureAwait(false);
                    //        }
                    //    }
                    //}

                }
                catch { 
                    return; 
                }

                //CompanionDeviceViewModel temp = LocalHardwareStaticDetails.PairedDeviceViewModelList
                //   .Where(i => i.CompanionDevice.WifiMacAddress == e.ConnectedDeviceMacAddress)
                //    .Single();

                //if (temp == null && e.ConnectSuccess)
                //{
                //    temp = LocalHardwareStaticDetails.PairedDeviceViewModelList
                //    .Where(i => i.CompanionDevice.BlueToothName == e.ConnectedDeviceName)
                //    .Single();
                //    if (temp != null)
                //    {

                //        Task.Run(() => PairDevice.Update(e, temp.CompanionDevice)).ConfigureAwait(false);
                //    }
                //}

                temp.WiFiConnected = e.ConnectSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        #endregion

        #region On connection changed
        //This read the "I am client and host" thing
        internal void OnConnectionChanged(object sender, CompanionDeviceEventArgs e)
        {
            Console.WriteLine(e.ConnectedDeviceName);
            //modify button colors
            CompanionDeviceViewModel connectedDevice = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                .Where(i => i.CompanionDevice.WifiMacAddress == e.ConnectedDeviceName).FirstOrDefault();

            // FirstOrDefault can return null and both branches below dereferenced it unguarded, so
            // an unrecognised peer threw instead of being reported. A miss is entirely ordinary
            // here: a device whose WiFi identity has not been resolved yet has a blank
            // WifiMacAddress and cannot match anything.
            if (connectedDevice == null)
            {
                Console.WriteLine("connection changed for an unknown peer '" + e.ConnectedDeviceName
                    + "', no device record matches that WiFi MAC");
                return;
            }

            if (e.ConnectSuccess)
            {
                Console.WriteLine("Connect Sucess");
                connectedDevice.WiFiConnected = true;
                //change button colors on device list
            }
            else
            {
                connectedDevice.WiFiConnected = false;
                Console.WriteLine("No Connection");
            }
        }
        #endregion

        #region on recieved message changed        
        internal void OnReceivedMessageChanged(object sender, CompanionDeviceEventArgs e)
        {
            Console.WriteLine("ReceievedMessage: " + e.ReceivedMessage);
        }
        #endregion

        //#region on recieved status update
        //private void OnReceivedStatusUpdate(object sender, StatusCheckEventArgs e)
        //{

        //}
        //#endregion

        #region Getting Peers list
        internal void OnPeersChanged(object sender, CompanionDeviceEventArgs e)
        {
            //Console.WriteLine(e.CompanionDeviceNameList);
            //Console.WriteLine(e.);
            try
            {
                foreach (var j in e.CompanionDeviceNameList)
                {
                    if (LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                        .Where(i => j == i.CompanionDevice.WiFiDeviceName).Any())
                    {
                        //Console.WriteLine("Peer " + j + " Changed");
                    }
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