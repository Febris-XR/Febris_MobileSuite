// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.BusinessLogic
{
    public class VideoLogic
    {
        private ILogger _log;
        //private IConfiguration _config;
        //private readonly JSONHandler _jSONHandler;
        private readonly FileManager _fileManager;
        //private readonly VideoUploadRequest _videoUploadRequest;
        private readonly VideoFileProcessing _videoProcessing;

        public VideoLogic(ILogger log)
        {
            _log = log;
            _videoProcessing = new VideoFileProcessing(_log);
            _fileManager = new FileManager();// _log);//, _config);
        }
        public VideoLogic()
        {            
            _videoProcessing = new VideoFileProcessing(_log);
            _fileManager = new FileManager();// _log);//, _config);
        }

        public async Task<object> ProcessFiles()
        {
            bool output = false;
            try
            {
                var unsentRecordingList = await _fileManager.GetDirectoryContentNames(FileSystem.RecordingsFilePath);

                if (unsentRecordingList.Count() > 0)
                {
                    foreach (var i in unsentRecordingList)
                    {
                        if (CheckIfTypeIsMP4(i))
                        {
                            bool success = _videoProcessing.ProcessVideoFile(i);
                        }

                        ////gather file content
                        //string tempFile = _fileManager.GetFileContent(PCFileSystem.StatementPath, i);
                        ////send it
                        //bool sendComplete = _videoUploadRequest.UploadStatement(tempFile).Result;
                        //if (!sendComplete)
                        //{
                        //    sendComplete = _videoUploadRequest.UploadStatementBackup(tempFile).Result;
                        //}


                        //if (sendComplete)
                        //{
                        //    _fileManager.MoveStatementFileToSent(i);
                        //}
                    }

                }

            }
            catch (Exception e)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = e.Message;
            }
            return output;
        }


        public bool CheckIfTypeIsMP4(string file)
        {
            bool isVideo = false;
            string extension = Path.GetExtension(file);
            if (extension.ToLower() == ".mp4")
            {
                isVideo = true;
            }
            else
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    try
                    {
                        Directory.Delete(file, true);
                    }
                    catch (Exception ex)
                    {
                        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                    }
                }
            }
            return isVideo;
        }
    }
}
