// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(Febris.MobileCompanionV3.Droid.Utilities.PlatformUtility))]
namespace Febris.MobileCompanionV3.Droid.Utilities
{
    
    public class PlatformUtility : IPlatformUtility
    {
        public async Task RestartApplication()
        {
            try
            {
                Intent intent = Forms.Context.PackageManager.GetLaunchIntentForPackage(Forms.Context.PackageName);
                intent.AddFlags(ActivityFlags.ClearTop);
                Forms.Context.StartActivity(intent);
                System.Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }            
        }
        public async Task QuitApplication()
        {
            try
            {
                //Instance.FinishAffinity(); // Finish all activities in the current task
                System.Environment.Exit(0); // Terminate the application process
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}