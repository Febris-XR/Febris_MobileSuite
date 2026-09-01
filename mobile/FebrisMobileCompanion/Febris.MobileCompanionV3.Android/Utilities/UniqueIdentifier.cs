// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using static Android.Provider.Settings;

[assembly: Xamarin.Forms.Dependency(typeof(Febris.MobileCompanionV3.Droid.Utilities.UniqueIdentifier))]
namespace Febris.MobileCompanionV3.Droid.Utilities
{
    public class UniqueIdentifier : IDevice
    {
        //string IDevice.GetIdentifier();

        public string GetIdentifier()
        {
            var context = Android.App.Application.Context;
            string id = Android.Provider.Settings.Secure.GetString(context.ContentResolver, Secure.AndroidId);
            return id;
        }

        public string GetWiFiMac()
        {
            var ni = NetworkInterface.GetAllNetworkInterfaces()
               .OrderBy(intf => intf.NetworkInterfaceType)
               .FirstOrDefault(intf => intf.OperationalStatus == OperationalStatus.Up
               && (intf.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
               || intf.NetworkInterfaceType == NetworkInterfaceType.Ethernet));

            var hw = ni.GetPhysicalAddress();
            return string.Join(":", (from ma in hw.GetAddressBytes() select ma.ToString("X2")).ToArray());            
        }

        //public string GetBluetoothMac()
        //{
        //    var ni = NetworkInterface.GetAllNetworkInterfaces()
        //       .OrderBy(intf => intf.NetworkInterfaceType)
        //       .FirstOrDefault(intf => intf.OperationalStatus == OperationalStatus.Up
        //       && (intf.NetworkInterfaceType == NetworkInterfaceType..Wireless80211
        //       || intf.NetworkInterfaceType == NetworkInterfaceType.Ethernet));

        //    var hw = ni.GetPhysicalAddress();
        //    return string.Join(":", (from ma in hw.GetAddressBytes() select ma.ToString("X2")).ToArray());
        //}
        //{
        //    return Settings.Secure.GetString(Forms.Context.ContentResolver, Settings.Secure.AndroidId);
        //}
    }
}