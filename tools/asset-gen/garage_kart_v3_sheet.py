from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-27-garage-kart-v3';REF=R/'art/reference/dotlp-2026-10-27';font=ImageFont.truetype(str(R/'art/_review/2026-10-23-course-ui/fonts/Galmuri11.ttf'),20)
files=['garage_v3_front','garage_v3_threequarter','garage_v3_interior','garage_v3_1x_6m','kart_v3_front','kart_v3_threequarter','kart_v3_side','kart_v3_1x_6m'];checks=[]
for n in files:
 im=Image.open(V/(n+'.png')).convert('RGB');assert im.size==(960,540);up=im.resize((1920,1080),Image.Resampling.NEAREST);up.save(V/(n+'_1080.png'));checks.append(dict(file=n,source=list(im.size),output=list(up.size),exact_nearest=up.resize(im.size,Image.Resampling.NEAREST).tobytes()==im.tobytes()))
# Every display crop uses coordinates on a 1920x1080 image. No fit-to-cell rescaling.
refs=[('01','a-short-hike_1.jpg',(100,300,740,660),(290,400,450,560)),('06','easy-delivery-co_1.jpg',(680,440,1320,800),(920,560,1080,720)),('12','crow-country_1.jpg',(100,650,740,1010),(600,740,760,900))]
ours=[('garage_v3_threequarter',(600,160,1240,520),(1140,400,1300,560)),('kart_v3_threequarter',(850,480,1490,840),(1220,520,1380,680)),('kart_v3_threequarter',(750,280,1390,640),(1080,420,1240,580))]
sheet=Image.new('RGB',(1920,2160),'#d8c9a8');d=ImageDraw.Draw(sheet)
for i,((label,ref,b1,b4),(own,o1,o4)) in enumerate(zip(refs,ours)):
 ri=Image.open(REF/ref);oi=Image.open(V/(own+'_1080.png'))
 for y,img,bx,scale in [(40,ri,b1,1),(440,oi,o1,1),(840,ri,b4,4),(1520,oi,o4,4)]:
  c=img.crop(bx)
  if scale==4:c=c.resize((640,640),Image.Resampling.NEAREST)
  sheet.paste(c,(i*640,y))
 for y,txt in [(10,label+' 기준 1×'),(410,'우리 컷 1×'),(810,label+' 기준 4×'),(1490,'우리 컷 4×')]:d.text((i*640+8,y),txt,font=font,fill='#36565a')
sheet.save(V/'sheet.png')
views=Image.new('RGB',(1920,2160))
for i,n in enumerate(files):views.paste(Image.open(V/(n+'.png')),(i%2*960,i//2*540))
views.save(V/'views.png')
(V/'scale-check.json').write_text(json.dumps(checks,indent=2));(V/'comparison-crops.json').write_text(json.dumps(dict(refs=refs,ours=ours),indent=2))
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>D165 정비소·고카트 v3</title><style>body{background:#d8c9a8;color:#36565a;font:18px sans-serif}img{max-width:100%}</style><h1>정비소·고카트 v3 — 도현 판정 필요</h1><p>960×540 → 1920×1080 Nearest2배. 기준과 우리 컷은 같은1080p 좌표에서 원본 크롭과4배 확대. PNG를100%로 열어 판정.</p><a href="measurement.md">제작 전 측정</a> · <a href="visual-gate.md">독립 판정</a> · <a href="scale-check.json">확대 검사</a><h2>기준 비교</h2><a href="sheet.png"><img src="sheet.png"></a><h2>흑실루엣</h2><img src="silhouette.png"><h2>각 방향·6m 거리</h2><img src="views.png"><p>참고 이미지: 내부 비교만. 에셋·빌드 미사용.</p>',encoding='utf8')
