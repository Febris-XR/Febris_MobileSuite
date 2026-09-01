// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Bluetooth;
using Android.Net.Wifi.P2p;
using Android.Net.Wifi.P2p.Nsd;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;

[assembly: Xamarin.Forms.Dependency(typeof(WiFiService))]
namespace Febris.MobileServerV3.Droid.Networking.WiFi
{
    #region Wifi Service
    public class WiFiService : IWiFiService
    {

        #region Variables
        public static WiFiService wifiService;
        public static WiFiP2pServer wiFiP2pServer;

        // MP2P-5: idempotency guard for DiscoveryLoop. Both DiscoveringPeers() and
        // ServiceBroadcastingHandler() previously called Task.Run(DiscoveryLoop) with
        // no coordination, which let the infinite while-loop stack up -- every call
        // added another forever-running loop hammering the radio. 0 = not running,
        // 1 = running. CompareExchange(1, 0) atomically claims the slot; the loop
        // clears it in `finally` so a clean shutdown can restart it later.
        private static int _discoveryLoopRunning;

        #endregion

        #region Constuctors
        public WiFiService()
        {
            wifiService = this;
        }
        #endregion

        #region Loops
        /// <summary>
        /// adding a loop for peer discovery so that it wont time out
        /// </summary>
        public async Task DiscoveryLoop()
        {
            // MP2P-5: only one DiscoveryLoop may be live at a time. If another call
            // already claimed the slot, return immediately -- that loop is doing the
            // same DiscoverPeers/Delay cycle this one would.
            if (Interlocked.CompareExchange(ref _discoveryLoopRunning, 1, 0) != 0)
            {
                Console.WriteLine("DiscoveryLoop: already running, skipping duplicate invocation");
                return;
            }
            try
            {
                while (true)
                {
                    try
                    {
                        WiFiStaticDetails.manager?.DiscoverPeers(WiFiStaticDetails.channel, new FebrisActionListener(() => { }));
                        await Task.Delay(WiFiStaticDetails.ServiceBroadcastInterval);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                        break;
                    }
                }
                Task.Run(() => WiFiService.ClearListeners());
            }
            finally
            {
                // Release the slot so a future shutdown/restart can spin a fresh loop.
                Interlocked.Exchange(ref _discoveryLoopRunning, 0);
            }
            //while (true)
            //{
            //    WiFiStaticDetails.manager?.DiscoverPeers(WiFiStaticDetails.channel, new FebrisActionListener(() => { }));
            //    await Task.Delay(WiFiStaticDetails.DiscoveryLoopTimer);
            //}
        }

        //private async Task ListenerLoop()
        //{
        //    while (true)
        //    {
        //        if (!WiFiStaticDetails.record.Contains(WiFiStaticDetails.ServiceName))
        //        {
        //            WiFiStaticDetails.record.Add(WiFiStaticDetails.ServiceName);
        //        }
        //        else
        //        {
        //            Console.WriteLine("record is already set");
        //        }

        //        if (WiFiStaticDetails.serviceInfo == null)
        //        {
        //            WiFiStaticDetails.serviceInfo = WifiP2pUpnpServiceInfo.NewInstance(WiFiStaticDetails.ServiceUUID, WiFiStaticDetails.ServiceType, WiFiStaticDetails.record);
        //        }
        //        else
        //        {
        //            Console.WriteLine("serviceInfo is already set");
        //        }



        //        //if (WiFiStaticDetails.receiver.IsServiceRunning)                     
        //        //{
        //        WiFiStaticDetails.manager?.AddLocalService(WiFiStaticDetails.channel, WiFiStaticDetails.serviceInfo, new FebrisActionListener(() => { }));
        //        //}
        //        //else
        //        //{
        //        //    Console.WriteLine("AddLocalService is already set");                    
        //        //}
        //        if (WiFiStaticDetails.receiver.IsOrderedBroadcast)
        //        {
        //            MainActivity main = (MainActivity)Forms.Context;
        //            main.RestartReciever();
        //        }
        //        else
        //        {
        //            MainActivity main = (MainActivity)Forms.Context;
        //            main.RestartReciever();
        //        }
        //        await Task.Delay(30000);
        //    }

        //}

        #endregion

        #region Discovery              
        /// <summary>
        /// Initalizes the process of discovering peers. But in reality if forces the UPNPservice to stay alive
        /// </summary>
        public void DiscoveringPeers()
        {
            //WiFiStaticDetails.manager?.DiscoverPeers(WiFiStaticDetails.channel, new FebrisActionListener(() => { }));
            Task.Run(() => DiscoveryLoop());

        }

        public static async Task RunLocalServiceListenerLoop()
        {
            try
            {
                //ClearedListeners();
                //while (true)
                //{
                //try
                //{
                if (!WiFiStaticDetails.record.Contains(WiFiStaticDetails.ServiceName))
                    WiFiStaticDetails.record.Add(WiFiStaticDetails.ServiceName);

                if (WiFiStaticDetails.serviceInfo == null)
                    WiFiStaticDetails.serviceInfo = WifiP2pUpnpServiceInfo.NewInstance(WiFiStaticDetails.ServiceUUID, WiFiStaticDetails.ServiceType, WiFiStaticDetails.record);

                //WiFiStaticDetails.manager.ClearLocalServices(WiFiStaticDetails.channel, new FebrisActionListener(WiFiService.wifiService.ClearedListeners));
                //WiFiStaticDetails.manager.ClearLocalServices(WiFiStaticDetails.channel, new FebrisActionListener(() => { }));
                //if (WiFiStaticDetails.receiver.IsServiceRunning)
                //{
                WiFiStaticDetails.manager?.AddLocalService(WiFiStaticDetails.channel, WiFiStaticDetails.serviceInfo, new FebrisActionListener(() => { }));
                //}
                //else
                //{
                //    Console.WriteLine("AddLocalService is already set");
                //}


                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.StackTrace);
                //    throw;
                //}
                //await Task.Delay(30000);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            //WiFiStaticDetails.record.Add(WiFiStaticDetails.ServiceName);
            //WiFiStaticDetails.serviceInfo = WifiP2pUpnpServiceInfo.NewInstance(WiFiStaticDetails.ServiceUUID, WiFiStaticDetails.ServiceType, WiFiStaticDetails.record);
            //WiFiStaticDetails.manager.AddLocalService(WiFiStaticDetails.channel, WiFiStaticDetails.serviceInfo, new FebrisActionListener(() => { }));
        }

        /// <summary>
        /// The ONLY live path by which this Server advertises itself, and the whole of issue 14
        /// fault C hangs off it.
        ///
        /// <para><b>RebuildLocalService is the SUCCESS CALLBACK of ClearLocalServices.</b> If the
        /// clear fails, the rebuild never runs, no local service is added, and the Server sits
        /// there looking perfectly healthy while advertising NOTHING. A Companion then discovers
        /// nothing, never receives a UPnP service response, and is stranded, which is exactly the
        /// observed fault C signature: the Companion's discovery loop running normally with zero
        /// failures and zero responses.</para>
        ///
        /// <para>Reached from the WiFiP2pDirectBroadcastReceiver constructor and from the tail of
        /// the peer-discovery loop. The other three AddLocalService sites in this file are all
        /// unreachable, their callers commented out, so there is no second chance.</para>
        /// </summary>
        public static async Task ClearListeners()
        {
            Console.WriteLine("local service: clearing, then rebuilding the UPnP advertisement");
            if (WiFiStaticDetails.manager == null || WiFiStaticDetails.channel == null)
            {
                Console.WriteLine("local service: CANNOT advertise, manager or channel is null "
                    + "(manager=" + (WiFiStaticDetails.manager == null ? "null" : "ok")
                    + " channel=" + (WiFiStaticDetails.channel == null ? "null" : "ok") + ")");
                return;
            }
            WiFiStaticDetails.manager.ClearLocalServices(WiFiStaticDetails.channel, new FebrisActionListener(WiFiService.wifiService.RebuildLocalService, "ClearLocalServices"));
        }

        public void RebuildLocalService()
        {
            try
            {
                if (!WiFiStaticDetails.record.Contains(WiFiStaticDetails.ServiceName))
                    WiFiStaticDetails.record.Add(WiFiStaticDetails.ServiceName);
                if (WiFiStaticDetails.serviceInfo == null)
                    WiFiStaticDetails.serviceInfo = WifiP2pUpnpServiceInfo.NewInstance(WiFiStaticDetails.ServiceUUID, WiFiStaticDetails.ServiceType, WiFiStaticDetails.record);
                Console.WriteLine("local service: adding UPnP advertisement '" + WiFiStaticDetails.ServiceName + "'");
                WiFiStaticDetails.manager?.AddLocalService(WiFiStaticDetails.channel, WiFiStaticDetails.serviceInfo, new FebrisActionListener(WiFiService.wifiService.ServiceBroadcastingHandler, "AddLocalService"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("local service: RebuildLocalService threw: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        /// <summary>
        /// WiFiStaticDetails.ServiceBroadcastInterval
        /// "I started a Thread that would periodically call WifiP2pManager's method discoverPeers. That seemed to be forcing to rebroadcast all the service information." 
        /// -- https://stackoverflow.com/questions/26300889/wifi-p2p-service-discovery-works-intermittently
        /// </summary>        
        public void ServiceBroadcastingHandler()
        {
            try
            {
                Task.Run(() => DiscoveryLoop());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public void ClearedListeners()
        {
            //Task.Run(() => ListenerLoop());
            WiFiStaticDetails.record.Add(WiFiStaticDetails.ServiceName);
            WiFiStaticDetails.serviceInfo = WifiP2pUpnpServiceInfo.NewInstance(WiFiStaticDetails.ServiceUUID, WiFiStaticDetails.ServiceType, WiFiStaticDetails.record);
            WiFiStaticDetails.manager?.AddLocalService(WiFiStaticDetails.channel, WiFiStaticDetails.serviceInfo, new FebrisActionListener(() => { }));
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Part of send device Message
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        /// <returns></returns>
        //public bool CopyStream(Stream inputStream, Stream outputStream)
        //{
        //    var buf = new byte[1024];
        //    try
        //    {
        //        int n;
        //        while ((n = inputStream.Read(buf, 0, buf.Length)) != 0)
        //            outputStream.Write(buf, 0, n);
        //        outputStream.Close();
        //        inputStream.Close();
        //    }
        //    catch (Java.Lang.Exception e)
        //    {
        //        return false;
        //    }
        //    return true;
        //}
        /// <summary>
        /// Part of send device Message
        /// 
        /// This is where the wifi bars are added. I don't actually think it does anything else. 
        /// 
        /// THIS APPEARS TO BE WHERE THE NEW DEVICE INFORMATION IS LOGGED... I DONT KNOW WHY
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        //public Stream GenerateStreamFromString(string s)
        //{
        //    MemoryStream stream = new MemoryStream();
        //    StreamWriter writer = new StreamWriter(stream);
        //    writer.Write(s);
        //    writer.Flush();
        //    stream.Position = 0;
        //    return stream;
        //}
        //public byte[] String2Bytes(string input)
        //{
        //    return Encoding.ASCII.GetBytes(input);
        //}
        //public string Bytes2String(byte[] input)
        //{
        //    return Encoding.Default.GetString(input);
        //}
        internal void GroupConnectionStatus(WifiP2pGroup group)
        {
            if (group == null || group.ClientList == null || group.ClientList.Count == 0)
            {
                foreach (var j in LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.ItemList ?? new List<CompanionDeviceViewModel>())
                {
                    try
                    {
                        EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.BlueToothMacAddress);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                //if(WiFiStaticDetails.serviceInfo == null)
                //{
                //    RebuildLocalService();
                //}
                return;
            }


            //ICollection<WifiP2pDevice> clientList;// = new ICollection<WifiP2pDevice>();
            ICollection<WifiP2pDevice> clientList = new List<WifiP2pDevice>();//.//group.ClientList;
            try
            {
                clientList = group.ClientList;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return;
            }

            #region commented out
            //check over clientList - This is just for visuals and does not need to really be updated like this
            //foreach (var j in clientList)
            //{
            //    try
            //    {
            //        if (LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(x => x.CompanionDevice.WifiMacAddress == j.DeviceAddress).Any())
            //        {
            //            try
            //            {
            //                CompanionDeviceViewModel temp = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(i => i.CompanionDevice.WifiMacAddress == j.DeviceAddress).Single();
            //                //WifiP2pDevice temp = clientList.Where(y => y.DeviceAddress == j.CompanionDevice.WifiMacAddress).Single();
            //                bool connected = false;
            //                if (j.Status == WifiP2pDeviceState.Connected)
            //                {
            //                    connected = true;
            //                }
            //                EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, temp.CompanionDevice.WifiMacAddress, temp.CompanionDevice.BlueToothMacAddress);
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex.Message);
            //                Console.WriteLine(ex.StackTrace);
            //                EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.DeviceAddress, string.Empty);
            //            }
            //        }
            //        else if (LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(x => x.CompanionDevice.BlueToothName.Contains(j.DeviceName)).Any())
            //        {
            //            try
            //            {
            //                //CompanionDeviceViewModel
            //                List<CompanionDeviceViewModel> tempList = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(y => y.CompanionDevice.Name.Contains(j.DeviceName)).ToList();
            //                CompanionDeviceViewModel temp;
            //                if (tempList.Count == 1)
            //                {
            //                    temp = tempList.Single();
            //                }
            //                else
            //                {
            //                    if (tempList.Where(i => i.CompanionDevice.WifiMacAddress == null).Any())
            //                    {
            //                        if (tempList.Where(i => i.CompanionDevice.WifiMacAddress == null).Count() == 1)
            //                        {
            //                            temp = tempList.Where(i => i.CompanionDevice.WifiMacAddress == null).Single();
            //                        }
            //                        else
            //                        {
            //                            //need to add something here
            //                        }
            //                    }

            //                }


            //                bool connected = false;
            //                if (j.Status == WifiP2pDeviceState.Connected)
            //                {
            //                    connected = true;
            //                }
            //                EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, temp.CompanionDevice.WifiMacAddress, temp.CompanionDevice.BlueToothMacAddress);
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex.Message);
            //                Console.WriteLine(ex.StackTrace);
            //                EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, temp.CompanionDevice.WifiMacAddress, temp.CompanionDevice.BlueToothMacAddress);
            //            }
            //            //try
            //            //{
            //            //    CompanionDeviceViewModel temp = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(i => i.CompanionDevice.WifiMacAddress == j.DeviceAddress).Single();
            //            //    //WifiP2pDevice temp = clientList.Where(y => y.DeviceAddress == j.CompanionDevice.WifiMacAddress).Single();
            //            //    bool connected = false;
            //            //    if (j.Status == WifiP2pDeviceState.Connected)
            //            //    {
            //            //        connected = true;
            //            //    }
            //            //    EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, temp.CompanionDevice.WifiMacAddress, temp.CompanionDevice.BlueToothMacAddress);
            //            //}
            //            //catch (Exception ex)
            //            //{
            //            //    Console.WriteLine(ex.Message);
            //            //    Console.WriteLine(ex.StackTrace);
            //            //    EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.DeviceAddress, string.Empty);
            //            //}
            //        }
            //        else if (clientList.Count() > LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Count())
            //        {
            //            if (!LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(x => x.CompanionDevice.BlueToothName.Contains(j.DeviceName)).Any()
            //                && !LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(x => x.CompanionDevice.WifiMacAddress == j.DeviceAddress).Any())
            //            {

            //                CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
            //                {

            //                };
            //                List<CompanionDeviceViewModel> quarenteneList;

            //            }
            //        }


            //    }
            //    catch (Exception ex)
            //    {
            //        //StatusUpdateHelper.
            //        Console.WriteLine(ex.Message);
            //        Console.WriteLine(ex.StackTrace);
            //        throw;
            //    }
            //}
            #endregion
            ///HERE IS LIST BUILDING
            foreach (var j in LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList)
            {
                try
                {
                    if (clientList.Where(x => x.DeviceAddress == j.CompanionDevice.WifiMacAddress).Any())
                    {
                        try
                        {
                            WifiP2pDevice temp = clientList
                                .Where(y => y.DeviceAddress == j.CompanionDevice.WifiMacAddress)
                                .Single();
                            bool connected = false;
                            if (temp.Status == WifiP2pDeviceState.Connected)
                            {
                                connected = true;
                            }
                            EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.BlueToothMacAddress);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                            EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.BlueToothMacAddress);
                        }
                    }
                    //else if (clientList.Where(x => x.DeviceName.Contains(j.CompanionDevice.Name)).Any())
                    // NULL GUARD, issue 18 made real. Every numerically-paired record has a null
                    // BlueToothName, and string.Contains(null) throws ArgumentNullException, so
                    // this branch crashed GroupConnectionStatus on every pass once such a device
                    // existed. Observed live 2026-07-28 as a repeating "Value cannot be null"
                    // inside this lambda. The Bluetooth-name branch is only meaningful for
                    // Bluetooth-onboarded records, so it is skipped when the name is absent.
                    else if (!string.IsNullOrEmpty(j.CompanionDevice.BlueToothName)
                        && clientList.Where(x => x.DeviceName != null && x.DeviceName.Contains(j.CompanionDevice.BlueToothName)).Any())
                    {
                        try
                        {
                            List<WifiP2pDevice> tempList = clientList
                                .Where(y => y.DeviceName.Contains(j.CompanionDevice.Name))
                                .ToList();
                            WifiP2pDevice temp;
                            if (tempList.Count == 1)
                            {
                                temp = tempList.Single();
                            }
                            else if (j.CompanionDevice.WifiMacAddress == null)
                            {
                                temp = clientList
                                    .Where(y => y.DeviceName.Contains(j.CompanionDevice.Name))
                                    .Single();
                            }
                            else
                            {
                                temp = clientList
                                    .Where(y => y.DeviceName.Contains(j.CompanionDevice.Name))
                                    .Single();
                            }

                            bool connected = false;
                            if (temp.Status == WifiP2pDeviceState.Connected)
                            {
                                connected = true;
                            }
                            EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                            EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
                        }
                    }
                    //else if (clientList.Where(y => y.DeviceAddress == default).Any())
                    //else if (j.CompanionDevice.UniqueIdentifier == default)
                    //{
                    //    ///Had to add this so the Meta Quest 3S would be recognized. (its wifi name is Android_somethingStupid. I have no idea why)
                    //    ///
                    //    try
                    //    {
                    //        ///LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    //        ///

                    //        //foreach(var i in LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList)
                    //        //{

                    //        //}

                    //        List<CompanionDeviceViewModel> refTempList = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    //            .Where(x => x.CompanionDevice.UniqueIdentifier == default)
                    //            .ToList();
                    //        List<WifiP2pDevice> tempList = new List<WifiP2pDevice>();//clientList.Where(y => LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Any(x => x.CompanionDevice.WifiMacAddress != y.DeviceAddress)).ToList();
                    //        WifiP2pDevice temp = default;

                    //        if (refTempList.Count == 1)
                    //        {

                    //            tempList = clientList.Where(x=>!LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Any(y=>y.CompanionDevice.WifiMacAddress==x.DeviceAddress)).ToList();

                    //            if (tempList.Count == 1)
                    //            {
                    //                temp = tempList.Single();

                                    
                    //            }

                    //            j.CompanionDevice.WifiMacAddress = temp.DeviceAddress;
                    //            j.CompanionDevice.WiFiDeviceName = temp.DeviceName;
                    //        }


                            
                            

                    //        bool connected = false;
                    //        if (temp.Status == WifiP2pDeviceState.Connected)
                    //        {
                    //            connected = true;
                    //        }
                    //        EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine(ex.Message);
                    //        Console.WriteLine(ex.StackTrace);
                    //        EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
                    //    }
                    //    ///Had the wrong starting point here
                    //    //try
                    //    //{


                    //    //    List<WifiP2pDevice> quarenteneList = new List<WifiP2pDevice>();

                    //    //    WifiP2pDevice temp = default;

                    //    //    //List<WifiP2pDevice> tempList = clientList.Where(y => y.DeviceName.Contains(j.CompanionDevice.Name)).ToList();  

                    //    //    //Get the single device from the registered list that does not have a uniqueIdentifier and use the only device on the tempList to pair up
                    //    //    //we laready have j. J is from the registered list and J needs to be modified

                    //    //    List<WifiP2pDevice> tempList = clientList.Where(y => y.DeviceName.Contains(j.CompanionDevice.Name)).ToList();

                    //    //    if (tempList.Count == 1)
                    //    //    {
                    //    //        temp = tempList.Single();
                    //    //    }

                    //    //    bool connected = false;
                    //    //    if (temp.Status == WifiP2pDeviceState.Connected)
                    //    //    {
                    //    //        connected = true;
                    //    //    }
                    //    //    EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);


                    //    //}
                    //    //catch (Exception ex)
                    //    //{
                    //    //    Console.WriteLine(ex.Message);
                    //    //    Console.WriteLine(ex.StackTrace);
                    //    //    EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
                    //    //}

                    //}
                    else if (clientList.Count > LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Count)
                    {
                        try
                        {
                            List<WifiP2pDevice> quarenteneList = new List<WifiP2pDevice>();

                            List<WifiP2pDevice> tempList = clientList.Where(y => y.DeviceName.Contains(j.CompanionDevice.Name)).ToList();
                            WifiP2pDevice temp;
                            if (tempList.Count == 1)
                            {
                                temp = tempList.Single();
                            }
                            else if (j.CompanionDevice.WifiMacAddress == null)
                            {
                                temp = clientList.Where(y => y.DeviceName.Contains(j.CompanionDevice.Name)).Single();
                            }
                            else
                            {
                                temp = clientList.Where(y => y.DeviceName.Contains(j.CompanionDevice.Name)).Single();
                            }

                            bool connected = false;
                            if (temp.Status == WifiP2pDeviceState.Connected)
                            {
                                connected = true;
                            }
                            EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(connected, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                            EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
                        }

                    }
                    else
                    {
                        ///I think I can attach a device to a temp area and start listening for initalization loop. if the initalization loop finds something... add it. if not boot it.
                        try
                        {
                            EventHandlerHelper._eventHandler.RefreshConnectSuccessChanged(false, j.CompanionDevice.WifiMacAddress, j.CompanionDevice.Name);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        #endregion

        /// <summary>
        /// THIS SEEMS TO FILL IN THR INFORMATION BUT DOERS NOT REJECT CONNECTIONS
        /// </summary>
        /// <param name="DeviceUniqueIdentifier"></param>
        /// <param name="BlueToothDeviceName"></param>
        /// <param name="BlueToothDeviceAlias"></param>
        /// <param name="BlueToothDeviceType"></param>
        /// <returns></returns>
        public CompanionDeviceEventArgs WiFiInformationRequest(string DeviceUniqueIdentifier, string BlueToothDeviceName, string BlueToothDeviceAlias, string BlueToothDeviceType)
        {
            CompanionDeviceEventArgs data = new CompanionDeviceEventArgs();

            try
            {
                List<CompanionDeviceViewModel> device = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList //maybe change this to the view model list
                            .Where(i =>
                            //i.CompanionDevice.BlueToothAlias == BlueToothDeviceAlias
                            //&& 
                            i.CompanionDevice.BlueToothName == BlueToothDeviceName
                            //&& i.CompanionDevice.BlueToothType == BlueToothDeviceType
                            //&& (i.CompanionDevice.WifiMacAddress == null || i.CompanionDevice.WifiMacAddress == string.Empty)
                            //&& (i.CompanionDevice.UniqueIdentifier == DeviceUniqueIdentifier || i.CompanionDevice.UniqueIdentifier == null || i.CompanionDevice.UniqueIdentifier == string.Empty)
                            )
                            .ToList();
                CompanionDeviceViewModel selectedDevice = new CompanionDeviceViewModel();

                if (device.Count == 0)
                {
                    Console.WriteLine("no devices to update matching this description");

                    device = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList //maybe change this to the view model list
                            .Where(i =>
                            (i.CompanionDevice.WifiMacAddress == null || i.CompanionDevice.WifiMacAddress == string.Empty || i.CompanionDevice.WifiMacAddress == default)
                            && (i.CompanionDevice.UniqueIdentifier == DeviceUniqueIdentifier || i.CompanionDevice.UniqueIdentifier == null || i.CompanionDevice.UniqueIdentifier == string.Empty)
                            )
                            .ToList();
                    if (device.Count == 1)
                    {
                        selectedDevice = device.First();

                        //add bluetooth mac for easy sorting
                        data.BlueToothName = selectedDevice.CompanionDevice.BlueToothName;
                        data.BlueToothMacAddress = selectedDevice.CompanionDevice.BlueToothMacAddress;

                        //search group to get WifiP2pDevice 
                        WifiP2pDevice gatheredData = GatherWiFiDeviceInformation(selectedDevice.CompanionDevice);

                        //add data to output                    
                        data.WiFiDeviceName = gatheredData.DeviceName;
                        data.WifiMacAddress = gatheredData.DeviceAddress;
                        data.WiFiPrimaryDeviceType = gatheredData.PrimaryDeviceType;
                        data.WiFiSecondaryDeviceType = gatheredData.SecondaryDeviceType;
                    }
                }
                else if (device.Count == 1)
                {
                    selectedDevice = device.First();

                    //add bluetooth mac for easy sorting
                    data.BlueToothName = selectedDevice.CompanionDevice.BlueToothName;
                    data.BlueToothMacAddress = selectedDevice.CompanionDevice.BlueToothMacAddress;

                    //search group to get WifiP2pDevice 
                    WifiP2pDevice gatheredData = GatherWiFiDeviceInformation(selectedDevice.CompanionDevice);

                    //add data to output                    
                    data.WiFiDeviceName = gatheredData.DeviceName;
                    data.WifiMacAddress = gatheredData.DeviceAddress;
                    data.WiFiPrimaryDeviceType = gatheredData.PrimaryDeviceType;
                    data.WiFiSecondaryDeviceType = gatheredData.SecondaryDeviceType;
                }
                else if (device.Count > 1)
                {
                    Console.WriteLine("more than one device matches this description");

                    //not sure what to do here. 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return data;

        }

        public static WifiP2pDevice GatherWiFiDeviceInformation(CompanionDevice input)
        {
            WifiP2pDevice output = new WifiP2pDevice();
            try
            {
                ICollection<WifiP2pDevice> clientList = WiFiStaticDetails.WifiGroup.ClientList;
                //checked to only include clients whos wifimacs are unaccounted for
                List<CompanionDeviceViewModel> pairedDevices = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList;
                //ICollection<WifiP2pDevice> filteredList = clientList.Where(i => i.DeviceAddress != pairedDevices.Any(j=>j.WifiMacAddress));
                //ICollection<WifiP2pDevice> filteredList = clientList.Where(i => i.DeviceAddress != pairedDevices.Any(j => j.WifiMacAddress));
                // All, not Any. The intent stated in the comment above is "clients whose MACs are
                // unaccounted for", and `Any(!=)` does not express that: it is true whenever ANY
                // known device has a different MAC, which is true almost always, so the filter
                // barely filtered. With one Companion that was harmless because the list held one
                // entry either way. With two it pushed the count above one and sent every
                // resolution down the "more than one device matches this description" branch,
                // which returns nothing, so the second Companion never got a MAC at all.
                //
                // `All(!=)` means the client's MAC matches no device we already know about, which
                // is what "unaccounted for" actually means. Devices then resolve one per
                // announcement as each is claimed.
                ICollection<WifiP2pDevice> filteredList = clientList
                    .Where(i => pairedDevices.All(j => j.CompanionDevice.WifiMacAddress != i.DeviceAddress))
                    .ToList();

                if (filteredList.Count == 0)
                {
                    Console.WriteLine("no devices to update matching this description");
                }
                else if (filteredList.Count == 1)
                {
                    output = filteredList.First();

                    //output = filteredList.Where(i => i.DeviceName == input.BlueToothName || i.DeviceName == input.BlueToothAlias || i.DeviceName.Contains(input.BlueToothName) || i.DeviceName.Contains(input.BlueToothAlias)).Single();
                }
                else if (filteredList.Count > 1)
                {
                    Console.WriteLine("more than one device matches this description");

                    output = filteredList.Where(i => i.DeviceName == input.BlueToothName
                    || i.DeviceName == input.BlueToothAlias
                    || i.DeviceName.Contains(input.BlueToothName)
                    || i.DeviceName.Contains(input.BlueToothAlias))
                        .Single();
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

            return output;

        }

        public void ListenForServices()
        {
            throw new NotImplementedException();
        }
    }


    #endregion   

}