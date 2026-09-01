// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.P2pCommunication.WiFi
{
    public interface IWiFiP2pServer
    {
        Task<bool> SocketSender(byte[] dataPacket, CompanionDeviceViewModel clientAddress);

        /// <summary>
        /// Send on the connection at <paramref name="clientIpAddress"/> directly, without
        /// resolving a paired device first.
        ///
        /// This exists for exactly one job: replying to a frame the peer gate REFUSED. The
        /// device-based overload resolves through the paired-device list, which is the lookup
        /// that just failed, so it cannot carry the refusal back. Answering on the socket the
        /// frame arrived on is both the only route available and the more honest one, because
        /// it does not trust anything the peer asserted in the header.
        ///
        /// Use it only for that. Ordinary traffic should keep going through the device
        /// overload so routing stays tied to a resolved device.
        /// </summary>
        Task<bool> SocketSender(byte[] dataPacket, string clientIpAddress);

        /// <summary>
        /// Addresses of peers with a LIVE socket right now, whether or not they are paired.
        ///
        /// This is the seam that makes onboarding-by-pairing possible. A Companion gets a socket
        /// purely by joining the WiFi Direct group and dialling in; nothing about the transport
        /// requires a prior pairing. So the set of connected peers is strictly larger than the set
        /// of known devices, and the difference is exactly the set of devices a human might want
        /// to pair with.
        ///
        /// The socket table lives in the Android head, so the portable UI cannot read it directly
        /// and asks through here, matching how the rest of this tier reaches platform state.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<string> ConnectedPeerAddresses();
    }
    //public interface IWiFiService
    //{
    //    #region Events
    //    #region Connection Events
    //    event EventHandler<CompanionDeviceEventArgs> WifiP2pPeersChangedAction;
    //    event EventHandler<CompanionDeviceEventArgs> WifiP2pStateChangedAction;
    //    event EventHandler<CompanionDeviceEventArgs> WifiP2pConnectSuccessAction;
    //    event EventHandler<CompanionDeviceEventArgs> WifiP2pConnectionChangedAction;
    //    event EventHandler<CompanionDeviceEventArgs> WifiP2pReceiveMessageAction;
    //    #endregion
    //    #region data communication events
    //    event EventHandler<CompanionDeviceEventArgs> CompanionCreationAction;
    //    event EventHandler<DownloadEventArgs> WifiP2pCompanionDownloadAction;
    //    event EventHandler<UploadEventArgs> WifiP2pCompanionUploadAction;
    //    //event EventHandler<ModuleInitalizationEventArgs> WifiP2pModuleInitalizationAction;
    //    #endregion
    //    #region data communication events
    //    event EventHandler<ConnectionStatusCheckEventArgs> WifiP2pCompanionStatusCheckAction;
    //    #endregion
    //    #endregion

    //    #region Calls from xamarin shared
    //    void DiscoveringPeers();
    //    CompanionDeviceEventArgs WiFiInformationRequest(string DeviceUniqueIdentifier, string BlueToothDeviceName, string BlueToothDeviceAlias, string BlueToothDeviceType);
    //    #endregion

    //}


}
