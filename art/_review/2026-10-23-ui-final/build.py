"""D-123 UI final candidate. CC0 photo + deterministic pixel drawing. No AI textures."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps
import numpy as np
import json,hashlib,sys,shutil,csv,html

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
sys.path.insert(0,str(ROOT/'tools/asset-gen'))
from photo_to_pixel import quantize_exact,load_palette
for folder in ['sprites','guides','materials','reference','fonts','validation','mockups']:(OUT/folder).mkdir(exist_ok=True)
PAL=ROOT/'art/source/palette/pitcrew_d123_32.json'
COLORS=load_palette(PAL)
PAPER='#E4D6BF';INK='#30362F';DARK='#231F1B';FIBRE='#C4A687';TEAL='#677B80';STAMP='#8A4631';AMBER='#B89360'
BASE=json.loads((ROOT/'art/_review/2026-10-23-d123/sprites.json').read_text('utf-8'))
PHOTO=ROOT/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png'
CROP=(825,590,1000,705)
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def rel(p):return Path(p).resolve().relative_to(ROOT).as_posix()
for src,dst in [
 ('art/_review/2026-10-23-ui-flow/fonts/Galmuri11.ttf','Galmuri11.ttf'),
 ('art/_review/2026-10-23-ui-flow/fonts/Galmuri11.woff2','Galmuri11.woff2'),
 ('art/_review/2026-10-23-ui-flow/fonts/Galmuri-OFL.txt','Galmuri-OFL.txt'),
 ('F1/Assets/UI/Fonts/Pretendard/Pretendard-Regular.ttf','Pretendard-Regular.ttf'),
 ('F1/Assets/UI/Fonts/Pretendard/OFL.txt','Pretendard-OFL.txt')]:shutil.copyfile(ROOT/src,OUT/'fonts'/dst)
sources=[]
for src,dst,kind in [
 ('docs/qa/evidence/slice-start.png','game-960.png','existing game screenshot; review background only'),
 ('art/concept/lofi-styles-2026-10-13/02_warm_street.png','concept.png','AI concept reference only; never sprite input'),
 ('art/_review/2026-10-23-cargo-set-v2/box/close.png','cargo-existing.png','existing art-team render; no model editing'),
 ('art/_review/2026-10-23-d123/mockup-call.png','old-call.png','old AI concept; comparison only; never sprite input')]:
    shutil.copyfile(ROOT/src,OUT/'reference'/dst)
    sources.append({'original':src,'snapshot':'reference/'+dst,'sha256':sha(ROOT/src),'scope':kind})

photo=Image.open(PHOTO).convert('RGB').crop(CROP)
photo.save(OUT/'materials/photo-crop.png')
small=photo.resize((64,64),Image.Resampling.BOX)
small.save(OUT/'materials/photo-downsample-64.png')
grey=np.asarray(ImageOps.grayscale(small)).astype(float)
lo=float(grey.min());hi=float(np.percentile(grey,70))
f=np.clip((grey-lo)/(hi-lo),0,1)
tone=Image.fromarray((np.array([196,166,135])+(np.array([228,214,191])-np.array([196,166,135]))*f[:,:,None]).astype('uint8'))
tone.save(OUT/'materials/photo-toned-64.png')
paper=quantize_exact(tone,COLORS,'none')
paper.save(OUT/'materials/paper-photo-64.png')
paper.resize((256,256),Image.Resampling.NEAREST).save(OUT/'materials/paper-photo-preview.png')
sources.append({'original':rel(PHOTO),'sha256':sha(PHOTO),'source_url':'https://ambientcg.com/view?id=CardboardSet001','license_url':'https://docs.ambientcg.com/license/','license':'CC0-1.0','technique':'Surface Photogrammetry','crop_ltrb':CROP,'source_is_raw_camera_photo':False,'pipeline':['crop unprinted paper/card fibre','BOX 64x64','real luma remap: min to 70th percentile','photo_to_pixel.quantize_exact, shared 32 palette, dither none'],'tone_luma_range':[lo,hi],'UI_units':'pixels, no physical metre/density claim','photographic_patch':'materials/paper-photo-64.png'})
(OUT/'sources.json').write_text(json.dumps(sources,ensure_ascii=False,indent=2),encoding='utf-8')

def tile(src,size):
    dest=Image.new(src.mode,size)
    for y in range(0,size[1],src.height):
        for x in range(0,size[0],src.width):dest.paste(src.crop((0,0,min(src.width,size[0]-x),min(src.height,size[1]-y))),(x,y))
    return dest

spec=[]
for old in BASE:
    s=dict(old);w,h=s['size'];bw,bh=w//4,h//4
    name=s['slot'];im=Image.new('RGBA',(bw,bh),(0,0,0,0));d=ImageDraw.Draw(im)
    if name in ['paperPanel','paperCard']:
        im=tile(paper.convert('RGBA'),(bw,bh));d=ImageDraw.Draw(im)
        # Border strips must not repeat a photographic edge/fold as a motif.
        # Keep fibre in the centre; handling marks live in fixed corner cells only.
        ll,bb,rr,tt=[v//4 for v in s['border_LBRT']]
        d.rectangle((0,0,bw-1,tt-1),fill=PAPER)
        d.rectangle((0,bh-bb,bw-1,bh-1),fill=PAPER)
        d.rectangle((0,0,ll-1,bh-1),fill=PAPER)
        d.rectangle((bw-rr,0,bw-1,bh-1),fill=PAPER)
        # Real handling wear only at bottom corners. Middle fibre remains photographic.
        d.line([(1,0),(bw-2,0)],fill=FIBRE)
        d.line([(0,1),(0,bh-3)],fill=FIBRE)
        d.line([(bw-1,1),(bw-1,bh-3)],fill=FIBRE)
        d.line([(2,bh-1),(bw-3,bh-1)],fill=FIBRE)
        for x,y in [(0,0),(bw-1,0),(0,bh-1),(1,bh-1),(bw-1,bh-1),(bw-2,bh-1)]:d.point((x,y),fill=(0,0,0,0))
        d.line([(2,bh-3),(5,bh-3)],fill=FIBRE);d.line([(bw-6,bh-3),(bw-3,bh-3)],fill=FIBRE)
    elif name in ['stampRing','stampSprite']:
        # Two ink rings, imperfect pressure along lower-left/right contact arcs.
        d.ellipse((2,2,bw-3,bh-3),outline=STAMP,width=1)
        d.ellipse((5,5,bw-6,bh-6),outline=STAMP,width=1)
        for r in [(4,bh//2,5,bh//2+2),(bw//3,bh-6,bw//3+2,bh-4),(bw-7,bh//3,bw-4,bh//3+1)]:d.rectangle(r,fill=(0,0,0,0))
        d.line([(bw-8,bh//2-4),(bw-7,bh//2+2)],fill=STAMP,width=2)
    elif name=='enamelPlate':
        d.rectangle((0,0,bw-1,bh-1),fill=TEAL)
        d.rectangle((1,1,bw-2,bh-2),fill=INK)
        # Two fasteners and limited corner enamel chips; no full-field noise.
        d.point((2,2),fill=PAPER);d.point((bw-3,bh-3),fill=PAPER)
        d.line([(0,bh-4),(0,bh-2)],fill=FIBRE)
    elif name=='slipHeader':
        d.rectangle((0,0,bw-1,bh-1),fill=PAPER)
        for x in range(2,bw-2,4):d.line([(x,bh-3),(x+1,bh-3)],fill=FIBRE)
    elif name=='buttonPlate':
        d.rectangle((0,0,bw-1,bh-1),fill=PAPER,outline=TEAL,width=1)
        d.line([(1,bh-2),(bw-2,bh-2)],fill=FIBRE)
    elif name=='gaugeTrackSprite':
        d.rectangle((0,0,bw-1,bh-1),fill=PAPER,outline=FIBRE,width=1)
    elif name in ['gaugeFill','gaugeFillSprite']:
        d.rectangle((0,0,bw-1,bh-1),fill=INK)
    else:raise ValueError(name)
    out=im.resize((w,h),Image.Resampling.NEAREST)
    path=OUT/'sprites'/f'{name}.png';out.save(path)
    s.update({'path':rel(path),'author_grid':[bw,bh],'export_cell_px':4,'native_display_cell_px':2,'source_to_logical':0.5,'tint':'Color.white; full-color palette texture','previous_tint':old['tint'],'mode':'Simple' if name.startswith('stamp') else ('Tiled' if name in ['paperPanel','paperCard','slipHeader'] else 'Sliced'),'sha256':sha(path),'ai_generated':False,'source_kind':'photo+script' if name in ['paperPanel','paperCard'] else 'script','wear':'photo fibre + lower handling corners' if name in ['paperPanel','paperCard'] else ('ink pressure gaps on ring only' if name.startswith('stamp') else 'scripted structural marks only')})
    spec.append(s)
manifest={'schema':'pitcrew-ui-sprites-v1','baseline':'art/_review/2026-10-23-d123/sprites.json','palette':rel(PAL),'sprite_root':rel(OUT/'sprites'),'sprites':spec,'release_scope':'sprites/*.png only; no reference/materials/mockups/guides/fonts auto-import','grid':'export 4px macrocell -> logical960 2px macrocell; paper uses tiled 9-slice to avoid stretched fibre','sources_manifest':rel(OUT/'sources.json')}
(OUT/'sprites.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'ui-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')

# Add only task-owned rows; preserve all other teams' latest records.
csvpath=ROOT/'art/provenance.csv'
with csvpath.open(encoding='utf-8-sig',newline='') as f:
    reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
prefix='ui_final_20261023_'
rows=[r for r in rows if not r['asset_id'].startswith(prefix)]
for s in spec:
    photo_source=s['source_kind']=='photo+script'
    rows.append(dict(zip(fields,[prefix+s['slot'],s['path'],'https://ambientcg.com/view?id=CardboardSet001' if photo_source else 'script://art/_review/2026-10-23-ui-final/build.py','CC0-1.0; Project-authored' if photo_source else 'Project-authored','2026-10-23','photo_to_pixel.quantize_exact; ui-final/build.py' if photo_source else 'ui-final/build.py; Pillow ImageDraw','no',s['sha256'],'UI sprite only; 4px export cells to 2px logical cells; original D123 size/border; full-color no tint. '+s['wear']])))
with csvpath.open('w',encoding='utf-8',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)

TEXT_LOG=[];PLACEMENTS=[]
def txt(im,pos,value,size=24,color=INK,font='g',record=True):
    scale=3 if font=='g' and size==36 else 2
    base=12 if font=='g' else size//scale
    path=OUT/'fonts'/('Galmuri11.ttf' if font=='g' else 'Pretendard-Regular.ttf')
    f=ImageFont.truetype(str(path),base);b=f.getbbox(value)
    if font=='p':
        # One native pixel between glyphs, preserving word spaces at low resolution.
        advances=[round(f.getlength(c))+(1 if ord(c)>127 else 0) for c in value]
        mask=Image.new('1',(max(1,sum(advances)),max(1,b[3]-b[1])))
        md=ImageDraw.Draw(mask);xx=0
        for c,a in zip(value,advances):md.text((xx,-b[1]),c,font=f,fill=1);xx+=a
    else:
        mask=Image.new('1',(max(1,b[2]-b[0]),max(1,b[3]-b[1])))
        ImageDraw.Draw(mask).text((-b[0],-b[1]),value,font=f,fill=1)
    mask=mask.resize((mask.width*scale,mask.height*scale),Image.Resampling.NEAREST)
    x,y=map(int,pos);im.paste(color,(x,y),mask)
    if record:TEXT_LOG.append({'text':value,'rect':[x,y,mask.width,mask.height],'font':'Galmuri11' if font=='g' else 'Pretendard','logical_em_px':size,'base_em_px':base,'integer_scale':scale})
    return (mask.width,mask.height)

def rect(im,b,fill=None,outline=None,width=2):
    x,y,w,h=map(int,b);ImageDraw.Draw(im).rectangle((x,y,x+w-1,y+h-1),fill=fill,outline=outline,width=width)
def hline(im,y,x=48,w=864,color=FIBRE):rect(im,(x,y,w,2),color)

def nine(name,size):
    s=next(s for s in spec if s['slot']==name)
    src=Image.open(ROOT/s['path']).convert('RGBA').resize((s['size'][0]//2,s['size'][1]//2),Image.Resampling.NEAREST)
    if s['mode']=='Simple':
        assert size==src.size,(name,size,src.size)
        return src
    l,b,r,t=[v//2 for v in s['border_LBRT']];sw,sh=src.size;dw,dh=size
    assert dw>=l+r and dh>=t+b and dw%2==dh%2==0
    out=Image.new('RGBA',size)
    xs=[0,l,sw-r,sw];ys=[0,t,sh-b,sh];xd=[0,l,dw-r,dw];yd=[0,t,dh-b,dh]
    for iy in range(3):
        for ix in range(3):
            piece=src.crop((xs[ix],ys[iy],xs[ix+1],ys[iy+1]));tw=xd[ix+1]-xd[ix];th=yd[iy+1]-yd[iy]
            if tw<=0 or th<=0:continue
            if s['mode']=='Tiled':piece=tile(piece,(tw,th))
            else:piece=piece.resize((tw,th),Image.Resampling.NEAREST)
            out.paste(piece,(xd[ix],yd[iy]))
    return out
def panel(im,name,b):
    x,y,w,h=b;im.alpha_composite(nine(name,(w,h)),(x,y));PLACEMENTS.append({'slot':name,'rect':list(b)})
def button(im,b,label,primary=False):
    panel(im,'buttonPlate',b);x,y,w,h=b
    if primary:rect(im,(x,y,6,h),INK)
    txt(im,(x+18,y+16),label)
def tag(im,b,label):panel(im,'enamelPlate',b);txt(im,(b[0]+16,b[1]+14),label,color=PAPER)
def stamp(im,x,y,label,slot='stampRing'):
    n=128 if slot=='stampRing' else 150
    panel(im,slot,(x,y,n,n))
    txt(im,(x+n//2-len(label)*12,y+n//2-12),label,color=STAMP)
def scene():
    # Existing game shot remains intact; flat shade is only a preview modal dimmer.
    bg=Image.open(OUT/'reference/game-960.png').convert('RGBA')
    return Image.alpha_composite(bg,Image.new('RGBA',bg.size,(35,31,27,90)))
def clock(im,time):
    panel(im,'paperPanel',(780,20,156,82));txt(im,(798,30),time,36);txt(im,(798,72),'남은 시간',24)
screens=[]
def finish(name,title,ui):
    bg=scene();im=Image.alpha_composite(bg,ui).convert('RGB')
    p=OUT/'mockups'/f'{name}.png';im.save(p)
    im.resize((1920,1080),Image.Resampling.NEAREST).save(OUT/'mockups'/f'{name}@2x.png')
    ui.save(OUT/'mockups'/f'{name}-ui.png')
    screens.append({'id':name,'title':title,'path':'mockups/'+name+'.png','zoom':'mockups/'+name+'@2x.png','ui_layer':'mockups/'+name+'-ui.png','size':[960,540],'placements':list(PLACEMENTS),'text':list(TEXT_LOG),'background':'reference/game-960.png','numbers':'illustrative; not engine state'})
    TEXT_LOG.clear();PLACEMENTS.clear()

ui=Image.new('RGBA',(960,540));panel(ui,'paperPanel',(28,24,904,492))
txt(ui,(48,48),'배달 사무소',36);txt(ui,(330,58),'오늘의 주문',24)
txt(ui,(804,42),'1:30',36);txt(ui,(792,84),'남은 시간',24)
hline(ui,116)
for i,(place,cargo,reward,care) in enumerate([('시청','반품 상자','1,500원','흔들림 주의'),('부두','활어','1,200원','신선도 주의'),('물류 센터','문구 상자','900원','표준 화물')]):
    x=48+i*288
    panel(ui,'paperCard',(x,138,264,262))
    txt(ui,(x+18,158),f'전표 0{i+1}',24)
    if i==0:
        rect(ui,(x,138,264,262),outline=TEAL,width=4);rect(ui,(x+4,142,4,254),INK);txt(ui,(x+186,158),'선택',24,TEAL)
    txt(ui,(x+18,206),place,36 if i<2 else 24)
    txt(ui,(x+18,258),cargo,24)
    hline(ui,304,x+18,228)
    txt(ui,(x+18,324),'보상 '+reward,24,font='p')
    txt(ui,(x+18,366),care,24,font='p')
txt(ui,(48,436),'선택 1/3 · 보상 1,500원',24,font='p')
txt(ui,(48,478),'고르기부터 배달까지 같은 시계',24,font='p')
button(ui,(648,438,264,60),'포장하러 →',True)
finish('orders','주문판',ui)

ui=Image.new('RGBA',(960,540));clock(ui,'1:22')
panel(ui,'paperPanel',(176,90,608,416))
txt(ui,(200,114),'전화 수신 전표',24)
txt(ui,(200,160),'시청 행사팀',36)
stamp(ui,636,104,'긴급')
hline(ui,220,200,412)
txt(ui,(200,246),'곧 철수가 끝나요.',24,font='p')
txt(ui,(200,282),'반품 물품을 먼저 부탁드릴게요.',24,font='p')
txt(ui,(200,336),'시청 · 반품 상자',24)
txt(ui,(200,378),'기본 1,000원 + 긴급 500원',24,font='p')
button(ui,(200,436,164,52),'거절')
button(ui,(500,436,260,52),'이 주문 추가',True)
finish('call','긴급 전화',ui)

ui=Image.new('RGBA',(960,540));panel(ui,'paperPanel',(24,20,912,82))
txt(ui,(44,38),'포장',36);txt(ui,(164,48),'화물 1/1 · 반품 상자',24)
txt(ui,(798,30),'1:08',36);txt(ui,(798,72),'남은 시간',24)
panel(ui,'paperPanel',(24,116,196,316))
panel(ui,'buttonPlate',(34,128,176,78))
rect(ui,(34,128,4,78),INK)
for i,(s,v) in enumerate([('1 버블랩','약 230원'),('2 폼','약 380원'),('3 하드케이스','약 520원')]):
    yy=138+i*88;txt(ui,(42,yy),s,24);txt(ui,(42,yy+34),v,24,font='p')
txt(ui,(156,172),'선택',24,TEAL)
panel(ui,'paperPanel',(240,116,418,316))
txt(ui,(260,134),'현재 앞면',24)
# Composite an existing render only. No Blender/model/art-team source changes.
cargo=Image.open(OUT/'reference/cargo-existing.png').convert('RGBA')
cargo=cargo.crop(cargo.getchannel('A').getbbox())
ratio=min(154/cargo.width,122/cargo.height)
sz=(int(cargo.width*ratio),int(cargo.height*ratio))
cargo=cargo.resize(sz,Image.Resampling.NEAREST).resize((sz[0]*2,sz[1]*2),Image.Resampling.NEAREST)
ui.alpha_composite(cargo,(449-cargo.width//2,172))
panel(ui,'paperPanel',(678,116,258,316))
txt(ui,(696,134),'보호 예상 B',24)
for i,(label,val) in enumerate([('앞',85),('뒤',50),('좌',45),('우',45)]):
    y=186+i*42;txt(ui,(696,y),label,24)
    panel(ui,'gaugeTrackSprite',(742,y+4,116,16))
    # Keep every filled width on the 2px logical grid and preserve border minimum.
    fw=max(8,2*round(108*val/100/2));panel(ui,'gaugeFillSprite',(746,y+8,fw,8))
    txt(ui,(866,y),str(val)+'%',24,font='p')
hline(ui,352,696,222)
txt(ui,(696,372),'포장비 230원',24)
txt(ui,(696,402),'보상 1,500원',24,font='p')
panel(ui,'paperPanel',(24,442,912,78))
txt(ui,(44,454),'WASD 회전 · 드래그 감기',24,font='p')
txt(ui,(44,486),'비용은 예상 · 배치 후 출발',24,font='p')
button(ui,(716,452,200,54),'배치하기',True)
finish('packing','포장',ui)

ui=Image.new('RGBA',(960,540));panel(ui,'paperPanel',(244,24,472,492))
txt(ui,(272,48),'배달 영수증',24)
txt(ui,(272,94),'배달 완료',36)
stamp(ui,576,38,'인수')
hline(ui,170,272,416)
txt(ui,(272,190),'순이익',24);txt(ui,(454,184),'+1,270원',36)
hline(ui,234,272,416)
for y,left,right in [(256,'배달 보상','1,500원'),(296,'포장비','-230원'),(336,'화물 상태','96% · 양호')]:
    txt(ui,(272,y),left,24,font='p');txt(ui,(510,y),right,24,font='p')
txt(ui,(272,386),'물건 잘 받았습니다.',24,font='p')
button(ui,(272,444,416,54),'돌아가기 →',True)
finish('receipt','정산 영수증',ui)
(OUT/'screens.json').write_text(json.dumps(screens,ensure_ascii=False,indent=2),encoding='utf-8')

# Review sheets, never included in the sprite import allowlist.
sheet=Image.new('RGB',(1980,1280),PAPER)
txt(sheet,(28,22),'멈춘 화면 4종 / 최종 검토 후보',36,record=False)
for i,s in enumerate(screens):
    x=20+i%2*980;y=86+i//2*592
    txt(sheet,(x,y),f'{i+1:02d}  '+s['title'],24,record=False)
    sheet.paste(Image.open(OUT/s['path']),(x,y+34))
sheet.save(OUT/'screen-sheet.png')
sheet=Image.new('RGB',(1440,1760),PAPER)
txt(sheet,(28,20),'10 슬롯 / 크기·9-slice 여백 유지',36,record=False)
for i,s in enumerate(spec):
    x=28+i%2*706;y=96+i//2*328
    txt(sheet,(x,y),s['slot'],24,record=False)
    iw,ih=s['size'];im=Image.open(ROOT/s['path']).convert('RGBA')
    guide=Image.new('RGBA',(iw,ih),(196,199,201,255));guide.alpha_composite(im)
    l,b,r,t=s['border_LBRT'];d=ImageDraw.Draw(guide)
    if any([l,b,r,t]):
        for xx in [l,iw-r]:d.line([(xx,0),(xx,ih-1)],fill=STAMP,width=2)
        for yy in [t,ih-b]:d.line([(0,yy),(iw-1,yy)],fill=STAMP,width=2)
    guide.save(OUT/'guides'/f'{s["slot"]}.png')
    thumb=guide.resize((iw//2,ih//2),Image.Resampling.NEAREST);sheet.paste(thumb.convert('RGB'),(x,y+42))
    txt(sheet,(x+180,y+60),f'{iw} x {ih}',24,record=False)
    txt(sheet,(x+180,y+100),'L/B/R/T '+ '/'.join(map(str,s['border_LBRT'])),24,font='p',record=False)
    txt(sheet,(x+180,y+144),s['mode']+' / Color.white',24,font='p',record=False)
sheet.save(OUT/'sprite-sheet.png')
compare=Image.new('RGB',(1920,1930),PAPER)
txt(compare,(30,22),'기준 원화 / 실제 게임 / 최종 UI',36,record=False)
for i,(p,title) in enumerate([('reference/concept.png','D-096 원화 · 참고 전용'),('reference/game-960.png','기존 게임 캡처 · slice-start')]):
    im=Image.open(OUT/p).convert('RGB');im=ImageOps.fit(im,(960,540),method=Image.Resampling.NEAREST)
    txt(compare,(i*960+24,88),title,24,record=False);compare.paste(im,(i*960,124))
for i,s in enumerate(screens):
    x=i%2*960;y=700+i//2*600;txt(compare,(x+24,y),s['title'],24,record=False);compare.paste(Image.open(OUT/s['path']),(x,y+36))
txt(compare,(24,1900),'배경·화물은 기존 캡처 합성. 실제 Unity 적용 결과 아님. 원화/구시안 픽셀은 스프라이트 입력에서 제외.',24,font='p',record=False)
compare.save(OUT/'compare.png')
grain=Image.new('RGB',(1632,820),PAPER)
txt(grain,(24,22),'미세 질감 → 2px 종이 격자',36,record=False)
old=Image.open(OUT/'reference/old-call.png').crop((400,116,528,244)).convert('RGB')
new=nine('paperPanel',(256,256)).convert('RGB').crop((32,32,160,160))
world=Image.open(OUT/'reference/game-960.png').crop((668,150,796,278)).convert('RGB')
for x,im,title in [(24,old,'이전 시안 / 미세 질감'),(560,new,'새 종이 / 2px 격자'),(1096,world,'실제 게임 / 비교')]:
    txt(grain,(x,94),title,24,record=False)
    grain.paste(im.resize((512,512),Image.Resampling.NEAREST),(x,140))
txt(grain,(24,694),'비교 확대 4배 / 별도 원본 patch-new-128.png, 정확한 2배 patch-new-128@2x.png 제공.',24,font='p',record=False)
txt(grain,(24,744),'종이 중앙은 border를 유지한 Tiled. 목업의 섬유는 가로·세로 2px씩 유지.',24,font='p',record=False)
grain.save(OUT/'grain-compare.png');new.save(OUT/'materials/patch-new-128.png');new.resize((256,256),Image.Resampling.NEAREST).save(OUT/'materials/patch-new-128@2x.png')

def esc(t):return html.escape(str(t))
cards=''.join(f'<article><h3>{s["title"]}</h3><div class="scroll"><img width="960" height="540" class="native" src="{s["path"]}" alt="{s["title"]}"></div><p><a href="{s["path"]}">960 원본</a> · <a href="{s["zoom"]}">1920 2× 확대</a></p></article>' for s in screens)
rows=''.join(f'<tr><td>{s["owner"]}.{s["slot"]}</td><td>{s["size"][0]}×{s["size"][1]}</td><td>{"/".join(map(str,s["border_LBRT"]))}</td><td>{s["mode"]}</td><td><a href="sprites/{s["slot"]}.png">PNG</a> · <a href="guides/{s["slot"]}.png">여백</a></td></tr>' for s in spec)
css='''@font-face{font-family:P;src:url(fonts/Pretendard-Regular.ttf)}@font-face{font-family:G;src:url(fonts/Galmuri11.woff2)}*{box-sizing:border-box}body{margin:0;background:#e4d6bf;color:#30362f;font:18px/1.65 P,sans-serif}header,main,footer{max-width:1280px;margin:auto;padding:40px}header{padding-top:60px;border-bottom:2px solid #30362f}.eyebrow{font-size:13px;letter-spacing:.14em;color:#8a4631}h1{font:36px/1.5 G;margin:14px 0}h2{font-size:28px;font-weight:400;margin-top:52px}h3{font-size:21px;font-weight:400}a{color:inherit;text-underline-offset:4px}nav{display:flex;gap:22px;flex-wrap:wrap}.badge{display:inline-block;border:1px solid #8a4631;color:#8a4631;padding:5px 14px;margin:10px 0 24px}.notice{background:#c4a687;border-left:4px solid #8a4631;padding:16px 22px}.scroll{overflow-x:auto;background:#231f1b;padding:16px}.native{display:block;width:960px;height:540px;max-width:none;margin:auto;image-rendering:pixelated}img{max-width:100%;height:auto;display:block;image-rendering:pixelated}article{padding:24px 0;border-bottom:1px solid #97826a}table{border-collapse:collapse;width:100%;font-size:15px}td,th{padding:10px 6px;text-align:left;border-bottom:1px solid #97826a}.cols{display:grid;grid-template-columns:1fr 1fr;gap:24px}small{font-size:14px}footer{font-size:14px}@media(max-width:700px){header,main,footer{padding:22px}.cols{grid-template-columns:1fr}h1{font-size:28px}table{font-size:12px}.table-scroll{overflow:auto}}'''
page=f'''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>PitCrew / UI FINAL / 멈춘 화면</title><style>{css}</style><body><header><div class="eyebrow">PITCREW / UI RESEARCH 03 / 2026.10.23</div><h1>잠깐 멈춘 손에,<br>바랜 배달 전표.</h1><p>CC0 사진의 종이섬유, 직접 찍은 잉크선과 글자.<br>화면은 960×540. 종이 한 칸은 2×2px.</p><span class="badge">도현 승인 필요 · 게임 미적용</span><nav><a href="#screens">화면 4종</a><a href="#grain">픽셀 비교</a><a href="#sprites">10 슬롯</a><a href="#compare">원화·게임 옆 비교</a><a href="HANDOFF.md">임포트 명세</a></nav></header><main><p class="notice">출시 후보는 <b>sprites/*.png 10개만</b>입니다. 기존 AI 종이·도장·HUD 제거 배경은 사용하지 않았습니다. 원화와 이전 시안은 비교 자료로만 표시합니다. Unity 실행·임포트는 하지 않았습니다.</p><section id="screens"><h2>01 / 멈춘 화면 네 장</h2><p>원본 1px = CSS 1px. 좁은 창에서는 가로 스크롤. 실제 게임 캡처 위 UI 합성이며 수치는 예시입니다.</p>{cards}<p><a href="screen-sheet.png">4화면 PNG 시트</a></p></section><section id="grain"><h2>02 / 종이도 세계와 같은 화면 격자</h2><a href="grain-compare.png"><img src="grain-compare.png" alt="종이 픽셀 비교"></a><p>무인쇄 사진 → BOX 64×64 → 밝기 재색 → photo_to_pixel.py의 정확한 팔레트 양자화. 디더·랜덤 노이즈 없음. 4px 블록으로 PNG 저장, 960 논리 좌표에서는 2px 블록. 종이 9-slice는 Tiled로 늘어난 영역을 채웁니다.</p><p>사진 원문: <a href="https://ambientcg.com/view?id=CardboardSet001">ambientCG Cardboard Set 001 / Surface Photogrammetry</a> · <a href="https://docs.ambientcg.com/license/">CC0</a>. 흰 종이를 직접 촬영했다고 주장하지 않습니다.</p></section><section id="sprites"><h2>03 / 기존 슬롯 계약과 최종 PNG</h2><p>크기·L/B/R/T 여백은 D-123과 동일. 모든 PNG는 완성 색이므로 <b>Image.color = Color.white</b>. 기존 ColorBlock/재료 틴트를 그대로 곱하면 안 됩니다. 종이·머리선은 border를 유지한 Tiled, 나머지는 표의 형식입니다.</p><div class="table-scroll"><table><tr><th>슬롯</th><th>원본 px</th><th>L/B/R/T</th><th>Image.Type</th><th>파일</th></tr>{rows}</table></div><p><a href="sprite-sheet.png">스프라이트 시트</a> · <a href="ui-manifest.json">검사 명세</a> · <a href="validation/check-asset-ui.json">check_asset.py 결과</a> · <a href="validation/review.json">화면 검증</a></p></section><section id="compare"><h2>04 / 원화·현재 게임과 나란히</h2><a href="compare.png"><img src="compare.png" alt="기준 원화와 게임 캡처와 UI 네 장 비교"></a><p>원화는 색·정서 참고 전용. 게임 배경은 기존 slice-start.png, 포장 상자는 아트팀 기존 box/close.png를 합성했습니다. 3D 모델을 만들거나 고치지 않았습니다.</p></section><section><h2>05 / 전달과 남은 확인</h2><p>Galmuri11 12px 원형 → 본문 24px·제목/핵심 숫자 36px. 긴 대사·내역은 Pretendard 12px 마스크 → 24px 비교를 적용. Jua 없음. 실제 TMP 가독성은 QA 확인 필요.</p><p><a href="HANDOFF.md">슬롯·크기·여백·폰트 표 / 적용 순서</a> · <a href="SOURCES.md">사진·폰트·캡처 출처</a> · <a href="build.py">재생성 코드</a> · <a href="validation/graphics-review.md">그래픽 검토</a></p><p>도현 승인 필요: 네 화면의 위계·종이 질감·도장, Tiled/완성 색 계약, 정산 뒤 돌아가기. QA/프로그래밍 임포트 필요.</p></section></main><footer>승인 전 F1/Assets에 넣지 않음. 출시 AI 생성 이미지 0 정책에 맞춰 스프라이트 입력 경로를 사진/스크립트로 제한. 출시 빌드 전체 전수 감사는 별도.</footer></body></html>'''
(OUT/'index.html').write_text(page,encoding='utf-8')
print(json.dumps({'sprites':len(spec),'screens':len(screens),'photo_colors':paper.getcolors(),'preserved_borders':True,'provenance_ui_rows':10},ensure_ascii=False))
