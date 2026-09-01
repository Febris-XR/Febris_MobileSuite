// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: Xamarin.Forms.Dependency(typeof(PackageInstallObserver))]
namespace Febris.MobileCompanionV3.Droid.Utilities
{
    /// <summary>
    /// https://stackoverflow.com/questions/63547423/onnewintent-is-not-called-after-packageinstaller-failure
    /// </summary>
    public class PackageInstallObserver : PackageInstaller.SessionCallback
    {
        private PackageInstaller PackageInstaller { get; }
        public event EventHandler InstallFailed;

        public PackageInstallObserver(PackageInstaller packageInstaller) => PackageInstaller = packageInstaller;

        public override void OnActiveChanged(int sessionId, bool active)
        {
            if (active)
            { Console.WriteLine("Package Installer Callback - " + sessionId.ToString() + " Activity Changed Successfully"); }
            else
            {
                Console.WriteLine("Package Installer Callback - " + sessionId.ToString() + " Activity Changed Unsuccessfully");
                //var sesssionList = Android.App.Application.Context.PackageManager.PackageInstaller.StagedSessions;
                //Console.WriteLine(sesssionList.ToString());
                //InstallFailed?.Invoke(this, EventArgs.Empty);
            }
            //Console.WriteLine("Package Installer Callback - " + sessionId.ToString() + " OnActiveChanged ");
        }

        public override void OnBadgingChanged(int sessionId)
        {
            Console.WriteLine("Package Installer Callback - " + sessionId.ToString() + " OnBadgingChanged ");
        }

        public override void OnCreated(int sessionId)
        { 
            Console.WriteLine("Package Installer Callback - " + sessionId.ToString() + " OnCreated "); 
        }

        public override void OnFinished(int sessionId, bool success)
        {
            PackageInstaller.UnregisterSessionCallback(this);
            PackageInstaller.Dispose();
            if (success) 
            { 
                Console.WriteLine("Package Installer Callback - "+sessionId.ToString()+" Completed Successfully"); 
            }
            else
            {
                Console.WriteLine("Package Installer Callback - " + sessionId.ToString() + " Completed Unsuccessfully");
                InstallFailed?.Invoke(this, EventArgs.Empty); 
            }
        }

        public override void OnProgressChanged(int sessionId, float progress)
        {
            Console.WriteLine("Package Installer Callback - " + sessionId.ToString() + " OnProgressChanged ");

        }
    }
}