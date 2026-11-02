"""Directly authored drawings, source-photo processing. No image synthesis model."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import numpy as np,json,hashlib
R=Path(__file__).resolve().parents[3];S=R/'art/source/packing-materials';V=R/'art/_review/2026-11-02-packing-materials'
for p in (S/'textures',V/'concept',V/'evidence'):p.mkdir(parents=True,exist_ok=True)
def font(n):return ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
src=R/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png'
im=Image.open(src).convert('L').crop((810,570,990,690)).resize((256,256))
a=np.asarray(im,dtype=float);lo=np.asarray(im.filter(ImageFilter.GaussianBlur(12)),dtype=float);grain=a-lo;grain=np.clip(grain/max(np.percentile(abs(grain),95),1),-1,1)
# Fine paper fibres, not centimeter-scale cloudy patches. Resample and repeat the same photo crop.
grain=np.tile(np.asarray(Image.fromarray(grain.astype('float32')).resize((64,64),Image.Resampling.BILINEAR)),(4,4))
palette={'kraft':'ae9065','kraft_edge':'9b8059','paper':'cbc7b3','pulp':'aaa58c','egg':'d2c6ac','tape':'b79861','label':'d6d2c1','ink':'71766c','core':'aa9879'}
manifest=[]
for name,h in palette.items():
 rgb=np.array([int(h[i:i+2],16) for i in (0,2,4)])
 out=Image.fromarray(np.clip(rgb*(1+grain[:,:,None]*.035),0,255).astype('uint8'))
 if name=='paper':
  # Abstract soft bars: no glyphs, actual article, logo, barcode or personal data.
  marks=Image.new('RGBA',(256,256));d=ImageDraw.Draw(marks)
  for c in range(4):
   for line in range(29):
    y=15+line*8;x=12+c*61;length=38+((line*13+c*7)%14)
    d.rectangle((x,y,x+length,y+2),fill=(71,81,77,90))
  marks=marks.filter(ImageFilter.GaussianBlur(1.5));out=Image.alpha_composite(out.convert('RGBA'),marks).convert('RGB')
 out.save(S/'textures'/f'{name}.png')
 manifest.append({'name':name,'source':str(src.relative_to(R)),'source_url':'https://ambientcg.com/view?id=CardboardSet001','license':'CC0-1.0','processing':'blank paper crop; subtract low-frequency lighting; downsample/repeat source grain 4x; bound grain +/-3.5%; recolor; paper only adds blurred authored non-text bars'})
# Bubble contours manually measured from the CC0 photo: one oblique pair of cells.
# Only geometry measurements and high-pass micro-roughness are transferred. Source highlights never enter albedo.
photo=Image.open(S/'reference/bubble_wrap_cc0.jpg').convert('L')
crop=photo.crop((1500,1100,2000,1600)).resize((256,256))
micro=np.asarray(crop,dtype=float)-np.asarray(crop.filter(ImageFilter.GaussianBlur(5)),dtype=float)
micro=np.clip(micro/max(np.percentile(abs(micro),95),1),-1,1)
N=256;yy,xx=np.mgrid[:N,:N]/N;h=np.zeros((N,N));seal=np.zeros_like(h)
for cx,cy,rx,ry in [(.25,.25,.205,.19),(.75,.75,.195,.207)]:
 dx=(xx-cx+.5)%1-.5;dy=(yy-cy+.5)%1-.5;r=np.sqrt((dx/rx)**2+(dy/ry)**2)
 h=np.maximum(h,np.sqrt(np.maximum(0,1-r*r)))
 seal=np.maximum(seal,np.exp(-((r-1)/.06)**2))
Image.fromarray((h*255).astype('uint8')).save(S/'textures/bubble_height.png')
dy,dx=np.gradient(h);n=np.stack((-dx*26,-dy*26,np.ones_like(h)),axis=2);n/=np.linalg.norm(n,axis=2)[:,:,None]
Image.fromarray(((n*.5+.5)*255).astype('uint8')).save(S/'textures/bubble_normal.png')
rgba=np.zeros((N,N,4),dtype=np.uint8);rgba[:,:,:3]=np.clip(np.array([166,188,181])*(1+micro[:,:,None]*.018),0,255);rgba[:,:,3]=np.clip(65+seal*85+h*20,0,255)
Image.fromarray(rgba).save(S/'textures/bubble_rgba.png')
Image.fromarray(rgba[:,:,:3]).save(S/'textures/bubble_albedo.png')
Image.fromarray(np.clip(60+micro*6+seal*20,0,255).astype('uint8')).save(S/'textures/bubble_roughness.png')
manifest.append({'name':'bubble maps','source':'art/source/packing-materials/reference/bubble_wrap_cc0.jpg','source_url':'https://www.rawpixel.com/image/6040280/photo-image-texture-public-domain-plastic','license':'CC0-1.0','processing':'manual cell contour interpretation; source high-pass roughness; neutral albedo, authored dome normal/height/alpha, no source highlights or shadow bake','repeat_metres':.024,'crop':[1500,1100,2000,1600]})
for m in manifest:m['source_sha256']=hashlib.sha256((R/m['source']).read_bytes()).hexdigest()
(S/'texture_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
items=[('boxes','골판지 상자 S / M / L','내경 .320×.100×.320 / .315×.150×.315 / .500×.300×.500 m','뚜껑 날개 4장 · 바닥 중앙 피벗'),('bubble','뽁뽁이 롤 + 풀린 시트','롤 Ø.160 × .320 m / 시트 폭 .300 m','반투명 필름 · 실제 빛에 반응하는 알갱이'),('newspaper','신문지 묶음 + 구긴 공 A / B','묶음 .300×.060×.220 m / 공 Ø.110→.048 m','읽을 수 없는 열 무늬 · 기사와 로고 없음'),('eggs','30구 달걀판','콜라이더 .300×.065×.300 m / 5 × 6 배열','회갈색 종이판 · 미색 달걀 30알'),('tools','박스테이프 롤 + 주소 라벨','롤 Ø.100×.025 m / 라벨 .100×.020×.070 m','황갈색 테이프 · 무문자 미색 라벨')]
(S/'design.json').write_text(json.dumps(items,ensure_ascii=False,indent=2),encoding='utf8')
for kind,title,dims,note in items:
 im=Image.new('RGB',(1400,850),'#ece7db');d=ImageDraw.Draw(im);d.text((48,35),title,font=font(38),fill='#2f4947');d.text((48,100),dims,font=font(24),fill='#64736a')
 if kind=='boxes':
  for i,(w,hh) in enumerate([(230,85),(225,130),(310,190)]):
   x=100+i*425;y=510;d.polygon([(x,y),(x+w,y+15),(x+w,y-hh),(x,y-hh-15)],fill='#ae9065');d.polygon([(x,y-hh-15),(x+w,y-hh),(x+w+55,y-hh-80),(x+55,y-hh-95)],fill='#806b4d');d.polygon([(x,y-hh-15),(x+55,y-hh-95),(x+10,y-hh-145),(x-45,y-hh-65)],fill='#bfa67c');d.polygon([(x+w,y-hh),(x+w+55,y-hh-80),(x+w+105,y-hh-35),(x+w+50,y-hh+45)],fill='#b39a70');d.text((x+50,560),['S','M','L'][i],font=font(28),fill='#2f4947')
 elif kind=='bubble':
  d.rounded_rectangle((110,260,470,530),radius=120,fill='#bac7bc');d.ellipse((105,260,240,530),fill='#cfd8cd');d.ellipse((147,340,200,450),fill='#8f8f79');d.polygon([(600,300),(1180,360),(1100,570),(520,510)],fill='#cad3c5')
  for y in range(330,520,25):
   for x in range(620,1080,25):d.ellipse((x,y,x+14,y+13),outline='#a6bdb6',width=2)
 elif kind=='newspaper':
  for y in range(470,330,-8):d.polygon([(100,y),(500,y+35),(580,y-90),(180,y-125)],fill='#cbc7b3',outline='#b6b4a1')
  for x in [760,1090]:d.polygon([(x-90,390),(x-40,295),(x+55,305),(x+110,400),(x+40,500),(x-70,490)],fill='#c9c4ae');d.line([(x-90,390),(x+25,370),(x-10,460),(x+110,400)],fill='#a5a798',width=5)
 elif kind=='eggs':
  d.polygon([(250,560),(1000,560),(1170,300),(420,300)],fill='#aaa58c')
  for z in range(6):
   for x in range(5):
    xx=390+x*130-z*22;yy=310+z*38;d.ellipse((xx,yy,xx+68,yy+80),fill='#d2c6ac',outline='#b5b29a',width=2)
 else:
  d.ellipse((170,270,550,530),fill='#b79861');d.ellipse((265,330,455,465),fill='#9a967f');d.ellipse((282,343,439,455),fill='#ece7db');d.polygon([(760,330),(1130,360),(1060,530),(690,500)],fill='#d6d2c1')
 d.text((48,690),note,font=font(27),fill='#2f4947');d.text((48,765),'직접 그린 설계도 · D-259 후보 · 도현 승인 필요 · 게임 미투입',font=font(21),fill='#69796c');im.save(V/'concept'/f'{kind}_concept.png')
print('packing textures and 5 concepts prepared')
