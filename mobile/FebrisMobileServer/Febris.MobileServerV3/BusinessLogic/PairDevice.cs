// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.DataLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworkModels.WiFi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.BusinessLogic
{
    public class PairDevice
    {
        private CompanionDeviceContext _context = new CompanionDeviceContext();
        public IWiFiService wifi = DependencyService.Get<IWiFiService>();
        DataProtection _dataProtection = new DataProtection();


        #region create device
        /// <summary>
        /// Create the device record for a Companion that has just completed a numeric-comparison
        /// pairing (docs/MOBILE_AUTH.md 4.1).
        ///
        /// THIS IS WHAT MAKES PAIRING ONBOARD RATHER THAN DECORATE. Every other path into the
        /// device list runs through Bluetooth: OnCompanionCreation keys on the Bluetooth MAC, and
        /// ProcessInitalization will only attach a WiFi identity to a record that already matches
        /// a Bluetooth NAME. A device paired numerically has none of that, and needs none of it.
        ///
        /// The identity written here is the one the ceremony actually authenticated: the device
        /// identifier the Companion sent in its pairing response, on the socket whose six-digit
        /// code a human just confirmed. No Bluetooth name is involved, which is the whole reason
        /// this exists.
        /// </summary>
        public static async Task<bool> CreateNumericallyPairedDevice(string deviceUniqueIdentifier)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceUniqueIdentifier)) return false;

                var list = LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.ItemList;
                if (list != null && list.Any(i => i?.CompanionDevice != null
                        && i.CompanionDevice.UniqueIdentifier == deviceUniqueIdentifier))
                {
                    return true;   // already known, pairing just added the key
                }

                var device = new CompanionDevice
                {
                    Name = "Companion " + deviceUniqueIdentifier,
                    UniqueIdentifier = deviceUniqueIdentifier
                    // No address stored on the MODEL: WiFiIPAddress lives on the view model and is
                    // filled by UpdateIPAddress on the next frame. That now works precisely because
                    // this record exists, so the peer finally RESOLVES instead of being refused.
                };

                CompanionDeviceContext context = new CompanionDeviceContext();
                List<CompanionDeviceViewModel> saved = await context.Post(device);
                if (saved != null)
                {
                    LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList = saved;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateNumericallyPairedDevice failed: " + ex.Message);
                return false;
            }
        }


        internal void OnCompanionCreation(object sender, CompanionDeviceEventArgs e)
        {
            try
            {
                CompanionDevice temp = new CompanionDevice()
                {
                    Name = e.Name,
                    BlueToothAlias = e.BlueToothAlias,
                    BlueToothName = e.BlueToothName,
                    BlueToothMacAddress = e.BlueToothMacAddress,
                    BlueToothType = e.BlueToothType
                };


                if (!LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.ItemList?.Where(i => i.CompanionDevice.BlueToothMacAddress == temp.BlueToothMacAddress).Any() ?? false)
                {
                    List<CompanionDeviceViewModel> data = _context.Post(temp).Result;
                    LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList = data;
                }
                //if (!LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.BlueToothMacAddress == temp.BlueToothMacAddress).Any())
                //{
                //    CompanionDeviceViewModel device = new CompanionDeviceViewModel()
                //    {
                //        CompanionDevice = temp
                //    };
                //    LocalHardwareStaticDetails.PairedDeviceViewModelList.Add(device);
                //    //serialize
                //    SaveCompanionDeviceList();
                //}
            }
            catch { }
        }

        internal static async Task<CompanionDeviceViewModel> UpdateCompanionDevice(CompanionDevice input)
        {
            try
            {


                //CompanionDeviceViewModel temp = new CompanionDeviceViewModel();
                CompanionDeviceViewModel temp = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    .Where(i => i.CompanionDevice.UniqueIdentifier == input.UniqueIdentifier)
                    .Single();

                #region This is on the base model but is not updating while running
                //List<CompanionDevice> tempList = await _context.GetSavedList();//_context.ge.Get;

                //CompanionDevice temp = tempList
                //    .Where(i => i.UniqueIdentifier == input.UniqueIdentifier)
                //    .Single();
                #endregion
                temp.CompanionDevice.Name = input.Name;

                CompanionDeviceContext _context = new CompanionDeviceContext();
                List<CompanionDeviceViewModel> data =await _context.Update(temp.CompanionDevice);
                LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList = data;
                LocalHardwareStaticDetails.StaticMainVM.HardwareVM.DisplayedItem = temp;
                //LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList = 

                //LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemResultList = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                //.Where(i => i.CompanionDevice.WifiMacAddress != default).ToList();
                return temp;
            }
            catch { }
            return default;
        }
        #endregion

        #region update device
        public static async Task UpdateCompanionDevice(CompanionDeviceEventArgs input, CompanionDevice temp)
        {
            try
            {
                temp.WifiMacAddress = input.ConnectedDeviceMacAddress;
                temp.WiFiDeviceName = input.ConnectedDeviceName;
                temp.WiFiPrimaryDeviceType = input.WiFiPrimaryDeviceType;
                temp.WiFiSecondaryDeviceType = input.WiFiSecondaryDeviceType;



                //if (LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.BlueToothMacAddress == temp.BlueToothMacAddress).Any())
                //{
                //    CompanionDeviceViewModel deviceToUpdate = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.BlueToothMacAddress == temp.BlueToothMacAddress).First();
                //    deviceToUpdate.CompanionDevice = temp;
                //    SaveCompanionDeviceList();
                //}
            }
            catch { }
        }

        //update viewModel
        public static async Task UpdateCompanionDeviceViewModel(CompanionDeviceEventArgs input, CompanionDeviceViewModel temp)
        {
            try
            {
                temp.CompanionDevice.WifiMacAddress = input.ConnectedDeviceMacAddress;
                temp.CompanionDevice.WiFiDeviceName = input.ConnectedDeviceName;
                temp.CompanionDevice.WiFiPrimaryDeviceType = input.WiFiPrimaryDeviceType;
                temp.CompanionDevice.WiFiSecondaryDeviceType = input.WiFiSecondaryDeviceType;

                //if (LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.BlueToothMacAddress == temp.CompanionDevice.BlueToothMacAddress).Any())
                //{
                //    CompanionDeviceViewModel deviceToUpdate = LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice.BlueToothMacAddress == temp.CompanionDevice.BlueToothMacAddress).Single();

                //    deviceToUpdate = temp;

                //    SaveCompanionDeviceList();
                //}
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
        }

        public static async Task<CompanionDeviceViewModel> WiFiInformationRequest(PacketHeaderModel input)
        {
            CompanionDeviceViewModel temp = new CompanionDeviceViewModel();
            try
            {
                IWiFiService wifi = DependencyService.Get<IWiFiService>();
                CompanionDeviceEventArgs data = wifi.WiFiInformationRequest(input.DeviceUniqueIdentifier, input.BlueToothDeviceName, input.BlueToothDeviceAlias, input.BlueToothDeviceType);
                //now use companion device data to find the correct device
                //temp = LocalHardwareStaticDetails.PairedDeviceViewModelList
                //    .Where(i => i.CompanionDevice.BlueToothMacAddress == data.BlueToothMacAddress)
                //    .Single();
                //temp = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                //    .Where(i => i.CompanionDevice.BlueToothMacAddress == data.BlueToothMacAddress)
                //    .Single();
                // KEYED ON THE UNIQUE IDENTIFIER, not the Bluetooth name. This lookup used to be
                // `.Where(BlueToothName == data.BlueToothName).Single()`, which was the SECOND
                // Bluetooth-name gate on this path and would have thrown for a numerically-paired
                // device even if the caller's guard had let it through: such a device has no
                // Bluetooth name, and Single() throws both when nothing matches and when several
                // do. Every record lacking a Bluetooth name would have matched every other one.
                //
                // FirstOrDefault plus an explicit null check rather than Single(), because "no
                // device to update" is an ordinary outcome here, not an exception. The service
                // call above already returns empty data when it cannot identify a peer.
                temp = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    .Where(i => i.CompanionDevice.UniqueIdentifier == input.DeviceUniqueIdentifier)
                    .FirstOrDefault();

                if (temp == null)
                {
                    Console.WriteLine("WiFiInformationRequest: no device known by identifier '"
                        + input.DeviceUniqueIdentifier + "', nothing to update");
                    return null;
                }

                if (string.IsNullOrEmpty(data.WifiMacAddress))
                {
                    // Writing an empty MAC would be worse than writing nothing: the empty value is
                    // what the guard upstream uses to decide this device still needs resolving, so
                    // storing it changes nothing while making the next attempt look identical.
                    Console.WriteLine("WiFiInformationRequest: WiFi identity not resolvable yet for '"
                        + input.DeviceUniqueIdentifier + "', leaving it blank to retry");
                    return temp;
                }

                temp.CompanionDevice.WifiMacAddress = data.WifiMacAddress;
                temp.CompanionDevice.WiFiDeviceName = data.WiFiDeviceName;
                temp.CompanionDevice.WiFiPrimaryDeviceType = data.WiFiPrimaryDeviceType;
                temp.CompanionDevice.WiFiSecondaryDeviceType = data.WiFiSecondaryDeviceType;
                if (string.IsNullOrEmpty(temp.CompanionDevice.UniqueIdentifier))
                {
                    temp.CompanionDevice.UniqueIdentifier = input.DeviceUniqueIdentifier;
                }
                Console.WriteLine("WiFi identity resolved for '" + input.DeviceUniqueIdentifier
                    + "': mac=" + data.WifiMacAddress + " name=" + data.WiFiDeviceName);
                CompanionDeviceContext _context = new CompanionDeviceContext();
                await _context.Update(temp.CompanionDevice);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally { }
            return temp;
        }
        #endregion

        #region Remove device
        internal static void OnCompanionDelete(CompanionDevice device)
        {
            try
            {
                CompanionDeviceViewModel temp = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList
                    .Where(i => i.CompanionDevice == device)
                    .Single();

                CompanionDeviceContext _context = new CompanionDeviceContext();
                LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList = _context.Delete(device).Result;

                //LocalHardwareStaticDetails.PairedDeviceViewModelList.Where(i => i.CompanionDevice == device).Single();
                //LocalHardwareStaticDetails.PairedDeviceViewModelList.Remove(temp);
                ////serialize
                //SaveCompanionDeviceList();
            }
            catch { }
        }
        internal static void OnCompanionDelete(CompanionDeviceViewModel device)
        {
            try
            {
                OnCompanionDelete(device.CompanionDevice);


                //LocalHardwareStaticDetails.PairedDeviceViewModelList.Remove(device);
                //serialize
                //SaveCompanionDeviceList();
            }
            catch { }
        }
        #endregion

        #region save list
        /// <summary>
        /// save the paired device list
        /// </summary>        
        internal static void SaveCompanionDeviceList()
        {
            //serialize
            List<CompanionDevice> listToSave = new List<CompanionDevice>();
            foreach (var i in LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList)
            {
                listToSave.Add(i.CompanionDevice);
            }
            string serializedString = Newtonsoft.Json.JsonConvert.SerializeObject(listToSave);
            //string serializedString = Newtonsoft.Json.JsonConvert.SerializeObject(StaticDetails.PairedDeviceViewModelList);
            //send jobject to file manager
            FileManager _fileManager = new FileManager();
            bool written = _fileManager.Set(serializedString, Path.Combine(FileSystem.BasePath, "CompanionDeviceList.json"));
        }
        #endregion

        #region open saved devices
        /// <summary>
        /// open the paired device list
        /// </summary>        
        //internal static void OpenCompanionDeviceList()
        //{
        //    LocalJSONHandler _jSONHandler = new LocalJSONHandler();
        //    FileManager _fileManager = new FileManager();
        //    try
        //    {
        //        string deviceListString = _fileManager.GetFileContent(FileSystem.BasePath, "CompanionDeviceList.json");
        //        _jSONHandler.DeserialiseDeviceListJSON(deviceListString);
        //    }
        //    catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

        //}
        #endregion

        #region Header testing - old
        //public static async Task TestHeader(PacketHeaderModel input)
        //{
        //    try
        //    {
        //        //run through bluetooth mac address and wifimacaddresses
        //        if (input.BodyType == BodyType._initalize)
        //        {
        //            if (StaticDetails.PairedDeviceViewModelList
        //                .Where(i =>
        //                i.CompanionDevice.BlueToothAlias == input.BlueToothDeviceAlias
        //                && i.CompanionDevice.BlueToothName == input.BlueToothDeviceName
        //                && i.CompanionDevice.BlueToothType == input.BlueToothDeviceType
        //                && (i.CompanionDevice.WifiMacAddress == null || i.CompanionDevice.WifiMacAddress == string.Empty)
        //                && (i.CompanionDevice.UniqueIdentifier == input.DeviceUniqueIdentifier || i.CompanionDevice.UniqueIdentifier == null || i.CompanionDevice.UniqueIdentifier == string.Empty))
        //                .Any())
        //            {

        //                //CompanionDeviceViewModel deviceToUpdate = StaticDetails.PairedDeviceViewModelList
        //                //    .Where(i => //i.CompanionDevice.BlueToothMacAddress == input.BlueToothDeviceAlias)
        //                //    i.CompanionDevice.BlueToothAlias == input.BlueToothDeviceAlias
        //                //&& i.CompanionDevice.BlueToothName == input.BlueToothDeviceName
        //                //&& i.CompanionDevice.BlueToothType == input.BlueToothDeviceType
        //                //&& (i.CompanionDevice.WifiMacAddress == null || i.CompanionDevice.WifiMacAddress == string.Empty)
        //                //&& (i.CompanionDevice.UniqueIdentifier == input.DeviceUniqueIdentifier || i.CompanionDevice.UniqueIdentifier == null))
        //                //    .Single();

        //                CompanionDevice device = await WiFiInformationRequest(input);
        //                Console.WriteLine(StaticDetails.PairedDeviceViewModelList);
        //                SaveCompanionDeviceList();

        //            }
        //        }
        //    }
        //    catch { }
        //}

        #endregion

    }
}
