// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Febris.SharedMobileLibrary.Utilites
{
    //Needs work
    public class ProcessUtilites
    {
        public void StartProcess(ProcessOptions processType)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = processType.ToString(),
                    CreateNoWindow = true,

                }

            };
            process.Start();
        }

        public void StopProcess(ProcessOptions processType)
        {
            string processName = Path.GetFileNameWithoutExtension(processType.ToString());

            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill();
            }
        }

    }
}
