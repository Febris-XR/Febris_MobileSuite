// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// What happened to an attempted socket write.
    ///
    /// <para><b>Why this is not a bool.</b> "The frame did not go out" covers two conditions whose
    /// correct responses are opposite, and collapsing them wedged the Companion completely once
    /// pairing shipped.</para>
    ///
    /// <para>A send GATED by an unresolved handshake is a healthy connection mid-negotiation, and
    /// the only correct response is to leave it alone and try again shortly. A send that FAILED is
    /// a connection that may be dead, and the correct response is to tear it down and redial.
    /// While both were reported as <c>false</c>, the Companion's <c>SocketSender</c> read every
    /// handshake refusal as a dead link and forced a reconnect, which closed the socket that the
    /// in-flight <c>BeginHandshake</c> was writing its client hello to. The handshake then died
    /// with <c>SocketException: Socket closed</c>, the connection was abandoned, and the cycle
    /// repeated forever. A paired device could not talk to its peer at all.</para>
    ///
    /// <para>This is the same defect shape as the ack path, which is why that one now returns an
    /// <c>AckStatus</c> rather than a bool: silence and an explicit failure meant different things
    /// to the caller and one of them destroyed a statement. A caller that must branch on WHY an
    /// operation did not succeed cannot be handed a bool.</para>
    ///
    /// <para><b>Do not add an implicit conversion to bool.</b> The entire value of this type is
    /// that <c>if (!sent)</c> stops compiling and every caller is forced to say which of the two
    /// failure meanings it is handling.</para>
    /// </summary>
    public enum P2pSendOutcome
    {
        /// <summary>The frame reached the socket. It does NOT mean the peer processed it.</summary>
        Sent = 0,

        /// <summary>
        /// Refused by the send gate because the handshake has not resolved yet. The connection is
        /// healthy. Do NOT tear it down, and do NOT report the payload as delivered: the caller
        /// should retry on its next poll, which for a statement is what keeps it out of the
        /// Uploaded set.
        /// </summary>
        RefusedHandshakePending = 1,

        /// <summary>
        /// The write threw. The socket has been closed and disposed by the writer, so the
        /// connection is gone and the caller should redial.
        /// </summary>
        Failed = 2
    }
}
