// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.APIInteractions;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models.ViewModels;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.BusinessLogic
{
    class VideoFileProcessing
    {
        private ILogger _log;
        private readonly VideoUploadRequest _apiRequest;
        private IConfiguration _config;
        private readonly FileManager _fileManager;

        public VideoFileProcessing(ILogger log)
        {
            _log = log;

            _apiRequest = new VideoUploadRequest(_log, _config);
            _fileManager = new FileManager(_log, _config);

        }

        //*********************************************************************************************
        // handles setting up the video file
        //Todo: need to make sure only true is returned if every file is uploaded
        //https://www.c-sharpcorner.com/article/upload-large-files-to-mvc-webapi-using-partitioning/
        //*********************************************************************************************
        //public bool VideoFileHandler(string filePath, string fileName)
        //{
        //    Process process = ProgressBarService.StartProgressBar(fileName, StatusType.Uploading);
        //    bool rslt = false;
        //    try
        //    {

        //        //int i = 0;
        //        VideoFile videoFile = new VideoFile()
        //        {
        //            FileName = fileName,
        //            //TempFolder = Path.Combine(StaticDetails.SplitFilePath, fileName),
        //            TempFolder = Path.Combine(PCFileSystem.SplitFilePath, fileName),
        //            MaxFileSizeMB = 5,
        //            FileParts = new List<string>()
        //        };

        //        bool splitFile = SplitFile(videoFile, filePath);
        //        if (splitFile == true)
        //        {
        //            Console.WriteLine(fileName + " File split successful");
        //        }

        //        foreach (string File in videoFile.FileParts)
        //        {
        //            bool uploaded = false;
        //            uploaded = UploadFile(File);
        //            //bool uploaded = UploadFile(File);
        //            if (uploaded == true)
        //            {
        //                Console.WriteLine(File + " has been uploaded");
        //                rslt = true;
        //            }
        //            else
        //            {
        //                Console.WriteLine("upload did not work");
        //                rslt = false;
        //                break;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.LogError(ex.Message);
        //    }
        //    ProgressBarService.StopProgressBar(process);

        //    return rslt;
        //}

        public bool ProcessVideoFile(string fileName)
        {
            //string filePath = PCFileSystem.RecordingsFilePath;
            string filePath = Path.Combine(FileSystem.RecordingsFilePath, fileName);
            //Process process = ProgressBarService.StartProgressBar(fileName, StatusType.Uploading);
            bool rslt = false;
            try
            {

                //int i = 0;
                VideoFile videoFile = new VideoFile()
                {
                    FileName = fileName,
                    //TempFolder = Path.Combine(StaticDetails.SplitFilePath, fileName),
                    TempFolder = Path.Combine(FileSystem.SplitFilePath, fileName),
                    MaxFileSizeMB = 5,
                    FileParts = new List<string>()
                };

                bool splitFile = SplitFile(videoFile, filePath);
                if (splitFile)
                {
                    Console.WriteLine(fileName + " File split successful");
                }
                else
                {
                    return rslt;
                }

                foreach (string File in videoFile.FileParts)
                {
                    bool uploaded = false;
                    uploaded = UploadFile(File);
                    //bool uploaded = UploadFile(File);
                    if (uploaded == true)
                    {
                        Console.WriteLine(File + " has been uploaded");
                        rslt = true;
                    }
                    else
                    {
                        Console.WriteLine("upload did not work");
                        rslt = false;
                        break;
                    }
                }

                if (rslt)
                {
                   // bool complete = MoveAndZipFile(fileName);
                }

            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
            }
            finally
            {
                //ProgressBarService.StopProgressBar(process);
            }


            return rslt;
        }

        //private bool MoveAndZipFile(string i)
        //{
        //    bool zipFile = false;
        //    var zipTask = Task.Run(() => _fileManager.ZipVideoFile(i));//, files.ElementAt(i)));
        //    zipTask.Wait();
        //    zipFile = zipTask.Result;
        //    //_fileManager.ZipVideoFile(i);
        //    //zipFile = true;

        //    if (zipFile == true)
        //    {
        //        //something
        //    }
        //    return zipFile;
        //}

        //*********************************************************************************************
        //split file the file into chuncks and save in the splitvideos folder
        //*********************************************************************************************
        public bool SplitFile(VideoFile videoFile, string FilePath)
        {
            try
            {
                bool rslt = false;
                string BaseFileName = Path.GetFileName(FilePath);
                // set the size of file chunk we are going to split into  
                int BufferChunkSize = videoFile.MaxFileSizeMB * (1024 * 1024);
                // set a buffer size and an array to store the buffer data as we read it  
                const int READBUFFER_SIZE = 1024;
                byte[] FSBuffer = new byte[READBUFFER_SIZE];
                // open the file to read it into chunks  
                using (FileStream FS = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))//sometimes access is denied to local folders
                {
                    // calculate the number of files that will be created  
                    int TotalFileParts = 0;
                    if (FS.Length < BufferChunkSize)
                    {
                        TotalFileParts = 1;
                    }
                    else
                    {
                        float PreciseFileParts = ((float)FS.Length / (float)BufferChunkSize);
                        TotalFileParts = (int)Math.Ceiling(PreciseFileParts);
                    }

                    int FilePartCount = 0;
                    // scan through the file, and each time we get enough data to fill a chunk, write out that file  
                    while (FS.Position < FS.Length)
                    {
                        string FilePartName = String.Format("{0}.part_{1}.{2}",
                        BaseFileName,
                        (FilePartCount + 1).ToString(),
                        TotalFileParts.ToString());

                        //FilePartName = Path.Combine(StaticDetails.SplitFilePath, FilePartName);
                        FilePartName = Path.Combine(FileSystem.SplitFilePath, FilePartName);
                        videoFile.FileParts.Add(FilePartName);
                        using (FileStream FilePart = new FileStream(FilePartName, FileMode.Create))
                        {
                            int bytesRemaining = BufferChunkSize;
                            int bytesRead = 0;
                            while (bytesRemaining > 0 && (bytesRead = FS.Read(FSBuffer, 0,
                             Math.Min(bytesRemaining, READBUFFER_SIZE))) > 0)
                            {
                                FilePart.Write(FSBuffer, 0, bytesRead);
                                bytesRemaining -= bytesRead;
                            }
                        }
                        // file written, loop for next chunk  
                        FilePartCount++;
                    }
                    rslt = true;
                }
                return rslt;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                return false;
            }

        }

        //*********************************************************************************************
        //Upload each chunk        
        //Todo: delete small file if video upload worked
        //*********************************************************************************************
        private bool UploadFile(string FileName)
        {
            bool rslt = false;
            //FebrisLocalLibrary.Communication.FebrisRestClient febrisRestClient = new FebrisLocalLibrary.Communication.FebrisRestClient();
            rslt = _apiRequest.VideoUpload(FileName).Result;
            return rslt;
        }
    }
}
