// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.SharedMobileLibrary.Models;
using Java.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xamarin.Forms;

namespace Febris.MobileServerV3.Droid.Networking.Bluetooth
{
    public class BluetoothCommunication // will need another way to connect 
    {
        private BluetoothSocket _bluetoothSocket;
        private BluetoothServerSocket _bluetoothServerSocket;
        //private Stream _streamIn;
        //private Stream _streamOut;
        private BluetoothManager _manager;


        public AssetManager Assets { get; private set; }

        public void ServiceSender(CompanionDevice input)
        {
            AssetManager assets = ((AssetManager)Forms.Context.Assets);
            //set variables            
            try
            {
                #region get device
                BluetoothDevice _device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(input.BlueToothMacAddress);
                try
                {
                    BluetoothAdapter.DefaultAdapter.CancelDiscovery();
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("CancelDiscovery: " + ex.StackTrace);
                }
                #endregion


                #region try using standard again
                //List of all found possible uuids
                //string standardSPPConnectionString = "0000110a-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "00001105-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "00001115-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "00001116-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "0000110e-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "0000112f-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "00001112-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "0000111f-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "00001132-0000-1000-8000-00805f9b34fb";//access to messages
                //string standardSPPConnectionString = "00000000-0000-1000-8000-00805f9b34fb";
                //string standardSPPConnectionString = "00000000-0000-1000-8000-00805f9b34fb";


                string standardSPPConnectionString = "00001101-0000-1000-8000-00805F9B34FB";
                //UUID standardSPPConnectionString = UUID.RandomUUID();                                
                //string standardSPPConnectionString = "0000110E-0000-1000-8000-00805F9B34FB";//from https://stackoverflow.com/questions/20009565/connect-to-android-bluetooth-socket                
                try
                {

                    //_bluetoothSocket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString(standardSPPConnectionString));                    
                    _bluetoothSocket = _device.CreateInsecureRfcommSocketToServiceRecord(UUID.FromString(standardSPPConnectionString));
                    //_bluetoothSocket = _device.CreateInsecureRfcommSocketToServiceRecord(standardSPPConnectionString);
                    //_bluetoothSocket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString(standardSPPConnectionString));                    
                    if (_bluetoothSocket.IsConnected)
                    {
                        _bluetoothSocket.Close();
                    }
                    _bluetoothSocket.Connect();
                }
                catch (System.Exception ex) { Console.WriteLine(ex.StackTrace); }

                #endregion

                #region get socket connection from fetched uuids with sdp - spp socket is suppose to be standard (add in later) - spp socket does not exist in the system
                //Get socket connection
                //ParcelUuid[] deviceUUIDList = null;
                //if (_device.FetchUuidsWithSdp())
                //{
                //    deviceUUIDList = _device.GetUuids();
                //    foreach (var i in deviceUUIDList)
                //    {
                //        Console.WriteLine(i);
                //    }
                //}

                //if ((deviceUUIDList != null) && (deviceUUIDList.Length > 0))
                //{
                //    foreach (var uuid in deviceUUIDList)
                //    {
                //        try
                //        {
                //            //NativeActivity.BluetoothService
                //            //_bluetoothSocket = _device.CreateRfcommSocketToServiceRecord(uuid.Uuid);
                //            _bluetoothSocket = _device.CreateInsecureRfcommSocketToServiceRecord(uuid.Uuid);
                //            if (_bluetoothSocket.IsConnected)
                //            {
                //                _bluetoothSocket.Close();
                //            }
                //            //SynchronizationContext currentContext = SynchronizationContext.Current;
                //            _bluetoothSocket.Connect();
                //            System.Threading.Thread.Sleep(5000);
                //            Console.WriteLine("Kinda Working UUID: " + uuid);
                //            //int socketMaxPackage = _bluetoothSocket.MaxTransmitPacketSize;
                //            //no idea if this will work                            
                //            //_bluetoothSocket=_bluetoothServerSocket.Accept();
                //            int socketMaxPackage = _bluetoothSocket.MaxTransmitPacketSize;

                //            if (_bluetoothSocket.IsConnected)
                //            {
                //                Console.WriteLine("UUID is connected: " + uuid);
                //                break;
                //            }
                //            else
                //            {
                //                _bluetoothSocket.Close();
                //            }
                //        }
                //        catch (System.Exception ex)
                //        {
                //            Console.WriteLine("ex: " + ex.Message);
                //            try { _bluetoothSocket.Close(); }
                //            catch
                //            {
                //                Console.WriteLine("turns out it was not connected anyway so it didn't need to be closed");
                //            }
                //        }
                //    }
                //}
                #endregion

                #region trying something different     


                #endregion

                #region variables                
                Stream inputStream = _bluetoothSocket.InputStream;
                Stream outputStream = _bluetoothSocket.OutputStream;
                byte[] responseBuffer = new byte[1024];
                OutputStreamInvoker outputStreamInvoker = _bluetoothSocket.OutputStream as OutputStreamInvoker;
                var javaStream = outputStreamInvoker.BaseOutputStream;
                Mutex _mx = new Mutex();
                int timeout = 5000;
                #endregion

                #region send stream

                //Send asset
                using (Stream socketStream = outputStream)
                {
                    const int maxReadSize = 256 * 1024;
                    byte[] content;
                    using (BinaryReader br = new BinaryReader(assets.Open("FebrisTestPackage.zip")))
                    {
                        content = br.ReadBytes(maxReadSize);
                    }
                    bool writable = socketStream.CanWrite;
                    // bool writable = outputStream.CanWrite;
                    bool writable2 = outputStreamInvoker.CanWrite;

                    #region test data readout
                    Console.WriteLine("Device is bonded: " + _device.BondState);
                    bool isConnected = _bluetoothSocket.IsConnected;
                    Console.WriteLine("Connection is made and is active: " + isConnected);
                    int maxPackage = _bluetoothSocket.MaxTransmitPacketSize;
                    Console.WriteLine("Maxtransmitpacketsize: " + maxPackage.ToString());
                    BluetoothConnectionType connectionType = _bluetoothSocket.ConnectionType;
                    Console.WriteLine("Connection Type: " + connectionType);
                    #endregion


                    #region callback awaiter?
                    //WaitCallback

                    #endregion



                    outputStreamInvoker.Write(content, 0, content.Length);
                    System.Threading.Thread.Sleep(timeout);



                    socketStream.Write(content, 0, content.Length);
                    System.Threading.Thread.Sleep(timeout);

                    outputStream.Write(content, 0, content.Length);
                    System.Threading.Thread.Sleep(timeout);

                    _bluetoothSocket.OutputStream.Write(content, 0, content.Length);
                    System.Threading.Thread.Sleep(timeout);

                    //writable = socketStream.CanWrite;

                    //_mx.WaitOne();
                }

                #endregion

            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            _bluetoothSocket.Close();
        }


        public void CompanionSoftwareSender(CompanionDevice input)
        {
            AssetManager assets = ((AssetManager)Forms.Context.Assets);
            //set variables            
            try
            {
                #region get device
                BluetoothDevice _device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(input.BlueToothMacAddress);
                try
                {
                    BluetoothAdapter.DefaultAdapter.CancelDiscovery();
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("CancelDiscovery: " + ex.StackTrace);
                }
                #endregion

                #region Setup Socket Server
                //_bluetoothServerSocket = ;


                #endregion

                #region variables                
                Stream inputStream = _bluetoothSocket.InputStream;
                Stream outputStream = _bluetoothSocket.OutputStream;
                byte[] responseBuffer = new byte[1024];
                OutputStreamInvoker outputStreamInvoker = _bluetoothSocket.OutputStream as OutputStreamInvoker;
                var javaStream = outputStreamInvoker.BaseOutputStream;
                Mutex _mx = new Mutex();
                int timeout = 5000;
                #endregion

                #region send stream

                //Send asset
                using (Stream socketStream = outputStream)
                {
                    const int maxReadSize = 256 * 1024;
                    byte[] content;
                    using (BinaryReader br = new BinaryReader(assets.Open("FebrisTestPackage.zip")))
                    {
                        content = br.ReadBytes(maxReadSize);
                    }
                    bool writable = socketStream.CanWrite;
                    // bool writable = outputStream.CanWrite;
                    bool writable2 = outputStreamInvoker.CanWrite;

                    #region test data readout
                    Console.WriteLine("Device is bonded: " + _device.BondState);
                    bool isConnected = _bluetoothSocket.IsConnected;
                    Console.WriteLine("Connection is made and is active: " + isConnected);
                    int maxPackage = _bluetoothSocket.MaxTransmitPacketSize;
                    Console.WriteLine("Maxtransmitpacketsize: " + maxPackage.ToString());
                    BluetoothConnectionType connectionType = _bluetoothSocket.ConnectionType;
                    Console.WriteLine("Connection Type: " + connectionType);
                    #endregion


                    #region callback awaiter?
                    //WaitCallback

                    #endregion



                    outputStreamInvoker.Write(content, 0, content.Length);
                    System.Threading.Thread.Sleep(timeout);



                    socketStream.Write(content, 0, content.Length);
                    System.Threading.Thread.Sleep(timeout);

                    outputStream.Write(content, 0, content.Length);
                    System.Threading.Thread.Sleep(timeout);

                    _bluetoothSocket.OutputStream.Write(content, 0, content.Length);
                    System.Threading.Thread.Sleep(timeout);

                    //writable = socketStream.CanWrite;

                    //_mx.WaitOne();
                }

                #endregion

            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            _bluetoothSocket.Close();
        }
    }
}