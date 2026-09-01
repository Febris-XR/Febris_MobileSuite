// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Content;
using Android.Net;
using Android.Net.Wifi.P2p;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.MobileCompanionV3.Resources;
using System;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    /// <summary>
    /// https://github.com/Brighthui/WifiP2P/tree/master/WifiP2P
    /// </summary>
    public class WiFiP2pDirectBroadcastReceiver : BroadcastReceiver
    {
        #region variables and constructors     
        private readonly WifiP2pManager _p2pManager;
        private readonly WifiP2pManager.Channel _channel;
        private readonly MainActivity _activity;
        //WiFiService wiFi = new WiFiService();
        private bool _referincingServerDevice = false;

        public WiFiP2pDirectBroadcastReceiver(WifiP2pManager p2pManager, WifiP2pManager.Channel channel, MainActivity activity)
        {
            _p2pManager = p2pManager;
            _channel = channel;
            _activity = activity;
        }

        #endregion

        #region On Receive Peer connection  
        /// <summary>
        /// Group owner change does not register as anything
        /// </summary>
        /// <param name="context"></param>
        /// <param name="intent"></param>
        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;
            #region Wifi p2p state changed action
            if (action == WifiP2pManager.WifiP2pStateChangedAction)
            {
                // Determine if Wifi P2P mode is enabled or not, alert
                // the Activity.                
                int state = intent.GetIntExtra(WifiP2pManager.ExtraWifiState, -1);
                if ((WifiP2pState)state == WifiP2pState.Enabled)
                {
                    //WiFiService.wifiService.RefreshStateChanged(true);
                    EventHandlerHelper._eventHandler.RefreshStateChanged(true);
                    Console.WriteLine("Wifi is active");
                }
                else
                {
                    //WiFiService.wifiService.RefreshStateChanged(false);
                    EventHandlerHelper._eventHandler.RefreshStateChanged(false);
                    Console.WriteLine("Wifi is not active");
                }
                
            }
            #endregion
            #region Peers changed action
            else if (action == WifiP2pManager.WifiP2pPeersChangedAction)
            {
                // request available peers from the wifi p2p manager. This is an
                // asynchronous call and the calling activity is notified with a
                // callback on PeerListListener.onPeersAvailable()
                if (_p2pManager != null)
                {
                    try
                    {
                        _p2pManager.RequestPeers(_channel, new FebrisPeerListListener());
                        _p2pManager.RequestGroupInfo(_channel, new FebrisGroupInfoListener());
                    }
                    catch (Exception ex)
                    {
                        string ss = ex.Message;
                    }
                }
            }
            #endregion            
            #region Connection Changed action
            else if (action == WifiP2pManager.WifiP2pConnectionChangedAction)
            {
                try
                {
                    if (_p2pManager != null)
                    {
                        NetworkInfo networkInfo = (NetworkInfo)intent.GetParcelableExtra(WifiP2pManager.ExtraNetworkInfo);
                        WiFiStaticDetails.networkInfo = networkInfo;

                        #region deleted/moved
                        //try {
                        //    //_p2pManager.RequestConnectionInfo(_channel, new FebrisConnectionTestInfoListener());
                        //    _p2pManager.RequestConnectionInfo(_channel, new FebrisConnectionInfoListener());
                        //    WifiP2pGroup group = (WifiP2pGroup)intent.GetParcelableExtra(WifiP2pManager.ExtraWifiP2pGroup);
                        //    WiFiStaticDetails.WifiGroup = group;
                        //    WiFiService.wifiService.GroupConnectionStatus(group);
                        //} 
                        //catch (Exception ex) { 
                        //    Console.WriteLine(ex.StackTrace); 
                        //}




                        //networkInfo.
                        //needs a group to get group info
                        //var groupinfo = _p2pManager.RequestGroupInfo(_channel, new FebrisGroupInfoListener());

                        //GroupInfo groupInfo
                        #endregion
                        Console.WriteLine("Network info on Connection Changed action: \n" + networkInfo);
                        if (networkInfo.IsConnected)
                        {
                            _p2pManager.RequestConnectionInfo(_channel, new FebrisConnectionInfoListener());
                        }
                        else
                        {
                            string networkState = networkInfo.GetState().ToString();
                            if (networkState == NetworkInfo.State.Disconnected.ToString() && _referincingServerDevice == true)
                                //if (networkState == "DISCONNECTED" && _referincingServerDevice == true)
                            {
                                Task.Delay(5000);
                                EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                                //WiFiService.wifiService.DiscoveringPeers();                            
                                Console.WriteLine("the network state did disconnect");
                            }
                        }
                        _referincingServerDevice = false;
                    }
                    #region Deleted/moved

                    //try
                    //{
                    //    WiFiStaticDetails.networkInfo = (NetworkInfo)intent.GetParcelableExtra(WifiP2pManager.ExtraNetworkInfo);
                    //    Console.WriteLine("Connection Changed Action NetworkInfo: " + WiFiStaticDetails.networkInfo);
                    //}
                    //catch (Exception ex)
                    //{
                    //    LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Gather Network Data: " + ex.Message;
                    //    Console.WriteLine(ex.Message);
                    //    Console.WriteLine(ex.StackTrace);
                    //}
                    //try
                    //{
                    //    WiFiStaticDetails.GroupInfo = (WifiP2pGroup)intent.GetParcelableExtra(WifiP2pManager.ExtraWifiP2pGroup);
                    //    Console.WriteLine("Connection Changed Action GroupInfo: " + WiFiStaticDetails.GroupInfo);
                    //    //WiFiService.wifiService.GroupConnectionStatus(WiFiStaticDetails.GroupInfo);
                    //}
                    //catch (Exception ex)
                    //{
                    //    LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Gathering Group Data: " + ex.Message;
                    //    Console.WriteLine(ex.Message);
                    //    Console.WriteLine(ex.StackTrace);
                    //}
                    // Connection state changed! We should probably do something about
                    // that.
                    //WiFiService.wifiService.RefreshConnectSuccessChanged(WiFiStaticDetails.networkInfo.IsConnected, WiFiStaticDetails.GroupInfo.Owner.DeviceAddress, string.Empty);                    
                    //WiFiService.wifiService.RefreshConnectSuccessChanged(WiFiStaticDetails.GroupInfo);
                    //if (_p2pManager != null)
                    //{
                    //    if (WiFiStaticDetails.networkInfo.IsConnected)//&& WiFiStaticDetails.HostInfo.GroupFormed)
                    //    {
                    //        _p2pManager.RequestConnectionInfo(_channel, new FebrisConnectionInfoListener());
                    //    }
                    //    else //if (!WiFiStaticDetails.networkInfo.IsConnected)
                    //    {
                    //        //if(WiFiStaticDetails.networkInfo.GetState()==NetworkState.)                      
                    //        string networkState = WiFiStaticDetails.networkInfo.GetState().ToString();
                    //        if (networkState == NetworkInfo.State.Disconnected.ToString())//"DISCONNECTED")
                    //        {
                    //            Task.Delay(5000);
                    //            Console.WriteLine("the network state did disconnect");
                    //            EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                    //            //WiFiService.RestartConnectionDiscovery();
                    //        }
                    //    }
                    //Pass back to shared body
                    //EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(WiFiStaticDetails.GroupInfo, WiFiStaticDetails.networkInfo);

                    //_referincingServerDevice = false;
                    //}
                    #endregion


                }
                catch (Exception ex)
                {
                    LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error In Connection Changed Action: " + ex.Message;
                    Console.WriteLine(ex.StackTrace);
                    EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                }

            }
            #endregion
            #region Device changed action
            else if (action == WifiP2pManager.WifiP2pThisDeviceChangedAction)
            {
                //int m = 0;
                //update device
                _referincingServerDevice = true;
                try
                {
                    //WiFiStaticDetails.WiFiThisDevice = (WifiP2pDevice)intent.GetParcelableExtra(WifiP2pManager.ExtraWifiP2pDevice);
                    //Console.WriteLine("This device info: " + WiFiStaticDetails.WiFiThisDevice);
                    try
                    {
                        _p2pManager.RequestGroupInfo(_channel, new FebrisGroupInfoListener());
                    }
                    catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
                    //var mac = intent.GetParcelableExtra(WifiP2pManager.ExtraWifiP2pDevice);
                    //Console.WriteLine("This device info: " + mac);
                }
                catch (Exception ex)
                {
                    LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Gathering Device Info: " + ex.Message;
                }
                //if (_p2pManager != null)
                //{
                //    //try {                        
                //    //    _p2pManager.RequestConnectionInfo(_channel, new FebrisConnectionInfoListener()); 
                //    //} catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
                //    WiFiStaticDetails.networkInfo = (NetworkInfo)intent.GetParcelableExtra(WifiP2pManager.ExtraNetworkInfo);
                //    EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(WiFiStaticDetails.GroupInfo, WiFiStaticDetails.networkInfo);
                //}

            }
            #endregion            
            #region Discovery changed action
            else if (action == WifiP2pManager.WifiP2pDiscoveryChangedAction)
            {
                Console.WriteLine("Wifi Discovery Changed action");
                try
                {
                    if (_p2pManager != null)
                    {
                        _p2pManager.RequestGroupInfo(_channel, new FebrisGroupInfoListener());
                        //_p2pManager.RequestConnectionInfo(_channel, new FebrisConnectionInfoListener());
                    }
                }
                catch (Exception ex)
                {
                    LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Changing Discovery State: " + ex.Message;
                    Console.WriteLine(ex.StackTrace);
                }
            }
            #endregion

        }
        #endregion
    }
}