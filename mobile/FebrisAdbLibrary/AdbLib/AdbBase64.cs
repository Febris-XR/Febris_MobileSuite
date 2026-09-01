// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Util;
using Febris.AdbLibrary.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.AdbLibrary.AdbLib
{
    public class AdbBase64 : IAdbBase64
    {
        public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbBase64");
        public string encodeToString(byte[] data)
        {
            try
            {
                var output = Convert.ToBase64String(data);
                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbBase64 encodeToString: " + ex.StackTrace); throw;}
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbBase64 encodeToString: " + ex.StackTrace); throw;
            }
            return default;
        }
        
    }
}
