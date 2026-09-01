// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Febris.SharedMobileLibrary.P2pNetworkModels
//{
//    public class WiFiStaticDetails
//    {
//        public static IDataProtection _dataProtection { get; set; }// = DependencyService.Get<IDataProtection>();
//        public static WifiP2pManager manager { get; set; }
//        public static Channel channel { get; set; }

//        public static BroadcastReceiver receiver { get; set; }

//        public static List<WifiP2pDevice> CompanionDeviceList = new List<WifiP2pDevice>();

//        public static WifiP2pInfo HostInfo { get; set; }

//        public static TestMessage DeviceMessage = new TestMessage();

//        public static WifiP2pGroup WifiGroup { get; set; }


//        public static StatusUpdate StatusUpdate = new StatusUpdate();
//        public const int FebrisSocket = 65218;
//        public const string ServiceName = "FebrisMobileServer";
//        public const string ServiceType = "_presence._tcp";
//        //public static string ServiceUUID = ServiceUUIDSetting();
//        public static string ServiceUUID
//        {
//            get
//            {
//                return ServiceUUIDSetting();
//            }
//        }

//        public static string _serviceUUID = string.Empty;
//        internal static string ServiceUUIDSetting()
//        {
//            if (_dataProtection == null)
//            { return _serviceUUID; }
//            if (!String.IsNullOrEmpty(_serviceUUID))
//            { return _serviceUUID; }
//            //if (_serviceUUID != string.Empty && _serviceUUID != "") 
//            //{ return _serviceUUID; }

//            string output = string.Empty;
//            string localFileOutput = string.Empty;
//            //get uuid from a local file
//            try
//            {
//                localFileOutput = _dataProtection.GetInput(ConfigType.ServiceUUID);
//            }
//            catch { }
//            if (String.IsNullOrEmpty(localFileOutput))// == string.Empty || localFileOutput==null || localFileOutput == "")
//            {
//                output = Guid.NewGuid().ToString();
//                //save to local file
//                try
//                {
//                    _dataProtection.StoreInput(output, ConfigType.ServiceUUID);
//                }
//                catch
//                {

//                }
//            }
//            else { output = localFileOutput; }
//            _serviceUUID = output;
//            return output;
//        }

//        //public static WifiP2pServiceInfo serviceInfo;// { get; set; }

//        //this is for bonjor
//        //public static Dictionary<string, string> record = new Dictionary<string, string>();
//        //record.Add("listenport", FebrisSocket);
//        //record.Add("buddyname", "John Doe" + (int) (Math.random()* 1000));
//        //record.Add("available", "visible");
//        //public static WifiP2pDnsSdServiceInfo serviceInfo;// { get; set; }

//        //universal plug and play
//        public static List<string> record = new List<string>();
//        public static WifiP2pUpnpServiceInfo serviceInfo;

//        //public const string BROADCASTFILTER = "com.yourpackage.intent.action.IMAGEOPTIMIZER";

//        public static ServerSocket _serverSocket { get; set; }


//        internal const string PostStatementUrl = "poststatement/";
//        internal const string ModuleDownloaderUrl = "getmodule/";
//        internal const string PostVideoUrl = "postvideo/";
//        internal const string StatusUrl = "getstatus/";

//        internal const string StartModuleUrl = "launchmodule/";

//        internal const int BodyByteBuffer = 1024;
//        internal static readonly int ExpectedHeaderLength = 4;

//        internal static Dictionary<string, ServerSocketThread> ServerSocketDictionary = new Dictionary<string, ServerSocketThread>();
//    }
//}
