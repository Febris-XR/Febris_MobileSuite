// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Droid.Networking.WiFi
{
    /// <summary>
    /// This is not currently used but created to make tracking issues easier.
    /// </summary>
    public class WiFiP2pGroupClass
    {
        #region Variables
        // MP2P-5: removed dead `tempCompanionDeviceWifiMacAddress` static -- declared
        // here and in two sibling files but never read or written anywhere.
        public static WiFiService _wifiService;
        public static WiFiP2pServer wiFiP2pServer;
        public static WiFiP2pGroupClass _wiFiP2pGroupClass;

        #endregion

        #region Constuctors
        public WiFiP2pGroupClass(WiFiService wifiService)
        {
            _wifiService = wifiService;
            _wiFiP2pGroupClass = this;
        }
        #endregion


        #region 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="group"></param>
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
                return;
            }
                                    
            ICollection<WifiP2pDevice> clientList = new List<WifiP2pDevice>();
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
            foreach (var j in LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList)
            {
                try
                {
                    if (clientList.Where(x => x.DeviceAddress == j.CompanionDevice.WifiMacAddress).Any())
                    {
                        try
                        {
                            WifiP2pDevice temp = clientList.Where(y => y.DeviceAddress == j.CompanionDevice.WifiMacAddress).Single();
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


    }
}