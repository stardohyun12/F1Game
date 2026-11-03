from PIL import Image
im = Image.open('docs/qa/tmp/uiv1/z/fontsize.png').convert('RGB')
W, H = im.size
px = im.load()
bg = px[200, H - 1 - 330]  # inside first bg row (canvas y≈330)
print('bg sample', bg)
def isbg(c): return sum(abs(a - b) for a, b in zip(c, bg)) < 12
# 가로: 캔버스 y=330 줄에서 bg 가 시작·끝나는 x
row = H - 1 - 330
xs = [x for x in range(W) if isbg(px[x, row])]
print('x extent', min(xs), max(xs), 'count', len(xs))
# 세로: x=250 열에서 bg 연속 구간
col = 250
runs = []; start = None
for y in range(H):
    b = isbg(px[col, y])
    if b and start is None: start = y
    if not b and start is not None: runs.append((H - 1 - (y - 1), H - 1 - start, y - start)); start = None
print('vertical runs (canvasYbottom, canvasYtop, len):', runs[:22])
