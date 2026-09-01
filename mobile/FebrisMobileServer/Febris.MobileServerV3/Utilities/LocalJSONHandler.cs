// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Utilities
{
    public class LocalJSONHandler
    {
        
        public LocalJSONHandler()
        {

        }
        public void DeserialiseDeviceListJSON(string strJSON)
        {
            try
            {
                //convert string to object list
                var jObj = JsonConvert.DeserializeObject<List<CompanionDevice>>(strJSON);
                //StaticDetails.PairedDeviceList = jObj;
                foreach (var i in jObj)
                {
                    CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
                    {
                        CompanionDevice = i
                    };
                    LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Add(temp);
                }
            }
            catch (Exception ex)
            {
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
            }
        }
        
        public void DeserialiseHardwareStatusUpdate(string stringJson)
        {
            try
            {
                HardwareStatusUpdate jObj = JsonConvert.DeserializeObject<HardwareStatusUpdate>(stringJson);

                if (LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Any())
                {
                    CompanionDeviceViewModel model = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Single();
                    model.BatteryCharge = jObj?.BatteryCharge??default;
                    model.StatementIdList = jObj?.StatementFileList??new List<string>();
                    model.OldStatementIdList = jObj?.OldStatementFileList ?? new List<string>();
                    model.OldVideoIdList = jObj?.OldVideoFileList ?? new List<string>();
                    model.VideoIdList = jObj?.VideoFileList ?? new List<string>();
                    model.AppList = jObj?.AppList ?? new List<string>();
                    model.ModuleFileList = jObj?.ModuleFileList ?? new List<string>();
                    model.ZippedFileList = jObj?.ZippedFileList ?? new List<string>();
                    model.StorageSpaceRemaining = jObj?.StorageSpaceRemaining??default;

                    //To Companions
                    CompanionModuleLogic _companionModuleLogic = new CompanionModuleLogic();
                    var checkCompanionModules = _companionModuleLogic.ScanForNeededModuleUploads(model);
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when deserializing hardware status update: " + ex.Message);
                //this needs to be set up 
                //_log.LogError(ex.Message);
            }
        }

        public T DeserialiseJSONString<T>(string strJSON)
        {
            T output = default(T);
            try
            {
                //convert string to object list
                output = JsonConvert.DeserializeObject<T>(strJSON);
                //StaticDetails._testList = jObj;
            }
            catch (Exception ex)
            {
                //output = default(T);
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
            }
            return output;
        }
    }
}
