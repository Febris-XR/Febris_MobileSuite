// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Networking.WiFi;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.MobileCompanionV3.Droid.Utilities.Installer;
using Febris.MobileCompanionV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(Febris.MobileCompanionV3.Droid.Utilities.ModulePackageUtility))]
namespace Febris.MobileCompanionV3.Droid.Utilities
{
    public class ModulePackageUtilityStaticDetails
    {
        //public static string ApkUriPrefix = "com.febris.a";

        public static Intent InProgressInstallation { get; set; }

        public static ModulePackageUtility StaticModulePackageUtility { get; set; }

        public static PackageInstallObserver packageInstallObserver { get; set; }

        public static PackageManagerBroadcastReceiver receiver { get; set; }
        public static IntentFilter _intentFilter { get; set; }
        public static PackageInstaller _installer { get; set; }

        public const int INST_APP = 6524;
        public const int UNINST_APP = 8455;

        /// <summary>
        /// This is used for blocking internally -- moved to localHarwareStaticDetails
        /// </summary>
        //public static bool ActionProcessing = false;

        //public static PackageManager.Channel channel { get; internal set; }
    }
    public class ModulePackageUtility : IModulePackageUtility
    {
        #region variables and constructors
        //private MainActivity _mainActivity;
        //public string PACKAGE_INSTALLED_ACTION =
        //    "com.example.android.apis.content.SESSION_API_PACKAGE_INSTALLED";
        public string PACKAGE_INSTALLED_ACTION = "com.febris.";
        public static string _moduleFiles = "modulefiles";
        public static string _publicFiles = "public_files";
        Context _context = Android.App.Application.Context;
        Looper backgroundLooper = Looper.MyLooper();
        private object sessionCreationLock = new object();

        public static PackageManager _manager { get; set; }

        public static PackageInstallObserver _callback { get; set; }

        //public ModulePackageUtility(MainActivity mainActivity)
        //{
        //    //_mainActivity = (MainActivity)getActivity.;
        //    //var context = Android.App.Application.Context;
        //    _mainActivity = mainActivity;
        //}

        //IExternalPlatformFileSystem _externalPlatformFileSystem = DependencyService.Get<IExternalPlatformFileSystem>();

        public ModulePackageUtility()
        {
            _context = Android.App.Application.Context;
            //_manager = (PackageManager)Forms.Context.GetSystemService(Context.packag);
            _manager = _context.PackageManager;
            //_context = ;


        }

        #endregion

        #region GetList
        public async Task<List<string>> GetInstalledList()
        {
            List<string> output = new List<string>();
            //Intent i;
            PackageManager _packageManager = _context.ApplicationContext.PackageManager;
            //PackageManager pm = PackageManager();
            try
            {
                var appList = _packageManager.GetInstalledApplications(PackageInfoFlags.MetaData);
                //if (i == null)
                //    throw new PackageManager.NameNotFoundException();
                //i.AddCategory(Intent.CategoryLauncher);
                //i.SetType("text/plain");
                //i.PutExtra("arguments", statementArguments);
                //_context.StartActivity(i);
                output = appList.Where(i =>
                i.ProcessName.StartsWith("com.febris")
                &&
                i.ProcessName != _context.PackageName
                ).Select(i => i.ProcessName).ToList();
            }
            catch (PackageManager.NameNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
            return output;
        }

        public async Task<List<string>> GetInstalledList(Dictionary<string, string> packageIndex)
        {
            List<string> output = new List<string>();
            //Intent i;
            //PackageManager _packageManager = _context.ApplicationContext.PackageManager;
            //PackageManager pm = PackageManager();
            try
            {
                PackageManager _packageManager = _context.ApplicationContext.PackageManager;
                var appList = _packageManager.GetInstalledApplications(PackageInfoFlags.MetaData);
                //foreach(var i in appList)
                //{
                //    Console.WriteLine(i.PackageName);
                //}

                output = packageIndex.Where(i => appList
                .Any(j => j.PackageName == i.Value))
                    .Select(i => i.Key).ToList() ?? default;

                //output = appList.Where(i=>packageIndex.Any(j=>j.Value==i.PackageName)).Select(i=>i.)


                //output = appList.Where(i =>
                //i.ProcessName.StartsWith("com.febris")
                //&&
                //i.ProcessName != _context.PackageName
                //).Select(i => i.ProcessName).ToList();
            }
            catch (PackageManager.NameNotFoundException e)
            {
                Console.WriteLine(e.Message);
                return new List<string>();
            }
            return output;
        }


        #endregion

        #region Intent Builder

        #endregion

        #region installing and uninstalling modules
        public async Task InstallModule(string moduleName, string filePath)
        {
            //if (ModulePackageUtilityStaticDetails.ActionProcessing) { return; }
            if (Resources.LocalHardwareStaticDetails.ActionProccessingBlocker) { return; }
            try
            {
                Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = true;
                //ModulePackageUtilityStaticDetails.ActionProcessing = true;

                #region installer set up on main

                ///https://stackoverflow.com/questions/59685804/android-packageinstaller-not-installing-apk/61889386#61889386
                //PackageInstaller packageInstaller = ModulePackageUtilityStaticDetails._installer;
                #endregion

                #region not set up in main
                /////https://stackoverflow.com/questions/59685804/android-packageinstaller-not-installing-apk/61889386#61889386
                //PackageInstaller packageInstaller = _context.PackageManager.PackageInstaller;
                #endregion

                #region Register callback
                //Device.BeginInvokeOnMainThread(() =>
                //{
                //    _callback = new PackageInstallObserver(packageInstaller);
                //    //observer.InstallFailed += EventHandlerHelper.OnInstallFailed; // Subscribe to event
                //    packageInstaller.RegisterSessionCallback(_callback);
                //});
                #endregion

                #region gather apk file path
                #region Post Android 11
                ///Gather the .apk file from the directory
                string apkFileName = await FileManager.GetDirectoryApkFiles(filePath);
                ///create the full path to the .apk file inside the directory
                string fullApkFilePath = Path.Combine(filePath, apkFileName);

                #endregion
                #region pre-Android 11
                //string apkFileName = await FileManager.GetDirectoryApkFiles(filePath);
                //string fullApkFilePath = Path.Combine(filePath, apkFileName);
                #endregion
                //Android.Net.Uri uri = Android.Net.Uri.FromFile(new Java.IO.File(fullApkFilePath));

                #region Post Android 11
                ///This one didn't work. I guess the uri needs to be built using the environment
                Java.IO.File externalFileBaseLine = new Java.IO.File(fullApkFilePath);
                Android.Net.Uri baselineUri = FileProvider.GetUriForFile(_context, _context.PackageName + ".provider", externalFileBaseLine);

                //string internalPath = Path.Combine(moduleName, apkFileName);
                //string pathToModulefolder = Path.Combine(SharedFileSystem._publicFileName, "Modules", "Modules");
                //Java.IO.File externalFile = Android.OS.Environment.GetExternalStoragePublicDirectory(pathToModulefolder);

                ///Trying this one using the preset filesystem paths -- This returns the directory Uri but not the apk Uri
                //string pathToModulefolder = FileSystem.ModulePath;                
                //Java.IO.File providerFile = new Java.IO.File(pathToModulefolder,moduleName);
                //Android.Net.Uri uri = FileProvider.GetUriForFile(_context, _context.PackageName + ".provider", providerFile);

                ///Trying this one by allowing the path to be thought of as an external public directory. Not sure if this works in 11
                //string pathToModulefolder = FileSystem.ModulePath;
                //Java.IO.File externalFile = Android.OS.Environment.GetExternalStoragePublicDirectory(pathToModulefolder);
                //Java.IO.File providerFile = new Java.IO.File(externalFile, moduleName);
                //Android.Net.Uri uri = FileProvider.GetUriForFile(_context, _context.PackageName + ".provider", providerFile);

                      
                ///Take the internal path and use it to find the external path --THIS ONE WORKS!!
                string internalpath = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModulePath, moduleName, apkFileName);
                Java.IO.File internalProviderFile = new Java.IO.File(internalpath);
                Android.Net.Uri uri = FileProvider.GetUriForFile(_context, _context.PackageName + ".provider", internalProviderFile);

                //Android.Net.Uri uri = FileProvider.GetUriForFile(_context, _context.PackageName+".provider", externalFile);


                //Android.Net.Uri uri = FileProvider.GetUriForFile(_context, _context.PackageName + ".provider", new Java.IO.File(internalPath));
                #endregion
                Console.WriteLine("uri using the full internal Apk File path: " + baselineUri.ToString());
                Console.WriteLine("uri using the second method being tested: " + uri.ToString());
                #region pre-Android 11
                //string externalpath = Path.Combine(moduleName, apkFileName);
                ////Java.IO.File externalFile = _context.GetExternalFilesDir(string.Empty);
                //string pathToModulefolder = Path.Combine(SharedFileSystem._publicFileName, "Modules", "Modules");                
                //Java.IO.File externalFile = Android.OS.Environment.GetExternalStoragePublicDirectory(pathToModulefolder);//.GetExternalStoragePublicDirectory();
                ////ContentProvider.
                //Android.Net.Uri uri = FileProvider.GetUriForFile(_context, _context.PackageName + ".provider", new Java.IO.File(externalFile, externalpath));
                #endregion

                #endregion


                PackageInfo packageInfo = _context.PackageManager.GetPackageArchiveInfo(fullApkFilePath, 0);

                await Task.Run(() => InstallPackage(uri, moduleName, packageInfo));



                ///This needs to be somewhere else like a callback                
               // await ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, uri.ToString(), true);


                #region currently unused

                //#region Pure intent build trial - previously install attempt here as well.
                /////Build the intent
                //#region Intent Builder
                ////Intent intent = new Intent();
                ////Intent intent = new Intent(_context,_context.Class);
                ////Intent intent = new Intent(Intent.ActionInstallPackage);
                //Intent intent = new Intent(_context, _callback.Class);

                //#region Action
                ////intent.SetAction("ACTIONINSTALLCOMPLETE");
                ////intent.SetAction(Intent.ActionPackageFirstLaunch);//.ActionInstallPackage);//.ActionView);
                ////intent.SetAction(Android.Provider.Settings.ActionManageUnknownAppSources);//I dont think this matters here because 
                //#endregion

                //#region Data
                ////intent.SetData(uri);
                //#endregion

                //#region Type
                ////intent.SetType("application/vnd.android.package-archive");
                //#endregion

                //#region Flags
                ////intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                ////intent.AddFlags(ActivityFlags.ClearTop);
                ////intent.AddFlags(ActivityFlags.NewTask);
                //#endregion

                //#region Extras
                ////intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                //#endregion

                //#region Class
                /////https://bitcoden.com/answers/install-apps-silently-with-granted-install_packages-permission
                ////intent.SetClass(_context,);

                //#endregion

                //#region ??

                ////intent.SetDataAndType(uri, "application/vnd.android.package-archive");


                ////PackageManager _packageManager = _context.ApplicationContext.PackageManager;
                ////intent = _packageManager.GetLaunchIntentForPackage("com.google.android.youtube");
                ////if (intent == null)
                ////    throw new PackageManager.NameNotFoundException();
                ////intent.AddCategory(Intent.CategoryLauncher);
                ////intent.SetType("text/plain");

                //#endregion
                //#endregion

                /////create a callback that can tell me what in the world is going on
                //#region Intent Callback session
                ////PendingIntent pendingIntent = PendingIntent.GetActivity(_context, 1654, intent, 0);
                ////IntentSender statusReceiver = pendingIntent.IntentSender;
                //#endregion

                /////try to start the activity
                ////_context.StartActivity(intent);
                //#endregion

                //#region trying adb shell - this did nothing
                ////_ = Java.Lang.Runtime.GetRuntime().Exec("install -g "+fullApkFilePath);


                //#endregion

                //#region using packagemanager

                //#region Session setup
                //PackageInstaller.SessionParams sessionParams = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);                
                ////sessionParams.SetReferrerUri("com.febris." + moduleName);
                ////sessionParams.SetAppPackageName(apkFileName);               
                //int sessionId = packageInstaller.CreateSession(sessionParams);
                //PackageInstaller.Session session = packageInstaller.OpenSession(sessionId);

                ////session.AppPackageName = "";
                ////var info = packageInstaller.GetSessionInfo(sessionId);

                ////I wonder if this is the issue?
                //session = AddApkToInstallSession(fullApkFilePath, session);                                
                //#endregion


                //#region Intent Builder - moved
                ////Intent intent = new Intent();

                ////#region Action
                //////intent.SetAction("ACTIONINSTALLCOMPLETE");
                ////intent.SetAction(Intent.ActionView);
                //////intent.SetAction(Android.Provider.Settings.ActionManageUnknownAppSources);//I dont think this matters here because 
                ////#endregion

                ////#region Data
                //////intent.SetData(uri);
                ////#endregion

                ////#region Type
                ////intent.SetType("application/vnd.android.package-archive");
                ////#endregion

                ////#region Flags
                ////intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                ////intent.AddFlags(ActivityFlags.ClearTop);
                ////intent.AddFlags(ActivityFlags.NewTask);
                ////#endregion

                ////#region Extras
                ////intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                ////#endregion

                ////#region Class
                ///////https://bitcoden.com/answers/install-apps-silently-with-granted-install_packages-permission
                //////intent.SetClass(_context,);

                ////#endregion

                ////#region ??
                //////intent.SetDataAndType(uri, "application/vnd.android.package-archive");



                ////#endregion

                //#endregion

                //#region Receiver (to get results) - unused?
                ////// Create an install status receiver.

                /////this is implicit
                ////Intent intent = new Intent(PACKAGE_INSTALLED_ACTION+moduleName);
                //// Intent.ActionInstallPackage);
                ////Intent intent = new Intent(Intent.ActionPackageInstall);
                ////Intent intent = new Intent(Intent.ActionPackageInstall);


                ////intent.SetAction(Intent.ActionInstallPackage); "ACTIONINSTALLCOMPLETE"


                /////This is explicit
                ////Intent intent = new Intent(_context, _callback.Class);// _context.Class);               
                ////Intent intent = new Intent(Android.Provider.Settings.ActionManageUnknownAppSources, uri);                
                ////Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName));
                ////Intent intent = new Intent(PACKAGE_INSTALLED_ACTION,uri);

                ////intent.SetAction(Intent.ActionPackageInstall);// PACKAGE_INSTALLED_ACTION);
                ////intent.SetAction(Android.Content.ActivityFlags)
                ////intent.SetAction(PACKAGE_INSTALLED_ACTION);




                //////intent.AddFlags(ActivityFlags.GrantWriteUriPermission);

                ////////intent.SetData(uri);
                ////////intent.Data = FileProvider.GetUriForFile(_context, fullApkFilePath, new Java.IO.File(_context.GetExternalFilesDir(Android.OS.Environment.DirectoryDownloads).ToString()+ moduleName));


                ////////intent.SetDataAndType(uri, "application/vnd.android.package-archive");
                //////intent.SetFlags(ActivityFlags.NewTask); // without this flag android returned a intent error!
                //////                                        //PendingIntent pendingIntent = PendingIntent.GetActivity(_context, 0, intent, 0);



                //#endregion

                //#region Intent Callback session
                //PendingIntent pendingIntent = PendingIntent.GetActivity(_context, 8675, intent, PendingIntentFlags.UpdateCurrent);
                //IntentSender statusReceiver = pendingIntent.IntentSender;
                //#endregion

                //var packageinfo = packageInstaller.GetSessionInfo(sessionId);
                //Console.WriteLine(packageinfo.ToString());                
                //session.Commit(statusReceiver);       
                ////session.
                //session.Close();
                //#endregion
                #endregion
            }
            catch (Exception ex)
            {
                StatusUpdateHelper.Error("An Error Occured While Package was being installed: " + ex.Message);
                Console.WriteLine("Module installer error: " + ex.Message);
                //throw;
                //ModulePackageUtilityStaticDetails.ActionProcessing = false;
                Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = false;
            }
            finally
            {
                //packageInstaller.UnregisterSessionCallback(_callback);
            }
        }

        private async Task InstallPackage(Android.Net.Uri uri, string moduleName, PackageInfo packageInfo)
        {
            try
            {


                //string newPackageName = ModulePackageUtilityStaticDetails.ApkUriPrefix + moduleName;

                if (uri == null)
                {
                    throw new Java.Lang.RuntimeException("Set the data uri to point to an apk location!");
                }
                // https://code.google.com/p/android/issues/detail?id=205827
                if ((Build.VERSION.SdkInt < BuildVersionCodes.N)
                        && (!ContentResolver.SchemeFile.Equals(uri.Scheme)))
                {
                    throw new Java.Lang.RuntimeException("PackageInstaller < Android N only supports file scheme!");
                }
                if ((Build.VERSION.SdkInt >= BuildVersionCodes.N)
                        && (!ContentResolver.SchemeContent.Equals(uri.Scheme)))
                {
                    throw new Java.Lang.RuntimeException("PackageInstaller >= Android N only supports content scheme!");
                }

                Intent intent = new Intent();

                // Note regarding ExtraNotUnknownSource:
                // works only when being installed as system-app
                // https://code.google.com/p/android/issues/detail?id=42253

                if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBean)
                {
                    intent.SetAction(Intent.ActionInstallPackage);
                    intent.SetData(uri);
                    intent.PutExtra(Intent.ExtraReturnResult, true);
                    intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                    intent.PutExtra(Intent.ExtraAllowReplace, true);
                }
                else if (Build.VERSION.SdkInt < BuildVersionCodes.N)
                {
                    intent.SetAction(Intent.ActionInstallPackage);
                    intent.SetData(uri);
                    intent.PutExtra(Intent.ExtraReturnResult, true);
                    intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                }
                else //if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                { // Android N
                    ///This works on Android 11 but is blocked by google play protect

                    intent.SetAction(Intent.ActionInstallPackage);
                    intent.SetData(uri);
                    // grant READ permission for this contentAndroid.Net.Uri
                    intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                    intent.PutExtra(Intent.ExtraReturnResult, true);
                    intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                    //intent.Package

                    
                }






                //else
                //{
                //    //lock (sessionCreationLock)
                //    //{
                //    ///https://stackoverflow.com/questions/59685804/android-packageinstaller-not-installing-apk
                //    // For Android 8.0 and later, use the PackageInstaller API
                //    PackageInstaller packageInstaller = _context.PackageManager.PackageInstaller;

                //    //PackageInstaller.SessionParams sessionParams = new PackageInstaller.SessionParams(PackageInstaller.SessionParams.Mode.FullInstall);
                //    PackageInstaller.SessionParams sessionParams = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);
                //    int sessionId = -1;


                //    sessionId = packageInstaller.CreateSession(sessionParams);
                //    PackageInstaller.Session session = packageInstaller.OpenSession(sessionId);
                //    AddApkToInstallSession(uri, session);

                //    ///Intent creation (if needed)
                //    /// intent = new Intent(); -- is already setup
                //    //intent = new Intent(_context, _context.Class);

                //    ///Can set class after the fact (not sure if needed because we are not targeting a class)
                //    //intent.SetClass(_context, _context.Class);

                //    ///Action
                //    //intent = new Intent(_context, _context.Class);// Intent.ActionPackageInstall);
                //    intent.SetAction(Intent.ActionInstallPackage);
                //    //intent.SetAction(Intent.ActionPackageInstall);

                //    ///Flags
                //    intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                //    ///Extras
                //    //intent.SetAction(ModulePackageUtilityStaticDetails.StaticModulePackageUtility.PACKAGE_INSTALLED_ACTION);
                //    //intent.SetAction("PACKAGE_INSTALLED_ACTION");
                //    intent.PutExtra(Intent.ExtraReturnResult, true);
                //    intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                //    //intent.PutExtras(Intent.ExtraNotUnknownSource);


                //    ///Trying to add something else that will hopefully start the intent
                //    //intent.AddFlags(ActivityFlags.NewTask);

                //    //Intent intentInstall = new Intent(Intent.ActionInstallPackage);
                //    //PendingIntent pendingIntent = PendingIntent.GetBroadcast(_context, 0, intentInstall, 0);
                //    try
                //    {
                //        //await Task.Run(() =>
                //        //{
                //        // Perform the installation process here
                //        intent = new Intent(_context, typeof(MainActivity));
                //        var pendingIntent = PendingIntent.GetActivity(_context, 0, intent, PendingIntentFlags.OneShot);
                //        //var pendingIntent = PendingIntent.GetActivity(_context, 0, intent, 0);
                //        var statusReceiver = pendingIntent.IntentSender;

                //        // Commit the session
                //        session.Commit(statusReceiver);
                //        //});

                //        ////var pendingIntent = PendingIntent.GetActivity(_context, 0, intent, 0);
                //        //var pendingIntent = PendingIntent.GetActivity(_context, 0, intent, PendingIntentFlags.OneShot);
                //        //var statusReceiver = pendingIntent.IntentSender;
                //        //session.Commit(statusReceiver);
                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine(ex.StackTrace);
                //        if (sessionId != -1)
                //        {
                //            try
                //            {
                //                packageInstaller.AbandonSession(sessionId);
                //            }
                //            catch (Exception ex2)
                //            {
                //                // Handle the abort session exception
                //                Console.WriteLine(ex2.StackTrace);
                //            }
                //        }
                //        //throw;
                //    }
                //    finally
                //    {
                //        if (session != null)
                //        {
                //            session.Close();
                //        }
                //    }
                //    // }
                //}
                //string packageInfoString = packageInfo.PackageName;

                //await ResultReciverSetup(uri, moduleName, packageInfo, intent);

                try
                {


                    ///Add the needed information to the intent
                    intent.PutExtra("ModuleDirectoryName", moduleName);
                    intent.PutExtra("PackageInfo", packageInfo);
                    //intent.Package
                    //intent.PutExtra("Uri",uri);
                    intent.SetData(uri);

                    intent.PutExtras(intent);//This may work? no idea


                    ///Added result data
                    ModulePackageUtilityStaticDetails.InProgressInstallation = intent;


                    ///Original -- This actually still works. -- Had to correct the Uri to point internally but still need the OnActivityResult to return a value so the install can be added to the database
                    ((Activity)Xamarin.Forms.Forms.Context).StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);
                    

                    ///new trial - needs an explicit activity called in the manifest. 
                    ///


                    ///This one makes the screen go black and ??? happen -- It may actually launch the apk without installing. In the logs Unity information was present and that is the game engine used.
                    //Platform.CurrentActivity.StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);


                    //((Activity)Xamarin.Forms.Forms.Context).StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);


                    //var currentActivity = Platform.CurrentActivity;
                    //currentActivity.StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);




                    ///Activity Not Found
                    //var mainActivity = Platform.CurrentActivity as MainActivity;
                    //mainActivity.StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);

                    ///This is was activity not found / bad cast
                    //((Activity)Xamarin.Forms.Forms.Context).StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);

                    //((Activity)Xamarin.Forms.Forms.Context).StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);

                    ///This is the main activity 
                    //_context.StartActivity(intent);//, ModulePackageUtilityStaticDetails.INST_APP);
                }
                catch (ActivityNotFoundException e)
                {
                    Console.WriteLine("PackageInstaller >= Android N only supports content scheme! : " + e.Message);
                    //ModulePackageUtilityStaticDetails.ActionProcessing = false;
                    Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = false;
                }
                // MP2P-6 follow-up: removed the inner finally that ran GC.Collect / WaitForPendingFinalizers
                // / GC.Collect. It was duplicate work -- the outer InstallPackage finally below runs anyway,
                // and one forced full GC per install is already on the high side. See the outer finally
                // for the Xamarin.Android JNI-handle rationale.

                #region not used
                //string packageInfoString = JsonConvert.SerializeObject(packageInfo, Formatting.Indented,
                //    new JsonSerializerSettings()
                //    {
                //        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                //    }
                //);
                //string packageInfoString = JsonConvert.SerializeObject(packageInfo);
                //intent.PutExtra(InstallerStaticDetails.moduleId, newPackageName);
                //intent.PutExtra(InstallerStaticDetails.PackageInfoIntent, packageInfoString);

                //try
                //{
                //    //Context _context = Android.App.Application.Context;
                //    //_context.StartActivity(intent);                
                //    //_context.StartActivityForResult(intent, REQUEST_CODE_INSTALL);

                //    //this works but never hits callback
                //    ((Activity)Xamarin.Forms.Forms.Context).StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);

                //    //((MainActivity)Android.App.Application.Context).StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);
                //    //MainActivity.StartActivityForResult(intent, ModulePackageUtilityStaticDetails.INST_APP);

                //    //EventHandlerHelper.AddToPackageIndex(moduleName,intent);
                //}
                //catch (ActivityNotFoundException e)
                //{
                //    Console.WriteLine("PackageInstaller >= Android N only supports content scheme! : " + e.Message);
                //    //Log.e(TAG, "ActivityNotFoundException", e);
                //    //installer.InstallerBroadcastReceiver(canonicalUri, Installer.ACTION_INSTALL_INTERRUPTED,
                //    //        "This Android rom does not support ACTION_INSTALL_PACKAGE!");
                //    //finish();
                //}
                #endregion

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Resources.LocalHardwareStaticDetails.ActionProccessingBlocker = false;
                //ModulePackageUtilityStaticDetails.ActionProcessing = false;
                throw;
            }
            finally
            {
                // MP2P-6 follow-up: this single GC.Collect / WaitForPendingFinalizers / GC.Collect
                // triple is retained at the outer boundary of the install flow. The flow allocates
                // managed wrappers around JNI handles (Java.IO.File, Android.Net.Uri via
                // FileProvider.GetUriForFile, Intent.PutExtra(IBinder), StartActivityForResult). On
                // older Xamarin.Android runtimes those wrappers can sit until the next GC, holding
                // their JNI handles. The recommended pattern is to force one collection at the end
                // of an infrequent, JNI-heavy operation -- see Xamarin.Android perf docs on
                // GC.Collect usage. Module install is exactly that: user-initiated, runs once per
                // module, the cost of a blocking GC is negligible compared to the network + apk
                // unzip + PackageInstaller dialog. Per-method GC.Collect calls (the prior anti-
                // pattern) were removed in MP2P-6.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        /// <summary>
        /// The Uri is not opening can try things like
        /// FileProvider.GetUriForFile(context, authority, file)
        /// 
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="session"></param>
        private void AddApkToInstallSession(Android.Net.Uri uri, PackageInstaller.Session session)
        {
            Stream packageInSession = default;
            Stream input = default;
            try
            {
                packageInSession = session.OpenWrite("COSU", 0, -1);

                ///testing
                var info = _context.ContentResolver.OpenFileDescriptor(uri, "r");

                input = _context.ContentResolver.OpenInputStream(uri);


                if (input != null)
                {
                    input.CopyTo(packageInSession);
                }
                else
                {
                    packageInSession.Close();
                    input.Close();
                    throw new Exception("Inputstream is null");
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                packageInSession.Close();
                input.Close();
            }

            ////That this is necessary could be a Xamarin bug.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private async Task ResultReciverSetup(Android.Net.Uri uri, string moduleName, PackageInfo packageInfo, Intent intent)
        {
            try
            {
                ///Get Info after the result it made
                InstallerResultReceiver resultReceiver = new InstallerResultReceiver(new Android.OS.Handler(backgroundLooper), async (resultCode, resultData) =>
                {
                    // Handle the result here
                    Console.WriteLine($"Result Code: {resultCode}");
                    // Process the resultData if needed

                    ///This will feed the resulting installation information back to the front end after a result is reached
                    bool success = false;
                    if (resultCode == (int)Result.Ok)
                    {
                        success = true;
                    }
                    await ModuleEventHandlerHelper.ModuleIndexUpdate(moduleName, packageInfo, uri.ToString(), success);


                    // Check if the message queue is empty before quitting the looper
                    if (!backgroundLooper.Queue.IsIdle)
                    {
                        // There are still pending messages in the queue
                        Console.WriteLine("Pending messages in the queue. Not quitting the looper yet.");
                    }
                    else
                    {
                        // No pending messages, safe to quit the looper
                        backgroundLooper.QuitSafely();
                        Console.WriteLine("Background looper quit safely.");
                    }

                });
                intent.PutExtra(Intent.ExtraResultReceiver, resultReceiver);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when trying to set up the Installer Result Receiver" + ex.StackTrace);
                throw;
            }
        }


        /// <summary>
        /// https://stackoverflow.com/questions/59685804/android-packageinstaller-not-installing-apk/61889386#61889386
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="session"></param>
        private static PackageInstaller.Session AddApkToInstallSession(/*string moduleName,*/ string filePath, PackageInstaller.Session session)
        {
            try
            {
                #region previous trys
                //string apkFileName = FileManager.GetDirectoryApkFiles(filePath);
                //string fullApkFilePath = Path.Combine(filePath, apkFileName);

                //Android.Net.Uri uri = Android.Net.Uri.Parse("file://" + fullApkFilePath);


                //string apkName = "com.febris.Package";// + moduleName;
                //Android.Net.Uri apkUri= Android.Net.Uri.Parse(filePath);

                //var input = _context.ContentResolver.OpenInputStream(apkUri)

                //change package to com.febris.Package
                //Stream packageInSession = session.OpenWrite("com.febris.packageinstall", 0, -1);
                //Stream packageInSession = session.OpenWrite("package", 0, -1);
                //Stream packageInSession = session.OpenWrite(ModulePackageUtilityStaticDetails.ApkUriPrefix + moduleName, 0, -1);

                //Stream input = Application.Context.ContentResolver.OpenInputStream(apkUri);
                #endregion
                Stream input = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                //Stream packageInSession = session.OpenWrite("package", 0, input.Length);
                Stream packageInSession = session.OpenWrite("package", 0, -1);

                if (input != null)
                {
                    input.CopyTo(packageInSession);
                    session.Fsync(packageInSession);
                }
                else
                {
                    throw new Exception("Inputstream is null");
                }

                packageInSession.Close();
                input.Close();

                //That this is necessary could be a Xamarin bug.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                //return session;


                //using (var input = new FileStream(fullApkFilePath, FileMode.Open, FileAccess.Read))
                //{
                //    //string apkFileName = FileManager.GetDirectoryContent(filePath);
                //    using (var packageInSession = session.OpenWrite(apkName, 0, -1))
                //    {
                //        input.CopyTo(packageInSession);
                //        packageInSession.Close();
                //    }
                //    input.Close();
                //}
                ////That this is necessary could be a Xamarin bug.
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
            }
            catch (Exception ex)
            {
                StatusUpdateHelper.Error("An Error Occured With Apk Install Session: " + ex.Message);
                //throw;
            }
            return session;
        }

        //private static void AddApkToInstallSession(/*Context context,*/ Android.Net.Uri apkUri, PackageInstaller.Session session)
        //{
        //    var packageInSession = session.OpenWrite("package", 0, -1);
        //    var input = context.ContentResolver.OpenInputStream(apkUri);

        //    try
        //    {
        //        if (input != null)
        //        {
        //            input.CopyTo(packageInSession);
        //        }
        //        else
        //        {
        //            throw new Exception("Inputstream is null");
        //        }
        //    }
        //    finally
        //    {
        //        packageInSession.Close();
        //        input.Close();
        //    }

        //    //That this is necessary could be a Xamarin bug.
        //    GC.Collect();
        //    GC.WaitForPendingFinalizers();
        //    GC.Collect();
        //}

        public async Task<bool> UninstallApplication(string uri)
        {
            bool output = false;
            try
            {
                #region Package installer

                //PackageInstaller packageInstaller = _context.ApplicationContext.PackageManager.PackageInstaller;

                //PackageInstaller.SessionParams sessionParams = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);
                //int sessionId = packageInstaller.CreateSession(sessionParams);
                //packageInstaller.Uninstall(packetName, PendingIntent.GetBroadcast(_context, sessionId, new Intent("android.intent.action.MAIN"), 0).IntentSender);

                #endregion


                #region Intent                

                Intent intent = new Intent();
                Android.Net.Uri formattedUri = Android.Net.Uri.Parse("package:" + uri);
                // Note regarding ExtraNotUnknownSource:
                // works only when being installed as system-app
                // https://code.google.com/p/android/issues/detail?id=42253

                if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBean)
                {
                    intent.SetAction(Intent.ActionDelete);
                    intent.SetData(formattedUri);
                    intent.PutExtra(Intent.ExtraReturnResult, true);
                    intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                    intent.PutExtra(Intent.ExtraAllowReplace, true);
                }
                else if (Build.VERSION.SdkInt < BuildVersionCodes.N)
                {
                    intent.SetAction(Intent.ActionDelete);
                    intent.SetData(formattedUri);
                    intent.PutExtra(Intent.ExtraReturnResult, true);
                    intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                }
                else
                { // Android N
                    intent.SetAction(Intent.ActionDelete);
                    intent.SetData(formattedUri);
                    // grant READ permission for this contentAndroid.Net.Uri
                    intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                    intent.PutExtra(Intent.ExtraReturnResult, true);
                    intent.PutExtra(Intent.ExtraNotUnknownSource, true);
                    //intent.Package
                }

                ModulePackageUtilityStaticDetails.InProgressInstallation = intent;

                ((Activity)Xamarin.Forms.Forms.Context).StartActivityForResult(intent, ModulePackageUtilityStaticDetails.UNINST_APP);
                #endregion



                output = true;

                return output;
            }
            catch (Exception ex)
            {
                StatusUpdateHelper.Error("Error Removing Module: " + ex.Message);
                return output;
                //throw;
            }
        }

        #endregion

        #region deleting modules
        public async Task<bool> DeleteModule(string moduleName)
        {

            bool output = false;
            try
            {
                string path = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModulePath, moduleName);
                FileManager _fileManager = new FileManager();
                output = await _fileManager.DeleteDirectory(path);
                //output = FileManager.DeleteFile(path);

                return output;
            }
            catch (Exception ex)
            {
                StatusUpdateHelper.Error("Error Removing Module: " + ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteCompressedModule(string packetName)
        {
            bool output = false;
            try
            {
                string path = Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ZippedModulePath, packetName);
                //output = _externalPlatformFileSystem.DeleteFile(path);
                output = await FileManager.DeleteFile(path);

                return output;
            }
            catch (Exception ex)
            {
                StatusUpdateHelper.Error("Error Removing Module: " + ex.Message);
                throw;
            }
        }
        #endregion

        #region Run Module
        public async Task RunModule(string packageName, string statementArguments)
        {
            //check to make sure the statement is okay


            //Intent newApp = new Intent(Intent.ActionMain);//.ACTION_MAIN);
            //newApp.SetComponent(new ComponentName("com.newApp.package", "com.google.android.youtube"));
            //newApp.SetType("text/plain");
            //newApp.PutExtra("args", statementArguments);
            //_context.StartActivity(newApp);


            Intent i;
            //PackageManager _packageManager = _context.ApplicationContext.PackageManager;
            //PackageManager pm = PackageManager();
            try
            {
                //string packageName = ModulePackageUtilityStaticDetails.ApkUriPrefix + packageName;
                //i = _packageManager.GetLaunchIntentForPackage("com.google.android.youtube");
                PackageManager _packageManager = _context.ApplicationContext.PackageManager;
                i = _packageManager.GetLaunchIntentForPackage(packageName);
                if (i == null)
                    throw new PackageManager.NameNotFoundException();
                i.AddCategory(Intent.CategoryLauncher);
                //i.SetType("text/plain");
                i.PutExtra(StatementPassingStaticDetails.ArgumentExtraTag, statementArguments).SetType("text/plain");
                //Console.WriteLine(i);
                //LogIntentDetails(i);

                // HOLD STATEMENT UPLOADS FOR THE DURATION OF THE RUN. The simulation is about to
                // start writing statements through the STATEMENT_CREATE broadcast, and a run can
                // emit more than one. Uploading mid-run ships a partial record AND marks it
                // Uploaded, so the finished version would never be sent. Released when this app
                // returns to the foreground (MainActivity.OnResume), which is what the simulation
                // closing looks like from here.
                //
                // Set BEFORE StartActivity, deliberately. Doing it after leaves a window in which
                // the simulation is already running and the upload loop is not held.
                Resources.LocalHardwareStaticDetails.SetRunningSimulation(packageName);

                _context.StartActivity(i);
            }
            catch (PackageManager.NameNotFoundException ex)
            {
                // The launch never happened, so nothing is running and the hold must not stand.
                // Leaving it set here is how a failed launch would silently stop every future
                // statement upload until the 4h cap expired.
                Console.WriteLine("RunModule: '" + packageName + "' is not installed, cannot launch");
                Resources.LocalHardwareStaticDetails.ClearRunningSimulation("launch failed, package not found");
                Console.WriteLine(ex.StackTrace);
            }
            catch(Exception ex)
            {
                Console.WriteLine("RunModule: launching '" + packageName + "' failed: " + ex.Message);
                Resources.LocalHardwareStaticDetails.ClearRunningSimulation("launch failed");
                Console.WriteLine(ex.StackTrace);
            }
        }


        #region Testing
        private void LogIntentDetails(Intent intent)
        {
            // Example: Log action and extras
            string action = intent.Action;
            Console.WriteLine("Action: " + action);

            // Example: Log extras
            Bundle extras = intent.Extras;
            if (extras != null)
            {
                foreach (string key in extras.KeySet())
                {
                    object value = extras.Get(key);
                    Console.WriteLine("Extra: " + key + " = " + value);
                }
            }
        }
        #endregion

        #endregion

    }



}