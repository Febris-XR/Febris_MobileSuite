// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.Utilities
{
    public class PairingPageStatusHelper
    {
        public static void GenericMessage(string input)
        {
            try
            {
                LocalHardwareStaticDetails.StaticMainVM.HardwareVM.PairingStatus = input;
            }
            catch { }
        }

        public static void BluetoothConnected()
        {
            try
            {
                LocalHardwareStaticDetails.StaticMainVM.HardwareVM.PairingStatus = "Bluetooth Connection Made";
            }
            catch { }
        }

        public static void FileUploadSuccess()
        {
            try
            {
                LocalHardwareStaticDetails.StaticMainVM.HardwareVM.PairingStatus = "Companion Application Upload Succeeded";
            }
            catch { }
        }

        public static void FileUploadFailed()
        {
            try
            {
                LocalHardwareStaticDetails.StaticMainVM.HardwareVM.PairingStatus = "Companion Application Upload Failed";
            }
            catch { }
        }
    }
}
