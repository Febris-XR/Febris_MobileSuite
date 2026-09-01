// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DevicePage : ContentPage, INotifyPropertyChanged
    {
        #region variables and constructors
        //private PairDevice _pairDevice = new PairDevice();
        //private Communication _communication = new Communication();
        //IWiFiService wifi = (Application.Current as Febris.MobileApp.App).wifi;

        //#region device list
        //private ObservableCollection<CompanionDeviceViewModel> _deviceList { get; set; }
        //public ObservableCollection<CompanionDeviceViewModel> deviceList 
        //{ 
        //    get { return _deviceList; } 
        //    set { 
        //        _deviceList = value;
        //        OnPropertyChanged(nameof(deviceList));
        //    }
        //}
        //#endregion
        
        public DevicePage()
        {
            InitializeComponent();
            //BindingContext = this;
            //wifi = DependencyService.Get<IWiFiService>();
        }
        #endregion

        #region page operations
        protected override void OnAppearing()
        {            
            //deviceList = new ObservableCollection<CompanionDeviceViewModel>(LocalHardwareStaticDetails.PairedDeviceViewModelList);            
            //////DeviceList.ItemsSource = deviceList;                        
        }
        #endregion

        #region Buttons
        private void DeviceInfo_Click(object sender, EventArgs args)
        {
            //try
            //{
            //    //get device from binding
            //    var referenceObject = (Button)sender;
            //    CompanionDeviceViewModel selectedItem = referenceObject.CommandParameter as CompanionDeviceViewModel;
            //    //use device to start up device modal
            //    Navigation.PushAsync(new DeviceInfoModal(selectedItem));
            //}
            //catch(Exception ex)
            //{
            //    Console.WriteLine("DeviceInfo_Click error: " + ex.Message);            
            //}
        }

        async void StartStreaming_Click(object sender, EventArgs args)
        {
            //var b = (Button)sender;
            //CompanionDeviceViewModel selected = b.CommandParameter as CompanionDeviceViewModel;
            //try
            //{
            //    //request stream from this device                
            //    RequestHelper.VideoStreamRequest(selected);
            //    Navigation.PushAsync(new VideoStreamPage(selected));
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}
        }

        #endregion

        #region Select device
        private void SelectDevice_Click(object sender, EventArgs args)
        {
            //try
            //{                
            //    var b = (Button)sender;
            //    CompanionDeviceViewModel selectedDevice = b.CommandParameter as CompanionDeviceViewModel;
            //    if (selectedDevice != null)
            //    {
            //        if (LocalHardwareStaticDetails.SelectedDevice == selectedDevice)
            //        {
            //            //selectedDevice.DeviceSelected = false;                        
            //            LocalHardwareStaticDetails.SelectedDevice = null;
            //        }
            //        else
            //        {
            //            //selectedDevice.DeviceSelected=true;
            //            LocalHardwareStaticDetails.SelectedDevice = selectedDevice;                    
            //        }                    
            //        SetButtonColors();
            //    }
            //}
            //catch { }
        }
        //private void Device_Select(object sender, SelectionChangedEventArgs e)
        //{
        //    try
        //    {
        //        var selectedDevice = (CompanionDeviceViewModel)DeviceList.SelectedItem;

        //        if (LocalHardwareStaticDetails.SelectedDevice == selectedDevice)
        //        {                    
        //            LocalHardwareStaticDetails.SelectedDevice = null;
        //        }
        //        else
        //        {                    
        //            LocalHardwareStaticDetails.SelectedDevice = selectedDevice;
        //        }
        //        SetButtonColors();
        //    }
        //    catch { }
        //}
        //private void Device_Deselect(object sender, SelectionChangedEventArgs e)
        //{
        //    try
        //    {
        //        //var selectedDevice = (CompanionDeviceViewModel)DeviceList.SelectedItem;
        //        var selectedDevice = (CompanionDeviceViewModel)deviceList.SelectedItem;

        //        if (LocalHardwareStaticDetails.SelectedDevice == selectedDevice)
        //        {
        //            LocalHardwareStaticDetails.SelectedDevice = null;
        //        }
        //        else
        //        {
        //            LocalHardwareStaticDetails.SelectedDevice = selectedDevice;
        //        }
        //        SetButtonColors();
        //    }
        //    catch { }
        //}
        private void SetButtonColors()
        {
            //foreach (var i in deviceList)
            //{
            //    if (i == LocalHardwareStaticDetails.SelectedDevice)
            //    {
            //        i.DeviceSelected = true;
            //    }
            //    else
            //    {
            //        i.DeviceSelected = false;
            //    }
            //}
        }                
        #endregion

    }
}