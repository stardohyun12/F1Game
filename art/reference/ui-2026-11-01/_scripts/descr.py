"""Steam about_the_game 에서 UI·HUD 관련 문장만 뽑는다 (제작자 설명). 사용: python -I descr.py"""
import json, re, urllib.request, html

IDS = {"shorthike": 1055540, "liligator": 1586800, "jalopy": 446020, "toem": 1307580, "mailtime": 1607240,
       "wheelworld": 1497460, "deathstranding": 1850570, "webfishing": 3146520, "dredge": 1562430, "unpacking": 1135690,
       "edc": 3293010}
KEY = re.compile(r"\b(UI|HUD|interface|menu|map|compass|dashboard|gauge|dialog|dialogue|text|speech|talk|pixel|resolution|retro|PS1|PlayStation|notebook|journal|diary|radio|letter|package|order|deliver)\w*", re.I)

def get(u):
    return urllib.request.urlopen(urllib.request.Request(u, headers={"User-Agent": "Mozilla/5.0"}), timeout=30).read()

for g, i in IDS.items():
    d = json.loads(get(f"https://store.steampowered.com/api/appdetails?appids={i}&l=english"))[str(i)]["data"]
    t = html.unescape(re.sub(r"<[^>]+>", " ", d.get("about_the_game", "") + " " + d.get("short_description", "")))
    t = re.sub(r"\s+", " ", t)
    sents = [s.strip() for s in re.split(r"(?<=[.!?])\s+", t) if KEY.search(s)]
    print("==", g, i, d["name"], "| dev:", d.get("developers"))
    for s in sents[:7]:
        print("  -", s[:260])
