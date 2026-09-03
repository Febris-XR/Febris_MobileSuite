// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileCompanionV3.Utilities
{
    public class StatusUpdateHelper
    {
        /// <summary>
        /// Raised when the TCP SOCKET opens, not when a Febris session exists.
        ///
        /// <para>
        /// The only caller is WiFiP2pRequestReceiver.ServiceDiscoveryToggleAction, on the branch
        /// taken when discovery stops, and discovery stops because ClientSocketCreation returned
        /// socket.IsConnected. At that instant the far end has sent nothing. It may not be a Febris
        /// peer, it may hold no pairing secret, and it may never answer.
        ///
        /// The message said "Connection To Server Made", which claimed a session the device had no
        /// evidence for. Compare the Mobile Server, which marks a peer connected only once an
        /// accepted Febris frame arrives, and which clears it again after 150 seconds of silence.
        /// The Companion has no such watchdog.
        ///
        /// TEXT ONLY for now. The flag still flips at socket open, so the icons remain as they
        /// were. Making the STATE truthful needs a third state and is a separate change.
        /// </para>
        /// </summary>
        public static void Connected()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Linked to server. Waiting for it to respond.";
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = true;
        }
        /// <summary>
        /// Raised when an accepted Febris frame arrives from the server. Idempotent, because it is
        /// called on EVERY inbound frame and most of them arrive while it is already true.
        /// </summary>
        public static void ServerResponding()
        {
            if (!LocalHardwareStaticDetails.StaticMainVM.HomeVM.ServerResponding)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Connected to server.";
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.ServerResponding = true;
            }
        }
        public static void Disconnected()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Disconnected From Server";
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = false;
            // Cleared with the transport flag rather than on any teardown path of its own. The
            // teardown paths in WiFiP2pServer are not all genuine disconnects. A SUPERSEDED
            // connection also runs the finally at :340, and demoting there would show a dead link
            // while a live replacement was already carrying traffic.
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.ServerResponding = false;
        }
        public static void UploadingData()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Uploading Data To Server";
        }
        public static void DownloadingData()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Downloading Data From Server";
        }

        internal static void ServerConnectionInitalizing()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Connection Initalizing";            
        }

        internal static void VideoStreamRunning()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Video Stream Enabled";
        }

        internal static void VideoStreamEnded()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Video Stream Ended";            
        }

        internal static void Blank()
        {

            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = string.Empty;            
        }

        internal static void ProcessingData()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Processing Data";            
        }

        internal static void ProcessingComplete()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Data Done Processing";            
        }

        internal static void FileNotSent()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "File Upload Failure";
        }

        public static void Error(string message)
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error: "+message;            
        }

        public static void General(string message)
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = message;
        }
    }
}
