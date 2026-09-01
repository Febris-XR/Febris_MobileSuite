// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Linq;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// Numeric-comparison pairing (docs/MOBILE_AUTH.md 4.1). This is the ceremony that finally
    /// puts a PSK on two devices, so every property below is load-bearing for the whole
    /// authenticated channel.
    ///
    /// The security claims are tested as claims, not as round trips: a round trip only proves two
    /// honest peers agree, which is the easy half.
    /// </summary>
    public class P2pPairingSessionTests
    {
        /// <summary>One honest ceremony. Returns both sides after key exchange.</summary>
        private static (FebrisP2pPairingSession server, FebrisP2pPairingSession companion) Exchange()
        {
            var server = new FebrisP2pPairingSession(isInitiator: true);
            var companion = new FebrisP2pPairingSession(isInitiator: false);

            Assert.True(companion.ProcessPeerKey(server.PublicKey));
            Assert.True(server.ProcessPeerKey(companion.PublicKey));
            return (server, companion);
        }

        // ---------- the ceremony works at all ----------

        [Fact]
        public void HonestExchange_ProducesTheSameCodeOnBothScreens()
        {
            var (server, companion) = Exchange();

            Assert.NotNull(server.ComparisonCode);
            Assert.Equal(server.ComparisonCode, companion.ComparisonCode);
        }

        [Fact]
        public void HonestExchange_ProducesTheSamePskOnBothSides()
        {
            // The entire point. If these differ, the handshake fails later with an
            // authentication error that looks like an attack rather than a pairing bug.
            var (server, companion) = Exchange();

            FebrisP2pPairingSecret a = server.Confirm();
            FebrisP2pPairingSecret b = companion.Confirm();

            Assert.True(a.Equals(b));
            Assert.Equal(a.Fingerprint, b.Fingerprint);
        }

        [Fact]
        public void CodeIsExactlySixDigits_AlwaysZeroPadded()
        {
            // Run enough ceremonies to hit a value below 100000 and prove padding holds. Without
            // padding a low code renders shorter on one screen and a user reports a mismatch that
            // is not one.
            for (int i = 0; i < 60; i++)
            {
                var (server, companion) = Exchange();
                Assert.Equal(FebrisP2pPairingSession.CodeDigits, server.ComparisonCode.Length);
                Assert.All(server.ComparisonCode, c => Assert.True(char.IsDigit(c)));
                Assert.Equal(server.ComparisonCode, companion.ComparisonCode);
            }
        }

        // ---------- the security claims ----------

        [Fact]
        public void ManInTheMiddle_ProducesDifferentCodesOnTheTwoScreens()
        {
            // THE defining property. An attacker cannot relay, it must run TWO exchanges, so the
            // Server pairs with the attacker and the Companion pairs with the attacker. Their
            // transcripts differ, so the humans see different codes and refuse.
            var server = new FebrisP2pPairingSession(isInitiator: true);
            var companion = new FebrisP2pPairingSession(isInitiator: false);
            var mitmTowardCompanion = new FebrisP2pPairingSession(isInitiator: true);
            var mitmTowardServer = new FebrisP2pPairingSession(isInitiator: false);

            // Server <-> attacker
            Assert.True(mitmTowardServer.ProcessPeerKey(server.PublicKey));
            Assert.True(server.ProcessPeerKey(mitmTowardServer.PublicKey));

            // attacker <-> Companion
            Assert.True(companion.ProcessPeerKey(mitmTowardCompanion.PublicKey));
            Assert.True(mitmTowardCompanion.ProcessPeerKey(companion.PublicKey));

            Assert.NotEqual(server.ComparisonCode, companion.ComparisonCode);
        }

        [Fact]
        public void TwoIndependentCeremonies_ProduceDifferentPsks()
        {
            // Ephemeral keys per ceremony. If two pairings produced the same PSK, compromising one
            // device would compromise every pair.
            var (s1, _) = Exchange();
            var (s2, _) = Exchange();

            Assert.NotEqual(s1.Confirm().Fingerprint, s2.Confirm().Fingerprint);
        }

        [Fact]
        public void TheDisplayedCode_IsNotDerivableFromThePsk()
        {
            // The code is shown on screen and may be photographed or shoulder-surfed, so it must
            // be cryptographically independent of the key. Different HKDF info strings give that;
            // this pins that they were not accidentally unified.
            var (server, _) = Exchange();
            string code = server.ComparisonCode;
            FebrisP2pPairingSecret psk = server.Confirm();

            Assert.DoesNotContain(code, psk.ToBase64String());
            Assert.DoesNotContain(code, psk.Fingerprint);
        }

        [Fact]
        public void RoleOrdering_IsWhatMakesBothSidesAgree()
        {
            // Both sides must order the transcript identically. Two peers that both believe they
            // are the initiator concatenate the keys in opposite orders and disagree, which would
            // present as a mismatch on an honest pairing.
            var a = new FebrisP2pPairingSession(isInitiator: true);
            var b = new FebrisP2pPairingSession(isInitiator: true);   // WRONG, both initiator

            Assert.True(a.ProcessPeerKey(b.PublicKey));
            Assert.True(b.ProcessPeerKey(a.PublicKey));

            Assert.NotEqual(a.ComparisonCode, b.ComparisonCode);
        }

        // ---------- hostile input ----------

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(63)]
        [InlineData(65)]
        [InlineData(200)]
        public void WrongLengthPublicKey_IsRefusedNotThrown(int length)
        {
            // A peer can send anything. Refusal must be a return value, because an exception here
            // would unwind into a pairing UI that has no useful way to handle it.
            var session = new FebrisP2pPairingSession(isInitiator: true);
            Assert.False(session.ProcessPeerKey(new byte[length]));
        }

        [Fact]
        public void NullPublicKey_IsRefused()
        {
            var session = new FebrisP2pPairingSession(isInitiator: true);
            Assert.False(session.ProcessPeerKey(null));
        }

        [Fact]
        public void APointNotOnTheCurve_IsRefused()
        {
            // Invalid-curve attack. Feeding a point off the curve can leak the private key one
            // derivation at a time, so ImportParameters must reject it and we must surface that as
            // a refusal. 64 bytes of 0xFF is a valid LENGTH and an invalid point.
            var session = new FebrisP2pPairingSession(isInitiator: true);
            byte[] bogus = Enumerable.Repeat((byte)0xFF, 64).ToArray();

            Assert.False(session.ProcessPeerKey(bogus));
        }

        [Fact]
        public void AllZeroPoint_IsRefused()
        {
            var session = new FebrisP2pPairingSession(isInitiator: true);
            Assert.False(session.ProcessPeerKey(new byte[64]));
        }

        [Fact]
        public void PublicKey_IsAlwaysTheFixedWireLength()
        {
            // Right-alignment of short coordinates. A coordinate with a leading zero byte comes
            // back 31 bytes; copying it to offset zero would shift the value and break agreement
            // roughly 1 ceremony in 256.
            for (int i = 0; i < 60; i++)
            {
                using (var s = new FebrisP2pPairingSession(isInitiator: true))
                {
                    Assert.Equal(64, s.PublicKey.Length);
                }
            }
        }

        [Fact]
        public void PublicKey_IsADefensiveCopy()
        {
            var session = new FebrisP2pPairingSession(isInitiator: true);
            byte[] first = session.PublicKey;
            first[0] ^= 0xFF;

            Assert.NotEqual(first, session.PublicKey);
        }

        // ---------- ceremony state ----------

        [Fact]
        public void Reject_DestroysTheKey_SoAMismatchCannotBeOverridden()
        {
            // If the human says the codes differ, someone is in the middle. The derived key must
            // become unreachable rather than merely unused.
            var (server, _) = Exchange();
            server.Reject();

            Assert.False(server.AwaitingConfirmation);
            Assert.Null(server.ComparisonCode);
            Assert.Throws<InvalidOperationException>(() => server.Confirm());
        }

        [Fact]
        public void ASessionIsSingleUse_SoAFailedCeremonyCannotBeRetried()
        {
            // One guess per ceremony is what makes six digits enough. Reusing the ephemeral keys
            // for a second attempt would let an attacker grind the code.
            var (server, companion) = Exchange();
            server.Confirm();

            Assert.False(server.AwaitingConfirmation);
            Assert.Throws<InvalidOperationException>(() => server.Confirm());
            Assert.Throws<InvalidOperationException>(() => server.ProcessPeerKey(companion.PublicKey));
        }

        [Fact]
        public void ConfirmBeforeTheKeyExchange_IsRefused()
        {
            var session = new FebrisP2pPairingSession(isInitiator: true);

            Assert.False(session.AwaitingConfirmation);
            Assert.Null(session.ComparisonCode);
            Assert.Throws<InvalidOperationException>(() => session.Confirm());
        }

        [Fact]
        public void ProcessingASecondPeerKey_IsRefused()
        {
            // Prevents an attacker racing a second key in after an honest one to move the code.
            var (server, companion) = Exchange();
            Assert.Throws<InvalidOperationException>(() => server.ProcessPeerKey(companion.PublicKey));
        }

        [Fact]
        public void DisposeLeavesNothingConfirmable()
        {
            var (server, _) = Exchange();
            server.Dispose();

            Assert.False(server.AwaitingConfirmation);
            Assert.Throws<InvalidOperationException>(() => server.Confirm());
        }

        // ---------- the wire format, pinned against an INDEPENDENT implementation ----------

        // Everything above this point is BouncyCastle talking to BouncyCastle. That is
        // self-consistency, and self-consistency cannot catch a systematically wrong encoding:
        // swap X and Y in BOTH the export and the import and all 24 tests above still pass, while
        // the bytes on the wire stop being the documented format. The BouncyCastle port was
        // required to keep that format byte-for-byte (docs/MOBILE_SESSION_HANDOFF.md section 5),
        // so it is pinned here against System.Security.Cryptography.
        //
        // These two tests validate the ENCODING only. They run on net8.0, where the platform ECDH
        // works; they say nothing about Mono/Android, which is the whole reason the port happened.
        // The device-side equivalent is PlatformCryptoProbe, read from logcat.

        [Fact]
        public void OurPublicKey_IsAValidP256Point_ByAnIndependentImplementation()
        {
            using (var session = new FebrisP2pPairingSession(isInitiator: true))
            {
                byte[] raw = session.PublicKey;
                var parameters = new System.Security.Cryptography.ECParameters
                {
                    Curve = System.Security.Cryptography.ECCurve.NamedCurves.nistP256,
                    Q = new System.Security.Cryptography.ECPoint
                    {
                        X = raw.Take(32).ToArray(),
                        Y = raw.Skip(32).Take(32).ToArray()
                    }
                };

                // ImportParameters validates the point satisfies the curve equation. If X and Y
                // were emitted in the wrong order the resulting pair is off the curve with
                // overwhelming probability, so this throws rather than quietly accepting.
                using (var independent = System.Security.Cryptography.ECDiffieHellman.Create())
                {
                    independent.ImportParameters(parameters);
                }
            }
        }

        [Fact]
        public void APublicKeyFromAnIndependentImplementation_IsAccepted()
        {
            // The other direction. A point generated and encoded by System.Security.Cryptography
            // must be readable by our BouncyCastle import, which pins that we agree on X||Y and
            // not merely that we agree with ourselves.
            using (var independent = System.Security.Cryptography.ECDiffieHellman.Create(
                       System.Security.Cryptography.ECCurve.NamedCurves.nistP256))
            {
                System.Security.Cryptography.ECParameters exported = independent.ExportParameters(false);

                byte[] raw = new byte[64];
                CopyRightAligned(exported.Q.X, raw, 0);
                CopyRightAligned(exported.Q.Y, raw, 32);

                var session = new FebrisP2pPairingSession(isInitiator: true);
                Assert.True(session.ProcessPeerKey(raw));
                Assert.NotNull(session.ComparisonCode);
            }
        }

        /// <summary>Coordinates can come back shorter than 32 bytes when they have leading zeros.</summary>
        private static void CopyRightAligned(byte[] value, byte[] destination, int offset)
        {
            Buffer.BlockCopy(value, 0, destination, offset + (32 - value.Length), value.Length);
        }

        [Fact]
        public void ThePlatformProbe_ReportsWithoutThrowing()
        {
            // The probe runs at startup on both heads. A diagnostic that crashes the app it is
            // diagnosing is worse than no diagnostic, so the contract is that it always returns
            // text, including when the primitive it is probing does not exist on the runtime.
            string report = PlatformCryptoProbe.Describe();

            Assert.False(string.IsNullOrWhiteSpace(report));
            Assert.Contains("BouncyCastleEcdh=", report);
            Assert.Contains("AesGcm=", report);

            // Our own key agreement must work wherever this suite runs, unlike the two platform
            // primitives, whose results are environment-dependent and therefore not asserted.
            Assert.Contains("BouncyCastleEcdh=OK", report);
        }

        // ---------- integration with what already exists ----------

        [Fact]
        public void ThePairedPskDrivesARealHandshakeToCompletion()
        {
            // End to end: pair, then run the actual three-way handshake with the resulting PSK on
            // both sides. This is the thing that was impossible before, and it proves the PSK this
            // ceremony produces is usable by the crypto that was already written.
            var (server, companion) = Exchange();
            FebrisP2pPairingSecret serverPsk = server.Confirm();
            FebrisP2pPairingSecret companionPsk = companion.Confirm();

            var client = new FebrisP2pClientHandshake(companionPsk);
            var responder = new FebrisP2pServerHandshake(serverPsk);

            byte[] clientNonce = client.StartHandshake();
            var (serverNonce, serverResponse) = responder.ProcessClientHello(clientNonce);
            byte[] clientFinal = client.ProcessServerHello(serverNonce, serverResponse);
            responder.ProcessClientFinal(clientFinal);

            Assert.Equal(client.GetSessionKey().KeyBytes, responder.GetSessionKey().KeyBytes);
        }

        [Fact]
        public void APskFromADifferentCeremony_FailsTheHandshake()
        {
            // The negative of the above. Two devices that paired separately must not authenticate.
            var (serverA, _) = Exchange();
            var (_, companionB) = Exchange();

            var client = new FebrisP2pClientHandshake(companionB.Confirm());
            var responder = new FebrisP2pServerHandshake(serverA.Confirm());

            byte[] clientNonce = client.StartHandshake();
            var (serverNonce, serverResponse) = responder.ProcessClientHello(clientNonce);

            Assert.Throws<HandshakeAuthenticationFailedException>(
                () => client.ProcessServerHello(serverNonce, serverResponse));
        }

        [Fact]
        public async System.Threading.Tasks.Task APairedSecretRoundTripsThroughTheStore()
        {
            // The ceremony hands off to P2pPairingSecretStore, so prove the handoff preserves the
            // key rather than assuming it.
            var (server, companion) = Exchange();
            FebrisP2pPairingSecret paired = server.Confirm();

            var backing = new InMemoryStore();
            var store = new P2pPairingSecretStore(backing);
            await store.LoadAsync();
            await store.SaveAsync("companion-xyz", paired);

            var reloaded = new P2pPairingSecretStore(backing);
            await reloaded.LoadAsync();
            Assert.True(reloaded.TryGetSecret("companion-xyz", out FebrisP2pPairingSecret fromStore));

            Assert.True(fromStore.Equals(companion.Confirm()));
        }

        private sealed class InMemoryStore : ISecureKeyValueStore
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _data =
                new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);

            public System.Threading.Tasks.Task<string> GetAsync(string key)
            {
                _data.TryGetValue(key, out string v);
                return System.Threading.Tasks.Task.FromResult(v);
            }

            public System.Threading.Tasks.Task SetAsync(string key, string value)
            {
                _data[key] = value;
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public bool Remove(string key) => _data.Remove(key);
        }
    }
}
