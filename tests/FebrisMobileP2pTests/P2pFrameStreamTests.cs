// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.P2pNetworking;
using Xunit;

namespace Febris.SimulationLibrary.Tests
{
    /// <summary>
    /// Pins <see cref="FebrisP2pFrameParser.ReadFrameBytesAsync"/>, the wire-side read
    /// introduced to resolve MDM-B2.
    ///
    /// Before that change the socket layer decided frame boundaries by reading until
    /// <c>IsDataAvailable()</c> returned false and dispatching whatever had accumulated.
    /// Two consequences, both reproduced here as regression tests: a COALESCED read (two
    /// frames arriving together) was parsed for the first frame only and the rest were
    /// silently discarded, and a SPLIT read (one frame arriving in pieces) truncated.
    /// At the old 10fps screenshot cadence that was mostly masked. At video frame rates
    /// it is the dominant failure mode, so it had to be fixed before any encoder work.
    ///
    /// These live in the simulation-library test project because it already references
    /// Febris.SharedMobileLibrary (netstandard2.0) and runs on Linux CI, so the frame
    /// contract is covered without needing an Android toolchain.
    /// </summary>
    public class P2pFrameStreamTests
    {
        private static PacketHeaderModel Header(string name)
        {
            return new PacketHeaderModel
            {
                PacketName = name,
                DeviceUniqueIdentifier = "0123456789abcdef"
            };
        }

        private static byte[] BuildFrame(string name, byte[] body)
        {
            return new FebrisP2pFrameBuilder().Build(Header(name), body);
        }

        /// <summary>A stream that hands back at most <c>_chunk</c> bytes per read, so the
        /// parser's read-exactly loops are actually exercised rather than being handed a
        /// whole frame in one call the way a MemoryStream would.</summary>
        private sealed class DripStream : Stream
        {
            private readonly byte[] _data;
            private readonly int _chunk;
            private int _position;

            public DripStream(byte[] data, int chunk)
            {
                _data = data;
                _chunk = chunk;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int remaining = _data.Length - _position;
                if (remaining <= 0) return 0;
                int n = Math.Min(Math.Min(count, _chunk), remaining);
                Buffer.BlockCopy(_data, _position, buffer, offset, n);
                _position += n;
                return n;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _data.Length;
            public override long Position { get => _position; set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        [Fact]
        public async Task ReadFrameBytesAsync_SingleFrame_ReturnsFrameVerbatim()
        {
            byte[] body = Encoding.UTF8.GetBytes("payload one");
            byte[] frame = BuildFrame("Test", body);

            using (MemoryStream stream = new MemoryStream(frame))
            {
                byte[] read = await new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None);

                Assert.NotNull(read);
                // Verbatim matters: the receivers re-parse these bytes with ParseBytes,
                // so the read must preserve the framing, not strip it.
                Assert.Equal(frame, read);

                var (header, parsedBody) = new FebrisP2pFrameParser().ParseBytes(read);
                Assert.Equal("Test", header.PacketName);
                Assert.Equal(body, parsedBody);
            }
        }

        [Fact]
        public async Task ReadFrameBytesAsync_TwoCoalescedFrames_ReadsBothInOrder()
        {
            // THE regression. Both frames arrive in a single TCP read. The old occupancy
            // heuristic dispatched the blob, ParseBytes decoded frame one, and frame two
            // was dropped without a trace.
            byte[] first = BuildFrame("First", Encoding.UTF8.GetBytes("aaa"));
            byte[] second = BuildFrame("Second", Encoding.UTF8.GetBytes("bbbbbb"));

            byte[] both = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, both, 0, first.Length);
            Buffer.BlockCopy(second, 0, both, first.Length, second.Length);

            IFebrisP2pFrameParser parser = new FebrisP2pFrameParser();
            using (MemoryStream stream = new MemoryStream(both))
            {
                byte[] a = await parser.ReadFrameBytesAsync(stream, CancellationToken.None);
                byte[] b = await parser.ReadFrameBytesAsync(stream, CancellationToken.None);
                byte[] c = await parser.ReadFrameBytesAsync(stream, CancellationToken.None);

                Assert.Equal(first, a);
                Assert.Equal(second, b);
                Assert.Null(c); // clean EOF once both are consumed

                Assert.Equal("First", parser.ParseBytes(a).Header.PacketName);
                Assert.Equal("Second", parser.ParseBytes(b).Header.PacketName);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(17)]
        [InlineData(64)]
        public async Task ReadFrameBytesAsync_FrameSplitAcrossReads_Reassembles(int chunkSize)
        {
            // The other half of the old defect: a frame split across reads used to truncate.
            // chunk=1 also proves the preamble itself survives being delivered one byte at a
            // time, and chunk=17 lands a read boundary exactly on the preamble edge.
            byte[] body = Encoding.UTF8.GetBytes(new string('x', 500));
            byte[] frame = BuildFrame("Split", body);

            byte[] read = await new FebrisP2pFrameParser()
                .ReadFrameBytesAsync(new DripStream(frame, chunkSize), CancellationToken.None);

            Assert.Equal(frame, read);
            Assert.Equal(body, new FebrisP2pFrameParser().ParseBytes(read).Body);
        }

        [Fact]
        public async Task ReadFrameBytesAsync_CleanEofAtFrameBoundary_ReturnsNull()
        {
            // A peer closing between frames is a normal disconnect. It must not surface as
            // a truncation error, otherwise every ordinary teardown logs a false failure.
            using (MemoryStream empty = new MemoryStream(new byte[0]))
            {
                Assert.Null(await new FebrisP2pFrameParser().ReadFrameBytesAsync(empty, CancellationToken.None));
            }
        }

        [Fact]
        public async Task ReadFrameBytesAsync_EofPartWayThroughPreamble_Throws()
        {
            // Closing mid-frame is NOT normal and must still be reported.
            byte[] frame = BuildFrame("Partial", Encoding.UTF8.GetBytes("zz"));
            byte[] halfPreamble = new byte[8];
            Buffer.BlockCopy(frame, 0, halfPreamble, 0, halfPreamble.Length);

            using (MemoryStream stream = new MemoryStream(halfPreamble))
            {
                await Assert.ThrowsAsync<TruncatedFrameException>(
                    () => new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None));
            }
        }

        [Fact]
        public async Task ReadFrameBytesAsync_EofPartWayThroughBody_Throws()
        {
            byte[] frame = BuildFrame("Partial", Encoding.UTF8.GetBytes("a long-ish body here"));
            byte[] missingTail = new byte[frame.Length - 4];
            Buffer.BlockCopy(frame, 0, missingTail, 0, missingTail.Length);

            using (MemoryStream stream = new MemoryStream(missingTail))
            {
                await Assert.ThrowsAsync<TruncatedFrameException>(
                    () => new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None));
            }
        }

        [Fact]
        public async Task ReadFrameBytesAsync_WrongMagic_Throws()
        {
            byte[] frame = BuildFrame("Bad", Encoding.UTF8.GetBytes("q"));
            frame[0] = 0x00; // corrupt the FBP2 magic

            using (MemoryStream stream = new MemoryStream(frame))
            {
                await Assert.ThrowsAsync<InvalidFrameMagicException>(
                    () => new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None));
            }
        }

        [Fact]
        public async Task ReadFrameBytesAsync_UnsupportedVersion_Throws()
        {
            byte[] frame = BuildFrame("Bad", Encoding.UTF8.GetBytes("q"));
            frame[4] = 0x7F; // version byte sits immediately after the 4 magic bytes

            using (MemoryStream stream = new MemoryStream(frame))
            {
                await Assert.ThrowsAsync<UnsupportedFrameVersionException>(
                    () => new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None));
            }
        }

        [Fact]
        public void VideoBodyTypes_HaveDistinctValues_AndDoNotRedefine667()
        {
            // 667 must keep its legacy value. There is no version negotiation in
            // PacketHeaderModel, so redefining it would make mixed-version pairs
            // silently misinterpret each other instead of failing loudly.
            Assert.Equal(667, (int)BodyType._videoStream);
            Assert.Equal(734, (int)BodyType._endVideoStream);

            int[] added = { (int)BodyType._videoStreamStart, (int)BodyType._videoCodecConfig, (int)BodyType._videoFrame };
            Assert.Equal(added.Length, added.Distinct().Count());

            // No collision with anything already defined.
            int[] existing = Enum.GetValues(typeof(BodyType))
                .Cast<BodyType>()
                .Where(b => b != BodyType._videoStreamStart && b != BodyType._videoCodecConfig && b != BodyType._videoFrame)
                .Select(b => (int)b)
                .ToArray();
            foreach (int value in added)
            {
                Assert.DoesNotContain(value, existing);
            }
        }

        [Fact]
        public void VideoHeaderFields_AreOmittedEntirely_WhenNotVideo()
        {
            // The JSON header is paid per frame, so the video fields must cost nothing
            // on the frames that do not carry video.
            byte[] frame = BuildFrame("Statement", Encoding.UTF8.GetBytes("{}"));
            string wire = Encoding.UTF8.GetString(frame);

            foreach (string field in new[] { "SequenceNumber", "PtsMicros", "IsKeyFrame", "Rotation", "Width", "Height" })
            {
                Assert.DoesNotContain(field, wire);
            }
        }

        [Fact]
        public async Task VideoHeaderFields_RoundTripThroughTheWire()
        {
            PacketHeaderModel header = new PacketHeaderModel
            {
                BodyType = BodyType._videoFrame,
                PacketName = "Video Frame",
                DeviceUniqueIdentifier = "0123456789abcdef",
                SequenceNumber = 42u,
                PtsMicros = 1234567L,
                IsKeyFrame = true,
                Rotation = 90,
                Width = 1280,
                Height = 720
            };
            byte[] body = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x65 }; // Annex B start code + IDR NAL
            byte[] frame = new FebrisP2pFrameBuilder().Build(header, body);

            using (MemoryStream stream = new MemoryStream(frame))
            {
                byte[] read = await new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None);
                var (parsed, parsedBody) = new FebrisP2pFrameParser().ParseBytes(read);

                Assert.Equal(BodyType._videoFrame, parsed.BodyType);
                Assert.Equal(42u, parsed.SequenceNumber);
                Assert.Equal(1234567L, parsed.PtsMicros);
                Assert.True(parsed.IsKeyFrame);
                Assert.Equal(90, parsed.Rotation);
                Assert.Equal(1280, parsed.Width);
                Assert.Equal(720, parsed.Height);
                Assert.Equal(body, parsedBody);
            }
        }

        [Theory]
        [InlineData(BodyType._videoFrame)]
        [InlineData(BodyType._videoCodecConfig)]
        public async Task VideoBodyOverTheVideoCap_IsRejectedBeforeTheGlobalCap(BodyType bodyType)
        {
            // A corrupted length prefix on a video frame must not make a headset
            // allocate up to the 256 MB global ceiling. The rejection has to happen
            // after the header is decoded but before the body is read, which is why
            // ReadFrameBytesAsync reads the header separately.
            long oversize = FebrisP2pFrame.MaxVideoBodyBytes + 1;
            Assert.True(oversize < FebrisP2pFrame.MaxBodyBytes, "must still be under the global cap to prove the per-type cap fired");

            byte[] frame = ForgeFrameWithDeclaredBodyLength(bodyType, oversize);

            using (MemoryStream stream = new MemoryStream(frame))
            {
                await Assert.ThrowsAsync<OversizedFrameException>(
                    () => new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None));
            }
        }

        [Fact]
        public async Task NonVideoBodyOverTheVideoCap_IsStillAccepted()
        {
            // The tighter cap must apply ONLY to video. A module ZIP well over 512 KB
            // is legitimate, so this proves the per-type cap did not become global.
            int size = (int)FebrisP2pFrame.MaxVideoBodyBytes + 4096;
            byte[] frame = new FebrisP2pFrameBuilder().Build(
                new PacketHeaderModel { BodyType = BodyType._module, PacketName = "Module", DeviceUniqueIdentifier = "0123456789abcdef" },
                new byte[size]);

            using (MemoryStream stream = new MemoryStream(frame))
            {
                byte[] read = await new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None);
                Assert.Equal(size, new FebrisP2pFrameParser().ParseBytes(read).Body.Length);
            }
        }

        /// <summary>Builds a frame whose preamble DECLARES a body length it does not carry,
        /// which is what a corrupted length prefix looks like on the wire.</summary>
        private static byte[] ForgeFrameWithDeclaredBodyLength(BodyType bodyType, long declaredBodyLength)
        {
            byte[] real = new FebrisP2pFrameBuilder().Build(
                new PacketHeaderModel { BodyType = bodyType, PacketName = "Forged", DeviceUniqueIdentifier = "0123456789abcdef" },
                new byte[0]);

            // Body length is the big-endian UInt64 at offset 9 of the preamble.
            byte[] forged = (byte[])real.Clone();
            ulong v = (ulong)declaredBodyLength;
            for (int i = 0; i < 8; i++)
            {
                forged[9 + i] = (byte)(v >> (8 * (7 - i)));
            }
            return forged;
        }

        [Theory]
        [InlineData(BodyType._videoStream)]
        [InlineData(BodyType._videoStreamStart)]
        [InlineData(BodyType._installModule)]
        [InlineData(BodyType._uninstallModule)]
        [InlineData(BodyType._statement)]
        public void Authorize_UnknownPeer_IsDenied_ForPrivilegedTypes(BodyType bodyType)
        {
            // The whole point: a device that was never paired must not be able to start a
            // screen capture, push or remove a module, or inject a statement.
            var result = P2pPeerAuthorization.Authorize(
                new PacketHeaderModel { BodyType = bodyType, DeviceUniqueIdentifier = "attacker-device" },
                peerIsKnown: false);

            Assert.False(result.IsAuthorized);
            Assert.NotNull(result.DenyReason);
        }

        [Fact]
        public void Authorize_Initialize_IsAllowedFromUnknownPeer()
        {
            // _initalize must stay open or onboarding breaks. Its handler requires the
            // device to already exist in the paired list, matched on Bluetooth name, and
            // the stored WiFi identifier is still empty at that point.
            var result = P2pPeerAuthorization.Authorize(
                new PacketHeaderModel { BodyType = BodyType._initalize, DeviceUniqueIdentifier = "" },
                peerIsKnown: false);

            Assert.True(result.IsAuthorized);
            Assert.True(P2pPeerAuthorization.IsPreAuthenticationBodyType(BodyType._initalize));
        }

        [Fact]
        public void Authorize_KnownPeer_IsAllowed()
        {
            var result = P2pPeerAuthorization.Authorize(
                new PacketHeaderModel { BodyType = BodyType._videoFrame, DeviceUniqueIdentifier = "paired-device" },
                peerIsKnown: true);

            Assert.True(result.IsAuthorized);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Authorize_BlankIdentifier_IsDenied_EvenIfCallerSaysKnown(string identifier)
        {
            // A blank identifier must never satisfy the gate. If it did, the cheapest
            // possible forged frame would be the one that passes.
            var result = P2pPeerAuthorization.Authorize(
                new PacketHeaderModel { BodyType = BodyType._videoFrame, DeviceUniqueIdentifier = identifier },
                peerIsKnown: true);

            Assert.False(result.IsAuthorized);
        }

        [Fact]
        public void Authorize_NullHeader_IsDenied()
        {
            Assert.False(P2pPeerAuthorization.Authorize(null, peerIsKnown: true).IsAuthorized);
        }

        [Fact]
        public void OnlyOnboardingHandshakeAndPairing_ArePreAuthenticated()
        {
            // Guards the blast radius of the pre-auth exemption. If someone later adds a
            // body type to that set, this test makes them justify it. It has already done
            // that job once: adding the two pairing types below turned this red.
            //
            // The three handshake messages are exempt because they ARE the
            // authentication, so requiring it would deadlock the connection. They are
            // safe to exempt because they carry no instruction and no payload: a peer
            // without the PSK simply fails the HMAC check and is dropped.
            //
            // The two PAIRING messages are exempt for a stronger reason: pairing CREATES
            // the credential, so there is nothing to require yet. They are also the only
            // members of this set whose safety does not depend on the channel at all. A
            // man in the middle must run two ECDH exchanges, so the two devices display
            // different six-digit codes and a human refuses. They carry an ephemeral
            // public key and nothing else, so an attacker who sends one learns nothing and
            // gains nothing beyond a code that will not match.
            var expectedExempt = new[]
            {
                BodyType._initalize,
                BodyType._handshakeClientHello,
                BodyType._handshakeServerHello,
                BodyType._handshakeClientFinal,
                BodyType._pairingRequest,
                BodyType._pairingResponse
            };

            foreach (BodyType bodyType in Enum.GetValues(typeof(BodyType)).Cast<BodyType>())
            {
                Assert.Equal(
                    expectedExempt.Contains(bodyType),
                    P2pPeerAuthorization.IsPreAuthenticationBodyType(bodyType));
            }
        }

        [Fact]
        public void HandshakeBodyTypes_AreDistinctAndRoutable()
        {
            // IsHandshakeBodyType is the narrower predicate the socket pumps use to route
            // a frame into the handshake state machine instead of the dispatcher, so
            // _initalize must NOT match it even though both are pre-authentication.
            Assert.True(P2pPeerAuthorization.IsHandshakeBodyType(BodyType._handshakeClientHello));
            Assert.True(P2pPeerAuthorization.IsHandshakeBodyType(BodyType._handshakeServerHello));
            Assert.True(P2pPeerAuthorization.IsHandshakeBodyType(BodyType._handshakeClientFinal));
            Assert.False(P2pPeerAuthorization.IsHandshakeBodyType(BodyType._initalize));
            Assert.False(P2pPeerAuthorization.IsHandshakeBodyType(BodyType._videoFrame));

            int[] added =
            {
                (int)BodyType._handshakeClientHello,
                (int)BodyType._handshakeServerHello,
                (int)BodyType._handshakeClientFinal
            };
            Assert.Equal(added.Length, added.Distinct().Count());

            int[] existing = Enum.GetValues(typeof(BodyType))
                .Cast<BodyType>()
                .Where(b => !P2pPeerAuthorization.IsHandshakeBodyType(b))
                .Select(b => (int)b)
                .ToArray();
            foreach (int value in added)
            {
                Assert.DoesNotContain(value, existing);
            }
        }

        [Fact]
        public async Task ReadFrameBytesAsync_EmptyBody_RoundTrips()
        {
            // Control frames carry no body, so bodyLength 0 must not be mistaken for EOF.
            byte[] frame = new FebrisP2pFrameBuilder().Build(Header("NoBody"));

            using (MemoryStream stream = new MemoryStream(frame))
            {
                byte[] read = await new FebrisP2pFrameParser().ReadFrameBytesAsync(stream, CancellationToken.None);

                Assert.Equal(frame, read);
                Assert.Empty(new FebrisP2pFrameParser().ParseBytes(read).Body);
            }
        }
    }
}
