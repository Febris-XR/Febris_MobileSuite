// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using Android.App;
//using Android.Content;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;
//using Febris.MobileServerV3.Droid.Utilities;
//using Febris.SharedMobileLibrary.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//[assembly: Xamarin.Forms.Dependency(typeof(ExternalPlatformFileSystem))]
//namespace Febris.MobileServerV3.Droid.Utilities
//{
//    public class ExternalPlatformFileSystem : IExternalPlatformFileSystem
//    {
//        public static string _publicFileName = "com.febris.public_files";

//        public async Task<string> GetBaseDirectoryPath()
//        {
//            try
//            {
//                var SDK = Build.VERSION.SdkInt;

//                string output = string.Empty;
//                string baseDirectory = string.Empty;
//                string publicFileName = string.Empty;
//                string basePath = string.Empty;


//                if (SDK <= BuildVersionCodes.Q)
//                {
//                    baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
//                    publicFileName = _publicFileName;
//                    basePath = Path.Combine(baseDirectory, publicFileName);
//                }
//                else if (SDK > BuildVersionCodes.Q)
//                {
//                    //baseDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath;
//                    //baseDirectory = Android.App.Application.Context.get.GetExternalFilesDir(string.Empty).AbsolutePath;
//                    //baseDirectory = Android.App.Application.Context.GetExternalFilesDir(string.Empty).AbsolutePath;
//                    //baseDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath;//Android.OS.Environment.DataDirectory.AbsolutePath;//.DirectoryDocuments;//.AbsolutePath;
//                    //baseDirectory = Android.OS.Environment.DirectoryDocuments;
//                    //baseDirectory = Android.OS.Environment.StorageDirectory.AbsolutePath;
//                    baseDirectory = Android.OS.Environment.StorageDirectory.AbsolutePath;
//                    publicFileName = _publicFileName;
//                    basePath = Path.Combine(baseDirectory, publicFileName);
//                }
//                //string output = string.Empty;
//                //string baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
//                //string publicFileName = _publicFileName;
//                ///This creates a file. I need a folder
//                Java.IO.File baseFile = new Java.IO.File(baseDirectory, publicFileName);
//                bool newFileCreated = baseFile.Mkdir();
//                if (newFileCreated)
//                {
//                    Console.WriteLine("new file created with path " + baseFile.AbsolutePath);
//                }
//                output = baseFile.AbsolutePath;

//                //}
//                //output = baseDirectory.CreateNewFile(publicFileName);

//                return output;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                throw;
//            }
//        }

//        public async Task<string> CreateFile(string input)
//        {
//            try
//            {
//                string output = string.Empty;
//                string baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
//                string publicFileName = _publicFileName;
//                string basePath = Path.Combine(baseDirectory, publicFileName);

//                Java.IO.File baseFile = new Java.IO.File(basePath, input);
//                bool newFileCreated = baseFile.CreateNewFile();
//                if (newFileCreated)
//                {
//                    Console.WriteLine("new file created with path " + baseFile.AbsolutePath);
//                }
//                output = baseFile.AbsolutePath;

//                return output;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                throw;
//            }
//        }

//        public async Task<string> CreateDirectory(string input)
//        {
//            try
//            {
//                string output = string.Empty;
//                string baseDirectory = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
//                string publicFileName = _publicFileName;
//                string basePath = Path.Combine(baseDirectory, publicFileName);

//                Java.IO.File baseFile = new Java.IO.File(basePath, input);
//                bool newFileCreated = baseFile.Mkdir();
//                if (newFileCreated)
//                {
//                    Console.WriteLine("new file created with path " + baseFile.AbsolutePath);
//                }
//                output = baseFile.AbsolutePath;

//                return output;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                throw;
//            }
//        }

//        public async Task<object> GetPlatformSpecificFile()
//        {
//            try
//            {
//                bool output = false;


//                return output;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.StackTrace);
//                throw;
//            }
//        }
//    }
//}