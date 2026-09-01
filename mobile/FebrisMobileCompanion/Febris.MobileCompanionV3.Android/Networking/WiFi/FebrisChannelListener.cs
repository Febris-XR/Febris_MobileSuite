// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    public class FebrisChannelListener : Java.Lang.Object, IChannelListener
    {
        /// <summary>
        /// The framework dropped our P2P channel. This is a LOUD event that used to be completely
        /// silent, and it is a prime suspect for issue 14 fault C.
        ///
        /// <para>Two things happen here that matter and were previously unobservable. It calls
        /// <c>RemoveGroup</c>, which tears the group down, and it REPLACES
        /// <c>WiFiStaticDetails.channel</c> with a freshly initialised one. A new channel carries
        /// NONE of the service requests registered on the old one, so anything that registered
        /// once at startup is silently gone afterwards. The service listener loop re-adds its
        /// request every iteration and should recover, but "should" is exactly the word this
        /// document keeps having to retract, so the transition is now logged on both sides.</para>
        /// </summary>
        public void OnChannelDisconnected()
        {
            Console.WriteLine("P2P CHANNEL DISCONNECTED: removing group and re-initialising the "
                + "channel. Every service request registered on the old channel is now gone.");
            try
            {
                WiFiStaticDetails.manager.RemoveGroup(WiFiStaticDetails.channel, new FebrisActionListener(() =>{ }, "RemoveGroup (channel disconnected)"));
                WiFiStaticDetails.channel = WiFiStaticDetails.manager.Initialize((MainActivity)Forms.Context, Looper.MainLooper, new FebrisChannelListener());
                Console.WriteLine("P2P channel re-initialised: "
                    + (WiFiStaticDetails.channel == null ? "FAILED, channel is null" : "ok"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Channel Listener Error: "+ex.Message);
            }
        }
    }
}