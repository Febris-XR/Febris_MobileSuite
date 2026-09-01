// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.DataLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Febris.MobileServerV3.P2pCommunication.BlueTooth
{
    public class BTRequestReceiver
    {
        private CompanionDeviceContext companionDeviceContext = new CompanionDeviceContext();
        public IWiFiService wifi = DependencyService.Get<IWiFiService>();
        DataProtection _dataProtection = new DataProtection();


        #region create device
        internal void CompanionCreationEvent(object sender, CompanionDeviceEventArgs e)
        {
            try
            {
                CompanionDevice temp = new CompanionDevice()
                {
                    Name = e.Name,
                    BlueToothAlias = e.BlueToothAlias,
                    BlueToothName = e.BlueToothName,
                    BlueToothMacAddress = e.BlueToothMacAddress,
                    BlueToothType = e.BlueToothType
                };


                if (!LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.ItemList?.Where(i => i.CompanionDevice.BlueToothMacAddress == temp.BlueToothMacAddress).Any() ?? false)
                {
                    List<CompanionDeviceViewModel> data = companionDeviceContext.Post(temp).Result;
                    LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList = data;
                }
                //if (!LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.BlueToothMacAddress == temp.BlueToothMacAddress).Any())
                //{
                //    CompanionDeviceViewModel device = new CompanionDeviceViewModel()
                //    {
                //        CompanionDevice = temp
                //    };
                //    LocalHardwareStaticDetails.PairedDeviceViewModelList.Add(device);
                //    //serialize
                //    SaveCompanionDeviceList();
                //}
            }
            catch { }
        }
        #endregion

        #region 
        #endregion

        #region 
        #endregion

        #region 
        #endregion

        #region 
        #endregion

        #region 
        #endregion



    }
}
