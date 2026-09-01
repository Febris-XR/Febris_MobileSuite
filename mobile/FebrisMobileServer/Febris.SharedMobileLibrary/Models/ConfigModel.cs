// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models
{
    public class ConfigModel 
    {
        public ConfigModel()
        {
            DeveloperAccount = false;
            Domain = string.Empty;
            DomainPrefix = string.Empty;
            DomainPort = string.Empty;
            DomainPath = string.Empty;
            UserName = string.Empty;
            Password = string.Empty;
        }

        //public string Name { get; set; }
        //public string UniqueIdentifier { get; set; }

        #region domain breakdown

        /// <summary>
        /// Explicit opt-in FEDERATION flag. When true the client federates to the
        /// "developer account" endpoint (<c>LocalHardwareStaticDetails.DeveloperUrl</c>) -- the
        /// mobile Server's ONLY direct central-tier hit. Defaults to <c>false</c> (severance,
        /// OSS_NODE_PLAN 3.2/3.5): out of the box the client talks only to the operator's node,
        /// built from the <see cref="Domain"/>/<see cref="DomainPrefix"/>/<see cref="DomainPort"/>/
        /// <see cref="DomainPath"/> fields below. Set true only to deliberately federate to a
        /// developer/central node (and DeveloperUrl must be supplied via config/env -- it no
        /// longer ships a compiled Febris SaaS default).
        /// </summary>
        public bool DeveloperAccount { get; set; }
        public string Domain { get; set; }
        public string DomainPrefix { get; set; }
        public string DomainPort { get; set; }
        public string DomainPath { get; set; }
        #endregion

        public string UserName { get; set; }
        public string Password { get; set; }

    }
}
