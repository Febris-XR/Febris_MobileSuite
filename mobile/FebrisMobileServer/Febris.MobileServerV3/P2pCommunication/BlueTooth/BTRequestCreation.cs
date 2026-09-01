// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Febris.MobileServerV3.P2pCommunication.BlueTooth
{
    public class BTRequestCreation
    {
        //IAssociationRequest _associationRequest = DependencyService.Get<IAssociationRequest>();
        //IBluetoothService _bluetoothRequest = DependencyService.Get<IBluetoothService>();
        IBluetoothRequest _bluetoothRequest = DependencyService.Get<IBluetoothRequest>();

        #region Pairing                
        public async Task CompanionPairing()
        {
            //try{}catch (Exception ex) { Console.WriteLine(ex.Message); }
            //get headset data for pairing
            try
            {
                await _bluetoothRequest.MakePairingRequest();//.MakeRequest();

            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            //try{}
            //catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        #endregion

        #region upload file                
        public async Task UploadFile(string compMac)
        {            
            try
            {
                CompanionSoftwareLogic context = new CompanionSoftwareLogic();
                CompanionAppViewModel appData = context.Get().Result ?? default;

                //string path = LocalHardwareStaticDetails._CompanionFileFullPath;
                //await Share.RequestAsync(new ShareFileRequest
                //{
                //    Title = "Companion Application",
                //    File = new ShareFile(path)
                //});
                string softwareID = appData.LocalSoftwarePackage.UUID.ToString();
                string directoryPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.UncompressedCompanionApplicationPath, softwareID);
                await _bluetoothRequest.SendUpload(compMac, appData.LocalSoftwarePackage.UUID.ToString());// + ".zip");

            }
            catch (Exception ex) 
            {
                PairingPageStatusHelper.GenericMessage(ex.Message);                
            }            
        }
        #endregion
    }
}
