// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Security.Cryptography;
using System.Text;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// The 256-bit pre-shared key established at pairing time between a Mobile
    /// Server and a Companion device. Established once out-of-band (QR scan,
    /// numeric-code entry, or admin-issued copy) and persisted on both sides
    /// via platform-specific secure storage.
    ///
    /// <para>
    /// <b>Lifecycle:</b> created once per device pair. Re-pairing generates a
    /// new PSK; the old one is destroyed. Suspended / Revoked devices keep
    /// their PSK on file but the runtime rejects handshakes via the
    /// CompanionDevice.Status check (see MDM Tier 2 / Tier 2.2).
    /// </para>
    ///
    /// <para>
    /// <b>What this is NOT:</b> not a session key -- that's derived per session
    /// via HKDF over (PSK + handshake nonces). The PSK never appears in bulk
    /// traffic. Not a password hash -- there's no salt-and-hash because the PSK
    /// is high-entropy random; verification uses HMAC-equality, not password-
    /// equality semantics.
    /// </para>
    ///
    /// <para>
    /// <b>Threat model:</b> rejects a network attacker without the PSK. Does
    /// not protect against an attacker who has obtained the PSK (e.g., disk
    /// extraction from a paired device). Mitigation for that case: PSK rotation
    /// on suspected compromise, which is the Tier 2 device-replacement flow.
    /// </para>
    /// </summary>
    public sealed class FebrisP2pPairingSecret
    {
        /// <summary>The required key size in bytes. 256 bits matches AES-256
        /// and HMAC-SHA256's natural key size; chosen so the same PSK can
        /// directly seed HKDF for any session-key length we need.</summary>
        public const int KeySizeBytes = 32;

        private readonly byte[] _keyBytes;

        /// <summary>Read-only access to the raw key bytes. Returns a defensive
        /// copy to prevent in-place mutation by a caller. Use sparingly --
        /// every reference to the bytes increases the attack surface for an
        /// in-process memory disclosure.</summary>
        public byte[] KeyBytes
        {
            get
            {
                byte[] copy = new byte[KeySizeBytes];
                Buffer.BlockCopy(_keyBytes, 0, copy, 0, KeySizeBytes);
                return copy;
            }
        }

        /// <summary>Short audit-display string: first 16 hex chars of
        /// SHA-256(KeyBytes). Useful in logs / UI ("paired with device X under
        /// PSK 4a3f...") without revealing the PSK itself. The pre-image
        /// resistance of SHA-256 means publishing this fingerprint doesn't
        /// reduce the PSK's security.</summary>
        public string Fingerprint { get; }

        private FebrisP2pPairingSecret(byte[] keyBytes)
        {
            if (keyBytes == null) throw new ArgumentNullException(nameof(keyBytes));
            if (keyBytes.Length != KeySizeBytes)
            {
                throw new ArgumentException(
                    "PSK must be exactly " + KeySizeBytes + " bytes; got " + keyBytes.Length + ".",
                    nameof(keyBytes));
            }

            _keyBytes = new byte[KeySizeBytes];
            Buffer.BlockCopy(keyBytes, 0, _keyBytes, 0, KeySizeBytes);

            // Compute fingerprint once at construction; KeyBytes is immutable.
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(_keyBytes);
                StringBuilder sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
                Fingerprint = sb.ToString();
            }
        }

        /// <summary>Generate a fresh random PSK from the OS CSPRNG. Use this
        /// when initiating a pairing flow (Mobile Server's "Pair new device"
        /// action -- Tier 2.1).</summary>
        public static FebrisP2pPairingSecret Generate()
        {
            byte[] bytes = new byte[KeySizeBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }
            return new FebrisP2pPairingSecret(bytes);
        }

        /// <summary>Construct from raw bytes. Used by deserialization paths
        /// (loading from on-disk persistence). The byte array must be exactly
        /// <see cref="KeySizeBytes"/> bytes; a defensive copy is taken so the
        /// caller can scrub their own array.</summary>
        public static FebrisP2pPairingSecret FromBytes(byte[] keyBytes)
        {
            return new FebrisP2pPairingSecret(keyBytes);
        }

        /// <summary>Base64 string encoding of the PSK. Convenient for QR-code
        /// rendering -- 44 chars for 32 bytes, no padding ambiguity.</summary>
        public string ToBase64String()
        {
            return Convert.ToBase64String(_keyBytes);
        }

        /// <summary>Decode a base64 string back into a PSK. Used by the
        /// Companion-side QR-scan path. Throws <see cref="FormatException"/>
        /// for malformed input; throws <see cref="ArgumentException"/> if the
        /// decoded length isn't <see cref="KeySizeBytes"/>.</summary>
        public static FebrisP2pPairingSecret FromBase64String(string base64)
        {
            if (base64 == null) throw new ArgumentNullException(nameof(base64));
            byte[] bytes = Convert.FromBase64String(base64);
            return new FebrisP2pPairingSecret(bytes);
        }

        /// <summary>Constant-time equality. Use this rather than
        /// <c>SequenceEqual</c> to avoid timing-side-channel leakage if a
        /// caller ever compares PSKs from untrusted sources.</summary>
        public bool Equals(FebrisP2pPairingSecret other)
        {
            if (other == null) return false;
            return ConstantTimeEquals(_keyBytes, other._keyBytes);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FebrisP2pPairingSecret);
        }

        public override int GetHashCode()
        {
            // Fingerprint is already a SHA-256-derived value; use its string
            // hash. Don't expose raw key bytes via GetHashCode.
            return Fingerprint?.GetHashCode() ?? 0;
        }

        /// <summary>Compare two byte arrays in constant time relative to their
        /// length. Returns false immediately on length mismatch; otherwise the
        /// inner loop runs the same number of operations regardless of where
        /// the first difference is.</summary>
        internal static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

        /// <summary>Internal accessor for the handshake state machine.
        /// Returns a defensive copy. Sealed to package-private through the
        /// <see cref="HandshakeInternals"/> friend class.</summary>
        internal byte[] CopyKeyBytesForHmac()
        {
            byte[] copy = new byte[KeySizeBytes];
            Buffer.BlockCopy(_keyBytes, 0, copy, 0, KeySizeBytes);
            return copy;
        }
    }
}
