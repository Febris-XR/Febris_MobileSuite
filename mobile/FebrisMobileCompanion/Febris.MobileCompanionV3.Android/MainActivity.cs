// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Febris.SharedMobileLibrary.P2pNetworking;
using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.MobileCompanionV3.Droid.Networking.WiFi;
using Android.Net.Wifi.P2p;
using Android.Bluetooth;
using Android;
using Febris.MobileCompanionV3.Droid.Utilities;
using Xamarin.Forms;
using Febris.MobileCompanionV3.Utilities;
using Android.Widget;
using Febris.MobileCompanionV3.Resources;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Febris.MobileCompanionV3.Droid.Utilities.Installer;
using Febris.MobileCompanionV3.Droid.Services;
using AndroidX.Core.Content;

namespace Febris.MobileCompanionV3.Droid
{
    [Activity(Label = "Febris.MobileCompanionV3", Icon = "@mipmap/Febris_Companion", Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize
        )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        #region wifi broadcast        
        internal readonly IntentFilter _wifiP2pIntentFilter = new IntentFilter();
        internal readonly IntentFilter _statementIntentFilter = new IntentFilter();
        private DataProtection _dataProtection = new DataProtection();
        WiFiService _wiFi = new WiFiService();
        WiFiP2pServer _wiFiP2PServer = new WiFiP2pServer();
        //Context _context = Android.App.Application.Context;
        //Looper backgroundLooper = Looper.MyLooper();

        #endregion
        internal readonly IntentFilter _packageIntentFilter = new IntentFilter();

        // Bug fix: previously `OnDestroy` called `UnregisterReceiver(new StatementReceiver())`
        // -- a fresh, never-registered instance, so the unregister was a no-op and the
        // real receiver leaked across activity restarts. Store the registered instance
        // here and unregister the same instance.
        private Febris.MobileCompanionV3.Droid.Services.StatementReceiver _statementReceiver;

        /// <summary>
        /// Appends the runtime permissions Android added after this permission list was last
        /// touched, guarded by API level so the array stays valid on older devices.
        ///
        /// WHY THIS IS NOT OPTIONAL. This head targets SDK 33, so Android 13 enforcement
        /// applies in full:
        ///   - NEARBY_WIFI_DEVICES (API 33) gates WiFi Direct. Discovery and connection throw
        ///     SecurityException without it. ACCESS_FINE_LOCATION stopped covering nearby-device
        ///     APIs at 13, so the existing location grants do NOT substitute.
        ///   - POST_NOTIFICATIONS (API 33) gates every notification, including the ongoing one
        ///     the screen-capture foreground service is required to display.
        /// Declaring them in the manifest is not enough; both are dangerous permissions and
        /// must be requested at runtime.
        ///
        /// Requesting a permission the running platform does not know is harmless, the system
        /// ignores it, but the guards keep the intent legible.
        /// </summary>
        private static string[] AddModernRuntimePermissions(string[] basePermissions)
        {
            var all = new List<string>(basePermissions);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)   // API 33
            {
                all.Add(Manifest.Permission.NearbyWifiDevices);
                all.Add(Manifest.Permission.PostNotifications);
            }

            // BLUETOOTH REMOVED 2026-09-01. This block used to request BluetoothScan,
            // BluetoothConnect and BluetoothAdvertise on API 31 and above. Two things were wrong
            // with it. The comment claimed they were "declared in the manifest already" and they
            // are not, they are commented out there, so Android ignored the request outright and
            // the permissions could never be granted. And nothing needed them: onboarding is
            // numeric-comparison pairing over WiFi Direct, and the Companion's Bluetooth classes
            // were never instantiated. See CompanionData.ReadBluetoothNameOrEmpty for the one
            // remaining Bluetooth read, which is documented as allowed to return empty.

            return all.ToArray();
        }

        /// <summary>
        /// Sends the user to the "install unknown apps" settings screen when that permission
        /// is not held. Separated out and called after the UI loads, see the note at the call
        /// site: doing it during OnCreate backgrounded the activity before Forms initialized.
        ///
        /// This is a special permission and cannot be granted through RequestPermissions, so
        /// the settings navigation is the only route.
        /// </summary>
        private void RequestInstallPackagesPermissionIfNeeded()
        {
            try
            {
                if (!PackageManager.CanRequestPackageInstalls())
                {
                    Intent request = new Intent(Android.Provider.Settings.ActionManageUnknownAppSources);
                    request.SetData(Android.Net.Uri.FromParts("package", PackageName, null));
                    //can change this to a activity for result
                    StartActivity(request);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

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

                            // The Companion keeps no device list, so it stores the key and stops.
                            bool saved = !string.IsNullOrWhiteSpace(await P2pPairingCoordinator.ConfirmAsync(
                                peerIdentifierForConfirm == null ? null : peerIdentifierForConfirm(code)));

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

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // FIRST, so anything that fails afterwards can say so. This head logs through
            // mono-stdout today, which is exactly the plumbing that silently died on the Server
            // head and cost an evening of guessing. Android.Util.Log does not depend on it.
            Console.SetOut(new Utilities.AndroidLogWriter());
            Console.WriteLine("Febris Companion starting; console routed to logcat");

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            #region Permissions

            string[] permissions = {
            Manifest.Permission.AccessFineLocation,
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessWifiState,
            Manifest.Permission.ChangeWifiState,
            Manifest.Permission.Internet,

            Manifest.Permission.BluetoothAdmin,
            Manifest.Permission.Bluetooth,            
            Manifest.Permission.BluetoothPrivileged,
            Manifest.Permission.AccessNetworkState,
            Manifest.Permission.InstallPackages,
            Manifest.Permission.InstallShortcut,
            Manifest.Permission.RequestInstallPackages,
            Manifest.Permission.DeletePackages,

            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage,
            Manifest.Permission.ManageExternalStorage
            };
            //PermissionChecker(permissions).Wait();
            //RequestPermissionsAsync()
            RequestPermissions(AddModernRuntimePermissions(permissions), 0);

            #region Bluetooth Permissions   
            //if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Bluetooth) != (int)Permission.Granted)
            //{
            //    Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.Bluetooth }, requestCode);
            //}
            //try
            //{
            //    if (!PackageManager.CanRequestPackageInstalls())
            //    {
            //        //Activity.RequestPermissions(this, new string[] {Manifest.Permission.RequestInstallPackages}, 1);
            //        //ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.RequestInstallPackages }, 1);

            //        //StartActivity(new Intent(
            //        //    Android.Provider.Settings.ActionApplicationDetailsSettings,
            //        //    Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName)));

            //        Intent request = new Intent(Android.Provider.Settings.ActionBluetoothSettings);
            //        request.SetData(Android.Net.Uri.FromParts("package", PackageName, null));
            //        //can change this to a activity for result
            //        StartActivity(request);

            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}

            #endregion




            #region Storage
            //bool ready = await HasStoragePermission();
            //if (!ready)
            //{
            //    Console.WriteLine("STORAGE PERMISSION DOES NOT EXIST!!!");
            //}
            #endregion

            #endregion

            LoadApplication(new App());

            // The Companion files the PSK under the GROUP OWNER'S WiFi MAC, not under any
            // identifier the peer asserts. See docs/MOBILE_AUTH.md 4.4: each side keys by the
            // stable name it holds for the other device, and the Companion has no device
            // identifier for the Server.
            //
            // TWO SOURCES, same as ClientSocketThread.ResolveServerIdentity, and for the same
            // reason. The view-model address is only written by OnConnectSuccessChanged when
            // the group-info event happens to arrive with networkInfo already connected. On a
            // fresh device the group info beat the connection state, the write was skipped, and
            // the ceremony reached this lambda with nothing: ConfirmAsync(null) refused the
            // save while the Server had already committed its half, a one-sided pairing created
            // by the normal UI path with no error anywhere. The live group is the second source
            // of the SAME value, so a null here means neither source has it and the refusal is
            // correct.
            _ = InitialisePairingAsync(_ =>
                !string.IsNullOrWhiteSpace(LocalHardwareStaticDetails.StaticMainVM?.ConfigVM?.P2pGroup?.OwnerAddress)
                    ? LocalHardwareStaticDetails.StaticMainVM.ConfigVM.P2pGroup.OwnerAddress
                    : WiFiStaticDetails.WifiGroup?.Owner?.DeviceAddress);

            // ASKED AFTER THE UI EXISTS, DELIBERATELY.
            //
            // This block used to run BEFORE LoadApplication, and StartActivity navigates the
            // user away to a Settings screen. So MainActivity backgrounded itself before Forms
            // had initialized and before anything was on screen. Two consequences:
            //
            //   1. Under the Visual Studio debugger the app appears to FREEZE on its splash.
            //      The debugger is waiting for the app to connect while the activity is
            //      already paused behind Settings. Launched from the device icon the same
            //      build runs fine, which makes it look like a debugger fault rather than
            //      an ordering problem in our own OnCreate.
            //   2. Backing out of Settings left the user staring at a splash screen with no
            //      UI ever having been created.
            //
            // Requesting it after LoadApplication keeps the behaviour (the permission is
            // still asked for on every launch until granted) while the app has a rendered
            // page to come back to.
            RequestInstallPackagesPermissionIfNeeded();

            //WiFiStaticDetails._dataProtection = DependencyService.Get<IDataProtection>();





            #region wifi p2p broadcast
            WiFiP2pManagerSetup();
            _wifiP2pIntentFilter.AddAction(WifiP2pManager.WifiP2pStateChangedAction);
            _wifiP2pIntentFilter.AddAction(WifiP2pManager.WifiP2pPeersChangedAction);
            _wifiP2pIntentFilter.AddAction(WifiP2pManager.WifiP2pConnectionChangedAction);
            _wifiP2pIntentFilter.AddAction(WifiP2pManager.WifiP2pThisDeviceChangedAction);
            _wifiP2pIntentFilter.AddAction(WifiP2pManager.WifiP2pDiscoveryChangedAction);
            WiFiStaticDetails._intentFilter = _wifiP2pIntentFilter;





            //wiFi.ListenForServices();
            //wiFi.DiscoveringPeers();
            #endregion

            #region bluetooth
            //BluetoothHelper._manager = (BluetoothManager)base.GetSystemService(BluetoothService);
            // VideoUtility was deleted 2026-07-26. It was a dead near-duplicate of the old
            // screenshot loop and this construction was already commented out. Screen
            // capture now lives behind IScreenEncoder (Services/ScreenEncoder.cs).


            /////Make this device more identifiable
            /////
            ////BluetoothDevice device = (BluetoothDevice)base.GetSystemService(BluetoothService);
            ////BluetoothProfile profile = (BluetoothProfile)base.GetSystemService(BluetoothService);
            //UniqueIdentifier uniqueIdentifier = new UniqueIdentifier();
            //string uId = uniqueIdentifier.GetIdentifier();
            //LocalHardwareStaticDetails._thisDevice = new SharedMobileLibrary.Models.CompanionDevice()
            //{
            //    BlueToothName = BluetoothHelper._manager.Adapter.Name,
            //    BlueToothMacAddress = BluetoothHelper._manager.Adapter.Address,
            //    UniqueIdentifier = uId//,
            //    //WiFiDeviceName = WiFiStaticDetails.manager.
            //};

            #endregion

            #region Package installing


            PackageManagerSetup();
            _packageIntentFilter.AddAction(Intent.ActionInstallPackage);
            _packageIntentFilter.AddAction(Intent.ActionInstallFailure);
            _packageIntentFilter.AddAction(Intent.ExtraInstallerPackageName);
            _packageIntentFilter.AddAction(Intent.ActionUninstallPackage);
            _packageIntentFilter.AddAction(Intent.ActionPackagesSuspended);
            _packageIntentFilter.AddAction(Intent.ActionPackagesUnsuspended);
            _packageIntentFilter.AddAction(Intent.ActionPackageInstall);
            _packageIntentFilter.AddAction(Intent.ActionPackageRestarted);
            _packageIntentFilter.AddAction(Intent.ActionPackageReplaced);
            _packageIntentFilter.AddAction(PackageInstaller.ActionSessionCommitted);
            _packageIntentFilter.AddAction(PackageInstaller.ActionSessionUpdated);
            _packageIntentFilter.AddDataScheme("package");
            //_packageIntentFilter.AddAction(ModulePackageUtilityStaticDetails.StaticModulePackageUtility.PACKAGE_INSTALLED_ACTION);
            ModulePackageUtilityStaticDetails._intentFilter = _packageIntentFilter;


            //add intent filter?
            //ModulePackageUtilityStaticDetails.packageInstallObserver = PackageInstaller.SessionCallback();

            #endregion

            #region statement listener
            _statementIntentFilter.AddAction(StatementStaticDetails.StatementCreation);
            _statementIntentFilter.AddAction(StatementStaticDetails.StatementUpdate);
            _statementIntentFilter.AddAction(StatementStaticDetails.StatementError);
            StatementStaticDetails._intentFilter = _statementIntentFilter;

            // MOB-B1 blocker 2: the StatementReceiver is now registered at APPLICATION
            // scope (MainApplication.OnCreate) so it survives this Activity being torn
            // down while a launched sim is foregrounded. Registering it here too would
            // double-deliver, so the Activity-scoped registration is removed.
            //_statementReceiver = new StatementReceiver();
            //RegisterReceiver(_statementReceiver, StatementStaticDetails._intentFilter);

            #endregion

            #region Test data generation            
            //TestStatements.AddTestStatements();
            #endregion

            #region start package utility
            ModulePackageUtilitySetup();
            #endregion

        }



        #region Permission 
        private const int RequestReadWriteExternalStorage = 2230;
        private const int RequestForManageAllFiles = 2231;

        private TaskCompletionSource<bool> requestPermissionResult;

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            if (requestCode == RequestForManageAllFiles)
            {
                // Check if the permission request was successful
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    // Permission granted, proceed with your code
                    Toast.MakeText(this, "Permission granted", ToastLength.Short).Show();
                }
                else
                {
                    // Permission denied, handle accordingly
                    Toast.MakeText(this, "Permission denied", ToastLength.Short).Show();
                }
            }
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

        private void RequestManageExternalStoragePermission()
        {
            try
            {
                Intent intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                intent.AddCategory(Android.Content.Intent.CategoryDefault);
                intent.SetData(Android.Net.Uri.FromParts("package", PackageName, null));

                // Navigates to settings, when the user dismisses them, calls OnActivityResult with our constant
                StartActivityForResult(intent, RequestForManageAllFiles);
            }
            catch (Exception ex)
            {
                // Handle exception appropriately
                Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Short).Show();
            }
        }

        #region Package install
        //public bool HasPackageInstallPermission()
        //{
        //    bool hasPermissions = false;

        //    var SDK = Build.VERSION.SdkInt;

        //    if (SDK >= BuildVersionCodes.Q)
        //    {
        //        // since sdk 30; stricter permissions requires special 'manage storage permission'
        //        // requires to go to system settings
        //        //hasPermissions = Android.OS.Environment.IsExternalStorageManager;
        //        PermissionStatus permissionsStatus = Permissions.CheckStatusAsync<Permissions.StorageWrite>().Result;

        //        if (permissionsStatus != PermissionStatus.Granted)
        //        {
        //            RequestStoragePermission();
        //        }
        //        else
        //        {
        //            hasPermissions = true;
        //        }
        //    }
        //    else if (SDK > BuildVersionCodes.M)
        //    {
        //        // since sdk 23-28 we request write/read external storage only
        //        hasPermissions =
        //            (PackageManager.CheckPermission(Manifest.Permission.ReadExternalStorage, PackageName) == Permission.Granted
        //            && PackageManager.CheckPermission(Manifest.Permission.WriteExternalStorage, PackageName) == Permission.Granted);

        //    }
        //    else
        //    {
        //        // sdk bellow 23 no permissions needed
        //        hasPermissions = true;
        //    }

        //    return hasPermissions;
        //}
        //public Task<bool> RequestPackageInstallPermission()
        //{
        //    var SDK = Build.VERSION.SdkInt;

        //    if (SDK <= BuildVersionCodes.M)
        //    {
        //        return Task.FromResult(true);
        //    }
        //    else if (SDK <= BuildVersionCodes.Q)
        //    {
        //        requestPermissionResult ??= new TaskCompletionSource<bool>();

        //        // handled by callback 'OnRequestPermissionsResult'
        //        RequestPermissions(new string[] {
        //            Manifest.Permission.InstallPackages,
        //            Manifest.Permission.UninstallShortcut,
        //            Manifest.Permission.InstallShortcut,
        //            Manifest.Permission.RequestInstallPackages,
        //        },
        //        RequestReadWriteExternalStorage);

        //        return requestPermissionResult.Task;
        //    }
        //    else
        //    {
        //        requestPermissionResult ??= new TaskCompletionSource<bool>();

        //        try
        //        {
        //            Intent intent = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
        //            intent.AddCategory(Android.Content.Intent.CategoryDefault);
        //            intent.SetData(Android.Net.Uri.FromParts("package", PackageName, null));

        //            // navigates to settings, when user dismisses them calls OnActivityResult with our constant
        //            StartActivityForResult(intent, RequestForManageAllFiles);

        //        }
        //        catch (Exception)
        //        {
        //            // this bad! (probably outdated 'permission model' as android likes to change them every once in a while)
        //            return Task.FromResult<bool>(false);
        //        }

        //        return requestPermissionResult.Task;
        //    }
        //}
        /// <summary>
        /// https://stackoverflow.com/questions/47872162/how-to-use-packagemanager-canrequestpackageinstalls-in-android-oreo
        /// </summary>
        /// <returns></returns>
        private bool HasPackageInstallPermission()
        {
            bool output = false;

            //PermissionStatus hasPermissions2 = Permissions.CheckStatusAsync<Manifest.Permission.RequestInstallPackages>().Result;
            Permission hasPermissions = CheckSelfPermission(Manifest.Permission.RequestInstallPackages);
            if (hasPermissions != Permission.Granted)
            {

                //StartActivity(new Intent(
                //    Android.Provider.Settings.ActionApplicationDetailsSettings,
                //    Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName)));

                //Intent request = new Intent(Android.Provider.Settings.ActionManageUnknownAppSources)
                //    .SetData(Android.Net.Uri.Parse(string.Format("package", Android.App.Application.Context.PackageName)));

                Intent request = new Intent(Android.Provider.Settings.ActionManageUnknownAppSources);
                request.SetData(Android.Net.Uri.FromParts("package", PackageName, null));

                StartActivityForResult(request, 1234);




                //PermissionInfo pi = new PermissionInfo();
                //pi.name = myCustomPermission;
                //pi.labelRes = R.string.permission_label;
                //pi.protectionLevel = PermissionInfo.PROTECTION_DANGEROUS;
                //final PackageManager packageManager = getApplicationContext().getPackageManager();
                //packageManager.addPermission(pi);
                //Permissions.RequestAsync<Manifest.Permission.RequestInstallPackages>();
                ////this.packagein.RequestDragAndDropPermissions 
                //this.PackageManager.AddPermissionAsync();
            }
            else
            {
                output = true;
            }

            return output;
        }
        #endregion

        #region storage

        /// <summary>
        /// creates the pop up request
        /// </summary>
        /// <returns></returns>
        //public async Task<bool> HasStoragePermission()
        //{
        //    bool hasPermissions = false;

        //    try
        //    {
        //        hasPermissions = ExternalFileProvider.IsPermissionGranted(this);
        //        if (!hasPermissions)
        //        {
        //            new AlertDialog.Builder(this).SetTitle("All files permission")
        //                .SetMessage("Due to Android 11 restrictions, this application requires all files permission")
        //                .SetPositiveButton("Allow", (senderAlert, args) =>
        //                {
        //                    // Request the permission when the user clicks "Allow"
        //                    //AndroidX.Core.App.ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadExternalStorage }, RequestForManageAllFiles);
        //                    //RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage }, RequestForManageAllFiles);
        //                    RequestManageExternalStoragePermission();
        //                }).SetNegativeButton("Deny", (senderAlert, args) =>
        //                {
        //                    // Handle the case when the user denies the permission
        //                    Toast.MakeText(this, "Permission denied", ToastLength.Short).Show();
        //                })
        //                .SetIcon(Android.Resource.Drawable.IcDialogAlert)
        //                .Show();

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Toast.MakeText(this, "Permission Already Granted", ToastLength.Long).Show();
        //        Console.WriteLine("OnStart Error: " + ex.Message);
        //    }

        //    #region I dont think this works properly

        //    //var SDK = Build.VERSION.SdkInt;

        //    //if (SDK >= BuildVersionCodes.Q)
        //    //{
        //    //    // since sdk 30; stricter permissions requires special 'manage storage permission'
        //    //    // requires to go to system settings
        //    //    //hasPermissions = Android.OS.Environment.IsExternalStorageManager;
        //    //    //var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        //    //    PermissionStatus permissionsStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        //    //    //PermissionStatus permissionsStatus = await Permissions.CheckStatusAsync<Permissions.ManageExternalStorage>();
        //    //    //PermissionStatus permissionsStatus = await Permissions.CheckStatusAsync<Manifest.Permission.WriteExternalStorage>();//.ManageExternalStorage>(); 


        //    //    if (permissionsStatus != PermissionStatus.Granted)
        //    //    {
        //    //        hasPermissions = await RequestStoragePermission();
        //    //    }
        //    //    else
        //    //    {
        //    //        hasPermissions = true;
        //    //    }
        //    //}
        //    //else if (SDK > BuildVersionCodes.M)
        //    //{
        //    //    // since sdk 23-28 we request write/read external storage only
        //    //    hasPermissions =
        //    //        (PackageManager.CheckPermission(Manifest.Permission.ReadExternalStorage, PackageName) == Permission.Granted
        //    //        && PackageManager.CheckPermission(Manifest.Permission.WriteExternalStorage, PackageName) == Permission.Granted);

        //    //}
        //    //else
        //    //{
        //    //    // sdk bellow 23 no permissions needed
        //    //    hasPermissions = true;
        //    //}
        //    #endregion

        //    return hasPermissions;
        //}

        #region unused
        //public async Task<bool> RequestStoragePermission()
        //{
        //    var SDK = Build.VERSION.SdkInt;

        //    if (SDK <= BuildVersionCodes.M)
        //    {
        //        return true;
        //        //return Task.FromResult(true);
        //    }
        //    else if (SDK <= BuildVersionCodes.Q)
        //    {
        //        System.Console.WriteLine("sdk is 10 or under for permissions is hit.");
        //        requestPermissionResult ??= new TaskCompletionSource<bool>();

        //        // handled by callback 'OnRequestPermissionsResult'
        //        RequestPermissions(new string[] {
        //            Manifest.Permission.ReadExternalStorage,
        //            Manifest.Permission.WriteExternalStorage },
        //            RequestReadWriteExternalStorage);

        //        return await requestPermissionResult.Task;
        //    }
        //    else
        //    {
        //        System.Console.WriteLine("sdk is over 11 for permissions is hit.");
        //        requestPermissionResult ??= new TaskCompletionSource<bool>();

        //        try
        //        {
        //            // Request 'MANAGE_EXTERNAL_STORAGE' permission
        //            RequestPermissions(new string[] { Manifest.Permission.ManageExternalStorage }, RequestForManageAllFiles);
        //            //await Permissions.RequestAsync<Manifest.Permission.ManageExternalStorage>();

        //            #region this works I think?
        //            //Intent intent = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
        //            //intent.AddCategory(Android.Content.Intent.CategoryDefault);
        //            //intent.SetData(Android.Net.Uri.FromParts("package", PackageName, null));

        //            //// navigates to settings, when user dismisses them calls OnActivityResult with our constant
        //            //StartActivityForResult(intent, RequestForManageAllFiles);
        //            #endregion

        //        }
        //        catch (Exception)
        //        {
        //            // this bad! (probably outdated 'permission model' as android likes to change them every once in a while)
        //            //return Task.FromResult<bool>(false);
        //            return false;
        //        }

        //        //return requestPermissionResult.Task;               
        //        return await requestPermissionResult.Task;
        //    }
        //}
        #endregion
        #endregion
        #endregion

        /// <summary> Call this in activity OnActivityResult override </summary>
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            try
            {
                // Screen-capture consent is handled BEFORE the non-Ok early return below,
                // deliberately. Declining the dialog is an ordinary outcome and the encoder
                // is awaiting this result, so if it were handled in the switch it would
                // never be reached on a decline and StartAsync would hang forever.
                if (requestCode == Services.ScreenCaptureConsent.RequestCode)
                {
                    Services.ScreenCaptureConsent.Deliver(resultCode, data);
                    return;
                }

                if (resultCode != Result.Ok)
                {
                    return;
                }

                switch (requestCode)
                {
                    case RequestForManageAllFiles:
                        {
                            if (Build.VERSION.SdkInt > BuildVersionCodes.Q)
                            {
                                if (Android.OS.Environment.IsExternalStorageManager)
                                {
                                    requestPermissionResult?.TrySetResult(true);
                                }
                                else
                                {
                                    requestPermissionResult?.TrySetResult(false);
                                }
                            }
                            break;
                        }
                    case 1234:
                        {
                            if (this.PackageManager.CanRequestPackageInstalls())
                            {
                                //callInstallProcess();
                            }

                            break;
                        }
                    case ModulePackageUtilityStaticDetails.INST_APP:
                        {
                            try
                            {
                                data = ModulePackageUtilityStaticDetails.InProgressInstallation;
                                Console.WriteLine(data.ToString());
                                if (resultCode.Equals(Result.Ok))
                                {

                                    try
                                    {

                                        StatusUpdateHelper.General("Application Installed Successfully");
                                        Console.WriteLine("Application has installed Check here for errors");
                                        //Android.Net.Uri packageUri = data.Data.EncodedPath;
                                        //string moduleName = data.Data..;
                                        //string packageNamePossible = data.GetStringExtra(PackageName);// "android.intent.extra.PACKAGE_NAME");
                                        //string packageName = packageUri.GetQueryParameter(packageUri.ToString()); // Adjust based on your URI structure
                                        //PackageInfo packageInfo = PackageManager.GetPackageInfo(packageName, PackageInfoFlags.MetaData);
                                        //await ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, uri.ToString(), success);
                                        //var extraIntent = data.inten.GetIntExtra();//.GetParcelableExtra("PackageInfo");
                                        //data = ModulePackageUtilityStaticDetails.InProgressInstallation;
                                        var uri = data.Data;
                                        var packageInfoObj = data.GetParcelableExtra("PackageInfo");
                                        PackageInfo packageInfo = default;
                                        if (packageInfoObj is PackageInfo outputObject)
                                        {
                                            packageInfo = outputObject;
                                        }
                                        string moduleDirectoryName = data.GetStringExtra("ModuleDirectoryName");

                                        Utilities.EventHandlers.ModuleEventHandlerHelper.ModuleIndexUpdate(moduleDirectoryName, packageInfo, uri.ToString(), true);
                                        //Utilities.EventHandlers.ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, packageUri.ToString(), true);                                        
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.StackTrace);
                                        throw;
                                    }
                                }
                                else
                                {
                                    StatusUpdateHelper.Error("Application Not Installed");
                                    Android.Net.Uri packageUri = data.Data;
                                    var packageInfoObj = data.GetParcelableExtra("PackageInfo");
                                    PackageInfo packageInfo = default;
                                    if (packageInfoObj is PackageInfo outputObject)
                                    {
                                        packageInfo = outputObject;
                                    }
                                    string moduleDirectoryName = data.GetStringExtra("ModuleDirectoryName");
                                    //string packageName = packageUri.GetQueryParameter(packageUri.ToString()); // Adjust based on your URI structure
                                    //PackageInfo packageInfo = PackageManager.GetPackageInfo(packageName, PackageInfoFlags.MetaData);
                                    //await ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, uri.ToString(), success);
                                    Utilities.EventHandlers.ModuleEventHandlerHelper.ModuleIndexUpdate(moduleDirectoryName, packageInfo, packageUri.ToString(), false);
                                    
                                }
                                ModulePackageUtilityStaticDetails.InProgressInstallation = default;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.StackTrace);
                                throw;
                            }
                            finally
                            {
                                Febris.MobileCompanionV3.Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = false;
                                //ModulePackageUtilityStaticDetails.ActionProcessing = false;
                            }

                            //if (resultCode.Equals(Result.Ok))
                            //{

                            //}
                            ////var extras = data.GetIntExtra.Extras;
                            ////var anotherintent = Intent.GetIntent;//.GetIntent();
                            //var a = data.GetStringExtra(InstallerStaticDetails.PackageInfoIntent);
                            ////var packageInfo = data.Extra(InstallerStaticDetails.PackageInfoIntent);

                            ////var moduleInfo = data.Extras(InstallerStaticDetails.moduleId);

                            //Console.WriteLine("OnActivityResult for app install is hit RESULT CODE: " + resultCode.ToString());

                            break;
                        }
                    case ModulePackageUtilityStaticDetails.UNINST_APP:
                        {
                            if (resultCode.Equals(Result.Ok))
                            {
                                StatusUpdateHelper.General("Application Removed Successfully");
                                Android.Net.Uri packageUri = data.Data;
                                string moduleDirectoryName = packageUri.GetQueryParameter(packageUri.ToString()); // Adjust based on your URI structure
                                PackageInfo packageInfo = PackageManager.GetPackageInfo(moduleDirectoryName, PackageInfoFlags.MetaData);
                                //await ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, uri.ToString(), success);
                                Utilities.EventHandlers.ModuleEventHandlerHelper.ModuleIndexUpdate(moduleDirectoryName, packageInfo, packageUri.ToString(), false);
                                break;
                            }
                            else
                            {
                                StatusUpdateHelper.Error("Application Not Removed");
                                Android.Net.Uri packageUri = data.Data;
                                string moduleDirectoryName = packageUri.GetQueryParameter(packageUri.ToString()); // Adjust based on your URI structure
                                PackageInfo packageInfo = PackageManager.GetPackageInfo(moduleDirectoryName, PackageInfoFlags.MetaData);
                                //await ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, uri.ToString(), success);
                                Utilities.EventHandlers.ModuleEventHandlerHelper.ModuleIndexUpdate(moduleDirectoryName, packageInfo, packageUri.ToString(), true);
                                break;
                            }

                            //var extras = data.GetIntExtra.Extras;
                            //var anotherintent = Intent.GetIntent;//.GetIntent();
                            //var a = data.GetStringExtra(InstallerStaticDetails.PackageInfoIntent);
                            //var packageInfo = data.Extra(InstallerStaticDetails.PackageInfoIntent);

                            //var moduleInfo = data.Extras(InstallerStaticDetails.moduleId);

                            //Console.WriteLine("OnActivityResult for app install is hit RESULT CODE: " + resultCode.ToString());
                            break;
                        }
                    //case RequestForManageAllFiles:
                    //    {


                    //        break;
                    //    }
                    default:
                        {
                            base.OnActivityResult(requestCode, resultCode, data);
                            break;
                        }


                }
                #region if statement stack
                //if (requestCode == RequestForManageAllFiles)
                //{
                //    if (Build.VERSION.SdkInt > BuildVersionCodes.Q)
                //    {
                //        if (Android.OS.Environment.IsExternalStorageManager)
                //        {
                //            requestPermissionResult?.TrySetResult(true);
                //        }
                //        else
                //        {
                //            requestPermissionResult?.TrySetResult(false);
                //        }
                //    }

                //}
                //else if (requestCode == 1234 && resultCode == Result.Ok)//Activity.RESULT_OK)
                //{
                //    if (this.PackageManager.CanRequestPackageInstalls())
                //    {
                //        //callInstallProcess();
                //    }
                //}
                //else if (requestCode == ModulePackageUtilityStaticDetails.INST_APP)
                //{
                //    if (resultCode.Equals(Result.Ok))
                //    {

                //    }
                //    //var extras = data.GetIntExtra.Extras;
                //    //var anotherintent = Intent.GetIntent;//.GetIntent();
                //    var a = data.GetStringExtra(InstallerStaticDetails.PackageInfoIntent);
                //    //var packageInfo = data.Extra(InstallerStaticDetails.PackageInfoIntent);

                //    //var moduleInfo = data.Extras(InstallerStaticDetails.moduleId);

                //    Console.WriteLine("OnActivityResult for app install is hit RESULT CODE: " + resultCode.ToString());
                //}
                //else
                //{
                //    base.OnActivityResult(requestCode, resultCode, data);
                //}
                #endregion
            }
            catch (Exception ex)
            {
                //_callback.OnFailure("Failed to hit failure");
                Console.WriteLine(ex.Message);
            }


        }

        /// <summary>
        /// Trying to get information from activity result back into the system in a useful state
        /// </summary>
        /// <param name="requestCode"></param>
        /// <param name="resultCode"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal async Task ActivityResultContract(int requestCode, Result resultCode, Intent data)
        {

        }


        #region managers
        public void WiFiP2pManagerSetup()
        {
            try
            {
                WiFiStaticDetails.manager = (WifiP2pManager)base.GetSystemService(WifiP2pService);
                WiFiStaticDetails.channel = WiFiStaticDetails.manager.Initialize(this, base.MainLooper, new FebrisChannelListener());
            }
            catch (Exception ex)
            {
                Console.WriteLine("P2pManager Setup:" + ex.Message);
            }
        }

        public void PackageManagerSetup()
        {
            try
            {
                ModulePackageUtilityStaticDetails._installer = this.PackageManager.PackageInstaller;//(ModulePackageUtilityStaticDetails)base.GetSystemService(PackageManager);
                //ModulePackageUtilityStaticDetails.channel = ModulePackageUtilityStaticDetails._installer.Initialize(this, base.MainLooper, new FebrisChannelListener());
            }
            catch (Exception ex)
            {
                Console.WriteLine("P2pManager Setup:" + ex.Message);
            }
        }
        #endregion


        #region Activity Events  
        protected override void OnStart()
        {
            base.OnStart();


            try
            {
                WiFiStaticDetails.receiver = new WiFiP2pDirectBroadcastReceiver(WiFiStaticDetails.manager, WiFiStaticDetails.channel, this);
                RegisterReceiver(WiFiStaticDetails.receiver, _wifiP2pIntentFilter);
                //ModulePackageUtilityStaticDetails.receiver = new PackageManagerBroadcastReceiver();// WiFiStaticDetails.manager, WiFiStaticDetails.channel, this);
                //RegisterReceiver(ModulePackageUtilityStaticDetails.receiver, ModulePackageUtilityStaticDetails._intentFilter);

                //RegisterReceiver(new StatementReciever(), StatementStaticDetails._intentFilter);
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnStart Error: " + ex.Message);
            }

            #region connection data collection
            WiFiStaticDetails.BTThisDevice = BluetoothAdapter.DefaultAdapter;
            #endregion

        }

        /// <summary>
        /// Releases the statement-upload hold when a simulation ends.
        ///
        /// <para><b>Why OnResume is the signal.</b> The Companion launches a module with
        /// <c>StartActivity</c> and has no handle on it afterwards. It has no reliable way to poll
        /// whether that package is still alive either: since Android 10
        /// <c>GetRunningAppProcesses</c> returns only the caller's own processes, and
        /// <c>UsageStatsManager</c> needs the special PACKAGE_USAGE_STATS access. What IS reliable
        /// is that this activity comes back to the foreground when the thing in front of it goes
        /// away, whether the learner finished it, backed out of it, or it crashed. That covers "over
        /// or closed" without depending on the simulation cooperating.</para>
        ///
        /// <para><b>What this does not catch, stated rather than hidden.</b> A simulation that is
        /// merely backgrounded without closing also brings this activity forward, so the hold is
        /// released while it could in principle still emit statements. That is the correct trade:
        /// the alternative is holding a learner's record indefinitely. The
        /// <see cref="LocalHardwareStaticDetails.MaxSimulationHold"/> cap covers the opposite case,
        /// where focus never returns at all. If runs ever need to survive backgrounding, the honest
        /// fix is a terminating xAPI statement from the simulation itself rather than more
        /// lifecycle guessing.</para>
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            try
            {
                if (!string.IsNullOrWhiteSpace(LocalHardwareStaticDetails.RunningSimulationPackage))
                {
                    LocalHardwareStaticDetails.ClearRunningSimulation("companion returned to the foreground");
                }
            }
            catch (Exception ex)
            {
                // Never let this stop the activity resuming. A missed release is covered by the cap.
                Console.WriteLine("OnResume: releasing the simulation hold failed: " + ex.Message);
            }
        }

        #region I think this is redundant
        /// <summary>
        /// Needed to check permissions for file management
        /// </summary>
        //protected override void OnResume()
        //{
        //    base.OnResume();
        //    try
        //    {
        //        if (!ExternalFileProvider.IsPermissionGranted(this))
        //        {
        //            new AlertDialog.Builder(this).SetTitle("All files permission")
        //                .SetMessage("Due to Android 11 restrictions, this application requires all files permission")
        //                .SetPositiveButton("Allow", (senderAlert, args) => {
        //                    // Request the permission when the user clicks "Allow"
        //                    AndroidX.Core.App.ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadExternalStorage }, RequestForManageAllFiles);
        //                    //RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage }, RequestForManageAllFiles);
        //                }).SetNegativeButton("Deny", (senderAlert, args) => {
        //                    // Handle the case when the user denies the permission
        //                    Toast.MakeText(this, "Permission denied", ToastLength.Short).Show();
        //                })
        //                .SetIcon(Android.Resource.Drawable.IcDialogAlert)
        //                .Show();

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Toast.MakeText(this, "Permission Already Granted", ToastLength.Long).Show();
        //        Console.WriteLine("OnStart Error: " + ex.Message);
        //    }

        //    #region connection data collection
        //    //WiFiStaticDetails.BTThisDevice = BluetoothAdapter.DefaultAdapter;
        //    #endregion
        //}
        #endregion

        protected override void OnRestart()
        {
            base.OnRestart();
            try
            {
                //WiFiStaticDetails.receiver = new WiFiP2pDirectBroadcastReceiver(WiFiStaticDetails.manager, WiFiStaticDetails.channel, this);
                //RegisterReceiver(WiFiStaticDetails.receiver, _intentFilter);
                //ModulePackageUtilityStaticDetails.receiver = new PackageManagerBroadcastReceiver();// WiFiStaticDetails.manager, WiFiStaticDetails.channel, this);
                //RegisterReceiver(ModulePackageUtilityStaticDetails.receiver, ModulePackageUtilityStaticDetails._intentFilter);


            }
            catch (Exception ex)
            {
                Console.WriteLine("OnRestart Error: " + ex.Message);
            }
        }

        protected override void OnDestroy()
        {            
            try
            {
                UnregisterReceiver(WiFiStaticDetails.receiver);
                //UnregisterReceiver(ModulePackageUtilityStaticDetails.receiver);
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnDestroy Error: " + ex.Message);
            } 
            try
            {
                // MOB-B1 blocker 2: the StatementReceiver is now owned by MainApplication
                // (app scope), so this Activity must NOT unregister it -- doing so would
                // kill the receiver the moment the Activity is torn down (e.g. when a sim
                // foregrounds), which is the exact bug we're fixing. Left commented as the
                // historical Activity-scoped teardown.
                //// Bug fix: unregister the SAME receiver instance that was
                //// registered in OnCreate, not a freshly-constructed one
                //// (a fresh instance was never registered, so the unregister
                //// was a no-op and the original receiver leaked).
                //if (_statementReceiver != null)
                //{
                //    UnregisterReceiver(_statementReceiver);
                //    _statementReceiver = null;
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnDestroy Error: " + ex.Message);
            }
            base.OnDestroy();
        }

        protected override void OnStop()
        {
            base.OnStop();
            try
            {
                //UnregisterReceiver(WiFiStaticDetails.receiver);
                //UnregisterReceiver(WiFiStaticDetails.receiver);
                //UnregisterReceiver(ModulePackageUtilityStaticDetails.receiver);
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnStop Error: " + ex.Message);
            }
        }
        #endregion


        #region Package installer
        public void ModulePackageUtilitySetup()
        {
            try
            {

                ModulePackageUtilityStaticDetails.StaticModulePackageUtility = new ModulePackageUtility();// this);                
                //PackageInstaller packageInstaller = base.PackageManager.PackageInstaller; 
                //PackageInstaller.SessionParams sessionParams = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall); 
                //sessionParams.SetAppPackageName("packageName");
            }
            catch (Exception ex)
            {
                StatusUpdateHelper.Error("An Error Occured While Initalizing Package Utility: " + ex.Message);
                Console.WriteLine("Package Installer Setup:" + ex.Message);
            }
        }

        #endregion


        #region Intent receiver
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            Bundle extras = intent.Extras;

            if (ModulePackageUtilityStaticDetails.StaticModulePackageUtility.PACKAGE_INSTALLED_ACTION.Equals(intent.Action))
            {
                int status = extras.GetInt(PackageInstaller.ExtraStatus);
                string message = extras.GetString(PackageInstaller.ExtraStatusMessage);

                switch (status)
                {                    
                    case (int)PackageInstallStatus.PendingUserAction:
                        {
                            // This test app isn't privileged, so the user has to confirm the install.
                            Intent confirmIntent = (Intent)extras.Get(Intent.ExtraIntent);
                            StartActivity(confirmIntent);
                            break;
                        }
                    case (int)PackageInstallStatus.Success:
                        {
                            Toast.MakeText(this, "Install succeeded!", ToastLength.Long).Show();
                            Android.Net.Uri packageUri = intent.Data;
                            string packageName = packageUri.GetQueryParameter(packageUri.ToString()); // Adjust based on your URI structure
                            PackageInfo packageInfo = PackageManager.GetPackageInfo(packageName, PackageInfoFlags.MetaData);
                            //await ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, uri.ToString(), success);
                            Utilities.EventHandlers.ModuleEventHandlerHelper.ModuleIndexUpdate(packageName, packageInfo, packageUri.ToString(), true);
                            break;
                        }
                    case (int)PackageInstallStatus.Failure:
                    case (int)PackageInstallStatus.FailureAborted:
                    case (int)PackageInstallStatus.FailureBlocked:
                    case (int)PackageInstallStatus.FailureConflict:
                    case (int)PackageInstallStatus.FailureIncompatible:
                    case (int)PackageInstallStatus.FailureInvalid:
                    case (int)PackageInstallStatus.FailureStorage:
                        {
                            Android.Net.Uri packageUri = intent.Data;
                            string packageName = packageUri.GetQueryParameter(packageUri.ToString()); // Adjust based on your URI structure
                            PackageInfo packageInfo = PackageManager.GetPackageInfo(packageName, PackageInfoFlags.MetaData);
                            //await ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, uri.ToString(), success);
                            Utilities.EventHandlers.ModuleEventHandlerHelper.ModuleIndexUpdate(packageName, packageInfo, packageUri.ToString(), false);
                            Toast.MakeText(this, "Install failed! " + status + ", " + message,
                                    ToastLength.Long).Show();
                            break;
                        }
                    default:
                        Toast.MakeText(this, "Unrecognized status received from installer: " + status,
                               ToastLength.Long).Show();
                        break;
                }
            }
          
        }
        #endregion



        public async Task PermissionChecker(string[] permissions)
        {
            foreach (var i in permissions)
            {

                //var status = await Permissions.CheckStatusAsync<i>();

            }
        }




    }

}