// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Febris.SharedMobileLibrary.P2pNetworking;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Android.Bluetooth;
using System.Threading.Tasks;
using Xamarin.Forms;
using Febris.MobileServerV3.Droid.Networking.WiFi;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.MobileServerV3.Droid.Networking.Bluetooth;
using Android.Companion;
using Android.Net.Wifi.P2p;
using Febris.MobileServerV3.Droid.Communication.WiFi;
using Android;
using Android.Net.Wifi.P2p.Nsd;
using Febris.MobileServerV3.Droid.Networking.USB;
using Android.Hardware.Usb;
using Febris.MobileServerV3.Droid.Networking;
using Xamarin.Essentials;
using Android.Util;
using Febris.AdbLibrary.AdbLib;
using Febris.AdbLibrary.Interface;

/// <summary>
/// KNOWN ISSUE: file permission request is incomplete. Currently
/// `OnCreate` calls <c>RequestPermissions(permissions, 0)</c> with the
/// full list of permissions below, but some permissions don't prompt
/// the user -- they have to be granted manually in Android Settings.
/// <para>
/// Most likely cause: on Android 11+ (API 30+) the following permissions
/// can no longer be granted via the standard runtime-permission prompt:
/// </para>
/// <list type="bullet">
///   <item><c>ManageExternalStorage</c> -- requires
///     <c>Intent.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION</c>
///     (open Settings to the all-files-access page for the app).</item>
///   <item><c>InstallPackages</c> / <c>RequestInstallPackages</c> --
///     requires <c>Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES</c>
///     (open Settings to the install-unknown-apps page).</item>
///   <item><c>DeletePackages</c> -- privileged permission, can only be
///     granted via signed-system-app or `pm grant` ADB; runtime prompt
///     won't help.</item>
/// </list>
/// <para>
/// Proper fix: in <c>OnCreate</c>, split the permission list into
/// "runtime-grantable" vs "settings-intent-required", call
/// <c>RequestPermissions</c> for the first set, and for the second set
/// fire the matching <c>StartActivityForResult(new Intent(action))</c>
/// per permission, awaiting the user returning from Settings via
/// <c>OnActivityResult</c>. Requires device testing on multiple API
/// levels.
/// </para>
/// </summary>
namespace Febris.MobileServerV3.Droid
{
    [Activity(Label = "Febris.MobileServerV3", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        #region variables      
        public static readonly IntentFilter _wiFiIntentFilter = new IntentFilter();
        public static readonly IntentFilter _BTIntentFilter = new IntentFilter();
        public static readonly IntentFilter _usbIntentFilter = new IntentFilter();

        public static readonly Android.Util.LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris");
        //public static WiFiP2pServer wiFiP2pServer;
        //public static WiFiService wifiService;

        // REMOVED 2026-07-29: _AdbStream and _AdbConnection.
        //
        // Both were declared here and assigned NOWHERE. _AdbConnection was additionally
        // `static readonly` with no initialiser, so it could only ever be null. _AdbStream was
        // worse than unused: USBBroadcastReceiver dereferenced it on every USB detach
        // (`MainActivity._AdbStream.Dispose()`), which threw NullReferenceException into a generic
        // catch and skipped CleanConnection(), leaving the USB statics pointing at a device that
        // had been physically unplugged.
        //
        // The real ADB state lives in USBStaticDetails: _adbConnection, _Host_To_Device_Stream and
        // _Device_To_Host_Stream, all of which ARE assigned by AdbHelpers. These two were a
        // duplicate set that never got wired, sitting next to working code where they read as the
        // obvious thing to use. Same reader-with-no-writer shape as issues 14, 21 and 25.
        #endregion


        /// <summary>
        /// Brings the pairing and handshake credential store online, and hooks the numeric
        /// comparison dialog to it.
        ///
        /// UNTIL THIS RUNS the entire authenticated channel is inert: P2pHandshakePolicy.Evaluate
        /// finds no store, every connection resolves to Skipped with a null session key, and a
        /// confirmed pairing would have nowhere to be written. Assigning the store is the single
        /// switch that makes the handshake live, which is why it is done ONCE, here, after the UI
        /// exists to show a code.
        ///
        /// LoadAsync must complete before the socket pumps start, because an unloaded store
        /// answers "unpaired" for every peer, which silently downgrades every connection to
        /// unauthenticated rather than failing visibly.
        /// </summary>
        private static async Task InitialisePairingAsync(Func<string, string> peerIdentifierForConfirm)
        {
            try
            {
                // Records what this runtime can and cannot do, once, at startup. The pairing
                // ceremony's key agreement moved to BouncyCastle because Mono/Android does not
                // implement ECDiffieHellman, and the AesGcm line decides whether the planned v3
                // frame AEAD can use the platform primitive or must follow ECDH to BouncyCastle.
                // Measured here rather than reasoned about, because reasoning from a successful
                // COMPILE is exactly what put an insecure stub on this device in the first place.
                Console.WriteLine(PlatformCryptoProbe.Describe());

                var store = new P2pPairingSecretStore(new EssentialsSecureKeyValueStore());
                int loaded = await store.LoadAsync();
                Console.WriteLine("pairing store loaded, " + loaded + " pairing(s) known");

                P2pHandshakePolicy.SecretStore = store;
                P2pPairingCoordinator.Store = store;

                P2pPairingCoordinator.CodeReady += code =>
                {
                    // Raised from the socket thread. The dialog must be marshalled to the UI
                    // thread or Forms throws, and this is the one place the ceremony touches UI.
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            bool matches = await Xamarin.Forms.Application.Current.MainPage.DisplayAlert(
                                "Pairing code",
                                "Confirm this code matches the OTHER device:" +
                                System.Environment.NewLine + System.Environment.NewLine + code +
                                System.Environment.NewLine + System.Environment.NewLine +
                                "If the codes differ, someone may be intercepting. Choose They differ.",
                                "They match",
                                "They differ");

                            if (!matches)
                            {
                                P2pPairingCoordinator.Reject();
                                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert(
                                    "Pairing refused", "No pairing was saved.", "OK");
                                return;
                            }

                            string pairedWith = await P2pPairingCoordinator.ConfirmAsync(
                                peerIdentifierForConfirm == null ? null : peerIdentifierForConfirm(code));
                            bool saved = !string.IsNullOrWhiteSpace(pairedWith);

                            // CREATE THE DEVICE RECORD. This is what makes pairing ONBOARD rather
                            // than merely store a key: without it the PSK exists, the pair can
                            // authenticate, and the device still never appears in the list, which
                            // reads to an operator as "pairing did nothing".
                            if (saved)
                            {
                                await Febris.MobileServerV3.BusinessLogic.PairDevice
                                    .CreateNumericallyPairedDevice(pairedWith);
                                Febris.MobileServerV3.Resources.LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.RefreshUnpairedPeers();
                            }

                            await Xamarin.Forms.Application.Current.MainPage.DisplayAlert(
                                saved ? "Paired" : "Pairing failed",
                                saved ? "This device pair now shares a key."
                                      : "The pairing could not be saved.",
                                "OK");
                        }
                        catch (Exception ex)
                        {
                            P2pPairingCoordinator.Abort();
                            Console.WriteLine("pairing dialog failed: " + ex.Message);
                        }
                    });
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("pairing store init failed: " + ex.Message);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            ///Have to add this so Xamarin and android will not over zelously block my api certs. This is not a good thing to have in here but I have 
            ///no choice.
            //System.Net.ServicePointManager
            //.ServerCertificateValidationCallback +=
            //(sender, cert, chain, sslPolicyErrors) => true;


            base.OnCreate(savedInstanceState);

            // FIRST, before anything that might want to report a problem. This head otherwise
            // emits no application logging at all on device, which is what made every failure
            // during pairing bring-up a guess instead of a reading.
            Console.SetOut(new Utilities.AndroidLogWriter());
            Console.WriteLine("Febris Mobile Server starting; console routed to logcat");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            #region permissions
            string[] permissions = {
            Manifest.Permission.AccessFineLocation,
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessWifiState,
            Manifest.Permission.ChangeWifiState,
            Manifest.Permission.Internet,
            Manifest.Permission.Bluetooth,
            Manifest.Permission.BluetoothAdmin,
            Manifest.Permission.RequestInstallPackages,
            Manifest.Permission.DeletePackages,
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage

            };
            RequestPermissions(permissions, 0);
            #region Storage
            bool ready = HasStoragePermission();
            if (!ready)
            {
                Console.WriteLine("STORAGE PERMISSION DOES NOT EXIST!!!");
            }

            #endregion
            //CheckUSBPermissions();
            #endregion

            LoadApplication(new App());

            // Server files the PSK under the Companion's DeviceUniqueIdentifier, which the
            // coordinator already captured from the pairing response frame, so no override.
            _ = InitialisePairingAsync(null);

            WiFiStaticDetails._dataProtection = DependencyService.Get<IDataProtection>();

            #region Bluetooth handling
            BluetoothStaticDetails.manager = (BluetoothManager)base.GetSystemService(BluetoothService);
            //_BTIntentFilter.AddAction(BluetoothManager);
            BluetoothStaticDetails._bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            BluetoothStaticDetails.processing = new BluetoothProcessing();

            _BTIntentFilter.AddAction(BluetoothDevice.ActionFound);
            _BTIntentFilter.AddAction(BluetoothDevice.ActionBondStateChanged);
            _BTIntentFilter.AddAction(BluetoothDevice.ActionPairingRequest);
            _BTIntentFilter.AddAction(BluetoothDevice.ExtraPreviousBondState);
            //_BTIntentFilter.AddAction(BluetoothStaticDetails.SELECT_DEVICE_REQUEST_CODE);
            #endregion

            #region Usb Handling
            IAdbBase64 base64 = new AdbBase64();
            USBStaticDetails._AdbCrypto = new AdbCrypto(base64);
            //_AdbCrypto = new AdbCrypto();
            try
            {
                //AdbCrypto.LoadAdbKeyPair(base64, new Java.IO.File(string.Empty, "private_key"), new Java.IO.File(string.Empty, "public_key"));
                //AdbCrypto.LoadAdbKeyPair(base64, new Java.IO.File("private_key"), new Java.IO.File("public_key"));
                USBStaticDetails._AdbCrypto.LoadAdbKeyPair(new Java.IO.File(FilesDir, "private_key"), new Java.IO.File(FilesDir, "public_key"));
            }
            catch (Exception e)
            {
                _logger.Println("Issue gathering adb crypto: " + e.StackTrace);
            }

           // if (USBStaticDetails._AdbCrypto == default)
           if(!USBStaticDetails._AdbCrypto.KeyPairExists())
            {
                try
                {
                    //AdbCrypto.GenerateAdbKeyPair(base64);
                    USBStaticDetails._AdbCrypto.GenerateAdbKeyPair();// (base64);
                    USBStaticDetails._AdbCrypto.SaveAdbKeyPair(new Java.IO.File(FilesDir,"private_key"), new Java.IO.File(FilesDir, "public_key"));
                    //_AdbCrypto.SaveAdbKeyPair(new Java.IO.File(string.Empty, "private_key"), new Java.IO.File(string.Empty, "public_key"));
                    USBStaticDetails._AdbCrypto.LoadAdbKeyPair(new Java.IO.File(FilesDir, "private_key"), new Java.IO.File(FilesDir, "public_key"));
                }
                catch (Exception e)
                {
                    _logger.Println("Issue creating adb crypto: " + e.StackTrace);
                }
            }


            USBStaticDetails._usbManager = (UsbManager)base.GetSystemService(UsbService);
            _usbIntentFilter.AddAction(UsbManager.ActionUsbAccessoryAttached);
            _usbIntentFilter.AddAction(UsbManager.ActionUsbAccessoryDetached);
            _usbIntentFilter.AddAction(UsbManager.ActionUsbDeviceAttached);
            _usbIntentFilter.AddAction(UsbManager.ActionUsbDeviceDetached);
            _usbIntentFilter.AddAction(UsbManager.ExtraAccessory);
            _usbIntentFilter.AddAction(UsbManager.ExtraDevice);
            _usbIntentFilter.AddAction(UsbManager.ExtraPermissionGranted);
            _usbIntentFilter.AddAction(USBStaticDetails.ACTION_USB_PERMISSION);
            #endregion

            #region wifi p2p broadcast  
            ///create manager and channel
            WiFiStaticDetails.manager = (WifiP2pManager)base.GetSystemService(WifiP2pService);
            WiFiStaticDetails.channel = WiFiStaticDetails.manager.Initialize(this, base.MainLooper, new FebrisChannelListener());
            ///add permissions, not sure if this is really needed but doesn't hurt

            ///add intent actions for broadcast receiver
            _wiFiIntentFilter.AddAction(WifiP2pManager.WifiP2pStateChangedAction);
            _wiFiIntentFilter.AddAction(WifiP2pManager.WifiP2pPeersChangedAction);
            _wiFiIntentFilter.AddAction(WifiP2pManager.WifiP2pConnectionChangedAction);
            _wiFiIntentFilter.AddAction(WifiP2pManager.WifiP2pThisDeviceChangedAction);
            _wiFiIntentFilter.AddAction(WifiP2pManager.WifiP2pDiscoveryChangedAction);
            //*******************this is some more stuff in there that has potental.*****************

            #region wifi p2p Service broadcast            
            //WiFiStaticDetails.manager.ClearLocalServices(WiFiStaticDetails.channel, new FebrisActionListener(WiFiService.wifiService.ClearedListeners));
            //WiFiStaticDetails.record.Add(WiFiStaticDetails.ServiceName);
            //WiFiStaticDetails.serviceInfo = WifiP2pUpnpServiceInfo.NewInstance(WiFiStaticDetails.ServiceUUID, WiFiStaticDetails.ServiceType, WiFiStaticDetails.record);
            //WiFiStaticDetails.manager.AddLocalService(WiFiStaticDetails.channel, WiFiStaticDetails.serviceInfo, new FebrisActionListener(() => { }));

            #endregion
            #endregion

            #region Create socket            
            Task.Run(() => WiFiP2pServer.wiFiP2pServer.SocketCreationFactory());
            //Task.Run(() => USBStaticDetails.service.HostCreationFactory());
            #endregion                  
        }

        #region on activity result - bluetooth pairing               
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            try
            {
                string action = data.Action;
                _logger.Println("OnActivityResult has been hit. with data action: " + action);
                if (resultCode != Result.Ok)
                {
                    return;
                }
                if (requestCode == BluetoothStaticDetails.SELECT_DEVICE_REQUEST_CODE && data != null)
                {
                    #region Bluetooth                    
                    BluetoothDevice deviceToPair = (BluetoothDevice)data.GetParcelableExtra(CompanionDeviceManager.ExtraDevice);
                    #endregion
                    #region BluetoothLE                    
                    //Android.Bluetooth.LE.ScanResult deviceToPair = (Android.Bluetooth.LE.ScanResult)data.GetParcelableExtra(CompanionDeviceManager.ExtraDevice);
                    #endregion
                    #region Wifi                    
                    //Android.Net.Wifi.ScanResult deviceToPair = (Android.Net.Wifi.ScanResult)data.GetParcelableExtra(CompanionDeviceManager.ExtraDevice);
                    //WifiP2pDevice deviceToPair2 = (WifiP2pDevice)data.GetParcelableExtra(CompanionDeviceManager.ExtraDevice);
                    #endregion
                    if (deviceToPair != null)
                    {
                        #region Bluetooth
                        bool connected = deviceToPair.CreateBond();
                        #endregion
                        #region BluetoothLE                        
                        //deviceToPair.Device.CreateBond();                        
                        #endregion
                        #region Wifi


                        //manager.Connect();
                        //Android.Net.Wifi.P2p.WifiP2pDevice p2PDevice = (Android.Net.Wifi.P2p.WifiP2pDevice)deviceToPair;
                        //???
                        //Have been trying a few things but could not figure it out. 
                        #endregion

                        Bond state = deviceToPair.BondState;
                        while (state == Bond.Bonding)
                        {
                            state = deviceToPair.BondState;
                        }
                        try
                        {
                            Task.Run(() => EventHandlerHelper._eventHandler.CreateCompanionDevice(deviceToPair));
                            //Task.Run(() => WiFiService.wifiService.CreateCompanionDevice(deviceToPair));
                            //Task.Run(() => EventHandlerHelper._eventHandler.CreateCompanionDevice(deviceToPair));
                        }
                        catch { }
                    }
                }

                else
                {
                    base.OnActivityResult(requestCode, resultCode, data);
                }
            }
            catch (Exception ex)
            {
                //_callback.OnFailure("Failed to hit failure");
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region Application events
        protected override void OnStart()
        {
            base.OnStart();
            try
            {

                //EventLog.WriteEvent(1, "Starting up");                
                _logger.Println("Febris is starting up");
                WiFiStaticDetails.receiver = new WiFiP2pDirectBroadcastReceiver(WiFiStaticDetails.manager, WiFiStaticDetails.channel, this);
                RegisterReceiver(WiFiStaticDetails.receiver, _wiFiIntentFilter);

                USBStaticDetails._receiver = new USBBroadcastReceiver();
                //USBStaticDetails._receiver = new USBBroadcastReceiver(USBStaticDetails._usbManager, this); 
                RegisterReceiver(USBStaticDetails._receiver, _usbIntentFilter);//, ActivityFlags.DebugLogResolution);
                

                BluetoothStaticDetails.receiver = new BluetoothBroadcastReceiver();
                RegisterReceiver(BluetoothStaticDetails.receiver, _BTIntentFilter);
            }
            catch (Exception)
            {
                _logger.Println("a receiver setup failed");
                Console.WriteLine("The receiver setup failed");
                throw;
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            UnregisterReceiver(WiFiStaticDetails.receiver);
            UnregisterReceiver(USBStaticDetails._receiver);
            UnregisterReceiver(BluetoothStaticDetails.receiver);
        }

        //protected override void OnResume()
        //{
        //    base.OnResume();


        //    WiFiStaticDetails.receiver = new WiFiP2pDirectBroadcastReceiver(WiFiStaticDetails.manager, WiFiStaticDetails.channel, this);
        //    RegisterReceiver(WiFiStaticDetails.receiver, _wiFiIntentFilter);
        //}

        //protected override void OnPause()
        //{
        //    base.OnPause();
        //    //UnregisterReceiver(WiFiStaticDetails.receiver);
        //}

        public void RestartReciever()
        {
            RegisterReceiver(WiFiStaticDetails.receiver, _wiFiIntentFilter);
            RegisterReceiver(USBStaticDetails._receiver, _usbIntentFilter);
            RegisterReceiver(BluetoothStaticDetails.receiver, _BTIntentFilter);
        }

        //may need something to extend treads with send and receive 

        #endregion

        #region Permission 
        private const int RequestReadWriteExternalStorage = 2230;
        private const int RequestForManageAllFiles = 2231;
        private const int RequestUSBAccess = 2828;
        private TaskCompletionSource<bool> requestPermissionResult;

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {

            //for (var i = 0; permissions.Length <= i; i++)
            //{

            //    if (grantResults[i] != Permission.Granted)
            //    {
            //        grantResults[i] = Permission.Granted;
            //        //var status = await Permissions.RequestAsync<>().Result;
            //        //RequestPermission(permissions[i],0);
            //    }

            //}


            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        #region Usb Permissions
        //public bool CheckUSBPermissions()
        //{
        //    bool hasPermissions = false;

        //    var SDK = Build.VERSION.SdkInt;

        //    UsbManager _manager = USBStaticDetails._usbManager;

        //    //hasPermissions = _manager.HasPermission();

        //    if (!hasPermissions)
        //    {

        //        RequestUsbPermissions(_manager);
        //    }


        //    return hasPermissions;
        //}

        //private void RequestUsbPermissions(UsbManager _manager)
        //{
        //    ////PendingIntent mPermissionIntent = PendingIntent.GetBroadcast(this, 2828, new Intent(USBStaticDetails.ACTION_USB_PERMISSION), 0);
        //    //PendingIntent mPermissionIntent = PendingIntent.GetBroadcast(this, USBStaticDetails.USB_DEVICE_ATTACHED, new Intent(USBStaticDetails.ACTION_USB_PERMISSION), PendingIntentFlags.Immutable);
        //    ////_manager.RequestPermission(this, mPermissionIntent);
        //    ////_manager.RequestPermission(, mPermissionIntent);
        //    //mPermissionIntent.Send();
        //    ////mPermissionIntent.Send();
        //    ////StartActivityForResult(mPermissionIntent, RequestUSBAccess);

        //}
        #endregion

        #region storage
        public bool HasStoragePermission()
        {
            bool hasPermissions = false;

            var SDK = Build.VERSION.SdkInt;

            if (SDK >= BuildVersionCodes.Q)
            {
                // since sdk 30; stricter permissions requires special 'manage storage permission'
                // requires to go to system settings
                //hasPermissions = Android.OS.Environment.IsExternalStorageManager;
                PermissionStatus permissionsStatus = Permissions.CheckStatusAsync<Permissions.StorageWrite>().Result;

                if (permissionsStatus != PermissionStatus.Granted)
                {
                    RequestStoragePermission();
                }
                else
                {
                    hasPermissions = true;
                }
            }
            else if (SDK > BuildVersionCodes.M)
            {
                // since sdk 23-28 we request write/read external storage only
                hasPermissions =
                    (PackageManager.CheckPermission(Manifest.Permission.ReadExternalStorage, PackageName) == Permission.Granted
                    && PackageManager.CheckPermission(Manifest.Permission.WriteExternalStorage, PackageName) == Permission.Granted
                    );

            }
            else
            {
                // sdk bellow 23 no permissions needed
                hasPermissions = true;
            }

            return hasPermissions;
        }

        public Task<bool> RequestStoragePermission()
        {
            var SDK = Build.VERSION.SdkInt;

            if (SDK <= BuildVersionCodes.M)
            {
                return Task.FromResult(true);
            }
            else if (SDK <= BuildVersionCodes.Q)
            {
                requestPermissionResult ??= new TaskCompletionSource<bool>();

                // handled by callback 'OnRequestPermissionsResult'
                RequestPermissions(new string[] {
                    Manifest.Permission.ReadExternalStorage,
                    Manifest.Permission.WriteExternalStorage },
                                RequestReadWriteExternalStorage);

                return requestPermissionResult.Task;
            }
            else
            {
                requestPermissionResult ??= new TaskCompletionSource<bool>();

                try
                {
                    Intent intent = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                    intent.AddCategory(Android.Content.Intent.CategoryDefault);
                    intent.SetData(Android.Net.Uri.FromParts("package", PackageName, null));

                    // navigates to settings, when user dismisses them calls OnActivityResult with our constant
                    StartActivityForResult(intent, RequestForManageAllFiles);

                }
                catch (Exception)
                {
                    // this bad! (probably outdated 'permission model' as android likes to change them every once in a while)
                    return Task.FromResult<bool>(false);
                }

                return requestPermissionResult.Task;
            }
        }
        #endregion
        #endregion

        ///This is included by default but don't know if it is needed
        //public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        //{
        //    Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        //    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        //}
    }
}