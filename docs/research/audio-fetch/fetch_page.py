"""Fetch pages into a fresh dir and print status + license-ish lines. Usage: fetch_page.py OUTDIR URL [URL...]"""
import os
import re
import sys
import urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36"


def main():
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    for i, url in enumerate(sys.argv[2:]):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": UA})
            with urllib.request.urlopen(req, timeout=30) as r:
                data = r.read()
                code = r.status
        except Exception as e:  # noqa
            print(f"ERR {url} {e}")
            continue
        name = re.sub(r"[^A-Za-z0-9]+", "_", url)[-60:] + ".html"
        path = os.path.join(out, name)
        with open(path, "wb") as f:
            f.write(data)
        txt = data.decode("utf-8", "replace")
        print(f"OK {code} {len(data)} {url} -> {path}")
        for m in re.finditer(r"(?i)(license|licence|creativecommons\.org/[^\"' <]+|href=\"[^\"]+\.(?:wav|mp3|ogg|flac)[^\"]*\")", txt):
            s = max(0, m.start() - 80)
            print("   ", txt[s:m.end() + 120].replace("\n", " "))
            break
        # list downloadable audio links
        for m in re.finditer(r"href=\"([^\"]+\.(?:wav|mp3|ogg|flac)[^\"]*)\"", txt):
            print("   FILE", m.group(1))


main()
