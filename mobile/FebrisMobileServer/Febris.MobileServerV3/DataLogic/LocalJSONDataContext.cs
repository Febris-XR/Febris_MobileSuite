// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.DataLogic
{
    //public class LocalJSONDataContext
    //{

        //public void CompanionDeviceList(string strJSON)
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
        //            LocalHardwareStaticDetails.PairedDeviceViewModelList.Add(temp);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
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

        //public T DeserialiseJSONString<T>(string strJSON)
        //{
        //    T output = default(T);
        //    try
        //    {
        //        //convert string to object list
        //        output = JsonConvert.DeserializeObject<T>(strJSON);
        //        //StaticDetails._testList = jObj;
        //    }
        //    catch (Exception ex)
        //    {
        //        //output = default(T);
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //    }
        //    return output;
        //}
    //}
    //public class CompanionDeviceContext
    //{
    //    private FileManager _fileManager = new FileManager();

    //    public async Task<List<CompanionDevice>> GetSavedList()
    //    {
    //        List<CompanionDevice> output = new List<CompanionDevice>();
    //        try
    //        {
    //            //FileManager _fileManager = new FileManager();
    //            string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json");
    //            output = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
    //            return output;
    //        }
    //        catch (Exception ex)
    //        {
    //            return output;
    //            //this needs to be set up    
    //            //_log.LogInformation(ex.Message);
    //            throw;
    //        }
    //    }

    //    public async Task<List<CompanionDeviceViewModel>> GetList()
    //    {
    //        List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
    //        try
    //        {
    //            List<CompanionDevice> data = await GetSavedList();
                
    //            foreach (var i in data)
    //            {
    //                CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
    //                {
    //                    CompanionDevice = i
    //                };
    //                output.Add(temp);
    //            }
    //            return output;
    //        }
    //        catch (Exception ex)
    //        {
    //            return output;
    //            //this needs to be set up    
    //            //_log.LogInformation(ex.Message);
    //            throw;
    //        }
    //    }

    //    public async Task<List<CompanionDeviceViewModel>> Post(CompanionDevice input)
    //    {
    //        List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
    //        try
    //        {
    //            List<CompanionDevice> data = await GetSavedList();
    //            if (data == null)
    //            {
    //                data = new List<CompanionDevice>();
    //            }
    //            if (!data.Where(i => i.BlueToothMacAddress == input.BlueToothMacAddress).Any())
    //            {
    //                data.Add(input);
    //                string stringData = JsonConvert.SerializeObject(data);
    //                bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));
    //            }
                
                
                
    //            //FileManager _fileManager = new FileManager();
    //            //string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json")??string.Empty;
    //            //List<CompanionDevice> data = new List<CompanionDevice>();
    //            //if (!string.IsNullOrEmpty(stringData))
    //            //{
    //            //    data = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
    //            //}
    //            //List<CompanionDevice> data = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);


    //            //foreach (var i in data)
    //            //{
    //            //    CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
    //            //    {
    //            //        CompanionDevice = i
    //            //    };
    //            //    output.Add(temp);
    //            //}

    //            output = await GetList();
    //            return output;
    //        }
    //        catch (Exception ex)
    //        {
    //            //this needs to be set up    
    //            //_log.LogInformation(ex.Message);
    //            throw;
    //        }
    //    }

    //    public async Task<List<CompanionDeviceViewModel>> Update(CompanionDevice input)
    //    {
    //        List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
    //        try
    //        {
    //            List<CompanionDevice> data = await GetSavedList();
    //            //FileManager _fileManager = new FileManager();
    //            //string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json");
    //            //List<CompanionDevice> data = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
    //            List<CompanionDevice> item = data.Where(i => i.BlueToothMacAddress == input.BlueToothMacAddress).ToList();
    //            //item = input;
    //            foreach(var i in item)
    //            {
    //                data.Remove(i);
    //            }
    //            //data.Remove(item);
    //            data.Add(input);
    //            string stringData = JsonConvert.SerializeObject(data);
    //            bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));

    //            //foreach (var i in data)
    //            //{
    //            //    CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
    //            //    {
    //            //        CompanionDevice = i
    //            //    };
    //            //    output.Add(temp);
    //            //}

    //            output = await GetList();
    //            return output;
    //        }
    //        catch (Exception ex)
    //        {
    //            //this needs to be set up    
    //            //_log.LogInformation(ex.Message);
    //            throw;
    //        }
    //    }

    //    public async Task<List<CompanionDeviceViewModel>> Delete(CompanionDevice input)
    //    {
    //        List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
    //        try
    //        {

    //            //FileManager _fileManager = new FileManager();
    //            //string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json");
    //            //List<CompanionDevice> data = await GetSavedList();//JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
    //            //data.Remove(input);
    //            //string stringData = JsonConvert.SerializeObject(data);
    //            //bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));
                
    //            List<CompanionDevice> data = await GetSavedList();
    //            List<CompanionDevice> item = data.Where(i => i.UniqueIdentifier == input.UniqueIdentifier).ToList();
    //            //item = input;
    //            foreach (var i in item)
    //            {
    //                data.Remove(i);
    //            }
    //            string stringData = JsonConvert.SerializeObject(data);
    //            bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));

    //            output = await GetList();
    //            return output;
    //        }
    //        catch (Exception ex)
    //        {
    //            //this needs to be set up    
    //            //_log.LogInformation(ex.Message);
    //            throw;
    //        }
    //    }

    //    public void DeserialiseHardwareStatusUpdate(string stringJson)
    //    {
    //        try
    //        {
    //            //HardwareStatusUpdate jObj = JsonConvert.DeserializeObject<HardwareStatusUpdate>(stringJson);
    //            //if (LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Any())
    //            //{
    //            //    CompanionDeviceViewModel model = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Single();
    //            //    model.BatteryCharge = jObj.BatteryCharge;
    //            //    model.ModuleBaseIdList = jObj.ModuleFileList;
    //            //    model.StatementIdList = jObj.StatementFileList;
    //            //    model.OldStatementIdList = jObj.OldStatementFileList;
    //            //    model.OldVideoIdList = jObj.OldVideoFileList;
    //            //    model.VideoIdList = jObj.VideoFileList;
    //            //    model.StorageSpaceRemaining = jObj.StorageSpaceRemaining;
    //            //}
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error when deserializing hardware status update: " + ex.Message);
    //            //this needs to be set up 
    //            //_log.LogError(ex.Message);
    //        }
    //    }

    //    public T DeserialiseJSONString<T>(string strJSON)
    //    {
    //        T output = default(T);
    //        try
    //        {                
    //            output = JsonConvert.DeserializeObject<T>(strJSON);             
    //        }
    //        catch (Exception ex)
    //        {
    //            output = default(T);
    //            //this needs to be set up    
    //            //_log.LogInformation(ex.Message);
    //        }
    //        return output;
    //    }
    //}

    //move domain saving here

    //move credential saving here
    //public class ModuleFileContext
    //{
    //    private FileManager _fileManager = new FileManager();


    //    public async Task<bool> DeleteOldList()
    //    {
    //        bool output = false;
    //        try
    //        {
    //            List<string> fileList = _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);

    //            foreach (var i in fileList)
    //            {
    //                output = _fileManager.DeleteFolders(Path.Combine(FileSystem.zipFolderPath, i));
    //            }
    //            output = true;

    //            return output;
    //        }
    //        catch (Exception ex)
    //        {
    //            //this needs to be set up    
    //            //_log.LogInformation(ex.Message);
    //            throw;
    //        }
    //    }

    //    public void DeserialiseStatusUpdate(string stringJson)
    //    {
    //        try
    //        {
    //            //HardwareStatusUpdate jObj = JsonConvert.DeserializeObject<HardwareStatusUpdate>(stringJson);
    //            //if (LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Any())
    //            //{
    //            //    CompanionDeviceViewModel model = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.UniqueIdentifier == jObj.ClientUniqueId).Single();
    //            //    model.BatteryCharge = jObj.BatteryCharge;
    //            //    model.ModuleBaseIdList = jObj.ModuleFileList;
    //            //    model.StatementIdList = jObj.StatementFileList;
    //            //    model.OldStatementIdList = jObj.OldStatementFileList;
    //            //    model.OldVideoIdList = jObj.OldVideoFileList;
    //            //    model.VideoIdList = jObj.VideoFileList;
    //            //    model.StorageSpaceRemaining = jObj.StorageSpaceRemaining;
    //            //}
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error when deserializing hardware status update: " + ex.Message);
    //            //this needs to be set up 
    //            //_log.LogError(ex.Message);
    //        }
    //    }

    //    public T DeserialiseJSONString<T>(string strJSON)
    //    {
    //        T output = default(T);
    //        try
    //        {
    //            output = JsonConvert.DeserializeObject<T>(strJSON);
    //        }
    //        catch (Exception ex)
    //        {
    //            output = default(T);
    //            //this needs to be set up    
    //            //_log.LogInformation(ex.Message);
    //        }
    //        return output;
    //    }

    //    internal async Task<List<string>> GetNameList()
    //    {
    //        List<string> output = new List<string>();
    //        try
    //        {
    //            output = _fileManager.GetDirectoryContentNames(FileSystem.ZippedModulePath);
    //            return output;
    //        }
    //        catch (Exception)
    //        {

    //            throw;
    //        }
    //    }

    //    public async Task<byte[]> GetFileContents(Guid moduleId)
    //    {
    //        byte[] output = { };
    //        try
    //        {
    //            output = FileManager.OutgoingFileData(Path.Combine(FileSystem.ZippedModulePath,moduleId.ToString()+".zip"));
    //            return output;
    //        }
    //        catch (Exception)
    //        {

    //            throw;
    //        }
    //    }
    //}
}
