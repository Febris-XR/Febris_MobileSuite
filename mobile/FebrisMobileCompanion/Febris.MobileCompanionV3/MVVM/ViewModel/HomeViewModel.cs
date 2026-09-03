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

        /// <summary>
        /// TRUE once an accepted Febris frame has arrived FROM the server, which is a different and
        /// later fact than WiFiConnected.
        ///
        /// <para>
        /// WiFiConnected means a TCP socket opened. At that instant the far end has sent nothing,
        /// so it may not be a Febris peer at all. This is the Companion side of what the Mobile
        /// Server already does, where NotePeerIsTalking is called before dispatch for every body
        /// type and its comment reads "exactly the condition connected is meant to describe".
        /// The two ends now mean the same thing by the word.
        /// </para>
        ///
        /// <para>
        /// Deliberately a SECOND flag rather than a redefinition of WiFiConnected, which is read by
        /// VideoStreamProcessing to gate a running video stream. Changing what that flag means
        /// would have reached the stream, so its meaning and its two writers are untouched.
        /// </para>
        /// </summary>
        private bool _serverResponding;

        public bool ServerResponding
        {
            get { return _serverResponding; }
            set
            {
                _serverResponding = value;
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

        // Defaulted TRUE, so the spinner ran from construction, before the app had connected to
        // anything or been asked to do any work. Combined with there being no writer that ever set
        // it false, it simply never stopped. It starts idle now.
        private bool _progressBarActive = false;

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
