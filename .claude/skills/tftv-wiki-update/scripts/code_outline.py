#!/usr/bin/env python3
"""List what TFTV code changes, file by file, with line numbers ready to cite.

  python3 -I code_outline.py SRC_DIR PATH [PATH ...] [--grep TERM]

SRC_DIR is the release checkout ($WORK/src). Each PATH is a file or folder under it (e.g.
TFTV/TFTVVests.cs or TFTV/TFTVDrills). For every .cs file it prints, by method:
  patch    Harmony patches: which game method TFTV changes the behaviour of
  def      defs fetched by name (DefCache.GetDef<Type>("Name")): the game data being edited
  set      literal values assigned to fields (damage, costs, charges, ...)
  loc      localization keys used (checked against the release's CSVs): look them up with loc_find.py --key
Commented-out code is skipped: it does not run in the game. Lines print as path:line, the form the
ledger's [src: ...] citations use. --grep keeps only lines matching TERM (a case-insensitive
regular expression), plus the method each one sits in.

This is a map to read from, not a substitute for reading: follow each item into the code to see
the conditions around it (difficulty, config options, Aircraft Rework Beta checks).
"""
import argparse
import csv
import os
import re
import sys

PATCH = re.compile(r"\[HarmonyPatch\(\s*typeof\((\w+)\)\s*(?:,\s*(?:nameof\(\w+\.(\w+)\)|\"(\w+)\"))?")
METHOD = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|internal|protected|static|override|"
                    r"virtual|async|unsafe|extern|new)\s+)+[\w<>\[\],\.\s]+?\s+(\w+)\s*\([^;]*$")
GETDEF = re.compile(r"GetDef<(\w+)>\(\s*\"([^\"]+)\"\s*\)")
GETDEF_GUID = re.compile(r"(?:\((\w+)\)\s*)?Repo\.GetDef\(\s*\"([0-9a-f\-]{36})\"\s*\)")
INIT = re.compile(r"(?<![\.\w])([A-Z]\w*)\s*=\s*(-?\d+(?:\.\d+)?f?|true|false)\s*(?=[,}]|$)")
SET = re.compile(r"([A-Za-z_][\w\.\[\]]*\.\w+)\s*=\s*(-?\d+(?:\.\d+)?f?|true|false)\s*;")
STRING = re.compile(r"\"([A-Za-z][A-Za-z0-9_]{5,})\"")
FLAGS = re.compile(r"AircraftReworkOn|AircraftReworkMode\.RunningOn|TFTVNewGameOptions\.\w+|config\.\w+|Config\.\w+")


def strip_comments(lines):
    """Blank out // and /* */ comments, keeping line numbers. Good enough for this codebase."""
    out, in_block = [], False
    for line in lines:
        res, i = "", 0
        in_str = False
        while i < len(line):
            two = line[i:i + 2]
            if in_block:
                if two == "*/":
                    in_block, i = False, i + 2
                    continue
                i += 1
                continue
            c = line[i]
            if c == '"' and (i == 0 or line[i - 1] != "\\"):
                in_str = not in_str
            if not in_str and two == "//":
                break
            if not in_str and two == "/*":
                in_block, i = True, i + 2
                continue
            res += c
            i += 1
        out.append(res)
    return out


def loc_keys(src):
    d = os.path.join(src, "Dist", "Assets", "Localization")
    keys = set()
    if os.path.isdir(d):
        for name in os.listdir(d):
            if name.endswith(".csv"):
                with open(os.path.join(d, name), encoding="utf-8-sig", newline="") as f:
                    keys.update((r.get("Key") or "").strip() for r in csv.DictReader(f))
    return keys


def outline(path, rel, grep, keys):
    with open(path, encoding="utf-8-sig", errors="replace") as f:
        lines = strip_comments(f.read().split("\n"))
    method = "(file level)"
    printed_method = None
    out = []
    pending_patch = []
    for n, line in enumerate(lines, 1):
        if not line.strip():
            continue
        items = []
        for m in PATCH.finditer(line):
            target = m[1] + ("." + (m[2] or m[3]) if (m[2] or m[3]) else "")
            pending_patch.append(target)
            items.append(f"patch    {target}")
        mm = METHOD.match(line)
        if mm and mm[1] not in ("if", "for", "foreach", "while", "switch", "catch", "using", "lock"):
            method = mm[1]
        for m in GETDEF.finditer(line):
            items.append(f"def      {m[1]} \"{m[2]}\"")
        for m in GETDEF_GUID.finditer(line):
            items.append(f"def      {m[1] or '?'} by id {m[2]} (see the variable name and the lines below)")
        for m in SET.finditer(line):
            items.append(f"set      {m[1]} = {m[2]}")
        for m in INIT.finditer(line):
            items.append(f"set      {m[1]} = {m[2]}  (in an initializer: read the lines above)")
        for m in STRING.finditer(line):
            if m[1] in keys:
                items.append(f"loc      {m[1]}")
        fl = sorted(set(FLAGS.findall(line)))
        if fl and (items or re.search(r"\bif\s*\(", line)):
            items.append("when     " + ", ".join(fl))
        if grep:
            items = [i for i in items if grep.search(i) or grep.search(line)]
            if not items and grep.search(line):
                items = ["line     " + " ".join(line.split())[:140]]
        if not items:
            continue
        if printed_method != method:
            out.append(f"  {method}" + ("" if method.startswith("(") else "()"))
            printed_method = method
        for i in items:
            out.append(f"    {rel}:{n}  {i}")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("paths", nargs="+")
    ap.add_argument("--grep")
    a = ap.parse_args()
    grep = re.compile(a.grep, re.I) if a.grep else None
    keys = loc_keys(a.src)
    files = []
    for p in a.paths:
        full = os.path.join(a.src, p)
        if os.path.isdir(full):
            for root, _, names in os.walk(full):
                files += [os.path.join(root, x) for x in sorted(names) if x.endswith(".cs")]
        elif os.path.isfile(full):
            files.append(full)
        else:
            print(f"(not in this release: {p})", file=sys.stderr)
    for f in sorted(files):
        rel = os.path.relpath(f, a.src)
        body = outline(f, rel, grep, keys)
        if body:
            print(f"== {rel}")
            print("\n".join(body))


if __name__ == "__main__":
    main()
