// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System.Net;
using System.Net.Sockets;
using Febris.SharedMobileLibrary.Utilites;
using FluentAssertions;
using Xunit;

namespace Febris.MobileP2p.Tests
{
    /// <summary>
    /// The mobile server gated its node work on an ICMP ping to a hardcoded <c>google.com</c>, the
    /// same defect the PC tier carried with <c>8.8.8.8</c>. The work behind that gate talks to the
    /// node, so the node is what has to answer.
    ///
    /// <para>
    /// <b>Why this file exists at all, given the same tests live in Febris.SharedServices.Tests.</b>
    /// <c>NodeReachability</c> is a VENDORED copy. The two tiers cannot share an assembly, because
    /// Febris.SharedServices is net8.0 with an ASP.NET Core framework reference and this library is
    /// netstandard2.1. A copy that nothing exercises is a copy that drifts, and drift is exactly what
    /// produced the original mess of one check with two hosts and two timeouts. These pin THIS copy.
    /// </para>
    /// </summary>
    public class VendoredNodeReachabilityTests
    {
        [Theory]
        [InlineData("https://node.example.org:5102/api/", "node.example.org", 5102)]
        [InlineData("http://node.example.org/api/", "node.example.org", 80)]
        [InlineData("https://node.example.org/api/", "node.example.org", 443)]
        [InlineData("https://10.0.0.7:5102/api/", "10.0.0.7", 5102)]
        public void TryGetEndpoint_AbsoluteHttpUrl_YieldsHostAndPort(string url, string host, int port)
        {
            NodeReachability.TryGetEndpoint(url, out string actualHost, out int actualPort).Should().BeTrue();
            actualHost.Should().Be(host);
            actualPort.Should().Be(port);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Token/Refresh")]
        [InlineData("ftp://node.example.org/")]
        [InlineData("{ApiUrl}")]
        public void TryGetEndpoint_UnusableValue_IsRefused(string url)
        {
            NodeReachability.TryGetEndpoint(url, out _, out _).Should().BeFalse();
        }

        [Fact]
        public void IsNodeReachable_Unconfigured_FailsClosed()
        {
            // A device that has not been told which node it belongs to has nothing to probe and must
            // not assume it is fine.
            NodeReachability.IsNodeReachable(null).Should().BeFalse();
            NodeReachability.IsNodeReachable(string.Empty).Should().BeFalse();
        }

        [Fact]
        public void IsNodeReachable_SomethingListening_IsTrue()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                NodeReachability.IsNodeReachable($"http://127.0.0.1:{port}/api/", 2000).Should().BeTrue();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public void IsNodeReachable_NothingListening_IsFalse()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            NodeReachability.IsNodeReachable($"http://127.0.0.1:{port}/api/", 2000).Should().BeFalse();
        }
    }
}
