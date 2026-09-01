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

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    /// <summary>
    /// Success/failure callback for every WifiP2pManager call this tier makes.
    ///
    /// THIS CLASS USED TO BE BLIND, AND THAT IS WHY ISSUE 14 FAULT C SURVIVED TWO FIX ATTEMPTS.
    /// <see cref="OnFailure"/> had no body at all, its only content commented out, and every
    /// caller passed <c>new FebrisActionListener(() =&gt; { })</c>. WifiP2pManager reports
    /// AddServiceRequest, DiscoverServices and DiscoverPeers failures ASYNCHRONOUSLY through this
    /// interface and nowhere else, and Android fails them routinely with Busy, Error and
    /// NoServiceRequests. So a call that failed every single time was indistinguishable, in the
    /// log, from a call that succeeded and simply got no answer.
    ///
    /// That distinction is the whole of fault C. The service listener loop
    /// (WiFiService.ListenerLoop) already re-issues the discovery request on every iteration and
    /// keeps doing so while a Companion is stranded, so "it stopped asking" was never the problem.
    /// What nobody could see was whether the asking was being refused.
    ///
    /// <para><b>Label every call site.</b> The parameterless-ish constructor is kept so the
    /// existing sites compile, but it logs as "unlabelled" on failure, which is a prompt to pass
    /// one rather than a silent default.</para>
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
        /// Which WifiP2pManager call this listener belongs to, e.g. "DiscoverServices". It is the
        /// only thing that makes a failure line actionable, because the reason code alone does not
        /// say which of the three requests in a single loop iteration was refused.
        /// </param>
        public FebrisActionListener(Action onSuccessAction, string label)
        {
            _action = onSuccessAction;
            _label = string.IsNullOrWhiteSpace(label) ? "unlabelled" : label;
        }

        public void OnFailure(WifiP2pFailureReason reason)
        {
            // Deliberately unconditional and deliberately not Debug-only. This is the instrument
            // for a defect that only appears on hardware, in a Release build, minutes into a
            // stranded state.
            Console.WriteLine("WifiP2p " + _label + " FAILED: " + reason);
        }

        public void OnSuccess()
        {
            try
            {
                _action?.Invoke();
            }
            catch (Exception ex)
            {
                // The success callback is supplied by the caller and runs on a framework thread.
                // Letting it throw here would surface as an unexplained crash far from its cause.
                Console.WriteLine("WifiP2p " + _label + " success handler threw: " + ex.Message);
            }
        }
    }

    //public class FebrisServiceActionListener : Java.Lang.Object, WifiP2pManager.IActionListener
    //{
    //    //private readonly Context _context;
    //    //private readonly string _failure;
    //    private readonly Action _action;

    //    public FebrisServiceActionListener(Action onSuccessAction)
    //    {
    //        //_context = context;
    //        //_failure = failure;
    //        _action = onSuccessAction;
    //    }

    //    public void OnFailure(WifiP2pFailureReason reason)
    //    {
    //        //Toast.MakeText(_context, _failure + " Failed : " + reason,
    //        //                ToastLength.Short).Show();
    //    }

    //    public void OnSuccess()
    //    {
    //        //Toast.MakeText(_context, _failure + "Discovery Initiated",
    //        //                ToastLength.Short).Show();
    //        _action.Invoke();
    //    }
    //}
    //public class FebrisP2pActionListener : Java.Lang.Object, WifiP2pManager.IActionListener
    //{
    //    //private readonly Context _context;
    //    //private readonly string _failure;
    //    private readonly Action _action;

    //    public FebrisP2pActionListener(Action onSuccessAction)
    //    {
    //        //_context = context;
    //        //_failure = failure;
    //        _action = onSuccessAction;
    //    }

    //    public void OnFailure(WifiP2pFailureReason reason)
    //    {
    //        //Toast.MakeText(_context, _failure + " Failed : " + reason,
    //        //                ToastLength.Short).Show();
    //    }

    //    public void OnSuccess()
    //    {
    //        //Toast.MakeText(_context, _failure + "Discovery Initiated",
    //        //                ToastLength.Short).Show();
    //        _action.Invoke();
    //    }
    //}

}