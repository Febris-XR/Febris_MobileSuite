// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Enums;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Febris.SharedMobileLibrary.FileSystem
    
{
    public class DataProtection: IDataProtection
    {
        private ILogger _log;
        private IConfiguration _config;

        public DataProtection(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }
        
        public DataProtection(ILogger log)
        {
            _log = log;
        }

        public DataProtection()
        {
        }
        
        public async void EncryptInput(string input, ConfigType configType)
        {
            try
            {
                await SecureStorage.SetAsync(configType.ToString(), input);                                
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        public async Task<string> DecryptFile(ConfigType configType)
        {
            try
            {
                string output = await SecureStorage.GetAsync(configType.ToString());
                return output;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return string.Empty;
            }
        }
        public async Task<bool> CredentialsExist()
        {
            bool exist = false;
            try
            {
                //bool credentialsGathered = false;
                string user = string.Empty;
                string secret = string.Empty;
                user = await DecryptFile(ConfigType.UserName);
                secret = await DecryptFile(ConfigType.Password);

                if ((user != string.Empty|| user != null) && (secret != string.Empty|| secret != string.Empty))
                {
                    exist = true;
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            return exist;
        }

        internal async Task<(bool exists, string userName, string secret)> GetCredentials()
        {
            try
            {
                bool exists = await CredentialsExist();
                string user = string.Empty;
                string secret = string.Empty;
                user = await DecryptFile(ConfigType.UserName);
                secret = await DecryptFile(ConfigType.Password);
                return (exists, user, secret);
            }
            catch
            {
                return (false, string.Empty, string.Empty);
            }
        }

        public async void StoreInput(string input, ConfigType configType)
        {
            try
            {
                Preferences.Set(configType.ToString(), input);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        public async Task<string> GetInput(ConfigType configType)
        {
            try
            {
                string output = Preferences.Get(configType.ToString(), string.Empty);
                return output;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return string.Empty;
            }
        }

        string IDataProtection.GetInput(ConfigType configType)
        {
            try
            {
                string output = Preferences.Get(configType.ToString(), string.Empty);
                return output;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                return string.Empty;
            }
        }
    }

    public interface IDataProtection
    {
        void StoreInput(string input, ConfigType configType);
        string GetInput(ConfigType configType);
    }
}
