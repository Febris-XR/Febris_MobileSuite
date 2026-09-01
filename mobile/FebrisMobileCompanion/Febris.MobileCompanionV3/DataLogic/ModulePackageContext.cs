// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.ModelLibrary.Models.XApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Had to combine both ModuleFileContext and PackageContext to reduce confusion. 
/// 
/// This is also in action to deal with issues caused by the addition of Android 11+ support
/// </summary>
namespace Febris.MobileCompanionV3.DataLogic
{
    public class ModulePackageContext
    {
        private LocalDatabase _context = App.DbContext;
        private ModuleFileContext _moduleFileContext = new ModuleFileContext();



        #region Interactable Calls

        #region Get
        /// <summary>
        /// This gathers the information from the database 
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAppNameList()
        {
            List<string> output = new List<string>();
            try
            {
                List<ModulePackageModel> itemList = await GetAppList();
                output = itemList.Where(i => i.Installed).Select(i => i.Name).ToList() ?? default;
                return output;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<ModulePackageModel>> GetAppList()
        {
            List<ModulePackageModel> output = new List<ModulePackageModel>();
            try
            {
                List<ModulePackageStorageModel> itemList = await GetIndexListFromDb();
                foreach (var i in itemList)
                {
                    ModulePackageModel item = new ModulePackageModel(i);

                    if (i.ModuleFileId != default)
                    {
                        ModuleFileModel decompressedModuleFile = await _moduleFileContext.Get(i.ModuleFileId);
                        item.ModuleFile = decompressedModuleFile;
                    }
                    if (i.CompressedModuleFileId != default)
                    {
                        ModuleFileModel compressedModuleFile = await _moduleFileContext.Get(i.CompressedModuleFileId);
                        item.CompressedModuleFile = compressedModuleFile;
                    }
                    if (i.ModuleId != default)
                    {
                        ModuleStorage childStorage = await _context.Get<ModuleStorage>(j => j.Id == i.ModuleId) ?? default;
                        Module mod = new Module(childStorage);
                        item.Module = mod;
                    }

                    output.Add(item);
                }

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<ModulePackageModel> Get(long input)
        {
            ModulePackageModel output = default;
            try
            {
                ///Get the actual package
                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.Id == input) ?? default;
                ///Get the module File
                ModuleFileModel modFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId) ?? default;
                ///Get the module File
                ModuleFileModel compFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);//await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;
                ///Get the Module
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId) ?? default;
                Module mod = new Module(childStorage);
                ///Combine them all
                output = new ModulePackageModel(storage, mod, modFile, compFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<List<string>> GetModuleNameIndex()
        {
            try
            {
                List<string> output = new List<string>();
                try
                {
                    List<ModulePackageStorageModel> itemList = await GetIndexListFromDb();
                    output = itemList.Where(i => i.ModuleFileId != default).Select(i => i.ModuleDirectoryName).ToList() ?? default;
                    return output;
                }
                catch (Exception)
                {

                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        internal async Task<ModulePackageModel> Get(Guid input)
        {
            ModulePackageModel output = default;
            try
            {

                ///Get the actual package
                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.UUID == input) ?? default;
                ///Get the module File
                ModuleFileModel decompressedModuleFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId) ?? default;
                ///Get the module File
                ModuleFileModel compressedModuleFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);//await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;
                ///Get the Module
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId) ?? default;
                Module mod = new Module(childStorage);
                ///Combine them all
                output = new ModulePackageModel(storage, mod, decompressedModuleFile, compressedModuleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<ModulePackageModel> GetByModuleDirectioryName(string input)
        {
            ModulePackageModel output = default;
            try
            {
                ///Get the actual package
                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleDirectoryName == input) ?? default;
                
                ///Need to grab the Module information because the directory name needs to be used. 
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId) ?? default;

                /////Get the actual package
                //ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleFileId == childStorage.Id) ?? default;

                ///Get the module File
                ModuleFileModel decompressedModuleFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId) ?? default;
                ///Get the module File
                ModuleFileModel compressedModuleFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);//await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;

                ///Create the view Models
                Module mod = new Module(childStorage);
                ///Combine them all
                output = new ModulePackageModel(storage, mod, decompressedModuleFile, compressedModuleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //throw;
            }
            return output;
        }

        internal async Task<ModulePackageModel> GetCompressedByModuleDirectioryName(string input)
        {
            ModulePackageModel output = default;
            try
            {
                ///Get the actual package
                List<ModulePackageStorageModel> storageList = await _context.GetList<ModulePackageStorageModel>();// (i => i.ModuleDirectoryName == input) ?? default;

                storageList = storageList.Where(i => i.ModuleDirectoryName == input
                && i.CompressedModuleFileId != 0).ToList();

                if (storageList.Count > 0)
                {
                    Console.WriteLine("The are not modules listed");
                }


                ///Get the first one
                ModulePackageStorageModel storage = storageList.FirstOrDefault();


                ///Need to grab the Module information because the directory name needs to be used. 
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId) ?? default;

                /////Get the actual package
                //ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleFileId == childStorage.Id) ?? default;

                ///Get the module File
                ModuleFileModel decompressedModuleFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId) ?? default;
                ///Get the module File
                ModuleFileModel compressedModuleFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);//await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;

                ///Create the view Models
                Module mod = new Module(childStorage);
                ///Combine them all
                output = new ModulePackageModel(storage, mod, decompressedModuleFile, compressedModuleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<ModulePackageModel> GetUncompressedByModuleDirectioryName(string input)
        {
            ModulePackageModel output = default;
            try
            {
                ///Get the actual package
                List<ModulePackageStorageModel> storageList = await _context.GetList<ModulePackageStorageModel>();// (i => i.ModuleDirectoryName == input) ?? default;

                storageList = storageList.Where(i => i.ModuleDirectoryName == input
                && i.ModuleFileId != 0).ToList();

                if (storageList.Count <= 0)
                {
                    List<ModuleFileModel> modFileList = await _moduleFileContext.Get();
                    if (modFileList.Count > 0)
                    {
                        foreach(var i in modFileList)
                        {
                            await _moduleFileContext.Delete(i);
                        }
                    }
                }

                ///Get the first one
                ModulePackageStorageModel storage = storageList.FirstOrDefault();


                ///Need to grab the Module information because the directory name needs to be used. 
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId) ?? default;

                /////Get the actual package
                //ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleFileId == childStorage.Id) ?? default;

                ///Get the module File
                ModuleFileModel decompressedModuleFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId) ?? default;
                ///Get the module File
                ModuleFileModel compressedModuleFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);//await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;

                ///Create the view Models
                Module mod = new Module(childStorage);
                ///Combine them all
                output = new ModulePackageModel(storage, mod, decompressedModuleFile, compressedModuleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<ModulePackageModel> Get(string input)
        {
            ModulePackageModel output = default;
            try
            {
                ///Get the actual package
                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.UriString == input) ?? default;

                ///Get the module File
                ModuleFileModel decompressedModuleFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId) ?? default;
                ///Get the module File
                ModuleFileModel compressedModuleFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);//await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;              
                ///Get the Module
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId) ?? default;
                Module mod = new Module(childStorage);
                ///Combine them all
                output = new ModulePackageModel(storage, mod, decompressedModuleFile, compressedModuleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }


        internal async Task<ModulePackageModel> GetByModule(Guid input)
        {
            ModulePackageModel output = default;
            try
            {
                ///Get the module
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.UUID == input);
                Module mod = new Module(childStorage);
                ///Get the actual parent
                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleId == childStorage.Id);
                output = new ModulePackageModel(storage);
                ///get second child
                ModuleFileModel childStorage2 = await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId);
                ///Get compreessed model file
                ModuleFileModel compressedModuleFile = await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;


                output = new ModulePackageModel(storage, mod, childStorage2, compressedModuleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<ModulePackageModel> GetByModule(string input)
        {
            ModulePackageModel output = default;
            try
            {
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Name == input);
                Module mod = new Module(childStorage);

                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleId == mod.Id);
                //output = new ModulePackageModel(storage, mod);

                ///Get the module File
                ModuleFileModel decompressedModuleFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId) ?? default;
                ///Get the module File
                ModuleFileModel compressedModuleFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);//await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;

                output = new ModulePackageModel(storage, mod, decompressedModuleFile, compressedModuleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<bool> ExistsByDirectoryName(ModulePackageModel modulePackage)
        {
            bool exists = false;
            try
            {
                exists = await _context.Exists<ModulePackageStorageModel>(i => i.ModuleDirectoryName == modulePackage.ModuleDirectoryName);

                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while querying DB for module package existance: " + ex.Message);
                throw;
            }

        }

        internal async Task<ModulePackageModel> GetByModule(long input)
        {
            ModulePackageModel output = default;
            try
            {

                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == input);
                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleId == childStorage.Id);
                Module mod = new Module(childStorage);
                //output = new ModulePackageModel(storage, mod);


                ///Get the module File
                ModuleFileModel decompressedModuleFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId) ?? default;
                ///Get the module File
                ModuleFileModel compressedModuleFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);//await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId) ?? default;

                output = new ModulePackageModel(storage, mod, decompressedModuleFile, compressedModuleFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<ModulePackageModel> GetByModuleFile(long input)
        {
            ModulePackageModel output = default;
            try
            {
                ModuleFileModel childStorage2 = await _context.Get<ModuleFileModel>(i => i.Id == input);

                ModuleFileModel compressedFile = default;
                ModuleFileModel decompressedFile = default;
                if (childStorage2.Compressed)
                {
                    compressedFile = childStorage2;
                }
                else
                {
                    decompressedFile = childStorage2;
                }

                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleFileId == childStorage2.Id);

                if (childStorage2.Compressed)
                {
                    decompressedFile = await _moduleFileContext.Get(storage.ModuleFileId);// await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId);
                }
                else
                {
                    //compressedFile = await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId);
                    compressedFile = await _moduleFileContext.Get(storage.CompressedModuleFileId);
                }



                //ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleFileId == childStorage2.Id);
                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId);





                Module mod = new Module(childStorage);
                //ModuleFileModel file = new ModuleFileModel(childStorage2);
                output = new ModulePackageModel(storage, mod, decompressedFile, compressedFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<ModulePackageModel> GetByModuleFile(Guid input)
        {
            ModulePackageModel output = default;
            try
            {


                ModuleFileModel childStorage2 = await _context.Get<ModuleFileModel>(i => i.UUID == input);


                ModuleFileModel compressedFile = default;
                ModuleFileModel decompressedFile = default;
                if (childStorage2.Compressed)
                {
                    compressedFile = childStorage2;
                }
                else
                {
                    decompressedFile = childStorage2;
                }

                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleFileId == childStorage2.Id);

                if (childStorage2.Compressed)
                {
                    decompressedFile = await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId);
                }
                else
                {
                    compressedFile = await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId);
                }

                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId);

                Module mod = new Module(childStorage);
                //ModuleFileModel file = new ModuleFileModel(childStorage2);
                output = new ModulePackageModel(storage, mod, decompressedFile, compressedFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }



        internal async Task<ModulePackageModel> GetByModuleFile(string input)
        {
            ModulePackageModel output = default;
            try
            {
                ModuleFileModel childStorage2 = await _context.Get<ModuleFileModel>(i => i.FilePath == input);



                ModuleFileModel compressedFile = default;
                ModuleFileModel decompressedFile = default;
                if (childStorage2.Compressed)
                {
                    compressedFile = childStorage2;
                }
                else
                {
                    decompressedFile = childStorage2;
                }

                ModulePackageStorageModel storage = await _context.Get<ModulePackageStorageModel>(i => i.ModuleFileId == childStorage2.Id);

                if (childStorage2.Compressed)
                {
                    decompressedFile = await _context.Get<ModuleFileModel>(i => i.Id == storage.ModuleFileId);
                }
                else
                {
                    compressedFile = await _context.Get<ModuleFileModel>(i => i.Id == storage.CompressedModuleFileId);
                }

                ModuleStorage childStorage = await _context.Get<ModuleStorage>(i => i.Id == storage.ModuleId);

                Module mod = new Module(childStorage);

                //ModuleFileModel file = new ModuleFileModel(childStorage2);
                output = new ModulePackageModel(storage, mod, decompressedFile, compressedFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }
        #endregion

        #region Post
        public async Task<ModulePackageModel> Post(ModulePackageModel data)
        {
            ModulePackageModel output = default;
            try
            {
                output = await PostToDb(data);

                //output = await GetIndexListFromDb();
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        #endregion

        #region Update
        internal async Task<ModulePackageModel> Update(ModulePackageModel input)
        {
            ModulePackageModel output = default;
            try
            {

                ModuleStorage modStorage = new ModuleStorage(input.Module);
                modStorage = await _context.Update<ModuleStorage>(modStorage);
                input.Module = new Module(modStorage);

                ModuleFileModel compressedFileStorage = default;
                ModuleFileModel decompressedFile = default;
                if (input.ModuleFile != default)
                {
                    decompressedFile = await _moduleFileContext.Update(input.ModuleFile);
                    input.ModuleFile = decompressedFile;
                }

                if (input.CompressedModuleFile != default)
                {
                    compressedFileStorage = await _moduleFileContext.Update(input.CompressedModuleFile);
                    input.CompressedModuleFile = compressedFileStorage;
                }

                ModulePackageStorageModel storage = new ModulePackageStorageModel(input);
                ModulePackageStorageModel item = await _context.Update<ModulePackageStorageModel>(storage);
                output = new ModulePackageModel(item, input.Module, input.ModuleFile, input.CompressedModuleFile);
                //return output;



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }
        #endregion

        #region Delete       

        //public async Task<bool> Delete(List<ModulePackageModel> input)
        //{
        //    bool output = false;
        //    try
        //    {
        //        foreach (var i in input)
        //        {
        //            output = await DeleteFromDb(i);
        //            if (!output)
        //            {
        //                Console.WriteLine(i.Module.Name + " Was not removed properly");
        //            }
        //        }
        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw;
        //    }
        //}
        #endregion


        /// <summary>
        /// This method will check all of the files and ensure that the Database is accurate
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckDatabaseToFileSystemCrossReference()
        {
            bool output = false;
            try
            {

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }




        /// <summary>
        /// This method will check all of the Installed Applications and ensure that the Database is accurate
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckDatabaseToApplicationCrossReference()
        {
            bool output = false;
            try
            {

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        #endregion

        #region Handling Database
        private async Task<List<ModulePackageStorageModel>> GetIndexListFromDb()
        {
            List<ModulePackageStorageModel> output = default;
            try
            {
                output = await _context.GetList<ModulePackageStorageModel>();
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<ModulePackageModel> PostToDb(ModulePackageModel input)
        {
            ModulePackageModel output = default;
            try
            {
                ModuleStorage modStorage = new ModuleStorage(input.Module);
                modStorage = await _context.Create<ModuleStorage>(modStorage);
                input.Module = new Module(modStorage);

                if (input.CompressedModuleFile != default)
                {
                    input.CompressedModuleFile = await _moduleFileContext.Post(input.CompressedModuleFile);
                }
                if (input.ModuleFile != default)
                {
                    input.ModuleFile = await _moduleFileContext.Post(input.ModuleFile);
                }

                ModulePackageStorageModel storage = new ModulePackageStorageModel(input);
                ModulePackageStorageModel item = await _context.Create<ModulePackageStorageModel>(storage);
                output = new ModulePackageModel(item, input.Module, input.ModuleFile, input.CompressedModuleFile);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteFromDb(ModulePackageModel input)
        {
            bool output = false;
            try
            {
                if (input.Module != default)
                {
                    ModuleStorage modStorage = new ModuleStorage(input.Module);
                    output = await _context.Delete<ModuleStorage>(modStorage);
                }

                if (input.ModuleFile != default)
                {
                    output = await _moduleFileContext.Delete(input.ModuleFile);
                }

                if (input.CompressedModuleFile != default)
                {
                    output = await _moduleFileContext.Delete(input.CompressedModuleFile);
                }

                ModulePackageStorageModel storage = new ModulePackageStorageModel(input);
                output = await _context.Delete<ModulePackageStorageModel>(storage);

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }





        #endregion

    }





}
