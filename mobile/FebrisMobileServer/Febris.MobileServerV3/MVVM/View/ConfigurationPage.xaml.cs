// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Enums;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigurationPage : ContentPage
    {
        //IDevice device = DependencyService.Get<IDevice>();
        //DataProtection _dataProtection = new DataProtection();
        public ConfigurationPage()
        {
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM.ConfigVM;
        }



        private void Submit_click(object sender, EventArgs args)
        {
            //            try
            //            {
            //                string username = UserName.Text;
            //                var password = Password.Text;
            //                var domainPrefix = DomainPrefix.Text;
            //                //bool saved = _configSettings.Set(input).Result;
            //                //StaticDetails.prefix = input;
            //                if (username != string.Empty || username != null)
            //                {
            //                    _dataProtection.EncryptInput(username, ConfigType.UserName);
            //                }
            //                if (password != string.Empty || username != null)
            //                {
            //                    _dataProtection.EncryptInput(password, ConfigType.Password);
            //                }
            //                if (domainPrefix != string.Empty || domainPrefix != null)
            //                {
            //#if (!DEBUG)
            //                    LocalHardwareStaticDetails.prefix = _dataProtection.GetInput(EnumLibrary.ConfigType.Prefix).Result;
            //#else
            //                    LocalHardwareStaticDetails.prefix = _dataProtection.GetInput(ConfigType.Prefix).Result;
            //#endif
            //                    _dataProtection.StoreInput(domainPrefix, ConfigType.Prefix);
            //                }



            //            }
            //            catch { }
        }
        

        #region pairing
        private void PairDevice_click(object sender, EventArgs args)
        {
            //bool paired = false;
            try
            {
                //paired = _communication.WifiPairing().Result;
                Navigation.PushAsync(new PairingPage());
            }
            catch { }
        }

        #endregion

        #region See files on system
        private void FilesOnSystem_click(object sender, EventArgs args)
        {
            try
            {
                Navigation.PushAsync(new LocalFileRepositoryPage());
            }
            catch { }
        }
        #endregion

        private void CompanionApp_click(object sender, EventArgs args)
        {
            try
            {
                Navigation.PushAsync(new CompanionAppPage());
            }
            catch { }
        }
    }
}