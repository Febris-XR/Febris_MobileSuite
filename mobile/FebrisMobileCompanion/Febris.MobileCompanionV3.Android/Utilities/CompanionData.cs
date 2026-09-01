// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(Febris.MobileCompanionV3.Droid.Utilities.CompanionData))]
namespace Febris.MobileCompanionV3.Droid.Utilities
{
    public class CompanionData : ICompanionDataCollection
    {
        public static CompanionData _companionData;
        public CompanionData()
        {
            _companionData = this;
        }


        //public event EventHandler<ClientSocketCreationCheckEventArgs> ClientSocketCreatedSuccessfullyAction;


        /// <summary>
        /// The device descriptor sent in the <c>_initalize</c> ping and with every file upload.
        ///
        /// <para><b>This must not throw.</b> It is the FIRST statement of
        /// <c>LoopLogic.Ping_InitalizeRequest</c> and of <c>LoopLogic.FileUpload</c>, both of which
        /// build their packet header afterwards. A throw here means the header is never built, so
        /// the Companion never announces itself and never uploads a statement.</para>
        /// </summary>
        public Task<DeviceStatusModel> GetStatusInformation()
        {
            return Task.FromResult(new DeviceStatusModel()
            {
                BlueToothDeviceName = ReadBluetoothNameOrEmpty()
            });
        }

        /// <summary>
        /// The Bluetooth adapter name, or empty when it cannot be read.
        ///
        /// <para><b>Why this is allowed to be empty rather than fatal.</b> Reading the adapter name
        /// requires <c>BLUETOOTH_CONNECT</c> from API 31, and this Companion declares no Bluetooth
        /// permission at all: they are commented out of the manifest and have been since the client
        /// trees were migrated in <c>8cfb156</c>. On Android 12 and newer the platform therefore
        /// throws a SecurityException here, which previously propagated and killed the caller.</para>
        ///
        /// <para><b>Why the permission is not simply added back.</b> The Bluetooth name is no longer
        /// an identity. Device identity on this tier is <c>DeviceUniqueIdentifier</c>, the Android
        /// SSAID, which the caller reads separately and which is what the pairing store and the
        /// packet header key on. Numeric-comparison pairing REPLACED Bluetooth onboarding rather
        /// than sitting on top of it (docs/MOBILE_AUTH.md 4.1), and the old flow was unsound anyway:
        /// it matched devices on a user-editable display name. Re-declaring a runtime Bluetooth
        /// permission would put a prompt in front of the operator to populate a field nothing
        /// authenticates.</para>
        /// </summary>
        private static string ReadBluetoothNameOrEmpty()
        {
            try
            {
                BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

                // Null on a device with no Bluetooth hardware, which is legal and not an error.
                if (bluetoothAdapter == null) return string.Empty;

                return bluetoothAdapter.Name ?? string.Empty;
            }
            catch (Exception ex)
            {
                // Broad by intent. The platform reports a missing Bluetooth permission as a
                // SecurityException on some releases and as a null adapter on others, and the
                // caller has no use for the distinction: either way there is no name to send.
                Console.WriteLine("bluetooth name unavailable, continuing without it: " + ex.Message);
                return string.Empty;
            }
        }



        //internal void ClientSocketCreatedSuccessfully(Java.Net.Socket socket)
        //{
        //    ClientSocketCreationCheckEventArgs args = new ClientSocketCreationCheckEventArgs() 
        //    {
        //        Success=socket.IsConnected
        //    };
        //    ClientSocketCreatedSuccessfullyAction(this, args);
        //}
    }
}