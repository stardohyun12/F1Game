# artlive 20261030-2003 — 밭 재료 CC0 사진 후보 찾기(Commons API). 파일 페이지 라이선스가 CC0 인 것만.
# python -X utf8 -I docs/qa/tmp/cc0_pick_I.py search  → 후보 목록(json) 출력
import json, sys, urllib.request, urllib.parse
UA = {"User-Agent": "F1Game-artlive/1.0 (stardohyun11@gmail.com)"}
API = "https://commons.wikimedia.org/w/api.php"
def get(params):
    q = API + "?" + urllib.parse.urlencode(params | {"format": "json"})
    return json.load(urllib.request.urlopen(urllib.request.Request(q, headers=UA), timeout=30))
QUERIES = {"soil": ["plowed soil field CC0", "tilled soil texture", "garden soil ridges"],
           "cabbage": ["napa cabbage leaves", "chinese cabbage field", "baechu"],
           "onion": ["welsh onion green", "spring onion field", "Allium fistulosum"],
           "fence": ["wooden fence weathered", "wood fence posts rural", "old wooden fence texture"]}
out = {}
for k, qs in QUERIES.items():
    hits = []
    for q in qs:
        r = get({"action": "query", "generator": "search", "gsrsearch": q + " filetype:bitmap", "gsrnamespace": 6, "gsrlimit": 30,
                 "prop": "imageinfo", "iiprop": "url|size|extmetadata", "iiurlwidth": 1600})
        for p in (r.get("query", {}).get("pages", {}) or {}).values():
            ii = p["imageinfo"][0]; md = ii.get("extmetadata", {})
            lic = md.get("LicenseShortName", {}).get("value", "")
            if lic.strip().upper() not in ("CC0", "CC0 1.0", "CC-ZERO", "PUBLIC DOMAIN DEDICATION (CC0)") and "CC0" not in lic.upper(): continue
            if ii["width"] < 1200: continue
            art = md.get("Artist", {}).get("value", "")
            hits.append({"title": p["title"], "lic": lic, "w": ii["width"], "h": ii["height"], "thumb": ii.get("thumburl"), "page": ii["descriptionurl"], "artist": art[:120]})
    seen = set(); out[k] = [h for h in hits if not (h["title"] in seen or seen.add(h["title"]))]
print(json.dumps(out, ensure_ascii=False, indent=1))
