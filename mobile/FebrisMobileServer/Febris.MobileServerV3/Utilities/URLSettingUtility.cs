// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileServerV3.Utilities
{
    public class URLSettingUtility
    {
        public static void SetURL()
        {            
            ConfigModel _configModel = ConfigLogic.GetSettings().Result;

            if (_configModel.DeveloperAccount)
            {
                LocalHardwareStaticDetails.ApiUrl = LocalHardwareStaticDetails.DeveloperUrl;
            }
            else
            {
                string prefix = _configModel.DomainPrefix??string.Empty;
                string domain = _configModel.Domain ?? string.Empty;
                string port = _configModel.DomainPort ?? string.Empty;
                string path = _configModel.DomainPath ?? string.Empty;
                string newUrl = string.Empty;
                if (!prefix.Contains("https://")) 
                {
                    newUrl = "https://";
                }

                if (!string.IsNullOrEmpty(prefix))
                {
                    newUrl += prefix+".";
                }


                if (!string.IsNullOrEmpty(domain))
                {
                    newUrl += domain;
                }

                if (!string.IsNullOrEmpty(port))
                {
                    newUrl += ":"+port;
                }
                if (!string.IsNullOrEmpty(path))
                {
                    newUrl += "/" + path + "/";
                }

                LocalHardwareStaticDetails.ApiUrl = newUrl;
            }
        }
    }
}
