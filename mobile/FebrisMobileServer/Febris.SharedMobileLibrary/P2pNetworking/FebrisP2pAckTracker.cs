// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Tracks outstanding sends awaiting acknowledgement. Sender calls
    /// <see cref="RegisterPending(Guid, TimeSpan)"/> immediately before writing
    /// a frame; the returned <see cref="Task{TResult}"/> resolves when:
    /// <list type="bullet">
    ///   <item>the matching ack arrives -- receiver calls
    ///         <see cref="TryResolve(Guid, AckStatus)"/> with the original
    ///         <c>MessageId</c> + the resolved <see cref="AckStatus"/>;</item>
    ///   <item>the per-body-type timeout elapses without an ack --
    ///         resolved as <see cref="AckStatus.Failure_Timeout"/>;</item>
    ///   <item>the socket disconnects and the caller invokes
    ///         <see cref="FailAll(AckStatus)"/> to drain pending sends.</item>
    /// </list>
    ///
    /// <para>
    /// <b>Concurrency.</b> Every operation is safe under contention. The
    /// underlying <see cref="ConcurrentDictionary{TKey, TValue}"/> serializes
    /// dictionary mutations; <see cref="TaskCompletionSource{T}"/> is itself
    /// thread-safe.
    /// </para>
    ///
    /// <para>
    /// <b>Why not just await a Task with a CancellationTokenSource per send?</b>
    /// The ack arrives on a different code path (the dispatcher's
    /// <c>ProcessAcknowledge</c> case) -- there's no natural callback site for
    /// a CTS. The dictionary indirection is the bridge: the dispatcher looks
    /// up the MessageId, finds the TCS, calls TrySetResult. That's the
    /// canonical pattern for request/response over a stream-based transport.
    /// </para>
    ///
    /// <para>
    /// <b>Backward compat.</b> Pre-MP2P-8 peers don't populate MessageId, so
    /// they send <c>Guid.Empty</c>. Callers MUST NOT register Guid.Empty --
    /// the constructor of every <see cref="RegisterPending(Guid, TimeSpan)"/>
    /// call asserts a non-empty Guid. The dispatcher's ack-emission logic
    /// skips emission when the incoming MessageId is Guid.Empty (no
    /// correlation possible). Old peers keep working without ack semantics.
    /// </para>
    /// </summary>
    public class FebrisP2pAckTracker
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<AckStatus>> _pending
            = new ConcurrentDictionary<Guid, TaskCompletionSource<AckStatus>>();

        private readonly IFebrisP2pLogger _logger;
        private readonly P2pTelemetry _telemetry;

        /// <summary>
        /// Process-wide shared tracker (MOB-B1 blocker 3 / MP2P-8). The send path
        /// (e.g. the Companion's <c>LoopLogic</c> statement loop calling
        /// <see cref="RegisterPending(Guid, TimeSpan)"/>) and the ack-receipt path
        /// (the dispatcher's <c>_acknowledge</c> case calling
        /// <see cref="TryResolve(Guid, AckStatus)"/>) run on different call stacks
        /// but MUST share ONE tracker instance, otherwise the pending TCS created on
        /// send is invisible to the resolver. Each app process (Companion, Server)
        /// gets its own <see cref="Instance"/>; the two never coexist in one process,
        /// so a single static singleton is correct for both sides. Mirrors the
        /// existing <see cref="P2pTelemetry.Instance"/> /
        /// <see cref="ConsoleFebrisP2pLogger.Instance"/> singleton pattern in this
        /// library. Tests that need isolation construct their own tracker instead.
        /// </summary>
        public static FebrisP2pAckTracker Instance { get; } = new FebrisP2pAckTracker();

        /// <summary>Number of currently-pending acks. Diagnostic; do not gate
        /// behavior on this value (race window between read and act).</summary>
        public int PendingCount => _pending.Count;

        public FebrisP2pAckTracker(IFebrisP2pLogger logger = null, P2pTelemetry telemetry = null)
        {
            _logger = logger ?? ConsoleFebrisP2pLogger.Instance;
            // MP2P-10: every resolved / timed-out / drained ack bumps the matching
            // counter on the telemetry sink. Tests inject a fresh instance for
            // isolation; production code defaults to the shared singleton so the
            // debug UI can read aggregated counts across the process.
            _telemetry = telemetry ?? P2pTelemetry.Instance;
        }

        /// <summary>
        /// Register an outgoing send awaiting ack. The caller MUST then write
        /// the frame; the returned task will resolve when the matching ack
        /// arrives, the <paramref name="timeout"/> elapses, or the tracker is
        /// drained.
        ///
        /// <para>
        /// <b>Lifetime:</b> the tracker holds a strong reference to the TCS
        /// until one of the three resolution paths fires. Callers must NOT
        /// abandon the returned task without awaiting it (otherwise the entry
        /// stays in the dictionary until the timeout, then resolves and the
        /// result is silently dropped).
        /// </para>
        /// </summary>
        /// <param name="messageId">UUID stamped onto the outgoing frame's
        /// <c>PacketHeaderModel.MessageId</c>. Must not be <see cref="Guid.Empty"/>
        /// -- that value is reserved for "no tracking requested" on the wire.</param>
        /// <param name="timeout">How long to wait for an ack before resolving
        /// with <see cref="AckStatus.Failure_Timeout"/>. Use
        /// <see cref="DefaultTimeoutFor(BodyType)"/> for the canonical
        /// per-body-type values.</param>
        /// <exception cref="ArgumentException"><paramref name="messageId"/> is
        /// <see cref="Guid.Empty"/>, or the timeout is non-positive.</exception>
        /// <exception cref="InvalidOperationException">A pending registration
        /// already exists for this <paramref name="messageId"/>. Callers must
        /// use fresh UUIDs per send; reuse is a sender bug.</exception>
        public Task<AckStatus> RegisterPending(Guid messageId, TimeSpan timeout)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException(
                    "MessageId must be non-empty. Guid.Empty is reserved for legacy peers " +
                    "that don't speak MP2P-8 ack semantics.", nameof(messageId));
            }
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    "Timeout must be positive; got " + timeout + ".", nameof(timeout));
            }

            var tcs = new TaskCompletionSource<AckStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pending.TryAdd(messageId, tcs))
            {
                throw new InvalidOperationException(
                    "A pending ack is already registered for MessageId " + messageId +
                    ". Callers must use fresh Guids per send.");
            }

            // Arm the timeout. We deliberately don't track a CancellationTokenSource
            // per entry -- the Task.Delay continues running until it elapses even when
            // the ack arrives early; the continuation then finds the dictionary entry
            // already gone (TryRemove returns false) and is a no-op. The cost is one
            // background Task per outstanding send awaiting its timeout, which is
            // bounded by frame send rate (peers don't fire thousands of frames per
            // second).
            _ = Task.Delay(timeout).ContinueWith(_ =>
            {
                if (_pending.TryRemove(messageId, out var pendingTcs))
                {
                    pendingTcs.TrySetResult(AckStatus.Failure_Timeout);
                    _telemetry.Increment(AckStatus.Failure_Timeout);
                    _logger.Log(FebrisP2pLogLevel.Warn,
                        "Ack timed out for MessageId " + messageId + " after " +
                        timeout.TotalSeconds + "s; treating as delivery uncertain.");
                }
            }, TaskScheduler.Default);

            return tcs.Task;
        }

        /// <summary>
        /// Called by the dispatcher when an <c>_acknowledge</c> frame arrives.
        /// Looks up the original send by <paramref name="messageId"/>
        /// (== ack frame's <c>InResponseTo</c>) and resolves the pending
        /// task with <paramref name="status"/>.
        /// </summary>
        /// <returns><c>true</c> if a pending registration was found and
        /// resolved; <c>false</c> if the MessageId was unknown (e.g., the ack
        /// arrived after timeout already fired, or for a never-registered id).</returns>
        public bool TryResolve(Guid messageId, AckStatus status)
        {
            if (messageId == Guid.Empty) return false; // Legacy / unsolicited ack -- no tracking entry.

            if (_pending.TryRemove(messageId, out var tcs))
            {
                bool set = tcs.TrySetResult(status);
                if (set)
                {
                    _telemetry.Increment(status);
                    _logger.Log(FebrisP2pLogLevel.Debug,
                        "Resolved ack for MessageId " + messageId + " with status " + status + ".");
                }
                return set;
            }
            return false;
        }

        /// <summary>
        /// Drain every pending registration, resolving each with
        /// <paramref name="reason"/>. Use this when the underlying socket
        /// drops -- every outstanding send is now "delivery uncertain" and
        /// the BLL above the transport needs to learn that immediately
        /// rather than waiting for each per-frame timeout.
        /// </summary>
        public void FailAll(AckStatus reason)
        {
            // Snapshot the keys first -- iterating directly over the dictionary
            // while mutating is fine for ConcurrentDictionary, but a snapshot
            // gives us a stable count for the log line.
            var keys = _pending.Keys.ToArray();
            int drained = 0;
            foreach (var key in keys)
            {
                if (_pending.TryRemove(key, out var tcs))
                {
                    if (tcs.TrySetResult(reason))
                    {
                        _telemetry.Increment(reason);
                        drained++;
                    }
                }
            }
            if (drained > 0)
            {
                _logger.Log(FebrisP2pLogLevel.Warn,
                    "Drained " + drained + " pending ack(s) with status " + reason +
                    " (typically socket disconnect).");
            }
        }

        /// <summary>
        /// Returns <c>true</c> if a registration exists for
        /// <paramref name="messageId"/>. Diagnostic only -- do not gate
        /// behavior on this (race window between check and act).
        /// </summary>
        public bool IsPending(Guid messageId) => _pending.ContainsKey(messageId);

        /// <summary>
        /// Canonical per-body-type timeout. Module / install operations are
        /// slower (unzip + PackageInstaller dialog), so they get longer
        /// timeouts. Statements / status updates are fast.
        ///
        /// <para>
        /// Values are conservative defaults; callers are free to override per
        /// send via the explicit <see cref="RegisterPending(Guid, TimeSpan)"/>
        /// argument.
        /// </para>
        /// </summary>
        public static TimeSpan DefaultTimeoutFor(BodyType bodyType)
        {
            switch (bodyType)
            {
                // Module install touches the Android PackageInstaller intent +
                // a user-visible install dialog. Real-world install takes 10-60s.
                case BodyType._module:
                case BodyType._installModule:
                case BodyType._reinstallModule:
                case BodyType._uninstallModule:
                case BodyType._removeModule:
                case BodyType._removeZippedModule:
                    return TimeSpan.FromMinutes(5);

                // Video file write to disk; sized in tens of MB.
                case BodyType._video:
                case BodyType._oldVideos:
                    return TimeSpan.FromMinutes(2);

                // Statement / status / pairing: small payloads, near-instant.
                default:
                    return TimeSpan.FromSeconds(30);
            }
        }
    }
}
