# artlive 2128: s05(왼) ↔ s06(오) 같은 캠 나란히 — 4캠 세로로 쌓은 s06_pair.png 1장 + 캠별 s06_pair_<캠>.png
from PIL import Image, ImageDraw
E = 'D:/Github/F1Game/docs/qa/evidence/iter-field_patch/'; RV = 'D:/Github/F1Game/art/_review/2026-10-30-field-asset/'
rows = []
for cam in ('front', 'kart', 'close', 'yard'):
    a, b = Image.open(E + 's05_tex_%s.png' % cam).convert('RGB'), Image.open(E + 's06_finish_%s.png' % cam).convert('RGB')
    im = Image.new('RGB', (a.width + b.width + 8, a.height), (255, 255, 255)); im.paste(a, (0, 0)); im.paste(b, (a.width + 8, 0))
    d = ImageDraw.Draw(im); d.rectangle((0, 0, 150, 22), fill=(0, 0, 0)); d.text((6, 5), 's05 ' + cam, fill=(255, 255, 255))
    d.rectangle((a.width + 8, 0, a.width + 158, 22), fill=(0, 0, 0)); d.text((a.width + 14, 5), 's06 ' + cam, fill=(255, 255, 255))
    im.save(E + 's06_pair_%s.png' % cam); rows.append(im)
P = Image.new('RGB', (rows[0].width, sum(r.height for r in rows) + 8 * 3), (255, 255, 255)); y = 0
for r in rows: P.paste(r, (0, y)); y += r.height + 8
P = P.resize((P.width // 2, P.height // 2), Image.NEAREST)
for d in (E, RV): P.save(d + 's06_pair.png')
print('ok', P.size)
