// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models.Data;
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
    /// Added Database and reusing this name because it corresponds with the ModuleFileModel which is a nested model inside the ModulePackage. 
    /// I created this because the modulePackge context was getting too messy trying to also manage all of the Files associated with the overall package
    /// </summary>
    public class ModuleFileContext
    {
        private FileManager _fileManager = new FileManager();
        private LocalDatabase _context = App.DbContext;


        ///Crud operation
        #region Get

        internal async Task<List<ModuleFileModel>> Get()
        {            
            List<ModuleFileModel> output = default;
            try
            {
                output = await _context.GetList<ModuleFileModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }


        internal async Task<ModuleFileModel> Get(long input)
        {
            if(input == 0) { return default; }
            ModuleFileModel output = default;
            try
            {
                output = await _context.Get<ModuleFileModel>(i=>i.Id==input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }
        internal async Task<List<ModuleFileModel>> Get(string input)
        {
            if (input == default) { return default; }
            List<ModuleFileModel> output = default;
            try
            {
                output = await _context.GetList<ModuleFileModel>();
                output = output.Where(i => i.ModuleDirectoryName == input).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<List<ModuleFileModel>> GetAllCompressed()
        {
            List<ModuleFileModel> output = default;
            try
            {
                output = await _context.GetList<ModuleFileModel>();
                output = output.Where(i => i.Compressed == true).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<List<ModuleFileModel>> GetAllUncompressed()
        {
            List<ModuleFileModel> output = default;
            try
            {
                output = await _context.GetList<ModuleFileModel>();
                output = output.Where(i => i.Compressed == false).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return default;
                //throw;
            }
            return output;
        }

        #endregion
        #region Post
        internal async Task<ModuleFileModel> Post(ModuleFileModel input)
        {
            ModuleFileModel output = default;
            try
            {
                output = await _context.Create<ModuleFileModel>(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return default;
                //throw;
            }
            return output;
        }
        #endregion
        #region Update
        internal async Task<ModuleFileModel> Update(ModuleFileModel input)
        {
            ModuleFileModel output = default;
            try
            {                
                 output = await _context.Update<ModuleFileModel>(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }
        #endregion
        #region Delete

        internal async Task<bool> Delete(ModuleFileModel input)
        {
            bool output = default;
            try
            {
                try
                {
                    output = await DeleteFromFileSystem(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //throw;
                }
                try
                {
                    output = await DeleteFromDb(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //throw;
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return default;
                //throw;
            }
            return output;
        }

        #region internal
        private async Task<bool> DeleteFromDb(ModuleFileModel input)
        {
            bool output = default;
            try
            {                
                output = await _context.Delete(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
            return output;
        }
        private async Task<bool> DeleteFromFileSystem(ModuleFileModel input)
        {
            bool output = default;
            try
            {
                output = await _fileManager.DeleteDirectory(input.FilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
            return output;
        }

        //internal Task<bool> Delete(ModulePackageModel model)
        //{
        //    throw new NotImplementedException();
        //}
        #endregion
        #endregion

    }
}



//using Febris.MobileCompanionV3.Utilities;
//using Febris.SharedMobileLibrary.FileSystem;
//using Febris.SharedMobileLibrary.Interfaces;
//using Febris.SharedMobileLibrary.Models.Data;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Xamarin.Forms;

///// <summary>
///// **********************************************Moved to ModulePackageContext **********************
///// Moved due to ambiguity and compliance with android 11+
///// </summary>


//namespace Febris.MobileCompanionV3.DataLogic
//{
//    /// <summary>
//    /// Add Database updates in here. That way when ever changes are made, the database will update.
//    /// </summary>
//    public class ModuleFileContext
//    {
//        private FileManager _fileManager = new FileManager();
//        //IExternalPlatformFileSystem _externalPlatformFileSystem = DependencyService.Get<IExternalPlatformFileSystem>();

//        public async Task<bool> DeleteOldList()
//        {
//            bool output = false;
//            try
//            {
//                #region Post Android 11
//                List<ModuleFileModel> itemList = await App.DbContext.GetList<ModuleFileModel>();
//                itemList = itemList
//                    .Where(i => i.Compressed == true)                    
//                    .ToList();

//                foreach (var i in itemList)
//                {
//                    output = _fileManager.DeleteFolders(i.FilePath);
//                    bool deleted = await App.DbContext.Delete(i);
//                }


//                #endregion
//                #region pre-Android 11
//                //List<string> fileList = _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);
//                ////List<string> fileList = await _externalPlatformFileSystem.GetDirectoryContentNames(FileSystem.zipFolderPath);
//                //foreach (var i in fileList)
//                //{
//                //    output = _fileManager.DeleteFolders(Path.Combine(FileSystem.zipFolderPath, i));
//                //}

//                #endregion

//output = true;
//                return output;
//            }
//            catch (Exception ex)
//            {
//                //this needs to be set up    
//                //_log.LogInformation(ex.Message);
//                throw;
//            }
//        }

//        public void DeserialiseHardwareStatusUpdate(string stringJson)
//        {
//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11

//                #endregion
//                //HardwareStatusUpdate jObj = JsonConvert.DeserializeObject<HardwareStatusUpdate>(stringJson);
//                //if (LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Any())
//                //{
//                //    CompanionDeviceViewModel model = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Single();
//                //    model.BatteryCharge = jObj.BatteryCharge;
//                //    model.ModuleBaseIdList = jObj.ModuleFileList;
//                //    model.StatementIdList = jObj.StatementFileList;
//                //    model.OldStatementIdList = jObj.OldStatementFileList;
//                //    model.OldVideoIdList = jObj.OldVideoFileList;
//                //    model.VideoIdList = jObj.VideoFileList;
//                //    model.StorageSpaceRemaining = jObj.StorageSpaceRemaining;
//                //}
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error when deserializing hardware status update: " + ex.Message);
//                //this needs to be set up 
//                //_log.LogError(ex.Message);
//            }
//        }

//        public T DeserialiseJSONString<T>(string strJSON)
//        {
//            T output = default(T);
//            try
//            {
//                output = JsonConvert.DeserializeObject<T>(strJSON);
//            }
//            catch (Exception ex)
//            {
//                output = default(T);
//                //this needs to be set up    
//                //_log.LogInformation(ex.Message);
//            }
//            return output;
//        }

//        internal async Task<List<string>> GetAppList()
//        {
//            List<string> output = new List<string>();
//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11

//                #endregion

//                ModuleUtility _moduleUtility = new ModuleUtility();
//                output = await _moduleUtility.GetList();


//                //output = await _ApplicationInformation.GetApplicationList();

//                //output = _fileManager.GetDirectoryContentNames(FileSystem.ModulePath);
//                //foreach(var i in preoutput)
//                //{
//                //    output.Add(Guid.Parse(i));
//                //}

//                return output;
//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }

//        internal async Task<List<string>> GetNameList()
//        {
//            List<string> output = new List<string>();
//            try
//            {
//                #region Post Android 11
//                List<ModuleFileModel> itemList = await App.DbContext.GetList<ModuleFileModel>();
//                output = itemList
//                    .Where(i => i.Compressed == false)
//                    .Select(i => i.Module.Name)
//                    .ToList();

//                #endregion
//                #region pre-Android 11
//                //output = _fileManager.GetDirectoryContentNames(FileSystem.ModulePath);
//                #endregion


//                //output = await _externalPlatformFileSystem.GetDirectoryContentNames(FileSystem.ModulePath);
//                //foreach(var i in preoutput)
//                //{
//                //    output.Add(Guid.Parse(i));
//                //}

//                return output;
//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }

//        internal async Task<List<string>> GetZippedNameList()
//        {
//            List<string> output = new List<string>();
//            try
//            {
//                #region Post Android 11
//                List<ModuleFileModel> itemList = await App.DbContext.GetList<ModuleFileModel>();
//                output = itemList
//                    .Where(i => i.Compressed == true)
//                    .Select(i => i.Module.Name)
//                    .ToList();
//                #endregion
//                #region pre-Android 11
//                //output = _fileManager.GetDirectoryContentNames(FileSystem.ZippedModulePath);
//                #endregion

//                return output;
//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }
//    }
//}
