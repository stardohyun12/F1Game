# artlive 1914 (D-196, 도현 승인): 보관소·우체국 텍스처 분리. 7장이 md5 까지 같았다.
#  조사(docs/qa/tmp/cells_1914.py): 보관소 몸 벽 = wall.png 크림 칸(오른쪽 위 256², 263m²) + 살구 칸 24m², 지붕 = roof.png E2 칸.
#  우체국 몸 벽 = 크림 180 + 살구 80 + 살구B 30m², 지붕 = E3 칸 → 지붕은 이미 다른 칸이라 그대로.
#  택배 상자 = 살구B 칸(+ 크림 칸 띠) → 살구B 는 손대지 않는다.
#  보관소 자기 wall.png 의 크림 칸만 "바랜 회청"으로: 칸 평균색 → 목표색으로 채널 배율(명도 결·픽셀 단 그대로).
#  그 뒤 보관소 자기 palette.gpl = 기존 색 + 회청 칸 median-cut 24색, 칸을 그 팔레트로 다시 양자화(디더 없음, 교훈 16).
#  우체국은 지금 파일이 자기 사본(art/export/q4-postoffice/*.png + palette.gpl) — 그대로 둔다.
# python -X utf8 tools/asset-gen/d196_split_1914.py
import io, subprocess
from pathlib import Path
import numpy as np
from PIL import Image
R = Path(__file__).resolve().parents[2]
DEP = R / 'art/export/q3-depot'; LIB = R / 'F1/Assets/Art/Library/Buildings/depot'
TARGET = np.array([176, 188, 194], np.float64)  # 바랜 회청(밝은 칸 평균) — 우체국 살구·크림과 갈리게
def head(rel):
    return Image.open(io.BytesIO(subprocess.run(['git', 'cat-file', '--filters', 'HEAD:' + rel], cwd=R, capture_output=True, check=True).stdout))
def load_gpl(p):
    return [tuple(int(x) for x in l.split()[:3]) for l in p.read_text(encoding='utf-8').splitlines() if l[:1].isdigit() or (l[:1] == ' ' and l.strip()[:1].isdigit())]

im = head('art/export/q3-depot/wall.png').convert('RGB'); a = np.asarray(im, np.float64).copy()
cell = a[0:256, 256:512]; mean = cell.reshape(-1, 3).mean(0)
rec = np.clip(cell * (TARGET / mean), 0, 255)
q = Image.fromarray(rec.round().astype(np.uint8)).quantize(colors=24, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
new = [tuple(int(c) for c in q.getpalette()[i * 3:i * 3 + 3]) for i in sorted(set(np.asarray(q).ravel().tolist()))]
a[0:256, 256:512] = np.asarray(q.convert('RGB'), np.float64)
Image.fromarray(a.astype(np.uint8)).save(DEP / 'wall.png'); Image.fromarray(a.astype(np.uint8)).save(LIB / 'wall.png')
gp = DEP / 'palette.gpl'; old = head('art/export/q3-depot/palette.gpl') if False else None
txt = subprocess.run(['git', 'show', 'HEAD:art/export/q3-depot/palette.gpl'], cwd=R, capture_output=True, check=True).stdout.decode('utf-8')
have = set(load_gpl_t := [tuple(int(x) for x in l.split()[:3]) for l in txt.splitlines() if l.strip()[:1].isdigit()])
add = [c for c in new if c not in have]
txt = txt.rstrip('\n') + '\n# artlive 1914 (D-196): 보관소 벽 크림 칸 → 바랜 회청 %s\n' % (tuple(TARGET.astype(int)),) + \
      ''.join('%3d %3d %3d\td196_bluegrey_%02d\n' % (*c, i) for i, c in enumerate(add))
gp.write_text(txt, encoding='utf-8')
print('cell mean', mean.round(1), '→', TARGET, '| new colors', len(new), 'added to gpl', len(add))
