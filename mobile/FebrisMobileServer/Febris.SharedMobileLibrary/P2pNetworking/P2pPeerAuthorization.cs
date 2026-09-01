// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Decides whether an inbound P2P frame may be processed at all.
    ///
    /// WHY THIS EXISTS. The P2P receive path had no peer check of any kind. Every frame
    /// that parsed was dispatched, and on the Server it additionally re-pointed the
    /// outbound channel: <c>UpdateIPAddress</c> resolves a device purely by the
    /// attacker-supplied <see cref="PacketHeaderModel.DeviceUniqueIdentifier"/> and
    /// overwrites that device's IP with the connecting socket's source address, which is
    /// the key <c>SocketSender</c> then uses to choose a destination. One unauthenticated
    /// frame carrying an observed identifier redirected stream control and module pushes.
    ///
    /// This gate is the chokepoint fix from docs/MOBILE_P2P_VIDEO.md section 6.3, placed
    /// at the single receive entry on each side rather than scattered per-BodyType.
    ///
    /// WHAT THIS DOES NOT DO, stated plainly so nobody mistakes it for more than it is:
    /// it is an ALLOWLIST, not authentication. <c>DeviceUniqueIdentifier</c> is a
    /// self-asserted plaintext JSON string, so a device that has observed one legitimate
    /// frame can still replay that identifier and pass this gate. Closing that requires
    /// a real handshake and an authenticated channel, which is the separate v3 frame
    /// work. What this gate does buy is that a device which has NEVER been paired cannot
    /// drive the receiver at all, and it removes the trivial outbound-hijack above.
    /// </summary>
    public static class P2pPeerAuthorization
    {
        /// <summary>
        /// Body types allowed from a peer that is not yet resolvable by identifier,
        /// because they carry their own prior-pairing anchor further down.
        ///
        /// Only <see cref="BodyType._initalize"/> qualifies. Its handler
        /// (<c>ProcessInitalization</c>) requires the device to ALREADY exist in the
        /// paired list, matched on Bluetooth name, and is the step that associates a
        /// WiFi identity onto a device paired earlier over Bluetooth. At that moment the
        /// stored <c>UniqueIdentifier</c> may still be empty, so gating it on a known
        /// identifier would break onboarding rather than secure it.
        /// </summary>
        public static bool IsPreAuthenticationBodyType(BodyType bodyType)
        {
            // The three handshake messages ARE the authentication, so they cannot
            // require it without deadlocking the connection. They are safe to exempt
            // because they carry no instruction and no payload: a peer without the PSK
            // that sends them simply fails the HMAC check and is dropped.
            return bodyType == BodyType._initalize
                || bodyType == BodyType._handshakeClientHello
                || bodyType == BodyType._handshakeServerHello
                || bodyType == BodyType._handshakeClientFinal
                // Pairing CREATES the credential, so it cannot require one. Unlike the rest of
                // this set, its safety does not rest on the channel at all: a human compares six
                // digits out of band, and a man in the middle produces two different codes.
                || bodyType == BodyType._pairingRequest
                || bodyType == BodyType._pairingResponse;
        }

        /// <summary>Which tier is evaluating an inbound frame. The legality of a body type
        /// depends on which side received it, so the caller has to say who it is.</summary>
        public enum P2pTier
        {
            /// <summary>The Mobile Server, which accepts connections (handshake responder).</summary>
            MobileServer,

            /// <summary>The Companion, which dials out (handshake initiator).</summary>
            Companion
        }

        /// <summary>
        /// Whether <paramref name="bodyType"/> may legitimately ARRIVE at
        /// <paramref name="receiver"/>.
        ///
        /// <para><b>Why this is a separate check from the peer gate.</b> The allowlist answers
        /// "is this peer known", which says nothing about whether the peer is sending
        /// something only the other side should send. Both dispatchers were direction-blind:
        /// the Server had a live inbound case for <c>_videoStreamStart</c>, which the enum
        /// itself documents as Server-to-Companion, and the Companion would happily process a
        /// <c>_videoFrame</c> it should only ever emit.</para>
        ///
        /// <para><b>Why an exhaustive table rather than a deny-list.</b> Unknown types are
        /// REFUSED, and <c>P2pPeerAuthorizationTests</c> enumerates <see cref="BodyType"/> and
        /// fails if a member is unclassified. A new body type therefore cannot be added
        /// without a deliberate direction decision, which is the same closed-set discipline
        /// already used for the pre-authentication exemptions.</para>
        ///
        /// <para><b>Genuinely bidirectional types are listed as such rather than guessed.</b>
        /// <c>_endVideoStream</c> is documented as the stop in both directions,
        /// <c>_videoStream</c> is the deprecated overloaded 667 that served as both command
        /// and payload, and <c>_statement</c> has send sites on both tiers.</para>
        /// </summary>
        public static bool IsLegalInbound(BodyType bodyType, P2pTier receiver)
        {
            switch (bodyType)
            {
                // ---- Companion to Server. Illegal arriving at the Companion. ----
                case BodyType._initalize:
                case BodyType._statusUpdate:
                // DISABLED 2026-07-29, see the "Disabled reverse-direction uploads" block below.
                // case BodyType._fileUpload:
                // case BodyType._video:
                case BodyType._handshakeClientHello:
                case BodyType._handshakeClientFinal:
                case BodyType._videoCodecConfig:
                case BodyType._videoFrame:
                case BodyType._pairingResponse:
                    return receiver == P2pTier.MobileServer;

                // ---- Server to Companion. Illegal arriving at the Server. ----
                case BodyType._module:
                case BodyType._installModule:
                case BodyType._uninstallModule:
                case BodyType._reinstallModule:
                case BodyType._removeModule:
                case BodyType._removeZippedModule:
                case BodyType._oldStatements:
                case BodyType._oldVideos:
                case BodyType._acknowledge:
                case BodyType._handshakeServerHello:
                case BodyType._videoStreamStart:
                case BodyType._videoResyncRequest:
                case BodyType._pairingRequest:
                    return receiver == P2pTier.Companion;

                // ---- Legitimately bidirectional. ----
                case BodyType._statement:        // send sites on both tiers
                case BodyType._endVideoStream:   // documented as the stop in both directions
                case BodyType._videoStream:      // deprecated 667, overloaded both ways
                // DISABLED 2026-07-29, see the block below.
                // case BodyType._genericString:    // handled on both tiers, no single owner
                    return true;

                // ---- Never legitimate from the wire. ----
                case BodyType._junk:

                // ---- Disabled reverse-direction uploads, 2026-07-29. ----
                //
                // COMMENTED OUT RATHER THAN DELETED, at the owner's instruction, in case any
                // of it is still needed. Restoring means moving these three back to the groups
                // marked above and reverting the matching test arrangement.
                //
                // The owner states the architecture directly: the Server holds module archives
                // and distributes them to devices, and the Companion sends ONLY statements out
                // of its sqlite database. It must never send a module, an archive or a recorded
                // video file back.
                //
                // NO SENDER EXISTS FOR ANY OF THE THREE, on either tier. Establishing that takes
                // more than the obvious grep, and the obvious grep is wrong: searching for the
                // literal "BodyType = BodyType._x" is the exact failure MOBILE_SESSION_HANDOFF
                // rule 2.2 records, because it misses headers built through a helper. The check
                // that actually holds is BOTH of these:
                //   1. no literal "BodyType = BodyType._fileUpload/._video/._genericString"
                //      (the one _video sender is commented out at Companion LoopLogic.cs:523), AND
                //   2. every header built from a NON-literal BodyType is accounted for. There are
                //      five: VideoFramePacketizer.NewHeader, called only with _videoCodecConfig
                //      and _videoFrame; the three handshake echoes in ClientSocketThread and
                //      ServerSocketThread, which carry handshake types; and the
                //      P2pHandshakeCoordinator constructor. None can carry these three.
                //
                // So these were handler-only types: reachable from the wire, dispatched, and
                // capable of writing to disk, for a flow that by design does not exist. That is
                // attack surface and it is also what makes the tier read as having pathways
                // that do not make sense. Refusing them enforces the architecture structurally
                // instead of by convention.
                //
                // _fileUpload IS DISPATCHED ON THE COMPANION (WiFiP2pRequestProcessing.cs:175),
                // which reads like a Server-to-Companion push and is worth understanding before
                // anyone "restores" it. It was never reachable: the ORIGINAL table classified
                // _fileUpload as Companion-to-Server, so IsLegalInbound(_fileUpload, Companion)
                // already returned false, and ProcessFileUpload is a stub that returns false with
                // its body commented out. Someone intended a Server-to-Companion upload and never
                // finished it, while the direction table said the opposite. If that feature is
                // ever wanted, classify _fileUpload as Server-to-Companion and implement the
                // handler. Do NOT restore the old grouping, which was the contradiction.
                // The working Server-to-Companion file transfer today is _module.
                //
                // NOT TOUCHED, because these ARE the live screen stream and are genuinely
                // Companion to Server: _videoFrame, _videoCodecConfig, _videoStream,
                // _endVideoStream, _videoResyncRequest, _videoStreamStart.
                case BodyType._fileUpload:
                case BodyType._video:
                case BodyType._genericString:
                    return false;

                default:
                    // A body type nobody classified. Refusing is the safe default and the
                    // enum-enumerating test means this should be unreachable in practice.
                    return false;
            }
        }

        /// <summary>True for the three handshake transport messages specifically, as
        /// opposed to the wider pre-authentication set. The socket pumps use this to
        /// route a frame into the handshake state machine instead of the normal
        /// dispatcher.</summary>
        public static bool IsHandshakeBodyType(BodyType bodyType)
        {
            return bodyType == BodyType._handshakeClientHello
                || bodyType == BodyType._handshakeServerHello
                || bodyType == BodyType._handshakeClientFinal;
        }

        /// <summary>
        /// Evaluate a parsed frame. <paramref name="peerIsKnown"/> is supplied by the
        /// caller because the trust anchor differs per tier: the Server can resolve a
        /// paired device by identifier, and the Companion currently has no stored server
        /// identity to resolve against at all.
        /// </summary>
        public static P2pAuthorizationResult Authorize(PacketHeaderModel header, bool peerIsKnown)
        {
            if (header == null)
            {
                return P2pAuthorizationResult.Deny("frame carried no header");
            }

            if (IsPreAuthenticationBodyType(header.BodyType))
            {
                // Allowed to PROCEED, but the identifier is not trusted. See
                // P2pAuthorizationResult.PeerResolved: this branch runs before both checks
                // below, so nothing downstream may look a device up from this header.
                return P2pAuthorizationResult.AllowUnresolved();
            }

            if (string.IsNullOrWhiteSpace(header.DeviceUniqueIdentifier))
            {
                return P2pAuthorizationResult.Deny(
                    "frame of type " + header.BodyType + " carried no device identifier");
            }

            if (!peerIsKnown)
            {
                return P2pAuthorizationResult.Deny(
                    "device identifier is not a known paired peer, rejecting " + header.BodyType);
            }

            return P2pAuthorizationResult.AllowResolved();
        }
    }

    /// <summary>Outcome of a <see cref="P2pPeerAuthorization"/> check. Carries the reason
    /// so the caller can log WHY a frame was dropped, which is the difference between a
    /// diagnosable rejection and a silently vanishing stream.</summary>
    public struct P2pAuthorizationResult
    {
        public bool IsAuthorized { get; private set; }
        public string DenyReason { get; private set; }

        /// <summary>
        /// True only when the frame was allowed because its device identifier resolved to a
        /// KNOWN PAIRED PEER, as opposed to being waved through by the pre-authentication
        /// exemption.
        ///
        /// WHY THE CALLER NEEDS THIS. Allowing a frame and trusting its identifier are two
        /// different decisions, and collapsing them is what left the outbound-hijack open.
        /// <c>UpdateIPAddress</c> resolves a device purely by the identifier in the header and
        /// overwrites the address <c>SocketSender</c> routes on. An <c>_initalize</c> frame
        /// clears <see cref="P2pPeerAuthorization.Authorize"/> by exemption BEFORE either the
        /// empty-identifier check or the known-peer check runs, so an unpaired device could
        /// re-point another device's outbound channel with one frame.
        ///
        /// Gate anything that trusts the identifier on this, not on
        /// <see cref="IsAuthorized"/>.
        /// </summary>
        public bool PeerResolved { get; private set; }

        /// <summary>Allowed because the identifier resolved to a known paired peer.</summary>
        public static P2pAuthorizationResult AllowResolved()
        {
            return new P2pAuthorizationResult { IsAuthorized = true, DenyReason = null, PeerResolved = true };
        }

        /// <summary>Allowed by the pre-authentication exemption. The identifier is NOT trusted:
        /// the frame may proceed, but nothing may be looked up or rewritten from it.</summary>
        public static P2pAuthorizationResult AllowUnresolved()
        {
            return new P2pAuthorizationResult { IsAuthorized = true, DenyReason = null, PeerResolved = false };
        }

        public static P2pAuthorizationResult Deny(string reason)
        {
            return new P2pAuthorizationResult { IsAuthorized = false, DenyReason = reason, PeerResolved = false };
        }
    }
}
