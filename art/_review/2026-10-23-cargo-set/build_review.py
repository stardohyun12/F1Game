"""Review montage/sheets from the five exported-GLB renders, D124 960x540."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import json,numpy as np
R=Path(__file__).resolve().parent;repo=R.parents[2]
fontfile=repo/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf';f=ImageFont.truetype(str(fontfile),24);small=ImageFont.truetype(str(fontfile),18)
names={'box':'택배 상자','cake':'창 있는 케이크 상자','eggs':'30구 계란 포장','wine':'750ml 와인 · M','fish':'생선 · EPS · 얼음'}
tri={'box':144,'cake':290,'eggs':1032,'wine':188,'fish':410}
ref=Image.open(repo/'art/concept/lofi-styles-2026-10-13/02_warm_street.png').convert('RGB');ref960=ref.resize((960,540),Image.Resampling.BOX);ref960.save(R/'reference-960x540.png')
def rgba(path):return Image.open(path).convert('RGBA')
def paper(path,size):
 im=rgba(path);im.thumbnail(size);out=Image.new('RGBA',size,'#ddd6c6');out.alpha_composite(im,((size[0]-im.width)//2,(size[1]-im.height)//2));return out.convert('RGB')
sheet=Image.new('RGB',(2400,1350),'#ddd6c6');sd=ImageDraw.Draw(sheet)
placement={}
for i,(kind,title) in enumerate(names.items()):
 dr=R/kind;proof=json.loads((dr/'render-proof.json').read_text());dims=proof['dimensions_blender_xyz'];raw=rgba(dr/'scene-angle.png');cut=raw.crop(raw.getchannel('A').getbbox())
 width=max(1,round(126*dims[0]/.4));height=round(cut.height*width/cut.width);cut=cut.resize((width,height),Image.Resampling.NEAREST)
 rgb=Image.blend(cut.convert('RGB'),Image.new('RGB',cut.size,(163,179,176)),.025);rgb.putalpha(cut.getchannel('A'));cut=rgb
 x=470-width//2;y=850-height;scene=ref.convert('RGBA')
 shadow=Image.new('RGBA',ref.size);ImageDraw.Draw(shadow).ellipse((x-4,846,x+width+4,854),fill=(35,30,25,70));scene=Image.alpha_composite(scene,shadow.filter(ImageFilter.GaussianBlur(2)));scene.alpha_composite(cut,(x,y))
 # Place on the clear foreground road, never fake removal/reconstruction of the source car.
 scene=scene.convert('RGB');scene.save(dr/'scene-montage.png');low=scene.resize((960,540),Image.Resampling.BOX);low.save(dr/'scene-960x540.png')
 placement[kind]={'overlay_xywh':[x,y,width,height],'physical_width_m':dims[0],'source_cargo_removal':False,'note':'same clear foreground road placement; source car stays reference-only; not installed cargo'}
 # Six packing-inspection directions, same extent/camera zoom per asset.
 six=Image.new('RGB',(1920,1080),'#ddd6c6');d=ImageDraw.Draw(six);d.text((20,8),title+' / 포장 확인 6면 / 실제 GLB',font=f,fill='#30362f')
 for n,(view,label) in enumerate([('front','앞·송장'),('right','오른쪽·방향'),('back','뒤'),('left','왼쪽'),('top','위'),('bottom','바닥')]):
  xx=(n%3)*640;yy=60+(n//3)*500;six.paste(paper(dr/f'{view}.png',(640,480)),(xx,yy));d.text((xx+18,yy+5),label,font=small,fill='#30362f')
 six.save(dr/'six-faces.png')
 xx=i*480;sd.text((xx+16,18),title,font=f,fill='#30362f');sd.text((xx+16,52),f'64px/m · {tri[kind]} tris',font=small,fill='#30362f')
 sheet.paste(low.resize((480,270),Image.Resampling.NEAREST),(xx,100));sheet.paste(paper(dr/'close.png',(480,270)),(xx,420))
 sheet.paste(six.resize((480,270),Image.Resampling.NEAREST),(xx,760))
 sd.text((xx+16,1090),'원화 합성 / 근접 / 6면',font=small,fill='#30362f')
 if kind=='fish':sd.text((xx+16,1130),'열린 포장 확인 상태\n배송용 덮개 미포함',font=small,fill='#30362f',spacing=10)
 if kind=='wine':sd.text((xx+16,1130),'가상 상표 M / 실제 Galmuri\n좁은 면은 보조 창 검사',font=small,fill='#30362f',spacing=10)
 if kind=='eggs':sd.text((xx+16,1130),'30구 몰드 뚜껑\n닫힌 높이는 조립 가정',font=small,fill='#30362f',spacing=10)
sd.text((20,1290),'D-124 / 960×540 기준 · 원화 합성은 미술 검토용, Unity 캡처 아님 · 도현 미리보기 승인 필요',font=f,fill='#30362f')
sheet.save(R/'compare.png');sheet.resize((960,540),Image.Resampling.BOX).save(R/'compare-960x540.png')
# All five in a single reference-backed physical-scale lineup, not a gameplay placement.
line=rgba(R/'lineup-render.png');line=line.crop(line.getchannel('A').getbbox());line.thumbnail((880,270),Image.Resampling.NEAREST)
stage=ref960.convert('RGBA');stage.alpha_composite(line,((960-line.width)//2,510-line.height));d=ImageDraw.Draw(stage);d.rectangle((0,0,960,36),fill=(221,214,198,255));d.text((14,7),'D-124 5종 · 같은 실물 비례 · 원화 배경 검토 합성',font=small,fill='#30362f');stage.convert('RGB').save(R/'lineup-scene-960x540.png')
# A larger reference-backed source and a literal 960x540 reduction for review.
large=ref.convert('RGBA');scale=ref.width/960;bigline=line.resize((round(line.width*scale),round(line.height*scale)),Image.Resampling.NEAREST);large.alpha_composite(bigline,((ref.width-bigline.width)//2,round(510*scale)-bigline.height));large.convert('RGB').save(R/'lineup-scene.png')
large.resize((960,540),Image.Resampling.BOX).convert('RGB').save(R/'lineup-reduced-960x540.png')
sil=Image.new('RGBA',line.size,(0,0,0,0));sil.putalpha(line.getchannel('A'));silboard=Image.new('RGBA',(960,540),'white');silboard.alpha_composite(sil,((960-line.width)//2,400-line.height));silboard.convert('RGB').save(R/'silhouette-960x540.png')
(R/'comparison-proof.json').write_text(json.dumps({'resolution':[960,540],'montage_only':True,'runtime_verified':False,'source_reference':'art/concept/lofi-styles-2026-10-13/02_warm_street.png','placements':placement,'lineup':'all five identical camera, common physical scale, reference background; not installed game assets','filter':'BOX downscale; integer nearest review thumbnails'},indent=2),encoding='utf-8')
