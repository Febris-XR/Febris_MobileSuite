// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Security.Cryptography;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// HKDF-SHA256 implementation per RFC 5869, "HMAC-based Extract-and-Expand
    /// Key Derivation Function." netstandard2.0 lacks <c>System.Security.Cryptography.HKDF</c>
    /// (that arrived in netcoreapp3.0), so we implement it manually over the
    /// <c>HMACSHA256</c> primitive that has been in the base library since
    /// forever.
    ///
    /// <para>
    /// HKDF is used to derive a session key from the pre-shared key + handshake
    /// nonces. The pattern is canonical for symmetric-key cryptography: a long-
    /// lived secret is never used directly to encrypt bulk traffic; instead it
    /// seeds a per-session key via a deterministic-but-context-dependent
    /// derivation. This protects the long-lived secret from cryptanalysis of
    /// the bulk-traffic ciphertext.
    /// </para>
    ///
    /// <para>
    /// Validated against RFC 5869 Appendix A test vectors in
    /// <c>HkdfSha256Tests</c>.
    /// </para>
    /// </summary>
    internal static class HkdfSha256
    {
        private const int HashLengthBytes = 32; // SHA-256 = 32 bytes

        /// <summary>RFC 5869 section 2.3 -- Expand stage. Input keying material (IKM)
        /// is the long-lived secret; salt is optional context-binding bytes
        /// (we use the handshake nonces); info is a domain-separation label;
        /// outputLength is how many bytes of derived key material to produce.</summary>
        /// <exception cref="ArgumentNullException">ikm or info is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">outputLength is &lt;= 0
        /// or exceeds 255 * 32 (the RFC 5869 section 2.3 hard cap).</exception>
        public static byte[] Derive(byte[] ikm, byte[] salt, byte[] info, int outputLength)
        {
            if (ikm == null) throw new ArgumentNullException(nameof(ikm));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (outputLength <= 0) throw new ArgumentOutOfRangeException(nameof(outputLength), "outputLength must be > 0.");
            if (outputLength > 255 * HashLengthBytes)
            {
                // RFC 5869 section 2.3: output limited to 255 * HashLen bytes.
                throw new ArgumentOutOfRangeException(
                    nameof(outputLength),
                    "outputLength must not exceed 255 * HashLen = " + (255 * HashLengthBytes) + " bytes.");
            }

            // Extract: PRK = HMAC-SHA256(salt, IKM). RFC 5869 section 2.2.
            // Per spec, if salt is null or empty it's treated as HashLen zeros.
            byte[] saltOrZeros = (salt == null || salt.Length == 0) ? new byte[HashLengthBytes] : salt;
            byte[] prk;
            using (var extractHmac = new HMACSHA256(saltOrZeros))
            {
                prk = extractHmac.ComputeHash(ikm);
            }

            // Expand: T(1) = HMAC(PRK, info || 0x01); T(i) = HMAC(PRK, T(i-1) || info || i).
            // RFC 5869 section 2.3. Concatenate T(1)..T(N) and truncate to outputLength.
            byte[] output = new byte[outputLength];
            byte[] previousBlock = Array.Empty<byte>();
            int written = 0;
            byte counter = 0;

            using (var expandHmac = new HMACSHA256(prk))
            {
                while (written < outputLength)
                {
                    counter++;
                    // Build T(i) input: previousBlock || info || counter
                    byte[] hmacInput = new byte[previousBlock.Length + info.Length + 1];
                    Buffer.BlockCopy(previousBlock, 0, hmacInput, 0, previousBlock.Length);
                    Buffer.BlockCopy(info, 0, hmacInput, previousBlock.Length, info.Length);
                    hmacInput[hmacInput.Length - 1] = counter;

                    byte[] block = expandHmac.ComputeHash(hmacInput);

                    int copyCount = Math.Min(block.Length, outputLength - written);
                    Buffer.BlockCopy(block, 0, output, written, copyCount);
                    written += copyCount;
                    previousBlock = block;

                    // Best-effort scrub of intermediate HMAC input. netstandard2.0
                    // lacks CryptographicOperations.ZeroMemory; Array.Clear is the
                    // closest portable equivalent.
                    Array.Clear(hmacInput, 0, hmacInput.Length);
                }
            }

            // Scrub intermediate state.
            Array.Clear(prk, 0, prk.Length);
            Array.Clear(previousBlock, 0, previousBlock.Length);

            return output;
        }
    }
}
