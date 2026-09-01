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
        public static void Connected()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Connection To Server Made";
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = true;
        }
        public static void Disconnected()
        {
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Disconnected From Server";
            LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = false;
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
