// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Droid.Networking.Bluetooth
{
    //class BTActionListener : Java.Lang.Object, BluetoothManager.IActionListener
    //{
    //    private readonly Action _action;
    //    public BTActionListener(Action onSuccessAction)
    //    {
    //        _action = onSuccessAction;
    //    }

    //    //public void OnFailure(WifiP2pFailureReason reason)
    //    //{
    //    //    //Toast.MakeText(_context, _failure + " Failed : " + reason,
    //    //    //                ToastLength.Short).Show();
    //    //    Console.WriteLine("action :" + _action + "\n failure: " + reason.ToString());
    //    //}

    //    ///// <summary>
    //    ///// 
    //    ///// </summary>
    //    //public void OnSuccess()
    //    //{
    //    //    //Toast.MakeText(_context, _failure + "Discovery Initiated",
    //    //    //                ToastLength.Short).Show();
    //    //    Console.WriteLine("action :" + _action + " SUCCESS");
    //    //    _action.Invoke();
    //    //}
    //}
}