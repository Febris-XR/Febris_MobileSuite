// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.P2pNetworking.Crypto;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Supplies the per-pair pre-shared key for a peer.
    ///
    /// Deliberately an interface with no implementation in the shared library: the PSK
    /// must live in platform secure storage (Android Keystore, or the existing
    /// DataProtection path), which is head-specific. The shared library defines the
    /// contract and the policy, the heads provide the storage.
    /// </summary>
    public interface IP2pPairingSecretStore
    {
        /// <summary>Look up the PSK established with this peer at pairing time.
        /// Returns false when the pair has never been paired, or was forgotten.</summary>
        bool TryGetSecret(string peerIdentifier, out FebrisP2pPairingSecret secret);
    }

    /// <summary>What the connection should do about the handshake right now.</summary>
    public enum HandshakeDisposition
    {
        /// <summary>A PSK exists. Run the handshake and reject the peer if it fails.</summary>
        Perform,

        /// <summary>No PSK, and the deployment does not require one yet. Proceed
        /// UNAUTHENTICATED. This is a transitional state and must be logged loudly.</summary>
        SkipUnauthenticated,

        /// <summary>No PSK and the deployment requires one. Refuse the connection.</summary>
        Refuse
    }

    /// <summary>
    /// Decides whether a connection runs the handshake, skips it, or is refused.
    ///
    /// WHY THIS IS NOT SIMPLY "ALWAYS HANDSHAKE". As of 2026-07-26 there is no pairing
    /// UX anywhere in the mobile tier, so no PSK can be provisioned on any device. Wiring
    /// the handshake fail-closed today would refuse every connection in every existing
    /// deployment. Wiring it fail-open unconditionally would be dishonest, because the
    /// channel would look protected and would not be.
    ///
    /// So the disposition is driven by whether a PSK actually exists for the peer, and
    /// <see cref="RequireHandshake"/> lets a deployment turn the transitional escape
    /// hatch off the moment its devices are paired. The intent is that this flag flips to
    /// true by default once pairing ships, and this type is then deleted.
    ///
    /// See docs/MOBILE_AUTH.md section 5.
    /// </summary>
    public static class P2pHandshakePolicy
    {
        /// <summary>
        /// When true, a connection with no PSK is REFUSED rather than allowed through
        /// unauthenticated.
        ///
        /// Defaults to false ONLY because no pairing mechanism exists yet. It is not a
        /// judgement that unauthenticated P2P is acceptable. Flip it per deployment as
        /// soon as devices are paired, and change the default when pairing ships.
        /// </summary>
        public static bool RequireHandshake = false;

        /// <summary>
        /// The store the socket pumps consult. NULL until a head implements platform
        /// secure storage, which is exactly why the handshake is dormant today: with no
        /// store there is no secret, so every connection evaluates to
        /// <see cref="HandshakeDisposition.SkipUnauthenticated"/> and behaves as it did
        /// before the handshake was wired.
        ///
        /// A head assigns this once at startup. Deliberately a static because the socket
        /// threads are constructed deep inside the WiFi stack with no DI to thread a
        /// dependency through, which matches how the rest of this tier is wired.
        /// </summary>
        public static IP2pPairingSecretStore SecretStore;

        /// <summary>
        /// Evaluate a peer. <paramref name="store"/> may be null on a head that has not
        /// implemented storage yet, which is treated the same as "no secret".
        /// </summary>
        public static HandshakeDisposition Evaluate(IP2pPairingSecretStore store, string peerIdentifier, out FebrisP2pPairingSecret secret)
        {
            secret = null;

            if (store != null && !string.IsNullOrWhiteSpace(peerIdentifier))
            {
                FebrisP2pPairingSecret found;
                if (store.TryGetSecret(peerIdentifier, out found) && found != null)
                {
                    secret = found;
                    return HandshakeDisposition.Perform;
                }
            }

            return RequireHandshake
                ? HandshakeDisposition.Refuse
                : HandshakeDisposition.SkipUnauthenticated;
        }

        /// <summary>The warning to emit when a connection proceeds unauthenticated. Kept
        /// here so every call site says the same thing and it is greppable.</summary>
        public static string UnauthenticatedWarning(string peerIdentifier)
        {
            return "P2P UNAUTHENTICATED: no pairing secret for peer '" + peerIdentifier +
                "', proceeding without a handshake. The channel is NOT authenticated. " +
                "Pair the device, then set P2pHandshakePolicy.RequireHandshake = true.";
        }
    }
}
