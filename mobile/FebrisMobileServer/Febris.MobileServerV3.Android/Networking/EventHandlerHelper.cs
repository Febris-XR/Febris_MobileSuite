// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileServerV3.Droid.Networking;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(EventHandlerHelper))]
namespace Febris.MobileServerV3.Droid.Networking
{
    public class EventHandlerHelper : IEventHandlerHelper
    {
        public static EventHandlerHelper _eventHandler;
        public EventHandlerHelper()
        {
            _eventHandler = this;
        }

        #region Interface Events
        #region Bluetooth Events

        public event EventHandler<CompanionDeviceEventArgs> CompanionCreationAction;

        public event EventHandler<CompanionDeviceEventArgs> BlueToothStateChange;
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionUploadSuccessAction;
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionSuccessAction;
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionChangedAction;


        #endregion

        #region p2p Wifi Events
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pStateChangedAction;
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pPeersChangedAction;
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pConnectSuccessAction;
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pConnectionChangedAction;
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pReceiveMessageAction;
        //public event EventHandler<CompanionDeviceEventArgs> CompanionUpdateAction;
        public event EventHandler<ConnectionStatusCheckEventArgs> WifiP2pCompanionStatusCheckAction;
        public event EventHandler<DownloadEventArgs> WifiP2pCompanionDownloadAction;
        public event EventHandler<UploadEventArgs> WifiP2pCompanionUploadAction;
        public event EventHandler<CompanionDiscoveryEventArgs> ServiceDiscoveryToggleAction;
        //public event EventHandler<ModuleInitalizationEventArgs> WifiP2pModuleInitalizationAction;
        #endregion

        #region USB events
        public event EventHandler usbAttached;
        public event EventHandler<EventArgs> UsbConnectionAction;
        
        #endregion
        #endregion



        #region WiFip2p Broadcast reciever events
        /// <summary>
        /// I don't think I need this anymore
        /// </summary>
        /// <param name="devices"></param>
        public void RefreshPeersChanged(List<string> devices)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
            args.CompanionDeviceNameList = devices;
            // MP2P-5: ?.Invoke avoids NRE when no UI subscriber is attached yet.
            WifiP2pPeersChangedAction?.Invoke(this, args);
        }

        /// <summary>
        /// This can be used to show if the tablet's wifi is enabled. Show in device page.
        /// </summary>
        /// <param name="bEnable"></param>
        public void RefreshStateChanged(bool bEnable)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
            args.WiFiEnable = bEnable;
            // MP2P-5: ?.Invoke avoids NRE when no UI subscriber is attached yet.
            WifiP2pStateChangedAction?.Invoke(this, args);
        }

        /// <summary>
        /// Passes connection status to front end for processing
        /// </summary>
        /// <param name="connected"></param>
        public void RefreshConnectSuccessChanged(bool connected, string companionWiFiMacAddress, string companionBlueToothMacAddress)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
            //added to know what device is connected on other end
            args.ConnectedDeviceMacAddress = companionWiFiMacAddress;
            args.BlueToothMacAddress = companionBlueToothMacAddress;
            args.ConnectSuccess = connected;
            // MP2P-5: ?.Invoke avoids NRE when no UI subscriber is attached yet.
            WifiP2pConnectSuccessAction?.Invoke(this, args);
        }

        /// <summary>
        /// Doesnt seem needed
        /// Passes Connection Status to front end. Seems weird. need to look into more. Currently this just passes "is host" or "is client"
        /// </summary>
        /// <param name="ConnectDeviceName"></param>
        public void RefreshConnectionChanged(string ConnectDeviceName)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
            args.ConnectedDeviceName = ConnectDeviceName;
            // MP2P-5: ?.Invoke avoids NRE when no UI subscriber is attached yet.
            WifiP2pConnectionChangedAction?.Invoke(this, args);
        }

        /// <summary>
        /// Sends recieved message to the front end/main section
        /// </summary>
        /// <param name="ReceivedMessage"></param>
        public void RefreshReceivedMessage(string ReceivedMessage)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
            args.ReceivedMessage = ReceivedMessage;
            // MP2P-5: ?.Invoke avoids NRE when no UI subscriber is attached yet.
            WifiP2pReceiveMessageAction?.Invoke(this, args);
        }

        #endregion

        #region Usb Broadcast reciever events
        //public void RefreshAttachmentChanged(EventArgs args)
        //{
        //    //CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
        //    //args.CompanionDeviceNameList = devices;
        //    UsbConnectionAction(this, args);
        //}

        public void RefreshAttachmentChanged(EventArgs args)
        {
            // MP2P-5: ?.Invoke avoids NRE when no UI subscriber is attached yet.
            UsbConnectionAction?.Invoke(this, args);
        }
        #endregion

        #region Bluetooth Broadcast reciever events
        public void CreateCompanionDevice(BluetoothDevice input)
        {
            try
            {
                CompanionDeviceEventArgs args = new CompanionDeviceEventArgs()
                {
                    Name = input.Name,
                    BlueToothName = input.Name,
                    BlueToothMacAddress = input.Address,
                    BlueToothType = input.Type.ToString()//,
                    //ConnectionStatus = input.
                    //BlueToothBondState = input.BondState
                };
                //if (!String.IsNullOrEmpty(input.Alias))
                //{
                //    args.BlueToothAlias = input.Alias;
                //}
                // MP2P-5: ?.Invoke avoids NRE when no UI subscriber is attached yet.
                CompanionCreationAction?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally { }
        }




        #endregion



        public async Task UpdateCompanionDevice(WifiP2pDevice input)
        {
            try
            {
                CompanionDeviceEventArgs args = new CompanionDeviceEventArgs()
                {
                    WiFiDeviceName = input.DeviceName,
                    WifiMacAddress = input.DeviceAddress,
                    WiFiPrimaryDeviceType = input.PrimaryDeviceType,
                    WiFiSecondaryDeviceType = input.SecondaryDeviceType
                };
                // MP2P-5: ?.Invoke avoids NRE when no UI subscriber is attached yet.
                CompanionCreationAction?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally { }
        }


        /// <summary>
        /// send back to body for processing.
        /// </summary>
        /// <param name="companionIPAddress"></param>
        /// <param name="dataPackage"></param>
        /// <returns></returns>
        internal async Task ProcessDownloadedData(string companionIPAddress, byte[] dataPackage)
        {
            #region connection data
            Console.WriteLine("\n Recieved From : " + companionIPAddress + " Recieved data bytes: " + dataPackage.Length);
            #endregion            
            DownloadEventArgs _event = new DownloadEventArgs()
            {
                CompanionIPAddress = companionIPAddress,
                DataPackage = dataPackage
            };
            // MP2P-5: ?.Invoke avoids NRE when no subscriber is attached yet.
            WifiP2pCompanionDownloadAction?.Invoke(this, _event);

        }



        //public CompanionDeviceEventArgs WiFiInformationRequest(string DeviceUniqueIdentifier, string BlueToothDeviceName, string BlueToothDeviceAlias, string BlueToothDeviceType)
        //{
        //    CompanionDeviceEventArgs data = new CompanionDeviceEventArgs();

        //    try
        //    {
        //        List<CompanionDeviceViewModel> device = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList //maybe change this to the view model list
        //                    .Where(i =>
        //                    i.CompanionDevice.BlueToothAlias == BlueToothDeviceAlias
        //                    && i.CompanionDevice.BlueToothName == BlueToothDeviceName
        //                    && i.CompanionDevice.BlueToothType == BlueToothDeviceType
        //                    && (i.CompanionDevice.WifiMacAddress == null || i.CompanionDevice.WifiMacAddress == string.Empty)
        //                    && (i.CompanionDevice.UniqueIdentifier == DeviceUniqueIdentifier || i.CompanionDevice.UniqueIdentifier == null || i.CompanionDevice.UniqueIdentifier == string.Empty))
        //                    .ToList();
        //        CompanionDeviceViewModel selectedDevice = new CompanionDeviceViewModel();

        //        if (device.Count == 0)
        //        {
        //            Console.WriteLine("no devices to update matching this description");
        //        }
        //        else if (device.Count == 1)
        //        {
        //            selectedDevice = device.First();

        //            //add bluetooth mac for easy sorting
        //            data.BlueToothMacAddress = selectedDevice.CompanionDevice.BlueToothMacAddress;

        //            //search group to get WifiP2pDevice 
        //            WifiP2pDevice gatheredData = GatherWiFiDeviceInformation(selectedDevice.CompanionDevice);

        //            //add data to output                    
        //            data.WiFiDeviceName = gatheredData.DeviceName;
        //            data.WifiMacAddress = gatheredData.DeviceAddress;
        //            data.WiFiPrimaryDeviceType = gatheredData.PrimaryDeviceType;
        //            data.WiFiSecondaryDeviceType = gatheredData.SecondaryDeviceType;
        //        }
        //        else if (device.Count > 1)
        //        {
        //            Console.WriteLine("more than one device matches this description");

        //            //not sure what to do here. 
        //        }
        //    }
        //    catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

        //    return data;

        //}
        //public static WifiP2pDevice GatherWiFiDeviceInformation(CompanionDevice input)
        //{
        //    WifiP2pDevice output = new WifiP2pDevice();
        //    try
        //    {
        //        ICollection<WifiP2pDevice> clientList = WiFiStaticDetails.WifiGroup.ClientList;
        //        //checked to only include clients whos wifimacs are unaccounted for
        //        List<CompanionDeviceViewModel> pairedDevices = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList;
        //        //ICollection<WifiP2pDevice> filteredList = clientList.Where(i => i.DeviceAddress != pairedDevices.Any(j=>j.WifiMacAddress));
        //        //ICollection<WifiP2pDevice> filteredList = clientList.Where(i => i.DeviceAddress != pairedDevices.Any(j => j.WifiMacAddress));
        //        ICollection<WifiP2pDevice> filteredList = clientList.Where(i => pairedDevices.Any(j => j.CompanionDevice.WifiMacAddress != i.DeviceAddress)).ToList();

        //        if (filteredList.Count == 0)
        //        {
        //            Console.WriteLine("no devices to update matching this description");
        //        }
        //        else if (filteredList.Count == 1)
        //        {
        //            output = filteredList.First();

        //            //output = filteredList.Where(i => i.DeviceName == input.BlueToothName || i.DeviceName == input.BlueToothAlias || i.DeviceName.Contains(input.BlueToothName) || i.DeviceName.Contains(input.BlueToothAlias)).Single();
        //        }
        //        else if (filteredList.Count > 1)
        //        {
        //            Console.WriteLine("more than one device matches this description");

        //            output = filteredList.Where(i => i.DeviceName == input.BlueToothName
        //            || i.DeviceName == input.BlueToothAlias
        //            || i.DeviceName.Contains(input.BlueToothName)
        //            || i.DeviceName.Contains(input.BlueToothAlias))
        //                .Single();
        //        }
        //    }
        //    catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

        //    return output;

        //}
    }
}