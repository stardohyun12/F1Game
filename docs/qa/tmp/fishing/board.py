# 연속 프레임 보드: 찌 둘레 크롭 ×4 최근접. 사용: python board.py TAG cx cy first last [step]
import sys
from PIL import Image
tag, cx, cy, a, b = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5])
step = int(sys.argv[6]) if len(sys.argv) > 6 else 1
D = 'docs/qa/evidence/fishing-ripple/' + tag + '/'
W, H, S = 96, 54, 4
idx = list(range(a, b + 1, step))
if len(sys.argv) > 7: idx = [int(v) for v in sys.argv[7].split(',')]   # 골라 찍기
cols = 4 if len(idx) != 3 else 3
rows = (len(idx) + cols - 1) // cols
out = Image.new('RGB', (cols * W * S + (cols - 1) * 4, rows * H * S + (rows - 1) * 4), (20, 20, 20))
for k, i in enumerate(idx):
    im = Image.open(D + f'{tag}_{i:03d}.png').convert('RGB')
    c = im.crop((cx - W // 2, cy - H // 2, cx + W // 2, cy + H // 2)).resize((W * S, H * S), Image.NEAREST)
    out.paste(c, ((k % cols) * (W * S + 4), (k // cols) * (H * S + 4)))
out.save(D + f'board_{tag}_{a}-{b}.png')
print('saved', D + f'board_{tag}_{a}-{b}.png', len(idx))
