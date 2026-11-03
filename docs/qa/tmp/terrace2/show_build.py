import json, sys
s = open(sys.argv[1], encoding="utf-8").read()
print(s[:120])
b = json.loads(s[s.index("{"):])["build"]
print("ms", b["ms"]); print("terrain", b["terrain"])
print("walls", json.dumps(b["walls"], ensure_ascii=False))
print("ledges", json.dumps(b["ledges"], ensure_ascii=False))
print("path", b["path"]); print("skipped", b["skipped"])
