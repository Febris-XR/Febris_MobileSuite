// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Enums
{
    public enum UploadType
    {
        None,
        Statement,
        Video,
        Module
    }
    public enum DownloadType
    {
        None,
        Statement,
        Video,
        Module
    }

    public enum ConnectionStatus
    {
        Connected = 0,
        Invited = 1,
        Failed = 2,
        Available = 3,
        Unavailable = 4,
        Unknown = 5
    }
}
