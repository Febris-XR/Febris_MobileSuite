// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.ModelLibrary.Models.XApiModels;
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
    public class StatementFileContext
    {
        private FileManager _fileManager = new FileManager();
        ISharedFileSystem _externalPlatformFileSystem = DependencyService.Get<ISharedFileSystem>();
        private LocalDatabase _dbContext = App.DbContext;
        public async Task<bool> DeleteOldList()
        {
            bool output = false;
            try
            {
                #region pre-Android 11
                //List<string> fileList = _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);
                //foreach (var i in fileList)
                //{
                //    output = _fileManager.DeleteFolders(Path.Combine(FileSystem.zipFolderPath, i));
                //}
                //output = true;
                #endregion
                #region post-Android 11

                List<RawStatement> fileList = await App.DbContext.GetList<RawStatement>();
                fileList = fileList.Where(i => i.Uploaded == true).ToList();
                foreach (var i in fileList)
                {
                    output = await App.DbContext.Delete<RawStatement>(i);
                }
                output = true;

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

        internal async Task<List<RawStatement>> GetStatementList()
        {
            List<RawStatement> output = default;
            try
            {
                output = await _dbContext.GetList<RawStatement>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<List<string>> GetSentNameList()
        {
            List<string> output = new List<string>();
            try
            {
                #region pre-Android 11
                //output = _fileManager.GetDirectoryContentNames(FileSystem.OldStatementPath);
                #endregion
                #region Post Android 11                
                List<RawStatement> fileList = await App.DbContext.GetList<RawStatement>();
                fileList = fileList.Where(i => i.Uploaded == true).ToList();
                output = fileList.Select(i => i.Id.ToString()).ToList();
                #endregion

                return output;
            }
            catch (Exception)
            {
                return default;
                //throw;
            }
        }

        internal async Task<List<string>> GetUnsentNameList()
        {
            List<string> output = new List<string>();
            try
            {
                #region pre-Android 11
                //output = _fileManager.GetDirectoryContentNames(FileSystem.StatementPath);
                #endregion
                #region Post Android 11
                List<RawStatement> fileList = await _dbContext.GetList<RawStatement>();
                fileList = fileList.Where(i => i.Uploaded == false).ToList();
                output = fileList.Select(i=>i.Id.ToString()).ToList();
                #endregion


                return output;
            }
            catch (Exception)
            {
                return default;
                //throw;
            }
        }
    }
}
