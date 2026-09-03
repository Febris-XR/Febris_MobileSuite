#!/usr/bin/env python3
"""One source of truth for the mobile version line.

    python mobile/tools/version.py --check
    python mobile/tools/version.py --print
    python mobile/tools/version.py --set 0.2.1
    python mobile/tools/version.py --set-from-tag v0.2.1

WHY THIS EXISTS. The version used to live in four hand-edited places across two heads, plus the
packaging invocation and the distribution feed. Nothing checked that they agreed. A release meant
editing every one of them correctly from memory, and the failure mode is quiet, because an APK
whose manifest says 0.2.0 installs perfectly well while the feed advertises it as 0.2.1. The
mismatch only surfaces on a device, after publishing, and Android identifiers are permanent once
an APK ships.

WHAT IT OWNS. Both AndroidManifest.xml files and both Android AssemblyInfo.cs files. That is
every in-repo carrier of the version. The packaging scripts take --version on the command line
and the feed row is written at release, so those read from the tag rather than from source.

THE TWO HEADS SHARE ONE LINE, DELIBERATELY. The peer wire format couples the Server and the
Companion, so shipping one without the other is the failure this prevents. There is no way to
set them to different versions with this tool, which is the point.

versionCode is DERIVED, never typed. The agreed formula is

    versionCode = MAJOR * 10000 + MINOR * 100 + PATCH

so 0.2.0 is 200 and 0.2.1 is 201. Android refuses to install a build whose versionCode is not
strictly greater than the installed one, and the value is permanent, so deriving it removes the
one arithmetic step nobody performs twice the same way.

ENCODING IS PRESERVED. All four files are UTF-8 with a BOM and CRLF line endings. This script
edits bytes in place through targeted substitutions and never rewrites whole lines, so the BOM,
the line endings and every untouched character survive exactly.
"""

import argparse
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
MOBILE = os.path.dirname(HERE)

SERVER = "FebrisMobileServer/Febris.MobileServerV3.Android/Properties"
COMPANION = "FebrisMobileCompanion/Febris.MobileCompanionV3.Android/Properties"

MANIFESTS = [
    SERVER + "/AndroidManifest.xml",
    COMPANION + "/AndroidManifest.xml",
]
ASSEMBLY_INFOS = [
    SERVER + "/AssemblyInfo.cs",
    COMPANION + "/AssemblyInfo.cs",
]

RE_VERSION_CODE = re.compile(r'(android:versionCode=")(\d+)(")')
RE_VERSION_NAME = re.compile(r'(android:versionName=")([^"]*)(")')
RE_ASM_VERSION = re.compile(r'(\[assembly: AssemblyVersion\(")([^"]*)("\)\])')
RE_ASM_FILE_VERSION = re.compile(r'(\[assembly: AssemblyFileVersion\(")([^"]*)("\)\])')

SEMVER = re.compile(r"^(\d+)\.(\d+)\.(\d+)$")


def version_code(version):
    major, minor, patch = (int(p) for p in SEMVER.match(version).groups())
    if minor > 99 or patch > 99:
        sys.exit("MINOR and PATCH must each stay under 100, or the versionCode formula collides. "
                 "0.2.100 and 0.3.0 would both be 300.")
    return major * 10000 + minor * 100 + patch


def parse_version(raw):
    v = raw.strip()
    if v.startswith("v"):
        v = v[1:]
    if not SEMVER.match(v):
        sys.exit("Version must be MAJOR.MINOR.PATCH, for example 0.2.1. Got %r." % raw)
    return v


def read(rel):
    path = os.path.join(MOBILE, rel.replace("/", os.sep))
    if not os.path.exists(path):
        sys.exit("Missing %s. The tree moved and this script needs updating." % rel)
    with io.open(path, "rb") as fh:
        return path, fh.read().decode("utf-8")


def write(path, text):
    with io.open(path, "wb") as fh:
        fh.write(text.encode("utf-8"))


def find_one(pattern, text, rel, what):
    found = pattern.findall(text)
    if len(found) != 1:
        sys.exit("Expected exactly one %s in %s, found %d. Refusing to guess."
                 % (what, rel, len(found)))
    return found[0][1]


def current():
    """Return {label: (rel, value)} for every carrier, without judging agreement."""
    seen = {}
    for rel in MANIFESTS:
        _, text = read(rel)
        seen[rel + " versionName"] = (rel, find_one(RE_VERSION_NAME, text, rel, "versionName"))
        seen[rel + " versionCode"] = (rel, find_one(RE_VERSION_CODE, text, rel, "versionCode"))
    for rel in ASSEMBLY_INFOS:
        _, text = read(rel)
        seen[rel + " AssemblyVersion"] = (
            rel, find_one(RE_ASM_VERSION, text, rel, "AssemblyVersion"))
        seen[rel + " AssemblyFileVersion"] = (
            rel, find_one(RE_ASM_FILE_VERSION, text, rel, "AssemblyFileVersion"))
    return seen


def check():
    seen = current()
    names = {k: v for k, v in seen.items() if k.endswith("versionName")}
    codes = {k: v for k, v in seen.items() if k.endswith("versionCode")}
    asms = {k: v for k, v in seen.items() if "Assembly" in k}

    problems = []
    distinct = sorted({v for _, v in names.values()})
    if len(distinct) != 1:
        problems.append("versionName disagrees across the heads: %s" % ", ".join(distinct))
        version = None
    else:
        version = distinct[0]
        if not SEMVER.match(version):
            problems.append("versionName %r is not MAJOR.MINOR.PATCH" % version)
            version = None

    if version:
        want_code = str(version_code(version))
        for label, (rel, value) in sorted(codes.items()):
            if value != want_code:
                problems.append("%s has versionCode %s but %s derives %s"
                                % (rel, value, version, want_code))
        want_asm = version + ".0"
        for label, (rel, value) in sorted(asms.items()):
            if value != want_asm:
                problems.append("%s carries %s but the version line is %s, expected %s"
                                % (rel, value, version, want_asm))

    if problems:
        print("FAIL  the mobile version line is inconsistent")
        for p in problems:
            print("  " + p)
        print("\nRun: python mobile/tools/version.py --set <MAJOR.MINOR.PATCH>")
        return 1

    print("OK   mobile version line is %s, versionCode %d, consistent across %d file(s)"
          % (version, version_code(version), len(MANIFESTS) + len(ASSEMBLY_INFOS)))
    return 0


def show():
    for label, (rel, value) in sorted(current().items()):
        print("  %-14s %s" % (value, label.rsplit(" ", 1)[1] + " in " + rel))
    return 0


def apply(version):
    code = str(version_code(version))
    asm = version + ".0"
    changed = []

    for rel in MANIFESTS:
        path, text = read(rel)
        find_one(RE_VERSION_NAME, text, rel, "versionName")
        find_one(RE_VERSION_CODE, text, rel, "versionCode")
        new = RE_VERSION_NAME.sub(lambda m: m.group(1) + version + m.group(3), text)
        new = RE_VERSION_CODE.sub(lambda m: m.group(1) + code + m.group(3), new)
        if new != text:
            write(path, new)
            changed.append(rel)

    for rel in ASSEMBLY_INFOS:
        path, text = read(rel)
        find_one(RE_ASM_VERSION, text, rel, "AssemblyVersion")
        find_one(RE_ASM_FILE_VERSION, text, rel, "AssemblyFileVersion")
        new = RE_ASM_VERSION.sub(lambda m: m.group(1) + asm + m.group(3), text)
        new = RE_ASM_FILE_VERSION.sub(lambda m: m.group(1) + asm + m.group(3), new)
        if new != text:
            write(path, new)
            changed.append(rel)

    print("set version %s, versionCode %s" % (version, code))
    for rel in changed:
        print("  updated %s" % rel)
    if not changed:
        print("  every file already carried it, nothing written")
    print("\nStill to do by hand, because they live outside this repository:")
    print("  1. build and sign both APKs locally, since Xamarin cannot build on a CI runner")
    print("  2. name the release assets febris-mobile-server-v%s.zip and" % version)
    print("     febris-mobile-companion-v%s.zip" % version)
    print("  3. set version %s and versionCode %s on both Android rows of the feed manifest"
          % (version, code))
    print("     in Febris_ClientDist")
    print("\nStep 3 is checked. tools/verify_published.py reads the version back out of the")
    print("shipped APK and refuses to publish a row that disagrees with it.")
    return 0


def main():
    ap = argparse.ArgumentParser(description="Read or set the shared mobile version line.")
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--check", action="store_true",
                   help="verify every carrier agrees and versionCode matches the formula")
    g.add_argument("--print", dest="show", action="store_true",
                   help="list what each file currently carries")
    g.add_argument("--set", metavar="MAJOR.MINOR.PATCH",
                   help="write this version everywhere, deriving versionCode")
    g.add_argument("--set-from-tag", metavar="TAG",
                   help="same as --set, accepting a leading v as in v0.2.1")
    a = ap.parse_args()

    if a.check:
        return check()
    if a.show:
        return show()
    return apply(parse_version(a.set or a.set_from_tag))


if __name__ == "__main__":
    sys.exit(main())
