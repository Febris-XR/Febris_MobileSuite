// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.P2pNetworkModels.WiFi
{  
    public interface IWiFiService
    {
        #region Calls from xamarin shared
        void DiscoveringPeers();
        CompanionDeviceEventArgs WiFiInformationRequest(string DeviceUniqueIdentifier, string BlueToothDeviceName, string BlueToothDeviceAlias, string BlueToothDeviceType);
        void ListenForServices();
        #endregion
    }

    public interface ICompanionDataCollection
    {        
        Task<DeviceStatusModel> GetStatusInformation();
    }

    
}
