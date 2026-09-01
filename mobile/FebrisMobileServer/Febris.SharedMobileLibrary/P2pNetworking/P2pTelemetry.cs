// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Thread-safe per-<see cref="AckStatus"/> counter, surfaced for debug UIs and
    /// for the next operations-visibility tier of the MDM roadmap. The counters are
    /// incremented inside <see cref="FebrisP2pAckTracker"/> whenever a pending ack
    /// is resolved (success, failure, timeout, drained) -- meaning every outgoing
    /// frame that asked for an ack contributes exactly one increment somewhere in
    /// this table by the end of its lifecycle.
    ///
    /// <para>
    /// <b>Why a counter rather than a log scan.</b> The framer / parser / dispatcher
    /// emit structured log lines via <see cref="IFebrisP2pLogger"/>, which is the
    /// right shape for forensic debugging but the wrong shape for "what's my error
    /// rate?". Counters answer the rate question without parsing log files. The
    /// debug UI (TBD) reads them; ops dashboards can scrape them via a future
    /// host-exposed endpoint.
    /// </para>
    ///
    /// <para>
    /// <b>Concurrency.</b> Each counter slot is a <see cref="long"/> read /
    /// updated via <see cref="Interlocked.Increment(ref long)"/> and
    /// <see cref="Interlocked.Read(ref long)"/>. The slot table itself is fixed
    /// at construction time (one slot per declared <see cref="AckStatus"/> value),
    /// so no dictionary mutation happens at runtime.
    /// </para>
    ///
    /// <para>
    /// <b>Lifetime.</b> The class is a singleton via <see cref="Instance"/>, but
    /// a fresh instance can be constructed for tests that need isolation from
    /// the shared counter state. Per-instance, the counters accumulate for the
    /// life of the instance -- there is no rolling window. Callers that want a
    /// rate (per minute, per hour) take periodic <see cref="Snapshot"/> reads
    /// and diff them client-side.
    /// </para>
    /// </summary>
    public sealed class P2pTelemetry
    {
        /// <summary>Process-wide shared singleton. The tracker, framer, parser,
        /// and dispatcher all increment this instance. Tests that need isolation
        /// can <c>new P2pTelemetry()</c> instead.</summary>
        public static readonly P2pTelemetry Instance = new P2pTelemetry();

        // Slots indexed by AckStatus numeric value. The enum has stable numeric
        // values (Success=0, Failure_BadHeader=1, ..., Failure_Other=99), so the
        // slot table is sized to cover the largest declared value + 1 = 100.
        // Sparse but trivial -- every increment is O(1) and the working set is
        // small enough to live in L1 cache.
        private const int SlotCount = 100;
        private readonly long[] _counters = new long[SlotCount];

        /// <summary>Atomically increment the counter for <paramref name="status"/>.
        /// Thread-safe; non-throwing for any defined enum value.</summary>
        public void Increment(AckStatus status)
        {
            int slot = (int)status;
            if (slot < 0 || slot >= SlotCount)
            {
                // Out-of-range enum value (someone added a status without bumping
                // SlotCount). Fall back to Failure_Other rather than crashing the
                // caller -- telemetry must never throw.
                slot = (int)AckStatus.Failure_Other;
            }
            Interlocked.Increment(ref _counters[slot]);
        }

        /// <summary>Read the current count for <paramref name="status"/>. Atomic
        /// in the sense that the long read is non-torn; readers see a value that
        /// existed at some point during the call.</summary>
        public long Get(AckStatus status)
        {
            int slot = (int)status;
            if (slot < 0 || slot >= SlotCount) return 0;
            return Interlocked.Read(ref _counters[slot]);
        }

        /// <summary>Snapshot every counter currently being tracked, keyed by
        /// <see cref="AckStatus"/>. Returns an immutable copy; safe to enumerate
        /// while writers continue incrementing.</summary>
        public IReadOnlyDictionary<AckStatus, long> Snapshot()
        {
            // Enumerate the declared enum values rather than the full SlotCount --
            // callers care about the statuses we actually emit, not the sparse
            // empty slots between Failure_Timeout (5) and Failure_Other (99).
            var result = new Dictionary<AckStatus, long>();
            foreach (AckStatus status in Enum.GetValues(typeof(AckStatus)))
            {
                result[status] = Get(status);
            }
            return result;
        }

        /// <summary>Total increments across every status. Useful for "how many
        /// acks has the tracker observed this session" diagnostics.</summary>
        public long Total()
        {
            long sum = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                sum += Interlocked.Read(ref _counters[i]);
            }
            return sum;
        }

        /// <summary>Zero every counter. Used by tests; should not be called in
        /// production code -- there is no reset semantic the BLL needs.</summary>
        public void Reset()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                Interlocked.Exchange(ref _counters[i], 0);
            }
        }
    }
}
