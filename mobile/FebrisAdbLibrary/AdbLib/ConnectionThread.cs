// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.Util;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.AdbLibrary.AdbLib
{
    public class ConnectionThread : Java.Lang.Object, IRunnable
    {
        public static LogPrinter _logger = new Android.Util.LogPrinter(LogPriority.Debug, "Febris.AdbLibrary.AdbConnection.ConnectionThread");
        public AdbConnection _conn { get; set; }
        public ConnectionThread(AdbConnection conn)
        {
            _logger.Println("ConnectionThread Constructor Started");
            _conn = conn;
            Run();
        }

        public void Run()
        {
            _logger.Println("Adb Connection Thread Running");
            while (!_conn._ConnectionThread.IsInterrupted)
            {
                try
                {
                    /* Read and parse a message off the socket's input stream */
                    AdbMessage msg = AdbMessage.ParseAdbMessage(_conn._Channel);

                    /* Verify magic and checksum */
                    
                    //if (!AdbProtocol.ValidateMessage(msg))
                    if (!AdbProtocol.ValidateMessage(msg, "***IN***"))
                        continue;

                    switch (msg.GetCommand())
                    {
                        /* Stream-oriented commands */
                        case AdbProtocol.CMD_OKAY:
                        case AdbProtocol.CMD_WRTE:
                        case AdbProtocol.CMD_CLSE:
                            /* We must ignore all packets when not connected */
                            if (!_conn._Connected)
                                continue;

                            /* Get the stream object corresponding to the packet */
                            //AdbStream waitingStream = _OpenStreams..(msg.GetArg1());********************There may be an issue here******************************
                            AdbStream waitingStream = default;
                            _conn._OpenStreams.TryGetValue(msg.GetArg1(), out waitingStream);


                            if (waitingStream == null || waitingStream == default)
                                continue;

                            int command = msg.GetCommand();

                            lock (waitingStream)
                            {
                                //synchronized(waitingStream) {
                                if (command == AdbProtocol.CMD_OKAY)
                                {
                                    _logger.Println("----------------------------------------Okay Command Recieved-----------------------------------");
                                    /* We're ready for writes */
                                    waitingStream.UpdateRemoteId(msg.GetArg0());

                                    waitingStream.ReadyForWrite();

                                    /* Unwait an open/write */
                                    System.Threading.Monitor.Pulse(waitingStream);
                                }
                                else if (command == AdbProtocol.CMD_WRTE)
                                {
                                    ///This sends back an OKAY message. may need to use the payload to figure out if anything written is important.
                                    _logger.Println("----------------------------------------Write Command Recieved-----------------------------------");
                                    /* Got some data from our partner */
                                    byte[] payload = msg.GetPayload();
                                    waitingStream.AddPayload(payload);

                                    /* Tell it we're ready for more */
                                    waitingStream.SendReady();
                                }
                                else if (command == AdbProtocol.CMD_CLSE)
                                {
                                    _logger.Println("----------------------------------------Close Command Recieved-----------------------------------");
                                    /* He doesn't like us anymore :-( */
                                    _conn._OpenStreams.Remove(msg.GetArg1());

                                    /* Notify readers and writers */
                                    waitingStream.NotifyClose();
                                }
                            }

                            break;

                        case AdbProtocol.CMD_AUTH:
                            AdbMessage packet;
                            if (msg.GetArg0() == AdbProtocol.AUTH_TYPE_TOKEN)
                            {
                                /* This is an authentication challenge */
                                if (_conn._SentSignature)
                                {
                                    /* We've already tried our signature, so send our public key */
                                    packet = AdbProtocol.GenerateAuth(AdbProtocol.AUTH_TYPE_RSA_PUBLIC,
                                            _conn._Crypto.GetAdbPublicKeyPayload());
                                }
                                else
                                {
                                    /* We'll sign the token */
                                    packet = AdbProtocol.GenerateAuth(AdbProtocol.AUTH_TYPE_SIGNATURE,
                                            _conn._Crypto.SignAdbTokenPayload(msg.GetPayload()));
                                    _conn._SentSignature = true;
                                }
                                /* Write the AUTH reply */
                                _conn._Channel.Writex(packet);
                            }
                            break;

                        case AdbProtocol.CMD_CNXN:
                            lock (_conn)
                            {                                
                                /* We need to store the max data size */
                                _conn._MaxData = msg.GetArg1();

                                /* Mark us as connected and unwait anyone waiting on the connection */
                                _conn._Connected = true;
                                System.Threading.Monitor.PulseAll(_conn);
                            }
                            break;

                        default:
                            /* Unrecognized packet, just drop it */
                            break;
                    }
                }
                catch (Java.Lang.Exception e)
                {
                    /* The cleanup is taken care of by a combination of this thread
                     * and close() */
                    _logger.Println("Connection thread has failed: " + e.StackTrace);
                    //e.PrintStackTrace();
                    break;
                }
            }

            /* This thread takes care of cleaning up pending streams */
            //synchronized(_conn) {

            try
            {
                lock (_conn)
                {
                    _conn.CleanupStreams();
                    _conn._ConnectAttempted = false;
                    System.Threading.Monitor.PulseAll(_conn);
                    //_conn.NotifyAll();
                }
            }
            catch (AndroidException ex)
            {
                _logger.Println("An Android error occured in Connection Thread close out: " + ex.StackTrace); throw;
                throw;
            }
            catch (Java.IO.IOException ex)
            {
                _logger.Println("A Java IO error occured in CustomConnection Connection Thread close out: " + ex.StackTrace); throw;
                throw;
            }
            catch (Java.Lang.Exception ex)
            {
                _logger.Println("A java error occured in CustomConnection  Connection Thread close out: " + ex.StackTrace); throw;
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.Println("A generic error occured in CustomConnection  Connection Thread close out: " + ex.StackTrace); throw;
                throw;
            }
        }
    }
}
