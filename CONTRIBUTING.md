# Contributing

Thanks for looking. The toolchain is the hard part of this repository, so it comes first. A
contributor who cannot build cannot contribute, and the honest answer here is more complicated
than "install the SDK".

## What you need

**Clone to a SHORT path.** Measured on 2026-08-29, not theoretical: `aapt2` resource compilation
fails with `APT2098`/`APT2101`/`APT2261` ("failed to open file" on `.flat` intermediates) when the
repository sits in a deeply nested directory, because the generated `obj\Release\...\lp\NN\jl\res`
intermediates cross the Windows 260-character MAX_PATH limit. The identical tree builds cleanly
from a short root. `C:\src\febris-mobile` is fine; somewhere under a temp or profile
hierarchy probably is not. `subst X: <deep-path>` works as a rescue if you are already cloned deep.

**To build the two Android apps: Windows, Visual Studio 2022, and the Xamarin workload.** There is
no way around this. The Android heads are legacy non-SDK-style project files that import
`Xamarin.Android.CSharp.targets` from a Visual Studio install path, and the `dotnet` CLI does not
have it. Pointing `dotnet build` at either head fails with `MSB4019` on that import, which is the
expected result rather than a misconfiguration on your side.

Xamarin.Forms went out of support in May 2024, so the workload is a legacy install. Migration to
.NET MAUI is the intended path and has not started.

**To build and test everything else: the .NET 8 SDK is enough.** Six of the eight projects are
SDK-style and build on any platform:

```bash
dotnet build mobile/FebrisMobileServer/Febris.SharedMobileLibrary/Febris.SharedMobileLibrary.csproj -c Release
dotnet build mobile/FebrisMobileServer/Febris.MobileServerV3/Febris.MobileServerV3.csproj -c Release
dotnet build mobile/FebrisMobileCompanion/Febris.MobileCompanionV3/Febris.MobileCompanionV3.csproj -c Release
dotnet build mobile/FebrisAdbLibrary/Febris.AdbLibrary.csproj -c Release
dotnet test  tests/FebrisMobileP2pTests/Febris.MobileP2p.Tests.csproj -c Release
```

That covers the entire P2P stack, which is where nearly all the logic lives. **250 tests** run
this way with no Android device and no Xamarin install, and they are the practical gate for most
changes.

## There is no root solution, on purpose

Three solutions ship, one per app plus the ADB library. No aggregate solution is generated, and
that was measured rather than assumed: `dotnet sln add` over this file set produces a solution
containing **six of the eight** projects. It prints "Invalid project" for both Android heads and
then exits zero, so the failure is silent. A root solution that quietly omits the two app heads is
worse than no root solution, because it is the first thing a stranger opens and the apps are not
in it.

Open `Febris.MobileServerV3.sln` or `Febris.MobileCompanionV3.sln` in Visual Studio. Each already
contains its own head.

## The P2P protocol is a contract between two apps

This is the rule that matters most here. Server and Companion are separate applications that ship
separately and may be updated at different times on real devices.

- **Both halves change together, in one pull request.** A frame layout, a field, a handshake step
  or an acknowledgement code that changes on one side and not the other produces a pair that
  connects and then misbehaves in the field, which is the worst failure mode this project has.
- **Shared protocol code belongs in `Febris.SharedMobileLibrary`.** It is shared precisely so the
  two halves cannot drift. If you find yourself copying a constant into both apps, that constant
  belongs in the shared library, and the current wire-format class exists because that copying had
  already happened once.
- **The wire format is versioned.** A change that is not backward compatible needs the version to
  move, not a silent reinterpretation of the same bytes.

## One file here is vendored, and it is labelled

`Febris.SharedMobileLibrary/Utilites/NodeReachability.cs` is maintained in the Febris node
platform's shared services library and copied into this repository. Its header says so and says
why: the upstream library targets net8.0 and this one targets netstandard2.1, so the assembly
cannot be shared, and publishing it as a package of its own would have added a release gate for
one small class.

If you change it, say in the pull request what the upstream change is, so the two copies do not
drift. Its behaviour is pinned by `VendoredNodeReachabilityTests`, which exists precisely because
a copy nothing exercises is a copy that drifts.

## The cryptography

`P2pNetworking/Crypto` implements pairing and a three-way HMAC challenge-response handshake, with
session keys derived through HKDF-SHA256 per RFC 5869. The HKDF is hand-written because
`netstandard2.1` predates the framework implementation.

Two requests:

- **Do not replace a primitive with your own construction.** If the framework grows an HKDF this
  target can reach, swapping to it is a welcome change and needs the existing tests to stay green
  unmodified, since those tests are the compatibility statement.
- **Do not weaken the pairing ceremony to make it smoother.** One ceremony at a time, refused
  rather than queued, and a code a human compares on two screens. Every part of that is
  load-bearing. A change that makes pairing more convenient by removing the human comparison
  removes the security argument with it.

Security issues go through the private channel in [SECURITY.md](SECURITY.md), never a public
issue.

## Testing

The suites live in `tests/FebrisMobileP2pTests` and cover framing, the handshake and its
coordinator, pairing sessions and secret storage, direction legality, safe file names, and the
video packetizer and frame queue.

- A change to the P2P stack needs a test in the same pull request.
- A change that can only be verified on hardware should say so, and say what you ran it on. Device
  behaviour is not something the suite can reach, and pretending otherwise helps nobody.
- `BroadcastReceiverSenderTest` is a small manual Android harness rather than an automated suite.
  It is not in any of the three solutions and is not run by CI.

## Style

- One logical change per pull request.
- Match the surrounding file. The codebase is not uniform and there is no formatter gate.
- New first-party source files carry the two-line SPDX header the existing files carry.
- Say how you verified the change, and be specific when the answer is "on a device".

## Licence

By contributing you agree that your contributions are licensed under AGPL-3.0-only, the same
licence as the project. See [LICENSE](LICENSE).
