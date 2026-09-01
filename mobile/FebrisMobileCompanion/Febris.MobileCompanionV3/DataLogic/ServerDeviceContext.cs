// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Add Database updates in here. That way when ever changes are made, the database will update.
/// </summary>
namespace Febris.MobileCompanionV3.DataLogic
{
    public class ServerDeviceContext
    {
        private FileManager _fileManager = new FileManager();
        private LocalDatabase _dbContext = App.DbContext;

        public async Task<GroupOwnerDevice> GetSaved()
        {
            GroupOwnerDevice output = new GroupOwnerDevice();
            try
            {
                #region pre Android 11
                //FileManager _fileManager = new FileManager();
                //string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "GroupOwnerDevice.json");
                //output = JsonConvert.DeserializeObject<GroupOwnerDevice>(stringData);
                #endregion
                #region Post Android 11
                output = await _dbContext.GetLastItem<GroupOwnerDevice>("Id");
                //var preoutput = await _dbContext.GetList<GroupOwnerDevice>();
                //output = preoutput.Last();
                

                //List< GroupOwnerDevice > list = await App.DbContext.GetList<GroupOwnerDevice>();
                //output = list.Last();
                #endregion

                return output ??default;
            }
            catch (Exception ex)
            {
                return output;
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
                throw;
            }
        }

        public async Task<GroupOwnerDevice> Post(GroupOwnerDevice input)
        {
            GroupOwnerDevice output = new GroupOwnerDevice();
            try
            {
                #region pre Android 11
                //GroupOwnerDevice data = await GetSaved();
                //data = input;
                //string stringData = JsonConvert.SerializeObject(data);
                //bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "GroupOwnerDevice.json"));
                //output = await GetSaved();
                #endregion
                #region Post Android 11

                GroupOwnerDevice data = await GetSaved();
                if (data != default)
                {
                    data = input;
                    output = await _dbContext.Update(data);
                }
                else
                {
                    output = await _dbContext.Create(input);
                }
                              
                //output = await GetSaved();
                #endregion


                return output;
            }
            catch (Exception ex)
            {
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
                throw;
            }
        }

        //public async Task<List<CompanionDeviceViewModel>> Update(CompanionDevice input)
        //{
        //    List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
        //    try
        //    {
        //        List<CompanionDevice> data = await GetSavedList();
        //        //FileManager _fileManager = new FileManager();
        //        //string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json");
        //        //List<CompanionDevice> data = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
        //        List<CompanionDevice> item = data.Where(i => i.UniqueIdentifier == input.UniqueIdentifier).ToList();
        //        //item = input;
        //        foreach (var i in item)
        //        {
        //            data.Remove(i);
        //        }
        //        //data.Remove(item);
        //        data.Add(input);
        //        string stringData = JsonConvert.SerializeObject(data);
        //        bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));

        //        //foreach (var i in data)
        //        //{
        //        //    CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
        //        //    {
        //        //        CompanionDevice = i
        //        //    };
        //        //    output.Add(temp);
        //        //}

        //        output = await GetList();
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //        throw;
        //    }
        //}

        public async Task<bool> Delete()
        {
            bool output = false;
            try
            {
                #region pre android 11
                //output = FileManager.DeleteFile(Path.Combine(FileSystem.BasePath, "GroupOwnerDevice.json"));
                //LocalHardwareStaticDetails.StaticMainVM.ConfigVM = new MVVM.ViewModel.ConfigurationViewModel();
                #endregion
                #region post android 11
                var item = await App.DbContext.GetLastItem<GroupOwnerDevice>("Id");
                output = await App.DbContext.Delete<GroupOwnerDevice>(item);
                #endregion

                return output;
            }
            catch (Exception ex)
            {
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
                throw;
            }
        }

        public void DeserialiseHardwareStatusUpdate(string stringJson)
        {
            try
            {
                //HardwareStatusUpdate jObj = JsonConvert.DeserializeObject<HardwareStatusUpdate>(stringJson);
                //if (LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Any())
                //{
                //    CompanionDeviceViewModel model = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Single();
                //    model.BatteryCharge = jObj.BatteryCharge;
                //    model.ModuleBaseIdList = jObj.ModuleFileList;
                //    model.StatementIdList = jObj.StatementFileList;
                //    model.OldStatementIdList = jObj.OldStatementFileList;
                //    model.OldVideoIdList = jObj.OldVideoFileList;
                //    model.VideoIdList = jObj.VideoFileList;
                //    model.StorageSpaceRemaining = jObj.StorageSpaceRemaining;
                //}
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
                output = JsonConvert.DeserializeObject<T>(strJSON);
            }
            catch (Exception ex)
            {
                output = default(T);
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
            }
            return output;
        }
    }
}
