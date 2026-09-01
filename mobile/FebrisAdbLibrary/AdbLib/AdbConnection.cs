// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Util;
using Febris.AdbLibrary.Interface;
using Java.Interop;
using Java.IO;
using Java.Lang;
using Java.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Febris.AdbLibrary.AdbLib
{

    /// <summary>
    /// NEED TO GO THROUGH THIS
    /// 
    /// *************There seems to be a need for an ADBStream and I don't think any are ever created*******************
    /// </summary>
    public class AdbConnection : Java.Lang.Object, ICloseable, IRunnable
    {
        #region varaibles
        public static LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbConnection");

        /// <summary>
        /// 
        /// </summary>
        public IAdbChannel _Channel;

        /** The last allocated local stream ID. The ID
		 * chosen for the next stream will be this value + 1.
		 */
        public int _LastLocalId;

        /**
		 * The backend thread that handles responding to ADB packets.
		 */
        public Thread _ConnectionThread;

        /**
		 * Specifies whether a connect has been attempted
		 */
        public bool _ConnectAttempted;

        /**
		 * Specifies whether a CNXN packet has been received from the peer.
		 */
        public bool _Connected;

        /**
		 * Specifies the maximum amount data that can be sent to the remote peer.
		 * This is only valid after connect() returns successfully.
		 */
        public int _MaxData;

        /**
		 * An initialized ADB crypto object that contains a key pair.
		 */
        public AdbCrypto _Crypto;

        /**
		 * Specifies whether this connection has already sent a signed token.
		 */        
        public bool _SentSignature;

        /** 
		 * A hash map of our open streams indexed by local ID.
		 **/
        public Dictionary<int, AdbStream> _OpenStreams;        

        /// <summary>
        /// Going to try sockets as the other way seems to not work. ************ this is for tcp and should not work.
        /// </summary>
        //private Socket socket;
        //private InputStream inputStream;
        //OutputStream outputStream;

        #endregion

        #region constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="crypto"></param>
        public AdbConnection(IAdbChannel channel, AdbCrypto crypto)
        {
            try
            {
                _Crypto = crypto;
                _Channel = channel;
                _OpenStreams = new Dictionary<int, AdbStream>();
                _LastLocalId = 0;
                _ConnectionThread = CreateConnectionThread();
                //_MaxData = channel._EndPoint_Host_To_Device.MaxPacketSize;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Constructor: " + ex.StackTrace); throw; }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection Constructor: " + ex.StackTrace); throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="crypto"></param>
        /// <returns></returns>
        public static AdbConnection Create(IAdbChannel channel, AdbCrypto crypto)
        {
            LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbConnection.Create");
            try
            {
                AdbConnection newConn = new AdbConnection(channel, crypto);      
                return newConn;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Create: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in AdbConnection Create: " + ex.StackTrace); throw;
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A Java error occured in AdbConnection Create: " + ex.StackTrace); throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection Create: " + ex.StackTrace); throw;
            }
            return default;
        }

        /// <summary>
        /// Trying with streams this time **********Cant do this becuase this is for tcp
        /// </summary>
        //private AdbConnection(Socket socket, AdbCrypto crypto)
        //{
        //    _OpenStreams = new HashMap<Integer, AdbStream>();
        //    lastLocalId = 0;
        //    connectionThread = createConnectionThread();
        //}
        #endregion

        #region Thread creation and runnable

        /**
        * Creates a new connection thread.
        * @return A new connection thread.
        */
        private Thread CreateConnectionThread()
        {
            try
            {
                _logger.Println("Adb Connection Thread starting");
                AdbConnection conn = this;
                Thread output = new Thread(() => RunningConnectionThread(conn));                
                return output;
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection CreateConnectionThread: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in AdbConnection CreateConnectionThread: " + ex.StackTrace); throw;
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in AdbConnection CreateConnectionThread: " + ex.StackTrace); throw;
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection CreateConnectionThread: " + ex.StackTrace); throw;
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        private void RunningConnectionThread(AdbConnection conn)
        {
            _logger.Println("Starting task to run connection thread");
            Task.Run(async () => new ConnectionThread(conn));
            #region This kinda works but runs on the wrong thread
            //_logger.Println("Adb Connection Thread Running");
            //while (!conn._ConnectionThread.IsInterrupted)
            //{
            //    try
            //    {
            //        /* Read and parse a message off the socket's input stream */
            //        AdbMessage msg = AdbMessage.ParseAdbMessage(conn._Channel);//.parseAdbMessage(channel);



            //        /* Verify magic and checksum */
            //        if (!AdbAdbProtocol.ValidateMessage(msg))
            //            continue;

            //        switch (msg.GetCommand())
            //        {
            //            /* Stream-oriented commands */
            //            case AdbAdbProtocol.CMD_OKAY:
            //            case AdbAdbProtocol.CMD_WRTE:
            //            case AdbAdbProtocol.CMD_CLSE:
            //                /* We must ignore all packets when not connected */
            //                if (!conn._Connected)
            //                    continue;

            //                /* Get the stream object corresponding to the packet */
            //                //AdbStream waitingStream = _OpenStreams..(msg.GetArg1());********************There may be an issue here******************************
            //                AdbStream waitingStream = default;
            //                conn._OpenStreams.TryGetValue(msg.GetArg1(), out waitingStream);//.get(msg.getArg1());


            //                if (waitingStream == null || waitingStream == default)
            //                    continue;
            //                lock (waitingStream)
            //                {
            //                    //synchronized(waitingStream) {
            //                    if (msg.GetCommand() == AdbAdbProtocol.CMD_OKAY)
            //                    {
            //                        /* We're ready for writes */
            //                        waitingStream.UpdateRemoteId(msg.GetArg0());

            //                        waitingStream.ReadyForWrite();

            //                        /* Unwait an open/write */
            //                        System.Threading.Monitor.Pulse(waitingStream);
            //                        //waitingStream.Notify();
            //                    }
            //                    else if (msg.GetCommand() == AdbAdbProtocol.CMD_WRTE)
            //                    {
            //                        /* Got some data from our partner */
            //                        waitingStream.AddPayload(msg.GetPayload());

            //                        /* Tell it we're ready for more */
            //                        waitingStream.SendReady();
            //                    }
            //                    else if (msg.GetCommand() == AdbAdbProtocol.CMD_CLSE)
            //                    {
            //                        /* He doesn't like us anymore :-( */
            //                        conn._OpenStreams.Remove(msg.GetArg1());

            //                        /* Notify readers and writers */
            //                        waitingStream.NotifyClose();
            //                    }
            //                }

            //                break;

            //            case AdbAdbProtocol.CMD_AUTH:

            //                AdbMessage packet;

            //                if (msg.GetArg0() == AdbAdbProtocol.AUTH_TYPE_TOKEN)
            //                {
            //                    /* This is an authentication challenge */
            //                    if (conn._SentSignature)
            //                    {
            //                        /* We've already tried our signature, so send our public key */
            //                        packet = AdbAdbProtocol.GenerateAuth(AdbAdbProtocol.AUTH_TYPE_RSA_PUBLIC,
            //                                conn._Crypto.GetAdbPublicKeyPayload());
            //                    }
            //                    else
            //                    {
            //                        /* We'll sign the token */
            //                        packet = AdbAdbProtocol.GenerateAuth(AdbAdbProtocol.AUTH_TYPE_SIGNATURE,
            //                                conn._Crypto.SignAdbTokenPayload(msg.GetPayload()));
            //                        conn._SentSignature = true;
            //                    }

            //                    /* Write the AUTH reply */
            //                    conn._Channel.Writex(packet);
            //                }
            //                break;

            //            case AdbAdbProtocol.CMD_CNXN:
            //                //synchronized(conn) {
            //                lock (conn)
            //                {
            //                    /* We need to store the max data size */
            //                    conn._MaxData = msg.GetArg1();

            //                    /* Mark us as connected and unwait anyone waiting on the connection */
            //                    conn._Connected = true;
            //                    System.Threading.Monitor.PulseAll(conn);
            //                    //conn.NotifyAll();
            //                }
            //                break;

            //            default:
            //                /* Unrecognized packet, just drop it */
            //                break;
            //        }
            //    }
            //    catch (Java.Lang.Exception e)
            //    {
            //        /* The cleanup is taken care of by a combination of this thread
            //         * and close() */
            //        _logger.Println("Connection thread has failed: " + e.StackTrace);
            //        //e.PrintStackTrace();
            //        break;
            //    }
            //}

            ///* This thread takes care of cleaning up pending streams */
            ////synchronized(conn) {

            //try
            //{
            //    lock (conn)
            //    {
            //        CleanupStreams();
            //        conn._ConnectAttempted = false;
            //        System.Threading.Monitor.PulseAll(conn);
            //        //conn.NotifyAll();
            //    }
            //}
            //catch (AndroidException ex)
            //{
            //    _logger.Println("An Android error occured in Connection Thread close out: " + ex.StackTrace); throw;
            //    throw;
            //}
            //catch (Java.IO.IOException ex)
            //{
            //    _logger.Println("A Java IO error occured in AdbConnection Connection Thread close out: " + ex.StackTrace); throw;
            //    throw;
            //}
            //catch (Java.Lang.Exception ex)
            //{
            //    _logger.Println("A java error occured in AdbConnection  Connection Thread close out: " + ex.StackTrace); throw;
            //    throw;
            //}
            //catch (System.Exception ex)
            //{
            //    _logger.Println("A generic error occured in AdbConnection  Connection Thread close out: " + ex.StackTrace); throw;
            //    throw;
            //}
            #endregion
        }

        #endregion

        #region Connection Interactions
        /// <summary>
        /// Create and initalize new device connection
        /// </summary>
        public void Connect()
        {
            try
            {
                if (_Connected)
                    throw new IllegalStateException("Already connected");
                /* Write the CONNECT packet */
                AdbMessage package = AdbProtocol.GenerateConnect();
                _Channel.Writex(package);
                /* Start the connection thread to respond to the peer */
                _ConnectAttempted = true;
                _ConnectionThread.Start();
                lock (this)
                {
                    if (!_Connected) System.Threading.Monitor.Wait(this, AdbProtocol._TimeOut); 
                    if (!_Connected)
                    {
                        throw new Java.IO.IOException("Connection failed");
                    }
                }
                //_MaxData = 
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Connect: " + ex.StackTrace); throw; }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in AdbConnection Connect: " + ex.StackTrace); throw;
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A Java error occured in AdbConnection Connect: " + ex.StackTrace); throw;
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection Connect: " + ex.StackTrace); throw;
                throw;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public AdbStream Open(string destination)
        {
            try
            {
                int localId = ++_LastLocalId;
                if (!_ConnectAttempted)
                    throw new IllegalStateException("connect() must be called first");
                /* Wait for the connect response */
                lock (this)
                {
                    if (!_Connected) System.Threading.Monitor.Wait(this, AdbProtocol._TimeOut);
                    if (!_Connected)
                    {
                        throw new Java.IO.IOException("Connection failed");
                    }
                }

                /* Add this stream to this list of half-open streams */                
                AdbStream stream = new AdbStream(this, localId);
                _OpenStreams.Add(localId, stream);

                /* Send the open */
                AdbMessage openMessage = AdbProtocol.GenerateOpen(localId, destination);
                _Channel.Writex(openMessage);

                /* Wait for the connection thread to receive the OKAY */
                lock (stream)
                {
                    System.Threading.Monitor.Wait(stream, AdbProtocol._TimeOut);
                }

                /* Check if the open was rejected */
                if (stream.IsClosed())
                    throw new ConnectException("Stream open actively rejected by remote peer");

                /* We're fully setup now */
                return stream;
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in AdbConnection Open: " + ex.StackTrace); 
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in AdbConnection Open: " + ex.StackTrace); 
                throw;
                //throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A Java error occured in AdbConnection Open: " + ex.StackTrace); 
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection Open: " + ex.StackTrace); 
                throw;
            }
            return default;
        }

        #endregion

        #region Helpers
        /**
        * Gets the max data size that the remote client supports.
        * A connection must have been attempted before calling this routine.
        * This routine will block if a connection is in progress.
        * @return The maximum data size indicated in the connect packet.
        * @throws InterruptedException If a connection cannot be waited on.
        * @throws java.io.IOException if the connection fails
        */
        public async Task<int> GetMaxData()
        {
            try
            {
                if (!_ConnectAttempted)
                    throw new IllegalStateException("connect() must be called first");
                /* Block if a connection is pending, but not yet complete */
                lock (this)
                {
                    if (!_Connected) System.Threading.Monitor.Wait(this, AdbProtocol._TimeOut);
                    if (!_Connected)
                    {
                        throw new Java.IO.IOException("Connection failed");
                    }
                }
                //_Channel._EndPoint_Host_To_Device;
                return _MaxData;
            }
            catch (AndroidException ex) 
            { 
                _logger.Println("An Android error occured in AdbConnection GetMaxData: " + ex.StackTrace); 
                throw; 
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection GetMaxData: " + ex.StackTrace); 
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection GetMaxData: " + ex.StackTrace); 
                throw;
            }
            return default;
        }

        /**
        * This function terminates all I/O on streams associated with this ADB connection
        */
        public async Task CleanupStreams()
        {
            try
            {
                /* Close all streams on this connection */
                foreach (AdbStream s in _OpenStreams.Values)
                {
                    /* We handle exceptions for each close() call to avoid
                     * terminating cleanup for one failed close(). */
                    s.Close();
                }
                /* No open streams anymore */
                _OpenStreams.Clear();
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection CleanupStreams: " + ex.StackTrace); throw; }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection CleanupStreams: " + ex.StackTrace); throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection CleanupStreams: " + ex.StackTrace); throw;
            }
        }

        #endregion

        public void Close()
        {
            try
            {
                /* If the connection thread hasn't spawned yet, there's nothing to do */
                if (_ConnectionThread == null)
                    return;

                /* Closing the channel will kick the connection thread */
                _Channel.Close();

                /* Wait for the connection thread to die */
                _ConnectionThread.Interrupt();
                try
                {
                    _ConnectionThread.Join();
                    _ConnectionThread.Dispose();
                }
                catch (InterruptedException e) 
                {
                    _logger.Println("AdbConnection Interrupted");

                }
                CleanupStreams();
            }
            catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Close: " + ex.StackTrace); throw; }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection Close: " + ex.StackTrace); throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in AdbConnection Close: " + ex.StackTrace); throw;
            }
        }

        /// <summary>
        /// Obviouslly this does nothing. I think I can remove the IRunnable interaface
        /// </summary>
        public void Run()
        {
            throw new NotImplementedException();
        }
    }

    //public class ConnectionThread : Java.Lang.Object, IRunnable
    //{
    //    public static LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbConnection.ConnectionThread");
    //    public AdbConnection _conn { get; set; }
    //    public ConnectionThread(AdbConnection conn)
    //    {
    //        _logger.Println("ConnectionThread Constructor Started");
    //        _conn = conn;
    //        Run();
    //    }

    //    public void Run()
    //    {
    //        _logger.Println("Adb Connection Thread Running");
    //        while (!_conn._ConnectionThread.IsInterrupted)
    //        {
    //            try
    //            {
    //                /* Read and parse a message off the socket's input stream */
    //                AdbMessage msg = AdbMessage.ParseAdbMessage(_conn._Channel);

    //                /* Verify magic and checksum */
    //                if (!AdbProtocol.ValidateMessage(msg))
    //                    continue;

    //                switch (msg.GetCommand())
    //                {
    //                    /* Stream-oriented commands */
    //                    case AdbProtocol.CMD_OKAY:
    //                    case AdbProtocol.CMD_WRTE:
    //                    case AdbProtocol.CMD_CLSE:
    //                        /* We must ignore all packets when not connected */
    //                        if (!_conn._Connected)
    //                            continue;

    //                        /* Get the stream object corresponding to the packet */
    //                        //AdbStream waitingStream = _OpenStreams..(msg.GetArg1());********************There may be an issue here******************************
    //                        AdbStream waitingStream = default;
    //                        _conn._OpenStreams.TryGetValue(msg.GetArg1(), out waitingStream);


    //                        if (waitingStream == null || waitingStream == default)
    //                            continue;
    //                        lock (waitingStream)
    //                        {
    //                            //synchronized(waitingStream) {
    //                            if (msg.GetCommand() == AdbProtocol.CMD_OKAY)
    //                            {
    //                                /* We're ready for writes */
    //                                waitingStream.UpdateRemoteId(msg.GetArg0());

    //                                waitingStream.ReadyForWrite();

    //                                /* Unwait an open/write */
    //                                System.Threading.Monitor.Pulse(waitingStream);                                    
    //                            }
    //                            else if (msg.GetCommand() == AdbProtocol.CMD_WRTE)
    //                            {
    //                                /* Got some data from our partner */
    //                                waitingStream.AddPayload(msg.GetPayload());

    //                                /* Tell it we're ready for more */
    //                                waitingStream.SendReady();
    //                            }
    //                            else if (msg.GetCommand() == AdbProtocol.CMD_CLSE)
    //                            {
    //                                /* He doesn't like us anymore :-( */
    //                                _conn._OpenStreams.Remove(msg.GetArg1());

    //                                /* Notify readers and writers */
    //                                waitingStream.NotifyClose();                                    
    //                            }
    //                        }

    //                        break;

    //                    case AdbProtocol.CMD_AUTH:
    //                        AdbMessage packet;
    //                        if (msg.GetArg0() == AdbProtocol.AUTH_TYPE_TOKEN)
    //                        {
    //                            /* This is an authentication challenge */
    //                            if (_conn._SentSignature)
    //                            {
    //                                /* We've already tried our signature, so send our public key */
    //                                packet = AdbProtocol.GenerateAuth(AdbProtocol.AUTH_TYPE_RSA_PUBLIC,
    //                                        _conn._Crypto.GetAdbPublicKeyPayload());
    //                            }
    //                            else
    //                            {
    //                                /* We'll sign the token */
    //                                packet = AdbProtocol.GenerateAuth(AdbProtocol.AUTH_TYPE_SIGNATURE,
    //                                        _conn._Crypto.SignAdbTokenPayload(msg.GetPayload()));
    //                                _conn._SentSignature = true;
    //                            }
    //                            /* Write the AUTH reply */
    //                            _conn._Channel.Writex(packet);
    //                        }
    //                        break;

    //                    case AdbProtocol.CMD_CNXN:                            
    //                        lock (_conn)
    //                        {
    //                            /* We need to store the max data size */
    //                            _conn._MaxData = msg.GetArg1();

    //                            /* Mark us as connected and unwait anyone waiting on the connection */
    //                            _conn._Connected = true;
    //                            System.Threading.Monitor.PulseAll(_conn);
    //                        }
    //                        break;

    //                    default:
    //                        /* Unrecognized packet, just drop it */
    //                        break;
    //                }
    //            }
    //            catch (Java.Lang.Exception e)
    //            {
    //                /* The cleanup is taken care of by a combination of this thread
    //                 * and close() */
    //                _logger.Println("Connection thread has failed: " + e.StackTrace);
    //                //e.PrintStackTrace();
    //                break;
    //            }
    //        }

    //        /* This thread takes care of cleaning up pending streams */
    //        //synchronized(_conn) {

    //        try
    //        {
    //            lock (_conn)
    //            {
    //                _conn.CleanupStreams();
    //                _conn._ConnectAttempted = false;
    //                System.Threading.Monitor.PulseAll(_conn);
    //                //_conn.NotifyAll();
    //            }
    //        }
    //        catch (AndroidException ex)
    //        {
    //            _logger.Println("An Android error occured in Connection Thread close out: " + ex.StackTrace); throw;
    //            throw;
    //        }
    //        catch (Java.IO.IOException ex)
    //        {
    //            _logger.Println("A Java IO error occured in CustomConnection Connection Thread close out: " + ex.StackTrace); throw;
    //            throw;
    //        }
    //        catch (Java.Lang.Exception ex)
    //        {
    //            _logger.Println("A java error occured in CustomConnection  Connection Thread close out: " + ex.StackTrace); throw;
    //            throw;
    //        }
    //        catch (System.Exception ex)
    //        {
    //            _logger.Println("A generic error occured in CustomConnection  Connection Thread close out: " + ex.StackTrace); throw;
    //            throw;
    //        }
    //    }
    //}

    #region Old
    // public class AdbConnection : Java.Lang.Object, ICloseable
    // {
    //     public static LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbConnection");

    //     #region Variables

    //     //public static IAdbChannel _Channel;
    //     public IAdbChannel Channel;

    //     /** The last allocated local stream ID. The ID
    //* chosen for the next stream will be this value + 1.
    //*/
    //     //public static int _LastLocalId;
    //     public int LastLocalId;

    //     /**
    //* The backend thread that handles responding to ADB packets.
    //*/
    //     //public static Thread _ConnectionThread;
    //     public Thread ConnectionThread;

    //     /**
    //* Specifies whether a connect has been attempted
    //*/
    //     //public static bool _ConnectAttempted;
    //     public bool ConnectAttempted;

    //     /**
    //* Specifies whether a CNXN packet has been received from the peer.
    //*/
    //     //public static bool _Connected;
    //     public bool Connected;

    //     /**
    //* Specifies the maximum amount data that can be sent to the remote peer.
    //* This is only valid after connect() returns successfully.
    //*/
    //     //public static int _MaxData;
    //     public int MaxData;

    //     /**
    //* An initialized ADB crypto object that contains a key pair.
    //*/
    //     //public static AdbCrypto _Crypto;
    //     public AdbCrypto Crypto;

    //     /**
    //* Specifies whether this connection has already sent a signed token.
    //*/
    //     //public static bool _SentSignature;
    //     public bool SentSignature;

    //     /** 
    //* A hash map of our open streams indexed by local ID.
    //**/
    //     //private Dictionary<int, AdbStream> openStreams;
    //     //public static Dictionary<int, AdbStream> _OpenStreams;
    //     public Dictionary<int, AdbStream> OpenStreams;
    //     //private HashMap<int, AdbStream> _OpenStreams=new HashMap<int, AdbStream>();

    //     #endregion

    //     /**
    //* Internal constructor to initialize some internal state
    //*/
    //     //public AdbConnection()
    //     //{
    //     //    try
    //     //    {
    //     //        _OpenStreams = new Dictionary<int, AdbStream>();
    //     //        _LastLocalId = 0;
    //     //        _ConnectionThread = CreateConnectionThread();
    //     //    }
    //     //    catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Constructor: " + ex.StackTrace); throw;}
    //     //    catch (System.Exception ex)
    //     //    {
    //     //        _logger.Println("A generic error occured in AdbConnection Constructor: " + ex.StackTrace); throw;
    //     //    }
    //     //}
    //     public AdbConnection(IAdbChannel channel, AdbCrypto crypto)
    //     {
    //         try
    //         {
    //             //_Crypto = crypto;
    //             //_Channel = channel;
    //             //_OpenStreams = new Dictionary<int, AdbStream>();
    //             //_LastLocalId = 0;
    //             //_ConnectionThread = CreateConnectionThread();
    //             Crypto = crypto;
    //             Channel = channel;
    //             OpenStreams = new Dictionary<int, AdbStream>();
    //             LastLocalId = 0;
    //             ConnectionThread = CreateConnectionThread();
    //         }
    //         catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Constructor: " + ex.StackTrace); throw;}
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection Constructor: " + ex.StackTrace); throw;
    //         }
    //     }

    //     /**
    //* Creates a AdbConnection object associated with the socket and
    //* crypto object specified.
    //* @param channel The channel that the connection will use for communcation.
    //* @param crypto The crypto object that stores the key pair for authentication.
    //* @return A new AdbConnection object.
    //* @throws java.io.IOException If there is a socket error
    //*/
    //     //There may be an issue with 2 channels being created.
    //     public static AdbConnection Create(IAdbChannel channel, AdbCrypto crypto) 
    //     {
    //         LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary");
    //         try
    //         {
    //             AdbConnection newConn = new AdbConnection(channel, crypto);
    //             //newConn._Crypto = crypto;
    //             //newConn._Channel = channel;                
    //             return newConn;
    //         }
    //         catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Create: " + ex.StackTrace); throw;}
    //         catch (Java.Lang.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection Create: " + ex.StackTrace); throw;
    //         }
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection Create: " + ex.StackTrace); throw;
    //         }
    //         return default;
    //     }

    //     /**
    //      * Creates a new connection thread.
    //      * @return A new connection thread.
    //      */
    //     private Thread CreateConnectionThread()
    //     {
    //         try
    //         {
    //             _logger.Println("Adb Connection Thread starting");
    //             AdbConnection conn = this;
    //             Thread output = new Thread(() => RunningConnectionThread(conn));
    //             return output;
    //         }
    //         catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection CreateConnectionThread: " + ex.StackTrace); throw; }
    //         catch (Java.Lang.Exception ex)
    //         {
    //             _logger.Println("A java error occured in AdbConnection CreateConnectionThread: " + ex.StackTrace); throw;
    //             throw;
    //         }
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection CreateConnectionThread: " + ex.StackTrace); throw;
    //             throw;
    //         }
    //         //return default;
    //     }

    //     private void RunningConnectionThread(AdbConnection conn)
    //     {
    //         ////Task.Run(() => UsbThread());
    //         ////conn._ConnectionThread = //Task.Run(() => UsbThread());//new Thread(new UsbThread());
    //         ////conn._ConnectionThread = new Thread(new RunnableThreadHelper(this));
    //         //AdbConnection._ConnectionThread = new Thread(() => new UsbThread(this));
    //         ////conn._ConnectionThread.Start();

    //         //return AdbConnection._ConnectionThread;

    //         //return new Thread(//() => new Runnable(() =>
    //         //                  // {
    //         //                  //void Run()
    //         //                  //{
    //         _logger.Println("Adb Connection Thread Running");
    //         while (!conn.ConnectionThread.IsInterrupted)
    //         {
    //             try
    //             {
    //                 /* Read and parse a message off the socket's input stream */
    //                 AdbMessage msg = AdbMessage.ParseAdbMessage(conn.Channel);//.parseAdbMessage(channel);

    //                 /* Verify magic and checksum */
    //                 if (!AdbProtocol.ValidateMessage(msg))
    //                     continue;

    //                 switch (msg.GetCommand())
    //                 {
    //                     /* Stream-oriented commands */
    //                     case AdbProtocol.CMD_OKAY:
    //                     case AdbProtocol.CMD_WRTE:
    //                     case AdbProtocol.CMD_CLSE:
    //                         /* We must ignore all packets when not connected */
    //                         if (!conn.Connected)//.Connected)
    //                             continue;

    //                         /* Get the stream object corresponding to the packet */
    //                         //AdbStream waitingStream = _OpenStreams..(msg.GetArg1());
    //                         AdbStream waitingStream = default;
    //                         conn.OpenStreams.TryGetValue(msg.GetArg1(), out waitingStream);//.get(msg.getArg1());


    //                         if (waitingStream == null)
    //                             continue;
    //                         lock (waitingStream)
    //                         {
    //                             //synchronized(waitingStream) {
    //                             if (msg.GetCommand() == AdbProtocol.CMD_OKAY)
    //                             {
    //                                 /* We're ready for writes */
    //                                 waitingStream.UpdateRemoteId(msg.GetArg0());
    //                                 waitingStream.ReadyForWrite();

    //                                 /* Unwait an open/write */
    //                                 waitingStream.Notify();
    //                             }
    //                             else if (msg.GetCommand() == AdbProtocol.CMD_WRTE)
    //                             {
    //                                 /* Got some data from our partner */
    //                                 waitingStream.AddPayload(msg.GetPayload());

    //                                 /* Tell it we're ready for more */
    //                                 waitingStream.SendReady();
    //                             }
    //                             else if (msg.GetCommand() == AdbProtocol.CMD_CLSE)
    //                             {
    //                                 /* He doesn't like us anymore :-( */
    //                                 conn.OpenStreams.Remove(msg.GetArg1());

    //                                 /* Notify readers and writers */
    //                                 waitingStream.NotifyClose();
    //                             }
    //                         }

    //                         break;

    //                     case AdbProtocol.CMD_AUTH:

    //                         AdbMessage packet;

    //                         if (msg.GetArg0() == AdbProtocol.AUTH_TYPE_TOKEN)
    //                         {
    //                             /* This is an authentication challenge */
    //                             if (conn.SentSignature)
    //                             {
    //                                 /* We've already tried our signature, so send our public key */
    //                                 packet = AdbProtocol.GenerateAuth(AdbProtocol.AUTH_TYPE_RSA_PUBLIC,
    //                                         conn.Crypto.GetAdbPublicKeyPayload());
    //                             }
    //                             else
    //                             {
    //                                 /* We'll sign the token */
    //                                 packet = AdbProtocol.GenerateAuth(AdbProtocol.AUTH_TYPE_SIGNATURE,
    //                                         conn.Crypto.SignAdbTokenPayload(msg.GetPayload()));
    //                                 conn.SentSignature = true;
    //                             }

    //                             /* Write the AUTH reply */
    //                             conn.Channel.Writex(packet);
    //                         }
    //                         break;

    //                     case AdbProtocol.CMD_CNXN:
    //                         //synchronized(conn) {
    //                         lock (conn)
    //                         {
    //                             /* We need to store the max data size */
    //                             conn.MaxData = msg.GetArg1();

    //                             /* Mark us as connected and unwait anyone waiting on the connection */
    //                             conn.Connected = true;
    //                             //System.Threading.Monitor.PulseAll(conn, CustomAdbProtocol._TimeOut);
    //                             conn.NotifyAll();
    //                         }
    //                         break;

    //                     default:
    //                         /* Unrecognized packet, just drop it */
    //                         break;
    //                 }
    //             }
    //             catch (Java.Lang.Exception e)
    //             {
    //                 /* The cleanup is taken care of by a combination of this thread
    //                  * and close() */
    //                 e.PrintStackTrace();
    //                 break;
    //             }
    //         }

    //         /* This thread takes care of cleaning up pending streams */
    //         //synchronized(conn) {

    //         try
    //         {
    //             lock (conn)
    //             {
    //                 CleanupStreams();
    //                 conn.ConnectAttempted = false;
    //                 conn.NotifyAll();
    //             }
    //         }
    //         catch (AndroidException ex)
    //         {
    //             _logger.Println("An Android error occured in Connection Thread close out: " + ex.StackTrace); throw;
    //             //throw; 
    //         }
    //         catch (Java.Lang.Exception ex)
    //         {
    //             _logger.Println("A java error occured in AdbConnection  Connection Thread close out: " + ex.StackTrace); throw;
    //             //throw;
    //         }
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection  Connection Thread close out: " + ex.StackTrace); throw;
    //             //throw;
    //         }
    //     }


    //     /**
    //      * Gets the max data size that the remote client supports.
    //      * A connection must have been attempted before calling this routine.
    //      * This routine will block if a connection is in progress.
    //      * @return The maximum data size indicated in the connect packet.
    //      * @throws InterruptedException If a connection cannot be waited on.
    //      * @throws java.io.IOException if the connection fails
    //      */
    //     //public int GetMaxData()// throws InterruptedException, IOException
    //     public async Task<int> GetMaxData()
    //     {
    //         try
    //         {
    //             if (!ConnectAttempted)
    //                 throw new IllegalStateException("connect() must be called first");

    //             /* Block if a connection is pending, but not yet complete */
    //             //lock (this)
    //             lock (typeof(AdbConnection))
    //             {
    //                 if (!Connected)
    //                     Wait();

    //                 if (!Connected)
    //                 {
    //                     throw new Java.IO.IOException("Connection failed");
    //                 }
    //             }
    //             //int timekeeper = 0;
    //             //while (!Connected)
    //             //{
    //             //    Thread.Sleep(500);
    //             //    timekeeper += 500;
    //             //    if (timekeeper > AdbProtocol._TimeOut)
    //             //    {
    //             //        throw new Java.IO.IOException("Connection failed");
    //             //    }
    //             //}
    //             return MaxData;
    //         }
    //         catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection GetMaxData: " + ex.StackTrace); throw;}
    //         catch (Java.Lang.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection GetMaxData: " + ex.StackTrace); throw;
    //         }
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection GetMaxData: " + ex.StackTrace); throw;
    //         }
    //         return default;
    //     }

    //     /**
    //      * Connects to the remote device. This routine will block until the connection
    //      * completes.
    //      * @throws java.io.IOException If the socket fails while connecting
    //      * @throws InterruptedException If we are unable to wait for the connection to finish
    //      */
    //     // public void Connect()// //throws IOException, InterruptedException
    //     public void Connect()
    //     {
    //         try
    //         {
    //             if (Connected)
    //                 throw new IllegalStateException("Already connected");

    //             /* Write the CONNECT packet */
    //             AdbMessage package = AdbProtocol.GenerateConnect();///Thisi is still throwing underflow exception
    //             Channel.Writex(package);

    //             /* Start the connection thread to respond to the peer */
    //             ConnectAttempted = true;
    //             //ConnectionThread.Run();
    //             ConnectionThread.Start();

    //             /* Wait for the connection to go live */
    //             //lock (this)
    //             //lock (typeof(AdbConnection))
    //             AdbConnection conn = this;
    //             lock (conn)
    //             {
    //                 if (!Connected) conn.Wait(AdbProtocol._TimeOut);

    //                 if (!Connected)
    //                 {
    //                     throw new Java.IO.IOException("Connection failed");
    //                 }
    //             }
    //         }
    //         catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Connect: " + ex.StackTrace); throw;}
    //         catch (Java.Lang.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection Connect: " + ex.StackTrace); throw;
    //         }
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection Connect: " + ex.StackTrace); throw;
    //         }

    //     }

    //     /**
    //      * Opens an AdbStream object corresponding to the specified destination.
    //      * This routine will block until the connection completes.
    //      * @param destination The destination to open on the target
    //      * @return AdbStream object corresponding to the specified destination
    //      * @throws java.io.UnsupportedEncodingException If the destination cannot be encoded to UTF-8
    //      * @throws java.io.IOException If the stream fails while sending the packet
    //      * @throws InterruptedException If we are unable to wait for the connection to finish
    //      */
    //     public AdbStream Open(string destination)// throws UnsupportedEncodingException, IOException, InterruptedException
    //                                              //public async Task<AdbStream> Open(string destination)
    //     {
    //         try
    //         {
    //             int localId = ++LastLocalId;

    //             if (!ConnectAttempted)
    //                 throw new IllegalStateException("connect() must be called first");

    //             /* Wait for the connect response */
    //             AdbConnection conn = this;
    //             lock (conn)
    //             {
    //                 //lock (this)
    //                 //lock (typeof(AdbConnection))
    //                 // {                    
    //                 if (!Connected) conn.Wait(AdbProtocol._TimeOut);

    //                 if (!Connected)
    //                 {
    //                     throw new Java.IO.IOException("Connection failed");
    //                 }
    //             }



    //             /* Add this stream to this list of half-open streams */
    //             //AdbStream stream = OpenStreams[localId] ?? default;
    //             AdbStream stream;
    //             OpenStreams.TryGetValue(localId, out stream);
    //             if (stream != default)
    //             {
    //                 stream = new AdbStream(this, localId);
    //                 OpenStreams[localId] = stream;
    //             }
    //             else
    //             {
    //                 stream = new AdbStream(this, localId);
    //                 OpenStreams.Add(localId, stream);
    //             }

    //             /* Send the open */
    //             Channel.Writex(AdbProtocol.GenerateOpen(localId, destination));


    //             lock (stream)
    //             {
    //                 stream.Wait(AdbProtocol._TimeOut);
    //             }

    //             /* Wait for the connection thread to receive the OKAY */
    //             //synchronized(stream) {
    //             //    stream.Wait(AdbProtocol._TimeOut);
    //             //}
    //             //lock (stream)
    //             //{

    //             //    this.Wait(stream);
    //             //}



    //             /* Check if the open was rejected */
    //             if (stream.IsClosed())
    //                 throw new ConnectException("Stream open actively rejected by remote peer");

    //             /* We're fully setup now */
    //             return stream;
    //         }
    //         catch (AndroidException ex)
    //         {
    //             _logger.Println("An Android error occured in AdbConnection Open: " + ex.StackTrace); throw;
    //         }
    //         catch (Java.Lang.Exception ex)
    //         {
    //             _logger.Println("A Java error occured in AdbConnection Open: " + ex.StackTrace); throw;
    //         }
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection Open: " + ex.StackTrace); throw;
    //         }
    //         return default;
    //     }

    //     /**
    //      * This function terminates all I/O on streams associated with this ADB connection
    //      */
    //     // private void CleanupStreams()
    //     public async Task CleanupStreams()
    //     {
    //         try
    //         {
    //             /* Close all streams on this connection */

    //             foreach (AdbStream s in OpenStreams.Values)//.values())
    //             {
    //                 /* We handle exceptions for each close() call to avoid
    //                  * terminating cleanup for one failed close(). */
    //                 //try
    //                 //{
    //                 s.Close();
    //                 //}
    //                 //catch (IOException e) { }
    //             }

    //             /* No open streams anymore */
    //             OpenStreams.Clear();
    //         }
    //         catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection CleanupStreams: " + ex.StackTrace); throw;}
    //         catch (Java.Lang.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection CleanupStreams: " + ex.StackTrace); throw;
    //         }
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection CleanupStreams: " + ex.StackTrace); throw;
    //         }
    //     }

    //     /** This routine closes the Adb connection and underlying socket
    //      * @throws java.io.IOException if the socket fails to close
    //      */
    //     //@Override
    //     //public void Close()// //throws IOException
    //     public void Close()
    //     {
    //         try
    //         {
    //             /* If the connection thread hasn't spawned yet, there's nothing to do */
    //             if (ConnectionThread == null)
    //                 return;

    //             /* Closing the channel will kick the connection thread */
    //             Channel.Close();//.close();

    //             /* Wait for the connection thread to die */
    //             ConnectionThread.Interrupt();
    //             try
    //             {
    //                 ConnectionThread.Join();
    //             }
    //             catch (InterruptedException e) { }

    //             CleanupStreams();
    //         }
    //         catch (AndroidException ex) { _logger.Println("An Android error occured in AdbConnection Close: " + ex.StackTrace); throw;}
    //         catch (Java.Lang.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection Close: " + ex.StackTrace); throw;
    //         }
    //         catch (System.Exception ex)
    //         {
    //             _logger.Println("A generic error occured in AdbConnection Close: " + ex.StackTrace); throw;
    //         }
    //     }
    // }
    #endregion
}


