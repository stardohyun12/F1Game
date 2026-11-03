# artlive 20261030-2003 — 밭 재료 CC0 사진 후보 I1..I11 내려받기 + 번호 시트. 라이선스는 받을 때 파일 페이지(API extmetadata)에서 다시 확인.
# python -X utf8 -I docs/qa/tmp/cc0_get_I.py  → art/source/photo-picks/I/I<n>_<재료>.jpg · sheet_I.png · prov_I.json
import json, io, re, urllib.request as U, urllib.parse as P, os, sys
from PIL import Image, ImageDraw, ImageFont
H = {"User-Agent": "F1Game-artlive/1.0 (stardohyun11@gmail.com)"}
OUT = os.path.abspath("art/source/photo-picks/I"); os.makedirs(OUT, exist_ok=True)
PICKS = [("soil", "ph:farm_soil"), ("soil", "ph:brown_mud_dry"), ("soil", "c:Plowed field drying after rain.jpg"),
         ("cabbage", "c:Chinese cabbages.jpg"), ("cabbage", "c:Napa cabbages.png"), ("cabbage", "c:Cabbage plant picture and images.jpg"),
         ("onion", "c:Allium fistulosum - Copenhagen Botanical Garden - DSC07943.JPG"), ("onion", "c:Green onions sold in supermarkets.jpg"),
         ("fence", "ph:weathered_planks"), ("fence", "ph:wood_planks_grey"), ("fence", "cs:Wooden fence of the hotel")]
def js(url): return json.load(U.urlopen(U.Request(url, headers=H), timeout=60))
def raw(url): return U.urlopen(U.Request(url, headers=H), timeout=120).read()
def strip(s): return re.sub(r"<[^>]+>", "", s or "").strip()
prov = []
for i, (kind, src) in enumerate(PICKS, 1):
    tag, key = src.split(":", 1)
    if tag == "ph":
        f = js("https://api.polyhaven.com/files/" + key); url = f["Diffuse"]["2k"]["jpg"]["url"]
        info = js("https://api.polyhaven.com/info/" + key)
        rec = {"src": "Poly Haven", "page": "https://polyhaven.com/a/" + key, "lic": "CC0 (Poly Haven 전 자산 CC0, https://polyhaven.com/license)", "author": ", ".join(info.get("authors", {}).keys())}
    else:
        q = {"action": "query", "prop": "imageinfo", "iiprop": "url|size|extmetadata", "iiurlwidth": 1600, "format": "json"}
        if tag == "cs": q |= {"generator": "search", "gsrsearch": key + " filetype:bitmap", "gsrnamespace": 6, "gsrlimit": 1}
        else: q |= {"titles": "File:" + key}
        pg = list(js("https://commons.wikimedia.org/w/api.php?" + P.urlencode(q))["query"]["pages"].values())[0]
        ii = pg["imageinfo"][0]; md = ii["extmetadata"]; lic = md.get("LicenseShortName", {}).get("value", "")
        if "CC0" not in lic.upper(): print("SKIP not CC0", key, lic); continue
        url = ii["thumburl"]
        rec = {"src": "Wikimedia Commons", "page": ii["descriptionurl"], "lic": lic + " (" + strip(md.get("UsageTerms", {}).get("value", "")) + ")", "author": strip(md.get("Artist", {}).get("value", ""))[:80]}
    im = Image.open(io.BytesIO(raw(url))).convert("RGB"); im.thumbnail((1600, 1600))
    fn = f"I{i}_{kind}.jpg"; im.save(os.path.join(OUT, fn), quality=92)
    prov.append({"id": f"I{i}", "file": fn, "kind": kind, "size": f"{im.width}x{im.height}", **rec}); print(f"I{i}", fn, im.size, rec["lic"][:40], rec["page"])
json.dump(prov, open(os.path.join(OUT, "prov_I.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
# 시트: 4열, 칸 400x300, 번호·재료 라벨
cw, ch, cols = 400, 300, 4; rows = (len(prov) + cols - 1) // cols
sh = Image.new("RGB", (cw * cols, (ch + 34) * rows), (24, 24, 24)); d = ImageDraw.Draw(sh)
try: fnt = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 22)
except Exception: fnt = ImageFont.load_default()
for n, r in enumerate(prov):
    im = Image.open(os.path.join(OUT, r["file"])); im.thumbnail((cw - 8, ch - 8)); x, y = (n % cols) * cw, (n // cols) * (ch + 34)
    sh.paste(im, (x + (cw - im.width) // 2, y + 34 + (ch - im.height) // 2)); d.text((x + 8, y + 5), f'{r["id"]}  {r["kind"]}  ({r["src"].split()[0]})', fill=(255, 220, 90), font=fnt)
sh.save(os.path.join(OUT, "sheet_I.png")); print("sheet", sh.size)
