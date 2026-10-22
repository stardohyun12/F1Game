"""Reference layout + explicitly requested resizing only. No art/model generation."""
from PIL import Image, ImageOps, ImageDraw, ImageFont
from pathlib import Path
import urllib.request, json, hashlib
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
refs=[
 ('amalfi','Amalfi / piazza', 'https://hostel-positano.com/en/how-to-get-here/amalfi-positano-bus','https://hostel-positano.com/images/guides/amalfiPositanoBus/amalfi-piazza-duomo.jpg'),
 ('positano','Positano / church','https://comune.positano.sa.it/vivere-il-comune/luogo/edificio-di-culto/chiesa-di-santa-maria-assunta','https://comune.positano.sa.it/sites/default/files/2024-03/chiesa%20madre.jpg'),
 ('edc','Easy Delivery Co. / Steam','https://store.steampowered.com/app/3293010/Easy_Delivery_Co/','https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/287ddbe79e43b36d1ab74111609212f8cc7aeb64/ss_287ddbe79e43b36d1ab74111609212f8cc7aeb64.1920x1080.jpg'),
]
sources=[]
for name,title,page,url in refs:
 p=OUT/(name+'_reference.jpg')
 if not p.exists():
  req=urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'})
  with urllib.request.urlopen(req,timeout=25) as r: p.write_bytes(r.read())
 with Image.open(p) as im: im.verify()
 sources.append(dict(file=p.name,title=title,page_url=page,image_url=url,usage='Internal reference quotation only; not game asset; rights remain with source',sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
(OUT/'sources.json').write_text(json.dumps(sources,ensure_ascii=False,indent=2),encoding='utf8')
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',19)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',14)
board=Image.new('RGB',(1600,340),'#202631'); draw=ImageDraw.Draw(board)
items=[(OUT/(n+'_reference.jpg'),t) for n,t,_,_ in refs]+[(ROOT/'tools/town-gen/out/shots/Spline_drive.png','우리 게임 / 현재 실제 캡처')]
for i,(path,label) in enumerate(items):
 im=ImageOps.contain(Image.open(path).convert('RGB'),(390,235))
 board.paste(im,(i*400+(400-im.width)//2,48+(235-im.height)//2))
 draw.text((i*400+12,12),label,font=font,fill='white')
 draw.text((i*400+12,290),['광장 둘레 / 종탑','성당의 중심성','단순 덩어리 / 거리 가독성','현재 재질·도로와 맞출 것'][i],font=small,fill='#cad4dc')
draw.text((12,318),'외부 자료는 참고 인용. 출처 URL: sources.json / 계획 문서. 게임 투입용 이미지 아님.',font=small,fill='#cad4dc')
board.save(OUT/'reference_board.png')
Image.open(ROOT/'tools/town-gen/out/shots/Spline_drive.png').convert('RGB').resize((640,360),Image.Resampling.BOX).save(OUT/'current_640.png')
a=Image.open(ROOT/'art/concept/lofi-styles-2026-10-13/02_warm_street.png').convert('RGB').resize((640,360),Image.Resampling.BOX)
b=Image.open(ROOT/'art/concept/2026-10-20-town-variety/02_town_center_slope_v02.png').convert('RGB').crop((330,0,1236,510)).resize((640,360),Image.Resampling.BOX)
a.save(OUT/'option_A_640.png'); b.save(OUT/'option_B_640.png')
compare=Image.new('RGB',(1312,430),'#202631');d=ImageDraw.Draw(compare)
for x,label,im in [(8,'A  생활형 로파이 — 기존 D-096 이미지 축소',a),(664,'B  장식형 광장 — 미승인 기존 원화 부분 축소',b)]:
 d.text((x,8),label,font=font,fill='white');compare.paste(im,(x,40))
d.text((8,407),'각 화면 640×360 원본 표시. 신규 생성/실제 엔진 렌더 아님. 서로 다른 장면이므로 동조건 A/B 아님.',font=small,fill='#cad4dc')
compare.save(OUT/'options_640_board.png')
checks={n:Image.open(OUT/n).size for n in ['option_A_640.png','option_B_640.png','current_640.png','reference_board.png','options_640_board.png']}
assert all(checks[n]==(640,360) for n in ['option_A_640.png','option_B_640.png','current_640.png'])
(OUT/'validation.json').write_text(json.dumps({'dimensions':checks,'new_generated_art':0,'models_created':0,'errors':0},indent=2),encoding='utf8')
print(json.dumps(checks))

