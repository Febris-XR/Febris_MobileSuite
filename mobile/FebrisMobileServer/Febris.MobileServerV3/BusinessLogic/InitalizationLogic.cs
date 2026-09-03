// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.APIInteractions;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.BusinessLogic
{
    class InitalizationLogic
    {
        internal static async Task Get()
        {
            try
            {
                InitalizationRequest request = new InitalizationRequest();// _log, _config);
                HardwareInitializationResponse response = await request.Initalize();
                LocalHardwareStaticDetails.HardwareInitializationResponse = response;
                //really only need user,message, and module lists
                LocalHardwareStaticDetails.StaticMainVM.UserVM.UserList = response.UserInitaliztionViewModels.UserViewModelList;
                if (string.IsNullOrEmpty(LocalHardwareStaticDetails.StaticMainVM.UserVM.UserSearch))
                {
                    LocalHardwareStaticDetails.StaticMainVM.UserVM.SearchResultList = response.UserInitaliztionViewModels.UserViewModelList;
                }
                LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleList = response.ModuleList;
                if (string.IsNullOrEmpty(LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleSearch))
                {
                    LocalHardwareStaticDetails.StaticMainVM.ModuleVM.SearchResultList = response.ModuleList;
                }
                LocalHardwareStaticDetails.StaticMainVM.MessageboardVM.LocalMessageBoard = response.MessageboardViewModels.MessageBoardList;

                CompanionSoftwareLogic compsAppLogic = new CompanionSoftwareLogic();
                var something = compsAppLogic.CheckVersion();
                
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Requesting Data From Server: " + ex.Message;
                throw;
            }
            //throw new NotImplementedException();
        }
    }
}
