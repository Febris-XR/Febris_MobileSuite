// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileCompanionV3.Droid.Utilities
{
    public class PackageManagerBroadcastReceiver : BroadcastReceiver
    {


        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;
            switch (action)
            {
                case Intent.ActionInstallPackage:
                    {

                        break;
                    }
                case Intent.ActionInstallFailure:
                    {

                        break;
                    }
                case Intent.ActionPackageInstall:
                    {

                        break;

                    }
                case Intent.ExtraInstallerPackageName:
                    {

                        break;
                    }
                case Intent.ActionUninstallPackage:
                    {

                        break;
                    }
                case Intent.ActionPackagesSuspended:
                    {

                        break;
                    }
                case Intent.ActionPackagesUnsuspended:
                    {

                        break;
                    }
                case Intent.ActionPackageReplaced:
                    {

                        break;
                    }
                case Intent.ActionPackageRestarted:                    
                    {

                        break;
                    }
                case PackageInstaller.ActionSessionCommitted:
                    {

                        break;
                    }                
                case PackageInstaller.ActionSessionUpdated:
                    {

                        break;
                    }
                //case PackageInstaller.ActionSessionCommitted:

                //    {

                //        break;
                //    }
                //case PackageInstaller.ActionSessionCommitted:

                //    {

                //        break;
                //    }
                //case PackageInstaller.ActionSessionCommitted:

                //    {

                //        break;
                //    }
                //case PackageInstaller.ActionSessionCommitted:

                //    {

                //        break;
                //    }

                    //case ModulePackageUtilityStaticDetails.StaticModulePackageUtility.PACKAGE_INSTALLED_ACTION:
                    //    {

                    //        break;
                    //    }
            }
            
        }
    }
}