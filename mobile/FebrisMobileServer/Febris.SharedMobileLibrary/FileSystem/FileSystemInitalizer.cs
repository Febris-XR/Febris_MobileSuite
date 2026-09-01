// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.SharedMobileLibrary.FileSystem
{
    public class FileSystemInitalizer
    {
        
        private readonly IConfiguration _config;
        private ILogger _log;
               
        public FileSystemInitalizer(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public FileSystemInitalizer()
        {
        }
        public async Task FileInitalizer(string externalPath)
        {
            try
            {
                ISharedFileSystem externalPlatformFileSystem = DependencyService.Get<ISharedFileSystem>();                
                IExternalFileSystemMethods externalFileSystemRectifier = new ExternalFileSystemMethods();
                //trying something different
                //externalPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                bool complete = externalFileSystemRectifier.ExternalFileSystemRectifier(externalPath).Result;
                if (!complete)
                {
                    Console.WriteLine("files not set up correctly");
                }
                //FileSystem.ExternalBasePath = externalPath;

                List<string> externalFileList = new List<string>
                {
                    FileSystem.ExternalBasePath,                    
                    FileSystem.MediaPath,
                    FileSystem.VideoPath,
                    FileSystem.SplitFilePath,
                    FileSystem.RecordingsFilePath,
                    FileSystem.TempRecordingsFilePath,
                    FileSystem.zipFolderPath,
                    FileSystem.BaseModulePath,
                    FileSystem.ModuleLinkPath,
                    FileSystem.ModulePath,
                    FileSystem.ZippedModulePath,
                    FileSystem.BaseStatementPath,
                    FileSystem.StatementPath,
                    FileSystem.WorkingStatementPath,
                    FileSystem.OldStatementPath,                    
#if (DEBUG)
                    FileSystem.CompanionApplicationPath,
                    FileSystem.CompressedCompanionApplicationPath,
                    FileSystem.UncompressedCompanionApplicationPath
#endif
                };


                List<string> internalFileList = new List<string>
                {
                    
                    FileSystem.BasePath,
                    //FileSystem.ModuleLinkPath,
                    FileSystem.BaseLogPath,
                    FileSystem.UploaderLogPath,
                    FileSystem.LauncherLogPath,
                    FileSystem.RecorderLogPath,
                    FileSystem.SimulationLogBasePath,
                    FileSystem.ModuleManagerLogPath,
                    FileSystem.sLocation,
                    //FileSystem.SharedDataPath
#if (!DEBUG)
                    FileSystem.CompanionApplicationPath,
                    FileSystem.CompressedCompanionApplicationPath,
                    FileSystem.UncompressedCompanionApplicationPath
#endif
                };

                

                foreach (var file in internalFileList)
                {
                    try
                    {                        
                        CreateFileDirectory(file, string.Empty);
                        Console.WriteLine("Internal File " + file + " was created successfully");
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.Message);
                    }
                }
                //foreach (var file in externalFileList)
                for (var i=0; i<externalFileList.Count-1;i++)
                {
                    #region Pre Android 11
                    //try
                    //{
                    //    CreateFileDirectory(externalFileList[i], string.Empty);
                    //    Console.WriteLine("Internal File " + externalFileList[i] + " was created successfully");
                    //}
                    //catch (Exception ex)
                    //{
                    //    _log.Error(ex.Message);
                    //}
                    #endregion
                    #region Post Android 11
                    try
                    {
                        string[] tempNameArray = externalFileList[i].Split('/');
                        bool worked = false;
                        string uriPath = string.Empty;
                        (worked, uriPath) = await externalPlatformFileSystem.CreateDirectory(tempNameArray.Last(), externalFileList[i]);
                        //(worked, uriPath) = await externalPlatformFileSystem.CreateDirectory(string.Empty, externalFileList[i]);
                        if (worked)
                        {
                            Console.WriteLine("Uri path " + uriPath + " was created successfully");
                        }
                        else
                        {
                            Console.WriteLine("Uri path " + uriPath + " Failed");
                        }
                        externalFileList[i] = uriPath;


                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.Message);                       
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);                
            }
        }
        public void FileInitalizer()
        {
            try
            {
                List<string> FileList = new List<string>
                {
                    FileSystem.ExternalBasePath,
                    FileSystem.BasePath,
                    FileSystem.MediaPath,
                    FileSystem.VideoPath,
                    FileSystem.SplitFilePath,
                    FileSystem.RecordingsFilePath,
                    FileSystem.TempRecordingsFilePath,
                    FileSystem.zipFolderPath,
                    FileSystem.BaseModulePath,
                    FileSystem.ModuleLinkPath,
                    FileSystem.ModulePath,
                    FileSystem.ZippedModulePath,
                    FileSystem.BaseStatementPath,
                    FileSystem.StatementPath,
                    FileSystem.WorkingStatementPath,
                    FileSystem.OldStatementPath,
                    FileSystem.BaseLogPath,
                    FileSystem.UploaderLogPath,
                    FileSystem.LauncherLogPath,
                    FileSystem.RecorderLogPath,
                    FileSystem.SimulationLogBasePath,
                    FileSystem.ModuleManagerLogPath,
                    FileSystem.sLocation,                    
                    //FileSystem.SharedDataPath
                     FileSystem.CompanionApplicationPath,
                    FileSystem.CompressedCompanionApplicationPath,
                    FileSystem.UncompressedCompanionApplicationPath
                };

                foreach (var file in FileList)
                {
                    try
                    {
                        CreateFileDirectory(file, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        //_log.Error(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                //_log.Error(ex.Message);                
            }
        }

        //Todo: split internal and external. Route internal normally and external throught IExternalPlatformFileSystem


        //#############################################################################
        // connecting to the file storage retriever
        //#############################################################################
        public static void CreateFileDirectory(string path, string name)
        {
            try
            {                
                Directory.CreateDirectory(Path.Combine(path, name));
                Console.WriteLine(path + name + " was created created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(path+name +" was not created");
                //_log.Error(ex.Message);
            }
        }

    }
}
