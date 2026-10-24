"""D127/D128: CC0 photographs -> photo_to_pixel -> append-only 64px cells.

Run with Python 3.13; Pillow and numpy are used. Never writes inside F1/.
Powered by Poly Haven: https://polyhaven.com / https://api.polyhaven.com
"""
from __future__ import annotations
import argparse, ast, hashlib, json, math, sys, urllib.request
from pathlib import Path
from types import SimpleNamespace
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageFilter
from photo_to_pixel import convert, load_palette

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/source/hillhouse-surfaces'
REVIEW = ROOT / 'art/_review/2026-10-23-hillhouse/surfaces'
PALETTE = ROOT / 'art/source/palette/pitcrew_d123_32.json'
LICENSE = 'https://polyhaven.com/license'
FONT = ROOT / 'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf'
DATE = '2026-10-23'
SPECS = [
 ('wall_repaint', '덧칠한 벽', 'worn_plaster_wall', [4,0,19,25], [.015,.05,.20], '상단은 밝은 새 회벽, 아래 0.65m 이내의 한쪽 옛 도료 경계에만 사진 기반 박락. 윗벽 UV는 PNG y4..32, 낮은 벽은 y34..62 사용.'),
 ('wood_handworn', '손때 탄 문틀', 'wood_peeling_paint_weathered', [4,5,18,19], [.05,.25,.98], '회청색 재도장 아래 목재가 드러남. 손잡이 높이 접촉부와 하단 물튐 부위만 추가 마모.'),
 ('roof_repair', '기와 수리 바탕', 'clay_roof_tiles', [15,28,17], [.30,.95], '실제 곡면 기와 사진으로 교체. 적갈색 2색 큰 명암과 사진에서 연속된 세로 하이라이트 5개만 회갈색으로 유지. 노랑 점 없음. 넓은 수리는 모델 한 위치에만 배치.'),
 ('paver_worn', '닳은 보도블록', 'rectangular_paving', [7,17,4], [.20,.86], '사진 속 20×40cm 포장 블록의 실제 줄눈을 1px로 보강. 중앙 보행 접촉부만 밝게 닳고 잔점은 제거.'),
 ('layered_sign', '간판 위 간판', 'wood_peeling_paint_weathered', [21,4,16,0], [.09,.40,.88], '이전 상회 간판의 하단 글자와 못 구멍을 남긴 채 작은 수선 간판을 덧댐. Galmuri11 실제 글리프.'),
 ('asphalt_patch', '도로 수리', 'worn_asphalt', [5,8,7], [.13,.96], '구멍을 메운 국소 불규칙 패치. 직선 띠를 없애고 원천 사진 골재의 큰 평균값만 회색 3색으로 유지.'),
 ('foundation_repair', '기단 모르타르 수리', 'stone_brick_wall_001', [12,7,17,4], [.10,.76,.97], '사진 속 석재 블록·줄눈을 회갈색의 좁은 명도 사다리로 읽힘. 한쪽의 짧은 이음만 새 모르타르로 보수.'),
 ('faded_cloth', '바랜 생활 천', 'fabric_pattern_05', [18,19,25], [.20,.82], 'CC0 면직물 사진의 직조를 가장자리 봉제에만 남기고 중앙은 바랜 밝은 면. 두 접힘선만 보강해 빨래가 너덜거리지 않게 절제.'),
]

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def save_json(path, obj):
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    Path(path).write_text(json.dumps(obj, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
def request_json(url):
    req = urllib.request.Request(url, headers={'User-Agent':'PitCrew-art-surface-pipeline/1.0 (Powered by Poly Haven)'})
    with urllib.request.urlopen(req, timeout=45) as response: return json.load(response)

def download_source(source_id):
    folder = OUT/'photos'; folder.mkdir(parents=True,exist_ok=True)
    info_path=folder/(source_id+'.info.json'); files_path=folder/(source_id+'.files.json')
    info=json.loads(info_path.read_text('utf-8')) if info_path.exists() else request_json('https://api.polyhaven.com/info/'+source_id)
    files=json.loads(files_path.read_text('utf-8')) if files_path.exists() else request_json('https://api.polyhaven.com/files/'+source_id)
    save_json(info_path,info); save_json(files_path,files)
    diffuse_key=next((k for k in files if k.lower() in ('diff','diffuse','albedo')), 'col_01')
    data=files[diffuse_key]['1k']['jpg']; url=data['url']
    path=folder/Path(url).name
    if not path.exists():
        req=urllib.request.Request(url,headers={'User-Agent':'PitCrew-art-surface-pipeline/1.0'})
        with urllib.request.urlopen(req,timeout=90) as response: path.write_bytes(response.read())
    if hashlib.md5(path.read_bytes()).hexdigest()!=data['md5']: raise ValueError('Source MD5 mismatch: '+source_id)
    return {'id':source_id,'path':path.relative_to(ROOT).as_posix(),'url':'https://polyhaven.com/a/'+source_id,
            'download_url':url,'authors':info['authors'],'dimensions_mm':info.get('dimensions'),
            'license':'CC0-1.0','license_url':LICENSE,'sha256':sha(path),'md5_matches_provider':True}

def prepare_photo(source, outpath):
    """Reproduce a 2m square from the provider's millimetre dimensions.

    Diffuse source maps are seamless scans. Their pixels are repeated at original
    physical scale before a common 1024px photographic working crop is saved.
    Only the final photo_to_pixel step reduces to 64px and quantizes.
    """
    photo=Image.open(ROOT/source['path']).convert('RGB')
    physical=source['dimensions_mm']
    if len(physical)<2 or min(physical[:2])<=0: raise ValueError('No source dimensions')
    tile=(max(1,round(physical[0]/2000*1024)),max(1,round(physical[1]/2000*1024)))
    scaled=photo.resize(tile, Image.Resampling.LANCZOS)
    canvas=Image.new('RGB',(1024,1024))
    for y in range(0,1024,tile[1]):
        for x in range(0,1024,tile[0]): canvas.paste(scaled,(x,y))
    canvas.save(outpath)
    return canvas

def graded(photo, ladder, quantiles, colors, name):
    # Photographic luminance drives broad palette bands; no random grain layer.
    # Expose at final texel footprint, then nearest expand the exposure guide.
    # This stops subpixel photographic weave cancelling all useful value steps.
    # The raw 2m photo is separately run through the unmodified pipeline below.
    exposure=photo.resize((64,64),Image.Resampling.BOX).convert('L')
    if name=='wood_handworn': exposure=exposure.filter(ImageFilter.MedianFilter(5))
    if name in ('paver_worn','foundation_repair','asphalt_patch'):
        exposure=exposure.filter(ImageFilter.MedianFilter(3))
    gray=np.asarray(exposure,dtype=np.float32)
    if name=='roof_repair':
        # Keep only long photographed curvature highlights; isolated glints vanish.
        rgb=np.empty((64,64,3),dtype=np.uint8);rgb[:]=colors[28]
        dark=Image.fromarray(np.uint8(gray<np.quantile(gray,.30))*255).filter(ImageFilter.MedianFilter(3))
        rgb[np.asarray(dark)>127]=colors[15]
        bright=gray>np.quantile(gray,.66)
        runs=[]
        for xx in range(2,62):
            start=None
            for yy in range(65):
                if yy<64 and bright[yy,xx]:
                    if start is None:start=yy
                elif start is not None:
                    if yy-start>=5:runs.append((yy-start,xx,start,yy))
                    start=None
        chosen=[]
        for length,xx,start,end in sorted(runs,reverse=True):
            if all(abs(xx-other)>6 for other in chosen):
                rgb[start:end,xx]=colors[17];chosen.append(xx)
                if len(chosen)>=5:break
        return Image.fromarray(rgb).resize(photo.size,Image.Resampling.NEAREST)
    lo,hi=np.percentile(gray,[2,98]); gray=np.clip((gray-lo)/max(hi-lo,1),0,1)
    thresholds=np.quantile(gray,np.array(quantiles))
    bands=np.digitize(gray,thresholds)
    rgb=np.asarray([colors[i] for i in ladder],dtype=np.uint8)[bands]
    # Keep exact photo contours; 64px reduction/quantization occurs later.
    return Image.fromarray(rgb).resize(photo.size,Image.Resampling.NEAREST)

def details(name, image, colors):
    d=ImageDraw.Draw(image)
    d.fontmode='1'
    c=lambda i:colors[i]
    if name=='wall_repaint':
        # Repainted clean upper wall, exposed older coat only at the wet base.
        raw=np.asarray(image).copy(); old=np.empty_like(raw); old[:]=c(0)
        old[np.all(raw==np.asarray(c(4)),axis=2)]=c(4)
        old[np.all(raw==np.asarray(c(0)),axis=2)]=c(4)
        mask=Image.new('1',(64,64));m=ImageDraw.Draw(mask)
        m.polygon([(0,42),(10,43),(18,47),(26,46),(34,50),(42,51),(48,55),(56,54),(63,57),(63,63),(0,63)],fill=1)
        image=Image.new('RGB',(64,64),c(25));image.paste(Image.fromarray(old),(0,0),mask)
    elif name=='wood_handworn':
        # A repainted door keeps weathering at the jamb/handle and kick edge.
        # Retain source-photo contours there; the protected central field is paint.
        a=np.asarray(image).copy()
        for yy in range(5,53):
            for xx in range(6,48):
                if tuple(a[yy,xx])!=c(18): a[yy,xx]=c(18)
        image=Image.fromarray(a);d=ImageDraw.Draw(image)
        # Narrow worn strip around a handle/door jamb; UV can choose this strip.
        d.polygon([(52,23),(55,24),(54,31),(57,34),(55,39),(51,38),(52,32),(50,29)],fill=c(4))
        d.line([(53,25),(53,30),(55,32)],fill=c(0))
        d.polygon([(0,59),(12,61),(21,60),(24,63),(0,63)],fill=c(4))
    elif name=='roof_repair':
        # Curved scan already contains actual tile edges; no artificial stripe grid.
        pass
    elif name=='paver_worn':
        # Source photo: ten columns / five rows per measured two-metre square.
        for xx in (0,6,13,19,26,32,38,45,51,58,63): d.line([(xx,0),(xx,63)],fill=c(7))
        for yy in (0,13,26,38,51,63): d.line([(0,yy),(63,yy)],fill=c(7))
        for x,y in [(15,16),(27,28),(40,40)]: d.line([(x,y),(x+2,y)],fill=c(4))
    elif name=='layered_sign':
        font=ImageFont.truetype(str(FONT),11)
        d.rectangle((3,15,60,52),fill=c(4))
        d.text((10,34),'마을상회',font=font,fill=c(0),stroke_width=0)
        d.line([(4,51),(45,51)],fill=c(21))
        d.rectangle((8,13,57,37),fill=c(18))
        d.text((20,18),'수선',font=font,fill=c(25),stroke_width=0)
        d.line([(9,36),(38,36),(40,35),(57,35)],fill=c(5))
        for p in [(11,16),(54,16),(11,34),(54,34)]: d.point(p,fill=c(2))
        d.rectangle((3,53,8,54),fill=c(21));d.rectangle((54,53,59,54),fill=c(21))
    elif name=='asphalt_patch':
        # Old pothole repair, rounded by wear; no wooden-board silhouette.
        mask=Image.new('1',(64,64));m=ImageDraw.Draw(mask)
        outline=[(7,22),(15,16),(29,18),(34,23),(48,22),(54,31),(51,41),(54,48),(43,51),(28,48),(20,53),(8,46),(11,35)]
        m.polygon(outline,fill=1)
        a=np.asarray(image).copy(); lum=np.mean(a,axis=2)
        patch=np.asarray([c(8) if v>102 else c(5) for v in lum.ravel()],dtype=np.uint8).reshape(64,64,3)
        image.paste(Image.fromarray(patch),(0,0),mask)
        d=ImageDraw.Draw(image);d.line(outline+[outline[0]],fill=c(5),width=1)
    elif name=='foundation_repair':
        # Boundaries traced from the provider's 2m photographic crop, /16 pixels.
        # Average each real block, so bright mineral grain cannot become camouflage.
        photograph=image.copy();image=Image.new('RGB',(64,64),c(17));d=ImageDraw.Draw(image)
        blocks=[(0,0,10,8),(10,0,17,8),(17,0,24,8),(25,0,55,5),(55,0,64,5),
                (0,9,4,16),(4,9,24,16),(25,6,29,16),(30,6,44,16),(45,6,64,20),
                (0,17,11,27),(12,17,15,33),(16,17,36,27),(37,17,44,20),
                (37,21,54,27),(55,21,64,27),(0,28,4,35),(5,28,11,33),
                (16,28,27,33),(28,28,60,43),(61,28,64,46),(13,34,27,43),
                (0,36,11,53),(12,44,36,53),(37,44,52,58),(53,44,64,53),
                (0,54,3,64),(4,54,31,63),(32,54,36,58),(37,59,60,64),(53,54,64,58)]
        for x0,y0,x1,y1 in blocks:
            sample=np.asarray(photograph.crop((x0,y0,min(x1,64),min(y1,64))),dtype=float)
            tone=12 if sample.mean()<110 else 7
            d.polygon([(x0+1,y0),(x1-1,y0),(x1,y0+1),(x1,y1-1),(x1-1,y1),(x0+1,y1),(x0,y1-1),(x0,y0+1)],fill=c(tone))
            if x1-x0>6 and y1-y0>5:
                d.line([(x0+2,y1-1),(x1-2,y1-1)],fill=c(12))
            d.rectangle((x0,y0,x1,y1),outline=c(17),width=1)
        d.line([(46,43),(58,43),(60,45),(60,50)],fill=c(4),width=1)
    elif name=='faded_cloth':
        a=np.asarray(image).copy()
        a[np.all(a==np.asarray(c(18)),axis=2)]=c(19)
        image=Image.fromarray(a);d=ImageDraw.Draw(image)
        d.rectangle((3,3,60,60),fill=c(25))
        d.line([(3,3),(3,60),(60,60)],fill=c(19))
        d.line([(22,9),(23,22),(21,38),(22,52)],fill=c(19))
        d.line([(45,24),(44,37),(45,54)],fill=c(19))
    return image

def font(size=16):
    return ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',size)

def build():
    OUT.mkdir(parents=True,exist_ok=True); REVIEW.mkdir(parents=True,exist_ok=True)
    for folder in ('prepared','baseline','quantized','tiles'): (OUT/folder).mkdir(exist_ok=True)
    colors=load_palette(PALETTE)
    tree=ast.parse((ROOT/'tools/asset-gen/lofi_pilot.py').read_text('utf-8'))
    pal=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='pal' for t in n.targets))
    if len(pal)!=51: raise ValueError('Legacy tile count changed; allocate explicitly')
    source_ids=list(dict.fromkeys(s[2] for s in SPECS))
    sources={s:download_source(s) for s in source_ids}
    source_records=list(sources.values()); save_json(OUT/'sources.json',source_records)
    extension=Image.new('RGB',(512,512),colors[25]);legacy_path=ROOT/'F1/Assets/Art/LofiPilot/LofiAtlas.png'
    legacy=Image.open(legacy_path).convert('RGBA'); merged=legacy.copy()
    records=[]; rows=[]
    for offset,(name,label,source_id,ladder,quantiles,reason) in enumerate(SPECS):
        idx=51+offset;source=sources[source_id]
        physical_path=OUT/'prepared'/(name+'_2m-photo.png')
        photo=prepare_photo(source,physical_path)
        convert(SimpleNamespace(input=physical_path,output=OUT/'baseline'/(name+'.png'),palette=PALETTE,width_m=2,height_m=2,density=32,dither='none',resample='box',source_id=source_id,source_url=source['url'],license_url=LICENSE,crop=None,canvas_size=None,metadata=None))
        grade_path=OUT/'prepared'/(name+'_graded-photo.png')
        graded(photo,ladder,quantiles,colors,name).save(grade_path)
        quantized=OUT/'quantized'/(name+'.png')
        meta=convert(SimpleNamespace(input=grade_path,output=quantized,palette=PALETTE,width_m=2,height_m=2,density=32,dither='none',resample='box',source_id=source_id,source_url=source['url'],license_url=LICENSE,crop=None,canvas_size=None,metadata=None))
        tile=details(name,Image.open(quantized).convert('RGB'),colors)
        path=OUT/'tiles'/(name+'.png');tile.save(path)
        x=(idx%8)*64;y=(7-idx//8)*64
        extension.paste(tile,(x,y));merged.paste(tile.convert('RGBA'),(x,y))
        values=set(tile.getdata());outside=values-set(colors)
        if outside: raise ValueError('Outside palette: '+name)
        entry={'name':name,'label_ko':label,'index':idx,'path':path.relative_to(ROOT).as_posix(),'atlas_xy_topdown':[x,y],
            'blender_cell_xy':[idx%8,idx//8],'tile_size_px':[64,64],'physical_size_m':[2,2],'density_px_m':32,
            'sampling':'Point/Closest','uv_recommended_inset_px':2,'usable_extent_m_with_inset':1.875,
            'colors_rgb':sorted([list(v) for v in values]),'palette_outside_count':len(outside),'color_count':len(values),
            'dominant_color_fraction':max(n for n,_ in tile.getcolors())/4096,'sha256':sha(path),
            'source_id':source_id,'source_url':source['url'],'source_photo_sha256':source['sha256'],
            'prepared_photo_sha256':sha(physical_path),'graded_photo_sha256':sha(grade_path),
            'baseline_pipeline_metadata':(OUT/'baseline'/(name+'.json')).relative_to(ROOT).as_posix(),
            'grade_method':'BOX 64px footprint luminance percentiles -> material ladder -> nearest-expanded exposure guide -> photo_to_pixel; raw photo baseline also archived',
            'photo_to_pixel_metadata':quantized.with_suffix('.json').relative_to(ROOT).as_posix(),
            'color_ladder_indices':ladder,'wear_reason_ko':reason,'ai_generated':False}
        if name=='wood_handworn': entry['handwear_region_topdown_xywh']=[50,23,8,17]
        records.append(entry)
        rows.append({'asset_id':'hill_surface_'+name+'_v01','path':entry['path'],'source_url':source['url'],
            'license':'CC0-1.0'+('; Galmuri OFL-1.1' if name=='layered_sign' else ''),'accessed':DATE,
            'tool':'hill_surfaces.py; photo_to_pixel.py; Pillow','ai_generated':'no','sha256':entry['sha256'],'notes':reason})
    extension.save(OUT/'extension_atlas.png');merged.save(OUT/'merged_preview.png')
    original=np.asarray(legacy);after=np.asarray(merged);reserved=np.zeros((512,512),dtype=bool)
    for rec in records:
        x,y=rec['atlas_xy_topdown'];reserved[y:y+64,x:x+64]=True
    unchanged=bool(np.array_equal(original[~reserved],after[~reserved]))
    if not unchanged: raise ValueError('Legacy atlas altered outside new slots')
    checks={'pass':True,'legacy_original_sha256':sha(legacy_path),'legacy_file_untouched':True,
        'original_pixels_unchanged_outside_51_58':unchanged,'append_slots':list(range(51,59)),
        'new_tiles':len(records),'new_tile_size_px':[64,64],'atlas_size_px':[512,512],
        'new_tiles_palette_outside_count':0,'extension_palette_outside_count':len(set(extension.getdata())-set(colors)),
        'legacy_palette_claim':'Existing 0..50 cells retain legacy colors; shared32 check applies to extension only, not merged preview.',
        'source_md5_all_match_provider':all(s['md5_matches_provider'] for s in source_records),
        'density_px_m':[32,32],'density_deviation':0,'ai_generated_asset_files':0}
    save_json(OUT/'tiles.json',{'atlas':'extension_atlas.png','tiles':records,'checks':checks})
    save_json(OUT/'checks.json',checks)
    rows.append({'asset_id':'hillhouse_surface_extension_atlas_v01','path':(OUT/'extension_atlas.png').relative_to(ROOT).as_posix(),
        'source_url':'; '.join(s['url'] for s in source_records),'license':'CC0-1.0; Galmuri OFL-1.1',
        'accessed':DATE,'tool':'hill_surfaces.py; photo_to_pixel.py; Pillow','ai_generated':'no','sha256':sha(OUT/'extension_atlas.png'),
        'notes':'512x512 append-only extension atlas; new slots51-58; all32palette; source hashes in sources.json.'})
    save_json(OUT/'provenance-rows.json',rows)
    license_text=(ROOT/'art/_review/2026-10-23-d123/fonts/OFL.txt')
    # Font already checked in repository; locate its license without changing it.
    candidates=list(FONT.parent.glob('*OFL*'))+list(FONT.parent.glob('*LICENSE*'))
    source_text='# 언덕집 표면 사진 출처\n\n확인일: '+DATE+' · Powered by Poly Haven.\n\n'+\
        '공식 라이선스: https://polyhaven.com/license (CC0-1.0). 원본 사진·확산색 맵은 아래 저자 자료이며, 생성 AI는 사용하지 않았다.\n\n'+\
        '원본의 제공 치수(mm)를 사용해 2m×2m 사진 영역을 반복/크롭 → 기존 `photo_to_pixel.py` BOX 64×64, 32px/m, 디더 없음으로 원본 조건의 기준판(baseline/)을 먼저 보존했다.\n\n'+\
        '적용판은 같은 사진의 64px BOX 명도 면적 평균을 소재별 색 사다리로 재노출한 뒤 photo_to_pixel에 다시 넣었다. 서브픽셀 직조가 단일 중간색으로 뭉개지는 문제를 해결하기 위한 명시적인 변경이다. 원본 사진과 같은 결과라고 부르지 않는다. 이후 위치에 이유가 있는 마모·Galmuri 글자만 픽셀로 덧썼다.\n\n'+\
        '원본 사진보다 무늬를 키우지 않았다. 천 직조처럼 1픽셀보다 작은 무늬는 자연스럽게 소실된다. 첫 rough_linen 원천은 32px/m에서 단색이 되어 보관만 하고, 실제 체크 면직물 fabric_pattern_05로 바꿨다. 사진 캡처 현장·원본 RAW는 직접 확인하지 않았으며 공개 diffuse map·reference 사진·저자 메타데이터에 근거했다.\n\n'+\
        'R2 변경: 기단은 stone_brick_wall_001 사진의 실물 블록 경계를 추적하고 각 블록의 평균값으로 면을 정리했다. 보도는 사진의 20×40cm 줄눈만 보강했다. 지붕은 clay_roof_tiles 사진의 5px 이상 세로 명암 대역을 최대5개 보존하고 노랑 잔점을 제거했다. 전체 사진 위 임의 랜덤 노이즈를 더하지 않았다. 상세는 art/_review/2026-10-23-hillhouse/surfaces/r2-changes.md.\n\n'
    for s in source_records:
        source_text+='## '+s['id']+'\n\n- URL: '+s['url']+'\n- 파일: `'+s['path']+'`\n- 저자: '+json.dumps(s['authors'],ensure_ascii=False)+'\n- 제공 치수(mm): '+str(s['dimensions_mm'])+'\n- SHA256: `'+s['sha256']+'`\n- 라이선스: CC0-1.0, '+LICENSE+'\n\n'
    source_text+='## 글자\n\n- Galmuri11: `'+FONT.relative_to(ROOT).as_posix()+'`, OFL-1.1. 실제 글리프만 사용.\n- 라이선스 파일: '+', '.join('`'+p.relative_to(ROOT).as_posix()+'`' for p in candidates)+'\n\n## 변경 범위\n\n기존 `lofi_pilot.py`·`F1/Assets/Art/LofiPilot/LofiAtlas.png`는 읽기만 수행. 셀 51~58에만 붙인 `merged_preview.png`에서 다른 픽셀이 같음을 검사했다. 실제 적용은 프로그래밍팀. 이전 셀 색은 공용32색 규격이라고 주장하지 않는다. 신규 확장 아틀라스만 32색 규격이다.\n'
    (OUT/'SOURCES.md').write_text(source_text,encoding='utf-8')
    # Review only: nearest-neighbor enlarged tile sheet and source-to-tile proof.
    sheet=Image.new('RGB',(1440,920),(230,226,214));d=ImageDraw.Draw(sheet)
    d.text((30,20),'D-128 | 오래 산 흔적 · 64px 표면 8종',font=font(28),fill=(48,54,47))
    d.text((30,63),'64×64 = 2m×2m / 32px·m / Point / 공용 32색 / 새 슬롯 51–58',font=font(18),fill=(82,83,80))
    for i,rec in enumerate(records):
        x=30+(i%4)*355;y=108+(i//4)*398
        tile=Image.open(ROOT/rec['path']);sheet.paste(tile.resize((256,256),Image.Resampling.NEAREST),(x,y))
        d.text((x,y+268),str(rec['index'])+'  '+rec['label_ko'],font=font(20),fill=(48,54,47))
        d.text((x,y+300),rec['name'],font=font(15),fill=(82,83,80))
        d.text((x,y+323),'사진 → BOX64 → 팔레트 → 국소 마모',font=font(14),fill=(82,83,80))
    sheet.save(REVIEW/'surface-sheet.png')
    proof=Image.new('RGB',(1440,950),(230,226,214));d=ImageDraw.Draw(proof)
    d.text((30,18),'CC0 원천 / 물리 스케일 / 최종 픽셀',font=font(26),fill=(48,54,47))
    for i,rec in enumerate(records):
        x=28+(i%4)*355;y=77+(i//4)*430
        photo=Image.open(ROOT/sources[rec['source_id']]['path']).convert('RGB')
        proof.paste(photo.resize((145,145),Image.Resampling.LANCZOS),(x,y))
        tile=Image.open(ROOT/rec['path']);proof.paste(tile.resize((160,160),Image.Resampling.NEAREST),(x+165,y))
        proof.paste(tile.resize((256,256),Image.Resampling.NEAREST),(x,y+172))
        d.text((x+165,y+149),str(rec['index'])+' · 32px/m',font=font(14),fill=(48,54,47))
    proof.save(REVIEW/'source-proof.png')
    (REVIEW/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>D128 surfaces</title><style>body{background:#e6e2d6;color:#30362f;font:18px sans-serif;max-width:1440px;margin:auto}img{width:100%;image-rendering:pixelated}a{color:#52656a}</style><h1>세월의 흔적 · 표면 8종</h1><p>64px / 32px·m / CC0 사진 / 도현 승인 필요</p><img src="surface-sheet.png"><img src="source-proof.png"><p><a href="../../../source/hillhouse-surfaces/SOURCES.md">출처</a></p>',encoding='utf-8')
    print(json.dumps(checks,ensure_ascii=False,indent=2))

if __name__=='__main__': build()
