// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Febris.AdbLibrary.AdbLib;
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.Droid.Utilities;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.Droid.Networking.USB
{
    class USBRequestCreator
    {
        private LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.Networking.USB.USBRequestCreator");

        //public static async Task<byte[]> PushRequestBuilder(string input)
        //{
        //    LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris");
        //    FileManager _fileManager = new FileManager();
        //    string _finalFilePath = string.Empty;
        //    //Local
        //    try
        //    {
        //        //Check for all files containing this uuid
        //        List<string> listOfFiles = _fileManager.GetDirectoryContentNames(Febris.SharedMobileLibrary.FileSystem.FileSystem.BasePath);
        //        List<string> filteredList = listOfFiles.Where(i => i.Contains(input)).ToList();
        //        _finalFilePath = ApkFileSearch(_finalFilePath, filteredList);
        //        if (!string.IsNullOrEmpty(_finalFilePath))
        //        {
        //            if (filteredList.Contains(input + ".apk"))
        //            {

        //            }
        //            else if (filteredList.Contains(input + ".zip"))
        //            {
        //                string zippedFilePath = System.IO.Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BasePath, input + ".zip");
        //                string unzippedFilePath = System.IO.Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BasePath, input);
        //                bool processed = _fileManager.FileUnzipper(zippedFilePath, unzippedFilePath, string.Empty).Result;
        //                if (processed)
        //                {
        //                    _finalFilePath = ApkFileSearch(_finalFilePath, filteredList);
        //                }
        //                else
        //                {
        //                    PairingPageStatusHelper.GenericMessage("There was an issue unpacking the compressed software file");
        //                }
        //            }
        //            else
        //            {
        //                PairingPageStatusHelper.GenericMessage("The correct companion software is not currently on this device. It needs to be downloaded before it can be installed");
        //                return default;
        //            }
        //        }
        //        if (string.IsNullOrEmpty(_finalFilePath))
        //        {
        //            PairingPageStatusHelper.GenericMessage("An unknown issue has occured with the software file in question and it cannot be found or processed");
        //            return default;
        //        }



        //        byte[] dataPackage = default;
        //        byte[] filePackage = FileManager.OutgoingFileData(_finalFilePath);
        //        string stream = "sync:";
        //        string sendId = "SEND";
        //        string mode = ",33206";

        //        byte[] streamArray = ConversionHelpers.String2Bytes(stream);
        //        byte[] sendIdArray = ConversionHelpers.String2Bytes(sendId);
        //        byte[] modeArray = ConversionHelpers.String2Bytes(mode);


        //        //combine arrays
        //        dataPackage = ConversionHelpers.ArrayCombinerBuilder(streamArray, sendIdArray);
        //        dataPackage = ConversionHelpers.ArrayCombinerBuilder(dataPackage, filePackage);
        //        dataPackage = ConversionHelpers.ArrayCombinerBuilder(dataPackage, modeArray);

        //        return dataPackage;
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured in USBRequestCreator RequestBuilder: " + ex.StackTrace); }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in USBRequestCreator RequestBuilder: " + ex.StackTrace);
        //    }
        //    return default;
        //}
        //public static async Task<byte[]> NewestLocalPackageInstallRequestBuilder(string input)

        /// <summary>
        /// THIS IS A BAD REPEATED METHOD!!!!!!!!!!!!!!!!!!!!! - GATHER INFO ON BUTTON CLICK AND PASS IT DOWN. NO EVENT NEEDED.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        //public static async Task<string> NewestLocalPackageInstallRequestBuilder(string input)
        //{
        //    LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris");
        //    FileManager _fileManager = new FileManager();
        //    string _finalFilePath = string.Empty;
        //    //Local
        //    try
        //    {
        //        //Check for all files containing this uuid
        //        List<string> listOfFiles = _fileManager.GetDirectoryContentNames(FileSystem.UncompressedCompanionApplicationPath);
        //        List<string> filteredList = listOfFiles.Where(i => i.Contains(input)).ToList();
        //        _finalFilePath = ApkFileSearch(_finalFilePath, filteredList);
        //        if (!string.IsNullOrEmpty(_finalFilePath))
        //        {
        //            //if (filteredList.Contains(input + ".apk"))
        //            //{

        //            //}
        //            //else 
        //            if (filteredList.Contains(input + ".zip"))
        //            {
        //                string zippedFilePath = System.IO.Path.Combine(FileSystem.BasePath, input + ".zip");
        //                string unzippedFilePath = System.IO.Path.Combine(FileSystem.BasePath, input);
        //                bool processed = _fileManager.FileUnzipper(zippedFilePath, unzippedFilePath, string.Empty).Result;
        //                if (processed)
        //                {
        //                    _finalFilePath = ApkFileSearch(_finalFilePath, filteredList);
        //                }
        //                else
        //                {
        //                    PairingPageStatusHelper.GenericMessage("There was an issue unpacking the compressed software file");
        //                }
        //            }
        //            else
        //            {
        //                PairingPageStatusHelper.GenericMessage("The correct companion software is not currently on this device. It needs to be downloaded before it can be installed");
        //                return default;
        //            }
        //        }
        //        if (string.IsNullOrEmpty(_finalFilePath))
        //        {
        //            PairingPageStatusHelper.GenericMessage("An unknown issue has occured with the software file in question and it cannot be found or processed");
        //            return default;
        //        }
                                
        //        return _finalFilePath;                
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured in USBRequestCreator RequestBuilder: " + ex.StackTrace); }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in USBRequestCreator RequestBuilder: " + ex.StackTrace);
        //    }
        //    return default;
        //}

        //private static string ApkFileSearch(string _finalFilePath, List<string> filteredList)
        //{
        //    LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris");
        //    try
        //    {
        //        foreach (var i in filteredList)
        //        {
        //            if (Path.GetExtension(i) != ".zip")
        //            {
        //                string tempPath = System.IO.Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BasePath, i);
        //                string apkFileIndirectory = FileManager.GetDirectoryApkFiles(tempPath);

        //                if (!string.IsNullOrEmpty(apkFileIndirectory))
        //                {
        //                    _finalFilePath = Path.Combine(Febris.SharedMobileLibrary.FileSystem.FileSystem.BasePath, apkFileIndirectory);
        //                    break;
        //                }
        //            }

        //        }

        //        return _finalFilePath;
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured in USBRequestCreator apkFileSearch: " + ex.StackTrace); }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in USBRequestCreator apkFileSearch: " + ex.StackTrace);
        //    }
        //    return default;

        //}

        /// <summary>
        /// Need to unzip the apk then read all of the bytes of that package and send it.
        /// </summary>
        /// <returns></returns>
        //public async Task<byte[]> GatherLocalSoftwareDataPackage()
        //{
        //    try
        //    {
        //        //byte[] arguments = await _moduleContext.GetFileContent(j);
        //        byte[] dataPackage = default;

        //        //string stream = "sync:";
        //        //string sendId = "SEND";
        //        //string mode = ",33206";





        //        return dataPackage;
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured in USBRequestCreator RequestBuilder: " + ex.StackTrace); }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in USBRequestCreator RequestBuilder: " + ex.StackTrace);
        //    }
        //    return default;
        //}

    }
}