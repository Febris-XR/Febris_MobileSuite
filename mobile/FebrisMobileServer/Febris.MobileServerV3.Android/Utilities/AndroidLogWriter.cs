// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Text;
using System.IO;

namespace Febris.MobileServerV3.Droid.Utilities
{
    /// <summary>
    /// Routes <see cref="Console"/> output into logcat.
    ///
    /// WHY THIS IS NEEDED. This head emits NO application logging on device: a full dump of its
    /// process on 2026-07-27 produced three runtime lines and nothing else, while the Companion
    /// head logs freely from identical Release settings. Neither manifest sets
    /// android:debuggable and their DebugSymbols/DebugType/AndroidManagedSymbols all match, so
    /// the cause of the difference was never found.
    ///
    /// Rather than keep hunting it, this makes the question irrelevant: Android.Util.Log always
    /// reaches logcat regardless of build configuration or stdout plumbing. One SetOut call
    /// revives every existing Console.WriteLine in the tier at once instead of editing hundreds
    /// of call sites.
    ///
    /// This is not cosmetic. Every wrong diagnosis during the pairing bring-up traced back to
    /// this head being unable to say what it did, including a send-gate deadlock whose own log
    /// line named the problem exactly and was never visible.
    /// </summary>
    public sealed class AndroidLogWriter : TextWriter
    {
        private const string Tag = "Febris";

        /// <summary>logcat truncates around 4k per entry; flushing well short of that keeps
        /// long frames readable rather than silently clipped mid-line.</summary>
        private const int MaxLine = 900;

        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly object _gate = new object();

        public override Encoding Encoding { get { return Encoding.UTF8; } }

        public override void Write(char value)
        {
            lock (_gate)
            {
                if (value == '\n') { FlushLine(); return; }
                if (value == '\r') return;
                _buffer.Append(value);
                if (_buffer.Length >= MaxLine) FlushLine();
            }
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            foreach (char c in value) Write(c);
        }

        public override void WriteLine(string value)
        {
            Write(value);
            lock (_gate) { FlushLine(); }
        }

        public override void Flush()
        {
            lock (_gate) { FlushLine(); }
        }

        /// <summary>Caller holds the lock.</summary>
        private void FlushLine()
        {
            if (_buffer.Length == 0) return;
            string line = _buffer.ToString();
            _buffer.Clear();
            try { Android.Util.Log.Info(Tag, line); }
            catch (Exception) { /* logging must never be able to break the caller */ }
        }
    }
}
