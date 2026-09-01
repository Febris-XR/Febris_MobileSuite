// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Utilities.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(Uninstaller))]
namespace Febris.MobileCompanionV3.Droid.Utilities.Installer
{
    public class Uninstaller
    {

        public static async Task<Intent> IntentBuilder()
        {
            Intent output = new Intent();
            try
            {

            }
            catch (Exception ex)
            {
                Console.WriteLine("IntentBuilder Issue: " + ex.StackTrace);
                throw;
            }
            return output;
        }

        public async Task UninstallPackage()
        {
            try
            {
                //Intent intent = await IntentBuilder(localApkUri);

                //PendingIntent installPendingIntent = PendingIntent.GetActivity(
                //_context,//.GetApplicationContext(),
                //localApkUri.GetHashCode(),//.hashCode(),
                //intent,
                //PendingIntentFlags.UpdateCurrent);//.FLAG_UPDATE_CURRENT);

                //sendBroadcastInstall(canonicalUri, Installer.ACTION_INSTALL_USER_INTERACTION,
                //installPendingIntent);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }

        }
    }
}