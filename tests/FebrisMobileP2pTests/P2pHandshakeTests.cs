// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Linq;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// First execution of the P2P handshake. As of 2026-07-26 this code had ZERO tests
    /// and ZERO callers anywhere in the repo, so nothing had ever run it.
    ///
    /// It is being wired into the connection path, which makes proving it a prerequisite
    /// rather than a nicety: a handshake that silently fails to authenticate looks
    /// exactly like one that works, because both peers still agree on a key.
    ///
    /// The failure cases matter more than the happy path here. Each one corresponds to a
    /// property the class comments claim, and a claim nobody has tested is a guess.
    /// </summary>
    public class P2pHandshakeTests
    {
        private static FebrisP2pPairingSecret Psk(byte fill)
        {
            return FebrisP2pPairingSecret.FromBytes(Enumerable.Repeat(fill, FebrisP2pPairingSecret.KeySizeBytes).ToArray());
        }

        /// <summary>Drives the full three-way exchange and returns both derived keys.</summary>
        private static (byte[] ClientKey, byte[] ServerKey) RunHandshake(
            FebrisP2pPairingSecret clientPsk, FebrisP2pPairingSecret serverPsk)
        {
            var client = new FebrisP2pClientHandshake(clientPsk);
            var server = new FebrisP2pServerHandshake(serverPsk);

            byte[] clientNonce = client.StartHandshake();
            var (serverNonce, serverResponse) = server.ProcessClientHello(clientNonce);
            byte[] clientFinal = client.ProcessServerHello(serverNonce, serverResponse);
            server.ProcessClientFinal(clientFinal);

            return (client.GetSessionKey().KeyBytes, server.GetSessionKey().KeyBytes);
        }

        // ---------- happy path ----------

        [Fact]
        public void MatchingPsk_BothSidesDeriveTheSameSessionKey()
        {
            var (clientKey, serverKey) = RunHandshake(Psk(0x11), Psk(0x11));

            Assert.Equal(FebrisP2pSessionKey.KeySizeBytes, clientKey.Length);
            Assert.Equal(clientKey, serverKey);
            // A key of all zeroes would also "match", so prove it is not degenerate.
            Assert.Contains(clientKey, b => b != 0x00);
        }

        [Fact]
        public void EachHandshake_DerivesADifferentSessionKey()
        {
            // Nonces are per-connection, so the same PSK must not yield the same key
            // twice. If it did, a recorded session would decrypt a later one.
            var (firstKey, _) = RunHandshake(Psk(0x22), Psk(0x22));
            var (secondKey, _) = RunHandshake(Psk(0x22), Psk(0x22));

            Assert.NotEqual(firstKey, secondKey);
        }

        // ---------- the properties the class comments claim ----------

        [Fact]
        public void WrongPskOnServer_ClientRejectsTheServerResponse()
        {
            var client = new FebrisP2pClientHandshake(Psk(0x33));
            var server = new FebrisP2pServerHandshake(Psk(0x44));

            byte[] clientNonce = client.StartHandshake();
            var (serverNonce, serverResponse) = server.ProcessClientHello(clientNonce);

            Assert.Throws<HandshakeAuthenticationFailedException>(
                () => client.ProcessServerHello(serverNonce, serverResponse));
        }

        [Fact]
        public void WrongPskOnClient_ServerRejectsTheClientFinal()
        {
            // The server must not accept a peer that could not verify it. This is the
            // half of mutual authentication that a client-only check would miss.
            var realServer = new FebrisP2pServerHandshake(Psk(0x55));
            var impostor = new FebrisP2pClientHandshake(Psk(0x66));

            byte[] clientNonce = impostor.StartHandshake();
            var (serverNonce, _) = realServer.ProcessClientHello(clientNonce);

            // The impostor cannot verify the real response, so it forges a final under
            // its own wrong PSK, which is what an attacker would actually do. It forges
            // over the correct transcript, so the ONLY thing failing here is the key.
            byte[] forgedFinal = HandshakeInternals.ComputeTaggedHmac(
                Psk(0x66),
                FebrisP2pHandshakeTags.ClientFinalTag,
                HandshakeInternals.BuildTranscript(clientNonce, serverNonce));

            Assert.Throws<HandshakeAuthenticationFailedException>(
                () => realServer.ProcessClientFinal(forgedFinal));
        }

        [Fact]
        public void ServerResponseReplayedAsClientFinal_IsRejected()
        {
            // This is what the domain-separated tags buy. Without them, an attacker who
            // captured a server response could echo it back as the client's final and
            // authenticate without ever holding the PSK.
            var psk = Psk(0x77);
            var client = new FebrisP2pClientHandshake(psk);
            var server = new FebrisP2pServerHandshake(psk);

            byte[] clientNonce = client.StartHandshake();
            var (_, serverResponse) = server.ProcessClientHello(clientNonce);

            Assert.Throws<HandshakeAuthenticationFailedException>(
                () => server.ProcessClientFinal(serverResponse));
        }

        [Fact]
        public void TamperedServerNonce_IsCaughtImmediatelyByTheClient()
        {
            // This test previously pinned the OPPOSITE behaviour, and it was pinning a
            // weakness rather than a design choice.
            //
            // server_response used to be HMAC(PSK, ServerResponseTag || CLIENT_nonce), which
            // did not cover the server's own nonce. The client structurally could not detect
            // a server_nonce altered in flight; it accepted, derived a key from the tampered
            // nonce, and only the responder caught the mismatch a full round trip later. The
            // initiator spent that window believing it held a usable session key.
            //
            // Both MACs now cover client_nonce || server_nonce, so the tamper changes the
            // value the client expects and it aborts on the spot. Detection moved from the
            // wrong side of the connection to the right one.
            var psk = Psk(0x88);
            var client = new FebrisP2pClientHandshake(psk);
            var server = new FebrisP2pServerHandshake(psk);

            byte[] clientNonce = client.StartHandshake();
            var (serverNonce, serverResponse) = server.ProcessClientHello(clientNonce);

            byte[] tamperedNonce = (byte[])serverNonce.Clone();
            tamperedNonce[0] ^= 0xFF;

            Assert.Throws<HandshakeAuthenticationFailedException>(
                () => client.ProcessServerHello(tamperedNonce, serverResponse));
        }

        [Fact]
        public void TamperedServerNonce_LeavesNoUsableSessionKeyOnTheClient()
        {
            // The abort must not leave a half-built state that still yields a key. Before
            // transcript binding the client would happily produce one from the tampered
            // nonce, which is exactly the window this fix closes.
            var psk = Psk(0x89);
            var client = new FebrisP2pClientHandshake(psk);
            var server = new FebrisP2pServerHandshake(psk);

            byte[] clientNonce = client.StartHandshake();
            var (serverNonce, serverResponse) = server.ProcessClientHello(clientNonce);

            byte[] tamperedNonce = (byte[])serverNonce.Clone();
            tamperedNonce[0] ^= 0xFF;

            Assert.Throws<HandshakeAuthenticationFailedException>(
                () => client.ProcessServerHello(tamperedNonce, serverResponse));

            // Aborted, so no key may be handed out at all.
            Assert.Throws<HandshakeStateException>(() => client.GetSessionKey());
        }

        [Fact]
        public void ServerResponse_CoversTheServerNonce_NotJustTheClientNonce()
        {
            // Pins the transcript property directly rather than through its consequences.
            // Two handshakes over the SAME client_nonce must produce different server
            // responses, because the server nonce differs. Under the old construction these
            // were byte-identical, which is what made tampering undetectable.
            var psk = Psk(0x8A);
            var client = new FebrisP2pClientHandshake(psk);
            byte[] clientNonce = client.StartHandshake();

            var (nonceA, responseA) = new FebrisP2pServerHandshake(psk).ProcessClientHello(clientNonce);
            var (nonceB, responseB) = new FebrisP2pServerHandshake(psk).ProcessClientHello(clientNonce);

            Assert.NotEqual(nonceA, nonceB);
            Assert.NotEqual(responseA, responseB);
        }

        [Fact]
        public void BothMacsCoverTheSameTranscript_AndAreSeparatedOnlyByTheirTag()
        {
            // Now that both MACs take identical input, the domain-separation tags are the
            // ONLY thing preventing one from being replayed as the other. That makes them
            // load-bearing, so pin it: swap the tag and nothing else, and the values must
            // still differ.
            var psk = Psk(0x8B);
            byte[] clientNonce = new byte[FebrisP2pHandshakeTags.NonceSizeBytes];
            byte[] serverNonce = new byte[FebrisP2pHandshakeTags.NonceSizeBytes];
            for (int i = 0; i < clientNonce.Length; i++) { clientNonce[i] = (byte)i; serverNonce[i] = (byte)(0x40 + i); }

            byte[] transcript = HandshakeInternals.BuildTranscript(clientNonce, serverNonce);

            byte[] asServer = HandshakeInternals.ComputeTaggedHmac(
                psk, FebrisP2pHandshakeTags.ServerResponseTag, transcript);
            byte[] asClient = HandshakeInternals.ComputeTaggedHmac(
                psk, FebrisP2pHandshakeTags.ClientFinalTag, transcript);

            Assert.NotEqual(asServer, asClient);
        }

        [Fact]
        public void Transcript_IsClientNonceThenServerNonce_AndIsOrderSensitive()
        {
            // Concatenation is only unambiguous because both nonces are fixed-length and
            // every entry point rejects other lengths. Pin the order so a refactor cannot
            // silently swap it and leave both peers agreeing on a different protocol than
            // the spec describes.
            byte[] a = new byte[FebrisP2pHandshakeTags.NonceSizeBytes];
            byte[] b = new byte[FebrisP2pHandshakeTags.NonceSizeBytes];
            for (int i = 0; i < a.Length; i++) { a[i] = 0x11; b[i] = 0x22; }

            byte[] ab = HandshakeInternals.BuildTranscript(a, b);

            Assert.Equal(a.Length + b.Length, ab.Length);
            Assert.Equal(0x11, ab[0]);
            Assert.Equal(0x22, ab[a.Length]);
            Assert.NotEqual(ab, HandshakeInternals.BuildTranscript(b, a));
        }

        [Fact]
        public void TamperedServerResponse_IsRejected()
        {
            var psk = Psk(0x99);
            var client = new FebrisP2pClientHandshake(psk);
            var server = new FebrisP2pServerHandshake(psk);

            byte[] clientNonce = client.StartHandshake();
            var (serverNonce, serverResponse) = server.ProcessClientHello(clientNonce);
            serverResponse[serverResponse.Length - 1] ^= 0x01; // single bit

            Assert.Throws<HandshakeAuthenticationFailedException>(
                () => client.ProcessServerHello(serverNonce, serverResponse));
        }

        [Fact]
        public void TamperedClientFinal_IsRejected()
        {
            var psk = Psk(0xAA);
            var client = new FebrisP2pClientHandshake(psk);
            var server = new FebrisP2pServerHandshake(psk);

            byte[] clientNonce = client.StartHandshake();
            var (serverNonce, serverResponse) = server.ProcessClientHello(clientNonce);
            byte[] clientFinal = client.ProcessServerHello(serverNonce, serverResponse);
            clientFinal[0] ^= 0x01;

            Assert.Throws<HandshakeAuthenticationFailedException>(
                () => server.ProcessClientFinal(clientFinal));
        }

        [Fact]
        public void MitmSubstitutingItsOwnNonce_CannotProduceTheSameSessionKey()
        {
            // The session key is salted with BOTH nonces, so an attacker who relays a
            // different nonce desynchronises the two keys even if the HMACs were to pass.
            var psk = Psk(0xBB);
            byte[] clientNonce = new FebrisP2pClientHandshake(psk).StartHandshake();

            byte[] honestServerNonce = HandshakeInternals.GenerateNonce();
            byte[] injectedServerNonce = HandshakeInternals.GenerateNonce();

            byte[] honest = HandshakeInternals.DeriveSessionKey(psk, clientNonce, honestServerNonce).KeyBytes;
            byte[] injected = HandshakeInternals.DeriveSessionKey(psk, clientNonce, injectedServerNonce).KeyBytes;

            Assert.NotEqual(honest, injected);
        }

        [Fact]
        public void SessionKey_IsUnavailableBeforeTheHandshakeCompletes()
        {
            // Handing out a key mid-handshake would let traffic flow before the peer had
            // been authenticated, which is the whole point of running one.
            var psk = Psk(0xCC);
            var client = new FebrisP2pClientHandshake(psk);
            var server = new FebrisP2pServerHandshake(psk);

            Assert.ThrowsAny<Exception>(() => client.GetSessionKey());
            Assert.ThrowsAny<Exception>(() => server.GetSessionKey());

            byte[] clientNonce = client.StartHandshake();
            server.ProcessClientHello(clientNonce);

            // The server has answered but has NOT yet verified the client.
            Assert.ThrowsAny<Exception>(() => server.GetSessionKey());
        }

        // ---------- HKDF conformance ----------

        [Fact]
        public void HkdfSha256_MatchesRfc5869TestCase1()
        {
            // A KNOWN-ANSWER test, not a round-trip. A subtly wrong HKDF still produces a
            // key both peers agree on, so the handshake would pass every test above while
            // being non-conformant and non-interoperable. Only a published vector catches
            // that. RFC 5869 Appendix A.1, the SHA-256 basic test case.
            byte[] ikm = Enumerable.Repeat((byte)0x0b, 22).ToArray();
            byte[] salt = Enumerable.Range(0, 13).Select(i => (byte)i).ToArray();          // 0x00..0x0c
            byte[] info = Enumerable.Range(0xf0, 10).Select(i => (byte)i).ToArray();       // 0xf0..0xf9

            byte[] okm = HkdfSha256.Derive(ikm, salt, info, 42);

            const string expected =
                "3cb25f25faacd57a90434f64d0362f2a" +
                "2d2d0a90cf1a5a4c5db02d56ecc4c5bf" +
                "34007208d5b887185865";

            Assert.Equal(expected, BitConverter.ToString(okm).Replace("-", "").ToLowerInvariant());
        }

        // ---------- the pre-shared key itself ----------

        [Fact]
        public void PairingSecret_RoundTripsThroughBase64()
        {
            FebrisP2pPairingSecret original = FebrisP2pPairingSecret.Generate();
            FebrisP2pPairingSecret restored = FebrisP2pPairingSecret.FromBase64String(original.ToBase64String());

            Assert.True(original.Equals(restored));
            Assert.Equal(original.Fingerprint, restored.Fingerprint);

            // And a restored PSK must actually work on the wire, not merely compare equal.
            var (clientKey, serverKey) = RunHandshake(original, restored);
            Assert.Equal(clientKey, serverKey);
        }

        [Fact]
        public void PairingSecret_Generate_IsNotDeterministic()
        {
            Assert.NotEqual(
                FebrisP2pPairingSecret.Generate().ToBase64String(),
                FebrisP2pPairingSecret.Generate().ToBase64String());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(16)]
        [InlineData(31)]
        [InlineData(33)]
        [InlineData(64)]
        public void PairingSecret_RejectsWrongKeySize(int size)
        {
            Assert.ThrowsAny<Exception>(() => FebrisP2pPairingSecret.FromBytes(new byte[size]));
        }

        [Fact]
        public void DifferentPairingSecrets_HaveDifferentFingerprints()
        {
            Assert.NotEqual(Psk(0x01).Fingerprint, Psk(0x02).Fingerprint);
        }

        // ---------- the transitional handshake policy ----------

        private sealed class FakeStore : Febris.SharedMobileLibrary.P2pNetworking.IP2pPairingSecretStore
        {
            private readonly string _pairedPeer;
            private readonly FebrisP2pPairingSecret _secret;

            public FakeStore(string pairedPeer, FebrisP2pPairingSecret secret)
            {
                _pairedPeer = pairedPeer;
                _secret = secret;
            }

            public bool TryGetSecret(string peerIdentifier, out FebrisP2pPairingSecret secret)
            {
                if (peerIdentifier == _pairedPeer) { secret = _secret; return true; }
                secret = null;
                return false;
            }
        }

        [Fact]
        public void Policy_PairedPeer_PerformsTheHandshake()
        {
            var store = new FakeStore("device-a", Psk(0xDD));

            var disposition = Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy
                .Evaluate(store, "device-a", out FebrisP2pPairingSecret secret);

            Assert.Equal(Febris.SharedMobileLibrary.P2pNetworking.HandshakeDisposition.Perform, disposition);
            Assert.NotNull(secret);
        }

        [Fact]
        public void Policy_UnpairedPeer_SkipsWhileTransitional_AndRefusesWhenRequired()
        {
            // Both halves in one test so the flag is always restored, and so the
            // transitional escape hatch is visibly paired with the thing that closes it.
            var store = new FakeStore("device-a", Psk(0xEE));
            bool original = Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy.RequireHandshake;
            try
            {
                Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy.RequireHandshake = false;
                Assert.Equal(
                    Febris.SharedMobileLibrary.P2pNetworking.HandshakeDisposition.SkipUnauthenticated,
                    Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy.Evaluate(store, "stranger", out _));

                Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy.RequireHandshake = true;
                Assert.Equal(
                    Febris.SharedMobileLibrary.P2pNetworking.HandshakeDisposition.Refuse,
                    Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy.Evaluate(store, "stranger", out _));
            }
            finally
            {
                Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy.RequireHandshake = original;
            }
        }

        [Fact]
        public void Policy_NullStore_IsTreatedAsNoSecret_NotAsPaired()
        {
            // A head that has not implemented storage yet must never be mistaken for a
            // paired peer. Fail toward "no secret", not toward "authenticated".
            Assert.Equal(
                Febris.SharedMobileLibrary.P2pNetworking.HandshakeDisposition.SkipUnauthenticated,
                Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy.Evaluate(null, "device-a", out FebrisP2pPairingSecret secret));
            Assert.Null(secret);
        }

        [Fact]
        public void Policy_DefaultsToNotRequiringAHandshake_AndThatIsDeliberate()
        {
            // Pins the transitional default so flipping it is a conscious, reviewed act
            // rather than something that drifts. When pairing ships, this test changes
            // in the same commit that changes the default.
            Assert.False(Febris.SharedMobileLibrary.P2pNetworking.P2pHandshakePolicy.RequireHandshake);
        }
    }
}
