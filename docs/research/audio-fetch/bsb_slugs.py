"""List sound page slugs on bigsoundbank for search terms. Usage: bsb_slugs.py TERM [TERM...]"""
import re
import sys
import urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36"
for term in sys.argv[1:]:
    try:
        t = urllib.request.urlopen(
            urllib.request.Request("https://bigsoundbank.com/search?q=" + term, headers={"User-Agent": UA}), timeout=30
        ).read().decode("utf-8", "replace")
    except Exception as e:  # noqa
        print(term, "ERR", e)
        continue
    hs = sorted(set(re.findall(r"href=['\"]([^'\"]*-s\d{4}\.html)", t)))
    print(term, len(hs))
    for h in hs:
        print("  ", h)
