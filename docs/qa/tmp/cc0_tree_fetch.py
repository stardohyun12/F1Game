# artlive 20261031-0142 — 나무 후보 16장 내려받기 + prov_T.json + sheet_T.png. python -X utf8 -I docs/qa/tmp/cc0_tree_fetch.py
import json, io, re, os, time, urllib.request as U, urllib.parse as P
from PIL import Image, ImageDraw, ImageFont
H = {"User-Agent": "F1Game-artlive/1.0 (stardohyun11@gmail.com)"}
OUT = "D:/Github/F1Game/art/source/photo-picks/T"; os.makedirs(OUT, exist_ok=True)
PICKS = {
 "pine_A_leaf": "c:Close-up of pine-needle 85.JPG", "pine_A_bark": "ph:knotted_pine_bark",
 "pine_B_leaf": "c:Pine needles close-up (Unsplash).jpg", "pine_B_bark": "ph:pine_bark",
 "celtis_A_leaf": "c:Celtis sinensis Beor St Creek 7th Brigade Park Chermside L1020887.jpg", "celtis_A_bark": "ph:chinese_hackberry_bark",
 "celtis_B_leaf": "c:Celtis sinensis Beor St Creek 7th Brigade Park Chermside L1020889.jpg", "celtis_B_bark": "ph:japanese_hackberry_bark",
 "camellia_A_leaf": "c:Pink formal double camellia flower among glossy leaves in a garden in Portland, Oregon.jpg", "camellia_A_bark": "ph:sakura_bark",
 "camellia_B_leaf": "c:Olea europaea FruitsandLeaves 2009December13 DehesaBoyaldePuertollano.jpg", "camellia_B_bark": "c:The epiphytic association Lecanoretum carpineae on the bark of Olea europaea.jpg",
 "cypress_A_leaf": "c:Cupressus sempervirens A.jpg", "cypress_A_bark": "ph:japanese_cedar_bark",
 "cypress_B_leaf": "c:Cupressus sempervirens B.jpg", "cypress_B_bark": "ph:chinese_cedar_bark",
}
PICKS.update({"pine_B_leaf": "c:20180404Pinus nigra3.jpg", "celtis_B_leaf": "c:Zelkova carpinifolia (Jardin des Plantes de Paris) - Leaves.jpg",
 "camellia_A_leaf": "c:Pale pink camellia bloom among dark glossy leaves in a garden in Portland, Oregon.jpg", "camellia_B_bark": "ph:bark_platanus"})
CROPS = {"camellia_A_leaf": (0, 0, 1, 0.38), "cypress_B_leaf": (0, 0.56, 1, 1)}  # 비율 크롭 (곤충·꽃 제외)
def js(u): return json.load(U.urlopen(U.Request(u, headers=H), timeout=60))
def raw(u):
    for i in range(4):
        try: return U.urlopen(U.Request(u, headers=H), timeout=120).read()
        except Exception as e: print("retry", e); time.sleep(5 * (i + 1))
    raise SystemExit("download failed " + u)
def strip(s): return re.sub(r"<[^>]+>", "", s or "").strip()
prov = []
for key, src in PICKS.items():
    tag, k = src.split(":", 1)
    sp, part, kind = key.split("_")
    fn = f"T_{sp}_{part}_{kind}.jpg"
    if tag == "ph":
        f = js("https://api.polyhaven.com/files/" + k); url = f["Diffuse"]["2k"]["jpg"]["url"]
        info = js("https://api.polyhaven.com/info/" + k)
        rec = {"source_site": "Poly Haven", "page_url": "https://polyhaven.com/a/" + k, "download_url": url,
               "license": "CC0 (polyhaven.com/license) — \"Our assets are all licensed as CC0, which is effectively Public Domain even in jurisdictions that do not support the Public Domain.\"",
               "author": ", ".join(info.get("authors", {}).keys())}
    else:
        q = {"action": "query", "prop": "imageinfo", "iiprop": "url|size|extmetadata", "iiurlwidth": 1600, "format": "json", "titles": "File:" + k}
        pg = list(js("https://commons.wikimedia.org/w/api.php?" + P.urlencode(q))["query"]["pages"].values())[0]
        ii = pg["imageinfo"][0]; md = ii["extmetadata"]; lic = md.get("LicenseShortName", {}).get("value", "")
        if not ("CC0" in lic.upper() or lic.strip().lower() == "public domain"):
            print("SKIP license", k, lic); continue
        url = ii["url"] if key in CROPS else (ii.get("thumburl") or ii["url"])
        rec = {"source_site": "Wikimedia Commons", "page_url": ii["descriptionurl"], "download_url": url,
               "license": lic + " (" + strip(md.get("UsageTerms", {}).get("value", "")) + ")", "author": strip(md.get("Artist", {}).get("value", ""))[:80]}
    im = Image.open(io.BytesIO(raw(url))).convert("RGB")
    if key in CROPS:
        l, t, r, b = CROPS[key]; W, Hh = im.size; im = im.crop((int(l * W), int(t * Hh), int(r * W), int(b * Hh)))
    if max(im.size) > 1600: im.thumbnail((1600, 1600))
    im.save(os.path.join(OUT, fn), quality=92)
    if key in CROPS: rec["note"] = f"원본에서 비율 크롭 {CROPS[key]} (꽃·곤충 제외)"
    prov.append({"file": fn, **rec, "size": f"{im.width}x{im.height}"}); print(fn, im.size, rec["license"][:30], rec["page_url"]); time.sleep(1)
json.dump(prov, open(os.path.join(OUT, "prov_T.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
# 시트
T = 256; LH = 22; sps = ["pine", "celtis", "camellia", "cypress"]; cols = [("A", "leaf"), ("A", "bark"), ("B", "leaf"), ("B", "bark")]
sh = Image.new("RGB", (T * 4, (T + LH) * 4), (24, 24, 24)); d = ImageDraw.Draw(sh)
try: fnt = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 13)
except Exception: fnt = ImageFont.load_default()
for r, sp in enumerate(sps):
    for c, (ab, kd) in enumerate(cols):
        fn = f"T_{sp}_{ab}_{kd}.jpg"; p = os.path.join(OUT, fn)
        if not os.path.exists(p): continue
        im = Image.open(p).convert("RGB"); w, h = im.size; s = min(w, h)
        im = im.crop(((w - s) // 2, (h - s) // 2, (w - s) // 2 + s, (h - s) // 2 + s)).resize((T, T), Image.LANCZOS)  # 중앙 정사각 크롭
        x, y = c * T, r * (T + LH); sh.paste(im, (x, y)); d.text((x + 4, y + T + 3), fn, fill=(255, 220, 90), font=fnt)
sh.save(os.path.join(OUT, "sheet_T.png")); print("sheet", sh.size)
