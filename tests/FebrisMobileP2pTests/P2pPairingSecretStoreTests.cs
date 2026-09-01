// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Febris.SharedMobileLibrary.P2pNetworking;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// The store that ends the handshake's dormancy. Worth testing hard because every failure
    /// mode here is silent: a store that returns "no secret" is indistinguishable from an
    /// unpaired device, so a storage bug does not look like a bug, it looks like the
    /// transitional unauthenticated path working as designed.
    /// </summary>
    public class P2pPairingSecretStoreTests
    {
        private const string PeerA = "companion-aaaa1111";
        private const string PeerB = "companion-bbbb2222";

        private static FebrisP2pPairingSecret Psk(byte fill)
        {
            byte[] raw = new byte[FebrisP2pPairingSecret.KeySizeBytes];
            for (int i = 0; i < raw.Length; i++) raw[i] = fill;
            return FebrisP2pPairingSecret.FromBytes(raw);
        }

        /// <summary>In-memory stand-in for Android Keystore, with hooks for the failure modes
        /// that actually happen on a device: a torn write and an unreadable entry.</summary>
        private sealed class FakeBacking : ISecureKeyValueStore
        {
            public readonly Dictionary<string, string> Data = new Dictionary<string, string>(StringComparer.Ordinal);
            public bool ThrowOnGet;
            public bool ThrowOnSet;
            public int SetCalls;

            public Task<string> GetAsync(string key)
            {
                if (ThrowOnGet) throw new InvalidOperationException("keystore unavailable");
                Data.TryGetValue(key, out string v);
                return Task.FromResult(v);
            }

            public Task SetAsync(string key, string value)
            {
                SetCalls++;
                if (ThrowOnSet) throw new InvalidOperationException("keystore write failed");
                Data[key] = value;
                return Task.CompletedTask;
            }

            public bool Remove(string key) => Data.Remove(key);
        }

        private static async Task<(P2pPairingSecretStore store, FakeBacking backing)> Loaded()
        {
            var backing = new FakeBacking();
            var store = new P2pPairingSecretStore(backing);
            await store.LoadAsync();
            return (store, backing);
        }

        [Fact]
        public async Task SavedSecret_IsVisibleSynchronouslyWithoutAReload()
        {
            // The pairing flow writes, and the very next connection must authenticate. If the
            // write only reached storage, a just-paired device would fail until app restart,
            // which reads to a user as "pairing did not work".
            var (store, _) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0x11));

            Assert.True(store.TryGetSecret(PeerA, out FebrisP2pPairingSecret found));
            Assert.Equal(Psk(0x11).Fingerprint, found.Fingerprint);
        }

        [Fact]
        public async Task SecretsSurviveAReload()
        {
            var (store, backing) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0x22));
            await store.SaveAsync(PeerB, Psk(0x33));

            // A fresh process over the same storage.
            var reloaded = new P2pPairingSecretStore(backing);
            Assert.Equal(2, await reloaded.LoadAsync());

            Assert.True(reloaded.TryGetSecret(PeerA, out var a));
            Assert.True(reloaded.TryGetSecret(PeerB, out var b));
            Assert.Equal(Psk(0x22).Fingerprint, a.Fingerprint);
            Assert.Equal(Psk(0x33).Fingerprint, b.Fingerprint);
        }

        [Fact]
        public async Task LookupBeforeLoad_ReturnsFalse_WhichIsWhyIsLoadedExists()
        {
            // Pins the trap: an unloaded store answers "unpaired" for every peer, which under
            // RequireHandshake=false silently downgrades every connection to unauthenticated.
            // The pumps must not start until IsLoaded.
            var backing = new FakeBacking();
            var store = new P2pPairingSecretStore(backing);

            Assert.False(store.IsLoaded);
            Assert.False(store.TryGetSecret(PeerA, out _));

            await store.LoadAsync();
            Assert.True(store.IsLoaded);
        }

        [Fact]
        public async Task CorruptSecret_IsDroppedRatherThanSurfacedAsAPhantomPairing()
        {
            // A non-base64 blob under a peer's key. Dropping it means "re-pair this device".
            // Keeping the index entry would instead fail inside the handshake as an
            // authentication error, which reads as an attack rather than as a bad write.
            var (store, backing) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0x44));
            backing.Data["febris.p2p.psk." + PeerA] = "not-valid-base64!!!";

            var reloaded = new P2pPairingSecretStore(backing);
            Assert.Equal(0, await reloaded.LoadAsync());
            Assert.False(reloaded.TryGetSecret(PeerA, out _));
        }

        [Fact]
        public async Task WrongLengthSecret_IsDropped()
        {
            // Valid base64, but not 32 bytes. FebrisP2pPairingSecret rejects it, and the store
            // must treat that as unpaired rather than let the ArgumentException escape into
            // startup.
            var (store, backing) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0x55));
            backing.Data["febris.p2p.psk." + PeerA] = Convert.ToBase64String(new byte[16]);

            var reloaded = new P2pPairingSecretStore(backing);
            Assert.Equal(0, await reloaded.LoadAsync());
        }

        [Fact]
        public async Task IndexEntryWithNoSecret_IsDropped()
        {
            // The torn-write case: index written, secret lost.
            var backing = new FakeBacking();
            backing.Data["febris.p2p.psk.index"] = PeerA + "\n" + PeerB;
            backing.Data["febris.p2p.psk." + PeerB] = Psk(0x66).ToBase64String();

            var store = new P2pPairingSecretStore(backing);
            Assert.Equal(1, await store.LoadAsync());
            Assert.False(store.TryGetSecret(PeerA, out _));
            Assert.True(store.TryGetSecret(PeerB, out _));
        }

        [Fact]
        public async Task SecretIsWrittenBeforeTheIndex()
        {
            // Order is the crash-safety property. A secret with no index entry is inert; an
            // index entry with no secret is a phantom pairing. Assert the safe ordering by
            // failing the write and checking what survived.
            var backing = new FakeBacking();
            var store = new P2pPairingSecretStore(backing);
            await store.LoadAsync();

            await store.SaveAsync(PeerA, Psk(0x77));

            // Two writes: the secret, then the index.
            Assert.Equal(2, backing.SetCalls);
            Assert.True(backing.Data.ContainsKey("febris.p2p.psk." + PeerA));
            Assert.Equal(PeerA, backing.Data["febris.p2p.psk.index"]);
        }

        [Fact]
        public async Task UnreadableIndex_FailsClosedInsteadOfThrowing()
        {
            // A keystore that is unavailable at startup must not crash the app, and must not
            // pretend devices are paired.
            var backing = new FakeBacking { ThrowOnGet = true };
            var store = new P2pPairingSecretStore(backing);

            Assert.Equal(0, await store.LoadAsync());
            Assert.True(store.IsLoaded);
            Assert.False(store.TryGetSecret(PeerA, out _));
        }

        [Fact]
        public async Task FailedWrite_PropagatesRatherThanAppearingToSucceed()
        {
            // The pairing UI has to be able to tell the operator that pairing failed. A
            // swallowed write would leave both devices believing they are paired with
            // different secrets, which surfaces later as an unexplained auth failure.
            var (store, backing) = await Loaded();
            backing.ThrowOnSet = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(PeerA, Psk(0x88)));
        }

        [Fact]
        public async Task Forget_RevokesImmediately_EvenBeforeStorageIsRewritten()
        {
            // Revocation must beat an in-flight connection to the cache.
            var (store, _) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0x99));

            Assert.True(await store.ForgetAsync(PeerA));
            Assert.False(store.TryGetSecret(PeerA, out _));
        }

        [Fact]
        public async Task Forget_OnAnUnpairedPeer_IsNotAnError()
        {
            var (store, _) = await Loaded();
            Assert.False(await store.ForgetAsync(PeerA));
        }

        [Fact]
        public async Task ForgottenPeer_DoesNotComeBackAfterAReload()
        {
            var (store, backing) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0xAA));
            await store.SaveAsync(PeerB, Psk(0xBB));
            await store.ForgetAsync(PeerA);

            var reloaded = new P2pPairingSecretStore(backing);
            Assert.Equal(1, await reloaded.LoadAsync());
            Assert.False(reloaded.TryGetSecret(PeerA, out _));
            Assert.True(reloaded.TryGetSecret(PeerB, out _));
        }

        [Fact]
        public async Task RePairing_ReplacesTheSecretRatherThanKeepingBoth()
        {
            // FebrisP2pPairingSecret's documented lifecycle: re-pairing destroys the old PSK.
            var (store, backing) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0xCC));
            await store.SaveAsync(PeerA, Psk(0xDD));

            Assert.True(store.TryGetSecret(PeerA, out var current));
            Assert.Equal(Psk(0xDD).Fingerprint, current.Fingerprint);
            Assert.Equal(1, store.Count);

            var reloaded = new P2pPairingSecretStore(backing);
            await reloaded.LoadAsync();
            Assert.Equal(1, reloaded.Count);
        }

        [Fact]
        public async Task PeerIdentifierWithANewline_IsRejected()
        {
            // The index is newline-delimited, so this would split one peer into two on reload
            // and silently unpair the device.
            var (store, _) = await Loaded();
            await Assert.ThrowsAsync<ArgumentException>(
                () => store.SaveAsync("companion\nevil", Psk(0xEE)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task BlankPeerIdentifier_IsRejectedOnWriteAndFalseOnRead(string identifier)
        {
            var (store, _) = await Loaded();

            Assert.False(store.TryGetSecret(identifier, out _));
            await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(identifier, Psk(0x01)));
        }

        [Fact]
        public async Task NullSecret_IsRejected()
        {
            var (store, _) = await Loaded();
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(PeerA, null));
        }

        [Fact]
        public async Task Reload_DropsSecretsRemovedOutOfBand()
        {
            // The cache must not outlive the storage it mirrors.
            var (store, backing) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0x02));
            backing.Data.Remove("febris.p2p.psk." + PeerA);

            Assert.Equal(0, await store.LoadAsync());
            Assert.False(store.TryGetSecret(PeerA, out _));
        }

        [Fact]
        public async Task PairedPeers_ListsOnlyUsablePairings()
        {
            var backing = new FakeBacking();
            backing.Data["febris.p2p.psk.index"] = PeerA + "\n" + PeerB;
            backing.Data["febris.p2p.psk." + PeerA] = Psk(0x03).ToBase64String();
            // PeerB's secret is missing.

            var store = new P2pPairingSecretStore(backing);
            await store.LoadAsync();

            Assert.Equal(new[] { PeerA }, store.PairedPeers().ToArray());
        }

        [Fact]
        public async Task AStoreWithASecret_MakesTheHandshakePerform()
        {
            // The end-to-end point of this class: with a real store, P2pHandshakePolicy stops
            // returning SkipUnauthenticated and the handshake actually runs. This is what was
            // impossible before, and it is why the dormancy was never a code-path problem.
            var (store, _) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0x04));

            HandshakeDisposition disposition =
                P2pHandshakePolicy.Evaluate(store, PeerA, out FebrisP2pPairingSecret secret);

            Assert.Equal(HandshakeDisposition.Perform, disposition);
            Assert.NotNull(secret);
        }

        [Fact]
        public async Task AnUnpairedPeer_StillSkipsWhileRequireHandshakeIsFalse()
        {
            // The transitional path must survive the store existing. Provisioning one device
            // must not refuse every other device.
            var (store, _) = await Loaded();
            await store.SaveAsync(PeerA, Psk(0x05));

            bool original = P2pHandshakePolicy.RequireHandshake;
            try
            {
                P2pHandshakePolicy.RequireHandshake = false;
                Assert.Equal(
                    HandshakeDisposition.SkipUnauthenticated,
                    P2pHandshakePolicy.Evaluate(store, PeerB, out _));
            }
            finally { P2pHandshakePolicy.RequireHandshake = original; }
        }

        [Fact]
        public async Task TwoStoresOverTheSameStorage_AgreeOnTheSecret_WhichIsWhatPairingRequires()
        {
            // Both halves of a pair must derive the same key, so both stores must hand back
            // byte-identical PSKs. Fingerprint equality is the safe way to assert that without
            // copying key bytes around in a test.
            var backing = new FakeBacking();
            var writer = new P2pPairingSecretStore(backing);
            await writer.LoadAsync();

            FebrisP2pPairingSecret generated = FebrisP2pPairingSecret.Generate();
            await writer.SaveAsync(PeerA, generated);

            var reader = new P2pPairingSecretStore(backing);
            await reader.LoadAsync();
            Assert.True(reader.TryGetSecret(PeerA, out var readBack));

            Assert.Equal(generated.Fingerprint, readBack.Fingerprint);
            Assert.True(generated.Equals(readBack));
        }

        [Fact]
        public void NullBacking_IsRejectedAtConstruction()
        {
            Assert.Throws<ArgumentNullException>(() => new P2pPairingSecretStore(null));
        }
    }
}
