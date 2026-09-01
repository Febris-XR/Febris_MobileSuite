// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.DataLogic;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.BusinessLogic
{
    public class ConfigLogic
    {
        ConfigContext _context = new ConfigContext();

        public static async Task SaveSettings()
        {
            try
            {
                ConfigModel newData = LocalHardwareStaticDetails.StaticMainVM.ConfigVM.ConfigModel;
                ConfigContext _context = new ConfigContext();
                ConfigModel data = await _context.Get()??new ConfigModel();

                data.DeveloperAccount = newData.DeveloperAccount;
                if (!newData.DeveloperAccount)
                {
                    data.Domain = newData.Domain;
                    data.DomainPrefix = newData.DomainPrefix;
                    data.DomainPort = newData.DomainPort;
                    data.DomainPath = newData.DomainPath;
                }

                if (!string.IsNullOrEmpty(newData.UserName))
                {
                    data.UserName = newData.UserName;
                }
                if (!string.IsNullOrEmpty(newData.Password))
                {
                    data.Password = newData.Password;
                }

                bool compelete = await _context.Post(newData);

                if (compelete)
                {
                    URLSettingUtility.SetURL();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task<ConfigModel> GetSettings()
        {
            try
            {
                ConfigContext _context = new ConfigContext();
                ConfigModel data = await _context.Get()??new ConfigModel();
                //if (data == default)
                //{
                //    data = new ConfigModel();
                //}
                return data;
                //LocalHardwareStaticDetails.StaticMainVM.ConfigVM.ConfigModel = data;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);                
            }
            return new ConfigModel();
        }
    }
}
