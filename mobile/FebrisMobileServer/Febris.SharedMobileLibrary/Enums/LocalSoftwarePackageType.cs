// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Enums
{
    /// <summary>
    /// Hand-kept copy of Febris.EnumLibrary.LocalSoftwarePackageType.
    ///
    /// <para>
    /// The members and their EXPLICIT numeric values must stay identical to the canonical
    /// definition. The distribution manifest's <c>kindId</c> is these numbers on the wire
    /// (distribution/tools/validate_manifest.py KINDS), so a divergence here is a delivery-feed
    /// bug, not a naming preference. This copy previously used implicit ordinals
    /// (PC=0, MobileServer=1, ...), which disagreed with the manifest on every member.
    /// </para>
    ///
    /// <para>
    /// Deliberately a hand-copy and NOT a ProjectReference to Febris.EnumLibrary: that edge
    /// would drag the node's AGPL enum assembly into the mobile island and destroy the repo
    /// cut. The sync obligation is enforced instead by a checked-in parity test
    /// (LocalSoftwarePackageTypeParityTests), which blocks the febris-mobile cut if it fails.
    /// </para>
    /// </summary>
    public enum LocalSoftwarePackageType
    {
        None = 0,
        PC = 100,
        AndroidMobileServer = 200,
        AndroidMobileCompanion = 300,
        CSharp = 400,
        CPP = 500
    }
}
