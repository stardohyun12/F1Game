"""Review-only reference montage. Never export the reference/composite as an asset."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
R=Path(__file__).resolve().parent;repo=R.parents[2];model=repo/'art/models/cargo_box_v02'
fontpath=repo/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf';font=ImageFont.truetype(str(fontpath),24);small=ImageFont.truetype(str(fontpath),20)
ref=Image.open(repo/'art/concept/lofi-styles-2026-10-13/02_warm_street.png').convert('RGB')
raw=Image.open(R/'box-scene-angle.png').convert('RGBA');cut=raw.crop(raw.getchannel('A').getbbox())
# Size-match to the orange parcel envelope; this is NOT a metric game-camera claim.
cut=cut.resize((126,114),Image.Resampling.NEAREST)
rgb=cut.convert('RGB');haze=Image.new('RGB',rgb.size,(163,179,176));rgb=Image.blend(rgb,haze,.025);rgb.putalpha(cut.getchannel('A'));cut=rgb
montage=ref.copy();montage.paste(cut,(776,625),cut)
# Foreground rear rail remains in front of the parcel. Explicit montage mask only.
mask=Image.new('L',ref.size,0);md=ImageDraw.Draw(mask)
md.polygon([(727,709),(944,709),(944,718),(727,718)],fill=255)
md.polygon([(780,718),(787,718),(871,773),(865,777)],fill=255)
md.polygon([(780,772),(785,777),(876,720),(870,717)],fill=255)
montage.paste(ref,(0,0),mask);montage.save(R/'scene-montage.png')
crop=(690,580,980,830)
ref.crop(crop).resize((870,750),Image.Resampling.NEAREST).save(R/'scene-before-3x.png')
montage.crop(crop).resize((870,750),Image.Resampling.NEAREST).save(R/'scene-after-3x.png')
out=Image.new('RGB',(3344,1680),'#ddd6c6');d=ImageDraw.Draw(out)
d.text((24,16),'기준 원화 / 원본 1×',font=font,fill='#30362f')
d.text((1696,16),'v02 / 원화 화물 자리 크기 합성 · 게임 캡처 아님',font=font,fill='#30362f')
out.paste(ref,(0,64));out.paste(montage,(1672,64))
d.text((24,1024),'실물 외측 400×300×300mm / 168 tris / 베벨 1단·덮개 틈·돌출 테이프·국소 눌림',font=small,fill='#30362f')
def onpaper(path,size):
 im=Image.open(path).convert('RGBA');im.thumbnail(size);canvas=Image.new('RGBA',im.size,'#ddd6c6');canvas.alpha_composite(im);return canvas.convert('RGB')
out.paste(onpaper(R/'box-close.png',(800,540)),(20,1090))
out.paste(onpaper(R/'silhouette.png',(500,375)),(830,1120))
out.paste(Image.open(model/'cargo_box_atlas_64.png').resize((384,384),Image.Resampling.NEAREST),(1370,1120))
out.paste(montage.crop(crop).resize((580,500),Image.Resampling.NEAREST),(1810,1090))
d.text((2430,1120),'수치 검사: 32px/m ±10% 통과\n64px / 팔레트 밖 0 / 168 tris\n앞: 작은 송장·바코드·주소선\n오른쪽: ↑↑ / 왼쪽: 잔 기호\n원화 색감에 맞춘 단일 태양광\n주변광과 동일색 해무 2.5%\n합성 위치·크기는 미술 검토용\n도현 승인 필요',font=small,fill='#30362f',spacing=16)
d.text((24,1615),'근접 렌더',font=small,fill='#30362f');d.text((830,1510),'흑실루엣',font=small,fill='#30362f');d.text((1370,1510),'아틀라스 6×',font=small,fill='#30362f');d.text((1810,1615),'장면 크롭 2×',font=small,fill='#30362f')
out.save(R/'compare.png')
sheet=Image.new('RGB',(1500,900),'#ddd6c6');sd=ImageDraw.Draw(sheet)
for i,(path,title) in enumerate([('box-front.png','앞 / 송장'),('box-right.png','옆 / 방향'),('box-top.png','위 / 플랩·테이프')]):
 x=i*500;sheet.paste(onpaper(R/path,(500,380)),(x,50));sd.text((x+20,10),title,font=small,fill='#30362f')
for i,name in enumerate(['front','right','left','top','back','bottom']):
 im=Image.open(model/f'patches/{name}_marked.png');x=15+i*250;sheet.paste(im.resize((im.width*16,im.height*16),Image.Resampling.NEAREST),(x,550));sd.text((x,505),name+' / 16×',font=small,fill='#30362f')
sd.text((20,810),'‘조심’은 32px/m 소형 송장에 들어가지 않음. 고밀도 글꼴 시안은 별도 보관·모델 미적용.',font=small,fill='#30362f')
sheet.save(R/'faces.png')
(R/'montage-proof.json').write_text(json.dumps({'reference':'art/concept/lofi-styles-2026-10-13/02_warm_street.png','overlay_xywh':[776,625,126,114],'purpose':'orange parcel envelope match for visual review only','haze_rgb':[163,179,176],'haze_mix':.025,'foreground_rail_mask':'explicit polygons in build_review.py','runtime_verified':False,'reference_used_in_asset_texture':False},indent=2),encoding='utf-8')
