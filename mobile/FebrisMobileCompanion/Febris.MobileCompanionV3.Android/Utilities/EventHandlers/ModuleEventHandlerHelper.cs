// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models.EventArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(ModuleEventHandlerHelper))]
namespace Febris.MobileCompanionV3.Droid.Utilities.EventHandlers
{
    public class ModuleEventHandlerHelper : IModuleEventHandlerHelper
    {
        public static ModuleEventHandlerHelper _eventHandler;
        public ModuleEventHandlerHelper()
        {
            _eventHandler = this;
        }

        public event EventHandler<ModuleEventArgs> ModuleIndexUpdateAction;


        #region package installer events
        internal async static Task OnInstallFailed(object sender, EventArgs e)
        {
            Console.WriteLine(e.ToString());
            #region Gather needed data

            #endregion
            ModuleEventArgs _args = new ModuleEventArgs()
            {

            };
        }

        internal async static Task OnInstallSuccess(object sender, EventArgs e)
        {
            Console.WriteLine(e.ToString());
            #region Gather needed data

            #endregion
            ModuleEventArgs _args = new ModuleEventArgs()
            {

            };
        }


        /// <summary>
        /// This is sending information back to ModulePackageEvents
        /// </summary>
        /// <param name="moduleDirectoryName"></param>
        /// <param name="packageInfo"></param>
        internal async static Task ModuleIndexUpdate(string moduleDirectoryName, PackageInfo packageInfo, string fileUri, bool installed)
        {
            #region Gather needed data

            #endregion
            ModuleEventArgs _args = new ModuleEventArgs()
            {
                ModuleDirectoryName = moduleDirectoryName,
                PackageName = packageInfo.PackageName,
                FileUri = fileUri,
                IsInstalled = installed
            };
            ModuleEventHandlerHelper._eventHandler.ModuleIndexUpdateAction(Application.Context, _args);
        }

        #endregion
    }
}