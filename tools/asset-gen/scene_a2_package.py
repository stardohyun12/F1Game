from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,csv,hashlib,re
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a2_kit';E=R/'art/export/scene-a2-kit';V=R/'art/_review/2026-10-27-scene-a2-kit';meta=json.loads((O/'manifest.json').read_text(encoding='utf-8'));order={m['name']:i for i,m in enumerate(json.loads((O/'selection.json').read_text(encoding='utf-8')))};meta=sorted(meta,key=lambda m:order[m['name']])
font=lambda s:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',s)
kit=[]
for m in meta:
 page=(R/'art/thirdparty'/m['pack']/'SOURCE.md').read_text(encoding='utf-8');urls=re.findall(r'https?://[^\s)]+',page);url=urls[0] if urls else '';lic='CC0-1.0';credit=m['pack']+' — CC0 (voluntary credit)'
 if m['name']=='pole_01':lic='CC-BY-3.0';credit='"Telephone pole" by Poly by Google, CC-BY 3.0, via Poly Pizza';url='https://poly.pizza/m/4Hazwkymnyg'
 if m['name']=='bicycle_01':lic='CC-BY-3.0';credit='"Bicycle" by Poly by Google, CC-BY 3.0, via Poly Pizza';url='https://poly.pizza/m/19VoUuA2pcN'
 if m['name'].startswith('tires'):credit='Quaternius, CC0, via Poly Pizza'
 kit.append(dict(name=m['name'],original_pack=m['pack'],original_file=m['source'],source_url=url,license=lic,credit=credit,modifications='uniform scale; tilt where specified; roof/wall tint; diffuse+AO baked; V5 palette32; original geometry retained',size_m_xyz=m['size'],axes='Blender X width/Y depth/Z height; FBX Y-up',placement=m['zone'],pivot='bottom bounding-box centre',triangles=m['tris'],texture=m['name']+'.png',model=m['name']+'.fbx',texels_per_m=32,scale_applied=m['scale_applied'],lean_degrees=m['lean'],physical_count=3 if m['name']=='pole_01' else 1,source_sha256=m['source_sha256']))
(E/'kit.json').write_text(json.dumps(dict(version=1,palette='palette32.gpl',unlit=True,unit='metre',assets=kit),ensure_ascii=False,indent=2),encoding='utf-8')
credits='# Scene A2 attribution\n\nSelected CC-BY models: Telephone pole and Bicycle. Modified scale, tilt, baked lighting and colours. No endorsement implied.\n\nCC-BY 3.0: https://creativecommons.org/licenses/by/3.0/\nCC0: https://creativecommons.org/publicdomain/zero/1.0/\n\nRequired five collected credits retained (three unused in this kit):\n'
for title,author,ident,used in [('Bicycle','Poly by Google','19VoUuA2pcN',True),('Telephone pole','Poly by Google','4Hazwkymnyg',True),('Tire','Poly by Google','cV6yx0v8Bfy',False),('old bike','Aeron Quick','bgT_0ItQRxd',False),('Vending Machine','dook','yNG4vqWVq0',False)]:credits+=f'- "{title}" by {author} — CC-BY 3.0, via Poly Pizza https://poly.pizza/m/{ident} ({"included" if used else "not included"})\n'
for pack in sorted({m['pack'] for m in meta}):credits+='\n## '+pack+'\n'+(R/'art/thirdparty'/pack/'SOURCE.md').read_text(encoding='utf-8')+'\n'+(R/'art/thirdparty'/pack/'LICENSE.txt').read_text(encoding='utf-8')+'\n'
(E/'CREDITS.md').write_text(credits,encoding='utf-8')
cards=[]
for m in meta:
 im=Image.open(V/(m['name']+'.png')).convert('RGBA');w=max(im.width+58,245);h=max(im.height,45)+76;card=Image.new('RGB',(w,h),(216,216,199));card.paste(im,(8,8),im);d=ImageDraw.Draw(card);d.rectangle((w-30,im.height+8-40,w-23,im.height+8),fill=(167,65,42));d.text((w-52,im.height+13),'1.7m',font=font(11),fill='#543c30');d.text((8,h-48),m['name'],font=font(16),fill='#22312e');d.text((8,h-26),'×'.join(f'{x:.2f}' for x in m['size'])+' m / '+str(m['tris'])+' tris',font=font(12),fill='#374440');cards.append(card)
width=max(1920,max(im.width for im in cards)+40);rows=[];current=[];rw=0;rh=0
for c in cards:
 if current and rw+c.width+16>width-40:rows.append((current,rh));current=[];rw=0;rh=0
 current.append(c);rw+=c.width+16;rh=max(rh,c.height)
if current:rows.append((current,rh))
canvas=Image.new('RGB',(width,110+sum(h+20 for _,h in rows)),(238,232,211));d=ImageDraw.Draw(canvas);d.text((20,15),'Scene A2 — 사람 제작 원형 / V5 구운 빛 · 32색',font=font(27),fill='#263b36');d.text((20,58),'전 항목 24 px/m 같은 축척 · 빨간 막대 1.7m · 참고용 Blender Unlit / 최종 판정 Unity',font=font(18),fill='#45534a');y=100
for cs,h in rows:
 x=20
 for c in cs:canvas.paste(c,(x,y+h-c.height));x+=c.width+16
 y+=h+20
canvas.save(V/'sheet.png');(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Scene A2 kit</title><style>body{background:#eee8d3;font:18px sans-serif}img{max-width:100%;image-rendering:pixelated}</style><h1>Scene A2 kit</h1><p>같은 축척 24px/m · 사람 막대1.7m · 최종 판정 Unity</p><a href="sheet.png"><img src="sheet.png"></a>',encoding='utf-8')
ledger=R/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:reader=csv.DictReader(f);fields=reader.fieldnames;rows=[r for r in reader if not r['asset_id'].startswith('scene_a2_kit_')]
for k in kit:
 for ext in ['fbx','png']:
  p=E/(k['name']+'.'+ext);rows.append(dict(asset_id='scene_a2_kit_'+k['name']+'_'+ext,path=p.relative_to(R).as_posix(),source_url=k['source_url'],license=k['license'],accessed='2026-10-27',tool='scene_a2_bake.py; scene_a2_quantize.py; scene_a2_export.py',ai_generated='no',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),notes=k['credit']+'; '+k['modifications']+';32texels/m; Unity not run'))
with ledger.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print('kit',len(kit),'sheet',canvas.size)
