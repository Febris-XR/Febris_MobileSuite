// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models;
using Febris.ModelLibrary.Models.XApiModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Utilites
{
    public class JSONHandler
    {
        public JSONHandler()
        {

        }
        //public void DeserialiseDeviceListJSON(string strJSON)
        //{
        //    try
        //    {
        //        //convert string to object list
        //        var jObj = JsonConvert.DeserializeObject<List<CompanionDevice>>(strJSON);
        //        //StaticDetails.PairedDeviceList = jObj;
        //        foreach (var i in jObj)
        //        {
        //            CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
        //            {
        //                CompanionDevice = i
        //            };
        //            StaticDetails.PairedDeviceViewModelList.Add(temp);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //    }
        //}

        //public void DeserialiseTestJSON(string strJSON)
        //{
        //    try
        //    {
        //        //convert string to object list
        //        var jObj = JsonConvert.DeserializeObject<List<ModuleBase>>(strJSON);
        //        StaticDetails._testList = jObj;
        //    }
        //    catch (Exception ex)
        //    {
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //    }
        //}

        //internal void InitalizeStatement(string response)
        //{
        //    try
        //    {
        //        Statement statement = JsonConvert.DeserializeObject<Statement>(response);
        //        string convertedStatement = JsonConvert.SerializeObject(statement, new JsonSerializerSettings()
        //        {
        //            NullValueHandling = NullValueHandling.Ignore,
        //            DefaultValueHandling = DefaultValueHandling.Ignore
        //        });
        //        StaticDetails.serializedStatement = convertedStatement;
        //    }
        //    catch (Exception ex)
        //    {
        //        //_log.LogError(ex.Message);
        //    }
        //}

        //public void DeserialiseInitalization(string stringJson)
        //{
        //    try
        //    {
        //        var jObj = JsonConvert.DeserializeObject<LauncherViewModel>(stringJson);
        //        StaticDetails._professionalList = jObj.ProfessionalList;
        //        StaticDetails._providerMessageBoard = jObj.InstitutionMessageBoard;
        //        StaticDetails._locationMessageBoard = jObj.LocationMessageBoard;
        //        StaticDetails._febrisMessageBoard = jObj.FebrisMessageBoard;
        //        StaticDetails._testList = jObj.ModuleBaseList;

        //        StaticDetails.ProfessionalViewModelList = new List<ProfessionalViewModel>();
        //        foreach (var i in jObj.ProfessionalList)
        //        {
        //            ProfessionalViewModel temp = new ProfessionalViewModel()
        //            {
        //                Professional = i
        //            };
        //            StaticDetails.ProfessionalViewModelList.Add(temp);
        //        }
        //        StaticDetails.ModuleBaseViewModelList = new List<ModuleBaseViewModel>();
        //        foreach (var i in jObj.ModuleBaseList)
        //        {
        //            ModuleBaseViewModel temp = new ModuleBaseViewModel()
        //            {
        //                ModuleBase = i
        //            };
        //            StaticDetails.ModuleBaseViewModelList.Add(temp);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("issue deserializing inital data into :" + ex.Message);
        //    }
        //}

        //public void DeserialiseHardwareStatusUpdate(string stringJson)
        //{
        //    try
        //    {
        //        HardwareStatusUpdate jObj = JsonConvert.DeserializeObject<HardwareStatusUpdate>(stringJson);
        //        if (LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Any())
        //        {
        //            CompanionDeviceViewModel model = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Single();
        //            model.BatteryCharge = jObj.BatteryCharge;
        //            model.ModuleBaseIdList = jObj.ModuleFileList;
        //            model.StatementIdList = jObj.StatementFileList;
        //            model.OldStatementIdList = jObj.OldStatementFileList;
        //            model.OldVideoIdList = jObj.OldVideoFileList;
        //            model.VideoIdList = jObj.VideoFileList;
        //            model.StorageSpaceRemaining = jObj.StorageSpaceRemaining;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error when deserializing hardware status update: " + ex.Message);
        //        //this needs to be set up 
        //        //_log.LogError(ex.Message);
        //    }
        //}

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
