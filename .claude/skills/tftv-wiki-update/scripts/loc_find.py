#!/usr/bin/env python3
"""Search TFTV's localization files: the texts players see in game.

  python3 -I loc_find.py SRC_DIR TERM [TERM ...] [--file Options] [--key] [--full] [--max 40]

SRC_DIR is the release checkout ($WORK/src). Every TERM (a case-insensitive regular expression) must
match the key or the English text. With --key, terms match the key only. Each hit prints in the
citation form the ledger uses, [loc: FILE:KEY], followed by the English text. Standard library only.

Examples:
  loc_find.py "$WORK/src" "deployment pos"            # find the option behind a changelog item
  loc_find.py "$WORK/src" hazmat vest --full          # full item description
  loc_find.py "$WORK/src" --key ^KEY_PHOENIXPEDIA_GUIDE_ --file Phoenixpedia --max 200
"""
import argparse
import csv
import os
import re
import sys

LOC_DIR = os.path.join("Dist", "Assets", "Localization")


def load(src, file_filter=None):
    d = os.path.join(src, LOC_DIR)
    if not os.path.isdir(d):
        sys.exit(f"No {LOC_DIR} in {src}: pass the release checkout ($WORK/src).")
    for name in sorted(os.listdir(d)):
        if not name.endswith(".csv") or (file_filter and file_filter.lower() not in name.lower()):
            continue
        with open(os.path.join(d, name), encoding="utf-8-sig", newline="") as f:
            for row in csv.DictReader(f):
                key = (row.get("Key") or "").strip()
                if key:
                    yield name, key, row.get("English") or ""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("terms", nargs="+")
    ap.add_argument("--file", help="only files whose name contains this")
    ap.add_argument("--key", action="store_true", help="match terms against the key only")
    ap.add_argument("--full", action="store_true", help="print the whole text, not 300 characters")
    ap.add_argument("--max", type=int, default=40)
    a = ap.parse_args()

    rx = [re.compile(t, re.I) for t in a.terms]
    hits = 0
    for name, key, text in load(a.src, a.file):
        hay = key if a.key else key + "\n" + text
        if all(r.search(hay) for r in rx):
            hits += 1
            if hits > a.max:
                continue
            body = " ".join(text.split()) if not a.full else text.strip()
            if not a.full and len(body) > 300:
                body = body[:300] + " …"
            print(f"[loc: {name}:{key}]\n    {body or '(no English text)'}")
    if hits > a.max:
        print(f"... {hits - a.max} more; narrow the terms or raise --max.")
    if not hits:
        print("No match.")


if __name__ == "__main__":
    main()
