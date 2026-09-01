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
using System.Threading;

[assembly: Xamarin.Forms.Dependency(typeof(InstallerResultReceiver))]
namespace Febris.MobileCompanionV3.Droid.Utilities.Installer
{
    public class InstallerResultReceiver : ResultReceiver
    {

        private Action<int, Bundle> _onReceiveResult;

        public InstallerResultReceiver(Handler handler, Action<int, Bundle> onReceiveResult) : base(handler)
        {
            _onReceiveResult = onReceiveResult;
        }

        protected override void OnReceiveResult(int resultCode, Bundle resultData)
        {
            // Handle the result in the callback
            _onReceiveResult?.Invoke(resultCode, resultData);
        }
        
    }

}