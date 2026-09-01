// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.APIInteractions;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.SharedMobileLibrary.Models.ViewModels;
using Febris.SharedMobileLibrary.P2pNetworking;
using Febris.SharedMobileLibrary.Services;
using Febris.SharedMobileLibrary.Utilites;
using Febris.ModelLibrary.Models.XApiModels;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.BusinessLogic
{
    public class StatementLogic
    {
        private ILogger _log;
        //private IConfiguration _config;
        private readonly JSONHandler _jSONHandler;
        private readonly FileManager _fileManager;
        private readonly StatementRequest _statementRequest;
        private readonly IWiFiP2pServer _wiFiP2PServer;

        public StatementLogic(ILogger log)
        {
            _log = log;
            _jSONHandler = new JSONHandler();// _log);
            _fileManager = new FileManager();// _log);//, _config);
            _statementRequest = new StatementRequest(null,null);
            _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();

        }
        public StatementLogic()
        {            
            _jSONHandler = new JSONHandler();// _log);
            _fileManager = new FileManager();// _log);//, _config);
            _statementRequest = new StatementRequest(null, null);
            _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
        }

        public async Task<object> ProcessFiles()
        {
            bool output = false;
            try
            {
                var unsentStatementList = await _fileManager.GetDirectoryContentNames(FileSystem.StatementPath);
                if (unsentStatementList.Count() > 0)
                {
                    foreach (var i in unsentStatementList)
                    {
                        //gather file content
                        string tempFile = _fileManager.GetFileContent(FileSystem.StatementPath, i);
                        //send it
                        bool sendComplete = _statementRequest.UploadStatement(tempFile).Result;
                        if (!sendComplete)
                        {
                            sendComplete = _statementRequest.UploadStatementBackup(tempFile).Result;
                        }


                        if (sendComplete)
                        {
                            _fileManager.MoveStatementFileToSent(i);
                        }
                    }

                }

            }
            catch (Exception e)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = e.Message;
            }
            return output;
        }



        public async Task<StatementInitalizationResponseViewModel> StatmentInitalizationRequest()
        {
            StatementInitalizationResponseViewModel output = new StatementInitalizationResponseViewModel();
            try
            {
                //Run StatusUpdate progress page

                CompanionDeviceViewModel device = LocalHardwareStaticDetails.StaticMainVM.HardwareVM.SelectedItem;

                output = await _statementRequest.StatmentInitalizationRequest(
                    LocalHardwareStaticDetails.StaticMainVM.UserVM.SelectedUser,
                    LocalHardwareStaticDetails.StaticMainVM.ModuleVM.SelectedModule
                    );

                // The API is the first of two stages and the one that fails on a bench with no
                // backend. Say which stage failed rather than dereferencing null below.
                if (output?.Statement == null)
                {
                    Console.WriteLine("launch: statement initialization returned no statement "
                        + "(API unreachable, or the user/module was rejected)");
                    return output;
                }

                bool complete = await ForwardToHeadset(output.Statement, device, LocalHardwareStaticDetails.StaticMainVM.ModuleVM.SelectedModule);
                if (!complete)
                {
                    Console.WriteLine("launch: the session statement could not be sent to the device");
                }

                return output;
            }
            catch (Exception ex)
            {
                // Was a bare rethrow with the message discarded, so the stage that failed was
                // never recorded anywhere.
                Console.WriteLine("launch: StatmentInitalizationRequest failed: " + ex.Message);
                throw;
            }
        }

        private async Task<bool> ForwardToHeadset(Statement statement, CompanionDeviceViewModel device, Module module)
        {
            bool output = false;
            try
            {
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {
                    PacketName = module.UUID.ToString(),
                    BodyType = BodyType._statement
                };
                // ROADMAP 22: the "remove record option if not selected" block that sat here is
                // GONE. It stripped the node's video attachment out of the statement whenever the
                // Server's local RecordSession checkbox was clear, which let the device veto the
                // educator's recording decision after the fact. The decision is now derived
                // node-side from the educator's per-cohort policy and travels as the attachment
                // itself, so the statement is forwarded to the Companion exactly as the node
                // built it.
                //
                // NOTE, so nobody reads more into this than it does: the mobile tier still starts
                // no recorder. Removing this veto stops the Server from CONTRADICTING the node; it
                // does not make a headset record. That capability does not exist on this tier yet.
                string stringStatement = JsonConvert.SerializeObject(statement);
                string arguments = P2PSharedDetails.StatementPreface + stringStatement;
                // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8 (legacy ASCII corrupted non-Latin chars).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);

                // AWAITED, and the result actually returned. This was
                //     Task.Run(() => _wiFiP2PServer.SocketSender(dataPackage, device));
                //     return output;              // output was declared false and never assigned
                // so the send was fire-and-forget AND the method reported failure even when the
                // frame went out. Success was literally unrepresentable, which is a large part of
                // why launching a session looked broken: there was no path by which it could ever
                // report having worked.
                output = await _wiFiP2PServer.SocketSender(dataPackage, device);
                Console.WriteLine("launch: forwarded session statement for module "
                    + module.UUID + ", sent=" + output);
                return output;
            }
            catch (Exception ex)
            {
                device.StatusMessage = ex.Message;
                throw;
            }            
        }
        
    }
}
