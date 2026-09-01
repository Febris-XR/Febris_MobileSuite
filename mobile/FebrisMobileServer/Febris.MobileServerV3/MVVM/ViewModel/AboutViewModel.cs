// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileApp.Resources;
using Febris.MobileServerV3.Resources;
using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class AboutViewModel : BaseViewModel
    {
        public AboutViewModel()
        {
            Title = "Home";


            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://febr.is"));
        }

        public ICommand OpenWebCommand { get; }
    }
}