// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeviceInfoModal : ContentPage, INotifyPropertyChanged
    {
        #region var
        //private CompanionDeviceViewModel _device { get; set; }
        //public CompanionDeviceViewModel device
        //{
        //    get { return _device; }
        //    set
        //    {
        //        _device = value;
        //        OnPropertyChanged(nameof(device));
        //    }
        //}

        //private bool _NameEditable { get; set; }
        //public bool NameEditable
        //{
        //    get
        //    {
        //        return _NameEditable;
        //    }
        //    set
        //    {
        //        _NameEditable = value;
        //        OnPropertyChanged(nameof(NameEditable));
        //    }
        //}

        #endregion

        public DeviceInfoModal()//CompanionDeviceViewModel selectedItem)
        {
            InitializeComponent();
            //device = selectedItem;
            //NameEditable = false;
            //this.BindingContext = device;
            BindingContext = LocalHardwareStaticDetails.StaticMainVM.HardwareVM;//.DisplayedItem;
        }

        //protected override void OnAppearing()
        //{
        //    //NameEditable = false;
        //}

        //async void RenameDevice_Click(object sender, EventArgs args)
        //{
        //    NameEditable = !NameEditable;
        //    EnableEditing.IsEnabled = NameEditable;
        //}

        async void StartStreaming_Click(object sender, EventArgs args)
        {
            //This page's BindingContext is the HardwareViewModel (see the constructor), so
            //CommandParameter="{Binding}" hands back a HardwareViewModel and the old
            //`b.CommandParameter as CompanionDeviceViewModel` always resolved to null, which
            //silently no-oped the button. DeleteOldStatements_Click below already used the
            //correct source. Both Task-returning calls are now awaited as well, so a failure
            //surfaces in the catch instead of being swallowed on a detached task.
            try
            {
                CompanionDeviceViewModel selected = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem;
                if (selected == null)
                {
                    Console.WriteLine("StartStreaming_Click: no device is displayed, ignoring");
                    return;
                }
                await RequestHelper.VideoStreamRequest(selected);
                await Navigation.PushAsync(new VideoStreamPage());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async void UninstallModule_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            string selected = b.CommandParameter as string;
            try
            {
                RequestHelper.UninstallModuleRequest(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem, selected);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        async void InstallModule_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            string selected = b.CommandParameter as string;
            try
            {
                RequestHelper.InstallModuleRequest(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem, selected);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async void ReinstallModule_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            string selected = b.CommandParameter as string;
            try
            {
                RequestHelper.ReinstallModuleRequest(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem, selected);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async void RemoveModule_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            string selected = b.CommandParameter as string;
            try
            {
                RequestHelper.RemoveModuleRequest(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem,selected);                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        async void RemoveZippedModule_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            string selected = b.CommandParameter as string;
            try
            {
                RequestHelper.RemoveZippedModuleRequest(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem, selected);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        async void UploadModule_Click(object sender, EventArgs args)
        {
            //Same null-cast defect as StartStreaming_Click above, same fix.
            try
            {
                CompanionDeviceViewModel selected = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem;
                if (selected == null)
                {
                    Console.WriteLine("UploadModule_Click: no device is displayed, ignoring");
                    return;
                }
                await RequestHelper.UploadModuleRequest(selected);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async void DeleteOldStatements_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            //CompanionDeviceViewModel selected = b.CommandParameter as CompanionDeviceViewModel;
            try
            {
                RequestHelper.DeleteOldStatementsRequest(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem);                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        async void DeleteOldVideos_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            //CompanionDeviceViewModel selected = b.CommandParameter as CompanionDeviceViewModel;
            try
            {
                RequestHelper.DeleteOldVideosRequest(LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}