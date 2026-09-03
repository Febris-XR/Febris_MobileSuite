#!/usr/bin/env python3
"""Build, release-sign and package the Febris mobile suite APKs.

    python mobile/packaging/build-apks.py --version 0.2.1 [--out DIR]
    python mobile/packaging/build-apks.py --version 0.2.1 --skip-build
    python mobile/packaging/build-apks.py --version 0.2.1 --debug-signed      (testers only)

Set FEBRIS_ANDROID_KEYSTORE_DIR to the OFF-TREE directory holding febris-mobile.p12 and
febris-mobile.p12.password.txt, or pass --keystore-dir. This script never reads the password. It
hands apksigner the path and apksigner reads the file itself.

WHY THIS EXISTS. The v0.2.0 APKs were built by hand from a temporary directory, which meant
published binaries nobody could reproduce. This is that recipe, made strict.

TWO ARTIFACTS PER APP, AND THE DIFFERENCE MATTERS.

  * The .apk is what a person sideloads, and what the website offers for direct download.
  * The .zip is what a NODE ingests. PackageIngestLogic gates on IsZip and refuses anything
    else, so a raw .apk can never reach a node. The Mobile Server fetches the Companion FROM
    the node, so without the zip there is no ADB or OTG distribution at all.

THE TRAP THIS SCRIPT AVOIDS. An APK *is* a zip archive, so simply renaming companion.apk to
companion.zip passes both of the node's gates. IsZip reads the filename and IsReadableArchive
opens it successfully. It then extracts to the APK's own internals, roughly 715 entries with no
.apk among them. PrepareInstall globs "*.apk" over the extracted folder, finds nothing, and the
loop body never runs, so the failure is completely silent. The zip must CONTAIN an apk, not BE
one renamed. The script asserts that each zip holds exactly one .apk.

SIGNING, AND WHY IT IS ASSERTED RATHER THAN TRUSTED. Android identifies an app by package name
PLUS signing certificate. v0.2.0 shipped signed with the Android DEBUG key while the release key
already existed, and every device that took that build has to be uninstalled once before it can
take a release-signed one. That mistake was invisible because nothing checked the signer after
the build. Now the script signs with the release key through apksigner and then REFUSES to
continue unless the certificate fingerprint it reads back equals the recorded release
fingerprint. A debug-signed build is still possible for a tester, but only by asking for it, and
its filenames say so.

VERSION, ALSO ASSERTED. The manifest inside the APK is read back with aapt2 and its package,
versionName and versionCode must match what was requested. An APK built at one version and
published as another installs perfectly and fails only on a device, after the identifiers are
permanent.

REQUIREMENTS. Windows with VS2022 and the Xamarin workload, and the Android SDK build-tools for
apksigner, aapt2 and zipalign. `dotnet build` cannot build these heads. Each head needs its own
MonoAndroid version, so they are built separately.
"""

import argparse
import hashlib
import os
import re
import shutil
import subprocess
import sys
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
MOBILE = os.path.dirname(HERE)

# The certificate fingerprint of febris-mobile.p12. Public, recorded in the keystore's own README.
# The distribution feed records each row's actual signer as signerSha256, so the 0.2.0 rows carry
# the debug signer that shipped and rows from 0.2.1 onward carry this value. A build whose signer
# is anything else is not a release build, whatever else is true of it.
RELEASE_SIGNER_SHA256 = "e0a8a54223e4351a550376579e1b750c4c6b2825e5c30be49c9cdb93c996263f"
KEYSTORE_FILE = "febris-mobile.p12"
PASSWORD_FILE = "febris-mobile.p12.password.txt"
KEY_ALIAS = "febris-mobile"

HEADS = [
    {
        "name": "mobile-server",
        "proj": "FebrisMobileServer/Febris.MobileServerV3.Android/Febris.MobileServerV3.Android.csproj",
        "outdir": "FebrisMobileServer/Febris.MobileServerV3.Android/bin",
        "package": "com.febris.mobileserver",
    },
    {
        "name": "mobile-companion",
        "proj": "FebrisMobileCompanion/Febris.MobileCompanionV3.Android/Febris.MobileCompanionV3.Android.csproj",
        "outdir": "FebrisMobileCompanion/Febris.MobileCompanionV3.Android/bin",
        "package": "com.febris.companion",
    },
]

SEMVER = re.compile(r"^(\d+)\.(\d+)\.(\d+)$")


# ---------------------------------------------------------------------------------------------
# Tool location. Nothing here is hardcoded to one machine.

# MSBuild is LOCATED, not hardcoded. The Xamarin.Android targets ship with Visual Studio rather
# than the .NET SDK, so this has to be the Visual Studio MSBuild. Where that lives is not fixed
# across editions. Identical to the resolver in the PC packaging recipe, on purpose.
#
# Order is deliberate. MSBUILD_PATH is the explicit override and always wins. vswhere is how
# Visual Studio itself answers this question. PATH is next. The edition sweep is a last resort.
def find_msbuild():
    override = os.environ.get("MSBUILD_PATH")
    if override:
        if not os.path.exists(override):
            sys.exit("MSBUILD_PATH is set to %s but nothing is there." % override)
        return override

    vswhere = os.path.join(
        os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)"),
        "Microsoft Visual Studio", "Installer", "vswhere.exe")
    if os.path.exists(vswhere):
        try:
            found = subprocess.check_output(
                [vswhere, "-latest", "-products", "*",
                 "-requires", "Microsoft.Component.MSBuild",
                 "-find", r"MSBuild\**\Bin\MSBuild.exe"],
                universal_newlines=True, stderr=subprocess.PIPE).strip().splitlines()
            for line in found:
                if line.strip() and os.path.exists(line.strip()):
                    return line.strip()
        except Exception:
            pass

    on_path = shutil.which("msbuild")
    if on_path:
        return on_path

    for edition in ("Enterprise", "Professional", "Community", "BuildTools"):
        guess = os.path.join(
            r"C:\Program Files\Microsoft Visual Studio\2022", edition,
            "MSBuild", "Current", "Bin", "MSBuild.exe")
        if os.path.exists(guess):
            return guess
    return None


def find_build_tools():
    """The newest Android build-tools directory that carries all three tools we need."""
    roots = [os.environ.get("ANDROID_SDK_ROOT"), os.environ.get("ANDROID_HOME"),
             os.path.join(os.environ.get("LOCALAPPDATA", ""), "Android", "Sdk")]
    for root in roots:
        if not root:
            continue
        base = os.path.join(root, "build-tools")
        if not os.path.isdir(base):
            continue
        versions = []
        for d in os.listdir(base):
            parts = d.split(".")
            if all(p.isdigit() for p in parts):
                versions.append((tuple(int(p) for p in parts), d))
        for _, d in sorted(versions, reverse=True):
            path = os.path.join(base, d)
            tools = {
                "apksigner": os.path.join(path, "apksigner.bat"),
                "aapt2": os.path.join(path, "aapt2.exe"),
                "zipalign": os.path.join(path, "zipalign.exe"),
            }
            if all(os.path.exists(p) for p in tools.values()):
                return tools
    sys.exit("No Android build-tools with apksigner, aapt2 and zipalign found. Set "
             "ANDROID_SDK_ROOT, or install build-tools through the SDK manager.")


# ---------------------------------------------------------------------------------------------

def run(cmd, capture=False):
    shown = [c for c in cmd]
    print("  $ %s" % " ".join(shown))
    if capture:
        r = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                           universal_newlines=True)
        if r.returncode != 0:
            sys.exit("FAILED: exit %d\n%s" % (r.returncode, r.stdout))
        return r.stdout
    r = subprocess.run(cmd)
    if r.returncode != 0:
        sys.exit("FAILED: exit %d" % r.returncode)
    return ""


def parse_version(raw):
    v = raw.strip()
    if v.startswith("v"):
        v = v[1:]
    m = SEMVER.match(v)
    if not m:
        sys.exit("--version must be MAJOR.MINOR.PATCH, for example 0.2.1. Got %r." % raw)
    major, minor, patch = (int(p) for p in m.groups())
    if minor > 99 or patch > 99:
        sys.exit("MINOR and PATCH must each stay under 100 or the versionCode formula collides. "
                 "0.2.100 and 0.3.0 would both be 300.")
    # The ratified formula. mobile/tools/version.py derives the same number into the manifests,
    # and the feed row carries it. Three places, one arithmetic, asserted below.
    return v, major * 10000 + minor * 100 + patch


def keystore_paths(a):
    """Where the release key lives. The password file is located and never opened here."""
    d = a.keystore_dir or os.environ.get("FEBRIS_ANDROID_KEYSTORE_DIR")
    if not d:
        sys.exit("Release signing needs the off-tree keystore directory. Set "
                 "FEBRIS_ANDROID_KEYSTORE_DIR or pass --keystore-dir. For a tester build that "
                 "is deliberately debug-signed, pass --debug-signed instead.")
    ks = os.path.join(d, KEYSTORE_FILE)
    pw = os.path.join(d, PASSWORD_FILE)
    for p in (ks, pw):
        if not os.path.isfile(p):
            sys.exit("Expected %s in the keystore directory and it is not there." % os.path.basename(p))
    try:
        inside = subprocess.run(["git", "-C", d, "rev-parse", "--show-toplevel"],
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    except FileNotFoundError:
        # The off-tree check is a safety property, so it fails closed rather than being skipped.
        sys.exit("git is not on PATH, so the keystore directory cannot be confirmed to be off-tree. "
                 "Refusing.")
    if inside.returncode == 0:
        sys.exit("The keystore directory %s is inside a git repository. It must live off-tree. "
                 "Refusing." % d)
    return ks, pw


def find_apk(base, package, config):
    """The exact file Xamarin declares for this configuration, bin/<Config>/<package>-Signed.apk.

    This used to walk bin/ and keep any directory whose PATH contained the configuration name.
    On the ratified clone location that path contains "FebrisGitHubReleases", which contains
    "release", so the filter accepted every directory including bin/Debug, and for the Server
    head the Debug output directory is bin/ itself. A stale Debug-configuration APK would have
    been picked up, re-signed with the release key so the signer check passed, carried the same
    package and version so the badging check passed, and shipped unoptimised and debuggable
    with every assertion green. Resolving the one declared path removes the whole class.
    """
    path = os.path.join(MOBILE, base.replace("/", os.sep), config, package + "-Signed.apk")
    return path if os.path.isfile(path) else None


def signer_of(apk, apksigner):
    out = run([apksigner, "verify", "--print-certs", apk], capture=True)
    dn = re.search(r"Signer #1 certificate DN: (.+)", out)
    sha = re.search(r"Signer #1 certificate SHA-256 digest: ([0-9a-f]{64})", out)
    if not dn or not sha:
        sys.exit("apksigner did not report a signer for %s:\n%s" % (apk, out))
    return dn.group(1).strip(), sha.group(1).lower()


def badging_of(apk, aapt2):
    """Package, versionCode, versionName and whether the manifest marks the app debuggable.

    application-debuggable is the direct signal that a Debug-configuration build slipped through.
    A Release build carries no such line. It is checked as well as the path in find_apk, because
    two independent guards against shipping a debug build are worth more than one.
    """
    out = run([aapt2, "dump", "badging", apk], capture=True)
    m = re.search(r"package: name='([^']+)' versionCode='(\d+)' versionName='([^']*)'", out)
    if not m:
        sys.exit("aapt2 did not report a package line for %s:\n%s" % (apk, out))
    debuggable = re.search(r"^application-debuggable", out, re.M) is not None
    return m.group(1), int(m.group(2)), m.group(3), debuggable


def sign_release(unsigned, signed, ks, pw, apksigner):
    # --ks-pass file: makes apksigner read the password itself. It is never on this command line,
    # never in this process's memory, and never in a log. --v4 is off so no .idsig sidecar appears
    # beside the artifact and gets mistaken for one.
    #
    # --key-pass is deliberately ABSENT. apksigner reads passwords from a file one line per
    # password, in order, so naming the same one-line file for both the store and the key makes
    # the second read hit end of file and fail. For a PKCS12 store the key password equals the
    # store password, which is what apksigner assumes when --key-pass is not given.
    run([apksigner, "sign",
         "--ks", ks, "--ks-type", "PKCS12", "--ks-key-alias", KEY_ALIAS,
         "--ks-pass", "file:" + pw,
         "--v4-signing-enabled", "false",
         "--out", signed, unsigned])


def main():
    ap = argparse.ArgumentParser(description="Build, sign and package the mobile suite APKs.")
    ap.add_argument("--version", required=True,
                    help="MAJOR.MINOR.PATCH. No default on purpose. The value is asserted "
                         "against the manifest inside each APK.")
    ap.add_argument("--config", default="Release")
    ap.add_argument("--out", default=os.path.join(HERE, "out"))
    ap.add_argument("--skip-build", action="store_true",
                    help="repackage what is already under bin/Release instead of invoking MSBuild")
    ap.add_argument("--keystore-dir", default=None,
                    help="off-tree directory holding %s and %s. Overrides "
                         "FEBRIS_ANDROID_KEYSTORE_DIR." % (KEYSTORE_FILE, PASSWORD_FILE))
    ap.add_argument("--debug-signed", action="store_true",
                    help="testers only. Keep the Android debug signature, and name every "
                         "output -DEBUGSIGNED so it cannot be mistaken for a release artifact.")
    a = ap.parse_args()

    version, code = parse_version(a.version)
    tools = find_build_tools()
    ks = pw = None
    if not a.debug_signed:
        ks, pw = keystore_paths(a)

    msbuild = None
    if not a.skip_build:
        msbuild = find_msbuild()
        if not msbuild:
            sys.exit("MSBuild not found. These heads need VS2022 with the Xamarin workload, or "
                     "set MSBUILD_PATH.")
        print("== toolchain\n  msbuild    %s\n  build-tools %s" % (msbuild, os.path.dirname(tools["apksigner"])))

    # --out is never recursively deleted. It is a user-supplied path, and a typo or a relative
    # path would have wiped whatever it named. Only the four files this run is about to write are
    # removed, and writing anywhere under the keystore directory is refused outright.
    out_dir = os.path.abspath(a.out)
    if ks:
        keystore_dir = os.path.abspath(os.path.dirname(ks))
        try:
            shared = os.path.commonpath([out_dir, keystore_dir])
        except ValueError:
            shared = None
        if shared == keystore_dir:
            sys.exit("--out %s is inside the keystore directory. Refusing." % a.out)
    os.makedirs(out_dir, exist_ok=True)

    suffix = "-DEBUGSIGNED" if a.debug_signed else ""
    for h in HEADS:
        for ext in (".apk", ".zip"):
            stale = os.path.join(out_dir, "febris-%s-v%s%s%s" % (h["name"], version, suffix, ext))
            if os.path.exists(stale):
                os.remove(stale)
    results, rows = [], []
    for h in HEADS:
        print("== %s" % h["name"])
        if not a.skip_build:
            # -restore, so a fresh clone builds. Without it the first build on a clean checkout
            # fails on missing packages, which the PC recipe already learned.
            run([msbuild, os.path.join(MOBILE, h["proj"].replace("/", os.sep)), "-restore",
                 "-t:SignAndroidPackage", "-p:Configuration=" + a.config, "-v:m", "-nologo"])

        built = find_apk(h["outdir"], h["package"], a.config)
        if not built:
            sys.exit("  no %s/%s/%s-Signed.apk. Only the file the csproj declares for the %s "
                     "configuration is accepted, nothing is searched for."
                     % (h["outdir"], a.config, h["package"], a.config))

        named = os.path.join(a.out, "febris-%s-v%s%s.apk" % (h["name"], version, suffix))
        if a.debug_signed:
            shutil.copy2(built, named)
        else:
            sign_release(built, named, ks, pw, tools["apksigner"])

        # Alignment must survive re-signing or the APK loses the page-aligned resource access
        # Android depends on. zipalign -c is a check, it writes nothing.
        run([tools["zipalign"], "-c", "4", named], capture=True)

        # Read the artifact back and refuse anything that is not what was asked for.
        dn, sha = signer_of(named, tools["apksigner"])
        if a.debug_signed:
            if "Android Debug" not in dn:
                sys.exit("  --debug-signed was requested but the signer is %s" % dn)
            print("  signer   %s (DEBUG, by request)" % dn)
        else:
            if sha != RELEASE_SIGNER_SHA256:
                sys.exit("  signer fingerprint is %s\n  expected %s\n  This is not the release "
                         "key. Refusing to package it." % (sha, RELEASE_SIGNER_SHA256))
            print("  signer   %s  %s" % (dn, sha[:16]))

        pkg, got_code, got_name, debuggable = badging_of(named, tools["aapt2"])
        problems = []
        if debuggable and not a.debug_signed:
            problems.append("the manifest marks the app debuggable, so this is a Debug-configuration "
                            "build and not a release artifact")
        if pkg != h["package"]:
            problems.append("package is %s, expected %s" % (pkg, h["package"]))
        if got_name != version:
            problems.append("versionName is %s, expected %s" % (got_name, version))
        if got_code != code:
            problems.append("versionCode is %d, expected %d" % (got_code, code))
        if problems:
            sys.exit("  the built APK disagrees with --version %s:\n    %s\n  Run "
                     "mobile/tools/version.py --set %s and rebuild." % (version, "\n    ".join(problems), version))
        print("  manifest %s versionName %s versionCode %d" % (pkg, got_name, got_code))

        # The zip CONTAINS the apk. See the trap described in this file's docstring.
        #
        # Written DETERMINISTICALLY. A plain z.write() stamps the current time into the entry
        # header, so rebuilding the same APK produces a zip with a different sha256. The feed
        # records that sha256, so a non-reproducible zip means every rebuild silently invalidates
        # the manifest even when the payload is byte-identical. Fixing the timestamp makes the zip
        # a pure function of its contents.
        zpath = os.path.join(a.out, "febris-%s-v%s%s.zip" % (h["name"], version, suffix))
        info = zipfile.ZipInfo(h["package"] + ".apk", date_time=(1980, 1, 1, 0, 0, 0))
        info.compress_type = zipfile.ZIP_DEFLATED
        info.external_attr = 0o644 << 16
        with open(named, "rb") as fh:
            apk_bytes = fh.read()
        with zipfile.ZipFile(zpath, "w") as z:
            z.writestr(info, apk_bytes)
        with zipfile.ZipFile(zpath) as z:
            inner = [n for n in z.namelist() if n.lower().endswith(".apk")]
        if len(inner) != 1:
            sys.exit("  the zip must contain exactly one .apk, found %r" % inner)
        print("  zip      contains %s" % inner[0])

        for p in (named, zpath):
            with open(p, "rb") as fh:
                raw = fh.read()
            results.append((os.path.basename(p), len(raw), hashlib.sha256(raw).hexdigest()))
        rows.append((h["package"] + ".apk", len(apk_bytes), hashlib.sha256(apk_bytes).hexdigest(), sha))

    print("\n== artifacts")
    for n, size, digest in results:
        print("  %-46s %10d bytes  sha256 %s" % (n, size, digest))

    print("\n== feed contains[] entries, in the shape the distribution feed records them")
    for name, size, digest, signer in rows:
        print('  { "fileName": "%s", "sizeBytes": %d,' % (name, size))
        print('    "sha256": "%s",' % digest)
        print('    "signerSha256": "%s" }' % signer)

    if a.debug_signed:
        print("\n  DEBUG-SIGNED. Not for release, not for the feed, and a device that takes this build")
        print("  must be uninstalled before it can take a release-signed one.")
    else:
        print("\n  Feed rows point at the .zip, since a node ingests nothing else. The .apk is for")
        print("  people, published as a release asset beside it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
