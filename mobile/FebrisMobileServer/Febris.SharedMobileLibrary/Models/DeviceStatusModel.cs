// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models
{
    public class DeviceStatusModel
    {
        public string BlueToothDeviceName { get; set; }
        //public int BatteryCharge { get; set; }


    }
    public class ClientSocketCreationCheckEventArgs
    {        
        public bool Success { get; set; }

        public ClientSocketCreationCheckEventArgs()
        {
            Clear();
        }
        public void Clear()
        {
            Success = false;            
        }
    }
}
