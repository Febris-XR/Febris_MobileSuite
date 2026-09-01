// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Utilites;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class ConfigViewModel : BaseViewModel
    {
        public RelayCommand SaveDomainSettins { get; set; }       
        #region Added command user selection

        private object _saveSettingsCommand;
        public object SaveSettingsCommand
        {
            get { return _saveSettingsCommand; }
            set
            {
                _saveSettingsCommand = value;
                Task.Run(() => ConfigLogic.SaveSettings());
                // NODE-9. The credential does not live in ConfigModel: it authenticates the device,
                // so it goes to platform secure storage rather than the plaintext config file that
                // ConfigLogic writes. Saved on the same action so the operator has one Save.
                Task.Run(() => SaveDeviceCredential());
                OnPropertyChanged();
            }
        }
        
        public ConfigViewModel()
        {
            //CurrentConfigCommand = string.Empty;
            LoadHardwareLicense();
            ConfigModel = ConfigLogic.GetSettings().Result;
            SaveDomainSettins = new RelayCommand(x => { SaveSettingsCommand = ConfigModel; });
            DeveloperAccount = ConfigModel.DeveloperAccount;
        }

        // NODE-9. Where the node-minted device credential lives.
        private readonly DeviceCredentialStore _credentialStore =
            new DeviceCredentialStore(new EssentialsSecureKeyValueStore());

        /// <summary>
        /// NODE-9. Shows the credential this device is REGISTERED with, which is what the operator
        /// needs to see and correct. It used to show <c>IDevice.GetIdentifier()</c>, a derived value
        /// that no longer authenticates against anything, so an unregistered device looked
        /// configured. An unregistered device now correctly shows an empty field.
        ///
        /// <para>
        /// Async against a synchronous constructor, so the property is assigned when the read
        /// completes and the binding picks it up through OnPropertyChanged. Secure storage needs a
        /// live platform context and cannot be read on the constructor's thread.
        /// </para>
        /// </summary>
        private async void LoadHardwareLicense()
        {
            try
            {
                HardwareLicense = await _credentialStore.GetAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                HardwareLicense = string.Empty;
            }
        }

        /// <summary>
        /// NODE-9. Persists what the operator entered. An empty box is left alone rather than
        /// treated as a request to erase: the screen opens empty on a device whose credential could
        /// not be read, and saving unrelated settings from there must not wipe a working
        /// registration. Clearing a credential is a decommissioning action, not a side effect of
        /// pressing Save.
        /// </summary>
        private async Task SaveDeviceCredential()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(HardwareLicense))
                {
                    return;
                }

                await _credentialStore.SaveAsync(HardwareLicense);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion



        #region Service status
        //private bool _uploaderRunning;
        //public bool UploaderRunning
        //{
        //    get { return _uploaderRunning; }
        //    set { _uploaderRunning = value; }
        //}
        //private bool _downloaderRunning;
        //public bool DownloaderRunning
        //{
        //    get { return _downloaderRunning; }
        //    set { _downloaderRunning = value; }
        //}
        #endregion



        #region normal vm properties

        private string _hardwareLicense;
        public string HardwareLicense
        {
            get { return _hardwareLicense; }
            set { 
                _hardwareLicense = value;
                OnPropertyChanged();
            }
        }

        private bool _domainVisible;
        public bool DomainVisible
        {
            get { return _domainVisible; }
            set { 
                _domainVisible = value; 
                OnPropertyChanged(); 
            }
        }

        private bool _credentialsExist;
        public bool CredentialsExist
        {
            get { return _credentialsExist; }
            set { 
                _credentialsExist = value; 
                OnPropertyChanged(); 
            }
        }
        #endregion

        private bool _developerAccount;

        public bool DeveloperAccount
        {
            get { return _developerAccount; }
            set { _developerAccount = value;
                ConfigModel.DeveloperAccount = value;
                DomainVisible = !value;
                OnPropertyChanged(); }
        }


        private ConfigModel _configModel;
        public ConfigModel ConfigModel
        {
            get { return _configModel; }
            set
            {
                _configModel = value;
                OnPropertyChanged();
                //_developerAccount = value.DeveloperAccount;
            }
        }

        //private void SaveDomain(string domainPrefix)
        //{
        //    try
        //    {                
        //        ConfigSettings _configSettings = new ConfigSettings();// _log, _config);
        //        bool complete = _configSettings.SetDomainPrefix(domainPrefix.ToLower()).Result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //}
    }
}
