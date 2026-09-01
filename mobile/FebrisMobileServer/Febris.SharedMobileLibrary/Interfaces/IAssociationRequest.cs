// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface IAssociationRequest
    {
        Task MakeRequest();
    }
    public interface IBluetoothRequest
    {
        Task MakePairingRequest();
        Task SendUpload(string deviceAddress);
        Task SendUpload(string deviceAddress,string fileName);
        Task Reconnect(string deviceAddress);

    }
}
