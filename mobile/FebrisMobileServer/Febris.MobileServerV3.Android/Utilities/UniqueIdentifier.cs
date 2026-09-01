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
using static Android.Provider.Settings;

[assembly: Xamarin.Forms.Dependency(typeof(Febris.MobileServerV3.Droid.Utilities.UniqueIdentifier))]
namespace Febris.MobileServerV3.Droid.Utilities
{
    public class UniqueIdentifier : IDevice
    {
        //string IDevice.GetIdentifier();

        public string GetIdentifier()
        {
            var context = Android.App.Application.Context;
            string id = Android.Provider.Settings.Secure.GetString(context.ContentResolver, Secure.AndroidId);
            return id;
        }
        //{
        //    return Settings.Secure.GetString(Forms.Context.ContentResolver, Settings.Secure.AndroidId);
        //}
    }
}