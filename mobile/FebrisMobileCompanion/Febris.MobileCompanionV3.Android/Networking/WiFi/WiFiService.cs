// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Net.Wifi;
using Android.Net.Wifi.P2p;
using Android.Net.Wifi.P2p.Nsd;
using Febris.MobileCompanionV3.Droid.Networking.WiFi;
using Febris.MobileCompanionV3.Droid.Utilities;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using Java.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[assembly: Xamarin.Forms.Dependency(typeof(WiFiService))]
namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    /// <summary>
    /// Listening location for the WiFiP2p service that is broadcast. 
    /// This area needs to be refactored but do not currently have time. 
    /// </summary>
    #region Wifi Service
    public class WiFiService : IWiFiService
    {
        // MP2P-5: removed dead `tempCompanionDeviceWifiMacAddress` static -- declared
        // here and in two sibling files (Server WiFiService + WiFiP2pGroupClass) but
        // never read or written anywhere in either project.
        public static WiFiService wifiService;
        public static FileManager _fileManager = new FileManager();
        public static bool _InitalizationComplete = false;

        public WiFiService()
        {
            wifiService = this;
        }



        #region Loops container


        #region Peer Discovery Loop
        public void DiscoveringPeers()
        {
            Task.Run(() => DiscoveryLoop());
        }
        /// <summary>
        /// adding a loop for peer discovery so that it wont time out
        /// </summary>
        public async Task DiscoveryLoop()
        {
            while (true)
            {
                try
                {
                    if (LocalHardwareStaticDetails.RunPeerDiscovery)
                    {
                        WiFiStaticDetails.manager?.DiscoverPeers(WiFiStaticDetails.channel, new FebrisActionListener(() => { }, "DiscoverPeers"));
                    }
                    await Task.Delay(LocalHardwareStaticDetails.PeerDiscoveryListenerLoopFrequency);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in Service Listener Loop: " + ex.Message);
                }
            }
        }

        #endregion

        #region service discovery Loop
        public void ListenForServices()
        {
            Task.Run(() => ListenerLoop());
        }


        private async Task ListenerLoop()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        if (LocalHardwareStaticDetails.RunServiceListener)
                        {
                            // A NULL manager or channel silently disables this entire loop, because
                            // every call below is null-conditional. That is one of the two ways a
                            // stranded Companion looks identical to a healthy one in the log, so it
                            // is stated rather than inferred. The channel can be invalidated when
                            // the peer that owned the group disappears, which is exactly the
                            // fault C scenario. See issue 14.
                            if (WiFiStaticDetails.manager == null || WiFiStaticDetails.channel == null)
                            {
                                Console.WriteLine("service listener: P2P manager or channel is NULL, "
                                    + "discovery cannot run (manager=" + (WiFiStaticDetails.manager == null ? "null" : "ok")
                                    + " channel=" + (WiFiStaticDetails.channel == null ? "null" : "ok") + ")");
                            }

                            // LABELLED. These three fail asynchronously through IActionListener and
                            // nowhere else, and until 2026-07-29 that callback had an empty body, so
                            // a permanently refused request was invisible. The reason code alone is
                            // not enough because all three run in one iteration.
                            WiFiStaticDetails.manager?.RemoveServiceRequest(WiFiStaticDetails.channel, WifiP2pServiceRequest.NewInstance(ServiceType.All), new FebrisActionListener(() => { }, "RemoveServiceRequest"));
                            WiFiStaticDetails.manager?.AddServiceRequest(WiFiStaticDetails.channel, WifiP2pServiceRequest.NewInstance(ServiceType.All), new FebrisActionListener(() => { }, "AddServiceRequest"));
                            WiFiStaticDetails.manager?.DiscoverServices(WiFiStaticDetails.channel, new FebrisActionListener(() => { }, "DiscoverServices"));
                            WiFiStaticDetails.manager?.SetUpnpServiceResponseListener(WiFiStaticDetails.channel, new FebrisUpnpServiceResponseListener());
                            //WiFiStaticDetails.manager?.SetServiceResponseListener(WiFiStaticDetails.channel, new FebrisServiceResponseListener());
                        }
                        await Task.Delay(LocalHardwareStaticDetails.ServiceListenerLoopFrequency);
                    }
                    catch (Exception ex)
                    {
                        // LOG AND KEEP GOING. This used to `break`, which ended service discovery
                        // for the LIFE OF THE PROCESS on the first error, and errors are routine
                        // here exactly when the Server dies mid-group. The peer-discovery loop
                        // above logs and continues, which is why a stranded Companion kept
                        // printing "Peer count" every thirty seconds while never receiving another
                        // UPnP service response, and the connect is triggered ONLY by a service
                        // response. That asymmetry was issue 14 fault C, the reason a Server
                        // restart stranded every Companion until a manual reset.
                        //
                        // Same defect shape as the upload loops: a permanent exit for a condition
                        // that is temporary by nature.
                        Console.WriteLine("Error in Service Listener Loop, continuing: " + ex.Message);
                        await Task.Delay(LocalHardwareStaticDetails.ServiceListenerLoopFrequency);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Service Listener Loop: " + ex.Message);
                ListenForServices();
            }
        }
        #endregion

        #endregion

        #region Library/Body interaction
        //step 1


        //public void RefreshStateChanged(bool bEnable)
        //{
        //    CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
        //    args.WiFiEnable = bEnable;
        //    //WifiP2pStateChangedAction(this, args);
        //}
        //step 2



        //step 3


        #endregion


        /// <summary>
        /// May want to look further into this area
        /// </summary>
        /// <param name="Device"></param>
        #region Connection Handling   
        public void ConnectDevice(WifiP2pDevice Device)
        {
            Console.WriteLine("*************Attempting to Connect to Host Server in WiFiService*************");
            try
            {
                WiFiStaticDetails.HostDevice = Device;

                // ALREADY IN THE GROUP: build the socket, do not try to join again.
                //
                // When the SERVER APPLICATION restarts, its WiFi Direct group survives and this
                // device never leaves it, so the peer comes back reporting status Connected. Asking
                // Android to Connect() to a peer it considers already connected does not invoke the
                // action listener at all: no success, no failure, nothing. ConnectSuccess therefore
                // never runs, RequestConnectionInfo is never called, and the socket is never
                // rebuilt, which strands every queued statement indefinitely.
                //
                // Measured on device: four "Attempting to Connect" over twenty-five minutes, the
                // peer visible at status 0 throughout, and not one Connect Success between them.
                //
                // The group already exists here. The only thing missing is the TCP socket, and
                // ConnectSuccess is exactly the path that asks for what is needed to build one.
                // See docs/MOBILE_KNOWN_ISSUES.md issue 14, fault C.
                // NOTE the Connect below still runs. An earlier version returned here instead, and
                // that was the wrong trade: the benefit is unproven, because this branch has never
                // actually been reached on device, while the risk is real. A stale peer cache can
                // report Connected for a group that is gone, and skipping Connect in that case
                // would remove the one call that could have recovered it. Asking for connection
                // info is harmless and idempotent, so do both rather than choosing.
                if (Device.Status == WifiP2pDeviceState.Connected)
                {
                    Console.WriteLine("peer already reports connected, asking for connection info as well as reconnecting");
                    ConnectSuccess();
                }

                WifiP2pConfig config = new WifiP2pConfig();
                config.GroupOwnerIntent = 0;// config.GroupOwnerIntentMin;
                config.DeviceAddress = Device.DeviceAddress;
                config.Wps.Setup = WpsInfo.Pbc;

                //if (string.IsNullOrEmpty(LocalHardwareStaticDetails.StaticMainVM?.ConfigVM?.P2pGroup?.OwnerAddress ?? string.Empty)
                //    || LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerAddress == Device.DeviceAddress
                //    )
                //{
                // THE MOST IMPORTANT LABEL IN THE TIER. This is the actual connect attempt, and
                // its failure reason was discarded. A stranded Companion that IS calling Connect
                // and being refused looks exactly like one that never called Connect at all, and
                // telling those apart is the open half of issue 14 fault C.
                WiFiStaticDetails.manager.Connect(
                WiFiStaticDetails.channel,
                config,
                new FebrisActionListener(ConnectSuccess, "Connect to " + (Device?.DeviceAddress ?? "unknown"))
                );
                
                //}                
            }
            catch (Exception ex)
            {
                Console.WriteLine("WiFiService ConnectDevice Error: " + ex.Message);
                Console.WriteLine("WiFiService ConnectDevice Error: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// This is called if ConnectDevice is successful.
        ///
        /// <para><b>Asks for the connection details directly rather than waiting to be told.</b>
        /// Everything that builds the socket hangs off <c>RequestConnectionInfo</c>, and the only
        /// other caller runs on a <c>WIFI_P2P_CONNECTION_CHANGED</c> broadcast with
        /// <c>IsConnected</c> true. That broadcast reports a TRANSITION. If this device never
        /// actually left the group there is no transition to report, the broadcast never arrives,
        /// and nothing ever rebuilds the socket.</para>
        ///
        /// <para>That is not hypothetical, it is what happens when the SERVER APPLICATION restarts
        /// while its group persists. Discovery finds the peer again, connect is called, this method
        /// runs with success, and then the Companion waits forever for a broadcast that will not
        /// come. Observed on device as repeated "Connect Success" with no "Network info on
        /// Connection Changed action" between them, no socket, and every queued statement stranded
        /// indefinitely. See docs/MOBILE_KNOWN_ISSUES.md issue 14.</para>
        ///
        /// <para>Safe when the broadcast DOES also fire, because both paths converge on
        /// <see cref="FebrisConnectionInfoListener"/> and the existing receiver can already deliver
        /// it more than once for a single connection.</para>
        ///
        /// ToDo:
        /// --Add paired server to the list of stored data. This will make is easier to refind the same device.
        /// </summary>
        static void ConnectSuccess()
        {
            //*************************Moved this from FebrisGroupInfoListener because it would trigger either too soon or only intermittenantly send back host info***************
            //EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(WiFiStaticDetails.WifiGroupInfo, WiFiStaticDetails.networkInfo);
            //WiFiService.wifiService.GroupConnectionStatus(WiFiStaticDetails.GroupInfo);
            Console.WriteLine("Connect Success");

            try
            {
                if (WiFiStaticDetails.manager != null && WiFiStaticDetails.channel != null)
                {
                    Console.WriteLine("connect success: requesting connection info directly");
                    WiFiStaticDetails.manager.RequestConnectionInfo(
                        WiFiStaticDetails.channel, new FebrisConnectionInfoListener());
                }
                else
                {
                    Console.WriteLine("connect success: no P2P manager or channel, cannot request connection info");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("connect success: requesting connection info failed: " + ex.Message);
            }
        }

        static void ConnectionClosed()
        {
            Console.WriteLine("Connect Dropped out");
        }

        public void RefreshConnectSuccessChanged(bool bEnable)
        {

        }

        #endregion

        #region Helpers - unused
        public Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        internal void GroupConnectionStatus(WifiP2pGroup group)
        {
            //EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(group);
            //if (group == null || group.Owner == null)
            //{
            //    //    foreach (var j in LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList)
            //    {
            try
            {
                //EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, );
                EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(group);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            //}
            return;
            //}
            //else
            //{
            //    WifiP2pDevice ownerDevice = group.Owner;
            //    var status = ownerDevice.Status;
            //    bool connected = false;
            //    if (status == WifiP2pDeviceState.Connected)
            //    {
            //        connected = true;

            //    }
            //    //EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, group.Owner.DeviceAddress, string.Empty);
            //    //EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(group);
            //}







            //ICollection<WifiP2pDevice> clientList;// = new ICollection<WifiP2pDevice>();
            //ICollection<WifiP2pDevice> clientList = new List<WifiP2pDevice>();//.//group.ClientList;
            //try
            //{
            //    clientList = group.ClientList;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.StackTrace);
            //    return;
            //}

            //check over clienList
            //foreach (var j in LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList)
            //{
            //    try
            //    {
            //        if (clientList.Where(x => x.DeviceAddress == group.Owner.DeviceAddress).Any())
            //        {
            //            try
            //            {
            //                WifiP2pDevice temp = clientList.Where(y => y.DeviceAddress == j.CompanionDevice.WifiMacAddress).Single();
            //                bool connected = false;
            //                if (temp.Status == WifiP2pDeviceState.Connected)
            //                {
            //                    connected = true;
            //                }

            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex.Message);
            //                Console.WriteLine(ex.StackTrace);
            //                RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.BlueToothMacAddress);
            //            }
            //        }
            //        //else if (clientList.Where(x => x.DeviceName.Contains(j.CompanionDevice.Name)).Any())
            //        else if (clientList.Where(x => x.DeviceName.Contains(j.CompanionDevice.BlueToothName)).Any())
            //        {
            //            try
            //            {
            //                List<WifiP2pDevice> tempList = clientList.Where(y => y.DeviceName.Contains(j.CompanionDevice.Name)).ToList();
            //                WifiP2pDevice temp;
            //                if (tempList.Count == 1)
            //                {
            //                    temp = tempList.Single();
            //                }
            //                else if (j.CompanionDevice.WifiMacAddress == null)
            //                {
            //                    temp = clientList.Where(y => y.DeviceName.Contains(j.CompanionDevice.Name)).Single();
            //                }
            //                else
            //                {
            //                    temp = clientList.Where(y => y.DeviceName.Contains(j.CompanionDevice.Name)).Single();
            //                }

            //                bool connected = false;
            //                if (temp.Status == WifiP2pDeviceState.Connected)
            //                {
            //                    connected = true;
            //                }
            //                RefreshConnectSuccessChanged(connected, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex.Message);
            //                Console.WriteLine(ex.StackTrace);
            //                RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
            //            }
            //        }
            //        else
            //        {
            //            try
            //            {
            //                RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex.StackTrace);
            //            }

            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //        Console.WriteLine(ex.StackTrace);
            //    }
            //}
        }



        #endregion


        #region Background server and client threads (I don't think these are used)
        public Java.Lang.Runnable ClientThread = new Java.Lang.Runnable(async () =>
        {
            DatagramSocket socket = null;
            InetAddress host = WiFiStaticDetails.HostInfo.GroupOwnerAddress;
            int port = WiFiStaticDetails.FebrisSocket;

            byte[] sendData;
            byte[] receiveData = new byte[1024];


            while (true)
            {
                sendData = WiFiService.wifiService.String2Bytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                try
                {
                    if (socket == null)
                    {
                        socket = new DatagramSocket(port);
                    }
                }
                catch (Java.Lang.Exception)
                {

                    throw;
                }


                //Client Send
                try
                {
                    DatagramPacket packetSend = new DatagramPacket(sendData, sendData.Length, host, port);
                    socket.Send(packetSend);
                }
                catch (Java.Lang.Exception)
                {

                    throw;
                }

                await Task.Delay(5000);

                //Client Receive
                try
                {
                    DatagramPacket packetReceive = new DatagramPacket(receiveData, receiveData.Length);
                    socket.Receive(packetReceive);
                }
                catch (Java.Lang.Exception)
                {

                    throw;
                }
            }

        });
        public Java.Lang.Runnable ServerThread = new Java.Lang.Runnable(() =>
        {
            DatagramSocket socket = null;
            InetAddress client = null;
            int port = WiFiStaticDetails.FebrisSocket;

            byte[] sendData = new byte[1024];
            byte[] receiveData = new byte[1024];


            while (true)
            {
                try
                {
                    if (socket == null)
                    {
                        socket = new DatagramSocket(port);
                    }
                }
                catch (Java.Lang.Exception)
                {

                    throw;
                }

                DatagramPacket receivePacket = new DatagramPacket(receiveData, receiveData.Length);


                //Server Receive, have to receive first to know clinet address
                try
                {
                    socket.Receive(receivePacket);
                    receiveData = receivePacket.GetData();

                    //WiFiService.wifiService.RefreshReceivedMessage(WiFiService.wifiService.Bytes2String(receiveData));
                    if (client == null)
                        client = receivePacket.Address;

                }
                catch (Java.Lang.Exception)
                {

                    throw;
                }

                //Server Send
                try
                {
                    if (client != null)
                    {
                        DatagramPacket packetSend = new DatagramPacket(sendData, 0, sendData.Length, client, port);
                        socket.Send(packetSend);
                    }
                }
                catch (Java.Lang.Exception)
                {

                    throw;
                }



            }

        });
        public byte[] String2Bytes(string input)
        {
            return Encoding.ASCII.GetBytes(input);
        }
        public string Bytes2String(byte[] input)
        {
            return Encoding.Default.GetString(input);
        }


        #endregion

        public CompanionDeviceEventArgs WiFiInformationRequest(string DeviceUniqueIdentifier, string BlueToothDeviceName, string BlueToothDeviceAlias, string BlueToothDeviceType)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
