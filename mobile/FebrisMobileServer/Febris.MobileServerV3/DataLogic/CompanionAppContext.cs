// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.SharedMobileLibrary.Utilites;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.DataLogic
{
    public class CompanionAppContext
    {
        private FileManager _fileManager = new FileManager();


        public async Task<bool> DeleteOldCompanionApp(Guid input)
        {
            bool output = false;
            try
            {
                List<string> fileList = await _fileManager.GetDirectoryContentNames(FileSystem.CompressedCompanionApplicationPath);

                foreach (var i in fileList)
                {
                    //output = await _fileManager.DeleteFolders(Path.Combine(FileSystem.CompressedCompanionApplicationPath, i));
                    output = await _fileManager.DeleteDirectory(Path.Combine(FileSystem.CompressedCompanionApplicationPath, i));
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

        //internal async Task<LocalSoftwarePackage> GetCurrentInfo()
        //{
        //    LocalSoftwarePackage output = new LocalSoftwarePackage();
        //    try
        //    {
        //        string preoutput = _fileManager.GetFileContent(FileSystem.CompanionApplicationPath,"CompanionAppInfo.json");
        //        output = JsonConvert.DeserializeObject<LocalSoftwarePackage>(preoutput);                
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);                
        //    }
        //    return output;
        //}

        internal async Task<LocalSoftwarePackage> GetLocalVersionInfo()
        {            
            LocalSoftwarePackage output = new LocalSoftwarePackage();
            try
            {
                string preoutput = _fileManager.GetFileContent(FileSystem.CompanionApplicationPath, "CompanionAppInfo.json");
                output = JsonConvert.DeserializeObject<LocalSoftwarePackage>(preoutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return output;
        }

        internal async Task<bool> SaveInfo(LocalSoftwarePackage input)
        {
            bool output = false;
            try
            {
                string data = JsonConvert.SerializeObject(input);
                string path = Path.Combine(FileSystem.CompanionApplicationPath, "CompanionAppInfo.json");
                output = _fileManager.Set(data,path);                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return output;
        }

        internal async Task<bool> FileExists(Guid input)
        {
            bool output = false;
            try
            {                           
                List<string> uncompressedFiles = await _fileManager.GetDirectoryContentNames(FileSystem.UncompressedCompanionApplicationPath);
                List<string> compressedFiles = await _fileManager.GetDirectoryContentNames(FileSystem.CompressedCompanionApplicationPath);
                output = uncompressedFiles.Where(i => i.Contains(input.ToString())).Any();
                if (!output)
                {
                    bool compressedFileExists = compressedFiles.Where(i => i.Contains(input.ToString())).Any();
                    if (compressedFileExists)
                    {
                        PairingPageStatusHelper.GenericMessage("Decompressing File");
                        bool success = await CompressionHelper.Decompress(
                            Path.Combine(FileSystem.CompressedCompanionApplicationPath,input+".zip"),
                            FileSystem.UncompressedCompanionApplicationPath,
                            input.ToString());
                        // Path.Combine(FileSystem.CompressedCompanionApplicationPath, "input.zip"));
                        if (success)
                        {
                            output = success;
                            PairingPageStatusHelper.GenericMessage("File Decompressed and ready to use");
                        }
                        else
                        {
                            PairingPageStatusHelper.GenericMessage("The desired file either could not be decompressed or is broken on the system");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                PairingPageStatusHelper.GenericMessage(ex.Message);
            }
            return output;
        }

        

        //public T DeserialiseJSONString<T>(string strJSON)
        //{
        //    T output = default(T);
        //    try
        //    {
        //        output = JsonConvert.DeserializeObject<T>(strJSON);
        //    }
        //    catch (Exception ex)
        //    {
        //        output = default(T);
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //    }
        //    return output;
        //}

        //internal async Task<List<string>> GetNameList()
        //{
        //    List<string> output = new List<string>();
        //    try
        //    {
        //        output = _fileManager.GetDirectoryContentNames(FileSystem.BasePath);
        //        return output;
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}

        //public async Task<byte[]> GetFileContents(Guid itemId)
        //{
        //    byte[] output = { };
        //    try
        //    {
        //        output = FileManager.OutgoingFileData(Path.Combine(FileSystem.BasePath, itemId.ToString() + ".zip"));
        //        return output;
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}
    }
}
