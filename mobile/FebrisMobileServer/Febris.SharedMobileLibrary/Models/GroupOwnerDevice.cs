// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models
{
    public class GroupOwnerDevice:BaseModel
    {
        #region This is the standard model setup
        public string Name { get; set; }
        //public string UniqueIdentifier { get; set; }

        #region Bluetooth info
        //https://docs.microsoft.com/en-us/dotnet/api/android.bluetooth.bluetoothdevice?view=xamarin-android-sdk-9
        //https://developer.android.com/reference/android/bluetooth/BluetoothDevice        
        //public string BlueToothAlias { get; set; }
        //public string BlueToothName { get; set; }
        //public string BlueToothMacAddress { get; set; }
        //public string BlueToothType { get; set; }
        //public int BlueToothBondState { get; set; } //this seems weird        
        #endregion

        #region Wifip2pdevice info        
        //https://docs.microsoft.com/en-us/dotnet/api/android.net.wifi.p2p.wifip2pdevice?view=xamarin-android-sdk-9
        //https://developer.android.com/reference/android/net/wifi/p2p/WifiP2pDevice
        public string WiFiDeviceName { get; set; }
        public string WifiMacAddress { get; set; }
        public string WiFiPrimaryDeviceType { get; set; }
        public string WiFiSecondaryDeviceType { get; set; }
        #endregion
        #endregion
    }
}
