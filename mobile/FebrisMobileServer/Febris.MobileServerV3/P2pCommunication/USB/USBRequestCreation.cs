// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.P2pCommunication.USB
{
    public class USBRequestCreation
    {
        IUSBRequests _request = DependencyService.Get<IUSBRequests>();

        #region Pairing                
        public async Task CompanionPairing()
        {           
            try
            {
                await _request.MakeRequest();

            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }            
        }
        #endregion

        #region upload file                
        //public async Task UploadFile(string input)
        //{
        //    try
        //    {
        //        CompanionSoftwareLogic context = new CompanionSoftwareLogic();
        //        CompanionAppViewModel appData = context.Get().Result ?? default;
        //        input = appData.LocalSoftwarePackage.UUID.ToString();// + ".zip";
        //        //need to unzip
        //        await _request.PrepareUpload(input);

        //    }
        //    catch (Exception ex) { Console.WriteLine(ex.Message); }
        //}

        //internal void UploadFile()
        //{
        //    try
        //    {
        //        CompanionSoftwareLogic context = new CompanionSoftwareLogic();
        //        CompanionAppViewModel appData = context.Get().Result ?? default;                                
        //        string softwareID = appData.LocalSoftwarePackage.UUID.ToString();
        //        ///send down to device specific layer so it can find the exact software                          
        //        string directoryPath = Path.Combine(FileSystem.UncompressedCompanionApplicationPath,softwareID);


        //        Task.Run(()=>_request.PrepareUpload(directoryPath));

        //        //_request.SendUpload(input);                

        //    }
        //    catch (Exception ex) { Console.WriteLine(ex.Message); }
        //}


        internal void InstallApplication()
        {
            try
            {
                CompanionSoftwareLogic context = new CompanionSoftwareLogic();
                CompanionAppViewModel appData = context.Get().Result ?? default;
                string softwareID = appData.LocalSoftwarePackage.UUID.ToString();
                ///send down to device specific layer so it can find the exact software                          
                string directoryPath = Path.Combine(FileSystem.UncompressedCompanionApplicationPath, softwareID);
                
                Task.Run(() => _request.PrepareInstall(directoryPath));
            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex.Message); 
                Utilities.PairingPageStatusHelper.GenericMessage("Preparation to Install software package has failed"); 
            }
        }

        internal void PushApplication()
        {
            try
            {
                CompanionSoftwareLogic context = new CompanionSoftwareLogic();
                CompanionAppViewModel appData = context.Get().Result ?? default;
                string softwareID = appData.LocalSoftwarePackage.UUID.ToString();
                ///send down to device specific layer so it can find the exact software                          
                string directoryPath = Path.Combine(FileSystem.UncompressedCompanionApplicationPath, softwareID);


                Task.Run(() => _request.PrepareUpload(directoryPath));

                //_request.SendUpload(input);                

            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex.Message);
                Utilities.PairingPageStatusHelper.GenericMessage("Preparation to send software package has failed");

            }
        }

        #endregion
    }
}
