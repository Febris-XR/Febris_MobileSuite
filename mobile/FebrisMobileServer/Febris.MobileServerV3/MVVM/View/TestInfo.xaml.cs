// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TestInfo : ContentPage
    {

        //private ModuleBaseViewModel _moduleBaseViewModel;
        //public ModuleBaseViewModel moduleBaseViewModel
        //{
        //    get { return _moduleBaseViewModel; }
        //    set
        //    {
        //        _moduleBaseViewModel = value;
        //        OnPropertyChanged(nameof(moduleBaseViewModel));
        //    }
        //}
        public TestInfo()//ViewModels.ModuleBaseViewModel selected)
        {
            InitializeComponent();
            //moduleBaseViewModel = selected;
            //BindingContext = moduleBaseViewModel;
        }

        //protected override void OnAppearing()
        //{
        //    //populate list of devices it is on
        //    PopulateDeviceList();
        //    //make sure it is installed

        //    //maybe some other stuff
        //}

        //private void PopulateDeviceList()
        //{

        //}

        //private void PopulateAnotherThingList()
        //{

        //}

    }
}