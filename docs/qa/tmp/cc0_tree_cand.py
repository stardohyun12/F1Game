# 후보 미리보기 시트 (최종 아님) → art/source/photo-picks/T/_cand/cand.png
import json, io, os, zipfile, urllib.request as U, urllib.parse as P, time
from PIL import Image, ImageDraw, ImageFont
H = {"User-Agent": "F1Game-artlive/1.0 (stardohyun11@gmail.com)"}
C = [("c:Camellia sasanqua at Koishikawa Kourakuen IMG 3875.jpg"), ("c:Camellia sasanqua at Koishikawa Kourakuen IMG 3877.jpg"), ("c:-365 oliveBranch (22989348482).jpg"),
     ("c:Celtis australis trunkandLeaves 2009November01 DehesaBoyaldePuertollano.jpg"), ("c:Zelkova serrata Autumn.jpg"), ("c:Adi çitlembik-yaprak.JPG"), ("c:Zelkova carpinifolia (Jardin des Plantes de Paris) - Leaves.jpg"),
     ("c:Cupressaceae branch, cones and seeds.jpg"), ("c:False cypress foliage (416394e9-155d-451f-67ff-f165b7c25e60).jpg"), ("c:False cypress foliage (513d0434-155d-451f-6707-c6ddfc2385fa).jpg"),
     ("c:20180404Pinus nigra3.jpg"), ("c:20180404Pinus nigra4.jpg"), ("c:Pinus pinea needles 2009-11-22 DehesaBoyaldePuertollano.jpg"),
     ("ag:Bark004"), ("ag:Bark005"), ("ag:Bark009"), ("ag:Bark002")]
def js(u): return json.load(U.urlopen(U.Request(u, headers=H), timeout=60))
def raw(u):
    for i in range(3):
        try: return U.urlopen(U.Request(u, headers=H), timeout=180).read()
        except Exception as e: print("retry", e); time.sleep(4)
OUT = "D:/Github/F1Game/art/source/photo-picks/T/_cand"; os.makedirs(OUT, exist_ok=True)
ims = []
for s in C:
    tag, k = s.split(":", 1)
    try:
        if tag == "c":
            q = {"action": "query", "prop": "imageinfo", "iiprop": "url|extmetadata", "iiurlwidth": 400, "format": "json", "titles": "File:" + k}
            ii = list(js("https://commons.wikimedia.org/w/api.php?" + P.urlencode(q))["query"]["pages"].values())[0]["imageinfo"][0]
            lic = ii["extmetadata"]["LicenseShortName"]["value"]; im = Image.open(io.BytesIO(raw(ii["thumburl"]))).convert("RGB")
        else:
            r = js("https://ambientcg.com/api/v2/full_json?type=Material&include=downloadData&id=" + k)["foundAssets"][0]
            dl = [d for d in r["downloads"] if d["attribute"] == "1K-JPG"][0]["downloadLink"]; lic = "ambientCG"
            z = zipfile.ZipFile(io.BytesIO(raw(dl))); nm = [n for n in z.namelist() if n.lower().endswith("_color.jpg")][0]
            im = Image.open(io.BytesIO(z.read(nm))).convert("RGB")
    except Exception as e: print("FAIL", s, e); continue
    im.thumbnail((400, 400)); ims.append((k[:45], lic[:10], im)); print(k, lic)
cw = 400; cols = 4; rows = (len(ims) + cols - 1) // cols
sh = Image.new("RGB", (cw * cols, (cw + 20) * rows), (24, 24, 24)); d = ImageDraw.Draw(sh); f = ImageFont.load_default()
for n, (k, lic, im) in enumerate(ims):
    x, y = (n % cols) * cw, (n // cols) * (cw + 20); sh.paste(im, (x, y)); d.text((x + 3, y + cw + 4), f"{n} {k} {lic}", fill=(255, 220, 90), font=f)
sh.save(OUT + "/cand.png")
