// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models.EventArguments;
using Febris.ModelLibrary.Models.XApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Febris.SharedMobileLibrary.Models.EventArguments.StatementEventArgs;

[assembly: Xamarin.Forms.Dependency(typeof(StatementEventHandlerHelper))]
namespace Febris.MobileCompanionV3.Droid.Utilities.EventHandlers
{
    public class StatementEventHandlerHelper : IStatementEventHandlerHelper
    {
        public static StatementEventHandlerHelper _eventHandler;
        public StatementEventHandlerHelper()
        {
            _eventHandler = this;
        }
        public event EventHandler<StatementEventArgs> StatementCreateAction;
        public event EventHandler<StatementEventArgs> StatementUpdateAction;
        public event EventHandler<StatementEventArgs> StatementErrorAction;


        #region package installer events

        /// <summary>
        /// This is sending information back to ModulePackageEvents
        /// </summary>
        /// <param name="moduleDirectoryName"></param>
        /// <param name="packageInfo"></param>
        internal async static Task StatementCreationEvent(StatementEventArgs _args)
        {
            // FIX (MDM-B7): null-conditional invoke so no-subscriber delegate does not throw NRE. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
            StatementEventHandlerHelper._eventHandler.StatementCreateAction?.Invoke(Application.Context, _args);
        }
        internal async static Task StatementUpdateEvent(StatementEventArgs _args)
        {
            // FIX (MDM-B7): null-conditional invoke so no-subscriber delegate does not throw NRE. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
            StatementEventHandlerHelper._eventHandler.StatementUpdateAction?.Invoke(Application.Context, _args);
        }

        internal async static Task StatementErrorEvent(StatementEventArgs _args)
        {
            // FIX (MDM-B7): null-conditional invoke so no-subscriber delegate does not throw NRE. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
            StatementEventHandlerHelper._eventHandler.StatementErrorAction?.Invoke(Application.Context, _args);
        }

        #endregion


    }
}