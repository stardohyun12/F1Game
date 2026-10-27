"""집 h판 — 정비소 V5 와 같은 표면 (아트2, 과제 20261027-1543, D-176).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_h.py
이번 방법 —
  형태·이야기: e판 생성기(opus_houses_e.py → opus_houses.py) 그대로 — 같은 이름·원점·모듈·카드(간판 글자·덧창 3종·손댄 흔적·소품).
  밀도: 정비소 V5 와 같은 32텍셀/m(면마다 고유 칸, 칸 사이 4px).
  바탕(albedo): 칠 색(집마다 크림·살구·연황토·연청록·흰, 바랜 쪽으로 18%) × CC0 사진 밝기 결(색은 안 가져옴).
        벽 = 회반죽(Poly Haven worn_plaster_wall, 진폭 ±7%·1텍셀 흐림 — 점무늬 전면 금지) 또는 판자(V1b 정비소 planks.png, 창고·카페),
        지붕 = 기와(ambientCG RoofingTiles011A) / 평지붕 = 콘크리트(ambientCG Concrete042A), 함석 차양 = V1b tin_roof.png,
        덧창·문 = 칠한 나무(ambientCG PaintedWood008C), 판자·간판 바탕 = V1b wood.png, 돌 = V1b stone.png.
        얼룩은 벽 아래(0.35~0.65m)·모서리·창 밑 빗물 줄만(벽 텍셀의 ≤25% 목표, 측정해 적음).
  빛: 정비소 V5 굽기와 같은 해(SUN (-15,-18,14), 방향 (.7,.65,-.45), 2.4, 색 (1,.83,.60), 각 .10)·하늘(.30,.52,.65)×.65·
        Cycles 128 샘플·디노이즈·반사 4·Standard·노출 0, DIFFUSE(직접+간접+색) + AO. 땅판(흙색) 하나 깔고 집 한 채씩.
  후처리: scene_a_v5_quantize.py 와 같은 식(AO .70+.30·ao, 청록 그늘, 선형 ×2, 어둠 들어올림) → 밝기 70 아래만 55~70 으로(창 안·턱 검정 금지)
        → 정비소 palette32.gpl(art/export/scene-a-v5b-garage) 최근접, 디더 없음. unlit(Emission).
  간판 글자: 굽기 뒤 글자 텍셀을 팔레트에서 다시 찍음(바탕과 밝기 차 ≥35, 획 = 정수배 확대 ≥1텍셀).
출력: art/export/scene-a4-houses-h/<이름>.fbx·.png, kit.json, palette32.gpl(정비소 것 복사), measure.json, parts.json · 작업 art/models/opus_houses_h/
"""
import os, sys, json, math, time, shutil
MYDOC_H = __doc__
_HE = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_e.py')
_es = open(_HE, encoding='utf-8').read(); _es = _es[:_es.index('# ---------- 굽기 ----------')]


def _erep(a, b):
    global _es; assert a in _es, a; _es = _es.replace(a, b, 1)


# c판 소스에 h 패치(e 패치 뒤, exec 앞) — 빛·그늘·짙은 선은 Cycles 가 굽는다
_erep("exec(compile(_src, 'opus_houses.py', 'exec'))", '''_patch("DENS, AW, GAP = (9, 256, 2) if VARIANT == 'c'", "DENS, AW, GAP = (32, 2048, 4) if VARIANT == 'c'")
_patch("col = np.round(np.clip(light(n, base), 0, 1) * 255 / 6) * 6", "col = np.clip(base, 0, 1)")
_patch("if role == 'wall': base = DARK(W_)", "if role == 'wall': base = W_")
_patch("        col[dark] = DARK(col[dark]); col[bright] = ", "        col, dark, bright = HTEX(role, col, dark, bright, P, n, I, Jj, w, hh, h, g, r)\\n        col[dark] = DARK(col[dark]); col[bright] = ")
_patch("        if role in ('wall', 'frame', 'glass', 'door', 'shutter', 'stone'):", "        if False:")
_patch("        col = light(n, col)\\n", "")
_patch("img = np.zeros((AH, AW, 3)); img[:] = W_ * .8;", "img = np.zeros((AH, AW, 3)); img[:] = W_ * .8; GLY[0] = np.zeros((AH, AW), bool); NRM[0] = np.zeros((AH, AW, 3)); SWX.clear();")
_patch("img[s_['y'] - 1:s_['y'] + 2, s_['x'] - 1:s_['x'] + 2] = s_['col'];", "img[s_['y'] - 1:s_['y'] + 2, s_['x'] - 1:s_['x'] + 2] = s_['col']; SWX.append((s_['x'], s_['y'], s_['faces'][0]['n'], s_['role']));")
_patch("img[y0:y0 + hh + 2, x0:x0 + w + 2] = np.clip(col, 0, 1);", "img[y0:y0 + hh + 2, x0:x0 + w + 2] = np.clip(col, 0, 1); NRM[0][y0:y0 + hh + 2, x0:x0 + w + 2] = n; GLY[0][y0:y0 + hh + 2, x0:x0 + w + 2] |= GPOP(col.shape[:2]);")
exec(compile(_src, 'opus_houses.py', 'exec'))''')
# 글자: 32텍셀/m 에선 3×5 가 15cm → 판에 맞게 정수배 확대, 글자 텍셀 기록
_erep("text, ink, vert = BOARDS[xid]; m = glyphs(text, vert)[::-1]; mh, mw = m.shape",
      "text, ink, vert = BOARDS[xid]; m = glyphs(text, vert)[::-1]; s_ = max(1, min((hh - 2) // (m.shape[0] + 2), (w - 2) // (m.shape[1] + 2), 4)); m = np.kron(m, np.ones((s_, s_), bool)); mh, mw = m.shape")
_erep("sub[m] = srgb(ink)", "sub[m] = srgb(ink); G_ = np.zeros(col.shape[:2], bool); G_[1 + oy:1 + oy + mh, 1 + ox:1 + ox + mw] = m; LASTG.append(G_)")
# 창 안 그림(커튼·화분·진열)은 e판 9텍셀/m 기준 → 3×3 칸으로 묶어 같은 크기로
_erep("def GLASS(col, dark, I, Jj, w, hh, code):  # 1 커튼",
      "def GLASS(col, dark, I, Jj, w, hh, code):\n    I, Jj, w, hh = I // 3, Jj // 3, max(1, w // 3), max(1, hh // 3)  # h: 1 커튼")
GLY, NRM, SWX, LASTG, WREC, GSTAT = [None], [None], [], [], [], {}


def GPOP(sh):  # 이 칸에서 찍은 글자만(모양이 같은 것) 꺼내고 비움
    GCNT[0] += len(LASTG); g_ = LASTG[-1] if LASTG and LASTG[-1].shape == sh else False; GCNT[1] += g_ is not False; LASTG.clear(); return g_


GCNT = [0, 0]


exec(compile(_es, _HE, 'exec'))
from mathutils import Vector

OUT = J('art', 'export', 'scene-a4-houses-h'); WORK = J('art', 'models', 'opus_houses_h'); os.makedirs(OUT, exist_ok=True); os.makedirs(WORK, exist_ok=True)
GAR = J('art', 'export', 'scene-a-v5b-garage'); V1B = J('art', 'export', 'scene-a-v1b-photobash'); SA = J('art', 'source', 'photos', 'scene-a')
PLANK = ('house_warehouse', 'house_cafe')
t0 = time.time()


def lum01(a): return a @ np.array((.299, .587, .114))


def img_np(path):
    im = bpy.data.images.load(path); w, hh = im.size; a = np.empty(w * hh * 4, np.float32); im.pixels.foreach_get(a)
    a = a.reshape(hh, w, 4)[:, :, :3][::-1].astype(np.float64); bpy.data.images.remove(im); return a  # 행 0 = 위


def mkdet(path, metres, amp, blur=0):
    """사진 → 밝기 결(평균 1, ±amp). 32텍셀/m 로 넓이 평균 축소. 색은 버림."""
    a = img_np(path); tw = max(4, round(metres[0] * DENS)); th = max(4, round(metres[1] * DENS))
    if a.shape[1] != tw or a.shape[0] != th: a = area_resize(a, tw, th)
    L = lum01(a)
    if blur: L = box_blur(L, blur)
    d = (L - L.mean()) / (L.std() + 1e-6); return 1 + amp * np.clip(d, -2, 2) / 2


def sample(det, u, v):
    th, tw = det.shape; return det[(th - 1 - np.floor(v * DENS).astype(int) % th), np.floor(u * DENS).astype(int) % tw]


GR = srgb((112, 100, 82)); FADEC = srgb((206, 198, 182))
FADE = lambda c, k=.18: c * (1 - k) + FADEC * k


def GRIME(P, n, hu, h, g, seed):
    z = P[..., 2]; zb = g['zb']; m = np.zeros(z.shape)
    hb = .28 + .22 * vnoise(hu / .8, 0 * hu, seed); m = np.maximum(m, np.clip((hb - (z - zb)) / .25, 0, 1) * .9)  # 아래 튀김
    if abs(n[1]) > .7: dist = np.minimum(np.abs(P[..., 0] + g['W'] / 2), np.abs(P[..., 0] - g['W'] / 2))
    else: dist = np.minimum(np.abs(P[..., 1] + g['D'] / 2), np.abs(P[..., 1] - g['D'] / 2))
    cw = .10 + .14 * vnoise(z / .7, 0 * z + 2, seed + 1); m = np.maximum(m, np.clip((cw - dist) / .06, 0, 1) * .45 * (vnoise(z / .9, 0 * z + 5, seed + 2) > .6))  # 모서리
    for nw, Uw, u0, u1, zt, dpl in WREC:  # 창 밑 빗물 줄
        if float(n @ nw) < .95 or abs(float(np.mean(P @ nw)) - dpl) > .5: continue
        h2 = P @ Uw; Ls = .5 + .7 * vnoise(h2 / .3, 0 * h2 + 3, seed + 3); st = vnoise(h2 / .07, 0 * h2 + 11, seed + 4) > .55
        ins = (h2 > u0 + .04) & (h2 < u1 - .04) & (z < zt) & (z > zt - Ls) & st
        m = np.maximum(m, ins * .6 * np.clip(1 - (zt - z) / Ls, 0, 1))
    return m


def HTEX(role, col, dark, bright, P, n, I, Jj, w, hh, h, g, r):
    hu, vv = P @ r['U'], P @ r['V']; nm = h['name']; sd = sum(map(ord, nm)) % 97
    if role in ('wall', 'soffit'):
        det = sample(DET['planks' if nm in PLANK else 'plaster'], hu, vv); base = FADE(srgb(h['wall'])) * (.92 if role == 'soffit' else 1)
        col = base * det[..., None]; dark[:] = False; bright[:] = False
        if role == 'wall':
            m = GRIME(P, n, hu, h, g, sd); a = (.32 * m)[..., None]; col = col * (1 - a) + GR * a
            st_ = GSTAT.setdefault(nm, [0, 0]); st_[0] += int((m[1:-1, 1:-1] > .15).sum()); st_[1] += int(m[1:-1, 1:-1].size)
    elif role == 'roof':
        col = srgb(h['roof_c']) * sample(DET['tile'], hu, vv)[..., None]; dark[:] = False; bright[:] = False
    elif role == 'flat':
        col = srgb(h['roof_c']) * sample(DET['concrete'], hu, vv)[..., None]; dark[:] = False; bright[:] = False
    elif role == 'tin':
        col = np.clip(r['pc'], 0, 1) * sample(DET['tin'], hu, vv)[..., None]; dark[:] = False
    elif role == 'shutter':
        edge = (I == 0) | (I == w - 1) | (Jj == 0) | (Jj == hh - 1)
        col = FADE(srgb(h['shutter']), .12) * sample(DET['wood'], hu, vv)[..., None]; dark = edge | ((Jj % 4 == 1) & ~edge)  # 루버 12cm
    elif role == 'door':
        col = col * sample(DET['wood'], hu, vv)[..., None]
    elif role in ('plank', 'board'):
        d_ = sample(DET['boards'], hu, vv)[..., None]; keep = LASTG[-1] if (role == 'board' and LASTG and LASTG[-1].shape == col.shape[:2]) else np.zeros(col.shape[:2], bool)
        col = np.where(keep[..., None], col, col * d_)
    elif role == 'frame':
        col = col * sample(DET['plaster'], hu, vv)[..., None]
    elif role in ('stone',):
        col = srgb((190, 184, 160)) * sample(DET['stone'], hu, vv)[..., None]; dark[:] = False
    elif role == 'slab':
        col = col * sample(DET['concrete'], hu, vv)[..., None]
    return col, dark, bright


def v5(raw01, ao):  # scene_a_v5_quantize.py 와 같은 식 (opus_v5b_garage.py 와 같음)
    arr = raw01 * 255 * (.70 + .30 * ao)[..., None]; shade = (1 - ao) * .16; arr[..., 1] += shade * 24; arr[..., 2] += shade * 28
    s = np.clip(arr / 255, 0, 1); lin = np.where(s <= .04045, s / 12.92, ((s + .055) / 1.055) ** 2.4)
    lift = (1 - s.max(2)) ** 2; lin = lin * 2.0 + lift[..., None] * np.array([.009, .017, .019])
    enc = np.where(lin <= .0031308, lin * 12.92, 1.055 * np.maximum(lin, 0) ** (1 / 2.4) - .055); return np.clip(enc * 255, 0, 255)


def nearest_idx(flat, P_):
    idx = np.empty(len(flat), np.int32)
    for s0 in range(0, len(flat), 1 << 17): idx[s0:s0 + (1 << 17)] = ((flat[s0:s0 + (1 << 17), None] - P_[None]) ** 2).sum(2).argmin(1)
    return idx


# ---------- 굽기 ----------
PLOG = {}
bpy.ops.wm.read_factory_settings(use_empty=True)
DET = dict(plaster=mkdet(os.path.join(PH, 'worn_plaster_wall_diff_1k.jpg'), (2.0, 2.0), .07, 1),
           planks=mkdet(os.path.join(V1B, 'garage_v1b.fbm', 'planks.png'), (8, 3), .16),
           tile=mkdet(os.path.join(FAC, 'RoofingTiles011A_Color.jpg'), (1.5, 1.5), .22),
           concrete=mkdet(os.path.join(SA, 'Concrete042A', 'Concrete042A_1K-JPG_Color.jpg'), (2.0, 2.0), .06, 1),
           tin=mkdet(os.path.join(V1B, 'garage_v1b.fbm', 'tin_roof.png'), (8, 8), .18),
           wood=mkdet(os.path.join(SA, 'PaintedWood008C', 'PaintedWood008C_1K-JPG_Color.jpg'), (1.0, 1.0), .14),
           boards=mkdet(os.path.join(V1B, 'garage_v1b.fbm', 'wood.png'), (4, 4), .12),
           stone=mkdet(os.path.join(V1B, 'env_v1b.fbm', 'stone.png'), (4, 2), .20))
sc = bpy.context.scene
sun_d = bpy.data.lights.new('sun', 'SUN'); sun_d.energy = 2.4; sun_d.color = (1, .83, .60); sun_d.angle = .10
sun = bpy.data.objects.new('sun', sun_d); sc.collection.objects.link(sun); sun.location = (-15, -18, 14)
sun.rotation_euler = Vector((.7, .65, -.45)).to_track_quat('-Z', 'Y').to_euler()
wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True; wn = wd.node_tree.nodes; wn.clear(); bg = wn.new('ShaderNodeBackground')
bg.inputs[0].default_value = (.30, .52, .65, 1); bg.inputs[1].default_value = .65; wo = wn.new('ShaderNodeOutputWorld'); wd.node_tree.links.new(bg.outputs[0], wo.inputs[0])
prefs = bpy.context.preferences.addons['cycles'].preferences; prefs.compute_device_type = 'OPTIX'; prefs.get_devices()
for dv in prefs.devices: dv.use = dv.type == 'OPTIX'
sc.render.engine = 'CYCLES'; sc.cycles.device = 'GPU'; sc.cycles.samples = 128; sc.cycles.use_denoising = True; sc.cycles.max_bounces = 4
sc.view_settings.view_transform = 'Standard'; sc.view_settings.exposure = 0
b_ = sc.render.bake; b_.margin = 3; b_.use_clear = True; b_.use_pass_direct = True; b_.use_pass_indirect = True; b_.use_pass_color = True
gm = bpy.data.meshes.new('ground'); gm.from_pydata([(-30, -30, 0), (30, -30, 0), (30, 30, 0), (-30, 30, 0)], [], [(0, 1, 2, 3)])
gnd = bpy.data.objects.new('ground', gm); sc.collection.objects.link(gnd); gmat = bpy.data.materials.new('ground'); gmat.use_nodes = True
gmat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.305, .262, .162, 1); gmat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 1; gm.materials.append(gmat)

GPAL = np.array(read_gpl(os.path.join(GAR, 'palette32.gpl')), np.float64); GPL = lum01(GPAL)
Lext = 149.3  # 정비소 measure.json outside_lit_ref (마당 p90)
kit = dict(task='20261027-1543', note='도현 승인 전. F1/Assets 에 넣지 않음. Unity 임포트 미확인. 원점 = e판과 같음(집 바닥 가운데, 축대 집은 축대 바닥).',
           axis='Blender: 정면 -Y, 위 +Z → FBX axis_forward -Z, axis_up Y, 1u = 1m', method=MYDOC_H.split('이번 방법 —')[1].split('출력:')[0].strip(),
           palette='palette32.gpl (= art/export/scene-a-v5b-garage/palette32.gpl, 새 팔레트 아님)', texels_per_m=DENS, cards='docs/design/HOUSES.md',
           license='CC0 — elegantcrow Retro Modular House(모듈) · valsekamerplant PSX house plants(화분) · Poly Haven worn_plaster_wall · ambientCG RoofingTiles011A·Concrete042A·PaintedWood008C · '
                   'V1b 정비소 사진 텍스처(planks·tin_roof·wood·stone, 아트1 V1b — 원본 CC0). 글자·소품은 직접 만든 상자.', files=[])
meas = {}
for k, h in enumerate(HOUSES):
    h.update(CARDS[h['name']][1]); LASTG.clear()
    o, g = build(h)
    me = o.data; rl = np.zeros(len(me.polygons), np.int32); me.attributes['role'].data.foreach_get('value', rl); WREC.clear()
    for f in me.polygons:
        if rl[f.index] == RID['glass'] and f.area > .1 and abs(f.normal.z) < .3:
            nw = np.array(f.normal); Uw = np.cross((0, 0, 1.), nw); Uw /= np.linalg.norm(Uw); ps = np.array([list(me.vertices[i].co) for i in f.vertices])
            WREC.append((nw, Uw, float((ps @ Uw).min()), float((ps @ Uw).max()), float(ps[:, 2].min()), float(ps.mean(0) @ nw)))
    img, cls, nf = pack_paint_c(o, h, g, k * 13 + 5)
    gly, nrm, swx = GLY[0].copy(), NRM[0].copy(), list(SWX)
    tris = sum(len(p.vertices) - 2 for p in me.polygons); pts = np.array([v.co for v in me.vertices]); lo, hi = pts.min(0), pts.max(0)
    AH, AW_ = img.shape[:2]; print('BUILT', h['name'], 'tris', tris, 'tex', AW_, AH, 'win', len(WREC), round(time.time() - t0), flush=True)
    alb = save_png(img, os.path.join(WORK, h['name'] + '_albedo.png'))
    mat = bpy.data.materials.new('bake_' + h['name']); mat.use_nodes = True; nt = mat.node_tree; nt.nodes.clear()
    tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = alb; tx.interpolation = 'Closest'; df = nt.nodes.new('ShaderNodeBsdfDiffuse'); df.inputs['Roughness'].default_value = .5
    mo = nt.nodes.new('ShaderNodeOutputMaterial'); nt.links.new(tx.outputs[0], df.inputs[0]); nt.links.new(df.outputs[0], mo.inputs[0])
    tgt = nt.nodes.new('ShaderNodeTexImage'); me.materials.clear(); me.materials.append(mat)
    for f in me.polygons: f.material_index = 0
    for ob in sc.objects:
        if ob.type == 'MESH' and ob not in (o, gnd): ob.hide_render = True
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    res = {}
    for kind in ('DIFFUSE', 'AO'):
        bi = bpy.data.images.new(h['name'] + '_' + kind, AW_, AH, alpha=False); tgt.image = bi; nt.nodes.active = tgt
        bpy.ops.object.bake(type=kind); bi.filepath_raw = os.path.join(WORK, h['name'] + '_' + kind.lower() + '.png'); bi.file_format = 'PNG'; bi.save()
        a = np.empty(AW_ * AH * 4, np.float32); bi.pixels.foreach_get(a); res[kind] = a.reshape(AH, AW_, 4)[:, :, :3].astype(np.float64)
        print('BAKE', h['name'], kind, round(time.time() - t0), flush=True)
    raw, ao = res['DIFFUSE'], res['AO'][..., 0]
    # 한 점 칸(작은 면 공유 칸)은 UV 가 점이라 안 구워짐 → 같은 쪽을 보는 구운 면의 빛 비율(중앙값)로 채움
    used = raw.sum(2) > 1e-4  # 구워진 텍셀(삼각형 면은 칸의 절반만 쓰임 — 나머지는 안 보임)
    for x, y, n_, role in swx: used[y - 1:y + 2, x - 1:x + 2] = True
    vis = (cls != RID['hidden']) & used; rectm = vis & (np.abs(nrm).sum(2) > .5); ratio = raw / np.maximum(img, .02)
    for x, y, n_, role in swx:
        sel = rectm & ((nrm @ np.array(n_)) > .9)
        if sel.sum() < 50: sel = rectm
        sl = (slice(y - 1, y + 2), slice(x - 1, x + 2)); raw[sl] = img[sl] * np.median(ratio[sel], 0); ao[sl] = np.median(ao[sel])
    P = v5(raw, ao); L = lum01(P)
    low = vis & (L < 70); Lt = 55 + np.clip(L, 0, 70) / 70 * 15  # 창 안·턱 검정 금지(p5 ≥ 55): 70 아래만 55~70 으로
    TINT = np.array((112, 124, 112)); up = P * np.clip(Lt / np.maximum(L, 1), 1, 6)[..., None]; up = up * .9 + TINT * (lum01(up) / lum01(TINT))[..., None] * .1
    P[low] = up[low]; P = np.clip(P, 0, 255)
    idx = nearest_idx(P.reshape(-1, 3), GPAL).reshape(AH, AW_)
    # 간판 글자: 바탕(주변 9×9 판 텍셀 평균)과 밝기 차 ≥35 인 팔레트 색 중 가장 가까운 색
    nglyph = 0
    if gly.any():
        bgm = (cls == RID['board']) & ~gly; Pb = np.stack([box_blur(P[..., c] * bgm, 4) for c in range(3)], -1) / np.maximum(box_blur(bgm.astype(float), 4), 1e-3)[..., None]
        for yy, xx in zip(*np.nonzero(gly)):
            lb = lum01(Pb[yy, xx]); li = lum01(P[yy, xx]); ok = (GPL <= lb - 35) if li < lb else (GPL >= lb + 35)
            if not ok.any(): ok = np.ones(len(GPAL), bool)
            cand = np.nonzero(ok)[0]; idx[yy, xx] = cand[((GPAL[cand] - P[yy, xx]) ** 2).sum(1).argmin()]; nglyph += 1
    q = GPAL[idx] / 255; im = save_png(q, os.path.join(OUT, h['name'] + '.png')); np.save(os.path.join(WORK, h['name'] + '_cls.npy'), cls[::-1])
    Lq = lum01(q) * 255; wall = (cls == RID['wall']) & used; glass = (cls == RID['glass']) & used
    print('GLYPH', h['name'], 'appended', GCNT[0], 'kept', GCNT[1], 'px', nglyph, flush=True); GCNT[:] = [0, 0]
    gs = GSTAT.get(h['name'], [0, 1])
    meas[h['name']] = dict(texels_per_m=DENS, size=[AW_, AH], colors_visible=int(len(np.unique(idx[vis]))), wall_colors=int(len(np.unique(idx[wall]))),
                           L_wall_mean=round(float(Lq[wall].mean()), 1), L_visible_mean=round(float(Lq[vis].mean()), 1), p5_visible=round(float(np.percentile(Lq[vis], 5)), 1),
                           p5_glass=round(float(np.percentile(Lq[glass], 5)), 1) if glass.any() else None, under55_visible_pct=round(float((Lq[vis] < 55).mean() * 100), 3),
                           lifted_px_pct=round(float(low[vis].mean() * 100), 2), grime_wall_pct=round(gs[0] / max(gs[1], 1) * 100, 1), glyph_px=nglyph, tris=int(tris),
                           speckle_1px_pct=round(speckle(idx), 3))
    export(o, im, os.path.join(OUT, h['name'] + '.fbx'))
    for ob in sc.objects: ob.hide_render = False
    o.hide_render = True; o.hide_viewport = True
    kit['files'].append(dict(name=h['name'], fbx=h['name'] + '.fbx', png=h['name'] + '.png', note=h['note'], place=h['place'], size_m=[round(float(v), 2) for v in (hi - lo)],
                             footprint_m=[float(g['W']), float(g['D'])], wall_top_m=round(float(g['Hw']), 2), tris=int(tris), tex=[AW_, AH], wall=list(h['wall']), roof=list(h['roof_c'])))
    print('MEAS', h['name'], meas[h['name']], round(time.time() - t0), flush=True)
shutil.copy2(os.path.join(GAR, 'palette32.gpl'), os.path.join(OUT, 'palette32.gpl'))
kit['sources_used'] = sorted({os.path.relpath(p, TP).replace('\\', '/') for p in LIB})
json.dump(kit, open(os.path.join(OUT, 'kit.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(dict(luma='0.299R+0.587G+0.114B (32색 뒤, 텍스처 픽셀)', files=meas), open(os.path.join(OUT, 'measure.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
json.dump(PLOG, open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False, default=lambda v: v.tolist() if hasattr(v, 'tolist') else float(v))
print('DONE', round(time.time() - t0), flush=True)
