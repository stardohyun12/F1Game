"""Review-only deterministic compositing of the real GLB render on a concept plate."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import json
R=Path(__file__).resolve().parent;ROOT=R.parents[2]
FONT=ROOT/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf'
def font(s=22):return ImageFont.truetype(str(FONT),s)
def fit(im,size):return im.convert('RGB').resize(size,Image.Resampling.NEAREST)
bg=Image.open(R/'concept-empty.png').convert('RGBA')
for dist in [30,45,60]:
    p=R/f'distance-{dist}m-render.png'
    if not p.exists():continue
    render=Image.open(p).convert('RGBA')
    plate=bg.resize(render.size,Image.Resampling.NEAREST)
    plate.alpha_composite(render)
    plate.convert('RGB').save(R/f'composite-{dist}m.png')
    plate.resize((960,540),Image.Resampling.NEAREST).convert('RGB').save(R/f'composite-{dist}m-960.png')
if (R/'composite-45m.png').exists():
    Image.open(R/'composite-45m.png').save(R/'composite.png')
    Image.open(R/'composite-45m-960.png').save(R/'composite-960.png')
def neutral(src,dest):
    im=Image.open(R/src).convert('RGBA');back=Image.new('RGBA',im.size,(202,208,207,255));back.alpha_composite(im);back.convert('RGB').save(R/dest)
for name in ['close','entry-detail','front','side','back','top']:
    if (R/(name+'-render.png')).exists():neutral(name+'-render.png',name+'.png')
if (R/'top.png').exists():
    sheet=Image.new('RGB',(1920,1400),'#e5e2d7');d=ImageDraw.Draw(sheet);d.text((32,24),'홀로 떨어진 집 | 실측 비례 · 32px/m',font=font(28),fill='#30362f')
    for i,name in enumerate(['front','side','back','top']):
        x=(i%2)*960;y=80+(i//2)*650;sheet.paste(fit(Image.open(R/(name+'.png')),(960,600)),(x,y));d.text((x+24,y+606),{'front':'정면','side':'측면','back':'후면','top':'위'}[name],font=font(),fill='#30362f')
    sheet.save(R/'view-sheet.png')
if (R/'composite-60m-960.png').exists():
    sheet=Image.new('RGB',(1920,1230),'#e5e2d7');d=ImageDraw.Draw(sheet)
    d.text((28,20),'같은 GLB · 수평 FOV 50° · 카메라 높이 2.1m · 960×540',font=font(26),fill='#30362f')
    for i,dist in enumerate([30,45,60]):
        x=(i%2)*960;y=70+(i//2)*580;sheet.paste(Image.open(R/f'composite-{dist}m-960.png'),(x,y));d.text((x+24,y+544),f'정면 벽까지 {dist}m',font=font(),fill='#30362f')
    d.text((990,720),'집 본체 7.2 × 5.8m\n문 0.92 × 2.08m\n지붕마루 5.25m / 굴뚝 6.4m\n\n그림 배경은 검토용 원화\n집은 내보낸 GLB의 실제 렌더\nUnity 플레이 화면이 아님',font=font(24),fill='#30362f',spacing=14)
    sheet.save(R/'distance-sheet.png')
if (R/'close.png').exists():
    sheet=Image.new('RGB',(1920,1180),'#e5e2d7');d=ImageDraw.Draw(sheet)
    d.text((24,18),'방향 원화 / 실제 제작물',font=font(28),fill='#30362f')
    sheet.paste(fit(Image.open(R/'concept.png'),(960,540)),(0,65));sheet.paste(fit(Image.open(R/'composite-960.png'),(960,540)),(960,65))
    d.text((24,612),'AI 검토 원화 · 게임 파일에 미포함',font=font(),fill='#30362f');d.text((984,612),'실제 GLB 원화 합성 · 45m · 960×540',font=font(),fill='#30362f')
    sheet.paste(fit(Image.open(R/'close.png'),(800,500)),(0,665))
    old=Image.open(ROOT/'art/concept/lofi-styles-2026-10-13/02_warm_street.png');sheet.paste(fit(old,(890,500)),(1000,665))
    d.text((815,750),'근접\n↔\n기준\n원화',font=font(),fill='#30362f',spacing=10)
    sheet.save(R/'compare.png')
print('review sheets written')
