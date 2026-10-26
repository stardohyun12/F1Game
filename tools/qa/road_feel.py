"""노면 품질 표 + 위 평면 히트맵 (과제 0320-1).

입력: docs/qa/tmp/road-feel.tsv (Unity: tools/qa/road_feel_measure.cs)
      docs/qa/tmp/townv6-bot-cover.txt (+ -feel.tsv) — 있으면 (e) 차체 수직 가속도·접지 끊김
사용: python -X utf8 tools/qa/road_feel.py <태그>   → docs/qa/evidence/townv6-feel/heat_<태그>.png, table_<태그>.md
개수는 '곳' 단위다: 같은 길·같은 종류의 표본이 3 m 안에 이어지면 한 곳으로 센다.
"""
import json, math, os, sys, collections
from PIL import Image, ImageDraw, ImageFont

tag = sys.argv[1] if len(sys.argv) > 1 else 'before'
T = 'docs/qa/tmp/'; OUT = sys.argv[2] if len(sys.argv) > 2 else 'docs/qa/evidence/townv6-feel/'; os.makedirs(OUT, exist_ok=True)
CLASS = lambda kind: '돌길' if kind == 'shortcut' else ('큰길' if kind == 'main' else '마을길')
TYPES = ['bump', 'step', 'float', 'buried', 'gap', 'cross', 'air', 'az']
NAME = {'bump': '덜컹(경사변화>3%/m)', 'step': '단차>2cm', 'float': '뜸>5cm', 'buried': '묻힘>5cm', 'gap': '길 아래 빈틈>5cm',
        'cross': '가로기울기>6%', 'air': '접지 끊김(봇)', 'az': '수직가속>6m/s²(봇)'}
COL = {'bump': (230, 40, 40), 'step': (160, 0, 200), 'float': (30, 120, 255), 'buried': (140, 90, 30), 'gap': (0, 190, 220),
       'cross': (255, 150, 0), 'air': (255, 0, 160), 'az': (255, 220, 0)}

rows = []  # (road, cls, type, x, z, value)
for ln in open(T + 'road-feel.tsv', encoding='utf-8-sig').read().splitlines()[1:]:
    r, kind, rough, ty, x, z, v = ln.split('\t')
    if ty == 'cross_hp': continue   # 급굽이 바깥 = 제외(과제)
    rows.append((r, CLASS(kind), ty, float(x), float(z), float(v)))
kinds = {}
for ln in open(T + 'road-feel-roads.tsv', encoding='utf-8-sig').read().splitlines()[1:]:
    r, kind, rough, ln_ = ln.split('\t'); kinds[r] = (CLASS(kind), float(ln_))
bot = {}
if os.path.exists(T + 'townv6-bot-cover.txt'):
    for ln in open(T + 'townv6-bot-cover.txt', encoding='utf-8-sig').read().splitlines():
        p = ln.split('\t')
        if len(p) >= 13 and p[0] in kinds:
            b = bot.setdefault(p[0], [0.0, 0.0, 0, 0]); b[0] = max(b[0], float(p[9])); b[1] = max(b[1], float(p[10])); b[2] += int(p[11]); b[3] += p[4] != 'ok'
if os.path.exists(T + 'townv6-bot-cover-feel.tsv'):
    for ln in open(T + 'townv6-bot-cover-feel.tsv', encoding='utf-8-sig').read().splitlines():
        x, z, ty, v, r = ln.split('\t')
        if r in kinds: rows.append((r, kinds[r][0], ty, float(x), float(z), float(v)))

# 곳 단위로 묶기
spots = []
groups = collections.defaultdict(list)
for row in rows: groups[(row[0], row[2])].append(row)
for (r, ty), g in groups.items():
    cl = []
    for row in g:
        for c in cl:
            if math.hypot(c[-1][3] - row[3], c[-1][4] - row[4]) < 3: c.append(row); break
        else: cl.append([row])
    for c in cl: spots.append((r, c[0][1], ty, sum(x[3] for x in c) / len(c), sum(x[4] for x in c) / len(c), max(abs(x[5]) for x in c)))

cnt = collections.Counter((s[1], s[2]) for s in spots)
km = collections.Counter()
for r, (c, l) in kinds.items(): km[c] += l / 1000
lines = [f'# 노면 품질 — {tag}', '', '단위: 곳(3 m 안 이어진 표본 = 1곳). 급굽이 바깥 가로기울기는 뺐다.', '']
lines.append('| 항목 | ' + ' | '.join(f'{c} ({km[c]:.1f} km)' for c in ['큰길', '마을길', '돌길']) + ' |')
lines.append('|---|---|---|---|')
for ty in TYPES: lines.append(f'| {NAME[ty]} | ' + ' | '.join(str(cnt[(c, ty)]) for c in ['큰길', '마을길', '돌길']) + ' |')
mcnt = collections.Counter((r[1], r[2]) for r in rows if r[2] in ('float', 'buried', 'gap'))
lines.append('\n- 길이로(가장자리 m, 양쪽 합): ' + ' · '.join(f'{NAME[t]} ' + '/'.join(str(mcnt[(c, t)]) for c in ['큰길', '마을길', '돌길']) for t in ('float', 'buried', 'gap')) + ' (큰길/마을길/돌길)')
if bot:
    for c in ['큰길', '마을길', '돌길']:
        rs = [v for r, v in bot.items() if kinds[r][0] == c]
        if rs: lines.append(f'\n- 봇 {c}: 길별 수직가속 RMS 최대 {max(v[0] for v in rs):.2f} m/s² · 피크 최대 {max(v[1] for v in rs):.1f} m/s² · 접지 끊김 합 {sum(v[2] for v in rs)} · 실패 {sum(v[3] for v in rs)}')
worst = collections.Counter(s[0] for s in spots if s[2] in ('bump', 'step', 'float', 'air'))
lines.append('\n가장 나쁜 길 10: ' + ', '.join(f'{r}({n})' for r, n in worst.most_common(10)))
open(OUT + f'table_{tag}.md', 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
print('\n'.join(lines))

# 히트맵: 위 = +z, 1 px = 0.5 m
J = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
S, X0 = 2.0, -400.0; W = int(800 * S)
im = Image.new('RGB', (W, W + 60), (24, 26, 30)); d = ImageDraw.Draw(im)
px = lambda x, z: ((x - X0) * S, W - (z - X0) * S)
for r in J['roads']:
    k = r['knots']; w = 3 if r['kind'] == 'main' else 2
    d.line([px(p[0], p[1]) for p in k], fill=(90, 92, 98) if r['kind'] in ('alley', 'stairs') else (150, 152, 158), width=w)
for s in sorted(spots, key=lambda s: TYPES.index(s[2]) if s[2] in TYPES else 0):
    if s[2] not in COL: continue
    x, y = px(s[3], s[4]); rad = 3 + min(6, s[5] / (3 if s[2] in ('bump', 'cross') else 5 if s[2] in ('step',) else 10))
    d.ellipse([x - rad, y - rad, x + rad, y + rad], outline=COL[s[2]], width=2)
try: f = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18)
except Exception: f = ImageFont.load_default()
xx = 8
for ty in TYPES:
    d.ellipse([xx, W + 20, xx + 14, W + 34], outline=COL[ty], width=3); d.text((xx + 18, W + 16), f'{NAME[ty]} {sum(v for (c, t), v in cnt.items() if t == ty)}', fill=(230, 230, 230), font=f); xx += 200
d.text((8, 8), f'Map_TownV6 노면 — {tag}', fill=(255, 255, 255), font=f)
im.save(OUT + f'heat_{tag}.png'); print('→', OUT + f'heat_{tag}.png')
