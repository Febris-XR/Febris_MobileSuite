// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;

namespace Febris.SharedMobileLibrary.P2pNetworking
{
    /// <summary>
    /// Severity levels for <see cref="IFebrisP2pLogger"/>. Mapped 1:1 to
    /// <c>Android.Util.LogPriority</c> by the Android implementation.
    /// </summary>
    public enum FebrisP2pLogLevel
    {
        /// <summary>Verbose / per-frame trace. Off by default in production.</summary>
        Debug,

        /// <summary>Normal lifecycle events: handshake completed, frame sent,
        /// frame received. Always on.</summary>
        Info,

        /// <summary>Recoverable problem: oversized frame rejected, peer disconnect
        /// mid-stream, retry triggered. Operator should investigate if frequent.</summary>
        Warn,

        /// <summary>Unrecoverable problem: framing exception, handshake failure,
        /// caller-visible exception. Always logged.</summary>
        Error
    }

    /// <summary>
    /// Sink for P2P framing + transport diagnostics. The shared library produces
    /// log events; the platform decides where they land. Android wires this to
    /// <c>Android.Util.Log</c> with tag <c>"FebrisP2p"</c>; the netstandard
    /// fallback (<see cref="ConsoleFebrisP2pLogger"/>) writes to
    /// <see cref="System.Console"/> so the same code path stays testable from
    /// xUnit.
    ///
    /// <para>
    /// <b>Why this interface exists.</b> Before MP2P-7 the framing + dispatch
    /// layer scattered raw <c>Console.WriteLine(ex.StackTrace)</c> everywhere.
    /// That has three problems:
    /// <list type="bullet">
    ///   <item>Console output on Android disappears unless you have <c>adb logcat</c>
    ///         attached at the right moment.</item>
    ///   <item>No severity -- a debug trace and a fatal exception look identical.</item>
    ///   <item>No structured fields -- you can't search for "all frames for
    ///         messageId X" without parsing prose.</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <b>Usage.</b> Consumers do NOT assume DI is wired. The framer + parser
    /// take an <c>IFebrisP2pLogger</c> in the constructor; if null, they fall
    /// back to <see cref="ConsoleFebrisP2pLogger.Instance"/>. Android shells
    /// register a real logger via <c>Xamarin.Forms.DependencyService</c> or
    /// equivalent and pass it in.
    /// </para>
    /// </summary>
    public interface IFebrisP2pLogger
    {
        /// <summary>
        /// Emit a single log event. Implementations MUST be non-throwing -- a
        /// logger crash must not crash the caller. Implementations MAY drop
        /// events below their configured threshold but MUST evaluate the
        /// <paramref name="message"/> argument lazily-safe (i.e., callers can
        /// pass a pre-formatted string without worrying about wasted work
        /// since framing operations are infrequent compared to the cost of
        /// the I/O they wrap).
        /// </summary>
        /// <param name="level">Severity of the event.</param>
        /// <param name="message">Human-readable message. Should describe the
        /// event in active voice ("Parsed frame", "Rejected oversized header").</param>
        /// <param name="exception">Optional exception associated with the event.
        /// Always passed for <see cref="FebrisP2pLogLevel.Error"/>; usually null
        /// for Info/Debug.</param>
        void Log(FebrisP2pLogLevel level, string message, Exception exception = null);
    }

    /// <summary>
    /// Default fallback logger: writes every event to <see cref="System.Console"/>.
    /// Suitable for unit tests, command-line tools, and any platform where no
    /// platform-specific logger is registered. The Android shells register a
    /// dedicated <c>Android.Util.Log</c> forwarder instead.
    ///
    /// <para>
    /// <b>Format:</b> <c>[FebrisP2p][Level] message</c>, with the exception's
    /// type + message + stack trace appended on a new line when present.
    /// </para>
    ///
    /// <para>
    /// Thread-safe in the trivial sense: <see cref="System.Console.WriteLine(string)"/>
    /// is itself thread-safe. Multi-line output from concurrent callers may
    /// interleave, but no individual line will be torn.
    /// </para>
    /// </summary>
    public sealed class ConsoleFebrisP2pLogger : IFebrisP2pLogger
    {
        /// <summary>Shared singleton -- the framer/parser default to this when
        /// no explicit logger is supplied.</summary>
        public static readonly ConsoleFebrisP2pLogger Instance = new ConsoleFebrisP2pLogger();

        /// <inheritdoc/>
        public void Log(FebrisP2pLogLevel level, string message, Exception exception = null)
        {
            // Per the contract: logger crashes must never crash the caller.
            try
            {
                string line = "[FebrisP2p][" + level + "] " + (message ?? string.Empty);
                Console.WriteLine(line);
                if (exception != null)
                {
                    Console.WriteLine("[FebrisP2p][" + level + "] " + exception.GetType().Name + ": " + exception.Message);
                    if (!string.IsNullOrEmpty(exception.StackTrace))
                    {
                        Console.WriteLine(exception.StackTrace);
                    }
                }
            }
            catch
            {
                // Swallow -- we cannot let a logger failure propagate.
            }
        }
    }

    /// <summary>
    /// Test double: captures every log event in an in-memory list. Used by
    /// the framer / parser tests to assert that the right severity + message
    /// fire on the right code paths without coupling to Console output.
    ///
    /// <para>
    /// <b>Not for production use.</b> The capture list grows unbounded.
    /// </para>
    /// </summary>
    public sealed class CapturingFebrisP2pLogger : IFebrisP2pLogger
    {
        private readonly System.Collections.Generic.List<FebrisP2pLogEntry> _entries
            = new System.Collections.Generic.List<FebrisP2pLogEntry>();
        private readonly object _lock = new object();

        /// <summary>Snapshot of every event captured so far. Returns a copy --
        /// safe to enumerate while new events are being captured.</summary>
        public System.Collections.Generic.IReadOnlyList<FebrisP2pLogEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries.ToArray();
                }
            }
        }

        /// <inheritdoc/>
        public void Log(FebrisP2pLogLevel level, string message, Exception exception = null)
        {
            lock (_lock)
            {
                _entries.Add(new FebrisP2pLogEntry(level, message ?? string.Empty, exception));
            }
        }
    }

    /// <summary>A single captured log event. Immutable; safe to share across
    /// threads.</summary>
    public sealed class FebrisP2pLogEntry
    {
        public FebrisP2pLogLevel Level { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public FebrisP2pLogEntry(FebrisP2pLogLevel level, string message, Exception exception)
        {
            Level = level;
            Message = message;
            Exception = exception;
        }
    }
}
