"""Course signs and UI review. CC0 photo + font/script; no Unity, no AI image input."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps
import numpy as np
import json, hashlib, shutil, csv, math, sys, html

OUT=Path(__file__).resolve().parent;ROOT=OUT.parents[2]
PREV=ROOT/'art/_review/2026-10-23-ui-final'
for f in ['textures','layers','materials','reference','fonts','hud','arrival','readability','validation']:(OUT/f).mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(ROOT/'tools/asset-gen'))
from photo_to_pixel import quantize_exact,load_palette
PAL=ROOT/'art/source/palette/pitcrew_d123_32.json';PALETTE=load_palette(PAL)
PAPER='#E4D6BF';INK='#30362F';DARK='#231F1B';FIBRE='#C4A687';TEAL='#677B80';RUST='#8A4631';AMBER='#B89360';BROWN='#795244';GREY='#525350'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def rel(p):return Path(p).resolve().relative_to(ROOT).as_posix()
def write(name,data):(OUT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
for name in ['Galmuri11.ttf','Galmuri11.woff2','Galmuri-OFL.txt','Pretendard-Regular.ttf','Pretendard-OFL.txt']:shutil.copyfile(PREV/'fonts'/name,OUT/'fonts'/name)
SOURCES=[]
for name in ['1-start','2-curve','3-coast','4-hairpin','5-village','fork-sign','detour','overlook','meadow','lonehouse','packing-timeleft']:
    src=ROOT/'docs/qa/evidence'/('proto-course-'+name+'.png');dst=OUT/'reference'/(name+'.png')
    shutil.copyfile(src,dst);SOURCES.append({'source':rel(src),'snapshot':rel(dst),'sha256':sha(src),'scope':'existing capture; not captured in this task'})
for src,name,scope in [(ROOT/'F1/Assets/Editor/BuildPostcardSlice.cs','BuildPostcardSlice.cs','read-only code snapshot; newer 1827 fork/meadow changes observed'),(ROOT/'F1/Assets/Settings/Camera/CameraFeel_4_Postcard.asset','CameraFeel_4_Postcard.asset','FOV50-62; chase distance4.2m'),(ROOT/'docs/qa/tmp/proto-cap2.cs','proto-cap2.cs','edit-mode capture method'),(ROOT/'F1/Assets/Scripts/Packing/PackingScreen.cs','PackingScreen.cs','time-left label already implemented; display truncates seconds')]:
    shutil.copyfile(src,OUT/'reference'/name);SOURCES.append({'source':rel(src),'snapshot':rel(OUT/'reference'/name),'sha256':sha(src),'scope':scope})
concept=ROOT/'art/concept/lofi-styles-2026-10-13/02_warm_street.png'
shutil.copyfile(concept,OUT/'reference/02_warm_street.png')
SOURCES.append({'source':rel(concept),'snapshot':rel(OUT/'reference/02_warm_street.png'),'sha256':sha(concept),'scope':'D-096 concept reference only; never an asset input or release import'})
PHOTO=ROOT/'art/source/photos/course-signs/rusty_painted_metal_diff_1k.png'
PAPERPHOTO=ROOT/'art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png'
photo=Image.open(PHOTO).convert('RGB');pw,ph=photo.size
crop=(int(pw*.20),int(ph*.38),int(pw*.83),int(ph*.96))
small=photo.crop(crop).resize((64,64),Image.Resampling.BOX);small.save(OUT/'materials/metal-downsample-64.png')
rust=quantize_exact(small,PALETTE,'none');rust.save(OUT/'materials/metal-palette-64.png')
paper=Image.open(PREV/'materials/paper-photo-64.png').convert('RGB')
SOURCES.extend([
 {'source':rel(PHOTO),'sha256':sha(PHOTO),'source_url':'https://polyhaven.com/a/rusty_painted_metal','license_url':'https://polyhaven.com/license','license':'CC0-1.0','author':'Amal Kumar','crop_ltrb':crop,'pipeline':'photo diffuse -> BOX64 -> photo_to_pixel.quantize_exact none -> localized edge/bolt masks'},
 {'source':rel(PAPERPHOTO),'sha256':sha(PAPERPHOTO),'source_url':'https://ambientcg.com/view?id=CardboardSet001','license':'CC0-1.0','pipeline':'reuse verified ui-final photo paper patch; source crop825,590,1000,705 -> BOX64 -> luma recolor -> exact palette'},
])
write('sources.json',SOURCES)

def text(im,xy,value,size=24,color=INK,shadow=False,font='g'):
    scale=size//12;assert scale>=1 and size%12==0
    f=ImageFont.truetype(str(OUT/'fonts'/('Galmuri11.ttf' if font=='g' else 'Pretendard-Regular.ttf')),12)
    b=f.getbbox(value);mask=Image.new('1',(max(1,b[2]-b[0]),max(1,b[3]-b[1])))
    ImageDraw.Draw(mask).text((-b[0],-b[1]),value,font=f,fill=1)
    mask=mask.resize((mask.width*scale,mask.height*scale),Image.Resampling.NEAREST)
    x,y=map(int,xy)
    if shadow:im.paste(DARK,(x+2,y+2),mask)
    im.paste(color,(x,y),mask)
    return [x,y,mask.width+(2 if shadow else 0),mask.height+(2 if shadow else 0)]
def rectangle(im,b,fill,outline=None,width=1):
    x,y,w,h=b;ImageDraw.Draw(im).rectangle((x,y,x+w-1,y+h-1),fill=fill,outline=outline,width=width)
def tile(src,size):
    dest=Image.new(src.mode,size)
    for y in range(0,size[1],src.height):
        for x in range(0,size[0],src.width):dest.paste(src.crop((0,0,min(src.width,size[0]-x),min(src.height,size[1]-y))),(x,y))
    return dest
def arrow(im,x,y,left=False,color=INK):
    d=ImageDraw.Draw(im)
    if left:d.polygon([(x,y),(x+14,y),(x+14,y+4),(x+7,y+4),(x+18,y+15),(x+14,y+19),(x+3,y+8),(x+3,y+15),(x,y+15)],fill=color)
    else:d.polygon([(x+9,y),(x+18,y+9),(x+12,y+9),(x+12,y+21),(x+6,y+21),(x+6,y+9),(x,y+9)],fill=color)

SIGNS=[]
definitions=[
 ('shortcut','지름길 — 급경사, 화물 주의',(112,64),PAPER,'bolt rust; lower runoff; old repair strip'),
 ('coast','해안길 — 완만',(112,64),INK,'sun-faded teal top edge; bolt rust; bottom repair strip'),
 ('alley','골목 끝 집',(80,64),PAPER,'handled lower edge; replaced gate label; one paper patch'),
 ('cafe','해안 카페',(96,64),INK,'repainted enamel; corner rust; pasted open notice'),
 ('orchard','헤어핀 과수원',(112,64),PAPER,'bottom soil/rain; replaced lower harvest notice'),
 ('hill','언덕 위 홀로 떨어진 집',(80,64),PAPER,'old enamel edge; replaced name strip; bell notice'),
 ('mart','MART',(128,48),INK,'older sign visible at lower rim; overpaint; delivery paper patch'),
]
for index,(sid,title,active,paint,wear) in enumerate(definitions):
    w,h=active;size=(2**math.ceil(math.log2(w)),2**math.ceil(math.log2(h)))
    base=Image.new('RGBA',size);face=Image.new('RGBA',active,paint);d=ImageDraw.Draw(face)
    # Photo is restricted to physical failure points, never full-field random noise.
    mask=Image.new('L',active);md=ImageDraw.Draw(mask)
    md.rectangle((0,h-3,w-1,h-1),fill=255)
    md.rectangle((0,0,2,h-1),fill=255);md.rectangle((w-3,0,w-1,h-1),fill=255)
    md.rectangle((5,3,8,11),fill=255);md.rectangle((w-9,3,w-6,8),fill=255)
    patch=tile(rust.convert('RGBA'),active);face.paste(patch,(0,0),mask)
    d=ImageDraw.Draw(face)
    d.line((3,1,w-4,1),fill=FIBRE if paint==PAPER else TEAL)
    d.line((3,h-5,w//3,h-5),fill=FIBRE if paint==PAPER else GREY)
    for x in [6,w-7]:d.rectangle((x,4,x+1,5),fill=INK if paint==PAPER else PAPER)
    # A visible older coat, limited to a narrow exposed paint edge.
    d.line((w//2,h-2,w-6,h-2),fill=TEAL)
    glyph=Image.new('RGBA',size);labels=[]
    def label(x,y,s,n=12,c=INK,role='detail'):
        r=text(glyph,(x,y),s,n,c);labels.append({'text':s,'font':'Galmuri11','font_em_texels':n,'rect_texels':r,'role':role});return r
    if sid in ['shortcut','coast']:
        c=INK if sid=='shortcut' else PAPER
        arrow(glyph,8,5,sid=='coast',c)
        label(33,5,'지름길' if sid=='shortcut' else '해안길',24,c,'primary')
        if sid=='shortcut':
            label(8,29,'급경사',24,c,'primary');label(58,52,'화물주의',12,c)
        else:label(34,32,'완만',24,c,'primary')
    elif sid=='alley':
        label(5,8,'01',12,INK,'number');label(27,3,'골목',24,INK,'primary');label(27,27,'끝집',24,INK,'primary')
        patch=tile(paper,(68,12)).convert('RGBA');face.paste(patch,(5,50));label(8,50,'문 앞 배송',12)
    elif sid=='cafe':
        label(6,7,'02',12,PAPER,'number');label(40,3,'해안',24,PAPER,'primary');label(40,27,'카페',24,PAPER,'primary')
        d=ImageDraw.Draw(glyph);d.rectangle((8,28,25,40),outline=PAPER,width=2);d.line((7,44,28,44),fill=PAPER,width=2);d.line((26,30,30,30,30,37,26,37),fill=PAPER,width=2)
        patch=paper.crop((0,0,44,12)).convert('RGBA');face.paste(patch,(42,50));label(46,50,'영업중',12,INK)
    elif sid=='orchard':
        label(4,18,'03',24,INK,'number');label(35,3,'헤어핀',24,INK,'primary');label(35,27,'과수원',24,INK,'primary')
        face.paste(tile(paper,(69,12)).convert('RGBA'),(36,50));label(39,50,'오늘 수확',12)
    elif sid=='hill':
        label(5,5,'외딴집',24,INK,'primary');label(8,32,'04 언덕위',12)
        face.paste(tile(paper,(70,12)).convert('RGBA'),(5,50));label(8,50,'초인종 →',12)
    else:
        label(29,5,'MART',24,PAPER,'primary')
        # Old painted strip and a later paper notice have different ages.
        rectangle(face,(4,33,46,12),TEAL);label(6,34,'매일',12,PAPER)
        face.paste(tile(paper,(69,12)).convert('RGBA'),(55,33));label(59,34,'배달 접수',12)
    base.paste(face,(0,0));final=Image.alpha_composite(base,glyph)
    for a in labels:
        x,y,ww,hh=a['rect_texels'];assert x+ww<=w and y+hh<=h,(sid,a)
    final.save(OUT/'textures'/(sid+'.png'));base.save(OUT/'layers'/(sid+'-base.png'));glyph.save(OUT/'layers'/(sid+'-letters.png'))
    SIGNS.append({'id':sid,'title':title,'path':rel(OUT/'textures'/(sid+'.png')),'size_px':list(size),'active_xywh':[0,0,w,h],'physical_m':[w/32,h/32],'density_px_m':[32,32],'uv_bounds_bl':[0,1-h/size[1],w/size[0],1],'letters':labels,'wear':wear,'wear_mask_scope':'outer sides2-3texels; bottom3texels; bolt local runs; repair paper only','letter_density_exception':False,'font_source':'https://github.com/quiple/galmuri','font_sha256':sha(OUT/'fonts/Galmuri11.ttf'),'ai_generated':False,'sha256':sha(OUT/'textures'/(sid+'.png'))})
write('signs.json',SIGNS)

# A comparison sheet at exact 4x texture scale, with source pixel dimensions.
sheet=Image.new('RGB',(1440,1550),PAPER);text(sheet,(28,24),'동네에서 오래 쓴 일곱 표지',36)
for i,s in enumerate(SIGNS):
    x=28+i%2*706;y=92+i//2*360;w,h=s['active_xywh'][2:]
    text(sheet,(x,y),s['title'],24)
    plate=Image.open(ROOT/s['path']).crop((0,0,w,h)).convert('RGBA').resize((w*4,h*4),Image.Resampling.NEAREST)
    sheet.paste(plate,(x,y+38),plate)
    text(sheet,(x,y+38+h*4+12),f'{s["physical_m"][0]:g}m x {s["physical_m"][1]:g}m / 32px/m',24)
sheet.save(OUT/'sign-sheet.png')

# Fixed nine-slice parts from approved-direction final candidate, not regenerated.
UI_SPEC=json.loads((PREV/'sprites.json').read_text('utf-8'))
def nine(slot,size):
    s=next(s for s in UI_SPEC if s['slot']==slot);im=Image.open(ROOT/s['path']).convert('RGBA').resize((s['size'][0]//2,s['size'][1]//2),Image.Resampling.NEAREST)
    if s['mode']=='Simple':assert size==im.size;return im
    l,b,r,t=[v//2 for v in s['border_LBRT']];sw,sh=im.size;w,h=size;out=Image.new('RGBA',size)
    xs=[0,l,sw-r,sw];ys=[0,t,sh-b,sh];xx=[0,l,w-r,w];yy=[0,t,h-b,h]
    for iy in range(3):
        for ix in range(3):
            a=im.crop((xs[ix],ys[iy],xs[ix+1],ys[iy+1]));ss=(xx[ix+1]-xx[ix],yy[iy+1]-yy[iy])
            if min(ss)<=0:continue
            a=tile(a,ss) if s['mode']=='Tiled' else a.resize(ss,Image.Resampling.NEAREST)
            out.paste(a,(xx[ix],yy[iy]))
    return out
def panel(im,b,slot='paperPanel'):
    x,y,w,h=b;im.alpha_composite(nine(slot,(w,h)),(x,y))
def background(name):return Image.open(OUT/'reference'/(name+'.png')).convert('RGBA').resize((960,540),Image.Resampling.NEAREST)
SCREENS=[]
def save_screen(folder,sid,title,bg,ui,groups,note):
    path=f'{folder}/{sid}.png';zoom=f'{folder}/{sid}@2x.png';overlay=f'{folder}/{sid}-ui.png'
    im=Image.alpha_composite(bg,ui).convert('RGB');im.save(OUT/path);im.resize((1920,1080),Image.Resampling.NEAREST).save(OUT/zoom);ui.save(OUT/overlay)
    mask=Image.new('1',(960,540));md=ImageDraw.Draw(mask)
    for x,y,w,h in groups:md.rectangle((x,y,x+w-1,y+h-1),fill=1)
    SCREENS.append({'id':sid,'title':title,'path':path,'zoom':zoom,'overlay':overlay,'groups':groups,'envelope_percent':round(np.asarray(mask).sum()/518400*100,2),'ink_percent':round((np.asarray(ui)[:,:,3]>0).sum()/518400*100,2),'note':note,'sha256':sha(OUT/path)})
def bounds(items):
    x=min(a[0] for a in items);y=min(a[1] for a in items);return [x,y,max(a[0]+a[2] for a in items)-x,max(a[1]+a[3] for a in items)-y]
for sid,urgent in [('outbound',False),('outbound-urgent',True),('outbound-choice',False)]:
    ui=Image.new('RGBA',(960,540));groups=[]
    def t(x,y,s,size=24,c=PAPER):return text(ui,(x,y),s,size,c,True)
    groups.append(bounds([t(24,24,'03 헤어핀 과수원 ↑'),t(24,58,'지름길: 급경사 · 화물 주의' if sid=='outbound-choice' else '직선 280m · 배달 0/1')]))
    groups.append(bounds([t(808,24,'0:08' if urgent else '1:14',36,AMBER if urgent else PAPER),t(808,66,'남은 시간')]))
    groups.append(bounds([t(24,466,'케이크 62%' if urgent else '케이크 96%'),t(24,500,'흔들림 주의' if urgent else '양호',24,AMBER if urgent else PAPER)]))
    groups.append(t(810,494,'72 km/h'))
    if urgent:rectangle(ui,(796,24,4,60),AMBER);groups.append([796,24,4,60])
    save_screen('hud',sid,'갈 때 · '+('갈림길 접근 안내' if sid=='outbound-choice' else '마감·손상 경고' if urgent else '일반'),background('fork-sign' if not urgent else '4-hairpin'),ui,groups,'illustrative numbers; straight-line distance, not route remaining length; central60%x60% clear; choice hint replaces second line at fork approach45m, no additional panel')
for sid in ['return-entry','return-quiet','return-guide']:
    ui=Image.new('RGBA',(960,540));groups=[]
    if sid=='return-entry':groups.append(text(ui,(24,492),'돌아오는 길',24,PAPER,True))
    if sid=='return-guide':groups.append(bounds([text(ui,(24,24),'MART ↙',24,PAPER,True),text(ui,(24,58),'길 안내',24,PAPER,True)]))
    save_screen('hud',sid,{'return-entry':'귀로 · 진입 4초','return-quiet':'귀로 · 기본 0%','return-guide':'귀로 · 안내 요청'}[sid],background('overlook'),ui,groups,'existing outward overlook capture as visual proxy; actual return drive not captured; direction illustrative')

# Reuse the actual final packing UI composition, update the header into one common clock.
ui=Image.open(PREV/'mockups/packing-ui.png').convert('RGBA')
panel(ui,(24,20,912,82));text(ui,(44,38),'포장',36);text(ui,(164,48),'화물 1/1 · 골목 끝 집',24)
text(ui,(798,30),'2:01',36);text(ui,(798,72),'남은 시간',24)
# Existing box preview is a reused art-team render. Costs are example figures, not runtime.
panel(ui,(24,116,196,316));panel(ui,(34,128,176,78),'buttonPlate');rectangle(ui,(34,128,4,78),INK)
for i,(material,cost) in enumerate([('1 버블랩','약 90원'),('2 폼','약 150원'),('3 하드케이스','약 210원')]):
    text(ui,(42,138+i*88),material);text(ui,(42,172+i*88),cost,font='p')
text(ui,(156,172),'선택',24,TEAL)
panel(ui,(678,116,258,316));text(ui,(696,134),'보호 예상 B')
for i,(label,value) in enumerate([('앞',85),('뒤',50),('좌',45),('우',45)]):
    yy=186+i*42;text(ui,(696,yy),label);panel(ui,(742,yy+4,116,16),'gaugeTrackSprite');panel(ui,(746,yy+8,2*round(108*value/100/2),8),'gaugeFillSprite');text(ui,(866,yy),str(value)+'%',font='p')
rectangle(ui,(696,352,222,2),FIBRE);text(ui,(696,372),'포장비 90원');text(ui,(696,402),'보상 600원',font='p')
bg=Image.alpha_composite(background('1-start'),Image.new('RGBA',(960,540),(35,31,27,90)))
save_screen('hud','packing-time','포장 · 공용 남은 시간',bg,ui,[[24,20,912,500]],'existing packing has a time-left label already; this art proposal unifies the duplicated clock; numbers illustrative')

for sid in ['a-slip','b-customer','c-release']:
    ui=Image.new('RGBA',(960,540));groups=[]
    if sid=='a-slip':
        panel(ui,(24,24,520,140));panel(ui,(400,30,128,128),'stampRing')
        text(ui,(44,38),'아슬아슬!',36);text(ui,(44,84),'2.8초 남기고',24);text(ui,(44,122),'무사 도착 · 유리 96%',24)
        text(ui,(440,80),'인수',24,RUST);groups=[[24,24,520,140]]
        title='A / 인수 도장';note='0.12s clock lock -> 0.16s stamp contact -> hold1.6s -> receipt. One paper tap + customer acknowledgement. No fullscreen flash/shake or new reward.'
    elif sid=='b-customer':
        panel(ui,(632,24,304,188));panel(ui,(644,38,128,128),'stampRing');text(ui,(684,88),'인수',24,RUST)
        text(ui,(794,48),'2.8',36);text(ui,(794,94),'초 남김',24);text(ui,(652,176),'무사 도착 · 유리 96%',24)
        panel(ui,(24,442,644,74));text(ui,(44,456),'집주인',24,TEAL);text(ui,(44,486),'덕분에 제시간에 받았네요.',24,font='p');groups=[[632,24,304,188],[24,442,644,74]]
        title='B / 손님의 인정';note='0.12s clock lock -> customer subtitle/voice0.2-2.2s -> small receipt. Human acknowledgement is the peak; voice not produced. Keep house/horizon uncovered.'
    else:
        panel(ui,(708,24,228,112));text(ui,(728,44),'2.8초',36);text(ui,(728,96),'남기고 도착',24)
        panel(ui,(242,442,476,74));text(ui,(262,456),'무사 도착',24);text(ui,(262,488),'이제 천천히 돌아가요.',24,font='p');groups=[[708,24,228,112],[242,442,476,74]]
        title='C / 긴장 풀기';note='clock freezes in same corner -> converts to paper stub0.18s -> hold1.2s -> collapse receipt. Wind rises after clock vanishes. No score burst. Sound/timing proposal only.'
    save_screen('arrival',sid,title,background('lonehouse'),ui,groups,note)
write('screens.json',SCREENS)

# Perspective sampler: distances are car-to-plane; camera is 4.2m behind car.
# This reproduces the official frustum formula, not a Unity scene render.
def project_texture(s,distance,fov,yaw):
    tex=Image.open(ROOT/s['path']).convert('RGBA').crop(tuple([0,0]+s['active_xywh'][2:]))
    w,h=tex.size;wm,hm=s['physical_m'];z0=distance+4.2;ang=math.radians(yaw);focal=270/math.tan(math.radians(fov/2))
    def project(x,y):
        X=(x/w-.5)*wm;Y=(.5-y/h)*hm;Z=z0+math.sin(ang)*X
        return [740+focal*math.cos(ang)*X/Z,205-focal*Y/Z]
    src=[(0,0),(w,0),(w,h),(0,h)];dst=[project(*a) for a in src]
    aa=[];bb=[]
    for (x,y),(u,v) in zip(dst,src):
        aa.extend([[x,y,1,0,0,0,-u*x,-u*y],[0,0,0,x,y,1,-v*x,-v*y]]);bb.extend([u,v])
    co=np.linalg.solve(np.array(aa),np.array(bb))
    layer=tex.transform((960,540),Image.Transform.PERSPECTIVE,co,Image.Resampling.NEAREST)
    metrics=[]
    for label in s['letters']:
        x,y,ww,hh=label['rect_texels'];a=project(x,y);b=project(x,y+hh);c=project(x+ww,y);e=project(x+ww,y+hh)
        metrics.append({'text':label['text'],'role':label['role'],'min_body_height_px':round(min(abs(b[1]-a[1]),abs(e[1]-c[1])),2)})
    return layer,metrics,dst
READ=[]
for fov,yaw,case in [(50,0,'base'),(62,30,'stress')]:
    for s in SIGNS:
        for distance in [10,15,20]:
            layer,measure,quad=project_texture(s,distance,fov,yaw)
            bg=background('3-coast');im=Image.alpha_composite(bg,layer)
            text(im,(24,24),f'{distance}m / FOV {fov} / {yaw}도',24,PAPER,True)
            text(im,(24,490),'차량 기준 거리 · 투영 시험 / 실제 배치 아님',24,PAPER,True)
            name=f'readability/{s["id"]}-{case}-{distance}m.png';im.convert('RGB').save(OUT/name)
            READ.append({'id':s['id'],'case':case,'car_distance_m':distance,'camera_depth_m':distance+4.2,'vertical_fov_deg':fov,'yaw_deg':yaw,'path':name,'projected_quad_px':quad,'text_measurements':measure})
    board=Image.new('RGB',(1040,1490),PAPER);text(board,(24,20),'거리별 실제 픽셀 / '+('50도 정면' if case=='base' else '62도 · 30도 비스듬히'),24)
    for j,distance in enumerate([10,15,20]):text(board,(120+j*300,66),f'{distance}m',24)
    for i,s in enumerate(SIGNS):
        yy=112+i*190;text(board,(16,yy+50),f'{i+1:02d}',24)
        for j,distance in enumerate([10,15,20]):
            im=Image.open(OUT/f'readability/{s["id"]}-{case}-{distance}m.png').crop((590,130,890,280));board.paste(im,(92+j*304,yy))
    text(board,(24,1450),'100% 크롭. 전체 960x540 원본은 readability/. 확대해서 읽은 결과로 세지 않음.',24,font='p')
    board.save(OUT/('readability-'+case+'.png'))
write('readability.json',READ)

# Sheets with all actual pixel mockups.
def screen_sheet(items,name,title):
    rows=math.ceil(len(items)/2);sheet=Image.new('RGB',(1980,84+rows*594),PAPER);text(sheet,(24,20),title,36)
    for i,s in enumerate(items):
        x=20+i%2*980;y=88+i//2*594;text(sheet,(x,y),s['title'],24);sheet.paste(Image.open(OUT/s['path']),(x,y+34))
    sheet.save(OUT/name)
screen_sheet([s for s in SCREENS if s['path'].startswith('hud/')],'hud-sheet.png','갈 때 긴장 / 돌아오는 길의 여백')
screen_sheet([s for s in SCREENS if s['path'].startswith('arrival/')],'arrival-sheet.png','도착의 정점 / 세 가지 연출 후보')
compare=Image.new('RGB',(1920,610),PAPER);text(compare,(24,18),'기존 포장 / 공용 시계 한 자리로',36)
compare.paste(background('packing-timeleft').convert('RGB'),(0,70));compare.paste(Image.open(OUT/'hud/packing-time.png'),(960,70));compare.save(OUT/'packing-compare.png')

# MART elevation comparison uses a measured screen crop, not an engine sign replacement.
mart=next(s for s in SIGNS if s['id']=='mart');comp=Image.new('RGB',(1280,620),PAPER)
text(comp,(24,24),'MART / 현재 간판과 교체 원화',36)
source=background('1-start').crop((0,120,182,212));source.resize((728,368),Image.Resampling.NEAREST).convert('RGB').save(OUT/'reference/mart-crop@4x.png')
comp.paste(source.resize((546,276),Image.Resampling.NEAREST).convert('RGB'),(24,112))
new=Image.open(ROOT/mart['path']).crop((0,0,128,48)).resize((640,240),Image.Resampling.NEAREST);comp.paste(new,(616,112),new)
text(comp,(24,430),'현재 화면 일부 3배',24);text(comp,(616,430),'새 원본 5배 · 실제 폰트',24)
text(comp,(24,500),'바랜 칠 / 아래 남은 옛 도장색 / 나사 녹 / 새 배달 접수 종이',24)
text(comp,(24,550),'마모는 고정점과 물길에만. 입체 간판·씬 배치는 아트/프로그래밍 적용 필요.',24,font='p');comp.save(OUT/'mart-compare.png')

csvpath=ROOT/'art/provenance.csv'
with csvpath.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
prefix='course_ui_20261023_';rows=[r for r in rows if not r['asset_id'].startswith(prefix)]
for s in SIGNS:rows.append(dict(zip(fields,[prefix+s['id'],s['path'],'https://polyhaven.com/a/rusty_painted_metal ; https://ambientcg.com/view?id=CardboardSet001 ; https://github.com/quiple/galmuri','CC0-1.0; OFL-1.1; Project-authored','2026-10-23','photo_to_pixel.quantize_exact; course-ui/build.py; Pillow font','no',s['sha256'],'32px/m active UV, padded POT. '+s['wear']+'. Text from Galmuri11, no AI image input.'])))
with csvpath.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print(json.dumps({'signs':len(SIGNS),'screens':len(SCREENS),'distance_samples':len(READ),'source_photo_size':photo.size},ensure_ascii=False))
