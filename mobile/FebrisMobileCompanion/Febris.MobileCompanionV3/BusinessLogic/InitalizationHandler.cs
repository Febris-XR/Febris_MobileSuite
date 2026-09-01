// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.MVVM.ViewModel;
using Febris.MobileCompanionV3.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileCompanionV3.BusinessLogic
{
    public class InitalizationHandler
    {
        public void Initalize()
        {
            try
            {



                //InitalizationRequest request = new InitalizationRequest();// _log, _config);
                //HardwareInitializationResponse response = request.Initalize().Result;
                //LocalHardwareStaticDetails._hardwareInitializationResponse = response;

                LocalHardwareStaticDetails.StaticMainVM = new MainViewModel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //_log.LogError(ex.Message);
            }
        }
    }
}
