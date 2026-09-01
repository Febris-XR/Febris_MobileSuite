// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Linq;
using Febris.SharedMobileLibrary.P2pNetworking;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// The drop policy from docs/MOBILE_P2P_VIDEO.md 3.4. Worth testing hard because the
    /// failure mode is not a crash: dropping the wrong frame produces seconds of visible
    /// garbage that looks like a decoder bug rather than a queue bug.
    /// </summary>
    public class VideoFrameQueueTests
    {
        private static VideoQueueItem Frame(uint seq) =>
            new VideoQueueItem(new byte[] { 1 }, isKeyFrame: false, isCodecConfig: false, sequenceNumber: seq, ptsMicros: seq * 33_000L);

        private static VideoQueueItem KeyFrame(uint seq) =>
            new VideoQueueItem(new byte[] { 2 }, isKeyFrame: true, isCodecConfig: false, sequenceNumber: seq, ptsMicros: seq * 33_000L);

        private static VideoQueueItem Config() =>
            new VideoQueueItem(new byte[] { 3 }, isKeyFrame: false, isCodecConfig: true, sequenceNumber: 0, ptsMicros: 0);

        private static uint[] Drain(VideoFrameQueue q)
        {
            var seen = new System.Collections.Generic.List<uint>();
            while (q.TryDequeue(out VideoQueueItem item)) seen.Add(item.SequenceNumber);
            return seen.ToArray();
        }

        [Fact]
        public void PreservesOrder()
        {
            // Order is the whole point. There is nothing sortable downstream, so if the
            // queue reorders, the decoder receives garbage it cannot detect.
            var q = new VideoFrameQueue(8);
            for (uint i = 0; i < 5; i++) Assert.True(q.TryEnqueue(Frame(i)));

            Assert.Equal(new uint[] { 0, 1, 2, 3, 4 }, Drain(q));
        }

        [Fact]
        public void WhenFull_DropsOldest_KeepingTheNewest()
        {
            // Live video: the newest frame is the valuable one.
            var q = new VideoFrameQueue(3);
            for (uint i = 0; i < 5; i++) q.TryEnqueue(Frame(i));

            Assert.Equal(3, q.Count);
            Assert.Equal(new uint[] { 2, 3, 4 }, Drain(q));
            Assert.Equal(2, q.DroppedFrames);
        }

        [Fact]
        public void NeverEvictsAKeyFrame()
        {
            // The one that matters. Dropping an IDR corrupts every frame after it until the
            // next one, so an ordinary frame must be evicted in its place even though the
            // keyframe is older.
            var q = new VideoFrameQueue(3);
            q.TryEnqueue(KeyFrame(0));
            q.TryEnqueue(Frame(1));
            q.TryEnqueue(Frame(2));

            Assert.True(q.TryEnqueue(Frame(3)));

            // Frame 1 evicted, not the older keyframe.
            Assert.Equal(new uint[] { 0, 2, 3 }, Drain(q));
        }

        [Fact]
        public void NeverEvictsCodecConfig()
        {
            // Without SPS/PPS the decoder cannot be configured at all, so this is even less
            // droppable than a keyframe.
            var q = new VideoFrameQueue(2);
            q.TryEnqueue(Config());
            q.TryEnqueue(Frame(1));

            Assert.True(q.TryEnqueue(Frame(2)));

            var remaining = new System.Collections.Generic.List<VideoQueueItem>();
            while (q.TryDequeue(out VideoQueueItem item)) remaining.Add(item);

            Assert.True(remaining[0].IsCodecConfig);
            Assert.Equal(2u, remaining[1].SequenceNumber);
        }


        [Fact]
        public void CodecConfigItem_CarriesTheDimensionsTheDecoderNeeds()
        {
            // Codec config now travels through the queue instead of being applied on the
            // dispatch thread, so the item has to carry everything Configure() takes. Without
            // the dimensions the consumer would have to keep them in a side channel, which is
            // exactly the shared mutable state routing through the queue removes.
            var item = new VideoQueueItem(new byte[] { 9 }, isKeyFrame: false, isCodecConfig: true,
                                          sequenceNumber: 0, ptsMicros: 0, width: 1280, height: 720);

            Assert.True(item.IsCodecConfig);
            Assert.True(item.IsProtected);
            Assert.Equal(1280, item.Width);
            Assert.Equal(720, item.Height);
        }

        [Fact]
        public void OrdinaryFrames_CarryNoDimensions()
        {
            // The five-argument constructor is still the one the frame path uses, and it must
            // keep meaning "no dimensions" rather than silently inventing some.
            var item = new VideoQueueItem(new byte[] { 1 }, false, false, 7, 33_000L);

            Assert.Equal(0, item.Width);
            Assert.Equal(0, item.Height);
            Assert.False(item.IsProtected);
        }

        [Fact]
        public void ARealCodecConfigItem_SurvivesPressureThatEvictsFrames()
        {
            // The protection was unreachable before, because nothing ever constructed an item
            // with isCodecConfig: true. Now that the receive path does, prove it actually
            // works end to end rather than only in the synthetic test above.
            var q = new VideoFrameQueue(3);
            q.TryEnqueue(new VideoQueueItem(new byte[] { 9 }, false, true, 0, 0, 1280, 720));
            for (uint i = 1; i < 10; i++) q.TryEnqueue(Frame(i));

            Assert.True(q.TryDequeue(out VideoQueueItem first));
            Assert.True(first.IsCodecConfig);
            Assert.Equal(1280, first.Width);
        }

        [Fact]
        public void WhenFullOfProtectedItems_RefusesTheIncomingFrame()
        {
            // Refusing one new frame costs a single picture. Evicting a queued IDR would
            // corrupt the frames already accepted behind it, so refusal is the lesser harm.
            var q = new VideoFrameQueue(2);
            q.TryEnqueue(Config());
            q.TryEnqueue(KeyFrame(1));

            Assert.False(q.TryEnqueue(Frame(2)));
            Assert.Equal(2, q.Count);
            Assert.Equal(1, q.DroppedFrames);
        }

        [Fact]
        public void WhenFullOfProtectedItems_EvenAnIncomingKeyFrameIsRefused()
        {
            // Deliberate: protecting the incoming item by evicting a queued one would break
            // the frames already behind it. Nothing queued is sacrificed for a newcomer.
            var q = new VideoFrameQueue(2);
            q.TryEnqueue(KeyFrame(0));
            q.TryEnqueue(KeyFrame(1));

            Assert.False(q.TryEnqueue(KeyFrame(2)));
            Assert.Equal(new uint[] { 0, 1 }, Drain(q));
        }

        [Fact]
        public void DropCount_DrivesTheResyncDecision()
        {
            // A non-zero count means the stream is corrupt until the next IDR, which is the
            // signal to call RequestKeyFrame. It must be explicitly clearable so the caller
            // does not request a sync frame on every subsequent tick.
            var q = new VideoFrameQueue(2);
            for (uint i = 0; i < 5; i++) q.TryEnqueue(Frame(i));

            Assert.Equal(3, q.DroppedFrames);
            q.ClearDropCount();
            Assert.Equal(0, q.DroppedFrames);
        }

        [Fact]
        public void Clear_DiscardsEverythingIncludingProtectedItems()
        {
            // For teardown and hard resync, where the backlog is known to be worthless.
            var q = new VideoFrameQueue(4);
            q.TryEnqueue(Config());
            q.TryEnqueue(KeyFrame(1));
            q.Clear();

            Assert.Equal(0, q.Count);
            Assert.False(q.TryDequeue(out _));
        }

        [Fact]
        public void EmptyQueue_DequeuesFalseNotNullReference()
        {
            var q = new VideoFrameQueue(2);
            Assert.False(q.TryDequeue(out VideoQueueItem item));
            Assert.Null(item);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CapacityMustBePositive(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VideoFrameQueue(capacity));
        }

        [Fact]
        public void NullPayload_IsRejectedAtConstruction()
        {
            Assert.Throws<ArgumentNullException>(() => new VideoQueueItem(null, false, false, 0, 0));
        }

        [Fact]
        public void SustainedOverflow_KeepsExactlyCapacityAndTheLatestFrames()
        {
            // The steady state under a link that cannot keep up: bounded memory, newest
            // frames retained, and a drop count that reflects reality.
            var q = new VideoFrameQueue(5);
            for (uint i = 0; i < 100; i++) q.TryEnqueue(Frame(i));

            Assert.Equal(5, q.Count);
            Assert.Equal(95, q.DroppedFrames);
            Assert.Equal(new uint[] { 95, 96, 97, 98, 99 }, Drain(q));
        }
    }
}
