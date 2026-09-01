// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.IO;
using System.IO.Compression;

namespace Febris.SharedMobileLibrary.Utilites
{
    /// <summary>
    /// Limits applied by <see cref="SafeZipExtractor"/>. Defaults are deliberately
    /// generous because legitimate module packages can be large simulation builds,
    /// but they still reject the absurd entry counts, sizes, and compression ratios
    /// that a zip bomb produces. Tune against real package telemetry per deployment.
    /// </summary>
    public sealed class SafeZipLimits
    {
        /// <summary>Maximum total uncompressed bytes across the whole archive.</summary>
        public long MaxTotalUncompressedBytes { get; set; } = 8L * 1024 * 1024 * 1024;

        /// <summary>Maximum uncompressed bytes for any single entry.</summary>
        public long MaxEntryUncompressedBytes { get; set; } = 8L * 1024 * 1024 * 1024;

        /// <summary>Maximum number of entries in the archive.</summary>
        public int MaxEntryCount { get; set; } = 200_000;

        /// <summary>
        /// Maximum per-entry uncompressed/compressed ratio. A single DEFLATE pass tops
        /// out near 1032:1, so a ratio above this floor is a strong zip-bomb signal.
        /// </summary>
        public double MaxCompressionRatio { get; set; } = 1000.0;

        /// <summary>
        /// The ratio check is only enforced once an entry's uncompressed size exceeds
        /// this floor, so small highly-compressible files do not trip a false positive.
        /// </summary>
        public long RatioCheckFloorBytes { get; set; } = 4L * 1024 * 1024;

        public static SafeZipLimits Default => new SafeZipLimits();
    }

    /// <summary>
    /// Thrown when an archive violates a <see cref="SafeZipLimits"/> rule (zip-slip
    /// path traversal or a zip-bomb count/size/ratio breach).
    /// </summary>
    public class SafeZipExtractionException : Exception
    {
        public SafeZipExtractionException(string message) : base(message) { }
    }

    /// <summary>
    /// Hardened replacement for <c>ZipFile.ExtractToDirectory</c>. Module packages are
    /// attacker-supplied archives extracted on client devices, so every extraction must
    /// pass through here instead of the raw framework call. Guards against:
    /// zip-slip (entries whose path escapes the destination), and zip bombs (excessive
    /// entry count, per-entry and total uncompressed size, and compression ratio).
    /// </summary>
    public static class SafeZipExtractor
    {
        /// <summary>
        /// Extracts <paramref name="sourceArchivePath"/> into <paramref name="destinationDirectory"/>,
        /// rejecting any entry that breaches <paramref name="limits"/>. Uncompressed size is
        /// counted from the actual decompressed bytes during extraction, so a forged central
        /// directory cannot smuggle a bomb past the cheap up-front checks. On failure a freshly
        /// created destination directory is removed so no partial extraction is left behind.
        /// </summary>
        public static void ExtractToDirectory(string sourceArchivePath, string destinationDirectory, SafeZipLimits limits = null)
        {
            if (sourceArchivePath == null) throw new ArgumentNullException(nameof(sourceArchivePath));
            if (destinationDirectory == null) throw new ArgumentNullException(nameof(destinationDirectory));
            limits = limits ?? SafeZipLimits.Default;

            // Normalized destination root with a trailing separator so a prefix match cannot
            // accidentally allow a sibling directory (for example Dest vs Dest-evil).
            string destRoot = Path.GetFullPath(destinationDirectory);
            string sep = Path.DirectorySeparatorChar.ToString();
            string destRootWithSep = destRoot.EndsWith(sep) ? destRoot : destRoot + sep;

            bool destExistedBefore = Directory.Exists(destRoot);
            Directory.CreateDirectory(destRoot);

            try
            {
                long totalWritten = 0;

                using (var archive = ZipFile.OpenRead(sourceArchivePath))
                {
                    if (archive.Entries.Count > limits.MaxEntryCount)
                    {
                        throw new SafeZipExtractionException(
                            $"Archive entry count {archive.Entries.Count} exceeds the limit of {limits.MaxEntryCount}.");
                    }

                    foreach (var entry in archive.Entries)
                    {
                        string targetPath = Path.GetFullPath(Path.Combine(destRoot, entry.FullName));

                        bool isDirectoryEntry =
                            entry.FullName.EndsWith("/") ||
                            entry.FullName.EndsWith("\\") ||
                            string.IsNullOrEmpty(entry.Name);

                        // Zip-slip guard. The resolved path must stay inside the destination root.
                        string compareTarget = isDirectoryEntry && !targetPath.EndsWith(sep)
                            ? targetPath + sep
                            : targetPath;
                        if (targetPath != destRoot &&
                            !compareTarget.StartsWith(destRootWithSep, StringComparison.Ordinal))
                        {
                            throw new SafeZipExtractionException(
                                $"Archive entry '{entry.FullName}' would extract outside the destination directory (zip-slip).");
                        }

                        if (isDirectoryEntry)
                        {
                            Directory.CreateDirectory(targetPath);
                            continue;
                        }

                        // Cheap up-front checks. Length/CompressedLength come from the central
                        // directory and can be forged, so they are a first line only. The streaming
                        // copy below is the authoritative enforcement.
                        if (entry.Length > limits.MaxEntryUncompressedBytes)
                        {
                            throw new SafeZipExtractionException(
                                $"Archive entry '{entry.FullName}' uncompressed size {entry.Length} exceeds the per-entry limit of {limits.MaxEntryUncompressedBytes}.");
                        }

                        if (entry.CompressedLength > 0 && entry.Length > limits.RatioCheckFloorBytes)
                        {
                            double ratio = (double)entry.Length / entry.CompressedLength;
                            if (ratio > limits.MaxCompressionRatio)
                            {
                                throw new SafeZipExtractionException(
                                    $"Archive entry '{entry.FullName}' compression ratio {ratio:F0} exceeds the limit of {limits.MaxCompressionRatio:F0} (possible zip bomb).");
                            }
                        }

                        string parent = Path.GetDirectoryName(targetPath);
                        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

                        // Stream the decompression, counting actual bytes so a forged central-directory
                        // size cannot get a bomb past the up-front checks.
                        using (var entryStream = entry.Open())
                        using (var outStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[81920];
                            long entryWritten = 0;
                            int read;
                            while ((read = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                entryWritten += read;
                                totalWritten += read;

                                if (entryWritten > limits.MaxEntryUncompressedBytes)
                                {
                                    throw new SafeZipExtractionException(
                                        $"Archive entry '{entry.FullName}' exceeded the per-entry uncompressed limit of {limits.MaxEntryUncompressedBytes} during extraction (possible zip bomb).");
                                }
                                if (totalWritten > limits.MaxTotalUncompressedBytes)
                                {
                                    throw new SafeZipExtractionException(
                                        $"Archive exceeded the total uncompressed limit of {limits.MaxTotalUncompressedBytes} during extraction (possible zip bomb).");
                                }

                                outStream.Write(buffer, 0, read);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Do not leave a partial (or partially malicious) extraction behind. Only remove
                // the destination if this call created it, so pre-existing content is never deleted.
                if (!destExistedBefore)
                {
                    try { Directory.Delete(destRoot, true); } catch { /* best effort cleanup */ }
                }
                throw;
            }
        }
    }
}
