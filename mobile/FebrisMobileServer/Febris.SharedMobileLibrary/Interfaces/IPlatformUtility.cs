// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Interfaces
{
    public interface IPlatformUtility
    {
        Task RestartApplication();

        Task QuitApplication();
    }
}
