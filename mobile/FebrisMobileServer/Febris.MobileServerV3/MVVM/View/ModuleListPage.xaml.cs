// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ModuleListPage : ContentPage
    {
        public ModuleListPage()
        {
            try
            {
                InitializeComponent();
                BindingContext = LocalHardwareStaticDetails.StaticMainVM.ModuleVM;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

        }
    }
}