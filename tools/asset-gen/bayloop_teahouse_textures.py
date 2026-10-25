"""Reuse audited CC0 photo atlas, add only OFL typeset information layer."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw,ImageFont
import numpy as np
R=Path(__file__).resolve().parents[2];O=R/'art/models/bayloop_teahouse_v01';O.mkdir(parents=True,exist_ok=True)
src=R/'art/models/bayloop_italy_v01/Italy_cream_Atlas.png'
im=Image.open(src).convert('RGB')
from photo_to_pixel import load_palette
pal=load_palette(R/'art/source/palette/pitcrew_d123_32.json')
# Preserve photograph structure, change only two close-value wood dye ladders.
base=np.asarray(Image.open(R/'art/models/bayloop_buildings_v01/BayLoopPhotoAtlas.png').convert('RGB'))
for idx,ladder in [(1,[22,23]),(2,[21,24]),(8,[26,27])]:
 x=idx%4*256;y=idx//4*256;a=base[y:y+256,x:x+256];lum=a@np.array([.2126,.7152,.0722]);cuts=np.linspace(lum.min(),lum.max()+.001,len(ladder)+1)[1:-1];levels=np.digitize(lum,cuts)
 im.paste(Image.fromarray(np.asarray(pal,dtype=np.uint8)[np.asarray(ladder)[levels]]),(x,y))
tile=im.crop((258,2,510,254))
# Wood-photo substrate retained in the sign cell. No generated surface noise.
im.paste(tile,(258,770));d=ImageDraw.Draw(im);font=ImageFont.truetype(str(R/'art/_review/2026-10-23-course-ui/fonts/Galmuri11.ttf'),11)
ink=Image.open(src).convert('RGB').getpixel((769,769))
# Explicit palette dark brown, sourced from shared palette.
ink=tuple(min(pal,key=lambda c:sum(c)))
mask=Image.new('1',(28,16));md=ImageDraw.Draw(mask);md.text((3,0),'찻집',font=font,fill=1)
im.paste(ink,(258,770,286,786),mask)
im.save(O/'TeaHousePhotoAtlas.png')
meta=json.loads((R/'art/models/bayloop_buildings_v01/texture-manifest.json').read_text(encoding='utf8'))
meta.update(parent_atlas=str(src.relative_to(R)).replace('\\','/'),parent_sha256=hashlib.sha256(src.read_bytes()).hexdigest(),information_layer='찻집: Galmuri11 OFL1.1, binary mask, 32px/m, sign cell wood photo substrate',generator='tools/asset-gen/bayloop_teahouse_textures.py')
(O/'texture-manifest.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf8')
print(O/'TeaHousePhotoAtlas.png')
