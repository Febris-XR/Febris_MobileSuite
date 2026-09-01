// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Util;
using Java.Lang;
using Java.Nio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Febris.AdbLibrary.AdbLib
{
    public class Helpers//: Java.Lang.Object
    {
        LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers");


        #region Conversions
        /// <summary>
        /// Converting ByteBuffer to a byte array
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] ConvertByteBufferToArray(ByteBuffer input)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.ConvertByteBufferToArray");
            try
            {
                //int startingIndex = 0;
                //int length = input.Capacity();
                //byte[] output = new byte[length];
                ////input.Rewind();
                ////System.Runtime.InteropServices.Marshal.Copy(input.GetDirectBufferAddress(), output, startingIndex, length);
                ////byte[] output = input.cop;


                //input.Get(output, 0, length);

                //input.Rewind();
                //string s = Java.Nio.Charset.StandardCharsets.Utf8.Decode(input).ToString();
                //byte[] output1 = StringToBytes(s);

                //bool jhnj = Helpers.CompareByteArrays(output1, output);
                input.Rewind();
                byte[] output = new byte[input.Capacity()];
                input.Get(output, 0, output.Length);

                //input.Rewind();
                //string s = Java.Nio.Charset.StandardCharsets.Utf8.Decode(input).ToString();
                //byte[] output1 = StringToBytes(s);

                //bool jhnj = Helpers.CompareByteArrays(output1, output);

                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in Helpers ConvertByteBufferToArray: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Helpers ConvertByteBufferToArray: " + ex.StackTrace); throw;
            }
            return default;
        }

        public static byte[] StringToBytes(string payload, char minValue)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.StringToBytes");
            try
            {
                //System.Text.StringBuilder sb = new System.Text.StringBuilder(payload);
                //sb.Append(minValue);
                //string secondOption = sb.ToString();
                //byte[] secondoutput = Encoding.UTF8.GetBytes(secondOption);

                string data = payload + minValue;
                byte[] output = Encoding.UTF8.GetBytes(data);


                //bool equivalentArrays = Helpers.CompareByteArrays(output, secondoutput);
                //bool equivalentStrings = Helpers.CompareStrings(data, secondOption);

                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in Helpers StringToBytes: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Helpers StringToBytes: " + ex.StackTrace); throw;
                throw;
            }

        }

        public static byte[] StringToBytes(string payload)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.StringToBytes");
            try
            {
                string data = payload;
                byte[] output = Encoding.UTF8.GetBytes(data);
                //byte[] output1 = Encoding.ASCII.GetBytes(data);
                //bool compair = Helpers.CompareByteArrays(output, output1);
                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in Helpers StringToBytes: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Helpers StringToBytes: " + ex.StackTrace); throw;
                throw;
            }
        }

        public static byte[] CharToBytes(char payload)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.CharToBytes");
            try
            {
                char[] data = new char[payload];
                byte[] output = Encoding.UTF8.GetBytes(data);
                //byte[] output1 = Encoding.ASCII.GetBytes(data);
                //bool compair = Helpers.CompareByteArrays(output, output1);
                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in Helpers CharToBytes: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Helpers CharToBytes: " + ex.StackTrace); throw;
                throw;
            }
        }


        public static bool CompareByteArrays(byte[] a, byte[] b)
        {

            if (a.SequenceEqual(b))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

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

        public static bool CompareStrings(string a, string b)
        {
            if (a == b)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string ByteArrayToString(byte[] input)
        {
            try
            {
                string s = BitConverter.ToString(input);

                //string s = UTF8Encoding.UTF8.GetString(input);
                //string a = ASCIIEncoding.ASCII.GetString(input);
                InternalHexDecoder(s);
                //HexDecoder2(s);
                return s;
            }
            catch (System.Exception)
            {
                return string.Empty;
                throw;
            }

        }

        //public static void HexDecoder2(string hex)
        //{
        //    hex=hex.Replace("-", string.Empty);
        //    LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.HexDecoder2");
        //    try
        //    {
        //        // initialize the ASCII code string as empty.
        //        string ascii = "";

        //        for (int i = 0; i < hex.Length; i += 2)
        //        {

        //            // extract two characters from hex string
        //            string part = hex.Substring(i, 2);

        //            // change it into base 16 and
        //            // typecast as the character
        //            char ch = (char)Convert.ToInt32(part, 16); ;

        //            // add this char to final ASCII string
        //            ascii = ascii + ch;
        //        }
        //        _logger.Println("Decoded Hex: " + ascii);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("Hex Decoding Failed:"+ ex.Message);
        //    }
        //}
        public static void InternalHexDecoder(string input)
        {
            input = input.Replace("-", string.Empty);
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.HexDecoder");
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < input.Length; i += 2)
                {
                    string hs = string.Empty;

                    hs = input.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;
                }

                _logger.Println("Decoded Hex: " + ascii);
            }
            catch (System.Exception ex)
            {
                _logger.Println("Hex Decoding Failed:" + ex.Message);
            }

        }

        public static string HexDecoder(string input)
        {
            string output = string.Empty;
            input = input.Replace("-", string.Empty);
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.HexDecoder");
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < input.Length; i += 2)
                {
                    string hs = string.Empty;

                    hs = input.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;
                }
                _logger.Println("Decoded Hex: " + ascii);
                output = ascii;                
            }
            catch (System.Exception ex)
            {
                _logger.Println("Hex Decoding Failed:" + ex.Message);
            }
            return output;
        }

        public static byte[] IntToBytes(int payload)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.IntToBytes");
            try
            {
                int data = payload;
                byte[] output = BitConverter.GetBytes(data);
                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in Helpers IntToBytes: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Helpers IntToBytes: " + ex.StackTrace); throw;
                throw;
            }
        }

        public static byte[] ObjectToByteArray(Java.Lang.Object obj)
        {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var ms = new System.IO.MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        #endregion
        public static byte[][] ByteArrayToChunks(byte[] byteData, long BufferSize)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Helpers.ByteArrayToChunks");
            try
            {
                byte[][] chunks = byteData.Select((value, index) =>
            new { PairNum = System.Math.Floor(index / (double)BufferSize), value })
                .GroupBy(pair => pair.PairNum)
                .Select(grp => grp.Select(g => g.value)
                .ToArray())
                .ToArray();

                return chunks;
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in Helpers ByteArrayToChunks: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Helpers ByteArrayToChunks: " + ex.StackTrace);
                throw;
            }
        }


        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //public static long CurrentTimeMillis()
        //{
        //    return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        //}

        public static int CurrentTimeMillis()
        {
            try
            {
                var intermediate = (DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
                //int output = 0;// Convert.ToInt32(intermediate);
                //int output = System.Math.Round(intermediate);
                int output = (int)(intermediate);
                return output;
            }
            catch 
            { 
                return default; 
            }
        }
    }
}
