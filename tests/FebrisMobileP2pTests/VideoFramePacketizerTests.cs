// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Linq;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// The only part of the encode path testable without Android. MediaProjection and
    /// MediaCodec are not testable at all, so keeping this logic separate is what makes the
    /// Android side thin enough to review by eye.
    /// </summary>
    public class VideoFramePacketizerTests
    {
        private const string DeviceId = "0123456789abcdef";
        private const int W = 1280;
        private const int H = 720;

        private static VideoFramePacketizer New() => new VideoFramePacketizer(DeviceId);
        private static byte[] Unit(int size = 64) => Enumerable.Range(0, size).Select(i => (byte)i).ToArray();

        private static (PacketHeaderModel Header, byte[] Body) Parse(byte[] frame)
        {
            return new FebrisP2pFrameParser().ParseBytes(frame);
        }

        [Fact]
        public void VideoFrame_CarriesEveryFieldTheDecoderNeeds()
        {
            byte[] accessUnit = Unit();
            var (header, body) = Parse(New().BuildVideoFrame(accessUnit, true, 1234567L, W, H, 90));

            Assert.Equal(BodyType._videoFrame, header.BodyType);
            Assert.Equal(accessUnit, body);
            Assert.Equal(0u, header.SequenceNumber);
            Assert.Equal(1234567L, header.PtsMicros);
            Assert.True(header.IsKeyFrame);
            Assert.Equal(W, header.Width);
            Assert.Equal(H, header.Height);
            Assert.Equal(90, header.Rotation);
            Assert.Equal(DeviceId, header.DeviceUniqueIdentifier);
        }


        [Fact]
        public void ZeroRotation_IsOmittedFromTheWireEntirely()
        {
            // Rotation is nullable with NullValueHandling.Ignore so it costs nothing when
            // unset, but the packetizer assigned a literal 0, which made it non-null and put
            // "Rotation":0 on every frame at 30fps for a value no receiver reads. The field is
            // kept because rotation is a real future need on a headset; it just does not ship
            // until it carries information.
            var (header, _) = Parse(New().BuildVideoFrame(Unit(), false, 0L, W, H, 0));
            Assert.Null(header.Rotation);

            var (config, _) = Parse(New().BuildCodecConfig(Unit(40), W, H, 0));
            Assert.Null(config.Rotation);
        }

        [Fact]
        public void NonZeroRotation_StillCrossesTheWire()
        {
            var (header, _) = Parse(New().BuildVideoFrame(Unit(), false, 0L, W, H, 270));
            Assert.Equal(270, header.Rotation);
        }

        [Fact]
        public void SequenceNumber_IsMonotonicAcrossFrames()
        {
            // The only orderable value on the wire. MessageId is a random Guid v4, so
            // without this the receiver cannot detect loss or reordering at all.
            var p = New();
            for (uint expected = 0; expected < 5; expected++)
            {
                var (header, _) = Parse(p.BuildVideoFrame(Unit(), false, expected * 33_000L, W, H, 0));
                Assert.Equal(expected, header.SequenceNumber);
            }
            Assert.Equal(5u, p.NextSequenceNumber);
        }

        [Fact]
        public void CodecConfig_DoesNotConsumeASequenceNumber()
        {
            // SPS/PPS is not a picture. Numbering it would make the receiver see a gap in
            // the frame sequence and conclude it had lost one.
            var p = New();
            p.BuildCodecConfig(Unit(40), W, H, 0);
            Assert.Equal(0u, p.NextSequenceNumber);

            var (header, _) = Parse(p.BuildVideoFrame(Unit(), true, 0L, W, H, 0));
            Assert.Equal(0u, header.SequenceNumber);
        }

        [Fact]
        public void CodecConfig_CarriesDimensionsButNoFrameFields()
        {
            var (header, body) = Parse(New().BuildCodecConfig(Unit(40), W, H, 180));

            Assert.Equal(BodyType._videoCodecConfig, header.BodyType);
            Assert.Equal(Unit(40), body);
            Assert.Equal(W, header.Width);
            Assert.Equal(H, header.Height);
            Assert.Equal(180, header.Rotation);

            // Not a picture, so these are meaningless here and must be absent.
            Assert.Null(header.SequenceNumber);
            Assert.Null(header.PtsMicros);
            Assert.Null(header.IsKeyFrame);
        }

        [Fact]
        public void KeyFrameFlag_RoundTripsBothWays()
        {
            // Load bearing for the drop policy: a dropped non-IDR corrupts every frame
            // until the next keyframe, so the receiver must be able to tell them apart.
            var p = New();
            Assert.True(Parse(p.BuildVideoFrame(Unit(), true, 0L, W, H, 0)).Header.IsKeyFrame);
            Assert.False(Parse(p.BuildVideoFrame(Unit(), false, 0L, W, H, 0)).Header.IsKeyFrame);
        }

        [Fact]
        public void OversizeAccessUnit_IsRejectedOnTheSendingDevice()
        {
            // Better to fail on the device that produced it than to spend a round trip and
            // have the receiver drop the connection.
            byte[] tooBig = new byte[FebrisP2pFrame.MaxVideoBodyBytes + 1];
            Assert.Throws<ArgumentException>(() => New().BuildVideoFrame(tooBig, false, 0L, W, H, 0));
        }

        [Fact]
        public void RejectedOversizeFrame_DoesNotPunchAHoleInTheSequence()
        {
            // If a rejected frame still advanced the counter, the receiver would see a gap
            // and believe it had lost a picture it was never sent.
            var p = New();
            p.BuildVideoFrame(Unit(), true, 0L, W, H, 0);          // seq 0
            Assert.Equal(1u, p.NextSequenceNumber);

            Assert.Throws<ArgumentException>(
                () => p.BuildVideoFrame(new byte[FebrisP2pFrame.MaxVideoBodyBytes + 1], false, 0L, W, H, 0));

            Assert.Equal(1u, p.NextSequenceNumber);                 // unchanged
            var (header, _) = Parse(p.BuildVideoFrame(Unit(), false, 0L, W, H, 0));
            Assert.Equal(1u, header.SequenceNumber);                // contiguous
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public void EmptyBuffer_IsRejected(int? size)
        {
            byte[] body = size.HasValue ? new byte[size.Value] : null;
            Assert.Throws<ArgumentException>(() => New().BuildVideoFrame(body, false, 0L, W, H, 0));
            Assert.Throws<ArgumentException>(() => New().BuildCodecConfig(body, W, H, 0));
        }

        [Fact]
        public void FramesSurviveTheRealWireReader()
        {
            // End to end against the same length-driven reader the socket pumps use, so the
            // packetizer is proven against the actual transport rather than just the parser.
            var p = New();
            byte[] config = p.BuildCodecConfig(Unit(40), W, H, 0);
            byte[] first = p.BuildVideoFrame(Unit(100), true, 0L, W, H, 0);
            byte[] second = p.BuildVideoFrame(Unit(80), false, 33_000L, W, H, 0);

            byte[] stream = config.Concat(first).Concat(second).ToArray();
            var parser = new FebrisP2pFrameParser();

            using (var ms = new System.IO.MemoryStream(stream))
            {
                Assert.Equal(config, parser.ReadFrameBytesAsync(ms).GetAwaiter().GetResult());
                Assert.Equal(first, parser.ReadFrameBytesAsync(ms).GetAwaiter().GetResult());
                Assert.Equal(second, parser.ReadFrameBytesAsync(ms).GetAwaiter().GetResult());
                Assert.Null(parser.ReadFrameBytesAsync(ms).GetAwaiter().GetResult());
            }
        }

        [Fact]
        public void SequenceNumber_WrapsWithoutThrowing()
        {
            // uint wrap is ~19 months at 30fps, but an unchecked wrap must not become an
            // OverflowException on a headset mid-session.
            var p = New();
            typeof(VideoFramePacketizer)
                .GetField("_sequenceNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(p, uint.MaxValue);

            Assert.Equal(uint.MaxValue, Parse(p.BuildVideoFrame(Unit(), false, 0L, W, H, 0)).Header.SequenceNumber);
            Assert.Equal(0u, Parse(p.BuildVideoFrame(Unit(), false, 0L, W, H, 0)).Header.SequenceNumber);
        }
    }
}
