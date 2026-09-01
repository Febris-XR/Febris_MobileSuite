// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Java.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.Droid.Networking.WiFi
{
    public class ServerSocketThread
    {
        internal Stream _inputStream;
        internal Stream _outputStream;
        internal Socket _socket;

        // Stateless, so one instance per connection is enough. Reads whole v2 frames
        // off the wire using the length prefix (MDM-B2).
        private readonly IFebrisP2pFrameParser _frameParser = new FebrisP2pFrameParser();

        // Serializes senders. WriteToSocket took no lock, so two concurrent callers could
        // interleave their WriteAsync calls and splice two frames together on the wire.
        // The length-prefixed reader then treats the spliced bytes as one malformed frame.
        // Rare while sends are infrequent, but video shares this socket with control traffic.
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        // The Server accepts, so it is the handshake RESPONDER: it starts on the first
        // frame rather than sending anything proactively. With no SecretStore registered
        // this evaluates to SkipUnauthenticated on the first frame and the read path
        // behaves exactly as it did before the handshake existed.
        private readonly P2pHandshakeCoordinator _handshake =
            P2pHandshakeCoordinator.CreateResponder(P2pHandshakePolicy.SecretStore);

        private readonly IFebrisP2pFrameBuilder _frameBuilder = new FebrisP2pFrameBuilder();

        public ServerSocketThread(Socket socket)
        {
            _socket = socket;
            _inputStream = socket.InputStream;
            _outputStream = socket.OutputStream;
        }

        /// <param name="isHandshakeFrame">True only for the handshake messages themselves,
        /// which must bypass the gate below because they are what resolves it.</param>
        public async Task<bool> WriteToSocket(byte[] dataPackage, bool isHandshakeFrame = false)
        {
            // SEND-SIDE HANDSHAKE GATE. The receive side has always dropped application
            // traffic while the handshake is unresolved; the send side did not check at all,
            // so this tier would emit frames the peer was guaranteed to discard and report
            // them as sent. SocketSender returning true makes the caller believe delivery
            // happened, which on the statement path is how data gets marked Uploaded and
            // deleted.
            //
            // Dormant today because SecretStore is never assigned, so every connection is
            // Skipped and AllowsApplicationTraffic is true from the first instant. It stops
            // being dormant the moment pairing ships.
            if (!isHandshakeFrame && !_handshake.AllowsApplicationTraffic)
            {
                Console.WriteLine("send refused: handshake is " + _handshake.State +
                    ", application traffic is not permitted yet");
                return false;
            }

            bool output = false;
            //var buf = new byte[1024];
            // One frame at a time per connection, so a frame always reaches the wire contiguously.
            await _writeLock.WaitAsync().ConfigureAwait(false);
            Stream inputStream = new MemoryStream(dataPackage);
            Stream outputStream = _outputStream;
            try
            {
                int n;
                while ((n = await inputStream.ReadAsync(dataPackage, 0, dataPackage.Length)) > 0)
                {
                    await outputStream.WriteAsync(dataPackage, 0, n);
                }
                inputStream.Close();
                inputStream.Dispose();
                output = true;
            }
            //catch (SocketException ex)
            //{
            //    ///Need to re-establish connection

            //    Console.WriteLine("Error at WriteToSocket: " + ex.Message);
            //    Console.WriteLine("Error at WriteToSocket: " + ex.StackTrace);
            //    //_socket.Close();
            //    //_socket.Dispose();
            //    throw;
            //}
            catch (Exception ex)
            {
                Console.WriteLine("Error at WriteToSocket: " + ex.Message);
                Console.WriteLine("Error at WriteToSocket: " + ex.StackTrace);
                _socket.Close();
                _socket.Dispose();
                //throw;
            }
            finally
            {
                // Released on every path, otherwise one write failure deadlocks every
                // later sender on this connection.
                _writeLock.Release();
            }
            // MP2P-6: removed per-packet GC.Collect + GC.WaitForPendingFinalizers from finally.
            // Forcing a blocking full GC on every socket write is the opposite of resource discipline --
            // it pauses every managed thread (including the UI) so the runtime can scan the heap to
            // recover a few KB of buffers that the GC would have collected on its own schedule.
            return output;
        }

        /// <summary>
        /// Routes a frame received while the handshake is still unresolved.
        /// Returns true when the caller should go on to dispatch it normally, false when
        /// the frame was consumed by the handshake or the connection must be dropped.
        /// </summary>
        private async Task<bool> HandleHandshakeFrame(byte[] framedPacket)
        {
            PacketHeaderModel header;
            byte[] body;
            try
            {
                (header, body) = _frameParser.ParseBytes(framedPacket);
            }
            catch (FebrisP2pFrameException ex)
            {
                Console.WriteLine("handshake gate: unparseable frame, dropping. " + ex.Message);
                return false;
            }

            if (!P2pPeerAuthorization.IsHandshakeBodyType(header.BodyType))
            {
                // The peer sent application traffic without offering a handshake. Decide
                // once whether that is acceptable. Without this the responder would sit in
                // NotStarted dropping every frame forever, which is the normal case until
                // pairing ships and would break the Server outright.
                if (_handshake.State == P2pHandshakeState.NotStarted)
                {
                    if (_handshake.ResolveUnauthenticatedPeer(header.DeviceUniqueIdentifier))
                    {
                        Console.WriteLine(P2pHandshakePolicy.UnauthenticatedWarning(header.DeviceUniqueIdentifier));
                        return true; // dispatch this frame normally
                    }

                    Console.WriteLine("handshake gate: refusing peer. " + _handshake.FailureReason);
                    _socket.Close();
                    _socket.Dispose();
                    return false;
                }

                // Mid-handshake. Dropping rather than queueing is deliberate: holding it
                // would mean acting on it later for a peer that may yet fail.
                Console.WriteLine("handshake gate: dropping " + header.BodyType + " received before the handshake resolved");
                return false;
            }

            P2pHandshakeMessage reply = _handshake.Handle(header.BodyType, body, header.DeviceUniqueIdentifier);

            if (_handshake.State == P2pHandshakeState.Failed)
            {
                Console.WriteLine("handshake FAILED for '" + _handshake.PeerIdentifier + "': " + _handshake.FailureReason);
                _socket.Close();
                _socket.Dispose();
                return false;
            }

            if (reply != null)
            {
                PacketHeaderModel replyHeader = new PacketHeaderModel
                {
                    BodyType = reply.BodyType,
                    PacketName = reply.BodyType.ToString(),
                    DeviceUniqueIdentifier = header.DeviceUniqueIdentifier
                };
                await WriteToSocket(_frameBuilder.Build(replyHeader, reply.Body), isHandshakeFrame: true);
            }

            if (_handshake.State == P2pHandshakeState.Complete)
            {
                Console.WriteLine("handshake complete with '" + _handshake.PeerIdentifier +
                    "', peer proven: " + _handshake.PeerIsProven);
            }

            return false; // handshake frames are never forwarded to the dispatcher
        }

        public async Task ReadFromSocket()
        {
            byte[] dataPackage;
            Stream inputStream = _inputStream;
            try
            {
                #region this breaks the system
                //if (inputStream.IsDataAvailable())
                //{
                //    while ((n = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0)
                //    {
                //        await outputStream.WriteAsync(buf, 0, n);
                //        //if (!inputStream.IsDataAvailable())
                //        //{
                //        //    break;
                //        //}
                //        //if (n < 1024)
                //        //{
                //        //    break;
                //        //}
                //    }
                //    dataPackage = outputStream.ToArray();
                //    //Task.Run(() => WiFiService.wifiService.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));
                //    Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));

                //}
                //outputStream.Close();
                //   outputStream.Dispose();
                #endregion
                #region Length-driven framing (MDM-B2 resolved)
                // Frame boundaries now come from the v2 length prefix instead of being guessed
                // from socket-buffer occupancy. ReadFrameBytesAsync reads exactly
                // 17 + headerLength + bodyLength and returns the frame verbatim, so a coalesced
                // read no longer silently discards every frame after the first, and a split read
                // no longer truncates.
                //
                // The downstream contract is deliberately UNCHANGED, which is what made the
                // original cutover look expensive: ProcessDownloadedData still receives raw
                // framed bytes and WiFiP2pRequestReceiver still calls ParseBytes on them. Only
                // the boundary decision moved.
                //
                // Returning after a single frame is correct: WiFiP2pServer already calls this in
                // a loop for the life of the connection.
                dataPackage = await _frameParser.ReadFrameBytesAsync(inputStream).ConfigureAwait(false);
                if (dataPackage == null)
                {
                    // Clean end-of-stream ON a frame boundary, i.e. the peer closed normally.
                    inputStream.Close();
                    return;
                }

                // HANDSHAKE GATE. Only while the handshake is unresolved do we pay to parse
                // the header here. Once it completes or skips, frames pass straight through
                // as raw bytes exactly as before, so there is no per-frame cost on the
                // steady-state path (which will be carrying 30-60 video frames a second).
                if (!_handshake.AllowsApplicationTraffic)
                {
                    if (!await HandleHandshakeFrame(dataPackage))
                    {
                        return;
                    }
                }

                // Captured into locals so the queued continuation cannot observe a later value.
                string peerAddress = _socket.InetAddress.ToString().Replace(@"/", "");
                byte[] framedPacket = dataPackage;

                // AWAITED, and that is load-bearing. Without the await this loop reads frame
                // N+1 while frame N is still queued on the thread pool, so two work items race
                // into VideoFrameQueue.TryEnqueue. The queue is a plain FIFO that never sorts
                // on SequenceNumber, so arrival order IS decode order and H.264 access units
                // can reach MediaCodec with their references out of order. The Companion's
                // mirror site (ClientSocketThread.cs) already awaited; only this tier, the one
                // that actually receives video, had lost the guarantee.
                //
                // This is break 1 of docs/MOBILE_P2P_VIDEO.md 1.5, which section 3.4 claims the
                // rewrite closed. It was closed on one side only.
                //
                // It also let a _videoCodecConfig work item land behind frames sent before it,
                // and ProcessCodecConfig clears the queue, discarding valid queued pictures.
                await Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(peerAddress, framedPacket));
                #endregion
                //if (n <= 0)
                //{
                //    inputStream.Close();
                //    inputStream.Dispose();
                //    //_socket.Close();
                //    //_socket.Dispose(); 
                //}
                //if (inputStream.ReadByte() == -1)
                //{
                //    _socket.Close();
                //    _socket.Dispose();                    
                //}
                //dataPackage = outputStream.ToArray();
                ////Task.Run(() => WiFiService.wifiService.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));
                //Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));
                //outputStream.Close();
                //outputStream.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error at ReadFromSocket: " + ex.Message);
                Console.WriteLine("Error at ReadFromSocket: " + ex.StackTrace);
                _socket.Close();
                _socket.Dispose();
            }
            // MP2P-6: removed per-packet GC.Collect + GC.WaitForPendingFinalizers from finally
            // (same reasoning as WriteToSocket above).
        }
    }
}