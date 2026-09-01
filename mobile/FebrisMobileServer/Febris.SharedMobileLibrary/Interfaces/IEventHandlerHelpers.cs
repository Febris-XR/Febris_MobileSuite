// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models.EventArguments;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface IEventHandlerHelper
    {
        #region Broadcast Reciever Events

        #endregion
        #region Companion Connection Discovery (Server will not need this because it is always looking)
        event EventHandler<CompanionDiscoveryEventArgs> ServiceDiscoveryToggleAction;
        #endregion
        #region data communication events
        //event EventHandler<ModuleInitalizationEventArgs> WifiP2pModuleInitalizationAction;
        #endregion


        #region Wifi p2p Events

        event EventHandler<CompanionDeviceEventArgs> WifiP2pPeersChangedAction;
        event EventHandler<CompanionDeviceEventArgs> WifiP2pStateChangedAction;
        event EventHandler<CompanionDeviceEventArgs> WifiP2pConnectSuccessAction;
        event EventHandler<CompanionDeviceEventArgs> WifiP2pConnectionChangedAction;
        event EventHandler<CompanionDeviceEventArgs> WifiP2pReceiveMessageAction;

        event EventHandler<DownloadEventArgs> WifiP2pCompanionDownloadAction;
        event EventHandler<UploadEventArgs> WifiP2pCompanionUploadAction;
        event EventHandler<ConnectionStatusCheckEventArgs> WifiP2pCompanionStatusCheckAction;
        #endregion

        #region Bluetooth Events
        event EventHandler<CompanionDeviceEventArgs> CompanionCreationAction;
        event EventHandler<BTCompanionDeviceEventArgs> BTConnectionUploadSuccessAction;
        event EventHandler<BTCompanionDeviceEventArgs> BTConnectionSuccessAction;
        event EventHandler<BTCompanionDeviceEventArgs> BTConnectionChangedAction;
        #endregion

        #region Usb Events
        event EventHandler<EventArgs> UsbConnectionAction;
        #endregion

    }

    public interface IModuleEventHandlerHelper
    {
        event EventHandler<ModuleEventArgs> ModuleIndexUpdateAction;

    }

    public interface IStatementEventHandlerHelper
    {
        event EventHandler<StatementEventArgs> StatementUpdateAction;
        event EventHandler<StatementEventArgs> StatementCreateAction;
        event EventHandler<StatementEventArgs> StatementErrorAction;

    }
}
