// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.BusinessLogic;
using Febris.MobileCompanionV3.P2pCommunication.WiFi;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;
using Xamarin.Forms.Xaml;
using Febris.MobileCompanionV3.Utilities;
using Febris.MobileCompanionV3.DataLogic;
using Febris.MobileCompanionV3.Services;
using Febris.MobileCompanionV3.Resources;

namespace Febris.MobileCompanionV3
{
    public partial class App : Application
    {
        private InitalizationHandler _initalization = new InitalizationHandler();
        WiFiP2pRequestReceiver _wiFiP2PRequestReceiver = new WiFiP2pRequestReceiver();
        StatementRecieverProcessing _statementRecieverProcessing = new StatementRecieverProcessing();
        public bool _wifiIsEnabled = false;

        #region Variables
        public IWiFiService _wifi;
        public IWiFiP2pServer _wiFiP2PServer;
        public IEventHandlerHelper _eventHandlerHelper;
        public IModuleEventHandlerHelper _moduleEventHandlerHelper;
        public IDataProtection _dataProtection;
        public ICompanionDataCollection _companionData;
        public ISharedFileSystem _externalPlatformFileSystem;
        public IStatementEventHandlerHelper _statementRecieverEventHandlerHelper;
        public static IPlatformUtility _platformUtility;
        public TestMessage _testMessage;
        //add status model
        public StatusUpdate _statusUpdate;
        //add download 
        //add upload
        public ModulePackageEvents _moduleUtility;
        #endregion

        #region Local Database        
        private static LocalDatabase _dbContext;

        public static LocalDatabase DbContext
        {
            get
            {
                if (_dbContext == null)
                {                    
                    _dbContext = LocalDatabase.Instance.GetAwaiter().GetResult();                
                }
                return _dbContext;
            }
        }

        //public static async Task GetDbInstance()
        //{
        //    _dbContext = await LocalDatabase.Instance;
        //}
        #endregion


        public App()
        {
            InitializeComponent();

            #region Event Dependencies
            _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
            _wifi = DependencyService.Get<IWiFiService>();
            _eventHandlerHelper = DependencyService.Get<IEventHandlerHelper>();
            _moduleEventHandlerHelper = DependencyService.Get<IModuleEventHandlerHelper>();
            _statementRecieverEventHandlerHelper = DependencyService.Get<IStatementEventHandlerHelper>();
            _platformUtility = DependencyService.Get<IPlatformUtility>();
            #endregion

            #region Event handling

            #region Module
            _moduleUtility = new ModulePackageEvents();
            _moduleEventHandlerHelper.ModuleIndexUpdateAction += _moduleUtility.ModuleIndexUpdateAction;
            #endregion

            #region Networking
            _eventHandlerHelper.WifiP2pCompanionStatusCheckAction += _wiFiP2PRequestReceiver.OnReceivedStatusUpdate;
            _eventHandlerHelper.WifiP2pCompanionDownloadAction += _wiFiP2PRequestReceiver.P2PWifiDownloadTransfer;
            //I think this is not the best way to handle this but can use it to send back if things are successful to ui
            // DISABLED 2026-07-29 alongside the handler itself, which wrote raw wire packages
            // into the statement directory under an invented Guid. The event has no raiser
            // anywhere, so this subscription was already inert. Deletion is in the roadmap.
            //_eventHandlerHelper.WifiP2pCompanionUploadAction += _wiFiP2PRequestReceiver.P2PWifiUploadTransfer;
            //wifi.WifiP2pModuleInitalizationAction += StatementInitalization;

            #region Broadcast Reciever Communication
            _eventHandlerHelper.WifiP2pPeersChangedAction += _wiFiP2PRequestReceiver.OnPeersChanged;
            _eventHandlerHelper.WifiP2pStateChangedAction += _wiFiP2PRequestReceiver.OnStateChanged;
            _eventHandlerHelper.WifiP2pConnectSuccessAction += _wiFiP2PRequestReceiver.OnConnectSuccessChanged;
            _eventHandlerHelper.WifiP2pConnectionChangedAction += _wiFiP2PRequestReceiver.OnConnectionChanged;
            _eventHandlerHelper.WifiP2pReceiveMessageAction += _wiFiP2PRequestReceiver.OnReceivedMessageChanged;
            #endregion
            #endregion

            #region Statement events
            _statementRecieverEventHandlerHelper.StatementCreateAction += _statementRecieverProcessing.OnStatementCreate;
            _statementRecieverEventHandlerHelper.StatementUpdateAction += _statementRecieverProcessing.OnStatementUpdate;
            _statementRecieverEventHandlerHelper.StatementErrorAction += _statementRecieverProcessing.OnStatementError;
            #endregion

            #endregion

            #region Companion Device Data Communicaiton
            _companionData = DependencyService.Get<ICompanionDataCollection>();
            //_companionData.ClientSocketCreatedSuccessfullyAction += _wiFiP2PRequestReceiver.OnClientSocketCreation;
            #endregion

            #region Companion discovery toggle
            _eventHandlerHelper.ServiceDiscoveryToggleAction += _wiFiP2PRequestReceiver.ServiceDiscoveryToggleAction;
            #endregion 

            Task.Run(()=>_initalization.Initalize());


            

            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
            try
            {
                #region Initalize file system (All internal)
                FileSystemInitalizer _fileSystemInitalizer = new FileSystemInitalizer();
                _fileSystemInitalizer.FileInitalizer();
                #endregion
                #region initalize file system (obsolete because nothing will be stored on the shared system. Changed to content provider and added a DB)
                //_externalPlatformFileSystem = DependencyService.Get<ISharedFileSystem>();
                //string externalBasePath = _externalPlatformFileSystem.GetBaseDirectoryPath().Result;
                ////string secondbase = Xamarin.Essentials.FileSystem.AppDataDirectory;
                //FileSystemInitalizer _fileSystemInitalizer = new FileSystemInitalizer();
                //_fileSystemInitalizer.FileInitalizer(externalBasePath);

                #endregion
                //_initalization.Initalize();

                ///run data integrity check -- This may be errasing all data -- it was. refactor
                Task.Run(() => ModulePackageLogic.CheckDataIntegrity());

                _wifi.ListenForServices();
                _wifi.DiscoveringPeers();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting Discovery loops: " + ex.Message);
            }
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            try
            {
                //wifi.ListenForServices();
                //wifi.DiscoveringPeers();
            }
            catch { }
        }

        public static async Task Reset()
        {
            //Intent intent = new Intent(this, typeof(MainActivity));
            //intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
            //this.StartActivity(intent);

            //(App.Current as App).MainPage.Dispatcher.BeginInvokeOnMainThread(() =>
            //{
            //    (App.Current as App).MainPage = new AppShell();
            //});
            try
            {
                await _platformUtility.RestartApplication();
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Restarting Applicaiton";
                //throw;
            }
        }

        public static async Task QuitAppliction()
        {
            //Intent intent = new Intent(this, typeof(MainActivity));
            //intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
            //this.StartActivity(intent);

            //(App.Current as App).MainPage.Dispatcher.BeginInvokeOnMainThread(() =>
            //{
            //    (App.Current as App).MainPage = new AppShell();
            //});
            try
            {
                await _platformUtility.QuitApplication();
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Quiting Applicaiton";
                //throw;
            }
        }

    }
}
