// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using Android.App;
//using Android.Companion;
//using Android.Content;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;
//using AndroidX.Core.App;
//using Febris.SharedMobileLibrary.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Xamarin.Forms;

//[assembly: Xamarin.Forms.Dependency(typeof(Febris.MobileServerV3.Droid.Networking.Bluetooth.CompanionAssociationRequest))]
//namespace Febris.MobileServerV3.Droid.Networking.Bluetooth
//{
//    public class CompanionAssociationRequest : CompanionDeviceManager.Callback, IAssociationRequest
//    {
//        #region variables
        
//        AssociationRequest _request { get; set; }
//        AssociationRequest.Builder _requestBuilder { get; set; }
//        #region Bluetooth
//        BluetoothDeviceFilter _filter { get; set; }
//        BluetoothDeviceFilter.Builder _filterBuilder { get; set; }
//        #endregion       
//        CompanionDeviceManager _companionDeviceManager { get; set; }
//        CompanionDeviceManager.Callback _callback { get; set; }
//        Context _context = Android.App.Application.Context;
//        #endregion

//        #region Assocaiation Request
//        public async Task MakeRequest()
//        {
//            try
//            {
//                await HasNotificationAccess();
//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

//            try
//            {
//                _companionDeviceManager = (CompanionDeviceManager)Forms.Context.GetSystemService(Context.CompanionDeviceService);
//                await Request();
//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

//            try
//            {
//                Console.WriteLine(_request.ToString());
//                _companionDeviceManager.Associate(_request, CompanionDeviceManagerCallback().Result, null);
//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
//        }

//        #endregion

//        #region Filter and Request building
//        private async Task Request()
//        {
//            try
//            {
//                await RequestBuilder();
//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

//            try
//            {
//                _request = _requestBuilder.Build();
//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
//        }
//        private async Task RequestBuilder()
//        {
//            try
//            {
//                await Filter();
//                _requestBuilder = new AssociationRequest.Builder();
//                _requestBuilder.AddDeviceFilter(_filter);
//                _requestBuilder.SetSingleDevice(false);

//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
//        }
//        private async Task FilterBuilder()
//        {
//            try
//            {
//                _filterBuilder = new BluetoothDeviceFilter.Builder();
//                _filter = _filterBuilder.Build();
//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
//        }
//        private async Task Filter()
//        {
//            try
//            {
//                await FilterBuilder();
//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
//        }
//        #endregion

//        #region callback and overrides

//        private async Task<CompanionDeviceManager.Callback> CompanionDeviceManagerCallback()
//        {
//            try
//            {
//                _callback = DependencyService.Get<CompanionDeviceManager.Callback>();
//            }
//            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
//            return _callback;
//        }

//        public override void OnDeviceFound(IntentSender chooserLauncher)
//        {
//            try
//            {
//                DeviceSelectionRequest(chooserLauncher);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//            }
//        }

//        public override void OnFailure(Java.Lang.ICharSequence error)
//        {
//            try
//            {
//                //add notification here
//                Console.WriteLine("Devices not found");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//            }
//        }

//        private void DeviceSelectionRequest(IntentSender chooserLauncher)
//        {
//            try
//            {
//                #region activity result                
//                ActivityFlags flagsMask = ActivityFlags.BroughtToFront;
//                ActivityFlags flagsValues = ActivityFlags.NewTask;
//                int extraFlags = 0;
//                Intent fillInIntent = null;
//                ((Activity)Forms.Context).StartIntentSenderForResult(chooserLauncher, BluetoothStaticDetails.SELECT_DEVICE_REQUEST_CODE, fillInIntent, flagsMask, flagsValues, extraFlags);
//                #endregion   
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//            }
//        }

//        #endregion

//        #region notification access
//        public async Task HasNotificationAccess()
//        {
//            bool hasAccess = NotificationManagerCompat.From(_context).AreNotificationsEnabled();
//            if (!hasAccess)
//            {
//                await AskForNotificationAccess();
//            }
//        }

//        public async Task AskForNotificationAccess()
//        {

//            Console.WriteLine("notificaitons do not have access");
//        }

//        #endregion
//    }
//}