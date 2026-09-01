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
    public class CompressionHelper
    {
        #region zipped file system
        public async static Task<bool> Decompress(string compressedPath, string outputPath, string outputFileName)
        {
            bool unzipped = false;
            //Process process = FebrisLocalLibrary.Service.ProgressBarService.StartProgressBar(zipFile, FebrisLocalLibrary.Enums.StatusType.Processing);
            try
            {
                //string compressedPath = compressedPath;
                // Normalizes the path.
                //string extractPath = Path.GetFullPath(StaticDetails.ModulePath);
                //string extractPath = Path.GetFullPath(outputPath);
                //string UncompressedPath = string.Empty;
                string UncompressedPath = Path.Combine(outputPath, outputFileName);
                //destinationPath = Path.GetFullPath(Path.Combine(extractPath, Path.GetFileNameWithoutExtension(InputPath)));
                //FileSystem.FileSystemInitalizer.CreateFileDirectory(extractPath, outputFileName);
                // Module packages are attacker-supplied archives. Extract through the
                // hardened path (zip-slip + zip-bomb guards), never the raw framework call.
                SafeZipExtractor.ExtractToDirectory(compressedPath, UncompressedPath);
                unzipped = true;

                //await RemoveOldEditions(StaticDetails.tempLinkName);
                //Task.Run(() => FindSimulationApplication(destinationPath, module.ToString(), outputPath)).Wait();
                //await FindSimulationApplication(destinationPath).Wait();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                //Log.LogError(e.StackTrace);
            }
            //FebrisLocalLibrary.Service.ProgressBarService.StopProgressBar(process);
            return unzipped;
        }

        public async static Task<string> SearchForMatchingFileType(string extensionType, string directoryPath) 
        {
            string output = string.Empty;            
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("*");
                sb.Append(extensionType);

                string[] allfiles = Directory.GetFiles(directoryPath, sb.ToString(), SearchOption.AllDirectories);

                string finalFile = string.Empty;
                foreach (string i in allfiles)
                {
                    if (i != "UnityCrashHandler64.apk")
                    {
                        finalFile = i;
                        break;
                    }
                }

                output = Path.Combine(directoryPath, finalFile);
            }
            catch (Exception e)
            {
                
                //Log.LogError(e.Message);
            }
            //FebrisLocalLibrary.Service.ProgressBarService.StopProgressBar(process);
            return output;
        }

        


        #endregion
    }
}
