// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Utilites;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.FileSystem
{
    public class FileManager
    {
        private ILogger _log;
        private IConfiguration _config;

        public FileManager(ILogger log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }

        public FileManager()
        {
        }

        //#########################################################
        // Get file directory content
        //#########################################################
        public async Task<List<string>> GetDirectoryContentNames(string path)
        {
            try
            {
                IEnumerable<string> files = Directory.EnumerateFileSystemEntries(path);
                List<string> fileNames = new List<string>();
                foreach (var file in files)
                {
                    string tempName = Path.GetFileName(file).ToString();
                    Console.WriteLine(tempName);
                    fileNames.Add(tempName);
                }
                return fileNames;
            }
            catch (Exception ex)
            {               


                _log.Information(ex.Message);
                //throw;
                return default;
            }
        }

        public static async Task<string> GetDirectoryApkFiles(string DirectoryPath)
        {
            string output = string.Empty;
            //bool rslt = false;
            //find enumerable list of directories
            //IEnumerable<string> folders = Directory.EnumerateDirectories(StaticDetails.zippedModulePath);
            //IEnumerable<string> files = Directory.EnumerateFiles(StaticDetails.zippedModulePath);
            try
            {
                //string[] allfiles = Directory.GetFiles(rootDirectory, "*.exe", SearchOption.AllDirectories);
                string[] allfiles = Directory.GetFiles(DirectoryPath, "*.apk", SearchOption.AllDirectories);
                output = Path.GetFileName(allfiles[0]);
                //foreach (string i in allfiles)
                //{
                //    if (i != "UnityCrashHandler64.exe")
                //    {
                //        moduleName = i;
                //        simulationFound = true;
                //        break;
                //    }
                //}
                //await CreateShortCut(StaticDetails.tempLinkName, StaticDetails.tempModuleName);
                //Task.Run(() => CreateShortCut(linkName, moduleName)).Wait();
                //return rslt;
                //FebrisLocalLibrary.Service.ProgressBarService.StopProgressBar(process);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //_log.LogError(ex.Message);
            }
            return output;
        }

        public async Task<List<string>> GetDirectoryContentWithFullPath(string directoryPath)
        {
            try
            {
                IEnumerable<string> files = Directory.EnumerateFileSystemEntries(directoryPath);
                List<string> fileNames = new List<string>();
                foreach (var file in files)
                {
                    string tempName = Path.GetFileName(file).ToString();
                    string tempFullPath = Path.Combine(directoryPath, tempName);                    
                    fileNames.Add(tempFullPath);
                }
                return fileNames;
            }
            catch (Exception ex)
            {


                _log.Information(ex.Message);
                //throw;
                return default;
            }
        }

        #region Get content
        //#########################################################
        // Get file directory content
        //#########################################################
        public string GetFileContent(string path, string name)
        {
            try
            {
                string fileName = Path.Combine(path, name);
                string content = File.ReadAllText(fileName);
                return content;
            }
            catch (Exception ex)
            {
                // _log.Information(ex.Message);
                return string.Empty;
                //throw;
            }
        }
        //#########################################################
        // Get file directory content
        //#########################################################
        public string GetFileContent(string path)
        {
            try
            {
                string content = File.ReadAllText(path);
                return content;
            }
            catch (Exception ex)
            {
                _log.Information(ex.Message);
                return string.Empty;
                //throw;
            }
        }

        public static FileStream OutgoingFileStream(string path)
        {
            try
            {
                FileStream stream = new FileStream(path, FileMode.Open);

                return stream;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public static byte[] OutgoingFileData(string path)
        {
            try
            {
                byte[] fileContent = File.ReadAllBytes(path);
                return fileContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        #endregion

        #region Set content
        public bool Set(JObject input, string path)
        {
            bool dataWritten = false;
            using (StreamWriter file = new StreamWriter(path))
            {
                try
                {
                    string statementString = SerializeString(input);
                    //file.Write(SerializeString(statementString));
                    file.Write(statementString);
                    //SerializeString(statement);
                    dataWritten = true;
                }
                catch (Exception ex)
                {
                    //_log.Error("WriteToDataFile Error: " + ex.Message);
                    throw;
                }
            }
            return dataWritten;
        }
        public bool Set(string input, string path)
        {
            bool dataWritten = false;
            using (StreamWriter file = new StreamWriter(path))
            {
                try
                {
                    //file.Write(SerializeString(statementString));
                    file.Write(input);
                    //SerializeString(statement);
                    dataWritten = true;
                }
                catch (Exception ex)
                {
                    //_log.Error("WriteToDataFile Error: " + ex.Message);
                    throw;
                }
            }
            return dataWritten;
        }
        #endregion

        #region miss
        public static string SerializeString(JObject jObject)
        {
            string outputString = string.Empty;
            try
            {
                outputString = JsonConvert.SerializeObject(jObject);
            }
            catch (Exception ex)
            {
                //_log.Error("SerializeString Error: " + ex.Message);
            }
            return outputString;
        }



        public static async Task<JObject> ChangeToObject(string inputString)
        {
            JObject jObject = new JObject();
            try
            {
                jObject = JObject.Parse(inputString);
            }
            catch (Exception ex)
            {
                //_log.Error("ChangeToObject Error: " + ex.Message);
            }
            return jObject;
        }

        #endregion


        /// <summary>
        /// This is all of the sections strait from the compaion area
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>

        #region unusable
        //public List<string> GetDirectoryContentNames(string path)
        //{
        //    try
        //    {
        //        IEnumerable<string> files = Directory.EnumerateFileSystemEntries(path);
        //        List<string> fileNames = new List<string>();
        //        foreach (var file in files)
        //        {
        //            string tempName = Path.GetFileName(file).ToString();
        //            Console.WriteLine(tempName);
        //            fileNames.Add(tempName);
        //        }
        //        return fileNames;
        //    }
        //    catch (Exception ex)
        //    {
        //        //_log.Information(ex.Message);
        //        //throw;
        //        return null;
        //    }
        //}

        #region File Content work
        //#########################################################
        // Get file directory content
        //#########################################################
        //public string GetFileContent(string path, string name)
        //{
        //    try
        //    {
        //        string fileName = Path.Combine(path, name);
        //        string content = File.ReadAllText(fileName);
        //        return content;
        //    }
        //    catch (Exception ex)
        //    {
        //        //_log.Information(ex.Message);
        //        return null;
        //        //throw;
        //    }
        //}
        ////#########################################################
        //// Get file directory content
        ////#########################################################
        //public string GetFileContent(string path)
        //{
        //    try
        //    {
        //        string content = File.ReadAllText(path);
        //        return content;
        //    }
        //    catch (Exception ex)
        //    {
        //        //_log.Information(ex.Message);
        //        return null;
        //        //throw;
        //    }
        //}

        //public static FileStream OutgoingFileStream(string path)
        //{
        //    try
        //    {
        //        //FileStream stream = File.ReadAllText(path);
        //        byte[] fileContent = File.ReadAllBytes(path);
        //        //FileStream stream = File.Open(path, FileMode.Open,FileAccess.ReadWrite);
        //        FileStream stream = new FileStream(path, FileMode.Open);

        //        return stream;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        Console.WriteLine(ex.Message);
        //        throw;
        //    }
        //}
        //public static byte[] OutgoingFileData(string path)
        //{
        //    try
        //    {
        //        byte[] fileContent = File.ReadAllBytes(path);
        //        return fileContent;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        Console.WriteLine(ex.Message);
        //        throw;
        //    }
        //}



        #endregion

        #region Set content
        //public bool Set(JObject input, string path)
        //{
        //    bool dataWritten = false;
        //    using (StreamWriter file = new StreamWriter(path))
        //    {
        //        try
        //        {
        //            string statementString = SerializeString(input);
        //            //file.Write(SerializeString(statementString));
        //            file.Write(statementString);
        //            //SerializeString(statement);
        //            dataWritten = true;
        //        }
        //        catch (Exception ex)
        //        {
        //            //_log.Error("WriteToDataFile Error: " + ex.Message);
        //            throw;
        //        }
        //    }
        //    return dataWritten;
        //}
        //public bool Set(string input, string path)
        //{
        //    bool dataWritten = false;
        //    using (StreamWriter file = new StreamWriter(path))
        //    {
        //        try
        //        {
        //            //file.Write(SerializeString(statementString));
        //            file.Write(input);
        //            //SerializeString(statement);
        //            dataWritten = true;
        //        }
        //        catch (Exception ex)
        //        {
        //            //_log.Error("WriteToDataFile Error: " + ex.Message);
        //            throw;
        //        }
        //    }
        //    return dataWritten;
        //}
        #endregion

        #region miss
        //public static string SerializeString(JObject jObject)
        //{
        //    string outputString = string.Empty;
        //    try
        //    {
        //        outputString = JsonConvert.SerializeObject(jObject);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        //_log.LogError(ex.Message);
        //    }
        //    return outputString;
        //}
        //public static JObject ChangeToObject(string inputString)
        //{
        //    JObject jObject = new JObject();
        //    try
        //    {
        //        jObject = JObject.Parse(inputString);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        //_log.LogError(ex.Message);
        //    }
        //    return jObject;
        //}

        #endregion





        #endregion



        #region File Content work       
        public static async Task<bool> DeleteFile(string path)
        {
            bool deleted = false;
            try
            {
                File.Delete(path);
                deleted = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return deleted;
        }
        #endregion



        public async Task<bool> MoveStatementFileToSent(string name)
        {
            try
            {
                string currentFileLocaiton = Path.Combine(FileSystem.StatementPath, name);
                string newFileLocation = Path.Combine(FileSystem.OldStatementPath, name);
                File.Move(currentFileLocaiton, newFileLocation);
                return true;
            }
            catch (Exception ex)
            {
                _log.Information(ex.Message);
                return false;
                //throw;
            }
        }


        #region directory handling
        //public bool ZipVideoFile(string fileName, string fullfilePath)
        //{
        //    bool rslt = false;
        //    //string filePath = StaticDetails.VideoPath;
        //    string filePath = FileSystem.VideoPath;
        //    //string zipFolderPath = StaticDetails.zipFolderPath;
        //    string zipFolderPath = FileSystem.zipFolderPath;
        //    string BaseFileName = Path.GetFileNameWithoutExtension(fileName);
        //    //var zipPath = Path.Combine(StaticDetails.VideoPath, BaseFileName + ".zip");            
        //    var zipPath = Path.Combine(FileSystem.VideoPath, BaseFileName + ".zip");
        //    //this creates the zip but puts nothing in it. 
        //    try
        //    {
        //        //create a folder
        //        string newFolderName = Path.Combine(filePath, BaseFileName);
        //        Directory.CreateDirectory(newFolderName);

        //        //move file to that folder
        //        string moveFileToNewFolder = Path.Combine(Path.Combine(filePath, newFolderName), fileName);
        //        string currentFileLocation = fullfilePath;
        //        bool movedFolder = MoveFolder(currentFileLocation, moveFileToNewFolder);
        //        if (movedFolder == false)
        //        {
        //            //somthing
        //        }

        //        //zip the folder
        //        //ZipFile.CreateFromDirectory(newFolderName, zipPath);
        //        //string[] arrFiles = { fileName };


        //        //****************************This may be an issue at some point. it is not tested ***********************************
        //        ZipFile.CreateFromDirectory(fileName, Path.Combine(zipFolderPath, BaseFileName + ".zip"));




        //        //move to the zip folder
        //        //bool zipFileMoved = MoveFolder(zipPath, Path.Combine(zipFolderPath, BaseFileName + ".zip"));
        //        //if (zipFileMoved == true)
        //        //{
        //        //    //_ = DeleteSplitFiles();
        //        //    _ = DeleteFolders(newFolderName);
        //        //}
        //        //if (fileZipped)
        //        //{
        //            //_ = DeleteSplitFiles();
        //            _ = DeleteFolders(newFolderName);
        //        //}


        //        rslt = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        //_log.LogError(ex.Message);
        //    }

        //    return rslt;
        //}

        //public bool ZipVideoFile(string fileName)//, object p)
        //{
        //    bool rslt = false;
        //    //string filePath = StaticDetails.VideoPath;
        //    string filePath = FileSystem.VideoPath;
        //    //string zipFolderPath = StaticDetails.zipFolderPath;
        //    string zipFolderPath = FileSystem.zipFolderPath;
        //    string BaseFileName = Path.GetFileNameWithoutExtension(fileName);
        //    //var zipPath = Path.Combine(StaticDetails.VideoPath, BaseFileName + ".zip");            
        //    var zipPath = Path.Combine(FileSystem.VideoPath, BaseFileName + ".zip");
        //    //this creates the zip but puts nothing in it. 
        //    try
        //    {
        //        //create a folder
        //        string newFolderName = Path.Combine(filePath, BaseFileName);
        //        Directory.CreateDirectory(newFolderName);

        //        //move file to that folder
        //        string moveFileToNewFolder = Path.Combine(Path.Combine(filePath, newFolderName), fileName);
        //        string currentFileLocation = Path.Combine(FileSystem.RecordingsFilePath, fileName);
        //        bool movedFolder = MoveFolder(currentFileLocation, moveFileToNewFolder);
        //        if (movedFolder == false)
        //        {
        //            //somthing
        //        }


        //        //zip the folder
        //        ZipFile.CreateFromDirectory(newFolderName, zipPath);
        //        //move to the zip folder
        //        bool zipFileMoved = MoveFolder(zipPath, Path.Combine(zipFolderPath, BaseFileName + ".zip"));
        //        if (zipFileMoved == true)
        //        {
        //            _ = DeleteSplitFiles();
        //            _ = DeleteFolders(newFolderName);
        //        }


        //        rslt = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.Error(ex.Message);
        //    }

        //    return rslt;
        //}

        //public bool DeleteSplitFiles()
        //{
        //    bool rslt = false;

        //    try
        //    {
        //        //IEnumerable<string> splitFiles = Directory.EnumerateFileSystemEntries(StaticDetails.SplitFilePath);
        //        IEnumerable<string> splitFiles = Directory.EnumerateFileSystemEntries(FileSystem.SplitFilePath);
        //        foreach (var file in splitFiles)
        //        {
        //            File.Delete(file);
        //        }

        //        rslt = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.Error(ex.Message);
        //    }

        //    return rslt;
        //}

        //private static bool ZipThisFile(string[] arrFiles, string sZipToDirectory, string sZipFileName)
        //{
        //    if (Directory.Exists(sZipToDirectory))
        //    {
        //        FileStream fNewZipFileStream;
        //        ZipOutputStream zos;

        //        try
        //        {
        //            //fNewZipFileStream = File.Create(sZipToDirectory + sZipFileName);
        //            fNewZipFileStream = File.Create(Path.Combine(sZipToDirectory, sZipFileName));
        //            zos = new ZipOutputStream(fNewZipFileStream);

        //            for (int i = 0; i < arrFiles.Length; i++)
        //            {
        //                ZipEntry entry = new ZipEntry(arrFiles[i].Substring(arrFiles[i].LastIndexOf("/") + 1));
        //                zos.PutNextEntry(entry);

        //                byte[] fileContents = File.ReadAllBytes(arrFiles[i]);
        //                zos.Write(fileContents);
        //                zos.CloseEntry();
        //            }
        //            zos.Close();
        //            fNewZipFileStream.Close();
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.StackTrace);
        //            string sErr = ex.Message;
        //            return false;
        //        }
        //        finally
        //        {
        //            fNewZipFileStream = null;
        //            zos = null;
        //        }
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        public async Task<bool> MoveFolder(string FullCurrentPath, string FullNewPath)
        {
            bool rslt = false;
            try
            {
                File.Move(FullCurrentPath, FullNewPath);
                rslt = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //_log.LogError(ex.Message);
            }
            return rslt;
        }

        public async Task<bool> DeleteDirectory(string FullFileName)
        {
            bool rslt = false;

            try
            {
                Directory.Delete(FullFileName, true);

                rslt = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //_log.LogError(ex.Message);
            }

            return rslt;
        }
        #endregion

        #region miss

        public static async Task<bool> DeleteOldFiles()
        {
            FileManager _fileManager = new FileManager();

            bool filesRemoved = false;
            List<string> oldStatementNames = new List<string>();
            List<string> oldVideoFileNames = new List<string>();

            try
            {
                oldStatementNames = await _fileManager.GetDirectoryContentNames(FileSystem.OldStatementPath);
            }
            catch { }
            try
            {
                oldVideoFileNames = await _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);
            }
            catch { }

            int oldStatementFileCount = oldStatementNames.Count();
            int oldVideoFileCount = oldVideoFileNames.Count();

            foreach (var i in oldStatementNames)
            {
                try
                {
                    bool deleted = await DeleteFile(Path.Combine(FileSystem.OldStatementPath, i));
                    if (deleted)
                    {
                        oldStatementFileCount -= 1;
                    }
                }
                catch { }
            }

            foreach (var i in oldVideoFileNames)
            {
                try
                {
                    bool deleted = await DeleteFile(Path.Combine(FileSystem.zipFolderPath, i));
                    if (deleted)
                    {
                        oldVideoFileCount -= 1;
                    }
                }
                catch { }
            }


            if (oldStatementFileCount == 0 && oldVideoFileCount == 0)
            {
                filesRemoved = true;
            }

            return filesRemoved;
        }

        #endregion

        public async Task<bool> FileUnzipper(string zipFilePath, string unzipFolderPath, string linkName)
        {

            bool unzipped = false;
            //Process process = FebrisLocalLibrary.Service.ProgressBarService.StartProgressBar(zipFile, FebrisLocalLibrary.Enums.StatusType.Processing);
            try
            {
                string zipPath = zipFilePath;                
                //check if the file exists before unzipping
                unzipped = Directory.Exists(Path.Combine(unzipFolderPath));
                if (unzipped)
                {
                    return unzipped;
                }

                // Module packages are attacker-supplied archives. Extract through the
                // hardened path (zip-slip + zip-bomb guards), never the raw framework call.
                SafeZipExtractor.ExtractToDirectory(zipPath, unzipFolderPath);

                unzipped = Directory.Exists(unzipFolderPath);                                

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                
                ///maybe add that the file should be deleted here?

                //return false;
            }
            //FebrisLocalLibrary.Service.ProgressBarService.StopProgressBar(process);
            return unzipped;

        }

        

        public async Task<bool> FindSimulationApplication(string rootDirectory, string moduleName, string linkName)
        {
            //Process process = FebrisLocalLibrary.Service.ProgressBarService.StartProgressBar("Simulation", FebrisLocalLibrary.Enums.StatusType.Installing);           

            bool simulationFound = false;
            //bool rslt = false;
            //find enumerable list of directories
            //IEnumerable<string> folders = Directory.EnumerateDirectories(StaticDetails.zippedModulePath);
            //IEnumerable<string> files = Directory.EnumerateFiles(StaticDetails.zippedModulePath);
            try
            {
                //string[] allfiles = Directory.GetFiles(rootDirectory, "*.exe", SearchOption.AllDirectories);
                string[] allfiles = Directory.GetFiles(rootDirectory, "*.apk", SearchOption.AllDirectories);
                foreach (string i in allfiles)
                {
                    if (i != "UnityCrashHandler64.exe")
                    {
                        moduleName = i;
                        simulationFound = true;
                        break;
                    }
                }
                //await CreateShortCut(StaticDetails.tempLinkName, StaticDetails.tempModuleName);
                Task.Run(() => CreateShortCut(linkName, moduleName)).Wait();
                //return rslt;
                //FebrisLocalLibrary.Service.ProgressBarService.StopProgressBar(process);
            }
            catch (Exception ex)
            {
                //_log.LogError(ex.Message);
            }
            return simulationFound;
        }

        public async Task<bool> CreateShortCut(string linkName, string targetFilePath)
        {
            bool shortCutCreated = false;
            try
            {
                //targetFilePath = Path.Combine(StaticDetails.ModulePath, targetFilePath);
                targetFilePath = Path.Combine(FileSystem.ModulePath, targetFilePath);

                //string shortcutLocation = Path.Combine(StaticDetails.ModuleLinkPath, linkName + ".lnk");
                string shortcutLocation = Path.Combine(FileSystem.ModuleLinkPath, linkName + ".lnk");


                //WshShell shell = new WshShell();
                //IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
                //shortcut.TargetPath = targetFilePath;
                //shortcut.Save();
                shortCutCreated = true;
            }
            catch (Exception ex)
            {
                //_log.LogError(ex.Message);
            }
            return shortCutCreated;
        }

        public static async Task<string> FindAPKApplication(string directoryPath)//, string moduleName, string linkName)
        {
            string apkPath = string.Empty;
            try
            {
                string[] allfiles = Directory.GetFiles(directoryPath, "*.apk", SearchOption.AllDirectories);
                //string fileName = string.Empty;
                foreach (string i in allfiles)
                {
                    apkPath = i;                    
                    break;
                }
                if (string.IsNullOrEmpty(apkPath)) { throw new Exception(); }

                return apkPath;
            }
            catch (Exception ex)
            {
                //_log.LogError(ex.Message);
                throw;
            }
            
        }
    }
}
