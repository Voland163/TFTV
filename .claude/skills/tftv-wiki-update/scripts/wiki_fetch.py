#!/usr/bin/env python3
"""Download the live wikitext of a page on wiki.phoenixpoint.com, plus its revision metadata.

Usage:
  python3 -I wiki_fetch.py OUT_FILE [--title Terror_from_the_Void] [--meta META_JSON]
  python3 -I wiki_fetch.py --check-revision META_JSON   # has the page changed since META_JSON?

Writes the raw wikitext byte-for-byte as the wiki stores it, and a JSON file with
revid, timestamp, user, comment and size. Standard library only.
"""
import argparse
import json
import sys
import urllib.parse
import urllib.request

API = "https://wiki.phoenixpoint.com/api.php"
UA = "tftv-wiki-update/1.0 (wiki maintenance script)"


def api_get(params):
    params = dict(params, format="json", formatversion="2")
    url = API + "?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.load(r)


def latest_revision(title):
    d = api_get({
        "action": "query", "prop": "revisions", "titles": title,
        "rvprop": "ids|timestamp|user|comment|size|content", "rvslots": "main",
    })
    page = d["query"]["pages"][0]
    if page.get("missing"):
        sys.exit(f"Page {title!r} does not exist on the wiki.")
    rev = page["revisions"][0]
    content = rev["slots"]["main"]["content"]
    meta = {k: rev.get(k) for k in ("revid", "parentid", "timestamp", "user", "comment", "size")}
    meta["title"] = page["title"]
    meta["pageid"] = page["pageid"]
    return content, meta


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out", nargs="?", help="file to write the wikitext to")
    ap.add_argument("--title", default="Terror_from_the_Void")
    ap.add_argument("--meta", help="where to write revision metadata JSON (default: OUT.meta.json)")
    ap.add_argument("--check-revision", metavar="META_JSON",
                    help="compare the live revision with a saved metadata file and exit")
    a = ap.parse_args()

    if a.check_revision:
        saved = json.load(open(a.check_revision, encoding="utf-8"))
        _, live = latest_revision(saved.get("title", a.title))
        if live["revid"] == saved["revid"]:
            print(f"UNCHANGED: live revision is still {live['revid']} ({live['timestamp']}).")
            return 0
        print(f"CHANGED: saved baseline is {saved['revid']} ({saved['timestamp']}), live is "
              f"{live['revid']} ({live['timestamp']} by {live['user']}: {live['comment']!r}).")
        print("Fetch the live page again and merge those edits before publishing.")
        return 1

    if not a.out:
        ap.error("OUT_FILE is required unless --check-revision is given")
    content, meta = latest_revision(a.title)
    # newline="" keeps the wiki's own line endings untouched.
    with open(a.out, "w", encoding="utf-8", newline="") as f:
        f.write(content)
    meta_path = a.meta or a.out + ".meta.json"
    with open(meta_path, "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=2)
    print(f"Wrote {a.out} ({len(content.encode('utf-8'))} bytes, {content.count(chr(10)) + 1} lines), "
          f"revision {meta['revid']} of {meta['timestamp']} by {meta['user']}. Metadata: {meta_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
