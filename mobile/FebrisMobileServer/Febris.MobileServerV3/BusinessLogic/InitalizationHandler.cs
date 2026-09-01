// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.APIInteractions;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Models.ViewModels;
using Febris.SharedMobileLibrary.Utilites;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.BusinessLogic
{
    public class InitalizationHandler
    {
        //private readonly IConfiguration _config;        
        //private ILogger _log;

        //public InitalizationHandler(ILogger log, IConfiguration config)
        //{
        //    _log = log;
        //    _config = config;
        //}
        //private readonly JSONHandler _jSONHandler;
        //public InitalizationHandler()
        //{
        //    _jSONHandler = new JSONHandler();
        //}
        public async Task Initalize()
        {
            try
            {
                LocalHardwareStaticDetails.StaticMainVM = new MainViewModel();
                //await APICheckingLoop();
                URLSettingUtility.SetURL();
                Task.Run(() => CheckTimer.StartApiCheckingLoop());
                Task.Run(() => CheckTimer.StartCheckingLoop());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //_log.LogError(ex.Message);
            }
        }
               

        //private static async Task APICheckingLoop()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            InitalizationRequest request = new InitalizationRequest();// _log, _config);
        //            HardwareInitializationResponse response = await request.Initalize();
        //            LocalHardwareStaticDetails._hardwareInitializationResponse = response;
        //        }
        //        catch (Exception ex)
        //        {
        //            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Requesting Data From Server: " + ex.Message;
        //        }
        //        Task.Wait(LocalHardwareStaticDetails.APIDataRequestFrequency);
        //    }
        //}
    }
}
