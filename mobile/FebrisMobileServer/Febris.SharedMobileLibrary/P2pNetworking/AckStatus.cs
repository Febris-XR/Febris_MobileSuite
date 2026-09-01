// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Outcome of processing a received frame, reported back to the sender via
    /// an <c>_acknowledge</c> frame keyed by <c>InResponseTo == originalMessageId</c>.
    /// The sender's <see cref="FebrisP2pAckTracker"/> resolves the pending
    /// <see cref="System.Threading.Tasks.TaskCompletionSource{T}"/> with this
    /// status; the BLL above the transport reads it to decide whether to retry,
    /// surface "delivery uncertain" to the user, or move on.
    ///
    /// <para>
    /// <b>Stable numeric values.</b> These values land on the wire (serialized
    /// inside <c>PacketHeaderModel.AckStatus</c>) and MUST NOT be renumbered.
    /// New statuses get appended; deprecated ones stay reserved.
    /// </para>
    ///
    /// <para>
    /// <b>Failure categories.</b> The split between BadHeader / BodyParse /
    /// HandlerThrew / DiskFull / Timeout / Other lets the sender make smarter
    /// retry decisions:
    /// <list type="bullet">
    ///   <item><see cref="Failure_BadHeader"/> / <see cref="Failure_BodyParse"/>
    ///         -- sender-side bug; retrying won't help.</item>
    ///   <item><see cref="Failure_HandlerThrew"/> -- receiver-side bug; retry
    ///         once after a delay, then surface to the user.</item>
    ///   <item><see cref="Failure_DiskFull"/> -- receiver out of space; user
    ///         needs to free storage. Don't retry until they do.</item>
    ///   <item><see cref="Failure_Timeout"/> -- synthesized by the sender's
    ///         tracker when no ack arrives within the per-body-type timeout.
    ///         Means "delivery uncertain": the receiver may have processed
    ///         the frame and we just didn't hear back. Retry is usually safe
    ///         for idempotent body types (modules) and surfaces "delivery
    ///         uncertain" for non-idempotent ones (statements).</item>
    ///   <item><see cref="Failure_Other"/> -- receiver couldn't categorize.
    ///         Investigate via logs.</item>
    /// </list>
    /// </para>
    /// </summary>
    public enum AckStatus
    {
        /// <summary>Frame received, header valid, body handled cleanly.</summary>
        Success = 0,

        /// <summary>Header passed framing validation but its semantic content
        /// failed downstream (e.g., unknown PacketName, BodyType out of range).</summary>
        Failure_BadHeader = 1,

        /// <summary>Body bytes couldn't be deserialized into the structure the
        /// handler expected (e.g., expected JSON, got binary).</summary>
        Failure_BodyParse = 2,

        /// <summary>Handler threw a non-categorized exception during processing.
        /// Receiver logs the exception; sender knows delivery happened but
        /// processing failed.</summary>
        Failure_HandlerThrew = 3,

        /// <summary>Disk write failed because the receiver is out of space.
        /// Most common during module / video delivery.</summary>
        Failure_DiskFull = 4,

        /// <summary>Synthesized by the sender when the per-body-type timeout
        /// elapses without an ack arriving. Means "we don't know whether the
        /// receiver processed this." NEVER appears on the wire -- only inside
        /// the sender's <see cref="FebrisP2pAckTracker"/>.</summary>
        Failure_Timeout = 5,

        /// <summary>Generic catch-all. Use the more specific statuses where
        /// possible; this exists so receivers can always respond rather than
        /// dropping the ack frame.</summary>
        Failure_Other = 99
    }
}
