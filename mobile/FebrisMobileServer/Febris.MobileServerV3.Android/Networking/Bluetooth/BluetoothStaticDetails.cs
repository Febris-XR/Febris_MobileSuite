// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Droid.Networking.Bluetooth
{
    public class BluetoothStaticDetails
    {
        public static int SELECT_DEVICE_REQUEST_CODE = 0;


        //public const string _febris_UUID = "00001106-0000-1000-8000-00805F9B34FB";//this is suppose to be file transfer service
        //public const string _febris_UUID = "00001104-0000-1000-8000-00805F9B34FB";
        public const string _febris_UUID = "00001105-0000-1000-8000-00805f9b34fb";
        //public const string _febris_UUID = "00001105-0000-1000-8000-00805f9b34fb";
        //public const string _febris_UUID = "00001105-0000-1000-8000-00805f9b34fb";
        //public const string _febris_UUID = "00001105-0000-1000-8000-00805f9b34fb";
        //public const string _febris_UUID = "00001101-0000-1000-8000-00805F9B34FB";
        //public const string _febris_UUID = "0000110E-0000-1000-8000-00805F9B34FB";
        //public const string _febris_UUID = "0000110A-0000-1000-8000-00805F9B34FB";
        //public const string _febris_UUID = "0000110B-0000-1000-8000-00805F9B34FB";
        //public const string _febris_UUID = "00001101-0000-1000-8000-00805f9b34fb";//uuid for spp

        //all that show up
        //0000110a-0000-1000-8000-00805f9b34fb //Advanced Audio Distribution Profile (A2DP)
        //00001105-0000-1000-8000-00805f9b34fb //OBEXOBJECTPUSH_UUID
        //00001115-0000-1000-8000-00805f9b34fb //Personal Area Networking User (PANU) Profile
        //00001116-0000-1000-8000-00805f9b34fb //Network Access Point (NAP) Profile
        //0000112d-0000-1000-8000-00805f9b34fb //only on moto --heart rate shit
        //0000110e-0000-1000-8000-00805f9b34fb //Audio/Video Remote Control Profile (AVRCP) Allows sending command frames to a target.
        //0000112f-0000-1000-8000-00805f9b34fb //phonebook access
        //00001112-0000-1000-8000-00805f9b34fb //Headset Profile (HSP) Acts as an audio gateway alone.
        //0000111f-0000-1000-8000-00805f9b34fb //Hands-Free Profile (HFP) Acts as an audio gateway alone
        //00001132-0000-1000-8000-00805f9b34fb
        //00000000-0000-1000-8000-00805f9b34fb       



        public const string _serviceName = "Febris_Controller_Connection";

        public const int timeout = 5000;

        internal const string _CompanionFileName = "FebrisTestPackage.zip";
        //FebrisTestPackage.zip

        internal static BluetoothManager manager { get; set; }
        //internal static Callback callback { get; set; }
        //internal static Channel channel { get; set; }
        internal static BluetoothAdapter _bluetoothAdapter { get; set; }
        public static BluetoothBroadcastReceiver receiver { get; internal set; }

        public static BluetoothService service { get; set; }

        public static BluetoothProcessing processing { get; internal set; }
    }
}