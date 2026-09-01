// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Util;
using Java.IO;
using Java.Nio;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.AdbLibrary.AdbLib
{
    /// <summary>
    /// Custom c# Adb Protocol.
    /// Used for communicated with an android debugger commandline USB OTG
    /// </summary>
    public class AdbProtocol
    {
        #region Var
        public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbProtocol.AdbProtocol");
        /** The length of the ADB message header */
        public const int ADB_HEADER_LENGTH = 24;

        public const int CMD_SYNC = 0x434e5953;

        /** CNXN is the connect message. No messages (except AUTH) 
		 * are valid before this message is received. */
        public const int CMD_CNXN = 0x4e584e43;

        /** The current version of the ADB protocol */
        public const int CONNECT_VERSION = 0x01000000;

        /** The maximum data payload supported by the ADB implementation */
        public const int CONNECT_MAXDATA = 4096;
        //public const int CONNECT_MAXDATA = 256 * 1024;

        /** AUTH is the authentication message. It is part of the
         * RSA public key authentication added in Android 4.2.2. */
        public const int CMD_AUTH = 0x48545541;

        /** This authentication type represents a SHA1 hash to sign */
        public const int AUTH_TYPE_TOKEN = 1;

        /** This authentication type represents the signed SHA1 hash */
        public const int AUTH_TYPE_SIGNATURE = 2;

        /** This authentication type represents a RSA public key */
        public const int AUTH_TYPE_RSA_PUBLIC = 3;

        /** OPEN is the open stream message. It is sent to open
         * a new stream on the target device. */
        public const int CMD_OPEN = 0x4e45504f;

        /** OKAY is a success message. It is sent when a write is
         * processed successfully. */
        public const int CMD_OKAY = 0x59414b4f;

        /** CLSE is the close stream message. It it sent to close an
         * existing stream on the target device. */
        public const int CMD_CLSE = 0x45534c43;

        /** WRTE is the write stream message. It is sent with a payload
         * that is the data to write to the stream. */
        public const int CMD_WRTE = 0x45545257;

        /// <summary>
        /// This is a constant for prevent over and underflow. It is odd though. 
        /// I have no idea if it is interacting the correct way
        /// </summary>
        public const uint UNSIGNED_MAGIC = 0xFFFFFFFF;
        //public const uint UNSIGNED_MAGIC = 0xffffffff;

        public const int _TimeOut = 100000;


        #region
        /// <summary>
        /// 
        /// </summary>
        public const string _StatId = "STAT";
        public static byte[] _StatIdArray = Helpers.StringToBytes(_StatId);

        public const string _SendId = "SEND";
        public static byte[] _SendIdArray = Helpers.StringToBytes(_SendId);

        public const string _DataId = "DATA";
        public static byte[] _DataIdArray = Helpers.StringToBytes(_DataId);

        public const string _DoneId = "DONE";
        public static byte[] _DoneIdArray = Helpers.StringToBytes(_DoneId);
        /// <summary>
        /// ADB Modes
        /// </summary>
        /// public const string 
        /// 
        public const int _PushFileMode_mode = 0x0100644;//this is the same as 33188 in oct format
        public const int _PushOGAlt_mode = 0x0100666;//33206; 
        public const int OGAlt_mode = 33206;//",33206"; //From ADB-OTG project
        public const int Alt_mode = 0644;//",0644";///full file path  ///from https://github.com/cstyan/adbDocumentation
        public const int Alt2_mode = 33188;//",33188";///full file path, the mode of the file in decimal (0644 becomes 33188)
        //public const string _DEFAULT_PUSH_MODE = ",33272";
        public const int _OctDEFAULT_PUSH_MODE = 0x0100770;
        public const int _DEFAULT_PUSH_MODE = 33272;//81F8   ///This is the same as st_mode                                                  
        public const int _MAX_CHUNK_SIZE = 65536;
        public const int _MAX_PUSH_DATA = 2048;
        //public const int _MAX_CHUNK_SIZE = 65536;
        //public const int _MAX_CHUNK_SIZE = 65536;
        //public const int _MAX_CHUNK_SIZE = 65536;
        //public const int _MAX_CHUNK_SIZE = 65536;
        public const int _Alt3_MODE = 0777;
        public const int _Alt4_MODE = 07777;
        public const int _Alt5_MODE = 0666;
        #endregion


        ///1896 static const char *const DATA_DEST = "/data/local/tmp/%s"; This is the internal storage of the device
        ///1897 static const char *const SD_DEST = "/sdcard/tmp/%s";This is the external storage sd card


        /** The payload sent with the connect message */
        //public static byte[] CONNECT_PAYLOAD;
        public static byte[] CONNECT_PAYLOAD()
        {
            byte[] output = default;
            try
            {
                //byte[] convertedelseware = { '68', 6f, 73, 74, 3a, 3a, "5c", "30" };

                string host = GenerateIdentityString();
                byte[] encoding = Helpers.StringToBytes(host, char.MinValue);               
                //string backout = Helpers.ByteArrayToString(encoding);

                return encoding;

                //output = System.Text.UTF8Encoding.UTF8.GetBytes("host::\0");
                //output = "host::\0".getBytes("UTF-8");
            }
            catch { }
            return output;
        }

        /**
       * Generates an Identity string for connecting        
       * The system identity string should be "<systemtype>:<serialno>:<banner>"
       * where systemtype is "bootloader", "device", or "host", serialno is some
       * kind of unique ID (or empty), and banner is a human-readable version
       * or identifier string.  The banner is used to transmit useful properties.
       */
        internal static string GenerateIdentityString()
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateIdentityString");
            try
            {
                string host = "host";// + char.MinValue+':';
                //string host = "host-usb";// + char.MinValue+':';
                string serialNumber = "";
                //string serial = "24" + char.MinValue + ':';
                string banner = "";
                string output = string.Join(":", host, serialNumber, banner);// + char.MinValue;
                //string output = host + serial + banner;
                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol GenerateReady: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateReady: " + ex.StackTrace); throw;
            }
            return default;
        }
        #endregion

        #region  Message Generation
        /**
         * This function generates an ADB message given the fields.
         * @param cmd Command identifier
         * @param arg0 First argument
         * @param arg1 Second argument
         * @param payload Data payload
         * @return AdbMessage
         */
        public static AdbMessage GenerateMessage(int cmd, int arg0, int arg1, byte[] payload)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateMessage");
            /* struct message {
             * 		unsigned command;       // command identifier constant
             * 		unsigned arg0;          // first argument
             * 		unsigned arg1;          // second argument
             * 		unsigned data_length;   // length of payload (0 is allowed)
             * 		unsigned data_check;    // checksum of data payload
             * 		unsigned magic;         // command ^ 0xffffffff
             * };
             */
            try
            {
                AdbMessage package = AdbMessage.CreateMessage(cmd, arg0, arg1, payload); //new AdbMessage(cmd, arg0, arg1, payload);
                //_logger.Println("Build package array Legth: " + package.GetMessage().Length);
                //_logger.Println("Build package array: " + package.GetMessage().ToString());
                //_logger.Println("package array to string: " + Helpers.ByteArrayToString(package.GetMessage()));
                return package;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol GenerateMessage: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateMessage: " + ex.StackTrace); throw;
            }
            return default;
        }
        #endregion

        #region Validation Testing
        /**
         * This function validate the ADB message by checking
         * its command, magic, and payload checksum.
         * @param msg ADB message to validate
         * @return True if the message was valid, false otherwise
         */
        public static bool ValidateMessage(AdbMessage msg)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.ValidateMessage");
            try
            {                
                int command = msg.GetCommand();
                int magic = msg.GetMagic();
                int messagemagic = (int)(magic ^ UNSIGNED_MAGIC);                
                if (command != messagemagic)
                {
                    return false;
                }
                             
                if (msg.GetPayloadLength() != 0)
                {
                    _logger.Println("#################################Start Validate Message Payload####################################################");
                    //_logger.Println("AdbMessage Parsed Command: " + (msg.GetCommand()) + " Payload:" + Helpers.ByteArrayToString(msg._Payload));//.ToString()); 
                    _logger.Println("Incoming Message Command: " + (msg.GetCommand()) + " Payload: " + Helpers.ByteArrayToString(msg.GetPayload()));
                    _logger.Println("##################################End Validate Message Payload#####################################################");

                    if (AdbMessage.Checksum(msg.GetPayload()) != msg.GetChecksum())
                        return false;
                }

                return true;                
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol ValidateMessage: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol ValidateMessage: " + ex.StackTrace); throw;
            }
            return default;
        }

        public static bool ValidateMessage(AdbMessage msg, string i)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.ValidateMessage");
            try
            {
                int command = msg.GetCommand();
                int magic = msg.GetMagic();
                int messagemagic = (int)(magic ^ UNSIGNED_MAGIC);
                if (command != messagemagic)
                {
                    return false;
                }

                if (msg.GetPayloadLength() != 0)
                {
                    if(i== "***IN***")
                    {
                        _logger.Println("################################################# INCOMMING #########################################################");
                    }else if(i == "&&&Out&&&")
                    {
                        _logger.Println("################################################### OUT #############################################################");
                    }
                    _logger.Println(i + i + i + i + i +"  Start Validate Message Payload  " + i + i + i + i + i);
                    //_logger.Println("AdbMessage Parsed Command: " + (msg.GetCommand()) + " Payload:" + Helpers.ByteArrayToString(msg._Payload));//.ToString()); 
                    _logger.Println("Incoming Message Command: " + (msg.GetCommand()) + " Payload: " + Helpers.ByteArrayToString(msg.GetPayload()));
                    _logger.Println(i + i + i + i + i + "  End Validate Message Payload  "  + i + i + i + i + i);
                    //_logger.Println("##################################End Validate Message Payload#####################################################");

                    if (AdbMessage.Checksum(msg.GetPayload()) != msg.GetChecksum())
                        return false;
                }

                return true;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol ValidateMessage: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol ValidateMessage: " + ex.StackTrace); throw;
            }
            return default;
        }

        #endregion

        #region Static Operations
        /**
         * Generates a connect message with default parameters.
         * @return AdbMessage
         */
        public static AdbMessage GenerateConnect()
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateConnect");
            try
            {
                byte[] payload = CONNECT_PAYLOAD();
                //byte[] payload = default;
                AdbMessage package = GenerateMessage(CMD_CNXN, CONNECT_VERSION, CONNECT_MAXDATA, payload);
                return package;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol GenerateConnect: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateConnect: " + ex.StackTrace); throw;
            }
            return default;
        }

        /**
         * Generates an auth message with the specified type and payload.
         * @param type Authentication type (see AUTH_TYPE_* constants)
         * @param data The payload for the message
         * @return AdbMessage
         */
        public static AdbMessage GenerateAuth(int type, byte[] data)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateAuth");
            try
            {
                return GenerateMessage(CMD_AUTH, type, 0, data);
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol GenerateAuth: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateAuth: " + ex.StackTrace); throw;
            }
            return default;
        }

        /**
         * Generates an open stream message with the specified local ID and destination.
         * @param localId A unique local ID identifying the stream
         * @param dest The destination of the stream on the target
         * @return AdbMessage
         * @throws java.io.UnsupportedEncodingException If the destination cannot be encoded to UTF-8
         */
        public static AdbMessage GenerateOpen(int localId, string dest)// throws UnsupportedEncodingException
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateOpen");
            try
            {
                //byte[] destinationArray = Helpers.StringToBytes(dest);
                //ByteBuffer bbuf = ByteBuffer.Allocate(destinationArray.Length + 1);
                ////System.Text.UTF8Encoding.UTF8.GetBytes(dest);
                ////byte[] destinationArray = Helpers.StringToBytes(dest);
                //bbuf.Put(destinationArray);
                //bbuf.PutChar(char.MinValue);//.Put(byte.MinValue);
                byte[] payload = Helpers.StringToBytes(dest,char.MinValue);//Helpers.ConvertByteBufferToArray(bbuf);
                return GenerateMessage(CMD_OPEN, localId, 0, payload);
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol GenerateOpen: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateOpen: " + ex.StackTrace); throw;
            }
            return default;
        }

        /**
         * Generates a write stream message with the specified IDs and payload. 
         * @param localId The unique local ID of the stream
         * @param remoteId The unique remote ID of the stream
         * @param data The data to provide as the write payload
         * @return AdbMessage
         */
        public static AdbMessage GenerateWrite(int localId, int remoteId, byte[] data)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateWrite");
            try
            {
                return GenerateMessage(CMD_WRTE, localId, remoteId, data);
            }
            catch (AndroidException ex) 
            { 
                _logger.Println("An Android error occured in AdbProtocol GenerateWrite: " + ex.StackTrace);
                throw; 
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateWrite: " + ex.StackTrace); 
                throw;
            }
            return default;
        }

        /**
         * Generates a close stream message with the specified IDs.
         * @param localId The unique local ID of the stream
         * @param remoteId The unique remote ID of the stream
         * @return AdbMessage
         */
        public static AdbMessage GenerateClose(int localId, int remoteId)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateClose");
            try
            {
                return GenerateMessage(CMD_CLSE, localId, remoteId, null);
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol GenerateClose: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateClose: " + ex.StackTrace); throw;
            }
            return default;
        }

        /**
         * Generates an okay message with the specified IDs.
         * @param localId The unique local ID of the stream
         * @param remoteId The unique remote ID of the stream
         * @return AdbMessage
         */
        public static AdbMessage GenerateReady(int localId, int remoteId)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateReady");
            try
            {
                return GenerateMessage(CMD_OKAY, localId, remoteId, null);
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol GenerateReady: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateReady: " + ex.StackTrace); throw;
            }
            return default;
        }

        public static byte[] GenerateSTATPayload()
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.Protocol.GenerateSTATPayload");
            try
            {
                int length = (_StatId+ _DEFAULT_PUSH_MODE.ToString()).Length;
                string start = string.Join(",", _StatId, _DEFAULT_PUSH_MODE.ToString(), length, 10);// DateTime.Now.Millisecond.ToString());
                byte[] output = Helpers.StringToBytes(start);
                
                //byte[] id = _StatIdArray;
                //byte[] mode = _DEFAULT_PUSH_MODE;
                //byte[] size = ;
                //byte[] time = ;
                
                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbProtocol GenerateReady: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbProtocol GenerateReady: " + ex.StackTrace); throw;
            }
            return default;
        }


        #endregion

    }        
}
