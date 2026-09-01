// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.P2pCommunication.WiFi
{
    public interface IWiFiP2pServer
    {
        //Task<bool> SocketSender(byte[] dataPacket, string clientAddress);
        Task<bool> SocketSender(byte[] dataPacket);
    }
}
