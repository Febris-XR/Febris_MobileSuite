// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VideoStreamPage : ContentPage
    {
        //CompanionDeviceViewModel _selected;
        public VideoStreamPage()
        {
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem;
        }

        //end video page
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // LOCAL TEARDOWN FIRST, and unconditionally. This used to be only the outbound
            // request, so the decoder and frame pump were released solely as a side effect of
            // a message to the peer. If that send failed, or the Companion had already gone,
            // a scarce hardware MediaCodec session and its Surface binding stayed alive for a
            // stream nobody was watching. Releasing our own resources must not depend on
            // reaching someone else.
            P2pCommunication.WiFi.VideoStreamProcessing.StopStream();

            RequestHelper.EndVideoStreamRequest(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem);
            //_selected.ImageList = null;
            //_selected.VideoStream = null;
        }
    }
}