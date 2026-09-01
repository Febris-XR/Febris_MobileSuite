// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileApp.Resources;
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class CompanionDeviceViewModel : BaseViewModel
    {      
        public CompanionDeviceViewModel()
        {
            RenameDeviceConfig = new RelayCommand(x => { RenameDeviceConfigCommand = CompanionDevice; });            
        }

        #region Actions
        public RelayCommand RenameDeviceConfig { get; set; }

        private bool _nameEditable { get; set; }
        public bool NameEditable
        {
            get
            {
                return _nameEditable;
            }
            set
            {
                _nameEditable = value;
                OnPropertyChanged(nameof(NameEditable));
            }
        }

        private object _renameDeviceConfigCommand;
        public object RenameDeviceConfigCommand
        {
            get { return _renameDeviceConfigCommand; }
            set
            {                
                if (NameEditable)
                {
                    _renameDeviceConfigCommand = value;
                    //Task.Run(() => ConfigLogic.SaveSettings());
                    Task.Run(() => PairDevice.UpdateCompanionDevice(CompanionDevice));
                    //LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem = PairDevice.UpdateCompanionDevice(CompanionDevice).Result;
                    //var something = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    //    .Where(i => i.CompanionDevice.UniqueIdentifier == CompanionDevice.UniqueIdentifier)
                    //    .Single();
                    //something = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem;

                    OnPropertyChanged();
                }
                NameEditable = !NameEditable;
            }
        }

        #endregion

        #region Keep viewmodel updated - replaced by baseviewmodel        
        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged([CallerMemberName] string name = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        //}

        #endregion

        //public CompanionDeviceViewModel()
        //{
        //    ImageService.Instance.Initialize();
        //}

        private CompanionDevice _companionDevice;
        public CompanionDevice CompanionDevice
        {
            get
            {
                return _companionDevice;
            }
            set
            {
                if (value == _companionDevice) return;
                //_companionDevice = null;
                _companionDevice = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(CompanionDevice));
            }
        }

        #region Bluetooth
        private Color _bluetoothConnectedButtonColor;// = (Color)Application.Current.Resources["Secondary"];
        public Color BluetoothConnectedButtonColor
        {
            get
            {
                if (_blueToothConnected == true)
                {
                    _bluetoothConnectedButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _bluetoothConnectedButtonColor = (Color)Application.Current.Resources["Transparent"];
                }
                return _bluetoothConnectedButtonColor;
            }
            set
            {
                if (_blueToothConnected == true)
                {
                    _bluetoothConnectedButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _bluetoothConnectedButtonColor = (Color)Application.Current.Resources["Transparent"];
                }
                _bluetoothConnectedButtonColor = value;
                OnPropertyChanged(nameof(BluetoothConnectedButtonColor));
            }
        }
        public bool _blueToothConnected { get; set; }
        public bool BlueToothConnected
        {
            get
            {
                return _blueToothConnected;
            }
            set
            {
                _blueToothConnected = value;

                if (_blueToothConnected == true)
                {
                    BluetoothConnectedButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    BluetoothConnectedButtonColor = (Color)Application.Current.Resources["Secondary"];
                }
                OnPropertyChanged(nameof(BlueToothConnected));
            }
        }
        #endregion

        #region Wifi
        private Color _wiFiConnectedButtonColor;
        public Color WiFiConnectedButtonColor
        {
            get
            {
                //OnPropertyChanged("_wiFiConnected");
                //OnPropertyChanged("WiFiConnected");
                if (_wiFiConnected == true)
                {
                    _wiFiConnectedButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _wiFiConnectedButtonColor = (Color)Application.Current.Resources["Transparent"];
                }
                return _wiFiConnectedButtonColor;
            }
            set
            {
                if (_wiFiConnected == true)
                {
                    _wiFiConnectedButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _wiFiConnectedButtonColor = (Color)Application.Current.Resources["Transparent"];
                }
                _wiFiConnectedButtonColor = value;
                OnPropertyChanged(nameof(WiFiConnectedButtonColor));

            }
        }

        private bool _wiFiConnected { get; set; }
        public bool WiFiConnected
        {
            get
            {
                return _wiFiConnected;
            }
            set
            {
                _wiFiConnected = value;

                if (_wiFiConnected == true)
                {
                    WiFiConnectedButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                    StatusMessage = "Device connected and receiving data.";
                    VideoStreamAvailable = true;
                }
                else
                {
                    WiFiConnectedButtonColor = (Color)Application.Current.Resources["Secondary"];
                    StatusMessage = "Device connection closed, searching for new connection.";
                    VideoStreamAvailable = false;
                }
                //OnPropertyChanged("WiFiConnected");
                //OnPropertyChanged(nameof(WiFiConnected));
                OnPropertyChanged();
            }
        }

        private string _wiFiIPAddress;

        /// <summary>Where SocketSender routes frames for this device. Set by UpdateIPAddress when
        /// a frame arrives from a RESOLVED peer, so a non-empty value means we have heard from
        /// this Companion over WiFi and have somewhere to send.</summary>
        public string WiFiIPAddress
        {
            get { return _wiFiIPAddress; }
            set
            {
                if (_wiFiIPAddress == value) return;
                _wiFiIPAddress = value;
                OnPropertyChanged();
            }
        }

        public int WiFiPort { get; set; }
        public int WiFiStatus { get; set; }
        #endregion

        #region Video Stream
        private Color _videoStreamAvailableButtonColor;
        public Color VideoStreamAvailableButtonColor
        {
            get
            {
                if (VideoStreamAvailable == true)
                {
                    _videoStreamAvailableButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _videoStreamAvailableButtonColor = (Color)Application.Current.Resources["Transparent"];
                }
                return _videoStreamAvailableButtonColor;
            }
            set
            {
                if (VideoStreamAvailable == true)
                {
                    _videoStreamAvailableButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _videoStreamAvailableButtonColor = (Color)Application.Current.Resources["Transparent"];
                }
                _videoStreamAvailableButtonColor = value;
                OnPropertyChanged();// nameof(VideoStreamAvailableButtonColor));

            }
        }

        private bool _videoStreamConnected { get; set; }
        public bool VideoStreamConnected
        {
            get
            {
                return _videoStreamConnected;
            }
            set
            {
                _videoStreamConnected = value;

                if (_videoStreamConnected == true)
                {
                    //VideoStreamAvailableButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                    StatusMessage += " Video Stream Running.";
                }
                else
                {
                    //VideoStreamAvailableButtonColor = (Color)Application.Current.Resources["Secondary"];
                    StatusMessage += " Video Stream Closed.";
                }
                OnPropertyChanged();// (nameof(VideoStreamConnected));
            }
        }

        private bool _videoStreamAvailable = false;
        public bool VideoStreamAvailable
        {
            get
            {
                return _videoStreamAvailable;
            }
            set
            {
                _videoStreamAvailable = value;

                if (_videoStreamAvailable == true)
                {
                    // VideoStreamConnectedButtonVisab = (Color)Application.Current.Resources["ButtonSelection"];
                    StatusMessage += " Video Stream Is Available.";
                    VideoStreamAvailableButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    //VideoStreamConnectedButtonColor = (Color)Application.Current.Resources["Secondary"];
                    StatusMessage += " Video Stream Is Not Available.";
                    VideoStreamAvailableButtonColor = (Color)Application.Current.Resources["Secondary"];
                }
                OnPropertyChanged();// (nameof(VideoStreamAvailable));
            }
        }

        // StreamImage (ImageSource) removed: the stream is H.264 decoded straight onto the
        // Surface behind VideoSurfaceView, so no picture passes through the view model and
        // there is nothing here to bind. See docs/MOBILE_P2P_VIDEO.md 3.

        //private List<Image> _imageList;
        //public List<Image> ImageList
        //{
        //    get { return _imageList; }
        //    set { SetProperty(ref _imageList, value); }
        //}


        //private byte[] _videoStream { get; set; }
        //public byte[] VideoStream
        //{
        //    get
        //    {
        //        return _videoStream;
        //    }
        //    set
        //    {                
        //        if (value == _videoStream) return;
        //        _videoStream = value;


        //    //    ImageService.Instance
        //    //.LoadStream(GetStreamFromImageByte(,value))
        //    //.Into(imageView);
        //    //ImageService.Instance.StreamImageSource



        //        OnPropertyChanged(nameof(VideoStream));
        //    }
        //}

        //private Stream _videoStream { get; set; }
        //public Stream VideoStream
        //{
        //    get
        //    {
        //        return _videoStream;
        //    }
        //    set
        //    {
        //        if (value == _videoStream) return;
        //        _videoStream = value;
        //        OnPropertyChanged(nameof(VideoStream));
        //    }
        //}

        #endregion

        #region Select device                      

        private Color _selectedDeviceBackgroundButtonColor;
        public Color SelectedDeviceBackgroundButtonColor
        {
            get
            {
                if (_deviceSelected == true)
                {
                    _selectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _selectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["Transparent"];
                }
                return _selectedDeviceBackgroundButtonColor;
            }
            set
            {
                if (_deviceSelected == true)
                {
                    _selectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _selectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["Transparent"];
                }
                _selectedDeviceBackgroundButtonColor = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(SelectedDeviceBackgroundButtonColor));

            }
        }
        private bool _deviceSelected { get; set; }
        public bool DeviceSelected
        {
            get
            {
                return _deviceSelected;
            }
            set
            {
                _deviceSelected = value;

                if (_deviceSelected == true)
                {
                    SelectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    SelectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["Secondary"];
                }
                OnPropertyChanged();
                //OnPropertyChanged(nameof(DeviceSelected));
            }
        }

        #endregion

        #region Send Companion application                      

        private Color _sendCompanionAppBackgroundButtonColor;
        public Color SendCompanionAppBackgroundButtonColor
        {
            get
            {
                if (_deviceSelected == true)
                {
                    _selectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _selectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["Secondary"];
                }
                return _selectedDeviceBackgroundButtonColor;
            }
            set
            {
                if (_deviceSelected == true)
                {
                    _selectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    _selectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["Secondary"];
                }
                _selectedDeviceBackgroundButtonColor = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(SendCompanionAppBackgroundButtonColor));

            }
        }
        private bool _companionApplicationSent { get; set; }
        public bool CompanionApplicationSent
        {
            get
            {
                return _deviceSelected;
            }
            set
            {
                _deviceSelected = value;

                if (_deviceSelected == true)
                {
                    SelectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
                }
                else
                {
                    SelectedDeviceBackgroundButtonColor = (Color)Application.Current.Resources["Secondary"];
                }
                OnPropertyChanged();
                //OnPropertyChanged(nameof(CompanionApplicationSent));
            }
        }

        #endregion

        #region Info Button Color
        //private Color _infoButtonColor;
        //public Color InfoButtonColor
        //{
        //    get
        //    {
        //        //if (_infoButtonColor == true)
        //        //{
        //        _infoButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
        //        //}
        //        //else
        //        //{
        //        //    _bluetoothConnectedButtonColor = (Color)Application.Current.Resources["Secondary"];
        //        //}
        //        return _infoButtonColor;
        //    }
        //    set
        //    {
        //        //if (_infoButtonColor == true)
        //        //{
        //        _infoButtonColor = (Color)Application.Current.Resources["ButtonSelection"];
        //        //}
        //        //else
        //        //{
        //        //    _infoButtonColor = (Color)Application.Current.Resources["Secondary"];
        //        //}
        //        //_infoButtonColor = value;
        //        //OnPropertyChanged(nameof(BluetoothConnectedButtonColor));
        //    }
        //}
        #endregion

        #region Status


        private bool _progressBar = true;
        public bool ProgressBar
        {
            get
            {
                return _progressBar;
            }
            set
            {
                if (value == _progressBar) return;
                _progressBar = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(ProgressBar));
                //OnPropertyChanged("ProgressBar");
            }
        }
        private string _statusMessage { get; set; }
        public string StatusMessage
        {
            get
            {
                if (String.IsNullOrEmpty(_statusMessage))
                {
                    ProgressBar = true;
                    return "Trying To Connect...";
                }
                else
                {
                    return _statusMessage;
                }
            }
            set
            {
                if (value == _statusMessage) return;
                _statusMessage = value;
                OnPropertyChanged();
                //OnPropertyChanged("StatusMessage");
                //OnPropertyChanged(nameof(StatusMessage));
            }
        }


        #region Battery 
        private string _batteryIcon;
        public string BatteryIcon
        {
            get
            {
                if (BatteryCharge < 8)
                {
                    _batteryIcon = (string)Application.Current.Resources["Battery_Empty"];
                }
                else if (BatteryCharge < 35)
                {
                    _batteryIcon = (string)Application.Current.Resources["Battery_Quarter"];
                }
                else if (BatteryCharge < 65)
                {
                    _batteryIcon = (string)Application.Current.Resources["Battery_Half"];
                }
                else if (BatteryCharge < 85)
                {
                    _batteryIcon = (string)Application.Current.Resources["Battery_ThreeQuarter"];
                }
                else
                {
                    _batteryIcon = (string)Application.Current.Resources["Battery_Full"];
                }
                return _batteryIcon;
            }
            set
            {
                //if (BatteryCharge < 8)
                //{
                //    _batteryIcon = (string)Application.Current.Resources["Battery_Empty"];
                //}
                //else if (BatteryCharge < 35)
                //{
                //    _batteryIcon = (string)Application.Current.Resources["Battery_Quarter"];
                //}
                //else if (BatteryCharge < 65)
                //{
                //    _batteryIcon = (string)Application.Current.Resources["Battery_Half"];
                //}
                //else if (BatteryCharge < 85)
                //{
                //    _batteryIcon = (string)Application.Current.Resources["Battery_ThreeQuarter"];
                //}
                //else
                //{
                //    _batteryIcon = (string)Application.Current.Resources["Battery_Full"];
                //}
                if (value == _batteryIcon) return;
                _batteryIcon = value;
                OnPropertyChanged(nameof(BatteryIcon));
                //OnPropertyChanged(nameof(BatteryCharge));
            }
        }

        private Color _batteryIconColor;
        public Color BatteryIconColor
        {
            get
            {
                if (BatteryCharge < 8)
                {
                    _batteryIconColor = (Color)Application.Current.Resources["VeryLow_Battery"];
                }
                else if (BatteryCharge < 35)
                {
                    _batteryIconColor = (Color)Application.Current.Resources["Low_Battery"];
                }
                else if (BatteryCharge < 65)
                {
                    _batteryIconColor = (Color)Application.Current.Resources["Medium_Battery"];
                }
                else if (BatteryCharge < 85)
                {
                    _batteryIconColor = (Color)Application.Current.Resources["High_Battery"];
                }
                else
                {
                    _batteryIconColor = (Color)Application.Current.Resources["Full_Battery"];
                }
                return _batteryIconColor;
            }
            set
            {
                //if (BatteryCharge < 8)
                //{
                //    _batteryIconColor = (Color)Application.Current.Resources["VeryLow_Battery"];
                //}
                //else if (BatteryCharge < 35)
                //{
                //    _batteryIconColor = (Color)Application.Current.Resources["Low_Battery"];
                //}
                //else if (BatteryCharge < 65)
                //{
                //    _batteryIconColor = (Color)Application.Current.Resources["Medium_Battery"];
                //}
                //else if (BatteryCharge < 90)
                //{
                //    _batteryIconColor = (Color)Application.Current.Resources["High_Battery"];
                //}
                //else
                //{
                //    _batteryIconColor = (Color)Application.Current.Resources["Full_Battery"];
                //}
                if (value == _batteryIconColor) return;
                _batteryIconColor = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(BatteryIconColor));

                //OnPropertyChanged(nameof(BatteryCharge));
            }
        }

        private double _batteryCharge { get; set; }
        public double BatteryCharge
        {
            get
            {
                return _batteryCharge;
            }
            set
            {
                if (value == _batteryCharge) return;
                _batteryCharge = value;
                if (BatteryCharge < 8)
                {
                    BatteryIcon = (string)Application.Current.Resources["Battery_Empty"];
                    BatteryIconColor = (Color)Application.Current.Resources["VeryLow_Battery"];
                }
                else if (BatteryCharge < 35)
                {
                    BatteryIcon = (string)Application.Current.Resources["Battery_Quarter"];
                    BatteryIconColor = (Color)Application.Current.Resources["Low_Battery"];
                }
                else if (BatteryCharge < 65)
                {
                    BatteryIcon = (string)Application.Current.Resources["Battery_Half"];
                    BatteryIconColor = (Color)Application.Current.Resources["Medium_Battery"];
                }
                else if (BatteryCharge < 85)
                {
                    BatteryIcon = (string)Application.Current.Resources["Battery_ThreeQuarter"];
                    BatteryIconColor = (Color)Application.Current.Resources["High_Battery"];
                }
                else
                {
                    BatteryIcon = (string)Application.Current.Resources["Battery_Full"];
                    BatteryIconColor = (Color)Application.Current.Resources["Full_Battery"];
                }
                OnPropertyChanged();
                //OnPropertyChanged(nameof(BatteryCharge));
            }
        }

        //void SetBatteryIcon()
        //{

        //}
        #endregion

        private List<string> _zippedFileList { get; set; }
        public List<string> ZippedFileList
        {
            get
            {
                return _zippedFileList;
            }
            set
            {
                _zippedFileList = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(ZippedFileList));
            }
        }

        private List<string> _appList { get; set; }
        public List<string> AppList
        {
            get
            {
                return _appList;
            }
            set
            {
                _appList = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(AppList));
            }
        }


        //module lists
        //private List<string> _moduleIdList { get; set; }
        //public List<string> ModuleIdList
        //{
        //    get
        //    {
        //        return _moduleIdList;
        //    }
        //    set
        //    {
        //        if (value == _moduleIdList) return;
        //        _moduleIdList = value;
        //        foreach (var i in _moduleIdList)
        //        {
        //           // ModuleBaseList.Add(LocalHardwareStaticDetails._testList.Where(j => j.UUID == Guid.Parse(i)).Single());
        //        }
        //        //OnPropertyChanged("ModuleBaseIdList");
        //        OnPropertyChanged(nameof(ModuleIdList));
        //    }
        //}
        private List<Guid> _moduleIdList { get; set; }
        public List<Guid> ModuleIdList
        {
            get
            {
                return _moduleIdList;
            }
            set
            {
                if (value == _moduleIdList) return;
                _moduleIdList = value;
                foreach (var i in _moduleIdList)
                {
                    ModuleList.Add(LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleList.Where(j => j.UUID == i).Single());
                }
                OnPropertyChanged();
                //OnPropertyChanged("ModuleBaseIdList");
                //OnPropertyChanged(nameof(ModuleIdList));
            }
        }
        private List<string> _moduleFileList { get; set; }
        public List<string> ModuleFileList
        {
            get
            {
                return _moduleFileList;
            }
            set
            {
                if (value == _moduleFileList) return;
                _moduleFileList = value;
                //foreach (var i in _moduleFileList)
                //{
                //    Guid uuid = Guid.Parse(i);
                //    ModuleIdList.Add(uuid);
                //    //ModuleList.Add(LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleList.Where(j => j.UUID == i).Single());
                //}
                //OnPropertyChanged("ModuleBaseIdList");
                //OnPropertyChanged(nameof(ModuleFileList));
                OnPropertyChanged();
            }
        }
        private List<Module> _moduleList { get; set; }
        public List<Module> ModuleList
        {
            get
            {
                return _moduleList;
            }
            set
            {
                if (value == _moduleList) return;
                _moduleList = value;
                //OnPropertyChanged(nameof(ModuleList));
                //OnPropertyChanged("ModuleBaseList");
                OnPropertyChanged();
            }
        }
        //statement lists
        private List<string> _statementIdList { get; set; }
        public List<string> StatementIdList
        {
            get
            {
                return _statementIdList;
            }
            set
            {
                if (value == _statementIdList) return;
                _statementIdList = value;
                if (_statementIdList.Count == 0 && VideoIdList?.Count == 0)
                {
                    ProgressBar = false;
                    StatusMessage = "Data Packages are up to date";
                }
                else
                {
                    ProgressBar = true;
                }
                //OnPropertyChanged(nameof(StatementIdList));
                OnPropertyChanged();
            }
        }
        private List<string> _oldStatementIdList { get; set; }
        public List<string> OldStatementIdList
        {
            get
            {
                return _oldStatementIdList;
            }
            set
            {
                if (value == _oldStatementIdList) return;
                _oldStatementIdList = value;
                //OnPropertyChanged(nameof(OldStatementIdList));
                OnPropertyChanged();
            }
        }
        //video lists
        private List<string> _videoIdList { get; set; }
        public List<string> VideoIdList
        {
            get
            {
                return _videoIdList;
            }
            set
            {
                if (value == _videoIdList) return;
                _videoIdList = value;
                //OnPropertyChanged(nameof(VideoIdList));
                OnPropertyChanged();
            }
        }
        private List<string> _oldVideoIdList { get; set; }
        public List<string> OldVideoIdList
        {
            get
            {
                return _oldVideoIdList;
            }
            set
            {
                if (value == _oldVideoIdList) return;
                _oldVideoIdList = value;
                //OnPropertyChanged(nameof(OldVideoIdList));
                OnPropertyChanged();
            }
        }


        private long _storageSpaceRemaining { get; set; }
        public long StorageSpaceRemaining
        {
            get
            {
                return _storageSpaceRemaining;
            }
            set
            {
                if (value == _storageSpaceRemaining) return;
                _storageSpaceRemaining = value;
                OnPropertyChanged();
                //OnPropertyChanged(nameof(StorageSpaceRemaining));
            }
        }


        #endregion



    }
}
