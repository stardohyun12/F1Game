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
  unity    export → F1/Assets/Art/Library/Buildings/<건물> 복사 → docs/qa/tmp/library_import.cs(ONLY=depot) → 프리팹
  viewer   tools/viewer/export_glb.py <건물>
  check    tools/art/check_rules.py --only <건물> --gate (텍셀·원통·글씨 포함) → docs/art/rule-audit/last_<건물>.md
사진(CC0, Poly Haven): white_plaster_02, rubberized_track — 출처 grain_1722/1736 과 같음. AI 이미지 없음.
"""
import sys, os, json, shutil, subprocess, hashlib, time
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

R = Path(__file__).resolve().parents[2]
BL = os.environ.get('BLENDER', 'D:/Tools/Blender/blender.exe')
CFG = {'depot': dict(src=R / 'art/source/q3-depot', exp=R / 'art/export/q3-depot', lib=R / 'F1/Assets/Art/Library/Buildings/depot', fbx='q3_depot.fbx',
                     pngs=['wall', 'trim', 'floor', 'roof', 'decals', 'icons', 'clock_dial'])}
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
    for s in c['pngs']:  # --from grain 으로 다시 돌려도 결이 두 번 입혀지지 않게, 입력은 늘 uv 단계 스냅샷
        sp = c['tmp'] / '04_uv' / (s + '.png')
        if sp.exists(): shutil.copy2(sp, E / sp.name)
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
    # 글씨 텍스처: 세상 64px/m 격자로(Nearest). 아이콘 256→80(택배 라벨 ≈70px/m · 간판 ≈52), 데칼 512→160(≈66)
    for s, n in (('icons', 80), ('decals', 160)):
        Image.open(E / (s + '.png')).convert('RGBA').resize((n, n), Image.NEAREST).save(E / (s + '.png')); log[s] = n
    d = np.asarray(Image.open(E / 'clock_dial.png').convert('RGBA')).copy()  # 시계 바늘 칸: 원 밖 왼쪽 아래 8x3 잉크
    ink = d[29:32, 0:3].reshape(-1, 4)[0]; d[29:32, 0:8] = ink; Image.fromarray(d).save(E / 'clock_dial.png')
    (c['tmp'] / 'grain.json').write_text(json.dumps(log, ensure_ascii=False, indent=1), encoding='utf-8')

def s_quant(c):
    E = c['exp']; L = lay(c); allc = set(); log = {}
    for s in c['pngs']:
        im = Image.open(E / (s + '.png')).convert('RGBA'); a = np.asarray(im).copy(); H, W = a.shape[:2]
        if s in ('icons', 'decals', 'clock_dial'): a[..., 3] = np.where(a[..., 3] >= 128, 255, 0); rects = [[0, 0, W, H]]; N = 24
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
    lines = ['GIMP Palette', 'Name: q3-depot (build_asset.py 2041 — 결 뒤 칸별 median-cut 합집합)', 'Columns: 8', '#']
    lines += ['%3d %3d %3d\tc%03d' % (*p, i) for i, p in enumerate(sorted(allc))]
    (E / 'palette.gpl').write_text('\n'.join(lines) + '\n', encoding='utf-8'); log['palette'] = len(allc)
    (c['tmp'] / 'quant.json').write_text(json.dumps(log, indent=1), encoding='utf-8'); print('   colors', log)

def s_unity(c):
    E, Lb = c['exp'], c['lib']
    for p in [E / c['fbx']] + [E / (s + '.png') for s in c['pngs']]: shutil.copy2(p, Lb / p.name)
    out = unity_exec((R / 'docs/qa/tmp/library_import.cs').read_text(encoding='utf-8')); print('  ', out[:300])
    if 'error' in out.lower() and 'OK' not in out: raise SystemExit('Unity 임포트 실패')
def unity_exec(code):  # scripts/ucli-http.sh exec 과 같은 HTTP — 파이썬에서 bash 를 띄우면 0xC0000005 로 죽어서 직접 보낸다
    import glob, urllib.request
    port = None
    for f in glob.glob(os.path.expanduser('~/.unity-cli/instances/*.json')):
        try: d = json.load(open(f, encoding='utf-8'))
        except Exception: continue
        if d.get('projectPath', '').replace('\\', '/').rstrip('/').lower() == 'd:/github/f1game/f1' and d.get('state') != 'stopped': port = d['port']; break
    if not port: raise SystemExit('F1 Editor connector 없음')
    body = json.dumps({'command': 'exec', 'params': {'code': code}}).encode()
    d = json.loads(urllib.request.urlopen(urllib.request.Request(f'http://127.0.0.1:{port}/command', data=body, headers={'Content-Type': 'application/json'}), timeout=280).read())
    if not d.get('success'): raise SystemExit('Unity exec 실패: ' + str(d.get('message')))
    o = d.get('data'); return o if isinstance(o, str) else json.dumps(o, ensure_ascii=False)
def s_viewer(c): blender(R / 'tools/viewer/export_glb.py', c['name'])
def s_check(c):
    p = subprocess.run([sys.executable, '-X', 'utf8', 'tools/art/check_rules.py', '--only', c['name'], '--gate', '--md', f'docs/art/rule-audit/last_{c["name"]}.md',
                        '--json', str(c['tmp'] / 'rules.json')], cwd=R, capture_output=True, text=True, encoding='utf-8', errors='replace')
    print('\n'.join(p.stdout.splitlines()[-3:]))
    po = R / 'art/export/q4-postoffice'; md5 = lambda f: hashlib.md5(f.read_bytes()).hexdigest()
    same = [s for s in ('wall', 'roof', 'trim', 'floor', 'icons', 'decals') if (po / (s + '.png')).exists() and md5(po / (s + '.png')) == md5(c['exp'] / (s + '.png'))]
    print('   우체국과 md5 같은 텍스처:', same or '없음')

STAGES = [('source', s_source), ('overlap', s_overlap), ('split', s_split), ('snap', s_snap), ('uv', s_uv), ('grain', s_grain), ('quant', s_quant),
          ('unity', s_unity), ('viewer', s_viewer), ('check', s_check)]
if __name__ == '__main__':
    name = sys.argv[1]; c = dict(CFG[name], name=name); c['tmp'] = R / f'docs/qa/tmp/build/{name}'; c['tmp'].mkdir(parents=True, exist_ok=True)
    start = sys.argv[sys.argv.index('--from') + 1] if '--from' in sys.argv else 'source'
    only = sys.argv[sys.argv.index('--only') + 1] if '--only' in sys.argv else None
    on = False
    for i, (sn, fn) in enumerate(STAGES):
        on = on or sn == start
        if not on or (only and sn != only): continue
        print(f'[{i:02d} {sn}]'); fn(c); snapshot(c, i, sn)
        if '--to' in sys.argv and sn == sys.argv[sys.argv.index('--to') + 1]: break
    print('BUILD DONE', name)
