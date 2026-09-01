// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Net.Wifi.P2p;
using Android.Views;
using Febris.MobileServerV3.Droid.Networking;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using Febris.MobileServerV3.P2pCommunication;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using static Android.Net.Wifi.P2p.WifiP2pManager;

namespace Febris.MobileServerV3.Droid.Communication.WiFi
{
    public class FebrisPeerListListener : Java.Lang.Object, IPeerListListener
    {
        #region wifi services
        IWiFiService wifi = DependencyService.Get<IWiFiService>();
        #endregion
        /// <summary>
        /// Need to add the removal of disconnected hardware********************
        /// <para>
        /// Bug fixes applied here:
        /// </para>
        /// <list type="number">
        ///   <item><b>Thread safety:</b> the method runs on the Android
        ///     WiFi P2P broadcast receiver thread; <c>CompanionDeviceList</c>
        ///     is also read by the UI thread + other consumers. All mutation
        ///     now goes through a lock on <c>WiFiStaticDetails.CompanionDeviceListLock</c>.</item>
        ///   <item><b>Missing Add:</b> the previous body called
        ///     <c>CompanionDeviceList.Clear()</c> and then iterated the
        ///     (just-emptied) list checking for duplicates, but never
        ///     actually added any devices back. The list was always empty
        ///     after this method ran. Restored the dedup-and-add intent
        ///     the comment ("if there is something that matches add it.
        ///     if not do not add it") implied.</item>
        ///   <item><b>Event firing:</b> moved out of the per-device foreach
        ///     to a single call after population. The previous per-device
        ///     firing was always with an empty list anyway (because of bug
        ///     #2 above), so this is the intended single-fire-after-population
        ///     behavior.</item>
        /// </list>
        /// </summary>
        /// <param name="peers"></param>
        public void OnPeersAvailable(WifiP2pDeviceList peers)
        {
            List<string> lstDevice = new List<string>();

            lock (WiFiStaticDetails.CompanionDeviceListLock)
            {
                WiFiStaticDetails.CompanionDeviceList.Clear();
                foreach (var i in peers.DeviceList)
                {
                    // Dedup by DeviceName -- if already in the list,
                    // skip (the list was just cleared so the first pass
                    // can never match, but keep the guard so future
                    // callers that don't Clear() first stay correct).
                    bool alreadyPresent = false;
                    foreach (var p in WiFiStaticDetails.CompanionDeviceList)
                    {
                        if (p.DeviceName == i.DeviceName)
                        {
                            alreadyPresent = true;
                            break;
                        }
                    }
                    if (!alreadyPresent)
                    {
                        WiFiStaticDetails.CompanionDeviceList.Add(i);
                    }
                }

                // Build the name list under the lock too, so the snapshot
                // we hand to RefreshPeersChanged is consistent.
                foreach (var p in WiFiStaticDetails.CompanionDeviceList)
                {
                    lstDevice.Add(p.DeviceName);
                }
            }

            // Fire the event OUTSIDE the lock so handlers can do whatever
            // they want without risking deadlock.
            //WiFiService.wifiService.RefreshPeersChanged(lstDevice);
            EventHandlerHelper._eventHandler.RefreshPeersChanged(lstDevice);
        }

    }
}