"""Steam 공식 스토어 스크린샷 수집 (참고용). 사용: python -I fetch.py <outdir>
앱 ID는 storesearch 로 찾고, appdetails 의 screenshots[].path_full 을 받는다. 게임당 최대 N장."""
import sys, json, os, urllib.request, urllib.parse

OUT = sys.argv[1]
N = int(os.environ.get("N", 6))
GAMES = {
    "shorthike": 1055540,
    "liligator": None, "jalopy": None, "toem": None, "mailtime": None,
    "wheelworld": None, "deathstranding": None, "webfishing": None, "dredge": 1562430,
}
TERMS = {
    "liligator": "Lil Gator Game", "jalopy": "Jalopy", "toem": "TOEM",
    "mailtime": "Mail Time", "wheelworld": "Wheel World",
    "deathstranding": "DEATH STRANDING DIRECTOR'S CUT", "webfishing": "WEBFISHING",
}

def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    return urllib.request.urlopen(req, timeout=30).read()

def find_id(term):
    u = "https://store.steampowered.com/api/storesearch/?" + urllib.parse.urlencode({"term": term, "cc": "us", "l": "english"})
    items = json.loads(get(u)).get("items", [])
    return [(i["id"], i["name"]) for i in items[:3]]

meta = {}
if len(sys.argv) > 2:  # 재실행: name:id name:id ...
    GAMES = {a.split(":")[0]: int(a.split(":")[1]) for a in sys.argv[2:]}
    if any(len(a.split(":")) > 2 for a in sys.argv[2:]):  # name:id:N
        N = int([a for a in sys.argv[2:] if len(a.split(":")) > 2][0].split(":")[2])
    TERMS = {}
for g, term in TERMS.items():
    c = find_id(term)
    print(g, c)
    GAMES[g] = c[0][0] if c else None

for g, aid in GAMES.items():
    if not aid:
        continue
    d = json.loads(get(f"https://store.steampowered.com/api/appdetails?appids={aid}&l=english"))[str(aid)]["data"]
    shots = d.get("screenshots", [])
    os.makedirs(os.path.join(OUT, g), exist_ok=True)
    meta[g] = {"appid": aid, "name": d["name"], "dev": d.get("developers"), "total": len(shots), "files": []}
    for s in shots[:N]:
        url = s["path_full"].split("?")[0]
        fn = os.path.join(OUT, g, f"{s['id']:02d}.jpg")
        try:
            open(fn, "wb").write(get(url))
            meta[g]["files"].append({"file": f"{g}/{s['id']:02d}.jpg", "url": url})
        except Exception as e:
            print("FAIL", g, url, e)
    print(g, aid, d["name"], len(shots))
json.dump(meta, open(os.path.join(OUT, "_meta.json"), "w"), ensure_ascii=False, indent=1)
