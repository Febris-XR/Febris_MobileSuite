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
                // DIAGNOSTIC. This branch ignores the four domain fields entirely, and
                // DeveloperUrl ships empty, so a ticked Developer Account box silently
                // produces an empty ApiUrl and every request fails in WebConnection.Connect
                // with nothing on screen explaining why.
                Console.WriteLine("ApiUrl set from DEVELOPER ACCOUNT branch: '"
                    + LocalHardwareStaticDetails.ApiUrl + "'");
            }
            else
            {
                string prefix = _configModel.DomainPrefix??string.Empty;
                string domain = _configModel.Domain ?? string.Empty;
                string port = _configModel.DomainPort ?? string.Empty;
                string path = _configModel.DomainPath ?? string.Empty;
                string newUrl = string.Empty;

                // SCHEME HANDLING. This used to hardcode "https://" and then append the prefix as a
                // SUBDOMAIN, so no value of DomainPrefix could ever produce an http:// URL. Entering
                // "http" gave https://http.<domain>, and the apparent escape hatch was broken too:
                // a prefix of "https://" left newUrl empty and then appended "https://.", producing
                // https://.<domain>.
                //
                // That made a plain-HTTP node unreachable by construction, which is the normal shape
                // of a self-hosted node on a LAN. Observed on hardware 2026-09-02: the Server could
                // not reach a node on http://192.168.1.219:5101 by any configuration, and failed in
                // WebConnection.Connect before any credential logic ran.
                //
                // The prefix now doubles as the scheme when it names one, and keeps its original
                // subdomain meaning otherwise. Unset still means https, so existing deployments are
                // unchanged.
                string scheme = "https://";
                string subdomain = string.Empty;
                string trimmedPrefix = prefix.Trim();

                if (trimmedPrefix.EndsWith("://", StringComparison.OrdinalIgnoreCase))
                {
                    scheme = trimmedPrefix.ToLowerInvariant();
                }
                else if (trimmedPrefix.Equals("http", StringComparison.OrdinalIgnoreCase)
                      || trimmedPrefix.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    scheme = trimmedPrefix.ToLowerInvariant() + "://";
                }
                else
                {
                    subdomain = trimmedPrefix;
                }

                newUrl = scheme;

                if (!string.IsNullOrEmpty(subdomain))
                {
                    newUrl += subdomain + ".";
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

                // MUST end in a slash. Every caller concatenates directly onto this, for
                // example InitalizationRequest does endpoint + "Launcher/" + method, so a
                // missing slash yields http://host:5101Launcher/Initalize and WebRequest.Create
                // throws UriFormatException. The old code only ever produced a trailing slash
                // as a side effect of appending a non-empty path, so an operator who left Domain
                // Path blank, which is correct for a node at the site root, got a broken URI.
                if (!newUrl.EndsWith("/"))
                {
                    newUrl += "/";
                }

                LocalHardwareStaticDetails.ApiUrl = newUrl;
                Console.WriteLine("ApiUrl assembled from config: '" + newUrl
                    + "'  (prefix='" + prefix + "' domain='" + domain
                    + "' port='" + port + "' path='" + path + "')");
            }
        }
    }
}
