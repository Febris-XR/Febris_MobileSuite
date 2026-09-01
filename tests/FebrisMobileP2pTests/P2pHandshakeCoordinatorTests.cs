// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Linq;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// Drives two coordinators against each other, which is the closest thing to a real
    /// connection that can run without Android sockets. The socket pumps cannot be unit
    /// tested, so everything stateful lives here where it can be.
    /// </summary>
    public class P2pHandshakeCoordinatorTests
    {
        private const string CompanionId = "companion-aaaa";

        private static FebrisP2pPairingSecret Psk(byte fill)
        {
            return FebrisP2pPairingSecret.FromBytes(
                Enumerable.Repeat(fill, FebrisP2pPairingSecret.KeySizeBytes).ToArray());
        }

        private sealed class Store : IP2pPairingSecretStore
        {
            private readonly string _peer;
            private readonly FebrisP2pPairingSecret _secret;
            public Store(string peer, FebrisP2pPairingSecret secret) { _peer = peer; _secret = secret; }
            public bool TryGetSecret(string peerIdentifier, out FebrisP2pPairingSecret secret)
            {
                if (peerIdentifier == _peer) { secret = _secret; return true; }
                secret = null;
                return false;
            }
        }

        /// <summary>Empty store, standing in for a device that has never been paired.</summary>
        private sealed class EmptyStore : IP2pPairingSecretStore
        {
            public bool TryGetSecret(string peerIdentifier, out FebrisP2pPairingSecret secret)
            {
                secret = null;
                return false;
            }
        }

        [Fact]
        public void BothPaired_ExchangeCompletes_AndKeysAgree()
        {
            var psk = Psk(0x42);
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, psk), CompanionId);
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, psk));

            P2pHandshakeMessage hello = initiator.Begin();
            Assert.Equal(BodyType._handshakeClientHello, hello.BodyType);
            Assert.Equal(P2pHandshakeState.AwaitingServerHello, initiator.State);

            P2pHandshakeMessage serverHello = responder.Handle(hello.BodyType, hello.Body, CompanionId);
            Assert.Equal(BodyType._handshakeServerHello, serverHello.BodyType);
            Assert.Equal(P2pHandshakeState.AwaitingClientFinal, responder.State);

            P2pHandshakeMessage clientFinal = initiator.Handle(serverHello.BodyType, serverHello.Body, null);
            Assert.Equal(BodyType._handshakeClientFinal, clientFinal.BodyType);

            Assert.Null(responder.Handle(clientFinal.BodyType, clientFinal.Body, CompanionId));

            Assert.Equal(P2pHandshakeState.Complete, initiator.State);
            Assert.Equal(P2pHandshakeState.Complete, responder.State);
            Assert.Equal(initiator.SessionKey.KeyBytes, responder.SessionKey.KeyBytes);
            Assert.True(initiator.AllowsApplicationTraffic);
            Assert.True(responder.AllowsApplicationTraffic);

            // The responder learns who connected from the frame it was handed.
            Assert.Equal(CompanionId, responder.PeerIdentifier);
        }

        [Fact]
        public void PeerIsProven_IsTrueOnBothSidesOnceComplete()
        {
            // This test used to assert the initiator was Complete but NOT proven, and that
            // asymmetry was a real weakness rather than a design choice: server_response
            // covered the CLIENT nonce only, so a tampered server nonce was invisible to the
            // initiator and was caught a round trip later by the responder.
            //
            // Both MACs now cover client_nonce || server_nonce, so the initiator detects
            // tampering itself and the special case is gone. Worth pinning explicitly,
            // because a stale `Assert.False` here would have stayed green while silently
            // re-pinning a weakness the code no longer has.
            var psk = Psk(0x43);
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, psk), CompanionId);
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, psk));

            var hello = initiator.Begin();
            var serverHello = responder.Handle(hello.BodyType, hello.Body, CompanionId);
            var clientFinal = initiator.Handle(serverHello.BodyType, serverHello.Body, null);
            responder.Handle(clientFinal.BodyType, clientFinal.Body, CompanionId);

            Assert.Equal(P2pHandshakeState.Complete, initiator.State);
            Assert.True(initiator.PeerIsProven);
            Assert.True(responder.PeerIsProven);
        }

        [Fact]
        public void PeerIsProven_IsFalseBeforeTheExchangeCompletes()
        {
            // Proven must track Complete exactly, not "we started and nothing blew up".
            // The initiator is mid-exchange here: it has sent its hello and has no evidence
            // about the peer at all.
            var psk = Psk(0x44);
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, psk), CompanionId);

            initiator.Begin();

            Assert.Equal(P2pHandshakeState.AwaitingServerHello, initiator.State);
            Assert.False(initiator.PeerIsProven);
        }

        [Fact]
        public void MismatchedPsk_ResponderRejectsAndDoesNotIssueAKey()
        {
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, Psk(0x01)), CompanionId);
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, Psk(0x02)));

            var hello = initiator.Begin();
            var serverHello = responder.Handle(hello.BodyType, hello.Body, CompanionId);

            // The initiator rejects the response it cannot verify.
            Assert.Null(initiator.Handle(serverHello.BodyType, serverHello.Body, null));
            Assert.Equal(P2pHandshakeState.Failed, initiator.State);
            Assert.False(initiator.AllowsApplicationTraffic);
            Assert.Null(initiator.SessionKey);
            Assert.NotNull(initiator.FailureReason);
        }

        [Fact]
        public void PeerOffersHandshakeButResponderHasNoSecret_IsRefusedNotDowngraded()
        {
            // The important one. A peer that asked to authenticate must never be quietly
            // handed an unauthenticated session, even while RequireHandshake is false.
            // Silent downgrade is how an attacker turns a transitional flag into a bypass.
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, Psk(0x09)), CompanionId);
            var responder = P2pHandshakeCoordinator.CreateResponder(new EmptyStore());

            var hello = initiator.Begin();
            Assert.NotNull(hello);

            Assert.Null(responder.Handle(hello.BodyType, hello.Body, CompanionId));
            Assert.Equal(P2pHandshakeState.Failed, responder.State);
            Assert.False(responder.AllowsApplicationTraffic);
        }

        [Fact]
        public void NoSecretAnywhere_SkipsUnauthenticated_WhileTransitional()
        {
            bool original = P2pHandshakePolicy.RequireHandshake;
            try
            {
                P2pHandshakePolicy.RequireHandshake = false;
                var initiator = P2pHandshakeCoordinator.CreateInitiator(new EmptyStore(), CompanionId);

                Assert.Null(initiator.Begin());
                Assert.Equal(P2pHandshakeState.Skipped, initiator.State);
                Assert.True(initiator.AllowsApplicationTraffic);  // transitional, by design
                Assert.False(initiator.PeerIsProven);             // but never "proven"
                Assert.Null(initiator.SessionKey);
            }
            finally { P2pHandshakePolicy.RequireHandshake = original; }
        }

        [Fact]
        public void NoSecretAnywhere_RefusesOnceRequireHandshakeIsSet()
        {
            bool original = P2pHandshakePolicy.RequireHandshake;
            try
            {
                P2pHandshakePolicy.RequireHandshake = true;
                var initiator = P2pHandshakeCoordinator.CreateInitiator(new EmptyStore(), CompanionId);

                Assert.Null(initiator.Begin());
                Assert.Equal(P2pHandshakeState.Failed, initiator.State);
                Assert.False(initiator.AllowsApplicationTraffic);
            }
            finally { P2pHandshakePolicy.RequireHandshake = original; }
        }

        [Fact]
        public void ApplicationTrafficIsNotAllowedMidHandshake()
        {
            // The window that matters: between hello and completion, nothing may flow.
            var psk = Psk(0x44);
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, psk), CompanionId);
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, psk));

            Assert.False(initiator.AllowsApplicationTraffic);   // before Begin
            var hello = initiator.Begin();
            Assert.False(initiator.AllowsApplicationTraffic);   // awaiting server hello

            responder.Handle(hello.BodyType, hello.Body, CompanionId);
            Assert.False(responder.AllowsApplicationTraffic);   // awaiting client final
        }

        [Theory]
        [InlineData(BodyType._statement)]
        [InlineData(BodyType._videoFrame)]
        [InlineData(BodyType._installModule)]
        public void NonHandshakeFrameFedToTheCoordinator_Fails(BodyType bodyType)
        {
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, Psk(0x45)));

            Assert.Null(responder.Handle(bodyType, new byte[] { 1, 2, 3 }, CompanionId));
            Assert.Equal(P2pHandshakeState.Failed, responder.State);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(47)]   // one short of nonce + hmac
        [InlineData(49)]   // one long
        public void MalformedServerHello_FailsWithoutThrowing(int length)
        {
            // A pump must be able to close the socket, never unwind through an exception.
            var psk = Psk(0x46);
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, psk), CompanionId);
            initiator.Begin();

            Assert.Null(initiator.Handle(BodyType._handshakeServerHello, new byte[length], null));
            Assert.Equal(P2pHandshakeState.Failed, initiator.State);
            Assert.NotNull(initiator.FailureReason);
        }

        [Fact]
        public void NullBody_FailsWithoutThrowing()
        {
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, Psk(0x47)), CompanionId);
            initiator.Begin();

            Assert.Null(initiator.Handle(BodyType._handshakeServerHello, null, null));
            Assert.Equal(P2pHandshakeState.Failed, initiator.State);
        }

        [Fact]
        public void OutOfOrderFrames_AreRejected()
        {
            var psk = Psk(0x48);
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, psk));

            // A client final before any hello must not be accepted.
            Assert.Null(responder.Handle(BodyType._handshakeClientFinal, new byte[32], CompanionId));
            Assert.Equal(P2pHandshakeState.Failed, responder.State);
        }

        [Fact]
        public void RolesAreEnforced()
        {
            var psk = Psk(0x49);
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, psk), CompanionId);
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, psk));

            // The responder never calls Begin, it starts on the first received frame.
            Assert.Throws<InvalidOperationException>(() => responder.Begin());

            // And an initiator must not be fed a client hello.
            initiator.Begin();
            Assert.Null(initiator.Handle(BodyType._handshakeClientHello, new byte[16], CompanionId));
            Assert.Equal(P2pHandshakeState.Failed, initiator.State);
        }

        [Fact]
        public void BeginTwice_IsRejected()
        {
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, Psk(0x4A)), CompanionId);
            initiator.Begin();
            Assert.Throws<InvalidOperationException>(() => initiator.Begin());
        }

        [Fact]
        public void Responder_PeerNeverHandshakes_AndNeitherSideHasASecret_Skips()
        {
            // Regression for a bug caught while wiring the pumps. The responder starts at
            // NotStarted where AllowsApplicationTraffic is FALSE, so without an explicit
            // resolve it would drop every frame forever whenever the peer does not
            // handshake, which is the normal case until pairing ships. That would have
            // broken the Server outright.
            var responder = P2pHandshakeCoordinator.CreateResponder(new EmptyStore());
            Assert.False(responder.AllowsApplicationTraffic);   // the trap

            Assert.True(responder.ResolveUnauthenticatedPeer(CompanionId));
            Assert.Equal(P2pHandshakeState.Skipped, responder.State);
            Assert.True(responder.AllowsApplicationTraffic);
            Assert.False(responder.PeerIsProven);
        }

        [Fact]
        public void Responder_HoldsASecretButPeerSkipsTheHandshake_IsRefused()
        {
            // The important half. If this side holds a secret, a peer that sends traffic
            // without authenticating must be refused. Accepting it would let an attacker
            // opt out of authentication simply by staying silent.
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, Psk(0x50)));

            Assert.False(responder.ResolveUnauthenticatedPeer(CompanionId));
            Assert.Equal(P2pHandshakeState.Failed, responder.State);
            Assert.False(responder.AllowsApplicationTraffic);
            Assert.NotNull(responder.FailureReason);
        }

        [Fact]
        public void Responder_ResolveUnauthenticated_RefusesWhenRequireHandshakeIsSet()
        {
            bool original = P2pHandshakePolicy.RequireHandshake;
            try
            {
                P2pHandshakePolicy.RequireHandshake = true;
                var responder = P2pHandshakeCoordinator.CreateResponder(new EmptyStore());

                Assert.False(responder.ResolveUnauthenticatedPeer(CompanionId));
                Assert.Equal(P2pHandshakeState.Failed, responder.State);
            }
            finally { P2pHandshakePolicy.RequireHandshake = original; }
        }

        [Fact]
        public void Responder_ResolveUnauthenticated_DoesNotDowngradeACompletedHandshake()
        {
            // Once authenticated, a later stray call must not reopen anything.
            var psk = Psk(0x51);
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, psk), CompanionId);
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, psk));

            var hello = initiator.Begin();
            var serverHello = responder.Handle(hello.BodyType, hello.Body, CompanionId);
            var clientFinal = initiator.Handle(serverHello.BodyType, serverHello.Body, null);
            responder.Handle(clientFinal.BodyType, clientFinal.Body, CompanionId);

            Assert.True(responder.ResolveUnauthenticatedPeer(CompanionId));
            Assert.Equal(P2pHandshakeState.Complete, responder.State);
            Assert.True(responder.PeerIsProven);
        }

        [Fact]
        public void ResolveUnauthenticatedPeer_IsResponderOnly()
        {
            var initiator = P2pHandshakeCoordinator.CreateInitiator(new EmptyStore(), CompanionId);
            Assert.Throws<InvalidOperationException>(() => initiator.ResolveUnauthenticatedPeer(CompanionId));
        }

        [Fact]
        public void ImpostorClaimingAPairedIdentity_FailsTheHandshake()
        {
            // The peer identifier is self-asserted, so the responder looks up a PSK by the
            // CLAIM. That is sound because the claim only selects a candidate: an impostor
            // that does not hold the secret cannot produce a valid final.
            var responder = P2pHandshakeCoordinator.CreateResponder(new Store(CompanionId, Psk(0x4B)));
            var impostor = P2pHandshakeCoordinator.CreateInitiator(new Store(CompanionId, Psk(0xFF)), CompanionId);

            var hello = impostor.Begin();
            var serverHello = responder.Handle(hello.BodyType, hello.Body, CompanionId);

            // The impostor cannot verify the genuine response.
            Assert.Null(impostor.Handle(serverHello.BodyType, serverHello.Body, null));
            Assert.Equal(P2pHandshakeState.Failed, impostor.State);

            // And the responder never reaches a key.
            Assert.NotEqual(P2pHandshakeState.Complete, responder.State);
            Assert.Null(responder.SessionKey);
        }
    }
}
