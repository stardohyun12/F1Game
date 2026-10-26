"""과제 0601 — A2b 텍스처: 길(아스팔트)·돌담 타일을 32색으로, 땅·나뭇잎 점무늬 대비 낮추기.
CC0 사진(Poly Haven, art/source/hillhouse-surfaces/photos) → 32 texel/m 로 줄이고 scene-a-v5-combo/palette32.gpl 가까운 색(디더 없음)."""
from PIL import Image, ImageFilter, ImageChops, ImageEnhance
A = 'F1/Assets/Art/SceneA2/'
pal = [tuple(int(v) for v in l.split()[:3]) for l in open('art/export/scene-a-v5-combo/palette32.gpl') if l[:1].isdigit() or (l[:1] == ' ' and l.strip()[:1].isdigit())]
pimg = Image.new('P', (1, 1)); flat = [c for p in pal for c in p]; pimg.putpalette(flat + flat[:3] * (256 - len(pal)))
def q(im): return im.convert('RGB').quantize(palette=pimg, dither=Image.Dither.NONE).convert('RGB')
def soft(im, k=0.5, r=3):   # 점무늬(고주파) 대비를 k 배로 — 큰 덩어리 색은 유지
    im = im.convert('RGB'); b = im.filter(ImageFilter.GaussianBlur(r))
    return Image.blend(b, im, k)
def norm(im, tgt):   # 평균색을 tgt 로 맞춤(사진 노출 차이 제거)
    st = [sum(c) / len(c) for c in zip(*im.getdata())]
    return Image.merge('RGB', [ch.point(lambda v, f=t / max(1, m): min(255, int(v * f))) for ch, t, m in zip(im.split(), tgt, st)])
photo = 'art/source/hillhouse-surfaces/photos/'
# 길: worn_asphalt 2 m → 64 px(32 texel/m). 조금 밝히고 푸른 회색 쪽으로 — 마당 흙(따뜻한 주황)과 명도·색 구분
a = Image.open(photo + 'worn_asphalt_diff_1k.jpg').convert('RGB').resize((64, 64), Image.LANCZOS)
a = norm(soft(a, 0.6, 2), (96, 110, 101))
q(a).save(A + 'Gen/asphalt_tile.png')
# 돌담·연석: stone_block_wall (가설 2 m) → 64 px
w = Image.open(photo + 'stone_block_wall_diff_1k.jpg').convert('RGB').resize((64, 64), Image.LANCZOS)
q(norm(soft(w, 0.8, 1), (158, 152, 112))).save(A + 'Gen/wall_tile.png')
# 대비 낮춤: 풀 타일, 마당 판, 나무 5장
q(soft(Image.open(A + 'Gen/grass_tile.png'), 0.45, 3)).save(A + 'Gen/grass_tile_soft.png')
q(soft(Image.open(A + 'far/yard_ground_baked32.png'), 0.5, 3)).save(A + 'Gen/yard_ground_soft.png')
for i in range(1, 6):
    t = Image.open(A + f'kit/autumn_tree_0{i}.png'); al = t.getchannel('A') if t.mode == 'RGBA' else None
    s = q(soft(t, 0.5, 2))
    if al: s.putalpha(al)
    s.save(A + f'Gen/autumn_tree_0{i}_soft.png')
# 전봇대 나무: wood_peeling_paint_weathered → 갈색으로, 전선: 어두운 1색
q(norm(soft(Image.open(photo + 'wood_peeling_paint_weathered_diff_1k.jpg').convert('RGB').resize((32, 32), Image.LANCZOS), 0.7, 1), (112, 80, 50))).save(A + 'Gen/pole_wood.png')
Image.new('RGB', (4, 4), (30, 36, 38)).save(A + 'Gen/wire.png')
print('pal', len(pal), 'ok')
