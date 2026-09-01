// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Threading.Tasks;
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;

namespace Febris.SharedMobileLibrary.FileSystem
{
    /// <summary>
    /// The credential this device authenticates to a node with (NODE-9).
    ///
    /// <para>
    /// WHY THIS EXISTS. The Mobile Server used to send <c>IDevice.GetIdentifier()</c> as its
    /// licence key. Audit T9 changed the node to MINT the device credential at registration and
    /// store only its hash, so a value the device computes for itself matches no row and
    /// authentication can never succeed. It fails as a plain 401, which is indistinguishable from a
    /// wrong credential, and that is why the break went unnoticed. The node shows the minted string
    /// once; the operator pastes it into the configuration screen; this keeps it.
    /// </para>
    ///
    /// <para>
    /// STORED IN THE KEYSTORE, NOT IN config.json. The obvious place would have been
    /// <c>ConfigModel</c> beside the other settings, but that file is plaintext JSON. This value
    /// authenticates the device, so it belongs in platform secure storage. It is deliberately NOT
    /// the device identifier's replacement for every purpose: <c>GetIdentifier()</c> is still the
    /// right answer for "which device is this" in statements and P2P, and only the AUTHENTICATION
    /// call site changed.
    /// </para>
    ///
    /// <para>
    /// BUILT ON <see cref="ISecureKeyValueStore"/> RATHER THAN THE EXISTING <c>DataProtection</c>
    /// CLASS, for the reason already recorded on <see cref="EssentialsSecureKeyValueStore"/>:
    /// <c>DataProtection.EncryptInput</c> is <c>async void</c> over a <c>_log</c> that is null on
    /// the parameterless constructor, so a storage failure there is an unobserved exception on a
    /// background thread against a null reference, and the write disappears with nothing reported.
    /// A lost write here means a device that can never authenticate and an operator with no way to
    /// tell why, so <see cref="SaveAsync"/> reports failure instead.
    /// </para>
    ///
    /// <para>
    /// NOT VERIFIED ON A DEVICE. The backing store needs a live Android context. What is tested is
    /// this class's own behaviour against an in-memory backing.
    /// </para>
    /// </summary>
    public sealed class DeviceCredentialStore
    {
        /// <summary>
        /// Secure-storage key. Namespaced like the P2P keys so the app's entries stay recognisable
        /// in a keystore shared with whatever else the platform puts there.
        /// </summary>
        public const string CredentialKey = "febris.device.credential";

        private readonly ISecureKeyValueStore _backing;

        public DeviceCredentialStore(ISecureKeyValueStore backing)
        {
            _backing = backing ?? throw new ArgumentNullException(nameof(backing));
        }

        /// <summary>
        /// The stored credential, or empty when this device has not been registered.
        ///
        /// <para>
        /// A storage failure also reads as empty, on purpose. The two cases are not distinguishable
        /// from here, and both mean the same thing to the caller: this device cannot authenticate
        /// right now and the operator needs to be told so. Throwing instead would take down the
        /// authentication path on exactly the devices that are not yet set up, which is the normal
        /// state of a device out of the box.
        /// </para>
        /// </summary>
        public async Task<string> GetAsync()
        {
            try
            {
                string stored = await _backing.GetAsync(CredentialKey).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(stored) ? string.Empty : stored.Trim();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Whether this device has a credential to authenticate with. Callers use this to say
        /// "register this device first" rather than sending a request that cannot succeed.
        /// </summary>
        public async Task<bool> HasCredentialAsync()
        {
            string credential = await GetAsync().ConfigureAwait(false);
            return credential.Length > 0;
        }

        /// <summary>
        /// Stores the credential the node minted.
        ///
        /// <para>
        /// Whitespace is trimmed because the value is copied by hand from a portal page onto a
        /// touch keyboard, and a trailing space or newline hashes to a completely different value:
        /// the node would reject the device with no indication that the only fault was an invisible
        /// character.
        /// </para>
        /// </summary>
        /// <returns>
        /// True only when the credential is actually stored. An empty input is refused rather than
        /// written, because an empty stored credential reads back as "not registered" anyway, and
        /// reporting success would leave the configuration screen looking saved while the device
        /// stays unable to authenticate.
        /// </returns>
        public async Task<bool> SaveAsync(string credential)
        {
            string trimmed = (credential ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                return false;
            }

            try
            {
                await _backing.SetAsync(CredentialKey, trimmed).ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
                // Surfaced as false rather than swallowed. The pairing store made the same call for
                // the same reason: a write that silently fails is the one failure mode this cannot
                // have.
                return false;
            }
        }

        /// <summary>
        /// Forgets the credential, for a device being handed to another node or decommissioned.
        /// The next authentication attempt then correctly reports that the device is unregistered.
        /// </summary>
        public bool Clear()
        {
            try
            {
                return _backing.Remove(CredentialKey);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
