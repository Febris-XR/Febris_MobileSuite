// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Models.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.DataLogic
{
    public class ConfigContext
    {
        private FileManager _fileManager = new FileManager();

        internal async Task<ConfigModel> Get()
        {
            ConfigModel output = new ConfigModel();
            try
            {
                string preoutput = _fileManager.GetFileContent(FileSystem.ConfigLocation, string.Empty);// "Config.json");
                output = JsonConvert.DeserializeObject<ConfigModel>(preoutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return output;
        }

        internal async Task<bool> Post(ConfigModel input)
        {
            bool output = false;
            try
            {
                string data = JsonConvert.SerializeObject(input);
                //string path = Path.Combine(FileSystem.ConfigLocation, "Config.json");
                output = _fileManager.Set(data, FileSystem.ConfigLocation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return output;
        }
                
    }
}
