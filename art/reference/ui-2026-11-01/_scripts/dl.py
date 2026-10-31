"""사용: python -I dl.py <out_path> <url> [<out_path> <url> ...]"""
import sys, urllib.request, os
a = sys.argv[1:]
for p, u in zip(a[::2], a[1::2]):
    os.makedirs(os.path.dirname(p), exist_ok=True)
    open(p, "wb").write(urllib.request.urlopen(urllib.request.Request(u, headers={"User-Agent": "Mozilla/5.0"}), timeout=30).read())
    print(p, os.path.getsize(p))
