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
    /// <summary>
    /// Saves companion device information locally
    /// </summary>
    public class CompanionDeviceContext
    {
        private FileManager _fileManager = new FileManager();

        public async Task<List<CompanionDevice>> GetSavedList()
        {
            List<CompanionDevice> output = new List<CompanionDevice>();
            try
            {
                //FileManager _fileManager = new FileManager();
                string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json");
                output = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
                return output;
            }
            catch (Exception ex)
            {
                return output;
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
                throw;
            }
        }

        public async Task<List<CompanionDeviceViewModel>> GetList()
        {
            List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
            try
            {
                List<CompanionDevice> data = await GetSavedList();

                foreach (var i in data)
                {
                    CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
                    {
                        CompanionDevice = i
                    };
                    output.Add(temp);
                }
                return output;
            }
            catch (Exception ex)
            {
                return output;
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
                throw;
            }
        }

        public async Task<List<CompanionDeviceViewModel>> Post(CompanionDevice input)
        {
            List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
            try
            {
                List<CompanionDevice> data = await GetSavedList();
                if (data == null)
                {
                    data = new List<CompanionDevice>();
                }
                // DEDUPE ON THE UNIQUE IDENTIFIER, not the Bluetooth MAC.
                //
                // This used to be `i.BlueToothMacAddress == input.BlueToothMacAddress`, which was
                // correct while every device was onboarded over Bluetooth and therefore had one.
                // A numerically-paired device has NO Bluetooth MAC at all, because the Companion
                // cannot read its own adapter, so the field is null on every such record. Two nulls
                // compare equal, so the FIRST numerically-paired device saved and every subsequent
                // one was treated as a duplicate and silently dropped.
                //
                // That is why a Server holding two pairings listed one device: both secrets were
                // stored, because the PSK store keys on the identifier, but the second device
                // record was never written. Observed on hardware 2026-07-28 with two Companions.
                //
                // The Bluetooth comparison is KEPT as a fallback so genuinely Bluetooth-onboarded
                // records, which may predate unique identifiers, still deduplicate as they did.
                bool alreadyKnown =
                    (!string.IsNullOrWhiteSpace(input.UniqueIdentifier)
                        && data.Any(i => i.UniqueIdentifier == input.UniqueIdentifier))
                    || (!string.IsNullOrWhiteSpace(input.BlueToothMacAddress)
                        && data.Any(i => i.BlueToothMacAddress == input.BlueToothMacAddress));

                if (!alreadyKnown)
                {
                    data.Add(input);
                    string stringData = JsonConvert.SerializeObject(data);
                    bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));
                }



                //FileManager _fileManager = new FileManager();
                //string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json")??string.Empty;
                //List<CompanionDevice> data = new List<CompanionDevice>();
                //if (!string.IsNullOrEmpty(stringData))
                //{
                //    data = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
                //}
                //List<CompanionDevice> data = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);


                //foreach (var i in data)
                //{
                //    CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
                //    {
                //        CompanionDevice = i
                //    };
                //    output.Add(temp);
                //}

                output = await GetList();
                return output;
            }
            catch (Exception ex)
            {
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
                throw;
            }
        }

        public async Task<List<CompanionDeviceViewModel>> Update(CompanionDevice input)
        {
            List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
            try
            {
                List<CompanionDevice> data = await GetSavedList();
                //FileManager _fileManager = new FileManager();
                //string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json");
                //List<CompanionDevice> data = JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
                // MATCH THE RECORD BEING UPDATED, not every record with a null Bluetooth MAC.
                //
                // This was `i.BlueToothMacAddress == input.BlueToothMacAddress`, and since every
                // numerically-paired device has a NULL Bluetooth MAC, that matched EVERY SUCH
                // RECORD. The loop below then removed all of them and re-added only the one being
                // updated, so a single update collapsed the whole device list to one entry.
                //
                // WiFiInformationRequest calls Update as soon as any device's WiFi identity
                // resolves, so with two Companions paired the first resolution silently destroyed
                // the other's record. This is the same null-Bluetooth-key mistake as Post above,
                // in a place where it deletes data rather than merely refusing to add it.
                List<CompanionDevice> item = data
                    .Where(i => !string.IsNullOrWhiteSpace(input.UniqueIdentifier)
                                    ? i.UniqueIdentifier == input.UniqueIdentifier
                                    : (!string.IsNullOrWhiteSpace(input.BlueToothMacAddress)
                                        && i.BlueToothMacAddress == input.BlueToothMacAddress))
                    .ToList();
                //item = input;
                foreach (var i in item)
                {
                    data.Remove(i);
                }
                //data.Remove(item);
                data.Add(input);
                string stringData = JsonConvert.SerializeObject(data);
                bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));

                //foreach (var i in data)
                //{
                //    CompanionDeviceViewModel temp = new CompanionDeviceViewModel()
                //    {
                //        CompanionDevice = i
                //    };
                //    output.Add(temp);
                //}

                output = await GetList();
                return output;
            }
            catch (Exception ex)
            {
                //this needs to be set up    
                //_log.LogInformation(ex.Message);
                throw;
            }
        }

        public async Task<List<CompanionDeviceViewModel>> Delete(CompanionDevice input)
        {
            List<CompanionDeviceViewModel> output = new List<CompanionDeviceViewModel>();
            try
            {

                //FileManager _fileManager = new FileManager();
                //string stringData = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json");
                //List<CompanionDevice> data = await GetSavedList();//JsonConvert.DeserializeObject<List<CompanionDevice>>(stringData);
                //data.Remove(input);
                //string stringData = JsonConvert.SerializeObject(data);
                //bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));

                List<CompanionDevice> data = await GetSavedList();
                List<CompanionDevice> item = data.Where(i => i.UniqueIdentifier == input.UniqueIdentifier).ToList();
                //item = input;
                foreach (var i in item)
                {
                    data.Remove(i);
                }
                string stringData = JsonConvert.SerializeObject(data);
                bool saved = _fileManager.Set(stringData, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));

                output = await GetList();
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
