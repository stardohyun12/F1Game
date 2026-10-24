"""A1: measured photo crops, preserved baseline, restrained photo-led colour correction.

No generated noise, synthetic material generator, or AI imagery. Final texels
come from the measured photo, colour mapping, artist-traced existing joints,
and a 3-pixel seam correction.
"""
import argparse
import csv
import hashlib
import json
from pathlib import Path
from types import SimpleNamespace
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from photo_to_pixel import convert, load_palette, quantize_exact

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/textures/2026-10-24-kp'
PAL = ROOT / 'art/source/palette/pitcrew_d123_32.json'
KEYS = ['wood', 'stone', 'plaster', 'roof']
LABELS = ['교토 · 세로 목재', '교토 · 돌포장', '포지타노 · 회벽', '포지타노 · 기와']

def rel(p): return Path(p).relative_to(ROOT).as_posix()
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def save_json(p, d): Path(p).write_text(json.dumps(d, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

def seam_correct(image, band=3):
    """Equal opposing boundary colours, retaining inner photo structure."""
    a = np.asarray(image).astype(float).copy()
    for axis in [1, 0]:
        b = np.swapaxes(a, 0, axis)
        for i in range(band):
            mean = (b[i].copy()+b[-1-i].copy())/2
            weight = (1-i/band)**2
            b[i] = b[i]*(1-weight)+mean*weight
            b[-1-i] = b[-1-i]*(1-weight)+mean*weight
        a = np.swapaxes(b, 0, axis)
    return Image.fromarray(np.clip(a, 0, 255).astype('uint8'))

def grade(key, small, colors):
    a = np.asarray(small).astype(float)
    lum = a @ [0.2126, 0.7152, 0.0722]
    if key == 'wood':
        # Read the real grain in a brown ladder; avoid the charcoal/rust ambiguity.
        selected = [2,21,24,15,22]
        thresholds=np.percentile(lum,[4,18,55,88])
        band=np.digitize(lum,thresholds)
        result=np.array([colors[i] for i in selected],dtype=float)[band]
        process = 'photo luminance ranks 4/18/55/88 -> wood brown ladder 2,21,24,15,22; real grain and board gaps retained'
    elif key == 'stone':
        # A grey ladder, without inventing new colours outside the common palette.
        lo, hi = np.percentile(lum, [2, 98])
        tone = np.clip((lum-lo)/(hi-lo), 0, 1)
        dark = np.array(colors[5]); light = np.array(colors[7])
        result = dark[None,None,:]+tone[:,:,None]*(light-dark)
        selected = [5,8,7,10,11,12,20]
        process = 'photo luminance 2..98 percentile -> close neutral-grey ladder 5,8,7,10,11,12,20; no random noise'
    elif key == 'plaster':
        # The lightest existing cream and a subdued grey retain plaster pores.
        # Source local dirt positions remain; no painted artificial stains.
        smooth = np.asarray(small.filter(ImageFilter.GaussianBlur(.38))).astype(float) @ [0.2126,.7152,.0722]
        low, mid = np.percentile(smooth, [3, 23])
        result = np.empty_like(a)
        result[:] = colors[25]
        result[smooth < mid] = colors[19]
        result[smooth < low] = colors[0]
        selected = [0,19,25]
        process = 'photo luminance blur .38px; 3/23 percentiles -> cream ladder 0,19,25; real dirt mask only'
    else:
        smooth=np.asarray(small.filter(ImageFilter.GaussianBlur(.55))).astype(float) @ [.2126,.7152,.0722]
        lo,hi=np.percentile(smooth,[3,97]);tone=np.clip((smooth-lo)/(hi-lo),0,1)
        dark=np.array(colors[21]);light=np.array(colors[23])
        result=dark[None,None,:]+tone[:,:,None]*(light-dark)
        selected = [21,24,22,23,16]
        process = 'photo blur .55px; luminance 3..97 percentile -> dusty clay ladder 21,24,22,23,16; retains tile overlap without charcoal pinholes'
    graded = Image.fromarray(np.clip(result, 0, 255).astype('uint8'))
    final = quantize_exact(seam_correct(graded), [colors[i] for i in selected], 'none')
    if key == 'stone':
        # Artist-traced EXISTING source joints (source 1024px coordinates), not
        # an invented paving grid. One output pixel = 1.5625cm mortar emphasis.
        lines=[[(0,254),(128,257),(286,252),(464,257),(630,253),(800,254),(1024,257)],
               [(0,513),(128,517),(285,512),(456,518),(630,514),(798,518),(1024,515)],
               [(0,772),(128,776),(288,770),(456,775),(628,771),(799,775),(1024,772)]]
        for row,xs in enumerate([[128,463,797],[288,628,960],[128,456,797],[288,626,958]]):
            for x in xs: lines.append([(x,row*256),(x-3,row*256+128),(x,row*256+256)])
        ink=ImageDraw.Draw(final)
        for line in lines:
            ink.line([(round(x/8),round(y/8)) for x,y in line],fill=colors[9],width=1)
        final=quantize_exact(seam_correct(final),colors,'none')
        selected.append(9)
        process += '; manually trace existing photographed mortar joints at 1px using palette9; coordinates recorded in kp_textures.py'
    return final, process, selected

def generate():
    OUT.mkdir(parents=True, exist_ok=True)
    for folder in ['albedo', 'original_reduced', 'baseline', 'atlas_cells', 'proof', 'review']:
        (OUT/folder).mkdir(exist_ok=True)
    # Other downloaded candidates stay in sources/ as rejected research evidence.
    colors = load_palette(PAL)
    tiles = []
    for key in KEYS:
        source = json.loads((OUT/f'source_{key}.json').read_text(encoding='utf-8'))
        photo = Image.open(ROOT/source['path']).convert('RGB')
        dims = source['dimensions_mm']
        meters = [v/1000 for v in dims]
        size = tuple(round(m*64) for m in meters)
        crop = [0,0,*photo.size]
        small = photo.resize(size, Image.Resampling.BOX)
        base = OUT/'baseline'/f'{key}_photo_to_pixel_64ppm.png'
        convert(SimpleNamespace(input=ROOT/source['path'], output=base, palette=PAL,
            width_m=meters[0], height_m=meters[1], density=64, dither='none', resample='box',
            source_id=source['id'], source_url=source['url'], license_url=source['license_url'],
            crop=crop, canvas_size=None, metadata=None))
        rotate = 90 if key == 'wood' else 0
        if rotate:
            small = small.transpose(Image.Transpose.ROTATE_90)
        small.save(OUT/'original_reduced'/f'{key}_photo_64ppm.png')
        final, correction, selected = grade(key, small, colors)
        albedo = OUT/'albedo'/f'A1_KP_{key}_64ppm.png'
        final.save(albedo)
        # Proposed legacy cell: 60 physical texels, no rescale, plus 2px extrusion.
        cx,cy=(size[0]-60)//2,(size[1]-60)//2
        active = np.asarray(final)[cy:cy+60,cx:cx+60]
        Image.fromarray(np.pad(active, ((2,2),(2,2),(0,0)), mode='edge')).save(OUT/'atlas_cells'/f'{key}_60plus2.png')
        tiled = Image.new('RGB',(size[0]*3,size[1]*3))
        for y in range(3):
            for x in range(3): tiled.paste(final,(x*size[0],y*size[1]))
        tiled.resize((size[0]*6,size[1]*6),Image.Resampling.NEAREST).save(OUT/'review'/f'{key}_repeat_3x3.png')
        source['ai_generated'] = False
        process = [f'Direct official 1k diffuse PNG download; MD5 + SHA verified; {source["id"]}',
            f'full physical repeat {meters}m from {dims} mm source; no geometry stretch or cropped joint pattern',
            'photo_to_pixel.py: physical source size, 64px/m, BOX, exact common 32 palette, dither none; preserved unmodified baseline',
            f'rotate photo patch {rotate} degrees; '+correction,
            '3px symmetric seam colour correction, then exact palette quantization; no generated texture/noise/AI',
            'PNG Point intent; GLB proof sampler Nearest/Repeat, physical-size UV quad; Unity import still pending']
        tiles.append({'id':key, 'albedo':rel(albedo), 'model':rel(OUT/'proof'/f'{key}_metric.glb'),
            'physical_size_m':meters, 'density_px_m':64, 'ai_generated':False,
            'source':source, 'process':process, 'crop_ltrb':crop, 'rotation_degrees':rotate,
            'palette_indices':selected, 'actual_used_colors':len(final.getcolors(4096)),
            'atlas_cell':{'active_pixels':[60,60], 'gutter_pixels':2, 'active_size_m':[.9375,.9375]}})
    save_json(OUT/'manifest.json', {'schema_version':1, 'palette':rel(PAL), 'tiles':tiles})
    board(tiles)

def font(size): return ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', size)

def board(tiles):
    b = Image.new('RGB',(1160,1374),'#eee8dc'); d=ImageDraw.Draw(b)
    d.text((24,15),'A1  /  사진 → 픽셀 → 보정',font=font(28),fill='#292e2a')
    d.text((24,57),'각 타일 중앙 1m(64px) 발췌 · 4배 Nearest · AI 생성 0 · albedo 32색 검사',font=font(18),fill='#52534b')
    columns=[(264,'원본 사진 축소'),(550,'photo_to_pixel 기준판'),(836,'보정 결과')]
    for x,title in columns: d.text((x,96),title,font=font(18),fill='#292e2a')
    for row,t in enumerate(tiles):
        y=135+row*300
        d.text((24,y+8),LABELS[row],font=font(20),fill='#292e2a')
        m=t['physical_size_m'][0]; size=round(m*64)
        d.text((24,y+48),f'전체 {m:g}m / {size} × {size}px\n{t["actual_used_colors"]}색 / Point\n64px/m',font=font(16),fill='#52534b')
        paths=[OUT/'original_reduced'/f'{t["id"]}_photo_64ppm.png',OUT/'baseline'/f'{t["id"]}_photo_to_pixel_64ppm.png',ROOT/t['albedo']]
        for index,((x,_),p) in enumerate(zip(columns,paths)):
            im=Image.open(p)
            if index==1 and t['rotation_degrees']: im=im.transpose(Image.Transpose.ROTATE_90)
            x0,y0=(im.width-64)//2,(im.height-64)//2
            im=im.crop((x0,y0,x0+64,y0+64))
            b.paste(im.resize((256,256),Image.Resampling.NEAREST),(x,y))
    d.text((24,1342),'Powered by Poly Haven · 촬영지는 미확인: 교토/포지타노 재료 대응 시험 · 게임 화면 검사는 별도',font=font(16),fill='#52534b')
    b.save(OUT/'compare-4x.png')
    # Full repeat units at exactly 4x, without cropping; large archival sheet.
    full=Image.new('RGB',(3400,2810),'#eee8dc');fd=ImageDraw.Draw(full)
    fd.text((24,18),'A1 · 전체 반복 단위 · 정확히 4배 Nearest',font=font(30),fill='#292e2a')
    fd.text((24,65),'원본 축소본 / 기존 파이프라인 기준판 / 최종 보정 · 전체 크기와 줄눈 배열 보존',font=font(20),fill='#52534b')
    y=120
    for row,t in enumerate(tiles):
        paths=[OUT/'original_reduced'/f'{t["id"]}_photo_64ppm.png',OUT/'baseline'/f'{t["id"]}_photo_to_pixel_64ppm.png',ROOT/t['albedo']]
        m=t['physical_size_m'][0];s=round(m*64)*4
        fd.text((24,y+40),LABELS[row],font=font(21),fill='#292e2a')
        fd.text((24,y+78),f'{m:g}m / {s//4}px\n4× = {s}px',font=font(18),fill='#52534b')
        for index,(p,title) in enumerate(zip(paths,['원본 축소','기준판','보정 결과'])):
            x=240+index*(s+24);im=Image.open(p)
            if index==1 and t['rotation_degrees']:im=im.transpose(Image.Transpose.ROTATE_90)
            fd.text((x,y),title,font=font(22),fill='#292e2a')
            full.paste(im.resize((s,s),Image.Resampling.NEAREST),(x,y+40))
        y+=s+70
    full.save(OUT/'compare-full-4x.png')

def provenance():
    manifest=json.loads((OUT/'manifest.json').read_text(encoding='utf-8'))
    path=ROOT/'art/provenance.csv'
    with path.open(encoding='utf-8-sig', newline='') as f:
        reader=csv.DictReader(f); fields=reader.fieldnames; rows=list(reader)
    rows=[r for r in rows if not r['asset_id'].startswith('a1_kp_')]
    for t in manifest['tiles']:
        for kind,p in [('source',t['source']['path']),('albedo',t['albedo']),('proof_quad',t['model'])]:
            rows.append(dict(asset_id=f'a1_kp_{t["id"]}_{kind}',path=p,source_url=t['source']['url'],
                license='CC0-1.0',accessed='2026-10-24',tool='photo_to_pixel.py; kp_textures.py; kp_texture_preview.py',
                ai_generated='no',sha256=sha(ROOT/p),notes='; '.join(t['process'])+'; palette check applies to unlit albedo only'))
    with path.open('w',encoding='utf-8',newline='') as f:
        writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)
    print('Registered 12 A1 source/albedo/proof rows; preserved other asset rows.')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--provenance',action='store_true');args=p.parse_args()
    provenance() if args.provenance else generate()
