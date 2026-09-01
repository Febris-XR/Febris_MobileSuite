// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Febris.MobileCompanionV3.MVVM.ViewModel
{
    public class HomeViewModel : BaseViewModel
    {
        //public IWiFiService _wiFiService = DependencyService.Get<IWiFiService>();

        public HomeViewModel()
        {
            StatusMessage = string.Empty;
        }
        private bool _wiFiConnected;

        public bool WiFiConnected
        {
            get { return _wiFiConnected; }
            set 
            {
                _wiFiConnected = value;
                OnPropertyChanged();
            }
        }

        private P2pServerInfo _p2pServerInfo;

        public P2pServerInfo P2pServerInfo
        {
            get { return _p2pServerInfo; }
            set 
            { 
                _p2pServerInfo = value;
                OnPropertyChanged();
            }
        }

        private bool _progressBarActive = true;

        public bool ProgressBarActive
        {
            get { return _progressBarActive; }
            set
            {
                _progressBarActive = value;
                OnPropertyChanged();            
            }
        }

        private string _statusMessage;

        public string StatusMessage
        {
            get { return _statusMessage; }
            set { 
                _statusMessage = value;
                OnPropertyChanged();
            }
        }


    }

    
}
