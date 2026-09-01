// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Companion;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using Febris.MobileServerV3.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.Droid.Networking.Bluetooth
{
    public class BluetoothBroadcastReceiver : BroadcastReceiver
    {
        private readonly BluetoothManager _manager;
        string BLE_PIN = "1234";
        public BluetoothBroadcastReceiver()
        {
            _manager = BluetoothStaticDetails.manager;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;
            switch (action)
            {
                case BluetoothDevice.ActionFound:
                    {
                        var state = _manager.Adapter.State;

                        //send the state back to event handlers
                        BluetoothStaticDetails.processing.RefreshBTState(state);

                        


                        break;
                    }
                case BluetoothDevice.ActionBondStateChanged:
                    {
                        //#region Bluetooth                    
                        ////BluetoothDevice deviceToPair = (BluetoothDevice)intent.GetParcelableExtra(CompanionDeviceManager.ExtraDevice);
                        //BluetoothDevice deviceToPair = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                        //#endregion                        
                        //if (deviceToPair != null)
                        //{
                        //    #region Bluetooth
                        //    bool connected = deviceToPair.CreateBond();
                        //    #endregion                           

                        //    Bond state = deviceToPair.BondState;
                        //    if (state == Bond.Bonded)
                        //    {
                        //        Task.Run(() => EventHandlerHelper._eventHandler.CreateCompanionDevice(deviceToPair));
                        //    }

                        //    //while (state == Bond.Bonding)
                        //    //{
                        //    //    state = deviceToPair.BondState;
                        //    //}
                        //    //try
                        //    //{                                
                                
                        //    //}
                        //    //catch { }
                        //}
                        /////Gives a print out
                        ////var device = BluetoothStaticDetails._bluetoothAdapter.BondedDevices;
                        ////string output = string.Empty;
                        ////foreach(var i in device)
                        ////{
                        ////    output += i.Name + " ";
                        ////}
                        ////PairingPageStatusHelper.GenericMessage(output);
                        break;
                    }
                case BluetoothDevice.ActionPairingRequest:
                    {
                        BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                        //device.SetPin(Encoding.ASCII.GetBytes(BLE_PIN));
                        //device.SetPairingConfirmation(true);


                        break;
                    }
                case BluetoothDevice.ExtraPreviousBondState:
                    {


                        break;
                    }
                case BluetoothDevice.ActionAclConnected:
                    {


                        break;
                    }
                case BluetoothDevice.ActionAclDisconnected:
                    {


                        break;
                    }

            }
            #region bluetooth state changed action - old
            //if (action == BluetoothDevice.ActionFound)
            //{
            //}
            //#endregion
            //#region bluetooth state changed action
            //if (action == BluetoothDevice.ActionBondStateChanged)
            //{                
            //}
            //#endregion
            //#region bluetooth state changed action
            //if (action == BluetoothDevice.ActionPairingRequest)
            //{
            //}
            //#endregion
            //#region bluetooth state changed action
            //if (action == BluetoothDevice.ExtraPreviousBondState)
            //{
            //}
            #endregion
        }
    }
}