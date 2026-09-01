// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    public class FebrisPeerListListener : Java.Lang.Object, IPeerListListener
    {
        #region wifi services
        IWiFiService wifi = DependencyService.Get<IWiFiService>();
        #endregion
        /// <summary>
        /// Can use this to filter out hardware
        /// </summary>
        /// <param name="peers"></param>
        public void OnPeersAvailable(WifiP2pDeviceList peers)
        {
            try
            {
                WiFiStaticDetails.CompanionDeviceList.Clear();
                Console.WriteLine("Peer count: "+peers.DeviceList.Count());
                foreach (var i in peers.DeviceList)
                {
                    ///Check to see if there is only one peer. If that one peer is not a group owner. leave the group.
                    //if (peers.DeviceList.Count() == 1)
                    //{
                    //    if (!i.IsGroupOwner)
                    //    {
                    //        WiFiStaticDetails.manager.RemoveGroup(WiFiStaticDetails.channel, new FebrisActionListener(() => { }));
                    //        //throw new NotImplementedException();
                    //        WiFiStaticDetails.channel = WiFiStaticDetails.manager.Initialize((MainActivity)Forms.Context, Looper.MainLooper, new FebrisChannelListener());
                    //    }
                    //}

                    //if there is something that matches add it. if not do not add it
                    //CompanionDeviceViewModel dev = 
                    List<string> lstDevice = new List<string>();
                    foreach (var p in WiFiStaticDetails.CompanionDeviceList)
                    {
                        if (p.DeviceName == i.DeviceName)
                            continue;
                    }                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}