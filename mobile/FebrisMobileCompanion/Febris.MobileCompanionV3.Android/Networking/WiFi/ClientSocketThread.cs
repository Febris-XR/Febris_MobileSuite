// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Febris.MobileCompanionV3.Droid.Utilities.EventHandlers;
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.Interfaces;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Java.Net;
using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Febris.MobileCompanionV3.Droid.Networking.WiFi
{
    public class ClientSocketThread
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

        // The Companion dials out, so it is the handshake INITIATOR and must send the
        // client hello proactively. Waiting for a read first would deadlock: the responder
        // has nothing to say until it receives one.
        //
        // PEER KEY. See ResolveServerIdentity below: the store is keyed on the group owner's
        // WiFi MAC, learned from the WiFi Direct connection event rather than from any frame
        // the peer authored. It only selects WHICH secret to try; the handshake is what proves
        // the peer actually holds it, so this is not an identity claim.
        private readonly P2pHandshakeCoordinator _handshake;
        private readonly IFebrisP2pFrameBuilder _frameBuilder = new FebrisP2pFrameBuilder();

        public ClientSocketThread(Socket socket)
        {
            _socket = socket;
            _inputStream = socket.InputStream;
            _outputStream = socket.OutputStream;
            _handshake = P2pHandshakeCoordinator.CreateInitiator(
                P2pHandshakePolicy.SecretStore,
                ResolveServerIdentity(socket));
            //CompanionData._companionData.ClientSocketCreatedSuccessfully(socket);
        }

        /// <summary>
        /// The string this Companion looks its pairing secret up under.
        ///
        /// WHY NOT THE IP ADDRESS, which is what this used to be. The PSK is per-pair and
        /// permanent; the IP is per-session and assigned by whoever owns the group. Keying on
        /// it meant a paired device stopped being recognised the moment DHCP handed out a
        /// different address. Worse, the IP is the same value `UpdateIPAddress` lets an
        /// unauthenticated frame rewrite, so an attacker could steer which secret this side
        /// went looking for.
        ///
        /// The WiFi MAC of the group owner is stable, is learned from the WiFi Direct
        /// connection event rather than from any frame the peer authored, and is already
        /// persisted in GroupOwnerDevice.WifiMacAddress and hydrated into P2pGroup.OwnerAddress
        /// at startup (MainViewModel.cs:72).
        ///
        /// NOTE THE DELIBERATE ASYMMETRY WITH THE RESPONDER, which keys by the Companion's
        /// DeviceUniqueIdentifier. That is correct rather than an inconsistency: each side keys
        /// by the stable identifier it actually holds for the OTHER device. The pairing flow's
        /// job is to write the same PSK under each side's own name for its peer.
        ///
        /// Falls back to the socket address only so an unpaired device behaves exactly as it
        /// did before. With no secret under either string the disposition is identical, so the
        /// fallback cannot mask a pairing that should have been found.
        /// </summary>
        private static string ResolveServerIdentity(Socket socket)
        {
            try
            {
                string ownerMac = LocalHardwareStaticDetails.StaticMainVM?.ConfigVM?.P2pGroup?.OwnerAddress;
                if (!string.IsNullOrWhiteSpace(ownerMac))
                {
                    return ownerMac;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ResolveServerIdentity: group owner unavailable: " + ex.Message);
            }

            // SECOND SOURCE FOR THE SAME IDENTITY: the live WiFi Direct group. The view model is
            // hydrated from persisted state at startup and can legitimately be empty on the first
            // connection after a reinstall or a data clear, which is exactly when this used to fall
            // through to the IP. The group owner's DeviceAddress is the SAME value pairing keys on,
            // so this resolves correctly rather than merely differently.
            try
            {
                string liveOwnerMac = WiFiStaticDetails.WifiGroup?.Owner?.DeviceAddress;
                if (!string.IsNullOrWhiteSpace(liveOwnerMac))
                {
                    Console.WriteLine("ResolveServerIdentity: using the live group owner address");
                    return liveOwnerMac;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ResolveServerIdentity: live group unavailable: " + ex.Message);
            }

            // NO IP FALLBACK. Returning the socket address here looked harmless and was not.
            //
            // The removed fallback carried its own justification: "with no secret under either
            // string the disposition is identical, so the fallback cannot mask a pairing that
            // should have been found." That was TRUE when it was written, because no PSK could
            // exist yet. The moment pairing shipped it inverted. The store is keyed by the group
            // owner's WiFi MAC (docs/MOBILE_AUTH.md 4.4), so looking up by IP does not yield
            // "unpaired", it yields SILENTLY MIS-KEYED: the Companion skips a handshake it should
            // have performed, and the Server, which does hold a secret, rejects the plain traffic
            // that follows. The socket dies, the Companion redials, forever.
            //
            // Observed on both Companions 2026-07-28 as
            // "UNAUTHENTICATED: no pairing secret for peer '192.168.49.1'" with zero handshake
            // attempts. See issue 19.
            //
            // Returning null is the honest answer to "which identity is this?" when we do not yet
            // know. P2pHandshakePolicy treats an unknown peer as unresolvable rather than as
            // unpaired, so the connection is retried instead of being silently downgraded.
            Console.WriteLine("ResolveServerIdentity: group owner address not known yet, "
                + "refusing to key the pairing secret by IP");
            return null;
        }

        /// <param name="isHandshakeFrame">True only for the handshake messages themselves,
        /// which must bypass the gate below because they are what resolves it.</param>
        /// <returns>See <see cref="P2pSendOutcome"/>. A gated send and a failed send are
        /// DIFFERENT, and the caller must not treat them alike: one is a healthy connection
        /// mid-negotiation and the other is a corpse.</returns>
        public async Task<P2pSendOutcome> WriteToSocket(byte[] dataPackage, bool isHandshakeFrame = false)
        {
            // SEND-SIDE HANDSHAKE GATE. The receive side has always dropped application
            // traffic while the handshake is unresolved; the send side did not check at all,
            // so this tier would happily emit frames the peer was guaranteed to discard and
            // report them as sent. That is worse than useless: SocketSender returning true
            // makes the caller believe delivery happened, and on the statement path a false
            // "sent" is how data gets marked Uploaded and deleted.
            //
            // NO LONGER DORMANT, and it bit exactly as predicted. Once pairing shipped, the
            // Companion's own _initalize ping raced BeginHandshake onto this socket, was
            // correctly refused here, and SocketSender then read that refusal as a dead link
            // and closed the socket the handshake was mid-write on. Reporting the refusal as
            // its own outcome rather than as a bare false is what stops that.
            if (!isHandshakeFrame && !_handshake.AllowsApplicationTraffic)
            {
                Console.WriteLine("send refused: handshake is " + _handshake.State +
                    ", application traffic is not permitted yet");
                return P2pSendOutcome.RefusedHandshakePending;
            }

            P2pSendOutcome outcome = P2pSendOutcome.Failed;
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
                outcome = P2pSendOutcome.Sent;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on write to socket: " + ex.Message);
                Console.WriteLine("Error on write to socket: " + ex.StackTrace);
                _socket.Close();
                _socket.Dispose();

                // Reported rather than rethrown. Every caller already has to handle a
                // non-Sent outcome, and throwing here additionally unwound BeginHandshake,
                // which turned a single failed write into an abandoned connection.
                return P2pSendOutcome.Failed;
            }
            finally
            {
                // Released on the failure path too, otherwise one write failure deadlocks
                // every later sender on this connection.
                _writeLock.Release();
            }
            // MP2P-6: removed per-packet GC.Collect + GC.WaitForPendingFinalizers from finally.
            // Forcing a blocking full GC after every socket write freezes managed threads (including
            // the UI) to reclaim a few KB the GC would have collected on its own schedule.
            return outcome;
        }

        /// <summary>
        /// Sends the client hello. MUST be called once after connect and BEFORE the read
        /// loop starts: the responder says nothing until it receives one, so waiting for a
        /// read first would deadlock both sides.
        ///
        /// With no SecretStore registered this is a no-op that marks the handshake skipped,
        /// so the connection proceeds exactly as it did before the handshake was wired.
        /// Returns false when the connection must be abandoned.
        /// </summary>
        public async Task<bool> BeginHandshake()
        {
            try
            {
                P2pHandshakeMessage hello = _handshake.Begin();

                if (_handshake.State == P2pHandshakeState.Failed)
                {
                    Console.WriteLine("handshake refused before connect: " + _handshake.FailureReason);
                    return false;
                }

                if (_handshake.State == P2pHandshakeState.Skipped)
                {
                    Console.WriteLine(P2pHandshakePolicy.UnauthenticatedWarning(_handshake.PeerIdentifier));
                    return true;
                }

                PacketHeaderModel header = new PacketHeaderModel
                {
                    BodyType = hello.BodyType,
                    PacketName = hello.BodyType.ToString(),
                    DeviceUniqueIdentifier = DependencyService.Get<IDevice>()?.GetIdentifier()
                };
                P2pSendOutcome outcome = await WriteToSocket(
                    _frameBuilder.Build(header, hello.Body), isHandshakeFrame: true);

                if (outcome != P2pSendOutcome.Sent)
                {
                    // The hello is what unblocks everything else, so there is no point keeping
                    // a connection whose first message did not reach the wire.
                    Console.WriteLine("BeginHandshake could not send the client hello: " + outcome);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("BeginHandshake failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Routes a frame received while the handshake is unresolved. Returns true when the
        /// caller should dispatch it normally, false when it was consumed or the connection
        /// must be dropped. Mirrors ServerSocketThread.HandleHandshakeFrame.
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
                Console.WriteLine("handshake gate: dropping " + header.BodyType + " received before the handshake resolved");
                return false;
            }

            P2pHandshakeMessage reply = _handshake.Handle(header.BodyType, body, header.DeviceUniqueIdentifier);

            if (_handshake.State == P2pHandshakeState.Failed)
            {
                Console.WriteLine("handshake FAILED: " + _handshake.FailureReason);
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
                    DeviceUniqueIdentifier = DependencyService.Get<IDevice>()?.GetIdentifier()
                };
                P2pSendOutcome outcome = await WriteToSocket(
                    _frameBuilder.Build(replyHeader, reply.Body), isHandshakeFrame: true);

                if (outcome != P2pSendOutcome.Sent)
                {
                    // Logged rather than acted on: WriteToSocket has already torn the socket
                    // down on Failed, and the read loop will observe that on its next pass.
                    Console.WriteLine("handshake reply could not be sent: " + outcome);
                }
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
                //    Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(_socket.InetAddress.ToString().Replace(@"/", ""), dataPackage));

                //}
                //outputStream.Close();
                //outputStream.Dispose();
                #endregion
                #region Length-driven framing (MDM-B2 resolved)
                // Mirrors ServerSocketThread. Frame boundaries come from the v2 length prefix
                // rather than from IsDataAvailable() occupancy, so coalesced reads no longer
                // discard every frame after the first and split reads no longer truncate. The
                // downstream contract is unchanged: ProcessDownloadedData still receives raw
                // framed bytes and WiFiP2pRequestReceiver still calls ParseBytes on them.
                //
                // Returning after one frame is correct, WiFiP2pServer calls this in a loop.
                dataPackage = await _frameParser.ReadFrameBytesAsync(inputStream).ConfigureAwait(false);
                if (dataPackage == null)
                {
                    // Clean end-of-stream ON a frame boundary. Preserves the previous
                    // disconnect behaviour, which kicked discovery off again.
                    inputStream.Close();
                    DrainPendingAcks("clean end-of-stream");
                    EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                    return;
                }

                // HANDSHAKE GATE. Only while the handshake is unresolved do we pay to parse
                // the header here. Once it completes or skips, frames pass straight through
                // as raw bytes exactly as before, so the steady-state path carries no
                // per-frame cost.
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
                await Task.Run(() => EventHandlerHelper._eventHandler.ProcessDownloadedData(peerAddress, framedPacket));
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on read from socket: " + ex.Message);
                //Console.WriteLine("Error on read from socket: " + ex.StackTrace);
                _socket.Close();
                //WiFiP2pServer._clientSocketThread = null;
                //EventHandlerHelper._eventHandler.RestartConnectionDiscovery();
                _socket.Dispose();
                DrainPendingAcks("socket error: " + ex.Message);
            }
            // MP2P-6: removed per-packet GC.Collect + GC.WaitForPendingFinalizers from finally
            // (same reasoning as WriteToSocket above).
        }

        /// <summary>
        /// Resolve every outstanding ack as an explicit failure when the link dies.
        ///
        /// WHY THIS MATTERS MORE THAN IT LOOKS. FebrisP2pAckTracker.FailAll was written for
        /// exactly this and had ZERO callers, so a known-dead socket was left to each pending
        /// registration's 30-second timeout. That is not a cosmetic difference: the timeout arm
        /// in LoopLogic marks the statement Uploaded best-effort and queues it for deletion,
        /// because that arm exists to degrade gracefully against a LEGACY peer that cannot ack.
        /// A socket we watched die is not that condition. Failure_Other routes to the retry
        /// branch instead, so the statement survives to the next poll.
        ///
        /// Also avoids each subsequent statement stalling its own 30 seconds against a link
        /// already known to be gone.
        /// </summary>
        private static void DrainPendingAcks(string reason)
        {
            try
            {
                FebrisP2pAckTracker.Instance.FailAll(AckStatus.Failure_Other);
                Console.WriteLine("Drained pending acks (" + reason + ").");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to drain pending acks: " + ex.Message);
            }
        }
    }
}