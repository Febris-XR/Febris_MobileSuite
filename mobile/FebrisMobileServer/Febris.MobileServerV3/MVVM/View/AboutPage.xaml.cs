// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.Resources;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
        
        private InitalizationHandler _initalization = new InitalizationHandler();
        //private TokenHandler _tokenHandler = new TokenHandler();
        
        public AboutPage()
        {           
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM;
        }
        
        public void Reload(object sender, EventArgs args)
        {
            //Task.Run(() => _tokenHandler.GetToken()).Wait();
            //Task.Run(()=>_initalization.Initalize());
            Task.Run(()=> InitalizationLogic.Get());
        }


    }
}