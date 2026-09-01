// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;
using FluentAssertions;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// NODE-9: the Mobile Server's store for the credential the node mints.
    ///
    /// <para>
    /// WHY THIS MATTERS. The Mobile Server authenticated with <c>IDevice.GetIdentifier()</c>, a
    /// value it derived itself. Audit T9 made the node mint the credential and keep only its hash,
    /// so a self-derived value matches nothing and the device can never authenticate. The failure
    /// is a plain 401, identical to a wrong credential, so nothing about it looks like a bug.
    /// </para>
    ///
    /// <para>
    /// EVERY FAILURE IN THIS CLASS IS EQUALLY SILENT. A store that loses the credential, trims it
    /// wrongly, or reports a save that never happened all surface at the device as the same 401.
    /// So the behaviours pinned below are the ones that would otherwise be invisible: what an
    /// absent credential reads as, what a failed write reports, and what happens to the whitespace
    /// a touch keyboard adds.
    /// </para>
    /// </summary>
    public class DeviceCredentialStoreTests
    {
        // A realistic value: base64url, 43 characters, matching DeviceCredential.Generate on the
        // node side. Using a realistic shape rather than "abc" keeps the trimming and round-trip
        // assertions honest.
        private const string Minted = "n3Zq7Kx2Ld8Rw1Yb4Tv6Hs0Pj9Mc5Ge2Af7Un3Iy1Ko";

        /// <summary>
        /// In-memory stand-in for Android Keystore, with hooks for the failure modes that actually
        /// happen on a device: an unreadable entry and a refused write.
        /// </summary>
        private sealed class FakeBacking : ISecureKeyValueStore
        {
            public readonly Dictionary<string, string> Data = new Dictionary<string, string>(StringComparer.Ordinal);
            public bool ThrowOnGet;
            public bool ThrowOnSet;

            public Task<string> GetAsync(string key)
            {
                if (ThrowOnGet) throw new InvalidOperationException("keystore unavailable");
                Data.TryGetValue(key, out string value);
                return Task.FromResult(value);
            }

            public Task SetAsync(string key, string value)
            {
                if (ThrowOnSet) throw new InvalidOperationException("keystore write failed");
                Data[key] = value;
                return Task.CompletedTask;
            }

            public bool Remove(string key) => Data.Remove(key);
        }

        /// <summary>
        /// The normal state of a device out of the box. It must read as empty, and it must not
        /// throw: the authentication path calls this on every attempt, and an exception here would
        /// take down the client on precisely the unregistered devices this change exists to serve.
        /// </summary>
        [Fact]
        public async Task An_unregistered_device_reads_as_empty()
        {
            DeviceCredentialStore store = new DeviceCredentialStore(new FakeBacking());

            (await store.GetAsync()).Should().BeEmpty();
            (await store.HasCredentialAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task A_saved_credential_round_trips_unchanged()
        {
            DeviceCredentialStore store = new DeviceCredentialStore(new FakeBacking());

            (await store.SaveAsync(Minted)).Should().BeTrue();

            (await store.GetAsync()).Should().Be(Minted);
            (await store.HasCredentialAsync()).Should().BeTrue();
        }

        /// <summary>
        /// Trimming is not cosmetic. The credential is typed or pasted on a touch keyboard, which
        /// readily adds a trailing space, and the node hashes what arrives: one invisible character
        /// and the device is rejected with nothing to indicate why.
        /// </summary>
        [Fact]
        public async Task Whitespace_around_a_pasted_credential_is_removed_on_the_way_in()
        {
            FakeBacking backing = new FakeBacking();
            DeviceCredentialStore store = new DeviceCredentialStore(backing);

            await store.SaveAsync("  " + Minted + " \r\n");

            backing.Data[DeviceCredentialStore.CredentialKey].Should().Be(Minted,
                "the stored value itself must be clean, not merely trimmed on the way out");
            (await store.GetAsync()).Should().Be(Minted);
        }

        /// <summary>
        /// And on the way out as well, so a credential written by an older build, or by hand, still
        /// authenticates rather than failing for a reason nobody can see.
        /// </summary>
        [Fact]
        public async Task Whitespace_already_in_storage_is_removed_on_the_way_out()
        {
            FakeBacking backing = new FakeBacking();
            backing.Data[DeviceCredentialStore.CredentialKey] = " " + Minted + "\n";

            DeviceCredentialStore store = new DeviceCredentialStore(backing);

            (await store.GetAsync()).Should().Be(Minted);
        }

        /// <summary>
        /// Saving nothing must report failure. Reporting success would leave the configuration
        /// screen looking saved while the device stays unregistered.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n")]
        public async Task An_empty_credential_is_refused_rather_than_stored(string input)
        {
            FakeBacking backing = new FakeBacking();
            DeviceCredentialStore store = new DeviceCredentialStore(backing);

            (await store.SaveAsync(input)).Should().BeFalse();

            backing.Data.Should().NotContainKey(DeviceCredentialStore.CredentialKey,
                "a refused save must not write anything");
        }

        /// <summary>
        /// THE FAILURE MODE THIS CLASS EXISTS TO AVOID. The older DataProtection helper writes
        /// through an <c>async void</c> method over a possibly-null logger, so a keystore failure
        /// vanishes and the caller believes the credential was saved. Here it is reported.
        /// </summary>
        [Fact]
        public async Task A_keystore_write_failure_is_reported_rather_than_swallowed()
        {
            FakeBacking backing = new FakeBacking { ThrowOnSet = true };
            DeviceCredentialStore store = new DeviceCredentialStore(backing);

            (await store.SaveAsync(Minted)).Should().BeFalse();
        }

        /// <summary>
        /// A keystore that cannot be read degrades to "not registered". That is the fail-closed
        /// answer and it is actionable: re-enter the credential. Throwing would instead crash the
        /// authentication path.
        /// </summary>
        [Fact]
        public async Task A_keystore_read_failure_degrades_to_unregistered()
        {
            FakeBacking backing = new FakeBacking { ThrowOnGet = true };
            DeviceCredentialStore store = new DeviceCredentialStore(backing);

            (await store.GetAsync()).Should().BeEmpty();
            (await store.HasCredentialAsync()).Should().BeFalse();
        }

        /// <summary>
        /// Re-registering replaces the credential. The previous value must not survive: it is the
        /// one a node operator has just revoked.
        /// </summary>
        [Fact]
        public async Task Re_registering_replaces_the_previous_credential()
        {
            DeviceCredentialStore store = new DeviceCredentialStore(new FakeBacking());
            const string Replacement = "Zx4Nq8Wt1Bv5Cr2Lm7Ka0Jd6Hf3Sp9Ug4Ye8Oi2Tn5";

            await store.SaveAsync(Minted);
            await store.SaveAsync(Replacement);

            (await store.GetAsync()).Should().Be(Replacement);
        }

        /// <summary>
        /// Decommissioning a device forgets its credential, and the device then correctly reports
        /// itself unregistered rather than retrying a credential the node no longer honours.
        /// </summary>
        [Fact]
        public async Task Clearing_forgets_the_credential()
        {
            DeviceCredentialStore store = new DeviceCredentialStore(new FakeBacking());
            await store.SaveAsync(Minted);

            store.Clear().Should().BeTrue();

            (await store.GetAsync()).Should().BeEmpty();
            (await store.HasCredentialAsync()).Should().BeFalse();
        }

        [Fact]
        public void A_store_cannot_be_built_without_a_backing()
        {
            Action build = () => new DeviceCredentialStore(null);

            build.Should().Throw<ArgumentNullException>();
        }
    }
}
