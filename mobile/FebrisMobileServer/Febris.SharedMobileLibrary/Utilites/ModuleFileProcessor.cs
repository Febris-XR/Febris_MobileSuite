// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Utilites
{
    class ModuleFileProcessor
    {
        #region zipped file system
        public bool FileUnzipper(string zipFile, Guid module, string linkName)
        {
            bool unzipped = false;
            //Process process = FebrisLocalLibrary.Service.ProgressBarService.StartProgressBar(zipFile, FebrisLocalLibrary.Enums.StatusType.Processing);
            try
            {
                string zipPath = zipFile;
                // Normalizes the path.
                //string extractPath = Path.GetFullPath(StaticDetails.ModulePath);
                string extractPath = Path.GetFullPath(FileSystem.FileSystem.ModulePath);
                string destinationPath = string.Empty;

                destinationPath = Path.GetFullPath(Path.Combine(extractPath, Path.GetFileNameWithoutExtension(zipFile)));

                // Module packages are attacker-supplied archives. Extract through the
                // hardened path (zip-slip + zip-bomb guards), never the raw framework call.
                SafeZipExtractor.ExtractToDirectory(zipPath, destinationPath);
                unzipped = true;

                //await RemoveOldEditions(StaticDetails.tempLinkName);
                Task.Run(() => FindSimulationApplication(destinationPath, module.ToString(), linkName)).Wait();
                //await FindSimulationApplication(destinationPath).Wait();

            }
            catch (Exception e)
            {
                // Log so a rejected (malicious or corrupt) package is not silently swallowed.
                Serilog.Log.Error(e, "ModuleFileProcessor.FileUnzipper: extraction failed or was rejected by SafeZipExtractor");
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
                ///.apk, .xapk, .apkm, and for shell and terminal .sh and .cmd
                string[] allfiles = Directory.GetFiles(rootDirectory, "*.apk", SearchOption.AllDirectories);

                foreach (string i in allfiles)
                {
                    if (i != "UnityCrashHandler64.apk")
                    {
                        moduleName = i;
                        simulationFound = true;
                        break;
                    }
                }
                //await CreateShortCut(StaticDetails.tempLinkName, StaticDetails.tempModuleName);
                //Task.Run(() => CreateShortCut(linkName, moduleName)).Wait();
                //return rslt;
                //FebrisLocalLibrary.Service.ProgressBarService.StopProgressBar(process);
            }
            catch (Exception ex)
            {
                //_log.LogError(ex.Message);
            }
            return simulationFound;
        }


        //https://stackoverflow.com/questions/59685804/android-packageinstaller-not-installing-apk/61889386#61889386
        //private static void AddApkToInstallSession(Context context, Android.Net.Uri apkUri, PackageInstaller.Session session)
        //{
        //    var packageInSession = session.OpenWrite("package", 0, -1);
        //    var input = context.ContentResolver.OpenInputStream(apkUri);

        //    try
        //    {
        //        if (input != null)
        //        {
        //            input.CopyTo(packageInSession);
        //        }
        //        else
        //        {
        //            throw new Exception("Inputstream is null");
        //        }
        //    }
        //    finally
        //    {
        //        packageInSession.Close();
        //        input.Close();
        //    }

        //    //That this is necessary could be a Xamarin bug.
        //    GC.Collect();
        //    GC.WaitForPendingFinalizers();
        //    GC.Collect();
        //}



        //public bool CreateShortCut(string linkName, string targetFilePath)
        //{
        //    bool shortCutCreated = false;
        //    try
        //    {
        //        //targetFilePath = Path.Combine(StaticDetails.ModulePath, targetFilePath);
        //        targetFilePath = Path.Combine(FileSystem.FileSystem.ModulePath, targetFilePath);

        //        //string shortcutLocation = Path.Combine(StaticDetails.ModuleLinkPath, linkName + ".lnk");
        //        string shortcutLocation = Path.Combine(FileSystem.FileSystem.ModuleLinkPath, linkName + ".lnk");

        //        WshShell shell = new WshShell();
        //        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

        //        shortcut.TargetPath = targetFilePath;
        //        shortcut.Save();
        //        shortCutCreated = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        //_log.LogError(ex.Message);
        //    }
        //    return shortCutCreated;
        //}


        #endregion



        #region SharpZipLib https://stackoverflow.com/questions/42118378/how-to-unzip-downloaded-zip-file-in-xamarin-forms
        //private async Task<bool> UnzipFileAsync(string zipFilePath, string unzipFolderPath)
        //{
        //    try
        //    {
        //        var entry = new ZipEntry(Path.GetFileNameWithoutExtension(zipFilePath));
        //        var fileStreamIn = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read);
        //        var zipInStream = new ZipInputStream(fileStreamIn);
        //        entry = zipInStream.GetNextEntry();
        //        while (entry != null && entry.CanDecompress)
        //        {
        //            var outputFile = unzipFolderPath + @"/" + entry.Name;
        //            var outputDirectory = Path.GetDirectoryName(outputFile);
        //            if (!Directory.Exists(outputDirectory))
        //            {
        //                Directory.CreateDirectory(outputDirectory);
        //            }

        //            if (entry.IsFile)
        //            {
        //                var fileStreamOut = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
        //                int size;
        //                byte[] buffer = new byte[4096];
        //                do
        //                {
        //                    size = await zipInStream.ReadAsync(buffer, 0, buffer.Length);
        //                    await fileStreamOut.WriteAsync(buffer, 0, size);
        //                } while (size > 0);
        //                fileStreamOut.Close();
        //            }

        //            entry = zipInStream.GetNextEntry();
        //        }
        //        zipInStream.Close();
        //        fileStreamIn.Close();
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //    return true;
        //}
        #endregion
    }
}
