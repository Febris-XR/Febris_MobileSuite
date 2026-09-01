// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using Febris.SharedMobileLibrary.Models;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Turns a MediaCodec output buffer into a v2 frame ready for the socket.
    ///
    /// This exists as its own unit because it is the only part of the encode path that can
    /// be tested without Android: sequence numbering, header population, body-type
    /// selection and the size cap are all pure logic, while MediaProjection and MediaCodec
    /// are not testable at all. Keeping them apart means the Android side stays thin enough
    /// to review by eye.
    ///
    /// NOT thread-safe by design. MediaCodec delivers output on a single callback thread,
    /// so one packetizer belongs to one encode session and is driven from that thread.
    /// Sharing one across sessions would corrupt the sequence numbering, which is the one
    /// thing a receiver uses to detect loss and reordering.
    /// </summary>
    public sealed class VideoFramePacketizer
    {
        private readonly IFebrisP2pFrameBuilder _builder;
        private readonly string _deviceIdentifier;
        private uint _sequenceNumber;

        public VideoFramePacketizer(string deviceIdentifier)
            : this(deviceIdentifier, new FebrisP2pFrameBuilder())
        {
        }

        public VideoFramePacketizer(string deviceIdentifier, IFebrisP2pFrameBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            _deviceIdentifier = deviceIdentifier;
            _builder = builder;
        }

        /// <summary>The number the NEXT video frame will carry. Starts at 0.</summary>
        public uint NextSequenceNumber { get { return _sequenceNumber; } }

        /// <summary>
        /// Builds the codec-config frame (SPS + PPS). The receiver cannot configure its
        /// decoder until it has this, so it is sent once at session start and again on any
        /// resync. It deliberately does NOT consume a sequence number: it is not a picture,
        /// and numbering it would make the receiver see a gap in the frame sequence.
        /// </summary>
        public byte[] BuildCodecConfig(byte[] spsPps, int width, int height, int rotationDegrees)
        {
            RequireBody(spsPps, "codec config");
            RequireVideoSize(spsPps, BodyType._videoCodecConfig);

            PacketHeaderModel header = NewHeader(BodyType._videoCodecConfig);
            header.Width = width;
            header.Height = height;
            // Only when there is something to say. Rotation is nullable with
            // NullValueHandling.Ignore precisely so it costs nothing when unset, but assigning
            // a literal 0 makes it non-null, so "Rotation":0 shipped on every frame at 30fps
            // for a value no receiver reads. Keeping the field (rotation is a real future need
            // on a headset) while not paying for it until it carries information.
            if (rotationDegrees != 0)
            {
                header.Rotation = rotationDegrees;
            }
            return _builder.Build(header, spsPps);
        }

        /// <summary>
        /// Builds one encoded access unit. <paramref name="isKeyFrame"/> comes from
        /// BufferInfo.Flags and BUFFER_FLAG_KEY_FRAME, and is load bearing: the send-side
        /// drop policy must never discard a keyframe, because every frame after a dropped
        /// non-IDR is corrupt until the next one.
        /// </summary>
        public byte[] BuildVideoFrame(byte[] accessUnit, bool isKeyFrame, long ptsMicros, int width, int height, int rotationDegrees)
        {
            RequireBody(accessUnit, "video frame");
            RequireVideoSize(accessUnit, BodyType._videoFrame);

            PacketHeaderModel header = NewHeader(BodyType._videoFrame);
            header.SequenceNumber = _sequenceNumber;
            header.PtsMicros = ptsMicros;
            header.IsKeyFrame = isKeyFrame;
            header.Width = width;
            header.Height = height;
            // Only when there is something to say. Rotation is nullable with
            // NullValueHandling.Ignore precisely so it costs nothing when unset, but assigning
            // a literal 0 makes it non-null, so "Rotation":0 shipped on every frame at 30fps
            // for a value no receiver reads. Keeping the field (rotation is a real future need
            // on a headset) while not paying for it until it carries information.
            if (rotationDegrees != 0)
            {
                header.Rotation = rotationDegrees;
            }

            byte[] frame = _builder.Build(header, accessUnit);

            // Advance only after a successful build, so a rejected oversize frame does not
            // punch a hole in the sequence and make the receiver think it lost a picture.
            unchecked { _sequenceNumber++; }
            return frame;
        }

        private PacketHeaderModel NewHeader(BodyType bodyType)
        {
            return new PacketHeaderModel
            {
                BodyType = bodyType,
                PacketName = bodyType.ToString(),
                DeviceUniqueIdentifier = _deviceIdentifier
            };
        }

        private static void RequireBody(byte[] body, string what)
        {
            if (body == null || body.Length == 0)
            {
                throw new ArgumentException("Cannot build a " + what + " frame from an empty buffer.");
            }
        }

        /// <summary>
        /// Rejects here rather than letting the receiver reject it, so a runaway encoder is
        /// caught on the device that produced it instead of costing a round trip and a
        /// dropped connection. Same limit the parser enforces.
        /// </summary>
        private static void RequireVideoSize(byte[] body, BodyType bodyType)
        {
            if (body.LongLength > FebrisP2pFrame.MaxVideoBodyBytes)
            {
                throw new ArgumentException(
                    "Encoded " + bodyType + " is " + body.LongLength + " bytes, over the " +
                    FebrisP2pFrame.MaxVideoBodyBytes + " byte video cap. Lower the bitrate or resolution.");
            }
        }
    }
}
