// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Febris.SharedMobileLibrary.FileSystem
{
    public class FileSystem
    {
        //File structure
        //########################################################################
        //Base febrisfile
        //-Media
        //--Video
        //---splitvideo
        //---recordings  
        //---ZippedRecordings
        //-Modules
        //  - links
        //  - module files
        //-Statements
        //Logs
        //  - Uploader
        //  - Launcher
        //########################################################################
        //local folder paths        
        public static string BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Febris") ;
        public static string ExternalBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Febris");
        //public static string ExternalBasePath = Path.Combine(Android.OS.Environment.GetFolderPath(Android.OS.Environment.ExternalStorage), "com.febris.files");
        //########################################################################
        //media
        //########################################################################
        public static string MediaPath = Path.Combine(ExternalBasePath, "Media");
        //########################################################################
        //inside Media directory
        //########################################################################
        //video path
        public static string VideoPath = Path.Combine(MediaPath, "Videos");
        //########################################################################
        //inside video directory
        //########################################################################
        ////split file path
        public static string SplitFilePath = Path.Combine(VideoPath, "SplitVideos");
        //recording path
        public static string RecordingsFilePath = Path.Combine(VideoPath, "Recordings");
        //recording path
        public static string TempRecordingsFilePath = Path.Combine(VideoPath, "TempRecording");
        //zipped video path        
        public static string zipFolderPath = Path.Combine(VideoPath, "ZippedRecordings");
        //########################################################################
        //Modules
        //########################################################################
        public static string BaseModulePath = Path.Combine(ExternalBasePath, "Modules");
        //########################################################################
        //inside module directory
        //########################################################################
        public static string ModuleLinkPath = Path.Combine(BaseModulePath, "ModuleLinks");
        public static string ModulePath = Path.Combine(BaseModulePath, "Modules");
        public static string ZippedModulePath = Path.Combine(BaseModulePath, "ZippedModuleFiles");
        //internal static string simulationName = string.Empty;
        //internal static string ModuleLinkLaunch = ModuleLinkPath+@"\" + simulationName + ".lnk";
        //########################################################################
        //Statements directory
        //########################################################################
        public static string BaseStatementPath = Path.Combine(ExternalBasePath, "statements");
        //########################################################################
        //inside statement directory
        //########################################################################
        public static string StatementPath = Path.Combine(BaseStatementPath, "statements");
        public static string WorkingStatementPath = Path.Combine(BaseStatementPath, "workingstatement");
        public static string OldStatementPath = Path.Combine(BaseStatementPath, "oldstatements");
        //########################################################################
        //Logs
        //########################################################################
        public static string BaseLogPath = Path.Combine(BasePath, "Logs");
        //########################################################################
        //inside Log directory
        //########################################################################
        public static string UploaderLogPath = Path.Combine(BaseLogPath, "UploaderLogs");
        public static string LauncherLogPath = Path.Combine(BaseLogPath, "LauncherLogs");
        public static string RecorderLogPath = Path.Combine(BaseLogPath, "RecorderLogs");
        public static string ModuleManagerLogPath = Path.Combine(BaseLogPath, "ModuleManagerLogs");
        public static string SimulationLogBasePath = Path.Combine(BaseLogPath, "SimulationLogs");
        //########################################################################
        //Credentials
        //########################################################################
        public static string sLocation = Path.Combine(BasePath, "cred");
        //########################################################################
        //inside credential directory
        //########################################################################
        public static string userNameLocation = Path.Combine(sLocation, "user.dat");
        public static string passwordLocation = Path.Combine(sLocation, "s.dat");
        public static string ConfigLocation = Path.Combine(sLocation, "config.json");
        //########################################################################
        //Credentials
        //########################################################################
#if(DEBUG)
        public static string CompanionApplicationPath = Path.Combine(ExternalBasePath, "CompanionApplication");
#else
        public static string CompanionApplicationPath = Path.Combine(BasePath, "CompanionApplication");
#endif
        //########################################################################
        //inside credential directory
        //########################################################################
        public static string CompressedCompanionApplicationPath = Path.Combine(CompanionApplicationPath, "Compressed");
        public static string UncompressedCompanionApplicationPath = Path.Combine(CompanionApplicationPath, "Uncompressed");
        
        //########################################################################
        //inside directory?
        //########################################################################

        //Need dll assembly


        //#################################################################

        //.exe paths --- I am not sure this is needed. Can just search for the process.        
        //public static string localPath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        public static string uploaderName ="FebrisBackgroundUploader.exe" ;// Path.Combine(SoftwareExtensionPaths, @"\Uploader\FebrisBackgroundUploader.exe");
        public static string downloaderName = "FebrisModuleManager.exe";// Path.Combine(SoftwareExtensionPaths, @"\ModuleDownloader\FebrisModuleManger.exe");//lol I spelled it wrong on the build
        public static string UploaderPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "FebrisBackgroundUploader.exe");
        public static string DownloaderPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "FebrisModuleManager.exe");
        public static string ProgressBarPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"Febris.ConsoleProgressBar.exe");       
        public static string screenRecorderPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"FebrisScreenRecorder.exe");        
    }
}
