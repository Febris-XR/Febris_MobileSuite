# Mobile suite packaging

`build-apks.py` builds both Android heads, signs them with the release key, reads each artifact
back to prove it is what was asked for, and packs the zip a node ingests.

```
python mobile/packaging/build-apks.py --version 0.2.1
python mobile/packaging/build-apks.py --version 0.2.1 --skip-build
```

`--version` has no default on purpose. A default is a version number living in the pipeline, and
the value is asserted against the manifest inside each APK, so forgetting it is an error rather
than a silently mislabelled artifact. `--skip-build` takes exactly
`bin/<Config>/<package>-Signed.apk` for each head, the one file the csproj declares for that
configuration, and refuses if it is absent, instead of invoking MSBuild. Nothing is searched for,
because a search once matched a Debug-configuration build.

`--out` is never deleted. Only the four files a run writes are replaced, and an `--out` inside the
keystore directory is refused.

## Two artifacts per app, and the difference matters

Each head produces both an `.apk` and a `.zip`. They are not interchangeable.

The `.apk` is what a person sideloads, and what the site offers for direct download.

The `.zip` is what a NODE ingests. `PackageIngestLogic` gates on `IsZip` and refuses anything
else, so a raw `.apk` can never reach a node. A Mobile Server fetches the Companion from the
node, so with no zip row there is no ADB or OTG distribution at all.

## The trap this script exists to avoid

An APK is itself a zip archive, so renaming `companion.apk` to `companion.zip` passes both of
the node's gates. `IsZip` only reads the filename, and `IsReadableArchive` opens the file
successfully. The node stores the zip unchanged. The Mobile Server then downloads it and unpacks
it, which extracts the APK's own internals, roughly 715 entries with no `.apk` among them. Its
`PrepareInstall` globs `*.apk` over the unpacked folder, finds nothing, and the loop body never
runs. The failure is completely silent.

The zip must CONTAIN an apk rather than BE one renamed. The script asserts that each zip holds
exactly one `.apk` before it reports success.

## Signing

Android identifies an app by package name plus signing certificate. A device that has taken a
build under one certificate refuses an update under another with
`INSTALL_FAILED_UPDATE_INCOMPATIBLE`, and the only way forward is an uninstall.

The release key is `febris-mobile.p12`. It lives off-tree, is never committed and is never placed
in CI. Point `FEBRIS_ANDROID_KEYSTORE_DIR` at the directory holding it and its password file, or
pass `--keystore-dir`. The script hands `apksigner` the password file's path with `--ks-pass
file:` and never opens the file itself, so the password appears in no command line, no process
memory of this script, and no log. The script also refuses a keystore directory that sits inside
any git repository.

After signing, the script reads the certificate back with `apksigner verify --print-certs` and
**refuses to package** anything whose fingerprint is not the recorded release fingerprint,
`e0a8a542…6263f`. That check exists because v0.2.0 shipped signed with the Android debug key
while the release key already existed, and nothing noticed. Every device that took that build has
to be uninstalled once. Two bench devices at the time of writing. It will never be cheaper.

A tester who genuinely wants a debug-signed build passes `--debug-signed`. Every output is then
named `-DEBUGSIGNED` so it cannot be mistaken for a release artifact, and the summary says so.

The keystore's own README carries the owner obligations. Back both files up to two durable
locations, never regenerate over the file, never put it in CI.

## What is asserted, and why

| Check | Catches |
|---|---|
| signer fingerprint equals the release fingerprint | a debug-signed or wrongly signed build reaching the feed |
| `aapt2 dump badging` package, `versionName`, `versionCode` equal what was requested | an APK built at one version and published as another, which installs fine and fails only on a device |
| `versionCode` equals `MAJOR*10000 + MINOR*100 + PATCH` | the manifest and the feed disagreeing on the number Android actually compares |
| `zipalign -c 4` passes after signing | an artifact that lost alignment during re-signing |
| the zip holds exactly one `.apk` | the renamed-APK trap above |

Fix a version mismatch with `python mobile/tools/version.py --set X.Y.Z`, which owns all four
in-repo carriers, then rebuild.

## Reproducibility

The zips are written with a fixed entry timestamp so that the same APK always produces the same
zip bytes. Without that, every rebuild produces a fresh sha256 for an identical payload and
silently invalidates the feed checksum.

APK builds themselves are **not** reproducible. A rebuild of the same commit yields different
bytes, measured. So the feed row must always be derived from the exact bytes that were uploaded,
never from a later rebuild. The script prints each artifact's digest and the `contains[]` entry
in the shape the feed records it, so publishing is a copy rather than a re-derivation.

## Requirements

Windows with VS2022 and the Xamarin workload. `dotnet build` cannot build these heads. Each
head pins its own MonoAndroid version, so they are built one at a time rather than through a
single solution. MSBuild is located through vswhere rather than assumed at one edition's path,
and is invoked with `-restore`, so the first build on a fresh clone needs network access for
NuGet.

`git` on PATH, used to confirm the keystore directory is not inside any repository. That check
fails closed if git is missing.

The Android SDK build-tools, for `apksigner`, `aapt2` and `zipalign`. The newest installed
version that carries all three is used. `ANDROID_SDK_ROOT` and `ANDROID_HOME` are honoured, and
the default SDK location under `%LOCALAPPDATA%` is tried last.

## The full release pipeline

This file covers building the artifacts. Publishing them, writing the feed rows and the website
are covered by `docs/RELEASE_PIPELINE_PLAN.md` in
[Febris-XR/Febris_ClientDist](https://github.com/Febris-XR/Febris_ClientDist), which is the
release coordinator.
