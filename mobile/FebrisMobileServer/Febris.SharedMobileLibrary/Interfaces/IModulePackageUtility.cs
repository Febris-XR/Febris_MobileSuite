// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface IModulePackageUtility
    {
        Task RunModule(string moduleName, string statementArguments);

        Task InstallModule(string moduleName, string filePath);

        Task<bool> DeleteModule(string moduleName);
        Task<List<string>> GetInstalledList();
        Task<bool> DeleteCompressedModule(string packetName);
        Task<bool> UninstallApplication(string packetName);
        Task<List<string>> GetInstalledList(Dictionary<string, string> packageIndex);
    }
}
