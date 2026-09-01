// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication;
using Febris.MobileServerV3.P2pCommunication.USB;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Crypto = Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PairingPage : ContentPage, INotifyPropertyChanged
    {
        #region variables and constructors        
        private USBRequestCreation _uSBRequestCreation = new USBRequestCreation();
        //private ObservableCollection<CompanionDeviceViewModel> _deviceList { get; set; }
        //public ObservableCollection<CompanionDeviceViewModel> deviceList { get { return _deviceList; } }
        public PairingPage()
        {
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM.HardwareVM;
            //BindingContext = this;
            //_deviceList = new ObservableCollection<CompanionDeviceViewModel>(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList);
        }
        #endregion

        #region page operations
        //protected override void OnAppearing()
        //{
        //    //_deviceList = new ObservableCollection<CompanionDeviceViewModel>(LocalHardwareStaticDetails.PairedDeviceViewModelList);
        //    //DeviceList.ItemsSource = deviceList;
        //}
        #endregion

        #region Buttons

        private void RemovePairedDevice_Click(object sender, EventArgs args)
        {
            try
            {
                Button info = (Button)sender;
                CompanionDeviceViewModel selectedDevice = info.CommandParameter as CompanionDeviceViewModel;
                if (selectedDevice != null)
                {
                    PairDevice.OnCompanionDelete(selectedDevice);
                }
            }
            catch { }
        }

        #region Numeric-comparison pairing
        /// <summary>Opens the pairing modal. The list lives there, not on this page: pairing is a
        /// ceremony with a start and an end, so it gets a modal rather than a permanent panel.</summary>
        private async void OpenPairingModal_Click(object sender, EventArgs args)
        {
            try
            {
                await Navigation.PushModalAsync(new PairingModal());
            }
            catch (Exception ex)
            {
                Console.WriteLine("OpenPairingModal_Click failed: " + ex.Message);
            }
        }
        #endregion

        #region Bluetooth

        // BTDeviceScan_Click and Upload_Click were removed on 2026-09-01 with their buttons.
        //
        // Scan drove BTRequestCreation.CompanionPairing. Its discovery produced CompanionDevice
        // records carrying only Bluetooth fields and NO UniqueIdentifier, so nothing downstream
        // could turn one into a working peer. Upload pushed a file to a device's
        // BlueToothMacAddress, and the Companion has no Bluetooth receive path at all.
        //
        // Neither touched pairing. The ceremony is OpenPairingModal_Click above and runs entirely
        // over WiFi Direct, with PairingModal calling P2pPairingCoordinator.Begin and the frames
        // travelling on the P2P sockets. Discovery of a new headset happens when the Companion
        // joins the WiFi Direct group. See docs/MOBILE_AUTH.md.
        #endregion

        #region USB

        //private void USBDeviceScan_Click(object sender, EventArgs args)
        //{
        //    try
        //    {
        //        //Task.Run(() => _uSBRequestCreation.CompanionPairing());
        //        Task.Run(() => _uSBRequestCreation.UploadFile());//.CompanionPairing());
        //    }
        //    catch { }
        //}

        private void PushApplication_Click(object sender, EventArgs args)
        {
            try
            {
                Task.Run(() => _uSBRequestCreation.PushApplication());
            }
            catch { }
        }

        private void InstallApplication_Click(object sender, EventArgs args)
        {
            try
            {
                Task.Run(() => _uSBRequestCreation.InstallApplication());
            }
            catch { }
        }
        #endregion               
        #endregion

        #region List Handling
        //private void Device_Select(object sender, SelectionChangedEventArgs e)
        //{
        //    try
        //    {                
        //        LocalHardwareStaticDetails.SelectedDevice = (CompanionDeviceViewModel)DeviceList.SelectedItem;                
        //    }
        //    catch { }
        //}
        #endregion        
    }
}