// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Content;
using Android.Net;
using Android.Net.Wifi.P2p;
using Febris.MobileServerV3.Droid.Networking;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using System;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.Droid.Communication.WiFi
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
        private static bool _referincingServerDevice = false;

        public WiFiP2pDirectBroadcastReceiver(WifiP2pManager p2pManager, WifiP2pManager.Channel channel, MainActivity activity)
        {
            _p2pManager = p2pManager;
            _channel = channel;
            _activity = activity;

            //Task.Run(() => WiFiService.RunLocalServiceListenerLoop());
            Task.Run(() => WiFiService.ClearListeners());

            //WiFiStaticDetails.record.Add(WiFiStaticDetails.ServiceName);
            //WiFiStaticDetails.serviceInfo = Android.Net.Wifi.P2p.Nsd.WifiP2pUpnpServiceInfo.NewInstance(WiFiStaticDetails.ServiceUUID, WiFiStaticDetails.ServiceType, WiFiStaticDetails.record);
            //WiFiStaticDetails.manager.AddLocalService(WiFiStaticDetails.channel, WiFiStaticDetails.serviceInfo, new FebrisActionListener(() => { }));
        }

        #endregion

        #region On Receive Peer connection     
        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;
            #region Wifi p2p state changed action
            if (action == WifiP2pManager.WifiP2pStateChangedAction)
            {
                // Determine if Wifi P2P mode is enabled or not, alert
                // the Activity.
                int state = intent.GetIntExtra(WifiP2pManager.ExtraWifiState, -1);
                Console.WriteLine("Wifi state: " + ((WifiP2pState)state).ToString());
                if ((WifiP2pState)state == WifiP2pState.Enabled)
                {
                    //WiFiService.wifiService.RefreshStateChanged(true);
                    EventHandlerHelper._eventHandler.RefreshStateChanged(true);

                }
                else
                {
                    //WiFiService.wifiService.RefreshStateChanged(false);
                    EventHandlerHelper._eventHandler.RefreshStateChanged(false);

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
                        //_p2pManager.RequestPeers(_channel, new PeersListener());
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
                // Connection state changed! We should probably do something about
                // that.
                if (_p2pManager != null)
                {
                    NetworkInfo networkInfo = (NetworkInfo)intent.GetParcelableExtra(WifiP2pManager.ExtraNetworkInfo);
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
                    Console.WriteLine("Network info on Connection Changed action: \n" + networkInfo);
                    if (networkInfo.IsConnectedOrConnecting)//(networkInfo.IsConnected)
                    {                                                             
                        _p2pManager.RequestConnectionInfo(_channel, new FebrisConnectionInfoListener());                                                
                    }
                    else
                    {
                        string networkState = networkInfo.GetState().ToString();
                        if (networkState == "DISCONNECTED" && _referincingServerDevice == true)
                        {
                            Task.Delay(5000);
                            //WiFiService.wifiService.DiscoveringPeers();                            
                            Console.WriteLine("the network state did disconnect");
                        }
                    }
                    _referincingServerDevice = false;
                }
            }
            #endregion
            #region Device changed action
            else if (action == WifiP2pManager.WifiP2pThisDeviceChangedAction)
            {
                try
                {
                    try
                    {
                        _p2pManager.RequestGroupInfo(_channel, new FebrisGroupInfoListener());                        
                    }
                    catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
                    //NetworkInfo networkInfo = (NetworkInfo)intent.GetParcelableExtra(WifiP2pManager.ExtraNetworkInfo);
                    //WifiP2pDevice _device = (WifiP2pDevice)intent.GetParcelableExtra(WifiP2pManager.ExtraWifiP2pDevice);
                    //WiFiService.wifiService.RefreshConnectSuccessChanged(networkInfo.IsConnected, _device.DeviceAddress);
                    //WiFiService.wifiService.RefreshConnectionSuccessChanged(_device.Status, _device.DeviceAddress);
                }
                catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
                //this seems to do nothing
                //NetworkInfo networkInfo = (NetworkInfo)intent.GetParcelableExtra(WifiP2pManager.ExtraNetworkInfo);
                //Console.WriteLine("Network info on device changed action: \n"+networkInfo);                
                _referincingServerDevice = true;
            }
            #endregion              
            #region Discovery changed action
            else if (action == WifiP2pManager.WifiP2pDiscoveryChangedAction)
            {
                Console.WriteLine("Wifi Discovery Changed action");
                if (_p2pManager != null)
                {                   
                    _p2pManager.RequestGroupInfo(_channel, new FebrisGroupInfoListener());                                       
                }
            }
            #endregion              
        }
        #endregion
    }
}