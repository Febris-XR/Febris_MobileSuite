// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.SharedMobileLibrary.Interfaces;
using Java.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(Febris.MobileServerV3.Droid.Networking.Bluetooth.BluetoothService))]
namespace Febris.MobileServerV3.Droid.Networking.Bluetooth
{
    // MP2P-9: [Dead] BluetoothService shell. The class was an early attempt at a
    // dedicated Bluetooth RFCOMM transport (Reconnect / SendUpload / ConnectionStatusLoop
    // -- all commented out). Its declared `IBluetoothService` interface implementation
    // was abandoned (see the `//: IBluetoothService` annotation on the class line). The
    // two live methods (GenerateStreamFromFile / CopyStream) have zero callers anywhere
    // in the mobile tree; the `BluetoothStaticDetails.service` static property that
    // would have held the singleton is never assigned or read.
    //
    // Kept on disk pending the MP2P-4 Bluetooth Path A vs B decision (real transport
    // vs delete the Bluetooth surface entirely). The 3 events at the top of the class
    // (BTConnectionUploadSuccessAction / SuccessAction / ChangedAction) are still
    // visible to the IDE in case a Bluetooth feature wants to subscribe; they are
    // never fired today.
    //
    // The class will either be revived into a real transport (Path A -- fill out the
    // commented methods + actually fire the events) or removed entirely (Path B). Do
    // not touch in the meantime.
    #region [Dead - MP2P-9] BluetoothService shell - pending MP2P-4 Bluetooth decision
    public class BluetoothService //: IBluetoothService
    {
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionUploadSuccessAction;
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionSuccessAction;
        public event EventHandler<BTCompanionDeviceEventArgs> BTConnectionChangedAction;
        private static BluetoothSocket _socket { get; set; }
        private static BluetoothDevice _device { get; set; }


        #region connection
        //public void StartConnectionListener()
        //{
        //    Task.Run(() => ConnectionStatusLoop());
        //}

        //public void Reconnect(string input)
        //{
        //    try
        //    {

        //        //_device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(input);
        //        _device = BluetoothStaticDetails._bluetoothAdapter.GetRemoteDevice(input);
        //        Console.WriteLine(_device);
        //        try
        //        {
        //            #region this is for testing multipule uuids
        //            ////var services = _device.GetType();
        //            ////foreach (var i in services)
        //            ////{
        //            ////    Console.WriteLine("normal uuid: " + i.ToString());
        //            ////}



        //            ////Guid guid = Service.BluetoothService.GenericFileTransfer();
        //            //var uuidList = _device.GetUuids();
        //            //foreach (var i in uuidList)
        //            //{
        //            //    Console.WriteLine("normal uuid: "+i.ToString());
        //            //}
        //            //var sdpUuidList = _device.FetchUuidsWithSdp();
        //            //foreach (var i in uuidList)
        //            //{
        //            //    Console.WriteLine("sdp uuid: "+i.ToString());
        //            //}

        //            //for (var i = 0; i <= uuidList.Length; i++)
        //            //{
        //            //    Console.WriteLine(uuidList[i].ToString());
        //            //    try
        //            //    {                            
        //            //        _socket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString(uuidList[i].ToString()));
        //            //        if (!_socket.IsConnected)
        //            //        {
        //            //            _socket.Connect();
        //            //        }
        //            //        if (_socket.IsConnected)
        //            //        {
        //            //            break;
        //            //        }
        //            //    }
        //            //    catch
        //            //    {
        //            //        try
        //            //        {
        //            //            _socket = _device.CreateInsecureRfcommSocketToServiceRecord(UUID.FromString(uuidList[i].ToString()));
        //            //            if (!_socket.IsConnected)
        //            //            {
        //            //                _socket.Connect();
        //            //            }
        //            //            if (_socket.IsConnected)
        //            //            {
        //            //                break;
        //            //            }
        //            //        }
        //            //        catch { }
        //            //    }

        //            //}
        //            #endregion

        //            #region targeting single socket
        //            try
        //            {
        //                _socket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString(BluetoothStaticDetails._febris_UUID));
        //                if (!_socket.IsConnected)
        //                {
        //                    _socket.Connect();
        //                }
        //            }
        //            catch //(IOException e)
        //            {
        //                ////Console.WriteLine(e.StackTrace);
        //                _socket = _device.CreateInsecureRfcommSocketToServiceRecord(UUID.FromString(BluetoothStaticDetails._febris_UUID));
        //                if (!_socket.IsConnected)
        //                {
        //                    _socket.Connect();
        //                }
        //            }
        //            #endregion
        //        }
        //        catch //(IOException e)
        //        {
        //            ////Console.WriteLine(e.StackTrace);
        //            //_socket = _device.CreateInsecureRfcommSocketToServiceRecord(UUID.FromString(BluetoothStaticDetails._febris_UUID));
        //            ////_socket = _device.CreateInsecureL2capChannel(1);
        //            //if (!_socket.IsConnected)
        //            //{
        //            //    _socket.Connect();
        //            //}
        //        }

        //        if (_socket.IsConnected)
        //        {
        //            //send back event arg
        //            //    BTCompanionDeviceEventArgs args = new BTCompanionDeviceEventArgs();
        //            //args.DeviceAddress = _device.Address;
        //            //args.ConnectSuccess = _socket.IsConnected;
        //            //BTConnectionChangedAction(this, args);
        //        }


        //    }
        //    catch { }
        //    finally
        //    {
        //        //_device = null;
        //    }
        //}

        //public void ConnectionStatusLoop()
        //{
        //    while (true)
        //    {
        //        //BluetoothDevice
        //        foreach (var i in BluetoothAdapter.DefaultAdapter.BondedDevices)
        //        {
        //            //no idea how to tell what is currently active
        //            //BluetoothAdapter.DefaultAdapter.

        //        }

        //        BTCompanionDeviceEventArgs args = new BTCompanionDeviceEventArgs();
        //        args.DeviceAddress = "the device that is currently connected";
        //        BTConnectionChangedAction(this, args);
        //        Task.Delay(30000);
        //    }
        //}
        #endregion


        #region Upload
        //public void UploadStatus()
        //{

        //}
        //public void SendUpload(string input)
        //{
        //    //Stream inputStream;
        //    //Stream outputStream;
        //    //int bytes;
        //    string receivedInformation = string.Empty;

        //    if (_device == null)
        //    {
        //        _device = BluetoothStaticDetails._bluetoothAdapter.GetRemoteDevice(input);
        //    }

        //    if (_device.Address != input || _socket == null || !_socket.IsConnected)
        //    {
        //        Reconnect(input);
        //    }

        //    if (_socket.IsConnected)
        //    {
        //        //inputStream = _socket.InputStream;
        //        //outputStream = _socket.OutputStream;

        //        //Stream inputStream = _socket.InputStream;
        //        //Stream presentStream = GenerateStreamFromFile();
        //        //Stream sentStream;
        //        //CopyStream(presentStream, sentStream);

        //        //inputStream.Read();
        //        //StreamReader reader = new StreamReader(inputStream);
        //        //receivedInformation = reader.ReadToEnd();
        //        //outputStream.Write(buffer, );

        //        using (Stream socketStream = GenerateStreamFromFile())
        //        {
        //            var buf = new byte[1024];
        //            try
        //            {
        //                Console.WriteLine("Can Write to socket input stream: " + _socket.InputStream.CanWrite.ToString());
        //                Console.WriteLine("Can Write to socket output stream : " + _socket.OutputStream.CanWrite.ToString());

        //                //int n = socketStream.Read(buf, 0, buf.Length);
        //                //_socket.OutputStream.Write(buf, 0, buf.Length);
        //                //_socket.OutputStream.CopyToAsync(socketStream);
        //                socketStream.CopyToAsync(_socket.OutputStream);

        //                //int n = 0;
        //                //while ((n = socketStream.Read(buf, 0, buf.Length)) != 0)//(n = socketStream.Read(buf, 0, buf.Length)) != -1)
        //                //{
        //                //    Console.WriteLine("n value: " + n);

        //                //    //_socket.InputStream.Write(buf, 0, n);
        //                //    //_socket.OutputStream.Write(buf, 0, buf.Length);
        //                //    _socket.OutputStream.CopyToAsync(socketStream);


        //                //    Console.WriteLine("n value: " + n);
        //                //}                        
        //            }
        //            catch (Java.Lang.Exception e)
        //            {
        //                Console.WriteLine("Stream Error: " + e.Message);
        //            }
        //        }
        //        _socket.Close();


        //    }
        //    #region tried to transmit via intents - did not work

        //    //Stream inputStream = GenerateStreamFromFile();
        //    //Java.IO.File javaIOFile = new Java.IO.File(inputStream.ToString());

        //    //Android.Content.Res.AssetManager assets = ((Android.Content.Res.AssetManager)Forms.Context.Assets);
        //    //var photoAsset = assets.Open(BluetoothStaticDetails._CompanionFileName);
        //    //Java.IO.File javaIOFile = new Java.IO.File(photoAsset.ToString());

        //    //Intent sendIntent = new Intent();
        //    //sendIntent.SetAction(Intent.ActionSend);
        //    //sendIntent.SetType("multipart/*");
        //    //sendIntent.SetComponent(new ComponentName("com.android.bluetooth", "com.android.bluetooth.opp.BluetoothOppLauncherActivity"));
        //    ////sendIntent.PutExtra(Intent.ExtraStream, Android.Net.Uri.FromFile(javaIOFile));
        //    //sendIntent.PutExtra(Intent.ExtraStream, Android.Net.Uri.FromFile(javaIOFile));
        //    //sendIntent.PutExtra(BluetoothDevice.ExtraDevice, _device);
        //    //((Activity)Forms.Context).StartActivity(sendIntent);


        //    //((Activity)Android.App.Application.Context).StartActivity(sendIntent);
        //    //((Activity)Android.App.Application.Context).StartActivityForResult(sendIntent,0);


        //    //var photoAsset = Assets.OpenFd("BusinessCard.png");
        //    //var javaIOFile = new Java.IO.File(photoAsset.ToString());
        //    //var sendIntent = new Intent(Intent.ActionSend);
        //    //sendIntent.SetType("image/*");
        //    //sendIntent.SetComponent(new ComponentName("com.android.bluetooth", "com.android.bluetooth.opp.BluetoothOppLauncherActivity"));
        //    //sendIntent.PutExtra(Intent.ExtraStream, Android.Net.Uri.FromFile(javaIOFile));
        //    //StartActivity(sendIntent);
        //    #endregion
        //}

        private Stream GenerateStreamFromFile()
        {
            Android.Content.Res.AssetManager assets = Forms.Context.Assets;
            Stream output;
            byte[] content;
            const int maxReadSize = 256 * 1024;
            using (BinaryReader br = new BinaryReader(assets.Open(BluetoothStaticDetails._CompanionFileName)))
            {
                content = br.ReadBytes(maxReadSize);
            }
            output = new MemoryStream(content);
            return output;
        }

        public bool CopyStream(Stream PresentStream, Stream sentStream)
        {
            var buf = new byte[1024];
            try
            {
                int n;
                while ((n = PresentStream.Read(buf, 0, buf.Length)) != 0)
                {
                    sentStream.Write(buf, 0, n);
                }
                sentStream.Close();
                PresentStream.Close();
            }
            catch (Java.Lang.Exception e)
            {
                return false;
            }
            return true;
        }

        #endregion



    }
    #endregion // [Dead - MP2P-9] BluetoothService shell
}