// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// <see cref="ISecureKeyValueStore"/> over Xamarin.Essentials SecureStorage, which is
    /// Android Keystore backed.
    ///
    /// CHOSEN OVER THE EXISTING DataProtection CLASS DELIBERATELY. `DataProtection` wraps the
    /// same SecureStorage API, but its write is `async void` over a `_log` that is null on the
    /// parameterless constructor, so a storage failure there is an unobserved exception on a
    /// background thread against a null reference. Losing a PSK write silently is the one
    /// failure this store must not have, so the error path is surfaced rather than logged.
    ///
    /// Errors are NOT swallowed here. <see cref="P2pPairingSecretStore"/> decides what a
    /// failure means, because the correct response differs: a failed read degrades to
    /// "unpaired", while a failed write must reach the pairing UI as a failure rather than
    /// appear to succeed.
    ///
    /// NOT VERIFIED ON A DEVICE. SecureStorage needs a live Android context, so nothing here
    /// executes in the test suite.
    /// </summary>
    public sealed class EssentialsSecureKeyValueStore : ISecureKeyValueStore
    {
        public Task<string> GetAsync(string key)
        {
            return SecureStorage.GetAsync(key);
        }

        public Task SetAsync(string key, string value)
        {
            return SecureStorage.SetAsync(key, value);
        }

        public bool Remove(string key)
        {
            return SecureStorage.Remove(key);
        }
    }
}
