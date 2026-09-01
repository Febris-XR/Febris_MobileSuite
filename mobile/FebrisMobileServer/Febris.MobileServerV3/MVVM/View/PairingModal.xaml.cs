// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Crypto = Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    /// <summary>
    /// Numeric-comparison pairing (docs/MOBILE_AUTH.md 4.1 and 4.5).
    ///
    /// A MODAL rather than a panel on the pairing page, following the DeviceInfoModal precedent.
    /// Pairing is a ceremony with a beginning and an end: it starts on an explicit operator
    /// action, shows a code, and finishes with confirm or reject. A permanent box implied ambient
    /// state that does not exist and left a control on screen that is meaningful for only a few
    /// seconds at a time.
    ///
    /// Lists CONNECTED peers this Server has no record of. That is the whole point: a Companion
    /// gets a socket purely by joining the WiFi Direct group, so it can be paired before the
    /// Server knows it, and confirming the code is what creates the record. Nothing here depends
    /// on Bluetooth.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PairingModal : ContentPage
    {
        public PairingModal()
        {
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM.HardwareVM;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Sockets open and close independently of navigation, so the list is recomputed each
            // time the modal is shown rather than trusted to be current.
            LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.RefreshUnpairedPeers();
        }

        /// <summary>
        /// Start the ceremony with one connected peer.
        ///
        /// Routed by ADDRESS, because there is no device record to route by yet, and the peer has
        /// not authenticated so nothing it asserts about itself can be trusted for routing.
        /// </summary>
        private async void Pair_Click(object sender, EventArgs args)
        {
            try
            {
                var peer = ((Button)sender)?.CommandParameter as UnpairedPeerViewModel;
                if (peer == null || string.IsNullOrWhiteSpace(peer.IPAddress)) return;

                byte[] publicKey = Crypto.P2pPairingCoordinator.Begin();
                if (publicKey == null)
                {
                    await DisplayAlert("Pairing", "A pairing is already in progress.", "OK");
                    return;
                }

                var header = new PacketHeaderModel
                {
                    BodyType = BodyType._pairingRequest,
                    PacketName = "Pairing Request"
                    // No DeviceUniqueIdentifier: this device is unknown, and asserting one we
                    // invented would be worse than leaving it empty. Its response supplies the
                    // real one.
                };

                byte[] frame = new FebrisP2pFrameBuilder().Build(header, publicKey);
                bool sent = await DependencyService.Get<IWiFiP2pServer>()
                    .SocketSender(frame, peer.IPAddress);

                if (!sent)
                {
                    Crypto.P2pPairingCoordinator.Abort();
                    await DisplayAlert("Pairing", "Could not reach that device.", "OK");
                    LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.RefreshUnpairedPeers();
                }
            }
            catch (Exception ex)
            {
                Crypto.P2pPairingCoordinator.Abort();
                Console.WriteLine("Pair_Click failed: " + ex.Message);
                await DisplayAlert("Pairing", "Pairing could not start: " + ex.Message, "OK");
            }
        }
    }
}
