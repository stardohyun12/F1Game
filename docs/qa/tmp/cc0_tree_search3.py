import json, re, urllib.request, urllib.parse
UA = {"User-Agent": "F1Game-artlive/1.0 (stardohyun11@gmail.com)"}
def js(u): return json.load(urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=40))
API = "https://commons.wikimedia.org/w/api.php"
Q = {"camellia_leaf": ["Camellia japonica leaves", "Camellia sasanqua", "camellia hedge", "Camellia leaf", "camellia foliage", "Camellia japonica shrub", "glossy evergreen leaves", "Olea europaea branch", "olive branch leaves", "Olea europaea foliage"],
     "olive_bark": ["olive tree bark", "Olea europaea bark", "olive trunk bark texture", "Camellia japonica trunk", "Camellia stem bark"],
     "celtis_leaf": ["Celtis sinensis foliage", "Celtis leaves canopy", "Zelkova serrata foliage", "Zelkova leaves", "hackberry foliage", "Celtis australis leaves"],
     "cypress_leaf": ["Cupressus sempervirens foliage close", "Cupressus sempervirens branch", "Cupressus foliage", "cypress foliage", "Cupressus macrocarpa foliage", "Cupressus arizonica foliage"],
     "pine_leaf": ["Pinus thunbergii leaves", "Pinus thunbergii needles", "Pinus densiflora needles", "Pinus pinea needles", "Pinus nigra foliage", "Pinus pinaster needles"]}
for k, qs in Q.items():
    hits = {}
    for q in qs:
        try:
            r = js(API + "?" + urllib.parse.urlencode({"action": "query", "generator": "search", "gsrsearch": q + " filetype:bitmap", "gsrnamespace": 6, "gsrlimit": 50, "prop": "imageinfo", "iiprop": "size|extmetadata", "format": "json"}))
        except Exception as e: print("ERR", q, e); continue
        for p in (r.get("query", {}).get("pages", {}) or {}).values():
            ii = p["imageinfo"][0]; lic = ii.get("extmetadata", {}).get("LicenseShortName", {}).get("value", "")
            if not (lic.upper().startswith("PUBLIC DOMAIN") or "CC0" in lic.upper()): continue
            if ii["width"] < 1000 or "Poly Haven" in p["title"] or "MET" in p["title"] or "NYPL" in p["title"]: continue
            hits[p["title"][5:90]] = f'{lic} {ii["width"]}x{ii["height"]}'
    print("==", k, flush=True)
    for t, v in hits.items(): print("  ", t, "|", v, flush=True)
