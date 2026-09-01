// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using Febris.MobileCompanionV3.DataLogic;
//using Febris.MobileCompanionV3.Utilities;
//using Febris.SharedMobileLibrary.Interfaces;
//using Febris.SharedMobileLibrary.Models.Data;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Xamarin.Forms;


///// <summary>
///// ***************************************Moved to ModulePackageLogic ******************************
///// Moved due to Andrroid 11+ compliance and ambiguity
///// </summary>

//namespace Febris.MobileCompanionV3.BusinessLogic
//{
//    public class PackageLogic
//    {
//        PackageContext _packageContext;
//        IModulePackageUtility _packageUtility;

//        public PackageLogic()
//        {
//            _packageUtility = DependencyService.Get<IModulePackageUtility>();
//            _packageContext = new PackageContext();
//        }

//        #region retired post Android 11
//        //public async Task<Dictionary<string, string>> Update(string keyId, string packageName)
//        //{
//        //    Dictionary<string, string> output = new Dictionary<string, string>();
//        //    try
//        //    {
//        //        #region Post Android 11


//        //        #endregion
//        //        #region pre-Android 11


//        //        Dictionary<string, string> data = await GetIndexList();
//        //        //Dictionary<string, string> input = new Dictionary<string, string>();
//        //        //input.Add(keyId, packageName);
//        //        if (data == null)
//        //        {
//        //            data = new Dictionary<string, string>();
//        //            data.Add(keyId, packageName);
//        //        }
//        //        else if (data.Where(i => i.Key == keyId).Any())
//        //        {
//        //            data[keyId] = packageName;
//        //        }
//        //        else
//        //        {
//        //            data.Add(keyId, packageName);
//        //        }

//        //        string stringData = JsonConvert.SerializeObject(data);
//        //        stringData = await _packageContext.Post(stringData);
//        //        //bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, fileName));
//        //        output = await GetIndexList();
//        //        #endregion
//        //        return output;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        //this needs to be set up    
//        //        //_log.LogInformation(ex.Message);
//        //        throw;
//        //    }
//        //}
//        #endregion

//        public async Task<string> Get(string ModuleId)
//        {
//            try
//            {
//                string output = string.Empty;
//                #region Post Android 11
//                List<ModulePackageModel> modulePackageModelList = await _packageContext.GetIndexList();
//                output = modulePackageModelList
//                    .Where(i => i.UUID.ToString() == ModuleId)
//                    .Select(i => i.UriString)
//                    .First();


//                #endregion
//                #region pre-Android 11
                
//                //Dictionary<string, string> data = await GetIndexList();
//                //output = data[ModuleId];


//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                StatusUpdateHelper.Error("There was a error launching selected module.");
//                throw;
//            }
//        }

//        //internal async Task<Dictionary<string, string>> GetIndexList()
//        internal async Task<List<ModulePackageModel>> GetIndexList()
//        {
//            try
//            {
//                #region Post Android 11
//                //List<ModulePackageModel> itemList = await App.DbContext.GetList<ModulePackageModel>();
//                List<ModulePackageModel> output = await _packageContext.GetIndexList();

//                #endregion
//                #region pre-Android 11
//                //string data = await _packageContext.GetIndexList();
//               // Dictionary<string, string> output = JsonConvert.DeserializeObject<Dictionary<string, string>>(data) ?? default;
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//                throw;
//            }
//        }

//        public async Task<List<ModulePackageModel>> Remove(string keyId, string packageName)
//        {
//            List<ModulePackageModel> output = default;
//            ModulePackageModel toRemove = default;
//            try
//            {
//                List<ModulePackageModel> data = await GetIndexList();

//                if (data.Count <=0)
//                {
//                    return default;
//                }

                
//                if (!string.IsNullOrEmpty(keyId) && !string.IsNullOrEmpty(packageName))
//                {
//                    toRemove = data.Where(i => i.UUID.ToString() == keyId && i.UriString == packageName).First();
                    
//                }
//                else if (!string.IsNullOrEmpty(keyId))
//                {
//                    toRemove = data.Where(i => i.UUID.ToString() == keyId).First();
                    
//                }
//                else if (!string.IsNullOrEmpty(packageName))
//                {
//                    toRemove = data.Where(i => i.UriString == packageName).First();                    
//                }
//                else
//                { return output; }

//                if(toRemove != default)
//                {
//                    bool removed = await _packageContext.Remove(toRemove);
//                }
                
                
//                output = await GetIndexList();
//                return output;
//            }
//            catch (Exception ex)
//            {
//                //this needs to be set up    
//                //_log.LogInformation(ex.Message);
//                throw;
//            }
//        }
//        #region retired after android 11
//        //public async Task<Dictionary<string, string>> Remove(string keyId, string packageName)
//        //{
//        //    Dictionary<string, string> output = new Dictionary<string, string>();
//        //    try
//        //    {
//        //        Dictionary<string, string> data = await GetIndexList();

//        //        if (!string.IsNullOrEmpty(keyId) && !string.IsNullOrEmpty(packageName))
//        //        {
//        //            var toRemove = data.Where(i => i.Key == keyId && i.Value == packageName).First();
//        //            //var removed = data.Remove(Dictionary<string,string>(keyId, packageName));//.Where(i => i.Key == keyId && i.Value == packageName).Single().Remove();
//        //            var removed = data.Remove(toRemove.Key);
//        //        }
//        //        else if (!string.IsNullOrEmpty(keyId))
//        //        {
//        //            var toRemove = data.Where(i => i.Key == keyId).First();
//        //            var removed = data.Remove(toRemove.Key);
//        //        }
//        //        else if (!string.IsNullOrEmpty(packageName))
//        //        {
//        //            var toRemove = data.Where(i => i.Value == packageName).First();
//        //            var removed = data.Remove(toRemove.Key);
//        //        }
//        //        else
//        //        { return output; }

//        //        string stringData = JsonConvert.SerializeObject(data);
//        //        string outputData = await _packageContext.Post(stringData);
//        //        //bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, fileName));
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
//        #endregion
//        internal async Task<ModulePackageModel> Update(ModulePackageModel input)
//        {
//            ModulePackageModel output = default;
//            try
//            {
//                #region Post Android 11
//                output = await App.DbContext.Update(input);

//                #endregion
//                #region pre-Android 11


//                //Dictionary<string, string> data = await GetIndexList();
//                ////Dictionary<string, string> input = new Dictionary<string, string>();
//                ////input.Add(keyId, packageName);
//                //if (data == null)
//                //{
//                //    data = new Dictionary<string, string>();
//                //    data.Add(keyId, packageName);
//                //}
//                //else if (data.Where(i => i.Key == keyId).Any())
//                //{
//                //    data[keyId] = packageName;
//                //}
//                //else
//                //{
//                //    data.Add(keyId, packageName);
//                //}

//                //string stringData = JsonConvert.SerializeObject(data);
//                //stringData = await _packageContext.Post(stringData);
//                ////bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, fileName));
//                //output = await GetIndexList();
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                //this needs to be set up    
//                //_log.LogInformation(ex.Message);
//                throw;
//            }
//        }
//    }
//}
