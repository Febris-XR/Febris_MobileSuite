// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface ICompanionDevice
    {
        string Name { get; set; }

        #region Bluetooth info
        //https://docs.microsoft.com/en-us/dotnet/api/android.bluetooth.bluetoothdevice?view=xamarin-android-sdk-9
        //https://developer.android.com/reference/android/bluetooth/BluetoothDevice        
        string BlueToothAlias { get; set; }
        string BlueToothName { get; set; }
        string BlueToothMacAddress { get; set; }
        int BlueToothType { get; set; }

        bool BlueToothConnectionStatus { get; set; }
        int BlueToothBondState { get; set; } //this seems weird
        //public bool BlueToothBondState { get; set; }
        #endregion

        #region Wifip2pdevice info        
        //https://docs.microsoft.com/en-us/dotnet/api/android.net.wifi.p2p.wifip2pdevice?view=xamarin-android-sdk-9
        //https://developer.android.com/reference/android/net/wifi/p2p/WifiP2pDevice
        string WiFiDeviceName { get; set; }
        string WifiMacAddress { get; set; }
        string IPAddress { get; set; }
        string WiFiSocket { get; set; }


        string WiFiPrimaryDeviceType { get; set; }
        string WiFiSecondaryDeviceType { get; set; }

        int WiFiStatus { get; set; }
        //WiFiConnectionStatus WiFiConnectionStatus { get; set; }
        #endregion

    }
}
