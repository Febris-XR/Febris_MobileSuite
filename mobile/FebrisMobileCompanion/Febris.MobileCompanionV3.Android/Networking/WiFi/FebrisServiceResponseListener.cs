// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Net.Wifi.P2p;
using Android.Net.Wifi.P2p.Nsd;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.SharedMobileLibrary.Enums;
using Febris.SharedMobileLibrary.FileSystem;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    public class FebrisServiceResponseListener : Java.Lang.Object, IServiceResponseListener
    {
        public void OnServiceAvailable([GeneratedEnum] ServiceType protocolType, byte[] responseData, WifiP2pDevice srcDevice)
        {
            Console.WriteLine("Source device: " + srcDevice);

            //throw new NotImplementedException();
        }
    }

    public class FebrisUpnpServiceResponseListener : Java.Lang.Object, IUpnpServiceResponseListener
    {
        private DataProtection _dataProtection = new DataProtection();
        /// <summary>
        /// Can use this method to initalize pairing becuase all needed information is here!
        /// </summary>
        /// <param name="uniqueServiceNames"></param>
        /// <param name="srcDevice"></param>
        public void OnUpnpServiceAvailable(IList<string> uniqueServiceNames, WifiP2pDevice srcDevice)
        {
            string savedName = string.Empty;
            try
            {
                savedName = _dataProtection.GetInput(ConfigType.FullServerName).Result;
                Console.WriteLine("Upnp saved service name: " + savedName);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error getting server name");
            }            

            Console.WriteLine("Upnp Source device: " + srcDevice);
            foreach (var i in uniqueServiceNames)
            {           
                
                if (i.Contains(WiFiStaticDetails.ServiceName))
                {
                    Console.WriteLine("Source Name: " + i);
                    WiFiService.wifiService.ConnectDevice(srcDevice);                    
                }


                //add upnp name to saved area

                //if (String.IsNullOrEmpty(savedName)&&i.Contains(savedName))
                //{
                //    Console.WriteLine("Source Name: " + i);
                //    WiFiService.wifiService.ConnectDevice(srcDevice);
                //}
                //else if (i.Contains(WiFiStaticDetails.ServiceName))
                //{
                //    Console.WriteLine("Source Name: " + i);
                //    WiFiService.wifiService.ConnectDevice(srcDevice);
                //
                //
                //}


            }
            
        }
        //Upnp Source device: 
        // deviceAddress: 96:be:46:de:e7:96
        // primary type: 000A0050F2040005
        // secondary type: null
        // wps: 392
        // grpcapab: 0
        // devcapab: 37
        // status: 3
        // wfdInfo: WFD enabled: trueWFD DeviceInfo: 0
        // WFD CtrlPort: 0
        // WFD MaxThroughput: 0
        // groupownerAddress: null
        // GOdeviceName: null
        // interfaceAddress: 
        // SConnectInfo : null
        // contactInfoHash : null
        // ssDevInfo : 0
        // iconIdx : 0
        // semSamsungDeviceType : 0
        // serviceData : null
        // fw_invite : 0
        //Source Name: uuid:e097cea8-699b-4df4-ae87-6c845ca84e2b::visible
        //Upnp Source device: 
        // deviceAddress: 96:be:46:de:e7:96
        // primary type: 000A0050F2040005
        // secondary type: null
        // wps: 392
        // grpcapab: 0
        // devcapab: 37
        // status: 3
        // wfdInfo: WFD enabled: trueWFD DeviceInfo: 0
        // WFD CtrlPort: 0
        // WFD MaxThroughput: 0
        // groupownerAddress: null
        // GOdeviceName: null
        // interfaceAddress: 
        // SConnectInfo : null
        // contactInfoHash : null
        // ssDevInfo : 0
        // iconIdx : 0
        // semSamsungDeviceType : 0
        // serviceData : null
        // fw_invite : 0
        //Source Name: uuid:e097cea8-699b-4df4-ae87-6c845ca84e2b::available
        //Upnp Source device: 
        // deviceAddress: 96:be:46:de:e7:96
        // primary type: 000A0050F2040005
        // secondary type: null
        // wps: 392
        // grpcapab: 0
        // devcapab: 37
        // status: 3
        // wfdInfo: WFD enabled: trueWFD DeviceInfo: 0
        // WFD CtrlPort: 0
        // WFD MaxThroughput: 0
        // groupownerAddress: null
        // GOdeviceName: null
        // interfaceAddress: 
        // SConnectInfo : null
        // contactInfoHash : null
        // ssDevInfo : 0
        // iconIdx : 0
        // semSamsungDeviceType : 0
        // serviceData : null
        // fw_invite : 0
        //Source Name: uuid:e097cea8-699b-4df4-ae87-6c845ca84e2b::65218
        //Upnp Source device: 
        // deviceAddress: 96:be:46:de:e7:96
        // primary type: 000A0050F2040005
        // secondary type: null
        // wps: 392
        // grpcapab: 0
        // devcapab: 37
        // status: 3
        // wfdInfo: WFD enabled: trueWFD DeviceInfo: 0
        // WFD CtrlPort: 0
        // WFD MaxThroughput: 0
        // groupownerAddress: null
        // GOdeviceName: null
        // interfaceAddress: 
        // SConnectInfo : null
        // contactInfoHash : null
        // ssDevInfo : 0
        // iconIdx : 0
        // semSamsungDeviceType : 0
        // serviceData : null
        // fw_invite : 0
        //Source Name: uuid:e097cea8-699b-4df4-ae87-6c845ca84e2b::FebrisMobileServer
    }
}