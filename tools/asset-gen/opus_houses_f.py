"""집 f판 — 환경과 같은 그림체 (아트2, 과제 20261027-1504, D-175, docs/design/STYLE.md).
    python tools/asset-gen/style32_palette.py      (공용 팔레트 먼저)
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_f.py
이번 방법 —
  형태·이야기: e판(opus_houses_e.py) 그대로 — 간판 글자·덧창 종류·손댄 흔적·소품 배치 같음. 이 파일은 e판 소스를 읽어 아래만 바꿔 실행한다.
  STYLE 규칙 1: 1m ≈ 11텍셀(e 9).
  규칙 2: 벽·지붕 면마다 큰 바랜 칠 얼룩 1~2개(면 폭의 1/4~1/2, 2텍셀 계단 가장자리, 바램 = 흰 칠 쪽 45%·때 = 10% 어둡게 — ±6% 는 32색에 흡수돼 안 보였음). 함석 줄 1텍셀 → 2텍셀, 기와 줄 1텍셀 반복 → 3텍셀마다.
  규칙 3: 명암 3단(해/옆/그늘) — 옆·그늘은 청록 쪽으로(e 6·12% → 16·34%). 짙은 테두리도 갈색 대신 청록 쪽. 바닥 0.4m 접지 한 단(6채 모두, 흐림 없음).
  규칙 5: 가는 막대(빨래줄·난간 살·사다리·자전거 틀·받침)는 굵기 ≥ 0.08m.
  규칙 6: 팔레트 = art/palette/style32.gpl(게임 컷 환경 20색 + 집 앵커 12색 = 벽 5색×해/옆 2단 + 청록 그늘 + 간판 빨강. 집 칠은 초록 칸 제외) 하나.
출력: art/export/scene-a4-houses-f/<이름>.fbx·.png(e판과 같은 이름·원점), kit.json, palette32.gpl(= style32 복사), measure.json, parts.json
"""
import os, shutil
_E = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_e.py')
src = open(_E, encoding='utf-8').read(); FDOC = __doc__


def rep(a, b):
    global src; assert a in src, a; src = src.replace(a, b, 1)


# 1) c판 칠 함수에 거는 추가 패치 (e판의 _patch 목록 끝에 덧붙임)
rep("exec(compile(_src, 'opus_houses.py', 'exec'))\n", '''_patch("DARK = lambda c: c * .70 + srgb((70, 52, 44)) * .10", "DARK = lambda c: c * .74 + TEAL * .12")
_patch("f, tint = (1.0, 0) if d > .45 else (.86, .06) if d > .10 else (.74, .12)", "f, tint = (1.0, 0) if d > .45 else (.88, .16) if d > .10 else (.74, .34)")
_patch("sh = (z - g['zb'] < h.get('band', 0)) & (role == 'wall')", "sh = (z - g['zb'] < max(h.get('band', 0), .4)) & (role == 'wall')")
_patch("col[sh] = col[sh] * .84 * .9 + TEAL * .84 * .1", "col[sh] = col[sh] * .86 * .75 + TEAL * .86 * .25")
_patch("            dark |= np.abs(z - g['zb']) < tx  # 바닥 닿는 줄\\n", "            dark |= np.abs(z - g['zb']) < tx  # 바닥 닿는 줄\\n            col = BLOTCH(col, P, n, seed, (bx0, bx1, by0, by1), g)\\n")
_patch("col[(I // 1) % 2 == 1] *= .86  # 기와 줄: 2텍셀 간격 명암", "col[I % 3 == 2] *= .9  # 기와 줄: 3텍셀마다")
exec(compile(_src, 'opus_houses.py', 'exec'))
DENS, AW = 11, 512
''')
# 2) 출력 폴더·함석 줄·가는 막대
rep("OUT = J('art', 'export', 'scene-a4-houses-e'); WORK = J('art', 'models', 'opus_houses_e')", "OUT = J('art', 'export', 'scene-a4-houses-f'); WORK = J('art', 'models', 'opus_houses_f')")
rep("elif role == 'tin': col[I % 2 == 1] *= .9", "elif role == 'tin': col[I % 4 >= 2] *= .93")
rep("    p0, p1 = np.array(p0, float), np.array(p1, float); d =", "    sw, sh = max(sw, .08), max(sh, .08); p0, p1 = np.array(p0, float), np.array(p1, float); d =")
# 3) 큰 얼룩 함수 (c판 칠 함수가 전역에서 찾음)
rep("# ---------- 픽셀 글자", '''def BLOTCH(col, P, n, seed, box, g):
    """바랜 칠 큰 얼룩: 벽 한 면(앞·뒤·옆)마다 세계 좌표 타원 1~2개 — 모듈 면이 갈려도 이어짐. 지름 1.2~2.4m(5m 벽의 1/4~1/2), 가장자리 2텍셀 계단."""
    k = int(np.argmax(np.abs(n[:2]))); sd = 2 * k + int(n[k] > 0); x0, x1, y0, y1 = box
    a0, a1 = (x0, x1) if k == 1 else (y0, y1); zb_, zt = g['zb'] + .6, g['Hw'] - .6
    rng = np.random.default_rng(seed * 10 + sd); st = 2 / DENS
    s = np.floor(P[..., 1 - k] / st) * st + st / 2; zq = np.floor(P[..., 2] / st) * st + st / 2
    for j in range(1 + ((a1 - a0) * (zt - zb_) > 18)):
        cx, cz = rng.uniform(a0 + .8, a1 - .8), rng.uniform(zb_, max(zb_, zt)); rx, rz = rng.uniform(.6, 1.2), rng.uniform(.5, 1.0)
        m = ((s - cx) / rx) ** 2 + ((zq - cz) / rz) ** 2 < 1  # ±6% 는 32색에 흡수돼 사라짐(실측) → 바램 = 흰 칠 쪽 45%, 때 = 10% 어둡게
        col[m] = col[m] * .55 + srgb((232, 226, 212)) * .45 if j == 0 else col[m] * .9
    return col


# ---------- 픽셀 글자''')
# 4) 팔레트 = style32 하나
i0 = src.index("pal = read_gpl(J('art', 'export', 'scene-a4-houses-c', 'palette32.gpl'))"); i1 = src.index("kit = dict(task=")
src = src[:i0] + "pal = read_gpl(J('art', 'palette', 'style32.gpl')); shutil.copy(J('art', 'palette', 'style32.gpl'), os.path.join(OUT, 'palette32.gpl'))\n" + src[i1:]
rep("    idx = nearest(img, pal);", "    idx = nearest_f(img, pal, cls);")
rep("# ---------- 굽기 ----------", '''def nearest_f(img, pal, cls):
    """style32 에 가까운 칸 — 단 풀·나무 초록 칸(G 가 R·B 보다 5 넘게 큼)은 소품(화분 잎)만. 집 그늘이 올리브로 떨어지는 것 방지(실측)."""
    P = np.array(pal, np.float64); green = P[:, 1] - np.maximum(P[:, 0], P[:, 2]) > 5
    flat = img.reshape(-1, 3) * 255; d = ((flat[:, None] - P[None]) ** 2).sum(2)
    d[:, green] += np.where(cls.reshape(-1) == RID['prop'], 0, 1e6)[:, None]
    return d.argmin(1).astype(np.int32).reshape(img.shape[:2])


# ---------- 굽기 ----------''')
rep("kit = dict(task='20261027-1406',", "kit = dict(task='20261027-1504', style='docs/design/STYLE.md',")
rep("method=MYDOC.split('이번 방법 —')[1]", "method=FDOC.split('이번 방법 —')[1]")
rep("license='CC0", "palette_note='art/palette/style32.gpl — 게임 컷 b_game 환경 20 + 집 앵커 12', license='CC0")
exec(compile(src, _E, 'exec'))
