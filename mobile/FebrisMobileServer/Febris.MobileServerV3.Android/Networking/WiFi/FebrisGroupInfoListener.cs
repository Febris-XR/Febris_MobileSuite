// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Net.Wifi.P2p;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using System;

namespace Febris.MobileServerV3.Droid.Communication.WiFi
{
    internal class FebrisGroupInfoListener : Java.Lang.Object, WifiP2pManager.IGroupInfoListener
    {
        public void OnGroupInfoAvailable(WifiP2pGroup group)
        {
            Console.WriteLine("group info: " + group);
            WiFiStaticDetails.WifiGroup = group;
            WiFiService.wifiService.GroupConnectionStatus(group);
            //WiFiP2pGroupClass._wiFiP2pGroupClass.GroupConnectionStatus(group);
        }
    }
}