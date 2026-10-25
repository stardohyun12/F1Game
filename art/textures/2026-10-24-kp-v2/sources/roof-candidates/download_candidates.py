from pathlib import Path
import urllib.request,json,hashlib,sys,importlib.util
from types import SimpleNamespace
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[5]
OUT=Path(__file__).parent
def get(url):
    return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'PitCrewArtResearch/1.0 (+Powered by Poly Haven)'}),timeout=90).read()
spec=importlib.util.spec_from_file_location('pixel',ROOT/'tools/asset-gen/photo_to_pixel.py'); pixel=importlib.util.module_from_spec(spec);spec.loader.exec_module(pixel)
rows=[]
for asset in ['ceramic_roof_01','clay_roof_tiles_02','grey_roof_tiles']:
    directory=OUT/asset;directory.mkdir(parents=True,exist_ok=True)
    for endpoint in ['info','files']:
        p=directory/(endpoint+'.json')
        if not p.exists(): p.write_bytes(get(f'https://api.polyhaven.com/{endpoint}/{asset}'))
    info=json.loads((directory/'info.json').read_text());files=json.loads((directory/'files.json').read_text())
    diffuse_key=next(k for k in files if k.lower() in ('diff','diffuse'))
    entry=files[diffuse_key]['1k']['jpg'];p=directory/(asset+'_diff_1k.jpg')
    if not p.exists():p.write_bytes(get(entry['url']))
    md5=hashlib.md5(p.read_bytes()).hexdigest();assert md5==entry['md5']
    im=Image.open(p).convert('RGB');dims=info['dimensions'];cw=round(im.width*1000/dims[0]);ch=round(im.height*1000/dims[1]);x=(im.width-cw)//2;y=(im.height-ch)//2;crop=[x,y,x+cw,y+ch]
    im.resize((256,256),Image.Resampling.BOX).save(directory/'source_256.png')
    raw=im.crop(crop).resize((64,64),Image.Resampling.BOX);raw.save(directory/'crop_raw_64.png')
    args=SimpleNamespace(input=p,output=directory/'palette_64.png',palette=ROOT/'art/source/palette/pitcrew_d123_32.json',width_m=1,height_m=1,density=64,dither='none',resample='box',source_id=asset,source_url=f'https://polyhaven.com/a/{asset}',license_url='https://polyhaven.com/license',crop=crop,canvas_size=None,metadata=None)
    pixel.convert(args)
    rows.append({'id':asset,'path':p.relative_to(ROOT).as_posix(),'url':f'https://polyhaven.com/a/{asset}','download_url':entry['url'],'license':'CC0-1.0','license_url':'https://polyhaven.com/license','sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'provider_md5':entry['md5'],'md5_verified':True,'authors':info['authors'],'dimensions_mm':dims,'method':'photographic source; exact per-asset capture method not specified','method_evidence':'https://docs.polyhaven.com/en/faq','method_evidence_detail':'Official FAQ states all texture materials use photographic methods, not procedural or generative AI. This is provider-wide evidence, not a per-asset photogrammetry claim.','crop_ltrb_px':crop,'physical_size_m':[1,1],'density_px_m':64,'palette_png':(directory/'palette_64.png').relative_to(ROOT).as_posix(),'ai_generated':'no','process':['official diffuse JPG direct download','provider MD5 verified','physical 1m centre crop','BOX downsample to 64x64','photo_to_pixel.py exact shared-palette nearest RGB; no dither; no color correction']})
board=Image.new('RGB',(1120,1050),'#ded7c8');d=ImageDraw.Draw(board);font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20);small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
d.text((20,12),'ROOF SOURCES | 1m crop / 64px per metre / shared palette / NO recolour',font=font,fill='black')
for i,row in enumerate(rows):
    y=75+i*320;directory=OUT/row['id'];d.text((20,y-26),f"{row['id']} | original span {row['dimensions_mm'][0]/1000:.2f}m",font=font,fill='black')
    for j,(name,label) in enumerate([('source_256.png','Whole source (context)'),('crop_raw_64.png','1m source crop / 4x nearest'),('palette_64.png','1m exact palette / 4x nearest')]):
        img=Image.open(directory/name).resize((256,256),Image.Resampling.NEAREST);x=20+j*370;board.paste(img,(x,y));d.text((x,y+262),label,font=small,fill='black')
board.save(OUT/'roof-candidates-compare.png')
(ROOT/'art/textures/2026-10-24-kp-v2/source_roof_candidates.json').write_text(json.dumps({'provider':'Poly Haven','api_credit':'Powered by Poly Haven','candidates':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(rows,indent=2))
