// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>One queued access unit.</summary>
    public sealed class VideoQueueItem
    {
        public byte[] Payload { get; }
        public bool IsKeyFrame { get; }
        public bool IsCodecConfig { get; }
        public uint SequenceNumber { get; }
        public long PtsMicros { get; }

        /// <summary>Picture dimensions, carried ONLY on a codec-config item because that is the
        /// item the consumer configures the decoder from. Zero on ordinary frames, which never
        /// need them.</summary>
        public int Width { get; }
        public int Height { get; }

        public VideoQueueItem(byte[] payload, bool isKeyFrame, bool isCodecConfig, uint sequenceNumber, long ptsMicros)
            : this(payload, isKeyFrame, isCodecConfig, sequenceNumber, ptsMicros, 0, 0)
        {
        }

        public VideoQueueItem(byte[] payload, bool isKeyFrame, bool isCodecConfig, uint sequenceNumber, long ptsMicros, int width, int height)
        {
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            IsKeyFrame = isKeyFrame;
            IsCodecConfig = isCodecConfig;
            SequenceNumber = sequenceNumber;
            PtsMicros = ptsMicros;
            Width = width;
            Height = height;
        }

        /// <summary>Items that must never be discarded under pressure. Dropping either one
        /// corrupts every dependent frame until the next IDR, so losing a run of ordinary
        /// frames is strictly better than losing one of these.</summary>
        public bool IsProtected { get { return IsKeyFrame || IsCodecConfig; } }
    }

    /// <summary>
    /// Bounded FIFO between the network and the decoder, with the drop policy from
    /// docs/MOBILE_P2P_VIDEO.md 3.4.
    ///
    /// WHY A QUEUE AT ALL. Without one, a decoder that falls behind back-pressures into the
    /// receive loop, which back-pressures into the socket, which stalls control traffic
    /// behind video. Bounding it means the slowest component sheds load instead of stopping
    /// the pipeline.
    ///
    /// WHY DROP-OLDEST. In live video the newest frame is the valuable one. Dropping the
    /// front discards the most stale picture, which is what a viewer wants when the link
    /// cannot keep up.
    ///
    /// WHY PROTECTION MATTERS MORE THAN THE POLICY. H.264 frames depend on the preceding
    /// IDR. Dropping a keyframe corrupts everything after it until the next one, and
    /// dropping codec config leaves the decoder unable to start at all. So both are exempt
    /// from eviction, and when only protected items remain the INCOMING frame is refused
    /// instead. Refusing one new frame costs a single picture; evicting an IDR costs
    /// seconds of garbage.
    ///
    /// NOT thread-safe. One producer, one consumer, and the caller owns the lock. Adding
    /// internal locking would hide the ordering guarantee this exists to provide.
    /// </summary>
    public sealed class VideoFrameQueue
    {
        private readonly LinkedList<VideoQueueItem> _items = new LinkedList<VideoQueueItem>();
        private readonly int _capacity;

        public VideoFrameQueue(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
            _capacity = capacity;
        }

        public int Count { get { return _items.Count; } }
        public int Capacity { get { return _capacity; } }

        /// <summary>
        /// Frames discarded since the counter was last cleared. A non-zero value means the
        /// stream is now corrupt until the next IDR, so the consumer should ask the encoder
        /// for a sync frame and then call <see cref="ClearDropCount"/>.
        /// </summary>
        public int DroppedFrames { get; private set; }

        public void ClearDropCount() { DroppedFrames = 0; }

        /// <summary>
        /// Enqueues an item, evicting the oldest UNPROTECTED item if full.
        /// Returns false when the item could not be accepted, which only happens when the
        /// queue is full of protected items.
        /// </summary>
        public bool TryEnqueue(VideoQueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_items.Count >= _capacity)
            {
                if (!TryEvictOldestUnprotected())
                {
                    // Every queued item is a keyframe or codec config. Refusing the incoming
                    // frame is the lesser harm even when the incoming frame is itself
                    // protected, because evicting a queued IDR would corrupt the frames
                    // already accepted behind it.
                    DroppedFrames++;
                    return false;
                }
            }

            _items.AddLast(item);
            return true;
        }

        public bool TryDequeue(out VideoQueueItem item)
        {
            if (_items.Count == 0)
            {
                item = null;
                return false;
            }
            item = _items.First.Value;
            _items.RemoveFirst();
            return true;
        }

        /// <summary>Drops everything, including protected items. For teardown and for a
        /// hard resync where the queued backlog is known to be worthless.</summary>
        public void Clear()
        {
            _items.Clear();
        }

        private bool TryEvictOldestUnprotected()
        {
            LinkedListNode<VideoQueueItem> node = _items.First;
            while (node != null)
            {
                if (!node.Value.IsProtected)
                {
                    _items.Remove(node);
                    DroppedFrames++;
                    return true;
                }
                node = node.Next;
            }
            return false;
        }
    }
}
