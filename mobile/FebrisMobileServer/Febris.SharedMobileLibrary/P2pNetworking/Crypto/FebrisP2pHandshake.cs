// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Security.Cryptography;
using System.Text;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// Three-way HMAC challenge-response handshake protocol between Companion
    /// (initiator / "client") and Mobile Server (responder / "server").
    /// Establishes a per-session symmetric key, with mutual authentication
    /// proving both peers hold the same PSK.
    ///
    /// <para>
    /// <b>Protocol (see also: docs/AUDIT_MOBILE_P2P.md section 2, MDM Tier 1 MP2P-3):</b>
    /// <code>
    /// Client                              Server
    /// ------                              ------
    /// 1. Generate client_nonce (16 bytes random)
    ///    Send: client_nonce         --&gt;
    ///
    ///                                     2. Receive client_nonce
    ///                                        Generate server_nonce (16 bytes)
    ///                                        transcript = client_nonce || server_nonce
    ///                                        server_response =
    ///                                          HMAC-SHA256(PSK,
    ///                                                      ServerResponseTag || transcript)
    ///                              &lt;--      Send: server_nonce, server_response
    ///
    /// 3. Verify server_response under PSK, over the SAME transcript.
    ///    If invalid -&gt; HandshakeAuthenticationFailedException, abort.
    ///    client_final =
    ///      HMAC-SHA256(PSK, ClientFinalTag || transcript)
    ///    Send: client_final          --&gt;
    ///    Derive session_key =
    ///      HKDF-SHA256(PSK,
    ///                  salt=client_nonce || server_nonce,
    ///                  info=SessionInfoTag,
    ///                  out=32 bytes)
    ///
    ///                                     4. Receive client_final
    ///                                        Verify client_final under PSK.
    ///                                        If invalid -&gt; abort.
    ///                                        Derive session_key (same formula).
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <b>What this gives us:</b>
    /// <list type="bullet">
    ///   <item>Mutual authentication: both peers prove PSK possession.</item>
    ///   <item>Per-session key: bulk traffic never uses the PSK directly.</item>
    ///   <item>Replay defense (within a session): nonces are random per
    ///         connection; the same handshake transcript can't be replayed
    ///         to produce the same session key.</item>
    ///   <item>Domain-separated HMACs: a server-side HMAC can never be
    ///         replayed as a client-side HMAC because the input tag differs.
    ///         Both MACs now cover the identical transcript, so the tags are the
    ///         ONLY thing keeping them apart and are strictly load-bearing.</item>
    ///   <item>Transcript binding: both MACs cover client_nonce || server_nonce,
    ///         so neither side can be steered onto a nonce the other did not
    ///         commit to. Before this, each MAC covered only the other side's
    ///         nonce, and an altered server_nonce was undetectable by the client
    ///         until the responder rejected client_final a round trip later.</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <b>Residual asymmetry, which transcript binding does NOT remove:</b> the third
    /// message is unacknowledged, so the initiator still cannot learn whether the
    /// responder accepted its client_final. "Proven" on the initiator means the peer
    /// demonstrated PSK possession, not that a session was established.
    /// </para>
    ///
    /// <para>
    /// <b>What this does NOT give us:</b>
    /// <list type="bullet">
    ///   <item>Forward secrecy. If the PSK is later compromised AND the
    ///         attacker has captured the handshake nonces of a past session,
    ///         they can recompute the session key and decrypt past traffic.
    ///         Mitigation: rotate the PSK on suspected compromise (Tier 2
    ///         device-replacement flow).</item>
    ///   <item>PAKE-style protection against weak PSKs. The PSK is high-
    ///         entropy random by construction (<see cref="FebrisP2pPairingSecret.Generate"/>);
    ///         dictionary attacks are not in scope.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class FebrisP2pHandshakeTags
    {
        /// <summary>Domain-separation tag for the server's HMAC over the full
        /// transcript. Ensures a server's response can't be re-presented as a
        /// client's final.
        ///
        /// The <c>-v2</c> suffix marks the transcript-binding change: the v1 tags
        /// were MACed over a single nonce, so a peer still running v1 computes a
        /// different value and fails on MAC mismatch rather than silently
        /// interoperating under the weaker binding.</summary>
        public static readonly byte[] ServerResponseTag = Encoding.ASCII.GetBytes("febris-p2p-server-response-v2");

        /// <summary>Domain-separation tag for the client's HMAC over the full
        /// transcript. See <see cref="ServerResponseTag"/> for the -v2 suffix.</summary>
        public static readonly byte[] ClientFinalTag = Encoding.ASCII.GetBytes("febris-p2p-client-final-v2");

        /// <summary>HKDF info parameter for session-key derivation.</summary>
        public static readonly byte[] SessionInfoTag = Encoding.ASCII.GetBytes("febris-p2p-session-v1");

        /// <summary>Required nonce length. 16 bytes = 128 bits -- sufficient
        /// against birthday collisions for our session lifetime.</summary>
        public const int NonceSizeBytes = 16;

        /// <summary>HMAC-SHA256 output size.</summary>
        public const int HmacSizeBytes = 32;
    }

    /// <summary>Internal helpers shared between Client and Server handshake
    /// state machines. Not public -- exposes the HMAC primitive without the
    /// state-machine guardrails.</summary>
    internal static class HandshakeInternals
    {
        public static byte[] GenerateNonce()
        {
            byte[] nonce = new byte[FebrisP2pHandshakeTags.NonceSizeBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonce);
            }
            return nonce;
        }

        /// <summary>Compute HMAC-SHA256(PSK, tag || transcript). Both MACs cover the
        /// SAME transcript and are separated only by their tag, which is what makes
        /// the tags load-bearing rather than decorative.</summary>
        public static byte[] ComputeTaggedHmac(FebrisP2pPairingSecret psk, byte[] tag, byte[] transcript)
        {
            byte[] pskBytes = psk.CopyKeyBytesForHmac();
            try
            {
                byte[] input = new byte[tag.Length + transcript.Length];
                Buffer.BlockCopy(tag, 0, input, 0, tag.Length);
                Buffer.BlockCopy(transcript, 0, input, tag.Length, transcript.Length);

                using (var hmac = new HMACSHA256(pskBytes))
                {
                    return hmac.ComputeHash(input);
                }
            }
            finally
            {
                Array.Clear(pskBytes, 0, pskBytes.Length);
            }
        }

        /// <summary>
        /// The handshake transcript both MACs are computed over: client_nonce || server_nonce.
        ///
        /// WHY BOTH NONCES. Each MAC used to cover only the OTHER side's nonce, which left
        /// the transcript unbound: an on-path attacker could alter the server_nonce and the
        /// client could not tell, because server_response was a function of client_nonce
        /// alone. Detection slipped a full round trip to the responder, and the initiator
        /// spent that window believing it held a usable session key.
        ///
        /// Concatenation is unambiguous here ONLY because both nonces are fixed at
        /// <see cref="FebrisP2pHandshakeTags.NonceSizeBytes"/> and every entry point rejects
        /// any other length before reaching this method. Do not relax those length checks
        /// without adding explicit length framing, or the concatenation becomes
        /// canonicalization-ambiguous.
        /// </summary>
        public static byte[] BuildTranscript(byte[] clientNonce, byte[] serverNonce)
        {
            byte[] transcript = new byte[clientNonce.Length + serverNonce.Length];
            Buffer.BlockCopy(clientNonce, 0, transcript, 0, clientNonce.Length);
            Buffer.BlockCopy(serverNonce, 0, transcript, clientNonce.Length, serverNonce.Length);
            return transcript;
        }

        public static FebrisP2pSessionKey DeriveSessionKey(
            FebrisP2pPairingSecret psk,
            byte[] clientNonce,
            byte[] serverNonce)
        {
            // Salt = client_nonce || server_nonce. Mixing both makes the
            // session key depend on the full handshake transcript so a passive
            // attacker who knows the PSK still can't predict the session key
            // without seeing the nonces.
            byte[] salt = new byte[clientNonce.Length + serverNonce.Length];
            Buffer.BlockCopy(clientNonce, 0, salt, 0, clientNonce.Length);
            Buffer.BlockCopy(serverNonce, 0, salt, clientNonce.Length, serverNonce.Length);

            byte[] pskBytes = psk.CopyKeyBytesForHmac();
            try
            {
                byte[] derived = HkdfSha256.Derive(
                    ikm: pskBytes,
                    salt: salt,
                    info: FebrisP2pHandshakeTags.SessionInfoTag,
                    outputLength: FebrisP2pSessionKey.KeySizeBytes);
                return new FebrisP2pSessionKey(derived);
            }
            finally
            {
                Array.Clear(pskBytes, 0, pskBytes.Length);
                Array.Clear(salt, 0, salt.Length);
            }
        }
    }

    // NOTE (MDM-B5): Crypto primitives shipped but unwired (this handshake class has zero live callers outside tests), so all P2P frames remain plaintext. Tier 2 integration required for handshake and AES-GCM body wrap. Deferred per do-not-change-functionality. See docs/MODERNIZATION/MDM_MODERNIZATION.md.
    /// <summary>Client-side (initiator / Companion) state machine for the
    /// three-way handshake.</summary>
    public sealed class FebrisP2pClientHandshake
    {
        private enum State { Initial, AwaitingServerHello, AwaitingSessionKeyDerivation, Complete, Aborted }

        private readonly FebrisP2pPairingSecret _psk;
        private State _state;
        private byte[] _clientNonce;
        private byte[] _serverNonce;
        private FebrisP2pSessionKey _sessionKey;

        public FebrisP2pClientHandshake(FebrisP2pPairingSecret psk)
        {
            _psk = psk ?? throw new ArgumentNullException(nameof(psk));
            _state = State.Initial;
        }

        /// <summary>Step 1: generate the client_nonce and return the bytes to
        /// send to the server. After this call, the state machine expects
        /// <see cref="ProcessServerHello"/> next.</summary>
        public byte[] StartHandshake()
        {
            if (_state != State.Initial)
            {
                throw new HandshakeStateException(
                    "StartHandshake called from state " + _state + "; expected Initial.");
            }

            _clientNonce = HandshakeInternals.GenerateNonce();
            _state = State.AwaitingServerHello;
            // Return a defensive copy so the caller can't mutate our state.
            byte[] copy = new byte[_clientNonce.Length];
            Buffer.BlockCopy(_clientNonce, 0, copy, 0, _clientNonce.Length);
            return copy;
        }

        /// <summary>Step 3: process the server's response. Verifies the HMAC
        /// under the PSK; if it matches, computes the client_final to send and
        /// derives the session key. If verification fails,
        /// <see cref="HandshakeAuthenticationFailedException"/> is thrown and
        /// the state machine is aborted.</summary>
        /// <returns>The client_final bytes to send to the server (HMAC-SHA256
        /// over server_nonce under PSK with the ClientFinalTag).</returns>
        public byte[] ProcessServerHello(byte[] serverNonce, byte[] serverResponse)
        {
            if (_state != State.AwaitingServerHello)
            {
                throw new HandshakeStateException(
                    "ProcessServerHello called from state " + _state + "; expected AwaitingServerHello.");
            }
            if (serverNonce == null) throw new ArgumentNullException(nameof(serverNonce));
            if (serverResponse == null) throw new ArgumentNullException(nameof(serverResponse));

            if (serverNonce.Length != FebrisP2pHandshakeTags.NonceSizeBytes)
            {
                _state = State.Aborted;
                throw new HandshakePayloadSizeException(
                    "server_nonce must be exactly " + FebrisP2pHandshakeTags.NonceSizeBytes +
                    " bytes; got " + serverNonce.Length + ".");
            }
            if (serverResponse.Length != FebrisP2pHandshakeTags.HmacSizeBytes)
            {
                _state = State.Aborted;
                throw new HandshakePayloadSizeException(
                    "server_response must be exactly " + FebrisP2pHandshakeTags.HmacSizeBytes +
                    " bytes; got " + serverResponse.Length + ".");
            }

            // Bind the server nonce BEFORE verifying, because the MAC now covers it.
            // This is the whole point of the transcript fix: a tampered server_nonce
            // changes the expected value here, so the initiator detects it immediately
            // instead of a round trip later on the responder.
            _serverNonce = new byte[serverNonce.Length];
            Buffer.BlockCopy(serverNonce, 0, _serverNonce, 0, serverNonce.Length);

            byte[] transcript = HandshakeInternals.BuildTranscript(_clientNonce, _serverNonce);

            // Verify HMAC(PSK, ServerResponseTag || client_nonce || server_nonce).
            byte[] expected = HandshakeInternals.ComputeTaggedHmac(
                _psk,
                FebrisP2pHandshakeTags.ServerResponseTag,
                transcript);

            bool valid = FebrisP2pPairingSecret.ConstantTimeEquals(expected, serverResponse);
            Array.Clear(expected, 0, expected.Length);

            if (!valid)
            {
                _state = State.Aborted;
                _serverNonce = null;
                throw new HandshakeAuthenticationFailedException(
                    "Server's HMAC over the handshake transcript does not match. Possible causes: " +
                    "peer doesn't have the same PSK (impostor); peer has a stale PSK (re-pair " +
                    "required); the server_nonce was altered in flight; wire-level corruption of " +
                    "server_response bytes.");
            }

            // Compute client_final = HMAC(PSK, ClientFinalTag || client_nonce || server_nonce).
            // Same transcript, different tag: the tags are what keep the two MACs from
            // being interchangeable now that their input is identical.
            byte[] clientFinal = HandshakeInternals.ComputeTaggedHmac(
                _psk,
                FebrisP2pHandshakeTags.ClientFinalTag,
                transcript);

            _state = State.AwaitingSessionKeyDerivation;
            return clientFinal;
        }

        /// <summary>Step 4 (client): after sending client_final, both sides
        /// independently derive the session key. Throws if called before the
        /// handshake reaches a state where the key can be computed.</summary>
        public FebrisP2pSessionKey GetSessionKey()
        {
            if (_state != State.AwaitingSessionKeyDerivation && _state != State.Complete)
            {
                throw new HandshakeStateException(
                    "GetSessionKey called from state " + _state +
                    "; expected AwaitingSessionKeyDerivation or Complete.");
            }

            if (_sessionKey == null)
            {
                _sessionKey = HandshakeInternals.DeriveSessionKey(_psk, _clientNonce, _serverNonce);
                _state = State.Complete;
            }
            return _sessionKey;
        }
    }

    /// <summary>Server-side (responder / Mobile Server) state machine for the
    /// three-way handshake.</summary>
    public sealed class FebrisP2pServerHandshake
    {
        private enum State { Initial, AwaitingClientFinal, AwaitingSessionKeyDerivation, Complete, Aborted }

        private readonly FebrisP2pPairingSecret _psk;
        private State _state;
        private byte[] _clientNonce;
        private byte[] _serverNonce;
        private FebrisP2pSessionKey _sessionKey;

        public FebrisP2pServerHandshake(FebrisP2pPairingSecret psk)
        {
            _psk = psk ?? throw new ArgumentNullException(nameof(psk));
            _state = State.Initial;
        }

        /// <summary>Step 2: process the client's hello. Generates server_nonce
        /// and computes server_response = HMAC(PSK, ServerResponseTag ||
        /// client_nonce). Returns both for transmission back to the client.</summary>
        public (byte[] ServerNonce, byte[] ServerResponse) ProcessClientHello(byte[] clientNonce)
        {
            if (_state != State.Initial)
            {
                throw new HandshakeStateException(
                    "ProcessClientHello called from state " + _state + "; expected Initial.");
            }
            if (clientNonce == null) throw new ArgumentNullException(nameof(clientNonce));
            if (clientNonce.Length != FebrisP2pHandshakeTags.NonceSizeBytes)
            {
                _state = State.Aborted;
                throw new HandshakePayloadSizeException(
                    "client_nonce must be exactly " + FebrisP2pHandshakeTags.NonceSizeBytes +
                    " bytes; got " + clientNonce.Length + ".");
            }

            _clientNonce = new byte[clientNonce.Length];
            Buffer.BlockCopy(clientNonce, 0, _clientNonce, 0, clientNonce.Length);

            _serverNonce = HandshakeInternals.GenerateNonce();

            // MAC over the full transcript, so the responder commits to its own nonce
            // as well as the client's.
            byte[] serverResponse = HandshakeInternals.ComputeTaggedHmac(
                _psk,
                FebrisP2pHandshakeTags.ServerResponseTag,
                HandshakeInternals.BuildTranscript(_clientNonce, _serverNonce));

            _state = State.AwaitingClientFinal;

            byte[] serverNonceCopy = new byte[_serverNonce.Length];
            Buffer.BlockCopy(_serverNonce, 0, serverNonceCopy, 0, _serverNonce.Length);
            return (serverNonceCopy, serverResponse);
        }

        /// <summary>Step 4 (server): verify the client_final. If valid, the
        /// handshake completes and <see cref="GetSessionKey"/> returns the
        /// derived key. If invalid,
        /// <see cref="HandshakeAuthenticationFailedException"/> is thrown.</summary>
        public void ProcessClientFinal(byte[] clientFinal)
        {
            if (_state != State.AwaitingClientFinal)
            {
                throw new HandshakeStateException(
                    "ProcessClientFinal called from state " + _state + "; expected AwaitingClientFinal.");
            }
            if (clientFinal == null) throw new ArgumentNullException(nameof(clientFinal));
            if (clientFinal.Length != FebrisP2pHandshakeTags.HmacSizeBytes)
            {
                _state = State.Aborted;
                throw new HandshakePayloadSizeException(
                    "client_final must be exactly " + FebrisP2pHandshakeTags.HmacSizeBytes +
                    " bytes; got " + clientFinal.Length + ".");
            }

            byte[] expected = HandshakeInternals.ComputeTaggedHmac(
                _psk,
                FebrisP2pHandshakeTags.ClientFinalTag,
                HandshakeInternals.BuildTranscript(_clientNonce, _serverNonce));

            bool valid = FebrisP2pPairingSecret.ConstantTimeEquals(expected, clientFinal);
            Array.Clear(expected, 0, expected.Length);

            if (!valid)
            {
                _state = State.Aborted;
                throw new HandshakeAuthenticationFailedException(
                    "Client's HMAC over the handshake transcript does not match. See " +
                    "ClientHandshake docs for diagnostic categories.");
            }

            _state = State.AwaitingSessionKeyDerivation;
        }

        /// <summary>Step 5 (server): after verifying client_final, both sides
        /// independently derive the session key.</summary>
        public FebrisP2pSessionKey GetSessionKey()
        {
            if (_state != State.AwaitingSessionKeyDerivation && _state != State.Complete)
            {
                throw new HandshakeStateException(
                    "GetSessionKey called from state " + _state +
                    "; expected AwaitingSessionKeyDerivation or Complete.");
            }

            if (_sessionKey == null)
            {
                _sessionKey = HandshakeInternals.DeriveSessionKey(_psk, _clientNonce, _serverNonce);
                _state = State.Complete;
            }
            return _sessionKey;
        }
    }
}
