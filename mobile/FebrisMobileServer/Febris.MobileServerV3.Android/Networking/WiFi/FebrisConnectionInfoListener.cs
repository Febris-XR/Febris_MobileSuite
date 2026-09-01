// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Net.Wifi.P2p;
using Febris.MobileServerV3.Droid.Networking.WiFi;

namespace Febris.MobileServerV3.Droid.Communication.WiFi
{
    /// <summary>
    /// Good Example https://github.com/Brighthui/WifiP2P/blob/master/WifiP2P/WifiP2P.Android/WiFi/WiFiDirectBroadcastReceiver.cs
    /// *************This is honestly where all of the actual data transfer occures. This is where the server accepts the connection with the client*******************
    /// </summary>
    public class FebrisConnectionInfoListener : Java.Lang.Object, WifiP2pManager.IConnectionInfoListener
    {        
        /// <summary>
        /// This fires after the connection is made and listens for incoming information and connections.
        /// Can use this area to update the companion list with relevent information
        /// </summary>
        /// <param name="info"></param>
        public void OnConnectionInfoAvailable(WifiP2pInfo info)
        {
            if (info.GroupFormed)
            {
                WiFiStaticDetails.HostInfo = info;
                                
                //this may need to be changed. There has to be a better place... but maybe there is not
                //Task.Run(() => WiFiP2pServer.wiFiP2pServer.ServerSocketCreation());
                //System.Threading.Tasks.Task.Run(() => WiFiService.RunLocalServiceListenerLoop());

            }
        }
    }
    internal class FebrisConnectionTestInfoListener : Java.Lang.Object, WifiP2pManager.IConnectionInfoListener
    {
        public void OnConnectionInfoAvailable(WifiP2pInfo info)
        {
            WiFiStaticDetails.HostInfo = info;
        }
    }
}