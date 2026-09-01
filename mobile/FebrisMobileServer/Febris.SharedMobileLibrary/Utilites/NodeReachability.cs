// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
// VENDORED. This file is maintained in the Febris node platform's shared services library and
// copied here. A change belongs upstream first, then in this copy, so the two do not drift.
// The upstream file does NOT ship in this repository, which is the point of the copy.
//
// It is copied rather than referenced because the two tiers cannot share an assembly. The node's
// shared library targets net8.0, carries an ASP.NET Core framework reference and depends on two
// net8.0-only projects, so this netstandard2.1 library cannot consume it. Extracting the class
// into a small library of its own was considered and rejected on measurement: a ProjectReference
// becomes a nuspec dependency, so the shared library would then publish a dependency on a package
// outside its release set, which its own publish audit refuses. A fourth published package and a
// new release gate cost more than one copied file.
//
// The method bodies here are byte-identical to upstream. Only the namespace and this header
// differ, so a diff between the two files should show nothing else.
//
// Behaviour is pinned by VendoredNodeReachabilityTests in the mobile test suite. A copy that
// nothing exercises is a copy that drifts.

using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Utilites
{
    /// <summary>
    /// Answers "can this client reach the node it belongs to" for the PC background services.
    ///
    /// <para>
    /// <b>What this replaces, and why it was wrong.</b> Both Topshelf services gated their work on
    /// <c>PingRequest.IsConnnectedToInternet()</c>, which ICMP-pinged a hardcoded public address:
    /// <c>8.8.8.8</c> in the PC tier and <c>google.com</c> in the copies it was cloned from. The
    /// question the call sites actually ask is narrower than "is the internet up". The work behind
    /// the gate is downloading modules FROM the node and uploading statements and video TO it, so the
    /// only thing worth knowing is whether the node answers.
    /// </para>
    ///
    /// <para>
    /// Pinging a public resolver got that wrong in three separate ways. An air-gapped deployment,
    /// which <c>pc/tools/fetch-ffmpeg.ps1</c> states is explicitly in scope, has a perfectly
    /// reachable node and no route to Google, so the gate stayed shut forever and the service did
    /// nothing without ever reporting a problem. A network that blocks outbound ICMP failed the same
    /// way while every HTTP call would have succeeded. And when the public address answered but the
    /// node was down, the gate opened on evidence about somebody else's server.
    /// </para>
    ///
    /// <para>
    /// <b>The probe is a TCP connect, not a ping.</b> ICMP is frequently blocked on exactly the
    /// networks this software runs on, and a host answering ICMP says nothing about whether the API
    /// is listening. Opening a socket to the host and port the client is already configured to call
    /// is both cheaper to reason about and closer to the question.
    /// </para>
    ///
    /// <para>
    /// It fails CLOSED, consistently with the node URL resolution on each tier. No configured URL means
    /// not reachable, because a client that has not been told which node it belongs to has nothing to
    /// probe and must not guess one.
    /// </para>
    /// </summary>
    public static class NodeReachability
    {
        /// <summary>
        /// Default probe timeout. The two services it replaces used 1000 ms and 5000 ms for the same
        /// check, which is the kind of drift that duplicated code produces.
        /// </summary>
        public const int DefaultTimeoutMilliseconds = 2000;

        /// <summary>
        /// Pull the host and port out of a configured API URL. Pure and side-effect free, which is
        /// the half worth testing: the socket call cannot be meaningfully asserted, this can.
        ///
        /// <para>
        /// Returns false for anything that is not an absolute http or https URL. A relative value is
        /// the exact shape the unconfigured services used to produce, so it is rejected rather than
        /// coerced.
        /// </para>
        /// </summary>
        public static bool TryGetEndpoint(string apiUrl, out string host, out int port)
        {
            host = null;
            port = 0;

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                return false;
            }

            if (!Uri.TryCreate(apiUrl.Trim(), UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(uri.Host) || uri.Port <= 0 || uri.Port > 65535)
            {
                return false;
            }

            // Uri supplies 80 or 443 when the URL carries no explicit port.
            host = uri.Host;
            port = uri.Port;
            return true;
        }

        /// <summary>
        /// Probe the node named by <paramref name="apiUrl"/>. False when the URL is unusable or the
        /// socket does not open inside the timeout.
        /// </summary>
        public static bool IsNodeReachable(string apiUrl, int timeoutMilliseconds = DefaultTimeoutMilliseconds)
        {
            if (!TryGetEndpoint(apiUrl, out string host, out int port))
            {
                return false;
            }

            if (timeoutMilliseconds <= 0)
            {
                timeoutMilliseconds = DefaultTimeoutMilliseconds;
            }

            try
            {
                using (var client = new TcpClient())
                {
                    // ConnectAsync's CancellationToken overload is .NET 5 and later. This form
                    // compiles unchanged on netstandard2.1, which is what lets the mobile tier
                    // carry a byte-identical copy of this method.
                    Task connect = client.ConnectAsync(host, port);
                    Task finished = Task.WhenAny(connect, Task.Delay(timeoutMilliseconds))
                                        .GetAwaiter().GetResult();

                    if (!ReferenceEquals(finished, connect))
                    {
                        // Timed out. Disposing the client abandons the pending connect, so observe
                        // its eventual fault rather than leaving it unhandled.
                        connect.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                        return false;
                    }

                    connect.GetAwaiter().GetResult();
                    return client.Connected;
                }
            }
            catch (SocketException)
            {
                return false;
            }
            catch (Exception)
            {
                // A probe must never take the service down. Anything unexpected means "do not
                // attempt node work this tick", which is the same conservative answer.
                return false;
            }
        }

    }
}
