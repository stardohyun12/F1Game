"""Summarise bigsoundbank sound pages (title, description, duration, author, visible license line, audio URLs).
Usage: bsb_info.py SLUG.html [SLUG.html ...]   (slug like gulls-on-the-harbor-s2573.html)"""
import html
import json
import re
import sys
import urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36"


def clean(s):
    return re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", s))).strip()


for slug in sys.argv[1:]:
    url = "https://bigsoundbank.com/" + slug
    try:
        t = urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": UA}), timeout=30).read().decode("utf-8", "replace")
    except Exception as e:  # noqa
        print("ERR", url, e)
        continue
    print("=" * 8, url)
    h1 = re.search(r"<h1[^>]*>(.*?)</h1>", t, re.S)
    print("H1:", clean(h1.group(1)) if h1 else "?")
    lic = re.search(r"License</h2>\s*<p[^>]*>(.*?)</p>", t, re.S)
    print("LICENSE:", clean(lic.group(1))[:140] if lic else "NOT FOUND")
    for m in re.finditer(r'<script type="application/ld\+json">(.*?)</script>', t, re.S):
        try:
            d = json.loads(m.group(1))
        except Exception:
            continue
        if isinstance(d, dict) and d.get("@type") in ("AudioObject", "MusicRecording", "CreativeWork") or (isinstance(d, dict) and "contentUrl" in json.dumps(d)):
            for k in ("name", "description", "duration", "license", "author", "dateCreated", "datePublished"):
                if k in d:
                    print(f"  {k}:", str(d[k])[:400])
            for a in d.get("encoding", []) if isinstance(d.get("encoding"), list) else []:
                print("  enc:", a.get("encodingFormat"), a.get("contentUrl"))
            if "contentUrl" in d:
                print("  contentUrl:", d["contentUrl"])
    # page text description + duration
    for m in re.finditer(r"(?i)(duration|recorded by|recording|microphone|recorder)[^<]{0,160}", t):
        print("  ~", clean(m.group(0))[:200])
        break
    print("  AUDIO:", sorted(set(re.findall(r"https://bigsoundbank\.com/UPLOAD/(?:wav|ogg|mp3)/\d+\.\w+", t))))
