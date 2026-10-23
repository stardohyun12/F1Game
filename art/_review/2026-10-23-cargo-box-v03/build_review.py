from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
import numpy as np,json
R=Path(__file__).resolve().parent;repo=R.parents[2];dest=repo/'art/_review/2026-10-23-cargo-box'
fontfile=repo/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf';f=ImageFont.truetype(str(fontfile),24);small=ImageFont.truetype(str(fontfile),18)
reference=Image.open(repo/'art/concept/lofi-styles-2026-10-13/02_warm_street.png').convert('RGB')
ref480=reference.resize((480,270),Image.Resampling.BOX);ref480.save(R/'reference-480x270.png')
reference.crop((690,580,980,830)).resize((580,500),Image.Resampling.NEAREST).save(R/'reference-crop-2x.png')
montages={};low={}
for D in [32,64]:
 dr=R/f'density{D}';raw=Image.open(dr/'box-scene-angle.png').convert('RGBA');cut=raw.crop(raw.getchannel('A').getbbox()).resize((126,114),Image.Resampling.NEAREST)
 rgb=Image.blend(cut.convert('RGB'),Image.new('RGB',cut.size,(163,179,176)),.025);rgb.putalpha(cut.getchannel('A'));cut=rgb
 scene=reference.convert('RGBA')
 shadow=Image.new('RGBA',scene.size,(0,0,0,0));sd=ImageDraw.Draw(shadow);sd.ellipse((772,731,911,745),fill=(40,31,21,75));shadow=shadow.filter(ImageFilter.GaussianBlur(2));scene=Image.alpha_composite(scene,shadow)
 scene.alpha_composite(cut,(776,625))
 # Restore only actual low-chroma foreground metal pixels, not orange cargo pixels.
 shapes=Image.new('L',scene.size);md=ImageDraw.Draw(shapes)
 md.polygon([(727,709),(944,709),(944,718),(727,718)],fill=255)
 md.polygon([(780,718),(787,718),(871,773),(865,777)],fill=255)
 md.polygon([(780,772),(785,777),(876,720),(870,717)],fill=255)
 a=np.asarray(reference).astype(float);chroma=(a.max(2)-a.min(2))/np.maximum(a.max(2),1)
 mask=Image.fromarray(np.where((np.asarray(shapes)>0)&(chroma<.25)&(a.mean(2)>60),255,0).astype('uint8'))
 scene.paste(reference,(0,0),mask);scene=scene.convert('RGB');scene.save(dr/'scene-montage.png');montages[D]=scene
 low[D]=scene.resize((480,270),Image.Resampling.BOX);low[D].save(dr/'scene-480x270.png');low[D].resize((1440,810),Image.Resampling.NEAREST).save(dr/'scene-480x270-3x.png')
 scene.crop((690,580,980,830)).resize((580,500),Image.Resampling.NEAREST).save(dr/'scene-crop-2x.png')
pair=Image.new('RGB',(960,270));pair.paste(low[32],(0,0));pair.paste(low[64],(480,0));pair.save(dest/'density-compare-480x270.png')
sheet=Image.new('RGB',(1920,1550),'#ddd6c6');draw=ImageDraw.Draw(sheet)
def paper(path,box):
 im=Image.open(path).convert('RGBA');im.thumbnail(box);b=Image.new('RGBA',im.size,'#ddd6c6');b.alpha_composite(im);return b.convert('RGB')
for i,D in enumerate([32,64]):
 x=i*960;dr=R/f'density{D}';model=repo/f'art/models/cargo_box_v03/density{D}'
 draw.text((x+20,12),f'{D}px/m / '+('기준 유지' if D==32 else '밀도 예외 · 도현 승인 대기'),font=f,fill='#30362f')
 sheet.paste(low[D].resize((960,540),Image.Resampling.NEAREST),(x,60))
 draw.text((x+20,610),'480×270 축소 후 Point 2× / 동일 위치·광원·크기 / 원화 합성',font=small,fill='#30362f')
 sheet.paste(paper(dr/'box-close.png',(570,428)),(x+5,660))
 sheet.paste(paper(dr/'silhouette.png',(360,270)),(x+590,680))
 draw.text((x+590,965),'흑실루엣 / 동일 모델',font=small,fill='#30362f')
 sheet.paste(Image.open(model/'cargo_box_atlas_64.png').resize((320,320),Image.Resampling.NEAREST),(x+20,1120))
 sheet.paste(Image.open(dr/'scene-crop-2x.png').resize((464,400),Image.Resampling.NEAREST),(x+440,1100))
 draw.text((x+20,1455),'64px 아틀라스 5×',font=small,fill='#30362f')
 draw.text((x+440,1510),'동일 장면 크롭 / 실제 게임 캡처 아님',font=small,fill='#30362f')
sheet.save(dest/'density-compare.png');sheet.save(R/'density-compare.png')
layers=Image.new('RGB',(1560,1000),'#ddd6c6');ld=ImageDraw.Draw(layers)
stages=['00_downscaled','01_quantized','02_ladder_bayer','03_stamps','04_sparse_fill','05_final']
for row,D in enumerate([32,64]):
 for col,stage in enumerate(stages):
  im=Image.open(repo/f'art/models/cargo_box_v03/density{D}/patches/front_{stage}.png');scale=8 if D==64 else 16
  x=col*260+20;y=row*470+70;layers.paste(im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST),(x,y))
  ld.text((x,row*470+20),f'{D} / {stage[:2]}',font=small,fill='#30362f')
  ld.text((x,y+330),stage[3:],font=small,fill='#30362f')
layers.save(R/'texture-stages.png')
(R/'comparison-proof.json').write_text(json.dumps({'same_geometry':True,'reference_scene_overlay_xywh':[776,625,126,114],'downscale':'BOX to exactly480x270; nearest integer2x/3x display','camera':'same Blender orthographic match; NOT Unity runtime camera','haze_rgb':[163,179,176],'haze_mix':.025,'contact_shadow':'identical review-only ellipse; not baked asset texture','foreground_mask':'low-chroma source rail pixels within explicit polygons','64_density_approved':False},indent=2),encoding='utf-8')
