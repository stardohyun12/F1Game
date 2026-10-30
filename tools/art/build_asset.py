# -*- coding: utf-8 -*-
"""건물 하나를 원본에서 최종까지 한 번에 다시 만든다 (artlive 2041). 지금은 depot 만 구현.

  python -X utf8 tools/art/build_asset.py depot              # 전 단계
  python -X utf8 tools/art/build_asset.py depot --from grain # 그 단계부터(앞 단계 산출은 export 에 있는 그대로)

단계(함수 하나 = 단계 하나, 끝날 때마다 docs/qa/tmp/build/<건물>/NN_<단계>/ 에 fbx·png 사본):
  source   원본 생성: icons_shared.py → art/source/q3-depot/assemble.py(키트 텍스처 복사·조립) → optimize.py(묶음)
  overlap  겹친 면 제거: tools/asset-gen/fix_1846.py depot fix (낱개 분리가 "겹침 고친 fbx" 를 입력으로 받으므로 먼저)
  split    낱개 분리·이름·실제 시계: tools/asset-gen/split_1857.py depot + 벽 옷걸이 작업복(옛 PARCEL_21~26) → JACKET_01·02
  snap     접지: tools/asset-gen/snap_1950.py (PARCEL 바닥 → 아래 받침 윗면, 틈·파묻힘 0)
  uv       원통 16면 · 텍셀 32/64 · 점 UV 펴기 · 테이프 크림 띠 (tools/art/build_depot_bpy.py uv)
  grain    D-196 색(벽 회청·지붕 슬레이트·테이프 크림) + 사진 결(명도 4단, 칠 ±4/±10%, 고무 ±5/±14%) + 글씨 텍스처 64px/m
  quant    칸마다 median-cut → 합친 색을 그 건물 palette.gpl 로 쓰고 전 텍스처를 그 색으로(팔레트 일치 100%)
  unity    export → F1/Assets/Art/Library/Buildings/<건물> 복사 → docs/qa/tmp/library_import.cs(ONLY=depot) → 프리팹  (에디터 = F1Studio, 환경변수 UCLI_PROJ 로 바꿈)
  viewer   tools/viewer/export_glb.py <건물>
  check    tools/art/check_rules.py --only <건물> --gate (텍셀·원통·글씨 포함) → docs/art/rule-audit/last_<건물>.md
사진(CC0, Poly Haven): white_plaster_02, rubberized_track — 출처 grain_1722/1736 과 같음. AI 이미지 없음.
"""
import sys, os, json, shutil, subprocess, hashlib, time, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

R = Path(__file__).resolve().parents[2]
BL = os.environ.get('BLENDER', 'D:/Tools/Blender/blender.exe')
CFG = {'depot': dict(src=R / 'art/source/q3-depot', exp=R / 'art/export/q3-depot', lib=R / 'F1/Assets/Art/Library/Buildings/depot', fbx='q3_depot.fbx',
                     pngs=['wall', 'trim', 'floor', 'roof', 'decals', 'icons', 'clock_dial']),
       # 2117 정비소: 원본 = garage.blend(1653 16각·전등, 1709 바닥·앞마당·포스터 UV, 1846 타이어 이동이 들어 있음) + 텍스처 생성기 3개
       'garage': dict(src=R / 'art/source/q2-garage', exp=R / 'art/export/q2-garage', lib=R / 'F1/Assets/Art/Library/Buildings/garage', fbx='garage_v5_combo.fbx',
                      pngs=['garage_baked32', 'garage_details', 'dirt_baked32', 'garage_props', 'garage_lamp', 'garage_sign', 'garage_festival',
                            'garage_posters', 'garage_memory_1', 'garage_memory_2', 'garage_memory_3'],
                      paint=['garage_baked32', 'garage_details', 'dirt_baked32', 'garage_props', 'garage_lamp'],  # 칠·고무 결(D-197)
                      paper=['garage_sign', 'garage_festival', 'garage_posters'],  # 글씨 텍스처: 종이(밝은 픽셀)만 결, 잉크는 그대로
                      lib_name={'dirt_baked32': 'garage_dirt'}),
       # 2230 우체국: 원본 = tools/asset-gen/q4_postoffice.py(키트 조립 생성기). 1846 export 덧칠(창 구멍·유리, 겹친 면 띄움)은 생성기·overlap 단계로
       'postoffice': dict(src=R / 'tools/asset-gen', exp=R / 'art/export/q4-postoffice', lib=R / 'F1/Assets/Art/Library/Buildings/postoffice', fbx='q4_postoffice.fbx',
                          pngs=['wall', 'trim', 'floor', 'roof', 'decals', 'icons', 'postbox_G1'], lib_name={}, roll=(37, 71)),
       # 2303 주인공 방: 원본 = tools/asset-gen/q1_room.py(사진 결·palette_q1v2 양자화까지 생성기 안). 옛 grain_1736 export 덧칠은 생성기로
       'room': dict(src=R / 'tools/asset-gen', exp=R / 'art/export/q1-room', lib=R / 'F1/Assets/Art/Library/Buildings/room', fbx='room.fbx', pngs=['room'], lib_name={}),
       # 0031 주인공 2층집: 원본 = tools/asset-gen/house_home.py + 아래 h_grain(사진 칠). 2층 = room 프리팹(자식)
       'house_home': dict(src=R / 'tools/asset-gen', exp=R / 'art/export/house-home', lib=R / 'F1Studio/Assets/Art/Review/house_home', fbx='house_home.fbx',
                          pngs=['house_home'], lib_name={}),
       # artlive 2056 밭: 원본 = tools/asset-gen/field_patch.py + 아래 f_grain(사진 칠). 납품 = Library/Buildings/field_patch(F1Studio 와 정션)
       'field_patch': dict(src=R / 'tools/asset-gen', exp=R / 'art/export/field-patch', lib=R / 'F1/Assets/Art/Library/Buildings/field_patch', fbx='field_patch.fbx',
                           pngs=['field_patch'], lib_name={})}
PLAIN = R / 'art/textures/2026-10-24-kp/sources/candidate_white_plaster_02/white_plaster_02_diff_1k.png'
RUBBER = R / 'art/source/car-v01/photos/rubberized_track_diff_1k.jpg'
STEPS = {'paint': np.array([-0.10, -0.04, 0.04, 0.10]), 'rubber': np.array([-0.14, -0.05, 0.05, 0.14])}
D196 = {'wall_cream': (176, 188, 194), 'roof_E2': (118, 116, 110)}  # 바랜 회청 벽(1914 와 같은 값) · 우체국 E3 기와와 다른 슬레이트 회색

def run(cmd, env=None):
    t = time.time(); p = subprocess.run(cmd, cwd=R, capture_output=True, text=True, encoding='utf-8', errors='replace', env={**os.environ, **(env or {})})
    tail = [l for l in (p.stdout + p.stderr).splitlines() if l.strip()][-3:]
    print('   $', ' '.join(str(c) for c in cmd)[-110:], f'({time.time() - t:.0f}s, exit {p.returncode})'); [print('    ', l[:160]) for l in tail]
    if p.returncode != 0 or 'Traceback' in p.stdout + p.stderr: raise SystemExit(f'단계 실패: {cmd}')
    return p.stdout + p.stderr
def blender(script, *args, env=None): return run([BL, '-b', '--factory-startup', '-P', str(script), '--', *args], env)
def snapshot(c, n, name):
    d = R / f'docs/qa/tmp/build/{c["name"]}/{n:02d}_{name}'; d.mkdir(parents=True, exist_ok=True)
    for p in [c['exp'] / c['fbx']] + [c['exp'] / (s + '.png') for s in c['pngs']] + [c['exp'] / 'palette.gpl']:
        if p.exists(): shutil.copy2(p, d / p.name)
    print(f'  → {d.relative_to(R)}')

# ---------- 단계 ----------
def s_source(c):
    run([sys.executable, '-X', 'utf8', 'tools/asset-gen/icons_shared.py'])  # 보관소·우체국 공용 아이콘(같은 바이트로 다시 씀)
    blender(c['src'] / 'assemble.py', env={'NO_RENDER': '1'}); blender(c['src'] / 'optimize.py')
def s_overlap(c): blender(R / 'tools/asset-gen/fix_1846.py', c['name'], 'fix')
def s_split(c):
    blender(R / 'tools/asset-gen/split_1857.py', c['name'])
    blender(R / 'tools/art/build_depot_bpy.py', str(c['exp'] / c['fbx']), 'names', str(c['tmp'] / 'names.json'))
def s_snap(c):
    blender(R / 'tools/asset-gen/snap_1950.py'); shutil.copy2(R / 'docs/qa/tmp/snap_1950.json', c['tmp'] / 'snap.json')
    blender(R / 'tools/art/depot_clearance.py', str(c['exp'] / c['fbx']), 'fix', str(c['tmp'] / 'clearance.json'))
def s_uv(c): blender(R / 'tools/art/build_depot_bpy.py', str(c['exp'] / c['fbx']), 'uv', str(c['tmp'] / 'uv.json'))

_F = {}
def field(kind, w, h):  # 사진 명도 결(grain_1736 과 같은 꼴: 잔결 + 픽셀 대역 0.8 + 큰 얼룩 0.3), 표준편차 1
    if kind not in _F:
        p = Image.open(RUBBER if kind == 'rubber' else PLAIN).convert('L').resize((128, 128), Image.LANCZOS); a = np.asarray(p, np.float64)
        n = lambda x: (x - x.mean()) / x.std()
        b1 = np.asarray(p.filter(ImageFilter.GaussianBlur(1)), np.float64); b8 = np.asarray(p.filter(ImageFilter.GaussianBlur(8)), np.float64)
        _F[kind] = n(n(a / np.maximum(b8, 1) - 1) + 0.8 * n(a / np.maximum(b1, 1) - 1) + 0.3 * n(b8 / b8.mean() - 1))
    g = _F[kind]; return np.tile(g, (h // 128 + 1, w // 128 + 1))[:h, :w]
def lay(c): return json.loads((c['exp'] / 'layout.json').read_text(encoding='utf-8'))
def s_grain(c):
    E = c['exp']; L = lay(c); uvj = json.loads((c['tmp'] / 'uv.json').read_text(encoding='utf-8'))
    # 2148: 입력은 생성기 산출 스냅샷으로 고정(정비소와 같게) — 텍스처를 만드는 건 source(나머지 전부)와 split(clock_dial)뿐이고 uv 는 UV 만 옮긴다.
    # 옛날엔 04_uv 를 썼는데, --from split/uv 재실행이면 결 입힌 export 가 04_uv 로 찍혀 결이 두 번 들어갔다.
    for s in c['pngs']:
        sp = c['tmp'] / ('02_split' if s == 'clock_dial' else '00_source') / (s + '.png')
        if not sp.exists(): raise SystemExit(f'{sp} 없음 — source 단계부터 한 번 돌릴 것')
        shutil.copy2(sp, E / sp.name)
    band = int(uvj.get('cream_building_top_row') or 59)  # 위 [0,band) = 테이프 크림(uv 단계가 행 4~56 에 둠), 아래 = 건물(벽·천장 6.1m) 회청
    log = {'cream_band_rows': [0, band]}
    for s in ('wall', 'roof', 'floor', 'trim'):
        a = np.asarray(Image.open(E / (s + '.png')).convert('RGBA'), np.float64); rgb = a[..., :3]
        for cell in L[s]:  # D-196
            if cell['name'] not in D196: continue
            x, y, w, h = cell['rect_px']; y0 = y + (band if cell['name'] == 'wall_cream' else 0)
            reg = rgb[y0:y + h, x:x + w]; m = reg.reshape(-1, 3).mean(0); reg *= np.array(D196[cell['name']]) / m
            log[cell['name']] = [[round(v) for v in m], D196[cell['name']]]
        lum = rgb @ [.299, .587, .114]; H, W = lum.shape
        for kind in ('paint', 'rubber'):
            sel = (lum < 70) if kind == 'rubber' else (lum >= 70)
            st = STEPS[kind]; f = field(kind, W, H) * (st[2] + st[3]) / 2
            q = st[np.abs(f[..., None] - st).argmin(-1)]; rgb[sel] *= (1 + q[sel])[:, None]
        a[..., :3] = np.clip(rgb, 0, 255); Image.fromarray(a.round().astype(np.uint8)).save(E / (s + '.png'))
    # 글씨 텍스처: 세상 64px/m 격자로(Nearest). 아이콘 256→88(택배 라벨·전표 ≈77px/m · 간판 0.76~0.84m 판 ≈52~57 — 80 이면 큰 판이 47 로 범위 48~80 밖), 데칼 512→160(≈66)
    for s, n in (('icons', 88), ('decals', 160)):
        Image.open(E / (s + '.png')).convert('RGBA').resize((n, n), Image.NEAREST).save(E / (s + '.png')); log[s] = n
    # 시계 바늘은 uv 단계가 trim 으로 옮긴다(2058) → clock_dial.png 는 문자판 그대로
    (c['tmp'] / 'grain.json').write_text(json.dumps(log, ensure_ascii=False, indent=1), encoding='utf-8')

def s_quant(c):
    E = c['exp']; L = lay(c); allc = set(); log = {}
    for s in c['pngs']:
        im = Image.open(E / (s + '.png')).convert('RGBA'); a = np.asarray(im).copy(); H, W = a.shape[:2]
        if s in ('icons', 'decals', 'clock_dial'): a[..., 3] = np.where(a[..., 3] >= 128, 255, 0); rects = [[0, 0, W, H]]; N = 24
        elif s not in L: rects = [[x, y, 128, 128] for y in range(0, H, 128) for x in range(0, W, 128)]; N = 32  # 2230: 칸 표 밖 텍스처(우체통)
        else: rects = [cl['rect_px'] for cl in L[s]]; N = 32 if s != 'trim' else 16
        for x, y, w, h in rects:
            reg = a[y:y + h, x:x + w]; op = reg[..., 3] >= 128
            if op.sum() == 0: continue
            px = Image.fromarray(reg[..., :3][op][None, :, :].astype(np.uint8))
            qi = px.quantize(colors=N, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
            pal = np.array(qi.getpalette()[:3 * N], np.uint8).reshape(-1, 3); idx = np.asarray(qi)[0]
            out = reg[..., :3].copy(); out[op] = pal[idx]; reg[..., :3] = out
            allc |= {tuple(int(v) for v in p) for p in pal[np.unique(idx)]}
        a[a[..., 3] < 128, :3] = 0
        Image.fromarray(a).save(E / (s + '.png')); log[s] = int(len(np.unique(a[a[..., 3] >= 128][:, :3], axis=0)))
    lines = ['GIMP Palette', f'Name: {c["exp"].name} (build_asset.py 2041 — 결 뒤 칸별 median-cut 합집합)', 'Columns: 8', '#']
    lines += ['%3d %3d %3d\tc%03d' % (*p, i) for i, p in enumerate(sorted(allc))]
    (E / 'palette.gpl').write_text('\n'.join(lines) + '\n', encoding='utf-8'); log['palette'] = len(allc)
    (c['tmp'] / 'quant.json').write_text(json.dumps(log, indent=1), encoding='utf-8'); print('   colors', log)

def s_unity(c):
    E, Lb = c['exp'], c['lib']
    for p in [E / c['fbx']] + [E / (s + '.png') for s in c['pngs']]: shutil.copy2(p, Lb / p.name)
    out = unity_exec((R / 'docs/qa/tmp/library_import.cs').read_text(encoding='utf-8')); print('  ', out[:300])
    if 'error' in out.lower() and 'OK' not in out: raise SystemExit('Unity 임포트 실패')
UCLI_PROJ = os.environ.get('UCLI_PROJ') or str(R / 'F1Studio')  # 건물 스튜디오 전용 프로젝트(정션으로 Art/Library 공유) — 메인 F1 에디터는 쓰지 않는다
def unity_exec(code):  # scripts/ucli-http.sh exec 과 같은 HTTP — 파이썬에서 bash 를 띄우면 0xC0000005 로 죽어서 직접 보낸다
    import glob, urllib.request
    port = None; proj = UCLI_PROJ.replace('\\', '/').rstrip('/').lower()
    for f in glob.glob(os.path.expanduser('~/.unity-cli/instances/*.json')):
        try: d = json.load(open(f, encoding='utf-8'))
        except Exception: continue
        if d.get('projectPath', '').replace('\\', '/').rstrip('/').lower() == proj and d.get('state') != 'stopped': port = d['port']; break
    if not port: raise SystemExit(f'{os.path.basename(proj)} Editor connector 없음 ({UCLI_PROJ}) — powershell -NoProfile -File scripts/open-studio.ps1')
    # 스튜디오 씬이 아니면 연다(library_import.cs 는 STUDIO/STUDIO_Slot 이 필요) — studio_cap.sh 와 같은 스니펫
    body = json.dumps({'command': 'exec', 'params': {'code': (R / 'docs/qa/tmp/studio_open.cs').read_text(encoding='utf-8')}}).encode()
    urllib.request.urlopen(urllib.request.Request(f'http://127.0.0.1:{port}/command', data=body, headers={'Content-Type': 'application/json'}), timeout=120).read()
    body = json.dumps({'command': 'exec', 'params': {'code': code}}).encode()
    d = json.loads(urllib.request.urlopen(urllib.request.Request(f'http://127.0.0.1:{port}/command', data=body, headers={'Content-Type': 'application/json'}), timeout=280).read())
    if not d.get('success'): raise SystemExit('Unity exec 실패: ' + str(d.get('message')))
    o = d.get('data'); return o if isinstance(o, str) else json.dumps(o, ensure_ascii=False)
def s_viewer(c): blender(R / 'tools/viewer/export_glb.py', c['name'])
def s_check(c):
    blender(R / 'tools/art/depot_clearance.py', str(c['exp'] / c['fbx']), 'check', str(c['tmp'] / 'clearance_final.json'))
    p = subprocess.run([sys.executable, '-X', 'utf8', 'tools/art/check_rules.py', '--only', c['name'], '--gate', '--md', f'docs/art/rule-audit/last_{c["name"]}.md',
                        '--json', str(c['tmp'] / 'rules.json')], cwd=R, capture_output=True, text=True, encoding='utf-8', errors='replace')
    print('\n'.join(p.stdout.splitlines()[-3:]))
    if p.returncode != 0 or 'GATE PASS' not in p.stdout:
        raise SystemExit('보관소 규칙 게이트 실패: ' + p.stderr[-500:])
    po = R / 'art/export/q4-postoffice'; md5 = lambda f: hashlib.md5(f.read_bytes()).hexdigest()
    same = [s for s in ('wall', 'roof', 'trim', 'floor', 'icons', 'decals') if (po / (s + '.png')).exists() and md5(po / (s + '.png')) == md5(c['exp'] / (s + '.png'))]
    print('   우체국과 md5 같은 텍스처:', same or '없음')

# ---------- 정비소(garage) 단계 — 2117: 그날 export 에만 덧칠됐던 수정을 원본·단계로 ----------
def g_source(c):  # 텍스처: textures.py(지붕 사진·세부·간판·게시판·사진 액자) → fix_textures_v2.py(바닥·흙) → props_1709.py(소품 64px/m·포스터·게시판, 공구 1.3배)
    py = os.environ.get('PY_CV2', os.path.expanduser('~/AppData/Local/Programs/Python/Python313/python.exe'))  # textures.py 가 cv2 를 씀
    for s in ('textures.py', 'fix_textures_v2.py', 'props_1709.py'): run([py if os.path.exists(py) else sys.executable, '-X', 'utf8', str(c['src'] / s)])
def g_overlap(c):  # 겹침(바닥·앞마당 z 0.03/0.02/0.01)은 fix_1709 가 garage.blend 에 반영. 2130: 원통 16면(의자 다리·플라이휠)을 원본에 한 번만
    blender(c['src'] / 'fix_2130_cyl.py'); blender(c['src'] / 'fix_2148_uv.py')  # 2148: UV 축척(누운 드럼·판자 띠·탁자 받침)·간판 옆면
def g_split(c): blender(c['src'] / 'split_1857.py')  # 타이어 이동(q2_moved_1846 표시, 한 번만) + 물건별 이름 TIRE_/DRUM_/POSTER_… + 피벗 바닥 가운데 → fbx
def g_snap(c): print('   정비소: 접지 단계 없음(물건 피벗은 split 이 바닥 가운데로)')
def g_uv(c):
    blender(R / 'tools/art/build_garage_bpy.py', str(c['exp'] / c['fbx']), str(c['tmp'] / 'uv.json'))
def g_grain(c):
    E = c['exp']; log = {}
    # 2130: 입력은 source 스냅샷(정비소 uv 단계는 텍스처를 안 건드림). 옛날처럼 04_uv 를 쓰면 --from split 재실행 때 결 입힌 export 가
    # 04_uv 로 찍혀 결이 두 번·세 번 들어갔다(반점 2%→44%). → --from 은 source 를 한 번 돌린 뒤에만 의미 있음.
    for s in c['pngs']:
        sp = c['tmp'] / '00_source' / (s + '.png')
        if sp.exists() and s != 'garage_lamp': shutil.copy2(sp, E / sp.name)
    Image.new('RGBA', (32, 32), (250, 228, 178, 255)).save(E / 'garage_lamp.png')  # 전등 원판 전용(옛: props 사본 한 점)
    for s in c['paint'] + c['paper']:
        a = np.asarray(Image.open(E / (s + '.png')).convert('RGBA'), np.float64); rgb = a[..., :3]
        lum = rgb @ [.299, .587, .114]; H, W = lum.shape; op = a[..., 3] >= 128
        for kind in ('paint', 'rubber'):
            if s in c['paper']: sel = op & (lum >= 120) if kind == 'paint' else np.zeros_like(op)
            else: sel = op & ((lum < 70) if kind == 'rubber' else (lum >= 70))
            st = STEPS[kind]; f = field(kind, W, H) * (st[2] + st[3]) / 2
            q = st[np.abs(f[..., None] - st).argmin(-1)]; rgb[sel] *= (1 + q[sel])[:, None]
        a[..., :3] = np.clip(rgb, 0, 255); Image.fromarray(a.round().astype(np.uint8)).save(E / (s + '.png')); log[s] = [W, H]
    (c['tmp'] / 'grain.json').write_text(json.dumps(log, indent=1), encoding='utf-8')
def g_quant(c):  # 128px 칸마다 median-cut 32색 → 합친 색 = art/export/q2-garage/palette.gpl
    E = c['exp']; allc = set(); log = {}
    for s in c['pngs']:
        a = np.asarray(Image.open(E / (s + '.png')).convert('RGBA')).copy(); H, W = a.shape[:2]; a[..., 3] = np.where(a[..., 3] >= 128, 255, 0)
        for y in range(0, H, 128):
            for x in range(0, W, 128):
                reg = a[y:y + 128, x:x + 128]; op = reg[..., 3] >= 128
                if op.sum() == 0: continue
                qi = Image.fromarray(reg[..., :3][op][None, :, :]).quantize(colors=32, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
                pal = np.array(qi.getpalette()[:96], np.uint8).reshape(-1, 3); idx = np.asarray(qi)[0]
                out = reg[..., :3].copy(); out[op] = pal[idx]; reg[..., :3] = out
                allc |= {tuple(int(v) for v in p) for p in pal[np.unique(idx)]}
        a[a[..., 3] < 128, :3] = 0; Image.fromarray(a).save(E / (s + '.png')); log[s] = int(len(np.unique(a[a[..., 3] >= 128][:, :3], axis=0)))
    lines = ['GIMP Palette', 'Name: q2-garage (build_asset.py 2117 — 결 뒤 128px 칸별 median-cut 합집합)', 'Columns: 8', '#']
    lines += ['%3d %3d %3d\tc%03d' % (*p, i) for i, p in enumerate(sorted(allc))]
    (E / 'palette.gpl').write_text('\n'.join(lines) + '\n', encoding='utf-8'); log['palette'] = len(allc)
    (c['tmp'] / 'quant.json').write_text(json.dumps(log, indent=1), encoding='utf-8'); print('   colors', log)
def g_unity(c):
    E, Lb = c['exp'], c['lib']; shutil.copy2(E / c['fbx'], Lb / c['fbx'])
    for s in c['pngs']: shutil.copy2(E / (s + '.png'), Lb / (c['lib_name'].get(s, s) + '.png'))
    code = (R / 'docs/qa/tmp/library_import.cs').read_text(encoding='utf-8').replace('var ONLY = new[] { "depot" };', 'var ONLY = new[] { "garage" };')
    out = unity_exec(code); print('  ', out[:300])
def g_check(c):
    p = subprocess.run([sys.executable, '-X', 'utf8', 'tools/art/check_rules.py', '--only', c['name'], '--gate', '--md', f'docs/art/rule-audit/last_{c["name"]}.md',
                        '--json', str(c['tmp'] / 'rules.json')], cwd=R, capture_output=True, text=True, encoding='utf-8', errors='replace')
    print('\n'.join(p.stdout.splitlines()[-3:]))
    md5 = lambda f: hashlib.md5(f.read_bytes()).hexdigest(); mine = {md5(c['lib'] / (c['lib_name'].get(s, s) + '.png')): s for s in c['pngs']}
    same = sorted({f'{mine[md5(f)]}={f.relative_to(R / "F1/Assets/Art")}' for f in (R / 'F1/Assets/Art').rglob('*.png') if f.parent != c['lib'] and f.stat().st_size < 8e6 and md5(f) in mine})
    print('   다른 건물과 md5 같은 텍스처(D-196):', same or '없음')
    return p
# ---------- 우체국(postoffice) 단계 — 2230 ----------
def p_source(c):
    run([sys.executable, '-X', 'utf8', 'tools/asset-gen/icons_shared.py'])
    out = blender(c['src'] / 'q4_postoffice.py', '--no-render')  # 키트 텍스처 복사 + 조립 + 낱개 이름 + 유리 + 1~3cm 띄움 + 16면 → fbx·blend
def p_overlap(c):  # 남은 겹침(틀·창살 상자끼리 교차)은 작은 쪽 1cm 띄움 → 같은 방향 0 확인, 마주 보는 면(안쪽 캠)은 보고
    blender(R / 'tools/asset-gen/fix_1846.py', c['name'], 'fix'); shutil.copy2(R / 'docs/qa/tmp/fix_1846_postoffice_fix.json', c['tmp'] / 'overlap_fix.json')
    blender(R / 'tools/asset-gen/fix_1846.py', c['name'], 'check', 'both'); shutil.copy2(R / 'docs/qa/tmp/fix_1846_postoffice_check_both.json', c['tmp'] / 'overlap_both.json')
    for f in ('overlap_fix.json', 'overlap_both.json'):
        d = json.loads((c['tmp'] / f).read_text(encoding='utf-8')); d = d.get('after') or d['before']; print(f'   {f}: 겹친 쌍 {d["pairs"]} · 같은 덩어리 중복 {d["same_island_dups"]}')
def p_split(c): print('   우체국: 낱개(MAIL_·CHAIR_·POBOX_DOOR_·BIKE_·POSTBOX_·POT_·PLANTER_·COUNTER_·SHELF_)는 생성기가 이름·피벗까지 만든다')
def p_snap(c): print('   우체국: 접지는 생성기 좌표(의자 다리·소포는 받침에 0.5~1.5cm 박음)')
def p_uv(c): print('   우체국: 원통 16면·텍셀 32/64(px_for)는 생성기에서')
def p_grain(c):
    E = c['exp']; log = {}; ry, rx = c['roll']
    for s in c['pngs']:  # 입력 = source 스냅샷(결 중복 X)
        sp = c['tmp'] / '00_source' / (s + '.png')
        if not sp.exists(): raise SystemExit(f'{sp} 없음 — source 단계부터 한 번 돌릴 것')
        shutil.copy2(sp, E / sp.name)
    im = Image.open(E / 'postbox_G1.png').convert('RGBA'); im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(E / 'postbox_G1.png')  # 우체통 = 작은 물건 64px/m(원래 32)
    for s in ('wall', 'roof', 'floor', 'trim', 'postbox_G1'):  # 사진 결 — 보관소와 같은 사진, 결 위치를 (37,71)px 밀어 바이트가 다르게(D-196)
        a = np.asarray(Image.open(E / (s + '.png')).convert('RGBA'), np.float64); rgb = a[..., :3]; lum = rgb @ [.299, .587, .114]; H, W = lum.shape
        # 2251: 사서함 손잡이·막대(trim gutter 칸, 명도 80)는 ±10% 결이 양자화 뒤 잔결 0.023~0.027 로 기준 0.027 아래 → 이 칸만 고역 결 ±6·±12%(D-197 범위 안)
        boost = np.zeros((H, W), bool)
        if s == 'trim':
            for cc in json.loads((E / 'layout.json').read_text(encoding='utf-8'))['trim']:
                if cc['name'] == 'gutter': x0, y0, w0, h0 = cc['rect_px']; boost[y0:y0 + h0, x0:x0 + w0] = True
        for kind in ('paint', 'rubber'):
            sel = (lum < 70) if kind == 'rubber' else (lum >= 70)
            st = STEPS[kind]; f = np.roll(field(kind, W + 128, H + 128), (ry, rx), (0, 1))[:H, :W] * (st[2] + st[3]) / 2
            q = st[np.abs(f[..., None] - st).argmin(-1)]
            if kind == 'paint' and boost.any():  # 손잡이는 2~3px 면이라 큰 얼룩 성분이 한 면을 한 색으로 덮는다 → 같은 사진의 잔결(고역)만 써서 ±12%
                fp = np.pad(f, 1, mode='edge'); fh = f - sum(fp[i:i + H, j:j + W] for i in range(3) for j in range(3)) / 9; fh = fh / fh[boost].std() * (st[2] + st[3]) / 2
                stb = np.array([-0.12, -0.06, 0.06, 0.12]); q = np.where(boost, stb[np.abs(fh[..., None] - stb).argmin(-1)], q)
            rgb[sel] *= (1 + q[sel])[:, None]
        a[..., :3] = np.clip(rgb, 0, 255); Image.fromarray(a.round().astype(np.uint8)).save(E / (s + '.png')); log[s] = [W, H]
    for s, n in (('icons', 88), ('decals', 160)):  # 글씨 텍스처 64px/m 격자(보관소와 같은 축척)
        Image.open(E / (s + '.png')).convert('RGBA').resize((n, n), Image.NEAREST).save(E / (s + '.png')); log[s] = n
    # 데칼(전단·포스터): 종이(밝은 픽셀)만 결 — 정비소 글씨 텍스처와 같은 방식. 안 하면 보관소 decals 와 바이트가 같다(D-196, 첫 실행에서 걸림)
    a = np.asarray(Image.open(E / 'decals.png').convert('RGBA'), np.float64); rgb = a[..., :3]; H, W = rgb.shape[:2]
    sel = (a[..., 3] >= 128) & ((rgb @ [.299, .587, .114]) >= 120); st = STEPS['paint']
    f = np.roll(field('paint', W + 128, H + 128), (ry, rx), (0, 1))[:H, :W] * (st[2] + st[3]) / 2; q = st[np.abs(f[..., None] - st).argmin(-1)]
    rgb[sel] *= (1 + q[sel])[:, None]; a[..., :3] = np.clip(rgb, 0, 255); Image.fromarray(a.round().astype(np.uint8)).save(E / 'decals.png')
    (c['tmp'] / 'grain.json').write_text(json.dumps(log, indent=1), encoding='utf-8')
def p_unity(c):
    E, Lb = c['exp'], c['lib']
    for p in [E / c['fbx']] + [E / (s + '.png') for s in c['pngs']]: shutil.copy2(p, Lb / p.name)
    out = unity_exec((R / 'docs/qa/tmp/library_import.cs').read_text(encoding='utf-8').replace('var ONLY = new[] { "depot" };', 'var ONLY = new[] { "postoffice" };')); print('  ', out[:300])
# ---------- 주인공 방(room) 단계 — 2303 ----------
def r_source(c):  # Blender 는 생성기 SyntaxError 에도 exit 0 → 끝 줄 MEAS 가 없으면 실패로
    if 'MEAS' not in blender(c['src'] / 'q1_room.py', '--no-render'): raise SystemExit('q1_room.py 가 끝까지 안 돌았다(MEAS 없음)')
    # 낱개 이름·피벗·64 텍셀·1~3cm 띄움·유리·실내 캠·문짝 결 → room.fbx·room.png(palette_q1v2)
def r_overlap(c):
    for m, f in ((('fix',), 'overlap_fix.json'), (('check', 'both'), 'overlap_both.json')):
        blender(R / 'tools/asset-gen/fix_1846.py', 'room', *m); shutil.copy2(R / ('docs/qa/tmp/fix_1846_room_' + '_'.join(m) + '.json'), c['tmp'] / f)
        d = json.loads((c['tmp'] / f).read_text(encoding='utf-8')); d = d.get('after') or d['before']; print(f'   {f}: 겹친 쌍 {d["pairs"]} · 같은 덩어리 중복 {d["same_island_dups"]}')
def r_split(c): print('   방: 낱개(BED_·DESK_·CHAIR_·VEST_·BOOK_ …)는 생성기가 이름·피벗까지 만든다')
def r_snap(c): print('   방: 접지는 생성기 좌표(바닥·책상·선반 윗면에 바로)')
def r_uv(c): print('   방: 텍셀 32/64 는 생성기 _dn(면 ≤1.5m 작은 물건 = 64)')
def r_grain(c): print('   방: 사진 결은 생성기 photo()·문짝 결(±8% 아래쪽)에서. export 덧칠 없음')
def r_quant(c): print('   방: 생성기가 palette_q1v2.gpl 로 양자화')
def copy_retry(a, b, n=6):  # 2318: Unity 가 막 임포트한 png 를 매핑 중이면 WinError 1224 — 몇 초 뒤엔 풀린다
    for i in range(n):
        try: return shutil.copy2(a, b)
        except OSError as e:
            if i == n - 1: raise
            print('   잠김, 다시', b.name, e.winerror); time.sleep(3)
def r_unity(c):
    E, Lb = c['exp'], c['lib']
    for p in [E / c['fbx']] + [E / (s + '.png') for s in c['pngs']]: copy_retry(p, Lb / p.name)
    out = unity_exec((R / 'docs/qa/tmp/library_import.cs').read_text(encoding='utf-8').replace('var ONLY = new[] { "depot" };', 'var ONLY = new[] { "room" };')); print('  ', out[:300])
# ---------- 주인공 2층집(house_home) 단계 — 20261029-0031 ----------
# 원본 = tools/asset-gen/house_home.py(형태·낱개·UV 칸 표 layout.json). 텍스처는 여기 grain 이 칸마다 사진으로 칠한다(빛 없음, D-185).
# 사진(도현 선택 PICKS.md, CC0 — 출처 art/source/photo-picks/SOURCES.md): A2 부라노 살구 벽(벽·흰 틀·문 색), C2 초록 덧창, E2 기와, H1 돌담(기단).
# 방 결(나무 판)은 art/source/q1-room-cc0/fine_grained_wood(Poly Haven CC0). 2층 방은 fbx 에 없다 — Unity 프리팹에서 room 프리팹을 자식(+3.2m)으로.
PP = R / 'art/source/photo-picks'
HPH = {'A2': (PP / 'A/A2_apricot_facade_burano.jpg', (420, 640, 660, 900)), 'C2': (PP / 'C/C2_green_shutters_liguria.jpg', (360, 530, 450, 800)),
       'E2': (PP / 'E/E2_clay_roof_tiles_02.jpg', None), 'H1': (PP / 'H/H1_round_stone_wall.jpg', None),
       'WOOD': (R / 'art/source/q1-room-cc0/fine_grained_wood.jpg', None)}
HCROP = {'trim': ('A2', (690, 1080, 708, 1300)), 'door': ('A2', (460, 1130, 590, 1460))}  # 흰 창틀 · 나무 문(사진 속 같은 집)
def h_photo(k, box=None):
    p, b = HPH[k]; im = Image.open(p).convert('RGB'); return im.crop(box or b) if (box or b) else im
_HF = {}
def h_field(k):  # 사진 명도 결(field() 와 같은 꼴) 128px, 표준편차 1
    if k not in _HF:
        p = h_photo(k).convert('L').resize((128, 128), Image.LANCZOS); a = np.asarray(p, np.float64); n = lambda x: (x - x.mean()) / (x.std() + 1e-6)
        b1 = np.asarray(p.filter(ImageFilter.GaussianBlur(1)), np.float64); b8 = np.asarray(p.filter(ImageFilter.GaussianBlur(8)), np.float64)
        _HF[k] = n(n(a / np.maximum(b8, 1) - 1) + 0.8 * n(a / np.maximum(b1, 1) - 1) + 0.3 * n(b8 / b8.mean() - 1))
    return _HF[k]
def h_med(k, box=None): return np.median(np.asarray(h_photo(k, box), np.float64).reshape(-1, 3), 0)
def h_paint(cell, col):
    x, y, w, h = cell['rect']; d = cell['dens']; pt = cell['pt']; rs = np.random.default_rng(cell['seed'])
    if pt == 'entry_stone':
        # 8a68: a dressed entry slab is not a rubble wall. Keep broad, low-contrast
        # mineral variation; no photographic joints, baked shadows or pixel speckles.
        vv, uu = np.mgrid[0:h, 0:w] / float(d)
        mineral = .025 * np.sin(uu * 3.1 + vv * 1.7) + .018 * np.cos(vv * 4.3 - uu * 1.1)
        return np.array([164., 163., 151.]) * (1 + mineral[..., None])
    if pt in ('roof', 'stone'):  # 사진 albedo 그대로(E2 = 2.0m, H1 = 2.5m 한 장) — 칸마다 다른 자리
        k, span = ('E2', 2.0) if pt == 'roof' else ('H1', 2.5); n = max(8, round(span * d))
        t = np.asarray(h_photo(k).resize((n, n), Image.BOX), np.float64)[::-1]
        t = np.roll(t, (rs.integers(n), rs.integers(n)), (0, 1)); A = np.tile(t, (h // n + 1, w // n + 1, 1))[:h, :w].copy()
        if pt == 'roof': A *= col['roof'] / t.reshape(-1, 3).mean(0)  # 사진 평균색 = E2 중앙값(밝기만 사진 그대로)
        return A
    base = {'wall': 'wall', 'trim': 'trim', 'fascia': 'trim', 'door': 'door', 'shutter': 'shutter', 'pavers': 'terra', 'terracotta': 'terra', 'ridge': 'ridge',
            'soffit': 'soffit', 'iron': 'iron', 'soil': 'soil', 'leaf': 'leaf', 'flower': 'flower', 'brass': 'brass',
            'pane': 'pane', 'curtain': 'curtain', 'chair': 'soffit',  # 0052: 창 유리 안쪽(어두운 방) · 부엌 커튼 · 나무 의자
            'plaster': 'plaster', 'ceil': 'ceil', 'wainlow': 'wain', 'rwall': 'rwall', 'boards': 'boards', 'glass': 'glass', 'steel': 'steel', 'sinktop': 'steel',
            'stovetop': 'stove', 'cabinet': 'cabinet', 'counter': 'trim', 'sofa': 'sofa', 'cushion': 'cushion', 'apron': 'apron', 'basket': 'basket', 'mat': 'mat',  # 0351 집 안
            'plastic': 'plastic', 'ceramic': 'ceramic', 'coffee': 'coffee', 'shoecab': 'shoecab', 'sole': 'sole', 'leather': 'leather', 'hand': 'sole', 'shade': 'shade',
            'bulb': 'bulb', 'rug': 'rug', 'pepper': 'pepper', 'pan': 'pan', 'pantop': 'pan', 'fridge': 'fridge', 'fridgef': 'fridge', 'bin': 'bin', 'bedding': 'bedding',
            'quilt': 'quilt', 'drawer': 'soffit', 'wardrobe': 'wardrobe', 'wardrobef': 'wardrobe', 'dresserf': 'soffit', 'mirror': 'mirror', 'clockface': 'clockface',
            'paper': 'paper', 'spice1': 'spice1', 'spice2': 'spice2', 'spice3': 'spice3', 'jar': 'jar', 'cereal': 'cereal', 'cerealf': 'cereal', 'bagcloth': 'bagcloth',
            'cover': 'cover', 'remote': 'sole', 'cushionB': 'cushionB', 'blanket': 'blanket', 'pic1': 'pic1', 'pic2': 'pic2', 'calpage': 'paper',
            'bk1': 'bk1', 'bk2': 'bk2', 'bk3': 'bk3', 'bk4': 'bk4', 'bk5': 'bk5', 'bk6': 'bk6', 'clothA': 'clothA', 'clothB': 'clothB', 'clothC': 'clothC', 'clothD': 'clothD'}[pt]  # 0133 생활감
    A = np.ones((h, w, 3)) * col[base]
    fk = {'shutter': 'C2', 'door': 'WOOD', 'soffit': 'WOOD', 'chair': 'WOOD', 'boards': 'WOOD', 'basket': 'WOOD', 'cabinet': 'C2',
          'shoecab': 'WOOD', 'drawer': 'WOOD', 'wardrobe': 'WOOD', 'wardrobef': 'WOOD', 'dresserf': 'WOOD'}.get(pt, 'A2')
    f = np.roll(np.tile(h_field(fk), (h // 128 + 2, w // 128 + 2)), (rs.integers(128), rs.integers(128)), (0, 1))[:h, :w]
    if pt in ('door', 'soffit', 'chair', 'boards'): f = np.rot90(np.tile(h_field(fk), (w // 128 + 2, h // 128 + 2)), 1)[:h, :w]  # 나무 결 = 판 방향
    vv, uu = np.mgrid[0:h, 0:w]
    if pt == 'rwall':  # q1_room.wall_paint의 크림 3단 복원(판벽 위에 회색 고주파 결 금지).
        A[f > 1.75] *= 1.05; A[f < -2.1] *= .93
        return A
    if pt in ('plaster', 'ceil'):
        # D-204: 고대비 점을 임계처리하면 회벽이 회색 얼룩처럼 보인다.
        # 기존 A2 결의 배치를 유지하되 11cm 폭으로 부드럽게 하고 따뜻한 바탕색의 명도만 변주.
        smooth = (np.asarray(Image.fromarray(np.clip(128 + f * 20, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3.5)), np.float64) - 128) / 20
        smooth = np.clip(smooth / max(float(smooth.std()), .001), -2, 2)
        A *= (1 + smooth[..., None] * (.018 if pt == 'ceil' else .025))
        return A
    if pt == 'pane' and w > 8 and h > 8: A[(np.abs(uu - w // 2) <= 1) | (np.abs(vv - h // 2) <= 1)] = col['trim']  # 창살 십자(흰 틀 색)
    if pt == 'curtain': A[(uu % 6 == 5)] *= .9  # 커튼 주름 9cm
    if pt == 'shutter' and w > 8 and h > 8:  # 루버 6cm(64px/m 에서 4px) — 틀 3px 안쪽만. 판 사이 홈 = 칠한 색 어둠 한 단
        inn = (uu >= 3) & (uu < w - 3) & (vv >= 3) & (vv < h - 3); A[inn & (vv % 4 == 3)] *= .86
    if pt == 'door': A[(uu % 5 == 4)] *= .88; A[(vv == h // 3) | (vv == 2 * h // 3)] *= .9  # 판자 15cm · 가로 띠 2
    if pt == 'soffit': A[(vv % 4 == 3)] *= .9
    m = lambda x: max(1, round(x * d))  # 0351 집 안 무늬(미터 → px)
    if pt == 'boards': A[(vv % m(.2) == m(.2) - 1)] *= .88                                   # 마루 판 20cm
    if pt == 'wainlow':  # 주인공 방 판벽(q1_room 과 같은 색): 판 15cm · 위 띠 6cm(나무) · 걸레받이 8cm
        A[(uu % m(.15) == m(.15) - 1)] = [135., 157., 150.]
        A[vv >= h - m(.06)] = [161., 109., 66.]; A[vv < m(.08)] = [128., 80., 50.]
        return A  # q1_room 기존 하늘색 판벽·갈색 몰딩 보존
    if pt == 'cabinet' and w > 4: A[(uu % m(.5) == 0)] *= .78; A[(vv == h - m(.16))] *= .82    # 장 문 50cm · 서랍 줄
    if pt == 'counter': A[(uu % m(.15) == m(.15) - 1) | (vv % m(.15) == m(.15) - 1)] *= .9     # 타일 15cm
    if pt == 'sinktop' and w > 6 and h > 6: A[3:-3, 3:-3] *= .62                             # 개수대 안(움푹 = 칠한 어둠 아님, 스테인리스 안쪽 색)
    if pt == 'stovetop':
        for cu in (w // 2,):
            for cv in (h // 4, 3 * h // 4): r = np.hypot(uu - cu, vv - cv); A[(r >= m(.06)) & (r < m(.06) + 1.5)] = col['steel']  # 화구 2
    if pt == 'mat': A[(uu < 2) | (uu >= w - 2) | (vv < 2) | (vv >= h - 2)] *= .8
    if pt == 'apron': A[(uu % 6 < 1) | (vv % 6 < 1)] = col['apron'] * .6 + col['trim'] * .4  # 바랜 체크
    if pt == 'pavers' and w > 6 and h > 6: A[(uu % 10 == 9) | (vv % 10 == 9)] *= .82  # 30cm 타일 줄눈
    # 0133 도현 "생활감" 물건 무늬 — 색은 사진 중앙값(A2·C2·WOOD·E2)에서 파생, 줄·점은 64px/m 위치(미터 → px)
    if pt in ('shoecab', 'wardrobef') and w > 6: A[uu == w // 2] *= .72                                     # 두 짝 문 가운데 틈
    if pt == 'shoecab' and h > 6: A[vv == m(.5)] *= .75
    if pt == 'wardrobef' and w > 6 and h > 6: A[(abs(uu - w // 2) == 3) & (vv > m(.9)) & (vv < m(1.2))] = col['brass'] * [1, .95, .85]   # 문 손잡이 2
    if pt == 'dresserf' and w > 6 and h > 6:
        for fr in (.28, .56): A[vv == m(fr)] *= .7
        for fr in (.14, .42, .7): A[(abs(vv - m(fr)) <= 1) & (abs(uu - w // 2) <= 2)] = col['brass'] * [1, .95, .85]
    if pt == 'drawer' and w > 6 and h > 6:
        A[vv == h // 2] *= .7
        for fr in (.25, .75): A[(abs(vv - int(h * fr)) <= 1) & (abs(uu - w // 2) <= 2)] = col['brass'] * [1, .95, .85]
    if pt == 'fridgef' and h > 8: A[vv == m(1.2)] *= .72; A[(vv == m(.8) - 4)] *= .9
    if pt == 'rug' and w > 8 and h > 8:
        A[(uu < m(.05)) | (uu >= w - m(.05)) | (vv < m(.05)) | (vv >= h - m(.05))] *= .72    # 가장자리 5cm 띠
        A[(uu % m(.14) < 2) & (uu > m(.1)) & (uu < w - m(.1))] *= .9                          # 줄무늬 14cm
    if pt == 'quilt' and w > 8 and h > 8: A[(uu % m(.1) == 0) | (vv % m(.1) == 0)] *= .9      # 누빔 10cm 칸
    if pt == 'mirror' and w > 6 and h > 6: A[(uu < 2) | (uu >= w - 2) | (vv < 2) | (vv >= h - 2)] *= .62
    if pt == 'pantop' and w > 6:  # 계란 후라이: 바닥 동그라미 흰자 + 노른자
        r = np.hypot(uu - w / 2., vv - h / 2.); A[r < m(.075)] = [234., 230., 218.]; A[r < m(.03)] = [222., 164., 48.]
    if pt == 'clockface' and w > 6:  # 문자판: 12 눈금(바깥 1~2px)
        for k in range(12):
            a = k * math.pi / 6; px_, py_ = w / 2. + math.cos(a) * m(.14), h / 2. + math.sin(a) * m(.14); A[(abs(uu - px_) <= .8) & (abs(vv - py_) <= .8)] *= .45
    # 1421: 새 소품 무늬 (64px/m)
    if pt == 'blanket' and w > 8 and h > 8: A[(vv % m(.06) < m(.025))] *= .86; A[(uu < 2) | (uu >= w - 2)] *= .9              # 담요 가로 줄 6cm
    if pt == 'remote' and w > 3 and h > 6:  # 리모컨 윗면: 위 전원(어두운 붉은) · 가운데 동그라미 · 아래 숫자판 띠(살짝 밝게) — 대비는 잔결 상한 안
        A[(vv < h // 6) & (uu > w // 3) & (uu < 2 * w // 3)] = col['apron'] * .5; A[(abs(vv - h // 2) <= 1) & (abs(uu - w // 2) <= 1)] = col['steel'] * .42
        A[(vv > 2 * h // 3) & (vv % 3 == 0)] *= 1.5
    if pt == 'calpage' and w > 8 and h > 8:  # 달력 칸 7x5
        A[(uu % max(2, w // 7) == 0) | ((vv > h // 5) & (vv % max(2, h // 6) == 0))] *= .78; A[vv < h // 6] = col['apron'] * .8 + col['trim'] * .2
    if pt == 'pic1' and w > 8 and h > 8:  # 액자 그림: 하늘 위 · 풀 아래 · 가운데 지붕색 집 덩어리
        A[vv > h * .62] = col['leaf'] * .95; A[(uu > w * .35) & (uu < w * .65) & (vv > h * .4) & (vv <= h * .62)] = col['terra']
    if pt == 'pic2' and w > 8 and h > 8: A[uu > w // 2] = col['sofa'] * [1.05, 1.0, .95]; A[(vv > h * .5) & (uu > w // 4) & (uu < 3 * w // 4)] = col['trim']
    if pt in ('bk1', 'bk2', 'bk3', 'bk4', 'bk5', 'bk6') and w > 4 and h > 4: A[(vv > h * .3) & (vv < h * .4)] *= 1.15; A[vv < 2] *= .85    # 책등 띠
    if pt in ('spice1', 'spice2', 'spice3', 'cereal') and w > 4 and h > 4: A[(vv > h * .35) & (vv < h * .65)] = col['paper'] * .96    # 병 라벨 자리
    lum = A @ [.299, .587, .114]
    for kind in ('paint', 'rubber'):
        sel = (lum < 70) if kind == 'rubber' else (lum >= 70); st = STEPS[kind]
        if pt in ('plaster', 'ceil', 'apron', 'basket', 'curtain', 'brass'):
            st = np.array([-.08, -.025, .025, .08])  # D-197 하한 ±8%, 사진 결 유지
        if kind == 'rubber' and w * h <= 60: st = np.array([-.15, -.07, .07, .15])
        if kind == 'paint' and w * h <= 60: st = np.array([-.12, -.06, .06, .12])  # 1421: 2~6px 짜리 가는 면(컵 손잡이·걸이·병)은 ±10% 결이 양자화 뒤 잔결 기준 아래 → ±12%(D-197 범위 안, 사서함 손잡이와 같은 처리)
        ff = f * (st[2] + st[3]) / 2
        q = st[np.abs(ff[..., None] - st).argmin(-1)]; A[sel] *= (1 + q[sel])[:, None]
    return A
def h_source(c):
    if 'MEAS' not in blender(R / 'tools/asset-gen/house_home.py'): raise SystemExit('house_home.py 가 끝까지 안 돌았다(MEAS 없음)')
def h_overlap(c):  # 생성기가 붙는 면 skip·덧댄 판 띄움 → 여기선 검사만(fix 는 fbx 를 덮어써서 안 씀)
    for m, f in ((('check',), 'overlap_check.json'), (('check', 'both'), 'overlap_both.json')):
        blender(R / 'tools/asset-gen/fix_1846.py', 'house_home', *m); shutil.copy2(R / ('docs/qa/tmp/fix_1846_house_home_' + '_'.join(m) + '.json'), c['tmp'] / f)
        d = json.loads((c['tmp'] / f).read_text(encoding='utf-8')); d = d.get('after') or d['before']; print(f'   {f}: 겹친 쌍 {d["pairs"]} · 같은 덩어리 중복 {d["same_island_dups"]}')
def h_grain(c):
    L = lay(c); AW, AH = L['atlas']
    col = {'wall': h_med('A2'), 'trim': np.minimum(h_med(*HCROP['trim']), 212) * [1, .97, .9],  # 사진 흰 틀은 하늘빛 무채 → 석회 흰색 쪽으로
            'door': h_med(*HCROP['door']), 'shutter': h_med('C2'), 'roof': h_med('E2')}
    col.update(terra=col['roof'] * [1.08, .98, .92], ridge=col['roof'] * .84, soffit=col['door'] * 1.6, iron=np.array([58., 66., 60.]), soil=np.array([112., 84., 60.]),
               leaf=np.array([88., 124., 72.]), flower=np.array([196., 78., 82.]), brass=np.array([176., 146., 82.]),
               pane=np.array([74., 86., 92.]), curtain=col['trim'] * [1.04, 1.02, .94])  # 0052: 유리 너머 어둠(빛 굽기 아님 = 단색 + 결) · 크림 커튼
    col.update(plaster=np.array([242., 228., 200.]), ceil=np.array([246., 238., 218.]), wain=np.array([178., 209., 183.]), rwall=np.array([244., 235., 200.]),  # D-204 미색 회벽, q1_room 판벽 보존
               boards=col['door'] * 1.35, glass=np.array([186., 214., 226.]), steel=np.array([150., 156., 158.]), stove=np.array([54., 56., 58.]),
               cabinet=col['shutter'] * 1.45, sofa=np.array([98., 116., 132.]), cushion=np.array([112., 132., 148.]), apron=np.array([184., 92., 86.]),
               basket=np.array([178., 140., 92.]), mat=np.array([124., 90., 66.]))
    # 0133 생활감 물건 색 — 사진 중앙값(A2 벽·C2 덧창·WOOD 문·E2 지붕)에서 파생(재질 어림 = 흰 석회·도자·가죽·고무·나무). 빛·그림자 굽지 않음
    col.update(plastic=col['trim'] * [.78, .84, .9], ceramic=col['trim'] * [1.0, 1.0, .97], coffee=np.array([62., 40., 28.]), shoecab=col['door'] * 1.25,
               sole=np.array([34., 34., 36.]), leather=np.array([96., 64., 46.]), shade=np.array([226., 204., 156.]), bulb=np.array([250., 236., 188.]),
               rug=col['terra'] * [.92, .86, .82], pepper=np.array([44., 38., 34.]), pan=np.array([44., 46., 48.]), fridge=col['steel'] * [1.22, 1.2, 1.18],
               bin=col['iron'] * [1.4, 1.45, 1.4], bedding=col['trim'] * [.96, .96, .98], quilt=col['curtain'] * [.74, .6, .6] + [12., 6., 6.],
               wardrobe=col['door'] * 1.35, mirror=np.array([150., 176., 186.]), clockface=col['trim'] * [1., .98, .92])
    # 1421 생활감 2차 — 종이·병·책등·옷감 색. 모두 위 사진 중앙값 파생 색(trim·terra·leaf·flower·sofa·curtain)에 재질 어림 곱 — 빛 굽기 없음
    col.update(paper=col['trim'] * [1.0, .99, .95], spice1=col['terra'] * [.9, .7, .5], spice2=col['leaf'] * [.9, .9, .6], spice3=col['flower'] * [.9, .8, .5],
               jar=col['basket'] * [1.0, .9, .8], cereal=col['apron'] * [1.0, 1.05, .8], bagcloth=col['trim'] * [.88, .86, .78], cover=col['sofa'] * [1.1, .95, .8],
               cushionB=col['apron'] * [.9, 1.1, 1.2], blanket=col['curtain'] * [.9, .85, .8] + [10., 6., 0.], pic1=col['pane'] * [.9, 1.1, 1.15], pic2=col['terra'] * [1.1, .95, .75],
               bk1=col['apron'] * [.8, .8, .8], bk2=col['sofa'] * [.75, .8, .85], bk3=col['leaf'] * [.75, .8, .8], bk4=col['brass'] * [.8, .75, .7],
               bk5=col['trim'] * [.8, .78, .72], bk6=col['terra'] * [.75, .7, .7],
               clothA=col['sofa'] * [1.0, .95, .85], clothB=col['apron'] * [.95, 1.1, 1.0], clothC=col['trim'] * [.92, .9, .84], clothD=col['leaf'] * [.85, .85, .7])
    A = np.zeros((AH, AW, 4)); P = [(cell['rect'], h_paint(cell, col)) for cell in L['cells']]
    for (x, y, w, h), t in P:  # 0419: 3px edge dilation으로 얇은 면 주변 검은 빈칸 제거.
        e = np.pad(t, ((3, 3), (3, 3), (0, 0)), mode='edge')
        A[y-3:y+h+3, x-3:x+w+3, :3] = e; A[y-3:y+h+3, x-3:x+w+3, 3] = 255
    for (x, y, w, h), t in P: A[y:y + h, x:x + w, :3] = t
    # 불투명 집 아틀라스의 비사용 공간도 가장자리 색으로 확장한다.
    # 0.25m 검사칸/텍스처 필터가 UV 바깥 검은 배경을 잔결로 읽지 않게 한다.
    filled = np.flatnonzero((A[..., 3] > 0).any(axis=1))
    xx = np.arange(AW)
    for iy in filled:
        valid = np.flatnonzero(A[iy, :, 3] > 0); j = np.searchsorted(valid, xx)
        lo, hi = valid[np.maximum(j - 1, 0)], valid[np.minimum(j, len(valid) - 1)]
        nearest = np.where(xx - lo <= hi - xx, lo, hi)
        A[iy] = A[iy, nearest]
    for iy in np.flatnonzero(~(A[..., 3] > 0).any(axis=1)):
        A[iy] = A[filled[np.abs(filled - iy).argmin()]]
    Image.fromarray(np.clip(A[::-1], 0, 255).round().astype(np.uint8)).save(c['exp'] / 'house_home.png')
    (c['tmp'] / 'grain.json').write_text(json.dumps({k: [round(float(v)) for v in val] for k, val in col.items()}, indent=1), encoding='utf-8'); print('   colors', {k: [round(float(v)) for v in val] for k, val in col.items()})
def h_quant(c):  # 128px 칸마다 median-cut 32 → 합친 색 = art/export/house-home/palette.gpl (정비소와 같은 방식)
    g_quant(c); p = c['exp'] / 'palette.gpl'; p.write_text(p.read_text(encoding='utf-8').replace('q2-garage (build_asset.py 2117', 'house-home (build_asset.py 0031'), encoding='utf-8')
def h_unity(c):
    if Path(UCLI_PROJ).resolve() != (R / 'F1Studio').resolve(): raise SystemExit('house_home 검토는 F1Studio 에디터만 허용')
    from house_studio_ground import prepare
    ground_cut, _ = prepare()
    E, Lb = c['exp'], c['lib']; Lb.mkdir(parents=True, exist_ok=True)
    if not Lb.resolve().is_relative_to((R / 'F1Studio').resolve()): raise SystemExit('house_home 검토 산출물이 F1Studio 밖을 가리킴')
    for p in (E / c['fbx'], E / 'house_home.png'): copy_retry(p, Lb / p.name)
    code = (R / 'docs/qa/tmp/library_import.cs').read_text(encoding='utf-8').replace('var ONLY = new[] { "depot" };', 'var ONLY = new[] { "house_home" };')
    code = code.replace('var dir = L + "/" + e.nm;', 'var dir = "Assets/Art/Review/house_home";')
    # 텍스처 결 위로 중복된 12.5cm 셰이더 잡음이 미색 벽을 회색 반점으로 만들었다.
    code = code.replace('m.SetFloat("_Sat", 1);', 'm.SetFloat("_Sat", 1); m.SetFloat("_SN", 0);')
    code = code.replace('foreach (var e in POSE) AssetDatabase.ImportAsset(L + "/" + e.nm + "/" + e.nm + ".prefab", ImportAssetOptions.ForceUpdate);', 'AssetDatabase.ImportAsset("Assets/Art/Review/house_home/house_home.prefab", ImportAssetOptions.ForceUpdate);')
    # D-204: 편집 모드에 예전 독립 room이 함께 켜져 집 안에 방 상자가 겹쳤다.
    # 검토 스튜디오의 대상만 활성화하여 저장; 원본 room 자산은 바꾸지 않는다.
    # 0133 도현 "방 2개": 주인공 방 = 집 오른쪽 끝(x +2.4) · 문 없는 출입구 자리의 문손잡이 제거 · 천장 펜던트마다 따뜻한 점광원(자식 Light)
    code = code.replace('new Vector3(0, 3.2f, 1.35f) - off', 'new Vector3(-2.4f, 3.2f, 1.35f) - off')
    code = code.replace('log.Append(" room shell dropped, lamp "',
                        'foreach (var kn in ri.GetComponentsInChildren<Transform>(true).Where(t => t.name == "KNOB_01").ToArray()) UnityEngine.Object.DestroyImmediate(kn.gameObject);\n'
                        '    foreach (var pn in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("PENDANT_") || t.name.StartsWith("BEDLIGHT_")).ToArray()) { var lg = new GameObject("Light"); lg.transform.SetParent(pn, false); lg.transform.position = pn.position + Vector3.up * (pn.name.StartsWith("PENDANT_") ? 0.1f : 0.1f); var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, .82f, .55f); li.range = pn.name.StartsWith("PENDANT_") ? 4.2f : 2.2f; li.intensity = pn.name.StartsWith("PENDANT_") ? 1.1f : 0.6f; li.shadows = LightShadows.None; }\n'
                        '    log.Append(" room shell dropped, lamp "')
    code = code.replace('UnityEditor.SceneManagement.EditorSceneManager.SaveScene(',
                        'foreach (Transform child in slotT) child.gameObject.SetActive(child.name == "house_home");\n' + ground_cut + '\nUnityEditor.SceneManagement.EditorSceneManager.SaveScene(')
    out = unity_exec(code); print('  ', out[:800])
    if 'error' in out.lower() or 'MISS' in out: raise SystemExit('house_home Unity 임포트 확인 필요: ' + out)

def h_check(c):
    p = g_check(c)
    if p.returncode != 0 or 'GATE PASS' not in p.stdout:
        raise SystemExit('house_home 규칙 게이트 실패: ' + p.stdout[-1600:] + p.stderr[-500:])
HOUSE_HOME = {'source': h_source, 'overlap': h_overlap, 'split': lambda c: print('   낱개(DOOR_·SHUTTER_·RAIL_·PLANTER_·PLANT_)는 생성기가 이름·피벗까지'),
              'snap': lambda c: print('   접지: 생성기 좌표(덧창 바닥 = 창턱, 화분 = 발코니 바닥, 잎 = 흙)'), 'uv': lambda c: print('   텍셀 32/64 = 생성기 dens_for'),
              'grain': h_grain, 'quant': h_quant, 'unity': h_unity, 'check': h_check}
ROOM = {'source': r_source, 'overlap': r_overlap, 'split': r_split, 'snap': r_snap, 'uv': r_uv, 'grain': r_grain, 'quant': r_quant, 'unity': r_unity, 'check': g_check}
POSTOFFICE = {'source': p_source, 'overlap': p_overlap, 'split': p_split, 'snap': p_snap, 'uv': p_uv, 'grain': p_grain, 'quant': s_quant, 'unity': p_unity, 'check': g_check}
GARAGE = {'source': g_source, 'overlap': g_overlap, 'split': g_split, 'snap': g_snap, 'uv': g_uv, 'grain': g_grain, 'quant': g_quant, 'unity': g_unity, 'check': g_check}

# ---------- 밭(field_patch) 단계 — artlive 20261030-2056 ----------
# 원본 = tools/asset-gen/field_patch.py(형태·낱개·UV 칸 표 layout.json: dens·vz(칸 v 가 위로 가면 +1)·z01(물건 안 높이 0~1)). 텍스처는 여기서 칸마다 사진 칠(빛 없음, D-185).
# 사진(도현 "밭 재료는 추천대로" PICKS.md, CC0 — SOURCES.md I 줄·provenance.csv): I1 흙 · I5 배추(초록 잎 끝/흰 속) · I8 대파(초록 잎/흰 줄기) · I10 회색 바랜 판자(울).
# 고추·지주대·끈·물뿌리개·바구니·돌 = 집 팔레트(house_home h_grain 과 같은 값: 문 나무·흰 틀·덧창 초록·바구니·꽃 빨강·H1 돌).
HPH.update({'I1': (PP / 'I/I1_soil.jpg', None), 'I5': (PP / 'I/I5_cabbage.jpg', (200, 800, 600, 1000)), 'I5W': (PP / 'I/I5_cabbage.jpg', (800, 400, 1200, 600)),
            'I8': (PP / 'I/I8_onion.jpg', (0, 200, 600, 400)), 'I8W': (PP / 'I/I8_onion.jpg', (200, 600, 600, 1000)), 'I10': (PP / 'I/I10_fence.jpg', None)})
_FF = {}
FP_SOIL = np.array([157., 125., 89.])  # 2단계 목업에서 잰 둘레 땅 갈색 — 흙 사진은 결만, 색은 이 값
def f_paint(cell, col):
    x, y, w, h = cell['rect']; d = cell['dens']; pt = cell['pt']; rs = np.random.default_rng(cell['seed'])
    fk = {'soil': 'I1', 'soil_ridge': 'I1', 'cabbage_head': 'I5W', 'cabbage_leaf': 'I5', 'onion_stem': 'I8W', 'onion_leaf': 'I8', 'plank': 'I10', 'rail': 'I10', 'post': 'I10',
          'stake': 'WOOD', 'basket': 'WOOD', 'can': 'C2', 'stone': 'H1'}.get(pt, 'I5')
    t = h_field(fk)
    if d >= 64 or pt in ('plank', 'rail', 'post'): t = t[::3 if d >= 64 else 2, ::3 if d >= 64 else 2]; t = (t - t.mean()) / (t.std() + 1e-6)  # 작은 물건 칸(8~20px)엔 128px 결의 큰 얼룩만 걸려 잔결이 0.02 → 결을 3배 촘촘히(잎맥·흙알 크기)
    if min(w, h) <= 3:  # 폭 2~3px 칸(파 줄기·잎, 끈): 128px 결은 몇 px 짜리 덩어리라 위아래 이웃이 같은 색 → 사진 원본 해상도의 잔결(흐림 1.5 대비)을 그대로
        if fk not in _FF:
            g = h_photo(fk).convert('L'); a = np.asarray(g, np.float64); b = np.asarray(g.filter(ImageFilter.GaussianBlur(1.5)), np.float64); hp = a / np.maximum(b, 1) - 1; _FF[fk] = (hp - hp.mean()) / (hp.std() + 1e-6)
        t = _FF[fk]
    if pt in ('plank', 'rail', 'post', 'stake') and w > h: t = np.rot90(t)  # 나무 결 = 긴 쪽(사진 I10 은 결이 세로)
    n0, n1 = t.shape; f = np.roll(np.tile(t, (h // n0 + 2, w // n1 + 2)), (rs.integers(n0), rs.integers(n1)), (0, 1))[:h, :w]
    vv = np.mgrid[0:h, 0:w][0] / max(h - 1, 1); z0, z1 = cell.get('z01', [0, 1]); vz = cell.get('vz', 0)
    zr = z0 + (z1 - z0) * (vv if vz > 0 else 1 - vv if vz < 0 else np.full((h, w), .5))  # 픽셀 높이(물건 안 0~1)
    if pt in ('cabbage_head', 'cabbage_leaf'):  # 배추 = 밑 흰 속 → 위 초록 잎 끝(I5 사진 그대로의 두 색). 그늘 아님 — 재료 색
        k = np.round(np.clip((zr - (.45 if pt == 'cabbage_head' else .3)) / .35, 0, 1) * 3)[..., None] / 3; A = col['cab_w'] * (1 - k) + col['cab_g'] * k
    elif pt == 'onion_stem':  # 대파 = 흰 줄기(포기 높이 아래 20~35%) → 초록
        k = np.round(np.clip((zr - .2) / .15, 0, 1) * 3)[..., None] / 3;  # zr = 포기 전체(잎 끝 1.0) 기준 — 줄기 위끝이 ~.4
         # 색 띠 4단 — 부드러운 섞임은 칸별 32색 양자화에서 결을 먹는다
        A = col['oni_w'] * (1 - k) + col['oni_g'] * k
    else: A = np.ones((h, w, 3)) * col[{'soil': 'soil', 'soil_ridge': 'ridge', 'onion_leaf': 'oni_g', 'plank': 'wood', 'rail': 'wood', 'post': 'post', 'stake': 'stake',
                                       'string': 'string', 'can': 'can', 'basket': 'basket', 'pepper_leaf': 'leaf', 'pepper_stem': 'pstem', 'pepper_fruit': 'fruit', 'stone': 'stone'}[pt]]
    if pt == 'basket' and w > 4 and h > 4: A[(np.mgrid[0:h, 0:w][1] % 3 == 2)] *= .9  # 엮은 살 5cm(64px/m)
    lum = A @ [.299, .587, .114]
    for kind in ('paint', 'rubber'):
        sel = (lum < 70) if kind == 'rubber' else (lum >= 70); st = STEPS[kind]
        if w * h <= 60: st = np.array([-.15, -.07, .07, .15]) if kind == 'rubber' else np.array([-.12, -.06, .06, .12])  # 가는 면(끈·줄기) = ±12%(D-197)
        if min(w, h) <= 3: st = np.array([-.12, -.10, .10, .12])  # 폭 2~3px 칸(파 줄기·잎, 끈): 양옆이 번진 같은 색이라 잔결이 세로 결로만 → 위아래 이웃끼리 ±10~12% 로 갈림
        q = st[np.abs((f * (st[2] + st[3]) / 2)[..., None] - st).argmin(-1)]; A[sel] *= (1 + q[sel])[:, None]
    return A
def f_source(c):
    if 'MEAS' not in blender(R / 'tools/asset-gen/field_patch.py'): raise SystemExit('field_patch.py 가 끝까지 안 돌았다(MEAS 없음)')
def f_overlap(c):
    for m in (('check',), ('check', 'both')):
        blender(R / 'tools/asset-gen/fix_1846.py', 'field_patch', *m); f = R / ('docs/qa/tmp/fix_1846_field_patch_' + '_'.join(m) + '.json')
        if f.exists(): shutil.copy2(f, c['tmp'] / f.name); d = json.loads(f.read_text(encoding='utf-8')); d = d.get('after') or d['before']; print(f'   {f.name}: 겹친 쌍 {d["pairs"]} · 같은 덩어리 중복 {d["same_island_dups"]}')
def f_grain(c):
    L = lay(c); AW, AH = L['atlas']; door = h_med(*HCROP['door'])
    col = {'soil': FP_SOIL, 'ridge': FP_SOIL * [.93, .92, .9],  # 이랑 = 갓 일군 흙(조금 진함) — 빛 아님, 재료
           'cab_w': np.minimum(h_med('I5W'), 222), 'cab_g': h_med('I5'),
           'oni_w': np.minimum(h_med('I8W'), 222), 'oni_g': h_med('I8'),
           'wood': h_med('I10') * 1.7, 'post': h_med('I10') * 1.56,  # I10 albedo(70) 는 게임 화면에서 검게 읽힘 → 바랜 회색 명도 ~118, 색조는 사진 그대로
            'stake': door * 1.25, 'string': np.minimum(h_med(*HCROP['trim']), 212) * [1, .97, .9],
           'can': h_med('C2'), 'basket': np.array([178., 140., 92.]), 'leaf': np.array([88., 124., 72.]), 'pstem': np.array([88., 124., 72.]) * .82,
           'fruit': np.array([196., 78., 82.]) * [.95, .7, .6], 'stone': h_med('H1')}
    A = np.zeros((AH + 6, AW + 6, 4)); P = [(cell['rect'], f_paint(cell, col)) for cell in L['cells']]  # 둘레 3px 여유(가장자리 칸 번짐용) → 끝에 잘라냄
    for (x, y, w, h), t in P: A[y:y + h + 6, x:x + w + 6, :3] = np.pad(t, ((3, 3), (3, 3), (0, 0)), mode='edge'); A[y:y + h + 6, x:x + w + 6, 3] = 255
    for (x, y, w, h), t in P: A[y + 3:y + h + 3, x + 3:x + w + 3, :3] = t
    A = A[3:-3, 3:-3]
    filled = np.flatnonzero((A[..., 3] > 0).any(axis=1)); xx = np.arange(AW)  # 빈 곳 = 가장자리 색(h_grain 과 같음)
    for iy in filled:
        valid = np.flatnonzero(A[iy, :, 3] > 0); j = np.searchsorted(valid, xx)
        lo, hi = valid[np.maximum(j - 1, 0)], valid[np.minimum(j, len(valid) - 1)]; A[iy] = A[iy, np.where(xx - lo <= hi - xx, lo, hi)]
    for iy in np.flatnonzero(~(A[..., 3] > 0).any(axis=1)): A[iy] = A[filled[np.abs(filled - iy).argmin()]]
    A[..., 3] = 255
    Image.fromarray(np.clip(A[::-1], 0, 255).round().astype(np.uint8)).save(c['exp'] / 'field_patch.png')
    cl = {k: [round(float(v)) for v in val] for k, val in col.items()}; (c['tmp'] / 'grain.json').write_text(json.dumps(cl, indent=1), encoding='utf-8'); print('   colors', cl)
def f_quant(c):  # 128px 칸마다 median-cut 32 → 이 에셋 전용 art/export/field-patch/palette.gpl
    g_quant(c); p = c['exp'] / 'palette.gpl'; p.write_text(p.read_text(encoding='utf-8').replace('q2-garage (build_asset.py 2117', 'field-patch (build_asset.py artlive 2056'), encoding='utf-8')
def f_unity(c):
    if Path(UCLI_PROJ).resolve() != (R / 'F1Studio').resolve(): raise SystemExit('field_patch 는 F1Studio 에디터만')
    E, Lb = c['exp'], c['lib']; Lb.mkdir(parents=True, exist_ok=True)
    for p in (E / c['fbx'], E / 'field_patch.png'): copy_retry(p, Lb / p.name)
    code = (R / 'docs/qa/tmp/library_import.cs').read_text(encoding='utf-8').replace('var ONLY = new[] { "depot" };', 'var ONLY = new[] { "field_patch" };')
    code = code.replace('("house_home", "house_home.fbx", 0f, "keep") };', '("house_home", "house_home.fbx", 0f, "keep"), ("field_patch", "field_patch.fbx", 0f, "keep") };')
    code = code.replace('if (nm == "room")', 'if (nm == "field_patch") return "field_patch";\n  if (nm == "room")', 1)
    code = code.replace('m.SetFloat("_Sat", 1);', 'm.SetFloat("_Sat", 1); if (e.nm == "field_patch") { if (m.HasProperty("_SN")) m.SetFloat("_SN", 0); if (m.HasProperty("_Blotch")) m.SetFloat("_Blotch", 0); }')
    code = code.replace('else { var bc = root.AddComponent<BoxCollider>();', 'else if (e.nm == "field_patch") { }  /* 밭: 통 콜라이더 없음 — 울·배추 낱개 BoxCollider 만 */\n  else { var bc = root.AddComponent<BoxCollider>();')
    code = code.replace('foreach (var e in POSE) AssetDatabase.ImportAsset(', 'foreach (var e in POSE) if (ONLY.Contains(e.nm)) AssetDatabase.ImportAsset(')
    code = code.replace('UnityEditor.SceneManagement.EditorSceneManager.SaveScene(',  # 스튜디오 자리 = house_home 서쪽 마당(1단계 field_patch_import.cs 와 같은 곳)
                        '{ var fp = slotT.Find("field_patch"); fp.localPosition = new Vector3(-8.4f, 0f, -1.9f); foreach (Transform ch in slotT) ch.gameObject.SetActive(ch.name == "house_home" || ch.name == "field_patch"); log.Append(" slot " + fp.position.ToString("F2")); }\nUnityEditor.SceneManagement.EditorSceneManager.SaveScene(')
    out = unity_exec(code); print('  ', out[:800])
    if 'error' in out.lower() or 'MISS' in out: raise SystemExit('field_patch Unity 임포트 확인 필요: ' + out)
def f_check(c):
    p = g_check(c)
    if p.returncode != 0 or 'GATE PASS' not in p.stdout: raise SystemExit('field_patch 규칙 게이트 실패: ' + p.stdout[-1600:] + p.stderr[-500:])
FIELD_PATCH = {'source': f_source, 'overlap': f_overlap, 'split': lambda c: print('   낱개(CABBAGE_·ONION_·PEPPER_·FENCE_·CAN_·BASKET_)는 생성기가 이름·피벗까지'),
               'snap': lambda c: print('   접지: 생성기 좌표(작물 = 이랑 윗면, 소품 = 앞 통로 흙 +1.4cm)'), 'uv': lambda c: print('   텍셀 32/64 = 생성기 dens_for'),
               'grain': f_grain, 'quant': f_quant, 'unity': f_unity, 'check': f_check}

STAGES = [('source', s_source), ('overlap', s_overlap), ('split', s_split), ('snap', s_snap), ('uv', s_uv), ('grain', s_grain), ('quant', s_quant),
          ('unity', s_unity), ('viewer', s_viewer), ('check', s_check)]
PER = {'garage': GARAGE, 'postoffice': POSTOFFICE, 'room': ROOM, 'house_home': HOUSE_HOME, 'field_patch': FIELD_PATCH}  # 건물별로 다른 단계 함수(없으면 위 보관소 것)
if __name__ == '__main__':
    name = sys.argv[1]; c = dict(CFG[name], name=name); c['tmp'] = R / f'docs/qa/tmp/build/{name}'; c['tmp'].mkdir(parents=True, exist_ok=True)
    start = sys.argv[sys.argv.index('--from') + 1] if '--from' in sys.argv else 'source'
    only = sys.argv[sys.argv.index('--only') + 1] if '--only' in sys.argv else None
    on = False
    for i, (sn, fn) in enumerate(STAGES):
        on = on or sn == start
        if not on or (only and sn != only): continue
        print(f'[{i:02d} {sn}]'); PER.get(name, {}).get(sn, fn)(c); snapshot(c, i, sn)
        if '--to' in sys.argv and sn == sys.argv[sys.argv.index('--to') + 1]: break
    print('BUILD DONE', name)
