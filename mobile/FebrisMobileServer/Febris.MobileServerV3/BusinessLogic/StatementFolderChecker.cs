// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.APIInteractions;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Utilites;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.BusinessLogic
{
    class StatementFolderChecker
    {
        private ILogger _log;
        private IConfiguration _config;
        private readonly JSONHandler _jSONHandler;
        private readonly FileManager _fileManager;
        private readonly StatementRequest _statementRequest;

        public StatementFolderChecker(ILogger log)
        {
            _log = log;
            _jSONHandler = new JSONHandler();
            _fileManager = new FileManager(_log, _config);

        }

        public async Task<object> ProcessFiles()
        {
            bool output = false;
            try
            {
                var unsentStatementList = await _fileManager.GetDirectoryContentNames(FileSystem.StatementPath);
                if (unsentStatementList.Count() > 0)
                {
                    foreach (var i in unsentStatementList)
                    {
                        //gather file content
                        string tempFile = _fileManager.GetFileContent(FileSystem.StatementPath, i);
                        //send it
                        bool sendComplete = _statementRequest.UploadStatement(tempFile).Result;
                        if (!sendComplete)
                        {
                            sendComplete = _statementRequest.UploadStatementBackup(tempFile).Result;
                        }


                        if (sendComplete)
                        {
                            _fileManager.MoveStatementFileToSent(i);
                        }
                    }

                }

            }
            catch (Exception e)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = e.Message;
            }
            return output;
        }
    }
}
