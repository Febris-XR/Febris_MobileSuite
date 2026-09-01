// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProfessionalInfo : ContentPage, INotifyPropertyChanged
    {
        //ProfessionalViewModel _professionalViewModel { get; set; }
        //ProfessionalViewModel professionalViewModel
        //{
        //    get { return _professionalViewModel; }
        //    set
        //    {
        //        _professionalViewModel = value;
        //        OnPropertyChanged(nameof(professionalViewModel));
        //    }
        //}
        //public ProfessionalInfo(ProfessionalViewModel selectedProfessional)
        //{
        //    InitializeComponent();
        //    professionalViewModel = selectedProfessional;
        //    BindingContext = professionalViewModel;
        //}
        
        ////async void OnDismissButtonClicked(object sender, EventArgs args)
        ////{
        ////    await Navigation.PopModalAsync();
        ////}

        //protected override void OnAppearing()
        //{            
        //    ImageUpdater();
        //}

        //private void ImageUpdater()
        //{
        //    try
        //    {
        //        if (professionalViewModel.Professional.PhotoOfProfessional != null
        //            && professionalViewModel.Professional.PhotoOfProfessional != @"/images/DefaultMedia/DefaultPerson.png")
        //        {
        //            string uriPath = LocalHardwareStaticDetails.professionalImageLink + professionalViewModel.Professional.Id.ToString();
        //            FebrisLocalLibrary.Communication.FebrisRestClient request = new FebrisLocalLibrary.Communication.FebrisRestClient(/*_log, _config*/)
        //            {
        //                endPoint = uriPath,
        //                httpMethod = FebrisLocalLibrary.Communication.httpVerb.GET,
        //                authTech = FebrisLocalLibrary.Communication.AuthenticaitonTechnique.Token,
        //                authType = FebrisLocalLibrary.Communication.Authenticationtype.BearerToken,
        //            };
        //            string response = request.MakeRequest().Result;
        //            if (response == "Could Not Find Anything Here")
        //            {
        //                response = request.MakeRequest().Result;
        //            }
        //            byte[] requestedBitMap = JsonConvert.DeserializeObject<byte[]>(response);                    
        //            var bitmapStream = new System.IO.MemoryStream(requestedBitMap);                    
        //            ProfessionalPhoto.Source = ImageSource.FromStream(() => bitmapStream);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error on image updater: "+ex.Message);
        //        //_log.LogInformation(ex.Message);
        //    }
        //}

        #region old
        //protected override void OnAppearing()
        //{
        //    //ImageHandler.DefaultImageCheck();
        //    ImageUpdater();

        //    IdentificationNumber.Text = LocalHardwareStaticDetails.selectedProfessional.IdentificationNumber.ToString();
        //    Name.Text = LocalHardwareStaticDetails.selectedProfessional.LastName.ToString() + ", " + LocalHardwareStaticDetails.selectedProfessional.FirstName.ToString();
        //    PhoneNumber.Text = LocalHardwareStaticDetails.selectedProfessional.PhoneNumber.ToString();
        //    Email.Text = LocalHardwareStaticDetails.selectedProfessional.EmailAddress.ToString();
        //    Gender.Text = LocalHardwareStaticDetails.selectedProfessional.Gender.ToString();
        //    Occupation.Text = LocalHardwareStaticDetails.selectedProfessional.UserAccountType.ToString();
        //}
        //private void ImageUpdater()
        //{
        //    try
        //    {
        //        if (LocalHardwareStaticDetails.selectedProfessional.PhotoOfProfessional != null
        //            && LocalHardwareStaticDetails.selectedProfessional.PhotoOfProfessional != @"/images/DefaultMedia/DefaultPerson.png"
        //            /*&& VariableLib.selectedProfessional.PhotoOfProfessional != VariableLib.professionalImageLink + "DefaultPerson.png"*/)
        //        {
        //            string uriPath = LocalHardwareStaticDetails.professionalImageLink + LocalHardwareStaticDetails.selectedProfessional.Id.ToString();
        //            FebrisLocalLibrary.Communication.FebrisRestClient request = new FebrisLocalLibrary.Communication.FebrisRestClient(/*_log, _config*/)
        //            {
        //                endPoint = uriPath,
        //                httpMethod = FebrisLocalLibrary.Communication.httpVerb.GET,
        //                authTech = FebrisLocalLibrary.Communication.AuthenticaitonTechnique.Token,
        //                authType = FebrisLocalLibrary.Communication.Authenticationtype.BearerToken,
        //            };


        //            string response = request.MakeRequest().Result;
        //            if (response == "Could Not Find Anything Here")
        //            {
        //                response = request.MakeRequest().Result;
        //            }

        //            byte[] requestedBitMap = JsonConvert.DeserializeObject<byte[]>(response);

        //            //BitmapImage bitmap = new BitmapImage();
        //            //bitmap.BeginInit();
        //            //bitmap.StreamSource = new System.IO.MemoryStream(requestedBitMap);
        //            //bitmap.EndInit();

        //            //ProfessionalPhoto.Source = bitmap;

        //            //BitmapImage bitmap = new BitmapImage();
        //            //bitmap.BeginInit();
        //            //bitmap.StreamSource = new System.IO.MemoryStream(requestedBitMap);
        //            //bitmap.EndInit();

        //            var bitmapStream = new System.IO.MemoryStream(requestedBitMap);
        //            //var bitmap = SKBitmap.Decode(bitmapStream);

        //            //ProfessionalPhoto.Source = SKImage.FromBitmap(bitmap);
        //            ProfessionalPhoto.Source = ImageSource.FromStream(()=> bitmapStream);


        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        //_log.LogInformation(ex.Message);
        //    }
        //}
        #endregion
    }
}