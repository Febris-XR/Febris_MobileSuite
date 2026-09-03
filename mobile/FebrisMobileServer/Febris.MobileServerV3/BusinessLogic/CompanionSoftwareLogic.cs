// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.APIInteractions;
using Febris.MobileServerV3.DataLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Models.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.BusinessLogic
{
    public class CompanionSoftwareLogic
    {
        //private readonly IWiFiP2pServer _wiFiP2PServer;
        //private readonly ModuleLogic _moduleContext;
        //private readonly List<Guid> _moduleOnServerList;
        private readonly CompanionAppRequest _apiRequest;
        private readonly CompanionAppContext _context;

        public CompanionSoftwareLogic()
        {
            //_wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
            //wifi = DependencyService.Get<IWiFiService>();
            //_moduleContext = new ModuleLogic();
            //_moduleOnServerList = _moduleContext.GetNameList();
            _apiRequest = new CompanionAppRequest();
            _context = new CompanionAppContext();
        }

        public async Task<CompanionAppViewModel> Get()
        {
            try
            {
                LocalSoftwarePackage currentVersion = await _context.GetLocalVersionInfo();//.GetCurrentInfo();
                bool fileExists = await _context.FileExists(currentVersion.UUID);

                CompanionAppViewModel output = new CompanionAppViewModel()
                {
                    LocalSoftwarePackage = currentVersion,
                    ExistsInSystem = fileExists
                };
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //throw;
            }
            return default;
        }



        //internal

        /// <summary>
        /// Move to status update
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckVersion()
        {
            bool output = false;
            bool downloaded = false;
            bool savedInfo = false;
            bool fileExists = false;
            try
            {
                LocalSoftwarePackage localVersion = await _context.GetLocalVersionInfo();
                LocalSoftwarePackage latestVersion = await _apiRequest.GetLatestVersion();

                // NULL-SAFETY FIX (2026-07-29). This method previously repeated the comparison
                //     localVersion?.UUID != latestVersion.UUID && latestVersion != default
                // three times, and every copy dereferenced latestVersion BEFORE the null guard that
                // was supposed to protect it. C# evaluates left to right, so a node with no
                // Companion package published threw NullReferenceException here rather than
                // reporting that nothing was available. The explicit guard that would have caught it
                // sat commented out directly above. The `?? default` on both declarations looked like
                // a null check but is a no-op on a reference type, which is part of why this read as
                // safe.
                //
                // The API side cannot fix this. APIRequestFactory.MakeStringRequest lets WebRequest
                // throw on any 4xx and returns (string.Empty, InternalServerError) from its catch, so
                // a 404 arrives as an empty body that deserializes to null exactly as a
                // 200-with-null body did. The guard has to live here.
                //
                // The `else` branches were unsafe in the mirror-image way: they dereferenced
                // localVersion, which is null on any device that has never held a package.
                //
                // See docs/OSS_CLIENT_DISTRIBUTION.md section 3.7.
                if (latestVersion == null && localVersion == null)
                {
                    PairingPageStatusHelper.GenericMessage("No companion package is available from the server yet.");
                    return output;
                }

                // Decide once which package should be on disk instead of re-deriving it three times.
                // Prefer the upstream package whenever it differs from what is held locally.
                // When useLatest is false, localVersion is provably non-null: the only routes here
                // are latestVersion == null (so localVersion != null, per the guard above) or the two
                // UUIDs matching, which a null localVersion cannot do.
                // VERSION, not just uuid (2026-09-02). A release KEEPS its uuid across versions by
                // design, because CLIENT_RELEASE_GUIDE.md line 241 tells publishers to keep it and
                // change the version and artifact. So a uuid comparison alone can never notice an
                // update. The node would ingest 0.2.1 and this device would serve 0.2.0 forever,
                // because the uuids matched and the file for that uuid was already on disk.
                // See docs/OSS_CLIENT_DISTRIBUTION.md section 3.6, which called this out in writing.
                //
                // Difference, not "newer". The node is the authority on what it serves, and it
                // resolves latest itself. A client-side ordering rule here would fight that and
                // could strand a device when an operator deliberately rolls a release back.
                bool versionChanged = latestVersion != null
                    && localVersion != null
                    && !string.Equals(localVersion.Version, latestVersion.Version, StringComparison.OrdinalIgnoreCase);

                bool useLatest = latestVersion != null
                    && (localVersion?.UUID != latestVersion.UUID || versionChanged);
                Guid targetUUID = useLatest ? latestVersion.UUID : localVersion.UUID;

                // SAME uuid, NEW version. Both copies on disk are keyed by uuid, so they look
                // current while holding the previous release. Clear them or the download below is
                // skipped and the whole comparison above achieves nothing.
                if (versionChanged && localVersion.UUID == targetUUID)
                {
                    PairingPageStatusHelper.GenericMessage(
                        "A newer companion package is available. Replacing the local copy.");
                    await _context.DeleteCompanionAppVersion(targetUUID);
                }

                fileExists = await _context.FileExists(targetUUID);

                if (!fileExists)
                {
                    PairingPageStatusHelper.GenericMessage("File does not exist on the system and needs to be downloaded. Currently attempting.");
                    downloaded = await _apiRequest.Download(targetUUID);
                    fileExists = await _context.FileExists(targetUUID);

                    // Previously announced success unconditionally, including immediately after a
                    // download had failed.
                    if (fileExists)
                    {
                        PairingPageStatusHelper.GenericMessage("Download Successful.");
                    }
                }

                if (!fileExists)
                {
                    return output;
                }

                if (downloaded && latestVersion != null)
                {
                    savedInfo = await _context.SaveInfo(latestVersion);

                    // Only ever delete a PREVIOUS package. Two guards, both of which were missing.
                    // localVersion is null on a first-ever install, so the old unconditional
                    // DeleteFile(localVersion.UUID) threw on the very first SUCCESSFUL download. And
                    // when the local and target UUIDs match, deleting localVersion would delete the
                    // file that had just been downloaded.
                    if (localVersion != null && localVersion.UUID != targetUUID)
                    {
                        bool deleted = await DeleteFile(localVersion.UUID);
                    }
                }

                output = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            return output;
        }


        public async Task<bool> DeleteFile(Guid input)
        {
            bool output = false;
            //bool downloaded = false;
            //bool savedInfo = false;
            bool fileExists = false;
            try
            {
                LocalSoftwarePackage currentVersion = await _context.GetLocalVersionInfo();//.GetCurrentInfo();

                // NULL-SAFETY FIX (2026-07-29): currentVersion was dereferenced unguarded.
                // GetLocalVersionInfo returns null on a device that has never recorded a package, so
                // this threw rather than reporting that there was nothing to delete. Nothing to
                // delete is the normal state on a first install, not an error.
                if (currentVersion == null)
                {
                    output = true;
                    return output;
                }

                // Note this checks whether the CURRENTLY RECORDED package's file is present and then
                // deletes `input`, which is a different UUID whenever the caller is retiring an old
                // package. Preserved as-is because changing it alters behaviour beyond a null fix,
                // but it looks wrong and is worth revisiting. Logged in MOBILE_KNOWN_ISSUES.
                fileExists = await _context.FileExists(currentVersion.UUID);
                if (fileExists)
                {
                    await _context.DeleteOldCompanionApp(input);
                }
                output = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            return output;
        }
    }
}
