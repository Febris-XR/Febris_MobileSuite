// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System.IO;
using Febris.SharedMobileLibrary.P2pNetworking;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// The path-escape gate. Every case here corresponds to a write an unauthenticated peer
    /// could previously perform anywhere on the device, because Path.Combine discards its
    /// first argument when the second is rooted.
    /// </summary>
    public class P2pSafeFileNameTests
    {
        private static readonly string Base =
            Path.Combine(Path.GetTempPath(), "febris-safe-name-tests");

        // ---------- what must be accepted, so the fix does not break real traffic ----------

        [Theory]
        [InlineData("statement.json")]
        [InlineData("module.zip")]
        [InlineData("a")]
        [InlineData("file with spaces.mp4")]
        [InlineData("UPPER.MP4")]
        [InlineData("dotted.name.with.many.parts.json")]
        [InlineData("-leading-dash.json")]
        [InlineData("_underscore.json")]
        [InlineData("2026-07-27T12-00-00.json")]
        [InlineData(".hidden")]
        public void PlainFileNames_AreAccepted(string name)
        {
            Assert.True(P2pSafeFileName.IsPlainFileName(name));
            Assert.True(P2pSafeFileName.TryResolveWithin(Base, name, out string full));
            Assert.Equal(Path.GetFullPath(Path.Combine(Base, name)), full);
        }

        // ---------- the actual exploit ----------

        [Theory]
        [InlineData("/sdcard/Download/evil.apk")]
        [InlineData("/data/data/com.febris/files/evil")]
        [InlineData("C:\\Windows\\System32\\evil.dll")]
        [InlineData("\\\\server\\share\\evil")]
        public void RootedPaths_AreRefused_ThisIsTheWholeBug(string name)
        {
            // Path.Combine(base, "/sdcard/x") returns "/sdcard/x". The base is discarded
            // entirely, so the sender picks the destination.
            Assert.False(P2pSafeFileName.IsPlainFileName(name));
            Assert.False(P2pSafeFileName.TryResolveWithin(Base, name, out string full));
            Assert.Null(full);
        }

        [Theory]
        [InlineData("../evil.json")]
        [InlineData("..\\evil.json")]
        [InlineData("../../../../etc/hosts")]
        [InlineData("sub/evil.json")]
        [InlineData("sub\\evil.json")]
        [InlineData("./evil.json")]
        public void TraversalAndSubdirectories_AreRefused(string name)
        {
            Assert.False(P2pSafeFileName.IsPlainFileName(name));
            Assert.False(P2pSafeFileName.TryResolveWithin(Base, name, out _));
        }

        [Theory]
        [InlineData(".")]
        [InlineData("..")]
        public void DirectoryReferences_AreRefused(string name)
        {
            // Path.GetFileName returns these unchanged, so a sanitise-based fix would have
            // let them through.
            Assert.False(P2pSafeFileName.IsPlainFileName(name));
        }

        [Fact]
        public void DriveRelativePath_IsRefused()
        {
            // "C:evil.txt" is not rooted and contains no separator, but it resolves against
            // the current directory of drive C rather than the base.
            Assert.False(P2pSafeFileName.IsPlainFileName("C:evil.txt"));
        }

        [Fact]
        public void AlternateDataStreamSyntax_IsRefused()
        {
            Assert.False(P2pSafeFileName.IsPlainFileName("statement.json:hidden"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void BlankNames_AreRefused(string name)
        {
            Assert.False(P2pSafeFileName.IsPlainFileName(name));
            Assert.False(P2pSafeFileName.TryResolveWithin(Base, name, out _));
        }

        [Fact]
        public void EmbeddedNul_IsRefused()
        {
            // A NUL truncates the path in native layers below this one, so "safe.json\0.png"
            // can become "safe.json" somewhere further down.
            Assert.False(P2pSafeFileName.IsPlainFileName("safe.json\0.png"));
        }

        [Theory]
        [InlineData("bad\nname.json")]
        [InlineData("bad\rname.json")]
        [InlineData("bad\tname.json")]
        public void ControlCharacters_AreRefused(string name)
        {
            Assert.False(P2pSafeFileName.IsPlainFileName(name));
        }

        [Theory]
        [InlineData("evil.txt.")]
        [InlineData("evil.txt ")]
        public void TrailingDotOrSpace_IsRefused(string name)
        {
            // Windows silently trims both, so these would address the same file as the
            // untrimmed name. Two frames, one destination, no way to tell them apart.
            Assert.False(P2pSafeFileName.IsPlainFileName(name));
        }

        // ---------- the second line of defence ----------

        [Fact]
        public void SiblingDirectoryWithASharedPrefix_CannotPass()
        {
            // The reason the base is normalised to end in a separator before comparing. A
            // naive StartsWith would accept a resolved path under "<base>-evil".
            string sneaky = Base + "-evil";
            Assert.False(P2pSafeFileName.TryResolveWithin(Base, "../" + Path.GetFileName(sneaky) + "/x.json", out _));
        }

        [Fact]
        public void BlankBaseDirectory_IsRefused()
        {
            Assert.False(P2pSafeFileName.TryResolveWithin(null, "ok.json", out _));
            Assert.False(P2pSafeFileName.TryResolveWithin("", "ok.json", out _));
            Assert.False(P2pSafeFileName.TryResolveWithin("   ", "ok.json", out _));
        }

        [Fact]
        public void ResolvedPath_IsAlwaysUnderTheBase()
        {
            Assert.True(P2pSafeFileName.TryResolveWithin(Base, "ok.json", out string full));
            Assert.StartsWith(Path.GetFullPath(Base), full);
        }

        [Fact]
        public void RefusalMessage_NamesTheContextAndTruncatesTheAttackerString()
        {
            string huge = new string('A', 500);
            string message = P2pSafeFileName.RefusalMessage(huge, "ProcessVideo");

            Assert.Contains("ProcessVideo", message);
            Assert.Contains("REFUSED UNSAFE FILENAME", message);
            Assert.True(message.Length < 300);   // an attacker cannot flood the log through this
        }

        [Fact]
        public void RefusalMessage_HandlesNull()
        {
            Assert.Contains("<null>", P2pSafeFileName.RefusalMessage(null, "ProcessVideo"));
        }
    }
}
