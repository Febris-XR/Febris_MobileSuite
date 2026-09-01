// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.P2pNetworkModels.WiFi
{
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

    public class HardwareStatusUpdate
    {
        [JsonProperty("ClientUniqueId")]
        public string ClientUniqueId { get; set; }
        [JsonProperty("BatteryCharge")]
        public double BatteryCharge { get; set; }
        [JsonProperty("StorageSpaceRemaining")]
        public long StorageSpaceRemaining { get; set; }       
        [JsonProperty("StatementFileList")]
        public List<string> StatementFileList { get; set; }
        [JsonProperty("VideoFileList")]
        public List<string> VideoFileList { get; set; }
        [JsonProperty("OldStatementFileList")]
        public List<string> OldStatementFileList { get; set; }
        [JsonProperty("OldVideoFileList")]
        public List<string> OldVideoFileList { get; set; }

        [JsonProperty("ModuleFileList")]
        public List<string> ModuleFileList { get; set; }
        [JsonProperty("AppList")]
        public List<string> AppList { get; set; }
        [JsonProperty("ZippedFileList")]
        public List<string> ZippedFileList { get; set; }


        public HardwareStatusUpdate()
        {
            Clear();
        }

        public void Clear()
        {
            AppList = default;
            ZippedFileList = default;
            ModuleFileList = null;
            StatementFileList = null;
            VideoFileList = null;
            OldStatementFileList = null;
            OldVideoFileList = null;
            ClientUniqueId = "";
            BatteryCharge = 0;
            StorageSpaceRemaining = 0;
        }
    }
    #endregion   

    public class P2pServerInfo
    {
        public string GroupOwnerAddress { get; set; }

        public string GroupOwnerMac { get; set; }

        public string GroupInterface { get; set; }




    }
}
