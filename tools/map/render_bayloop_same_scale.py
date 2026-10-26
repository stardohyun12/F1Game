"""BayLoop 현재 계획을 실제 마을 지도와 같은 축척(1200m, 900px)·같은 색으로."""
import json, math
from PIL import Image, ImageDraw, ImageFont
import importlib.util
spec = importlib.util.spec_from_file_location('rt', 'tools/map/real_towns.py'); rt = importlib.util.module_from_spec(spec); spec.loader.exec_module(rt)
d = json.load(open('docs/map/bayloop-plan.json', encoding='utf-8'))
PX, S = rt.PX, rt.SIZE_M; k = PX / S
cx, cz = 0, 60
def P(x, z): return ((x - cx) * k + PX / 2, PX / 2 - (z - cz) * k)
img = Image.new('RGB', (PX, PX), (150, 190, 210)); dr = ImageDraw.Draw(img)
dr.polygon([P(x, z) for x, z in d['sea']['landPolygon']], fill=(238, 232, 218))
for p in d['parcels']:
    w, dd, r = p['w'], p['d'], math.radians(p.get('rot', 0))
    cs = [(-w/2, -dd/2), (w/2, -dd/2), (w/2, dd/2), (-w/2, dd/2)]
    pts = [P(p['x'] + a*math.cos(r) + b*math.sin(r), p['z'] - a*math.sin(r) + b*math.cos(r)) for a, b in cs]
    dr.polygon(pts, fill=(196, 120, 95), outline=(150, 90, 70))
col = {'main': ((70, 60, 55), 5.5), 'village': ((110, 95, 80), 4.5), 'shortcut': ((150, 130, 110), 3.5)}
for r in d['roads']:
    pts = [P(a[0], a[1]) for a in r['knots']]
    if r.get('closed'): pts.append(pts[0])
    if r['kind'] in ('alley', 'stairs'):
        dr.line(pts, fill=(220, 90, 30) if r['kind'] == 'stairs' else (230, 140, 60), width=2)
    else:
        c, wm = col.get(r['kind'], col['village']); dr.line(pts, fill=c, width=max(2, int(r.get('width', wm) * k * 1.6)), joint='curve')
f = rt.font(22); fs = rt.font(15)
dr.rectangle([0, 0, PX, 34], fill=(255, 255, 255)); dr.text((10, 5), '우리 BayLoop v4 (현재) — 같은 축척', fill=(30, 30, 30), font=f)
bar = 200 * k; dr.rectangle([12, PX - 30, 12 + bar, PX - 24], fill=(30, 30, 30)); dr.text((16 + bar, PX - 38), '200 m', fill=(30, 30, 30), font=fs)
img.save('docs/map/real-towns/zz_bayloop.png'); print('ok', len(d['parcels']), len(d['roads']))
