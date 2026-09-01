// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Util;
using Febris.AdbLibrary.Interface;
using Java.Nio;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.AdbLibrary.AdbLib
{
    public class AdbMessage
    {
        public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbMessage");
        private ByteBuffer _MessageBuffer { get; set; }
        private byte[] _Payload { get; set; }

        #region Initalize
        public AdbMessage(int command, int arg0, int arg1, byte[] data)
        {
            try
            {
                _MessageBuffer = ByteBuffer.Allocate(AdbProtocol.ADB_HEADER_LENGTH).Order(ByteOrder.LittleEndian);//.LITTLE_ENDIAN);
                _MessageBuffer.PutInt(0, command);
                _MessageBuffer.PutInt(4, arg0);
                _MessageBuffer.PutInt(8, arg1);
                //_MessageBuffer.PutInt(12, (data == null ? 0 : data.Length));
                //_MessageBuffer.PutInt(16, (data == null ? 0 : Checksum(data)));
                //_MessageBuffer.PutInt(20, (int)(command ^ AdbProtocol.UNSIGNED_MAGIC));
                if (data != default && data != null)
                {
                    _MessageBuffer.PutInt(12, (data.Length));
                    var check = Checksum(data);
                    _MessageBuffer.PutInt(16, check);
                }
                else
                {
                    _MessageBuffer.PutInt(12, 0);
                    _MessageBuffer.PutInt(16, 0);
                }

                _MessageBuffer.PutInt(20, (int)(command ^ AdbProtocol.UNSIGNED_MAGIC));
                //_MessageBuffer.PutInt(20, (int)(command | AdbProtocol.UNSIGNED_MAGIC));
                _Payload = data ?? default;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage Constructor: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage Constructor: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage Constructor: " + ex.StackTrace); throw;
                throw;
            }

        }

        public AdbMessage()
        {
        }

        #endregion

        #region Main Queries
        /// <summary>
        /// putting incomming messages into comething usable
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static AdbMessage ParseAdbMessage(IAdbChannel input) //throws IOException
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbMessage.ParseAdbMessage");
            try
            {
                ///these are just variables
                AdbMessage msg = new AdbMessage();
                msg._logger = _logger;
                ByteBuffer packet = ByteBuffer.Allocate(AdbProtocol.ADB_HEADER_LENGTH).Order(ByteOrder.LittleEndian);
                                
                byte[] dataPacket = input.Readx(AdbProtocol.ADB_HEADER_LENGTH);                
                packet.Put(dataPacket);                
                                
                msg._MessageBuffer = packet;

                int payloadLength = msg.GetPayloadLength();
                if (payloadLength != 0)
                {
                    byte[] payload = input.Readx(payloadLength);
                    msg.SetPayload(payload);
                }

                //_logger.Println("AdbMessage Parsed Command: "+ (msg.GetCommand()) + " Payload:" + Helpers.ByteArrayToString(msg._Payload));//.ToString()); 
                return msg;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage ParseAdbMessage: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage ParseAdbMessage: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage ParseAdbMessage: " + ex.StackTrace); throw;
                throw;
            }
        }


        /// <summary>
        /// Create Message without anything crazy. Simplifying it with a static call
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static AdbMessage CreateMessage(int command, int arg0, int arg1, byte[] data)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbMessage.CreateMessage");
            try
            {
                AdbMessage msg = new AdbMessage(command, arg0, arg1, data);
                return msg;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage CreateMessage: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage CreateMessage: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage CreateMessage: " + ex.StackTrace); throw;
                throw;
            }
        }


        #endregion

        #region helpers
        /**
         * This function performs a checksum on the ADB payload data.
         * @param payload Payload to checksum
         * @return The checksum of the payload
         */
        public static int Checksum(byte[] payload)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbMessage.Checksum");
            try
            {
                int checksum = 0;

                foreach (byte b in payload)
                {
                    /* We have to manually "unsign" these bytes because Java sucks */
                    if (b >= 0)
                        checksum += b;
                    else
                        checksum += b + 256;
                }

                return checksum;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage Checksum: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage Checksum: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage Checksum: " + ex.StackTrace); throw;
            }            
        }

        /** The command field of the message */
        public int GetCommand()
        {
            try
            {
                int package = _MessageBuffer.GetInt(0);
                return package;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetCommand: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage GetCommand: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage GetCommand: " + ex.StackTrace); throw;
            }
        }

        /** The arg0 field of the message */
        public int GetArg0()
        {
            try
            {
                int package = _MessageBuffer.GetInt(4);
                return package;
                //return _MessageBuffer.GetInt(4);
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetArg0: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage GetArg0: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage GetArg0: " + ex.StackTrace); throw;
            }
        }

        /** The arg1 field of the message */
        public int GetArg1()
        {
            try
            {
                int package = _MessageBuffer.GetInt(8);
                return package;
                //return _MessageBuffer.GetInt(8);
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetArg1: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage GetArg1: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage GetArg1: " + ex.StackTrace); throw;
            }
        }

        /** The payload length field of the message */
        public int GetPayloadLength()
        {
            try
            {
                int package = _MessageBuffer.GetInt(12);
                return package;                
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetPayloadLength: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage GetPayloadLength: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage GetPayloadLength: " + ex.StackTrace); throw;
            }
            return default;
        }

        /** The checksum field of the message */
        public int GetChecksum()
        {
            try
            {
                int package = _MessageBuffer.GetInt(16);
                return package;                
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetChecksum: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage GetChecksum: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage GetChecksum: " + ex.StackTrace); throw;
            }
            return default;
        }

        /** The magic field of the message */
        public int GetMagic()
        {
            try
            {
                int package = _MessageBuffer.GetInt(20);
                return package;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetMagic: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage GetMagic: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage GetMagic: " + ex.StackTrace); throw;
            }
            return default;
        }

        public byte[] GetMessage()
        {
            try
            {
                byte[] output = Helpers.ConvertByteBufferToArray(_MessageBuffer);
                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetMessage: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage GetMessage: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage GetMessage: " + ex.StackTrace); throw;
            }
            return default;
        }

        /** The payload of the message */
        public byte[] GetPayload()
        {
            try
            {
                return _Payload;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetPayload: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage GetPayload: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage GetPayload: " + ex.StackTrace); throw;
            }
            return default;
        }

        public void SetPayload(byte[] payload)
        {
            try
            {
                _Payload = payload;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage SetPayload: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in AdbMessage SetPayload: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbMessage SetPayload: " + ex.StackTrace); throw;
            }
        }

        #endregion
    }

    #region old
    //public class AdbMessage
    //{
    //    public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbMessage");
    //    private ByteBuffer _MessageBuffer;
          
    //    private byte[] _Payload;

    //    private AdbMessage()
    //    {
    //        //_MessageBuffer = default;
    //        //_Payload = default;
    //    }

    //    // sets the fields in the command header
    //    public AdbMessage(int command, int arg0, int arg1, byte[] data)
    //    {
    //        try
    //        {
    //            _MessageBuffer = ByteBuffer.Allocate(AdbProtocol.ADB_HEADER_LENGTH).Order(ByteOrder.LittleEndian);//.LITTLE_ENDIAN);
    //            _MessageBuffer.PutInt(0, command);
    //            _MessageBuffer.PutInt(4, arg0);
    //            _MessageBuffer.PutInt(8, arg1);
    //            //_MessageBuffer.PutInt(12, (data == null ? 0 : data.Length));
    //            //_MessageBuffer.PutInt(16, (data == null ? 0 : Checksum(data)));
    //            //_MessageBuffer.PutInt(20, (int)(command ^ AdbProtocol.UNSIGNED_MAGIC));
    //            if (data != null)
    //            {
    //                _MessageBuffer.PutInt(12, (data.Length));
    //                int check = Checksum(data);
    //                _MessageBuffer.PutInt(16, check);
    //            }
    //            else
    //            {
    //                _MessageBuffer.PutInt(12, 0);                    
    //                _MessageBuffer.PutInt(16, 0);
    //            }
                               
    //            _MessageBuffer.PutInt(20, (int)(command ^ AdbProtocol.UNSIGNED_MAGIC));
    //            //_MessageBuffer.PutInt(20, (int)(command | AdbProtocol.UNSIGNED_MAGIC));
    //            _Payload = data;
    //            //if (data != null)
    //            //{
    //            //    _MessageBuffer.Put(data, 0, data.Length);
    //            //    _Payload = data;
    //            //}

                
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage Constructor: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage Constructor: " + ex.StackTrace); throw;
    //        }

    //    }

    //    //public AdbMessage(int command, int arg0, int arg1)
    //    //{
    //    //    try
    //    //    {
    //    //        _MessageBuffer = ByteBuffer.Allocate(AdbProtocol.ADB_HEADER_LENGTH).Order(ByteOrder.LittleEndian);//.LITTLE_ENDIAN);
    //    //        _MessageBuffer.PutInt(0, command);
    //    //        _MessageBuffer.PutInt(4, arg0);
    //    //        _MessageBuffer.PutInt(8, arg1);
    //    //        _MessageBuffer.PutInt(20, (int)(command ^ AdbProtocol.UNSIGNED_MAGIC));

    //    //        _Payload = default;
    //    //    }
    //    //    catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage Constructor: " + ex.StackTrace); throw;}
    //    //    catch (System.Exception ex)
    //    //    {
    //    //        _logger.Println("A generic error occured in AdbMessage Constructor: " + ex.StackTrace); throw;
    //    //    }
    //    //}

    //    /**
    //     * Read and parse an ADB message from the supplied input stream.
    //     * This message is NOT validated.
    //     * @param in InputStream object to read data from
    //     * @return An AdbMessage object represented the message read
    //     * @throws java.io.IOException If the stream fails while reading
    //     */
    //    public static AdbMessage ParseAdbMessage(IAdbChannel input) //throws IOException
    //    {
    //        LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.ParseAdbMessage");
    //        try
    //        {
    //            AdbMessage msg = new AdbMessage();
    //            ByteBuffer packet = ByteBuffer.Allocate(AdbProtocol.ADB_HEADER_LENGTH).Order(ByteOrder.LittleEndian);//.LITTLE_ENDIAN);
    //            //packet.Rewind();
    //            /* Read the header first */
    //            //byte[] postPacket= packet.ToArray(0, packet.Length);
    //            //byte[] dataPacket = Helpers.ConvertByteBufferToArray(packet);
    //            //(dataPacket)=input.Readx(AdbProtocol.ADB_HEADER_LENGTH);
    //            //byte[] dataPacket = new byte[AdbProtocol.ADB_HEADER_LENGTH];
    //            //input.Readx(dataPacket, AdbProtocol.ADB_HEADER_LENGTH);//*****************************there is no way this should work
    //            byte[] dataPacket = input.Readx(AdbProtocol.ADB_HEADER_LENGTH);//*****************************there is no way this should work
    //            packet.Put(dataPacket);
    //            //input.Readx(packet.ToArray<byte>(), AdbProtocol.ADB_HEADER_LENGTH);
    //            //packet.Rewind();
    //            msg._MessageBuffer = packet;

    //            /* If there's a payload supplied, read that too */
    //            var payloadLength = msg.GetPayloadLength();
    //            if (payloadLength != 0)
    //            {
    //                ///this is just setting the payload length as an empty array?
    //                //msg.SetPayload(new byte[payloadLength]);
    //                byte[] payload = new byte[payloadLength];
    //                input.Readx(payload, msg.GetPayloadLength());
    //                msg.SetPayload(payload);
    //                //input.Readx(msg.GetPayload(), msg.GetPayloadLength());

    //            }
    //            //if (msg.GetPayloadLength() != 0)
    //            //{
    //            //    msg.SetPayload(new byte[msg.GetPayloadLength()]);
    //            //    input.Readx(msg.GetPayload(), msg.GetPayloadLength());
    //            //}
    //            _logger.Println("AdbMessage Parsed Message: " + msg.ToString());
    //            return msg;
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage ParseAdbMessage: " + ex.StackTrace); throw; }
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage ParseAdbMessage: " + ex.StackTrace); throw;
    //            throw;
    //        }
    //    }

    //    /**
    //     * This function performs a checksum on the ADB payload data.
    //     * @param payload Payload to checksum
    //     * @return The checksum of the payload
    //     */
    //    public static int Checksum(byte[] payload)
    //    {
    //        LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary");
    //        try
    //        {
    //            int checksum = 0;

    //            foreach (byte b in payload)
    //            {
    //                /* We have to manually "unsign" these bytes because Java sucks */
    //                if (b >= 0)
    //                    checksum += b;
    //                else
    //                    checksum += b + 256;
    //            }

    //            return checksum;
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage Checksum: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage Checksum: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }


    //    /** The command field of the message */
    //    public int GetCommand()
    //    {
    //        try
    //        {
    //            //_MessageBuffer.Rewind();
    //            int package = _MessageBuffer.GetInt(0);
    //            var a1 = _MessageBuffer.GetLong(0);
    //            var a2 = _MessageBuffer.GetShort(0);
    //            //var a3 = _MessageBuffer.GetInt();//.GetLong(0);

    //            return package;//_MessageBuffer.GetInt(0);//.getInt(0);
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetCommand: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage GetCommand: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }

    //    /** The arg0 field of the message */
    //    public int GetArg0()
    //    {
    //        try
    //        {
    //            int package = _MessageBuffer.GetInt(4);
    //            return package;
    //            //return _MessageBuffer.GetInt(4);
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetArg0: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage GetArg0: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }

    //    /** The arg1 field of the message */
    //    public int GetArg1()
    //    {
    //        try
    //        {
    //            int package = _MessageBuffer.GetInt(8);
    //            return package;
    //            //return _MessageBuffer.GetInt(8);
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetArg1: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage GetArg1: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }

    //    /** The payload length field of the message */
    //    public int GetPayloadLength()
    //    {
    //        try
    //        {
    //            int package = _MessageBuffer.GetInt(12);
    //            return package;
    //            //return _MessageBuffer.GetInt(12);
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetPayloadLength: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage GetPayloadLength: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }

    //    /** The checksum field of the message */
    //    public int GetChecksum()
    //    {
    //        try
    //        {
    //            int package = _MessageBuffer.GetInt(16);
    //            return package;
    //            //return _MessageBuffer.GetInt(16);
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetChecksum: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage GetChecksum: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }

    //    /** The magic field of the message */
    //    public int GetMagic()
    //    {
    //        try
    //        {
    //            int package = _MessageBuffer.GetInt(20);
    //            return package;
    //            //return _MessageBuffer.GetInt(20);
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetMagic: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage GetMagic: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }

    //    public byte[] GetMessage()
    //    {
    //        try
    //        {
    //            byte[] output = Helpers.ConvertByteBufferToArray(_MessageBuffer);
    //            return output;                
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetMessage: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage GetMessage: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }

    //    /** The payload of the message */
    //    public byte[] GetPayload()
    //    {
    //        try
    //        {
    //            return _Payload;
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage GetPayload: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage GetPayload: " + ex.StackTrace); throw;
    //        }
    //        return default;
    //    }

    //    public void SetPayload(byte[] payload)
    //    {
    //        try
    //        {
    //            _Payload = payload;
    //        }
    //        catch (AndroidException ex) { _logger.Println("An Android error occured in AdbMessage SetPayload: " + ex.StackTrace); throw;}
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in AdbMessage SetPayload: " + ex.StackTrace); throw;
    //        }
    //    }

    //}
    #endregion
}
