// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// Owns the ONE in-flight pairing ceremony and its handoff to persistent storage.
    ///
    /// <para>Shared by both tiers because the ceremony is symmetric apart from role and the key
    /// each side files the result under. Duplicating it per tier is exactly how the two halves of
    /// a protocol drift apart, which this codebase has been bitten by already.</para>
    ///
    /// <para><b>Only one ceremony at a time, deliberately.</b> Two concurrent ceremonies would put
    /// two codes on one screen with no way for the human to tell which belongs to which device,
    /// and the whole security argument rests on that human comparison being unambiguous. A second
    /// request while one is pending is REFUSED rather than queued, because by the time a queued one
    /// ran the operator would have walked away.</para>
    ///
    /// <para>Static because the socket pumps and the UI both need to reach it and this tier has no
    /// dependency injection, matching how <see cref="P2pHandshakePolicy"/> is wired.</para>
    /// </summary>
    public static class P2pPairingCoordinator
    {
        private static readonly object Gate = new object();
        private static FebrisP2pPairingSession _session;
        private static string _peerIdentifier;

        /// <summary>Where a confirmed PSK is written. Assigned at startup alongside
        /// <see cref="P2pHandshakePolicy.SecretStore"/>, and normally the same instance.</summary>
        public static P2pPairingSecretStore Store;

        /// <summary>Raised when a code is ready to show. The UI subscribes; the socket pump
        /// raises it. Kept as an event so the shared library needs no reference to either tier's
        /// view layer.</summary>
        public static event Action<string> CodeReady;

        /// <summary>The code currently awaiting a human decision, or null.</summary>
        public static string PendingCode
        {
            get { lock (Gate) { return _session != null && _session.AwaitingConfirmation ? _session.ComparisonCode : null; } }
        }

        public static bool CeremonyInFlight { get { lock (Gate) { return _session != null; } } }

        /// <summary>
        /// Server side. Start a ceremony and return the public key to send as the body of a
        /// <c>_pairingRequest</c>. Null when one is already running.
        /// </summary>
        public static byte[] Begin()
        {
            lock (Gate)
            {
                if (_session != null) return null;
                _session = new FebrisP2pPairingSession(isInitiator: true);
                return _session.PublicKey;
            }
        }

        /// <summary>
        /// Companion side. A <c>_pairingRequest</c> arrived. Returns our public key to send back
        /// as the <c>_pairingResponse</c> body, or null if we refuse.
        ///
        /// Refusing rather than replacing an in-flight ceremony matters: otherwise anyone can
        /// cancel an operator's pairing mid-comparison just by sending a request.
        /// </summary>
        public static byte[] AcceptRequest(byte[] serverPublicKey)
        {
            FebrisP2pPairingSession session;
            lock (Gate)
            {
                if (_session != null) return null;
                session = new FebrisP2pPairingSession(isInitiator: false);
                _session = session;
            }

            if (!session.ProcessPeerKey(serverPublicKey))
            {
                Abort();
                return null;
            }

            byte[] ours = session.PublicKey;
            RaiseCodeReady(session.ComparisonCode);
            return ours;
        }

        /// <summary>
        /// Server side. The <c>_pairingResponse</c> arrived. Returns false when the key is
        /// unusable, which aborts the ceremony.
        /// </summary>
        /// <param name="peerIdentifier">The identifier the CALLER will file the resulting PSK
        /// under, captured here so the UI does not have to carry it through the human's decision.
        /// On the Server this is the Companion's DeviceUniqueIdentifier from the response frame.</param>
        public static bool CompleteExchange(byte[] companionPublicKey, string peerIdentifier)
        {
            FebrisP2pPairingSession session;
            lock (Gate)
            {
                session = _session;
                _peerIdentifier = peerIdentifier;
            }
            if (session == null) return false;

            if (!session.ProcessPeerKey(companionPublicKey))
            {
                Abort();
                return false;
            }

            RaiseCodeReady(session.ComparisonCode);
            return true;
        }

        /// <summary>
        /// The human said the codes match. Persists the PSK under <paramref name="peerIdentifier"/>
        /// and ends the ceremony.
        ///
        /// <returns>The identifier the PSK was filed under, or null on failure.</returns>
        ///
        /// The identifier differs per tier by design, see docs/MOBILE_AUTH.md 4.4: the Server files
        /// under the Companion's device identifier, the Companion under the group owner's WiFi MAC.
        /// Each side keys by the stable name it holds for the OTHER device.
        /// </summary>
        public static async Task<string> ConfirmAsync(string peerIdentifier = null)
        {
            FebrisP2pPairingSession session;
            lock (Gate)
            {
                session = _session;
                // Fall back to whatever the exchange captured. The Companion passes its own
                // (the group owner's WiFi MAC); the Server normally relies on the captured one.
                if (string.IsNullOrWhiteSpace(peerIdentifier)) peerIdentifier = _peerIdentifier;
            }

            if (session == null || !session.AwaitingConfirmation) return null;
            if (string.IsNullOrWhiteSpace(peerIdentifier)) { Abort(); return null; }

            P2pPairingSecretStore store = Store;
            if (store == null)
            {
                // Nothing would persist, and reporting success would leave the operator
                // believing a pairing exists that does not survive the next frame.
                Abort();
                return null;
            }

            try
            {
                FebrisP2pPairingSecret secret = session.Confirm();
                await store.SaveAsync(peerIdentifier, secret).ConfigureAwait(false);
                // Returned rather than void so the caller can create the DEVICE RECORD under the
                // same identity the PSK was filed under. Abort() below clears the captured value,
                // so without this the caller has no way to know who it just paired with.
                return peerIdentifier;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                Abort();
            }
        }

        /// <summary>The human said the codes differ, which means someone is in the middle.</summary>
        public static void Reject() { Abort(); }

        /// <summary>Tear the ceremony down and destroy anything derived. Safe to call twice.</summary>
        public static void Abort()
        {
            FebrisP2pPairingSession doomed;
            lock (Gate)
            {
                doomed = _session;
                _session = null;
                _peerIdentifier = null;
            }
            if (doomed != null) doomed.Dispose();
        }

        private static void RaiseCodeReady(string code)
        {
            Action<string> handler = CodeReady;
            if (handler != null && code != null)
            {
                try { handler(code); } catch (Exception) { /* a UI fault must not abort pairing */ }
            }
        }
    }
}
