"""집 j4 — 도현이 고른 사진에서 부품만 잘라 정면으로 펴기 (과제 20261027-2002). PIL 만.
    python tools/asset-gen/opus_houses_j4_photos.py
사진을 통째로 깔지 않는다: 네 모서리(원본 픽셀)로 원근을 펴고 필요한 부분만 남긴다. 빛 펴기·축소·40색은 opus_houses_j4.py(Blender) 에서.
출력: art/_review/2026-10-27-houses-j4/_work/parts/<부품>.png · parts.json · ../provenance.csv
"""
import os, json, csv
from PIL import Image
S = 'art/source/photo-picks/'; O = 'art/_review/2026-10-27-houses-j4/_work/parts/'; os.makedirs(O, exist_ok=True)
SRC = {'A2': ('A/A2_apricot_facade_burano.jpg', 'https://commons.wikimedia.org/wiki/File:Corte-Comare-Burano-20050526-022.jpg', 'CC0 1.0', 'Gary Houston'),
       'B1': ('B/B1_shutters_greengrocer.jpg', 'https://commons.wikimedia.org/wiki/File:%E5%85%AB%E7%99%BE%E5%B1%8B%E3%81%AE%E3%81%8A%E7%9B%86%E4%BC%91%E3%81%BF.jpg', 'CC0 1.0', 'Syced'),
       'C1': ('C/C1_blue_louvered_urbino.jpg', 'https://commons.wikimedia.org/wiki/File:Blue_Louvered_Window_in_Urbino,_Italy.jpg', 'CC0 1.0', 'Julian Lupyan'),
       'C2': ('C/C2_green_shutters_liguria.jpg', 'https://commons.wikimedia.org/wiki/File:Finestra_ligure_dipinta_1.jpg', 'CC0 1.0', 'Dixy52'),
       'D1': ('D/D1_rice_shop_yoyogi.jpg', 'https://commons.wikimedia.org/wiki/File:Rice_and_food_shop_in_Yoyogi.jpg', 'CC0 1.0', 'Syced'),
       'D2': ('D/D2_soba_shop_noren.jpg', 'https://commons.wikimedia.org/wiki/File:Mud%C5%8D_(Tokiwadaira,_Matsudo)_02.jpg', 'CC0 1.0', 'Higa4'),
       'E2': ('E/E2_clay_roof_tiles_02.jpg', 'https://polyhaven.com/a/clay_roof_tiles_02', 'CC0', 'Amal Kumar'),
       'E3': ('E/E3_clay_roof_tiles_03.jpg', 'https://polyhaven.com/a/clay_roof_tiles_03', 'CC0', 'Amal Kumar')}
# 부품: (사진, 네 모서리 TL TR BR BL — 원본 픽셀, 출력 크기 px, 실제 크기 m(가로, 세로) 또는 None=면에 늘림, 무엇)
PARTS = {
    'shutter_C1': ('C1', [(182, 404), (392, 370), (378, 1436), (168, 1442)], (120, 480), None, '파란 루버 덧창 한 짝(왼쪽)'),
    'shutter_C2': ('C2', [(352, 520), (402, 545), (402, 788), (352, 763)], (120, 480), None, '초록 덧창 한 짝(가운데 창 왼쪽 짝)'),
    'shutter_B1': ('B1', [(1050, 430), (1588, 430), (1588, 962), (1050, 962)], (400, 400), None, '채소가게 셔터 한 칸(오른쪽, 전선 아래)'),
    'front_D1': ('D1', [(488, 588), (1180, 487), (1190, 1052), (488, 1045)], (480, 360), None, '쌀가게 1층 갈색 셔터(차양 아래)'),
    'front_D2': ('D2', [(243, 430), (625, 430), (625, 1080), (243, 1080)], (240, 400), None, '소바집 노렌 + 격자문'),
    'roof_E2': ('E2', [(0, 0), (1600, 0), (1600, 1600), (0, 1600)], (512, 512), (3.2, 3.2), '둥근 기와 줄(타일, 3.2m 로 봄)'),
    'roof_E3': ('E3', [(0, 0), (1600, 0), (1600, 1600), (0, 1600)], (512, 512), (3.0, 3.0), '평기와(타일, 3.0m 로 봄)'),
    'wall_A2': ('A2', [(225, 500), (950, 500), (950, 925), (225, 925)], (256, 150), None, '살구 벽 — 색만 씀(중앙값), 결·얼룩은 안 씀'),
}
meta = {}
for k, (pid, q, size, phys, what) in PARTS.items():
    im = Image.open(S + SRC[pid][0]).convert('RGB')
    (x0, y0), (x1, y1), (x2, y2), (x3, y3) = q
    out = im.transform(size, Image.QUAD, data=(x0, y0, x3, y3, x2, y2, x1, y1), resample=Image.BICUBIC)  # QUAD 순서: 왼위·왼아래·오른아래·오른위
    out.save(O + k + '.png'); meta[k] = dict(photo=pid, quad=q, size=size, phys_m=phys, what=what)
json.dump(meta, open(O + 'parts.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
USE = {'shutter_C1': 'house_narrow_b, house_shop 덧창', 'shutter_C2': 'house_narrow_a, house_slope 덧창', 'shutter_B1': 'house_warehouse 1층 문 칸',
       'front_D1': 'house_shop 1층 가게 정면(문 칸)', 'front_D2': 'house_cafe 1층 문(노렌)', 'roof_E2': 'house_narrow_a, house_shop, house_warehouse 지붕',
       'roof_E3': 'house_slope, house_cafe 지붕', 'wall_A2': 'house_shop 벽색 기준(색만)'}
with open('art/_review/2026-10-27-houses-j4/provenance.csv', 'w', encoding='utf-8-sig', newline='') as f:
    w = csv.writer(f); w.writerow(['part', 'photo_id', 'file', 'source_url', 'license', 'author', 'crop_quad_px(TL,TR,BR,BL)', 'used_on', 'what'])
    for k, m in meta.items():
        s = SRC[m['photo']]; w.writerow([k, m['photo'], 'art/source/photo-picks/' + s[0], s[1], s[2], s[3], ' '.join(f'{a},{b}' for a, b in m['quad']), USE[k], m['what']])
print('parts', list(meta))
