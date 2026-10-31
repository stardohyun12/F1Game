"""Download files into OUTDIR (fresh dir) and print size + sha256. Usage: download.py OUTDIR URL [URL...]"""
import hashlib
import os
import sys
import urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36"
out = sys.argv[1]
os.makedirs(out, exist_ok=True)
for url in sys.argv[2:]:
    name = url.rstrip("/").split("/")[-1]
    # keep source id in name so same-numbered files from different folders do not collide
    path = os.path.join(out, name)
    req = urllib.request.Request(url, headers={"User-Agent": UA, "Referer": "https://bigsoundbank.com/"})
    with urllib.request.urlopen(req, timeout=120) as r:
        data = r.read()
        ctype = r.headers.get("Content-Type")
    with open(path, "wb") as f:
        f.write(data)
    print("OK", url, len(data), ctype, hashlib.sha256(data).hexdigest(), path)
