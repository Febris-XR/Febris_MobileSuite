// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

// MP2P-9: [Historical] This file's class is the legacy v1 frame builder, superseded
// by `Febris.SharedMobileLibrary.P2pNetworking.FebrisP2pFrameBuilder` during MP2P-1
// and MP2P-2 (2026-05-27). All call sites that previously used
// `WiFiP2pRequestCreation.RequestBuilder(...)` were migrated to the shared framer.
// Grep confirms no live callers remain.
//
// Wrapped in `#if false` rather than deleted so the v1 frame shape stays visible:
//   * v1 layout was [4-byte header length][header][body] (no body length, no
//     magic, no version, header encoded as ASCII).
//   * Manual ArrayCombinerBuilder did an O(n*length) byte-by-byte combine.
//   * Empty `catch { }` blocks silently returned `byte[0]` on failure.
// The new v2 framer fixes all three. The class is byte-for-byte identical to the
// Server-side version in `mobile/FebrisMobileServer/.../WiFiP2pRequestCreation.cs`
// (one of the duplication problems MP2P-2 collapsed by lifting framing into the
// shared library).
//
// Do NOT enable this `#if false` block. Live frame building must go through
// `FebrisP2pFrameBuilder`.

namespace Febris.MobileCompanionV3.P2pCommunication.WiFi
{
#if false // MP2P-9: legacy v1 frame builder, superseded by FebrisP2pFrameBuilder.
    public class WiFiP2pRequestCreation
    {

        #region Request builder
        public static byte[] RequestBuilder(string input, PacketHeaderModel headerModel)
        {
            byte[] bodyData = { };
            byte[] header = { };
            byte[] headerSize = { };
            byte[] fullHeader = { };
            byte[] output = { };
            try
            {
                //get header
                header = HeaderBuilder(headerModel);
                //make preheader
                headerSize = HeaderPrefaceBuilder(header.Length);
                //combine byte arrays
                fullHeader = ArrayCombinerBuilder(headerSize, header);
                //get body
                bodyData = String2Bytes(input);
                //combine full header and body
                output = ArrayCombinerBuilder(fullHeader, bodyData);
            }
            catch { }
            finally { }

            return output;
        }

        public static byte[] RequestBuilder(byte[] input, PacketHeaderModel headerModel)
        {
            byte[] header = { };
            byte[] headerSize = { };
            byte[] fullHeader = { };
            byte[] output = { };
            try
            {
                //get header
                header = HeaderBuilder(headerModel);
                //make preheader
                headerSize = HeaderPrefaceBuilder(header.Length);
                //combine byte arrays
                fullHeader = ArrayCombinerBuilder(headerSize, header);
                //combine full header and body
                output = ArrayCombinerBuilder(fullHeader, input);
            }
            catch { }
            finally { }

            return output;
        }

        #endregion

        #region Header building
        public static byte[] HeaderBuilder(PacketHeaderModel input)
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
            finally { }
            return output;
        }

        public static byte[] HeaderPrefaceBuilder(int input)
        {
            byte[] output = new byte[LocalHardwareStaticDetails.ExpectedHeaderLength];
            try
            {
                byte[] buffer = Int2Bytes(input);
                buffer.CopyTo(output, 0);
            }
            catch { }
            finally { }
            return output;
        }

        //public static byte[] PreHeaderBuilder(int input)
        //{
        //    byte[] output = new byte[WiFiStaticDetails.ExpectedHeaderLength];
        //    try
        //    {
        //        byte[] buffer = Int2Bytes(input);
        //        buffer.CopyTo(output, 0);
        //    }
        //    catch { }
        //    finally { }
        //    return output;
        //}
        #endregion

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
#endif // MP2P-9: legacy v1 frame builder
}
