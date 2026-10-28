"""house_home 0419: 실제 Unity 캡처만으로 승인용 비교 시트 생성."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

R = Path(__file__).resolve().parents[3]
S = R / 'docs/qa/evidence/studio-house_home'
O = R / 'art/_review/2026-10-29-house-home-calm'
O.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
views = [('in_living', '거실'), ('in_stairs', '계단·천장'), ('in_hall2f', '2층 복도'), ('in', '주인공 방')]
sheet = Image.new('RGB', (2560, 820), '#202827')
draw = ImageDraw.Draw(sheet)
for row, prefix, label in [(0, 's03_interior', '수정 전'), (1, 's04_calm', '수정 후')]:
    for col, (view, title) in enumerate(views):
        im = Image.open(S / f'{prefix}_{view}.png').convert('RGB').resize((640, 360), Image.Resampling.LANCZOS)
        sheet.paste(im, (640 * col, row * 410 + 42))
        draw.text((640 * col + 12, row * 410 + 8), f'{label} · {title}', font=font, fill='white')
sheet.save(O / 'sheet.png')
links = ''.join(f'<figure><a href="../../../docs/qa/evidence/studio-house_home/s04_calm_{v}.png"><img src="../../../docs/qa/evidence/studio-house_home/s04_calm_{v}.png"></a><figcaption>{t}</figcaption></figure>' for v, t in views + [('front', '외관'), ('balcony', '발코니'), ('window', '창틀·유리')])
(O / 'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>house_home 실내 결 검토</title>
<style>body{margin:32px;background:#edf0e8;color:#26332f;font:17px/1.65 sans-serif}main{max-width:1440px;margin:auto}img{width:100%;height:auto}figure{margin:24px 0}a{color:#365f65}.grid{display:grid;grid-template-columns:1fr 1fr;gap:20px}</style><main>
<h1>house_home — 실내 결 마감</h1><p>도현 승인 필요 · 실제 F1Studio 캡처 · 본 F1 미적용</p>
<p>따뜻한 미색 회벽과 밝은 천장, 드문 사진 결 ±8%. 주인공 방의 미색 상단·하늘색 판벽·갈색 몰딩 복원.</p>
<a href="sheet.png"><img src="sheet.png" alt="위: 수정 전, 아래: 수정 후 비교 시트"></a>
<p>발코니 3.7×1.8m · 창틀 벽 안 6~10cm · 유리 16장. 기존 실내 구조 유지. 건물 본체 2,794 tris(중첩 방 가구 제외).</p>
<div class="grid">''' + links + '''</div><p>검사 및 남은 이슈: <a href="../../../docs/team/reports/art-20261029-0419.md">보고서</a></p></main>''', encoding='utf-8')
print(O / 'index.html')
