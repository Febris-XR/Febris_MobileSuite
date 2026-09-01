// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models.Data
{
    public class LocalSoftwarePackage : BaseModel
    {
        //public long Id { get; set; }
        //public Guid UUID { get; set; }


        //Out of date  
        //[Display(Name = "This Package is Obsolete")]
        public bool Obsolete { get; set; }

        //Basic information
        //[Display(Name = "Creation date")]
        //public DateTime CreationTimeStamp { get; set; }

        //[Display(Name = "Last Modified")]
        //public DateTime UpdateTimeStamp { get; set; }

        //[Display(Name = "Package Name")]
        public string Name { get; set; }

        public string Version { get; set; }

        //[Display(Name = "Description")]
        public string Description { get; set; }

        //[Display(Name = "What type of hardware is this package used with?")]
        public LocalSoftwarePackageType LocalSoftwarePackageType { get; set; }

        //[Display(Name = "Language of this package")]
        public LanguageMapTypeEnum Language { get; set; }
    }
}
