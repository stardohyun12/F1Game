"""Kenney colormap(512×512 색 띠 격자) 를 구역별 팔레트로 다시 칠한 변형을 만든다 (A+C 팔레트 실험, 2026-10-12).

건물 메시가 실제로 샘플하는 칸 (UV 16×16 격자, 실측):
  - 가운데 띠 (y 256~384):  x 0~64 회색 외벽 / 64~128 진회색 트림 / 128~192 연회색 / 192~256 흰색
  - 위 띠    (y 128~256):  x 320~384 하늘색 유리,  x 64~128 노랑·주황 포인트
각 칸은 세로 그라데이션(위 밝고 아래 어두움)이라, 목표색 × (픽셀 밝기 / 칸 평균 밝기) 로 칠해 음영을 살린다.

사용:  python -X utf8 tools/asset-gen/palette_colormap.py
출력:  F1/Assets/Rendering/Palette/colormap_<kit>_<zone>.png
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KITS = {
    "industrial": "F1/Assets/Model/kenney_city-kit-industrial_1.0/Models/FBX format/Textures/colormap.png",
    "commercial": "F1/Assets/Model/kenney_city-kit-commercial_2.1/Models/FBX format/Textures/colormap.png",
    "suburban":   "F1/Assets/Model/kenney_city-kit-suburban_20/Models/FBX format/Textures/colormap.png",
}
OUT = os.path.join(ROOT, "F1/Assets/Rendering/Palette")

# 칸 이름 → (x0, x1, y0, y1)  (이미지 픽셀, 위가 0)
CELLS = {
    "facade":  (0, 64, 256, 384),
    "trim":    (64, 128, 256, 384),
    "light":   (128, 192, 256, 384),
    "white":   (192, 256, 256, 384),
    "glass":   (320, 384, 128, 256),
    "accent":  (64, 128, 128, 256),
}

# 구역별 목표색 (디오라마 C 그림에서 뽑은 방향: 크림·테라코타·청록 / 모래·네이비 / 카키·녹)
ZONES = {
    "downtown": {"facade": (0xE9, 0xDC, 0xC2), "trim": (0x2E, 0x5F, 0x6A), "light": (0xF3, 0xEA, 0xD6),
                 "white": (0xFB, 0xF6, 0xEA), "glass": (0xC9, 0xE4, 0xE4), "accent": (0xE0, 0x7A, 0x4F)},
    "harbor":   {"facade": (0xE5, 0xCE, 0x9E), "trim": (0x2C, 0x3E, 0x57), "light": (0xF1, 0xE4, 0xC6),
                 "white": (0xF8, 0xF2, 0xE2), "glass": (0xD2, 0xE7, 0xEC), "accent": (0xD9, 0x5B, 0x43)},
    "industrial": {"facade": (0xB8, 0xA5, 0x79), "trim": (0x8A, 0x4A, 0x33), "light": (0xD6, 0xC9, 0xA6),
                   "white": (0xEB, 0xE3, 0xCE), "glass": (0xD4, 0xDE, 0xD2), "accent": (0xE0, 0x8A, 0x3A)},
}

def luma(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]

def recolor(im, cell, target):
    x0, x1, y0, y1 = cell
    px = im.load()
    vals = [luma(px[x, y]) for y in range(y0, y1) for x in range(x0, x1) if px[x, y][3] > 0]
    if not vals:
        return
    mean = sum(vals) / len(vals)
    for y in range(y0, y1):
        for x in range(x0, x1):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            k = luma((r, g, b)) / mean if mean > 0 else 1.0
            k = 0.75 + (k - 1.0) * 0.9 + 0.25  # 음영 폭을 살짝 줄여 플랫 느낌 유지
            px[x, y] = (min(255, int(target[0] * k)), min(255, int(target[1] * k)), min(255, int(target[2] * k)), a)

def main():
    os.makedirs(OUT, exist_ok=True)
    for kit, rel in KITS.items():
        src = Image.open(os.path.join(ROOT, rel)).convert("RGBA")
        for zone, pal in ZONES.items():
            im = src.copy()
            for cell, col in pal.items():
                recolor(im, CELLS[cell], col)
            out = os.path.join(OUT, f"colormap_{kit}_{zone}.png")
            im.save(out)
            print("OK", os.path.relpath(out, ROOT))

if __name__ == "__main__":
    main()
