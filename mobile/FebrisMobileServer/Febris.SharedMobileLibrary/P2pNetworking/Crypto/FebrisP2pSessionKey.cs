// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// 256-bit symmetric session key derived from the pre-shared key + handshake
    /// nonces via HKDF-SHA256. Used to authenticate-and-encrypt the body of every
    /// frame exchanged during a single connection.
    ///
    /// <para>
    /// <b>Lifecycle:</b> one session key per connection. A new connection (new
    /// socket, after disconnect / reconnect) runs the full handshake from
    /// scratch and produces a fresh session key. The PSK is the long-lived
    /// secret; the session key is short-lived.
    /// </para>
    ///
    /// <para>
    /// <b>Why a separate type, not just <c>byte[]</c>:</b> wrapping the bytes
    /// gives a single place to enforce "256-bit length" + a single audit
    /// surface for "what code can read these bytes." A bare <c>byte[]</c>
    /// passed around is harder to grep for.
    /// </para>
    /// </summary>
    public sealed class FebrisP2pSessionKey
    {
        /// <summary>Required key size in bytes -- 256 bits, matching AES-256 and
        /// HMAC-SHA256.</summary>
        public const int KeySizeBytes = 32;

        private readonly byte[] _keyBytes;

        /// <summary>Read-only access to the raw key bytes. Returns a defensive
        /// copy. The MP2P-3 AES-GCM wrapper (follow-on commit) is the intended
        /// consumer; outside that path the bytes shouldn't leave this wrapper.</summary>
        public byte[] KeyBytes
        {
            get
            {
                byte[] copy = new byte[KeySizeBytes];
                Buffer.BlockCopy(_keyBytes, 0, copy, 0, KeySizeBytes);
                return copy;
            }
        }

        internal FebrisP2pSessionKey(byte[] keyBytes)
        {
            if (keyBytes == null) throw new ArgumentNullException(nameof(keyBytes));
            if (keyBytes.Length != KeySizeBytes)
            {
                throw new ArgumentException(
                    "Session key must be exactly " + KeySizeBytes + " bytes; got " + keyBytes.Length + ".",
                    nameof(keyBytes));
            }

            _keyBytes = new byte[KeySizeBytes];
            Buffer.BlockCopy(keyBytes, 0, _keyBytes, 0, KeySizeBytes);
        }
    }
}
