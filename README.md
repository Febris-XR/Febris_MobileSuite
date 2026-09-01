This is the first part of the Febris OSS release. It is safe to call this version 4 of the Febris platform. Many aspects of Version 3 had to be stripped out (The central hub, marketplace, developer system, accreditation system, micro-credentialing, CRM, LMS components that added centralized truth, and there may be a few parts that are now gone that previously existed that I cannot recall right this second) and I used Claude to create and cut that seam. If there are lingering parts, I apologize and I will fix it as soon as I can. I feel like I stretched Claude's capabilities while working on this project. AI was not used on any of the other version of Febris so some of these cuts may seem a little ragged but the entire system was built by one person so, please cut me a little slack.

Claude is far better at documenting code than I have ever been and I suspect between my naming conventions and Claude's documentation, this release will be easy to follow.

# febris-mobile

**The Febris mobile device-management suite for Android. Two Xamarin.Forms apps that pair over
Wi-Fi Direct, authenticate each other with a real handshake, and move training modules and a live
screen stream between them without any network infrastructure.**

The pair is designed for rooms that have no usable Wi-Fi and no internet: a training floor, a
vehicle, a field exercise. The Server app runs on a host device and the Companion runs on the
learner devices. They find each other over Wi-Fi Direct, pair once through a code the operator
compares on both screens, and from then on hold a peer-to-peer link that carries module pushes,
installs, xAPI statements and video.

---

## Read this before you clone

This repository is **source, not a product you can install today.** Three things are true and
stated up front rather than discovered:

- **There is no signing keystore, so no publishable APK exists.** You can build and side-load
  debug builds. A Play-ready artifact needs a keystore that is not in this repository and will
  never be.
- **A Release-configuration APK has never been produced for either head.** Debug builds run and
  the P2P stack has been exercised on real hardware, but the Release path is unproven.
- **Xamarin.Forms reached end of support in May 2024.** Everything here builds against it anyway,
  which is why the toolchain notes in [CONTRIBUTING.md](CONTRIBUTING.md) are specific about
  versions. Migration to .NET MAUI is the intended path and has not started.

You need **Windows with Visual Studio 2022 and the Xamarin workload**. The Android heads are
legacy non-SDK-style projects and the `dotnet` CLI cannot build them.

---

## What is in here

| Project | Target | What it is |
|---|---|---|
| `Febris.MobileServerV3` | `netstandard2.1` | Server app, shared code |
| `Febris.MobileServerV3.Android` | Xamarin Android | Server app head |
| `Febris.SharedMobileLibrary` | `netstandard2.1` | the P2P stack, shared by both apps |
| `Febris.MobileCompanionV3` | `netstandard2.1` | Companion app, shared code |
| `Febris.MobileCompanionV3.Android` | Xamarin Android | Companion app head |
| `Febris.AdbLibrary` | `netstandard2.0` | ADB wrapper for provisioning devices from a desktop |
| `BroadcastReceiverSenderTest` | `netstandard2.0` | a small Android broadcast harness |
| `Febris.MobileP2p.Tests` | `net8.0` | **250 tests** over the P2P stack |

Three solutions ship, one per app plus the ADB library. There is deliberately no aggregate root
solution, and [CONTRIBUTING.md](CONTRIBUTING.md) explains why.

**The iOS heads are not here.** Both were untouched Xamarin.Forms template stubs, neither had the
image assets its project file declared, and building them needs a Mac. Shipping two stubs that
cannot build would have been worse than shipping neither.

## The P2P stack is the interesting part

It lives in `Febris.SharedMobileLibrary/P2pNetworking` and both apps consume the same copy,
because the two halves of a protocol drift the moment you duplicate them.

**Pairing is a human-verified ceremony.** The operator compares a code shown on both screens. Only
one ceremony can be in flight at a time and a second request is refused rather than queued,
because two codes on one screen would make the comparison ambiguous and that comparison is the
entire security argument.

**The handshake is a three-way HMAC challenge-response** between Companion as initiator and Server
as responder, proving both sides hold the same pre-shared key and establishing a per-session
symmetric key. The session key is derived with **HKDF-SHA256 (RFC 5869)**, hand-implemented over
`HMACSHA256` because `netstandard2.1` predates the framework's own HKDF. The long-lived secret
never encrypts bulk traffic directly.

**The wire format is framed and versioned.** Frames carry the magic bytes `FBP2`, and the layout
is defined in exactly one place rather than in per-project constant copies, which is what it
replaced.

**Video is a screen stream**, MediaProjection to MediaCodec to a packetizer, reassembled through a
frame queue and drawn on a SurfaceView in a foreground service. It has run on real hardware.

## Limits worth knowing

- **Manifests still carry `versionCode 1` and pre-rename package identifiers.** They need settling
  before anything is published, because a package identifier is permanent once installed.
- **Bandwidth, latency and resolution figures for the video path have never been measured on
  device.** The pipeline is verified to run. The numbers are not verified.
- The xAPI model types come from the `Febris.XApi.Models` package rather than from source in this
  repository. That package is Apache-2.0 and depending on it puts no copyleft obligation on it.

## Licence

**AGPL-3.0-only.** See [LICENSE](LICENSE).

These are client applications rather than network services, so the network-use clause has little
practical reach here. It is the platform licence and the suite carries it for consistency with the
node that these apps talk to.

## Security

Report vulnerabilities privately through this repository's Security tab. See
[SECURITY.md](SECURITY.md). Please do not open a public issue for a security bug.

The pairing and handshake code is the part where a report matters most.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). The toolchain is the hard part and it is documented there
first, because a contributor who cannot build cannot contribute.
