// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.DataLogic;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileCompanionV3.MVVM.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            LocalFileRepository = new LocalFileRepositoryViewModel()
            {
                ModuleVM = new ModuleViewModel(),
                VideoVM = new VideoViewModel(),
                StatementVM = new StatementViewModel()            
            };
            HomeVM = new HomeViewModel();
            ConfigVM = ConfigurationViewModelSetup();
        }

        private LocalFileRepositoryViewModel _localFileRepository;

        public LocalFileRepositoryViewModel LocalFileRepository
        {
            get { return _localFileRepository; }
            set
            {
                _localFileRepository = value;
                OnPropertyChanged();
            }
        }

        private HomeViewModel _homeVM;

        public HomeViewModel HomeVM
        {
            get { return _homeVM; }
            set
            {
                _homeVM = value;
                OnPropertyChanged();
            }
        }

        private ConfigurationViewModel _configVM;

        public ConfigurationViewModel ConfigVM
        {
            get { return _configVM; }
            set
            {
                _configVM = value;
                OnPropertyChanged();
            }
        }


        private ConfigurationViewModel ConfigurationViewModelSetup()
        {
            ConfigurationViewModel output = new ConfigurationViewModel();
            try
            {
                ServerDeviceContext _context = new ServerDeviceContext();
                GroupOwnerDevice groupOwner = _context.GetSaved().Result??default;
                        
                
                output.P2pGroup = new P2pGroup()
                {
                    OwnerDeviceName = groupOwner?.WiFiDeviceName??string.Empty,
                    OwnerAddress = groupOwner?.WifiMacAddress??string.Empty
                };

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

    }
}
