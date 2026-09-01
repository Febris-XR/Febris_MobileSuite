// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.FileSystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.DataLogic
{
    public class ModuleFileContext
    {
        private FileManager _fileManager = new FileManager();


        public async Task<bool> DeleteOldList()
        {
            bool output = false;
            try
            {
                List<string> fileList = await _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);

                foreach (var i in fileList)
                {
                    //output = _fileManager.DeleteFolders(Path.Combine(FileSystem.zipFolderPath, i));
                    output = await _fileManager.DeleteDirectory(Path.Combine(FileSystem.zipFolderPath, i));
                }
                output = true;

                return output;
            }
            catch (Exception ex)
            {
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
                throw;
            }
        }

        public void DeserialiseStatusUpdate(string stringJson)
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

        internal async Task<List<string>> GetNameList()
        {
            List<string> output = new List<string>();
            try
            {
                output = await _fileManager.GetDirectoryContentNames(FileSystem.ZippedModulePath);
                return output;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<byte[]> GetFileContents(Guid moduleId)
        {
            byte[] output = { };
            try
            {
                output = FileManager.OutgoingFileData(Path.Combine(FileSystem.ZippedModulePath, moduleId.ToString() + ".zip"));
                return output;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
