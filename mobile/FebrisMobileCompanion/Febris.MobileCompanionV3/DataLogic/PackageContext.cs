// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using Febris.SharedMobileLibrary.FileSystem;
//using Febris.SharedMobileLibrary.Models.Data;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using System.Threading.Tasks;

//namespace Febris.MobileCompanionV3.DataLogic
//{
//    /// <summary>
//    /// ********************************Moved to ModulePackageContext due to ambiguity and Android 11 + compliance*********************
//    /// </summary>
//    public class PackageContext
//    {
//        private FileManager _fileManager = new FileManager();
//        private string fileName = "PackageIndex.json";

//        //internal async Task<Dictionary<string, string>> GetIndexList()
//        //{
//        //    Dictionary<string, string> output = new Dictionary<string, string>();
//        //    try
//        //    {
//        //        string stringData = _fileManager.GetFileContent(FileSystem.BasePath, fileName);
//        //        output = JsonConvert.DeserializeObject<Dictionary<string, string>>(stringData);
//        //        return output;
//        //    }
//        //    catch (Exception)
//        //    {

//        //        throw;
//        //    }
//        //}

//        //internal async Task<string> GetIndexList()
//        //{
//        //    string output = string.Empty;
//        //    try
//        //    {
//        //        #region Post Android 11
//        //        output = await App.DbContext.GetList<PackageModel>();

//        //        #endregion
//        //        #region pre-Android 11
//        //        output = _fileManager.GetFileContent(FileSystem.BasePath, fileName);
//        //        #endregion

//        //        return output;
//        //    }
//        //    catch (Exception)
//        //    {

//        //        throw;
//        //    }
//        //} 
        
//        internal async Task<List<ModulePackageModel>> GetIndexList()
//        {
//            List<ModulePackageModel> output = default;
//            try
//            {
//                #region Post Android 11
//                output = await App.DbContext.GetList<ModulePackageModel>();
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                throw;
//            }
//        }

//        //public async Task<Dictionary<string, string>> Post(string keyId, string packageName)
//        //{
//        //    Dictionary<string, string> output = new Dictionary<string, string>();
//        //    try
//        //    {
//        //        Dictionary<string, string> data = await GetIndexList();
//        //        Dictionary<string, string> input = new Dictionary<string, string>(keyId, packageName);
//        //        if (data==null)
//        //        {
//        //            data = new Dictionary<string, string>();
//        //        }
//        //        data.Add(input);
//        //        string stringData = JsonConvert.SerializeObject(data);
//        //        bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, fileName));

//        //        output = await GetIndexList();
//        //        return output;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        //this needs to be set up    
//        //        //_log.LogInformation(ex.Message);
//        //        throw;
//        //    }
//        //}

//        //public async Task<string> Post(string data)
//        //{
//        //    string output = string.Empty;
//        //    try
//        //    {
//        //        #region Post Android 11



//        //        #endregion
//        //        #region pre-Android 11
//        //        bool saved = _fileManager.Set(data, Path.Combine(FileSystem.BasePath, fileName));
//        //        output = data;
//        //        #endregion
//        //        return output;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Console.WriteLine(ex.Message);
//        //        throw;
//        //    }
//        //}
        
//        public async Task<List<ModulePackageModel>> Post(ModulePackageModel data)
//        {
//            List<ModulePackageModel> output = default;
//            try
//            {
//                #region Post Android 11
//                ModulePackageModel item = await App.DbContext.Create<ModulePackageModel>(data);
//                output = await App.DbContext.GetList<ModulePackageModel>();

//                #endregion
//                #region pre-Android 11
//                //bool saved = _fileManager.Set(data, Path.Combine(FileSystem.BasePath, fileName));
//                //output = data;
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//                throw;
//            }
//        }

//        internal async Task<bool> Remove(ModulePackageModel toRemove)
//        {
//            bool output = false;
//            try
//            {

//                output = await App.DbContext.Delete(toRemove);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                throw;
//            }
//            return output;
            
//        }

//        //public async Task<Dictionary<string, string>> Remove(string keyId, string packageName)
//        //{
//        //    Dictionary<string, string> output = new Dictionary<string, string>();
//        //    try
//        //    {
//        //        Dictionary<string, string> data = await GetIndexList();

//        //        if (!string.IsNullOrEmpty(keyId) && !string.IsNullOrEmpty(packageName))
//        //        {
//        //            //var removed = data.Remove(Dictionary<string,string>(keyId, packageName));//.Where(i => i.Key == keyId && i.Value == packageName).Single().Remove();
//        //            var removed = data.Remove(data.First(i => i.Key == keyId && i.Value == packageName));
//        //        }
//        //        else if (!string.IsNullOrEmpty(keyId))
//        //        {
//        //            var removed = data.Remove(data.First(i => i.Key == keyId));
//        //        }
//        //        else if (!string.IsNullOrEmpty(packageName))
//        //        {
//        //            var removed = data.Remove(data.First(i => i.Value == packageName));
//        //        }
//        //        else
//        //        { return output; }
//        //        string stringData = JsonConvert.SerializeObject(data);
//        //        bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, fileName));                
//        //        output = await GetIndexList();
//        //        return output;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        //this needs to be set up    
//        //        //_log.LogInformation(ex.Message);
//        //        throw;
//        //    }
//        //}

//    }
//}
