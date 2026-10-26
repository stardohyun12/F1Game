"""장면 A 소품 키트 텍스처 (아트2, 2026-10-27). v4 텍스처를 그대로 복사하고 키트용만 새로 그린다.
도트 밀도 ≈ 24px/m (v4 건물과 같음 — 720 렌더에서 기준 칸 4~7px). 한 장 3~8색, 노이즈 없음(손으로 정한 블록).
    python -X utf8 tools/asset-gen/opus_kit_tex.py
"""
import json, os, shutil, random
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
V4 = os.path.join(ROOT, 'art', 'source', 'opus-garage-kart', 'tex_v4')
OUT = os.path.join(ROOT, 'art', 'source', 'opus-scene-a-kit', 'tex')


def hx(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


class C:
    def __init__(s, w, h, bg): s.im = Image.new('RGBA', (w, h), hx(bg)); s.w, s.h = w, h

    def r(s, x, y, w, h, c):  # 사각 블록 (y = 위에서부터)
        for i in range(max(0, x), min(s.w, x + w)):
            for j in range(max(0, y), min(s.h, y + h)): s.im.putpixel((i, j), hx(c))
        return s

    def save(s, n): s.im.save(os.path.join(OUT, n + '.png'))


def main():
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(V4): shutil.copy(os.path.join(V4, f), OUT)
    tile = json.load(open(os.path.join(OUT, 'tile_m.json')))
    R = random.Random(7)
    # 우체통: 빨강 몸, 투입구 띠, 크림 표찰, 녹 블록
    c = C(16, 24, '#c4473a').r(0, 0, 16, 2, '#9c3530').r(3, 6, 10, 2, '#3a2a2a').r(4, 11, 8, 5, '#eadbb8').r(5, 12, 6, 1, '#9c3530')
    c.r(5, 14, 4, 1, '#9c3530').r(0, 21, 16, 3, '#9c3530').r(12, 18, 2, 2, '#8a5a3a').r(2, 3, 1, 1, '#e07a60').save('mailbox')
    # 자판기 앞: 크림 틀, 캔 진열 3줄, 동전구, 꺼내는 칸
    c = C(24, 48, '#e8e2d0').r(2, 3, 20, 22, '#7fa6b8')
    cans = ['#c4473a', '#e0b040', '#4d86a8', '#5a9a5a', '#e8e2d0', '#d9803a']
    for row in range(3):
        c.r(2, 9 + row * 7, 20, 1, '#5d7f90')
        for k in range(5): c.r(3 + k * 4, 5 + row * 7, 2, 4, cans[(k + row * 2) % 6])
    c.r(16, 28, 4, 6, '#9aa0a0').r(17, 29, 2, 1, '#3a3a3a').r(4, 28, 9, 2, '#c4473a').r(4, 38, 16, 5, '#3a3a3a').r(0, 46, 24, 2, '#8a8a80')
    c.save('vend_front')
    C(16, 48, '#4d86a8').r(0, 6, 16, 3, '#e8e2d0').r(0, 40, 16, 8, '#3d6e8a').r(5, 20, 3, 2, '#8a5a3a').save('vend_side')
    # 생선 상자(플라스틱) 옆: 파랑/주황 + 손잡이 구멍, 위: 얼음 + 생선
    for n, b, d in (('fishbox_b', '#3f7fb5', '#2e5f8a'), ('fishbox_o', '#d9803a', '#a85e2a')):
        C(16, 8, b).r(0, 0, 16, 1, d).r(6, 2, 4, 2, d).r(0, 7, 16, 1, d).r(2, 4, 1, 2, d).r(13, 4, 1, 2, d).save(n)
    c = C(16, 12, '#e6eef0')
    for (x, y) in ((1, 1), (8, 2), (3, 6), (10, 7), (6, 9)):
        c.r(x, y, 5, 2, '#9aaab4').r(x + 5, y, 1, 2, '#7a8a94').r(x, y, 1, 1, '#3a3a3a')
    c.save('fish_top')
    # 화분 줄(테라코타)
    C(32, 8, '#b5643c').r(0, 0, 32, 2, '#d08050').r(0, 6, 32, 2, '#8e4a2c').r(10, 3, 1, 3, '#8e4a2c').r(21, 3, 1, 3, '#8e4a2c').save('planter')
    # 빨래 3장
    for n, b, s in (('cloth_a', '#efe9da', '#c9c0aa'), ('cloth_b', '#7fa6c8', '#5d84a8'), ('cloth_c', '#d77a6a', '#b05a4c')):
        C(8, 12, b).r(0, 0, 8, 1, s).r(0, 5, 8, 1, s).r(0, 11, 8, 1, s).save(n)
    # 단색 칠 (자전거·스쿠터 틀·사람)
    for n, b, d in (('paint_red', '#c4473a', '#9c3530'), ('paint_teal', '#4f9a94', '#3a7a74'), ('paint_cream', '#e8dcb8', '#c8b890'),
                    ('person', '#7d8a96', '#66727e'), ('porcelain', '#e8e6dc', '#b8b6ac')):
        C(4, 4, b).r(0, 3, 4, 1, d).save(n)
    # 스쿠터 몸: 민트 + 크림 띠 + 녹
    C(24, 16, '#8cc4b0').r(0, 7, 24, 3, '#efe6cc').r(0, 14, 24, 2, '#6aa290').r(18, 3, 3, 2, '#b07a4a').r(4, 11, 2, 1, '#6aa290').save('scooter')
    # 가게 간판: 남색 판, 크림 테, 생선 그림 + 글자 대신 굵은 블록
    c = C(48, 16, '#2e4a6a').r(0, 0, 48, 1, '#efe6cc').r(0, 15, 48, 1, '#efe6cc').r(0, 0, 1, 16, '#efe6cc').r(47, 0, 1, 16, '#efe6cc')
    c.r(4, 6, 9, 4, '#efe6cc').r(13, 5, 3, 6, '#efe6cc').r(5, 7, 1, 1, '#2e4a6a')
    for k, w in enumerate((5, 4, 6, 4)): c.r(20 + k * 6, 5, w, 2, '#e0b040').r(20 + k * 6, 9, w - 1, 2, '#e0b040')
    c.r(30, 12, 3, 1, '#8a6a4a').save('sign_shop')
    # 길 표지: 바랜 파랑, 흰 테, 화살표 + 글자 블록
    c = C(40, 20, '#3d6e9a').r(1, 1, 38, 1, '#eef0ea').r(1, 18, 38, 1, '#eef0ea').r(1, 1, 1, 18, '#eef0ea').r(38, 1, 1, 18, '#eef0ea')
    c.r(5, 9, 8, 2, '#eef0ea').r(5, 7, 2, 6, '#eef0ea').r(4, 8, 1, 4, '#eef0ea')
    for k, w in enumerate((6, 4, 5)): c.r(17 + k * 7, 8, w, 4, '#eef0ea')
    c.r(30, 15, 4, 2, '#8a6a4a').save('sign_road')
    # 차양 줄무늬(빨강/크림 4px), 아래 끝 짙게
    c = C(16, 24, '#efe6cc')
    for x in (0, 8): c.r(x, 0, 4, 24, '#c4473a')
    c.r(0, 21, 16, 3, '#9c3530').r(4, 21, 4, 3, '#c8b890').r(12, 21, 4, 3, '#c8b890').save('awning')
    # 소나무 잎: 짙은 초록 3단 블록
    c = C(16, 16, '#3f6b4a')
    for _ in range(14): c.r(R.randrange(16), R.randrange(16), R.choice((2, 3)), 2, R.choice(('#2f5540', '#5a8a5a')))
    c.save('pine')
    for n in ('mailbox', 'vend_front', 'vend_side', 'fishbox_b', 'fishbox_o', 'fish_top', 'planter', 'cloth_a', 'cloth_b', 'cloth_c',
              'paint_red', 'paint_teal', 'paint_cream', 'person', 'porcelain', 'scooter', 'sign_shop', 'sign_road', 'awning'):
        tile[n] = [1, 1]
    tile['pine'] = [2.6667, 2.6667]
    json.dump(tile, open(os.path.join(OUT, 'tile_m.json'), 'w'), indent=1)
    print('ok', len(os.listdir(OUT)))


main()
