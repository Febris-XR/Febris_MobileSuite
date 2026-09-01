// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using Febris.MobileCompanionV3.DataLogic;
//using Febris.MobileCompanionV3.Resources;
//using Febris.SharedMobileLibrary.Interfaces;
//using Febris.SharedMobileLibrary.Models.Data;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Xamarin.Forms;

///// <summary>
///// ***************************************Moved to ModulePackageLogic ******************************
///// Moved due to Andrroid 11+ compliance and ambiguity
///// </summary>

//namespace Febris.MobileCompanionV3.BusinessLogic
//{
//    public class ModuleLogic
//    {
//        private readonly ModuleFileContext _context;
//        private readonly PackageLogic _packageContext;
//        public ModuleLogic()
//        {
//            _context = new ModuleFileContext();
//            _packageContext = new PackageLogic();
//        }

//        ///Gather video files
//        //public async Task<List<string>> GetUnsentNameList()
//        //{
//        //    List<string> output = new List<string>();
//        //    try
//        //    {
//        //        output = await _context.GetUnsentNameList();
//        //        return output;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
//        //        throw;
//        //    }
//        //}

//        public async Task<List<string>> GetNameList()
//        {
//            List<string> output = new List<string>();
//            try
//            {
//                #region Post Android 11
//                List<Module> list = await App.DbContext.GetList<Module>();
//                output = list.Select(i => i.Name).ToList();
//                #endregion
//                #region pre-Android 11
//                //output = await _context.GetNameList();
//                #endregion




//                return output;
//            }
//            catch (Exception ex)
//            {
//                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
//                throw;
//            }
//        }

//        internal bool LaunchModule(string ModuleId, string data)
//        {
//            bool output = false;
//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11
//                string packageName = _packageContext.Get(ModuleId).Result;
//                IModulePackageUtility moduleUtility = DependencyService.Get<IModulePackageUtility>();
//                moduleUtility.RunModule(packageName, data);
//                #endregion

//                output = true;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }
//            return output;
//        }


//    }
//}
