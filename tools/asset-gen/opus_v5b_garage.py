"""V5 정비소 실내 다시 굽기 — 실내 보조 빛 + 최소 밝기 바닥, 바깥면은 V5 그대로 (아트2, 과제 20261027-1248).
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_v5b_garage.py
원인: V5 는 해(2.4)·하늘(0.65)만으로 구웠다. 정비소는 앞만 열린 상자라 안쪽 면이 해·하늘을 거의 못 받아 거의 검게 구워지고, unlit 이라 그대로 검정.
방법:
  1. 아트1 baked-lit.blend(읽기만)를 열어 정비소 묶음 + 바퀴자국 흙(dirt — 정비소 바닥 오른쪽 절반까지 들어와 있음)만 다시 굽는다.
     같은 해·하늘 + 실내 빛 3개: 천장 등 2개(전등 갓 자리, 형광등 흰빛) · 입구에서 안쪽을 향한 넓은 판 빛(햇빛 받은 마당 반사, 따뜻한 색)
  2. V5 후처리(AO·청록 그늘·노출)를 그대로 다시 적용해 '더해진 빛'만 뽑는다.
  3. 실내 픽셀에만(정비소: 상자 안에 중심이 있는 면 / 흙: 픽셀의 실제 위치가 정비소 안):
     V5 + k × 더해진 빛 (k = 실내 중앙 밝기가 바깥 밝은 면의 65%; 바깥 밝은 면 = 해 받는 마당 텍스처 p90)
     → 바깥 80% 넘는 더해진 빛은 눌러 줌(입구 쪽 바닥이 하얗게 날아가지 않게)
     → 중앙값 아래는 순서를 지키며 0→바깥 50%, 중앙값→그대로 로 선형 들어올림(청록·갈색 10%).
  4. V5 공용 32색으로 줄임. 실내 밖 픽셀은 V5 *_baked32.png 를 그대로 복사.
출력: art/export/scene-a-v5b-garage/{garage,dirt}_v5_combo.fbx(V5 파일 그대로 복사 — 형상·UV 같음) + *_baked32.png, measure.json · 작업 art/models/opus_v5b_garage/
"""
import bpy, json, os, shutil, time
import numpy as np
from mathutils import Vector

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); J = lambda *p: os.path.join(R, *p)
O = J('art', 'models', 'garage_scene_a_v5_combo'); E = J('art', 'export', 'scene-a-v5-combo')
OUT = J('art', 'export', 'scene-a-v5b-garage'); WORK = J('art', 'models', 'opus_v5b_garage')
os.makedirs(OUT, exist_ok=True); os.makedirs(WORK, exist_ok=True)
t0 = time.time()
bpy.ops.wm.open_mainfile(filepath=os.path.join(O, 'baked-lit.blend')); sc = bpy.context.scene
man = {m['group']: m for m in json.load(open(os.path.join(O, 'manifest.json')))}
BOX = ((-4.94, 4.98), (.03, 11.97), (-.2, 5.65))
inbox = lambda c: all(BOX[i][0] <= c[i] <= BOX[i][1] for i in range(3))
SKYSIDE = ('roof', 'rib', 'shade', 'hoist_beam', 'cable')  # 이 물체들의 위를 향한 면은 지붕 위/밖이라 제외
EXCL = ('wall_', 'win_', 'back', 'front_', 'header', 'wall_top', 'post_', 'rail', 'sh_', 'sign', 'pdoor', 'apron', 'loose_plank', 'tO', 'drum3', 'drum_lying', 'crate1', 'crate2')


def face_mask(obs, W, H):  # 정비소: BakedUV 칸 = 면마다 고유 사각형 → 면 중심이 상자 안이면 칸 전체
    mask = np.zeros((H, W), bool); nf = 0
    for o in obs:
        if o.name.startswith(EXCL): continue
        uv = o.data.uv_layers['BakedUV']; mw = o.matrix_world; nm = mw.to_3x3().inverted().transposed()
        for f in o.data.polygons:
            c = mw @ f.center; n = (nm @ f.normal).normalized()
            if not inbox(c) or (o.name.startswith(SKYSIDE) and n.z > .3): continue
            us = np.array([uv.data[li].uv[:] for li in f.loop_indices])
            x0, y0 = np.floor(us.min(0) * (W, H)).astype(int) - 1; x1, y1 = np.ceil(us.max(0) * (W, H)).astype(int) + 1
            mask[max(y0, 0):y1, max(x0, 0):x1] = True; nf += 1
    return mask, nf


def pixel_mask(obs, W, H):  # 흙: 면 하나가 안팎에 걸침 → 픽셀마다 실제 위치(UV→세계 아핀)로 판정
    mask = np.zeros((H, W), bool); nf = 0
    for o in obs:
        uv = o.data.uv_layers['BakedUV']; mw = o.matrix_world
        for f in o.data.polygons:
            li = list(f.loop_indices)[:3]; U = np.array([uv.data[i].uv[:] for i in li]) * (W, H)
            P = np.array([list(mw @ o.data.vertices[o.data.loops[i].vertex_index].co) for i in li])
            A = np.c_[U, np.ones(3)]
            if abs(np.linalg.det(A)) < 1e-9: continue
            M = np.linalg.solve(A, P)  # [u v 1] @ M = 세계 좌표
            us = np.array([uv.data[i].uv[:] for i in f.loop_indices]) * (W, H)
            x0, y0 = np.floor(us.min(0)).astype(int); x1, y1 = np.ceil(us.max(0)).astype(int) + 1
            yy, xx = np.mgrid[max(y0, 0):min(y1, H), max(x0, 0):min(x1, W)]
            Pw = np.stack([xx + .5, yy + .5, np.ones(xx.shape)], -1) @ M
            m = (Pw[..., 0] >= BOX[0][0]) & (Pw[..., 0] <= BOX[0][1]) & (Pw[..., 1] >= BOX[1][0]) & (Pw[..., 1] <= BOX[1][1])
            mask[max(y0, 0):min(y1, H), max(x0, 0):min(x1, W)] |= m; nf += 1
    return mask, nf


def area(name, loc, aim, size, color, power):
    d = bpy.data.lights.new(name, 'AREA'); d.shape = 'RECTANGLE'; d.size, d.size_y = size; d.color = color; d.energy = power
    o = bpy.data.objects.new(name, d); sc.collection.objects.link(o); o.location = loc
    o.rotation_euler = (Vector(aim) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler(); return o


LIGHTS = [area('ceil_lamp0', (.09, 5.0, 3.42), (.09, 5.0, 0), (1.2, .3), (1, .97, .9), 160),
          area('ceil_lamp1', (2.49, 8.5, 3.42), (2.49, 8.5, 0), (1.2, .3), (1, .97, .9), 160),
          area('door_bounce', (0, .35, 1.4), (0, 6, 1.2), (7.0, 2.8), (1, .80, .58), 420)]
prefs = bpy.context.preferences.addons['cycles'].preferences; prefs.compute_device_type = 'OPTIX'; prefs.get_devices()
for dv in prefs.devices: dv.use = dv.type == 'OPTIX'
sc.render.engine = 'CYCLES'; sc.cycles.device = 'GPU'; sc.cycles.samples = 128; sc.cycles.use_denoising = True; sc.cycles.max_bounces = 4
b = sc.render.bake; b.margin = 3; b.use_clear = True; b.use_pass_direct = True; b.use_pass_indirect = True; b.use_pass_color = True


def px(path_or_img):
    im = path_or_img if not isinstance(path_or_img, str) else bpy.data.images.load(path_or_img)
    w, h = im.size; return np.array(im.pixels[:], np.float32).reshape(h, w, 4)[:, :, :3]


def v5(raw01, ao):  # scene_a_v5_quantize.py 와 같은 식
    arr = raw01 * 255 * (.70 + .30 * ao)[..., None]; shade = (1 - ao) * .16; arr[..., 1] += shade * 24; arr[..., 2] += shade * 28
    s = np.clip(arr / 255, 0, 1); lin = np.where(s <= .04045, s / 12.92, ((s + .055) / 1.055) ** 2.4)
    lift = (1 - s.max(2)) ** 2; lin = lin * 2.0 + lift[..., None] * np.array([.009, .017, .019])
    enc = np.where(lin <= .0031308, lin * 12.92, 1.055 * np.maximum(lin, 0) ** (1 / 2.4) - .055); return np.clip(enc * 255, 0, 255)


lum = lambda a: a @ np.array((.299, .587, .114))
pal = np.array([[int(t) for t in l.split()[:3]] for l in open(os.path.join(E, 'palette32.gpl')) if l[:1].isdigit()], np.float64)
gpx = px(os.path.join(E, 'ground_baked32.png')) * 255; Lext = float(np.percentile(lum(gpx), 90)); del gpx  # 바깥 밝은 면 = 해 받는 마당 p90
TINT = np.array((112, 124, 112))  # 청록·갈색 사이 그늘색


def st(a, mask):
    v = lum(a)[mask]
    return dict(mean=round(float(v.mean()), 1), p5=round(float(np.percentile(v, 5)), 1), p50=round(float(np.median(v)), 1), p95=round(float(np.percentile(v, 95)), 1),
                std=round(float(v.std()), 1), colors=int(len(np.unique(a[mask].astype(int) @ np.array((65536, 256, 1))))), under40_pct=round(float((v < 40).mean() * 100), 2))


meas = dict(task='20261027-1248', luma='0.299R+0.587G+0.114B, 텍스처 픽셀(Blender 이미지 버퍼), 실내 마스크 안', outside_lit_ref='ground_baked32 p90 = %.1f' % Lext, groups={})
for key, mfn in (('garage', face_mask), ('dirt', pixel_mask)):
    g = man[key]; obs = [bpy.data.objects[n] for n in g['objects']]; W, H = g['size']
    mask, nf = mfn(obs, W, H); print('MASK', key, nf, int(mask.sum()), flush=True)
    img = next(i for i in bpy.data.images if i.name.startswith(key + '_bake'))
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True); o.hide_render = False
    bpy.context.view_layer.objects.active = obs[0]; bpy.ops.object.duplicate(); bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
    if len(bpy.context.selected_objects) > 1: bpy.ops.object.join()
    joined = bpy.context.object
    for o in obs: o.hide_render = True
    bpy.ops.object.bake(type='DIFFUSE'); print('BAKE_DONE', key, round(time.time() - t0), flush=True)
    img.filepath_raw = os.path.join(WORK, key + '_baked_raw_v5b.png'); img.file_format = 'PNG'; img.save()
    bpy.data.objects.remove(joined, do_unlink=True)
    for o in obs: o.hide_render = False
    ao = px(os.path.join(O, key + '_ao.png'))[..., 0]
    P_file = px(os.path.join(O, key + '_baked_ao.png')) * 255
    P_old = v5(px(os.path.join(O, key + '_baked_raw.png')), ao); P_new = v5(px(img), ao)
    add = np.clip(P_new - P_old, 0, None)
    lo_k, hi_k = 0., 8.
    for _ in range(30):  # 실내 중앙 밝기 = 바깥 밝은 면의 65% (정비소 기준으로 k 를 정하고 흙은 같은 k)
        k = (lo_k + hi_k) / 2; med = np.median(lum(P_file + k * add)[mask])
        lo_k, hi_k = (k, hi_k) if med < .65 * Lext else (lo_k, k)
    if key == 'dirt': k = meas['groups']['garage']['k_added_light']
    P = P_file + k * add; L = lum(P); Lo = lum(P_file); cap = max(.80 * Lext, 1)
    hot = mask & (L > cap) & (L > Lo)  # 더해진 빛이 바깥 80% 를 넘기면 넘친 만큼 30% 로 눌러 줌
    Lh = np.maximum(cap, Lo) + (L - np.maximum(cap, Lo)) * .3; P[hot] = (P * (Lh / np.maximum(L, 1))[..., None])[hot]
    F = max(.50 * Lext, 62.0); L = lum(P); Lk = float(np.median(L[mask])) if key == 'garage' else meas['groups']['garage']['median_ref']; dark = mask & (L < Lk)  # 흙(어두운 바퀴 자국)은 정비소 바닥 기준으로
    Lt = F + np.clip(L / Lk, 0, 1) * (Lk - F)
    up = P * np.clip(Lt / np.maximum(L, 1), 1, 4)[..., None]
    need = np.clip((Lt - lum(up)) / np.maximum(Lt, 1), 0, 1)[..., None]  # 4배로도 모자란 거의 검정 → 그늘색으로 채움
    up = up * (1 - need) + (TINT * (Lt / lum(TINT))[..., None]) * need
    up = up * .9 + TINT * (lum(up) / lum(TINT))[..., None] * .1  # 들어올린 픽셀만 청록·갈색 10%
    P[dark] = up[dark]; P = np.clip(P, 0, 255)
    flat = P[mask]; idx = np.empty(len(flat), np.int32)
    for s0 in range(0, len(flat), 1 << 17): idx[s0:s0 + (1 << 17)] = ((flat[s0:s0 + (1 << 17), None] - pal[None]) ** 2).sum(2).argmin(1)
    orig32 = px(os.path.join(E, key + '_baked32.png')) * 255; out = orig32.copy(); out[mask] = pal[idx]
    im = bpy.data.images.new(key + '_v5b', W, H, alpha=False)
    im.pixels.foreach_set(np.concatenate([out / 255, np.ones((H, W, 1))], 2).astype(np.float32).ravel())
    im.filepath_raw = os.path.join(OUT, key + '_baked32.png'); im.file_format = 'PNG'; im.save()
    shutil.copy2(os.path.join(E, key + '_v5_combo.fbx'), os.path.join(OUT, key + '_v5_combo.fbx'))
    os.makedirs(os.path.join(OUT, key + '_v5_combo.fbm'), exist_ok=True); shutil.copy2(os.path.join(OUT, key + '_baked32.png'), os.path.join(OUT, key + '_v5_combo.fbm', key + '_baked32.png'))
    np.save(os.path.join(WORK, key + '_interior_mask.npy'), mask)
    meas['groups'][key] = dict(interior_px=int(mask.sum()), interior_faces=nf, before_v5_32=st(orig32, mask), after_v5b_32=st(out, mask), k_added_light=round(k, 3), median_ref=round(Lk, 1),
                               hot_px_compressed=int(hot.sum()), lifted_px=int(dark.sum()), floor=round(F, 1), changed_px_outside_mask=int((np.abs(out - orig32).sum(2) > 0)[~mask].sum()),
                               v5_replica_abs_diff=round(float(np.abs(P_old - P_file).mean()), 3))
    print('MEAS', key, meas['groups'][key], flush=True)
shutil.copy2(os.path.join(E, 'palette32.gpl'), os.path.join(OUT, 'palette32.gpl'))
meas['lights'] = [dict(name=l.name, loc=[round(v, 2) for v in l.location], power_W=l.data.energy, color=[round(c, 2) for c in l.data.color], size=[l.data.size, l.data.size_y]) for l in LIGHTS]
json.dump(meas, open(os.path.join(OUT, 'measure.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, 'v5b-baked.blend'), copy=True)
print('GARAGE DONE', round(time.time() - t0), flush=True)
