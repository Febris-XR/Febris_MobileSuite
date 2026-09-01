// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    public class Models
    {
        #region event arguments
        //public class CompanionDeviceEventArgs
        //{
        //    public List<string> CompanionDeviceNameList { get; set; }
        //    public bool WiFiEnable { get; set; }
        //    public bool ConnectSuccess { get; set; }
        //    public string ConnectDeviceName { get; set; }
        //    public string ReceivedMessage { get; set; }


        //    public CompanionDeviceEventArgs()
        //    {
        //        CompanionDeviceNameList = null;
        //        WiFiEnable = false;
        //        ConnectSuccess = false;
        //        ConnectDeviceName = "";
        //        ReceivedMessage = "";
        //    }
        //}
        //public class StatusCheckEventArgs
        //{
        //    public string JsonResponse { get; set; }
        //    public string ReceivedFrom { get; set; }

        //    public StatusCheckEventArgs()
        //    {
        //        JsonResponse = "";
        //        ReceivedFrom = "";
        //    }
        //}
        //public class UploadEventArgs
        //{
        //    public string StatementName { get; set; }
        //    public string Statement { get; set; }
        //    public byte[] StatementBody { get; set; }
        //    public UploadType UploadType { get; set; }

        //    public UploadEventArgs()
        //    {
        //        StatementName = "";
        //        Statement = "";
        //        StatementBody = new byte[] { };
        //        UploadType = UploadType.None;
        //    }
        //}
        //public enum UploadType
        //{
        //    None,
        //    Statement,
        //    Video,
        //    Module
        //}
        //public class DownloadEventArgs
        //{
        //    public string StatementName { get; set; }
        //    public string Statement { get; set; }
        //    public byte[] StatementBody { get; set; }
        //    public DownloadType DownloadType { get; set; }

        //    public DownloadEventArgs()
        //    {
        //        StatementName = "";
        //        Statement = "";
        //        StatementBody = new byte[] { };
        //        DownloadType = DownloadType.None;
        //    }
        //}
        //public enum DownloadType
        //{
        //    None,
        //    Statement,
        //    Video,
        //    Module
        //}

        public class ModuleInitalizationEventArgs
        {
            public string Arugument { get; set; }
            public string[] ArgumentArray { get; set; }
            public string ModuleName { get; set; }
            public bool VideoNeeded { get; set; }
            public string VideoName { get; set; }

            public ModuleInitalizationEventArgs()
            {
                Arugument = "";
                ArgumentArray = new string[] { };
                ModuleName = "";
                VideoNeeded = false;
                VideoName = "";
            }
        }
        #endregion

        #region Model Classes
        public class TestMessage
        {
            public string ReceivedMessage { get; set; }
            public string ReceivedError { get; set; }
            public string ReceivedFrom { get; set; }

            public string SendMessage { get; set; }
            public string SendError { get; set; }
            public string SendTo { get; set; }

            public bool IsHost { get; set; }

            public TestMessage()
            {
                Clear();
            }

            public void Clear()
            {
                ReceivedMessage = "";
                ReceivedError = "";
                ReceivedFrom = "";

                SendMessage = "";
                SendError = "";
                SendTo = "";
            }
        }
        public class StatusUpdate
        {

            public List<string> ModuleFileList { get; set; }
            public List<string> StatementFileList { get; set; }
            public List<string> VideoFileList { get; set; }
            public string Address { get; set; }
            public int Socket { get; set; }

            public string ReceivedMessage { get; set; }
            public string ReceivedError { get; set; }
            public string ReceivedFrom { get; set; }

            public string SendMessage { get; set; }
            public string SendError { get; set; }
            public string SendTo { get; set; }

            public bool IsHost { get; set; }

            public StatusUpdate()
            {
                Clear();
            }

            public void Clear()
            {
                ReceivedMessage = "";
                ReceivedError = "";
                ReceivedFrom = "";

                SendMessage = "";
                SendError = "";
                SendTo = "";

                ModuleFileList = null;
                StatementFileList = null;
                VideoFileList = null;
                Address = "";
                Socket = 0;
            }
        }

        public class StartModule
        {
            public string CommandLineString { get; set; }
            public string SendTo { get; set; }
            public bool IsHost { get; set; }
            public StartModule()
            {
                Clear();
            }
            public void Clear()
            {
                CommandLineString = "";
                SendTo = "";
            }

        }
        public class UploadModule
        {
            public byte[] DataArray { get; set; }
            public string FileName { get; set; }
            public string SendTo { get; set; }
            public bool IsHost { get; set; }
            public UploadModule()
            {
                Clear();
            }
            public void Clear()
            {
                FileName = "";
                SendTo = "";
            }
        }
        public class DownloadResults
        {
            public byte[] DataArray { get; set; }
            public string FileName { get; set; }
            public string SendTo { get; set; }
            public bool IsHost { get; set; }
            public DownloadResults()
            {
                Clear();
            }

            public void Clear()
            {
                FileName = "";
                SendTo = "";
            }
        }
        #endregion

    }
}