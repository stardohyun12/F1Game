from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,importlib.util,numpy as np
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-27-scene-a-v4-baked'
spec=importlib.util.spec_from_file_location('audit',R/'docs/research/audit_compare.py');audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
for n in ['game','intro','game_raw_bake']:
 im=Image.open(V/(n+'_720.png'));assert im.size==(1280,720);im.resize((1920,1080),Image.Resampling.NEAREST).save(V/(n+'_1080.png'))
items=[('V4 baked24',str(V/'game_1080.png')),('V4 bake before quantization',str(V/'game_raw_bake_1080.png')),('V5 baseline','art/_review/2026-10-27-garage-kart-opus-v5/x1_game.png'),('01 A Short Hike','art/reference/dotlp-2026-10-27/a-short-hike_1.jpg'),('06 EDC','art/reference/dotlp-2026-10-27/easy-delivery-co_1.jpg'),('12 Crow Country','art/reference/dotlp-2026-10-27/crow-country_1.jpg')];rows=[]
for n,p in items:
 m,vis=audit.measure(n,p);rows.append(m)
 if n=='V4 baked24':audit.heat(vis['lstd'],20).save(V/'local-std-heat.png')
(V/'metrics.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf8');print([(x['name'],round(x['face_uniform_frac']*100,2),round(x['face_std_median'],3)) for x in rows])
f=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22);sheet=Image.new('RGB',(1920,1620),'#d7c8a7');d=ImageDraw.Draw(sheet)
for i,(label,file) in enumerate([('01','a-short-hike_1.jpg'),('06','easy-delivery-co_1.jpg'),('12','crow-country_1.jpg')]):
 im=Image.open(R/'art/reference/dotlp-2026-10-27'/file);sheet.paste(im.resize((640,360),Image.Resampling.NEAREST),(i*640,36));d.text((i*640+12,5),label,font=f,fill='#304e52')
for i,name in enumerate(['game','intro']):sheet.paste(Image.open(V/(name+'_1080.png')).resize((960,540),Image.Resampling.NEAREST),(i*960,440));d.text((i*960+10,406),name+' / unlit baked24',font=f,fill='#304e52')
sheet.paste(Image.open(V/'game_raw_bake_1080.png').resize((960,540),Image.Resampling.NEAREST),(0,1040));sheet.paste(Image.open(V/'game_1080.png').resize((960,540),Image.Resampling.NEAREST),(960,1040));d.text((10,1004),'구운 원본 / 팔레트 축소 전',font=f,fill='#304e52');d.text((970,1004),'공용24색 / 디더 없음',font=f,fill='#304e52');sheet.save(V/'sheet.png')
(V/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>V4 baked lighting</title><style>body{background:#d7c8a7;font:18px sans-serif}img{max-width:100%}</style><h1>Scene A V4 · Cycles bake → 24색 → Unlit</h1><p>도현 판정 필요. 시트는 탐색용 축소. 정확한 픽셀 판정은1080 원본에서.</p><a href="game_1080.png">게임 카메라1080</a> · <a href="intro_1080.png">소개1080</a> · <a href="metrics.json">동일 감사 코드 측정</a><img src="sheet.png">',encoding='utf8')
