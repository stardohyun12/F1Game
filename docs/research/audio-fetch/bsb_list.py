"""List sound pages on bigsoundbank for a search term. Usage: bsb_list.py TERM [URL_OVERRIDE]"""
import html
import re
import sys
import urllib.parse
import urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36"
term = sys.argv[1]
url = sys.argv[2] if len(sys.argv) > 2 else "https://bigsoundbank.com/search?q=" + urllib.parse.quote(term)
req = urllib.request.Request(url, headers={"User-Agent": UA})
with urllib.request.urlopen(req, timeout=30) as r:
    txt = r.read().decode("utf-8", "replace")
print("URL", url, len(txt))
seen = set()
for m in re.finditer(r"<a[^>]+href=['\"]([^'\"]*sound-\d+[^'\"]*\.html)['\"][^>]*>(.*?)</a>", txt, re.S):
    href, label = m.group(1), re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", m.group(2)))).strip()
    if href in seen:
        continue
    seen.add(href)
    print(href, "|", label[:90])
if not seen:
    print(txt[:1500])
