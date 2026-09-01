// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.AdbLibrary.AdbLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Droid.Networking.USB
{
    internal class USBStaticDetails
    {
        public static UsbManager _usbManager { get; set; }
        public static int USB_DEVICE_ATTACHED = 782;
        //internal static CustomConnection _adbConnection;
        internal static AdbConnection _adbConnection;
        internal static AdbCrypto _AdbCrypto;
        public const string ACTION_USB_PERMISSION = "com.febris.USB_PERMISSION";
       // public const string ACTION_USB_PERMISSION =  + ".USB_PERMISSION";
        public const string INSTALL_COMPLETE = "com.febris.INSTALL_COMPLETE";
        public const string INSTALL_FAILURE = "com.febris.INSTALL_FAILURE";
        

        public static UsbInterface _interface { get; set; }
        //public static USBService _service { get; set; }
        public static USBBroadcastReceiver _receiver { get; internal set; }        
        public static UsbDevice _device { get; internal set; }
        public static UsbDeviceConnection _connection { get; internal set; }
        public static UsbEndpoint _endpoint_IN { get; internal set; }
        public static UsbEndpoint _endpoint_OUT { get; internal set; }
        public static UsbEndpoint _adb_endpoint { get; internal set; }
        public static AdbStream _Host_To_Device_Stream { get; internal set; }
        public static AdbStream _Device_To_Host_Stream { get; internal set; }

        //public static UsbChannel _channel { get; internal set; }

        public static void CleanConnection()
        {
            _interface = default;
            //_service = default;
            _receiver = default;
            _device = default;
            _connection = default;
            _endpoint_IN = default;
            _endpoint_OUT = default;
            _adb_endpoint = default;
            _Host_To_Device_Stream = default;
            _Device_To_Host_Stream = default;
        }
    }

    internal class AdbRequest
    {
        public AdbRequest()
        {
            Message = default;
            Data = default;                
        }

        public string Message { get; set; }
        public string Data { get; set; }
    }
}