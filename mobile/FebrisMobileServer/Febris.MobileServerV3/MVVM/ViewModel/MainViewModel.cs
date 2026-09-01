// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Configuration;
using Febris.MobileApp.Resources;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Utilites;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Xamarin.Forms;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models.ViewModels;
using Febris.MobileServerV3.DataLogic;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ILogger _log;
        private readonly IConfiguration _config;


        #region different View Model

        //private object _currentView;
        //public object CurrentView
        //{
        //    get { return _currentView; }
        //    set
        //    {
        //        _currentView = value;
        //        OnPropertyChanged();
        //    }
        //}


        public HomeViewModel HomeVM { get; set; }

        private MessageBoardViewModel _messageboardVM;

        public MessageBoardViewModel MessageboardVM
        {
            get { return _messageboardVM; }
            set
            {
                _messageboardVM = value;
                OnPropertyChanged();
            }
        }

        //public MessageBoardViewModel MessageboardVM { get; set; }
        //public RelayCommand HomeViewCommand { get; set; }

        //public UserViewModel UserVM { get; set; }
        private UserViewModel _userVM;

        public UserViewModel UserVM
        {
            get { return _userVM; }
            set
            {
                _userVM = value;
                OnPropertyChanged();
            }
        }

        //public RelayCommand UserViewCommand { get; set; }
        //public RelayCommand SelectUser { get; set; }

        //public ModuleViewModel ModuleVM { get; set; }
        private ModuleViewModel _moduleVM;

        public ModuleViewModel ModuleVM
        {
            get { return _moduleVM; }
            set
            {
                _moduleVM = value;
                OnPropertyChanged();
            }
        }

        private ModuleFileViewModel _moduleFileVM;

        public ModuleFileViewModel ModuleFileVM
        {
            get { return _moduleFileVM; }
            set
            {
                _moduleFileVM = value;
                OnPropertyChanged();
            }
        }

        //public RelayCommand ModuleViewCommand { get; set; }

        //public LaunchViewModel LaunchVM { get; set; }
        private LaunchViewModel _launchVM;

        public LaunchViewModel LaunchVM
        {
            get { return _launchVM; }
            set
            {
                _launchVM = value;
                OnPropertyChanged();
            }
        }

        //public RelayCommand LaunchViewCommand { get; set; }

        //public ConfigViewModel ConfigVM { get; set; }
        private ConfigViewModel _configVM;

        public ConfigViewModel ConfigVM
        {
            get { return _configVM; }
            set
            {
                _configVM = value;
                OnPropertyChanged();
            }
        }

        //public RelayCommand ConfigViewCommand { get; set; }

        //public StatementViewModel StatementVM { get; set; }
        private StatementViewModel _statementVM;

        public StatementViewModel StatementVM
        {
            get { return _statementVM; }
            set
            {
                _statementVM = value;
                OnPropertyChanged();
            }
        }

        //public RelayCommand StatementViewCommand { get; set; }

        //public VideoViewModel VideoVM { get; set; }
        private VideoViewModel _videoVM;

        public VideoViewModel VideoVM
        {
            get { return _videoVM; }
            set
            {
                _videoVM = value;
                OnPropertyChanged();
            }
        }

        //public RelayCommand VideoViewCommand { get; set; }

        //public HardwareViewModel HardwareVM { get; set; }
        private HardwareViewModel _hardwareVM;

        public HardwareViewModel HardwareVM
        {
            get { return _hardwareVM; }
            set
            {
                _hardwareVM = value;
                OnPropertyChanged();
            }
        }

        private CompanionAppViewModel _companionAppVM;

        public CompanionAppViewModel CompanionAppVM
        {
            get { return _companionAppVM; }
            set
            {
                _companionAppVM = value;
                OnPropertyChanged();
            }
        }


        #endregion


        public MainViewModel()
        {

            HomeVM = new HomeViewModel();
            MessageboardVM = new MessageBoardViewModel()
            {
                FebrisMessageBoard = LocalHardwareStaticDetails.HardwareInitializationResponse?.MessageboardViewModels?.AdminMessageBoardList ?? new List<AdminMessageBoard>(),
                LocalMessageBoard = LocalHardwareStaticDetails.HardwareInitializationResponse?.MessageboardViewModels?.MessageBoardList ?? new List<MessageBoard>()
            };
            UserVM = new UserViewModel()
            {
                UserList = LocalHardwareStaticDetails.HardwareInitializationResponse?.UserInitaliztionViewModels?.UserViewModelList ?? new List<HardwareUserViewModel>(),
                SearchResultList = LocalHardwareStaticDetails.HardwareInitializationResponse?.UserInitaliztionViewModels?.UserViewModelList ?? new List<HardwareUserViewModel>(),
            };
            ModuleVM = new ModuleViewModel()
            {
                ModuleList = LocalHardwareStaticDetails.HardwareInitializationResponse?.ModuleList ?? new List<Module>(),
                SearchResultList = LocalHardwareStaticDetails.HardwareInitializationResponse?.ModuleList ?? new List<Module>()
            };
            LaunchVM = new LaunchViewModel() { };
            var deviceList = CollectCompanionDeviceList();
            HardwareVM = new HardwareViewModel()
            {
                ItemList = deviceList,//CollectCompanionDeviceList(),
                ItemResultList = deviceList,// CollectCompanionDeviceList(),
                UsbButtonVisability = false
            };
            StatementVM = StatementVMSetup().Result;
            VideoVM = VideoVMSetup().Result;
            CompanionAppVM = new CompanionAppViewModel()
            {
                LocalSoftwarePackage = new LocalSoftwarePackage()
            };
            ConfigVM = new ConfigViewModel();// { };
            //{
            //    ConfigModel = 
            //};
        }

        private List<CompanionDeviceViewModel> CollectCompanionDeviceList()
        {
            try
            {
                CompanionDeviceContext context = new CompanionDeviceContext();
                List<CompanionDeviceViewModel> output = context.GetList().Result;
                return output;
            }
            catch
            {
                return default;
            }
        }

        private async Task<VideoViewModel> VideoVMSetup()
        {
            try
            {
                FileManager _fileManager = new FileManager(null, null);
                VideoViewModel _vm = new VideoViewModel()
                {
                    UnsentVideoFileList = await _fileManager.GetDirectoryContentNames(FileSystem.RecordingsFilePath),
                    SentVideoFileList = await _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath)
                };
                return _vm;
            }
            catch
            {
                VideoViewModel _vm = new VideoViewModel()
                {
                    UnsentVideoFileList = default,
                    SentVideoFileList = default
                };
                return _vm;
            }
        }

        private async Task<StatementViewModel> StatementVMSetup()
        {
            try
            {
                FileManager _fileManager = new FileManager(null, null);
                StatementViewModel _vm = new StatementViewModel()
                {
                    UnsentStatementFileList = await _fileManager.GetDirectoryContentNames(FileSystem.StatementPath),
                    SentStatementFileList = await _fileManager.GetDirectoryContentNames(FileSystem.OldStatementPath)
                };
                return _vm;
            }
            catch
            {
                StatementViewModel _vm = new StatementViewModel()
                {
                    UnsentStatementFileList = default,
                    SentStatementFileList = default
                };
                return _vm;
            }
        }

        //private ConfigViewModel ConfigVMSetup()
        //{
        //    ConfigSettings _configSettings = new ConfigSettings();
        //    DataProtection _dataProtection = new DataProtection(_log, _config);
        //    JObject settings = _configSettings.Get().Result;

        //    bool credsExist = false;
        //    string userName = string.Empty;
        //    string secret = string.Empty;
        //    //(credsExist, userName, secret) = _dataProtection.GetCredentials().Result;

        //    ConfigViewModel output = new ConfigViewModel()
        //    {
        //        HardwareLicense = SetHardwareLicense(),
        //        DomainPrefix = settings["domainprefix"]?.ToString() ?? string.Empty,
        //        CredentialsExist = credsExist
        //    };

        //    if (credsExist)
        //    {
        //        output.EmailAddress = userName;
        //        output.Password = secret;
        //    }

        //    return output;
        //}

        /// <summary>
        /// NODE-9. Returns the credential this device is registered with, not the identifier it
        /// derives for itself. Audit T9 made the node mint the credential and keep only its hash,
        /// so the derived value authenticates against nothing.
        /// </summary>
        public string SetHardwareLicense()
        {
            try
            {
                DeviceCredentialStore credentialStore =
                    new DeviceCredentialStore(new EssentialsSecureKeyValueStore());

                return credentialStore.GetAsync().Result;
            }
            catch (Exception ex)
            {
                // Empty, not a throw. "This device is not registered" is the honest answer and the
                // caller can act on it; taking the view model down cannot.
                Console.WriteLine(ex.StackTrace);
                return string.Empty;
            }
        }

    }


}
