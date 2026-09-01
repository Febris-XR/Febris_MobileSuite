// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface IDeviceList
    {
        List<CompanionDevice> DeviceList();
        List<CompanionDevice> PairedDeviceList();
    }
}
