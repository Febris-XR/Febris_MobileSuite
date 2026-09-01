// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Networking.WiFi;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(EventHandlerHelper))]
namespace Febris.MobileCompanionV3.Droid.Utilities.EventHandlers
{
    public class EventHandlerHelper : IEventHandlerHelper
    {
        public static EventHandlerHelper _eventHandler;
        public EventHandlerHelper()
        {
            _eventHandler = this;
        }

        #region Interface Events
        #region Connection Events
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pStateChangedAction;
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pPeersChangedAction;
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pConnectSuccessAction;
        public event EventHandler<CompanionDeviceEventArgs> WifiP2pConnectionChangedAction;

       

        public event EventHandler<CompanionDeviceEventArgs> WifiP2pReceiveMessageAction;
        #endregion

        #region Communication Events
        public event EventHandler<CompanionDeviceEventArgs> CompanionCreationAction;
        //public event EventHandler<CompanionDeviceEventArgs> CompanionUpdateAction;
        public event EventHandler<ConnectionStatusCheckEventArgs> WifiP2pCompanionStatusCheckAction;
        public event EventHandler<DownloadEventArgs> WifiP2pCompanionDownloadAction;
        public event EventHandler<UploadEventArgs> WifiP2pCompanionUploadAction;
        //public event EventHandler<ModuleInitalizationEventArgs> WifiP2pModuleInitalizationAction;
        #endregion

        #region Discovery loops
        public event EventHandler<CompanionDiscoveryEventArgs> ServiceDiscoveryToggleAction;
        // public event EventHandler<EventArgs> UsbConnectionAction;
        #endregion
        #region USB events
        public event EventHandler<EventArgs> UsbConnectionAction;
        #endregion
        #region Bluetooth events
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionUploadSuccessAction;
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionSuccessAction;
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionChangedAction;
        #endregion
        #endregion

        #region Starting and stoping connection discovery with internal loops
        internal void EndConnectionDiscovery()
        {
            CompanionDiscoveryEventArgs args = new CompanionDiscoveryEventArgs()
            {
                StartDiscoveryLoops = false
            };

            ServiceDiscoveryToggleAction(this, args);

        }

        internal void RestartConnectionDiscovery()
        {
            CompanionDiscoveryEventArgs args = new CompanionDiscoveryEventArgs()
            {
                StartDiscoveryLoops = true
            };

            ServiceDiscoveryToggleAction(this, args);
        }
        #endregion



        #region Broadcast reciever events
        /// <summary>
        /// I don't think I need this anymore
        /// </summary>
        /// <param name="devices"></param>
        public void RefreshPeersChanged(List<string> devices)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
            args.CompanionDeviceNameList = devices;
            WifiP2pPeersChangedAction(this, args);
        }

        /// <summary>
        /// This can be used to show if the tablet's wifi is enabled. Show in device page.
        /// </summary>
        /// <param name="bEnable"></param>
        public void RefreshStateChanged(bool bEnable)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
            args.WiFiEnable = bEnable;
            WifiP2pStateChangedAction(this, args);
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
            WifiP2pConnectSuccessAction(this, args);
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
            WifiP2pConnectionChangedAction(this, args);
        }

        /// <summary>
        /// Sends recieved message to the front end/main section
        /// </summary>
        /// <param name="ReceivedMessage"></param>
        public void RefreshReceivedMessage(string ReceivedMessage)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs();
            args.ReceivedMessage = ReceivedMessage;
            WifiP2pReceiveMessageAction(this, args);
        }

        ///// <summary>
        ///// trying something a little different
        ///// </summary>
        ///// <param name="input"></param>
        public void RefreshConnectSuccessChanged(WifiP2pGroup input)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs()
            {
                Name = input?.Owner?.DeviceName ?? default,
                WiFiDeviceName = input?.Owner?.DeviceName ?? default,
                WifiMacAddress = input?.Owner?.DeviceAddress ?? default,
                WiFiPrimaryDeviceType = input?.Owner?.PrimaryDeviceType ?? default,
                WiFiSecondaryDeviceType = input?.Owner?.SecondaryDeviceType ?? default,

                WiFiGroupInterface = input?.Interface ?? default,
                WiFiNetworkName = input?.NetworkName ?? default
            };

            args.ConnectSuccess = WiFiStaticDetails.networkInfo?.IsConnected ?? false;

            #region Trying to use owner status but it is always unavailable

            //var status = input.Owner.Status;
            //switch (status)
            //{
            //    case WifiP2pDeviceState.Available:
            //        {
            //            args.ConnectionStatus = ConnectionStatus.Available;
            //            break;
            //        }
            //    case WifiP2pDeviceState.Connected:
            //        {
            //            args.ConnectionStatus = ConnectionStatus.Connected;
            //            break;
            //        }
            //    case WifiP2pDeviceState.Failed:
            //        {
            //            args.ConnectionStatus = ConnectionStatus.Failed;
            //            break;
            //        }
            //    case WifiP2pDeviceState.Invited:
            //        {
            //            args.ConnectionStatus = ConnectionStatus.Invited;
            //            break;
            //        }
            //    case WifiP2pDeviceState.Unavailable:
            //        {
            //            args.ConnectionStatus = ConnectionStatus.Unavailable;
            //            break;
            //        }
            //    default:
            //        {
            //            args.ConnectionStatus = ConnectionStatus.Unknown;
            //            break;
            //        }
            //}

            #endregion

            EventHandlerHelper._eventHandler.WifiP2pConnectSuccessAction(this, args);
        }
                
        internal void RefreshConnectSuccessChanged(WifiP2pGroup groupInfo, NetworkInfo networkInfo)
        {
            CompanionDeviceEventArgs args = new CompanionDeviceEventArgs()
            {
                Name = groupInfo?.Owner?.DeviceName ?? default,
                WiFiDeviceName = groupInfo?.Owner?.DeviceName ?? default,
                WifiMacAddress = groupInfo?.Owner?.DeviceAddress ?? default,
                WiFiPrimaryDeviceType = groupInfo?.Owner?.PrimaryDeviceType ?? default,
                WiFiSecondaryDeviceType = groupInfo?.Owner?.SecondaryDeviceType ?? default,
                ConnectSuccess = networkInfo?.IsConnected ?? false,
                WiFiGroupInterface = groupInfo?.Interface ?? default,
                WiFiNetworkName = groupInfo?.NetworkName ?? default
            };

            EventHandlerHelper._eventHandler.WifiP2pConnectSuccessAction(this, args);

        }




        #endregion


        //public void CreateCompanionDevice(BluetoothDevice input)
        //{
        //    try
        //    {
        //        CompanionDeviceEventArgs args = new CompanionDeviceEventArgs()
        //        {
        //            Name = input.Name,
        //            BlueToothName = input.Name,
        //            BlueToothMacAddress = input.Address,
        //            BlueToothType = input.Type.ToString()
        //        };
        //        //if (!String.IsNullOrEmpty(input.Alias))
        //        //{
        //        //    args.BlueToothAlias = input.Alias;
        //        //}
        //        CompanionCreationAction(this, args);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    finally { }
        //}

        //public async Task UpdateCompanionDevice(WifiP2pDevice input)
        //{
        //    try
        //    {
        //        CompanionDeviceEventArgs args = new CompanionDeviceEventArgs()
        //        {
        //            WiFiDeviceName = input.DeviceName,
        //            WifiMacAddress = input.DeviceAddress,
        //            WiFiPrimaryDeviceType = input.PrimaryDeviceType,
        //            WiFiSecondaryDeviceType = input.SecondaryDeviceType
        //        };
        //        CompanionCreationAction(this, args);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    finally { }
        //}


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
            WifiP2pCompanionDownloadAction(this, _event);

        }


    }
}