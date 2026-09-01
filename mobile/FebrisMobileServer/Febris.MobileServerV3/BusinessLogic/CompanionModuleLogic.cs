// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.BusinessLogic
{
    public class CompanionModuleLogic
    {
        private readonly IWiFiP2pServer _wiFiP2PServer;
        private readonly ModuleLogic _moduleContext;
        private readonly List<Guid> _moduleOnServerList;

        public CompanionModuleLogic()
        {
            _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
            //wifi = DependencyService.Get<IWiFiService>();
            _moduleContext = new ModuleLogic();

            // NAME IS MISLEADING AND IT COST AN Hour. _moduleOnServerList reads as "the modules
            // on this Server's filesystem". It is not: ModuleLogic.GetNameList() calls
            // ModuleRequest.ModuleListRequest(), which is an API GET to "getmoduleidlist". So
            // BOTH inputs to the distribution decision are network calls, and neither consults
            // the directory the archives actually live in.
            try
            {
                _moduleOnServerList = _moduleContext.GetNameList() ?? new List<Guid>();
            }
            catch (Exception ex)
            {
                // .Result on an unreachable endpoint throws an AggregateException. Previously this
                // propagated out of a constructor called from a non-awaited scan, so it vanished.
                Console.WriteLine("module scan: the module id list could not be fetched: " + ex.Message);
                _moduleOnServerList = new List<Guid>();
            }

#if DEBUG
            // BENCH BYPASS, DEBUG ONLY, never compiled into Release.
            //
            // The production flow is: download the archive from the API, store it on the
            // filesystem, then distribute it from the filesystem over WiFi Direct. Only the first
            // step needs the network, and an archive placed on disk by hand is indistinguishable
            // from a downloaded one by the time distribution runs. So when the API cannot be
            // reached, fall back to the directory the archives are really in, which is the same
            // source LocalFileRepositoryPage reads for the "Files On System" panel.
            if (_moduleOnServerList.Count == 0)
            {
                try
                {
                    var onDisk = new SharedMobileLibrary.FileSystem.FileManager()
                        .GetDirectoryContentNames(SharedMobileLibrary.FileSystem.FileSystem.ZippedModulePath)
                        .Result ?? new List<string>();

                    foreach (string entry in onDisk)
                    {
                        // Archives are stored as "<UUID>.zip", so strip the extension before
                        // parsing. Anything that is not a GUID is ignored rather than throwing.
                        string bare = System.IO.Path.GetFileNameWithoutExtension(entry);
                        if (Guid.TryParse(bare, out Guid id) && !_moduleOnServerList.Contains(id))
                        {
                            _moduleOnServerList.Add(id);
                        }
                    }

                    Console.WriteLine("module scan (DEBUG bypass): no API module list, found "
                        + _moduleOnServerList.Count + " archive(s) on disk in "
                        + SharedMobileLibrary.FileSystem.FileSystem.ZippedModulePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("module scan (DEBUG bypass) failed to read the module directory: " + ex.Message);
                }
            }
#endif
        }

        /// <summary>
        /// Move to status update
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ScanForNeededModuleUploads()
        {
            bool output = false;
            foreach (var i in LocalHardwareStaticDetails.StaticMainVM.HardwareVM.ItemList)
            {
                if (i.WiFiConnected)
                {
                    List<Guid> neededModules = await CheckAllModulesInList(i);

                    //Forward need modules to headsets
                    ForwardModulesToHeadset(neededModules, i).Wait();

                }
            }
            output = true;
            return output;
        }

        public async Task<bool> ScanForNeededModuleUploads(MVVM.ViewModel.CompanionDeviceViewModel model)
        {
            bool output = false;
            if (model.WiFiConnected)
            {
                List<Guid> neededModules = await CheckAllModulesInList(model);

                //Forward need modules to headsets
                ForwardModulesToHeadset(neededModules, model).Wait();

            }

            output = true;
            return output;
        }

        private async Task<List<Guid>> CheckAllModulesInList(MVVM.ViewModel.CompanionDeviceViewModel input)
        {
            List<Guid> output = new List<Guid>();
            List<Guid> expectedModuleIdList = new List<Guid>();
            List<Guid> filteredModuleList = new List<Guid>();

            List<Guid> filesOnDevice = new List<Guid>();
            List<Guid> filesExpectedToBeOnDevice = new List<Guid>();
            //List<Guid> filesCurrentlyOnServer = new List<Guid>();
            try
            {
                List<Febris.SharedMobileLibrary.Models.Data.Module> catalog = LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleList;

#if DEBUG
                // BENCH BYPASS, DEBUG ONLY. Never compiled into Release.
                //
                // The distribution decision needs two things: a CATALOG saying which modules
                // exist, which comes from the API, and the set of archives actually PRESENT on
                // this Server's filesystem, which is _moduleOnServerList from
                // ModuleLogic.GetNameList(). Only the catalog needs the network. The archive is
                // already local, because the production flow is: download from the API, store on
                // the filesystem, distribute over WiFi Direct from the filesystem.
                //
                // So on a bench with no reachable API the catalog is empty and nothing is ever
                // considered "needed", even though a perfectly good archive is sitting on disk
                // ready to send. That made the whole module path look untestable, which it is not.
                //
                // With no catalog, treat every archive on disk as a module that exists. That is
                // exactly what the API would have told us for a hand-placed file, so the rest of
                // the path (needed-set diff, ForwardModulesToHeadset, ProcessModule on the
                // Companion, and the integrity sweep afterwards) runs completely unmodified.
                //
                // To use it: place <UUID>.zip in files/Febris/Modules/ZippedModuleFiles on the
                // Server, then let a Companion send a status update. See
                // docs/MOBILE_KNOWN_ISSUES.md issue 23c for the placement recipe.
                if (catalog == null || catalog.Count == 0)
                {
                    Console.WriteLine("module scan (DEBUG bypass): no API catalog, treating the "
                        + _moduleOnServerList.Count + " archive(s) on disk as the catalog");
                    catalog = _moduleOnServerList
                        .Select(id => new Febris.SharedMobileLibrary.Models.Data.Module { UUID = id })
                        .ToList();
                }
#endif

                if (catalog == null)
                {
                    return output;
                }

                foreach (var i in catalog)
                {
                    if (_moduleOnServerList.Any(j => j == i.UUID))
                    {
                        if (!input.ModuleFileList.Any(k => k.Contains(i.UUID.ToString())))
                        {
                            output.Add(i.UUID);
                        }
                    }
                }

                if (output.Count > 0)
                {
                    Console.WriteLine("module scan: " + output.Count + " module(s) needed by '"
                        + (input.CompanionDevice?.Name ?? "device") + "'");
                }

                return output;
            }
            catch (Exception)
            {
                return output;
            }

        }


        private async Task ForwardModulesToHeadset(List<Guid> neededModules, MVVM.ViewModel.CompanionDeviceViewModel i)
        {
            //gather file data

            //IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();

            foreach (var j in neededModules)
            {
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {
                    PacketName = j.ToString(),
                    BodyType = BodyType._module
                };
                byte[] arguments = await _moduleContext.GetFileContent(j);

                // Logged because a module push had no trace at all, so a zero-byte read or a
                // failed send was indistinguishable from never having tried. These archives are
                // tens of megabytes, so the size is also the quickest sanity check that the
                // right file was loaded.
                Console.WriteLine("module push: sending " + j + " (" + (arguments?.Length ?? 0)
                    + " bytes) to '" + (i.CompanionDevice?.Name ?? "device") + "'");
                if (arguments == null || arguments.Length == 0)
                {
                    Console.WriteLine("module push: ABORTED, archive for " + j + " read as empty");
                    continue;
                }

                // MP2P-2: shared FebrisP2pFrameBuilder. Parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
                bool sent = await _wiFiP2PServer.SocketSender(dataPackage, i);
                Console.WriteLine("module push: " + j + " sent=" + sent);
            }





            //throw new NotImplementedException();
        }


    }
}
