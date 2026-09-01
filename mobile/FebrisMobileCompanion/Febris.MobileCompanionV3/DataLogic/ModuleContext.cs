// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// It turns out Sqlite lacks almost all of the power of sql in regards to relational operations.
/// </summary>
namespace Febris.MobileCompanionV3.DataLogic
{    
    public class ModuleContext
    {        
        private LocalDatabase _context = App.DbContext;


        #region Interactable Calls

        #region Get
        /// <summary>
        /// This gathers the information from the database 
        /// </summary>
        /// <returns></returns>
        public async Task<List<Module>> GetList()
        {
            List<Module> output = new List<Module>();
            try
            {
                output = await GetIndexListFromDb();                
                return output;
            }
            catch (Exception)
            {

                throw;
            }
        }
        
        internal async Task<Module> Get(long input)
        {
            Module output = default;
            try
            {
                output = await _context.Get<Module>(i => i.Id == input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        internal async Task<Module> Get(Guid input)
        {
            Module output = default;
            try
            {
                output = await _context.Get<Module>(i => i.UUID == input);
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
        public async Task<Module> Post(Module data)
        {
            Module output = default;
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
        
        #endregion

        #region Delete
        public async Task<bool> Delete(Module input)
        {
            bool output = false;
            try
            {
                
                output = await DeleteFromDb(input);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<bool> Delete(List<Module> input)
        {
            bool output = false;
            try
            {
                foreach (var i in input)
                {
                    output = await DeleteFromDb(i);
                    if (output)
                    {
                        Console.WriteLine(i.Name + " Was not removed properly");
                    }
                }
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
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
        private async Task<List<Module>> GetIndexListFromDb()
        {
            List<Module> output = default;
            try
            {                
                List<ModuleStorage> itemList = await _context.GetList<ModuleStorage>();
                foreach(var i in itemList)
                {
                    output.Add(new Module(i));
                }
                //output = await _context.GetList<Module>();
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private async Task<Module> PostToDb(Module input)
        {
            Module output = default;
            try
            {
                ModuleStorage storage = new ModuleStorage(input);
                ModuleStorage item = await _context.Create<ModuleStorage>(storage);
                output = new Module(item);                
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        internal async Task<Module> Update(Module input)
        {
            Module output = default;
            try
            {
                ModuleStorage storage = new ModuleStorage(input);
                storage = await _context.Update<ModuleStorage>(storage);
                output = new Module(storage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return output;
        }

        public async Task<bool> DeleteFromDb(Module input)
        {
            bool output = false;
            try
            {
                ModuleStorage storage = new ModuleStorage(input);
                output = await _context.Delete(storage);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }





        #endregion

        #region Handling actual Files


        #endregion

    }
}
