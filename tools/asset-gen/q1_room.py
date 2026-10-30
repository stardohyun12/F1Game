"""에셋 루프 #1 주인공 방 — 오프닝 첫 장면 (과제 20261028-0310, D-159/D-187). Blender bpy 절차 생성:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/q1_room.py
형태: 3.5×3.0m · 천장 2.4m, 나무 바닥 + 다다미 깔개, 회벽(아래 0.9m 연초록 판벽), 큰 미닫이 창(반쯤 열림, 밖 = 아침 만 판), 문 1.
가구: 침대(이불 개어 둠)·책상+의자(배달 조끼)·책장(카트 모형·작은 상장)·포스터 2(가상 리그 "MARE GP")·사진 4·라디오·달력(축제 날 동그라미)·잡지·공책·저금통.
생활 흔적(처음부터, 얼룩 없음): 문 옆 키 재기 연필 줄, 떼어 낸 포스터 테이프 자국, 보수 칠, 바닥 닳은 길, 모자 건 고리, 슬리퍼, 머그.
축: 창 = Blender -Y → FBX/Unity +Z. 피벗 = 바닥 가운데. 큰 면 32텍셀/m, 작은 물건 64텍셀/m. albedo(빛 없음, D-185) → palette53 양자화.
출력: art/export/q1-room/{room.fbx, room.png, palette53.gpl, measure.json} · 렌더 art/_review/2026-10-28-q1-room/{door,desk,window,back}_raw.png
"""
import bpy, bmesh, os, sys, json, math
import numpy as np
from mathutils import Vector, Matrix
R = 'D:/Github/F1Game/'; OUT = R + 'art/export/q1-room/'; RV = R + 'art/_review/2026-10-28-q1-room/'
for d in (OUT, RV): os.makedirs(d, exist_ok=True)
CD = [32]
bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene

# ---------- 팔레트: palette50(집 46 + 마트 4) + 방 3칸 ----------
PAL = []
for ln in open(R + 'art/export/q1-mart/palette50.gpl', encoding='utf-8'):
    p = ln.split()
    if len(p) >= 3 and all(t.isdigit() for t in p[:3]): PAL.append((tuple(int(t) for t in p[:3]), ' '.join(p[3:])))
NEW = [((196, 218, 222), 'room 아침 하늘 옅음'), ((150, 192, 210), 'room 아침 하늘'), ((218, 146, 132), 'room 저금통 분홍')]
PAL += NEW
with open(OUT + 'palette53.gpl', 'w', encoding='utf-8') as f:
    f.write('GIMP Palette\nName: q1-room 53 (palette50 + room 3)\nColumns: 8\n#\n')
    for c, n in PAL: f.write('%3d %3d %3d\t%s\n' % (*c, n))
P = np.array([c for c, _ in PAL], float)
C = dict(wall=(244, 235, 200), wain=(178, 209, 183), wainline=(135, 157, 150), rail=(161, 109, 66), skirt=(128, 80, 50), frame=(207, 209, 182),
         wood=(177, 148, 106), wood2=(158, 127, 78), wood3=(166, 148, 117), dkwood=(128, 80, 50), dkwood2=(102, 72, 43), ink=(146, 50, 42), dark=(50, 49, 43),
         char=(74, 77, 62), grey=(131, 134, 110), red=(186, 78, 60), orange=(214, 118, 52), yellow=(226, 192, 78), cream=(244, 235, 200), egg=(236, 202, 140),
         blue=(112, 120, 166), blue2=(92, 100, 142), teal=(62, 148, 171), sea=(98, 142, 141), sea2=(95, 128, 126), green=(100, 140, 98), green2=(80, 118, 80), green3=(62, 94, 66),
         sky1=NEW[0][0], sky2=NEW[1][0], pink=NEW[2][0], apricot=(231, 185, 140), straw=(191, 185, 123), straw2=(174, 166, 106), terra=(161, 109, 66))
def ramp(k, f): return tuple(int(min(255, round(v * f * (tt if f < 1 else 1)))) for v, tt in zip(C[k], (1, .965, .88)))
# 2차(과제 0406): 결 단계 — 큰 면 바탕색마다 어둠2·어둠1·밝음1 (사진 결이 양자화에서 살아남게)
for k in ['wall', 'wain', 'wainline', 'rail', 'skirt', 'wood', 'wood2', 'wood3', 'dkwood2', 'straw', 'straw2', 'green3', 'frame', 'egg', 'orange',
          'red', 'blue', 'blue2', 'green', 'green2', 'teal', 'char', 'pink', 'apricot']:  # 2318: 저금통 분홍·주둥이도 결 단계(없으면 단색 칸 39%)  # 2303: 낱개가 된 책·줄도 결 단계(제 색 어둠·밝음)가 있어야 단색 칸이 안 된다
    for f, t in ((.86, '어둠2'), (.93, '어둠1'), (1.05, '밝음1')):
        c = ramp(k, f)  # 어둠 단계는 황토 쪽(회색 얼룩 금지)
        if c not in [q for q, _ in PAL]: PAL.append((c, f'q1v2 결 {t} {k}'))
with open(OUT + 'palette_q1v2.gpl', 'w', encoding='utf-8') as f:
    f.write('GIMP Palette\nName: q1-room v2 (palette53 + 결 단계)\nColumns: 8\n#\n')
    for c, n in PAL: f.write('%3d %3d %3d\t%s\n' % (*c, n))
P = np.array([c for c, _ in PAL], float)

# ---------- 칠하기 도구 (A[v,u], v=0 아래, 단위 m — 밀도 CD[0]) ----------
def T(m): return int(round(m * CD[0]))
def rect(A, x0, x1, z0, z1, col):
    h, w = A.shape[:2]; A[max(T(z0), 0):min(T(z1), h), max(T(x0), 0):min(T(x1), w)] = col
def px(A, u0, u1, v0, v1, col):  # 텍셀 단위
    h, w = A.shape[:2]; A[max(v0, 0):min(v1, h), max(u0, 0):min(u1, w)] = col
def disk(A, cu, cv, r, col, ring=0):
    h, w = A.shape[:2]; yy, xx = np.mgrid[0:h, 0:w]; d = np.hypot(xx + .5 - cu, yy + .5 - cv); A[(d <= r) & (d > r - ring if ring else True)] = col
def grain(A, amt, seed, cell=6):
    rs = np.random.default_rng(seed); h, w = A.shape[:2]
    g = rs.uniform(-amt, amt, (h // cell + 2, w // cell + 2)); g = np.kron(g, np.ones((cell, cell)))[:h, :w]; A *= (1 + g)[..., None]
# 2차: CC0 재료 사진 결(art/source/q1-room-cc0, Poly Haven) — 명도 비율만: 큰 블러로 나눠 빛·그늘 평평 → 밀도로 BOX 축소 → 표준화 → 바탕색에 곱
PH = {'plaster': ('painted_plaster_wall', .7), 'plasterB': ('clay_plaster', .9),  # 3차: 벽 얼룩 1/3 크기(10~25cm), 두 사진 섞기
       'wood': ('fine_grained_wood', 1.2), 'tatami': ('tatami_mat', 1.8), 'fabric': ('rough_linen', 1.0), 'paper': ('painted_plaster_wall', .7)}
PHC = {}; PHUSE = set()
def ph_tile(key, d):
    if (key, d) in PHC: return PHC[key, d]
    fid, S = PH[key]; im = bpy.data.images.load(R + 'art/source/q1-room-cc0/' + fid + '.jpg'); W0, H0 = im.size
    a = np.empty(W0 * H0 * 4, np.float32); im.pixels.foreach_get(a); a = a.reshape(H0, W0, 4)[..., :3] @ np.array((.299, .587, .114), np.float32) + 1e-3; bpy.data.images.remove(im)
    fy, fx = np.fft.fftfreq(H0)[:, None], np.fft.fftfreq(W0)[None]; sg = W0 / 8
    low = np.real(np.fft.ifft2(np.fft.fft2(a) * np.exp(-2 * (math.pi * sg) ** 2 * (fx ** 2 + fy ** 2))))
    r = np.log(a / np.maximum(low, 1e-3)); n = max(4, int(round(S * d)))
    ey, ex = np.linspace(0, H0, n + 1).astype(int), np.linspace(0, W0, n + 1).astype(int)
    t = np.add.reduceat(np.add.reduceat(r, ey[:-1], 0), ex[:-1], 1) / np.outer(np.diff(ey), np.diff(ex))
    t = t - t.mean(); gy, gx = np.fft.fftfreq(n)[:, None], np.fft.fftfreq(n)[None]; bs = BL * min(d, 32) / 32  # 2318: 64px/m 작은 물건도 2px 덩어리(옛 4px = 한 면이 한 색 → 잔결 0.01). 결을 얼룩 덩어리로(기준 화면 집 벽처럼)
    t = np.real(np.fft.ifft2(np.fft.fft2(t) * np.exp(-2 * (math.pi * bs) ** 2 * (gx ** 2 + gy ** 2))))
    PHC[key, d] = np.clip((t - t.mean()) / (t.std() + 1e-6), -2.5, 2.5); return PHC[key, d]
def ph_lay(key, h, w, rs, rot=0):
    t = np.rot90(ph_tile(key, CD[0]), rot); PHUSE.add(PH[key][0])
    t = np.roll(t, (rs.integers(t.shape[0]), rs.integers(t.shape[1])), (0, 1)); return np.tile(t, (h // t.shape[0] + 1, w // t.shape[1] + 1))[:h, :w]
def photo(A, key, amp, seed, mix=None):  # mix: 다른 사진을 90° 돌려 다른 주기로 섞음 → 같은 무늬 반복이 안 보임
    h, w = A.shape[:2]; rs = np.random.default_rng(seed); t = ph_lay(key, h, w, rs)
    if mix: t = (t + ph_lay(mix, h, w, rs, 1)) / math.sqrt(2)
    A *= (1 + amp * t)[..., None]; return t
AMP = float(os.environ.get('AMP', .45)); BL = float(os.environ.get('BL', 2.0)); BL = float(os.environ.get('BL', 2.0))
def ph_of(pt):
    for keys, v in ((('hook', 'shelf'), ('wood', .26)), (('pillow', 'shirt', 'shoe', 'sole'), ('fabric', .26)), (('pencil', 'metal'), ('plaster', .26)), (('desk', 'drawers'), ('wood', .26)),  # 2318: 가장자리 번짐 뒤에도 잔결 0.02 → ±12%(D-197 위쪽)
                    (('cord', 'vest', 'strap', 'slipper', 'bag', 'towel'), ('fabric', .18)), (('pig', 'snout', 'kart', 'tire'), ('plaster', .18)), (('book',), ('paper', .18)),
                    # 2303: 낱개 작은 물건 결 ±8%(D-197 아래쪽) — 옛 ±3.6% 는 양자화에서 단색 · 2318: 개어 둔 셔츠(잔결 0.025)·저금통 도자기도
                    (('floor', 'ceiling', 'beam', 'doorframe', 'winframe', 'sill', 'desk', 'drawers', 'chair', 'shelf', 'bedwood', 'hook', 'radio'), ('wood', .08)),
                    (('tatami',), ('tatami', .10)), (('reveal',), ('plaster', .07)),
                    (('sheet', 'pillow', 'futon', 'curtain', 'towel', 'shirt', 'vest', 'slipper', 'bag', 'shoe'), ('fabric', .08)),
                    (('poster', 'calendar', 'magazine', 'notebook', 'cert', 'box', 'photo', 'book', 'plate', 'socket'), ('paper', .05))):
        if pt.startswith(keys): return v[0], v[1] * AMP
F5 = {'M': ['10001', '11011', '11111', '10101', '10001', '10001', '10001'], 'A': ['01110', '10001', '10001', '10001', '11111', '10001', '10001'],
      'R': ['11110', '10001', '10001', '11110', '10010', '10011', '10001'], 'E': ['11111', '10000', '10000', '11110', '10000', '10000', '11111'],
      'G': ['01110', '10001', '10000', '10111', '10001', '10001', '01110'], 'P': ['11110', '10001', '10001', '11110', '10000', '10000', '10000'],
      'C': ['01111', '10000', '10000', '10000', '10000', '10000', '01111'], 'D': ['11110', '10001', '10001', '10001', '10001', '10001', '11110'],
      'Y': ['10001', '10001', '11011', '01110', '00100', '00100', '00100'], ' ': ['00000'] * 7}
F3 = {'R': ['110', '101', '110', '101', '101'], 'A': ['111', '101', '111', '101', '101'], 'C': ['011', '100', '100', '100', '011'], 'E': ['111', '100', '110', '100', '111'],
      'D': ['110', '101', '101', '101', '110'], 'Y': ['101', '101', '010', '010', '010'], ' ': ['000'] * 5}
def text(A, s, u0, vtop, font, cell, col, gap=1):
    gw = len(font[s[0]][0])
    for i, ch in enumerate(s):
        for r, row in enumerate(font[ch]):
            for c, b in enumerate(row):
                if b == '1': u = u0 + i * (gw * cell + gap) + c * cell; v = vtop - (r + 1) * cell; px(A, u, u + cell, v, v + cell, col)
def twidth(s, font, cell, gap=1): return len(s) * len(font[s[0]][0]) * cell + (len(s) - 1) * gap
def car(A, u, v, s, body, num=True):  # 옆모습 오픈휠 경주차(가상), u,v = 왼쪽 아래 텍셀
    S = lambda a: int(round(a * s))
    px(A, u + S(1), u + S(18), v + S(1.6), v + S(3.4), body); px(A, u + S(6), u + S(12), v + S(3.4), v + S(4.6), body)
    px(A, u + S(18), u + S(21), v + S(1.6), v + S(2.6), body); px(A, u + S(19), u + S(22), v + S(.8), v + S(1.4), C['char'])
    px(A, u, u + S(1.2), v + S(3), v + S(6.2), C['char']); px(A, u - S(.6), u + S(3), v + S(5.6), v + S(6.4), body)
    disk(A, u + S(3.5), v + S(2.2), S(2.2), C['char']); disk(A, u + S(15.5), v + S(2), S(2), C['char'])
    disk(A, u + S(3.5), v + S(2.2), max(1, S(.8)), C['grey']); disk(A, u + S(15.5), v + S(2), max(1, S(.7)), C['grey'])
    disk(A, u + S(9.2), v + S(5.2), S(1.1), C['yellow'])
    if num: disk(A, u + S(12.3), v + S(2.6), S(1), C['cream'])

# ---------- 벽 (전체 벽을 한 장으로 칠해 두고 조각 면이 잘라 씀) ----------
WD = {'win': (Vector((1.75, -1.5, 0)), Vector((-1, 0, 0)), Vector((0, 1, 0)), 3.5), 'door': (Vector((-1.75, 1.5, 0)), Vector((1, 0, 0)), Vector((0, -1, 0)), 3.5),
      'L': (Vector((-1.75, -1.5, 0)), Vector((0, 1, 0)), Vector((1, 0, 0)), 3.0), 'R': (Vector((1.75, 1.5, 0)), Vector((0, -1, 0)), Vector((-1, 0, 0)), 3.0)}
HH = 2.4
def wall_common(A, seed):
    A[:] = C['wall']  # 3차: 6텍셀 네모 grain 뺌(네모 얼룩의 원인)
    rect(A, 0, 9, 0, .9, C['wain'])
    for u in np.arange(.15, A.shape[1] / 32, .15): rect(A, u, u + 1 / 32, .08, .88, C['wainline'])
    rect(A, 0, 9, .87, .93, C['rail']); rect(A, 0, 9, 0, .08, C['skirt'])
def wall_paint(name):
    CD[0] = 32; L = WD[name][3]; A = np.zeros((T(HH), T(L), 3)); wall_common(A, len(name) * 3)
    if name == 'win':  # 창 왼쪽 위 보수 칠(손질된 낡음)
        rect(A, 2.95, 3.4, .35, .6, C['frame'])  # 침대 머리 쪽 판벽 다시 칠한 칸
    if name == 'door':  # 문(벽에 칠함, 틀·손잡이는 기하) + 키 재기 연필 줄 + 떼어 낸 포스터 테이프 자국
        rect(A, 2.35, 3.15, 0, 1.95, C['wood2']); rect(A, 2.45, 3.05, .2, .9, C['wood']); rect(A, 2.45, 3.05, 1.05, 1.82, C['wood'])
        rect(A, 2.47, 3.03, .22, .25, C['dkwood']); rect(A, 2.47, 3.03, 1.07, 1.1, C['dkwood'])
        for z in (1.12, 1.31, 1.47): rect(A, 2.2, 2.29, z, z + 1 / 32, C['dkwood2'])
    # 2차: 테이프 자국 지움, 스위치·콘센트는 64 물체로(아래 기하)
    if name == 'door':  # 2303: 문짝 결 = 나무 사진, 단계 어둠1(-7%)·밝음1(+5%)만 → D-197 아래쪽 ±8% 안(옛 grain_1736 export 덧칠 대체)
        dv = A[:T(1.95), T(2.35):T(3.15)]; tw = ph_lay('wood', dv.shape[0], dv.shape[1], np.random.default_rng(31))
        for k in ('wood', 'wood2', 'dkwood', 'dkwood2'):
            mk = (dv == C[k]).all(-1); dv[mk & (tw > .9)] = ramp(k, 1.05); dv[mk & (tw < -.9)] = ramp(k, .93)
    m = (A == C['wall']).all(-1); t = photo(A, 'plaster', .075 * AMP * .5, len(name) * 7, mix='plasterB')  # 3차: 대비 절반(은은한 결, 손질된 낡은 칠)
    A[m] = C['wall']; A[m & (t > 1.75)] = ramp('wall', 1.05); A[m & (t < -2.1)] = ramp('wall', .93)  # 밝은 칸 위주(기준 집 벽처럼 거의 평평), 어두운 점은 드물게  # 윗벽은 제 단계(크림 3단)만 — 차가운 회색 얼룩 금지
    return np.clip(A, 0, 255)
WCACHE = {}
def wall_crop(name, u0, z0, h, w):
    if name not in WCACHE: WCACHE[name] = wall_paint(name)
    F = WCACHE[name]; CD[0] = 32; a = F[T(z0):T(z0) + h, T(u0):T(u0) + w]
    return np.pad(a, ((0, h - a.shape[0]), (0, w - a.shape[1]), (0, 0)), mode='edge')

# ---------- 칠하기: 큰 면·가구 (32) ----------
def p_flat(col, amt=.03, seed=7, lines=None, vert=False):
    def f(A):
        A[:] = col; grain(A, amt, seed)
        if lines:
            if vert: A[:, ::lines[0]] = lines[1]
            else: A[::lines[0]] = lines[1]
    return f
def p_floor(A):  # 판자가 Y(창→문) 방향, 폭 .2, 판 끝 이음 어긋남, 문→책상 닳은 길(밝은 판만)
    h, w = A.shape[:2]; rs = np.random.default_rng(3); cyc = [C['wood'], C['wood3'], C['wood'], C['wood2'], C['wood3']]
    bw = T(.2)
    for k, u in enumerate(range(0, w, bw)):
        v = -rs.integers(0, 40)
        while v < h:
            ln = rs.integers(40, 70); col = cyc[(k * 2 + v // 7) % len(cyc)]
            uc = (u + bw / 2) / 32 - 1.75; path = .55 < uc < 1.45  # 문 앞(x .6~1.4) 줄은 닳아 밝은 판만
            if path and col == C['wood2']: col = C['wood']
            px(A, u, u + bw, max(v, 0), v + ln, col); px(A, u, u + bw, max(v, 0), max(v, 0) + 1, C['dkwood'] if v > 0 else col); v += ln
        px(A, u, u + 1, 0, h, C['dkwood'])
def p_ceiling(A):
    h, w = A.shape[:2]; A[:] = C['wood3']; grain(A, .02, 5)
    for v in range(0, h, T(.25)): A[v] = C['wood2']
def p_tatami(A):
    h, w = A.shape[:2]; A[:] = C['straw']
    for v in range(0, h, 2): A[v] = C['straw2']
    b = 3; A[:b] = C['green3']; A[-b:] = C['green3']; A[:, :b] = C['green3']; A[:, -b:] = C['green3']
    A[h // 2:h // 2 + 1, b:-b] = C['straw2']  # 두 장 이음
def p_view(A):  # 창밖 아침 만: 하늘 계단 + 계단 구름 + 바다 + 오른쪽 곶·등대 + 아래 지붕 줄 (밀도 16)
    h, w = A.shape[:2]; CD[0] = 16
    hz = T(1.45 + 1.6)
    for v in range(hz, h): A[v] = C['sky1'] if v < hz + T(.9) else C['sky2']
    rs = np.random.default_rng(4)
    for cu, cv, rw, rh in ((40, hz + 30, 22, 5), (150, hz + 50, 30, 6), (175, hz + 44, 14, 4), (60, hz + 62, 12, 3), (205, hz + 22, 16, 3)):
        for v in range(-rh, rh + 1):
            hw = int(rw * math.sqrt(max(0, 1 - (v / (rh + .5)) ** 2)) / 2) * 2; px(A, cu - hw, cu + hw, cv + v, cv + v + 1, C['cream'])
    px(A, 0, w, hz - T(1.1), hz, C['sea'])
    for v in range(hz - T(1.1), hz, 3): px(A, 0, w, v, v + 1, C['sea2'])
    for k in range(9): u = 70 + k * 6 + rs.integers(0, 4); vv = hz - 3 - k * 2; px(A, u, u + 4 + k, vv, vv + 1, C['sky1'])  # 햇빛 반짝 줄
    px(A, 0, w, hz - 1, hz, C['sky1'])
    for u in range(0, 70):  # 곶(왼쪽 = 화면 오른쪽 멀리) 계단 언덕
        top = hz + int(10 * math.sin(min(1, u / 60) * math.pi * .55)) - u // 12; px(A, u, u + 1, hz - 2, top, C['green2'])
    px(A, 30, 33, hz + 4, hz + 13, C['cream']); px(A, 30, 33, hz + 7, hz + 9, C['red']); px(A, 29, 34, hz + 13, hz + 15, C['red'])  # 등대
    base = T(-.2 + 1.6)  # 지붕 줄(비탈 아래 이웃집들)
    u = 0
    while u < w:
        bw = int(rs.integers(16, 30)); top = base + int(rs.integers(-6, 6)); wc = [C['cream'], C['apricot'], C['egg'], C['frame']][rs.integers(4)]
        px(A, u, u + bw, 0, top, wc)
        for k in range(3): px(A, u + 3 + k * 6, u + 6 + k * 6, top - 12, top - 7, C['sea2'])
        for k in range(6): px(A, u - 1 + k, u + bw + 1 - k, top + k, top + k + 1, C['terra'] if k % 2 == 0 else C['dkwood'])
        if rs.random() < .4: px(A, u + bw - 6, u + bw - 3, top + 4, top + 9, C['apricot'])
        u += bw
    for cu in (100, 230):  # 나무 머리
        for v in range(7): hw = 7 - abs(v - 3); px(A, cu - hw, cu + hw, base + 2 + v, base + 3 + v, C['green'] if v > 2 else C['green2'])
    CD[0] = 32
def p_curtain(A):
    A[:] = C['wain']; A[:, ::3] = C['wainline']
def p_bedwood(A): A[:] = C['terra']; A[::4] = C['dkwood']
def p_sheet(A): A[:] = C['frame']; A[:2] = C['wain']  # 2차: 바랜 크림 홑청 + 연초록 단
def p_futonend(A):  # 개어 둔 끝: 세 겹 접힌 줄
    h = A.shape[0]; A[:] = C['wain']
    for f in (1 / 3, 2 / 3): A[int(h * f)] = C['wainline']
def p_vestbody(A):  # 조끼 몸판: 반사 띠 2줄 + 주머니 + 앞 지퍼
    h, w = A.shape[:2]; A[:] = C['orange']
    A[int(h * .45):int(h * .45) + 2] = C['cream']  # 반사 띠 1줄(2줄이면 콘처럼 읽힘)
    px(A, 3, w // 2 - 3, 2, int(h * .18), C['terra']); px(A, 4, w // 2 - 4, 3, int(h * .18) - 1, C['orange'])
    px(A, w // 2, w // 2 + 1, 0, h, C['terra'])
def p_vestsh(A): A[:] = C['orange']; A[:, :1] = C['terra']; A[:, -1:] = C['terra']
def p_plate(A): A[:] = C['frame']; h, w = A.shape[:2]; px(A, w // 2 - 1, w // 2 + 1, h // 2 - 2, h // 2 + 2, C['egg'])
def p_bag(A): h, w = A.shape[:2]; A[:] = C['blue2']; A[:, w // 2:w // 2 + 1] = C['blue']  # 3차 생활 물건: 책가방
def p_bagflap(A): h, w = A.shape[:2]; A[:] = C['blue']; A[:, w // 4:w // 4 + 2] = C['egg']; A[:, 3 * w // 4 - 2:3 * w // 4] = C['egg']
def p_shoe(A): h, w = A.shape[:2]; A[:] = C['red']; A[h // 2:h // 2 + 2] = C['cream']  # 운동화(줄 하나, 로고 없음)
def p_leaf(A): h, w = A.shape[:2]; A[:] = C['green']; A[:, w // 2] = C['green2']; A[:2] = C['green2']
def p_socket(A): A[:] = C['frame']; h, w = A.shape[:2]; px(A, w // 3 - 1, w // 3, h // 2 - 1, h // 2 + 1, C['grey']); px(A, 2 * w // 3, 2 * w // 3 + 1, h // 2 - 1, h // 2 + 1, C['grey'])  # 2303: 구멍 회색(6×5px 판에 검정 점 = 잔결 0.35)
def p_futon(c1, c2, check=True):
    def f(A):
        A[:] = c1
        if check: A[::5] = c2; A[:, ::5] = c2
        else: A[:2] = c2; A[-2:] = c2
    return f
def p_desk(A): A[:] = C['wood']; A[::5] = C['wood3']
def p_drawers(A):
    h, w = A.shape[:2]; A[:] = C['wood']
    for v in (T(.22), T(.45)): A[v:v + 1] = C['dkwood']
    for v in (T(.12), T(.34), T(.57)): px(A, w // 2 - 1, w // 2 + 2, v, v + 1, C['char'])

# ---------- 칠하기: 작은 물건 (64) ----------
def p_poster1(A):  # 가상 리그 MARE GP 세로 포스터
    h, w = A.shape[:2]; A[:] = C['sky2']
    px(A, 0, w, h - 12, h, C['cream']); tw = twidth('MARE', F5, 1); text(A, 'MARE', (w - tw) // 2, h - 3, F5, 1, C['ink'])
    disk(A, w - 9, h - 20, 5, C['yellow'])
    tw = twidth('GP', F5, 2, 2); text(A, 'GP', (w - tw) // 2 - 4, h - 15, F5, 2, C['cream'], 2)
    px(A, 0, w, 0, 13, C['grey']); px(A, 0, w, 11, 13, C['cream'])
    for u in range(0, w, 6): px(A, u, u + 3, 11, 13, C['red'])
    car(A, 6, 9, 1.45, C['red']); px(A, 0, w, 0, 3, C['ink'])
    A[:, :1] = C['cream']; A[:, -1:] = C['cream']
def p_poster2(A):  # 레이스 사진 포스터(가로): 관중석·트랙·차 둘·RACE DAY
    h, w = A.shape[:2]; A[:] = C['grey']; rs = np.random.default_rng(8)
    px(A, 0, w, h - 13, h, C['char'])
    for v in range(h - 12, h - 1, 3):
        for u in range(1, w - 2, 3): px(A, u, u + 2, v, v + 2, [C['red'], C['cream'], C['blue'], C['yellow'], C['egg']][rs.integers(5)])
    px(A, 0, w, h - 15, h - 13, C['cream'])
    for u in range(0, w, 6): px(A, u, u + 3, h - 15, h - 13, C['red'])
    car(A, 6, 14, 1.0, C['red']); car(A, 32, 12, 1.0, C['blue'])
    px(A, 0, w, 0, 11, C['cream']); text(A, 'RACE DAY', (w - twidth('RACE DAY', F5, 1)) // 2, 9, F5, 1, C['ink'])
def p_calendar(A):  # 위 바다 그림, 아래 7×5 칸, 축제 날 빨간 동그라미
    h, w = A.shape[:2]; A[:] = C['cream']; px(A, 0, w, h - 11, h, C['teal']); px(A, 0, w, h - 11, h - 8, C['sea']); px(A, 8, 14, h - 8, h - 6, C['cream'])
    px(A, 0, w, h - 13, h - 11, C['red']); px(A, w // 2 - 1, w // 2 + 1, h - 1, h, C['dark'])
    for r in range(5):
        for c in range(7): v = h - 17 - r * 3; px(A, 1 + c * 3, 3 + c * 3, v, v + 1, C['red'] if c == 6 else C['grey'])
    cu, cv = 1 + 4 * 3 + 1, h - 17 - 3 * 3
    px(A, cu - 3, cu + 3, cv - 2, cv - 1, C['ink']); px(A, cu - 3, cu + 3, cv + 2, cv + 3, C['ink']); px(A, cu - 3, cu - 2, cv - 2, cv + 3, C['ink']); px(A, cu + 2, cu + 3, cv - 2, cv + 3, C['ink'])
def p_photo(k):
    def f(A):
        h, w = A.shape[:2]; A[:] = C['cream']
        if k == 0: px(A, 1, w - 1, 1, h - 1, C['sky1']); px(A, 1, w - 1, 1, h // 2, C['teal']); px(A, 2, 6, h // 2, h // 2 + 2, C['cream'])
        if k == 1: px(A, 1, w - 1, 1, h - 1, C['grey']); px(A, 1, w - 1, h - 4, h - 1, C['green']); px(A, 2, w - 2, 3, 6, C['red'])
        if k == 2: px(A, 1, w - 1, 1, h - 1, C['sky2']); px(A, 2, w - 2, 1, 5, C['orange']); px(A, 3, w - 3, 5, 8, C['egg'])
        if k == 3: px(A, 1, w - 1, 1, h - 1, C['egg']); px(A, 1, w - 1, 1, 4, C['grey']); px(A, 2, 7, 4, 6, C['blue'])
    return f
def p_clock(A):
    h, w = A.shape[:2]; A[:] = C['wall']; disk(A, w / 2, h / 2, w / 2, C['dkwood']); disk(A, w / 2, h / 2, w / 2 - 1.5, C['cream'])
    c = w // 2; px(A, c, c + 1, c, c + 6, C['dark']); px(A, c, c + 4, c, c + 1, C['dark'])
def p_magazine(A):
    h, w = A.shape[:2]; A[:] = C['cream']; px(A, 0, w, h - 5, h, C['red']); px(A, 2, w - 2, h - 3, h - 2, C['cream']); px(A, 0, w, 3, h - 6, C['sky2'])
    car(A, 1, 4, .5, C['orange'], num=False); px(A, 1, w - 3, 1, 2, C['grey'])
def p_notebook(A):  # 왼쪽: 랩 기록 낙서 줄, 오른쪽: 코스 그림
    h, w = A.shape[:2]; A[:] = C['cream']; px(A, w // 2, w // 2 + 1, 0, h, C['frame'])
    for i, v in enumerate(range(h - 3, 2, -3)): px(A, 2, 5, v, v + 1, C['blue2']); px(A, 6, 6 + 3 + (i * 5) % 4, v, v + 1, C['grey'])
    px(A, 3, 10, 6, 7, C['ink'])  # 밑줄 친 가장 빠른 랩
    cu = w * .75; disk(A, cu, h / 2, 5.5, C['grey'], 1.2); px(A, int(cu) + 3, int(cu) + 7, int(h / 2) - 1, int(h / 2) + 1, C['cream']); px(A, int(cu) - 1, int(cu) + 1, int(h / 2) + 4, int(h / 2) + 7, C['red'])
def p_cert(A):
    h, w = A.shape[:2]; A[:] = C['cream']; A[:1] = C['terra']; A[-1:] = C['terra']; A[:, :1] = C['terra']; A[:, -1:] = C['terra']
    px(A, 3, w - 3, h - 4, h - 3, C['ink'])
    for v in (h - 7, h - 9): px(A, 3, w - 4, v, v + 1, C['grey'])
    disk(A, w - 4, 3.5, 2, C['red'])
def p_radio_front(A):
    h, w = A.shape[:2]; A[:] = C['dkwood']; px(A, 2, int(w * .58), 2, h - 2, C['frame'])
    for v in range(3, h - 2, 2): px(A, 2, int(w * .58), v, v + 1, C['char'])
    px(A, int(w * .64), w - 2, h - 6, h - 2, C['egg']); px(A, int(w * .64) + 4, int(w * .64) + 5, h - 6, h - 2, C['red'])
    for u in (int(w * .66), w - 5): px(A, u, u + 3, 2, 5, C['char'])
def p_pencil(tip_hi):  # 2318: 노란 몸통 + 깎은 나무 2px + 심 1px(오른쪽 끝 x1) — back 면은 U 가 반대라 tip_hi=False
    def f(A):
        h, w = A.shape[:2]; A[:] = C['yellow']; s = slice(w - 3, w - 1) if tip_hi else slice(1, 3); A[:, s] = C['apricot']; A[:, w - 1 if tip_hi else 0] = C['dark']
    return f
def p_book(k):
    cols = [C['red'], C['blue'], C['green'], C['egg'], C['teal'], C['orange'], C['blue2'], C['terra'], C['green2']]
    def f(A): h, w = A.shape[:2]; A[:] = cols[k % len(cols)]; px(A, 0, w, h - 4, h - 3, C['cream']); px(A, 0, w, 2, 3, C['cream'])
    return f
def p_vest(A):  # 배달 조끼: 주황 + 반사 띠 2줄 + 파란 어깨 패치
    h, w = A.shape[:2]; A[:] = C['orange']
    for z in (.10, .22): rect(A, 0, 9, z, z + .035, C['cream'])
    rect(A, .05, .13, .28, .34, C['blue'])
def p_kart(A): A[:] = C['red']; h, w = A.shape[:2]; px(A, w // 2 - 1, w // 2 + 2, 0, h, C['cream'])
def p_mugtop(A): A[:] = C['blue']; h, w = A.shape[:2]; px(A, 1, w - 1, 1, h - 1, C['dkwood2']); px(A, 1, 3, 1, 3, C['yellow']); px(A, 3, 5, 2, 4, C['red'])
def p_pigtop(A): A[:] = C['pink']; h, w = A.shape[:2]; px(A, w // 2 - 2, w // 2 + 2, h // 2, h // 2 + 1, C['dkwood2'])
def p_cap(A): A[:] = C['blue2']; h, w = A.shape[:2]; px(A, w // 2 - 2, w // 2 + 2, h // 2 - 1, h // 2 + 2, C['cream'])

PT = dict(floor=p_floor, ceiling=p_ceiling, tatami=p_tatami, view=p_view, curtain=p_curtain, reveal=p_flat(C['wall'], .02, 3), sill=p_flat(C['wood'], .02, 4, (4, C['wood3'])),
          winframe=p_flat(C['terra'], 0, 1), doorframe=p_flat(C['dkwood'], 0, 1), beam=p_flat(C['dkwood'], 0, 1, (5, C['dkwood2'])), metal=p_flat(C['egg'], 0, 1, (2, ramp('egg', .86))),
          bedwood=p_bedwood, sheet=p_sheet, pillow=p_futon(C['egg'], C['frame'], False), futon=p_futon(C['wain'], C['frame'], False), futonend=p_futonend,
          vestbody=p_vestbody, vestsh=p_vestsh, vestin=p_flat(C['terra'], 0, 1), plate=p_plate, socket=p_socket, bag=p_bag, bagflap=p_bagflap, shoe=p_shoe, sole=p_flat(C['cream'], 0, 1), leaf=p_leaf, pot=p_flat(C['terra'], 0, 1, (5, C['dkwood'])),
          desk=p_desk, drawers=p_drawers, chair=p_flat(C['terra'], 0, 1, (6, C['dkwood'])), shelf=p_flat(C['wood2'], 0, 1, (5, C['wood3'])), shelfback=p_flat(C['wood3'], .02, 6, (6, C['wood2']), True),
          poster1=p_poster1, poster2=p_poster2, calendar=p_calendar, clock=p_clock, magazine=p_magazine, notebook=p_notebook, cert=p_cert,
          radiof=p_radio_front, radio=p_flat(C['dkwood2'], 0, 1), radiotop=p_flat(C['dkwood'], 0, 1, (3, C['dkwood2'])), antenna=p_flat(C['grey'], 0, 1),
          vest=p_vest, strap=p_flat(C['orange'], 0, 1), towel=p_flat(C['cream'], 0, 1, (6, C['teal'])), shirt=p_flat(C['teal'], 0, 1, (4, C['sea2'])), kart=p_kart, tire=p_flat(C['char'], 0, 1), pig=p_flat(C['pink'], 0, 1), pigtop=p_pigtop, snout=p_flat(C['apricot'], 0, 1),
          mug=p_flat(C['blue'], 0, 1, (4, C['cream'])), mugtop=p_mugtop, pencil=p_pencil(True), pencilb=p_pencil(False), pencilg=p_flat(C['dark'], 0, 1), pencile=p_flat(C['pink'], 0, 1), cap=p_cap, brim=p_flat(C['blue2'], 0, 1),
          slipper=p_flat(C['sea'], 0, 1, (3, C['sea2'])), shade=p_flat(C['cream'], 0, 1, (5, C['frame'])), cord=p_flat(C['char'], 0, 1), hook=p_flat(C['wood'], 0, 1),
          box=p_flat(C['egg'], 0, 1, (5, C['wood'])))
for i in range(4): PT['photo%d' % i] = p_photo(i)
for i in range(9): PT['book%d' % i] = p_book(i)

# ---------- 기하 ----------
quads = {}; NM = [None]; PHF = [False]; PHS = set(); SN = 'ROOM_shellN'  # 0052: PHF = 옛 창 조각(칸만 예전 자리, 메시 없음) · SN = 새 문 조각(맨 뒤 줄, 메시는 ROOM_shell)
def nm(n): NM[0] = n  # 2303: 만질 수 있는 물건 = 낱개 오브젝트(NAME_NN, 피벗 바닥 가운데). 껍데기(ROOM_shell)는 그대로
def _dn(mesh, pts, dens):  # 2303: 작은 물건(면의 가장 긴 축 ≤1.5m) = 64, 그 밖 32 — check_rules 섬 기대값과 같은 규칙
    if mesh in ('ROOM_shell', SN): return mesh, dens
    ext = max(max(v[k] for v in pts) - min(v[k] for v in pts) for k in range(3)); return NM[0] or mesh, 64 if ext <= 1.5 else 32
def Q(mesh, o, U, V, pt, dens=32):
    o, U, V = Vector(o), Vector(U), Vector(V); mesh, dens = _dn(mesh, (o, o + U, o + U + V, o + V), dens); quads.setdefault(mesh, []).append((o, U, V, pt, dens))
    if PHF[0]: PHS.add((mesh, len(quads[mesh]) - 1))
def Q4(mesh, pts, pt, dens=32):  # 일반 사각(볼록) — 텍스처 크기는 첫 꼭짓점의 두 변 길이
    p = [Vector(v) for v in pts]; mesh, dens = _dn(mesh, p, dens); quads.setdefault(mesh, []).append((p[0], p[1] - p[0], p[3] - p[0], pt, dens, p))
def box(mesh, x0, x1, y0, y1, z0, z1, pt, dens=32, skip=()):
    g = (lambda k: pt.get(k, pt.get('*'))) if isinstance(pt, dict) else (lambda k: pt)
    dx, dy, dz = x1 - x0, y1 - y0, z1 - z0
    F = dict(front=((x0, y0, z0), (dx, 0, 0), (0, 0, dz)), back=((x1, y1, z0), (-dx, 0, 0), (0, 0, dz)), right=((x1, y0, z0), (0, dy, 0), (0, 0, dz)),
             left=((x0, y1, z0), (0, -dy, 0), (0, 0, dz)), top=((x0, y0, z1), (dx, 0, 0), (0, dy, 0)), bottom=((x0, y1, z0), (dx, 0, 0), (0, -dy, 0)))
    for k, (o, U, V) in F.items():
        if k not in skip and g(k): Q(mesh, o, U, V, g(k), dens)
def wallQ(mesh, wall, u0, u1, z0, z1, pt, off=0., dens=32, rot=0.):
    O, Ud, n, _ = WD[wall]; o = O + Ud * u0 + Vector((0, 0, z0)) + n * off; U = Ud * (u1 - u0); V = Vector((0, 0, z1 - z0))
    if rot: c = o + U / 2 + V / 2; M = Matrix.Rotation(math.radians(rot), 3, n); U = M @ U; V = M @ V; o = c - U / 2 - V / 2
    Q(mesh, o, U, V, pt, dens)
def wallpiece(wall, u0, u1, z0, z1): wallQ('ROOM_shell', wall, u0, u1, z0, z1, ('W', wall, u0, z0))
S, Fu, Pr = 'ROOM_shell', 'ROOM_furniture', 'ROOM_props'
# 바닥·천장
Q(S, (-1.75, -1.5, 0), (3.5, 0, 0), (0, 3.0, 0), 'floor'); Q(S, (-1.75, 1.5, HH), (3.5, 0, 0), (0, -3.0, 0), 'ceiling')
# 벽: 창 벽 = 발코니 문(0052 D-200: 문 구멍 u 1.3~2.2 = x ±.45 · z 0~2.05, 문짝은 house_home.py DOOR_BALCONY), 나머지는 통짜
# 옛 창 조각(벽 4·창 두께·창턱·틀·미닫이)은 PHF: 아틀라스 칸만 예전 순서·자리에 남긴다 → 다른 물건 칸·결·양자화가 그대로(다시 쌓으면 경계값 물건 7개가 떨어졌다)
PHF[0] = True; wallpiece('win', 0, .75, 0, HH); wallpiece('win', 2.75, 3.5, 0, HH); wallpiece('win', .75, 2.75, 0, .8); wallpiece('win', .75, 2.75, 2.0, HH); PHF[0] = False
wallpiece('door', 0, 3.5, 0, HH); wallpiece('L', 0, 3.0, 0, HH); wallpiece('R', 0, 3.0, 0, HH)
PHF[0] = True
Q(S, (-1, -1.65, .8), (0, .15, 0), (0, 0, 1.2), 'reveal'); Q(S, (1, -1.5, .8), (0, -.15, 0), (0, 0, 1.2), 'reveal'); Q(S, (-1, -1.5, 2.0), (2, 0, 0), (0, -.15, 0), 'reveal')
box(S, -1.06, 1.06, -1.66, -1.42, .76, .8, 'sill', skip=('back', 'bottom'))
for x0, x1, z0, z1 in ((-1, -.95, .8, 2.0), (.95, 1, .8, 2.0), (-1, 1, 1.95, 2.0), (-1, 1, .8, .85)): box(S, x0, x1, -1.61, -1.57, z0, z1, 'winframe', skip=('back',))
for sg, yy in ((-1, -1.565), (1, -1.528)):
    xa, xb = (-.95, .02) if sg < 0 else (-.02, .95)
    for x0, x1, z0, z1 in ((xa, xa + .04, .85, 1.95), (xb - .04, xb, .85, 1.95), (xa, xb, .85, .89), (xa, xb, 1.91, 1.95), (xa, xb, 1.38, 1.41), ((xa + xb) / 2 - .015, (xa + xb) / 2 + .015, .85, 1.95)):
        box(S, x0, x1, yy, yy + .035, z0, z1, 'winframe', skip=('back',))
PHF[0] = False
# 새 문 조각(SN): 벽 3 · 문 두께(15cm)·문턱 · 방 안 문틀(3cm 돋음, 옆 기둥 윗면 = 머리 밑면 → skip, 머리 밑면은 구멍 위만)
for u0, u1, z0, z1 in ((0, 1.3, 0, HH), (2.2, 3.5, 0, HH), (1.3, 2.2, 2.05, HH)): wallQ(SN, 'win', u0, u1, z0, z1, ('W', 'win', u0, z0))
Q(SN, (-.45, -1.65, 0), (0, .15, 0), (0, 0, 2.05), 'reveal'); Q(SN, (.45, -1.5, 0), (0, -.15, 0), (0, 0, 2.05), 'reveal'); Q(SN, (-.45, -1.5, 2.05), (.9, 0, 0), (0, -.15, 0), 'reveal')
Q(SN, (-.45, -1.65, 0), (.9, 0, 0), (0, .15, 0), 'sill')
box(SN, -.51, -.45, -1.5, -1.47, 0, 2.05, 'doorframe', skip=('front', 'bottom', 'top')); box(SN, .45, .51, -1.5, -1.47, 0, 2.05, 'doorframe', skip=('front', 'bottom', 'top'))
box(SN, -.51, .51, -1.5, -1.47, 2.05, 2.11, 'doorframe', skip=('front', 'bottom')); Q(SN, (-.45, -1.47, 2.05), (.9, 0, 0), (0, -.03, 0), 'doorframe')
box(S, -1.25, 1.3, -1.47, -1.43, 2.12, 2.15, 'winframe'); box(S, 1.0, 1.24, -1.47, -1.41, .66, 2.12, 'curtain'); box(S, -1.2, -1.02, -1.47, -1.42, .7, 2.12, 'curtain')
# 문틀·손잡이·고리·시계·천장 보·전등
for x0, x1, z1 in ((.55, .6, 2.0), (1.4, 1.45, 2.0), (.55, 1.45, 2.0)): box(S, x0, x1, 1.47, 1.5, 1.95 if x1 - x0 > .5 else 0, z1, 'doorframe', skip=('back',))
nm('KNOB_01'); box(Pr, .66, .7, 1.43, 1.5, .95, .99, 'metal', 64, skip=('back',))  # 2303: 벽에 붙임(옛: 2cm 뜸)
nm('HOOK_01'); box(Pr, .08, .5, 1.47, 1.5, 1.6, 1.66, 'hook', 64, skip=('back',)); nm('TOWEL_01'); box(Pr, .16, .36, 1.455, 1.47, 1.18, 1.62, 'towel', 64, skip=('back',))  # 2303: 벽·고리와 같은 면 0
nm('WALLCLOCK_01'); wallQ(Pr, 'door', 2.62, 2.88, 2.06, 2.32, 'clock', .012, 64); nm(None)
for y0 in (-.55, .55): box(S, -1.75, 1.75, y0, y0 + .12, 2.28, HH, 'beam', skip=('top',))
nm('LAMP_01'); box(Pr, -.01, .01, .05, .07, 1.95, HH, 'cord', 64, skip=('top',)); box(Pr, -.13, .13, -.07, .19, 1.8, 1.95, 'shade', 64)  # 2303: 줄이 천장까지(옛: 12cm 아래에서 뜸)
# 2차: 창밖 판 없앰(창틀만, 유리 없음 = 투명). 스위치·콘센트 = 64 물체(+X 벽)
nm(None); box(Pr, 1.735, 1.75, 1.2, 1.28, 1.1, 1.2, 'plate', 64, skip=('right', 'bottom')); box(Pr, 1.735, 1.75, -.4, -.3, .3, .38, 'socket', 64, skip=('right', 'bottom'))  # 2303: 스위치·콘센트·전기선 = 벽 설비(만지는 물건 아님) → ROOM_props 한 덩어리
# 3차 생활 물건(벽 밑에 무리, 흩뿌림 X): 책상 옆 책가방 · 라디오 전기선 → 콘센트 · 창턱 작은 화분 · 문 옆 운동화 · 책장 옆 바닥 책 더미 · 책상 벽 쪽 책 2권
nm('BAG_01'); box(Pr, 1.45, 1.72, -.2, .06, 0, .3, 'bag', 64, skip=('bottom', 'right')); box(Pr, 1.43, 1.72, -.21, .07, .22, .34, dict({'*': 'bagflap'}), 64, skip=('bottom', 'right'))
box(Pr, 1.40, 1.45, -.15, .01, .04, .19, 'bagflap', 64, skip=('right', 'bottom')); box(Pr, 1.56, 1.6, -.13, -.01, .34, .40, 'bagflap', 64, skip=('bottom',))
nm(None); box(Pr, 1.71, 1.725, -1.2, -1.185, .3, .72, 'cord', 64, skip=('right',)); box(Pr, 1.725, 1.74, -1.2, -.38, .3, .315, 'cord', 64, skip=('right', 'bottom'))
nm('PLANT_01'); box(Pr, .6, .72, -1.44, -1.32, 0, .1, 'pot', 64, skip=('bottom',)); box(Pr, .57, .75, -1.385, -1.375, .1, .3, 'leaf', 64); box(Pr, .655, .665, -1.47, -1.29, .1, .26, 'leaf', 64); box(Pr, .61, .71, -1.42, -1.34, .2, .24, 'leaf', 64)  # 0052: 창턱이 문이 돼 바닥(문 오른쪽)으로
for k, x0 in enumerate((.2, .34)):
    nm('SHOE_%02d' % (k + 1)); box(Pr, x0, x0 + .1, 1.15, 1.43, 0, .03, 'sole', 64, skip=('bottom',)); box(Pr, x0 + .01, x0 + .09, 1.22, 1.42, .03, .09, 'shoe', 64, skip=('bottom',))
nm('BOOKS_01')
for j in range(3): box(Fu, -.66 + j * .015, -.42 - j * .01, 1.22 + j * .01, 1.45, j * .035, (j + 1) * .035, 'book%d' % (j + 5), 64, skip=('bottom',))
nm('BOOKS_02'); box(Pr, 1.58, 1.73, -.98, -.78, .72, .75, 'book1', 64, skip=('bottom',)); box(Pr, 1.6, 1.72, -.96, -.8, .75, .77, 'book6', 64, skip=('bottom',))
# 깔개
nm('RUG_01'); box(Fu, -.7, .9, -.6, .85, 0, .012, dict(top='tatami'), skip=('bottom',))
# 침대(-X 벽): 틀·머리판·요·베개·개어 둔 이불 3단
nm('BED_01'); box(Fu, -1.75, -.8, -1.0, .95, 0, .28, 'bedwood', skip=('bottom', 'left')); box(Fu, -1.75, -.8, -1.0, -.95, .28, .72, 'bedwood', skip=('left',))
box(Fu, -1.73, -.83, -.95, .92, .28, .42, dict({'*': 'sheet'}), skip=('bottom', 'left'))
nm('PILLOW_01'); box(Fu, -1.6, -.98, -.92, -.62, .42, .52, 'pillow', 64, skip=('bottom',))
# 2차: 이불 = 세 겹 개어 둔 한 덩어리, 앞(접힌 쪽) 모서리 둥글게 — 옆 단면 8각을 X 로 밀어 냄
FP = [(.9, .42), (.46, .42), (.415, .445), (.4, .5), (.412, .575), (.46, .625), (.875, .625), (.9, .6)]
x0, x1 = -1.66, -.9; nm('FUTON_01')
for i in range(1, 8):
    (ya, za), (yb, zb) = FP[i], FP[(i + 1) % 8]; Q(Fu, (x0, ya, za), (x1 - x0, 0, 0), (0, yb - ya, zb - za), 'futon')
for a, b, c, d in ((0, 1, 2, 3), (0, 3, 4, 7), (4, 5, 6, 7)):
    Q4(Fu, [(x1, *FP[j]) for j in (a, b, c, d)], 'futonend'); Q4(Fu, [(x0, *FP[j]) for j in (d, c, b, a)], 'futonend')
nm('SHIRT_01'); box(Pr, -1.45, -1.05, .05, .33, .42, .435, 'shirt', 64, skip=('bottom',)); nm('BOX_01'); box(Pr, -.66, -.42, -.98, -.78, 0, .09, 'box', 64, skip=('bottom',))
nm('SLIPPER_01'); box(Pr, -.72, -.62, .3, .56, 0, .04, 'slipper', 64, skip=('bottom',)); nm('SLIPPER_02'); box(Pr, -.58, -.48, .26, .52, 0, .04, 'slipper', 64, skip=('bottom',))
# 책상(+X 벽, 창 옆)·서랍·의자·조끼
nm('DESK_01'); box(Fu, 1.15, 1.75, -1.45, -.25, .69, .72, 'desk', skip=('right',)); box(Fu, 1.17, 1.73, -.62, -.27, 0, .69, dict({'*': 'desk'}, left='drawers'), skip=('right', 'bottom', 'top'))
box(Fu, 1.17, 1.73, -1.43, -1.39, 0, .69, 'desk', skip=('right', 'bottom', 'top'))
nm('CHAIR_01'); box(Fu, .55, .95, -1.05, -.65, .42, .46, 'chair')
for x, y in ((.56, -1.04), (.91, -1.04), (.56, -.69), (.91, -.69)): box(Fu, x, x + .03, y, y + .03, 0, .42, 'chair', skip=('top', 'bottom'))
for x in (.55, .92): box(Fu, x, x + .03, -.68, -.65, .46, .92, 'chair', skip=('bottom',))  # 3차: 의자 90° — 등받이가 문 쪽
# 2차: 조끼 = 옷 실루엣(몸판 + 팔 구멍 + V 목 + 어깨), 등받이 넘어 앞뒤로 늘어지고 아래가 벌어짐. 안쪽 면도 있음(속 빈 뒤 안 보이게)
nm('VEST_01'); VB = [(-1.08, .50), (-.62, .50), (-.63, .72), (-1.07, .72)]; VL = [(-1.05, .72), (-.87, .72), (-.9, .95), (-1.03, .95)]; VR = [(-.83, .72), (-.65, .72), (-.67, .95), (-.80, .95)]  # 어깨 넓게(콘 모양 X)
for yt, yb, out in ((-.69, -.74, -1), (-.635, -.585, 1)):  # 3차: 자리 쪽 · 문 쪽(앞판이 문을 봄)
    Y = lambda z: yt + (yb - yt) * (.95 - z) / .45
    for poly, pt in ((VB, 'vestbody'), (VL, 'vestsh'), (VR, 'vestsh')):
        P3 = [(.75 + (y + .85), Y(z), z) for y, z in poly]; o = P3[::-1] if out > 0 else P3
        # 9529: Cull Off 재질에서 공면인 겉감/안감이 함께 그려졌다.
        # 겉감·UV 칸 순서는 보존하고 안감을 옷 안쪽으로 1cm 분리한다.
        inner = [(x, y - out * .01, z) for x, y, z in o[::-1]]
        Q4('ROOM_vest', o, pt, 64); Q4('ROOM_vest', inner, 'vestin', 64)
for ya, yb in ((-1.03, -.9), (-.8, -.67)): xa, xb = .75 + ya + .85, .75 + yb + .85; Q4('ROOM_vest', [(xa, -.69, .95), (xb, -.69, .95), (xb, -.635, .95), (xa, -.635, .95)], 'vestsh', 64)
# 책상 위: 라디오(창 쪽 구석)·잡지·공책·연필·저금통·머그
nm('RADIO_01'); box(Pr, 1.42, 1.7, -1.42, -1.1, .72, .92, dict({'*': 'radio'}, left='radiof', top='radiotop'), 64, skip=('bottom', 'right')); box(Pr, 1.62, 1.635, -1.15, -1.135, .92, 1.3, 'antenna', 64, skip=('bottom',))
nm('MAGAZINE_01'); Q(Pr, (1.2, -.76, .722), (0, -.21, 0), (.28, 0, 0), 'magazine', 64)
nm('NOTEBOOK_01'); Q(Pr, (1.18, -.28, .722), (0, -.42, 0), (.3, 0, 0), 'notebook', 64); nm('PENCIL_01'); box(Pr, 1.2, 1.36, -.256, -.24, .722, .738, dict(front='pencil', top='pencil', back='pencilb', right='pencilg', left='pencile'), 64, skip=('bottom',))  # 2318: 단면 1.6cm = 1px@64(옛 1.0×0.8cm 가 1px 로 반올림돼 86px/m)
nm('PIGGY_01'); box(Pr, 1.52, 1.68, -.58, -.46, .74, .86, dict({'*': 'pig'}, top='pigtop'), 64, skip=('bottom',)); box(Pr, 1.49, 1.52, -.55, -.49, .78, .82, 'snout', 64, skip=('right',))
for x, y in ((1.54, -.57), (1.64, -.57), (1.54, -.49), (1.64, -.49)): box(Pr, x, x + .02, y, y + .02, .72, .74, 'pig', 64, skip=('top', 'bottom'))
for y0 in (-.575, -.485): box(Pr, 1.53, 1.555, y0, y0 + .03, .86, .89, 'snout', 64, skip=('bottom',))  # 3차: 귀
box(Pr, 1.66, 1.69, -.53, -.51, .8, .83, 'snout', 64, skip=('left',))  # 꼬리
nm('MUG_01'); box(Pr, 1.6, 1.68, -.36, -.28, .72, .82, dict({'*': 'mug'}, top='mugtop'), 64, skip=('bottom',))
# 벽에 붙은 것(+X 벽): 포스터 1, 아래 달력, 옆 사진 4
nm('POSTER_01'); wallQ(Pr, 'R', 2.0, 2.66, 1.42, 2.3, 'poster1', .015, 64); nm('CALENDAR_01'); wallQ(Pr, 'R', 2.16, 2.5, .9, 1.36, 'calendar', .015, 64)  # 2303: 벽에서 1.5cm
for k, (u, z, r) in enumerate(((1.55, 1.85, -4), (1.72, 1.9, 3), (1.6, 1.6, 2), (1.77, 1.62, -3))): nm('PHOTO_%02d' % (k + 1)); wallQ(Pr, 'R', u, u + .13, z, z + .18, 'photo%d' % k, .012 + k * .003, 64, r)
# -X 벽(침대 위): 레이스 사진 포스터(살짝 기움)
nm('POSTER_02'); wallQ(Pr, 'L', 1.15, 2.05, 1.15, 1.75, 'poster2', .015, 64, 1.5)
# 책장(문 벽 -X 쪽): 옆판·선반 4·뒤판, 책·카트 모형·상장·상자
nm('SHELF_01'); box(Fu, -1.7, -1.68, 1.2, 1.5, 0, 1.6, 'shelf', skip=('back',)); box(Fu, -.72, -.7, 1.2, 1.5, 0, 1.6, 'shelf', skip=('back',))
for z in (0, .5, 1.0, 1.58): box(Fu, -1.68, -.72, 1.2, 1.5, z, z + .02, 'shelf', skip=('back',))
Q(Fu, (-1.68, 1.49, 0), (.96, 0, 0), (0, 0, 1.6), 'shelfback')
rs = np.random.default_rng(21); x = -1.66; k = 0
while x < -.78:
    bw = float(rs.choice([.03, .04, .05])); bh = float(rs.choice([.2, .24, .28])); nm('BOOK_%02d' % (k + 1)); box(Fu, x, x + bw, 1.26, 1.47, .02, .02 + bh, 'book%d' % (k % 9), 64, skip=('back', 'bottom')); x += bw + .003; k += 1
nm('BOOKS_03')
for j in range(3): box(Fu, -1.62, -1.3 + j * .01, 1.25, 1.45, 1.6 + j * .03, 1.63 + j * .03, 'book%d' % (j + 3), 64, skip=('bottom',))  # 위: 눕힌 잡지 더미
nm('KART_01'); box(Pr, -1.5, -1.24, 1.28, 1.42, 1.04, 1.08, 'kart', 64, skip=('bottom',)); box(Pr, -1.42, -1.34, 1.32, 1.38, 1.08, 1.12, 'tire', 64, skip=('bottom',))
for x, y in ((-1.5, 1.25), (-1.3, 1.25), (-1.5, 1.42), (-1.3, 1.42)): box(Pr, x, x + .05, y, y + .03, 1.02, 1.07, 'tire', 64, skip=('bottom',))
nm('CERT_01'); wallQ(Pr, 'door', .82, 1.06, 1.06, 1.24, 'cert', .015, 64, -3); nm('BOX_02'); box(Pr, -.98, -.75, 1.3, 1.46, .52, .66, 'box', 64, skip=('bottom', 'back'))

# ---------- 아틀라스(선반 쌓기) ----------
items = []
for mesh, qs in list(quads.items()) + [(SN, None)]:  # 0052: 새 문 조각(SN)은 맨 뒤
    if mesh == SN and qs is not None: continue
    for qi, (o, U, V, pt, dn, *_) in enumerate(quads.get(SN, []) if mesh == SN else qs): items.append([mesh, qi, max(1, int(round(U.length * dn))), max(1, int(round(V.length * dn))), pt, dn])
NOLD = sum(1 for it in items if it[0] != SN)  # 2303: 최소 1px(옛 2px = 1.5cm 줄·연필 옆면이 90~130px/m)
AW = 1024; order = sorted(range(NOLD), key=lambda k: -items[k][3]) + sorted(range(NOLD, len(items)), key=lambda k: -items[k][3]); x = y = rowh = 0; PAD = 2
for k in order:
    w, h = items[k][2], items[k][3]
    if x + w + PAD > AW or (k == order[NOLD] if NOLD < len(items) else False): x = 0; y += rowh + PAD; rowh = 0
    items[k] += [x, y]; x += w + PAD; rowh = max(rowh, h)
AH = 1 << max(9, int(math.ceil(math.log2(y + rowh + 1)))); print('ATLAS', AW, AH, len(items), flush=True)
A = np.zeros((AH, AW, 3)); A[:] = C['char']; USED = np.zeros((AH, AW), bool)
for mesh, qi, w, h, pt, dn, ux, uy in items:
    if isinstance(pt, tuple): blk = wall_crop(pt[1], pt[2], pt[3], h, w)
    else:
        CD[0] = dn; blk = np.zeros((h, w, 3)); blk[:] = C['wall']; PT[pt](blk); pm = ph_of(pt)
        if pm: photo(blk, pm[0], pm[1], qi * 7 + w * 3 + h)
        CD[0] = 32
    A[uy:uy + h, ux:ux + w] = blk; USED[uy:uy + h, ux:ux + w] = True

# ---------- 양자화 + 외톨이 정리 ----------
flat = A.reshape(-1, 3); idx = np.concatenate([np.argmin(((flat[i:i + 65536, None, :] - P[None]) ** 2 * (.35, .45, .2)).sum(-1), 1) for i in range(0, len(flat), 65536)]).reshape(AH, AW)
def iso_mask(I):
    p = np.pad(I, 1, mode='edge'); nb = [p[:-2, 1:-1], p[2:, 1:-1], p[1:-1, :-2], p[1:-1, 2:]]
    return np.all([n != I for n in nb], 0) & USED, nb
THIN = np.zeros((AH, AW), bool)  # 2318: 두께 ≤3px 면(연필·손잡이·줄)은 외톨이 정리 제외 — 정리하면 결이 전부 지워져 잔결 0
for mesh, qi, w, h, pt, dn, ux, uy in items:
    if min(w, h) <= 3: THIN[uy:uy + h, ux:ux + w] = True
for _ in range(4):
    m, nb = iso_mask(idx); m &= ~THIN; st = np.stack(nb, -1)
    for yy, xx in zip(*np.nonzero(m)):
        v, cnt = np.unique(st[yy, xx], return_counts=True); idx[yy, xx] = v[np.argmax(cnt)]
Qd = P[idx]; Qd[~USED] = C['char']
for mesh, qi, w, h, pt, dn, ux, uy in items:  # 2318: 가장자리 1px 번짐(PAD 2 의 제 몫) — 1px 단면(연필·책 등)의 3×3 잔결이 옆 빈칸 숯색을 안 섞게
    y0, x0 = max(uy - 1, 0), max(ux - 1, 0); e = np.pad(Qd[uy:uy + h, ux:ux + w], ((1, 1), (1, 1), (0, 0)), mode='edge')[y0 - uy + 1:, x0 - ux + 1:]
    y1, x1 = min(uy + h + 1, AH), min(ux + w + 1, AW); Qd[y0:y1, x0:x1] = e[:y1 - y0, :x1 - x0]
m, _ = iso_mask(idx); Lq = (Qd @ np.array((.299, .587, .114)))[USED]
img = bpy.data.images.new('room', AW, AH, alpha=False); pxa = np.ones((AH, AW, 4), np.float32); pxa[..., :3] = Qd / 255.
img.pixels.foreach_set(pxa.ravel()); img.filepath_raw = OUT + 'room.png'; img.file_format = 'PNG'; img.save()
bpy.data.images.remove(img); TEX = bpy.data.images.load(OUT + 'room.png')

# ---------- 메시 ----------
def mk_mat(name, emit):
    mat = bpy.data.materials.new(name); mat.use_nodes = True; nt = mat.node_tree; nt.nodes.clear()
    tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = TEX; tx.interpolation = 'Closest'; mo = nt.nodes.new('ShaderNodeOutputMaterial')
    bs = nt.nodes.new('ShaderNodeEmission' if emit else 'ShaderNodeBsdfDiffuse'); nt.links.new(tx.outputs[0], bs.inputs[0]); nt.links.new(bs.outputs[0], mo.inputs[0]); return mat
MAT = mk_mat('ROOM_alb', False); MATV = mk_mat('ROOM_view_unlit', True)
obs = []; tris = 0
for mesh in quads:
    if mesh == SN: continue
    me = bpy.data.meshes.new(mesh); bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    for mname, qi, w, h, pt, dn, ux, uy in items:
        if not (mname == mesh or (mesh == 'ROOM_shell' and mname == SN)) or (mname, qi) in PHS: continue
        q = quads[mname][qi]; o, U, V = q[:3]
        f = bm.faces.new([bm.verts.new(p) for p in (q[5] if len(q) > 5 else (o, o + U, o + U + V, o + V))])
        for lp, (a, b) in zip(f.loops, ((ux + .02, uy + .02), (ux + w - .02, uy + .02), (ux + w - .02, uy + h - .02), (ux + .02, uy + h - .02))): lp[uvl].uv = (a / AW, b / AH)
    if mesh != 'ROOM_shell':  # 2303: 피벗 = 바닥 가운데(D-195)
        cs = [v.co.copy() for v in bm.verts]; pv = Vector(((min(c.x for c in cs) + max(c.x for c in cs)) / 2, (min(c.y for c in cs) + max(c.y for c in cs)) / 2, min(c.z for c in cs)))
        for v in bm.verts: v.co -= pv
    else: pv = Vector()
    bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(mesh, me); ob.location = pv; sc.collection.objects.link(ob); me.materials.append(MATV if mesh == 'ROOM_view' else MAT); obs.append(ob)
    tris += sum(len(p.vertices) - 2 for p in me.polygons)
    if mesh == 'ROOM_view': ob.visible_shadow = False
# 0052: 창 → 발코니 문(유리 없음)
for en, loc in (('STUDIO_CamIn', (1.0, 1.3, 1.55)), ('STUDIO_CamInLook', (-.5, -1.2, 1.05))):  # 2303: studio_cap 실내 캠 = 렌더 door 와 같은 자리
    e = bpy.data.objects.new(en, None); e.location = loc; sc.collection.objects.link(e); obs.append(e)
bpy.ops.object.select_all(action='DESELECT')
for ob in obs: ob.select_set(True)
bpy.context.view_layer.objects.active = obs[0]
bpy.ops.export_scene.fbx(filepath=OUT + 'room.fbx', use_selection=True, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
                         path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH', 'EMPTY'})
def dens_of(sel):
    a = sum(i[2] * i[3] for i in items if sel(i)); wa = sum(quads[i[0]][i[1]][1].length * quads[i[0]][i[1]][2].length for i in items if sel(i)); return round(math.sqrt(a / wa), 2)
meas = dict(tris=tris, objects=[o.name for o in obs], atlas=[AW, AH], texels_per_m=dict(big=dens_of(lambda i: i[5] == 32), small=dens_of(lambda i: i[5] == 64)),
            colors=int(len(np.unique(idx[USED]))), palette='palette_q1v2.gpl (palette53 + 결 단계 %d)' % (len(PAL) - 53), isolated_pct=round(100 * m.sum() / USED.sum(), 3),
            p5=round(float(np.percentile(Lq, 5)), 1), p50=round(float(np.percentile(Lq, 50)), 1), p95=round(float(np.percentile(Lq, 95)), 1), photo_src=sorted(PHUSE), photo_amp=AMP,
            size_m=dict(w=3.5, d=3.0, h=HH), pivot='바닥 가운데', forward='창 = Blender -Y → FBX/Unity +Z', note='창밖 판 없음(창틀만). 창밖은 Unity 에서 맵의 진짜 마을')
json.dump(meas, open(OUT + 'measure.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False); print('MEAS', meas, flush=True)
if '--no-render' in sys.argv: sys.stdout.flush(); os._exit(0)  # 2303: build_asset.py room source 단계 — 시트 렌더(Cycles 4장) 생략
# 시트 전용(내보내지 않음): 창밖 = 실제 게임 캡처 임시 배경. Unity 에선 맵의 주인공 집 안에 놓아 진짜 마을이 보인다.
BG = bpy.data.images.load(R + 'docs/qa/evidence/scene-a7b/street_on.png'); bw_, bh_ = BG.size; BG.scale(480, 270)  # 방 텍셀과 같은 거칠기
mb = bpy.data.materials.new('BG'); mb.use_nodes = True; nt = mb.node_tree; nt.nodes.clear(); tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = BG; tx.interpolation = 'Closest'
em = nt.nodes.new('ShaderNodeEmission'); mo = nt.nodes.new('ShaderNodeOutputMaterial'); nt.links.new(tx.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], mo.inputs[0])
WB = 18.0; HB = WB * bh_ / bw_; meb = bpy.data.meshes.new('BG'); meb.from_pydata([(WB / 2, -7, -5.0), (-WB / 2, -7, -5.0), (-WB / 2, -7, -5.0 + HB), (WB / 2, -7, -5.0 + HB)], [], [(0, 1, 2, 3)])
uvb = meb.uv_layers.new(name='UV')
for k, uv in enumerate(((0, 0), (1, 0), (1, 1), (0, 1))): uvb.data[k].uv = uv
obb = bpy.data.objects.new('BG_capture', meb); sc.collection.objects.link(obb); meb.materials.append(mb); obb.visible_shadow = False

# ---------- 게이트 렌더: 창으로 드는 아침 해 1 + 창 하늘빛 근사 ----------
wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True; bgn = wd.node_tree.nodes['Background']; bgn.inputs[0].default_value = (.62, .72, .78, 1); bgn.inputs[1].default_value = float(os.environ.get("WORLD", .45))
sd = bpy.data.lights.new('sun', 'SUN'); sd.energy = float(os.environ.get('SUN', 4.2)); sd.angle = math.radians(2); so = bpy.data.objects.new('sun', sd); sc.collection.objects.link(so)
so.rotation_euler = Vector((-.3, 1, -.5)).to_track_quat('-Z', 'Y').to_euler()
ad = bpy.data.lights.new('skywin', 'AREA'); ad.shape = 'RECTANGLE'; ad.size = 2.0; ad.size_y = 1.2; ad.energy = float(os.environ.get('SKY', 95)); ad.color = (.78, .88, 1.0)
ao = bpy.data.objects.new('skywin', ad); sc.collection.objects.link(ao); ao.location = (0, -1.4, 1.4); ao.rotation_euler = Vector((0, 1, -.25)).to_track_quat('-Z', 'Y').to_euler()
ad.cycles.is_portal = False
sc.render.engine = 'CYCLES'; sc.cycles.samples = int(os.environ.get('SPP', 128)); sc.cycles.use_denoising = True; sc.cycles.filter_width = .5; sc.view_settings.view_transform = 'Standard'
try:
    pr = bpy.context.preferences.addons['cycles'].preferences; pr.compute_device_type = 'OPTIX'; pr.get_devices()
    for dv in pr.devices: dv.use = True
    sc.cycles.device = 'GPU'
except Exception as e: print('GPU off', e)
cd = bpy.data.cameras.new('cam'); cd.sensor_fit = 'VERTICAL'; cd.clip_start = .05; cam = bpy.data.objects.new('cam', cd); sc.collection.objects.link(cam); sc.camera = cam
def shot(name, pos, tgt, fov):
    cam.location = Vector(pos); cam.rotation_euler = (Vector(tgt) - cam.location).to_track_quat('-Z', 'Y').to_euler(); cd.angle = math.radians(fov)
    sc.render.resolution_x, sc.render.resolution_y = 2560, 1440; sc.render.filepath = RV + name + '_raw.png'; bpy.ops.render.render(write_still=True); print('RENDER', name, flush=True)
shot('door', (1.0, 1.3, 1.55), (-.5, -1.2, 1.05), 62)       # 문에서 들어서며 본 방
shot('desk', (-.55, .45, 1.45), (1.75, -.95, 1.15), 56)      # 책상·포스터·달력·조끼
shot('window', (.1, .35, 1.5), (-.1, -1.5, 1.35), 56)        # 창 쪽(창밖 만)
shot('back', (.3, -1.15, 1.5), (-.4, 1.5, 1.0), 62)          # 뒤 돌아 문·책장 쪽(벽 4면 확인용)
