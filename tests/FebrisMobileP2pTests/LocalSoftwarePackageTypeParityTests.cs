// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using MobileKind = Febris.SharedMobileLibrary.Enums.LocalSoftwarePackageType;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// ENUM-DIVERGE (NR-27) pre-split gate.
    ///
    /// <para>
    /// LocalSoftwarePackageType exists twice on purpose: once in Febris.EnumLibrary (AGPL, node)
    /// and once as a hand-copy in Febris.SharedMobileLibrary. A ProjectReference between them
    /// would drag AGPL platform code into the mobile island and destroy the announced repo cut,
    /// so the duplication is permanent and the sync obligation is enforced here instead.
    /// </para>
    ///
    /// <para>
    /// The duplicate-type architecture guard structurally CANNOT catch this, because mobile/ is
    /// excluded from its ScanRoots. That is why this test exists and why the febris-mobile cut is
    /// blocked until it is green.
    /// </para>
    ///
    /// <para>
    /// The expected values are asserted as literals rather than read from Febris.EnumLibrary,
    /// precisely because taking a reference to it is the thing being prevented. They are the
    /// numbers the distribution manifest's kindId carries on the wire
    /// (distribution/tools/validate_manifest.py KINDS), so changing either side is a
    /// compatibility break in the delivery feed.
    /// </para>
    /// </summary>
    public class LocalSoftwarePackageTypeParityTests
    {
        [Theory]
        [InlineData("None", 0)]
        [InlineData("PC", 100)]
        [InlineData("AndroidMobileServer", 200)]
        [InlineData("AndroidMobileCompanion", 300)]
        [InlineData("CSharp", 400)]
        [InlineData("CPP", 500)]
        public void MobileCopy_HasCanonicalMemberWithCanonicalValue(string name, int expected)
        {
            Enum.IsDefined(typeof(MobileKind), name)
                .Should().BeTrue($"the mobile copy must declare {name}");

            ((int)(MobileKind)Enum.Parse(typeof(MobileKind), name))
                .Should().Be(expected, $"{name} is wire-pinned by the manifest kindId");
        }

        [Fact]
        public void MobileCopy_HasExactlyTheCanonicalMemberSet()
        {
            // Catches ADDITIONS too, not just changed values: a member added on one side only
            // is the same divergence in the other direction.
            Enum.GetNames(typeof(MobileKind)).OrderBy(n => n, StringComparer.Ordinal)
                .Should().Equal("AndroidMobileCompanion", "AndroidMobileServer", "CPP", "CSharp", "None", "PC");
        }
    }
}
