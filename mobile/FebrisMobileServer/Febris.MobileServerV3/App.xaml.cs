// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using Xamarin.Forms;
using System.Linq;
using System.Threading.Tasks;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Utilites;
using Febris.MobileServerV3.P2pCommunication;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Enums;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Microsoft.Extensions.Configuration;
using Serilog;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.P2pCommunication.BlueTooth;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using Febris.MobileServerV3.P2pCommunication.USB;
using Febris.SharedMobileLibrary.Interfaces;

namespace Febris.MobileServerV3
{
    public partial class App : Application
    {
        private readonly IConfiguration _config;
        private ILogger _log;
        //public IExternalPlatformFileSystem _externalPlatformFileSystem;
        private InitalizationHandler _initalization = new InitalizationHandler();// _log,_config);
        DataProtection _dataProtection = new DataProtection();
        //private TokenHandler _tokenHandler = new TokenHandler();
        FileManager _fileManager = new FileManager();
        //JSONHandler _jSONHandler = new JSONHandler();
        //LocalFileTransfer _localFileTransfer = new LocalFileTransfer();
        //WiFiP2pRequestReceiver _wiFiP2PRequestReceiver = new WiFiP2pRequestReceiver();
        WiFiP2pRequestReceiver _wiFiP2PRequestReceiver = new WiFiP2pRequestReceiver();
        BTRequestReceiver _bTRequestReceiver = new BTRequestReceiver();
        USBRequestReceiver _usbRequestReceiver = new USBRequestReceiver();

        //PairDevice _pairDevice = new PairDevice();
        //WiFiP2pSecurity _wiFiP2PSecurity = new WiFiP2pSecurity();
        public bool _wifiIsEnabled = false;
        //ServerCommunicationReceiver _serverCommunicationReceiver = new ServerCommunicationReceiver();

        #region wifi services
        public IWiFiService _wifi;
        public IWiFiP2pServer _wiFiP2PServer;
        public IUsbService _usbService;
        public IEventHandlerHelper _eventHandlerHelper;
        public IDataProtection dataProtection;
        public TestMessage testMessage;
        //add status model
        public StatusUpdate statusUpdate;
        //add download 
        //add upload        
        #endregion

        public App()
        {
            InitializeComponent();


            #region initalize file system
            //FileSystemInitalizer _fileSystemInitalizer = new FileSystemInitalizer();
            //_fileSystemInitalizer.FileInitalizer();
            #endregion

            #region Event Dependencies
            _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
            _wifi = DependencyService.Get<IWiFiService>();
            _eventHandlerHelper = DependencyService.Get<IEventHandlerHelper>();
            dataProtection = DependencyService.Get<IDataProtection>();
            _usbService = DependencyService.Get<IUsbService>();
            #endregion

            #region Receiver handler
            #region Network handling

            _eventHandlerHelper.WifiP2pCompanionStatusCheckAction += _wiFiP2PRequestReceiver.OnReceivedStatusUpdate;
            _eventHandlerHelper.WifiP2pCompanionDownloadAction += _wiFiP2PRequestReceiver.P2PWifiDownloadTransfer;
            //I think this is not the best way to handle this but can use it to send back if things are successful to ui
            _eventHandlerHelper.WifiP2pCompanionUploadAction += _wiFiP2PRequestReceiver.P2PWifiUploadTransfer;
            //wifi.WifiP2pModuleInitalizationAction += StatementInitalization;

            #region WiFiP2p Broadcast Reciever Communication
            _eventHandlerHelper.WifiP2pPeersChangedAction += _wiFiP2PRequestReceiver.OnPeersChanged;
            _eventHandlerHelper.WifiP2pStateChangedAction += _wiFiP2PRequestReceiver.OnStateChanged;
            _eventHandlerHelper.WifiP2pConnectSuccessAction += _wiFiP2PRequestReceiver.OnConnectSuccessChanged;
            _eventHandlerHelper.WifiP2pConnectionChangedAction += _wiFiP2PRequestReceiver.OnConnectionChanged;
            _eventHandlerHelper.WifiP2pReceiveMessageAction += _wiFiP2PRequestReceiver.OnReceivedMessageChanged;
            #endregion
            #region Bluetooth Event handling
            _eventHandlerHelper.CompanionCreationAction += _bTRequestReceiver.CompanionCreationEvent;
            #endregion
            #region Usb event handling
            _eventHandlerHelper.UsbConnectionAction += _usbRequestReceiver.UsbConnectionAction;
            //_eventHandlerHelper.ActionUsbDeviceAttached += _usbRequestReceiver.ActionUsbDeviceAttached;
            //_eventHandlerHelper.ActionUsbDeviceDetached += _usbRequestReceiver.ActionUsbDeviceDetached;
            //_eventHandlerHelper.ActionUsbAccessoryAttached += _usbRequestReceiver.ActionUsbAccessoryAttached;
            //_eventHandlerHelper.ActionUsbAccessoryDetached += _usbRequestReceiver.ActionUsbAccessoryDetached;
            //_eventHandlerHelper.ExtraPermissionGranted += _usbRequestReceiver.ExtraPermissionGranted;
            //_eventHandlerHelper.ActionUsbDeviceAttached += _usbRequestReceiver.ActionUsbDeviceAttached;
            #endregion
            #endregion

            //LoopForServerReceivedMessage();
            //ListenerLoop();
            //UpDateRequestLoop();
            #endregion
                        
            MainPage = new AppShell();
        }

        #region On start, on sleep, and resume
        protected override void OnStart()
        {
            #region initalize file system
            //_externalPlatformFileSystem = DependencyService.Get<IExternalPlatformFileSystem>();
            //string externalBasePath = _externalPlatformFileSystem.GetBaseDirectoryPath().Result;
            //string secondbase = Xamarin.Essentials.FileSystem.AppDataDirectory;
            FileSystemInitalizer _fileSystemInitalizer = new FileSystemInitalizer();
            _fileSystemInitalizer.FileInitalizer();
            //_fileSystemInitalizer.FileInitalizer(externalBasePath);
            #endregion

            try
            {
                //Task.Run(() => _tokenHandler.GetToken()).Wait();
                //_initalization.Initalize();
                Task.Run(()=>_initalization.Initalize());
            }
            catch { }

            try
            {
                //_wifi.DiscoveringPeers();
            }
            catch { }
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            //wifi.DiscoveringPeers();
        }
        #endregion

        
        #region communication

        #region Check Current state of Companion
        private void OnCompanionStatusCheckup(object sender, CompanionDeviceEventArgs e)
        {
            if (e.WiFiEnable)
            {
                _wifiIsEnabled = true;
                Console.WriteLine("Wifi Enabled");
            }
            else
            {
                _wifiIsEnabled = false;
                Console.WriteLine("Wifi Diabled");
            }
        }

        #endregion

        #region Upload Module Files
        private void ModuleFileUpload(object sender, CompanionDeviceEventArgs e)
        {
            if (e.WiFiEnable)
            {
                _wifiIsEnabled = true;
                Console.WriteLine("Wifi Enabled");
            }
            else
            {
                _wifiIsEnabled = false;
                Console.WriteLine("Wifi Diabled");
            }
        }
        #endregion

        #region Download new statements
        private void DownloadNewStatements(object sender, CompanionDeviceEventArgs e)
        {
            if (e.WiFiEnable)
            {
                _wifiIsEnabled = true;
                Console.WriteLine("Wifi Enabled");
            }
            else
            {
                _wifiIsEnabled = false;
                Console.WriteLine("Wifi Diabled");
            }
        }
        #endregion

        #region Download Videos
        private void DownloadNewVideos(object sender, CompanionDeviceEventArgs e)
        {
            if (e.WiFiEnable)
            {
                _wifiIsEnabled = true;
                Console.WriteLine("Wifi Enabled");
            }
            else
            {
                _wifiIsEnabled = false;
                Console.WriteLine("Wifi Diabled");
            }
        }
        #endregion

        #region Loop for communication
        //get update
        private async void UpDateRequestLoop()
        {
            while (true)
            {
                try
                {


                    //StatusUpdate status = wifi.GetStatusUpdate();
                    //send this to another thread for processing
                    //if (status != null && status.ReceivedFrom != "")
                    //{
                    //    //Console.WriteLine("ReceivedFrom: "+status.ReceivedFrom);
                    //    //Console.WriteLine("ReceivedMessage: " + status.ReceivedMessage);
                    //}
                }
                catch { }
                await Task.Delay(1000);
            }
        }

        private async void ListenerLoop()
        {
            while (true)
            {
                //Listen for Statement response

                //Listen for Video response

                await Task.Delay(1000);
            }
        }

        //private async void UpDateRequestLoop(CompanionDeviceViewModel input)
        //{
        //    if (input != null)
        //    {
        //        while (true)
        //        {
        //            StatusUpdate output = new StatusUpdate();
        //            if (output != null)
        //            {
        //                output.SendTo = input.CompanionDevice.WifiMacAddress;
        //                //output.Address = input.CompanionDevice.WifiMacAddress;
        //            }
        //            //if socket is being used.
        //            if (input.WiFiPort != null && input.WiFiPort != 0)
        //            {
        //                output.Socket = input.WiFiPort;
        //            }

        //            //wifi.RequestDeviceStatus(output);
        //            await Task.Delay(600000);
        //        }
        //    }
        //}
        #endregion

        #endregion

        #region Usb serial communication
        private void SendUsbData()
        {

        }

        #endregion
    }
}
