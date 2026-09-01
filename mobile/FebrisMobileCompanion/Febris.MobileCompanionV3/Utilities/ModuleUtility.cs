// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using Febris.MobileCompanionV3.BusinessLogic;
//using Febris.MobileCompanionV3.DataLogic;
//using Febris.MobileCompanionV3.Resources;
//using Febris.SharedMobileLibrary.Interfaces;
//using Febris.SharedMobileLibrary.Models.Data;
//using Febris.SharedMobileLibrary.Models.EventArguments;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Xamarin.Forms;

///// <summary>
///// ********************************************************retired post Android 11 **************************
///// 
///// Moved to ModulePackageLogic due to ambiguity and compliance with android 11+
///// 
///// </summary>

//namespace Febris.MobileCompanionV3.Utilities
//{
//    public class ModuleUtility
//    {
//        IModulePackageUtility _packageUtility;// = DependencyService.Get<IModulePackageUtility>();
//        PackageLogic _packageContext;
//        public ModuleUtility()
//        {
//            _packageUtility = DependencyService.Get<IModulePackageUtility>();
//            _packageContext = new PackageLogic();
//        }

//        internal async Task<bool> InstallModule(string moduleName, string filePath)
//        {
//            bool output = false;
//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11


//                await _packageUtility.InstallModule(moduleName, filePath);
//                output = true;
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error installing module: " + ex.Message;
//                throw;
//            }

//        }
//        internal async Task<bool> InstallModule(string moduleName)
//        {
//            bool output = false;
//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11


//                string filePath = System.IO.Path.Combine(SharedMobileLibrary.FileSystem.FileSystem.ModulePath, moduleName);
//                await _packageUtility.InstallModule(moduleName, filePath);
//                output = true;
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error installing module: " + ex.Message;
//                throw;
//            }

//        }
//        internal async Task<bool> DeleteModule(string packetName)
//        {
//            bool output = false;
//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11


//                output = await _packageUtility.DeleteModule(packetName);
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error installing module: " + ex.Message;
//                throw;
//            }
//        }
//        internal async Task<bool> DeleteZipModule(string packetName)
//        {
//            bool output = false;
//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11



//                output = await _packageUtility.DeleteZipModule(packetName);
//                #endregion

//                return output;
//            }
//            catch (Exception ex)
//            {
//                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Deleteing Zip File: " + ex.Message;
//                throw;
//            }
//        }
//        internal async Task<bool> UninstallApplication(string packetName)
//        {
//            bool output = false;
//            try
//            {
//                #region Post Android 11
//                List< ModulePackageModel> packageIndex = await _packageContext.GetIndexList();
//                ModulePackageModel target = packageIndex
//                    .Where(i => i.PacketName == packetName)
//                    .First();

//                string uristring = target.UriString;
//                output = await _packageUtility.UninstallApplication(uristring);

//                ///Need to update database to reflect that the application was uninstalled
//                ///
//                if (output)
//                {
//                    target.Installed = false;
//                    target = await _packageContext.Update(target);
//                }
                

//                #endregion
//                #region pre-Android 11


//                //Dictionary<string, string> packageIndex = await _packageContext.GetIndexList();// ?? new Dictionary<string, string>();
//                //string uristring = packageIndex
//                //    .Where(i => i.Key == packetName)
//                //    .Select(i => i.Value)
//                //    .First();
//                //output = await _packageUtility.UninstallApplication(uristring);
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error uninstalling module: " + ex.Message;
//                throw;
//            }
//        }
//        internal async Task<List<string>> GetList()
//        {
//            List<string> output = new List<string>();
//            try
//            {
//                #region Post Android 11
//                List<ModulePackageModel> itemList = await _packageContext.GetIndexList();
//                output = itemList.Where(i => i.Installed).Select(i => i.PacketName).ToList();

//                #endregion
//                #region pre-Android 11


//                //Dictionary<string, string> packageIndex = await _packageContext.GetIndexList() ?? new Dictionary<string, string>();

//                //output = await _packageUtility.GetInstalledList(packageIndex) ?? new List<string>();
//                #endregion
//                return output;
//            }
//            catch (Exception ex)
//            {
//                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Getting Module List: " + ex.Message;

//                return default;
//                //throw;
//            }
//        }
//        internal async Task<bool> ReinstallApplication(string packetName)
//        {
//            bool output = false;
//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11
//                _ = await UninstallApplication(packetName);
//                #endregion

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error reinstalling module: " + ex.Message);
//            }

//            try
//            {
//                #region Post Android 11

//                #endregion
//                #region pre-Android 11
//                output = await InstallModule(packetName);
//                #endregion

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error reinstalling module: " + ex.Message);
//            }
//            return output;
//        }

//        #region Event Actions
//        internal void ModuleIndexUpdateAction(object sender, ModuleEventArgs e)
//        {
//            try
//            {
//                //instead send this to the bll so it can be filtered instead of just shooting from the hip like this. 
//                #region Post Android 11
//                ModulePackageModel data = new ModulePackageModel()
//                {
//                    Module = new Module()
//                    {
//                        UUID = Guid.Parse(e.ModuleId)
//                    },
//                    PacketName = e.PackageName
                    
//                };

//                data = _packageContext.Get(data).Result;





//                Console.WriteLine(output);
//                #endregion
//                #region pre-Android 11
//                //Dictionary<string, string> output = _packageContext.Update(e.ModuleId, e.PackageName).Result;
//                //Console.WriteLine(output);
//                #endregion




//                //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);

//            }
//        }

//        #endregion
//    }
//}
