// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Febris.MobileCompanionV3.Droid.Services;

namespace Febris.MobileCompanionV3.Droid
{
    /// <summary>
    /// FIX (MOB-B1 blocker 2): app-scoped registration of the <see cref="StatementReceiver"/>.
    ///
    /// The <c>com.febris.*</c> statement broadcasts from a running simulation are
    /// IMPLICIT, so an Android-8+ *manifest* receiver won't receive them -- a runtime,
    /// context-registered receiver is required. Previously that receiver was registered
    /// on <c>MainActivity</c> and unregistered in its <c>OnDestroy</c>, so when a launched
    /// simulation foregrounds (backgrounding the companion) and the OS reclaims the
    /// companion Activity, a completed broadcast arriving then was dropped. Registering on
    /// the Application context keeps the single receiver alive for the whole process
    /// lifetime, independent of any Activity, so <c>OnReceive</c> still fires while a sim
    /// is in the foreground. (The event subscriber that persists to the local SQLite DB is
    /// already app-scoped -- see App.xaml.cs -- so once OnReceive fires, persistence follows.)
    ///
    /// SOURCE-ONLY (Xamarin cannot be compiled on the current build host). After building,
    /// verify on-device that: (a) <c>MainApplication.OnCreate</c> runs; (b) progress AND
    /// completion statements are still received while a sim is foregrounded; (c) the
    /// receiver is registered exactly ONCE (no double delivery now that the MainActivity
    /// registration is removed).
    /// </summary>
    [Application]
    public class MainApplication : Application
    {
        private StatementReceiver _statementReceiver;

        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            var filter = new IntentFilter();
            filter.AddAction(StatementStaticDetails.StatementCreation);
            filter.AddAction(StatementStaticDetails.StatementUpdate);
            filter.AddAction(StatementStaticDetails.StatementError);
            StatementStaticDetails._intentFilter = filter;

            _statementReceiver = new StatementReceiver();
            RegisterReceiver(_statementReceiver, filter);
        }

        public override void OnTerminate()
        {
            // OnTerminate is only guaranteed on the emulator, but unregister defensively.
            try
            {
                if (_statementReceiver != null)
                {
                    UnregisterReceiver(_statementReceiver);
                    _statementReceiver = null;
                }
            }
            catch (Exception)
            {
                // Already unregistered / process tearing down -- nothing to do.
            }
            base.OnTerminate();
        }
    }
}
