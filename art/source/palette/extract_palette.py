"""Extract 21 representative colors and 11 role-guided source pixels, no AI texture."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw,ImageFont
import numpy as np
R=Path(__file__).resolve().parent;repo=R.parents[2]
source=repo/'art/concept/lofi-styles-2026-10-13/02_warm_street.png'
im=Image.open(source).convert('RGB');pixels=np.asarray(im).reshape(-1,3).astype(np.int32)
q=im.resize((418,235),Image.Resampling.NEAREST).quantize(colors=21,method=Image.Quantize.MEDIANCUT)
means=[q.getpalette()[i*3:i*3+3] for _,i in sorted(q.getcolors(),reverse=True)]
targets=[('carton_dark',[111,81,46]),('carton_mid',[150,116,72]),('carton_light',[184,146,96]),('tape',[123,86,47]),('label',[226,217,190]),('ink',[48,54,47]),('wine',[59,75,45]),('stamp',[139,70,51])]
targets += [('paper_fold',[137,112,68]),('paper_fibre',[146,119,75]),('paper_flat',[160,132,85])]
colors=[];entries=[]
for name,target in [(f'scene_{i:02}',c) for i,c in enumerate(means)]+targets:
 dist=np.sum((pixels-np.array(target))**2,axis=1);order=np.argsort(dist)
 ix=next(int(j) for j in order if tuple(pixels[j]) not in colors)
 rgb=tuple(int(v) for v in pixels[ix]);colors.append(rgb)
 entries.append({'name':name,'rgb':list(rgb),'hex':'#'+''.join(f'{v:02X}' for v in rgb),'source_xy':[ix%im.width,ix//im.width]})
data={'name':'PitCrew D123 shared 32','colors':[list(c) for c in colors],'entries':entries,'reference':str(source.relative_to(repo)),'reference_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'method':'21 median-cut targets + 11 material-role targets; each snapped to unique actual source pixel','use':'Reference colors only; generated reference image is not a game texture.'}
(R/'pitcrew_d123_32.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
(R/'pitcrew_d123_32.gpl').write_text('GIMP Palette\nName: PitCrew D123 Shared 32\nColumns: 8\n# All swatches sampled from approved reference; see JSON coordinates.\n'+'\n'.join(f'{c[0]:3} {c[1]:3} {c[2]:3}\t{e["name"]}' for c,e in zip(colors,entries))+'\n',encoding='utf-8')
sw=Image.new('RGB',(8,4));sw.putdata(colors);sw.resize((512,256),Image.Resampling.NEAREST).save(R/'pitcrew_d123_32.png')
sheet=Image.new('RGB',(1024,620),(220,216,200));d=ImageDraw.Draw(sheet)
font=ImageFont.truetype(str(repo/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf'),12)
for i,e in enumerate(entries):
 x=(i%8)*128;y=(i//8)*150;d.rectangle((x+8,y+8,x+119,y+96),fill=tuple(e['rgb']));d.text((x+8,y+102),e['name'],font=font,fill=(40,45,40));d.text((x+8,y+120),e['hex'],font=font,fill=(40,45,40))
sheet.save(R/'palette-labelled.png');print('32 source-sampled colors saved')
