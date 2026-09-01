// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Febris.SharedMobileLibrary.FileSystem;

namespace Febris.SharedMobileLibrary.Interfaces
{
    interface IExternalFileSystemMethods
    {
        Task<bool> ExternalFileSystemRectifier(string externalFilePath);
    }

    public class ExternalFileSystemMethods : IExternalFileSystemMethods
    {
        public async Task<bool> ExternalFileSystemRectifier(string externalFilePath)
        {
            bool output = false;
            try
            {
                #region Designed for the provider file system that is more universal for Android - this actually should still work 

                Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath = externalFilePath;
                Febris.SharedMobileLibrary.FileSystem.FileSystem.MediaPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath, "Media");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.MediaPath, "Videos");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.SplitFilePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath, "SplitVideos");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.RecordingsFilePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath, "Recordings");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.TempRecordingsFilePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath, "TempRecording");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.zipFolderPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath, "ZippedRecordings");
                ///Modules
                Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseModulePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath, "Modules");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.ZippedModulePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseModulePath, "ZippedModuleFiles");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.ModulePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseModulePath, "Modules");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.ModuleLinkPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseModulePath, "ModuleLinks");

                Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseStatementPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath, "statements");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.StatementPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseStatementPath, "statements");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.WorkingStatementPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseStatementPath, "workingstatement");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.OldStatementPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseStatementPath, "oldstatements");

                Febris.SharedMobileLibrary.FileSystem.FileSystem.CompanionApplicationPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath, "CompanionApplication");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.CompressedCompanionApplicationPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.CompanionApplicationPath, "Compressed");
                Febris.SharedMobileLibrary.FileSystem.FileSystem.UncompressedCompanionApplicationPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.CompanionApplicationPath, "Uncompressed");
                #endregion
                #region this is depreciated due to Android 11 restrictions
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath = externalFilePath;
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.MediaPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath, "Media");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.MediaPath, "Videos");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.SplitFilePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath, "SplitVideos");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.RecordingsFilePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath, "Recordings");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.TempRecordingsFilePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath, "TempRecording");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.zipFolderPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.VideoPath, "ZippedRecordings");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseModulePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath, "Modules");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.ModulePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseModulePath, "Modules");

                //Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseStatementPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath, "statements");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.StatementPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseStatementPath, "statements");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.WorkingStatementPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseStatementPath, "workingstatement");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.OldStatementPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BaseStatementPath, "oldstatements");

                //Febris.SharedMobileLibrary.FileSystem.FileSystem.CompanionApplicationPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.ExternalBasePath, "CompanionApplication");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.CompressedCompanionApplicationPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.CompanionApplicationPath, "Compressed");
                //Febris.SharedMobileLibrary.FileSystem.FileSystem.UncompressedCompanionApplicationPath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.CompanionApplicationPath, "Uncompressed");
                #endregion
                output = true;
            }
            catch (Exception)
            {

                throw;
            }

            return output;
        }
    }
}
