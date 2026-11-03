# artlive 20261031-0142 — 2차: Poly Haven/ambientCG 껍질 + Commons 보충 검색
import json, re, urllib.request, urllib.parse
UA = {"User-Agent": "F1Game-artlive/1.0 (stardohyun11@gmail.com)"}
def js(u): return json.load(urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=40))
d = js("https://api.polyhaven.com/assets?t=textures")
print("POLYHAVEN bark-ish:")
for k, v in d.items():
    s = (k + " " + " ".join(v.get("categories", [])) + " " + " ".join(v.get("tags", []))).lower()
    if any(w in s for w in ("bark", "tree", "trunk", "pine", "cypress", "olive")): print("  ", k, v.get("name"), v.get("categories"))
print("AMBIENTCG:")
for q in ("bark", "tree"):
    try:
        r = js("https://ambientcg.com/api/v2/full_json?type=Material&limit=40&q=" + q)
        for a in r.get("foundAssets", []): print("  ", a["assetId"], a.get("displayName"), a.get("tags", [])[:6])
    except Exception as e: print("ERR ambientcg", e)
API = "https://commons.wikimedia.org/w/api.php"
def get(params):
    return js(API + "?" + urllib.parse.urlencode(params | {"format": "json"}))
Q = {"cypress_leaf": ["Cupressus sempervirens", "Italian cypress", "Mediterranean cypress", "Cupressus foliage", "cypress branch scale leaves", "Thuja foliage", "Cupressus"],
     "cypress_bark": ["Cupressus bark", "Cupressus sempervirens bark", "redwood bark", "cedar bark", "Juniperus bark"],
     "camellia_leaf": ["Camellia", "Camellia japonica", "Olea europaea", "olive tree branch", "olive tree leaves", "evergreen glossy leaves"],
     "camellia_bark": ["olive tree trunk", "Olea europaea trunk", "Camellia bark", "smooth bark", "olive trunk"],
     "pine_leaf": ["Pinus thunbergii foliage", "Pinus thunbergii branch", "pine branch needles", "black pine needles", "Pinus nigra needles", "pine needles"],
     "pine_bark": ["Pinus thunbergii trunk", "Pinus nigra bark", "pine trunk bark", "Pinus pinaster bark"],
     "celtis_bark": ["Celtis bark", "Celtis sinensis trunk", "Celtis australis bark", "hackberry trunk", "Celtis occidentalis bark"],
     "celtis_leaf": ["Celtis australis leaves", "Celtis australis"]}
for k, qs in Q.items():
    hits = {}
    for q in qs:
        try:
            r = get({"action": "query", "generator": "search", "gsrsearch": q + " filetype:bitmap", "gsrnamespace": 6, "gsrlimit": 50,
                     "prop": "imageinfo", "iiprop": "url|size|extmetadata", "iiurlwidth": 1600})
        except Exception as e: print("ERR", q, e); continue
        for p in (r.get("query", {}).get("pages", {}) or {}).values():
            ii = p["imageinfo"][0]; md = ii.get("extmetadata", {}); lic = md.get("LicenseShortName", {}).get("value", "")
            if not (lic.upper().startswith("PUBLIC DOMAIN") or "CC0" in lic.upper() or lic.upper() == "PD"): continue
            if ii["width"] < 1000 or "Poly Haven" in p["title"]: continue
            hits[p["title"]] = f'{lic} | {ii["width"]}x{ii["height"]}'
    print("==", k)
    for t, v in hits.items(): print("  ", t, "|", v)
