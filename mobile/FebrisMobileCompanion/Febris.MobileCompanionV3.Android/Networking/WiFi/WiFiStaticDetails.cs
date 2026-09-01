// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Net;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using Java.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    public class WiFiStaticDetails
    {
        public static IDataProtection _dataProtection { get; set; }
        public static WifiP2pManager manager { get; set; }
        public static Channel channel { get; set; }
        public static BroadcastReceiver receiver { get; set; }

        public static IntentFilter _intentFilter { get; set; }


        public static List<WifiP2pDevice> CompanionDeviceList = new List<WifiP2pDevice>();

        public static WifiP2pInfo HostInfo { get; set; }

        public static TestMessage DeviceMessage = new TestMessage();

        public static WifiP2pGroup WifiGroup { get; set; }

        public static WifiP2pDevice HostDevice { get; set; }

        public static WifiP2pDevice WiFiThisDevice { get; set; }
        public static BluetoothAdapter BTThisDevice { get; set; }

        

        public static StatusUpdate StatusUpdate = new StatusUpdate();
        public static int FebrisSocket = 65218;
        public static string HostAddress = "";
        public static NetworkInfo networkInfo;// = new NetworkInfo();
        public static string ServiceName = "FebrisMobileServer";
        public static string ServiceType = "_febrisServer._tcp";
        //add service?

        //public static string DesiredServiceName = string.Empty;

        //public static bool RunServiceListener = true;
        //public static bool RunPeerDiscovery = true;

        internal const string PostStatementUrl = "poststatement/";
        internal const string ModuleDownloaderUrl = "getmodule/";
        internal const string PostVideoUrl = "postvideo/";
        internal const string StatusUrl = "getstatus/";

        internal const string StartModuleUrl = "launchmodule/";

        //public static ServerSocket _serverSocket { get; set; }
        private static WifiP2pGroup _wifiGroupInfo;

        public static WifiP2pGroup WifiGroupInfo
        {
            get { return _wifiGroupInfo; }
            set 
            { 
                _wifiGroupInfo = value;

                //LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerAddress = _groupInfo.Owner.DeviceAddress;
                //LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerDeviceName = _groupInfo.Owner.DeviceName;
                //LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.NetworkName = _groupInfo.NetworkName;
                //LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.GroupInterface = _groupInfo.Interface;

               
            }
        }

       

        internal const int ExpectedHeaderLength = 4;

        // REMOVED 2026-07-26: `public static bool StreamVideo`. It had no writer anywhere in
        // the repo and exactly one reader, the dead Utilities/VideoUtility.cs, so it could
        // never become true and the loop it guarded could never run. Deleting that file
        // orphaned it completely. The live flag is
        // Febris.MobileCompanionV3.Resources.LocalHardwareStaticDetails.StreamVideo, which
        // WiFiP2pRequestProcessing actually sets.
    }
}