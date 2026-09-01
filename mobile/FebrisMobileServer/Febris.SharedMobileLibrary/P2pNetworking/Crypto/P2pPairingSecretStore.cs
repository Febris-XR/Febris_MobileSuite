// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// Persists the per-pair PSKs and serves them to the socket pumps.
    ///
    /// This is the piece whose absence kept the handshake dormant. `P2pHandshakePolicy.SecretStore`
    /// had no implementation, so every connection evaluated to SkipUnauthenticated with a null
    /// session key, and nothing downstream of the handshake could ever run.
    ///
    /// <para><b>Why a cache, and why that is not a shortcut.</b> <see cref="IP2pPairingSecretStore.TryGetSecret"/>
    /// is synchronous because it is called from <c>P2pHandshakePolicy.Evaluate</c> deep inside the
    /// socket pumps, which have no await point available at that moment. Platform secure storage is
    /// async. Blocking on it there would deadlock or stall the accept loop, so the secrets are
    /// hydrated once via <see cref="LoadAsync"/> at startup and the hot path reads memory. The
    /// consequence to be honest about: a secret provisioned on ANOTHER device is not visible until
    /// this process reloads, and writes through this class update the cache immediately so a
    /// just-paired device works without a restart.</para>
    ///
    /// <para><b>Why an explicit index.</b> Secure storage is a key-value bag with no enumeration, so
    /// "which devices are paired" has to be recorded separately. The index is the authority for
    /// listing, but never for answering a lookup: <see cref="LoadAsync"/> drops index entries whose
    /// secret is missing or unreadable, so a torn write degrades to "that device needs re-pairing"
    /// rather than to a phantom pairing that fails at handshake time with a confusing error.</para>
    ///
    /// <para><b>Keyed by device identifier, on both sides.</b> Not by IP address. See
    /// <see cref="P2pHandshakePolicy.SecretStore"/> and docs/MOBILE_AUTH.md.</para>
    /// </summary>
    public sealed class P2pPairingSecretStore : IP2pPairingSecretStore
    {
        /// <summary>Prefix for an individual secret. The peer identifier is appended.</summary>
        private const string SecretKeyPrefix = "febris.p2p.psk.";

        /// <summary>Where the list of paired peer identifiers lives.</summary>
        private const string IndexKey = "febris.p2p.psk.index";

        /// <summary>Separator for the index. Newline rather than comma because a device
        /// identifier is an opaque platform string and commas are likelier in it.</summary>
        private static readonly char[] IndexSeparator = new[] { '\n' };

        private readonly ISecureKeyValueStore _backing;
        private readonly object _gate = new object();
        private readonly Dictionary<string, FebrisP2pPairingSecret> _cache =
            new Dictionary<string, FebrisP2pPairingSecret>(StringComparer.Ordinal);

        private bool _loaded;

        public P2pPairingSecretStore(ISecureKeyValueStore backing)
        {
            _backing = backing ?? throw new ArgumentNullException(nameof(backing));
        }

        /// <summary>True once <see cref="LoadAsync"/> has completed. A lookup before that
        /// returns false for every peer, which would silently look like "unpaired", so the
        /// pumps should not be started until this is true.</summary>
        public bool IsLoaded { get { lock (_gate) { return _loaded; } } }

        /// <summary>Number of usable pairings currently cached.</summary>
        public int Count { get { lock (_gate) { return _cache.Count; } } }

        /// <summary>
        /// Hydrate the cache from secure storage. Safe to call again; a reload replaces the
        /// cache wholesale rather than merging, so a secret removed out of band disappears.
        /// </summary>
        /// <returns>The number of pairings loaded.</returns>
        public async Task<int> LoadAsync()
        {
            string index = null;
            try
            {
                index = await _backing.GetAsync(IndexKey).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // An unreadable index is not fatal: it means no device can authenticate until
                // re-pairing, which is the correct fail-closed outcome rather than a crash on
                // a background thread at startup.
                index = null;
            }

            var loaded = new Dictionary<string, FebrisP2pPairingSecret>(StringComparer.Ordinal);

            foreach (string peerIdentifier in SplitIndex(index))
            {
                FebrisP2pPairingSecret secret = await TryReadSecretAsync(peerIdentifier).ConfigureAwait(false);
                if (secret != null)
                {
                    loaded[peerIdentifier] = secret;
                }
                // A missing or corrupt secret is deliberately dropped rather than kept as an
                // empty entry. Re-pair is the recovery, and a phantom entry would instead fail
                // later inside the handshake as an authentication error, which reads as an
                // attack rather than as a storage problem.
            }

            lock (_gate)
            {
                _cache.Clear();
                foreach (var kv in loaded) { _cache[kv.Key] = kv.Value; }
                _loaded = true;
                return _cache.Count;
            }
        }

        /// <summary>Synchronous hot-path lookup. Serves the cache only.</summary>
        public bool TryGetSecret(string peerIdentifier, out FebrisP2pPairingSecret secret)
        {
            secret = null;
            if (string.IsNullOrWhiteSpace(peerIdentifier))
            {
                return false;
            }

            lock (_gate)
            {
                return _cache.TryGetValue(peerIdentifier, out secret) && secret != null;
            }
        }

        /// <summary>
        /// Provision or replace the PSK for a peer. This is the pairing flow's write.
        ///
        /// Order matters: the secret is written BEFORE the index. A crash between the two
        /// leaves an orphan secret that nothing reads, which is inert. The reverse order
        /// would leave an index entry with no secret, which <see cref="LoadAsync"/> would
        /// have to discard anyway.
        /// </summary>
        public async Task SaveAsync(string peerIdentifier, FebrisP2pPairingSecret secret)
        {
            if (string.IsNullOrWhiteSpace(peerIdentifier))
            {
                throw new ArgumentException("peerIdentifier is required.", nameof(peerIdentifier));
            }
            if (secret == null) throw new ArgumentNullException(nameof(secret));
            if (peerIdentifier.IndexOf('\n') >= 0)
            {
                // The index is newline-delimited, so a newline in an identifier would split
                // one peer into two on the next load.
                throw new ArgumentException(
                    "peerIdentifier must not contain a newline.", nameof(peerIdentifier));
            }

            await _backing.SetAsync(SecretKeyPrefix + peerIdentifier, secret.ToBase64String())
                .ConfigureAwait(false);

            lock (_gate) { _cache[peerIdentifier] = secret; }

            await WriteIndexAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Forget a pairing. Re-pairing generates a fresh PSK rather than reusing this one,
        /// which is the lifecycle FebrisP2pPairingSecret already specifies.
        ///
        /// The cache entry is dropped first so an in-flight connection cannot authenticate
        /// against a secret the operator has just revoked.
        /// </summary>
        public async Task<bool> ForgetAsync(string peerIdentifier)
        {
            if (string.IsNullOrWhiteSpace(peerIdentifier))
            {
                return false;
            }

            bool wasPresent;
            lock (_gate) { wasPresent = _cache.Remove(peerIdentifier); }

            _backing.Remove(SecretKeyPrefix + peerIdentifier);
            await WriteIndexAsync().ConfigureAwait(false);

            return wasPresent;
        }

        /// <summary>The peers with a usable pairing, for the pairing UI.</summary>
        public IReadOnlyList<string> PairedPeers()
        {
            lock (_gate) { return _cache.Keys.ToList(); }
        }

        private async Task<FebrisP2pPairingSecret> TryReadSecretAsync(string peerIdentifier)
        {
            try
            {
                string encoded = await _backing.GetAsync(SecretKeyPrefix + peerIdentifier)
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(encoded))
                {
                    return null;
                }
                return FebrisP2pPairingSecret.FromBase64String(encoded);
            }
            catch (FormatException)
            {
                return null;   // not base64
            }
            catch (ArgumentException)
            {
                return null;   // decoded, but not 32 bytes
            }
            catch (Exception)
            {
                return null;   // storage failure; treat as unpaired
            }
        }

        private Task WriteIndexAsync()
        {
            string joined;
            lock (_gate) { joined = string.Join("\n", _cache.Keys); }
            return _backing.SetAsync(IndexKey, joined);
        }

        private static IEnumerable<string> SplitIndex(string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                return Enumerable.Empty<string>();
            }
            return index
                .Split(IndexSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.Ordinal);
        }
    }
}
