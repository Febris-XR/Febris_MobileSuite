// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.Net.Wifi.P2p;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Networking.WiFi;
using Febris.MobileCompanionV3.Droid.Utilities;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.MobileCompanionV3.P2pCommunication.WiFi;
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Java.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(WiFiP2pServer))]
namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    public class WiFiP2pServer : IWiFiP2pServer
    {
        #region variables and constructors
        //public static WiFiService wifiService= WiFiService.wifiService;
        public static WiFiP2pServer wiFiP2pServer;
        public static ClientSocketThread _clientSocketThread;
        //public static CompanionData _companionData;
        public WiFiP2pServer()
        {
            wiFiP2pServer = this;
        }
        #endregion

        #region Sending/Receiving

        /// <summary>
        /// Create sockets
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ClientSocketCreation()
        {
            Socket socket = new Socket();
            try
            {
                var host = WiFiStaticDetails.HostInfo.GroupOwnerAddress.ToString().Replace(@"/", "");
                int port = WiFiStaticDetails.FebrisSocket;
                Console.WriteLine("host: " + host);
                Console.WriteLine("port: " + port);
                socket.Bind(null);
                socket.Connect(new InetSocketAddress(host, port), 5000);
                if (socket.IsConnected)
                {
                    // Nagle would hold a small frame back waiting for more data to coalesce
                    // with. Set after Connect, since the option applies to a connected socket.
                    // Non-fatal if the platform refuses it.
                    try
                    {
                        socket.TcpNoDelay = true;
                    }
                    catch (Exception noDelayEx)
                    {
                        Console.WriteLine("could not set TcpNoDelay on client socket: " + noDelayEx.Message);
                    }

                    Task.Run(() => SocketReceiver(socket));
                }
                else
                {
                    socket.Dispose();
                    return false;
                }
                return socket?.IsConnected ?? false;
            }
            catch (Exception ex)
            {
                // A FAILED CONNECT IS ROUTINE AND RETRYABLE, NOT FATAL.
                //
                // This used to call ReconnectingSocketToServer() and then rethrow. Both were
                // wrong, and together they turned the single most expected failure on this path
                // into a permanent one.
                //
                // The only caller is FebrisConnectionInfoListener.ConnectToGroupOwner, whose
                // retry loop is `while (!connected)` with a Task.Delay on failure and a `break`
                // in its catch. So the rethrow escaped the loop on the FIRST failure and ended
                // retrying for good, while the Delay branch it was supposed to take never ran.
                //
                // ECONNREFUSED is exactly what a Server that is still booting returns, so the
                // one case the retry exists for was the case that killed it. Observed
                // 2026-07-29: "ECONNREFUSED ... after 5000ms" followed by total silence.
                //
                // The ReconnectingSocketToServer() call was also re-entrant: that method now
                // asks for connection info, which routes straight back into this same connect
                // path. Returning false lets the caller's existing backoff own the retry, which
                // is the only place that should.
                Console.WriteLine("Error Creating Socket: " + ex.Message);
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Creating Socket: " + ex.Message;
                try
                {
                    socket?.Dispose();
                }
                catch (Exception disposeEx)
                {
                    Console.WriteLine("Error Creating Socket: dispose also failed: " + disposeEx.Message);
                }
                return false;
            }
            //finally
            //{                
            //}
        }

        /// <summary>
        /// socket sender called from other methods
        /// </summary>
        /// <param name="dataPacket"></param>
        /// <returns></returns>
        public async Task<bool> SocketSender(byte[] dataPacket)
        {
            bool sent = false;
            try
            {

                if (_clientSocketThread == null)
                {
                    ReconnectingSocketToServer();
                    //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                }
                else if (_clientSocketThread._socket.IsClosed)
                {
                    ReconnectingSocketToServer();
                    //_clientSocketThread._socket.Dispose();
                    //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                }
                else if (_clientSocketThread._socket.IsConnected && !_clientSocketThread._socket.IsClosed)
                {
                    // THIS CONDITION IS NOT A LIVENESS CHECK, and treating it as one is why a
                    // dead link was never redialled. Java's Socket.isConnected() reports whether
                    // the socket EVER connected and stays true after the peer disappears;
                    // isClosed() is true only when close() was called LOCALLY. A socket whose
                    // peer has gone (FIN_WAIT1, which is what was observed on device after the
                    // Server restarted) therefore reports connected-and-not-closed forever and
                    // sails through here.
                    //
                    // The write is still attempted, because there is no cheap way to test a TCP
                    // peer other than writing to it. What changed is that a FAILED write now
                    // forces the reconnect rather than being reported as a benign false: without
                    // this the caller sees "not sent", nothing tears the connection down, and the
                    // next send repeats the whole cycle against the same corpse.
                    P2pSendOutcome outcome = await _clientSocketThread.WriteToSocket(dataPacket);
                    sent = outcome == P2pSendOutcome.Sent;

                    // ONLY a genuine failure forces the redial. A refusal means the handshake
                    // has not resolved yet, which is a HEALTHY connection in the middle of
                    // negotiating, and tearing it down guarantees it never finishes.
                    //
                    // This distinction is not theoretical. While both outcomes were a bare
                    // false, the Companion's own _initalize ping raced BeginHandshake onto a
                    // freshly published connection, was correctly refused by the send gate, and
                    // was then read here as a dead link. The reconnect closed the socket the
                    // handshake was writing its client hello to, BeginHandshake died with
                    // "Socket closed", and the connection was abandoned and never retried. Every
                    // paired device was unable to talk to its peer, while both sides reported
                    // the pairing as stored. See MOBILE_KNOWN_ISSUES.md issue 7.
                    //
                    // Both non-Sent outcomes still report NOT sent to the caller, which is what
                    // keeps a statement out of the Uploaded set so it is retried rather than
                    // dropped.
                    if (outcome == P2pSendOutcome.Failed)
                    {
                        Console.WriteLine("send failed on an apparently-live socket; forcing reconnect");
                        ReconnectingSocketToServer(_clientSocketThread);
                    }
                    else if (outcome == P2pSendOutcome.RefusedHandshakePending)
                    {
                        Console.WriteLine("send deferred: handshake still resolving, the caller should retry");
                    }
                }
                else
                {                    
                    Console.WriteLine("*********************************************************************************************************");
                    Console.WriteLine("******************************Unknown event has occured at SocketSender**********************************");
                    Console.WriteLine("*********************************************************************************************************");
                    //?????
                }
            }
            catch (Exception e)
            {
                sent = false;
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Sending Data Over Socket: " + e.Message;
                Console.WriteLine("Error: " + e.Message);
                //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                ReconnectingSocketToServer();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
            finally
            {
                // MP2P-6: removed GC.Collect + GC.WaitForPendingFinalizers; a forced full GC after
                // every send blocks all managed threads to reclaim memory the runtime would have
                // freed on its own. The dataPacket = null assignment below is also a noop in modern
                // CLRs but is harmless, so we leave it as a documentation hint.
                dataPacket = null;
                //await Task.Delay(5000);
                //Task.
                //ReconnectingSocketToServer();
            }

            return sent;
        }

        /// <summary>
        /// This is the receiver for the open socket for two way communication
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public async Task SocketReceiver(Socket socket)
        {
            // OWN THE INSTANCE, then publish it. The loop below used to dereference the
            // static on every iteration, which meant it operated on whatever connection was
            // current rather than the one it was started for. Combined with the teardown in
            // ReconnectingSocketToServer, which nulled and disposed the static without
            // checking it was still the same object, an old loop exiting after a redial would
            // close the socket of the connection that had just REPLACED it. The Server sees
            // connect-then-immediate-EOF and the pair can ping-pong.
            ClientSocketThread thisConnection = new ClientSocketThread(socket);
            _clientSocketThread = thisConnection;

            // Must run BEFORE the read loop. The Companion is the handshake initiator and
            // the responder stays silent until it receives a client hello, so starting to
            // read first would deadlock both sides. With no SecretStore registered this is
            // a no-op that marks the handshake skipped and the loop proceeds as before.
            if (!await thisConnection.BeginHandshake())
            {
                Console.WriteLine("abandoning connection: handshake could not be started");
                ClearIfCurrent(thisConnection);
                return;
            }

            try
            {
                while (true)
                {
                    try
                    {
                        // Bail out if a newer connection has replaced us. Checking the static
                        // for THIS purpose is correct; using it as the loop's subject was not.
                        if (!ReferenceEquals(_clientSocketThread, thisConnection))
                        {
                            Console.WriteLine("socket receiver: superseded by a newer connection, exiting");
                            return;   // deliberately not clearing: the new connection owns it
                        }

                        if (thisConnection._socket == null)
                        {
                            //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();

                            break;
                        }
                        else if (thisConnection._socket.IsClosed)
                        {
                            //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                            break;
                        }
                        else if (thisConnection._socket.IsConnected && !thisConnection._socket.IsClosed)
                        {
                            await thisConnection.ReadFromSocket();
                        }
                        else
                        {
                            Console.WriteLine("*********************************************************************************************************");
                            Console.WriteLine("******************************Unknown event has occured at SocketReceiver**********************************");
                            Console.WriteLine("*********************************************************************************************************");
                            //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                            //break;
                            //?????
                        }
                    }
                    catch (Exception ex)
                    {
                        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Receiving Data Over Socket Inside Of Listening Loop: " + ex.Message;
                        //Console.WriteLine("error on recieved data: " + ex.Message);
                        //Console.WriteLine("error on recieved data: " + ex.StackTrace);
                        //ReconnectingSocketToServer();
                        //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                        //_clientSocketThread = null;
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
                        //socket.Dispose();
                        break;
                    }
                    // MP2P-6: removed per-iteration GC.Collect + GC.WaitForPendingFinalizers from
                    // the receive loop. Forcing a full GC inside an infinite read-loop pauses every
                    // managed thread on every iteration.
                }
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Receiving Data Over Socket And Has Left Listening Loop: " + ex.Message;
                Console.WriteLine("error on recieved data loop: " + ex.Message);
                Console.WriteLine("error on recieved data loop: " + ex.StackTrace);
                //_clientSocketThread = null;
                //LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = false;
                //socket.Dispose();
            }
            finally
            {
                //_clientSocketThread = null;
                //_clientSocketThread = null;
                //LocalHardwareStaticDetails.StaticMainVM.HomeVM.WiFiConnected = false;
                //_clientSocketThread = null;
                //WiFiService.RestartConnectionDiscovery();
                //if (socket != null)
                //{
                //    try
                //    {
                //        //WiFiService.RestartConnectionDiscovery();                        
                //        //socket.Close();
                //        //socket.Dispose();

                //    }
                //    catch (IOException e)
                //    {
                //        LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Error Receiving Data Over Socket On Socket Closure: " + e.Message;
                //        //socket.Close();
                //        //socket.Dispose();
                //        //WiFiService.wifiService.RefreshReceivedMessage(e.Message);
                //    }
                //}
                //else
                //{
                //    //WiFiService.RestartConnectionDiscovery();
                //    //await SocketCreation();
                //}
                ReconnectingSocketToServer(thisConnection);
                //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                //WiFiService.RestartConnectionDiscovery();
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
        }
        #endregion

        /// <summary>
        /// Null the shared connection ONLY if it still points at <paramref name="candidate"/>.
        ///
        /// Interlocked because the exiting read loop and a fresh connect run on different
        /// threads, and a plain compare-then-assign can be preempted between the two halves,
        /// which is the whole window this closes.
        /// </summary>
        private static void ClearIfCurrent(ClientSocketThread candidate)
        {
            if (candidate == null)
            {
                return;
            }
            System.Threading.Interlocked.CompareExchange(ref _clientSocketThread, null, candidate);
        }

        /// <summary>
        /// Tear down a connection and kick discovery off again.
        /// </summary>
        /// <param name="doomed">The connection being torn down. Passed explicitly rather than
        /// read from the static, because by the time an exiting read loop calls this a NEWER
        /// connection may already be installed, and closing that one is exactly the bug this
        /// parameter exists to prevent. Null means "whatever is current", which is the
        /// behaviour of the external callers that have no instance in hand.</param>
        internal async void ReconnectingSocketToServer(ClientSocketThread doomed = null)
        {
            Console.WriteLine("***************************************************************");
            Console.WriteLine("*************ReconnectingSocketToServer Method hit*************");
            Console.WriteLine("***************************************************************");

            doomed = doomed ?? _clientSocketThread;
            if (doomed == null)
            {
                EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                return;
            }

            try
            {
                if (doomed._socket != null && !doomed._socket.IsClosed)
                {
                    doomed._socket.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Closing the socket threw an error: " + ex.Message);
                //throw;
            }

            try
            {
                if (doomed._socket != null && doomed._socket.IsConnected)
                {
                    doomed._socket.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disposing of socket threw an error: " + ex.Message);
                //throw;
            }

            try
            {
                //_clientSocketThread._socket = null;
                // Clear only if the static still points at the connection this teardown owns.
                // An unconditional null here is how a stale teardown used to erase a fresh
                // connection that had already been installed.
                ClearIfCurrent(doomed);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting Client socket thread back to default: " + ex.Message);
                //throw;
            }

            try
            {
                // ISSUE 14 FAULT C. This used to go straight to RestartConnectionDiscovery, which
                // is right only if the WiFi connection died with the socket. It usually did not.
                //
                // MEASURED on a stranded Companion, with the Server up and advertising:
                //   dumpsys wifip2p -> groupFormed: true, groupOwnerAddress: /192.168.49.1
                //   ip addr         -> inet 192.168.49.115/24 ... p2p0
                // The group belongs to the framework and wpa_supplicant, not to the Server's
                // process, so restarting the SERVER APP kills the TCP socket and leaves the group
                // completely intact. Discovery then cannot help, because the connect path runs
                // from a UPnP response through Connect to ConnectSuccess to ClientSocketCreation,
                // and a device already in the group produces no new connection event to drive it.
                // The Companion sat at "Peer count: 1" forever waiting for a WiFi change that had
                // already happened.
                //
                // So ASK what the connection actually looks like instead of assuming it is gone.
                // FebrisConnectionInfoListener already branches correctly on the answer: group
                // formed goes to ConnectToGroupOwner, which retries ClientSocketCreation, and
                // anything else falls back to RestartConnectionDiscovery. That branch was simply
                // never reached from here.
                if (WiFiStaticDetails.manager != null && WiFiStaticDetails.channel != null)
                {
                    Console.WriteLine("socket teardown: asking for connection info before assuming "
                        + "the group is gone (issue 14 fault C)");
                    WiFiStaticDetails.manager.RequestConnectionInfo(
                        WiFiStaticDetails.channel, new FebrisConnectionInfoListener());
                }
                else
                {
                    // No channel to ask through, so rediscovery is the only option left.
                    Console.WriteLine("socket teardown: no P2P manager or channel, restarting discovery");
                    EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("socket teardown: connection-info request threw, "
                    + "falling back to discovery: " + ex.Message);
                try
                {
                    EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                }
                catch (Exception inner)
                {
                    Console.WriteLine("Restarting connection discovery has thrown an error: " + inner.Message);
                }
            }
            Console.WriteLine("***************************************************************");



        }
    }
    // MP2P-9: [Historical] block preserved under a relabeled region. Below is a
    // commented-out duplicate of `ClientSocketThread` from before the class was
    // extracted to its own file. The live implementation lives in
    // `ClientSocketThread.cs`; this duplicate is on disk to mark where the class
    // used to live + how the inline implementation differed. Do NOT uncomment.
    #region [Historical - MP2P-9] Duplicate ClientSocketThread class - live impl in ClientSocketThread.cs
    //public class ClientSocketThread
    //{
    //    internal Stream _inputStream;
    //    internal Stream _outputStream;
    //    internal Socket _socket;

    //    public ClientSocketThread(Socket socket)
    //    {
    //        _socket = socket;
    //        _inputStream = socket.InputStream;
    //        _outputStream = socket.OutputStream;
    //        //CompanionData._companionData.ClientSocketCreatedSuccessfully(socket);
    //    }

    //    public async Task<bool> WriteToSocket(byte[] dataPackage)
    //    {
    //        bool sent = false;
    //        //var buf = new byte[1024];
    //        Stream inputStream = new MemoryStream(dataPackage);
    //        Stream outputStream = _outputStream;
    //        try
    //        {
    //            int n;
    //            while ((n = await inputStream.ReadAsync(dataPackage, 0, dataPackage.Length)) > 0)
    //            {
    //                await outputStream.WriteAsync(dataPackage, 0, n);
    //            }
    //            inputStream.Close();
    //            inputStream.Dispose();
    //            sent = true;
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error on write to socket: " + ex.Message);
    //            Console.WriteLine("Error on write to socket: " + ex.StackTrace);
    //            _socket.Close();
    //            _socket.Dispose();
    //            //return sent;
    //            throw;
    //        }
    //        finally
    //        {
    //            //await Task.Delay(500);
    //            //outputStream.Close();
    //        }
    //        return sent;
    //    }

    //    public async Task ReadFromSocket()
    //    {
    //        byte[] dataPackage = { };
    //        var buf = new byte[1024];
    //        MemoryStream outputStream = new MemoryStream();
    //        Stream inputStream = _inputStream;
    //        try
    //        {
    //            int n;
    //            #region this breaks the system
    //            //if (inputStream.IsDataAvailable())
    //            //{
    //            //    while ((n = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0)
    //            //    {
    //            //        await outputStream.WriteAsync(buf, 0, n);
    //            //        //if (!inputStream.IsDataAvailable())
    //            //        //{
    //            //        //    break;
    //            //        //}
    //            //        //if (n < 1024)
    //            //        //{
    //            //        //    break;
    //            //        //}
    //            //    }
    //            //    dataPackage = outputStream.ToArray();
    //            //    Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));

    //            //}
    //            //outputStream.Close();
    //            //outputStream.Dispose();
    //            #endregion
    //            #region This works but is a little messy
    //            while ((n = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0)
    //            {
    //                await outputStream.WriteAsync(buf, 0, n);
    //                if (!inputStream.IsDataAvailable())
    //                {
    //                    break;
    //                }
    //            }
    //            if (n <= 0)
    //            {
    //                inputStream.Close();
    //                //inputStream.Dispose();
    //                EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
    //                //_socket.Close();
    //                //_socket.Dispose();
    //            }
    //            dataPackage = outputStream.ToArray();
    //            Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));
    //            outputStream.Close();
    //            outputStream.Dispose();
    //            #endregion
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error on read from socket: " + ex.Message);
    //            //Console.WriteLine("Error on read from socket: " + ex.StackTrace);
    //            _socket.Close();
    //            //WiFiService.RestartConnectionDiscovery();
    //            //_socket.Dispose();
    //        }
    //        finally
    //        {
    //            //inputStream.Close();
    //        }
    //    }

    //}
    #endregion
}