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
using Febris.AdbLibrary.Interface;
using Febris.MobileServerV3.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.Droid.Networking.USB
{    
    public enum AdbCommandEnum
    {
        install,
        sync,
        pull,
        push,
        killall,
        broadcast,
        Instrument,
        ToUri,
        ToIntentUri,
        //shell,
        start,
        //startservice,
        //force-stop,
        //kill,
        //kill-all,
        //log,

    }
    public enum AdbPrefixEnum
    {
        RemoteShell,
        PackageManager,
        Adb,
        ActivityManager,
        PackageManagerShell,
    }
    public enum AdbTagEnum
    {
        Update,
        sync,
        pull,
        push,
        WaitForLaunchToComplete,
        EnableDebugging,
        StartProfiler,
        ActivityCounter,
        ForceStop,
        TraceOpenGLFunctions,
        SpecifyUser,
        Test,
        InternalMemPackage,
        FastDeploy,
        Incremental,
        NoIncremental,
        SomeABasedOperator,
    }
    public class AdbHelpers
    {

        //    public void asyncRefreshAdbConnection(final UsbDevice device)
        //    {
        //        if (device != null)
        //        {
        //            new Thread() {
        //            @Override
        //            public void run()
        //            {
        //                final UsbInterface intf = findAdbInterface(device);
        //                try
        //                {
        //                    setAdbInterface(device, intf);
        //                }
        //                catch (Exception e)
        //                {
        //                    Log.w(Const.TAG, "setAdbInterface(device, intf) fail", e);
        //                }
        //            }
        //        }.start();
        //    }
        //}
        public static void RefreshAdbConnection(UsbDevice device)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbHelpers");
            try
            {
                bool set = false;
                if (device != null)
                {                    
                    UsbInterface intf = FindAdbInterface(device);
                    try
                    {
                        set = SetAdbInterface(device, intf);
                        Resources.LocalHardwareStaticDetails.StaticMainVM.HardwareVM.UsbButtonVisability = set;
                        PairingPageStatusHelper.GenericMessage(USBStaticDetails._device.DeviceName+" has connected successfully and is ready to accept commands");
                    }
                    catch (Exception e)
                    {
                        //Log.w(Const.TAG, "setAdbInterface(device, intf) fail", e);
                    }
                }
                else
                {
                    Resources.LocalHardwareStaticDetails.StaticMainVM.HardwareVM.UsbButtonVisability = set;
                }
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured at AdbHelpers RefreshAdbConnection " + ex.StackTrace);
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("An Java error occured at AdbHelpers RefreshAdbConnection : " + ex.StackTrace);
            }
            catch (Exception ex)
            {
                _logger.Println("An Android error occured at AdbHelpers RefreshAdbConnection " + ex.StackTrace);
            }
        }

        //private UsbInterface findAdbInterface(UsbDevice device)
        //{
        //    int count = device.getInterfaceCount();
        //    for (int i = 0; i < count; i++)
        //    {
        //        UsbInterface intf = device.getInterface(i);
        //        if (intf.getInterfaceClass() == 255 && intf.getInterfaceSubclass() == 66 &&
        //                intf.getInterfaceProtocol() == 1)
        //        {
        //            return intf;
        //        }
        //    }
        //    return null;
        //}
        public static UsbInterface FindAdbInterface(UsbDevice device)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbHelpers");
            try
            {
                int count = device.InterfaceCount;//.GetInterfaceCount();
                for (int i = 0; i < count; i++)
                {
                    UsbInterface intf = device.GetInterface(i);
                    if (intf.InterfaceClass == UsbClass.VendorSpec && (int)intf.InterfaceSubclass == 66 &&
                            intf.InterfaceProtocol == 1)
                    {
                        return intf;
                    }
                }
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured at AdbHelpers FindAdbInterface " + ex.StackTrace);
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("An Java error occured at AdbHelpers FindAdbInterface : " + ex.StackTrace);
            }
            catch (Exception ex)
            {
                _logger.Println("An Android error occured at AdbHelpers FindAdbInterface " + ex.StackTrace);
            }
            return default;

        }

        //        private synchronized boolean setAdbInterface(UsbDevice device, UsbInterface intf) throws IOException, InterruptedException {
        //        if (adbConnection != null) {
        //            adbConnection.close();
        //            adbConnection = null;
        //            mDevice = null;
        //        }

        //        if (device != null && intf != null) {
        //            UsbDeviceConnection connection = mManager.openDevice(device);
        //            if (connection != null) {
        //                if (connection.claimInterface(intf, false)) {
        //                    handler.sendEmptyMessage(CONNECTING);
        //                    adbConnection = AdbConnection.create(new UsbChannel(connection, intf), adbCrypto);
        //                    adbConnection.connect();
        //                    //TODO: DO NOT DELETE IT, I CAN'T EXPLAIN WHY
        //                    adbConnection.open("shell:exec date");

        //                    mDevice = device;
        //                    handler.sendEmptyMessage(DEVICE_FOUND);
        //                    return true;
        //                } else
        //{
        //    connection.close();
        //}
        //            }
        //        }

        //        handler.sendEmptyMessage(DEVICE_NOT_FOUND);

        //mDevice = null;
        //return false;
        //    }

        public static bool SetAdbInterface(UsbDevice device, UsbInterface intf)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbHelpers");
            if (USBStaticDetails._adbConnection != null)
            {
                USBStaticDetails._adbConnection.Close();
                USBStaticDetails._adbConnection = null;
                USBStaticDetails._device = null;
            }                        
            try
            {
                if (device != null && intf != null)
                {
                    UsbDeviceConnection connection = default;
                    
                    #region Open Device
                    try
                    {
                        connection = USBStaticDetails._usbManager.OpenDevice(device);
                    }
                    catch
                    {
                        _logger.Println("Device.OpenDevice Failed and threw error in AdbHelper SetAdbInterface");
                    }
                    #endregion

                    ///Claim Interface, claim channel, Connect, and send a shell request
                    if (connection != null)
                    {
                        USBStaticDetails._connection = connection;

                        if (connection.ClaimInterface(intf, false))
                        {                                                        
                            IAdbChannel _channel = new UsbChannel(connection, intf);
                            USBStaticDetails._adbConnection = AdbConnection.Create(_channel, USBStaticDetails._AdbCrypto);
                            USBStaticDetails._adbConnection.Connect();


                            //TODO: DO NOT DELETE IT, I CAN'T EXPLAIN WHY -- This is never explained but I think it is asking for auth after shell command and is essentally a test?
                            USBStaticDetails._Host_To_Device_Stream = USBStaticDetails._adbConnection.Open("shell: exec date");
                            //USBStaticDetails._Host_To_Device_Stream = USBStaticDetails._adbConnection.Open("shell: ls -l");
                            
                            USBStaticDetails._device = device;   
                            
                            return true;
                        }
                        else
                        {
                            USBStaticDetails._adbConnection.Close();
                        }
                    }
                    else
                    {
                        PairingPageStatusHelper.GenericMessage("Error Occured in USB Connection, Please unplug cable from both ends and try again");
                    }
                }
                else
                {
                    USBStaticDetails._adbConnection.Close();
                    USBStaticDetails._adbConnection = null;
                    USBStaticDetails._device = null;
                }
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured at AdbHelpers SetAdbInterface " + ex.StackTrace);
                PairingPageStatusHelper.GenericMessage("Error Occured in USB Connection, Please unplug cable from both ends and try again");

            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("An Java error occured at AdbHelpers SetAdbInterface : " + ex.StackTrace);
                PairingPageStatusHelper.GenericMessage("Error Occured in USB Connection, Please unplug cable from both ends and try again");
            }
            catch (Exception ex)
            {
                _logger.Println("An Android error occured at AdbHelpers SetAdbInterface " + ex.StackTrace);
                PairingPageStatusHelper.GenericMessage("Error Occured in USB Connection, Please unplug cable from both ends and try again");
            }
            USBStaticDetails._device = null;
            return false;
        }


        /// <summary>
        /// This doesnt seem needed ---- check in main activity
        /// </summary>
        /// <param name="intent"></param>
        //internal void OnNewIntent(Intent intent)
        //{

        //    //println("From onNewIntent");
        //    UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
        //    RefreshAdbConnection(device);
        //}


        //private void initCommand()
        //{
        //    // Open the shell stream of ADB
        //    //logs.setText("");
        //    try
        //    {
        //        //stream = USBStaticDetails._adbConnection.Open("shell:");
        //    }
        //    catch (Java.IO.UnsupportedEncodingException e)
        //    {
        //        //e.printStackTrace();
        //        return;
        //    }
        //    catch (Java.IO.IOException e)
        //    {
        //        //e.printStackTrace();
        //        return;
        //    }
        //    catch (Java.Lang.InterruptedException e)
        //    {
        //        //e.printStackTrace();
        //        return;
        //    }

        //    // Start the receiving thread

        //    //new Thread(new Runnable() {



        //    //        public void run()
        //    //        {
        //    //            while (!stream.isClosed())
        //    //            {
        //    try
        //    {
        //        //                    // Print each thing we read from the shell stream
        //        //                    final String[] output = { new String(stream.read(), "US-ASCII")};
        //        //                    runOnUiThread(new Runnable() {
        //        //                        @Override
        //        //                        public void run()
        //        //                    {
        //        //                        if (user == null)
        //        //                        {
        //        //                            user = output[0].substring(0, output[0].lastIndexOf("/") + 1);
        //        //                        }
        //        //                        else if (output[0].contains(user))
        //        //                        {
        //        //                            System.out.println("End => " + user);
        //        //                        }

        //        //                        logs.append(output[0]);

        //        //                        scrollView.post(new Runnable() {
        //        //                                @Override
        //        //                                public void run()
        //        //                        {
        //        //                            scrollView.fullScroll(ScrollView.FOCUS_DOWN);
        //        //                            edCommand.requestFocus();
        //        //                        }
        //        //                    });
        //        //    }
        //        //});
        //    }
        //    catch (Java.IO.UnsupportedEncodingException e)
        //    {
        //        //e.printStackTrace();
        //        return;
        //    }
        //    catch (Java.Lang.InterruptedException e)
        //    {
        //        //e.printStackTrace();
        //        return;
        //    }
        //    catch (Java.IO.IOException e)
        //    {
        //        //e.printStackTrace();
        //        return;
        //    }
        //}


        //public static void HandleMessage(Android.OS.Message msg)
        //{
        //    switch (msg.What)
        //    {
        //        case ((int)MessageEnum.DEVICE_FOUND):
        //            //closeWaiting();
        //            //tvStatus.setText(getString(R.string.adb_device_connected));
        //            //usb_icon.setColorFilter(Color.parseColor("#4CAF50"));
        //            //checkContainer.setVisibility(View.GONE);
        //            //terminalView.setVisibility(View.VISIBLE);
        //            //initCommand();
        //            //showKeyboard();
        //            break;

        //        case ((int)MessageEnum.CONNECTING):
        //            //waitingDialog();
        //            //closeKeyboard();
        //            //tvStatus.setText(getString(R.string.waiting_device));
        //            //usb_icon.setColorFilter(Color.BLUE);
        //            //checkContainer.setVisibility(View.VISIBLE);
        //            //terminalView.setVisibility(View.GONE);
        //            break;

        //        case ((int)MessageEnum.DEVICE_NOT_FOUND):
        //            //closeWaiting();
        //            //closeKeyboard();
        //            //tvStatus.setText(getString(R.string.adb_device_not_connected));
        //            //usb_icon.setColorFilter(Color.RED);
        //            //checkContainer.setVisibility(View.VISIBLE);
        //            //terminalView.setVisibility(View.GONE);
        //            break;

        //        case ((int)MessageEnum.FLASHING):
        //            //Toast.makeText(MainActivity.this, "Flashing", Toast.LENGTH_SHORT).show();
        //            break;

        //        case ((int)MessageEnum.INSTALLING_PROGRESS):
        //            //Toast.makeText(MainActivity.this, "Progress", Toast.LENGTH_SHORT).show();
        //            break;

        //    }
        //}




        ///Parsing enums
        public static async Task<string> ParseTag(AdbTagEnum input)
        {
            try
            {
                string output = string.Empty;


                switch (input)
                {
                    case AdbTagEnum.Update:
                        {
                            output = "-r";
                            break;
                        }
                    case AdbTagEnum.WaitForLaunchToComplete:
                        {
                            output = "-W";
                            break;
                        }
                    case AdbTagEnum.EnableDebugging:
                        {
                            output = "-D";
                            break;
                        }
                    case AdbTagEnum.StartProfiler:
                        {
                            output = "-P";
                            break;
                        }
                    case AdbTagEnum.ActivityCounter:
                        {
                            output = "-R";
                            break;
                        }
                    case AdbTagEnum.ForceStop:
                        {
                            output = "-S";
                            break;
                        }
                    case AdbTagEnum.TraceOpenGLFunctions:
                        {
                            output = "--opengl-trace";
                            break;
                        }
                    case AdbTagEnum.SpecifyUser:
                        {
                            output = "--user";
                            break;
                        }
                    case AdbTagEnum.Test:
                        {
                            output = "-t";
                            break;
                        }
                    case AdbTagEnum.InternalMemPackage:
                        {
                            output = "-f";
                            break;
                        }
                    case AdbTagEnum.Incremental:
                        {
                            output = "--incremental";
                            break;
                        }
                    case AdbTagEnum.NoIncremental:
                        {
                            output = "--no-incremental";
                            break;
                        }
                    case AdbTagEnum.FastDeploy:
                        {
                            output = "--fastdeploy";
                            break;
                        }
                    case AdbTagEnum.SomeABasedOperator:
                        {
                            output = "-a";
                            break;
                        }
                    default:
                        //return string.Empty;
                        break;
                }
                return output;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        public static async Task<string> ParsePrefix(AdbPrefixEnum input)
        {
            try
            {
                string output = string.Empty;
                switch (input)
                {
                    case AdbPrefixEnum.Adb:
                        {
                            output = "adb";
                            break;
                        }
                    case AdbPrefixEnum.RemoteShell:
                        {
                            output = "shell";
                            break;
                        }
                    case AdbPrefixEnum.PackageManager:
                        {
                            output = "pm";
                            break;
                        }
                    case AdbPrefixEnum.ActivityManager:
                        {
                            output = "am";
                            break;
                        }
                    case AdbPrefixEnum.PackageManagerShell:
                        {
                            output = "shell:pm";
                            break;
                        }
                    default:
                        break;
                }
                return output;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        public static async Task<string> ParseCommand(AdbCommandEnum input)
        {
            try
            {
                string output = string.Empty;
                switch (input)
                {
                    case AdbCommandEnum.sync:
                        {
                            output = "sync";
                            break;
                        }
                    case AdbCommandEnum.pull:
                        {
                            output = "pull";
                            break;
                        }
                    case AdbCommandEnum.push:
                        {
                            output = "push";
                            break;
                        }
                    case AdbCommandEnum.install:
                        {
                            output = "install";
                            break;
                        }
                    case AdbCommandEnum.killall:
                        {
                            output = "kill-all";
                            break;
                        }
                    case AdbCommandEnum.broadcast:
                        {
                            output = "broadcast";
                            break;
                        }

                    case AdbCommandEnum.Instrument:
                        {
                            output = "instrument";
                            break;
                        }
                    case AdbCommandEnum.ToUri:
                        {
                            output = "to-uri";
                            break;
                        }
                    case AdbCommandEnum.ToIntentUri:
                        {
                            output = "to-intent-uri";
                            break;
                        }
                    case AdbCommandEnum.start:
                        {
                            output = "start";
                            break;
                        }
                    default:
                        break;
                }
                return output;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

    }

}