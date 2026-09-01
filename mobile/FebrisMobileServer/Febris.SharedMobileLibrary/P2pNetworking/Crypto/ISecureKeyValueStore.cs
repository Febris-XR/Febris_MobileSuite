// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// The narrow slice of platform secure storage the pairing-secret store needs.
    ///
    /// WHY THIS EXISTS RATHER THAN CALLING Xamarin.Essentials DIRECTLY. SecureStorage is
    /// static and needs a live Android context, so a store built on it cannot be unit
    /// tested at all. Every branch that matters here (a corrupt entry, a wrong-length key,
    /// an index that disagrees with what is actually stored) is a branch you only find by
    /// testing, and those are exactly the branches that decide whether a device can still
    /// authenticate after a bad write.
    ///
    /// Deliberately async, because the platform implementation is. The synchronous lookup
    /// the socket pumps need is served from a cache, not from here. See
    /// <see cref="P2pPairingSecretStore"/>.
    /// </summary>
    public interface ISecureKeyValueStore
    {
        /// <summary>Returns null when the key is absent.</summary>
        Task<string> GetAsync(string key);

        Task SetAsync(string key, string value);

        /// <summary>Returns false when the key was not present. Must not throw for a
        /// missing key: unpairing a device that was never paired is not an error.</summary>
        bool Remove(string key);
    }
}
