// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.APIInteractions;
using Febris.MobileServerV3.DataLogic;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Utilites;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Febris.MobileServerV3.BusinessLogic
{
    public class ModuleLogic
    {
        private ILogger _log;
        private IConfiguration _config;
        private readonly JSONHandler _jSONHandler;
        private readonly FileManager _fileManager;
        private readonly ModuleRequest _moduleRequest;
        private readonly ModuleFileContext _moduleContext;



        //private readonly Timer _timer;
        //private readonly ModuleFileProcessing _moduleProcessing;


        public ModuleLogic(ILogger log)
        {
            _log = log;
            _jSONHandler = new JSONHandler();// _log);
            _fileManager = new FileManager(_log, _config);
            _moduleRequest = new ModuleRequest(_log);
            //_moduleProcessing = new ModuleFileProcessing(_log);
            //_timer = new Timer(LocalHardwareStaticDetails.CheckTimeSpan) { AutoReset = true };
            //_timer.Elapsed += TimerElapsed;
            _moduleContext = new ModuleFileContext();

        }
        public ModuleLogic()
        {
            
            _jSONHandler = new JSONHandler();// _log);
            _fileManager = new FileManager(_log, _config);
            _moduleRequest = new ModuleRequest(_log);
            //_moduleProcessing = new ModuleFileProcessing(_log);
            //_timer = new Timer(LocalHardwareStaticDetails.CheckTimeSpan) { AutoReset = true };
            //_timer.Elapsed += TimerElapsed;
            _moduleContext = new ModuleFileContext();

        }

        #region insure assigned modules are downloaded
        //*********************************************************************************************************
        //Todo: check each file and make sure each module is up to date with API Get
        //
        //*********************************************************************************************************
        public bool ModuleChecker()
        {
            bool hasModules = false;
            try
            {
                List<Guid> listOfSetModules = new List<Guid>();
                listOfSetModules = _moduleRequest.ModuleListRequest().Result;
                                                
                var task = Task.Run(() => CycleThroughModules(listOfSetModules));
                task.Wait();
                bool downloadRslt = task.Result;

            }
            catch (Exception e)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = e.Message;
            }
            return hasModules;
        }

        /// <summary>
        /// Sending files to headsets
        /// </summary>
        /// <param name="j"></param>
        /// <returns></returns>
        internal async Task<byte[]> GetFileContent(Guid j)
        {
            byte[] output = { };
            output= await _moduleContext.GetFileContents(j);
            return output;
        }

        //*********************************************************************************************************
        //check each module matches db's modules
        //
        //*********************************************************************************************************
        private bool CycleThroughModules(List<Guid> moduleList)
        {
            bool downloaded = false;           
            try
            {
                IEnumerable<string> moduleFolders = Directory.EnumerateFiles(FileSystem.ZippedModulePath);
                foreach (Guid module in moduleList)
                {                    
                    bool fileExistsInSystem = moduleFolders.Where(i => i.Contains(Path.GetFileName(module.ToString()))).Any();
                    if (!fileExistsInSystem)
                    {                 
                        bool downloadRslt = _moduleRequest.DownloadModule(module).Result;                        
                    }
                }
            }
            catch (Exception e)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = e.Message;
            }
            return downloaded;
        }
        #endregion



        /// <summary>
        /// ToDo: Forward module files to headsets
        /// </summary>
        /// <returns></returns>
        //public bool ForwardModuleToHeadset()
        //{
        //    try
        //    {


        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
        //        throw;
        //    }            
        //}


        internal List<Guid> GetNameList()
        {
            List<Guid> listOfSetModules = new List<Guid>();
            listOfSetModules = _moduleRequest.ModuleListRequest().Result;
            return listOfSetModules;
        }

        /// <summary>
        /// ToDo: Create a real looping system
        /// </summary>
        /// <returns></returns>
        //public async Task ModuleChecking()
        //{
        //    try
        //    {


        //        while (true)
        //        {

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //        throw;
        //    }
        //    throw new NotImplementedException();
        //}       
    }
}
