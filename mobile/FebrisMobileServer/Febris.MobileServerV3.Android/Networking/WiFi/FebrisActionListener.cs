// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Droid.Networking.WiFi
{
    /// <summary>
    /// This method calls the reqired wifip2pconfig object that contains information about the device to connect to. Can notify you of the connection success or failure.
    /// </summary>
    public class FebrisActionListener : Java.Lang.Object, WifiP2pManager.IActionListener
    {
        private readonly Action _action;
        private readonly string _label;

        public FebrisActionListener(Action onSuccessAction)
            : this(onSuccessAction, "unlabelled")
        {
        }

        /// <param name="label">
        /// Which WifiP2pManager call this belongs to. This head already logged failures, unlike
        /// the Companion's copy, but "action failure: Error" does not say WHICH call failed, and
        /// the advertisement chain runs ClearLocalServices and AddLocalService back to back.
        /// Telling those apart is the difference between "the Server is advertising nothing" and
        /// "the Server advertised fine", which is the open half of issue 14 fault C.
        /// </param>
        public FebrisActionListener(Action onSuccessAction, string label)
        {
            _action = onSuccessAction;
            _label = string.IsNullOrWhiteSpace(label) ? "unlabelled" : label;
        }

        public void OnFailure(WifiP2pFailureReason reason)
        {
            Console.WriteLine("WifiP2p " + _label + " FAILED: " + reason);
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnSuccess()
        {
            //Toast.MakeText(_context, _failure + "Discovery Initiated",
            //                ToastLength.Short).Show();
            //Console.WriteLine("action :" + _action + " SUCCESS");
            _action.Invoke();
        }

    }
}