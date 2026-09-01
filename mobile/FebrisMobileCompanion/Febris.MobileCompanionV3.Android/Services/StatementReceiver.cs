// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.SharedMobileLibrary.Models.EventArguments;
using Febris.SharedMobileLibrary.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileCompanionV3.Droid.Services
{
    public class StatementStaticDetails
    {
        //public const string StatementAction = "com.febris.STATEMENT_RECIEVER";
        public static IntentFilter _intentFilter { get; set; }

        #region Actions
        public const string StatementCreation = "com.febris.STATEMENT_CREATE";
        public const string StatementUpdate = "com.febris.STATEMENT_UPDATE";
        public const string StatementError = "com.febris.STATEMENT_ERROR";
        #endregion
    }


    [BroadcastReceiver(Enabled = true)]
    //[IntentFilter(new[] { "com.febris.ACTION_CUSTOM_INTENT" })]
    internal class StatementReceiver : BroadcastReceiver
    {        
        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;

            Console.WriteLine(intent.ToString());
            Guid ReferenceId = default;
            string JsonStatement = default;
            if (intent.HasExtra(StatementPassingStaticDetails.StatementJsonIntentExtraTag))
            {
                Console.WriteLine("the intent has jsonstatement extra");                
                var jsonstatement = intent.GetStringExtra(StatementPassingStaticDetails.StatementJsonIntentExtraTag);
                JsonStatement = (string)jsonstatement;
            }
            else
            {
                Console.WriteLine("the intent does not have jsonstatement extra");
            }

            if (intent.HasExtra(StatementPassingStaticDetails.ReferenceUUIDIntentExtraTag))
            {
                Console.WriteLine("the intent has referenceId extra");                
                var referenceId = intent.GetStringExtra(StatementPassingStaticDetails.ReferenceUUIDIntentExtraTag);
                ReferenceId = Guid.Parse(referenceId.ToString());
            }
            else
            {
                Console.WriteLine("the intent does not have referenceId extra");
            }


            //Guid ReferenceId = default;
            //string JsonStatement = default;
            //var jsonstatement = intent.GetParcelableExtra(StatementPassingStaticDetails.StatementJsonIntentExtraTag);
            //var referenceId = intent.GetParcelableExtra(StatementPassingStaticDetails.ReferenceUUIDIntentExtraTag);
            //if (referenceId is string)
            //{
            //    ReferenceId = Guid.Parse(referenceId.ToString());
            //}
            //if(jsonstatement != default && jsonstatement is string)
            //{
            //    JsonStatement = (string)jsonstatement;
            //}
            Console.WriteLine("action recieved from broadcast: " + action);
            switch (action)            
            {
                case StatementStaticDetails.StatementCreation:
                    {
                        StatementEventArgs _args = new StatementEventArgs()
                        {
                            RawStatementJson = JsonStatement,
                            ReferenceUUID = ReferenceId
                        };
                        StatementEventHandlerHelper.StatementCreationEvent(_args);
                        break;
                    }
                case StatementStaticDetails.StatementUpdate:
                    {
                        StatementEventArgs _args = new StatementEventArgs()
                        {
                            RawStatementJson = JsonStatement,
                            ReferenceUUID = ReferenceId
                        };
                        StatementEventHandlerHelper.StatementUpdateEvent(_args);

                        break;
                    }
                case StatementStaticDetails.StatementError:
                    {
                        StatementEventArgs _args = new StatementEventArgs()
                        {                            
                            ReferenceUUID = ReferenceId
                        };
                        StatementEventHandlerHelper.StatementErrorEvent(_args);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Recieved intent not recognized by the statement reciever");
                        break;
                    }
            }
        }
    }
}