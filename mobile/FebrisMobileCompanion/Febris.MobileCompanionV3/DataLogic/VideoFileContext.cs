// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileCompanionV3.DataLogic
{
    /// <summary>
    /// Add Database updates in here. That way when ever changes are made, the database will update.
    /// </summary>
    public class VideoFileContext
    {
        private FileManager _fileManager = new FileManager();
        ISharedFileSystem _externalPlatformFileSystem = DependencyService.Get<ISharedFileSystem>();

        //public async Task<GroupOwnerDevice> GetSaved()
        //{
        //    GroupOwnerDevice output = new GroupOwnerDevice();
        //    try
        //    {
        //        //FileManager _fileManager = new FileManager();
        //        string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "GroupOwnerDevice.json");
        //        output = JsonConvert.DeserializeObject<GroupOwnerDevice>(stringData);
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        return output;
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //        throw;
        //    }
        //}

        //public async Task<List<CompanionDeviceViewModel>> GetList()
        //{
        //    List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
        //    try
        //    {
        //        List<CompanionDevice> data = await GetSavedList();

        //        foreach (var i in data)
        //        {
        //            CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
        //            {
        //                CompanionDevice = i
        //            };
        //            output.Add(temp);
        //        }
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        return output;
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //        throw;
        //    }
        //}

        //public async Task<GroupOwnerDevice> Post(GroupOwnerDevice input)
        //{
        //    GroupOwnerDevice output = new GroupOwnerDevice();
        //    try
        //    {
        //        GroupOwnerDevice data = await GetSaved();
        //        data = input;
        //        string stringData = JsonConvert.SerializeObject(data);
        //        bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "GroupOwnerDevice.json"));

        //        output = await GetSaved();
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //        throw;
        //    }
        //}

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

        public async Task<bool> DeleteOldList()
        {
            bool output = false;
            try
            {
                #region Post Android 11
                List<MediaModel> fileList = await App.DbContext.GetList<MediaModel>();

                foreach (var i in fileList)
                {
                    output = await _fileManager.DeleteDirectory(i.Path);
                    bool deleted = await App.DbContext.Delete(i);
                }

                output = true;
                #endregion
                #region pre-Android 11
                //List<string> fileList = _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);
                //foreach (var i in fileList)
                //{
                //    output = _fileManager.DeleteFolders(Path.Combine(FileSystem.zipFolderPath, i));
                //}
                //output = true;


                #endregion

                return output;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.StackTrace);
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

        internal async Task<List<string>> GetSentNameList()
        {
            List<string> output = new List<string>();
            try
            {
                #region Post Android 11
                List<MediaModel> fileList = await App.DbContext.GetList<MediaModel>();
                output = fileList.Where(i => i.Uploaded==true&&i.MediaType==MediaType.Video).Select(i=>i.Name).ToList();                
                #endregion               
                #region pre-Android 11
                //output = _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);
                #endregion
                return output;
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.StackTrace);
                throw;

            }
            
        }

        internal async Task<List<string>> GetUnsentNameList()
        {
            List<string> output = new List<string>();
            try
            {
                #region Post Android 11
                List<MediaModel> fileList = await App.DbContext.GetList<MediaModel>();
                output = fileList.Where(i => i.Uploaded == false && i.MediaType == MediaType.Video).Select(i => i.Name).ToList();
                #endregion
                #region pre-Android 11
                //output = _fileManager.GetDirectoryContentNames(FileSystem.RecordingsFilePath);
                #endregion

                return output;
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.StackTrace);
                throw;

            }
        }
    }
}
