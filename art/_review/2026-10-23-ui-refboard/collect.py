"""공식 Steam 등록 이미지의 조사용 복사본/출처 장부. 게임 자산으로 사용 금지."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import requests, json, hashlib
from PIL import Image, ImageDraw, ImageFont
from io import BytesIO

ROOT = Path(__file__).resolve().parent
GAMES = [('edc',3293010),('short-hike',1055540),('lunacid',1745510),('dredge',1562430),('motorways',1127500),('unpacking',1135690),('papers',239030),('obra-dinn',653530),('inscryption',1092790),('paradise',1160220),('crazy-taxi',71230),('pacific-drive',1458140)]

def collect(pair):
    slug, appid = pair
    endpoint = f'https://store.steampowered.com/api/appdetails?appids={appid}&l=english'
    res = requests.get(endpoint,timeout=40); res.raise_for_status()
    raw = res.json()[str(appid)]
    if not raw.get('success'): return {'slug':slug,'error':'Steam appdetails unavailable','appid':appid}
    d = raw['data']; out = {'slug':slug,'appid':appid,'name':d['name'],'source':f'https://store.steampowered.com/app/{appid}/','metadata_url':endpoint,'developers':d.get('developers',[]),'publishers':d.get('publishers',[]),'retrieved':'2026-10-23','shots':[],'movies':d.get('movies',[])}
    for shot in d.get('screenshots',[])[:6]:
        sid = shot['id']; url=shot['path_full']; local=f'images/{slug}-{sid}.jpg'
        data=requests.get(url,timeout=40); data.raise_for_status()
        (ROOT/local).write_bytes(data.content)
        im=Image.open(BytesIO(data.content))
        out['shots'].append({'id':sid,'url':url,'local':local,'width':im.width,'height':im.height,'sha256':hashlib.sha256(data.content).hexdigest()})
    return out

if __name__=='__main__':
    (ROOT/'images').mkdir(exist_ok=True)
    with ThreadPoolExecutor(max_workers=6) as ex: data=list(ex.map(collect,GAMES))
    (ROOT/'sources.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
    for offset in range(0,len(data),3):
        sheet=Image.new('RGB',(1440,900),'#202525'); draw=ImageDraw.Draw(sheet)
        for row,g in enumerate(data[offset:offset+3]):
            for col,s in enumerate(g.get('shots',[])):
                x=(col%3)*480; y=row*300+(col//3)*150
                im=Image.open(ROOT/s['local']); im.thumbnail((470,122))
                sheet.paste(im,(x,y+25)); draw.text((x+6,y+3),f"{g['slug']} / {s['id']}",font=font,fill='white')
        sheet.save(ROOT/f'contact-{offset//3+1}.jpg',quality=92)
    print(json.dumps([{'name':d.get('name',d['slug']),'shots':len(d.get('shots',[])),'error':d.get('error')} for d in data]))
