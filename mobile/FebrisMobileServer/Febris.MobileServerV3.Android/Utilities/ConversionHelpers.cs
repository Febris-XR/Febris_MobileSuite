// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Droid.Utilities
{
    public class ConversionHelpers
    {
        #region Helpers
        public static byte[] ArrayCombinerBuilder(byte[] frontend, byte[] backend)
        {
            byte[] output = new byte[frontend.Length + backend.Length];
            try
            {
                for (int i = 0; i < output.Length; i++)
                {
                    output[i] = i < frontend.Length ? frontend[i] : backend[i - frontend.Length];
                }
            }
            catch { }
            finally { }
            return output;
        }

        /// <summary>
        /// I am unsure if this is any different
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte[] ArrayCombiner(byte[] a, byte[] b)
        {
            if (a == default)
            {
                return b;
            }
            else if (b == default)
            {
                return a;
            }

            byte[] output = new byte[a.Length + b.Length];
            System.Buffer.BlockCopy(a, 0, output, 0, a.Length);
            System.Buffer.BlockCopy(b, 0, output, a.Length, b.Length);
            return output;
        }

        public static byte[] Int2Bytes(int input)
        {
            byte[] output = { };
            try
            {
                output = BitConverter.GetBytes(input);
            }
            catch { }
            finally { }
            return output;
        }

        public static byte[] String2Bytes(string input)
        {
            return Encoding.ASCII.GetBytes(input);
        }
        #endregion
    }
}