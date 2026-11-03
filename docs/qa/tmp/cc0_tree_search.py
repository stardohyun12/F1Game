# artlive 20261031-0142 — 나무 후보 검색. python -X utf8 -I docs/qa/tmp/cc0_tree_search.py
import json, re, urllib.request, urllib.parse
UA = {"User-Agent": "F1Game-artlive/1.0 (stardohyun11@gmail.com)"}
API = "https://commons.wikimedia.org/w/api.php"
def get(params):
    q = API + "?" + urllib.parse.urlencode(params | {"format": "json"})
    return json.load(urllib.request.urlopen(urllib.request.Request(q, headers=UA), timeout=30))
Q = {"pine_leaf": ["Pinus thunbergii needles", "Japanese black pine needles", "pine needles close up", "Pinus thunbergii"],
     "pine_bark": ["Pinus thunbergii bark", "pine bark", "Japanese black pine bark"],
     "celtis_leaf": ["Celtis sinensis leaves", "Celtis sinensis", "Celtis leaves", "hackberry leaves", "Zelkova serrata leaves"],
     "celtis_bark": ["Celtis sinensis bark", "hackberry bark", "Zelkova bark"],
     "camellia_leaf": ["Camellia japonica leaves", "Camellia japonica foliage", "Olea europaea leaves", "olive leaves"],
     "camellia_bark": ["Camellia japonica bark", "olive tree bark", "Olea europaea bark"],
     "cypress_leaf": ["Cupressus sempervirens foliage", "Cupressus sempervirens leaves", "cypress foliage close up"],
     "cypress_bark": ["Cupressus sempervirens bark", "cypress bark", "cedar bark fibrous"]}
OKL = ("CC0", "PUBLIC DOMAIN", "PD")
out = {}
for k, qs in Q.items():
    hits = {}
    for q in qs:
        try:
            r = get({"action": "query", "generator": "search", "gsrsearch": q + " filetype:bitmap", "gsrnamespace": 6, "gsrlimit": 40,
                     "prop": "imageinfo", "iiprop": "url|size|extmetadata", "iiurlwidth": 1600})
        except Exception as e:
            print("ERR", q, e); continue
        for p in (r.get("query", {}).get("pages", {}) or {}).values():
            ii = p["imageinfo"][0]; md = ii.get("extmetadata", {})
            lic = md.get("LicenseShortName", {}).get("value", "")
            if not any(lic.upper().startswith(o) or "CC0" in lic.upper() for o in OKL): continue
            if ii["width"] < 1000: continue
            art = re.sub(r"<[^>]+>", "", md.get("Artist", {}).get("value", ""))[:60]
            hits[p["title"]] = f'{lic} | {ii["width"]}x{ii["height"]} | {art}'
    out[k] = hits
    print("==", k)
    for t, v in hits.items(): print("  ", t, "|", v)
