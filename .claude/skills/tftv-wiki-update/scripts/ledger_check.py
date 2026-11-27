#!/usr/bin/env python3
"""Check that every change the ledger puts on the page is backed by the release code or its texts.

  python3 -I ledger_check.py wiki/LEDGER.md --src "$WORK/src" [--page wiki/Terror_from_the_Void.wiki]
                             [--sample 10] [--show]

Ledger lines look like
  - [x] <changelog item, paraphrased> → <disposition> [src: TFTV/TFTVVests.cs:120-134] [loc: TFTV_CharactersItemsFacilities_Localization.csv:KEY_…]
Citations:
  [src: PATH:LINE] or [src: PATH:FIRST-LAST]   a file in the release checkout, path from its root
  [loc: FILE.csv:KEY]                          a key in Dist/Assets/Localization/FILE.csv
  [page: "exact words"]                        text that is on the page (for `already`)
  [searched: what you looked for, where]       how you established that something is gone
Several citations of each kind are allowed on one line.

Rules (done lines, [x]):
  → <Section> / → <Section> (beta)    at least one [src:] or [loc:]
  already                             [page:] found in the page, and at least one [src:] or [loc:]
  vanilla-fix                         [src:] or [loc:] preferred; missing ones are listed as warnings
  removed-in-code                     [searched: …]
  superseded: <release>               names the release
  skip: … / question                  nothing required
Every citation must exist: the file, the line range inside it, the CSV key. A [src:] whose lines
are all comments, blank, braces or using/namespace lines fails: commented-out code does not run, and
the top of a file proves nothing.
Open lines ([ ]) are counted and listed, not failed.

--sample N prints N random done lines with the code and text they cite, for a spot check that the
citation really says what the ledger line claims. --show prints every citation. The exit status is 1
when any done line breaks a rule.
"""
import argparse
import collections
import csv
import json
import os
import random
import re
import sys

LINE = re.compile(r"^\s*[-*]\s*\[( |x|X)\]\s*(.*)$")
LOC_DIR = os.path.join("Dist", "Assets", "Localization")


def parse_cites(text):
    cites = collections.defaultdict(list)
    for kind, val in re.findall(r"\[(src|loc|page|searched):\s*([^\]]*)\]", text):
        cites[kind].append(val.strip())
    return cites


def disposition(body):
    if "→" not in body:
        return None, body
    item, rest = body.split("→", 1)
    rest = re.split(r"\[(?:src|loc|page|searched):", rest, maxsplit=1)[0]
    return rest.strip(), item.strip()


def kind_of(disp):
    d = disp.lower()
    for k in ("vanilla-fix", "already", "removed-in-code", "superseded", "skip", "question"):
        if d.startswith(k):
            return k
    return "section"


class Sources:
    def __init__(self, src):
        self.src = src
        self._files = {}
        self._loc = {}

    def lines(self, rel):
        if rel not in self._files:
            p = os.path.normpath(os.path.join(self.src, rel))
            if not p.startswith(os.path.normpath(self.src)) or not os.path.isfile(p):
                self._files[rel] = None
            else:
                with open(p, encoding="utf-8-sig", errors="replace") as f:
                    self._files[rel] = f.read().split("\n")
        return self._files[rel]

    def loc(self, name):
        if not name.endswith(".csv"):
            name += ".csv"
        if name not in self._loc:
            p = os.path.join(self.src, LOC_DIR, name)
            if not os.path.isfile(p):
                self._loc[name] = None
            else:
                with open(p, encoding="utf-8-sig", newline="") as f:
                    self._loc[name] = {(r.get("Key") or "").strip(): r.get("English") or ""
                                       for r in csv.DictReader(f)}
        return name, self._loc[name]


def code_only(lines):
    """True if any line in the range has code outside comments."""
    in_block = False
    for line in lines:
        s = line
        while s:
            if in_block:
                end = s.find("*/")
                if end < 0:
                    s = ""
                    break
                s, in_block = s[end + 2:], False
                continue
            start = s.find("/*")
            sl = s.find("//")
            if sl >= 0 and (start < 0 or sl < start):
                s = s[:sl]
                if s.strip():
                    return True
                break
            if start >= 0:
                if s[:start].strip():
                    return True
                s, in_block = s[start + 2:], True
                continue
            t = s.strip()
            if t and t not in ("{", "}", "};") and not re.match(r"(using|namespace)\b", t):
                return True
            break
    return False


def check_src(sources, val):
    m = re.match(r"^(.+?):(\d+)(?:-(\d+))?$", val)
    if not m:
        return None, f"[src: {val}] needs PATH:LINE or PATH:FIRST-LAST"
    rel, a, b = m[1].strip(), int(m[2]), int(m[3] or m[2])
    lines = sources.lines(rel)
    if lines is None:
        return None, f"[src: {val}]: no file {rel} in the release checkout"
    if not (1 <= a <= b <= len(lines)):
        return None, f"[src: {val}]: lines {a}-{b} outside the file ({len(lines)} lines)"
    if b - a > 80:
        return None, f"[src: {val}]: range over 80 lines; cite the lines that show the claim"
    chunk = lines[a - 1:b]
    if not code_only(chunk):
        return None, (f"[src: {val}]: only comments, braces, blank, using or namespace lines there "
                      "(commented-out code does not run)")
    return "\n".join(f"      {rel}:{a + i}  {l.rstrip()}" for i, l in enumerate(chunk)), None


def check_loc(sources, val):
    if ":" not in val:
        return None, f"[loc: {val}] needs FILE.csv:KEY"
    name, key = val.rsplit(":", 1)
    name, table = sources.loc(name.strip())
    if table is None:
        return None, f"[loc: {val}]: no {name} in {LOC_DIR}"
    key = key.strip()
    if key not in table:
        return None, f"[loc: {val}]: no key {key} in {name}"
    return f"      {name}:{key}  {' '.join(table[key].split())[:400]}", None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("ledger")
    ap.add_argument("--src", required=True, help="the release checkout ($WORK/src)")
    ap.add_argument("--page", default="wiki/Terror_from_the_Void.wiki",
                    help="page text that [page: …] quotes must be found in (more than one: comma-separated)")
    ap.add_argument("--state", default="wiki/STATE.json")
    ap.add_argument("--sample", type=int, default=0)
    ap.add_argument("--show", action="store_true")
    a = ap.parse_args()

    meta = os.path.join(a.src, "Dist", "meta.json")
    if os.path.isfile(meta) and os.path.isfile(a.state):
        ver = json.load(open(meta, encoding="utf-8-sig")).get("Version")
        want = json.load(open(a.state, encoding="utf-8")).get("target_version")
        if want and ver != want:
            sys.exit(f"--src is version {ver}, but {a.state} targets {want}: check out the release tag.")

    page = ""
    for p in a.page.split(","):
        if os.path.isfile(p.strip()):
            page += open(p.strip(), encoding="utf-8").read()
    page_norm = " ".join(page.split())
    sources = Sources(a.src)

    counts = collections.Counter()
    errors, warnings, open_items, unparsed, done_lines = [], [], [], [], []
    for n, raw in enumerate(open(a.ledger, encoding="utf-8"), 1):
        m = LINE.match(raw.rstrip("\n"))
        if not m:
            continue
        done, body = m[1].lower() == "x", m[2]
        disp, item = disposition(body)
        if disp is None:
            unparsed.append((n, body))
            continue
        kind = kind_of(disp)
        counts[("done " if done else "open ") + kind] += 1
        if not done:
            open_items.append((n, body))
            continue
        cites = parse_cites(body)
        problems, excerpts = [], []
        for v in cites["src"]:
            ex, err = check_src(sources, v)
            problems += [err] if err else []
            excerpts += [ex] if ex else []
        for v in cites["loc"]:
            ex, err = check_loc(sources, v)
            problems += [err] if err else []
            excerpts += [ex] if ex else []
        evidence = bool(cites["src"] or cites["loc"])
        if kind == "section" and not evidence:
            problems.append("changes the page but cites no [src:] or [loc:]")
        if kind == "already":
            if not evidence:
                problems.append("`already` needs [src:] or [loc:] showing the page is still right")
            if not cites["page"]:
                problems.append("`already` needs a [page: \"…\"] quote")
            for q in cites["page"]:
                qn = " ".join(q.strip().strip('"“”').split())
                if qn and qn not in page_norm:
                    problems.append(f"[page: {q}] not found in the page")
        if kind == "removed-in-code" and not cites["searched"]:
            problems.append("`removed-in-code` needs [searched: …] saying what was looked for and where")
        if kind == "superseded" and not re.search(r"superseded:\s*\S", disp, re.I):
            problems.append("`superseded:` must name the release that changed it again")
        if kind == "vanilla-fix" and not evidence:
            warnings.append((n, item[:100]))
        for p in problems:
            errors.append((n, p, item[:100]))
        if a.show or (excerpts and not problems):
            done_lines.append((n, item, disp, excerpts, cites["page"]))

    print(f"Ledger {a.ledger}, checked against {a.src}")
    for k in sorted(counts):
        print(f"  {counts[k]:5d}  {k}")
    if unparsed:
        print(f"\n{len(unparsed)} item lines without a '→ disposition' (give each one):")
        for n, b in unparsed[:30]:
            print(f"  line {n}: {b[:110]}")
    if open_items:
        print(f"\n{len(open_items)} open items ([ ]):")
        for n, b in open_items[:30]:
            print(f"  line {n}: {b[:110]}")
        if len(open_items) > 30:
            print(f"  … {len(open_items) - 30} more")
    if warnings:
        print(f"\n{len(warnings)} vanilla fixes without a citation (allowed, but cite where you can):")
        for n, b in warnings[:15]:
            print(f"  line {n}: {b}")
    if errors:
        print(f"\n{len(errors)} PROBLEMS in done lines:")
        for n, p, b in errors:
            print(f"  line {n}: {p}\n           ({b})")

    shown = done_lines if a.show else random.sample(done_lines, min(a.sample, len(done_lines)))
    if shown:
        print(f"\n{'All citations' if a.show else f'Spot check: {len(shown)} random cited lines'}"
              " — does each citation say what the line claims?")
        for n, item, disp, excerpts, quotes in sorted(shown):
            print(f"\n  line {n}: {item[:140]}\n    → {disp}")
            for q in quotes:
                print(f"      page: {q}")
            for ex in excerpts:
                print(ex)

    print("\nRESULT:", "FAIL" if errors or unparsed else "OK",
          f"({len(errors)} problems, {len(unparsed)} lines without a disposition, {len(open_items)} open)")
    return 1 if errors or unparsed else 0


if __name__ == "__main__":
    sys.exit(main())
