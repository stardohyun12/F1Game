"""실제 마을 길 지도를 같은 축척으로 그린다 (OSM Overpass + Open Topo Data SRTM).
결과: docs/map/real-towns/<id>.png, sheet.png. 내부 참고용.
사용: python tools/map/real_towns.py [id ...]
"""
import json, math, os, sys, time
import requests
from PIL import Image, ImageDraw, ImageFont

OUT = 'docs/map/real-towns'
SIZE_M = 1200          # 정사각 한 변 (m)
PX = 900               # 그림 한 변 (px)
UA = {'User-Agent': 'PitCrew-map-research/0.1 (personal game dev)'}

TOWNS = [
    ('onomichi',  '오노미치 (일본) — 해안 상점가 + 언덕 골목',          34.4095, 133.1985),
    ('tomonoura', '도모노우라 (일본, 포뇨 모델) — 작은 항구 마을',      34.3840, 133.3815),
    ('hakodate',  '하코다테 모토마치 (일본) — 바다로 떨어지는 언덕길',   41.7650, 140.7125),
    ('positano',  '포지타노 (이탈리아) — 비탈 스위치백',              40.6285, 14.4855),
    ('amalfi',    '아말피 (이탈리아) — 협곡 마을 + 해안도로',          40.6345, 14.6015),
    ('vernazza',  '베르나차 (이탈리아) — 친퀘테레 항구 마을',           44.1350, 9.6835),
    ('camogli',   '카몰리 (이탈리아) — 해변 따라 늘어선 마을',          44.3500, 9.1555),
    ('visby',     '비스뷔 (스웨덴, 코리코 모델) — 성벽 안 비탈 마을',    57.6395, 18.2945),
]

def bbox(lat, lon):
    dlat = SIZE_M / 2 / 111320
    dlon = SIZE_M / 2 / (111320 * math.cos(math.radians(lat)))
    return lat - dlat, lon - dlon, lat + dlat, lon + dlon

def overpass(b):
    s, w, n, e = b
    q = f"""[out:json][timeout:60];
(way["highway"]({s},{w},{n},{e});
 way["building"]({s},{w},{n},{e});
 way["natural"="coastline"]({s},{w},{n},{e});
 way["natural"="water"]({s},{w},{n},{e});
 way["landuse"~"farmland|orchard|vineyard|grass|meadow|forest"]({s},{w},{n},{e});
 way["natural"~"wood|scrub"]({s},{w},{n},{e});
 way["railway"~"rail|tram"]({s},{w},{n},{e});
 way["man_made"~"pier|breakwater"]({s},{w},{n},{e});
);out geom;"""
    for url in ['https://overpass-api.de/api/interpreter'] * 4 + ['https://maps.mail.ru/osm/tools/overpass/api/interpreter']:
        try:
            r = requests.post(url, data={'data': q}, headers=UA, timeout=180)
            if r.ok:
                return r.json()['elements']
            print('overpass status', r.status_code, url)
        except Exception as ex:
            print('overpass fail', url, ex)
        time.sleep(15)
    raise SystemExit('overpass failed')

def elevation(b, n=25):
    s, w, nn, e = b
    pts = [(s + (nn - s) * (i + .5) / n, w + (e - w) * (j + .5) / n) for i in range(n) for j in range(n)]
    out = []
    for k in range(0, len(pts), 100):
        chunk = pts[k:k + 100]
        loc = '|'.join(f'{a:.6f},{o:.6f}' for a, o in chunk)
        for _ in range(4):
            try:
                r = requests.get('https://api.opentopodata.org/v1/srtm30m', params={'locations': loc}, headers=UA, timeout=60)
                if r.ok:
                    out += [x['elevation'] if x['elevation'] is not None else 0 for x in r.json()['results']]
                    break
            except Exception as ex:
                print('elev retry', ex)
            time.sleep(2)
        else:
            out += [0] * len(chunk)
        time.sleep(1.1)
    return [out[i * n:(i + 1) * n] for i in range(n)]   # [row from south][col from west]

ROAD = {  # (색, 폭 m)
    'primary': ((70, 60, 55), 10), 'secondary': ((70, 60, 55), 9), 'tertiary': ((90, 75, 65), 8),
    'trunk': ((70, 60, 55), 11), 'unclassified': ((110, 95, 80), 6), 'residential': ((110, 95, 80), 6),
    'living_street': ((130, 110, 90), 5), 'service': ((150, 130, 110), 3.5), 'pedestrian': ((200, 120, 60), 4),
    'track': ((160, 140, 90), 3),
}
FOOT = {'footway', 'steps', 'path', 'cycleway', 'corridor'}

def font(sz):
    for f in ('C:/Windows/Fonts/malgunbd.ttf', 'C:/Windows/Fonts/malgun.ttf'):
        if os.path.exists(f):
            return ImageFont.truetype(f, sz)
    return ImageFont.load_default()

def render(tid, title, lat, lon):
    b = bbox(lat, lon)
    cache = f'{OUT}/{tid}.json'
    if os.path.exists(cache):
        d = json.load(open(cache, encoding='utf-8'))
    else:
        d = {'osm': overpass(b), 'elev': elevation(b)}
        json.dump(d, open(cache, 'w', encoding='utf-8'))
    s, w, n, e = b
    def P(la, lo):
        return ((lo - w) / (e - w) * PX, (n - la) / (n - s) * PX)
    m2px = PX / SIZE_M
    img = Image.new('RGB', (PX, PX), (238, 232, 218))
    # 고도: 바다(≤1m) 파랑, 육지 고도 음영 + 등고선 10m
    E = d['elev']; N = len(E); cell = PX / N
    emax = max(max(r) for r in E) or 1
    small = Image.new('RGB', (N, N))
    for i in range(N):
        for j in range(N):
            z = E[i][j]
            if z <= 1:
                c = (150, 190, 210)
            else:
                t = min(1, z / max(80, emax))
                c = (int(238 - 60 * t), int(232 - 45 * t), int(218 - 70 * t))
            small.putpixel((j, N - 1 - i), c)
    img = small.resize((PX, PX), Image.Resampling.BILINEAR)
    dr = ImageDraw.Draw(img)
    # 녹지·물
    for el in d['osm']:
        tg = el.get('tags', {}); g = el.get('geometry')
        if not g: continue
        pts = [P(p['lat'], p['lon']) for p in g]
        if tg.get('natural') == 'water':
            dr.polygon(pts, fill=(150, 190, 210))
        elif tg.get('landuse') or tg.get('natural') in ('wood', 'scrub'):
            if len(pts) > 2: dr.polygon(pts, fill=(196, 206, 160))
    # 건물
    for el in d['osm']:
        tg = el.get('tags', {}); g = el.get('geometry')
        if 'building' in tg and g and len(g) > 2:
            dr.polygon([P(p['lat'], p['lon']) for p in g], fill=(196, 120, 95), outline=(150, 90, 70))
    # 해안선·부두
    for el in d['osm']:
        tg = el.get('tags', {}); g = el.get('geometry')
        if not g: continue
        pts = [P(p['lat'], p['lon']) for p in g]
        if tg.get('natural') == 'coastline':
            dr.line(pts, fill=(40, 100, 150), width=3)
        elif tg.get('man_made') in ('pier', 'breakwater'):
            dr.line(pts, fill=(120, 120, 120), width=4)
        elif tg.get('railway'):
            dr.line(pts, fill=(60, 60, 140), width=2)
    # 길: 사람 길 먼저(주황 점선), 차 길 위에
    for el in d['osm']:
        tg = el.get('tags', {}); g = el.get('geometry'); h = tg.get('highway')
        if h in FOOT and g:
            pts = [P(p['lat'], p['lon']) for p in g]
            col = (220, 90, 30) if h == 'steps' else (230, 140, 60)
            for a, c in zip(pts, pts[1:]):
                dr.line([a, c], fill=col, width=2 if h == 'steps' else 1)
    for el in d['osm']:
        tg = el.get('tags', {}); g = el.get('geometry'); h = tg.get('highway')
        if h in ROAD and g:
            col, wm = ROAD[h]
            pts = [P(p['lat'], p['lon']) for p in g]
            dr.line(pts, fill=col, width=max(2, int(wm * m2px * 1.6)), joint='curve')
    # 제목·축척
    f = font(22); fs = font(15)
    dr.rectangle([0, 0, PX, 34], fill=(255, 255, 255))
    dr.text((10, 5), title, fill=(30, 30, 30), font=f)
    bar = 200 * m2px
    dr.rectangle([12, PX - 30, 12 + bar, PX - 24], fill=(30, 30, 30))
    dr.text((16 + bar, PX - 38), f'200 m   고도 최고 {emax:.0f} m', fill=(30, 30, 30), font=fs)
    img.save(f'{OUT}/{tid}.png')
    nb = sum(1 for x in d['osm'] if 'building' in x.get('tags', {}))
    nr = sum(1 for x in d['osm'] if x.get('tags', {}).get('highway') in ROAD)
    nf = sum(1 for x in d['osm'] if x.get('tags', {}).get('highway') in FOOT)
    print(tid, 'buildings', nb, 'car ways', nr, 'foot ways', nf, 'elev max', emax)

def legend():
    L = Image.new('RGB', (PX, 60), (255, 255, 255)); dr = ImageDraw.Draw(L); fs = font(15)
    items = [((70, 60, 55), '큰길'), ((110, 95, 80), '마을 차길'), ((150, 130, 110), '좁은 차길'),
             ((220, 90, 30), '계단'), ((230, 140, 60), '사람 길'), ((196, 120, 95), '건물'), ((150, 190, 210), '바다')]
    x = 10
    for c, t in items:
        dr.rectangle([x, 20, x + 22, 36], fill=c); dr.text((x + 28, 18), t, fill=(0, 0, 0), font=fs); x += 125
    return L

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    want = sys.argv[1:]
    for t in TOWNS:
        if want and t[0] not in want: continue
        render(*t)
    ims = [Image.open(f'{OUT}/{t[0]}.png') for t in TOWNS if os.path.exists(f'{OUT}/{t[0]}.png')]
    cols = 4; rows = math.ceil(len(ims) / cols); sm = PX // 2
    sheet = Image.new('RGB', (cols * sm, rows * sm + 60), (255, 255, 255))
    for k, im in enumerate(ims):
        sheet.paste(im.resize((sm, sm), Image.Resampling.LANCZOS), ((k % cols) * sm, (k // cols) * sm))
    sheet.paste(legend().resize((cols * sm, 60)), (0, rows * sm))
    sheet.save(f'{OUT}/sheet.png')
    print('sheet', len(ims))
