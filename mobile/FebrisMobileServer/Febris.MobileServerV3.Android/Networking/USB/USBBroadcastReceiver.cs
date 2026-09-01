// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.Util;
using Febris.AdbLibrary.AdbLib;
using Febris.AdbLibrary.Interface;
using Febris.MobileServerV3.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

//I  have no idea if this assembly call is needed
[assembly: UsesFeature("android.hardware.usb.host")]
namespace Febris.MobileServerV3.Droid.Networking.USB
{
    public class USBBroadcastReceiver : BroadcastReceiver
    {
        private UsbManager _usbManager;
        private MainActivity _mainActivity;
        public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris");
        //private call USBPermissionCallBack _uSBPermissionCallBack;
        public USBBroadcastReceiver()
        {
            //_logger.Println("USBBroadcastReceiver was triggered with no dependencies");
            _usbManager = USBStaticDetails._usbManager;
        }
        public USBBroadcastReceiver(UsbManager usbManager, MainActivity activity)
        {
            //_logger.Println("USBBroadcastReceiver was triggered with 2 dependencies");
            _usbManager = usbManager;
            _mainActivity = activity;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;
            _logger.Println("USB OnReceive was triggered with action: " + action);
            #region custom Permission
            if (USBStaticDetails.ACTION_USB_PERMISSION.Equals(action))
            {
                try
                {
                    _logger.Println("ACTION_USB_PERMISSION was triggered");
                    UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
                    if (USBStaticDetails._usbManager.HasPermission(device))
                    {
                        if (device != null)
                        {
                            if (intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
                            {
                                AdbHelpers.RefreshAdbConnection(device);
                            }
                            else
                            {
                                ExtraPermissionsRequest(device);
                            }
                        }
                    }
                    else
                    {
                        PairingPageStatusHelper.GenericMessage("A permissions error occured when connecting to USB device.");
                        PendingIntent permissionIntent = PendingIntent.GetBroadcast(Application.Context, 0, new Intent(USBStaticDetails.ACTION_USB_PERMISSION), 0);
                        _usbManager.RequestPermission(device, permissionIntent);
                    }
                }
                catch (AndroidException ex) { _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace); }
                catch (Exception ex)
                {
                    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                    _logger.Println("Cleaning usb connection data");
                    USBStaticDetails.CleanConnection();
                }
            }
            #endregion
            #region Device Attached
            else if (action == UsbManager.ActionUsbDeviceAttached)
            {
                try
                {
                    try { PairingPageStatusHelper.GenericMessage("New usb device attachment detected."); } catch { }
                    _logger.Println("ActionUsbDeviceAttached was triggered");
                    UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
                    USBStaticDetails._device = device;

                    if (!_usbManager.HasPermission(device))
                    {
                        RequestTempPermission(device);
                    }
                    else if (!intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
                    {
                        ExtraPermissionsRequest(device);
                    }
                    else
                    {
                        AdbHelpers.RefreshAdbConnection(device);
                    }
                }
                catch (AndroidException ex)
                {
                    _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                    _logger.Println("Cleaning usb connection data");
                    USBStaticDetails.CleanConnection();
                }
            }
            #endregion
            #region Device Detached
            else if (action == UsbManager.ActionUsbDeviceDetached)
            {
                try
                {
                    AdbHelpers.SetAdbInterface(null, null);
                    _logger.Println("device disconnected");
                    try { PairingPageStatusHelper.GenericMessage("usb device detached"); } catch { }

                    //UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);

                    //AdbHelpers.RefreshAdbConnection(device);
                    AdbHelpers.RefreshAdbConnection(null);

                    // TEARDOWN MUST NOT ABORT PART-WAY. This block used to be four unguarded
                    // dereferences in a row, and the third one could never succeed:
                    // MainActivity._AdbStream is declared and NEVER ASSIGNED anywhere, so
                    // _AdbStream.Dispose() threw NullReferenceException on every single detach.
                    // The generic catch below swallowed it, which meant CleanConnection() was
                    // never reached and the USB statics kept pointing at a device that was
                    // physically gone. The real stream is USBStaticDetails._Host_To_Device_Stream,
                    // set in AdbHelpers, which is what this was reaching for.
                    //
                    // Each step is now independently guarded and CleanConnection runs in a
                    // finally, so a failure in any one of them cannot leave stale state behind for
                    // the next attach.
                    try
                    {
                        if (USBStaticDetails._connection != null)
                        {
                            if (USBStaticDetails._interface != null)
                            {
                                USBStaticDetails._connection.ReleaseInterface(USBStaticDetails._interface);
                            }
                            USBStaticDetails._connection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Println("usb detach: releasing the connection failed: " + ex.Message);
                    }

                    try
                    {
                        USBStaticDetails._Host_To_Device_Stream?.Dispose();
                        USBStaticDetails._Device_To_Host_Stream?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Println("usb detach: disposing the adb stream(s) failed: " + ex.Message);
                    }
                }
                catch (AndroidException ex)
                {
                    _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                }
                finally
                {
                    // ALWAYS. Stale USB statics after a detach are what break the NEXT attach, and
                    // before this the cleanup sat after an unguarded dereference that always threw.
                    _logger.Println("Cleaning usb connection data");
                    USBStaticDetails.CleanConnection();
                }
            }
            #endregion
            #region Accessory Attached
            else if (action == UsbManager.ActionUsbAccessoryAttached)
            {
                try
                {
                    try { PairingPageStatusHelper.GenericMessage("ActionUsbAccessoryAttached was triggered"); } catch { }

                    Console.WriteLine("ActionUsbAccessoryAttached was triggered");
                    _logger.Println("ActionUsbAccessoryAttached was triggered");
                }
                catch (AndroidException ex)
                {
                    _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                }
            }
            #endregion
            #region Accessory Detached
            else if (action == UsbManager.ActionUsbAccessoryDetached)
            {
                try
                {
                    try { PairingPageStatusHelper.GenericMessage("ActionUsbAccessoryDetached was triggered"); } catch { }

                    Console.WriteLine("ActionUsbAccessoryDetached was triggered");
                    _logger.Println("ActionUsbAccessoryDetached was triggered");
                }
                catch (AndroidException ex)
                {
                    _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                }
            }
            #endregion
            #region Extra Accessory
            else if (action == UsbManager.ExtraAccessory)
            {
                try
                {
                    PairingPageStatusHelper.GenericMessage("ExtraAccessory was triggered");
                    Console.WriteLine("ExtraAccessory was triggered");
                    _logger.Println("ExtraAccessory was triggered");
                }
                catch (AndroidException ex)
                {
                    _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                }
            }
            #endregion
            #region Extra Device
            else if (action == UsbManager.ExtraDevice)
            {
                try
                {
                    PairingPageStatusHelper.GenericMessage("ExtraDevice was triggered");
                    Console.WriteLine("ExtraDevice was triggered");
                    _logger.Println("ExtraDevice was triggered");
                }
                catch (AndroidException ex)
                {
                    _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                }
            }
            #endregion
            #region Extra Permissions
            else if (action == UsbManager.ExtraPermissionGranted)
            {
                try
                {
                    UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
                    _logger.Println("ExtraPermissionGranted was triggered");
                    if (intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
                    {
                        if (device != null)
                        {
                            AdbHelpers.RefreshAdbConnection(device);
                        }
                    }
                    else { PairingPageStatusHelper.GenericMessage("A permissions error occured when connecting to USB device."); }
                }
                catch (AndroidException ex)
                {
                    _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                }
            }
            #endregion
            #region Install results
            if (USBStaticDetails.INSTALL_FAILURE.Equals(action))
            {
                //try
                //{
                //    //try
                //    //{
                //    //    PairingPageStatusHelper.GenericMessage("ACTION_USB_PERMISSION was triggered");
                //    //}
                //    //catch { }

                //    _logger.Println("ACTION_USB_PERMISSION was triggered");
                //    if (intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
                //    {
                //        UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
                //        List<UsbInterface> interfaceList = new List<UsbInterface>();
                //        for (int i = 0; i < device.InterfaceCount; i++)
                //        {
                //            UsbInterface tempInterface = device?.GetInterface(i);
                //            _logger.Println("Interface Found: " + tempInterface?.Name);
                //            interfaceList.Add(tempInterface);
                //        }
                //        UsbEndpoint endpoint_IN = default;
                //        UsbEndpoint endpoint_OUT = default;

                //        //bool containsAdbInterface = interfaceList.Where(i => i.Name.ToLower().Contains("adb")).Any();
                //        //bool containsAdbInterface = interfaceList.Where(i => i.Name.Contains("ADB")).Any();
                //        bool containsAdbInterface = interfaceList.Where(i => i.Name == "ADB Interface").Any();
                //        _logger.Println("Contains adb Interface: " + containsAdbInterface);

                //        if (containsAdbInterface)
                //        {
                //            _logger.Println("Attepting connection to ADB Interface");
                //            //UsbInterface usbInterface = interfaceList.Where(i => i.Name.ToLower().Contains("adb")).First();
                //            //UsbInterface usbInterface = interfaceList.Where(i => i.Name.Contains("ADB")).First();

                //            UsbInterface usbInterface = interfaceList.Where(i => i.Name == "ADB Interface").First();
                //            USBStaticDetails._interface = usbInterface;
                //            _logger.Println("Device Opening attempt");
                //            try
                //            {
                //                UsbDeviceConnection connection = _usbManager.OpenDevice(device);
                //                connection.ClaimInterface(usbInterface, true);
                //                //connection.ControlTransfer;
                //                USBStaticDetails._connection = connection;
                //            }
                //            catch
                //            {
                //                _logger.Println("Opening Device Failed");
                //                _logger.Println("Cleaning usb connection data");
                //                USBStaticDetails.CleanConnection();
                //            }

                //        }

                //        if (USBStaticDetails._connection == default)
                //        {
                //            foreach (var j in interfaceList)
                //            {
                //                endpoint_IN = default;
                //                endpoint_OUT = default;
                //                for (int i = 0; i < j?.EndpointCount; i++)
                //                {
                //                    _logger.Println("Testing endpoint " + j.GetEndpoint(i).Type.ToString() + " for Interface " + j.Name);
                //                    UsbEndpoint tempendpoint = j?.GetEndpoint(i);
                //                    if (tempendpoint.Type == UsbAddressing.Out)
                //                    {
                //                        Console.WriteLine("Outward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
                //                        _logger.Println("Outward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
                //                        endpoint_OUT = tempendpoint ?? default;
                //                    }
                //                    else if (tempendpoint.Type == UsbAddressing.In)
                //                    {
                //                        Console.WriteLine("Inward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
                //                        _logger.Println("Inward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
                //                        endpoint_IN = tempendpoint ?? default;
                //                    }
                //                }
                //                if (endpoint_IN != default && endpoint_OUT != default)
                //                {
                //                    USBStaticDetails._interface = j;
                //                    _logger.Println("Interface that can be used be used has been found: " + j?.Name + "with end point addresses- In: " + endpoint_IN.Address.ToString() + " Out:" + endpoint_OUT.Address.ToString());
                //                    break;
                //                }
                //            }

                //            USBStaticDetails._endpoint_IN = endpoint_IN;
                //            USBStaticDetails._endpoint_OUT = endpoint_OUT;

                //            if (USBStaticDetails._interface != default &&
                //                USBStaticDetails._endpoint_IN != default &&
                //                USBStaticDetails._endpoint_OUT != default &&
                //                USBStaticDetails._connection != default &&
                //                USBStaticDetails._interface != default
                //                )
                //            {
                //                _logger.Println("Creating non-adb usb connection");
                //                UsbDeviceConnection connection = _usbManager.OpenDevice(device);
                //                connection.ClaimInterface(USBStaticDetails._interface, true);
                //                USBStaticDetails._connection = connection;
                //            }
                //            else
                //            {
                //                _logger.Println("Cleaning usb connection data");
                //                USBStaticDetails.CleanConnection();
                //            }
                //        }
                //    }
                //    else
                //    {
                //        try
                //        {
                //            PairingPageStatusHelper.GenericMessage("Permission denied from " + USBStaticDetails._device.DeviceName);
                //        }
                //        catch { }
                //        Console.WriteLine("permission denied for device " + USBStaticDetails._device);
                //        _logger.Println("permission denied for device " + USBStaticDetails._device);
                //        _logger.Println("Cleaning usb connection data");
                //        USBStaticDetails.CleanConnection();
                //    }
                //}
                //catch (AndroidException ex) { _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace); }
                //catch (Exception ex)
                //{
                //    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                //    _logger.Println("Cleaning usb connection data");
                //    USBStaticDetails.CleanConnection();
                //}
            }
            if (USBStaticDetails.INSTALL_COMPLETE.Equals(action))
            {
                //try
                //{
                //    //try
                //    //{
                //    //    PairingPageStatusHelper.GenericMessage("ACTION_USB_PERMISSION was triggered");
                //    //}
                //    //catch { }

                //    _logger.Println("ACTION_USB_PERMISSION was triggered");
                //    if (intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
                //    {
                //        UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
                //        List<UsbInterface> interfaceList = new List<UsbInterface>();
                //        for (int i = 0; i < device.InterfaceCount; i++)
                //        {
                //            UsbInterface tempInterface = device?.GetInterface(i);
                //            _logger.Println("Interface Found: " + tempInterface?.Name);
                //            interfaceList.Add(tempInterface);
                //        }
                //        UsbEndpoint endpoint_IN = default;
                //        UsbEndpoint endpoint_OUT = default;

                //        //bool containsAdbInterface = interfaceList.Where(i => i.Name.ToLower().Contains("adb")).Any();
                //        //bool containsAdbInterface = interfaceList.Where(i => i.Name.Contains("ADB")).Any();
                //        bool containsAdbInterface = interfaceList.Where(i => i.Name == "ADB Interface").Any();
                //        _logger.Println("Contains adb Interface: " + containsAdbInterface);

                //        if (containsAdbInterface)
                //        {
                //            _logger.Println("Attepting connection to ADB Interface");
                //            //UsbInterface usbInterface = interfaceList.Where(i => i.Name.ToLower().Contains("adb")).First();
                //            //UsbInterface usbInterface = interfaceList.Where(i => i.Name.Contains("ADB")).First();

                //            UsbInterface usbInterface = interfaceList.Where(i => i.Name == "ADB Interface").First();
                //            USBStaticDetails._interface = usbInterface;
                //            _logger.Println("Device Opening attempt");
                //            try
                //            {
                //                UsbDeviceConnection connection = _usbManager.OpenDevice(device);
                //                connection.ClaimInterface(usbInterface, true);
                //                //connection.ControlTransfer;
                //                USBStaticDetails._connection = connection;
                //            }
                //            catch
                //            {
                //                _logger.Println("Opening Device Failed");
                //                _logger.Println("Cleaning usb connection data");
                //                USBStaticDetails.CleanConnection();
                //            }

                //        }

                //        if (USBStaticDetails._connection == default)
                //        {
                //            foreach (var j in interfaceList)
                //            {
                //                endpoint_IN = default;
                //                endpoint_OUT = default;
                //                for (int i = 0; i < j?.EndpointCount; i++)
                //                {
                //                    _logger.Println("Testing endpoint " + j.GetEndpoint(i).Type.ToString() + " for Interface " + j.Name);
                //                    UsbEndpoint tempendpoint = j?.GetEndpoint(i);
                //                    if (tempendpoint.Type == UsbAddressing.Out)
                //                    {
                //                        Console.WriteLine("Outward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
                //                        _logger.Println("Outward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
                //                        endpoint_OUT = tempendpoint ?? default;
                //                    }
                //                    else if (tempendpoint.Type == UsbAddressing.In)
                //                    {
                //                        Console.WriteLine("Inward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
                //                        _logger.Println("Inward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
                //                        endpoint_IN = tempendpoint ?? default;
                //                    }
                //                }
                //                if (endpoint_IN != default && endpoint_OUT != default)
                //                {
                //                    USBStaticDetails._interface = j;
                //                    _logger.Println("Interface that can be used be used has been found: " + j?.Name + "with end point addresses- In: " + endpoint_IN.Address.ToString() + " Out:" + endpoint_OUT.Address.ToString());
                //                    break;
                //                }
                //            }

                //            USBStaticDetails._endpoint_IN = endpoint_IN;
                //            USBStaticDetails._endpoint_OUT = endpoint_OUT;

                //            if (USBStaticDetails._interface != default &&
                //                USBStaticDetails._endpoint_IN != default &&
                //                USBStaticDetails._endpoint_OUT != default &&
                //                USBStaticDetails._connection != default &&
                //                USBStaticDetails._interface != default
                //                )
                //            {
                //                _logger.Println("Creating non-adb usb connection");
                //                UsbDeviceConnection connection = _usbManager.OpenDevice(device);
                //                connection.ClaimInterface(USBStaticDetails._interface, true);
                //                USBStaticDetails._connection = connection;
                //            }
                //            else
                //            {
                //                _logger.Println("Cleaning usb connection data");
                //                USBStaticDetails.CleanConnection();
                //            }
                //        }
                //    }
                //    else
                //    {
                //        try
                //        {
                //            PairingPageStatusHelper.GenericMessage("Permission denied from " + USBStaticDetails._device.DeviceName);
                //        }
                //        catch { }
                //        Console.WriteLine("permission denied for device " + USBStaticDetails._device);
                //        _logger.Println("permission denied for device " + USBStaticDetails._device);
                //        _logger.Println("Cleaning usb connection data");
                //        USBStaticDetails.CleanConnection();
                //    }
                //}
                //catch (AndroidException ex) { _logger.Println("An Android error occured when looking through device data for connection " + ex.StackTrace); }
                //catch (Exception ex)
                //{
                //    _logger.Println("A generic error occured when looking through device data for connection " + ex.StackTrace);
                //    _logger.Println("Cleaning usb connection data");
                //    USBStaticDetails.CleanConnection();
                //}
            }
            #endregion

        }

        private static void RequestTempPermission(UsbDevice device)
        {
            PendingIntent permissionIntent = PendingIntent.GetBroadcast(
                                        Application.Context,
                                        0,
                                        new Intent(USBStaticDetails.ACTION_USB_PERMISSION),//.Extras(UsbManager.ExtraPermissionGranted),
                                        0);
            USBStaticDetails._usbManager.RequestPermission(device, permissionIntent);
        }

        private static void ExtraPermissionsRequest(UsbDevice device)
        {
            PendingIntent permissionIntent = PendingIntent.GetBroadcast(Application.Context, 0, new Intent(UsbManager.ExtraPermissionGranted), 0);
            USBStaticDetails._usbManager.RequestPermission(device, permissionIntent);
        }

        //private void CycleThroughInterfaces(List<UsbInterface> interfaceList, ref UsbEndpoint endpoint_IN, ref UsbEndpoint endpoint_OUT)
        //{
        //    foreach (var j in interfaceList)
        //    {
        //        endpoint_IN = default;
        //        endpoint_OUT = default;
        //        CycleThroughEndPoints(ref endpoint_IN, ref endpoint_OUT, j);
        //        if (endpoint_IN != default && endpoint_OUT != default)
        //        {
        //            USBStaticDetails._interface = j;
        //            _logger.Println("Interface that can be used be used has been found: " + j?.Name + "with end point addresses- In: " + endpoint_IN.Address.ToString() + " Out:" + endpoint_OUT.Address.ToString());
        //            break;
        //        }
        //    }
        //}

        //private void CycleThroughEndPoints(ref UsbEndpoint endpoint_IN, ref UsbEndpoint endpoint_OUT, UsbInterface j)
        //{
        //    try
        //    {
        //        for (int i = 0; i < j?.EndpointCount; i++)
        //        {
        //            _logger.Println("Testing endpoint " + j.GetEndpoint(i).Type.ToString() + " for Interface " + j.Name);
        //            UsbEndpoint tempendpoint = j?.GetEndpoint(i);
        //            if (tempendpoint.Type == UsbAddressing.Out)
        //            {
        //                //Console.WriteLine("Outward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println("Outward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
        //                endpoint_OUT = tempendpoint ?? default;
        //            }
        //            else if (tempendpoint.Type == UsbAddressing.In)
        //            {
        //                //Console.WriteLine("Inward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println("Inward Endpoint for Interface " + j?.Name + " Found: " + tempendpoint?.Address);
        //                endpoint_IN = tempendpoint ?? default;
        //            }
        //            else if (tempendpoint.Type == UsbAddressing.XferControl)
        //            {
        //                //Console.WriteLine("Inward Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println(tempendpoint.Type.ToString() + " type Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                //endpoint_IN = tempendpoint ?? default;
        //            }
        //            else if (tempendpoint.Type == UsbAddressing.XferIsochronous)
        //            {
        //                //Console.WriteLine("Inward Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println(tempendpoint.Type.ToString() + " type Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                //endpoint_IN = tempendpoint ?? default;
        //            }
        //            else if (tempendpoint.Type == UsbAddressing.XferBulk)
        //            {
        //                //Console.WriteLine("Inward Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println(tempendpoint.Type.ToString() + " type Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                USBStaticDetails._adb_endpoint = tempendpoint ?? default;
        //                //endpoint_IN = tempendpoint ?? default;
        //            }
        //            else if (tempendpoint.Type == UsbAddressing.XferInterrupt)
        //            {
        //                //Console.WriteLine("Inward Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println(tempendpoint.Type.ToString() + " type Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                //endpoint_IN = tempendpoint ?? default;
        //            }
        //            else if (tempendpoint.Type == UsbAddressing.XferTypeMask)
        //            {
        //                //Console.WriteLine("Inward Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println(tempendpoint.Type.ToString() + " type Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                //endpoint_IN = tempendpoint ?? default;
        //            }
        //            else if (tempendpoint.Type == UsbAddressing.NumberMask)
        //            {
        //                //Console.WriteLine("Inward Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println(tempendpoint.Type.ToString() + " type Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                //endpoint_IN = tempendpoint ?? default;
        //            }
        //            else if (tempendpoint.Type == UsbAddressing.DirMask)
        //            {
        //                //Console.WriteLine("Inward Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                _logger.Println(tempendpoint.Type.ToString() + " type Endpoint for Interface " + USBStaticDetails._interface.Name + " Found: " + tempendpoint?.Address);
        //                //endpoint_IN = tempendpoint ?? default;
        //            }
        //            else
        //            {
        //                _logger.Println("THIS ENDPOINT DOES NOT MATCH ANY KNOWN USBADDRESSING ENUM");
        //            }
        //        }
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured when trying to establish endpoints: " + ex.StackTrace); }
        //    catch (Exception ex)
        //    {
        //        _logger.Println("A generic error occured when trying to establish endpoints: " + ex.StackTrace);
        //        _logger.Println("Cleaning usb connection data");
        //        USBStaticDetails.CleanConnection();
        //    }
        //}

        //private void GetProtocol(UsbInterface input)
        //{
        //    try
        //    {
        //        int protocol = input.InterfaceProtocol;
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured when trying to establish protocol: " + ex.StackTrace); }
        //    catch (Exception ex)
        //    {
        //        _logger.Println("A generic error occured when trying to establish protocol: " + ex.StackTrace);
        //        _logger.Println("Cleaning usb connection data");
        //        USBStaticDetails.CleanConnection();
        //    }


        //}

        //private void StartUsbConnection()
        //{
        //    throw new NotImplementedException();
        //}


        //    private bool SetAdbInterface(UsbDevice device, UsbInterface intf) //throws IOException, InterruptedException {
        //    //if (adbConnection != null) {
        //    //    adbConnection.close();
        //    //    adbConnection = null;
        //    //    mDevice = null;
        //    //}
        //    {
        //        if (device != null && intf != null)
        //        {
        //            UsbDeviceConnection connection = _usbManager.OpenDevice(device);
        //            if (connection != null)
        //            {
        //                if (connection.ClaimInterface(intf, false))
        //                {
        //                    //handler.sendEmptyMessage(CONNECTING);
        //                    USBStaticDetails._adbConnection = AdbConnection.Create(new UsbChannel(connection, intf), USBStaticDetails._AdbCrypto);
        //                    USBStaticDetails._adbConnection.Connect();
        //                    //TODO: DO NOT DELETE IT, I CAN'T EXPLAIN WHY
        //                    USBStaticDetails._adbConnection.Open("shell:exec date");

        //                    USBStaticDetails._device = device;
        //                    //handler.sendEmptyMessage(DEVICE_FOUND);
        //                    return true;
        //                }
        //                else
        //                {
        //                    connection.Close();
        //                }
        //            }
        //        }

        //        ///handler.sendEmptyMessage(DEVICE_NOT_FOUND);

        //        USBStaticDetails._device = null;
        //        return false;
        //    }
        //    public void RefreshAdbConnection(UsbDevice device)
        //    {
        //        if (device != null)
        //        {
        //            new System.Threading.Thread(new System.Threading.ThreadStart(()=>{
        //                //@Override
        //            //public void Run()
        //            //{
        //                UsbInterface intf = AdbHelpers.FindAdbInterface(device);
        //                try
        //                {
        //                    SetAdbInterface(device, intf);
        //                }
        //                catch (Exception e)
        //                {
        //                    //Log.w(Const.TAG, "setAdbInterface(device, intf) fail", e);
        //                }
        //            //}
        //        })).Start();
        //    }
        //}
    }
    
}