// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MessageBoardPage : ContentPage
    {
        public MessageBoardPage()
        {
            try
            {
                InitializeComponent();
                BindingContext = LocalHardwareStaticDetails.StaticMainVM.MessageboardVM;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }     
        }
        //protected override void OnAppearing()
        //{
        //    //FebrisMessageBoard.ItemsSource = LocalHardwareStaticDetails._febrisMessageBoard;
        //    //LocalMessageBoard.ItemsSource = LocalHardwareStaticDetails._providerMessageBoard;
        //}
    }
}