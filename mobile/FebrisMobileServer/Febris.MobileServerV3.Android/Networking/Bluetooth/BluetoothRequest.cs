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
using AndroidX.Core.App;
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.Droid.Networking.Bluetooth;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Interfaces;
using Java.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(BluetoothRequest))]
namespace Febris.MobileServerV3.Droid.Networking.Bluetooth
{
    public class BluetoothRequest : CompanionDeviceManager.Callback, IBluetoothRequest
    {
        AssociationRequest _request { get; set; }
        AssociationRequest.Builder _requestBuilder { get; set; }
        BluetoothDeviceFilter _filter { get; set; }
        BluetoothDeviceFilter.Builder _filterBuilder { get; set; }
        CompanionDeviceManager _companionDeviceManager { get; set; }
        //BluetoothManagerCallback _bluetoothManagerCallback { get; set; }
        CompanionDeviceManager.Callback _callback { get; set; }
        Context _context = Android.App.Application.Context;

        private static BluetoothSocket _socket { get; set; }
        private static BluetoothDevice _device { get; set; }

        #region pairing request
        public async Task MakePairingRequest()
        {
            try
            {
                await HasNotificationAccess();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            try
            {
                _companionDeviceManager = (CompanionDeviceManager)Forms.Context.GetSystemService(Context.CompanionDeviceService);
                await Request();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            try
            {
                Console.WriteLine(_request.ToString());
                _companionDeviceManager.Associate(_request, CompanionDeviceManagerCallback().Result, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }


        #region Helpers
        #region Filter and Request building
        private async Task Request()
        {
            try
            {
                await RequestBuilder();
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

            try
            {
                _request = _requestBuilder.Build();
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
        }
        private async Task RequestBuilder()
        {
            try
            {
                await Filter();
                _requestBuilder = new AssociationRequest.Builder();
                _requestBuilder.AddDeviceFilter(_filter);
                _requestBuilder.SetSingleDevice(false);

            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
        }
        private async Task FilterBuilder()
        {
            try
            {
                _filterBuilder = new BluetoothDeviceFilter.Builder();
                _filter = _filterBuilder.Build();
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
        }
        private async Task Filter()
        {
            try
            {
                await FilterBuilder();
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
        }
        #endregion

        private async Task<CompanionDeviceManager.Callback> CompanionDeviceManagerCallback()
        {
            try
            {
                _callback = DependencyService.Get<CompanionDeviceManager.Callback>();
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
            return _callback;
        }

        public override void OnDeviceFound(IntentSender chooserLauncher)
        {
            try
            {
                DeviceSelectionRequest(chooserLauncher);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        public override void OnFailure(Java.Lang.ICharSequence error)
        {
            try
            {
                //add notification here
                Console.WriteLine("Devices not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void DeviceSelectionRequest(IntentSender chooserLauncher)
        {
            try
            {
                #region activity result                
                ActivityFlags flagsMask = ActivityFlags.BroughtToFront;
                ActivityFlags flagsValues = ActivityFlags.NewTask;
                int extraFlags = 0;
                Intent fillInIntent = null;
                ((Activity)Forms.Context).StartIntentSenderForResult(chooserLauncher, BluetoothStaticDetails.SELECT_DEVICE_REQUEST_CODE, fillInIntent, flagsMask, flagsValues, extraFlags);
                #endregion   
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        #region notification access
        public async Task HasNotificationAccess()
        {
            bool hasAccess = NotificationManagerCompat.From(_context).AreNotificationsEnabled();
            if (!hasAccess)
            {
                await AskForNotificationAccess();
            }
        }

        public async Task AskForNotificationAccess()
        {

            Console.WriteLine("notificaitons do not have access");
        }

        #endregion
        #endregion
        #endregion

        public async Task Reconnect(string input)

        {
            try
            {

                //_device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(input);
                _device = BluetoothStaticDetails._bluetoothAdapter.GetRemoteDevice(input);
                Console.WriteLine(_device);
                try
                {
                    #region this is for testing multipule uuids
                    ////var services = _device.GetType();
                    ////foreach (var i in services)
                    ////{
                    ////    Console.WriteLine("normal uuid: " + i.ToString());
                    ////}



                    ////Guid guid = Service.BluetoothService.GenericFileTransfer();
                    //var uuidList = _device.GetUuids();
                    //foreach (var i in uuidList)
                    //{
                    //    Console.WriteLine("normal uuid: "+i.ToString());
                    //}
                    //var sdpUuidList = _device.FetchUuidsWithSdp();
                    //foreach (var i in uuidList)
                    //{
                    //    Console.WriteLine("sdp uuid: "+i.ToString());
                    //}

                    //for (var i = 0; i <= uuidList.Length; i++)
                    //{
                    //    Console.WriteLine(uuidList[i].ToString());
                    //    try
                    //    {                            
                    //        _socket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString(uuidList[i].ToString()));
                    //        if (!_socket.IsConnected)
                    //        {
                    //            _socket.Connect();
                    //        }
                    //        if (_socket.IsConnected)
                    //        {
                    //            break;
                    //        }
                    //    }
                    //    catch
                    //    {
                    //        try
                    //        {
                    //            _socket = _device.CreateInsecureRfcommSocketToServiceRecord(UUID.FromString(uuidList[i].ToString()));
                    //            if (!_socket.IsConnected)
                    //            {
                    //                _socket.Connect();
                    //            }
                    //            if (_socket.IsConnected)
                    //            {
                    //                break;
                    //            }
                    //        }
                    //        catch { }
                    //    }

                    //}
                    #endregion

                    #region targeting single socket
                    try
                    {
                        _socket = _device.CreateRfcommSocketToServiceRecord(UUID.FromString(BluetoothStaticDetails._febris_UUID));
                        if (!_socket.IsConnected)
                        {
                            _socket.Connect();
                        }
                    }
                    catch //(IOException e)
                    {
                        ////Console.WriteLine(e.StackTrace);
                        _socket = _device.CreateInsecureRfcommSocketToServiceRecord(UUID.FromString(BluetoothStaticDetails._febris_UUID));
                        if (!_socket.IsConnected)
                        {
                            _socket.Connect();
                        }
                    }
                    #endregion
                }
                catch //(IOException e)
                {
                    ////Console.WriteLine(e.StackTrace);
                    //_socket = _device.CreateInsecureRfcommSocketToServiceRecord(UUID.FromString(BluetoothStaticDetails._febris_UUID));
                    ////_socket = _device.CreateInsecureL2capChannel(1);
                    //if (!_socket.IsConnected)
                    //{
                    //    _socket.Connect();
                    //}
                }

                if (_socket.IsConnected)
                {
                    //send back event arg
                    //    BTCompanionDeviceEventArgs args = new BTCompanionDeviceEventArgs();
                    //args.DeviceAddress = _device.Address;
                    //args.ConnectSuccess = _socket.IsConnected;
                    //BTConnectionChangedAction(this, args);
                }


            }
            catch { }
            finally
            {
                //_device = null;
            }
        }

        public async Task SendUpload(string input)

        {
            //Stream inputStream;
            //Stream outputStream;
            //int bytes;
            string receivedInformation = string.Empty;

            if (_device == null)
            {
                _device = BluetoothStaticDetails._bluetoothAdapter.GetRemoteDevice(input);
            }

            if (_device.Address != input || _socket == null || !_socket.IsConnected)
            {
                //Reconnect(input);
            }

            //if (_socket.IsConnected)
            //{

            try
            {


                var file = System.IO.Path.Combine(FileSystem.CacheDirectory, LocalHardwareStaticDetails._CompanionFileName);
                File.WriteAllBytes(file, GenerateByteArrayFromFile());

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Companion Application",
                    File = new ShareFile(file)
                });


                //byte[] fileArray = GenerateByteArrayFromFile();
                ////var photoAsset = Assets.OpenFd("BusinessCard.png");
                ////var javaIOFile = new Java.IO.File(fileArray);
                //var sendIntent = new Intent(Intent.ActionSend);
                ////sendIntent.SetType("application/zip");
                //sendIntent.SetComponent(new ComponentName("com.android.bluetooth", "com.android.bluetooth.opp.BluetoothOppLauncherActivity"));
                //sendIntent.AddFlags(ActivityFlags.NewTask);
                //sendIntent.PutExtra(Intent.ExtraStream, fileArray);

                //Android.App.Application.Context.StartActivity(sendIntent);



                //byte[] fileArray = GenerateByteArrayFromFile();
                //var sharingIntent = new Intent(Intent.ActionSend);
                //sharingIntent.SetType("application/octet-stream");
                //sharingIntent.AddFlags(ActivityFlags.NewTask);



                //Intent intent = new Intent();
                //intent.SetAction(Intent.ActionSend);
                //Bundle bundle = new Bundle();
                //intent.PutExtra(Intent.ExtraStream, bundle);
                ////intent.PutExtra(Intent.ExtraStream, Uri.FromFile);// (new File(BluetoothStaticDetails._CompanionFileName))) ;
                ////intent.PutExtra(Intent.ExtraStream, new Uri(BluetoothStaticDetails._CompanionFileName));
                //startActivity(intent);


                ///This does local sharing. not what I want at all
                #region - not used
                //byte[] fileArray = GenerateByteArrayFromFile();
                //var sharingIntent = new Intent(Intent.ActionSend);
                //sharingIntent.SetType("application/octet-stream");
                //sharingIntent.AddFlags(ActivityFlags.NewTask);
                ////.SetFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                //sharingIntent.PutExtra(Intent.ExtraStream, fileArray);
                ////var acti = new Activity();
                //Android.App.Application.Context.StartActivity(sharingIntent);
                ////Android.App.Application.Context.StartActivity(Intent.CreateChooser(sharingIntent, "Send file")));
                #endregion

                ///This is pure socketstream but it appears to go nowhere
                #region pure socket stream
                //byte[] dataPackage = GenerateByteArrayFromFile();
                //Stream inputStream = new MemoryStream(dataPackage);
                //Stream outputStream = _socket.OutputStream;


                //int n;

                //while ((n = inputStream.Read(dataPackage, 0, dataPackage.Length)) > 0)
                //{
                //    await outputStream.WriteAsync(dataPackage, 0, n);
                //}
                //inputStream.Close();
                //inputStream.Dispose();
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error at WriteToSocket: " + ex.Message);
                Console.WriteLine("Error at WriteToSocket: " + ex.StackTrace);
                _socket.Close();
                _socket.Dispose();
            }
            finally
            { }



            //using (Stream inputStream = GenerateStreamFromFile())
            //{
            //    var buf = new byte[1024];
            //    try
            //    {



            //        int n;
            //        while ((n = inputStream.Read(dataPackage, 0, dataPackage.Length)) > 0)
            //        {
            //            await _socket.OutputStream.Write(dataPackage, 0, n);
            //        }
            //        inputStream.Close();
            //        inputStream.Dispose();
            //        //await _socket.OutputStream.WriteAsync(socketStream);


            //        Console.WriteLine("Can Write to socket input stream: " + _socket.InputStream.CanWrite.ToString());
            //        Console.WriteLine("Can Write to socket output stream : " + _socket.OutputStream.CanWrite.ToString());

            //        //int n = socketStream.Read(buf, 0, buf.Length);
            //        //_socket.OutputStream.Write(buf, 0, buf.Length);
            //        //_socket.OutputStream.CopyToAsync(socketStream);
            //        //int n;
            //        //while ((n = socketStream.Read(socketStream, 0, socketStream.Length)) > 0)
            //        //{
            //        //    await _socket.OutputStream.WriteAsync(socketStream);
            //        //}
            //        //inputStream.Close();
            //        //inputStream.Dispose();

            //        bool completed = inputStream.CopyToAsync(_socket.OutputStream).IsCompletedSuccessfully;
            //        if (completed) 
            //        {
            //            PairingPageStatusHelper.FileUploadSuccess();
            //        }
            //        else
            //        {
            //            PairingPageStatusHelper.FileUploadFailed();
            //        }
            //        //int n = 0;
            //        //while ((n = socketStream.Read(buf, 0, buf.Length)) != 0)//(n = socketStream.Read(buf, 0, buf.Length)) != -1)
            //        //{
            //        //    Console.WriteLine("n value: " + n);

            //        //    //_socket.InputStream.Write(buf, 0, n);
            //        //    //_socket.OutputStream.Write(buf, 0, buf.Length);
            //        //    _socket.OutputStream.CopyToAsync(socketStream);


            //        //    Console.WriteLine("n value: " + n);
            //        //}                        
            //    }
            //    catch (Java.Lang.Exception e)
            //    {
            //        Console.WriteLine("Stream Error: " + e.Message);
            //    }
            //}
            //_socket.Close();


            // }

        }

        public async Task SendUpload(string input, string fileName)

        {
            //Stream inputStream;
            //Stream outputStream;
            //int bytes;
            string receivedInformation = string.Empty;

            if (_device == null)
            {
                _device = BluetoothStaticDetails._bluetoothAdapter.GetRemoteDevice(input);
            }

            if (_device.Address != input || _socket == null || !_socket.IsConnected)
            {
                //Reconnect(input);
            }

            try
            {
                CompanionSoftwareLogic context = new CompanionSoftwareLogic();
                CompanionAppViewModel appData = context.Get().Result ?? default;
                string softwareID = appData.LocalSoftwarePackage.UUID.ToString();
                //string directoryPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.UncompressedCompanionApplicationPath, softwareID);
                string directoryPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.CompressedCompanionApplicationPath, softwareID+".zip");
                //var file = System.IO.Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BasePath, fileName);
                //byte[] data = Febris.SharedMobileLibrary;
                //File.WriteAllBytes(file, GenerateByteArrayFromFile());//This is building from a static string
                //ShareFile shareFile = new ShareFile(file);
                ShareFile shareFile = new ShareFile(directoryPath);

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Companion Application",
                    File = shareFile
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error at WriteToSocket: " + ex.Message);
                Console.WriteLine("Error at WriteToSocket: " + ex.StackTrace);
                _socket.Close();
                _socket.Dispose();
            }
            finally
            { }

        }

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
        private byte[] GenerateByteArrayFromFile()
        {
            Android.Content.Res.AssetManager assets = Forms.Context.Assets;
            //Stream output;
            byte[] content;
            const int maxReadSize = 256 * 1024;
            using (BinaryReader br = new BinaryReader(assets.Open(LocalHardwareStaticDetails._CompanionFileName)))
            {
                content = br.ReadBytes(maxReadSize);
            }
            //output = new MemoryStream(content);
            return content;
        }
    }
}