// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Hardware.Usb;
using Android.Util;
using Febris.AdbLibrary.Interface;
using Java.IO;
using Java.Lang;
using Java.Nio;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Febris.AdbLibrary.AdbLib
{
    public class UsbChannel : Java.Lang.Object, IAdbChannel, ICloseable
    {
        public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.UsbChannel");
        private UsbDeviceConnection _Connection;
        /// *************UsbConstants#USB_DIR_OUT if the direction is host to device************** 
        /// **************UsbConstants#USB_DIR_IN if the direction is device to host.*************
        private UsbEndpoint _EndPoint_Host_To_Device;
        private UsbEndpoint _EndPoint_Device_To_Host;
        private UsbInterface _Interface;
        private LinkedList<UsbRequest> _RequestPool;

        #region Inital creation
        /// <summary>
        /// Building out the channel that will be used for all transmission of data
        /// 1) create request pool for incoming communication
        /// 2) Establish interface endpoints
        /// 3)
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="intf"></param>
        public UsbChannel(UsbDeviceConnection connection, UsbInterface intf)
        {
            _RequestPool = new LinkedList<UsbRequest>();
            try
            {
                _Connection = connection;
                _Interface = intf;
                bool found = EstablishInterfaceEndpoints(intf);
                if (!found)
                {
                    throw new Java.IO.IOException("Endpoints could not be established, connection could not proceed");
                }
                //_adbConnection._MaxData = _EndPoint_Host_To_Device.MaxPacketSize;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in constructor UsbChannel: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in constructor UsbChannel: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Constructor UsbChannel: " + ex.StackTrace); throw;
            }

        }

        /// <summary>
        /// https://developer.android.com/reference/android/hardware/usb/UsbEndpoint
        /// 
        /// *************UsbConstants#USB_DIR_OUT if the direction is host to device************** 
        /// **************UsbConstants#USB_DIR_IN if the direction is device to host.*************
        /// 
        /// Searching and establishing in and out endpoints for this Channel and interface
        /// </summary>
        /// <param name="intf"></param>
        /// <returns></returns>
        private bool EstablishInterfaceEndpoints(UsbInterface intf)
        {
            bool found = false;
            UsbEndpoint epOut = null;
            UsbEndpoint epIn = null;
            try
            {
                for (int i = 0; i < intf.EndpointCount; i++)
                {
                    UsbEndpoint ep = intf.GetEndpoint(i);
                    if (ep.Type == UsbAddressing.XferBulk)
                    {
                        _logger.Println("Testing endpoints when starting UsbChannel");
                        if (ep.Direction == UsbAddressing.Out)
                        {
                            epOut = ep;
                            _logger.Println("UsbChannel endpoint Out has been found at: " + ep);
                        }
                        if (ep.Direction == UsbAddressing.In)
                        {
                            epIn = ep;
                            _logger.Println("UsbChannel endpoint used for In has been found at: " + ep);
                        }
                        if (epIn != default && epOut != default) { break; }
                    }
                }
                if (epOut == null || epIn == null)
                {
                    throw new IllegalArgumentException("not all endpoints found");
                }
                _EndPoint_Host_To_Device = epOut;
                _EndPoint_Device_To_Host = epIn;
                found = true;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in UsbChannel EstablishInterfaceEndpoints: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in UsbChannel EstablishInterfaceEndpoints: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in UsbChannel EstablishInterfaceEndpoints: " + ex.StackTrace); throw;
            }
            return found;
        }
        #endregion

        #region read
        /// <summary>
        /// To gather and remove requests existing in the pool
        /// </summary>
        /// <returns></returns>
        public async Task<UsbRequest> GetInRequest()
        {
            try
            {
                lock (_RequestPool)
                {
                    UsbRequest request = new UsbRequest();
                    //synchronized(mInRequestPool) {
                    if (!_RequestPool.Any())//.IsEmpty())
                    {
                        bool success = request.Initialize(_Connection, _EndPoint_Device_To_Host);
                        return request;
                    }
                    else
                    {
                        //request = _RequestPool.poll(_EndPoint_Device_To_Host);
                        request = _RequestPool.First();
                        _RequestPool.RemoveFirst();
                        return request;
                    }
                    //return request;
                }
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in UsbChannel GetInRequest: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in UsbChannel GetInRequest: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in UsbChannel GetInRequest: " + ex.StackTrace); throw;
            }
            return default;
        }

        #region Not needed/never used
        //public async Task<UsbRequest> GetOutRequest()
        //{
        //    try
        //    {
        //        //lock (_RequestPool_In)
        //        //{
        //        UsbRequest request = new UsbRequest();
        //        //synchronized(mInRequestPool) {
        //        if (!_RequestPool_Out.Any())//.IsEmpty())
        //        {
        //            bool success = request.Initialize(_Connection, _EndPoint_Host_To_Device);
        //            return request;
        //        }
        //        else
        //        {
        //            request = _RequestPool_Out.First();
        //            _RequestPool_Out.RemoveFirst();
        //            return request;
        //        }
        //        //}
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured in UsbChannel GetInRequest: " + ex.StackTrace); throw; }
        //    catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in UsbChannel GetInRequest: " + ex.StackTrace); throw; }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in UsbChannel GetInRequest: " + ex.StackTrace); throw;
        //    }
        //    return default;
        //}
        #endregion

        /// <summary>
        /// Add In request to pool
        /// </summary>
        /// <param name="request"></param>
        private void ReleaseInRequest(UsbRequest request)
        {
            try
            {
                lock (_RequestPool)
                {
                    _RequestPool.AddLast(request);
                }
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in Readx ReleaseInRequest: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in Readx  ReleaseInRequest: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Readx ReleaseInRequest: " + ex.StackTrace); throw;
            }
        }

        #region Not needed/never used
        //private void ReleaseOutRequest(UsbRequest request)
        //{
        //    try
        //    {
        //        lock (_RequestPool_Out)
        //        {
        //            _RequestPool_Out.AddLast(request);
        //        }
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured in Readx ReleaseInRequest: " + ex.StackTrace); throw; }
        //    catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in Readx  ReleaseInRequest: " + ex.StackTrace); throw; }
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in Readx ReleaseInRequest: " + ex.StackTrace); throw;
        //    }
        //}
        #endregion

        /// <summary>
        /// Reading from the pool of in requests send from the client device
        /// 
        /// 1)Initalize request gathering 
        /// 2) create bytebuffer that ????????????????????????????? creates a byte buffer with a specific length so it can be checked?
        /// 
        /// 3) add byte buffer to Queue 
        /// 
        /// 4) create while loop that waits for the next usb request.
        /// 
        /// 5) adds the request client data to the byte buffer and resets the data.
        /// 
        /// 6) Handle results
        ///     a) The end point is facing toward the device so it should really not be read because it is going out
        ///     b) If the byte buffers are the same (I think in terms of length, break)
        ///     c)Error
        /// 
        /// 7) Flips the ByteBuffer from write to read
        /// 
        /// 8) Moves Fills the byte array with the bytebuffer
        /// 
        /// ?) I have no idea how it is suppose to be passed back
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        public byte[] Readx(int length)
        {
            byte[] buffer = new byte[length];
            try
            {
                ///Initalize connection to Endpoint
                UsbRequest usbRequest = GetInRequest().Result;
                ///Create bytebuffer matching expected size passed in
                ByteBuffer expected = ByteBuffer.Allocate(length).Order(ByteOrder.LittleEndian);
                usbRequest.ClientData = expected;

                ///Add Empty bytebuffer to request Queue so it can receive informaiton
                bool queued = usbRequest.Queue(expected);
                if (!queued)
                {
                    _logger.Println("Buffer has failed to Queue read usbrequest without length");
                    //throw new IOException("fail to queue read UsbRequest");
                    queued = usbRequest.Queue(expected, length);
                    if (!queued)
                    {
                        _logger.Println("Buffer has failed to Queue read usbrequest (depreciated version)");
                        throw new IOException("fail to queue read UsbRequest");
                    }
                }

                ///Listen and fill bytebuffer?                
                while (true)
                {
                    ///Wait for the Queued operation to finish
                    UsbRequest wait = _Connection.RequestWait();
                    if (wait == null)
                    {
                        throw new IOException("Connection.requestWait return null");
                    }

                    ///add the recieved client data to the byte buffer
                    ByteBuffer clientData = (ByteBuffer)wait.ClientData;
                    wait.ClientData = null;

                    if (wait.Endpoint == _EndPoint_Host_To_Device)
                    {
                        _logger.Println("Usb request complete (line 201 custom channel)");
                        // a write UsbRequest complete, just ignore
                    }
                    else if (expected == clientData)///If the bytebuffer is equal, it means both are essentally default?
                    {
                        ReleaseInRequest(wait);
                        //ReleaseOutRequest(request);
                        break;
                    }
                    else
                    {
                        throw new IOException("unexpected behavior");
                    }
                }
                expected.Flip();
                expected.Get(buffer);
                return buffer;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in Readx UsbChannel: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in Readx  UsbChannel: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Readx UsbChannel: " + ex.StackTrace); throw;
            }
        }
        #endregion

        #region Write

        /// <summary>
        /// Write a Message to the Usb endpoint 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// 
        public async Task Writex(AdbMessage message)
        {
            try
            {
                ///This is not really needed other than for debugging

                //if (!AdbProtocol.ValidateMessage(message))
                //{
                //    _logger.Println("An Android error occured in AdbStream Write. cannot verify message");
                //}
                //if (!AdbProtocol.ValidateMessage(message, "&&&Out&&&"))
                //{
                //    _logger.Println("An Android error occured in AdbStream Write. cannot verify message");
                //}


                ///Original
                byte[] headerMessage = message.GetMessage();
                Writex(headerMessage);
                if (message.GetPayload() != null)
                {
                    byte[] payload = message.GetPayload();
                    Writex(payload);
                }
            }
            catch (AndroidException ex) 
            {
                _logger.Println("An Android error occured in Writex UsbChannel: " + ex.StackTrace); 
                throw; 
            }
            catch (Java.IO.IOException ex) 
            { 
                _logger.Println("An Java io error occured in Writex UsbChannel: " + ex.StackTrace); 
                throw; 
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Writex UsbChannel: " + ex.StackTrace); 
                throw;
            }
        }

        /// <summary>
        /// Writing to device usb endpoint
        /// --Note the Usb Out -> Host => Device (https://developer.android.com/reference/android/hardware/usb/UsbEndpoint)
        /// </summary>
        /// <param name="data"></param>
        private void Writex2(byte[] data, string unused)
        {
            try
            {
                ///Default
                int offset = 0;
                int transferred = 0;

                int maxBufferSize = _EndPoint_Host_To_Device.MaxPacketSize;

                byte[] temp;// = new byte[maxBufferSize];

                //ArrayCopy(buffer, offset, tmp, 0, buffer.Length);
                //int read = -1;
                //while((read = buffer.a)
                //System.Buffer.BlockCopy(buffer, 0, buff, 0, maxBufferSize);
                //if (data.Length >= maxBufferSize)
                //{
                //    temp = new byte[maxBufferSize];
                //    System.Array.Copy(data, temp, maxBufferSize);
                //}
                //else
                //{
                temp = new byte[data.Length];
                System.Array.Copy(data, temp, data.Length);
                //}
                //byte[] buffer = new byte[512];


                //data.CopyTo(buff, 0);
                while ((transferred = _Connection.BulkTransfer(_EndPoint_Host_To_Device, temp, offset, data.Length - offset, 1000000)) >= 0)
                {
                    offset += transferred;
                    if (offset >= data.Length)
                    {
                        break;
                    }
                    else
                    {
                        //if (data.Length >= maxBufferSize)
                        //{
                        //    System.Array.Copy(data, offset,temp, 0,maxBufferSize);
                        //}
                        //else
                        //{
                        System.Array.Copy(data, offset, temp, 0, data.Length - offset);
                        // }
                        //data.CopyTo(buff, offset);
                        //System.Buffer.BlockCopy(buffer,offset,buff,0, maxBufferSize);
                        //buffer.CopyTo(buff);
                    }

                }
                if (transferred < 0)
                {
                    throw new IOException("bulk transfer fail");
                }
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in Writex byte[] UsbChannel: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in Writex byte[] UsbChannel: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Writex Byte[] UsbChannel: " + ex.StackTrace); throw;
            }
        }


        private void Writex2(byte[] data)
        {
            try
            {
                ///Default
                int offset = 0;
                int transferred = 0;

                int maxBufferSize = _EndPoint_Host_To_Device.MaxPacketSize;
                int length = 0;
                                
                
                length = data.Length;
                //length = maxBufferSize;
                byte[] temp = new byte[length];
                //byte[] temp = new byte[maxBufferSize];
                System.Array.Copy(data, temp, length);
                //System.Array.Copy(data, temp, maxBufferSize);

                while ((transferred = _Connection.BulkTransfer(_EndPoint_Host_To_Device, temp, data.Length - offset, 1000000)) >= 0)
                {
                    offset += transferred;
                    if (offset >= data.Length)
                    {
                        break;
                    }
                    else
                    {
                        //temp = new byte[data.Length - offset];
                        //length = data.Length - offset;
                        //length -=offset;
                        //temp = new byte[length];
                        //temp = new byte[maxBufferSize]; 
                        System.Array.Copy(data, offset, temp, 0, data.Length - offset);
                    }
                }
                if (transferred < 0)
                {
                    throw new IOException("bulk transfer fail");
                }
                
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in Writex byte[] UsbChannel: " + ex.StackTrace);
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("An Java io error occured in Writex byte[] UsbChannel: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Writex Byte[] UsbChannel: " + ex.StackTrace);
                throw;
            }
        }

        private void Writex(byte[] data)
        {
            try
            {
                ///Default
                int offset = 0;
                int transferred = 0;

                int maxBufferSize = _EndPoint_Host_To_Device.MaxPacketSize;
                int length = 0;

                byte[] temp;// = new byte[maxBufferSize];
                if (data.Length > maxBufferSize)
                {
                    temp = new byte[maxBufferSize];
                    length = maxBufferSize;
                    System.Array.Copy(data, temp, length);
                }
                else
                {
                    temp = new byte[data.Length];
                    length = data.Length;
                    System.Array.Copy(data, temp, length);
                }


                while (true)
                {
                    //transferred = _Connection.BulkTransfer(_EndPoint_Host_To_Device, data, offset, data.Length - offset, 1000000);
                    //transferred = _Connection.BulkTransfer(_EndPoint_Host_To_Device, data, offset, data.Length, 1000000);
                    transferred = _Connection.BulkTransfer(_EndPoint_Host_To_Device, temp, 0, length, 1000000);
                    offset += transferred;
                    //_logger.Println("Bytes transfered via Writex: " + transferred);
                    if (offset >= data.Length)
                    {
                        break;
                    }
                    else if (transferred < 0)
                    {
                        throw new IOException("bulk transfer fail");
                    }
                    else //if (transferred >= 0)
                    {
                        if ((data.Length - offset) >= maxBufferSize)
                        {
                            temp = new byte[maxBufferSize];
                            //System.Array.Copy(data, temp, maxBufferSize);
                            length = maxBufferSize;
                            System.Array.Copy(data, offset, temp, 0, length);
                        }
                        else
                        {
                            temp = new byte[data.Length - offset];
                            length = data.Length - offset;
                            System.Array.Copy(data, offset, temp, 0, length);
                        }
                    }
                }
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in Writex byte[] UsbChannel: " + ex.StackTrace);
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("An Java io error occured in Writex byte[] UsbChannel: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in Writex Byte[] UsbChannel: " + ex.StackTrace);
                throw;
            }
        }
        #endregion

        public void Close()
        {
            try
            {
                _Connection.ReleaseInterface(_Interface);
                _Connection.Close();
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in UsbChannel Close: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex) { _logger.Println("An Java io error occured in UsbChannel Close: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in UsbChannel Close: " + ex.StackTrace); throw;
            }
        }


    }   
}
