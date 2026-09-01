// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.IO;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Resolves an attacker-authored filename against a base directory, or refuses.
    ///
    /// <para><b>The bug this exists to kill.</b> Five receive paths across both tiers did
    /// <c>Path.Combine(FileSystem.SomePath, header.PacketName)</c> and then wrote the frame
    /// body there. <c>PacketName</c> is a plain JSON string authored by whoever sent the
    /// frame, and <see cref="Path.Combine(string, string)"/> <b>discards its first argument
    /// entirely when the second is rooted</b>. So a <c>PacketName</c> of
    /// <c>/sdcard/Download/x.apk</c> does not land under the statement directory, it lands
    /// exactly where the sender asked. Relative traversal (<c>../../</c>) escapes just as
    /// well. On the Companion this was reachable before any peer check existed.</para>
    ///
    /// <para><b>Why refuse rather than sanitise.</b> Stripping the path off
    /// <c>../../evil.sh</c> and writing <c>evil.sh</c> still gives an unauthenticated peer a
    /// write of its chosen content under a name of its choosing. It converts a dangerous
    /// write into a quieter one rather than stopping it, and it hides the attempt from the
    /// logs. Every legitimate sender in this codebase emits a plain filename, so refusing
    /// anything else costs nothing and makes the attempt visible.</para>
    ///
    /// <para><b>Belt and braces.</b> The name is validated by inspection AND the resolved
    /// path is re-checked against the base directory afterwards. The second check is what
    /// catches anything the first did not anticipate, including platform-specific
    /// normalisation differences between Android and Windows.</para>
    /// </summary>
    public static class P2pSafeFileName
    {
        /// <summary>
        /// True when <paramref name="untrustedName"/> is a plain filename with no directory
        /// component, no traversal, and no characters the platform rejects.
        /// </summary>
        public static bool IsPlainFileName(string untrustedName)
        {
            if (string.IsNullOrWhiteSpace(untrustedName))
            {
                return false;
            }

            // "." and ".." are directory references, and Path.GetFileName happily returns
            // them unchanged, so they have to be named explicitly.
            if (untrustedName == "." || untrustedName == "..")
            {
                return false;
            }

            // Any separator at all means the sender is describing a location rather than a
            // name. Both separators are checked regardless of platform, because the frame
            // may have been authored on a different OS than the one receiving it.
            if (untrustedName.IndexOf('/') >= 0 ||
                untrustedName.IndexOf('\\') >= 0 ||
                untrustedName.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
                untrustedName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                return false;
            }

            // A volume separator catches "C:evil.txt", which is a drive-relative path rather
            // than a filename and is not covered by the separator check above.
            if (untrustedName.IndexOf(Path.VolumeSeparatorChar) >= 0 || untrustedName.IndexOf(':') >= 0)
            {
                return false;
            }

            if (Path.IsPathRooted(untrustedName))
            {
                return false;
            }

            // Control characters, including the embedded NUL that truncates a path in some
            // native layers below this one.
            foreach (char c in untrustedName)
            {
                if (c < 0x20 || c == 0x7f)
                {
                    return false;
                }
            }

            if (untrustedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            // Trailing dots and spaces are silently trimmed by Windows, so "evil.txt." and
            // "evil.txt" address the same file. Reject rather than let two names collide.
            char last = untrustedName[untrustedName.Length - 1];
            if (last == '.' || last == ' ')
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolve <paramref name="untrustedName"/> inside <paramref name="baseDirectory"/>.
        /// Returns false and leaves <paramref name="fullPath"/> null when the name is not a
        /// plain filename, or when the resolved path somehow still escapes the base.
        /// </summary>
        public static bool TryResolveWithin(string baseDirectory, string untrustedName, out string fullPath)
        {
            fullPath = null;

            if (string.IsNullOrWhiteSpace(baseDirectory) || !IsPlainFileName(untrustedName))
            {
                return false;
            }

            string candidate;
            string root;
            try
            {
                candidate = Path.GetFullPath(Path.Combine(baseDirectory, untrustedName));
                root = Path.GetFullPath(baseDirectory);
            }
            catch (Exception)
            {
                // GetFullPath throws on paths the platform considers malformed. That is a
                // refusal, not a crash.
                return false;
            }

            // Normalise the base to end in a separator so a sibling directory sharing a
            // prefix ("/data/statements-evil" against a base of "/data/statements") cannot
            // pass a naive StartsWith.
            if (root.Length == 0)
            {
                return false;
            }
            if (root[root.Length - 1] != Path.DirectorySeparatorChar)
            {
                root += Path.DirectorySeparatorChar;
            }

            if (candidate.IndexOf(root, StringComparison.Ordinal) != 0)
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }

        /// <summary>The log line for a refusal. Kept here so every call site says the same
        /// thing and the attempt is greppable across both tiers.</summary>
        public static string RefusalMessage(string untrustedName, string context)
        {
            string shown = untrustedName == null
                ? "<null>"
                : (untrustedName.Length > 120 ? untrustedName.Substring(0, 120) + "..." : untrustedName);

            return "P2P REFUSED UNSAFE FILENAME in " + context + ": '" + shown +
                "'. A frame tried to write outside its base directory. The frame was dropped.";
        }
    }
}
