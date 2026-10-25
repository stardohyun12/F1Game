"""PitCrew car v01: measured CC0 photos -> 64 texels/m -> shared-palette atlas.

No F1/Assets writes. Re-run with py -3.13 tools/asset-gen/car_texture.py.
Photo baseline and deliberately art-directed application are kept separately.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
from types import SimpleNamespace
import shutil
import urllib.request

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

from photo_to_pixel import convert, load_palette

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/source/car-v01'
MODEL = ROOT / 'art/models/car_v01'
REVIEW = ROOT / 'art/_review/2026-10-23-car'
PALETTE = ROOT / 'art/source/palette/pitcrew_d123_32.json'
FONT = ROOT / 'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf'
LICENSE = 'https://polyhaven.com/license'
DATE = '2026-10-23'
SIZE = 120
PHYSICAL = SIZE / 64
UA = {'User-Agent': 'PitCrew-car-art/1.0 (Powered by Poly Haven)'}

SPECS = [
    ('faded_red_main', 'rusty_metal_02', [15,28], [.08], '사진의 작은 도장 명도 변화 8%. 큰 카모 얼룩과 마모 픽셀 없음.'),
    ('red_worn_contact', 'rusty_metal_02', [15,28], [.08], '사이드포드 하단의 돌 튐/정비 접촉. local x54..73 y71..75 한 군데만.'),
    ('red_number_panel', 'rusty_metal_02', [15,28], [.08], '작은 실번호판. local [52,56,76,72), Galmuri11 07.'),
    ('exposed_dark_steel', 'rusty_metal_02', [9,5,12], [.08,.92], '하부 프레임 연결부 local x52..60 y70..73에만 녹.'),
    ('rubber_sidewall', 'rubberized_track', [6,2], [.94], '타이어 고무 소재 작은 명도와 외곽 몰드 압인 호. 중심64,64·반지름18px.'),
    ('rubber_tread', 'rubberized_track', [6,2,9], [.94,.995], '접지면 local(61,58)/(65,68)에 2px 마모만. 전체 줄 없음.'),
    ('orange_bag_main', 'rough_linen', [31,23], [.02], '원본 사진 미세 명도 2%; 중앙 작은 구김과 상단 봉제, 흙얼룩 없음.'),
    ('orange_bag_rear', 'rough_linen', [31,23], [.02], '후면 46x20px 중앙 사용 영역 가장자리 봉제/상단 지퍼. 큰 얼룩 없이 중앙 비움.'),
    ('orange_bag_lid', 'rough_linen', [31,23], [.02], '가방 뚜껑 접힘 두 개와 밝은 봉제. 스트랩 눌린 자리만 어두움.'),
    ('black_fabric_strap', 'rough_linen', [6,2,9], [.85,.98], '하중 받는 직물 끈. 단색 면 대신 실제 직물 면적 평균 명도.'),
    ('number_sticker', 'rough_linen', [0,25], [.02], 'local [52,56,76,72) 24x16px 실제 Galmuri11 숫자 07; 모서리 1px 들뜸.'),
    ('grey_repair_tape', 'rough_linen', [8,7,17], [.10,.93], '천 테이프 실물 질감. 실제 폭 8px=12.5cm의 패치에 쓸 작은 원천.'),
    ('cream_helmet', 'rusty_metal_02', [0,25], [.04], '헬멧 바랜 도장 명도. 무작위 칩 없음.'),
    ('dark_visor', 'rubberized_track', [6,2,9], [.90,.98], '반사 대신 무광 3색 저대비. 가짜 하이라이트 없음.'),
    ('dark_underbody', 'rusty_metal_02', [6,2,9], [.80,.96], '하부 오염은 재질 평균 명도만. 전체에 녹 뿌리지 않음.'),
    ('small_red_lamp', 'rusty_metal_02', [15,28], [.10], '작은 붉은 렌즈 영역. 팔레트 색 그대로, 발광 굽기 없음.'),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def dump(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')


def request(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60) as r:
        return r.read()


def source(source_id):
    folder = OUT / 'photos'
    folder.mkdir(parents=True, exist_ok=True)
    info_path, files_path = folder/(source_id+'.info.json'), folder/(source_id+'.files.json')
    legacy = ROOT/'art/source/hillhouse-surfaces/photos'
    for target in (info_path, files_path):
        if not target.exists():
            old = legacy/target.name
            if old.exists():
                shutil.copy2(old,target)
            else:
                part='info' if target==info_path else 'files'
                dump(target,json.loads(request('https://api.polyhaven.com/'+part+'/'+source_id)))
    info=json.loads(info_path.read_text('utf-8'))
    files=json.loads(files_path.read_text('utf-8'))
    key=next(k for k in files if k.lower().startswith(('diff','col')))
    file=files[key]['1k']['jpg']
    photo_path=folder/Path(file['url']).name
    if not photo_path.exists():
        old=legacy/photo_path.name
        if old.exists(): shutil.copy2(old,photo_path)
        else: photo_path.write_bytes(request(file['url']))
    return {'id':source_id,'path':photo_path.relative_to(ROOT).as_posix(),
            'sha256':sha(photo_path),'url':'https://polyhaven.com/a/'+source_id,
            'download_url':file['url'],'authors':info['authors'],
            'dimensions_mm':info['dimensions'],'license':'CC0-1.0',
            'license_url':LICENSE,'accessed':DATE,'ai_generated':False}


def physical_photo(rec):
    """Use source's declared metre dimensions; repeat sub-metre textiles, no enlarged weave."""
    im=Image.open(ROOT/rec['path']).convert('RGB')
    width_m,height_m=[float(v)/1000 for v in rec['dimensions_mm'][:2]]
    # 512px scratch image of a measured 1.875m square; final crop stays 120 texels.
    rw=max(1,round(512*width_m/PHYSICAL)); rh=max(1,round(512*height_m/PHYSICAL))
    scaled=im.resize((rw,rh),Image.Resampling.BOX)
    out=Image.new('RGB',(512,512))
    if rw>=512 and rh>=512:
        left=(rw-512)//2; top=(rh-512)//2
        out=scaled.crop((left,top,left+512,top+512))
    else:
        for y in range(0,512,rh):
            for x in range(0,512,rw):out.paste(scaled,(x,y))
    path=OUT/'physical'/(rec['id']+'.png');path.parent.mkdir(parents=True,exist_ok=True);out.save(path)
    return path,out


def pipeline(path, target, rec):
    return convert(SimpleNamespace(input=path,output=target,palette=PALETTE,
        width_m=PHYSICAL,height_m=PHYSICAL,density=64,dither='none',resample='box',
        source_id=rec['id'],source_url=rec['url'],license_url=LICENSE,
        crop=None,canvas_size=None,metadata=None))


def box_float(a,radius):
    """Float box filter prevents 8-bit quantile ties turning textile micrograin into snow."""
    k=radius*2+1
    p=np.pad(a,((radius,radius),(radius,radius)),mode='reflect')
    integral=np.pad(p,((1,0),(1,0))).cumsum(0).cumsum(1)
    return (integral[k:,k:]-integral[:-k,k:]-integral[k:,:-k]+integral[:-k,:-k])/(k*k)


def info_layer(name, image, colors):
    """Integer pixels only; no random wear, gradients, antialias, generated pseudo text."""
    d=ImageDraw.Draw(image)
    # Here coordinates are tile-local minus the 4px guard border.
    def line(points,color,width=1): d.line([(x-4,y-4) for x,y in points],fill=colors[color],width=width)
    def rect(box,color):d.rectangle(tuple(v-4 for v in box),fill=colors[color])
    def number():
        rect((52,56,75,71),25)
        line([(53,56),(64,56)],0)
        rect((52,70,53,71),15)
        mask=Image.new('1',image.size)
        md=ImageDraw.Draw(mask);font=ImageFont.truetype(str(FONT),11)
        bbox=md.textbbox((0,0),'07',font=font)
        w,h=bbox[2]-bbox[0],bbox[3]-bbox[1]
        md.text((60- w//2,60-h//2-bbox[1]),'07',font=font,fill=1)
        image.paste(colors[6],mask=mask)
    if name=='red_worn_contact':
        for p in [[(54,73),(57,73),(57,72),(60,72)],[(63,72),(67,72),(67,71),(70,71)],[(72,74),(73,74)]]:line(p,17)
        line([(55,74),(58,74)],15);line([(64,73),(65,73)],9)
    elif name=='red_number_panel':number()
    elif name=='number_sticker':number()
    elif name=='exposed_dark_steel':
        line([(52,72),(55,72),(55,71),(58,71)],15)
        line([(53,73),(54,73)],21)
        # Wheel rim shallow stamped metal, not a baked light/reflection.
        line([(54,53),(58,50),(64,49),(69,51)],7)
        line([(74,55),(77,59),(77,66),(75,69)],8)
        line([(70,76),(64,78),(58,76)],7)
        rect((60,62,62,64),7)
        rect((65,60,66,63),8)
    elif name=='rubber_sidewall':
        # Mould seam slightly raised on a tyre sidewall. Partial arcs, no invented letters.
        d.arc((42,42,78,78),start=210,end=302,fill=colors[9],width=1)
        d.arc((42,42,78,78),start=24,end=142,fill=colors[2],width=1)
        d.arc((44,44,76,76),start=38,end=114,fill=colors[2],width=1)
    elif name=='rubber_tread':
        rect((61,58,62,58),2)
        rect((65,68,66,68),9)
    elif name=='orange_bag_main':
        # A short soft fold, no frame around the whole box face.
        line([(47,56),(54,57),(60,57)],0)
        line([(48,57),(53,58),(57,58)],31)
        line([(79,68),(77,71),(74,72)],31)
        line([(80,68),(78,72)],0)
    elif name=='orange_bag_rear':
        # Readable inside the small rear panel crop [41,54,87,74).
        line([(42,55),(85,55)],22)
        line([(43,57),(82,57)],24)
        line([(46,58),(80,58)],23)
        rect((80,56,81,58),17)
        line([(42,58),(42,72),(85,72),(85,58)],22)
        line([(43,70),(47,71),(50,71)],29)
        for x in (45,52,59,66,73,80):line([(x,73),(x+1,73)],23)
        line([(46,54),(55,54),(60,55)],0)
        line([(44,64),(46,67),(47,69)],0)
        line([(82,64),(80,68),(78,69)],31)
    elif name=='orange_bag_lid':
        line([(47,60),(52,60),(56,61)],31)
        line([(74,67),(77,66),(81,66)],31)
        line([(48,59),(52,59),(57,60)],0)
        line([(73,66),(77,65),(80,65)],0)
        line([(43,47),(51,46),(76,46),(84,47)],0)
        line([(43,80),(52,81),(76,81),(84,80)],31)
    elif name=='grey_repair_tape':
        line([(60,56),(61,57),(61,62)],7)
        line([(65,64),(66,66)],17)
    return image


def main():
    OUT.mkdir(parents=True,exist_ok=True);MODEL.mkdir(parents=True,exist_ok=True);REVIEW.mkdir(parents=True,exist_ok=True)
    colors=load_palette(PALETTE)
    sources={sid:source(sid) for sid in sorted({s[1] for s in SPECS})}
    photo={sid:physical_photo(rec) for sid,rec in sources.items()}
    for sid,rec in sources.items():pipeline(photo[sid][0],OUT/'baseline'/(sid+'.png'),rec)
    atlas=Image.new('RGB',(512,512),colors[6]);records=[]
    for slot,(name,sid,ladder,thresholds,reason) in enumerate(SPECS):
        # Preserve source spatial structure while lowering contrast to the material's palette ladder.
        # Low pass is explicit: no synthetic full-surface noise, no enlargement of micro-weave.
        low=photo[sid][1].resize((120,120),Image.Resampling.BOX)
        lum=np.dot(np.asarray(low,dtype=np.float64),[.299,.587,.114])
        if name.startswith(('faded_red','red_','orange_bag','rubber_')) or name in ('cream_helmet','number_sticker'):
            # Detail-band from the actual photograph: reject broad stain areas.
            # The only extra marks below are physically located hand-layer features.
            lum=box_float(lum,1)-box_float(lum,5)
            filter_description='photo detail band: float 3x3 mean minus float 11x11 mean; no synthetic noise'
        else:
            lum=box_float(box_float(lum,3),3)
            filter_description='float 7x7 box filter twice at target density'
        cuts=[float(np.quantile(lum,t)) for t in thresholds]
        bands=np.digitize(lum,cuts,right=True)
        graded=Image.fromarray(np.asarray([colors[i] for i in ladder],dtype=np.uint8)[bands])
        guide=OUT/'graded'/(name+'.png');guide.parent.mkdir(parents=True,exist_ok=True)
        graded.resize((480,480),Image.Resampling.NEAREST).save(guide)
        target=OUT/'quantized'/(name+'.png');pipeline(guide,target,sources[sid])
        patch=info_layer(name,Image.open(target).convert('RGB'),colors)
        # Edge-extruded gutter contains palette entries, never a padding color outside the palette.
        padded=np.pad(np.asarray(patch),((4,4),(4,4),(0,0)),mode='edge')
        tile=Image.fromarray(padded);tilepath=OUT/'tiles'/(f'{slot:02d}_'+name+'.png')
        tilepath.parent.mkdir(parents=True,exist_ok=True);tile.save(tilepath)
        atlas.paste(tile,((slot%4)*128,(slot//4)*128))
        rgb,count=np.unique(np.asarray(tile).reshape(-1,3),axis=0,return_counts=True)
        records.append({'slot':slot,'name':name,'source_id':sid,'tile_path':tilepath.relative_to(ROOT).as_posix(),
            'atlas_rect_xywh':[(slot%4)*128,(slot//4)*128,128,128],
            'active_rect_local_xywh':[4,4,120,120],'active_physical_m':[PHYSICAL,PHYSICAL],
            'target_texels_per_m':64,'ladder_palette_indices':ladder,'luminance_quantiles':thresholds,
            'photo_processing':filter_description,'information_layer_reason_ko':reason,
            'used_colors':len(rgb),'dominant_fraction':float(max(count)/sum(count)),
            'sha256':sha(tilepath),'ai_generated':False})
    path=MODEL/'CarAtlas.png';atlas.save(path)
    rgb=np.asarray(atlas).reshape(-1,3);allowed=set(colors)
    outside=sum(tuple(x) not in allowed for x in rgb)
    assert outside==0 and all(r['used_colors']>=2 for r in records)
    contract={'revision':'R3: gate response, reduce broad stains, add functional cloth/tyre detail','atlas_size_px':[512,512],'grid_xy':[4,4],'slot_order':'top-left row-major PNG coordinates',
        'cell_size_px':128,'guard_px':4,'active_size_px':120,'density_px_m':64,
        'number_decal':{'slot':10,'local_rect_ltrb':[52,56,76,72],'size_m':[.375,.25],'text':'07','font':FONT.relative_to(ROOT).as_posix(),'font_size_px':11},
        'bag_rear_useful_crop':{'slot':7,'local_rect_ltrb':[41,54,87,74]},
        'blender_uv_formula':'u=(slot%4*128+local_x)/512; v=1-(slot//4*128+local_y)/512',
        'sampling':'Closest/Point; no mipmap in review; configure separately at Unity import',
        'palette':PALETTE.relative_to(ROOT).as_posix(),'palette_sha256':sha(PALETTE),
        'atlas_sha256':sha(path),'outside_palette_pixels':outside,'sources':list(sources.values()),'tiles':records}
    dump(OUT/'texture-contract.json',contract)
    dump(OUT/'texture-check.json',{'status':'PASS','revision':'R3','texture_size':[512,512],'power_of_two':True,'palette_outside':outside,
        'used_colors':len(set(tuple(c) for c in rgb)),'palette_size':len(colors),'all_tiles_at_least_two_colors':True,
        'metadata_density':64,'geometry_uv_density_check':'parent checker required; not inferred from PNG',
        'atlas_sha256':sha(path),'ai_generated':False})
    sheet=Image.new('RGB',(1160,1270),(32,31,28));draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype(str(FONT),11)
    draw.text((25,16),'CAR v01  /  CC0 PHOTO + SCRIPT PIXELS  /  64 px/m  /  SHARED 32 COLORS',font=font,fill=(228,214,191))
    for r in records:
        x=20+(r['slot']%4)*286;y=56+(r['slot']//4)*300
        tile=Image.open(ROOT/r['tile_path'])
        sheet.paste(tile.resize((256,256),Image.Resampling.NEAREST),(x,y))
        draw.rectangle((x+8,y+8,x+247,y+247),outline=(228,214,191))
        draw.text((x,y+262),f"{r['slot']:02d} {r['name']}",font=font,fill=(228,214,191))
    sheet.save(OUT/'texture-sheet.png');sheet.save(REVIEW/'texture-sheet.png')
    source_text='# Car v01 질감 출처\n\n확인일 2026-10-23. Powered by Poly Haven. 게임용 텍스처에 생성 AI 사용 없음.\n\n'
    source_text+='## 재현과 변경\n\n1. 제공 치수(mm)에 맞춰 1.875m 정사각 원본 영역을 구성했다. 작은 천 원본은 실측 크기를 유지하며 반복했다.\n2. 기존 photo_to_pixel.py를 그대로 호출해 BOX 120²·64px/m·공용 32색·디더 없음 기준판을 baseline/에 보존했다.\n3. 적용판은 사진 면적 평균에 float 7x7 박스 저역 필터를 두 번 적용하고 지정 명도 분위수로 소재별 2~3색 사다리에 대응시켰다. 무작위 노이즈를 추가하지 않았다. 원본 사진 재현 결과와 같은 결과라고 부르지 않는다.\n4. 의미가 있는 손 픽셀 층(번호, 봉제, 지퍼, 접힘, 접촉 마모)을 넣고 4px 가장자리 확장 후 512²로 묶었다. 마모 위치/이유는 texture-contract.json 각 타일에 기록했다.\n5. 사진은 표면 바탕만 사용했다. 원본 노멀/러프니스/PBR 맵은 사용하지 않았다. 차체 곡면/회전축/조명/UV 밀도는 모델 담당 별도 검사다.\n6. 초판 rusty_painted_metal 철판의 세로 무늬가 과해 평판 rusty_metal_02로 교체했다. 8비트 저역 평균은 린넨 분위수 동률 때문에 눈 같은 잔점을 만들어 float 필터로 고쳤다. 탈락 원천/기준판은 보존했으며 최종 atlas에는 쓰지 않았다.\n\n'
    source_text=source_text.replace('3. 적용판은 사진 면적 평균에 float 7x7','3. R2 적용판은 사진 면적 평균에 float 7x7')
    source_text+='## R3 독립 게이트 대응\n\n- R2의 큰 얼룩은 납작한 카모처럼 읽혔다. 차체·가방·고무·헬멧은 실제 사진의 3x3 float 평균에서 11x11 평균을 뺀 detail-band를 색 사다리에 대응시켜 넓은 얼룩만 억제했다. 무작위 수, 합성 노이즈, 가짜 글자는 사용하지 않았다.\n- 도장 주색 #8A4631은 유지하고 #795244 명도 변화 면적을 약 8%로 줄였다. 마모·금속 노출은 지정 하단 접촉부에만 남겼다.\n- 천 주색 #B89360은 유지하고 사진 변화는 2%로 줄였다. 크림 #C4A687 짧은 접힘, 봉제, 지퍼로 천 표면을 구분했다.\n- 고무 측면은 중앙64,64/반지름18px의 끊긴 몰드 압인 호, 접지면은 위치가 정해진 2px 마모 두 곳, 림은 중앙30px 범위의 눌러 찍은 회색 가장자리를 스크립트 픽셀로 그렸다.\n- R2 atlas/시트/계약/스크립트는 r2/에 보존했다. R3 시트에서 직접 확인했으며 최종 모델 조명/크롭 판정은 별도다.\n\n'
    for rec in sources.values():
        source_text+=f"## {rec['id']}\n\n- 원천: {rec['url']}\n- 저자: {json.dumps(rec['authors'],ensure_ascii=False)}\n- CC0: {LICENSE}\n- 공개 제공 치수(mm): {rec['dimensions_mm']}\n- 실사 diffuse: `{rec['path']}`\n- SHA256: `{rec['sha256']}`\n- 다운로드: {rec['download_url']}\n\n"
    source_text+='## 소재의 한계\n\nRusty Metal 02는 실제 도장 금속 사진이며 자동차 페인트 실측 샘플은 아니다. 명도 분포만 차체에 옮겼고 사진의 전체 녹 분포는 바랜 도장 2색으로 낮췄다. Rubberized Track은 고무 트랙 사진으로 타이어 재질의 저대비 바탕만 사용했다. Rough Linen은 린넨 사진으로 보온가방의 직물 인상을 위한 바탕이며 실제 단열 가방 촬영본은 아니다.\n\n## 글자\n\n- `07`: Galmuri11 실제 글리프, 11px, 이진 마스크로 팔레트 잉크 인쇄.\n- 폰트: `art/_review/2026-10-23-d123/fonts/Galmuri11.ttf`\n- OFL-1.1: `art/_review/2026-10-23-d123/fonts/LICENSE.txt`\n- 원화는 색 팔레트/방향 참조만. 원화 픽셀을 게임 텍스처로 복사하지 않음.\n'
    (OUT/'SOURCES.md').write_text(source_text,encoding='utf-8')
    rows=[]
    for rec in sources.values():rows.append({'asset_id':'car_v01_source_'+rec['id'],'path':rec['path'],'source_url':rec['url'],'license':'CC0-1.0','accessed':DATE,'tool':'Poly Haven download','ai_generated':'아니오','sha256':rec['sha256'],'notes':'Source photo; public dimensions in metadata; '+LICENSE})
    rows.append({'asset_id':'car_v01_atlas','path':path.relative_to(ROOT).as_posix(),'source_url':'; '.join(r['url'] for r in sources.values()),'license':'CC0-1.0; Galmuri OFL-1.1','accessed':DATE,'tool':'car_texture.py + photo_to_pixel.py','ai_generated':'아니오','sha256':sha(path),'notes':'512 atlas; 16x128 cells, 4px gutters; 64px/m; reasons and UV contract in art/source/car-v01/texture-contract.json'})
    dump(OUT/'provenance-rows.json',rows)
    print(json.dumps({'atlas':str(path),'outside_palette':outside,'used_colors':len(set(tuple(c) for c in rgb)),'tiles':len(records),'hash':sha(path)}))


if __name__=='__main__': main()
