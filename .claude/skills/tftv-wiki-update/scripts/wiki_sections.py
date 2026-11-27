#!/usr/bin/env python3
"""Split a large wikitext page into one file per section, and join the files back.

  python3 -I wiki_sections.py split PAGE.wiki SECTIONS_DIR [--level 2]
  python3 -I wiki_sections.py join  SECTIONS_DIR PAGE.wiki
  python3 -I wiki_sections.py list  PAGE.wiki            # heading outline with line numbers

split cuts the page before every heading of level <= --level (1 is =X=, 2 is ==X==), so each
file holds a heading and everything up to the next cut, deeper headings included. Files are
named NNNN-slug.wiki with NNNN going up in steps of 100, so a new section can be inserted by
creating, say, 1250-new-section.wiki between 1200-… and 1300-…. join concatenates every
*.wiki file in name order. split then join, unedited, gives back the page byte for byte
(split checks this before it writes anything).
"""
import argparse
import os
import re
import sys

HEADING = re.compile(r"^(={1,6})(?!=)(.*?[^=])\1\s*$")


def read(path):
    with open(path, encoding="utf-8", newline="") as f:
        return f.read()


def headings(text):
    out = []
    for i, line in enumerate(text.splitlines(keepends=True)):
        m = HEADING.match(line.rstrip("\r\n"))
        if m:
            out.append((i, len(m[1]), m[2].strip()))
    return out


def plain(title):
    t = re.sub(r"\[\[(?:file|image):[^\]]*\]\]", "", title, flags=re.I)
    t = re.sub(r"<[^>]+>", "", t)
    t = re.sub(r"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]", r"\1", t)
    return t.strip()


def slug(title):
    s = re.sub(r"[^a-z0-9]+", "-", plain(title).lower()).strip("-")
    return (s or "section")[:50]


def split(page, out_dir, level):
    text = read(page)
    lines = text.splitlines(keepends=True)
    cuts = [(i, title) for i, lv, title in headings(text) if lv <= level]
    chunks = []
    start, name = 0, "preamble"
    for i, title in cuts:
        if i > start:
            chunks.append((name, "".join(lines[start:i])))
        start, name = i, slug(title)
    chunks.append((name, "".join(lines[start:])))
    chunks = [(n, c) for n, c in chunks if c]
    if "".join(c for _, c in chunks) != text:
        sys.exit("Internal error: the split does not reproduce the page; nothing written.")
    os.makedirs(out_dir, exist_ok=True)
    if any(f.endswith(".wiki") for f in os.listdir(out_dir)):
        sys.exit(f"{out_dir} already holds .wiki files; use an empty directory.")
    for k, (n, c) in enumerate(chunks):
        with open(os.path.join(out_dir, f"{(k + 1) * 100:04d}-{n}.wiki"), "w",
                  encoding="utf-8", newline="") as f:
            f.write(c)
    print(f"Split {page} into {len(chunks)} files in {out_dir} (cut at heading level <= {level}).")


def join(in_dir, page):
    files = sorted(f for f in os.listdir(in_dir) if f.endswith(".wiki"))
    if not files:
        sys.exit(f"No .wiki files in {in_dir}.")
    parts = []
    for f in files:
        c = read(os.path.join(in_dir, f))
        if parts and not parts[-1].endswith("\n"):
            parts[-1] += "\n"  # keep the next heading at the start of a line
        parts.append(c)
    with open(page, "w", encoding="utf-8", newline="") as f:
        f.write("".join(parts))
    print(f"Joined {len(files)} files into {page}.")


def outline(page):
    for i, lv, title in headings(read(page)):
        print(f"{i + 1:6d}  {'  ' * (lv - 1)}{'=' * lv} {plain(title)}")


def main():
    ap = argparse.ArgumentParser()
    sub = ap.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("split"); s.add_argument("page"); s.add_argument("out_dir")
    s.add_argument("--level", type=int, default=2)
    j = sub.add_parser("join"); j.add_argument("in_dir"); j.add_argument("page")
    l = sub.add_parser("list"); l.add_argument("page")
    a = ap.parse_args()
    if a.cmd == "split":
        split(a.page, a.out_dir, a.level)
    elif a.cmd == "join":
        join(a.in_dir, a.page)
    else:
        outline(a.page)


if __name__ == "__main__":
    main()
