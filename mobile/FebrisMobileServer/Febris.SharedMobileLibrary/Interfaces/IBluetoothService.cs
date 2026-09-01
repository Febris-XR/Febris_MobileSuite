// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface IBluetoothService
    {
        event EventHandler<BTCompanionDeviceEventArgs> BTConnectionUploadSuccessAction;
        event EventHandler<BTCompanionDeviceEventArgs> BTConnectionSuccessAction;
        event EventHandler<BTCompanionDeviceEventArgs> BTConnectionChangedAction;

        void SendUpload(string deviceAddress);

        void Reconnect(string deviceAddress);
    }

    #region event arguments

    public class BTCompanionDeviceEventArgs
    {
        public string DeviceAddress { get; set; }
        public bool ConnectSuccess { get; set; }
        public string ConnectionStatus { get; set; }
        public bool UploadSuccess { get; set; }

        public string ReceivedMessage { get; set; }
        public string ReceivedError { get; set; }
        public string ReceivedFrom { get; set; }

        public string SendMessage { get; set; }
        public string SendError { get; set; }
        public string SendTo { get; set; }


        public BTCompanionDeviceEventArgs()
        {
            DeviceAddress = "";
            ConnectionStatus = "";
            ConnectSuccess = false;
            UploadSuccess = false;

            ReceivedMessage = "";
            ReceivedError = "";
            ReceivedFrom = "";

            SendMessage = "";
            SendError = "";
            SendTo = "";
        }
    }
    #endregion
}
