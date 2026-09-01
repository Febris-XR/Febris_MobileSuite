// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.FileSystem;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Febris.SharedMobileLibrary.Utilites
{
    public class ConfigSettings
    {
        private ILogger _log;
        private IConfiguration _config;
        private readonly FileManager _fileManager;

        public ConfigSettings(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
            _fileManager = new FileManager(_log, _config);
        }

        public ConfigSettings()
        {
            _fileManager = new FileManager(_log, _config);
        }

        public async Task<JObject> Get()
        {
            JObject configSettings = new JObject();
            try
            {
                string fileData = _fileManager.GetFileContent(FileSystem.FileSystem.ConfigLocation);
                configSettings = await FileManager.ChangeToObject(fileData);
            }
            catch
            {
            }
            return configSettings;
        }

        public async Task<bool> SetDomainPrefix(string input)
        {
            bool isSet = false;
            try
            {
                input = "{'domainprefix':" + "'" + input + "'}";
                JObject processedInput = await FileManager.ChangeToObject(input);
                isSet = _fileManager.Set(processedInput, FileSystem.FileSystem.ConfigLocation);
            }
            catch
            {

            }
            return isSet;
        }

    }
}
