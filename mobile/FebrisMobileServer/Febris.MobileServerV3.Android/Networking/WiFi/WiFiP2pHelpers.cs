// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.SharedMobileLibrary.Models;
using Java.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Febris.MobileServerV3.Droid.Networking.WiFi
{
    /// <summary>
    /// currently removed from WiFip2pServer to give easier to find methods 
    /// </summary>
    public class WiFiP2pHelpers
    {
        #region Socket
        public static ServerSocket GenerateFreePort()
        {
            ServerSocket serverSocket = new ServerSocket();
            try
            {
                serverSocket = new ServerSocket(WiFiStaticDetails.FebrisSocket);
                //serverSocket.ReuseAddress = false;
                serverSocket.ReuseAddress = true;
                return serverSocket;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating server socket: " + ex.Message);
                Console.WriteLine("Error generating server socket: " + ex.StackTrace);
                serverSocket.Close();
                //serverSocket.Dispose();
                throw;
            }

        }
        #endregion

        #region encoding/decoding
        #region encoding 
        public static byte[] String2Bytes(string input)
        {
            return Encoding.ASCII.GetBytes(input);
        }

        //internal async static Task<byte[]> Encode<T>(string input, BodyType type)
        //{
        //    byte[] output = { };

        //    try
        //    {
        //        if (type == BodyType._module)
        //        {
        //            using (FileStream fileStream = new FileStream(input, FileMode.Open))
        //            {
        //                using (MemoryStream memoryStream = new MemoryStream())
        //                {
        //                    await fileStream.CopyToAsync(memoryStream);
        //                    output = memoryStream.ToArray();
        //                }
        //            }
        //        }
        //        else if (type == BodyType._statement)
        //        {
        //            output = Encoding.ASCII.GetBytes(input);
        //        }
        //        else
        //        {
        //            throw new Exception();
        //        }



        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }

        //    return output;
        //}

        //internal async static Task<byte[]> Encode<T>(string input)
        //{
        //    byte[] output = { };
        //    output = Encoding.ASCII.GetBytes(input);
        //    return output;
        //}

        ////internal static (byte[] header, byte[] body) Encode<T>(T input)
        ////{
        ////    byte[] header = { };
        ////    byte[] body = { };

        ////    return (header, body);
        ////}

        //public Stream GenerateStreamFromString(string s)
        //{
        //    MemoryStream stream = new MemoryStream();
        //    StreamWriter writer = new StreamWriter(stream);
        //    writer.Write(s);
        //    writer.Flush();
        //    stream.Position = 0;
        //    return stream;
        //}

        //internal static byte[] SocketHeaderEncoder(string input)
        //{
        //    byte[] output = new byte[WiFiStaticDetails.HeaderLength];
        //    output = String2Bytes(input);
        //    return output;
        //}
        internal static byte[] SocketHeaderEncoder(int input)
        {
            byte[] output = new byte[WiFiStaticDetails.ExpectedHeaderLength];
            byte[] buffer = Int2Bytes(input);
            buffer.CopyTo(output, 0);
            return output;
        }

        public static byte[] Int2Bytes(int input)
        {
            return BitConverter.GetBytes(input);
        }

        #endregion

        #region decoding

        public static int Bytes2Int(byte[] input)
        {
            return BitConverter.ToInt32(input, 0);
        }
        public static string Bytes2String(byte[] input)
        {
            return Encoding.Default.GetString(input);
        }

        ////internal static T Decode<T>(byte[] input)
        ////{
        ////    var output;// = Encoding.Default(input);

        ////    return output;
        ////}
        //internal static string Decode(byte[] input)
        //{
        //    var output = Encoding.Default.GetString(input);
        //    return output;
        //}

        ////internal static File Decode(byte[] input)
        ////{
        ////    var output;// = Encoding.Default(input);

        ////    return output;
        ////}

        ////internal async static Task<byte[]> Encode<T>(T input)
        ////{
        ////    byte[] output = { };
        ////    //output = Encoding.ASCII.GetBytes(input);
        ////    var xs = new asciiSerializer(typeof(T));


        ////    return output;
        ////}

        //internal async static Task<T> decode<T>(byte[] input, BodyType type)
        //{
        //    T output = (T)(object)null;
        //    try
        //    {
        //        if (type == BodyType._video)
        //        {
        //            output = (T)(object)Bytes2String(input);

        //            //using (FileStream fileStream = new FileStream(input, FileMode.Open))
        //            //{
        //            //    using (MemoryStream memoryStream = new MemoryStream())
        //            //    {
        //            //        await fileStream.CopyToAsync(memoryStream);
        //            //        output = memoryStream.ToArray();
        //            //    }
        //            //}
        //        }
        //        else if (type == BodyType._statement)
        //        {
        //            output = (T)(object)Bytes2String(input);                    
        //        }
        //        else if (type == BodyType._genericString)
        //        {
        //            output = (T)(object)Bytes2String(input);
        //        }
        //        else if (type == BodyType._status)
        //        {                    
        //            output = (T)(object)Bytes2String(input);
        //        }
        //        else
        //        {
        //            throw new Exception();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.StackTrace);
        //    }

        //    return output;
        //}
        #endregion

        #region Header
        public static PacketHeaderModel ParsingJsonStringToHeaderModel(string input)
        {
            PacketHeaderModel output = new PacketHeaderModel();
            try
            {
                output = (PacketHeaderModel)JsonConvert.DeserializeObject(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return output;
        }
        public static PacketHeaderModel ParsingByteArrayToHeaderModel(byte[] input)
        {
            PacketHeaderModel output = new PacketHeaderModel();
            string jsonString = string.Empty;
            try
            {
                jsonString = Bytes2String(input);
                output = (PacketHeaderModel)JsonConvert.DeserializeObject(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return output;
        }
        public static byte[] HeaderModelToByteArray(PacketHeaderModel input)
        {
            byte[] output = { };
            string jsonString = string.Empty;
            try
            {
                jsonString = JsonConvert.SerializeObject(input);
                output = String2Bytes(jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return output;
        }
        public static string HeaderModelToJsonString(PacketHeaderModel input)
        {
            string output = string.Empty;
            output = JsonConvert.SerializeObject(input);
            return output;
        }

        //pull out header

        #endregion

        #endregion
    }
}