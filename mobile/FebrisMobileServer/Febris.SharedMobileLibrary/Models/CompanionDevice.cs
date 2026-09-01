// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models
{
    public class CompanionDevice //: INotifyPropertyChanged
    {
        #region Keep viewmodel updated       
        //public event PropertyChangedEventHandler PropertyChanged;

        //protected void OnPropertyChanged([CallerMemberName] string name = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        //}


        //private string name;
        //public string Name { get { return name; } set { name = value; OnPropertyChanged(); } }
        //#region Bluetooth info        
        ////https://docs.microsoft.com/en-us/dotnet/api/android.bluetooth.bluetoothdevice?view=xamarin-android-sdk-9
        ////https://developer.android.com/reference/android/bluetooth/BluetoothDevice        
        //private string blueToothAlias;
        //public string BlueToothAlias { get { return blueToothAlias; } set { blueToothAlias = value; OnPropertyChanged(); } }
        //private string blueToothName;
        //public string BlueToothName { get { return blueToothName; } set { blueToothName = value; OnPropertyChanged(); } }
        //private string blueToothMacAddress;
        //public string BlueToothMacAddress { get { return blueToothMacAddress; } set { blueToothMacAddress = value; OnPropertyChanged(); } }
        //private int blueToothType;
        //public int BlueToothType { get { return blueToothType; } set { blueToothType = value; OnPropertyChanged(); } }
        //private int blueToothBondState;
        //public int BlueToothBondState { get { return blueToothBondState; } set { blueToothBondState = value; OnPropertyChanged(); } }        
        //#endregion
        //#region Wifip2pdevice info        
        ////https://docs.microsoft.com/en-us/dotnet/api/android.net.wifi.p2p.wifip2pdevice?view=xamarin-android-sdk-9
        ////https://developer.android.com/reference/android/net/wifi/p2p/WifiP2pDevice
        //private string wiFiDeviceName;
        //public string WiFiDeviceName { get { return wiFiDeviceName; } set { wiFiDeviceName = value; OnPropertyChanged(); } }
        //private string wifiMacAddress;
        //public string WifiMacAddress { get { return wifiMacAddress; } set { wifiMacAddress = value; OnPropertyChanged(); } }        
        //private string wiFiPrimaryDeviceType;        
        //public string WiFiPrimaryDeviceType { get { return wiFiPrimaryDeviceType; } set { wiFiPrimaryDeviceType = value; OnPropertyChanged(); } }
        //private string wiFiSecondaryDeviceType;
        //public string WiFiSecondaryDeviceType { get { return wiFiSecondaryDeviceType; } set { wiFiSecondaryDeviceType = value; OnPropertyChanged(); } }
        //#endregion
        #endregion

        #region This is the standard model setup
        public string Name { get; set; }
        public string UniqueIdentifier { get; set; }

        #region Bluetooth info
        //https://docs.microsoft.com/en-us/dotnet/api/android.bluetooth.bluetoothdevice?view=xamarin-android-sdk-9
        //https://developer.android.com/reference/android/bluetooth/BluetoothDevice        
        public string BlueToothAlias { get; set; }
        public string BlueToothName { get; set; }
        public string BlueToothMacAddress { get; set; }
        public string BlueToothType { get; set; }
        public int BlueToothBondState { get; set; } //this seems weird        
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
