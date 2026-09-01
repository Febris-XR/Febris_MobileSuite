// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using static Android.Net.Wifi.P2p.WifiP2pManager;


namespace Febris.MobileServerV3.Droid.Communication.WiFi
{
    public class FebrisChannelListener : Java.Lang.Object, IChannelListener
    {
        public void OnChannelDisconnected()
        {
            //Console.WriteLine("Channel has disconnected");
            WiFiStaticDetails.channel = WiFiStaticDetails.manager.Initialize((MainActivity)Forms.Context, Looper.MainLooper, new FebrisChannelListener());
        }
    }
}