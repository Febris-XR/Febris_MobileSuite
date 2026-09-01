// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.P2pCommunication.WiFi;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Febris.MobileServerV3.Utilities
{
    public class RequestHelper
    {
        IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();

        public static async Task VideoStreamRequest(CompanionDeviceViewModel input)
        {
            IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
            PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
            {
                PacketName = "Start Video Stream",
                BodyType = BodyType._videoStream
            };
            string arguments = "";
            // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
            byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
            input.VideoStreamConnected = await _wiFiP2PServer.SocketSender(dataPackage, input);
        }

        public static async Task EndVideoStreamRequest(CompanionDeviceViewModel selected)
        {
            IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
            PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
            {
                // Was mislabelled "Start Video Stream", which made the two requests
                // indistinguishable in a packet log.
                PacketName = "End Video Stream",
                BodyType = BodyType._endVideoStream
            };
            string arguments = "";
            // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
            byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
            await _wiFiP2PServer.SocketSender(dataPackage, selected);

            // NOT assigned from the send result. Nothing anywhere set this false, and assigning
            // it here from "did the stop request reach the socket" left it TRUE on a successful
            // stop, so the operator's status line read connected for a stream that had just
            // been told to end. The stream is over either way: if the request failed to send,
            // it is over even harder.
            selected.VideoStreamConnected = false;
        }

        internal static async Task RemoveModuleRequest(CompanionDeviceViewModel selected, string moduleId)
        {
            try
            {
                IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {
                    PacketName = moduleId,
                    BodyType = BodyType._removeModule
                };
                string arguments = "";
                // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
                bool output = await _wiFiP2PServer.SocketSender(dataPackage, selected);
                selected.StatusMessage = "Module Removal Success: " + output;
            }
            catch (Exception ex)
            {
                selected.StatusMessage = "Error Removing Module: " + ex.Message;
                throw;
            }

            //throw new NotImplementedException();
        }
        
        internal static async Task RemoveZippedModuleRequest(CompanionDeviceViewModel selected, string moduleId)
        {
            try
            {
                IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {
                    PacketName = moduleId,
                    BodyType = BodyType._removeZippedModule
                };
                string arguments = "";
                // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
                bool output = await _wiFiP2PServer.SocketSender(dataPackage, selected);
                selected.StatusMessage = "Module Removal Success: " + output;
            }
            catch (Exception ex)
            {
                selected.StatusMessage = "Error Removing Module: " + ex.Message;
                throw;
            }
        }

        internal static async Task UploadModuleRequest(CompanionDeviceViewModel selected)
        {
            throw new NotImplementedException();
        }



        internal static async Task UninstallModuleRequest(CompanionDeviceViewModel selected, string moduleId)
        {
            try
            {
                IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {
                    PacketName = moduleId,
                    BodyType = BodyType._uninstallModule
                };
                string arguments = "";
                // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
                bool output = await _wiFiP2PServer.SocketSender(dataPackage, selected);
                selected.StatusMessage = "Module Uninstall Success: " + output;
            }
            catch (Exception ex)
            {
                selected.StatusMessage = "Error Uninstall Module: " + ex.Message;
                throw;
            }
        }
        internal static async Task InstallModuleRequest(CompanionDeviceViewModel selected, string moduleId)
        {
            try
            {
                IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {
                    PacketName = moduleId,
                    BodyType = BodyType._installModule
                };
                string arguments = "";
                // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
                bool output = await _wiFiP2PServer.SocketSender(dataPackage, selected);
                selected.StatusMessage = "Module Install Success: " + output;
            }
            catch (Exception ex)
            {
                selected.StatusMessage = "Error Install Module: " + ex.Message;
                throw;
            }

            //throw new NotImplementedException();
        }

        internal static async Task DeleteOldStatementsRequest(CompanionDeviceViewModel selected)
        {
            try
            {
                IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {                    
                    BodyType = BodyType._oldStatements
                };
                string arguments = "";
                // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
                bool output = await _wiFiP2PServer.SocketSender(dataPackage, selected);
                selected.StatusMessage = "Old Statement Removal Success: " + output;
            }
            catch (Exception ex)
            {
                selected.StatusMessage = "Error Removing Old Statements: " + ex.Message;
                throw;
            }
        }

        internal static async Task DeleteOldVideosRequest(CompanionDeviceViewModel selected)
        {
            try
            {
                IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {                    
                    BodyType = BodyType._oldVideos
                };
                string arguments = "";
                // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
                bool output = await _wiFiP2PServer.SocketSender(dataPackage, selected);
                selected.StatusMessage = "Old Video Removal Success: " + output;
            }
            catch (Exception ex)
            {
                selected.StatusMessage = "Error Removing Old Videos: " + ex.Message;
                throw;
            }
        }

        internal static async Task ReinstallModuleRequest(CompanionDeviceViewModel selected, string moduleId)
        {
            try
            {
                IWiFiP2pServer _wiFiP2PServer = DependencyService.Get<IWiFiP2pServer>();
                PacketHeaderModel packetHeaderModel = new PacketHeaderModel()
                {
                    PacketName = moduleId,
                    BodyType = BodyType._reinstallModule
                };
                string arguments = "";
                // MP2P-2: shared FebrisP2pFrameBuilder. String overload uses UTF-8; parameter order flipped from legacy (header, body).
                byte[] dataPackage = new FebrisP2pFrameBuilder().Build(packetHeaderModel, arguments);
                bool output = await _wiFiP2PServer.SocketSender(dataPackage, selected);
                selected.StatusMessage = "Module Reinstall Success: " + output;
            }
            catch (Exception ex)
            {
                selected.StatusMessage = "Error Reinstalling Module: " + ex.Message;
                throw;
            }

            //throw new NotImplementedException();
        }
    }
}
