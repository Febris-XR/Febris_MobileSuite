// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.ModelLibrary.Models.XApiModels;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models.EventArguments
{
    public class ModuleEventArgs
    {
        public string PackageName { get; set; }
        public string ModuleDirectoryName { get; set; }
        public string ModulePackageModelId { get; set; }
        public bool IsInstalled { get; set; }
        public string FileUri { get; set; }

        public ModuleEventArgs()
        {
            PackageName = string.Empty;
            ModuleDirectoryName = string.Empty;
            ModulePackageModelId = string.Empty;
            IsInstalled = false;
            FileUri = string.Empty;
        }
    }

    public class StatementEventArgs
    {
        public long ReferenceId { get; set; }
        public Guid ReferenceUUID { get; set; }
        public string RawStatementJson { get; set; }        
    }
}
