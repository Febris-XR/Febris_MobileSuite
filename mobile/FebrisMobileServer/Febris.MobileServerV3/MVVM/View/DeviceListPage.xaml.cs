// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeviceListPage : ContentPage, INotifyPropertyChanged
    {
        public DeviceListPage()
        {
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM.HardwareVM;
        }

        //protected override void OnAppearing()
        //{
        //    //LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemResultList = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
        //    //    .Where(i => i.CompanionDevice.WifiMacAddress != default).ToList();
        //    BindingContext = LocalHardwareStaticDetails.StaticMainVM.HardwareVM;
        //}


        private void DeviceInfo_Click(object sender, EventArgs args)
        {
            try
            {
                //get device from binding
                //var referenceObject = (Button)sender;
                //CompanionDeviceViewModel selectedItem = referenceObject.CommandParameter as CompanionDeviceViewModel;
                //use device to start up device modal
                Navigation.PushAsync(new DeviceInfoModal());
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeviceInfo_Click error: " + ex.Message);
            }
        }

    }
}