// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

// MP2P-9: [Historical] This file's class is the legacy v1 frame builder that was
// superseded by `Febris.SharedMobileLibrary.P2pNetworking.FebrisP2pFrameBuilder`
// during the MP2P-1 / MP2P-2 work in 2026-05-27. All 21 call sites that previously
// used `WiFiP2pRequestCreation.RequestBuilder(...)` were migrated to
// `new FebrisP2pFrameBuilder().Build(header, body)` (note the flipped argument
// order in the new API). Grep confirms no live callers remain.
//
// Wrapped in `#if false` rather than deleted so the v1 frame shape stays visible:
//   * v1 layout was [4-byte header length][header][body] (no body length, no
//     magic, no version, header encoded as ASCII).
//   * Manual ArrayCombinerBuilder did an O(n*length) byte-by-byte combine.
//   * Empty `catch { }` blocks silently returned `byte[0]` on failure.
// The new v2 framer fixes all three. If a v3 frame layout is ever considered,
// the differences between v1 (this file) and v2 (FebrisP2pFrameBuilder) provide
// the historical reference for what didn't work.
//
// Do NOT enable this `#if false` block. Live frame building must go through
// `FebrisP2pFrameBuilder`.

namespace Febris.MobileServerV3.P2pCommunication.WiFi
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
