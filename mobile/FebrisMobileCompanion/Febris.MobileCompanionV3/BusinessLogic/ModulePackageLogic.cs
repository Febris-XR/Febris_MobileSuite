// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.DataLogic;
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.SharedMobileLibrary.Models.EventArguments;
using Febris.ModelLibrary.Models.XApiModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

/// <summary>
/// This is the combination of PackageLogic and ModuleLogic. 
/// 
/// Created to handle the issues caused by Android 11+ and to handle support.
/// </summary>
namespace Febris.MobileCompanionV3.BusinessLogic
{
    public class ModulePackageEvents
    {
        private ModulePackageLogic _context = new ModulePackageLogic();

        //private ModuleLogic _moduleContext;


        /// <summary>
        /// This is from the end of the InstallModule method from ModulePackageUtility.Droid
        /// 
        /// It originates from the ModuleIndexUpdate(moduleName, packageInfo)
        /// 
        /// *****I think this package could do a little more heavy lifting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal async void ModuleIndexUpdateAction(object sender, ModuleEventArgs e)
        {
            try
            {
                //instead send this to the bll so it can be filtered instead of just shooting from the hip like this. 
                #region Post Android 11
                await _context.ModuleIndexUpdateAction(e);


                //long modId = _context.ModuleUpdate.Post(module);
                //module = _context.Get(modId);
                //data = _context.Get(data).Result;
                //Console.WriteLine(data);
                #endregion
                #region pre-Android 11
                //Dictionary<string, string> output = _packageContext.Update(e.ModuleId, e.PackageName).Result;
                //Console.WriteLine(output);
                #endregion




                //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

    }
    public class ModulePackageLogic
    {
        private ModulePackageContext _logicContext;
        private ModuleFileContext _moduleFileContext = new ModuleFileContext();
        IModulePackageUtility _packageUtility;

        public ModulePackageLogic()
        {
            _packageUtility = DependencyService.Get<IModulePackageUtility>();
            _logicContext = new ModulePackageContext();
        }

        #region For UI
        public async Task<List<string>> GetNameIndex()
        {
            List<string> output = new List<string>();
            try
            {
                output = await _logicContext.GetModuleNameIndex();//.GetAppNameList();--was gathering installed applications not files. 
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        public async Task<List<ModulePackageModel>> GetIndex()
        {
            List<ModulePackageModel> output = new List<ModulePackageModel>();
            try
            {
                output = await _logicContext.GetAppList();

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }



        public async Task<ModulePackageModel> Get(ModulePackageModel input)
        {
            ModulePackageModel output = default;
            try
            {
                if (input.Id != default)
                {
                    output = await _logicContext.Get(input.Id);
                }
                else if (input.UUID != default)
                {
                    output = await _logicContext.Get(input.UUID);
                }
                else if (input.UriString != default)
                {
                    output = await _logicContext.Get(input.UriString);
                }
                else if (input.Module.Id != default)
                {
                    output = await _logicContext.GetByModule(input.Module.Id);
                }
                else if (input.Module.UUID != default)
                {
                    output = await _logicContext.GetByModule(input.Module.UUID);
                }
                else if (input.Module?.Name != default)
                {
                    output = await _logicContext.GetByModule(input.Module.Name);
                }
                else if (input.ModuleFile.Id != default)
                {
                    output = await _logicContext.GetByModuleFile(input.ModuleFile.Id);
                }
                else if (input.ModuleFile.UUID != default)
                {
                    output = await _logicContext.GetByModuleFile(input.ModuleFile.UUID);
                }
                else if (input.ModuleFile?.FilePath != default)
                {
                    output = await _logicContext.GetByModuleFile(input.ModuleFile.FilePath);
                }
                else
                {
                    return default;
                }

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<List<string>> GetUncompressedModuleNameIndex()
        {
            List<string> output = new List<string>();
            try
            {
                List<ModuleFileModel> itemList = await _moduleFileContext.GetAllUncompressed();//.GetAppList();
                output = itemList.Select(i => i.ModuleDirectoryName).ToList() ?? default;
                //output = itemList
                //    .Where(i => i.ModuleFile != null && i.ModuleFile.Compressed==false)//default == false)
                //    .Select(i => i.ModuleDirectoryName)
                //    .ToList() ?? default;

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<List<string>> GetCompressedModuleNameIndex()
        {
            List<string> output = new List<string>();
            try
            {
                List<ModuleFileModel> itemList = await _moduleFileContext.GetAllCompressed();//.GetAppList();
                output = itemList.Select(i => i.ModuleDirectoryName).ToList() ?? default;
                //List<ModulePackageModel> itemList = await _logicContext.GetAppList();
                //output = itemList
                //    .Where(i => i.CompressedModuleFile != null && i.CompressedModuleFile.Compressed == true)
                //    //.Where(i => i.ModuleFile?.Compressed??default == true)
                //    .Select(i => i.ModuleDirectoryName)//.Module?.Name)
                //    .ToList() ?? default;

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        public async Task<List<string>> GetInstalledPackageNameIndex()
        {
            List<string> output = new List<string>();
            try
            {
                //output = await _logicContext.GetAppNameList();
                List<ModulePackageModel> itemList = await _logicContext.GetAppList();
                output = itemList
                    .Where(i => i.Installed)
                    .Select(i => i.ModuleDirectoryName)//.Module.Name)
                    .ToList();

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        #endregion

        #region For Launching
        /// <summary>
        /// For launching modules. Could do something a little cleaner like running from the saved Uri
        /// </summary>
        /// <param name="ModuleId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal async Task<bool> LaunchModule(string ModuleId, string data)
        {
            bool output = false;
            try
            {
                ModulePackageModel item = await _logicContext.GetByModuleDirectioryName(ModuleId);//.Get(Guid.Parse(ModuleId));

                //await _packageUtility.RunModule(item.ModuleDirectoryName, data);
                await _packageUtility.RunModule(item.Name, data);//.ModuleDirectoryName, data);
                output = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return output;
        }
        #endregion

        #region For Backend (needs updating)
        internal async Task<ModulePackageModel> CreateCompressedModulePackage(PacketHeaderModel header, string fullPath)
        {
            ModulePackageModel output = default;
            try
            {
                ModulePackageModel modulePackage = new ModulePackageModel
                {

                    Module = new Module
                    {
                        Name = header.PacketName
                    },
                    CompressedModuleFile = new ModuleFileModel
                    {
                        Compressed = true,
                        FilePath = fullPath,
                        ModuleDirectoryName = header.PacketName
                    },
                    UriString = default,
                    Installed = false,
                    ModuleDirectoryName = header.PacketName
                };


                output = await Create(modulePackage);
                return output;

            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Creating module DB info: " + ex.Message;
                throw;
            }
        }

        internal async Task<ModulePackageModel> CreateDecompressedModuleFile(string newFileNameandPath, ModulePackageModel data)
        {
            try
            {
                ModuleFileModel decompressedModuleFile = new ModuleFileModel()
                {
                    FilePath = newFileNameandPath,
                    Compressed = false,
                    ModuleDirectoryName = data.ModuleDirectoryName
                };
                decompressedModuleFile = await _moduleFileContext.Post(decompressedModuleFile);
                data.ModuleFile = decompressedModuleFile;
                data = await _logicContext.Update(data);
                return data;

            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Creating module DB info: " + ex.Message;
                //await DeleteCompressedModule(data.ModuleDirectoryName);
                await DeleteModule(data.ModuleDirectoryName);
                throw;
            }
        }
        //internal async Task<ModulePackageModel> DecompressedModuleDBUpdate(string newFileNameandPath, ModulePackageModel data)
        //{
        //    try
        //    {

        //        data.ModuleFile.FilePath = newFileNameandPath;
        //        data.ModuleFile.Compressed = false;
        //        data = await _logicContext.Update(data);
        //        return data;

        //    }
        //    catch (Exception ex)
        //    {
        //        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Creating module DB info: " + ex.Message;
        //        await DeleteModule(data.ModuleDirectoryName);
        //        throw;
        //    }
        //}
        private async Task<ModulePackageModel> Create(ModulePackageModel modulePackage)
        {
            ModulePackageModel output = default;
            try
            {
                bool alreadyExists = false;
                alreadyExists = await Exists(modulePackage);
                if (alreadyExists)
                {
                    output = await _logicContext.Update(modulePackage);
                }
                else
                {
                    output = await _logicContext.Post(modulePackage);
                }
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Creating module DB info: " + ex.Message;
                throw;
            }
        }

        private async Task<bool> Exists(ModulePackageModel modulePackage)
        {
            bool exists = false;
            try
            {
                exists = await _logicContext.ExistsByDirectoryName(modulePackage);
                //return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while checking for module package existance: " + ex.Message);
                //throw;
            }
            return exists;
        }

        //internal async Task<bool> InstallModule(string moduleName, string filePath)
        //{
        //    bool output = false;
        //    try
        //    {
        //        await _packageUtility.InstallModule(moduleName, filePath);
        //        output = true;
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error installing module: " + ex.Message;
        //        throw;
        //    }

        //}
        internal async Task<bool> InstallModule(string moduleName)
        {
            bool output = false;
            try
            {
                string fullFilePath = System.IO.Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModulePath, moduleName);
                //string filePath = SharedMobileLibrary.FileSystem.FileSystem.ModulePath;//System.IO.Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModulePath, moduleName);
                await _packageUtility.InstallModule(moduleName, fullFilePath);
                output = true;
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error installing module: " + ex.Message;
                throw;
            }

        }
        internal async Task<bool> InstallModule(string moduleName, string filePath, ModulePackageModel data)
        {
            bool output = false;
            try
            {
                await _packageUtility.InstallModule(moduleName, filePath);
                //data.Installed = true;
                //data.UriString = 
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error installing module: " + ex.Message;
                throw;
            }
        }
        internal async Task<bool> DeleteDBEntry(ModulePackageModel input)
        {
            bool output = false;
            try
            {
                output = await _logicContext.DeleteFromDb(input);
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Deleteing DB Entry module: " + ex.Message;
                throw;
            }
        }
        internal async Task<bool> DeleteModule(string packetName)
        {
            bool output = false;
            try
            {
                #region Post Android 11
                try
                {
                    ///Delete the first ModuleFileModel that has this packetName--needs to be implemented and is compressed
                    ModulePackageModel model = await _logicContext.GetUncompressedByModuleDirectioryName(packetName);//.GetByModuleDirectioryName(packetName);

                    if (model.ModuleFile != default)
                    {
                        output = await _moduleFileContext.Delete(model.ModuleFile);//.GetByModuleDirectioryName(packetName);                    
                    }
                    else
                    {
                        Console.WriteLine("A uncompressed file with that name could not be found");
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    //throw;
                }
                #endregion
                #region pre-Android 11

                //try
                //{
                //    output = await _packageUtility.DeleteModule(packetName);
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.StackTrace);
                //    //throw;
                //}
                //try
                //{
                //    ///This does not descriminate on what iit is deleting
                //    ModulePackageModel dbEntry = await _logicContext.GetByModuleDirectioryName(packetName);
                //    output = await DeleteDBEntry(dbEntry);
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.StackTrace);
                //    //throw;
                //}


                #endregion
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error installing module: " + ex.Message;
                throw;
            }
        }
        internal async Task<bool> DeleteCompressedModule(string packetName)
        {
            bool output = false;
            try
            {
                #region Post Android 11

                try
                {
                    ///Get the compressed Module index
                    List<ModuleFileModel> itemList = await _moduleFileContext.GetAllCompressed();
                    ModuleFileModel item = itemList.Where(i => i.ModuleDirectoryName.Contains(packetName)).FirstOrDefault();
                    ModulePackageModel model = await _logicContext.GetCompressedByModuleDirectioryName(item.ModuleDirectoryName);

                    ModulePackageModel model2 = await _logicContext.GetCompressedByModuleDirectioryName(packetName);

                    ///Delete the first ModuleFileModel that has this packetName--needs to be implemented and is compressed
                    ///ModulePackageModel model = await _logicContext.GetCompressedByModuleDirectioryName(packetName);
                    ///if moudule == null by directory name. 
                    if (model == default)
                    {
                        Console.WriteLine("A compressed file with that name could not be found");
                    }



                    output = await _moduleFileContext.Delete(model.CompressedModuleFile);//.GetByModuleDirectioryName(packetName);

                    ///Update Db Entry   
                    model.CompressedModuleFile = default;
                    model = await Update(model);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    //throw;
                }


                //try
                //{
                //    ///Need to go over. 
                //    output = await _packageUtility.DeleteCompressedModule(packetName);
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.StackTrace);
                //    //throw;
                //}
                //try
                //{
                //    ///Delete the first ModuleFileModel that has this packetName--needs to be implemented and is compressed
                //    ModulePackageModel dbEntry = await _logicContext.GetByModuleDirectioryName(packetName);
                //    output = await DeleteDBEntry(dbEntry);
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.StackTrace);
                //    //throw;
                //}

                #endregion
                #region pre-Android 11



                //output = await _packageUtility.DeleteZipModule(packetName);
                #endregion

                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Deleteing Zip File: " + ex.Message;
                throw;
            }
        }
        internal async Task<bool> UninstallApplication(string packetName)
        {
            bool output = false;
            try
            {
                #region Post Android 11
                List<ModulePackageModel> packageIndex = await _logicContext.GetAppList();//.GetIndexList();
                ModulePackageModel target = packageIndex
                    .Where(i => i.ModuleDirectoryName == packetName)
                    .First();

                string uristring = target.Name;
                output = await _packageUtility.UninstallApplication(uristring);

                ///Need to update database to reflect that the application was uninstalled
                ///
                if (output)
                {
                    target.Installed = false;
                    target.LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
                    target.UriString = default;

                    target = await _logicContext.Update(target);

                }


                #endregion
                #region pre-Android 11


                //Dictionary<string, string> packageIndex = await _packageContext.GetIndexList();// ?? new Dictionary<string, string>();
                //string uristring = packageIndex
                //    .Where(i => i.Key == packetName)
                //    .Select(i => i.Value)
                //    .First();
                //output = await _packageUtility.UninstallApplication(uristring);
                #endregion
                return output;
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error uninstalling module: " + ex.Message;
                throw;
            }
        }
        //internal async Task<List<string>> GetList()
        //{
        //    List<string> output = new List<string>();
        //    try
        //    {
        //        #region Post Android 11
        //        List<ModulePackageModel> itemList = await _logicContext.GetAppList();//.GetIndexList();
        //        output = itemList.Where(i => i.Installed).Select(i => i.ModuleDirectoryName).ToList();

        //        #endregion
        //        #region pre-Android 11


        //        //Dictionary<string, string> packageIndex = await _packageContext.GetIndexList() ?? new Dictionary<string, string>();

        //        //output = await _packageUtility.GetInstalledList(packageIndex) ?? new List<string>();
        //        #endregion
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Getting Module List: " + ex.Message;

        //        return default;
        //        //throw;
        //    }
        //}
        internal async Task<bool> ReinstallApplication(string packetName)
        {
            ///Get the package model so it can be used


            bool output = false;
            try
            {
                #region Post Android 11

                #endregion
                #region pre-Android 11
                _ = await UninstallApplication(packetName);
                #endregion

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reinstalling module: " + ex.Message);
            }

            try
            {
                #region Post Android 11

                #endregion
                #region pre-Android 11
                output = await InstallModule(packetName);
                #endregion

                ///create logic to keep track

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reinstalling module: " + ex.Message);
            }
            return output;
        }

        /// <summary>
        /// This is before the db - used file storage
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        //public async Task<List<ModulePackageModel>> Remove(string keyId, string packageName)
        //{
        //    List<ModulePackageModel> output = default;
        //    ModulePackageModel toRemove = default;
        //    try
        //    {
        //        List<ModulePackageModel> data = await GetIndex();

        //        if (data.Count <= 0)
        //        {
        //            return default;
        //        }


        //        if (!string.IsNullOrEmpty(keyId) && !string.IsNullOrEmpty(packageName))
        //        {
        //            toRemove = data.Where(i => i.UUID.ToString() == keyId && i.UriString == packageName).First();

        //        }
        //        else if (!string.IsNullOrEmpty(keyId))
        //        {
        //            toRemove = data.Where(i => i.UUID.ToString() == keyId).First();

        //        }
        //        else if (!string.IsNullOrEmpty(packageName))
        //        {
        //            toRemove = data.Where(i => i.UriString == packageName).First();
        //        }
        //        else
        //        { return output; }

        //        if (toRemove != default)
        //        {
        //            bool removed = await _logicContext.Delete(toRemove);
        //        }


        //        output = await GetIndex();
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        //this needs to be set up    
        //        //_log.LogInformation(ex.Message);
        //        throw;
        //    }
        //}



        internal async Task<ModulePackageModel> Update(ModulePackageModel input)
        {
            ModulePackageModel output = default;
            try
            {
                output = await _logicContext.Update(input);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }


        /// <summary>
        /// Is passed information from the event handler
        /// </summary>
        /// <param name="e"></param>
        internal async Task ModuleIndexUpdateAction(ModuleEventArgs e)
        {
            try
            {
                ModulePackageModel package = default;
                //Get the module package info
                package = await _logicContext.GetByModuleDirectioryName(e.ModuleDirectoryName);


                package.Installed = e.IsInstalled;
                package.LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
                package.UriString = e.FileUri;
                package.ModuleDirectoryName = e.ModuleDirectoryName;
                package.Name = e.PackageName;

                ///This might be best to be the e.packagename as well (currenly referenced too many times--Could change it to directory name like the others)
                //if (e.ModuleDirectoryName != default)
                //{
                //    package.Module.Name = e.ModuleDirectoryName;
                //}

                if (package == default)
                {
                    Console.WriteLine("An error occured while installing package");
                    return;
                }

                package = await Update(package);


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }


        }

        internal async Task<List<ModulePackageModel>> GetInstalledModuleDirectoryList()
        {
            List<ModulePackageModel> output = default;
            try
            {
                output = await GetIndex();
                output = output.Where(i => i.Installed == true).ToList();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<List<ModulePackageModel>> GetUncompressedModuleDirectoryList()
        {
            List<ModulePackageModel> output = default;
            try
            {
                output = await GetIndex();
                output = output.Where(i => i.ModuleFile != default).ToList();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<List<ModulePackageModel>> GetCompressedModuleDirectoryList()
        {
            List<ModulePackageModel> output = default;
            try
            {
                output = await GetIndex();
                output = output.Where(i => i.CompressedModuleFile != default).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }
        #endregion

        #region Data Integrity
        /// <summary>
        /// The key a compressed-module DIRECTORY ENTRY should be compared under.
        ///
        /// The archive on disk is "&lt;ModuleDirectoryName&gt;.zip" (written at
        /// WiFiP2pRequestProcessing.ProcessModule) while the database stores
        /// ModuleDirectoryName WITHOUT the extension (set from header.PacketName). The two
        /// comparison sites in CheckDataIntegrity compared those two strings directly, which
        /// can NEVER match, and both sites treat "no match" as "delete". So every integrity
        /// pass deleted the database record AND then the archive itself. The uncompressed arm
        /// is not affected because those entries are real directory names with no extension,
        /// which is why the compressed arm reads as correct on a skim.
        ///
        /// Strips only a trailing ".zip", deliberately. Path.GetFileNameWithoutExtension would
        /// truncate at ANY dot, which is identical for the GUID names used today and silently
        /// wrong the moment a name contains one.
        /// </summary>
        private static string CompressedEntryKey(string directoryEntry)
        {
            if (string.IsNullOrEmpty(directoryEntry)) { return directoryEntry; }
            return directoryEntry.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                ? directoryEntry.Substring(0, directoryEntry.Length - ".zip".Length)
                : directoryEntry;
        }

        /// <summary>
        /// This will check all files to ensure that there is a database entry for them. If there is not the file will be deleted or the database entry will be deleted.
        /// </summary>
        internal async static void CheckDataIntegrity()
        {
            ModulePackageLogic _logicContext = new ModulePackageLogic();
            FileManager _fileManager = new FileManager();
            ModuleFileContext _moduleFileContext = new ModuleFileContext();
            IModulePackageUtility _packageUtility = DependencyService.Get<IModulePackageUtility>();

            try
            {
                List<ModuleFileModel> ModulefilesWithIssues = new List<ModuleFileModel>();


                List<ModulePackageModel> modulePackagesWithCompressedModulefileMissing = new List<ModulePackageModel>();
                List<ModulePackageModel> modulePackagesWithUncompressedModulefileMissing = new List<ModulePackageModel>();
                List<ModulePackageModel> modulePackagesWithCompressedModulefiles = await _logicContext.GetCompressedModuleDirectoryList();
                List<ModulePackageModel> modulePackagesWithUncompressedModulefiles = await _logicContext.GetUncompressedModuleDirectoryList();
                List<string> presentCompressed = await _fileManager.GetDirectoryContentNames(FileSystem.ZippedModulePath);
                List<string> presentUncompressed = await _fileManager.GetDirectoryContentNames(FileSystem.ModulePath);

                //modulePackagesWithCompressedModulefileMissing = await CheckEachCompressedModuleFile(modulePackagesWithCompressedModulefiles);


                foreach (var i in modulePackagesWithCompressedModulefiles)
                {
                    // Compare the DB name against the entry with its .zip stripped. Comparing the
                    // raw entry here never matched, and the miss deletes the record below.
                    bool exists = i.CompressedModuleFile != null
                        && presentCompressed.Any(j => CompressedEntryKey(j) == i.CompressedModuleFile.ModuleDirectoryName);
                    if (!exists)
                    {
                        modulePackagesWithCompressedModulefileMissing.Add(i);
                    }
                }



                foreach (var i in modulePackagesWithUncompressedModulefiles)
                {
                    bool exists = presentUncompressed.Contains(i.ModuleFile.ModuleDirectoryName);//.Any(j => i.ModuleFile.ModuleDirectoryName.Contains(j));
                    if (!exists)
                    {
                        modulePackagesWithUncompressedModulefileMissing.Add(i);
                    }
                }



                ///Delete ModuleFileModels
                if (modulePackagesWithCompressedModulefileMissing.Count() > 0)
                {
                    ///Delete the compressed Module File entry
                    foreach (var i in modulePackagesWithCompressedModulefileMissing)
                    {
                        ///delete the actual fileModel
                        bool complete = await _logicContext.DeleteModulFile(i.CompressedModuleFile);
                        if (complete)
                        {
                            i.CompressedModuleFile = default;
                        }

                        ///update the pakage  model
                        ModulePackageModel updatedOutput = await _logicContext.Update(i);

                    }

                }


                if (modulePackagesWithUncompressedModulefileMissing.Count() > 0)
                {
                    ///Delete the compressed Module File entry
                    foreach (var i in modulePackagesWithCompressedModulefileMissing)
                    {
                        ///delete the actual fileModel
                        bool complete = await _logicContext.DeleteModulFile(i.ModuleFile);
                        if (complete)
                        {
                            i.CompressedModuleFile = default;
                        }

                        ///update the pakage  model
                        ModulePackageModel updatedOutput = await _logicContext.Update(i);

                    }


                }





                ///Check if there are any files that should not exist
                foreach (var i in presentCompressed)
                {
                    bool DoesNotNeedDeleting = false;
                    if (modulePackagesWithCompressedModulefiles.Count > 0)
                    {
                        // Same mismatch as above, and this is the site that deletes the ARCHIVE.
                        // The null guard matters here: the loop above sets CompressedModuleFile
                        // to default on records it removed, and those are the same instances.
                        DoesNotNeedDeleting = modulePackagesWithCompressedModulefiles
                            .Any(j => j.CompressedModuleFile != null
                                   && j.CompressedModuleFile.ModuleDirectoryName == CompressedEntryKey(i));
                    }
                    if (!DoesNotNeedDeleting)
                    {
                        string fullPath = Path.Combine(FileSystem.ZippedModulePath, i);
                        Console.WriteLine("Deleting Compressed File: " + fullPath);
                        bool deleted = await FileManager.DeleteFile(fullPath);
                        if (!deleted)
                        {
                            Console.WriteLine("Error deleting compressed File: " + fullPath);
                        }
                    }
                }

                foreach (var i in presentUncompressed)
                {
                    bool DoesNotNeedDeleting = false;
                    if (modulePackagesWithUncompressedModulefiles.Count > 0)
                    {
                        DoesNotNeedDeleting = modulePackagesWithUncompressedModulefiles.Where(j => j.ModuleFile.ModuleDirectoryName == i).Any();
                    }
                    //bool DoesNotNeedDeleting = modulePackagesWithUncompressedModulefiles.Where(j => j.ModuleFile.ModuleDirectoryName == i).Any();
                    if (!DoesNotNeedDeleting)
                    {
                        string fullPath = Path.Combine(FileSystem.ModulePath, i);
                        Console.WriteLine("Deleting Uncompressed Directory: " + fullPath);
                        bool deleted = await _fileManager.DeleteDirectory(fullPath);
                        if (!deleted)
                        {
                            Console.WriteLine("Error deleting uncompressed File: " + fullPath);
                        }
                    }


                }



                List<ModulePackageModel> allModulePackages = await _logicContext.GetIndex();
                if (allModulePackages.Count > 0)
                {
                    foreach (var i in allModulePackages)
                    {
                        if (i.ModuleFile == default && i.CompressedModuleFile == default && i.Installed == false)
                        {
                            await _logicContext.DeleteDBEntry(i);
                        }
                    }
                }


                ///Need to check if there are any Module File Models. If there are they need to be connected to packages
                List<ModuleFileModel> moduleFileModelList = await _moduleFileContext.Get();//.GetList();
                foreach (var i in moduleFileModelList)
                {
                    ModulePackageModel item = await _logicContext.Get(i);
                    if (item == default)
                    {
                        ///delete it.
                        await _moduleFileContext.Delete(i);
                    }
                }






                #region tied this it didn't work and deleted the entire database
                /////Gather All Zipped module Files
                //List<string> actualCompressedModuleDirectoryList = await _fileManager.GetDirectoryContentWithFullPath(FileSystem.ZippedModulePath);
                /////Gather list of uncompressed Modules from Db
                //List<ModulePackageModel> expectedCompressedModuleDirectoryList = await _logicContext.GetCompressedModuleDirectoryList();

                //await CompareExpectedAndActualDirectory(actualCompressedModuleDirectoryList, expectedCompressedModuleDirectoryList);


                /////Gather All Uncompressed module Files
                //List<string> actualUncompressedModuleDirectoryList = await _fileManager.GetDirectoryContentWithFullPath(FileSystem.ModulePath);
                /////Gather list of uncompressed Modules from Db
                //List<ModulePackageModel> expectedUncompressedModuleDirectoryList = await _logicContext.GetUncompressedModuleDirectoryList();

                //await CompareExpectedAndActualDirectory(actualUncompressedModuleDirectoryList, expectedUncompressedModuleDirectoryList);


                /////Gather all installed applications
                //List<string> actualInstalledModuleDirectoryList = await _packageUtility.GetInstalledList();
                /////Gather list of Installed Modules from Db
                //List<ModulePackageModel> expectedInstalledModuleDirectoryList = await _logicContext.GetInstalledModuleDirectoryList();

                //await CompareExpectedAndActualApplication(actualInstalledModuleDirectoryList, expectedInstalledModuleDirectoryList);
                #endregion
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error While checking data integrity: " + ex.Message;
                //throw;
            }
        }

        private async Task<ModulePackageModel> Get(ModuleFileModel i)
        {
            ModulePackageModel output = default;
            try
            {
                output = await _logicContext.GetByModuleDirectioryName(i.ModuleDirectoryName);
                return output;
            }
            catch (Exception ex)
            {

                throw;
            }

        }


        /// <summary>
        /// Needed to make a connection with the ModuleFileContext. This could be overkill but shouldn't hurt.
        /// </summary>
        /// <param name="moduleFile"></param>
        /// <returns></returns>
        private async Task<bool> DeleteModulFile(ModuleFileModel moduleFile)
        {
            bool output = false;
            try
            {
                output = await _moduleFileContext.Delete(moduleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error while deleting ModuleFile Db entry: " + ex.Message);
                //throw;
            }
            return output;
            //throw new NotImplementedException();
        }

        private async static Task CompareExpectedAndActualApplication(List<string> actualApplicationList, List<ModulePackageModel> expectedDirectoryList)
        {
            ModulePackageLogic _logicContext = new ModulePackageLogic();
            FileManager _fileManager = new FileManager();
            IModulePackageUtility _packageUtility = DependencyService.Get<IModulePackageUtility>();
            try
            {
                ///Check if there are any directories that aree not in the database
                //var missingDbEntries = actualDirectoryList.Where(i => !expectedDirectoryList.Any(j=>j.ModuleFile.FilePath==i)).ToList();
                List<string> missingDbEntries = actualApplicationList.Where(i =>
                !expectedDirectoryList.Any(j => j.ModuleDirectoryName == i))
                .ToList();


                if (missingDbEntries.Count > 0)
                {
                    ///delete directories that should not be there. 
                    foreach (var i in missingDbEntries)
                    {
                        await _packageUtility.UninstallApplication(i);
                    }

                }


                ///Cheeck if there are any databasee entires that do not exist in the directories
                //var missingDirectories = actualDirectoryList.Where(i => expectedDirectoryList.Any(j => j.ModuleFile.FilePath == i));
                List<ModulePackageModel> missingApplications = expectedDirectoryList.Where(i =>
                !actualApplicationList.Any(j => j == i.ModuleDirectoryName))
                .ToList();

                if (missingApplications.Count > 0)
                {
                    ///delete database entries what do not have directories
                    foreach (var i in missingApplications)
                    {
                        await _logicContext.DeleteDBEntry(i);
                    }

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //throw;
            }
        }

        private async static Task CompareExpectedAndActualDirectory(List<string> actualDirectoryList, List<ModulePackageModel> expectedDirectoryList)
        {
            ModulePackageLogic _logicContext = new ModulePackageLogic();
            FileManager _fileManager = new FileManager();
            IModulePackageUtility _packageUtility = DependencyService.Get<IModulePackageUtility>();
            try
            {
                ///Check if there are any directories that aree not in the database
                //var missingDbEntries = actualDirectoryList.Where(i => !expectedDirectoryList.Any(j=>j.ModuleFile.FilePath==i)).ToList();
                List<string> missingDbEntries = actualDirectoryList
                    .Where(i => !expectedDirectoryList.Any(j => j.ModuleFile?.FilePath == i))
                //!expectedDirectoryList.Any(j => j.ModuleFile.FilePath.Contains(i)))
                .ToList();


                if (missingDbEntries.Count > 0)
                {
                    ///delete directories that should not be there. 
                    foreach (var i in missingDbEntries)
                    {
                        if (i.Contains(".zip"))
                        {
                            await FileManager.DeleteFile(i);
                        }
                        else
                        {
                            await _fileManager.DeleteDirectory(i);
                        }
                    }

                }


                ///Cheeck if there are any databasee entires that do not exist in the directories
                //var missingDirectories = actualDirectoryList.Where(i => expectedDirectoryList.Any(j => j.ModuleFile.FilePath == i));
                List<ModulePackageModel> missingDirectories = expectedDirectoryList.Where(i =>
                !actualDirectoryList.Any(j => j == i.ModuleFile?.FilePath))
                .ToList();

                if (missingDirectories.Count > 0)
                {
                    ///delete database entries what do not have directories
                    foreach (var i in missingDirectories)
                    {
                        await _logicContext.DeleteDBEntry(i);
                    }

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }




        #endregion

    }
}
