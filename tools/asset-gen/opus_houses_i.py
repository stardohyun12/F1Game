"""집 i판 — EDC 건물 그래픽 기법 (D-177, 과제 20261027-1627).
    "D:/Tools/Blender/blender.exe" -b --factory-startup -P tools/asset-gen/opus_houses_i.py

이번 방법 —
  형태: e판 parts.json 의 치수·창·문·덧붙임 위치를 그대로 읽어 단순 상자로 다시 짓는다(변형·잔부품 없음).
  표면: 1m = 8텍셀 Point. 벽 = 한 색 평면, 창 = 짙은 회색 유리 + 1텍셀 밝은 창틀·중간살, 처마·지붕 끝 = 짙은 갈색 2텍셀 띠,
        1층 가게 = 큰 유리 띠 + 색 띠 한 줄, 간판 = 단색 판 + 3×5·5×5 픽셀 글자.
        굽기 없음 — 면 방향 명도만 정면 1.00 · 왼 0.95 · 오른/뒤 0.93 · 지붕 앞 1.05 · 지붕 뒤 0.96 · 아랫면 0.92.
        사진 무늬·얼룩·보수 칠 없음. 손댄 흔적은 형태로만(덧댄 차양·막은 창·새로 뚫은 창 틀 색).
출력: art/export/scene-a4-houses-i/ (fbx·png·kit.json·measure.json·palette.gpl) + art/_review/2026-10-27-houses-i/ 렌더.
"""
import bpy, bmesh, os, json, math
import numpy as np
from mathutils import Vector

ROOT = 'D:/Github/F1Game'
J = lambda *p: os.path.join(ROOT, *p)
SRC = J('art', 'export', 'scene-a4-houses-e', 'parts.json')
OUT = J('art', 'export', 'scene-a4-houses-i'); REV = J('art', '_review', '2026-10-27-houses-i')
os.makedirs(OUT, exist_ok=True); os.makedirs(REV, exist_ok=True)
P = json.load(open(SRC, encoding='utf-8'))
TPM = 8  # 텍셀/m

# ---------- 색 (EDC 처럼 회색 섞인 톤) ----------
WALL = dict(house_narrow_a=(198, 186, 158),   # 연황토
            house_narrow_b=(206, 204, 192),   # 크림(차가운)
            house_shop=(204, 180, 164),       # 바랜 살구
            house_slope=(166, 184, 178),      # 바랜 연청록
            house_warehouse=(194, 196, 194),  # 회백
            house_cafe=(212, 200, 180))       # 크림(따뜻한)
ACC = dict(house_narrow_a=(112, 92, 72), house_narrow_b=(84, 104, 110), house_shop=(150, 74, 64),
           house_slope=(82, 104, 112), house_warehouse=(92, 116, 96), house_cafe=(150, 92, 70))  # 색 띠·덧창·간판(e판 shutter_col 을 바랜 쪽으로)
ROOF = dict(house_narrow_a=(140, 104, 88), house_narrow_b=(128, 128, 126), house_shop=(112, 112, 116),
            house_slope=(146, 108, 92), house_warehouse=(116, 120, 118), house_cafe=(150, 110, 92))
GLASS, GLASS2, FRAME, FRAME_NEW = (64, 68, 72), (86, 90, 94), (178, 180, 176), (206, 204, 196)
EAVE, DOOR, PIPE, STONE, TIN = (84, 62, 50), (98, 84, 72), (96, 98, 98), (150, 148, 140), (148, 152, 152)
CREAM = (232, 226, 210)
SHADE = dict(F=1.00, L=.95, R=.93, B=.93, RF=1.05, RB=.96, T=1.03, D=.92)

FONT = dict(F='###/#../##./#../#..', I='###/.#./.#./.#./###', S='.##/#../.#./..#/##.', H='#.#/#.#/###/#.#/#.#', R='##./#.#/##./#.#/#.#',
            O='###/#.#/#.#/#.#/###', M='#.#/###/###/#.#/#.#', E='###/#../##./#../###', P='##./#.#/##./#../#..', A='.#./#.#/###/#.#/#.#',
            T='###/.#./.#./.#./.#.', L='#../#../#../#../###', C='.##/#../#../#../.##', **{'7': '###/..#/.#./.#./.#.', ' ': '../../../../..',
            'サ': '.#.#./#####/.#.#./...#./..#..', 'カ': '.#.../#####/.#..#/.#..#/#..#.', 'ナ': '..#../#####/..#../.#.../#....'})
glyph = lambda ch: [[c == '#' for c in row] for row in FONT[ch].split('/')]


# ---------- 면 = 다각형 + 평면 칠 ----------
class Face:
    def __init__(s, verts, uax, vax, col, shade, role):
        s.v = [Vector(p) for p in verts]; s.u = Vector(uax).normalized(); s.w = Vector(vax).normalized()
        o = s.v[0]; s.o = o
        uv = [((p - o).dot(s.u), (p - o).dot(s.w)) for p in s.v]
        s.u0 = min(a for a, b in uv); s.v0 = min(b for a, b in uv)
        s.lu = max(a for a, b in uv) - s.u0; s.lv = max(b for a, b in uv) - s.v0
        s.uv = [(a - s.u0, b - s.v0) for a, b in uv]
        s.tw = max(1, int(round(s.lu * TPM))); s.th = max(1, int(round(s.lv * TPM)))
        s.col = col; s.shade = shade; s.role = role; s.dec = []  # (kind, a0, a1, b0, b1, extra)  a,b = 면 안 좌표(m, 좌하 원점)
        s.uoff = 0.0; s.voff = 0.0  # 면 좌표 → 벽 좌표 (창 u, z 를 바로 쓰려고)


def box(fs, x0, x1, y0, y1, z0, z1, col, role='part', sides='FLRBTD', top=None, side_col=None):
    """축 정렬 상자. 바깥에서 본 면마다 u = 보는 사람 오른쪽, v = 위."""
    sc = side_col or col
    if 'F' in sides: fs.append(Face([(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], (1, 0, 0), (0, 0, 1), col, 'F', role))
    if 'B' in sides: fs.append(Face([(x1, y1, z0), (x0, y1, z0), (x0, y1, z1), (x1, y1, z1)], (-1, 0, 0), (0, 0, 1), sc, 'B', role))
    if 'L' in sides: fs.append(Face([(x0, y1, z0), (x0, y0, z0), (x0, y0, z1), (x0, y1, z1)], (0, -1, 0), (0, 0, 1), sc, 'L', role))
    if 'R' in sides: fs.append(Face([(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], (0, 1, 0), (0, 0, 1), sc, 'R', role))
    if 'T' in sides: fs.append(Face([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], (1, 0, 0), (0, 1, 0), top or col, 'T', role))
    if 'D' in sides: fs.append(Face([(x0, y1, z0), (x1, y1, z0), (x1, y0, z0), (x0, y0, z0)], (1, 0, 0), (0, -1, 0), sc, 'D', role))
    return fs[-len(sides):]


def win(face, u0, u1, z0, z1, kind='window', new=False):
    face.dec.append(('win' if kind == 'window' else kind, u0, u1, z0, z1, new))


# ---------- 집 하나 ----------
def build(name, h):
    W, D, Hw, zb = h['W'], h['D'], h['Hw'], h['zb']; acc = ACC[name]; wall = WALL[name]; roof = ROOF[name]
    fs = []; YF, YB, XL, XR = -D / 2, D / 2, -W / 2, W / 2
    if zb > 0:  # 축대
        box(fs, XL - .3, XR + .3, YF - .3, YB + .3, 0, zb, STONE, 'plinth', 'FLRBT')
        for k in range(3):  # 계단 3단 (문 앞)
            box(fs, -1.6, -0.4, YF - .3 - .45 * (3 - k), YF - .3 - .45 * (2 - k) if k < 2 else YF - .3, 0, zb * (k + 1) / 3, STONE, 'plinth', 'FLRT')
    gable = h['roof'] == 'gable'; oh = .35; rise = D * .32 if gable else 0
    # 벽: 앞·뒤 사각, 옆 = 오각(박공 포함)
    wf = box(fs, XL, XR, YF, YB, zb, Hw, wall, 'wall', 'FB')
    if gable:
        for x, uax, sh in ((XL, (0, -1, 0), 'L'), (XR, (0, 1, 0), 'R')):
            pts = [(x, YB, zb), (x, YF, zb), (x, YF, Hw), (x, 0, Hw + rise), (x, YB, Hw)]
            if sh == 'R': pts = [(x, YF, zb), (x, YB, zb), (x, YB, Hw), (x, 0, Hw + rise), (x, YF, Hw)]
            fs.append(Face(pts, uax, (0, 0, 1), wall, sh, 'wall'))
    else:
        box(fs, XL, XR, YF, YB, zb, Hw, wall, 'wall', 'LR')
    walls = {f.shade: f for f in fs if f.role == 'wall'}
    for f in walls.values():  # 면 좌표 = (u - 면 가운데, z)
        f.uoff = -f.lu / 2; f.voff = zb
    # 지붕
    if gable:
        t = .22; z_e = Hw - oh * rise / (D / 2)  # 처마 끝 높이(기울기 따라 내려감)
        for sgn, sh in ((-1, 'RF'), (1, 'RB')):
            ye = sgn * (D / 2 + oh)
            a, b = (XL - oh, ye, z_e), (XR + oh, ye, z_e); c, d = (XR + oh, 0, Hw + rise), (XL - oh, 0, Hw + rise)
            if sgn > 0: a, b, c, d = b, a, d, c
            top = Face([(p[0], p[1], p[2] + t) for p in (a, b, c, d)], (b[0] - a[0], 0, 0), (0, -ye, rise + oh * rise / (D / 2)), roof, sh, 'roof')
            und = Face([d, c, b, a], (a[0] - b[0], 0, 0), (0, ye, -(rise + oh * rise / (D / 2))), EAVE, 'D', 'roof')
            ed = Face([a, b, (b[0], b[1], b[2] + t), (a[0], a[1], a[2] + t)], (b[0] - a[0], 0, 0), (0, 0, 1), EAVE, 'F' if sgn < 0 else 'B', 'eave')
            fs += [top, und, ed]
            for x, s2 in ((XL - oh, 'L'), (XR + oh, 'R')):  # 박공 끝 띠
                q = [(x, ye, z_e), (x, 0, Hw + rise), (x, 0, Hw + rise + t), (x, ye, z_e + t)]
                if (s2 == 'L') != (sgn > 0): q = q[::-1]
                fs.append(Face(q, (0, -1 if s2 == 'L' else 1, 0), (0, 0, 1), EAVE, s2, 'eave'))
    else:
        box(fs, XL - .05, XR + .05, YF - .05, YB + .05, Hw, Hw + .25, EAVE, 'eave', 'FLRBT', top=roof)
    # ---- 창·문 (e판 위치 그대로) ----
    for o in h['openings']:
        f = walls.get({'front': 'F', 'left': 'L', 'right': 'R', 'back': 'B'}[o['face']])
        if f is None: continue
        win(f, o['u0'], o['u1'], o['z0'], o['z1'], o['kind'], o.get('own') == 'new')
    # 뒤 벽 창(e판엔 없음 — 단면 반복 방지로 층마다 하나)
    for s in range(h['stories']):
        z0 = zb + 3 * s + .4 + (0 if s else .0)
        if z0 + 2 < Hw: win(walls['B'], -.5, .5, z0, z0 + 2 if s else z0 + 1.6)
    F = walls['F']
    for b in h.get('blocked', []):
        z0 = zb + 3 * b['story'] + .4; F.dec.append(('blocked', b['uc'] - .5, b['uc'] + .5, z0, z0 + 2, None))
    for sh in h.get('shutter', []):  # 덧창 = 창 옆 단색 판 두 장 (열림/반/닫힘)
        z0 = zb + 3 * sh['story'] + .4
        for o in h['openings']:
            if o['face'] == 'front' and o['kind'] == 'window' and o.get('story') == sh['story'] and not o.get('own') and o['z0'] == z0:
                if any(d[0] == 'shut' and abs(d[1] - o['u0']) < .01 and abs(d[3] - z0) < .01 for d in F.dec): continue
                F.dec.append(('shut', o['u0'], o['u1'], o['z0'], o['z1'], tuple(sh['states']))); break
    # ---- 집마다 덧붙임 ----
    adds = {a['kind'] for a in h.get('add', [])}; signs = {s['text']: s['face'] for s in h.get('sign', [])}
    shopfront = name in ('house_shop', 'house_narrow_b', 'house_cafe')
    if shopfront:  # 1층 가게: 앞면 1층의 창·문을 유리 띠로 바꿈
        top = 2.3 if name != 'house_cafe' else 2.2
        dr = [d for d in F.dec if d[3] < 2.5 and d[0] in ('win', 'door')]
        F.dec = [d for d in F.dec if d not in dr]
        doors = [d for d in dr if d[0] == 'door']
        F.dec.append(('band', -W / 2, W / 2, 0, .3, acc))
        F.dec.append(('glassband', -W / 2 + .25, W / 2 - .25, .3, top, [(d[1], d[2]) for d in doors]))
        F.dec.append(('band', -W / 2, W / 2, top, top + .375, acc))
    if 'add_canopy' in adds:
        box(fs, -2.35, -.65, YF - .9, YF, 2.4, 2.52, EAVE, 'add', 'FLRTD', top=acc)
    if 'add_balcony_ext' in adds:
        box(fs, .55, 2.45, YF - .85, YF, 2.88, 3.03, EAVE, 'add', 'FLRTD', top=FRAME)
        rail = box(fs, .55, 2.45, YF - .85, YF - .79, 3.03, 4.0, wall, 'add', 'FLRBT', top=FRAME)
        for f in rail:
            if f.shade in 'FB': f.dec.append(('band', -1, 1, 3.87, 4.0, FRAME)); f.uoff = -f.lu / 2; f.voff = 3.03
    if 'add_roof_room' in adds:
        r = box(fs, -1.6, .6, .2, YB - .3, Hw + .25, Hw + 2.45, wall, 'add', 'FLRB')
        box(fs, -1.7, .7, .1, YB - .2, Hw + 2.45, Hw + 2.7, EAVE, 'eave', 'FLRBT', top=roof)
        for f in r:
            f.uoff = -f.lu / 2; f.voff = Hw + .25
            if f.shade == 'F': f.dec += [('door', -.8, .1, .0 + Hw + .25, Hw + 2.25, False)]; win(f, .35, .85, Hw + 1.0, Hw + 1.9)
    if 'add_side_pipe' in adds:
        box(fs, XL - .14, XL - .02, YF + .4, YF + .52, zb, Hw + .2, PIPE, 'add', 'FLRB')
    if 'add_tin_canopy' in adds:
        z = 2.45 if name == 'house_shop' else 2.5
        x0, x1 = (-W / 2 + .1, W / 2 - .1) if name == 'house_shop' else (-2.8, -.2)
        box(fs, x0, x1, YF - 1.0, YF, z, z + .1, EAVE, 'add', 'FLRTD', top=TIN)
    if 'add_vertical_sign' in adds:
        bd = box(fs, XR - .16, XR - .04, YF - .95, YF - .08, 3.0, 5.5, acc, 'sign', 'FLRBTD')
        for f in bd:
            if f.shade in 'LR': f.dec.append(('vtext', 'サカナ', CREAM)); f.voff = 0
    if 'add_roll_shutter' in adds:
        for d in list(F.dec):
            if d[0] == 'door' and d[1] < 0: F.dec.remove(d); F.dec.append(('roll', d[1], d[2], d[3], d[4], None))
    if 'add_shed' in adds:
        sx0, sx1 = XL - 2.0, XL - .3; zb = 0
        s = box(fs, sx0, sx1, -1.4, 1.4, zb, zb + 2.3, (150, 136, 116), 'add', 'FLRB')
        box(fs, sx0 - .2, sx1, -1.6, 1.6, zb + 2.3, zb + 2.5, EAVE, 'eave', 'FLRBT', top=roof)
        for f in s:
            f.uoff = -f.lu / 2; f.voff = zb
            if f.shade == 'F': f.dec.append(('door', -.45, .45, zb, zb + 1.9, False))
    if 'add_striped_awning' in adds:
        x0, x1, zt, zo, dd = -2.8, -.2, 2.75, 2.3, 1.1
        fs.append(Face([(x0, YF - dd, zo), (x1, YF - dd, zo), (x1, YF, zt), (x0, YF, zt)], (1, 0, 0), (0, dd, zt - zo), CREAM, 'RF', 'awning'))
        fs[-1].dec.append(('stripes', acc))
        v = Face([(x0, YF - dd, zo - .3), (x1, YF - dd, zo - .3), (x1, YF - dd, zo), (x0, YF - dd, zo)], (1, 0, 0), (0, 0, 1), CREAM, 'F', 'awning')
        v.dec.append(('stripes', acc)); fs.append(v)
        for x, sh in ((x0, 'L'), (x1, 'R')):
            q = [(x, YF, zt), (x, YF - dd, zo), (x, YF - dd, zo - .3)]
            if sh == 'L': q = q[::-1]
            fs.append(Face(q, (0, -1 if sh == 'L' else 1, 0), (0, 0, 1), acc, sh, 'awning'))
    # 간판
    for text, face in signs.items():
        if face == 'side-projecting': continue
        n = len(text); tw = (4 * n + 1) / TPM; th = 6 / TPM  # 판 = 글자 5 + 위아래 여백 → 7텍셀(0.875m)
        if face == 'front-roof':
            x0, z0 = -tw / 2 - .1, Hw + .55; y0 = YF + .5
            box(fs, x0 - .05, x0 + .05, y0, y0 + .1, Hw - .2, z0, EAVE, 'add', 'FLR'); box(fs, -x0 - .05, -x0 + .05, y0, y0 + .1, Hw - .2, z0, EAVE, 'add', 'FLR')
            bd = box(fs, x0, -x0, y0 - .1, y0, z0, z0 + th + .1, (96, 110, 104), 'sign', 'FLRBT', top=EAVE)
        else:
            z0 = dict(house_shop=2.6, house_narrow_b=2.35, house_warehouse=2.75, house_narrow_a=2.53)[name]  # 창·차양·발코니와 안 겹치는 높이
            x0 = -2.45 if name == 'house_narrow_a' else -tw / 2 - .1
            bd = box(fs, x0, x0 + tw + .2, YF - .08, YF, z0, z0 + th + .1, (64, 78, 72) if name != 'house_shop' else acc, 'sign', 'FLRT', top=EAVE)
        for f in bd:
            if f.shade == 'F': f.dec.append(('text', text, CREAM))
    return fs


# ---------- 칠 ----------
def shade(c, k): return tuple(int(min(255, round(v * k))) for v in c)


def paint(f):
    a = np.zeros((f.th, f.tw, 3), np.uint8); a[:] = shade(f.col, SHADE[f.shade]); k = SHADE[f.shade]
    T = lambda u: int(round((u - f.uoff) * TPM)); Z = lambda z: int(round((z - f.voff) * TPM))

    def rect(i0, i1, j0, j1, c):
        i0, i1 = max(0, i0), min(f.tw, i1); j0, j1 = max(0, j0), min(f.th, j1)
        if i1 > i0 and j1 > j0: a[j0:j1, i0:i1] = shade(c, k)

    def window(i0, i1, j0, j1, fr, mull=True):
        rect(i0, i1, j0, j1, fr); rect(i0 + 1, i1 - 1, j0 + 1, j1 - 1, GLASS)
        h = j1 - j0; w = i1 - i0
        if h >= 10: tj = j0 + int(round(h * .68)); rect(i0 + 1, i1 - 1, tj, j1 - 1, GLASS2); rect(i0, i1, tj, tj + 1, fr)
        if mull and w >= 6: m = (i0 + i1) // 2; rect(m, m + 1, j0, j1 if h < 10 else j0 + int(round(h * .68)), fr)

    for d in f.dec:
        kd = d[0]
        if kd in ('text', 'vtext'):
            gs = [glyph(ch) for ch in d[1]]
            if kd == 'text':
                x = (f.tw - (sum(len(g[0]) + 1 for g in gs) - 1)) // 2; y = (f.th - 5) // 2
                for g in gs:
                    for r, row in enumerate(g):
                        for c_, on in enumerate(row):
                            if on: rect(x + c_, x + c_ + 1, y + 4 - r, y + 5 - r, d[2])
                    x += len(g[0]) + 1
            else:
                y = f.th - 1
                for g in gs:
                    x = (f.tw - len(g[0])) // 2
                    for r, row in enumerate(g):
                        for c_, on in enumerate(row):
                            if on: rect(x + c_, x + c_ + 1, y - r - 1, y - r, d[2])
                    y -= 6
            continue
        if kd == 'stripes':
            for i in range(0, f.tw, 4): rect(i, i + 2, 0, f.th, d[1])
            continue
        i0, i1, j0, j1 = T(d[1]), T(d[2]), Z(d[3]), Z(d[4])
        if kd == 'win': window(i0, i1, j0, j1, FRAME_NEW if d[5] else FRAME, mull=not d[5])
        elif kd == 'door':
            rect(i0, i1, j0, j1, FRAME); rect(i0 + 1, i1 - 1, j0, j1 - 1, DOOR)
            if j1 - j0 >= 14: rect(i0 + 2, i1 - 2, j1 - 5, j1 - 2, GLASS)
        elif kd == 'blocked':  # 막은 창 = 창틀만 남고 안은 벽보다 6% 어두운 판
            rect(i0, i1, j0, j1, FRAME); rect(i0 + 1, i1 - 1, j0 + 1, j1 - 1, shade(f.col, .94))
        elif kd == 'shut':
            st = d[5]; w = i1 - i0; hw = max(2, w // 2)
            for side, s in zip((-1, 1), st):
                if s == 'open': rect(i0 - hw, i0, j0, j1, ACC_CUR) if side < 0 else rect(i1, i1 + hw, j0, j1, ACC_CUR)
                elif s == 'half': rect(i0, i0 + hw // 2 + 1, j0, j1, ACC_CUR) if side < 0 else rect(i1 - hw // 2 - 1, i1, j0, j1, ACC_CUR)
                else: rect(i0 + 1, i0 + hw, j0 + 1, j1 - 1, ACC_CUR) if side < 0 else rect(i1 - hw, i1 - 1, j0 + 1, j1 - 1, ACC_CUR)
        elif kd == 'band': rect(i0, i1, j0, j1, d[5])
        elif kd == 'roll':
            rect(i0, i1, j0, j1, FRAME); rect(i0 + 1, i1 - 1, j0, j1 - 1, TIN)
            for j in range(j0 + 1, j1 - 1, 2): rect(i0 + 1, i1 - 1, j, j + 1, shade(TIN, .9))
        elif kd == 'glassband':  # 큰 유리 띠: 1텍셀 틀, 약 1m 마다 중간살, 문 칸은 아래까지
            rect(i0, i1, j0, j1, FRAME); rect(i0 + 1, i1 - 1, j0 + 1, j1 - 1, GLASS)
            tj = j1 - 4; rect(i0 + 1, i1 - 1, tj, j1 - 1, GLASS2); rect(i0, i1, tj - 1, tj, FRAME)
            n = max(1, round((i1 - i0) / 8))
            for m in range(1, n): x = i0 + round((i1 - i0) * m / n); rect(x, x + 1, j0, j1, FRAME)
            for u0, u1 in d[5]:
                a0, a1 = T(u0), T(u1); rect(a0, a1, 0, tj - 1, FRAME); rect(a0 + 1, a1 - 1, 0, tj - 2, GLASS)
                rect(a0 + 1, a1 - 1, 0, 1, DOOR)
    return a


ACC_CUR = (0, 0, 0)


def pack(faces):
    rects = sorted(range(len(faces)), key=lambda i: -faces[i].th); Wt = 512; x = y = rowh = 0; pos = {}
    for i in rects:
        w, h = faces[i].tw + 2, faces[i].th + 2
        if x + w > Wt: x = 0; y += rowh; rowh = 0
        pos[i] = (x + 1, y + 1); x += w; rowh = max(rowh, h)
    Ht = 1 << max(6, (y + rowh - 1).bit_length())
    return Wt, Ht, pos


def make(name, h):
    global ACC_CUR
    ACC_CUR = ACC[name]
    fs = build(name, h)
    Wt, Ht, pos = pack(fs)
    tex = np.zeros((Ht, Wt, 3), np.uint8); tex[:] = EAVE
    role = np.zeros((Ht, Wt), np.uint8)  # 1 = 벽 평면(칠 없는 칸 측정용)
    me = bpy.data.meshes.new(name); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    for i, f in enumerate(fs):
        a = paint(f); px, py = pos[i]
        tex[py:py + f.th, px:px + f.tw] = a
        tex[py - 1, px:px + f.tw] = a[0]; tex[py + f.th, px:px + f.tw] = a[-1]  # 1텍셀 테두리 복제(Point 번짐 막기)
        tex[py - 1:py + f.th + 1, px - 1] = tex[py - 1:py + f.th + 1, px]; tex[py - 1:py + f.th + 1, px + f.tw] = tex[py - 1:py + f.th + 1, px + f.tw - 1]
        if f.role == 'wall': role[py:py + f.th, px:px + f.tw] = 1
        vs = [bm.verts.new(v) for v in f.v]; bf = bm.faces.new(vs)
        for lp, (a_, b_) in zip(bf.loops, f.uv):
            lp[uvl].uv = ((px + a_ / f.lu * f.tw) / Wt if f.lu else px / Wt, (py + b_ / f.lv * f.th) / Ht if f.lv else py / Ht)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    img = bpy.data.images.new(name + '_tex', Wt, Ht, alpha=True)
    px4 = np.ones((Ht, Wt, 4), np.float32); px4[..., :3] = tex / 255.0
    img.pixels.foreach_set(px4.ravel()); img.filepath_raw = os.path.join(OUT, name + '.png'); img.file_format = 'PNG'; img.save()
    img.colorspace_settings.name = 'sRGB'
    mat = bpy.data.materials.new(name + '_mat'); mat.use_nodes = True; nt = mat.node_tree; nt.nodes.clear()
    ti = nt.nodes.new('ShaderNodeTexImage'); ti.image = img; ti.interpolation = 'Closest'
    em = nt.nodes.new('ShaderNodeEmission'); outn = nt.nodes.new('ShaderNodeOutputMaterial')
    bs = nt.nodes.new('ShaderNodeBsdfPrincipled')  # FBX 로 텍스처 경로를 넘기려고 연결해 둠(렌더는 Emission)
    nt.links.new(ti.outputs[0], bs.inputs['Base Color']); nt.links.new(ti.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], outn.inputs[0])
    me.materials.append(mat)
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    # 측정: 벽 칸 색 수(면 방향별), 전체 색 수
    flat = tex.reshape(-1, 3); allc = len({tuple(c) for c in flat[::1].tolist()})
    wc = {}
    for i, f in enumerate(fs):
        if f.role != 'wall': continue
        px, py = pos[i]; a = tex[py:py + f.th, px:px + f.tw].reshape(-1, 3)
        base = np.array(shade(f.col, SHADE[f.shade])); share = float((np.abs(a.astype(int) - base).sum(1) == 0).mean())
        wc[f.shade] = dict(colors=len({tuple(c) for c in a.tolist()}), plain_share=round(share, 3))
    return ob, dict(tris=tris, tex=[Wt, Ht], colors=allc, wall_faces=wc, faces=len(fs))


bpy.ops.wm.read_factory_settings(use_empty=True)
meas = {}; obs = {}
for name, h in P.items():
    ob, m = make(name, h); obs[name] = ob; meas[name] = m; print('BUILT', name, m, flush=True)
    for o in bpy.context.scene.objects: o.select_set(False)
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'), use_selection=True, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_ALL', path_mode='STRIP', mesh_smooth_type='FACE')

# 팔레트(실제로 쓴 색 전부)
cols = set()
for name in P:
    im = bpy.data.images[name + '_tex']; a = np.array(im.pixels[:]).reshape(-1, 4)[:, :3]
    cols |= {tuple(int(round(v * 255)) for v in c) for c in np.unique(np.round(a, 4), axis=0)}
cols = sorted(cols)
open(os.path.join(OUT, 'palette.gpl'), 'w').write('GIMP Palette\nName: scene-a4-houses-i\nColumns: 8\n#\n' + ''.join(f'{r} {g} {b} c{i}\n' for i, (r, g, b) in enumerate(cols)))
kit = dict(task='20261027-1627', decision='D-177', note='도현 승인 전. F1/Assets 에 넣지 않음. Unity 임포트 미확인 — Unlit + Point(필터 없음)·밉맵 끔 권장.',
           axis='Blender: 정면 -Y, 위 +Z → FBX axis_forward -Z, axis_up Y, 1u = 1m. 원점 = 집 바닥 가운데(e판과 같음).',
           method=__doc__.split('이번 방법 —')[1].split('출력:')[0].strip(), texels_per_m=TPM, shade=SHADE, palette_colors=len(cols),
           files=[dict(name=n, fbx=n + '.fbx', png=n + '.png', wall=WALL[n], band=ACC[n], roof=ROOF[n], **meas[n]) for n in P])
json.dump(kit, open(os.path.join(OUT, 'kit.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(meas, open(os.path.join(OUT, 'measure.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('PALETTE', len(cols), flush=True)

# ---------- 미리보기: 한 줄 거리 1280×720 ----------
sc = bpy.context.scene
gap = 1.4; x = 0; order = ['house_slope', 'house_narrow_a', 'house_shop', 'house_narrow_b', 'house_cafe', 'house_warehouse']
for n in order:
    W = P[n]['W'] + (1.9 if n == 'house_slope' else 0)
    obs[n].location = (x + W / 2 + (1.9 / 2 if n == 'house_slope' else 0), 0, 0); x += W + gap
for o in obs.values(): o.location.x -= (x - gap) / 2
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.lens_unit = 'FOV'; cam.data.angle = math.radians(45)
yaw, pitch, dist = math.radians(18), math.radians(12), 53.0
tgt = Vector((-2.2, 0, 4.0)); cam.location = tgt + Vector((-math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch))) * dist
cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'; sc.eevee.taa_render_samples = 1; sc.render.filter_size = 0.0
sc.render.resolution_x, sc.render.resolution_y = 1280, 720; sc.render.dither_intensity = 0.0;sc.render.film_transparent = True
sc.view_settings.view_transform = 'Standard'; sc.view_settings.look = 'None'
sc.render.image_settings.file_format = 'PNG'; sc.render.image_settings.color_mode = 'RGBA'
sc.world = bpy.data.worlds.new('w'); sc.world.use_nodes = True
sc.render.filepath = os.path.join(REV, 'row_1x.png'); bpy.ops.render.render(write_still=True)
# 화면 px / 텍셀 (집 정면 가운데에서 1m 를 투영)
from bpy_extras.object_utils import world_to_camera_view as w2c
pp = {}
for n in order:
    o = obs[n]; c0 = o.location + Vector((0, -P[n]['D'] / 2, 3.0)); c1 = c0 + Vector((1, 0, 0)); c2 = c0 + Vector((0, 0, 1))
    a, b, c = (w2c(sc, cam, p) for p in (c0, c1, c2))
    pp[n] = dict(px_per_texel_h=round((b.x - a.x) * 1280 / TPM, 2), px_per_texel_v=round((c.y - a.y) * 720 / TPM, 2),
                 front_center_px=[round(a.x * 1280), round((1 - a.y) * 720)])
# 근접 크롭 기준점
anchors = dict(shop=obs['house_shop'].location + Vector((0, -3, 2.0)), apt=obs['house_narrow_a'].location + Vector((0, -3, 6.5)))
pp['anchors'] = {k: [round(w2c(sc, cam, v).x * 1280), round((1 - w2c(sc, cam, v).y) * 720)] for k, v in anchors.items()}
json.dump(pp, open(os.path.join(REV, 'screen_measure.json'), 'w'), indent=1)
print('SCREEN', pp, flush=True)
print('DONE', flush=True)
