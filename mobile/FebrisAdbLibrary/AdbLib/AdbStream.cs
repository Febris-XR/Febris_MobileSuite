// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Java.Util.Concurrent;
using Java.IO;
using Java.Util.Concurrent.Atomic;
using System;
using System.Text;
using System.Threading.Tasks;
using Java.Interop;
using Android.Util;

namespace Febris.AdbLibrary.AdbLib
{

    /// <summary>
    /// Stream for communication. I think this is 
    /// primarily for TCP but some aspects are also for usb
    /// Todo: Test?
    /// </summary>
    public class AdbStream : Java.Lang.Object, ICloseable
    {
        public LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbStream");
        /** The AdbConnection object that the stream communicates over */
        public AdbConnection _AdbConn;
        //internal CustomConnection _cConn;

        /** The local ID of the stream */
        public int _LocalId;

        /** The remote ID of the stream */
        public int _RemoteId;

        /** Indicates whether a write is currently allowed */
        //public AtomicBoolean _WriteReady;
        public bool _WriteReady;

        /** A queue of data from the target's write packets */
        // internal ConcurrentLinkedQueue _ReadQueue;
        public ConcurrentLinkedQueue _ReadQueue;

        /** Indicates whether the connection is closed already */
        public bool _IsClosed;

        public byte[] _LastPayload { get; set; }


        /**
		 * Creates a new AdbStream object on the specified AdbConnection
		 * with the given local ID.
		 * @param adbConn AdbConnection that this stream is running on
		 * @param localId Local ID of the stream
		 */
        public AdbStream(AdbConnection adbConn, int localId)
        {
            try
            {
                this._AdbConn = adbConn;
                this._LocalId = localId;
                this._ReadQueue = new ConcurrentLinkedQueue();// ();***********this is using something rather different in java and I am unsure if these work together)
                this._WriteReady = false;// new AtomicBoolean(false);
                this._IsClosed = false;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream constructor: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream constructor: " + ex.StackTrace); throw;
            }
        }
        #region old custom
        //public AdbStream(CustomConnection adbConn, int localId)
        //{
        //    try
        //    {
        //        this._cConn = adbConn;
        //        this._LocalId = localId;
        //        this._ReadQueue = new ConcurrentLinkedQueue();//<byte[]>();//***********this is using something rather different in java and I am unsure if these work together)
        //        this._WriteReady = new AtomicBoolean(false);
        //        this._IsClosed = false;
        //    }
        //    catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream constructor: " + ex.StackTrace); throw;}
        //    catch (System.Exception ex)
        //    {
        //        _logger.Println("A generic error occured in AdbStream constructor: " + ex.StackTrace); throw;
        //    }
        //}
        #endregion

        /**
		 * Called by the connection thread to indicate newly received data.
		 * @param payload Data inside the write message
		 */
        public void AddPayload(byte[] payload)
        {
            try
            {
                lock (_ReadQueue)
                {
                    _LastPayload = payload;
                    _ReadQueue.Add(payload);
                    System.Threading.Monitor.PulseAll(_ReadQueue);
                }
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream AddPayload: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream AddPayload: " + ex.StackTrace); throw;
            }
        }

        /**
		 * Called by the connection thread to send an OKAY packet, allowing the
		 * other side to continue transmission.
		 * @throws java.io.IOException If the connection fails while sending the packet
		 */
        public void SendReady()
        {
            try
            {
                /* Generate and send a READY packet */
                _AdbConn._Channel.Writex(AdbProtocol.GenerateReady(_LocalId, _RemoteId));
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream SendReady: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream SendReady: " + ex.StackTrace); throw;
            }
        }

        /**
         * Called by the connection thread to update the remote ID for this stream
         * @param remoteId New remote ID
         */
        public void UpdateRemoteId(int remoteId)
        {
            try
            {
                this._RemoteId = remoteId;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream UpdateRemoteId: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream UpdateRemoteId: " + ex.StackTrace); throw;
            }
        }

        /**
         * Called by the connection thread to indicate the stream is okay to send data.
         */
        public void ReadyForWrite()
        {
            try
            {
                _WriteReady = true;//.Set(true);
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream ReadyForWrite: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream ReadyForWrite: " + ex.StackTrace); throw;
            }
        }

        /**
         * Called by the connection thread to notify that the stream was closed by the peer.
         */
        public void NotifyClose()
        {
            try
            {
                /* We don't call close() because it sends another CLOSE */
                _IsClosed = true;

                /* Unwait readers and writers */
                lock (this)
                {
                    System.Threading.Monitor.PulseAll(this);
                }

                lock (_ReadQueue)
                {
                    System.Threading.Monitor.PulseAll(_ReadQueue);
                }
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream NotifyClose: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream NotifyClose: " + ex.StackTrace); throw;
            }
        }

        /**
         * Reads a pending write payload from the other side.
         * @return Byte array containing the payload of the write
         * @throws InterruptedException If we are unable to wait for data
         * @throws java.io.IOException If the stream fails while waiting
         */
        public async Task<byte[]> Read()
        {
            byte[] data = null;
            try
            {
                lock (_ReadQueue)
                {
                    /* Wait for the connection to close or data to be received */
                    //while (!_IsClosed) && (data = (byte[])_ReadQueue.Poll()) == null)
                    while (!_IsClosed)
                    {
                        var tmp = _ReadQueue.Poll();
                        if (tmp != null)
                        {
                            //(byte[])_ReadQueue.Poll());
                            data = Helpers.ObjectToByteArray(tmp);
                            break;
                        }
                        System.Threading.Monitor.Wait(_ReadQueue, 500);
                    }

                    if (_IsClosed)
                    {
                        throw new IOException("Stream closed");
                    }
                }
                return data;
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbStream Read: " + ex.StackTrace);
                //throw;
            }
            catch (Java.Lang.InterruptedException ex)
            {
                _logger.Println("An Android error occured in AdbStream Read: " + ex.StackTrace);
                //throw;
            }
            catch (IOException ex)
            {
                _logger.Println("A generic error occured in AdbStream Read: " + ex.StackTrace);
                //throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream Read: " + ex.StackTrace);
                //throw;
            }
            return default;
        }

        public async Task<byte[]> ReadLast()
        {
            byte[] data = null;
            try
            {
                lock (_ReadQueue)
                {
                    /* Wait for the connection to close or data to be received */
                    //while (!_IsClosed) && (data = (byte[])_ReadQueue.Poll()) == null)
                    while (!_IsClosed)
                    {
                        byte[] tmp = _LastPayload;
                        if (tmp != null&&tmp!=default)
                        {
                            //(byte[])_ReadQueue.Poll());
                            data = Helpers.ObjectToByteArray(tmp);
                            data = tmp;
                            break;
                        }
                        System.Threading.Monitor.Wait(_ReadQueue, 500);
                    }

                    if (_IsClosed)
                    {
                        throw new IOException("Stream closed");
                    }
                }
                _LastPayload = default;
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbStream Read: " + ex.StackTrace);
                //throw;
            }
            catch (Java.Lang.InterruptedException ex)
            {
                _logger.Println("An Android error occured in AdbStream Read: " + ex.StackTrace);
                //throw;
            }
            catch (IOException ex)
            {
                _logger.Println("A generic error occured in AdbStream Read: " + ex.StackTrace);
                //throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream Read: " + ex.StackTrace);
                //throw;
            }
            return data;
        }

        /**
         * Sends a write packet with a given String payload.
         * @param payload Payload in the form of a String
         * @throws java.io.IOException If the stream fails while sending data
         * @throws InterruptedException If we are unable to wait to send data
         */
        public void Write(string payload) //throws IOException, InterruptedException
        {
            try
            {
                /* ADB needs null-terminated strings */
                byte[] datapackage = Helpers.StringToBytes(payload, char.MinValue);
                Write(datapackage);
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream Write: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream Write: " + ex.StackTrace); throw;
            }
        }

        /**
         * Sends a write packet with a given byte array payload.
         * @param payload Payload in the form of a byte array
         * @throws java.io.IOException If the stream fails while sending data
         * @throws InterruptedException If we are unable to wait to send data
         */
        public void Write(byte[] payload) //throws IOException, InterruptedException
        {
            try
            {
                /* Make sure we're ready for a write */
                lock (this)
                {
                    //while (!_IsClosed && !_WriteReady.CompareAndSet(true, false)) System.Threading.Monitor.Wait(this, AdbProtocol._TimeOut);
                    while (!_IsClosed && !_WriteReady) System.Threading.Monitor.Wait(this, AdbProtocol._TimeOut);
                    if (_IsClosed)
                    {
                        Close();
                        throw new IOException("Stream closed");
                    }
                }

                /* Generate a WRITE packet and send it */
                AdbMessage message = AdbProtocol.GenerateWrite(_LocalId, _RemoteId, payload);

                _AdbConn._Channel.Writex(message);
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbStream Write byte[]: " + ex.StackTrace);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream Write byte[]: " + ex.StackTrace);
                throw;
            }
        }

        /**
         * Closes the stream. This sends a close message to the peer.
         * @throws java.io.IOException If the stream fails while sending the close message.
         * Notifyclose is a pulse method set up in this class. It is needded
         */
        public void Close() //throws IOException
        {
            try
            {
                lock (this)
                {
                    /* This may already be closed by the remote host */
                    if (_IsClosed)
                        return;

                    /* Notify readers/writers that we've closed */
                    NotifyClose();
                }

                _AdbConn._Channel.Writex(AdbProtocol.GenerateClose(_LocalId, _RemoteId));
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream Close: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream Close: " + ex.StackTrace); throw;
            }
        }

        /**
         * Retreives whether the stream is closed or not
         * @return True if the stream is close, false if not
         */
        public bool IsClosed()
        {
            try
            {
                return _IsClosed;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbStream IsClosed: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbStream IsClosed: " + ex.StackTrace); throw;
            }
            return default;
        }

    }
}
