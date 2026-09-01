// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CompanionAppPage : ContentPage
    {
        public CompanionAppPage()
        {
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM.CompanionAppVM;
            LocalHardwareStaticDetails.StaticMainVM.CompanionAppVM = CompanionAppVMSetup();
        }

        protected override void OnAppearing()
        {
            LocalHardwareStaticDetails.StaticMainVM.CompanionAppVM = CompanionAppVMSetup();
           
        }

        private CompanionAppViewModel CompanionAppVMSetup()
        {
            try
            {
                CompanionSoftwareLogic context = new CompanionSoftwareLogic();
                CompanionAppViewModel output = context.Get().Result ?? default;
                return output;
            }
            catch { return default; }
        }
        
        async void DeleteApp_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            string selected = b.CommandParameter as string;
            try
            {
                //RequestHelper.RemoveLocalModuleRequest(LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleList.DisplayedItem, selected);
                string path = Path.Combine(FileSystem.BasePath, LocalHardwareStaticDetails.StaticMainVM.CompanionAppVM.LocalSoftwarePackage.UUID.ToString() + ".zip");
                bool complete = await FileManager.DeleteFile(path);
                Console.WriteLine("File Removed: " + complete);
                LocalHardwareStaticDetails.StaticMainVM.CompanionAppVM.ExistsInSystem = !complete;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async void UpdateCheck_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            string selected = b.CommandParameter as string;
            try
            {
                CompanionSoftwareLogic logic = new CompanionSoftwareLogic();
                bool something = logic.CheckVersion().Result;


                //RequestHelper.RemoveLocalModuleRequest(LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleList.DisplayedItem, selected);
                //string path = Path.Combine(FileSystem.BasePath, selected + ".zip");

                //bool complete = FileManager.DeleteFile(path);
                Console.WriteLine("Version check success: " + something.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


    }
}