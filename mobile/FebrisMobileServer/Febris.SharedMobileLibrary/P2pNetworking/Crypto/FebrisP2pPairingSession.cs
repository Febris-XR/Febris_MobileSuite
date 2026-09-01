// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

// Aliased rather than imported wholesale: System.Security.Cryptography also declares an ECPoint,
// and it is still imported here for SHA256. An ambiguous reference is a build error, but the worse
// outcome would be a future edit resolving it to the wrong one, since both are plausible in this
// file.
using BcECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// Numeric-comparison pairing: establishes a per-pair PSK over an untrusted channel, with a
    /// human confirming a short code on both screens.
    ///
    /// <para><b>THE PROBLEM THIS SOLVES.</b> Everything downstream of the PSK was already written
    /// and none of it could run, because nothing put a PSK on two devices. The pre-existing
    /// alternative anchors device identity on the Bluetooth display name, which is user-editable,
    /// collides between two phones of the same model, and can be set by anyone to impersonate a
    /// paired device. See docs/MOBILE_AUTH.md 4.1.</para>
    ///
    /// <para><b>HOW IT WORKS.</b> Ephemeral ECDH on P-256. Each side generates a throwaway key
    /// pair, sends its public key, and derives a shared secret. The PSK is HKDF over that secret.
    /// Both sides then derive a six-digit code from the FULL transcript, meaning both public keys
    /// in a fixed order plus the shared secret, and display it. A human confirms the two screens
    /// match.</para>
    ///
    /// <para><b>WHY BOUNCYCASTLE AND NOT <c>ECDiffieHellman</c>.</b> Mono/Android does not
    /// implement <c>ECDiffieHellman</c>. That is a device measurement, not a suspicion: the
    /// ceremony threw on the first call and the pairing dialog rendered its "(INSECURE TEST)"
    /// title, which only appeared when the real path failed. The API had been "verified" by
    /// COMPILING against netstandard2.1, which proves the reference assembly exposes a member and
    /// says nothing about whether the runtime implements it. BouncyCastle is pure managed code, so
    /// it has no equivalent failure mode. See docs/MOBILE_KNOWN_ISSUES.md issue 2.
    ///
    /// <b>Nothing observable changed in the port.</b> The wire format is still the 64-byte raw
    /// P-256 public key, and the shared secret is still SHA-256 over the agreed x-coordinate
    /// left-padded to the field size, which is exactly what <c>DeriveKeyFromHash</c> computed. The
    /// transcript, both HKDF info strings and the code derivation are untouched, so the protocol
    /// this speaks is the one the previous implementation was designed to speak.</para>
    ///
    /// <para><b>WHY IT IS SECURE, AND AGAINST WHAT.</b>
    /// <list type="bullet">
    ///   <item><b>Passive eavesdropper:</b> sees two public keys and nothing else. Deriving the
    ///   shared secret from those is the elliptic-curve Diffie-Hellman problem. This is exactly why
    ///   a simpler "generate a PSK and send it, then compare fingerprints" design was rejected: it
    ///   would hand the key to anyone listening, and WiFi Direct is a broadcast medium.</item>
    ///   <item><b>Active man in the middle:</b> must run TWO exchanges, one with each side, so the
    ///   two sides end up with different transcripts and therefore DIFFERENT six-digit codes. The
    ///   human sees a mismatch and refuses. This is the property the whole design exists for, and
    ///   it is why the code is derived from the transcript rather than being a shared password or
    ///   a fingerprint of one side's key.</item>
    ///   <item><b>Brute force of the code:</b> an attacker who guesses gets one attempt per pairing
    ///   ceremony, because the ephemeral keys are discarded on failure. Six digits is 1 in a million
    ///   per attempt with no retry, which is the same strength Bluetooth Secure Simple Pairing
    ///   settles on for the same reason.</item>
    /// </list></para>
    ///
    /// <para><b>WHAT IT DOES NOT DO.</b> It does not authenticate WHICH physical device is on the
    /// other end. It guarantees the two endpoints displaying matching codes share a key and that
    /// nobody is between them. Confirming the right two devices are being paired is the human's
    /// job, which is the entire point of an out-of-band check.</para>
    ///
    /// <para>Single use. A session performs one ceremony and is then spent, so a failed pairing
    /// cannot be retried against the same ephemeral keys.</para>
    /// </summary>
    public sealed class FebrisP2pPairingSession : IDisposable
    {
        /// <summary>Digits in the comparison code. Six matches Bluetooth Secure Simple Pairing:
        /// long enough that a single blind guess is 1 in a million, short enough to read aloud
        /// and compare without error.</summary>
        public const int CodeDigits = 6;

        /// <summary>Domain separation for the PSK derivation.</summary>
        private static readonly byte[] PskInfo = Encoding.ASCII.GetBytes("febris-p2p-pairing-psk-v1");

        /// <summary>Domain separation for the displayed code. MUST differ from
        /// <see cref="PskInfo"/>: the code is shown on screen and photographed, so it must leak
        /// nothing about the key derived from the same secret.</summary>
        private static readonly byte[] CodeInfo = Encoding.ASCII.GetBytes("febris-p2p-pairing-code-v1");

        /// <summary>
        /// P-256, resolved once. Held as the domain rather than re-fetched per session because
        /// every ceremony uses the same curve and the lookup is a table search.
        /// </summary>
        private static readonly ECDomainParameters Domain = BuildDomain();

        private static ECDomainParameters BuildDomain()
        {
            // "secp256r1" is the SEC name for the curve NIST calls P-256 and X9.62 calls
            // prime256v1. All three resolve in this table; the SEC spelling is used because it is
            // the one that cannot be confused with a different registry's P-256.
            X9ECParameters curve = ECNamedCurveTable.GetByName("secp256r1");
            return new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());
        }

        private ECPrivateKeyParameters _ephemeralPrivate;
        private byte[] _ourPublic;
        private byte[] _theirPublic;
        private byte[] _psk;
        private string _code;
        private bool _spent;

        /// <summary>
        /// True for the side that ORDERS the transcript first. Both sides must agree, or they
        /// concatenate the two public keys in opposite orders and compute different codes for an
        /// honest exchange. The Server is the initiator by convention, matching its responder role
        /// in the handshake, and the roles are asserted rather than negotiated so a mismatch is a
        /// programming error rather than a runtime surprise.
        /// </summary>
        public bool IsInitiator { get; }

        public FebrisP2pPairingSession(bool isInitiator)
        {
            IsInitiator = isInitiator;

            // No try/catch. A platform that cannot generate a P-256 key pair cannot pair at all,
            // and the previous fallback-to-insecure-stub behaviour is exactly what let a ceremony
            // with NO man-in-the-middle resistance masquerade as a real one. Failing loudly here
            // is the correct outcome: the caller aborts the ceremony and nothing is persisted.
            var generator = new ECKeyPairGenerator("ECDH");
            generator.Init(new ECKeyGenerationParameters(Domain, new SecureRandom()));
            AsymmetricCipherKeyPair pair = generator.GenerateKeyPair();

            _ephemeralPrivate = (ECPrivateKeyParameters)pair.Private;
            _ourPublic = ExportRawPublicKey((ECPublicKeyParameters)pair.Public);
        }

        /// <summary>P-256 public key on the wire: the raw affine coordinates, X then Y, 32 bytes
        /// each. Chosen over an ASN.1 encoding because the length is then fixed and checkable.
        /// Unchanged by the BouncyCastle port, deliberately, so the protocol is untouched.</summary>
        internal const int CoordinateBytes = 32;
        internal const int RawPublicKeyBytes = CoordinateBytes * 2;

        /// <summary>The uncompressed-point prefix from SEC 1, which is what X||Y is missing.
        /// Added on import and stripped on export so the wire stays 64 bytes.</summary>
        private const byte UncompressedPointPrefix = 0x04;

        private static byte[] ExportRawPublicKey(ECPublicKeyParameters key)
        {
            // Normalize first: an un-normalized point carries projective coordinates, and reading
            // Affine* without it either throws or yields the wrong value depending on the curve
            // implementation.
            BcECPoint q = key.Q.Normalize();

            byte[] raw = new byte[RawPublicKeyBytes];
            // AsUnsignedByteArray left-pads to exactly CoordinateBytes. That padding is
            // load-bearing: a coordinate with leading zero bytes is naturally shorter, and copying
            // it to offset zero would shift the value and break agreement roughly 1 time in 256.
            Buffer.BlockCopy(BigIntegers.AsUnsignedByteArray(CoordinateBytes, q.AffineXCoord.ToBigInteger()),
                             0, raw, 0, CoordinateBytes);
            Buffer.BlockCopy(BigIntegers.AsUnsignedByteArray(CoordinateBytes, q.AffineYCoord.ToBigInteger()),
                             0, raw, CoordinateBytes, CoordinateBytes);
            return raw;
        }

        /// <summary>
        /// Rebuild a peer's point from the 64-byte wire form, validating it belongs on the curve.
        /// Throws on anything it will not accept, and every caller treats a throw as a refusal.
        /// </summary>
        private static ECPublicKeyParameters ImportRawPublicKey(byte[] raw)
        {
            byte[] encoded = new byte[1 + RawPublicKeyBytes];
            encoded[0] = UncompressedPointPrefix;
            Buffer.BlockCopy(raw, 0, encoded, 1, RawPublicKeyBytes);

            // DecodePoint validates the point satisfies the curve equation, which is the check
            // that stops an invalid-curve attack from recovering the private key one derivation at
            // a time. It must never be relaxed to a bare coordinate copy.
            BcECPoint point = Domain.Curve.DecodePoint(encoded);

            // Belt and braces over DecodePoint: the point at infinity has no affine coordinates and
            // would agree to a degenerate value, and IsValid additionally checks the point lies in
            // the correct subgroup.
            if (point == null || point.IsInfinity || !point.IsValid())
            {
                throw new ArgumentException("Peer public key is not a valid P-256 point.");
            }

            // The constructor validates the public point against the domain a second time. Cheap,
            // and it means a future change to either check alone cannot silently disable both.
            return new ECPublicKeyParameters(point, Domain);
        }

        /// <summary>Our ephemeral public key, to send to the peer. Safe to transmit in the clear.</summary>
        public byte[] PublicKey
        {
            get
            {
                byte[] copy = new byte[_ourPublic.Length];
                Buffer.BlockCopy(_ourPublic, 0, copy, 0, _ourPublic.Length);
                return copy;
            }
        }

        /// <summary>The six-digit code to display. Null until <see cref="ProcessPeerKey"/> succeeds.</summary>
        public string ComparisonCode { get { return _code; } }

        /// <summary>True once a code is available and the human can be asked.</summary>
        public bool AwaitingConfirmation { get { return _code != null && !_spent; } }

        /// <summary>
        /// Take the peer's public key, derive the shared secret, and compute both the PSK and the
        /// comparison code. Neither is released until the human confirms.
        /// </summary>
        /// <returns>False when the key is missing or malformed, which is a refusal rather than an
        /// exception because a peer can send anything.</returns>
        public bool ProcessPeerKey(byte[] peerPublicKey)
        {
            if (_spent) throw new InvalidOperationException("This pairing session has already been used.");
            if (_code != null) throw new InvalidOperationException("Peer key already processed.");
            if (peerPublicKey == null || peerPublicKey.Length == 0) return false;

            // Fixed length is itself a check: the only thing we accept is a P-256 point, so a
            // peer cannot steer us onto a different curve by sending a different encoding.
            if (peerPublicKey.Length != RawPublicKeyBytes) return false;

            try
            {
                ECPublicKeyParameters peer = ImportRawPublicKey(peerPublicKey);

                var agreement = new ECDHBasicAgreement();
                agreement.Init(_ephemeralPrivate);

                // CalculateAgreement returns the x-coordinate of the shared point. Hashing it,
                // rather than using it raw, reproduces ECDiffieHellman.DeriveKeyFromHash exactly
                // and is also correct on its own terms: a raw coordinate is not uniformly
                // distributed and is not safe to use directly as key material.
                byte[] agreedX = BigIntegers.AsUnsignedByteArray(
                    CoordinateBytes, agreement.CalculateAgreement(peer));

                byte[] shared;
                try
                {
                    using (var sha = SHA256.Create()) { shared = sha.ComputeHash(agreedX); }
                }
                finally
                {
                    Array.Clear(agreedX, 0, agreedX.Length);
                }

                _theirPublic = new byte[peerPublicKey.Length];
                Buffer.BlockCopy(peerPublicKey, 0, _theirPublic, 0, peerPublicKey.Length);

                byte[] transcript = BuildTranscript(shared);
                try
                {
                    _psk = HkdfSha256.Derive(shared, transcript, PskInfo, FebrisP2pPairingSecret.KeySizeBytes);
                    _code = DeriveCode(shared, transcript);
                    return true;
                }
                finally
                {
                    Array.Clear(shared, 0, shared.Length);
                    Array.Clear(transcript, 0, transcript.Length);
                }
            }
            catch (Exception)
            {
                // ANY failure importing or agreeing on a peer-supplied key is a refusal, and a
                // broad catch is the correct shape here rather than laziness.
                //
                // BouncyCastle reports a bad point as ArgumentException from DecodePoint, as
                // ArgumentException from the ECPublicKeyParameters validation, and as
                // InvalidOperationException when an agreement lands on infinity. Enumerating those
                // types would mean an invalid-curve attack succeeds against whichever one a future
                // library version reclassifies, and that attack recovers the private key one
                // derivation at a time.
                //
                // Catching broadly is safe BECAUSE the constructor already generated a P-256 key
                // pair on this device. Genuine lack of curve support would have thrown there,
                // before any peer data was involved, so by the time we reach an import every
                // remaining failure is attributable to the bytes the peer sent.
                return false;
            }
        }

        /// <summary>
        /// The human said the codes match. Returns the PSK to persist and spends the session.
        /// </summary>
        public FebrisP2pPairingSecret Confirm()
        {
            if (!AwaitingConfirmation)
            {
                throw new InvalidOperationException("No pairing is awaiting confirmation.");
            }
            _spent = true;
            FebrisP2pPairingSecret secret = FebrisP2pPairingSecret.FromBytes(_psk);
            Array.Clear(_psk, 0, _psk.Length);
            _psk = null;
            return secret;
        }

        /// <summary>
        /// The human said the codes do NOT match, which means someone is in the middle. Destroys
        /// the derived key so a later bug cannot resurrect it, and spends the session so the same
        /// ephemeral keys cannot be reused for a second guess.
        /// </summary>
        public void Reject()
        {
            _spent = true;
            if (_psk != null)
            {
                Array.Clear(_psk, 0, _psk.Length);
                _psk = null;
            }
            _code = null;
        }

        /// <summary>
        /// The bytes both sides hash: both public keys in a role-fixed order, then the shared
        /// secret. Including the shared secret is what stops an attacker who can see both public
        /// keys from predicting the code and coaching a user through the comparison.
        /// </summary>
        private byte[] BuildTranscript(byte[] shared)
        {
            byte[] first = IsInitiator ? _ourPublic : _theirPublic;
            byte[] second = IsInitiator ? _theirPublic : _ourPublic;

            byte[] transcript = new byte[first.Length + second.Length + shared.Length];
            Buffer.BlockCopy(first, 0, transcript, 0, first.Length);
            Buffer.BlockCopy(second, 0, transcript, first.Length, second.Length);
            Buffer.BlockCopy(shared, 0, transcript, first.Length + second.Length, shared.Length);
            return transcript;
        }

        /// <summary>
        /// Six digits from the transcript, zero padded so the length never varies. Taken modulo
        /// 10^6 from a 4-byte window of an HKDF output rather than from the PSK, so the displayed
        /// value is cryptographically independent of the key it accompanies.
        /// </summary>
        private static string DeriveCode(byte[] shared, byte[] transcript)
        {
            byte[] material = HkdfSha256.Derive(shared, transcript, CodeInfo, 4);
            try
            {
                uint value = ((uint)material[0] << 24) | ((uint)material[1] << 16)
                           | ((uint)material[2] << 8) | material[3];
                return (value % 1000000u).ToString("D" + CodeDigits);
            }
            finally
            {
                Array.Clear(material, 0, material.Length);
            }
        }

        /// <summary>
        /// Destroys anything still confirmable and drops the ephemeral private key.
        ///
        /// <para><b>Known limitation, recorded rather than hidden.</b> The private scalar lives
        /// inside an immutable BouncyCastle BigInteger, so it cannot be zeroed the way a byte[] or
        /// a platform key handle can. Dropping the reference leaves it to the garbage collector.
        /// This is a real, if small, regression against the previous implementation, and it is
        /// bounded by the key being ephemeral: it is generated per ceremony, is never persisted,
        /// and is worthless once the ceremony is spent.</para>
        /// </summary>
        public void Dispose()
        {
            Reject();
            _ephemeralPrivate = null;
        }
    }
}
