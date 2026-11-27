#!/usr/bin/env python3
"""Check an edited wikitext page before it goes to the wiki.

  python3 -I wiki_check.py PAGE.wiki --baseline LIVE.wiki [--offline] [--title Terror_from_the_Void]

Local checks: unclosed tables and inline tags, malformed headings, duplicate headings, Markdown
that slipped into wikitext, page size. Unless --offline, the page is also rendered by the wiki's
own parser (action=parse; nothing is saved), which finds images that are not uploaded, links to
pages that do not exist, links to anchors on this page that do not exist, and parser errors.

With --baseline, every problem is compared with the live page: problems that were already there
are counted but not listed one by one, and headings that disappeared are listed. The exit status
is 1 when the edited page has a problem the baseline does not.
"""
import argparse
import collections
import html
import json
import re
import sys
import urllib.parse
import urllib.request

API = "https://wiki.phoenixpoint.com/api.php"
UA = "tftv-wiki-update/1.0 (wiki maintenance script)"
HEADING = re.compile(r"^(={1,6})(?!=)(.*?[^=])\1\s*$")
INLINE_TAGS = ["b", "i", "u", "s", "code", "span", "div", "small", "sub", "sup", "center",
               "nowiki", "ref", "big", "font", "table", "tr", "td"]
MAX_BYTES = 2048 * 1024  # MediaWiki's default $wgMaxArticleSize


def read(path):
    with open(path, encoding="utf-8", newline="") as f:
        return f.read()


def plain_heading(t):
    t = re.sub(r"\[\[(?:file|image):[^\]]*\]\]", "", t, flags=re.I)
    t = re.sub(r"<[^>]+>", "", t)
    t = re.sub(r"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]", r"\1", t)
    return " ".join(t.split())


def local_issues(text):
    """Return {key: [line numbers]} where key is a line-number-free description."""
    issues = collections.defaultdict(list)
    lines = text.split("\n")
    stack = []
    in_pre = False
    for n, line in enumerate(lines, 1):
        s = line.lstrip()
        if re.search(r"<(pre|nowiki)>", s):
            in_pre = True
        if re.search(r"</(pre|nowiki)>", s):
            in_pre = False
            continue
        if in_pre:
            continue
        if s.startswith("{|"):
            stack.append(n)
        elif s.startswith("|}"):
            if stack:
                stack.pop()
            else:
                issues["table closed with |} but never opened"].append(n)
        if line.startswith("=") and not HEADING.match(line.rstrip("\r")):
            issues["malformed heading (unbalanced '=')"].append(n)
        if "```" in line:
            issues["Markdown code fence (```) — use <code> or <pre>"].append(n)
        if re.search(r"\[[^\[\]\n]+\]\(https?://", line):
            issues["Markdown link [text](url) — use [url text]"].append(n)
        if re.search(r"(?<!^)(?<![*])\*\*[^*\n]+\*\*", line) and not s.startswith("*"):
            issues["Markdown bold **text** — use <b>text</b> or '''text'''"].append(n)
        # "## item" right under a list line is a nested numbered list; anywhere else it is
        # almost certainly a Markdown heading.
        prev = lines[n - 2] if n > 1 else ""
        if re.match(r"#{2,6} \S", line) and not re.match(r"[#*:;]", prev):
            issues["'## ' line not under a list item: a Markdown heading? (use ==Heading==)"].append(n)
    for n in stack:
        issues["table opened with {| but never closed"].append(n)

    # Inline tags, counted over the whole page: wikitext tables legitimately spread them over lines.
    body = re.sub(r"<nowiki>.*?</nowiki>|<pre>.*?</pre>|<!--.*?-->", "", text, flags=re.S)
    for tag in INLINE_TAGS:
        opened = len(re.findall(rf"<{tag}(?:\s[^>]*)?(?<!/)>", body, flags=re.I))
        closed = len(re.findall(rf"</{tag}\s*>", body, flags=re.I))
        if opened != closed:
            issues[f"<{tag}> opened {opened}x but closed {closed}x"].append(0)

    seen = collections.defaultdict(list)
    for n, line in enumerate(lines, 1):
        m = HEADING.match(line.rstrip("\r"))
        if m:
            seen[plain_heading(m[2]).lower()].append(n)
    for h, ns in seen.items():
        if len(ns) > 1:
            issues[f"heading {h!r} appears {len(ns)}x (links to it reach only the first)"].extend(ns)
    return issues


def heading_set(text):
    return [plain_heading(m[2]) for m in (HEADING.match(l.rstrip("\r")) for l in text.split("\n")) if m]


def post(params):
    data = urllib.parse.urlencode(dict(params, format="json", formatversion="2")).encode()
    req = urllib.request.Request(API, data=data, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=180) as r:
        return json.load(r)


def missing_files(names):
    missing = set()
    names = sorted(set(names))
    for i in range(0, len(names), 50):
        batch = names[i:i + 50]
        d = post({"action": "query", "titles": "|".join("File:" + n for n in batch), "prop": "imageinfo"})
        for p in d["query"]["pages"]:
            if p.get("missing") and not p.get("known"):
                missing.add(p["title"].split(":", 1)[1])
    return missing


def remote_issues(text, title):
    d = post({"action": "parse", "title": title, "text": text, "contentmodel": "wikitext",
              "prop": "text|links|images", "disablelimitreport": "1", "disableeditsection": "1"})
    if "error" in d:
        sys.exit(f"Wiki parse failed: {d['error']}")
    p = d["parse"]
    out = html.unescape(p["text"])
    issues = collections.defaultdict(list)
    for name in missing_files(p.get("images", [])):
        issues[f"image not on the wiki: File:{name}"].append(0)
    for link in p.get("links", []):
        if not link.get("exists") and link.get("ns") == 0:
            issues[f"link to a page that does not exist: [[{link['title']}]]"].append(0)
    ids = set(re.findall(r'\sid="([^"]+)"', out))
    me = "/" + title.replace(" ", "_")
    for href in re.findall(r'href="([^"]*#[^"]*)"', out):
        path, frag = href.split("#", 1)
        if path not in ("", me):
            continue
        frag_u = urllib.parse.unquote(frag)
        if frag_u not in ids and frag not in ids:
            issues[f"link to an anchor not on this page: #{frag_u}"].append(0)
    n_err = len(re.findall(r'class="error"', out))
    if n_err:
        issues[f"parser error messages in the rendered page: {n_err}"].append(0)
    return issues


def report(name, cur, base):
    new = {k: v for k, v in cur.items() if k not in base}
    old = [k for k in cur if k in base]
    gone = [k for k in base if k not in cur]
    print(f"\n== {name}: {len(new)} new, {len(old)} already in the live page, {len(gone)} fixed")
    for k, ns in sorted(new.items()):
        where = ", ".join(str(n) for n in ns[:8] if n) or "-"
        print(f"  NEW  {k}  (lines: {where}{' …' if len(ns) > 8 else ''})")
    for k in sorted(gone):
        print(f"  fixed  {k}")
    return len(new)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("page")
    ap.add_argument("--baseline", help="the live wikitext this edit started from")
    ap.add_argument("--title", default="Terror_from_the_Void")
    ap.add_argument("--offline", action="store_true", help="skip the checks that ask the wiki")
    a = ap.parse_args()

    text = read(a.page)
    base = read(a.baseline) if a.baseline else ""
    size = len(text.encode("utf-8"))
    print(f"{a.page}: {size} bytes, {text.count(chr(10)) + 1} lines"
          + (f" (baseline {len(base.encode('utf-8'))} bytes)" if base else ""))
    bad = 0
    if size > MAX_BYTES:
        print(f"  NEW  page is larger than the wiki's default limit of {MAX_BYTES} bytes: move content to a subpage")
        bad += 1

    bad += report("Local checks", local_issues(text), local_issues(base) if base else {})
    if not a.offline:
        bad += report("Rendered by the wiki", remote_issues(text, a.title),
                      remote_issues(base, a.title) if base else {})

    if base:
        cur_h = collections.Counter(heading_set(text))
        removed = collections.Counter(heading_set(base)) - cur_h
        print(f"\n== Headings: {len(cur_h)} now, {sum(removed.values())} removed or renamed since the live page")
        for h in removed:
            print(f"  removed/renamed: {h}  (say why in CHANGES.md; links and anchors to it break)")
    print("\nRESULT:", "FAIL" if bad else "OK", f"({bad} new problem types)")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
