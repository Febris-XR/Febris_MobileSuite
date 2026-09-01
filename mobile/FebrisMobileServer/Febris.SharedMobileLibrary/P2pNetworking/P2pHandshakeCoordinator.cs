// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>A handshake frame to put on the wire, or null when there is nothing to send.</summary>
    public sealed class P2pHandshakeMessage
    {
        public BodyType BodyType { get; }
        public byte[] Body { get; }

        internal P2pHandshakeMessage(BodyType bodyType, byte[] body)
        {
            BodyType = bodyType;
            Body = body;
        }
    }

    public enum P2pHandshakeState
    {
        NotStarted,
        /// <summary>Initiator sent the client hello and is waiting for the server hello.</summary>
        AwaitingServerHello,
        /// <summary>Responder answered the client hello and is waiting for the client final.</summary>
        AwaitingClientFinal,
        /// <summary>The exchange finished on this side and the peer proved PSK possession.
        /// On the initiator this still does not confirm the responder ACCEPTED the client
        /// final, because the third message is unacknowledged. See
        /// <see cref="P2pHandshakeCoordinator.PeerIsProven"/>.</summary>
        Complete,
        /// <summary>No pairing secret and the deployment does not require one. Unauthenticated.</summary>
        Skipped,
        /// <summary>Authentication failed, or the connection was refused. Drop the socket.</summary>
        Failed
    }

    /// <summary>
    /// Drives <see cref="FebrisP2pClientHandshake"/> / <see cref="FebrisP2pServerHandshake"/> over the
    /// frame transport, so the socket pumps only have to send what they are handed and feed back what
    /// they receive.
    ///
    /// This exists as a separate unit on purpose: connection establishment is the worst place to debug
    /// a state machine, and the pumps cannot be unit tested (they need real Android sockets). Everything
    /// tricky lives here, where two coordinators can be run against each other in a test.
    ///
    /// ROLE MAPPING. The Companion dials out, so it is the initiator / handshake client. The Server
    /// accepts, so it is the responder. This matches FebrisP2pHandshake's own client and server naming.
    ///
    /// PEER IDENTITY. The responder does not know who connected until the first frame arrives, so the
    /// PSK is looked up by the identifier the peer CLAIMS in that frame header. That is sound rather
    /// than circular: a liar looks up someone else's secret and then fails the HMAC. The claim selects
    /// a candidate, the handshake decides whether it was true.
    /// </summary>
    public sealed class P2pHandshakeCoordinator
    {
        private readonly bool _isInitiator;
        private readonly IP2pPairingSecretStore _store;

        private FebrisP2pClientHandshake _client;
        private FebrisP2pServerHandshake _server;
        private FebrisP2pSessionKey _sessionKey;

        private P2pHandshakeCoordinator(bool isInitiator, IP2pPairingSecretStore store, string peerIdentifier)
        {
            _isInitiator = isInitiator;
            _store = store;
            PeerIdentifier = peerIdentifier;
            State = P2pHandshakeState.NotStarted;
        }

        /// <summary>The Companion side, which dials out and knows the peer up front.</summary>
        public static P2pHandshakeCoordinator CreateInitiator(IP2pPairingSecretStore store, string peerIdentifier)
        {
            return new P2pHandshakeCoordinator(true, store, peerIdentifier);
        }

        /// <summary>The Server side, which learns the peer from the first frame.</summary>
        public static P2pHandshakeCoordinator CreateResponder(IP2pPairingSecretStore store)
        {
            return new P2pHandshakeCoordinator(false, store, null);
        }

        public P2pHandshakeState State { get; private set; }
        public string PeerIdentifier { get; private set; }
        public string FailureReason { get; private set; }

        /// <summary>The derived key, or null until the exchange completes.</summary>
        public FebrisP2pSessionKey SessionKey { get { return _sessionKey; } }

        /// <summary>
        /// True once this side has verified that the peer holds the same PSK. Symmetric
        /// across both roles.
        ///
        /// THIS USED TO BE FALSE ON THE INITIATOR, and that was not a policy choice: the
        /// server response covered only the CLIENT's nonce, so a tampered server nonce was
        /// structurally undetectable to the initiator and surfaced a round trip later on the
        /// responder. Transcript binding removed that asymmetry, so the special case is gone
        /// rather than merely relaxed. See FebrisP2pHandshake's protocol docblock.
        ///
        /// WHAT IT STILL DOES NOT MEAN. The third message is unacknowledged, so an initiator
        /// at <see cref="P2pHandshakeState.Complete"/> knows the peer proved PSK possession
        /// but not that the peer accepted its own client_final. Proven is a statement about
        /// the peer's identity, not about session establishment. Do not use it as a
        /// "the connection is up" signal.
        /// </summary>
        public bool PeerIsProven
        {
            get { return State == P2pHandshakeState.Complete; }
        }

        /// <summary>Whether ordinary application frames may flow. True once the exchange completes,
        /// and also when it was skipped because no secret exists and the deployment does not yet
        /// require one.</summary>
        public bool AllowsApplicationTraffic
        {
            get { return State == P2pHandshakeState.Complete || State == P2pHandshakeState.Skipped; }
        }

        /// <summary>
        /// Initiator only. Call once on connect. Returns the client hello to send, or null when the
        /// handshake is skipped or refused, in which case check <see cref="State"/>.
        /// </summary>
        public P2pHandshakeMessage Begin()
        {
            if (!_isInitiator)
            {
                throw new InvalidOperationException("Begin() is for the initiator. The responder starts on the first received frame.");
            }
            if (State != P2pHandshakeState.NotStarted)
            {
                throw new InvalidOperationException("Begin() already called; state is " + State + ".");
            }

            FebrisP2pPairingSecret psk;
            HandshakeDisposition disposition = P2pHandshakePolicy.Evaluate(_store, PeerIdentifier, out psk);

            if (disposition == HandshakeDisposition.Refuse)
            {
                State = P2pHandshakeState.Failed;
                FailureReason = "no pairing secret for '" + PeerIdentifier + "' and RequireHandshake is set";
                return null;
            }
            if (disposition == HandshakeDisposition.SkipUnauthenticated)
            {
                State = P2pHandshakeState.Skipped;
                return null;
            }

            _client = new FebrisP2pClientHandshake(psk);
            byte[] clientNonce = _client.StartHandshake();
            State = P2pHandshakeState.AwaitingServerHello;
            return new P2pHandshakeMessage(BodyType._handshakeClientHello, clientNonce);
        }

        /// <summary>
        /// RESPONDER only. Call when the first frame received is NOT a handshake message, which
        /// means the peer never offered one.
        ///
        /// This exists because the responder starts at <see cref="P2pHandshakeState.NotStarted"/>,
        /// where <see cref="AllowsApplicationTraffic"/> is false. Without this the responder would
        /// sit dropping every frame forever whenever the peer does not handshake, which is the
        /// normal case until pairing ships.
        ///
        /// Returns true when traffic may proceed. The two outcomes:
        ///   - no secret held for this peer, so NEITHER side had one, skip unauthenticated
        ///   - a secret IS held, so the peer should have authenticated and did not, refuse
        /// The second is the important half: holding a secret and accepting a peer that ignored
        /// it would let an attacker opt out of authentication simply by staying silent.
        /// </summary>
        public bool ResolveUnauthenticatedPeer(string claimedPeerIdentifier)
        {
            if (_isInitiator)
            {
                throw new InvalidOperationException("ResolveUnauthenticatedPeer is for the responder.");
            }
            if (State != P2pHandshakeState.NotStarted)
            {
                return AllowsApplicationTraffic;
            }

            PeerIdentifier = claimedPeerIdentifier;

            FebrisP2pPairingSecret psk;
            HandshakeDisposition disposition = P2pHandshakePolicy.Evaluate(_store, claimedPeerIdentifier, out psk);

            if (disposition == HandshakeDisposition.Perform)
            {
                Fail("a pairing secret is held for '" + claimedPeerIdentifier +
                     "' but the peer sent application traffic without handshaking");
                return false;
            }
            if (disposition == HandshakeDisposition.Refuse)
            {
                Fail("no pairing secret for '" + claimedPeerIdentifier + "' and RequireHandshake is set");
                return false;
            }

            State = P2pHandshakeState.Skipped;
            return true;
        }

        /// <summary>
        /// Feed a received handshake frame. Returns the reply to send, or null when there is nothing
        /// to send back. Any authentication failure moves to <see cref="P2pHandshakeState.Failed"/>
        /// rather than throwing, because a pump must close the socket, not unwind.
        /// </summary>
        public P2pHandshakeMessage Handle(BodyType bodyType, byte[] body, string claimedPeerIdentifier)
        {
            try
            {
                switch (bodyType)
                {
                    case BodyType._handshakeClientHello:
                        return HandleClientHello(body, claimedPeerIdentifier);
                    case BodyType._handshakeServerHello:
                        return HandleServerHello(body);
                    case BodyType._handshakeClientFinal:
                        return HandleClientFinal(body);
                    default:
                        return Fail("frame " + bodyType + " is not a handshake message");
                }
            }
            catch (FebrisP2pHandshakeException ex)
            {
                return Fail(ex.Message);
            }
            catch (Exception ex)
            {
                // A malformed body must drop the connection, never crash the pump.
                return Fail("handshake error: " + ex.Message);
            }
        }

        private P2pHandshakeMessage HandleClientHello(byte[] body, string claimedPeerIdentifier)
        {
            if (_isInitiator) return Fail("initiator received a client hello");
            if (State != P2pHandshakeState.NotStarted) return Fail("unexpected client hello in state " + State);

            PeerIdentifier = claimedPeerIdentifier;

            FebrisP2pPairingSecret psk;
            HandshakeDisposition disposition = P2pHandshakePolicy.Evaluate(_store, claimedPeerIdentifier, out psk);

            if (disposition != HandshakeDisposition.Perform)
            {
                // The peer asked to authenticate and this side cannot. Refuse rather than
                // silently downgrading: a peer that offered a handshake must never be
                // handed an unauthenticated session it did not ask for.
                return Fail("peer offered a handshake but no pairing secret is held for '" + claimedPeerIdentifier + "'");
            }

            _server = new FebrisP2pServerHandshake(psk);
            var answer = _server.ProcessClientHello(body);

            byte[] payload = new byte[answer.ServerNonce.Length + answer.ServerResponse.Length];
            Buffer.BlockCopy(answer.ServerNonce, 0, payload, 0, answer.ServerNonce.Length);
            Buffer.BlockCopy(answer.ServerResponse, 0, payload, answer.ServerNonce.Length, answer.ServerResponse.Length);

            State = P2pHandshakeState.AwaitingClientFinal;
            return new P2pHandshakeMessage(BodyType._handshakeServerHello, payload);
        }

        private P2pHandshakeMessage HandleServerHello(byte[] body)
        {
            if (!_isInitiator) return Fail("responder received a server hello");
            if (State != P2pHandshakeState.AwaitingServerHello) return Fail("unexpected server hello in state " + State);

            int nonceSize = FebrisP2pHandshakeTags.NonceSizeBytes;
            int hmacSize = FebrisP2pHandshakeTags.HmacSizeBytes;
            if (body == null || body.Length != nonceSize + hmacSize)
            {
                return Fail("server hello must be " + (nonceSize + hmacSize) + " bytes, got " +
                    (body == null ? 0 : body.Length));
            }

            byte[] serverNonce = new byte[nonceSize];
            byte[] serverResponse = new byte[hmacSize];
            Buffer.BlockCopy(body, 0, serverNonce, 0, nonceSize);
            Buffer.BlockCopy(body, nonceSize, serverResponse, 0, hmacSize);

            byte[] clientFinal = _client.ProcessServerHello(serverNonce, serverResponse);
            _sessionKey = _client.GetSessionKey();
            State = P2pHandshakeState.Complete;
            return new P2pHandshakeMessage(BodyType._handshakeClientFinal, clientFinal);
        }

        private P2pHandshakeMessage HandleClientFinal(byte[] body)
        {
            if (_isInitiator) return Fail("initiator received a client final");
            if (State != P2pHandshakeState.AwaitingClientFinal) return Fail("unexpected client final in state " + State);

            _server.ProcessClientFinal(body);
            _sessionKey = _server.GetSessionKey();
            State = P2pHandshakeState.Complete;
            return null;
        }

        private P2pHandshakeMessage Fail(string reason)
        {
            State = P2pHandshakeState.Failed;
            FailureReason = reason;
            _sessionKey = null;
            return null;
        }
    }
}
