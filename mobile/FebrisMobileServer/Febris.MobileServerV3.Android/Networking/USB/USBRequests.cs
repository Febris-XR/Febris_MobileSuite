// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Febris.AdbLibrary.AdbLib;
using Febris.MobileServerV3.Droid.Networking.USB;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(USBRequests))]
namespace Febris.MobileServerV3.Droid.Networking.USB
{
    public class USBRequests : Attribute, IUSBRequests
    {
        private AdbConnection adbConnection;
        //private CustomConnection adbConnection;
        UsbManager _manager = USBStaticDetails._usbManager;
        Context _context = Android.App.Application.Context;
        public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris");

        public Task MakeRequest()
        {
            var devicelist = _manager.DeviceList;
            string output = string.Empty;
            foreach (var i in devicelist)
            {
                output = output + " " + i.Value.ProductName;
            }
            PairingPageStatusHelper.GenericMessage(output);
            throw new NotImplementedException();
        }

        public async Task SendUpload(string packagePath)
        {
            UsbManager _usbManager = USBStaticDetails._usbManager;
            PairingPageStatusHelper.GenericMessage("Sending software package to remote device");
            //adbConnection = USBStaticDetails._adbConnection;

            ///There is an issue here THAT IS NOT A GOOD ROUTE. A ROUTE ALREADY EXISTS. USE THAT. MAY NEED A PASSED EVENT
            //string remotePath = await USBRequestCreator.NewestLocalPackageInstallRequestBuilder(input);
            //AdbStream stream = await adbConnection.Open("shell:pm install -r " + remotePath);
            //AdbStream stream = adbConnection.Open("shell:pm install -r " + remotePath);
            //await Task.Run(() => InstallRequest(stream));
            await PushAndInstallRequest(packagePath, null);
        }

        public async Task SendUpload()
        {

            UsbManager _usbManager = USBStaticDetails._usbManager;
            //int devicesCount = _usbManager.DeviceList.Count;

            PairingPageStatusHelper.GenericMessage("Attempting to communicate with device: " + USBStaticDetails._device.DeviceName + " on interface: " + USBStaticDetails._interface.Name);
            string command = string.Empty;

            var intentToSend = GenerateInstallIntent();

            //_usbManager

            try
            {


            }
            catch (AndroidException ex) { _logger.Println("An Android error occured when trying to establish endpoints: " + ex.StackTrace); }
            catch (Exception ex)
            {
                _logger.Println("A generic error occured when trying to establish endpoints: " + ex.StackTrace);
                _logger.Println("Cleaning usb connection data");
                USBStaticDetails.CleanConnection();
            }

        }

        /// <summary>
        /// adb install -r /path/to/your-filename.apk
        /// </summary>
        /// <returns></returns>
        private object GenerateInstallIntent()
        {
            UsbRequest request = new UsbRequest();
            byte[] dataPackage = { };
            int length = 0;
            int timeout = 0;

            USBStaticDetails._connection.BulkTransfer(USBStaticDetails._adb_endpoint, dataPackage, length, timeout);

            throw new NotImplementedException();
        }

        private AdbRequest GenerateADBRequest()
        {
            try
            {

                AdbRequest request = new AdbRequest()
                {

                };

                return request;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured when trying to establish endpoints: " + ex.StackTrace); }
            catch (Exception ex)
            {
                _logger.Println("A generic error occured when trying to establish endpoints: " + ex.StackTrace);
                _logger.Println("Cleaning usb connection data");
                USBStaticDetails.CleanConnection();
                throw;
            }
            return default;
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

        /// <summary>
        /// > adb install -r /path/to/your-filename.apk
        /// 
        /// > am start -a "android.intent.action.MAIN" -c "android.intent.category.LAUNCHER" -n "com.febris.mobileserver/crc64fdf741cc05fefc1a.MainActivity"
        /// > Starting: Intent { act=android.intent.action.MAIN cat =[android.intent.category.LAUNCHER] cmp=com.febris.mobileserver/crc64fdf741cc05fefc1a.MainActivity}
        /// </summary>
        /// <returns></returns>
        public async Task PushAndInstallRequest(string localPath, string remotePath)
        {
            try
            {
                #region path variables
                ///Suggested by adb
                //remotePath = "/data/local/tmp/";
                remotePath = "/data/local/tmp/com.FebrisCompanionApplication/";



                //remotePath = "/sdcard/0/com.FebrisCompanionApplication/";
                //remotePath = "storage/self/primary/android/data/";                
                //remotePath = Android.OS.Environment.StorageDirectory.to;
                //remotePath = "/storage/emulated/0/com.FebrisCompanionApplication/";
                //remotePath = "/data/local/tmp/com.FebrisCompanionApplication/";
                //remotePath = "/data/local/tmp/com.FebrisCompanionApplication/" + Path.GetFileName(localPath);
                //remotePath ="/data/local/tmp/%s";
                //remotePath = "/sdcard/tmp/%s";
                //remotePath = "sdcard/tmp/%s";
                //remotePath = "sdcard/test.txt";
                //remotePath = "/sdcard/test.txt";
                //remotePath = "/sdcard/test";
                //remotePath = "~/sdcard/";
                //remotePath = "sdcard/";
                //remotePath = "sdcard/";
                //remotePath = "/sdcard/Download";
                //remotePath = "sdcard/Download";
                //remotePath = "sdcard/Download/";
                //remotePath = "sdcard/Download/test.txt";
                //remotePath = "/sdcard/Download/test.txt";
                //remotePath = "sdcard/Download/";
                #endregion
                bool pushed = false;
                IEnumerable<string> fileList = System.IO.Directory.GetFiles(localPath, "*.apk", System.IO.SearchOption.AllDirectories);
                for (var i = 0; i <= (fileList.Count() - 1); i++)
                {
                    pushed = PushRequest(fileList.ElementAt(i), remotePath, i + 1, fileList.Count()).IsCompletedSuccessfully;
                }

                //foreach (var i in fileList)
                //{
                //    pushed = PushRequest(i, remotePath).IsCompletedSuccessfully;
                //}
                if (pushed)
                {
                    PairingPageStatusHelper.GenericMessage("Installing Application");
                    foreach (var i in fileList)
                    {
                        bool installed = InstallRequest(i, remotePath).Result;
                        if (installed)
                        {
                            PairingPageStatusHelper.GenericMessage("Install Complete");
                            break;
                        }
                        else if (i == fileList.Last())
                        {
                            PairingPageStatusHelper.GenericMessage("Install Failed");
                        }
                    }
                }
                else
                {
                    PairingPageStatusHelper.GenericMessage("Application not pushed to device");
                }
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in USBRequests InstallRequest: " + ex.StackTrace);
                PairingPageStatusHelper.GenericMessage("Error Occured in USB Connection, Please unplug cable from both ends and try again");
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in USBRequests InstallRequest: " + ex.StackTrace);
                PairingPageStatusHelper.GenericMessage("Error Occured in USB Connection, Please unplug cable from both ends and try again");
            }
        }

        /// <summary>
        /// Installs the application onto the device in the temp file. 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> InstallRequest(string localPath, string remotePath)
        {
            try
            {
                #region variables                
                if (localPath.Contains("signed-apks"))
                {
                    remotePath = remotePath + "signed-apks/";
                }
                else if (localPath.Contains("abi-apks"))
                {
                    remotePath = remotePath + "abi-apks/";
                }
                string extendedremotePath = remotePath + Path.GetFileName(localPath);
                string shellString = string.Empty;
                #endregion
                if (USBStaticDetails._adbConnection._Connected)
                {
                    try
                    {
                        _logger.Println("----------------------------------------Install shell command starting----------------------------------------");
                        shellString = "shell:pm install -r " + extendedremotePath;
                        AdbStream stream = USBStaticDetails._adbConnection.Open(shellString);
                        _logger.Println("----------------------------------------Install shell command Complete----------------------------------------");

                        while (!stream._IsClosed)
                        {
                            byte[] response = stream.ReadLast().Result;
                            var responseConversion = Helpers.HexDecoder(Helpers.ByteArrayToString(response));
                            _logger.Println("Install Response:" + responseConversion);

                            if (responseConversion.StartsWith("Success"))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    catch (AndroidException ex)
                    {
                        _logger.Println("An Android error occured in USBRequests InstallRequest Unclosed Stream: " + ex.StackTrace);
                        throw;
                    }
                    catch (System.Exception ex)
                    {
                        _logger.Println("A generic error occured in USBRequests InstallRequest Unclosed Stream: " + ex.StackTrace);
                        throw;
                    }

                }
                else
                {
                    //add note saying the stream is closed or whatever
                    return false;
                }

            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in USBRequests InstallRequest: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in USBRequests InstallRequest: " + ex.StackTrace);
                throw;
            }
            return false;
        }

        /// <summary>
        /// push request       
        ///        
        /// Sends files from localpath to remote path on android device
        /// </summary>
        /// <returns></returns>               
        /// 
        #region Current
        public async Task<bool> PushRequest(string localPath, string remotePath, int currentFileIndex, int TotalFileIndex)
        {
            bool output = false;
            #region java version
            /************************************************                
            AdbStream stream = adbConnection.open("sync:");

            String sendId = "SEND";
            String mode = ",33206";
            int length = (remotePath + mode).length();
            stream.write(ByteUtils.concat(sendId.getBytes(), ByteUtils.intToByteArray(length)));


            stream.write(remotePath.getBytes());


            stream.write(mode.getBytes());

            byte[] buff = new byte[adbConnection.getMaxData()];
            InputStream is = new FileInputStream(local);

            long sent = 0;
            long total = local.length();
            int lastProgress = 0;
            while (true) {
                int read = is.read(buff);
                if (read < 0) {
                    break;
                }

                stream.write(ByteUtils.concat("DATA".getBytes(), ByteUtils.intToByteArray(read)));

                if (read == buff.length) {
                    stream.write(buff);
                } else {
                    byte[] tmp = new byte[read];
                    System.arraycopy(buff, 0, tmp, 0, read);
                    stream.write(tmp);
                }

                sent += read;

                final int progress = (int)(sent * 100 / total);
                if (lastProgress != progress) {
                    handler.sendMessage(handler.obtainMessage(Message.INSTALLING_PROGRESS, Message.PUSH_PART, progress));
                    lastProgress = progress;
                }

            }

            stream.write(ByteUtils.concat("DONE".getBytes(), ByteUtils.intToByteArray((int) System.currentTimeMillis())));

            byte[] res = stream.read();
            // TODO: test if res contains "OKEY" or "FAIL"
            Log.d(Const.TAG, new String(res));

            stream.write(ByteUtils.concat("QUIT".getBytes(), ByteUtils.intToByteArray(0)));

            **************************************************/
            #endregion
            try
            {
                PairingPageStatusHelper.GenericMessage("Starting upload of File " + currentFileIndex + " of " + TotalFileIndex);
                #region variables                
                if (localPath.Contains("signed-apks"))
                {
                    remotePath = remotePath + "signed-apks/";
                }
                else if (localPath.Contains("abi-apks"))
                {
                    remotePath = remotePath + "abi-apks/";
                }
                string extendedremotePath = remotePath + Path.GetFileName(localPath);
                //string mode = AdbProtocol._DEFAULT_PUSH_MODE.ToString();
                //string mode = AdbProtocol.OGAlt_mode.ToString();
                //int mode = AdbProtocol._DEFAULT_PUSH_MODE;
                int mode = AdbProtocol.OGAlt_mode;
                //int mode = AdbProtocol.Alt_mode;
                //int mode = AdbProtocol.Alt2_mode;
                //int mode = AdbProtocol._Alt3_MODE;
                //int mode = AdbProtocol._Alt4_MODE;
                //int mode = AdbProtocol._Alt5_MODE;
                string modeString = "," + mode;
                byte[] endArray = Helpers.CharToBytes(char.MinValue);
                #endregion

                #region Open Stream                
                _logger.Println("----------------------------------------Opening Sync Stream-----------------------------------");
                AdbStream stream = USBStaticDetails._adbConnection.Open("sync:");
                _logger.Println("----------------------------------------Sync Stream Opened-----------------------------------");
                #endregion

                #region Expected Length  
                /************************************************                
                String sendId = "SEND";
                String mode = ",33206";
                int length = (remotePath + mode).length();
                stream.write(ByteUtils.concat(sendId.getBytes(), ByteUtils.intToByteArray(length)));
                **************************************************/
                _logger.Println("----------------------------------------Writing Expected Length-----------------------------------");
                string lengthString = extendedremotePath + modeString;
                int length = lengthString.Length;
                byte[] sendIdArray = AdbProtocol._SendIdArray;
                byte[] lengthArray = Helpers.IntToBytes(length);
                byte[] expectedLengthPackage = Helpers.ArrayCombiner(sendIdArray, lengthArray);
                stream.Write(expectedLengthPackage);
                _logger.Println("----------------------------------------Expected Length Written-----------------------------------");
                lock (stream)
                {
                    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
                }
                #endregion

                #region Base Destination and mode
                /************************************************   
               stream.write(remotePath.getBytes());
               stream.write(mode.getBytes());
               **************************************************/
                _logger.Println("----------------------------------------Writing Remote Path and Mode Array-----------------------------------");
                byte[] remotePathArray = Helpers.StringToBytes(extendedremotePath);
                byte[] modestringArray = Helpers.StringToBytes(modeString);
                byte[] destinationAndModeArray = Helpers.ArrayCombiner(remotePathArray, modestringArray);
                stream.Write(destinationAndModeArray);
                //stream.Write(remotePathArray);
                _logger.Println("----------------------------------------Remote Path and Mode Array Written-----------------------------------");
                lock (stream)
                {
                    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
                }
                #endregion

                #region Bulk Data Loop  
                /************************************************                           
                byte[] buff = new byte[adbConnection.getMaxData()];
                InputStream is = new FileInputStream(local);

                long sent = 0;
                long total = local.length();
                int lastProgress = 0;
                while (true) {
                    int read = is.read(buff);
                    if (read < 0) {
                        break;
                    }

                    stream.write(ByteUtils.concat("DATA".getBytes(), ByteUtils.intToByteArray(read)));

                    if (read == buff.length) {
                        stream.write(buff);
                    } else {
                        byte[] tmp = new byte[read];
                        System.arraycopy(buff, 0, tmp, 0, read);
                        stream.write(tmp);
                    }

                    sent += read;

                }
                **************************************************/
                #region variables                
                long sent = 0;
                long total = 0;
                //total = CountFileBytes(localPath, total);
                FileInfo fileInfo = new FileInfo(localPath);
                total = fileInfo.Length;
                FileStream fileStream = File.OpenRead(localPath);
                #endregion                
                #region combined 
                ///Combined
                int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
                maxData -= 9;
                _logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
                while (true)
                {
                    byte[] buffer = new byte[maxData];
                    int read = fileStream.Read(buffer);
                    if (read <= 0)
                    {
                        break;
                    }

                    //byte[] headerbuffer = new byte[8];
                    byte[] headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));

                    byte[] dataPackage = new byte[read + headerbuffer.Length];
                    if (read == maxData)
                    {
                        ///Combined
                        dataPackage = Helpers.ArrayCombiner(headerbuffer, buffer);
                    }
                    else
                    {
                        ///Combined
                        byte[] tmp = new byte[read];
                        System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
                        //dataPackage = new byte[headerbuffer.Length + read];
                        dataPackage = Helpers.ArrayCombiner(headerbuffer, tmp);
                    }
                    stream.Write(dataPackage);

                    sent += read;
                    int progress = (int)(sent * 100 / total);
                    //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
                    PairingPageStatusHelper.GenericMessage("File " + currentFileIndex + " of " + TotalFileIndex + ": " + progress.ToString() + "% Upload Complete");

                }
                _logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
                #endregion
                #endregion

                #region cleanup
                /************************************************ 
                stream.write(ByteUtils.concat("DONE".getBytes(), ByteUtils.intToByteArray((int) System.currentTimeMillis())));

                byte[] res = stream.read();
                // TODO: test if res contains "OKEY" or "FAIL"
                Log.d(Const.TAG, new String(res));

                stream.write(ByteUtils.concat("QUIT".getBytes(), ByteUtils.intToByteArray(0)));

                **************************************************/

                _logger.Println("----------------------------------------Sending Done-----------------------------------");

                byte[] done = AdbProtocol._DoneIdArray;
                byte[] time = Helpers.IntToBytes(Helpers.CurrentTimeMillis());
                byte[] donePayload = Helpers.ArrayCombiner(done, time);
                stream.Write(donePayload);
                _logger.Println("----------------------------------------Done Sent-----------------------------------");
                lock (stream)
                {
                    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
                    output = true;
                }

                _logger.Println("----------------------------------------Sending Quit-----------------------------------");
                byte[] quit = Helpers.StringToBytes("QUIT");
                //byte[] zero = Helpers.IntToBytes(0);
                byte[] zero = Helpers.CharToBytes(char.MinValue);
                byte[] quitPayload = Helpers.ArrayCombiner(quit, zero);
                stream.Write(quitPayload);
                _logger.Println("----------------------------------------Quit Sent-----------------------------------");



                //lock (stream)
                //{
                //    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
                //}
                ////_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
                /************************************************                
                25. We send CLSE to device
                26. Device sends us CLSE
                **************************************************/
                //if (!stream._IsClosed)
                //{
                //    stream.Close();
                //}
                //stream.Dispose();
                _logger.Println("----------------------------------------Complete-----------------------------------");
                #endregion

                return output;
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in USBRequests InstallRequest: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in USBRequests InstallRequest: " + ex.StackTrace);
                throw;
            }
        }
        #endregion

        #region older
        //public async Task PushRequest(string localPath, string remotePath)
        //{
        //    #region java version
        //    /************************************************                
        //    AdbStream stream = adbConnection.open("sync:");

        //    String sendId = "SEND";
        //    String mode = ",33206";
        //    int length = (remotePath + mode).length();
        //    stream.write(ByteUtils.concat(sendId.getBytes(), ByteUtils.intToByteArray(length)));


        //    stream.write(remotePath.getBytes());


        //    stream.write(mode.getBytes());

        //    byte[] buff = new byte[adbConnection.getMaxData()];
        //    InputStream is = new FileInputStream(local);

        //    long sent = 0;
        //    long total = local.length();
        //    int lastProgress = 0;
        //    while (true) {
        //        int read = is.read(buff);
        //        if (read < 0) {
        //            break;
        //        }

        //        stream.write(ByteUtils.concat("DATA".getBytes(), ByteUtils.intToByteArray(read)));

        //        if (read == buff.length) {
        //            stream.write(buff);
        //        } else {
        //            byte[] tmp = new byte[read];
        //            System.arraycopy(buff, 0, tmp, 0, read);
        //            stream.write(tmp);
        //        }

        //        sent += read;

        //        final int progress = (int)(sent * 100 / total);
        //        if (lastProgress != progress) {
        //            handler.sendMessage(handler.obtainMessage(Message.INSTALLING_PROGRESS, Message.PUSH_PART, progress));
        //            lastProgress = progress;
        //        }

        //    }

        //    stream.write(ByteUtils.concat("DONE".getBytes(), ByteUtils.intToByteArray((int) System.currentTimeMillis())));

        //    byte[] res = stream.read();
        //    // TODO: test if res contains "OKEY" or "FAIL"
        //    Log.d(Const.TAG, new String(res));

        //    stream.write(ByteUtils.concat("QUIT".getBytes(), ByteUtils.intToByteArray(0)));

        //    **************************************************/
        //    #endregion
        //    try
        //    {
        //        #region variables                
        //        if (localPath.Contains("signed-apks"))
        //        {
        //            remotePath = remotePath + "signed-apks/";
        //        }
        //        else if (localPath.Contains("abi-apks"))
        //        {
        //            remotePath = remotePath + "abi-apks/";
        //        }
        //        string extendedremotePath = remotePath + Path.GetFileName(localPath);
        //        //string mode = AdbProtocol._DEFAULT_PUSH_MODE.ToString();
        //        //string mode = AdbProtocol.OGAlt_mode.ToString();
        //        //int mode = AdbProtocol._DEFAULT_PUSH_MODE;
        //        int mode = AdbProtocol.OGAlt_mode;
        //        //int mode = AdbProtocol.Alt_mode;
        //        //int mode = AdbProtocol.Alt2_mode;
        //        //int mode = AdbProtocol._Alt3_MODE;
        //        //int mode = AdbProtocol._Alt4_MODE;
        //        //int mode = AdbProtocol._Alt5_MODE;
        //        string modeString = "," + mode;
        //        byte[] endArray = Helpers.CharToBytes(char.MinValue);
        //        #endregion

        //        #region Open Stream                
        //        _logger.Println("----------------------------------------Opening Sync Stream-----------------------------------");
        //        AdbStream stream = USBStaticDetails._adbConnection.Open("sync:");
        //        _logger.Println("----------------------------------------Sync Stream Opened-----------------------------------");
        //        #endregion

        //        #region Expected Length  
        //        /************************************************                
        //        String sendId = "SEND";
        //        String mode = ",33206";
        //        int length = (remotePath + mode).length();
        //        stream.write(ByteUtils.concat(sendId.getBytes(), ByteUtils.intToByteArray(length)));
        //        **************************************************/
        //        _logger.Println("----------------------------------------Writing Expected Length-----------------------------------");
        //        string lengthString = extendedremotePath + modeString;
        //        int length = lengthString.Length;
        //        byte[] sendIdArray = AdbProtocol._SendIdArray;
        //        byte[] lengthArray = Helpers.IntToBytes(length);
        //        byte[] expectedLengthPackage = Helpers.ArrayCombiner(sendIdArray, lengthArray);
        //        stream.Write(expectedLengthPackage);
        //        _logger.Println("----------------------------------------Expected Length Written-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        #endregion

        //        #region Base Destination and mode
        //        /************************************************   
        //       stream.write(remotePath.getBytes());
        //       stream.write(mode.getBytes());
        //       **************************************************/
        //        _logger.Println("----------------------------------------Writing Remote Path and Mode Array-----------------------------------");
        //        byte[] remotePathArray = Helpers.StringToBytes(extendedremotePath);
        //        byte[] modestringArray = Helpers.StringToBytes(modeString);
        //        byte[] destinationAndModeArray = Helpers.ArrayCombiner(remotePathArray, modestringArray);
        //        stream.Write(destinationAndModeArray);
        //        //stream.Write(remotePathArray);
        //        _logger.Println("----------------------------------------Remote Path and Mode Array Written-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        #endregion

        //        #region Bulk Data Loop  
        //        /************************************************                           
        //        byte[] buff = new byte[adbConnection.getMaxData()];
        //        InputStream is = new FileInputStream(local);

        //        long sent = 0;
        //        long total = local.length();
        //        int lastProgress = 0;
        //        while (true) {
        //            int read = is.read(buff);
        //            if (read < 0) {
        //                break;
        //            }

        //            stream.write(ByteUtils.concat("DATA".getBytes(), ByteUtils.intToByteArray(read)));

        //            if (read == buff.length) {
        //                stream.write(buff);
        //            } else {
        //                byte[] tmp = new byte[read];
        //                System.arraycopy(buff, 0, tmp, 0, read);
        //                stream.write(tmp);
        //            }

        //            sent += read;

        //        }
        //        **************************************************/
        //        #region variables                
        //        long sent = 0;
        //        long total = 0;
        //        //total = CountFileBytes(localPath, total);
        //        FileInfo fileInfo = new FileInfo(localPath);
        //        total = fileInfo.Length;
        //        FileStream fileStream = File.OpenRead(localPath);
        //        #endregion                
        //        #region combined 
        //        ///Combined
        //        int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        maxData -= 9;
        //        _logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        while (true)
        //        {
        //            byte[] buffer = new byte[maxData];
        //            int read = fileStream.Read(buffer);
        //            if (read <= 0)
        //            {
        //                break;
        //            }

        //            //byte[] headerbuffer = new byte[8];
        //            byte[] headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));

        //            byte[] dataPackage = new byte[read + headerbuffer.Length];
        //            if (read == maxData)
        //            {
        //                ///Combined
        //                dataPackage = Helpers.ArrayCombiner(headerbuffer, buffer);
        //            }
        //            else
        //            {
        //                ///Combined
        //                byte[] tmp = new byte[read];
        //                System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //                //dataPackage = new byte[headerbuffer.Length + read];
        //                dataPackage = Helpers.ArrayCombiner(headerbuffer, tmp);
        //            }
        //            stream.Write(dataPackage);

        //            sent += read;
        //            int progress = (int)(sent * 100 / total);
        //            //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //            PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");

        //        }
        //        _logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #endregion

        //        #region cleanup
        //        /************************************************ 
        //        stream.write(ByteUtils.concat("DONE".getBytes(), ByteUtils.intToByteArray((int) System.currentTimeMillis())));

        //        byte[] res = stream.read();
        //        // TODO: test if res contains "OKEY" or "FAIL"
        //        Log.d(Const.TAG, new String(res));

        //        stream.write(ByteUtils.concat("QUIT".getBytes(), ByteUtils.intToByteArray(0)));

        //        **************************************************/

        //        _logger.Println("----------------------------------------Sending Done-----------------------------------");

        //        byte[] done = AdbProtocol._DoneIdArray;
        //        byte[] time = Helpers.IntToBytes(Helpers.CurrentTimeMillis());
        //        byte[] donePayload = Helpers.ArrayCombiner(done, time);
        //        stream.Write(donePayload);
        //        _logger.Println("----------------------------------------Done Sent-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }

        //        _logger.Println("----------------------------------------Sending Quit-----------------------------------");
        //        byte[] quit = Helpers.StringToBytes("QUIT");
        //        //byte[] zero = Helpers.IntToBytes(0);
        //        byte[] zero = Helpers.CharToBytes(char.MinValue);
        //        byte[] quitPayload = Helpers.ArrayCombiner(quit, zero);
        //        stream.Write(quitPayload);
        //        _logger.Println("----------------------------------------Quit Sent-----------------------------------");
        //        //lock (stream)
        //        //{
        //        //    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        //}
        //        ////_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        /************************************************                
        //        25. We send CLSE to device
        //        26. Device sends us CLSE
        //        **************************************************/
        //        //if (!stream._IsClosed)
        //        //{
        //        //    stream.Close();
        //        //}
        //        //stream.Dispose();
        //        _logger.Println("----------------------------------------Complete-----------------------------------");
        //        #endregion
        //    }
        //    catch (AndroidException ex)
        //    {
        //        _logger.Println("An Android error occured in USBRequests InstallRequest: " + ex.StackTrace);
        //        throw;
        //    }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in USBRequests InstallRequest: " + ex.StackTrace);
        //        throw;
        //    }
        //}
        #endregion

        #region These do not work
        //public async Task PushRequest1(string localPath, string remotePath)
        //{

        //    try
        //    {
        //        #region path variables
        //        //remotePath = "/sdcard/0/com.FebrisCompanionApplication/";
        //        //remotePath = "storage/self/primary/android/data/";
        //        //remotePath = "data/local/tmp/";
        //        //remotePath = "/data/local/tmp/";
        //        //remotePath = Android.OS.Environment.StorageDirectory.to;
        //        remotePath = "/storage/emulated/0/com.FebrisCompanionApplication/";
        //        //remotePath = "/data/local/tmp/com.FebrisCompanionApplication/";
        //        //remotePath = "/data/local/tmp/com.FebrisCompanionApplication/" + Path.GetFileName(localPath);
        //        //remotePath ="/data/local/tmp/%s";
        //        //remotePath = "/sdcard/tmp/%s";
        //        //remotePath = "sdcard/tmp/%s";
        //        //remotePath = "sdcard/test.txt";
        //        //remotePath = "/sdcard/test.txt";
        //        //remotePath = "/sdcard/test";
        //        //remotePath = "~/sdcard/";
        //        //remotePath = "sdcard/";
        //        //remotePath = "sdcard/";
        //        //remotePath = "/sdcard/Download";
        //        //remotePath = "sdcard/Download";
        //        //remotePath = "sdcard/Download/";
        //        //remotePath = "sdcard/Download/test.txt";
        //        //remotePath = "/sdcard/Download/test.txt";
        //        //remotePath = "sdcard/Download/";

        //        if (localPath.Contains("signed-apks"))
        //        {
        //            remotePath = remotePath + "signed-apks/";
        //        }
        //        else if (localPath.Contains("abi-apks"))
        //        {
        //            remotePath = remotePath + "abi-apks/";
        //        }

        //        string extendedremotePath = remotePath + Path.GetFileName(localPath);
        //        //string extendedremotePath = remotePath + "test.txt";
        //        //string extendedremotePath = localPath;
        //        //
        //        #endregion

        //        #region mode variables
        //        ///st_mode
        //        ///struct_stat 
        //        ///mode_t
        //        ///Try: 42770
        //        ///
        //        //S_ISBLK();
        //        //mode_t
        //        //ModeT
        //        //string mode = FileAttributes.IntegrityStream.ToString();

        //        //string mode = AttributeTargets.
        //        //string mode = "0004777";
        //        //string mode = "04777"; 0777
        //        //string mode = 0777.ToString();
        //        //int mode = AdbProtocol._PushFileMode_mode; // in dec 33188
        //        //int mode = AdbProtocol._PushOGAlt_mode; // in dec 33206
        //        //int mode = 33279;//This is the same as 0100777
        //        //string mode = "0755";
        //        //string mode = "0751";
        //        //string mode = "0666";
        //        //string mode = "42770";
        //        //string mode = "42770";
        //        //string mode = AdbProtocol._DEFAULT_PUSH_MODE.ToString();
        //        //string mode = AdbProtocol.OGAlt_mode.ToString();
        //        int mode = AdbProtocol.OGAlt_mode;
        //        //int mode = AdbProtocol.Alt_mode;
        //        //int mode = AdbProtocol.Alt2_mode;
        //        //int mode = AdbProtocol._Alt3_MODE;
        //        //int mode = AdbProtocol._Alt4_MODE;
        //        //int mode = AdbProtocol._Alt5_MODE;

        //        byte[] endArray = Helpers.CharToBytes(char.MinValue);
        //        #endregion

        //        byte[] response;
        //        bool complete = false;



        //        #region Open Stream
        //        /************************************************
        //        1. We send OPEN message to device
        //        2. We send sync: to the device sync: starts a SYNC service  
        //        3. Device sends us OKAY
        //        **************************************************/
        //        _logger.Println("----------------------------------------Opening Sync Stream-----------------------------------");
        //        //AdbStream stream = USBStaticDetails._adbConnection.Open("sync: pm");
        //        AdbStream stream = USBStaticDetails._adbConnection.Open("sync:");
        //        //AdbStream stream = USBStaticDetails._Host_To_Device_Stream;
        //        _logger.Println("----------------------------------------Sync Stream Opened-----------------------------------");
        //        #endregion

        //        /************************************************                
        //       4. We send WRTE message to device
        //       5. We send STAT to the device
        //       6. Device sends us OKAY
        //       **************************************************/
        //        #region Expected Length   
        //        _logger.Println("----------------------------------------Writing Expected Length-----------------------------------");
        //        //string lengthString = string.Join(",", extendedremotePath, mode);
        //        string lengthString = extendedremotePath + "," + mode;
        //        //string lengthString = string.Join(",", remotePath, mode);
        //        int length = lengthString.Length;
        //        byte[] sendIdArray = AdbProtocol._SendIdArray;
        //        byte[] lengthArray = Helpers.IntToBytes(length);
        //        byte[] initalPackage = Helpers.ArrayCombiner(sendIdArray, lengthArray);
        //        stream.Write(initalPackage);
        //        _logger.Println("----------------------------------------Expected Length Written-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        //_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        #endregion

        //        /************************************************                
        //        7. We send WRTE to device
        //        8. We send the destination of where we want to push a file to, sdcard/
        //        9. Device sends us OKAY
        //        **************************************************/
        //        #region Base Destination
        //        _logger.Println("----------------------------------------Writing Remote Path Array-----------------------------------");
        //        //byte[] remotePathArray = Helpers.StringToBytes(remotePath);
        //        byte[] remotePathArray = Helpers.StringToBytes(extendedremotePath);//,char.MinValue);
        //        stream.Write(remotePathArray);
        //        _logger.Println("----------------------------------------Remote Path Array Written-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        //_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        #endregion

        //        #region apparently not needed
        //        /************************************************
        //        10. Device sends us WRTE
        //        11. Device sends us STAT + some info about the destination we're sending to sdcard/
        //        12. We send OKAY to device
        //        **************************************************/
        //        ///Need to be listening hear for incoming stuff                
        //        //response = stream.Read().Result;
        //        //_logger.Println("stat response: " + Helpers.ByteArrayToString(response));
        //        #endregion


        //        /************************************************                
        //       13. We send WRTE to device
        //       14. We send SEND to device, note that there is another 4 bytes in the data payload of 
        //           this packet which is the length of the file destination + name in characters plus the 
        //           ',mode' portion from 17. So if we're sending sdcard//testFile.txt,XXXXX we write SEND26. 
        //           The ADB protocol is full of inconsistencies, in case you hadn't already noticed.
        //       15. Device sends us OKAY

        //       This seems to be done earlier
        //       **************************************************/
        //        #region mode Package
        //        _logger.Println("----------------------------------------Writing Mode Array-----------------------------------");
        //        string modestring = string.Join(",", extendedremotePath, mode);
        //        byte[] modestringArray = Helpers.StringToBytes(modestring);//,char.MinValue);
        //        //byte[] modestringArray = Helpers.StringToBytes("," + mode);//,char.MinValue);
        //        //byte[] modeArray = Helpers.StringToBytes("," + mode);//,char.MinValue);
        //        byte[] modeArray = Helpers.ArrayCombiner(remotePathArray, modestringArray);
        //        //stream.Write(modestringArray);

        //        //byte[] extendedPathandCommaArray = Helpers.StringToBytes(extendedremotePath + ",");
        //        //byte[] modeArray = Helpers.ArrayCombiner(extendedPathandCommaArray, Helpers.IntToBytes(mode));


        //        //byte[] modeArray = Helpers.StringToBytes(lengthString);
        //        //byte[] modeArray = Helpers.StringToBytes(string.Join(",", extendedremotePath, mode));
        //        stream.Write(modeArray);

        //        _logger.Println("----------------------------------------Mode Array Written-----------------------------------");
        //        //lock (stream)
        //        //{
        //        //    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        //}
        //        //_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        #endregion

        //        #region Bulk Data Loop
        //        /************************************************               
        //        16. We send WRTE to device
        //        17. We send string about the file we're sending sdcard//testFile.txt,XXXXX,DATAnnnnTheFileData
        //            -   This string can be confusing at first glance, to clarify, the format is: full file path, 
        //                    the mode of the file in decimal (0644 becomes 33188), DATAnnnnTheFileData 
        //                    where nnnn is the size of the file sending, each n is one byte.
        //            -   If your file is larger than 64k bits you just need to keep sending WRTE with file data 
        //                    followed by another DATA nnnnFileData until you've sent all the file data.
        //            -   When we're sending the packet containing the last of the file data we append DONEnnnn 
        //                    to the end of the packet, where nnnn is the creation time we want the file to have on the device.
        //        **************************************************/

        //        #region variables

        //        ///Variables
        //        long sent = 0;
        //        long total = 0;
        //        //total = CountFileBytes(localPath, total);
        //        FileInfo fileInfo = new FileInfo(localPath);
        //        total = fileInfo.Length;
        //        FileStream fileStream = File.OpenRead(localPath);

        //        #endregion
        //        #region Weird Repeaded Header -- does not work in any of the configs set before                
        //        //int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        //maxData -= 8;
        //        //_logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        //while (true)
        //        //{
        //        //    byte[] buffer = new byte[maxData];
        //        //    int read = fileStream.Read(buffer);
        //        //    if (read <= 0)
        //        //    {
        //        //        break;
        //        //    }

        //        //    byte[] headerbuffer = new byte[8];
        //        //    //if (read != maxData)
        //        //    //{
        //        //    //    headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DoneIdArray, Helpers.IntToBytes(read));
        //        //    //}
        //        //    //else
        //        //    //{
        //        //    headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //        //    //}
        //        //    stream.Write(headerbuffer);

        //        //    byte[] dataPackage = new byte[read + 8];
        //        //    if (read == maxData)
        //        //    {
        //        //        ///Combined
        //        //        dataPackage = Helpers.ArrayCombiner(headerbuffer, buffer);
        //        //    }
        //        //    else
        //        //    {
        //        //        ///Combined
        //        //        byte[] tmp = new byte[read];
        //        //        System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //        //        //dataPackage = new byte[headerbuffer.Length + read];
        //        //        dataPackage = Helpers.ArrayCombiner(headerbuffer, tmp);
        //        //        dataPackage = tmp;
        //        //    }
        //        //    stream.Write(dataPackage);

        //        //    sent += read;
        //        //    int progress = (int)(sent * 100 / total);
        //        //    //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //        //    PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        //}
        //        //_logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #region combined 
        //        ///Combined
        //        //int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        //maxData -= 8;
        //        //_logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        //while (true)
        //        //{
        //        //    lock (this)
        //        //    {
        //        //        byte[] buffer = new byte[maxData];
        //        //        int read = fileStream.Read(buffer);
        //        //        if (read <= 0)
        //        //        {
        //        //            break;
        //        //        }

        //        //        //byte[] headerbuffer = new byte[8];
        //        //        byte[] headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));

        //        //        byte[] dataPackage = new byte[read + headerbuffer.Length];
        //        //        if (read == maxData)
        //        //        {
        //        //            ///Combined
        //        //            dataPackage = Helpers.ArrayCombiner(headerbuffer, buffer);
        //        //        }
        //        //        else
        //        //        {
        //        //            ///Combined
        //        //            byte[] tmp = new byte[read];
        //        //            System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //        //            //dataPackage = new byte[headerbuffer.Length + read];
        //        //            dataPackage = Helpers.ArrayCombiner(headerbuffer, tmp);
        //        //        }
        //        //        stream.Write(dataPackage);

        //        //        sent += read;
        //        //        int progress = (int)(sent * 100 / total);
        //        //        //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //        //        PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        //    }
        //        //}
        //        //_logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #region combined with different starter
        //        ///Combined


        //        //int maxDataO = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        //int maxData = maxDataO;
        //        ////maxData -= 8;
        //        //_logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        //while (true)
        //        //{
        //        //    if (sent == 0)
        //        //    {
        //        //        maxData -= length + 8;
        //        //    }
        //        //    else
        //        //    {
        //        //        maxData= maxDataO - 8;
        //        //    }

        //        //    lock (this)
        //        //    {                        
        //        //        byte[] buffer = new byte[maxData];
        //        //        int read = fileStream.Read(buffer);
        //        //        if (read <= 0)
        //        //        {
        //        //            break;
        //        //        }

        //        //        //byte[] headerbuffer = new byte[8];
        //        //        byte[] headerbuffer;// = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));

        //        //        if (sent == 0)
        //        //        {
        //        //            byte[] intermediateArray = Helpers.ArrayCombiner(remotePathArray, modestringArray);
        //        //            byte[] intermediateArray2 = Helpers.ArrayCombiner(intermediateArray, AdbProtocol._DataIdArray);
        //        //            headerbuffer = Helpers.ArrayCombiner(intermediateArray2, Helpers.IntToBytes(read));
        //        //        }
        //        //        else
        //        //        {
        //        //            headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //        //        }

        //        //        byte[] dataPackage = new byte[read + 8];
        //        //        if (read == maxData)
        //        //        {
        //        //            ///Combined
        //        //            dataPackage = Helpers.ArrayCombiner(headerbuffer, buffer);
        //        //        }
        //        //        else
        //        //        {
        //        //            ///Combined
        //        //            byte[] tmp = new byte[read];
        //        //            System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //        //            //dataPackage = new byte[headerbuffer.Length + read];
        //        //            dataPackage = Helpers.ArrayCombiner(headerbuffer, tmp);
        //        //        }
        //        //        stream.Write(dataPackage);

        //        //        sent += read;
        //        //        int progress = (int)(sent * 100 / total);
        //        //        //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //        //        PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        //    }
        //        //}
        //        //_logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #region independent
        //        ///Not Combined
        //        int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        _logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        while (true)
        //        {
        //            byte[] buffer = new byte[maxData];
        //            int read = fileStream.Read(buffer);
        //            if (read <= 0)
        //            {
        //                break;
        //            }

        //            byte[] headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //            stream.Write(headerbuffer);

        //            byte[] dataPackage = new byte[read];
        //            if (read == maxData)
        //            {
        //                dataPackage = buffer;
        //            }
        //            else
        //            {
        //                byte[] tmp = new byte[read];
        //                System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //                dataPackage = tmp;
        //            }
        //            ///Fails here "missing , in ID_SEND"**********************************************#############################################################
        //            stream.Write(dataPackage);

        //            sent += read;
        //            int progress = (int)(sent * 100 / total);
        //            //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //            PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        }
        //        _logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #region independent with different starter                
        //        //int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        //_logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        //while (true)
        //        //{
        //        //    byte[] buffer = new byte[maxData];
        //        //    int read = fileStream.Read(buffer);
        //        //    if (read <= 0)
        //        //    {
        //        //        break;
        //        //    }

        //        //    byte[] headerbuffer;// = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //        //    if (sent == 0)
        //        //    {                        
        //        //        byte[] intermediateArray = Helpers.ArrayCombiner(remotePathArray, modestringArray);
        //        //        byte[] intermediateArray2 = Helpers.ArrayCombiner(intermediateArray, AdbProtocol._DataIdArray);
        //        //        headerbuffer = Helpers.ArrayCombiner(intermediateArray2, Helpers.IntToBytes(read));
        //        //    }
        //        //    else
        //        //    {
        //        //        headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //        //    }
        //        //    stream.Write(headerbuffer);

        //        //    byte[] dataPackage = new byte[read];
        //        //    if (read == maxData)
        //        //    {
        //        //        dataPackage = buffer;
        //        //    }
        //        //    else
        //        //    {
        //        //        byte[] tmp = new byte[read];
        //        //        System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //        //        dataPackage = tmp;
        //        //    }
        //        //    stream.Write(dataPackage);

        //        //    sent += read;
        //        //    int progress = (int)(sent * 100 / total);
        //        //    //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //        //    PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        //}
        //        //_logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #endregion

        //        #region cleanup
        //        /************************************************               
        //           -   When we're sending the packet containing the last of the file data we append DONEnnnn 
        //                   to the end of the packet, where nnnn is the creation time we want the file to have on the device.

        //        18. Device sends us OKAY NOTE: here were assuming we just sent a data packet that also contained DONE, indicating we've sent all the data.               
        //        **************************************************/
        //        _logger.Println("----------------------------------------Sending Done-----------------------------------");
        //        //byte[] done = Helpers.StringToBytes("DONE");
        //        byte[] done = AdbProtocol._DoneIdArray;
        //        //int time00=fileInfo.CreationTimeUtc;
        //        //byte[] time = Helpers.IntToBytes((int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        //        byte[] time = Helpers.IntToBytes(Helpers.CurrentTimeMillis());
        //        //byte[] time = Helpers.IntToBytes((int)DateTime.UtcNow.Millisecond);
        //        byte[] doneTimeStamp = Helpers.ArrayCombiner(done, time);

        //        byte[] donePayload = Helpers.ArrayCombiner(doneTimeStamp, endArray);
        //        //byte[] donePayload = Helpers.ArrayCombiner(done, time);
        //        //donePayload = Helpers.ArrayCombiner(donePayload, Helpers.CharToBytes(char.MinValue));

        //        stream.Write(donePayload);
        //        _logger.Println("----------------------------------------Done Sent-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        ////_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");

        //        /************************************************                
        //        19. Device sends us WRTE
        //        20. Device sends us OKAY
        //        21. We send OKAY to device                
        //        **************************************************/
        //        stream.SendReady();
        //        //response = stream.Read().Result;

        //        /************************************************
        //        22. We send WRTE to device
        //        23. We send QUIT to device
        //        24. Device sends us OKAY               
        //        **************************************************/
        //        _logger.Println("----------------------------------------Sending Quit-----------------------------------");
        //        byte[] quit = Helpers.StringToBytes("QUIT");
        //        //byte[] zero = Helpers.IntToBytes(0);
        //        byte[] zero = Helpers.CharToBytes(char.MinValue);
        //        byte[] quitPayload = Helpers.ArrayCombiner(quit, zero);
        //        stream.Write(quitPayload);
        //        _logger.Println("----------------------------------------Quit Sent-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        ////_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        /************************************************                
        //        25. We send CLSE to device
        //        26. Device sends us CLSE
        //        **************************************************/
        //        if (!stream._IsClosed)
        //        {
        //            stream.Close();
        //        }
        //        //stream.Dispose();
        //        _logger.Println("----------------------------------------Complete-----------------------------------");
        //        #endregion
        //    }
        //    catch (AndroidException ex)
        //    {
        //        _logger.Println("An Android error occured in USBRequests InstallRequest: " + ex.StackTrace);
        //        throw;
        //    }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in USBRequests InstallRequest: " + ex.StackTrace);
        //        throw;
        //    }
        //}

        //public async Task PushRequest0(string localPath, string remotePath)
        //{

        //    try
        //    {
        //        #region path variables
        //        //remotePath = "/sdcard/0/com.FebrisCompanionApplication/";
        //        //remotePath = "storage/self/primary/android/data/";
        //        //remotePath = "data/local/tmp/";
        //        //remotePath = "/data/local/tmp/";
        //        //remotePath = Android.OS.Environment.StorageDirectory.to;
        //        remotePath = "/storage/emulated/0/com.FebrisCompanionApplication/";
        //        //remotePath = "/data/local/tmp/com.FebrisCompanionApplication/";
        //        //remotePath = "/data/local/tmp/com.FebrisCompanionApplication/" + Path.GetFileName(localPath);
        //        //remotePath ="/data/local/tmp/%s";
        //        //remotePath = "/sdcard/tmp/%s";
        //        //remotePath = "sdcard/tmp/%s";
        //        //remotePath = "sdcard/test.txt";
        //        //remotePath = "/sdcard/test.txt";
        //        //remotePath = "/sdcard/test";
        //        //remotePath = "~/sdcard/";
        //        //remotePath = "sdcard/";
        //        //remotePath = "sdcard/";
        //        //remotePath = "/sdcard/Download";
        //        //remotePath = "sdcard/Download";
        //        //remotePath = "sdcard/Download/";
        //        //remotePath = "sdcard/Download/test.txt";
        //        //remotePath = "/sdcard/Download/test.txt";
        //        //remotePath = "sdcard/Download/";

        //        if (localPath.Contains("signed-apks"))
        //        {
        //            remotePath = remotePath + "signed-apks/";
        //        }
        //        else if (localPath.Contains("abi-apks"))
        //        {
        //            remotePath = remotePath + "abi-apks/";
        //        }

        //        string extendedremotePath = remotePath + Path.GetFileName(localPath);
        //        //string extendedremotePath = remotePath + "test.txt";
        //        //string extendedremotePath = localPath;
        //        //
        //        #endregion

        //        #region mode variables
        //        ///st_mode
        //        ///struct_stat 
        //        ///mode_t
        //        ///Try: 42770
        //        ///
        //        //S_ISBLK();
        //        //mode_t
        //        //ModeT
        //        //string mode = FileAttributes.IntegrityStream.ToString();

        //        //string mode = AttributeTargets.
        //        //string mode = "0004777";
        //        //string mode = "04777"; 0777
        //        //string mode = 0777.ToString();
        //        //int mode = AdbProtocol._PushFileMode_mode; // in dec 33188
        //        //int mode = AdbProtocol._PushOGAlt_mode; // in dec 33206
        //        //int mode = 33279;//This is the same as 0100777
        //        //string mode = "0755";
        //        //string mode = "0751";
        //        //string mode = "0666";
        //        //string mode = "42770";
        //        //string mode = "42770";
        //        //string mode = AdbProtocol._DEFAULT_PUSH_MODE.ToString();
        //        //string mode = AdbProtocol.OGAlt_mode.ToString();
        //        int mode = AdbProtocol.OGAlt_mode;
        //        //int mode = AdbProtocol.Alt_mode;
        //        //int mode = AdbProtocol.Alt2_mode;
        //        //int mode = AdbProtocol._Alt3_MODE;
        //        //int mode = AdbProtocol._Alt4_MODE;
        //        //int mode = AdbProtocol._Alt5_MODE;

        //        byte[] endArray = Helpers.CharToBytes(char.MinValue);
        //        #endregion

        //        byte[] response;
        //        bool complete = false;



        //        #region Open Stream
        //        /************************************************
        //        1. We send OPEN message to device
        //        2. We send sync: to the device sync: starts a SYNC service  
        //        3. Device sends us OKAY
        //        **************************************************/
        //        _logger.Println("----------------------------------------Opening Sync Stream-----------------------------------");
        //        //AdbStream stream = USBStaticDetails._adbConnection.Open("sync: pm");
        //        AdbStream stream = USBStaticDetails._adbConnection.Open("sync:");
        //        //AdbStream stream = USBStaticDetails._Host_To_Device_Stream;
        //        _logger.Println("----------------------------------------Sync Stream Opened-----------------------------------");
        //        #endregion

        //        #region Inital Package   
        //        /************************************************                
        //        4. We send WRTE message to device
        //        5. We send STAT to the device
        //        6. Device sends us OKAY
        //        **************************************************/

        //        _logger.Println("----------------------------------------Writing Inital Package-----------------------------------");
        //        string lengthString = string.Join(",", extendedremotePath, mode);
        //        //string lengthString = string.Join(",", remotePath, mode);
        //        int length = lengthString.Length;
        //        byte[] sendIdArray = AdbProtocol._SendIdArray;
        //        byte[] lengthArray = Helpers.IntToBytes(length);
        //        byte[] initalPackage = Helpers.ArrayCombiner(sendIdArray, lengthArray);
        //        stream.Write(initalPackage);
        //        _logger.Println("----------------------------------------Inital Package Written-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        //_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        #endregion

        //        #region remote Package
        //        /************************************************                
        //        7. We send WRTE to device
        //        8. We send the destination of where we want to push a file to, sdcard/
        //        9. Device sends us OKAY
        //        **************************************************/

        //        //_logger.Println("----------------------------------------Writing Remote Path Array-----------------------------------");
        //        //byte[] remotePathArray = Helpers.StringToBytes(remotePath);
        //        byte[] remotePathArray = Helpers.StringToBytes(extendedremotePath);//,char.MinValue);
        //        //stream.Write(remotePathArray);                
        //        //_logger.Println("----------------------------------------Remote Path Array Written-----------------------------------");
        //        //lock (stream)
        //        //{
        //        //    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        //}
        //        //_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        #endregion

        //        #region apparently not needed
        //        /************************************************
        //        10. Device sends us WRTE
        //        11. Device sends us STAT + some info about the destination we're sending to sdcard/
        //        12. We send OKAY to device
        //        **************************************************/
        //        ///Need to be listening hear for incoming stuff                
        //        //response = stream.Read().Result;
        //        //_logger.Println("stat response: " + Helpers.ByteArrayToString(response));
        //        #endregion

        //        #region mode Package
        //        /************************************************                
        //        13. We send WRTE to device
        //        14. We send SEND to device, note that there is another 4 bytes in the data payload of 
        //            this packet which is the length of the file destination + name in characters plus the 
        //            ',mode' portion from 17. So if we're sending sdcard//testFile.txt,XXXXX we write SEND26. 
        //            The ADB protocol is full of inconsistencies, in case you hadn't already noticed.
        //        15. Device sends us OKAY

        //        This seems to be done earlier
        //        **************************************************/

        //        _logger.Println("----------------------------------------Writing Mode Array-----------------------------------");
        //        //string modestring = string.Join(",",extendedremotePath,mode);
        //        //byte[] modestringArray = Helpers.StringToBytes(modestring);//,char.MinValue);
        //        //byte[] modestringArray = Helpers.StringToBytes("," + mode);//,char.MinValue);
        //        //byte[] modeArray = Helpers.StringToBytes("," + mode);//,char.MinValue);
        //        //byte[] modeArray = Helpers.ArrayCombiner(remotePathArray, modestringArray);
        //        //stream.Write(modestringArray);

        //        byte[] extendedPathandCommaArray = Helpers.StringToBytes(extendedremotePath + ",");
        //        byte[] modeArray = Helpers.ArrayCombiner(extendedPathandCommaArray, Helpers.IntToBytes(mode));


        //        //byte[] modeArray = Helpers.StringToBytes(string.Join(",", extendedremotePath, mode));
        //        stream.Write(modeArray);

        //        _logger.Println("----------------------------------------Mode Array Written-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        //_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        #endregion

        //        #region Bulk Data Loop
        //        /************************************************               
        //        16. We send WRTE to device
        //        17. We send string about the file we're sending sdcard//testFile.txt,XXXXX,DATAnnnnTheFileData
        //            -   This string can be confusing at first glance, to clarify, the format is: full file path, 
        //                    the mode of the file in decimal (0644 becomes 33188), DATAnnnnTheFileData 
        //                    where nnnn is the size of the file sending, each n is one byte.
        //            -   If your file is larger than 64k bits you just need to keep sending WRTE with file data 
        //                    followed by another DATA nnnnFileData until you've sent all the file data.
        //            -   When we're sending the packet containing the last of the file data we append DONEnnnn 
        //                    to the end of the packet, where nnnn is the creation time we want the file to have on the device.
        //        **************************************************/

        //        #region variables

        //        //int headerBytes = ;
        //        //byte[] headerbuffer = new byte[8];
        //        //byte[] headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray,int);
        //        // byte[] buffer = new byte[maxData-8];
        //        //byte[] buffer = new byte[maxData];
        //        ///Variables
        //        long sent = 0;
        //        long total = 0;
        //        //total = CountFileBytes(localPath, total);
        //        FileInfo fileInfo = new FileInfo(localPath);
        //        total = fileInfo.Length;
        //        FileStream fileStream = File.OpenRead(localPath);

        //        #endregion
        //        #region Weird Repeaded Header -- does not work in any of the configs set before                
        //        //int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        //maxData -= 8;
        //        //_logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        //while (true)
        //        //{
        //        //    byte[] buffer = new byte[maxData];
        //        //    int read = fileStream.Read(buffer);
        //        //    if (read <= 0)
        //        //    {
        //        //        break;
        //        //    }

        //        //    byte[] headerbuffer = new byte[8];
        //        //    //if (read != maxData)
        //        //    //{
        //        //    //    headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DoneIdArray, Helpers.IntToBytes(read));
        //        //    //}
        //        //    //else
        //        //    //{
        //        //    headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //        //    //}
        //        //    stream.Write(headerbuffer);

        //        //    byte[] dataPackage = new byte[read + 8];
        //        //    if (read == maxData)
        //        //    {
        //        //        ///Combined
        //        //        dataPackage = Helpers.ArrayCombiner(headerbuffer, buffer);
        //        //    }
        //        //    else
        //        //    {
        //        //        ///Combined
        //        //        byte[] tmp = new byte[read];
        //        //        System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //        //        //dataPackage = new byte[headerbuffer.Length + read];
        //        //        dataPackage = Helpers.ArrayCombiner(headerbuffer, tmp);
        //        //        dataPackage = tmp;
        //        //    }
        //        //    stream.Write(dataPackage);

        //        //    sent += read;
        //        //    int progress = (int)(sent * 100 / total);
        //        //    //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //        //    PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        //}
        //        //_logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #region combined 
        //        ///Combined
        //        //int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        //maxData -= 8;
        //        //_logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        //while (true)
        //        //{
        //        //    lock (this)
        //        //    {
        //        //        byte[] buffer = new byte[maxData];
        //        //        int read = fileStream.Read(buffer);
        //        //        if (read <= 0)
        //        //        {
        //        //            break;
        //        //        }

        //        //        //byte[] headerbuffer = new byte[8];
        //        //        byte[] headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));

        //        //        byte[] dataPackage = new byte[read + headerbuffer.Length];
        //        //        if (read == maxData)
        //        //        {
        //        //            ///Combined
        //        //            dataPackage = Helpers.ArrayCombiner(headerbuffer, buffer);
        //        //        }
        //        //        else
        //        //        {
        //        //            ///Combined
        //        //            byte[] tmp = new byte[read];
        //        //            System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //        //            //dataPackage = new byte[headerbuffer.Length + read];
        //        //            dataPackage = Helpers.ArrayCombiner(headerbuffer, tmp);
        //        //        }
        //        //        stream.Write(dataPackage);

        //        //        sent += read;
        //        //        int progress = (int)(sent * 100 / total);
        //        //        //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //        //        PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        //    }
        //        //}
        //        //_logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #region combined with different starter
        //        ///Combined


        //        //int maxDataO = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        //int maxData = maxDataO;
        //        ////maxData -= 8;
        //        //_logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        //while (true)
        //        //{
        //        //    if (sent == 0)
        //        //    {
        //        //        maxData -= length + 8;
        //        //    }
        //        //    else
        //        //    {
        //        //        maxData= maxDataO - 8;
        //        //    }

        //        //    lock (this)
        //        //    {                        
        //        //        byte[] buffer = new byte[maxData];
        //        //        int read = fileStream.Read(buffer);
        //        //        if (read <= 0)
        //        //        {
        //        //            break;
        //        //        }

        //        //        //byte[] headerbuffer = new byte[8];
        //        //        byte[] headerbuffer;// = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));

        //        //        if (sent == 0)
        //        //        {
        //        //            byte[] intermediateArray = Helpers.ArrayCombiner(remotePathArray, modestringArray);
        //        //            byte[] intermediateArray2 = Helpers.ArrayCombiner(intermediateArray, AdbProtocol._DataIdArray);
        //        //            headerbuffer = Helpers.ArrayCombiner(intermediateArray2, Helpers.IntToBytes(read));
        //        //        }
        //        //        else
        //        //        {
        //        //            headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //        //        }

        //        //        byte[] dataPackage = new byte[read + 8];
        //        //        if (read == maxData)
        //        //        {
        //        //            ///Combined
        //        //            dataPackage = Helpers.ArrayCombiner(headerbuffer, buffer);
        //        //        }
        //        //        else
        //        //        {
        //        //            ///Combined
        //        //            byte[] tmp = new byte[read];
        //        //            System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //        //            //dataPackage = new byte[headerbuffer.Length + read];
        //        //            dataPackage = Helpers.ArrayCombiner(headerbuffer, tmp);
        //        //        }
        //        //        stream.Write(dataPackage);

        //        //        sent += read;
        //        //        int progress = (int)(sent * 100 / total);
        //        //        //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //        //        PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        //    }
        //        //}
        //        //_logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #region independent
        //        ///Not Combined
        //        int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        _logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        while (true)
        //        {
        //            byte[] buffer = new byte[maxData];
        //            int read = fileStream.Read(buffer);
        //            if (read <= 0)
        //            {
        //                break;
        //            }

        //            byte[] headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //            stream.Write(headerbuffer);

        //            byte[] dataPackage = new byte[read];
        //            if (read == maxData)
        //            {
        //                dataPackage = buffer;
        //            }
        //            else
        //            {
        //                byte[] tmp = new byte[read];
        //                System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //                dataPackage = tmp;
        //            }
        //            stream.Write(dataPackage);

        //            sent += read;
        //            int progress = (int)(sent * 100 / total);
        //            //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //            PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        }
        //        _logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #region independent with different starter                
        //        //int maxData = USBStaticDetails._adbConnection.GetMaxData().Result;
        //        //_logger.Println("----------------------------------------Writing Bulk Data-----------------------------------");
        //        //while (true)
        //        //{
        //        //    byte[] buffer = new byte[maxData];
        //        //    int read = fileStream.Read(buffer);
        //        //    if (read <= 0)
        //        //    {
        //        //        break;
        //        //    }

        //        //    byte[] headerbuffer;// = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //        //    if (sent == 0)
        //        //    {                        
        //        //        byte[] intermediateArray = Helpers.ArrayCombiner(remotePathArray, modestringArray);
        //        //        byte[] intermediateArray2 = Helpers.ArrayCombiner(intermediateArray, AdbProtocol._DataIdArray);
        //        //        headerbuffer = Helpers.ArrayCombiner(intermediateArray2, Helpers.IntToBytes(read));
        //        //    }
        //        //    else
        //        //    {
        //        //        headerbuffer = Helpers.ArrayCombiner(AdbProtocol._DataIdArray, Helpers.IntToBytes(read));
        //        //    }
        //        //    stream.Write(headerbuffer);

        //        //    byte[] dataPackage = new byte[read];
        //        //    if (read == maxData)
        //        //    {
        //        //        dataPackage = buffer;
        //        //    }
        //        //    else
        //        //    {
        //        //        byte[] tmp = new byte[read];
        //        //        System.Buffer.BlockCopy(buffer, 0, tmp, 0, read);
        //        //        dataPackage = tmp;
        //        //    }
        //        //    stream.Write(dataPackage);

        //        //    sent += read;
        //        //    int progress = (int)(sent * 100 / total);
        //        //    //_logger.Println("Progress:"+progress+"% - Bytes:"+sent);
        //        //    PairingPageStatusHelper.GenericMessage(progress.ToString() + "% Upload Complete");
        //        //}
        //        //_logger.Println("----------------------------------------Bulk Data Complete-----------------------------------");
        //        #endregion
        //        #endregion

        //        #region cleanup
        //        /************************************************               
        //           -   When we're sending the packet containing the last of the file data we append DONEnnnn 
        //                   to the end of the packet, where nnnn is the creation time we want the file to have on the device.

        //        18. Device sends us OKAY NOTE: here were assuming we just sent a data packet that also contained DONE, indicating we've sent all the data.               
        //        **************************************************/
        //        _logger.Println("----------------------------------------Sending Done-----------------------------------");
        //        //byte[] done = Helpers.StringToBytes("DONE");
        //        byte[] done = AdbProtocol._DoneIdArray;
        //        //int time00=fileInfo.CreationTimeUtc;
        //        //byte[] time = Helpers.IntToBytes((int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        //        byte[] time = Helpers.IntToBytes(Helpers.CurrentTimeMillis());
        //        //byte[] time = Helpers.IntToBytes((int)DateTime.UtcNow.Millisecond);
        //        byte[] doneTimeStamp = Helpers.ArrayCombiner(done, time);

        //        byte[] donePayload = Helpers.ArrayCombiner(doneTimeStamp, endArray);
        //        //byte[] donePayload = Helpers.ArrayCombiner(done, time);
        //        //donePayload = Helpers.ArrayCombiner(donePayload, Helpers.CharToBytes(char.MinValue));

        //        stream.Write(donePayload);
        //        _logger.Println("----------------------------------------Done Sent-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        ////_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");

        //        /************************************************                
        //        19. Device sends us WRTE
        //        20. Device sends us OKAY
        //        21. We send OKAY to device                
        //        **************************************************/
        //        stream.SendReady();
        //        //response = stream.Read().Result;

        //        /************************************************
        //        22. We send WRTE to device
        //        23. We send QUIT to device
        //        24. Device sends us OKAY               
        //        **************************************************/
        //        _logger.Println("----------------------------------------Sending Quit-----------------------------------");
        //        byte[] quit = Helpers.StringToBytes("QUIT");
        //        //byte[] zero = Helpers.IntToBytes(0);
        //        byte[] zero = Helpers.CharToBytes(char.MinValue);
        //        byte[] quitPayload = Helpers.ArrayCombiner(quit, zero);
        //        stream.Write(quitPayload);
        //        _logger.Println("----------------------------------------Quit Sent-----------------------------------");
        //        lock (stream)
        //        {
        //            System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
        //        }
        //        ////_logger.Println("----------------------------------------Okay Recieved------------------------------------------------");
        //        /************************************************                
        //        25. We send CLSE to device
        //        26. Device sends us CLSE
        //        **************************************************/
        //        if (!stream._IsClosed)
        //        {
        //            stream.Close();
        //        }
        //        //stream.Dispose();
        //        _logger.Println("----------------------------------------Complete-----------------------------------");
        //        #endregion
        //    }
        //    catch (AndroidException ex)
        //    {
        //        _logger.Println("An Android error occured in USBRequests InstallRequest: " + ex.StackTrace);
        //        throw;
        //    }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in USBRequests InstallRequest: " + ex.StackTrace);
        //        throw;
        //    }
        //}
        #endregion

        private static long CountFileBytes(string localPath, long total)
        {
            #region if File
            FileInfo fileInfo = new FileInfo(localPath);
            total = fileInfo.Length;
            #endregion

            #region if Directory
            //IEnumerable<string> fileList = System.IO.Directory.GetFiles(localPath, "*.apk*", System.IO.SearchOption.AllDirectories);
            //foreach (var i in fileList)
            //{
            //    FileInfo tempInfo = new FileInfo(i);
            //    total += tempInfo.Length;
            //}
            ////DirectoryInfo fileInfo = new DirectoryInfo(localPath);
            ////foreach (var i in fileInfo.EnumerateFiles())
            ////{
            ////    total += i.Length;
            ////}
            #endregion
            return total;
        }

        private bool WaitForOkayFromDevice(AdbStream stream)
        {
            try
            {
                lock (stream)
                {
                    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task FastBootRequest()
        {
            try
            {
                if (USBStaticDetails._adbConnection._Connected)
                {
                    try
                    {
                        ///Buiilding something like this
                        ///Starting: Intent { act=android.intent.action.MAIN cat=[android.intent.category.LAUNCHER] cmp=com.febris.mobileserver/crc64fdf741cc05fefc1a.MainActivity }
                        string prefix = await AdbHelpers.ParsePrefix(AdbPrefixEnum.Adb);
                        string shellprefix = await AdbHelpers.ParsePrefix(AdbPrefixEnum.RemoteShell);
                        string commandPrefix = await AdbHelpers.ParsePrefix(AdbPrefixEnum.ActivityManager);
                        string command = await AdbHelpers.ParseCommand(AdbCommandEnum.start);
                        string commandTag = await AdbHelpers.ParseTag(AdbTagEnum.SomeABasedOperator);
                        string path1 = "\"android.intent.action.MAIN\"";
                        string path1Tag = "-c";
                        string path2 = "\"android.intent.category.LAUNCHER\"";
                        string path2Tag = "-n";
                        string path3 = "\"com.febris.mobileserver/crc64fdf741cc05fefc1a.MainActivity\"";

                        string output = string.Join(" ", prefix, shellprefix, commandPrefix, command, commandTag, path1, path1Tag, path2, path2Tag, path3);


                    }
                    catch (AndroidException ex) { _logger.Println("An Android error occured in USBRequests InstallRequest Unclosed Stream: " + ex.StackTrace); }
                    catch (System.Exception ex)
                    {
                        _logger.Println("A generic error occured in USBRequests InstallRequest Unclosed Stream: " + ex.StackTrace);
                    }

                }
                else
                {
                    //add note saying the stream is closed or whatever
                }

            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in USBRequests InstallRequest: " + ex.StackTrace); }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in USBRequests InstallRequest: " + ex.StackTrace);
            }
        }


        public async Task RunRequest()
        {
            try
            {
                if (USBStaticDetails._adbConnection._Connected)
                {
                    try
                    {
                        ///Buiilding something like this
                        ///Starting: Intent { act=android.intent.action.MAIN cat=[android.intent.category.LAUNCHER] cmp=com.febris.mobileserver/crc64fdf741cc05fefc1a.MainActivity }
                        string prefix = await AdbHelpers.ParsePrefix(AdbPrefixEnum.Adb);
                        string shellprefix = await AdbHelpers.ParsePrefix(AdbPrefixEnum.RemoteShell);
                        string commandPrefix = await AdbHelpers.ParsePrefix(AdbPrefixEnum.ActivityManager);
                        string command = await AdbHelpers.ParseCommand(AdbCommandEnum.start);
                        string commandTag = await AdbHelpers.ParseTag(AdbTagEnum.SomeABasedOperator);
                        string path1 = "\"android.intent.action.MAIN\"";
                        string path1Tag = "-c";
                        string path2 = "\"android.intent.category.LAUNCHER\"";
                        string path2Tag = "-n";
                        string path3 = "\"com.febris.mobileserver/crc64fdf741cc05fefc1a.MainActivity\"";

                        string output = string.Join(" ", prefix, shellprefix, commandPrefix, command, commandTag, path1, path1Tag, path2, path2Tag, path3);


                    }
                    catch (AndroidException ex) { _logger.Println("An Android error occured in USBRequests InstallRequest Unclosed Stream: " + ex.StackTrace); }
                    catch (System.Exception ex)
                    {
                        _logger.Println("A generic error occured in USBRequests InstallRequest Unclosed Stream: " + ex.StackTrace);
                    }

                }
                else
                {
                    //add note saying the stream is closed or whatever
                }

            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in USBRequests InstallRequest: " + ex.StackTrace); }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in USBRequests InstallRequest: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Sends directory path to device level for proper file to be found. 
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public async Task PrepareUpload(string directoryPath)
        {
            try
            {
                UsbManager _usbManager = USBStaticDetails._usbManager;
                PairingPageStatusHelper.GenericMessage("Preparing to send software package to remote device");
                ///find correct apk file
                string apkPath = await FileManager.FindAPKApplication(directoryPath);
                //adbConnection = USBStaticDetails._adbConnection;
                ///There is an issue here THAT IS NOT A GOOD ROUTE. A ROUTE ALREADY EXISTS. USE THAT. MAY NEED A PASSED EVENT
                //string remotePath = await USBRequestCreator.NewestLocalPackageInstallRequestBuilder(input);
                //AdbStream stream = await adbConnection.Open("shell:pm install -r " + remotePath);
                //AdbStream stream = adbConnection.Open("shell:pm install -r " + remotePath);
                //await Task.Run(() => InstallRequest(stream));
                //await InstallRequest(apkPath, string.Empty);
                await PushAndInstallRequest(directoryPath, string.Empty);
            }
            catch { throw; }
        }


        public async Task PrepareInstall(string directoryPath)
        {
            try
            {
                UsbManager _usbManager = USBStaticDetails._usbManager;
                PairingPageStatusHelper.GenericMessage("Preparing to install software packages if present on remote device");
                ///find correct apk file
                //string apkPath = await FileManager.FindAPKApplication(directoryPath);
                //adbConnection = USBStaticDetails._adbConnection;
                ///There is an issue here THAT IS NOT A GOOD ROUTE. A ROUTE ALREADY EXISTS. USE THAT. MAY NEED A PASSED EVENT
                //string remotePath = await USBRequestCreator.NewestLocalPackageInstallRequestBuilder(input);
                //AdbStream stream = await adbConnection.Open("shell:pm install -r " + remotePath);
                //AdbStream stream = adbConnection.Open("shell:pm install -r " + remotePath);
                //await Task.Run(() => InstallRequest(stream));
                //await InstallRequest(apkPath, string.Empty);

                #region path variables
                string remotePath = string.Empty;
                remotePath = "/data/local/tmp/com.FebrisCompanionApplication/";
                #endregion
                //IEnumerable<string> fileList = System.IO.Directory.GetFiles(apkPath, "*.apk", System.IO.SearchOption.AllDirectories);
                IEnumerable<string> fileList = System.IO.Directory.GetFiles(directoryPath, "*.apk", System.IO.SearchOption.AllDirectories);
                PairingPageStatusHelper.GenericMessage("Installing Application");
                foreach (var i in fileList)
                {
                    bool installed = InstallRequest(i, remotePath).Result;
                    if (installed)
                    {
                        PairingPageStatusHelper.GenericMessage("Install Complete");
                        break;
                    }
                    else if (i == fileList.Last())
                    {
                        PairingPageStatusHelper.GenericMessage("Install Failed");
                    }
                }

                await InstallRequest(directoryPath, remotePath);
            }
            catch { throw; }
        }

        //public static T[] CopySlice<T>(this T[] source, int index, int length, bool padToLength = false)
        //{
        //    int n = length;
        //    T[] slice = null;

        //    if (source.Length < index + length)
        //    {
        //        n = source.Length - index;
        //        if (padToLength)
        //        {
        //            slice = new T[length];
        //        }
        //    }

        //    if (slice == null) slice = new T[n];
        //    Array.Copy(source, index, slice, 0, n);
        //    return slice;
        //}

        //public static IEnumerable<T[]> Slices<T>(this T[] source, int count, bool padToLength = false)
        //{
        //    for (var i = 0; i < source.Length; i += count)
        //        yield return source.CopySlice(i, count, padToLength);
        //}
    }
}