"""집 e판 — AI 티 줄이기 (아트2, 과제 20261027-1406). c판 생성기(opus_houses.py) 정의를 그대로 불러 쓰고, docs/design/HOUSES.md 카드대로 덧붙인다.
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_e.py
이번 방법 —
  형태: c판과 같은 elegantcrow 모듈 조립(같은 이름·원점·바닥 크기) + 집마다 카드(HOUSES.md)에서 나온 덧붙임.
        창·문: 유리 0.15m·문짝 0.12m 안으로 밀고 안쪽 턱(그늘)·밖 창턱·문턱. 가게 유리창 = 진열 선반, 창 = 커튼·화분·반커튼, 문 = 손잡이·문턱·우편함·번지판.
        간판: 3×5·5×5 픽셀 글자를 텍셀에 직접 찍음(FISH·サカナ·ROOMS·REPAIR·ATELIER 7·CAFE).
        반복 깨기: 덧창 판자(2채)·루버 모듈(2채)·없음(2채), 상태 열림·반열림·닫힘. 새로 뚫은 창(틀이 다름, 줄에서 0.3m 어긋남), 막은 창.
        손댄 흔적: 함석 차양·덧방·넓힌 발코니, 보수 칠 조각(명도 ±6~8%), 홈통(처마 물받이→땅). 용마루 처짐 1~2.5%, 평면 한 모서리 2~4° 틀어짐.
  표면: c판 칠 그대로(9텍셀/m, 단색 + 빛 3단, 사진·얼룩 없음). 짙은 테두리를 c판보다 연하게(0.52 → 0.70). 아래 0.6m 띠는 2채만.
  색: c판 32색 중 많이 쓴 22 + e판 칠에서 오차 큰 픽셀 k-means 10.
출력: art/export/scene-a4-houses-e/<이름>.fbx·.png, kit.json, palette32.gpl, measure.json, parts.json(검사용 부품 목록 — check_house_rules.py 가 읽음)
"""
import os, sys, json, math
MYDOC = __doc__
os.environ['HOUSES_VARIANT'] = 'c'
_src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses.py'), encoding='utf-8').read()
_src = _src[:_src.index('bpy.ops.wm.read_factory_settings(use_empty=True)\nPH_KEEP')]


def _patch(a, b):
    global _src; assert a in _src, a; _src = _src.replace(a, b, 1)


_patch("    # 하나로 합치기\n", "    EXTRA(h, W, D, Hw, zb, overhangs)\n    # 하나로 합치기\n")
_patch("DARK = lambda c: c * .52 + srgb((70, 52, 44)) * .18", "DARK = lambda c: c * .70 + srgb((70, 52, 44)) * .10")
_patch("            if w >= 9: dark |= I == w // 2  # 겹창 가운데 살\n", "            if w >= 9: dark |= I == w // 2  # 겹창 가운데 살\n            col, dark = GLASS(col, dark, I, Jj, w, hh, XID[r['i']])\n")
_patch("        elif role == 'stone':\n", "        elif role in XR: col, dark = XPAINT(role, col, dark, I, Jj, w, hh, XID[r['i']])\n        elif role == 'stone':\n")
_patch("    rects = []; swatch = {}\n", "    rects = []; swatch = {}; XID = np.zeros(len(me.polygons), np.int32)\n    if 'xid' in me.attributes: me.attributes['xid'].data.foreach_get('value', XID)\n")
_patch("base = np.clip(r['pc'], 0, 1) if role == 'prop' else", "base = np.clip(r['pc'], 0, 1) if role in ('prop',) + XR else")
_patch("col = np.broadcast_to(BASE.get(role, W_), u.shape + (3,)).copy()", "col = np.broadcast_to(np.clip(r['pc'], 0, 1) if role in XR else BASE.get(role, W_), u.shape + (3,)).copy()")
_patch("sh = (z - g['zb'] < .6) & (role == 'wall')", "sh = (z - g['zb'] < h.get('band', 0)) & (role == 'wall')")
_patch("    for a in ('role', 'pcol'): o.data.attributes.remove(o.data.attributes[a])", "    for a in [a for a in ('role', 'pcol', 'xid') if a in o.data.attributes]: o.data.attributes.remove(o.data.attributes[a])")
exec(compile(_src, 'opus_houses.py', 'exec'))

OUT = J('art', 'export', 'scene-a4-houses-e'); WORK = J('art', 'models', 'opus_houses_e'); os.makedirs(OUT, exist_ok=True); os.makedirs(WORK, exist_ok=True)
TOK.update(n1=('windows/single_windows/window_single_01.fbx', 'window', None), dd=('windows/double_windows/window_double_01.fbx', 'window', None))
XR = ('board', 'plank', 'tin'); ROLES += list(XR); RID.update({r: ROLES.index(r) for r in XR})
POTS = sorted(glob.glob(os.path.join(TP, 'valsekamerplant-plants-pots', '**', 'Plants', '*.glb'), recursive=True))
FACEV = dict(front=((0, -1, 0), (1, 0, 0)), right=((1, 0, 0), (0, 1, 0)), left=((-1, 0, 0), (0, -1, 0)), back=((0, 1, 0), (-1, 0, 0)))
# 창 유리를 0.07m 만 밀어도 모듈 안쪽 면에 가려 갈색으로 보임(실측) → 창은 0, 문만 밀기
DEP = [float(os.environ.get('E_DEP', '0')), .07]; C = {}; LG = {}; BOARDS = [None]; WINS = []

# ---------- 픽셀 글자 (행 = 위→아래) ----------
FONT = dict(F='###/#../##./#../#..', I='###/.#./.#./.#./###', S='.##/#../.#./..#/##.', H='#.#/#.#/###/#.#/#.#', R='##./#.#/##./#.#/#.#',
            O='###/#.#/#.#/#.#/###', M='#.#/###/###/#.#/#.#', E='###/#../##./#../###', P='##./#.#/##./#../#..', A='.#./#.#/###/#.#/#.#',
            T='###/.#./.#./.#./.#.', L='#../#../#../#../###', C='.##/#../#../#../.##', **{'7': '###/..#/.#./.#./.#.', ' ': '../../../../..',
            'サ': '.#.#./#####/.#.#./...#./..#..', 'カ': '.#.../#####/.#..#/.#..#/#..#.', 'ナ': '..#../#####/..#../.#.../#....'})


def glyphs(s, vert):
    gs = [np.array([[c == '#' for c in row] for row in FONT[ch].split('/')]) for ch in s]
    if vert:
        W_ = max(g.shape[1] for g in gs)
        return np.concatenate([np.pad(g, ((0, 1), ((W_ - g.shape[1]) // 2, W_ - g.shape[1] - (W_ - g.shape[1]) // 2))) for g in gs])[:-1]
    H_ = max(g.shape[0] for g in gs); return np.concatenate([np.pad(g, ((H_ - g.shape[0], 0), (0, 1))) for g in gs], 1)[:, :-1]


def GLASS(col, dark, I, Jj, w, hh, code):  # 1 커튼 양옆 · 2 창턱 화분 · 3 반커튼(카페) · 4 가게 진열 선반 · 5 어두운 안
    if code == 1: col[((I <= 1) | (I >= w - 2)) & (Jj >= hh // 3)] = srgb((236, 226, 204))
    elif code == 2: col[(Jj <= 2) & (np.abs(I - w // 2) <= 1 + (Jj <= 1))] = srgb((96, 140, 84))
    elif code == 3: col[Jj <= hh // 2] = srgb((240, 232, 214)); dark |= (Jj == hh // 2 + 1) & (I > 0) & (I < w - 1)
    elif code == 4:
        shelf = (Jj % 4 == 0) & (Jj > 0) & (Jj < hh - 1); col[shelf] = srgb((150, 112, 80))
        item = (Jj % 4 != 0) & (Jj % 4 != 3) & (Jj < hh - 2) & (Jj > 0) & (I > 0) & (I < w - 1)
        k = (I + 2 * (Jj // 4)) % 5; col[item & (k == 0)] = srgb((214, 128, 82)); col[item & (k == 2)] = srgb((190, 204, 210)); col[item & (k == 3)] = srgb((230, 196, 110))
    elif code == 5: col[:] = srgb((52, 60, 62))
    return col, dark


def XPAINT(role, col, dark, I, Jj, w, hh, xid):
    edge = (I == 0) | (I == w - 1) | (Jj == 0) | (Jj == hh - 1)
    if role == 'plank': dark |= edge | ((I % 3 == 2) & ~edge)
    elif role == 'tin': col[I % 2 == 1] *= .9; dark |= Jj == 0
    elif role == 'board':
        dark |= edge
        if 0 < xid < len(BOARDS) and min(w, hh) >= 5:
            text, ink, vert = BOARDS[xid]; m = glyphs(text, vert)[::-1]; mh, mw = m.shape
            if mh <= hh - 2 and mw <= w - 2:
                oy, ox = (hh - mh) // 2, (w - mw) // 2; sub = col[1 + oy:1 + oy + mh, 1 + ox:1 + ox + mw]; sub[m] = srgb(ink)
            else: print('BOARD SMALL', text, w, hh, mw, mh, flush=True)
    return col, dark


# ---------- 조각 도우미 ----------
_place0 = place


def place(path, kind, T):  # 창·문 자리를 기억(유리 밀기·덧창·검사용)
    n0 = len(PARTS); r = _place0(path, kind, T)
    if kind in ('window', 'door'): WINS.append(dict(path=path, kind=kind, T=T.copy(), objs=PARTS[n0:], louvre=None))
    elif kind == 'shutter' and WINS: WINS[-1]['louvre'] = 'open' if 'open' in path else 'closed'
    return r


def setc(o, col, xid=0):
    me = o.data; n = len(me.polygons); cc = np.ones((n, 4), np.float32); cc[:, :3] = np.array(col, np.float32)[:3] / 255
    me.attributes['pcol'].data.foreach_set('color', cc.ravel())
    a = me.attributes.get('xid') or me.attributes.new('xid', 'INT', 'FACE'); a.data.foreach_set('value', np.full(n, xid, np.int32)); return o


def mk2(v, f, role, col, xid=0):
    o = mk(v, f, role); bm = bmesh.new(); bm.from_mesh(o.data); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(o.data); bm.free()
    return setc(o, col, xid)


def bx(x0, x1, y0, y1, z0, z1, col, role='prop', xid=0):
    return setc(box(x0, x1, y0, y1, z0, z1, side=role, top=role, bottom=role if z0 > C['zb'] + .02 else 'hidden'), col, xid)


def beam(p0, p1, sw, sh, col, role='prop'):
    p0, p1 = np.array(p0, float), np.array(p1, float); d = (p1 - p0) / np.linalg.norm(p1 - p0); a = np.cross(d, (0, 0, 1.))
    a = a / np.linalg.norm(a) if np.linalg.norm(a) > 1e-6 else np.array((1., 0, 0)); b = np.cross(a, d)
    v = [p + sa * a * sw / 2 + sb * b * sh / 2 for p in (p0, p1) for sa, sb in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    return mk2(v, [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], role, col)


def fv(face):
    n, U = (np.array(a, float) for a in FACEV[face]); return n, U, (C['D'] if face in ('front', 'back') else C['W']) / 2 + .1


def fp(face, u, z, o):
    n, U, s = fv(face); p = U * u + n * (s + o); p[2] = z; return p


def fbox(face, u0, u1, z0, z1, o0, o1, col, role='prop', xid=0):
    P = np.array([fp(face, u, 0, o) for u in (u0, u1) for o in (o0, o1)])
    return bx(P[:, 0].min(), P[:, 0].max(), P[:, 1].min(), P[:, 1].max(), z0, z1, col, role, xid)


def log(_k, **kw): LG.setdefault(_k, []).append(kw)


def life(what, x, y): log('life', what=what, x=round(float(x), 2), y=round(float(y), 2))


def op(face, story, k=0, kind='window'):
    return [o for o in C['ops'] if o['face'] == face and o['story'] == story and o['kind'] == kind][k]


def openings(h):
    """모듈 창·문: 유리·문짝 면을 안으로 밀고(가장자리 이음 끊음), 안쪽 턱 4면 + 밖 창턱/문턱."""
    ops = []
    for wi in WINS:
        rot = round(math.degrees(math.atan2(wi['T'][1][0], wi['T'][0][0]))) % 360; face = {0: 'front', 90: 'right', 180: 'back', 270: 'left'}[rot]
        n, U, s_w = fv(face); want = RID['glass' if wi['kind'] == 'window' else 'door']; dep = DEP[0] if wi['kind'] == 'window' else DEP[1]; pts = []
        for o in wi['objs']:
            bm = bmesh.new(); bm.from_mesh(o.data); rl = bm.faces.layers.int.get('role'); sel = [f for f in bm.faces if f[rl] == want]
            if sel:
                ss = set(sel); bmesh.ops.split_edges(bm, edges=list({e for f in sel for e in f.edges if any(g_ not in ss for g_ in e.link_faces)}))
                vs = {v for f in sel for v in f.verts}; pts += [np.array(v.co) for v in vs]
                for v in vs: v.co -= Vector(n * dep)
                bm.to_mesh(o.data)
            bm.free()
        if not pts: continue
        P = np.array(pts); u0, u1 = float((P @ U).min()), float((P @ U).max()); z0, z1 = float(P[:, 2].min()), float(P[:, 2].max()); s = float((P @ n).max())
        ops.append(dict(face=face, kind=wi['kind'], u0=u0, u1=u1, z0=z0, z1=z1, objs=wi['objs'], louvre=wi['louvre'], own=False, story=int((z0 - C['zb'] + .3) // 3)))
        p = lambda u, z, o_: U * u + n * (s + o_) + np.array((0, 0, z)); rc = np.array(h['wall']) * .8
        for (a, b, want_n) in (((u0, z1), (u1, z1), (0, 0, -1)), ((u0, z0), (u1, z0), (0, 0, 1)), ((u0, z0), (u0, z1), U), ((u1, z0), (u1, z1), -U)):
            v = [p(a[0], a[1], 0), p(b[0], b[1], 0), p(b[0], b[1], -dep), p(a[0], a[1], -dep)]
            nn = np.cross(v[1] - v[0], v[3] - v[0]); f = [(0, 1, 2, 3) if nn @ np.array(want_n, float) > 0 else (3, 2, 1, 0)]
            setc(mk(v, f, 'prop'), rc)
        if wi['kind'] == 'window': fbox(face, u0 - .08, u1 + .08, z0 - .12, z0 - .01, 0, .13, (226, 220, 204))
        elif z0 < C['zb'] + .3: fbox(face, u0 - .1, u1 + .1, C['zb'], C['zb'] + .11, 0, .32, (190, 184, 166))
    ops.sort(key=lambda o: (o['face'], o['kind'], o['story'], o['u0']))
    sills = [o['z0'] - C['zb'] - 3 * o['story'] for o in ops if o['kind'] == 'window']; C['sill'] = float(np.median(sills)) if sills else 1.0
    C['ops'] = ops


def glass_code(o, code):
    for ob in o['objs']:
        me = ob.data; r = np.zeros(len(me.polygons), np.int32); me.attributes['role'].data.foreach_get('value', r)
        a = me.attributes.get('xid') or me.attributes.new('xid', 'INT', 'FACE'); x = np.zeros(len(r), np.int32); a.data.foreach_get('value', x)
        x[r == RID['glass']] = code; a.data.foreach_set('value', x)
    o['code'] = code


def newwin(face, uc, story, w, hg, dz, frame, code=1, tag='new'):
    z0 = C['zb'] + 3 * story + C['sill'] + dz; u0, u1 = uc - w / 2, uc + w / 2; t = .11; z1 = z0 + hg
    for a, b, c, d in ((u0 - t, u1 + t, z1, z1 + t), (u0 - t, u0, z0, z1), (u1, u1 + t, z0, z1)): fbox(face, a, b, c, d, 0, .12, frame)
    fbox(face, u0 - t - .05, u1 + t + .05, z0 - t, z0, 0, .16, frame)
    fbox(face, u0, u1, z0, z1, -.02, .01, (64, 100, 106), 'glass', code)
    C['ops'].append(dict(face=face, kind='window', u0=u0, u1=u1, z0=z0, z1=z1, objs=[], louvre=None, own=tag, story=story, code=code))


def patch(face, u0, u1, z0, z1, k, why):
    fbox(face, u0, u1, z0, z1, 0, .012, np.clip(np.array(C['h']['wall']) * k, 0, 255))
    log('patch', face=face, u0=u0, u1=u1, z0=z0, z1=z1, k=k, why=why)


def blocked(face, uc, story, w, hg, k):
    z0 = C['zb'] + 3 * story + C['sill']; patch(face, uc - w / 2, uc + w / 2, z0, z0 + hg, k, 'window')
    fbox(face, uc - w / 2 - .08, uc + w / 2 + .08, z0 - .12, z0 - .01, 0, .1, (214, 208, 192)); log('blocked', face=face, uc=uc, story=story)


def leaves(o, states, col):  # 판자 덧창: 0° 닫힘 · 115° 반열림 · 175° 열림
    n, U, s = fv(o['face']); lw = (o['u1'] - o['u0']) / 2 + .04; z0, z1 = o['z0'] - .06, o['z1'] + .06
    for side, st in zip((-1, 1), states):
        hu = o['u0'] - .04 if side < 0 else o['u1'] + .04; a = math.radians({'closed': 0, 'half': 115, 'open': 175}[st])
        d = U * (-side) * math.cos(a) + n * math.sin(a); H = U * hu + n * (s + .06); H[2] = (z0 + z1) / 2
        beam(H, H + d * lw, .05, z1 - z0, col, 'plank')
    log('shutter', type='plank', face=o['face'], story=o['story'], states=list(states))


def canopy(face, u0, u1, ztop, out, drop, col, tag):
    v = [fp(face, u0, ztop, 0), fp(face, u1, ztop, 0), fp(face, u1, ztop - drop, out), fp(face, u0, ztop - drop, out)]
    v += [p - np.array((0, 0, .06)) for p in v]; mk2(v, [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], 'tin', col)
    for u in (u0 + .15, u1 - .15): beam(fp(face, u, ztop - .55, 0), fp(face, u, ztop - drop - .05, out * .75), .05, .05, (90, 92, 90))
    P = np.array(v); C['oh'].append(('box', P[:, 0].min(), P[:, 0].max(), P[:, 1].min(), P[:, 1].max() + .2, ztop))
    log('add', kind=tag, face=face)


def board(face, u0, u1, z0, z1, text, bg, ink, o0=.02, o1=.1):
    BOARDS.append((text, ink, False)); fbox(face, u0, u1, z0, z1, o0, o1, bg, 'board', len(BOARDS) - 1); log('sign', text=text, face=face)


def downpipe(x, bottom=None):  # 처마 물받이 → 홈통 → 땅(또는 빗물통 위)
    W, D, Hw = C['W'], C['D'], C['Hw']; YF = -D / 2 - .1; zg = Hw - .17; ye = -D / 2 - .45; zb_ = C['zb'] if bottom is None else bottom
    bx(-W / 2 - .3, W / 2 + .3, ye, ye + .14, zg - .12, zg, (150, 154, 150))
    bx(x - .05, x + .05, YF - .12, YF - .02, zb_, zg - .45, (150, 154, 150)); beam((x, ye + .07, zg - .1), (x, YF - .07, zg - .47), .1, .1, (150, 154, 150))
    log('pipe', bottom=[x, YF - .07, zb_], top=[x, ye + .07, zg - .1], support_bottom=zb_, support_top='gutter z %.2f..%.2f' % (zg - .12, zg))


def pot(i, x, y, z, ht, rot=0):
    pp = POTS[i]; mes, lo, hi = load(pp, 'prop'); c = (lo + hi) / 2; s = ht / max(hi[2] - lo[2], 1e-3)
    place(pp, 'prop', Tm(x, y, z, rot, s) @ Matrix.Translation((-c[0], -c[1], -lo[2]))); log('pot', model=i, x=x, y=y, h=ht); life('pot', x, y)


def wheel(cx, y, cz, r, col=(58, 60, 64)):
    a = np.linspace(0, 2 * np.pi, 9)[:-1]; ring = [(cx + r * math.cos(q), cz + r * math.sin(q)) for q in a]
    v = [(px, y - .025, pz) for px, pz in ring] + [(px, y + .025, pz) for px, pz in ring]
    mk2(v, [tuple(range(8)), tuple(range(8, 16))] + [(i, (i + 1) % 8, 8 + (i + 1) % 8, 8 + i) for i in range(8)], 'prop', col)


def bike(x, y, col):
    zb_ = C['zb']
    for dx in (-.5, .5): wheel(x + dx, y, zb_ + .33, .33)
    for p0, p1 in (((x - .5, y, .33), (x, y, .72)), ((x, y, .72), (x + .5, y, .33)), ((x, y, .72), (x + .38, y, .85)), ((x - .05, y, .72), (x - .1, y, .92))):
        beam((p0[0], p0[1], zb_ + p0[2]), (p1[0], p1[1], zb_ + p1[2]), .05, .05, col)
    bx(x - .22, x + .02, y - .06, y + .06, zb_ + .92, zb_ + .97, (60, 50, 44)); life('bike', x, y)


def chair(x, y, col):
    zb_ = C['zb']; bx(x - .2, x + .2, y - .2, y + .2, zb_ + .42, zb_ + .47, col)
    bx(x - .2, x + .2, y - .2, y - .16, zb_, zb_ + .42, col); bx(x - .2, x + .2, y + .16, y + .2, zb_, zb_ + .92, col); life('chair', x, y)


def laundry(p0, p1, zt, cols):
    for p in (p0, p1): beam((p[0], p[1], C['lz']), (p[0], p[1], zt), .05, .05, (120, 116, 108))
    beam((p0[0], p0[1], zt - .03), (p1[0], p1[1], zt - .03), .02, .02, (200, 200, 196))
    for i, c in enumerate(cols):
        t0, t1 = (i + .25) / len(cols), (i + .8) / len(cols); a = np.array(p0) + (np.array(p1) - np.array(p0)) * t0; b = np.array(p0) + (np.array(p1) - np.array(p0)) * t1
        bx(min(a[0], b[0]), max(a[0], b[0]) + .02, min(a[1], b[1]) - .02, max(a[1], b[1]) + .02, zt - .55 - .1 * (i % 2), zt - .04, c)
    log('laundry_ends', ends=[list(p0) + [zt], list(p1) + [zt]], attach='poles'); life('laundry', (p0[0] + p1[0]) / 2, p0[1])


# ---------- 집 카드(docs/design/HOUSES.md) ----------
def card_narrow_a():
    W, D, YF = C['W'], C['D'], -C['D'] / 2 - .1; sc = (150, 112, 72)
    for k, st in ((0, ('open', 'open')),): leaves(op('front', 1, 0), st, sc)
    leaves(op('front', 2, 0), ('half', 'open'), sc); leaves(op('front', 2, 1), ('open', 'open'), sc)
    for o in C['ops']:
        if o['kind'] == 'window' and not o['own']: glass_code(o, 1 if o['face'] == 'front' else 2)
    blocked('front', 1.5, 0, .9, 1.35, .93)
    newwin('right', 2.0, 2, .6, .8, .33, (232, 226, 210), 2); newwin('right', 2.0, 0, .9, .7, .3, (232, 226, 210), 1)
    d = op('front', 0, 0, 'door'); canopy('front', d['u0'] - .25, d['u1'] + .25, d['z1'] + .5, .75, .2, (146, 156, 158), 'add_canopy')
    zt = op('front', 1, 0)['z0'] - .2; board('front', -W / 2 + .05, -W / 2 + 2.45, zt - .82, zt, 'ROOMS', (232, 222, 196), (96, 64, 48))
    bx(-.35, .4, YF - .9, YF, 2.85, 3.0, (232, 230, 218)); beam((-.33, YF - .86, 3.9), (.4, YF - .86, 3.9), .04, .04, (81, 98, 88))
    for xx in (-.33, .4): beam((xx, YF - .86, 3.0), (xx, YF - .86, 3.9), .04, .04, (81, 98, 88))
    log('add', kind='add_balcony_ext', face='front')
    C['lz'] = 3.0; laundry((-.25, YF - .8), (2.5, YF - .8), 4.85, [(236, 228, 210), (120, 150, 170), (204, 124, 100)])
    pot(3, 2.2, YF - .3, 3.0, .55)
    fbox('front', d['u1'] + .15, d['u1'] + .45, 1.0, 1.35, 0, .15, (176, 84, 64)); life('mailbox', d['u1'] + .3, YF)
    fbox('front', d['u0'] - .3, d['u0'] - .06, 1.9, 2.08, 0, .03, (236, 232, 220)); life('number', d['u0'] - .2, YF)
    downpipe(W / 2 - .15)
    patch('front', d['u1'] + .05, d['u1'] + .9, 0, .7, 1.07, 'ground'); patch('front', op('front', 1, 0)['u0'] - .05, op('front', 1, 0)['u1'] + .05, 3.15, op('front', 1, 0)['z0'] - .13, .94, 'under-sill')
    return dict(sag=.02, skew=3.0, side=1)


def card_narrow_b():
    W, D, Hw, YF = C['W'], C['D'], C['Hw'], -C['D'] / 2 - .1
    for o in C['ops']:
        if o['kind'] == 'window' and not o['own']: glass_code(o, 1 if o['story'] == 1 else 2)
    newwin('front', 1.0, 0, 1.5, 1.0, 0, (110, 136, 132), 4, 'shopglass'); newwin('front', 1.0, 2, .7, .5, .3, (226, 220, 204), 1)
    bx(-1.95, .75, -.3, 2.4, Hw + .2, Hw + 2.2, (226, 218, 198)); log('add', kind='add_roof_room', face='top')
    v = [(-2.05, -.45, Hw + 2.25), (.85, -.45, Hw + 2.25), (.85, 2.55, Hw + 2.5), (-2.05, 2.55, Hw + 2.5)]; v += [(a, b, c - .06) for a, b, c in v]
    mk2(v, [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], 'tin', (150, 158, 160))
    BOARDS.append(('REPAIR', (232, 226, 210), False)); bx(-2.0, .85, -.36, -.3, Hw + .9, Hw + 1.8, (70, 100, 104), 'board', len(BOARDS) - 1); log('sign', text='REPAIR', face='front')
    bx(-W / 2 - .22, -W / 2 - .12, 1.0, 1.1, 0, Hw + .6, (150, 154, 150)); beam((-W / 2 - .17, 1.05, Hw + .6), (-W / 2 + .02, 1.05, Hw + .6), .08, .08, (150, 154, 150))
    log('pipe', bottom=[-W / 2 - .17, 1.05, 0], top=[-W / 2 + .02, 1.05, Hw + .6], support_bottom=0, support_top='parapet z %.2f..%.2f' % (Hw, Hw + .75)); log('add', kind='add_side_pipe', face='left')
    bike(1.0, YF - .45, (60, 110, 150)); bike(1.35, YF - 1.05, (170, 70, 60))
    bx(-1.95, -1.55, YF - .45, YF - .05, 0, .42, (176, 84, 64), 'plank'); life('toolbox', -1.75, YF - .25)
    d = op('front', 0, 0, 'door'); fbox('front', d['u1'] + .02, d['u1'] + .24, 1.85, 2.02, 0, .03, (236, 232, 220)); life('number', d['u1'], YF)
    patch('front', d['u0'] - .05, d['u1'] + .05, d['z1'] + .05, d['z1'] + .32, 1.07, 'door'); patch('front', -1.95, -1.55, .42, 1.05, .93, 'ground')
    return dict(sag=.015, skew=3.0, side=-1)


def card_shop():
    W, D, YF = C['W'], C['D'], -C['D'] / 2 - .1; sc = (170, 100, 84)
    glass_code(op('front', 0, 0), 4); glass_code(op('front', 1, 0), 1); glass_code(op('front', 1, 1), 2)
    for o in C['ops']:
        if o['face'] != 'front' and o['kind'] == 'window': glass_code(o, 1)
    leaves(op('front', 1, 0), ('open', 'open'), sc); leaves(op('front', 1, 1), ('closed', 'closed'), sc)
    newwin('right', 2.0, 1, .5, .7, .27, (236, 230, 214), 2)
    canopy('front', -W / 2 + .1, W / 2 - .1, 2.85, 1.2, .3, (150, 162, 168), 'add_tin_canopy')
    board('front', -1.0, 1.0, 2.5, 3.18, 'FISH', (236, 230, 214), (40, 72, 110), 1.15, 1.23)
    BOARDS.append(('サカナ', (236, 230, 214), True)); bx(W / 2 - .16, W / 2 - .04, YF - .95, YF - .15, 3.1, 5.2, (176, 70, 56), 'board', len(BOARDS) - 1)
    beam((W / 2 - .1, YF, 4.9), (W / 2 - .1, YF - .2, 4.9), .04, .04, (81, 98, 88)); log('sign', text='サカナ', face='side-projecting'); log('add', kind='add_vertical_sign', face='front')
    bx(.2, 2.8, YF - 1.0, YF - .3, 0, .75, (150, 112, 80), 'plank')
    for i, x in enumerate((.3, 1.15, 2.0)):
        bx(x, x + .66, YF - .95, YF - .35, .75, .87, (234, 238, 238)); bx(x + .06, x + .6, YF - .89, YF - .41, .87, .89, [(176, 194, 206), (214, 128, 82), (150, 170, 190)][i])
    life('fish_display', 1.5, YF - .65)
    for i, (z0, c) in enumerate(((0, (234, 238, 238)), (.3, (226, 232, 234)), (.6, (234, 238, 238)))): bx(-.3 + .03 * i, .2 + .03 * i, YF - .55, YF - .15, z0, z0 + .29, c)
    life('styro_stack', 0, YF - .35); bx(-.95, -.6, YF - .5, YF - .15, 0, .35, (80, 120, 160)); life('bucket', -.8, YF - .3)
    downpipe(-W / 2 + .15)
    patch('front', -.35, .25, .9, 1.5, .93, 'ground-splash'); patch('front', -2.9, -2.5, 0, .55, 1.06, 'ground')
    return dict(sag=.02, skew=2.5, side=1)


def card_slope():
    W, D, zb, YF = C['W'], C['D'], C['zb'], -C['D'] / 2 - .1
    for o in C['ops']:
        if o['kind'] == 'window' and not o['own']: glass_code(o, 2 if o['face'] == 'front' else 1)
    blocked('front', -2.0, 1, .9, 1.35, .93)
    newwin('front', -2.5, 0, .45, .9, .3, (236, 232, 220), 1); newwin('right', 1.5, 1, 1.3, .8, 0, (236, 232, 220), 1)
    x0 = -W / 2 - 1.7; x1 = -W / 2 - .1
    bx(x0, x1, -1.4, 1.6, 0, zb, (190, 184, 160), 'stone'); bx(x0, x1, -1.4, 1.6, zb, zb + 2.2, (238, 236, 226))
    v = [(x0 - .2, -1.55, zb + 2.1), (x1, -1.55, zb + 2.55), (x1, 1.75, zb + 2.55), (x0 - .2, 1.75, zb + 2.1)]; v += [(a, b, c - .06) for a, b, c in v]
    mk2(v, [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], 'tin', (160, 120, 96))
    bx(x0 + .45, x0 + 1.15, -1.46, -1.4, zb, zb + 1.9, (120, 96, 72), 'plank'); log('add', kind='add_shed', face='left')
    d = op('front', 0, 0, 'door')
    for i, (x, y, ht) in enumerate(((d['u1'] + .15, YF - .3, .7), (d['u1'] + .55, YF - .22, .4), (d['u1'] + .38, YF - .72, .9), (-2.55, YF - .95, .5), (2.35, YF - 1.25, .35))):
        pot((0, 1, 2, 0, 1)[i], x, y, zb, ht, 40 * i)
    chair(-1.9, YF - 1.2, (170, 120, 84)); C['lz'] = zb; laundry((1.0, YF - 1.35), (2.7, YF - 1.35), zb + 1.8, [(236, 230, 214), (210, 150, 140)])
    fbox('front', d['u0'] - .4, d['u0'] - .1, zb + 1.0, zb + 1.35, 0, .15, (96, 128, 96)); life('mailbox', d['u0'] - .25, YF)
    downpipe(-W / 2 + .15)
    patch('front', 1.0, 1.9, zb, zb + .5, 1.07, 'ground')
    return dict(sag=.015, skew=4.0, side=1)


def card_warehouse():
    W, D, YF = C['W'], C['D'], -C['D'] / 2 - .1
    for o in C['ops']:
        if o['kind'] == 'window': glass_code(o, 5 if o['face'] == 'front' else 1)
    newwin('front', 0, 1, 2.6, 1.0, .25, (110, 112, 108), 0, 'wide'); newwin('right', 2.5, 1, .6, .6, .3, (110, 112, 108), 1)
    d0, d1 = op('front', 0, 0, 'door'), op('front', 0, 1, 'door')
    fbox('front', d1['u0'] - .05, d1['u1'] + .05, 0, d1['z1'] + .05, 0, .08, (164, 172, 174), 'tin'); fbox('front', d1['u0'] - .15, d1['u1'] + .15, d1['z1'] + .05, d1['z1'] + .4, 0, .3, (150, 158, 160))
    log('add', kind='add_roll_shutter', face='front')
    canopy('front', d0['u0'] - .2, d0['u1'] + .2, d0['z1'] + .55, 1.0, .25, (150, 140, 120), 'add_tin_canopy')
    board('front', -2.1, 2.1, 3.12, 3.85, 'ATELIER 7', (60, 64, 62), (236, 228, 200))
    for i in range(5): bx(-3.95 + .02 * i, -3.25 - .03 * i, YF - 1.6, YF - .1, .1 * i, .1 * i + .09, (186 - 6 * i, 150 - 4 * i, 104))
    life('wood_pile', -3.6, YF - .8)
    lx = .55
    for x in (lx, lx + .45): beam((x, YF - .9, 0), (x, YF - .05, 2.9), .05, .05, (150, 120, 84))
    for t in np.linspace(.12, .9, 8): beam((lx, YF - .9 + .85 * t, 2.9 * t), (lx + .45, YF - .9 + .85 * t, 2.9 * t), .04, .04, (150, 120, 84))
    life('ladder', lx + .2, YF - .5)
    bx(3.55, 3.95, YF - 1.3, YF - .5, 0, .7, (150, 112, 80), 'plank'); life('sawhorse', 3.75, YF - .9); pot(4, 3.75, YF - .3, 0, .8)
    downpipe(-W / 2 + .15)
    patch('front', d1['u0'] - .9, d1['u0'] - .15, 0, .8, .94, 'ground')
    return dict(sag=.025, skew=2.0, side=1)


def card_cafe():
    W, D, Hw, YF = C['W'], C['D'], C['Hw'], -C['D'] / 2 - .1
    glass_code(op('front', 0, 0), 3); log('add', kind='add_striped_awning', face='front')
    for o in C['ops']:
        if o['face'] != 'front' and o['kind'] == 'window': glass_code(o, 2)
    newwin('right', 1.5, 0, .55, .55, .3, (190, 112, 80), 1)
    BOARDS.append(('CAFE', (240, 232, 214), False)); bx(-1.0, 1.0, YF - .62, YF - .56, Hw + .05, Hw + .95, (140, 70, 52), 'board', len(BOARDS) - 1)
    for x in (-.8, .8): beam((x, YF - .52, Hw - .1), (x, YF - .52, Hw + .1), .05, .05, (81, 98, 88))
    log('sign', text='CAFE', face='front-roof')
    bx(-1.95, -1.25, YF - 1.75, YF - 1.05, .7, .75, (150, 112, 80)); bx(-1.63, -1.57, YF - 1.43, YF - 1.37, 0, .7, (81, 98, 88)); life('table', -1.6, YF - 1.4)
    chair(-2.35, YF - 1.4, (95, 128, 126)); chair(-.85, YF - 1.4, (95, 128, 126))
    beam((2.45, YF - .75, 0), (2.45, YF - .45, .85), .5, .03, (52, 72, 62)); beam((2.45, YF - .15, 0), (2.45, YF - .45, .85), .5, .03, (52, 72, 62)); life('menu_board', 2.45, YF - .45)
    for i, (x, y, ht) in enumerate(((-.4, YF - .3, .8), (-.15, YF - .62, .45), (-.68, YF - .55, .6))): pot((3, 4, 0)[i], x, y, 0, ht, 50 * i)
    bx(2.55, 2.95, YF - .5, YF - .1, 0, .8, (110, 96, 80), 'plank'); life('rain_barrel', 2.75, YF - .3)
    downpipe(2.75, bottom=.8)
    patch('front', 2.15, 2.8, 1.5, 2.2, 1.08, 'under-awning'); patch('front', -2.9, -2.3, 0, .45, .94, 'ground')
    return dict(sag=.01, skew=3.0, side=-1)


CARDS = dict(
    house_narrow_a=(card_narrow_a, dict(front=[['D', 'w1', 'w2'], ['n1', 'w1', 'b'], ['n1', 'w1', 'n1']], shutter=(150, 112, 72), door=(120, 84, 60), pots=0, band=0)),
    house_narrow_b=(card_narrow_b, dict(front=[['D', 'w2'], ['s', 'c'], ['S', 'w2']], shutter=(95, 128, 126), door=(70, 100, 104), pots=0, band=.6)),
    house_shop=(card_shop, dict(front=[['DD', 'dn'], ['n1', 'w2', 'n1']], shutter=(170, 100, 84), door=(150, 92, 72), awning=None, sign=None, vending=False, pots=0, band=0)),
    house_slope=(card_slope, dict(front=[['w1', 'D', 'w1', 's'], ['w2', 'w2', 'c']], shutter=(84, 120, 134), door=(84, 120, 134), pots=0, band=.6)),
    house_warehouse=(card_warehouse, dict(front=[['w1', 'GG', 'w1', 'GG'], ['w1'] * 8], side=[['w2', 'S', 'w1', 'w2'], ['w2', 'w2', 'w2', 'w1']], door=(120, 96, 72), pots=0, band=0)),
    house_cafe=(card_cafe, dict(shutter=(190, 112, 80), door=(150, 92, 64), pots=0, band=0)),
)


def deform(sag, skew, side):
    W, D, Hw = C['W'], C['D'], C['Hw']; zr = Hw + (.1 + D / 2 * math.tan(math.radians(18)) if C['h']['roof'] == 'gable' else .75); t = math.tan(math.radians(skew))
    for o in PARTS:
        me = o.data; co = np.zeros(len(me.vertices) * 3); me.vertices.foreach_get('co', co); co = co.reshape(-1, 3); x, y, z = co[:, 0].copy(), co[:, 1].copy(), co[:, 2].copy()
        co[:, 2] -= sag * W * (1 - np.clip(2 * x / W, -1, 1) ** 2) * np.clip((z - Hw) / (zr - Hw), 0, 1)
        f = np.clip((x + W / 2) / W, 0, 1) if side > 0 else np.clip((W / 2 - x) / W, 0, 1)
        co[:, 0] += side * t * np.clip(y + D / 2, 0, None) * f
        me.vertices.foreach_set('co', co.ravel()); me.update()


def EXTRA(h, W, D, Hw, zb, overhangs):
    C.clear(); C.update(W=W, D=D, Hw=Hw, zb=zb, h=h, oh=overhangs); LG.clear()
    openings(h); fn = CARDS[h['name']][0]; dfm = fn(); deform(**dfm)
    for o in PARTS:
        if 'xid' not in o.data.attributes: o.data.attributes.new('xid', 'INT', 'FACE')
    ops = [dict({k: (round(v, 3) if isinstance(v, float) else v) for k, v in o.items() if k != 'objs'}) for o in C['ops']]
    PLOG[h['name']] = dict(W=W, D=D, Hw=Hw, zb=zb, stories=len(h['front']), roof=h['roof'], sill=round(C['sill'], 3), deform=dfm, openings=ops,
                           wall=list(h['wall']), shutter_col=list(h['shutter']), band=h.get('band', 0), **{k: list(v) for k, v in LG.items()})
    WINS.clear()


# ---------- 굽기 ----------
PLOG = {}
bpy.ops.wm.read_factory_settings(use_empty=True)
built = []
for k, h in enumerate(HOUSES):
    h.update(CARDS[h['name']][1])
    o, g = build(h)
    img, cls, nf = pack_paint_c(o, h, g, k * 13 + 5)
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons); pts = np.array([v.co for v in o.data.vertices]); lo, hi = pts.min(0), pts.max(0)
    built.append((h, o, img, cls, g, tris, nf, lo, hi)); print('BUILT', h['name'], 'tris', tris, 'tex', img.shape[1], img.shape[0], flush=True)
pal = read_gpl(J('art', 'export', 'scene-a4-houses-c', 'palette32.gpl')); P0 = np.array(pal, np.float64)
vp = np.concatenate([b[2][b[3] != RID['hidden']] for b in built]) * 255
d2 = ((vp[:, None] - P0[None]) ** 2).sum(2); use = np.bincount(d2.argmin(1), minlength=32); err = np.sqrt(d2.min(1))
wp = vp[err > 6]; rng = np.random.default_rng(5); wp = wp[rng.choice(len(wp), min(60000, len(wp)), replace=False)]; cen = wp[rng.choice(len(wp), 10, replace=False)]
for _ in range(25):
    lab = ((wp[:, None] - cen[None]) ** 2).sum(2).argmin(1); cen = np.array([wp[lab == i].mean(0) if (lab == i).any() else cen[i] for i in range(10)])
pal = [pal[i] for i in sorted(np.argsort(-use)[:22])] + [tuple(int(round(v)) for v in c) for c in cen]
print('PAL e: keep 22, new 10, err>6 px %', round(float((err > 6).mean() * 100), 1), flush=True)
open(os.path.join(OUT, 'palette32.gpl'), 'w').write('GIMP Palette\nName: scene-a4-houses-e (c판 32 중 22 + e판 칠 10)\nColumns: 8\n#\n' + ''.join(f'{r} {g_} {b_} c{i}\n' for i, (r, g_, b_) in enumerate(pal)))
kit = dict(task='20261027-1406', note='도현 승인 전. F1/Assets 에 넣지 않음. Unity 임포트 미확인. 원점 = 집 바닥(축대 집은 축대 바닥) 가운데 — c판과 같은 이름·원점·바닥 크기.',
           axis='Blender: 정면 -Y, 위 +Z → FBX axis_forward -Z, axis_up Y, 1u = 1m', method=MYDOC.split('이번 방법 —')[1].split('출력:')[0].strip(),
           sun_dir=SUN.round(3).tolist(), palette='palette32.gpl', texels_per_m=DENS, cards='docs/design/HOUSES.md',
           license='CC0 — elegantcrow Retro Modular House(모듈) · valsekamerplant PSX house plants(화분). 자판기(CC-BY) 는 e판에서 뺌. 글자·소품은 직접 만든 상자.', files=[])
meas = {}
for h, o, img, cls, g, tris, nf, lo, hi in built:
    idx = nearest(img, pal); q = np.array(pal, np.float64)[idx] / 255
    png = os.path.join(OUT, h['name'] + '.png'); im = save_png(q, png); np.save(os.path.join(WORK, h['name'] + '_cls.npy'), cls[::-1])
    Lm = q @ np.array((.299, .587, .114)) * 255; vis = cls != RID['hidden']
    meas[h['name']] = dict(L_visible=round(float(Lm[vis].mean()), 1), L_wall=round(float(Lm[cls == RID['wall']].mean()), 1), speckle_1px_pct=round(speckle(idx), 3),
                           colors=int(len(np.unique(idx))), size=[img.shape[1], img.shape[0]], wall_colors=int(len(np.unique(idx[cls == RID['wall']]))), tris=int(tris))
    export(o, im, os.path.join(OUT, h['name'] + '.fbx'))
    kit['files'].append(dict(name=h['name'], fbx=h['name'] + '.fbx', png=h['name'] + '.png', note=h['note'], place=h['place'], size_m=[round(float(v), 2) for v in (hi - lo)],
                             footprint_m=[float(g['W']), float(g['D'])], wall_top_m=round(float(g['Hw']), 2), tris=int(tris), tex=[img.shape[1], img.shape[0]], wall=h['wall'], roof=h['roof_c']))
    print('MEAS', h['name'], meas[h['name']], flush=True)
kit['sources_used'] = sorted({os.path.relpath(p, TP).replace('\\', '/') for p in LIB})
json.dump(kit, open(os.path.join(OUT, 'kit.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(dict(files=meas), open(os.path.join(OUT, 'measure.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(PLOG, open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False, default=lambda v: v.tolist() if hasattr(v, 'tolist') else float(v))
print('DONE', flush=True)
