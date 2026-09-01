// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface IUSBRequests
    {
        Task SendUpload(string packagePath);
        Task SendUpload();// string input);
        Task MakeRequest();
        //Task InstallRequest(string localPath, string remotePath);
        Task PrepareUpload(string directoryPath);
        Task PrepareInstall(string directoryPath);
    }
}
