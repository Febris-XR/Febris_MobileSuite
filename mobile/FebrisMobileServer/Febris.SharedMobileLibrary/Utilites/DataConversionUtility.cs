// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Utilites
{
    public class DataConversionUtility
    {

        public static async Task<byte[]> String2Bytes(string input)
        {
            byte[] output = default;
            try
            {
                output = Encoding.ASCII.GetBytes(input);
                //// Create a UTF-8 encoding instance
                //Encoding encoding = Encoding.UTF8;

                //// Convert the string to byte array
                //byte[] byteArray = encoding.GetBytes(input);

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }

        }

        public static async Task<int> Bytes2Int(byte[] input)
        {
            int output = default;
            try
            {
                output = BitConverter.ToInt32(input, 0);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }            
        }
        public static async Task<string> Bytes2String(byte[] input)
        {
            string output = default;
            try
            {
                output = Encoding.Default.GetString(input);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }            
        }





    }
}
