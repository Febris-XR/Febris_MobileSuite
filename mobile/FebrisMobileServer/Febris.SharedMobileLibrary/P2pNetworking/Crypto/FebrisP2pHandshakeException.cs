// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>Base type for handshake failures. Catch this when you want to
    /// handle "the handshake failed for whatever reason; close the connection
    /// and surface to the operator." Specific subtypes carry more diagnostic
    /// info for the per-companion error counters (MDM Tier 4.3).</summary>
    public class FebrisP2pHandshakeException : Exception
    {
        public FebrisP2pHandshakeException(string message) : base(message) { }
        public FebrisP2pHandshakeException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>The peer's HMAC response didn't match what we computed under
    /// our copy of the PSK. Means one of: peer doesn't have the PSK (impostor),
    /// peer has a stale/wrong PSK (re-pairing needed), or wire-level corruption
    /// of the nonce/response bytes. From the receiver's perspective these are
    /// indistinguishable -- treat as "abort, log, surface to operator."</summary>
    public class HandshakeAuthenticationFailedException : FebrisP2pHandshakeException
    {
        public HandshakeAuthenticationFailedException(string message) : base(message) { }
    }

    /// <summary>The handshake state machine was advanced out of order -- e.g.,
    /// caller invoked <c>ProcessServerHello</c> before <c>StartHandshake</c>,
    /// or invoked the same step twice. Indicates a caller bug, not a wire
    /// issue.</summary>
    public class HandshakeStateException : FebrisP2pHandshakeException
    {
        public HandshakeStateException(string message) : base(message) { }
    }

    /// <summary>A handshake-stage payload (nonce, response, final) was the
    /// wrong size. Indicates wire corruption or a protocol mismatch.</summary>
    public class HandshakePayloadSizeException : FebrisP2pHandshakeException
    {
        public HandshakePayloadSizeException(string message) : base(message) { }
    }
}
