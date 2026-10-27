"""주인공 방 2차 재료 사진 받기 — Poly Haven(CC0) diffuse 1k 만 (과제 20261028-0406).
    python tools/asset-gen/q1_room_fetch.py            # 후보 검색 + 받기 + 대조표 contact.png
출력: art/source/q1-room-cc0/<id>.jpg · sources.json(id·url·license·author) · contact.png
"""
import json, os, sys, urllib.request
from PIL import Image, ImageDraw
D = 'art/source/q1-room-cc0/'; os.makedirs(D, exist_ok=True)
UA = {'User-Agent': 'F1Game-art2-q1room/1.0 (stardohyun11 project, CC0 texture fetch)'}
def get(u): return urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=60).read()
allx = json.loads(get('https://api.polyhaven.com/assets?t=textures'))
KEYS = dict(plaster=['plaster', 'painted_plaster', 'beige_wall'], wood=['wood_floor', 'wood_table', 'fine_grained_wood', 'plank'],
            weave=['tatami', 'rattan', 'wicker', 'woven', 'straw', 'bamboo'], fabric=['fabric', 'cotton', 'linen', 'denim'], paper=['paper', 'cardboard'])
want = sys.argv[1:]  # 지정하면 그 id 만
pick = {}
for cls, ks in KEYS.items():
    hits = [i for i in allx if any(k in i for k in ks)]
    print(cls, len(hits), hits[:14])
    for i in (hits[:4] if not want else [w for w in want if w in hits]): pick[i] = cls
src = json.load(open(D + 'sources.json', encoding='utf-8')) if os.path.exists(D + 'sources.json') else {}
for i, cls in pick.items():
    if not os.path.exists(D + i + '.jpg'):
        f = json.loads(get('https://api.polyhaven.com/files/' + i)); u = f['Diffuse']['1k']['jpg']['url']; open(D + i + '.jpg', 'wb').write(get(u))
        src[i] = dict(cls=cls, url='https://polyhaven.com/a/' + i, file_url=u, license='CC0 1.0', author=', '.join(allx[i].get('authors', {}).keys()),
                      dims_mm=allx[i].get('dimensions'))
    print('got', i, cls, src[i].get('dims_mm'))
json.dump(src, open(D + 'sources.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
ids = list(src); S = Image.new('RGB', (6 * 260, ((len(ids) + 5) // 6) * 280), (30, 30, 30)); d = ImageDraw.Draw(S)
for k, i in enumerate(ids):
    x, y = (k % 6) * 260, (k // 6) * 280; S.paste(Image.open(D + i + '.jpg').convert('RGB').resize((256, 256)), (x, y)); d.text((x + 2, y + 260), f"{i} {src[i]['cls']}", fill=(255, 255, 255))
S.save(D + 'contact.png'); print('contact', S.size)
