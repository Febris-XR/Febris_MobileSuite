// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Febris.SharedMobileLibrary.P2pNetworking.Crypto
{
    /// <summary>
    /// Reports which cryptographic primitives the RUNTIME actually implements, by running them.
    ///
    /// <para><b>Why this exists.</b> This tier shipped a pairing ceremony built on
    /// <c>ECDiffieHellman</c> that could never work, because the API was "verified" by COMPILING
    /// against netstandard2.1. A reference assembly exposing a member says nothing about whether
    /// Mono's Android runtime implements it, and the gap was not discovered until the ceremony
    /// threw on a device and silently fell back to a stub with no man-in-the-middle resistance.
    /// See docs/MOBILE_KNOWN_ISSUES.md issue 2.</para>
    ///
    /// <para><b>What it is for next.</b> The planned v3 frame AEAD is specified against
    /// <c>AesGcm</c> on the strength of the same compile-only evidence, and <c>AesGcm</c> is a
    /// NEWER addition to that surface than <c>ECDiffieHellman</c>. This probe answers that question
    /// with a measurement before any AEAD code is written, rather than after.</para>
    ///
    /// <para><b>Contract: this must never throw.</b> It runs at startup on both heads, and a
    /// diagnostic that crashes the app it is diagnosing is worse than no diagnostic. Every probe is
    /// individually guarded and reports its own failure as text.</para>
    /// </summary>
    public static class PlatformCryptoProbe
    {
        /// <summary>
        /// One line per primitive, ready to write to the log. Safe to call at any time.
        /// </summary>
        public static string Describe()
        {
            var report = new StringBuilder();
            report.Append("crypto probe: ");
            report.Append("BouncyCastleEcdh=").Append(Run(ProbeBouncyCastleEcdh));
            report.Append("; PlatformEcdh=").Append(Run(ProbePlatformEcdh));
            report.Append("; AesGcm=").Append(Run(ProbeAesGcm));
            return report.ToString();
        }

        /// <summary>
        /// Runs one probe and converts any outcome into a short string.
        ///
        /// The catch is deliberately widest-possible. A runtime that lacks a type does not raise a
        /// tidy <c>NotImplementedException</c>: it can raise <c>TypeLoadException</c>,
        /// <c>MissingMethodException</c> or <c>PlatformNotSupportedException</c>, and on Mono the
        /// choice has not been consistent. The exception TYPE is part of what we want recorded, so
        /// it is included in the text rather than being flattened to "failed".
        /// </summary>
        private static string Run(Func<string> probe)
        {
            try
            {
                return probe();
            }
            catch (Exception ex)
            {
                return "FAIL(" + ex.GetType().Name + ": " + Summarise(ex.Message) + ")";
            }
        }

        private static string Summarise(string message)
        {
            if (string.IsNullOrEmpty(message)) return "no message";
            message = message.Replace(Environment.NewLine, " ").Replace('\n', ' ').Replace('\r', ' ');
            return message.Length <= 120 ? message : message.Substring(0, 120) + "...";
        }

        /// <summary>
        /// The primitive pairing now depends on. Runs a complete two-party ceremony and checks the
        /// two sides agree, so a single log line proves the real thing works rather than proving
        /// only that a constructor did not throw.
        ///
        /// NoInlining so that a runtime failure to load a type surfaces when this method is called,
        /// inside <see cref="Run"/>'s try, rather than while JITting the caller.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ProbeBouncyCastleEcdh()
        {
            using (var initiator = new FebrisP2pPairingSession(isInitiator: true))
            using (var responder = new FebrisP2pPairingSession(isInitiator: false))
            {
                if (!responder.ProcessPeerKey(initiator.PublicKey)) return "FAIL(responder refused key)";
                if (!initiator.ProcessPeerKey(responder.PublicKey)) return "FAIL(initiator refused key)";

                string a = initiator.ComparisonCode;
                string b = responder.ComparisonCode;

                if (string.IsNullOrEmpty(a)) return "FAIL(no code derived)";
                // Not logged. The code is only non-secret because it is worthless outside the
                // ceremony that produced it, and a habit of printing codes is how one ends up in a
                // log during a real pairing.
                if (a != b) return "FAIL(codes disagree, key agreement is broken)";

                return "OK(agreed, " + a.Length + "-digit code)";
            }
        }

        /// <summary>
        /// The primitive pairing USED to depend on, probed so the failure is recorded as a reading
        /// with an exception type and message. Issue 2 currently attributes this by elimination,
        /// from a dialog title, which is weaker evidence than it should be for a decision that
        /// added a dependency.
        ///
        /// An OK here is not a reason to revert: BouncyCastle is now the single place the pairing
        /// crypto lives, and splitting it back across two mechanisms with different failure modes
        /// would be a regression. It would simply mean the platform improved.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ProbePlatformEcdh()
        {
            using (ECDiffieHellman a = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman b = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            {
                byte[] ka = a.DeriveKeyFromHash(b.PublicKey, HashAlgorithmName.SHA256);
                byte[] kb = b.DeriveKeyFromHash(a.PublicKey, HashAlgorithmName.SHA256);

                if (ka == null || ka.Length == 0) return "FAIL(empty derivation)";
                for (int i = 0; i < ka.Length; i++)
                {
                    if (ka[i] != kb[i]) return "FAIL(derivations disagree)";
                }
                return "OK(agreed)";
            }
        }

        /// <summary>
        /// The primitive the v3 frame AEAD is currently specified against.
        ///
        /// <para>A full round trip, not just a constructor. A constructor that succeeds while
        /// <c>Encrypt</c> throws is a real shape for a partially implemented runtime primitive, and
        /// the plaintext is compared back because an AEAD that returns wrong bytes without throwing
        /// is the worst outcome of the three and the only one a "did it throw" check misses.</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ProbeAesGcm()
        {
            byte[] key = new byte[32];
            byte[] nonce = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
                rng.GetBytes(nonce);
            }

            byte[] plaintext = Encoding.ASCII.GetBytes("febris aes-gcm probe");
            byte[] associated = Encoding.ASCII.GetBytes("febris-p2p-v3-probe");
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            using (var gcm = new AesGcm(key))
            {
                gcm.Encrypt(nonce, plaintext, ciphertext, tag, associated);

                byte[] roundTripped = new byte[plaintext.Length];
                gcm.Decrypt(nonce, ciphertext, tag, roundTripped, associated);

                for (int i = 0; i < plaintext.Length; i++)
                {
                    if (plaintext[i] != roundTripped[i]) return "FAIL(round trip corrupted the plaintext)";
                }
            }

            return "OK(round trip)";
        }
    }
}
