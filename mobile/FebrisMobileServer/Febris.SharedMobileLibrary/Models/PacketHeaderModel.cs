// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.P2pNetworking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Models
{
    public class PacketHeaderModel
    {
        //package data
        [JsonProperty("BodyType")]
        public BodyType BodyType { get; set; }
        [JsonProperty("PacketName")]
        public string PacketName { get; set; }

        // BodySize removed. It had no setter and no reader anywhere in the repo, and being a
        // non-nullable long it shipped "BodySize":0 on every frame, which at 30fps of video is
        // pure overhead for a field nothing consumed. The real body length has lived in the v2
        // preamble since the framing rewrite (FebrisP2pFrame, offset 9), where the reader
        // actually needs it BEFORE the header is parsed, so a header copy could never have
        // been authoritative anyway.
        //
        // Removing a JSON property is safe in both directions: Newtonsoft ignores members it
        // does not recognise, so a peer still sending it is unaffected.

        //Wifi data
        [JsonProperty("DeviceUniqueIdentifier")]
        public string DeviceUniqueIdentifier { get; set; }

        //Bluetooth Data
        [JsonProperty("BlueToothDeviceName")]
        public string BlueToothDeviceName { get; set; }
        [JsonProperty("BlueToothDeviceAlias")]
        public string BlueToothDeviceAlias { get; set; }
        [JsonProperty("BlueToothDeviceType")]
        public string BlueToothDeviceType { get; set; }
        //[JsonProperty("BlueToothMacAddress")]
        //public string BlueToothDeviceMacAddress { get; set; }

        // MP2P-8: end-to-end acknowledgement.
        //
        // MessageId is a UUID v4 the sender stamps onto every outgoing frame. On
        // the receive side, the dispatcher emits an _acknowledge frame back with
        // InResponseTo = the original MessageId + AckStatus reflecting what
        // happened during processing. The sender keeps a
        // ConcurrentDictionary<Guid, TaskCompletionSource<AckStatus>> keyed by
        // MessageId in FebrisP2pAckTracker; the TCS resolves when the ack
        // arrives (or times out, or the socket drops).
        //
        // Backward compatibility: legacy peers running pre-MP2P-8 code don't
        // populate these fields, so they default to Guid.Empty / null on the
        // wire. The receiver treats Guid.Empty as "no tracking requested" and
        // skips ack-emission; the sender doesn't register a tracker entry, so
        // there is nothing to resolve. The protocol degrades gracefully -- old
        // peers keep working, new peers get end-to-end delivery confirmation
        // when both sides speak MP2P-8.
        [JsonProperty("MessageId")]
        public Guid MessageId { get; set; }

        [JsonProperty("InResponseTo", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? InResponseTo { get; set; }

        [JsonProperty("AckStatus", NullValueHandling = NullValueHandling.Ignore)]
        public AckStatus? AckStatus { get; set; }

        // MP2P-VIDEO: fields for the encoded screen stream (docs/MOBILE_P2P_VIDEO.md).
        //
        // Every one of these is nullable with NullValueHandling.Ignore, following the
        // MessageId/InResponseTo precedent above, so they serialize to NOTHING on the
        // frames that do not carry video. That matters: the JSON header is already 234
        // bytes and is paid per frame, and at 30-60fps a fatter header would be paid
        // 30-60 times a second for no reason.
        //
        // None of this is recoverable from the elementary stream by the receiver, which
        // is why it has to be on the wire:
        //  - a MediaCodec decoder cannot be Configure()d without width and height
        //  - Rotation is a display property, not an H.264 property, and a headset rotates
        //  - PtsMicros comes from BufferInfo and is not carried in-band
        //  - IsKeyFrame drives the drop policy, which must never discard an IDR
        //  - SequenceNumber is the only orderable value on the wire. MessageId is a
        //    random Guid v4, so it can detect neither reordering nor a gap.

        /// <summary>Monotonic per-stream counter. Detects loss and reordering.</summary>
        [JsonProperty("SequenceNumber", NullValueHandling = NullValueHandling.Ignore)]
        public uint? SequenceNumber { get; set; }

        /// <summary>Presentation timestamp in microseconds, from MediaCodec BufferInfo.</summary>
        [JsonProperty("PtsMicros", NullValueHandling = NullValueHandling.Ignore)]
        public long? PtsMicros { get; set; }

        /// <summary>True when the access unit is an IDR (BUFFER_FLAG_KEY_FRAME).
        /// A non-key frame may be dropped under pressure; a key frame must not be.</summary>
        [JsonProperty("IsKeyFrame", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsKeyFrame { get; set; }

        /// <summary>Display rotation in degrees (0, 90, 180, 270) at capture time.</summary>
        [JsonProperty("Rotation", NullValueHandling = NullValueHandling.Ignore)]
        public int? Rotation { get; set; }

        /// <summary>Encoded width in pixels. Sent explicitly to avoid an SPS parser in C#.</summary>
        [JsonProperty("Width", NullValueHandling = NullValueHandling.Ignore)]
        public int? Width { get; set; }

        /// <summary>Encoded height in pixels.</summary>
        [JsonProperty("Height", NullValueHandling = NullValueHandling.Ignore)]
        public int? Height { get; set; }
    }
    public enum BodyType
    {
        _junk=0,
        _initalize=5,
        _statusUpdate =50,
        _fileUpload =173,
        _statement=195,
        _oldStatements=215,
        _video=225,
        _oldVideos=243,
        _module=276,
        _installModule=290,
        _uninstallModule=335,
        _reinstallModule=472,
        _genericString=503,
        _acknowledge=567,
        // MP2P-3 handshake transport. These carry the three-way HMAC exchange in
        // P2pNetworking/Crypto/FebrisP2pHandshake. They are necessarily
        // PRE-AUTHENTICATION: they are the messages that establish authentication, so
        // they cannot themselves require it. P2pPeerAuthorization exempts exactly these
        // three plus _initalize, and a test enumerates the enum to keep that set closed.
        /// <summary>Companion to Server. Body is the 16-byte client nonce.</summary>
        _handshakeClientHello=601,
        /// <summary>Server to Companion. Body is server nonce (16B) followed by
        /// server response HMAC (32B).</summary>
        _handshakeServerHello=613,
        /// <summary>Companion to Server. Body is the 32-byte client final HMAC.</summary>
        _handshakeClientFinal=629,
        // DEPRECATED for new senders. 667 was overloaded: it served BOTH as the Server's
        // "start streaming" command AND as the carrier for every screenshot the Companion
        // sent back. It stays accepted so a mixed-version pair keeps working for one
        // release, and it is deliberately NOT redefined: there is no version negotiation
        // in PacketHeaderModel, so redefining it would make old and new builds silently
        // misinterpret each other rather than fail loudly.
        // Note PacketName cannot be used to tell them apart either, because
        // RequestHelper.cs:36 labels the END packet "Start Video Stream".
        _videoStream=667,
        // MP2P-VIDEO (docs/MOBILE_P2P_VIDEO.md section 3.3). Splits the old 667 into an
        // explicit command and three distinct payload kinds.
        /// <summary>Server to Companion. Start capture. Empty body.</summary>
        _videoStreamStart=671,
        /// <summary>Companion to Server. SPS + PPS, the BUFFER_FLAG_CODEC_CONFIG output.
        /// The Server cannot Configure() its decoder until it has this.</summary>
        _videoCodecConfig=689,
        /// <summary>Companion to Server. One H.264 access unit. Carries SequenceNumber,
        /// PtsMicros and IsKeyFrame in the header.</summary>
        _videoFrame=703,
        /// <summary>Server to Companion. Empty body. "Resend the codec config and force an
        /// IDR now."
        ///
        /// The recovery half of the video contract, which was built on both sides and joined
        /// by nothing: the Server counted dropped frames and the Companion implemented
        /// RequestKeyFrame and cached its SPS/PPS, but no message existed to connect them, so
        /// neither was ever called. Without it, losing the one-shot codec config leaves the
        /// decoder unconfigured and the screen black for the rest of the session, and a
        /// dropped non-IDR corrupts every frame until the next scheduled keyframe two seconds
        /// later.
        ///
        /// Additive: this is a new enum member on a JSON header, so a peer that does not know
        /// it simply refuses an unclassified type. No wire version bump.</summary>
        _videoResyncRequest=717,
        /// <summary>Server to Companion. Body is the Server's 64-byte ephemeral P-256 public
        /// key (X||Y). Opens a numeric-comparison pairing ceremony, docs/MOBILE_AUTH.md 4.1.
        ///
        /// PRE-AUTHENTICATION by necessity: pairing is what CREATES the credential, so it
        /// cannot require one. That is safe here in a way it is not elsewhere, because the
        /// ceremony's security comes from a human comparing six digits out of band, not from
        /// anything the channel asserts.</summary>
        _pairingRequest=761,
        /// <summary>Companion to Server. Body is the Companion's 64-byte ephemeral P-256
        /// public key. Completes the exchange; both sides can then derive the code.</summary>
        _pairingResponse=787,
        // Serves as the stream STOP in both directions. Already clean and
        // single-purpose, so MOBILE_P2P_VIDEO 3.3 sanctions reusing it rather than
        // minting a redundant _videoStreamStop.
        _endVideoStream=734,
        _removeModule=826,
        //_removeApp,
        _removeZippedModule=937



    }
}
