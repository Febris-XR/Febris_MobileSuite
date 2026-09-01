// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// Direction legality. Both dispatchers were direction-blind, so a peer could send a
    /// body type only the OTHER side has any business sending and be processed normally.
    ///
    /// This is a separate concern from the peer allowlist: knowing who a peer is says
    /// nothing about whether it is speaking out of turn.
    /// </summary>
    public class P2pDirectionLegalityTests
    {
        private static bool AtServer(BodyType t) =>
            P2pPeerAuthorization.IsLegalInbound(t, P2pPeerAuthorization.P2pTier.MobileServer);

        private static bool AtCompanion(BodyType t) =>
            P2pPeerAuthorization.IsLegalInbound(t, P2pPeerAuthorization.P2pTier.Companion);

        // ---------- the closed-set discipline ----------

        [Fact]
        public void EveryBodyType_IsClassifiedForAtLeastOneDirection()
        {
            // The point of this test. A new BodyType member cannot be added without someone
            // deciding which way it flows, because an unclassified member hits the default
            // case, is refused on BOTH sides, and would silently break its own feature.
            //
            // _junk is the original deliberate exception: it is never legitimate from the wire.
            //
            // _fileUpload, _video and _genericString joined it on 2026-07-29. They are the
            // reverse-direction upload types: the Companion sends only statements, so a module,
            // archive or recorded-video file arriving at the Server is not a flow that exists.
            // A sweep found ZERO live senders for all three on either tier. They are refused on
            // both sides now, which is why they are exempt from the classification requirement.
            // See DeliberatelyRefusedTypes_AreRefusedOnBothSides below, which pins that.
            var deliberatelyRefused = new[]
            {
                BodyType._junk,
                BodyType._fileUpload,
                BodyType._video,
                BodyType._genericString
            };

            var unclassified = Enum.GetValues(typeof(BodyType))
                .Cast<BodyType>()
                .Where(t => !deliberatelyRefused.Contains(t))
                .Where(t => !AtServer(t) && !AtCompanion(t))
                .ToList();

            Assert.True(
                unclassified.Count == 0,
                "Unclassified BodyType members (add them to P2pPeerAuthorization.IsLegalInbound): " +
                string.Join(", ", unclassified));
        }

        [Fact]
        public void Junk_IsRefusedOnBothSides()
        {
            Assert.False(AtServer(BodyType._junk));
            Assert.False(AtCompanion(BodyType._junk));
        }

        /// <summary>
        /// Pins the 2026-07-29 decision that the reverse-direction upload types are refused.
        ///
        /// The architecture is that the Server distributes module archives to devices and the
        /// Companion sends ONLY statements out of sqlite. These three types gave the wire a way
        /// to reach a disk-writing handler for a flow that does not exist, and none of them has
        /// a live sender anywhere. If a future feature genuinely needs one of them, restore it
        /// to its group in IsLegalInbound, re-add its InlineData below, and delete it from here
        /// AND from the deliberatelyRefused array above. Failing this test on purpose is the
        /// intended way to notice that decision.
        /// </summary>
        [Theory]
        [InlineData(BodyType._fileUpload)]
        [InlineData(BodyType._video)]
        [InlineData(BodyType._genericString)]
        public void DeliberatelyRefusedTypes_AreRefusedOnBothSides(BodyType t)
        {
            Assert.False(AtServer(t));
            Assert.False(AtCompanion(t));
        }

        [Fact]
        public void AnUndefinedEnumValue_IsRefused()
        {
            // A frame can carry any integer. Newtonsoft will happily deserialize 9999 into
            // the enum, so the default case has to refuse rather than fall through.
            BodyType bogus = (BodyType)9999;
            Assert.False(AtServer(bogus));
            Assert.False(AtCompanion(bogus));
        }

        // ---------- Companion to Server ----------

        [Theory]
        [InlineData(BodyType._initalize)]
        [InlineData(BodyType._statusUpdate)]
        // Moved to DeliberatelyRefusedTypes_AreRefusedOnBothSides on 2026-07-29.
        //[InlineData(BodyType._fileUpload)]
        //[InlineData(BodyType._video)]
        [InlineData(BodyType._handshakeClientHello)]
        [InlineData(BodyType._handshakeClientFinal)]
        [InlineData(BodyType._videoCodecConfig)]
        [InlineData(BodyType._videoFrame)]
        public void CompanionToServerTypes_AreLegalAtTheServerOnly(BodyType t)
        {
            Assert.True(AtServer(t));
            Assert.False(AtCompanion(t));
        }

        [Fact]
        public void TheVideoStreamMustStillWork()
        {
            // Guards the pipeline built in step 6. If this ever goes red, the direction table
            // has broken live video rather than an attack.
            Assert.True(AtServer(BodyType._videoCodecConfig));
            Assert.True(AtServer(BodyType._videoFrame));
            Assert.True(AtServer(BodyType._endVideoStream));
        }

        // ---------- Server to Companion ----------

        [Theory]
        [InlineData(BodyType._module)]
        [InlineData(BodyType._installModule)]
        [InlineData(BodyType._uninstallModule)]
        [InlineData(BodyType._reinstallModule)]
        [InlineData(BodyType._removeModule)]
        [InlineData(BodyType._removeZippedModule)]
        [InlineData(BodyType._oldStatements)]
        [InlineData(BodyType._oldVideos)]
        [InlineData(BodyType._acknowledge)]
        [InlineData(BodyType._handshakeServerHello)]
        [InlineData(BodyType._videoStreamStart)]
        public void ServerToCompanionTypes_AreLegalAtTheCompanionOnly(BodyType t)
        {
            Assert.True(AtCompanion(t));
            Assert.False(AtServer(t));
        }

        [Fact]
        public void VideoStreamStart_IsRefusedAtTheServer()
        {
            // Called out on its own because the Server HAD a live inbound dispatch case for
            // this, added during the step 6 decode work. The enum documents 671 as
            // "Server to Companion. Start capture.", so that case was dead code the moment it
            // was written and this is the check that says so.
            Assert.False(AtServer(BodyType._videoStreamStart));
            Assert.True(AtCompanion(BodyType._videoStreamStart));
        }

        [Fact]
        public void AModulePushCannotBeSentToTheServer()
        {
            // Module frames are how the Server provisions the Companion. Accepting one
            // inbound would let a peer drive the Server's module-install path.
            Assert.False(AtServer(BodyType._module));
            Assert.False(AtServer(BodyType._installModule));
        }

        // ---------- bidirectional, deliberately ----------

        [Theory]
        [InlineData(BodyType._statement)]      // send sites on both tiers
        [InlineData(BodyType._endVideoStream)] // documented as the stop in both directions
        [InlineData(BodyType._videoStream)]    // deprecated 667, overloaded both ways
        // Moved to DeliberatelyRefusedTypes_AreRefusedOnBothSides on 2026-07-29.
        //[InlineData(BodyType._genericString)]  // handled on both tiers, no single owner
        public void BidirectionalTypes_AreLegalBothWays(BodyType t)
        {
            Assert.True(AtServer(t));
            Assert.True(AtCompanion(t));
        }

        // ---------- interaction with the existing gates ----------

        [Fact]
        public void HandshakeTypes_AreDirectionalEvenThoughTheyArePreAuthentication()
        {
            // Pre-authentication means the peer gate exempts them, NOT that they may arrive
            // from either side. A responder receiving a server hello would mean something is
            // reflecting its own traffic back at it.
            Assert.True(P2pPeerAuthorization.IsPreAuthenticationBodyType(BodyType._handshakeServerHello));
            Assert.False(AtServer(BodyType._handshakeServerHello));

            Assert.True(P2pPeerAuthorization.IsPreAuthenticationBodyType(BodyType._handshakeClientHello));
            Assert.False(AtCompanion(BodyType._handshakeClientHello));
        }

        [Fact]
        public void Initalize_IsPreAuthenticationButServerInboundOnly()
        {
            // _initalize is exempt from the peer allowlist because it is the onboarding
            // frame, and that exemption must not also make it directionless.
            Assert.True(P2pPeerAuthorization.IsPreAuthenticationBodyType(BodyType._initalize));
            Assert.True(AtServer(BodyType._initalize));
            Assert.False(AtCompanion(BodyType._initalize));
        }

        [Fact]
        public void EveryTypeIsLegalSomewhere_OrIsJunk()
        {
            // Belt and braces against a copy-paste error that puts a type in neither branch
            // while some other test still passes.
            //
            // The exemption list matches deliberatelyRefused in
            // EveryBodyType_IsClassifiedForAtLeastOneDirection. Both must be updated together,
            // which is deliberate: refusing a type outright should take two edits, not one.
            foreach (BodyType t in Enum.GetValues(typeof(BodyType)).Cast<BodyType>())
            {
                if (t == BodyType._junk) continue;

                // Disabled reverse-direction uploads, 2026-07-29. See
                // DeliberatelyRefusedTypes_AreRefusedOnBothSides.
                if (t == BodyType._fileUpload) continue;
                if (t == BodyType._video) continue;
                if (t == BodyType._genericString) continue;

                Assert.True(AtServer(t) || AtCompanion(t), t + " is legal nowhere");
            }
        }


        // ---------- trust vs permission ----------

        [Fact]
        public void PreAuthenticationExemption_AllowsTheFrameButDoesNotResolveThePeer()
        {
            // The distinction that closes the outbound-hijack. _initalize must be able to
            // arrive from an unpaired device (that is onboarding), but nothing downstream may
            // look a device up from its header, because UpdateIPAddress rewrites the address
            // SocketSender routes on.
            var header = new PacketHeaderModel
            {
                BodyType = BodyType._initalize,
                DeviceUniqueIdentifier = "someone-elses-identifier"
            };

            var result = P2pPeerAuthorization.Authorize(header, peerIsKnown: false);

            Assert.True(result.IsAuthorized);
            Assert.False(result.PeerResolved);
        }

        [Fact]
        public void AnInitalizeWithNoIdentifierAtAll_IsStillNotResolved()
        {
            // The narrower hole: an empty identifier needs no knowledge of any real device,
            // and empty-matches-empty in the device lookup selects whoever is mid-onboarding.
            var header = new PacketHeaderModel { BodyType = BodyType._initalize, DeviceUniqueIdentifier = null };

            var result = P2pPeerAuthorization.Authorize(header, peerIsKnown: false);

            Assert.True(result.IsAuthorized);
            Assert.False(result.PeerResolved);
        }

        [Fact]
        public void AKnownPeerSendingOrdinaryTraffic_IsResolved()
        {
            var header = new PacketHeaderModel
            {
                BodyType = BodyType._statement,
                DeviceUniqueIdentifier = "a-real-paired-device"
            };

            var result = P2pPeerAuthorization.Authorize(header, peerIsKnown: true);

            Assert.True(result.IsAuthorized);
            Assert.True(result.PeerResolved);
        }

        [Fact]
        public void ADeniedFrame_IsNeverResolved()
        {
            var header = new PacketHeaderModel
            {
                BodyType = BodyType._statement,
                DeviceUniqueIdentifier = "an-unknown-device"
            };

            var result = P2pPeerAuthorization.Authorize(header, peerIsKnown: false);

            Assert.False(result.IsAuthorized);
            Assert.False(result.PeerResolved);
        }

        [Fact]
        public void EveryHandshakeType_AllowsWithoutResolving()
        {
            // Handshake frames are exempt too, and they also reach the dispatcher's default
            // case, so they must not be allowed to carry a trusted identifier either.
            foreach (BodyType t in new[]
            {
                BodyType._handshakeClientHello,
                BodyType._handshakeServerHello,
                BodyType._handshakeClientFinal
            })
            {
                var header = new PacketHeaderModel { BodyType = t, DeviceUniqueIdentifier = "claimed" };
                var result = P2pPeerAuthorization.Authorize(header, peerIsKnown: false);

                Assert.True(result.IsAuthorized, t + " must be allowed");
                Assert.False(result.PeerResolved, t + " must not be treated as a resolved peer");
            }
        }


        [Fact]
        public void VideoResyncRequest_FlowsServerToCompanionOnly()
        {
            // The recovery half of the video contract. The Server detects the loss (dropped
            // frames, or a decoder that refuses everything because its config never arrived)
            // and the Companion is the only side that can act on it, by resending SPS/PPS and
            // forcing an IDR. A Companion accepting one inbound would mean it was asking
            // itself to resync.
            Assert.True(AtCompanion(BodyType._videoResyncRequest));
            Assert.False(AtServer(BodyType._videoResyncRequest));
        }

        [Fact]
        public void VideoResyncRequest_IsNotPreAuthentication()
        {
            // It drives capture behaviour on a learner's device, so it must require a resolved
            // peer rather than riding the onboarding exemption.
            Assert.False(P2pPeerAuthorization.IsPreAuthenticationBodyType(BodyType._videoResyncRequest));
        }

        [Fact]
        public void VideoResyncRequest_DoesNotCollideWithAnotherBodyType()
        {
            // Added by hand into a hand-numbered enum, so pin the value and its uniqueness.
            Assert.Equal(717, (int)BodyType._videoResyncRequest);

            var all = Enum.GetValues(typeof(BodyType)).Cast<BodyType>().Select(t => (int)t).ToArray();
            Assert.Equal(all.Length, all.Distinct().Count());
        }

        [Fact]
        public void NoTypeIsAccidentallyBidirectional()
        {
            // Pins the bidirectional set, so widening it has to be deliberate rather than a
            // side effect of adding a case in the wrong group.
            //
            // Was four members. _genericString was removed on 2026-07-29 when the
            // reverse-direction upload types were refused: it was bidirectional only because
            // both tiers happened to handle it, with no sender on either and no single owner.
            var bidirectional = Enum.GetValues(typeof(BodyType))
                .Cast<BodyType>()
                .Where(t => AtServer(t) && AtCompanion(t))
                .OrderBy(t => (int)t)
                .ToArray();

            Assert.Equal(
                new[] { BodyType._statement, BodyType._videoStream, BodyType._endVideoStream }
                    .OrderBy(t => (int)t).ToArray(),
                bidirectional);
        }
    }
}
