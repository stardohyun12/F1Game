"""A1 second study: change photographic structure, preserve physical pixel scale."""
import argparse
import csv
import json
from pathlib import Path
from types import SimpleNamespace
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from photo_to_pixel import convert, load_palette, quantize_exact, sha256
import kp_textures as common

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/textures/2026-10-24-kp-v2'
OLD=ROOT/'art/textures/2026-10-24-kp'
PAL=ROOT/'art/source/palette/pitcrew_d123_32.json'
def rel(p):return Path(p).relative_to(ROOT).as_posix()
def read(p):return json.loads(Path(p).read_text(encoding='utf-8'))
def write(p,d):Path(p).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def wrap_photo(photo,size):
    """Physical-scale continuation. No stretch; opposite edges repaired separately."""
    a=np.asarray(photo);return Image.fromarray(np.tile(a,(int(np.ceil(size[1]/a.shape[0])),int(np.ceil(size[0]/a.shape[1])),1))[:size[1],:size[0]])

def make(revision):
    for d in ['albedo','original_reduced','baseline','proof','review']:(OUT/d).mkdir(parents=True,exist_ok=True)
    colors=load_palette(PAL);old=read(OLD/'manifest.json');roof=read(OUT/'source_roof_candidates.json')['candidates'][0]
    selected={'wood':read(OUT/('source_wood_e.json' if revision>=2 else 'source_wood_b.json')),'plaster':read(OLD/'source_plaster.json'),'roof':roof}
    tiles=[]
    for key in ['wood','stone','plaster','roof']:
        albedo=OUT/'albedo'/f'A1_KP_{key}_64ppm.png'
        if key=='stone':
            t=next(t for t in old['tiles'] if t['id']==key)
            shutil.copy2(ROOT/t['albedo'],albedo)
            for folder,filename in [('original_reduced','stone_photo_64ppm.png'),('baseline','stone_photo_to_pixel_64ppm.png')]:shutil.copy2(OLD/folder/filename,OUT/folder/filename)
            source=t['source'];physical=[2,2];process=t['process']+['A1 round2: unchanged passed stone albedo; original file SHA retained']
            crop=t['crop_ltrb'];rotation=0;final=Image.open(albedo).convert('RGB')
        else:
            source=selected[key];photo=Image.open(ROOT/source['path']).convert('RGB');meters=[v/1000 for v in source['dimensions_mm']]
            rotation=0
            if key=='wood':
                physical=[2,2];crop=[0,0,*photo.size];rotation=90
            elif key=='plaster':
                physical=[2,2];crop=[0,0,*photo.size]
            else:
                physical=[2,2];side=round(photo.width*2/meters[0]);crop=[216,185,216+side,185+side]
            size=tuple(round(m*64) for m in physical)
            patch=photo.crop(crop).resize(size,Image.Resampling.BOX)
            if rotation:patch=patch.transpose(Image.Transpose.ROTATE_90)
            patch.save(OUT/'original_reduced'/f'{key}_photo_64ppm.png')
            baseline=OUT/'baseline'/f'{key}_photo_to_pixel_64ppm.png'
            convert(SimpleNamespace(input=ROOT/source['path'],output=baseline,palette=PAL,width_m=physical[0],height_m=physical[1],density=64,dither='none',resample='box',source_id=source['id'],source_url=source['url'],license_url=source['license_url'],crop=crop,canvas_size=None,metadata=None))
            process=[f'Official CC0 photo diffuse download; {source["id"]}; provider MD5 verified',
                f'photo_to_pixel.py; physical crop {crop}; {physical}m -> {size}px; BOX; exact shared 32 palette; dither none; baseline preserved']
            if key=='plaster':
                # Quiet 2m painted wall + two physically scaled photographic chips.
                # These masks trace photographed boundaries, not generated dirt.
                a=np.asarray(patch).astype(float)+[53,43,28]
                working=Image.fromarray(np.clip(a,0,255).astype('uint8'))
                damage=read(OUT/'source_plaster_b.json');detail=Image.open(ROOT/damage['path']).convert('RGB')
                damage_size=damage['dimensions_mm'][0]/1000
                placements=[
                    {'polygon':[(330,15),(370,8),(410,16),(452,9),(499,25),(519,45),(511,70),(486,84),(463,108),(440,143),(409,154),(382,196),(359,183),(345,152),(338,126),(330,112),(328,72),(322,41)],'dest':[24,81]},
                    {'polygon':[(181,654),(203,608),(235,580),(264,587),(280,616),(281,649),(269,680),(275,713),(294,746),(275,784),(246,805),(208,790),(194,747),(176,711)],'dest':[54,103]}
                ]
                for chip in placements:
                    mask=Image.new('L',detail.size);ImageDraw.Draw(mask).polygon(chip['polygon'],fill=255)
                    if revision>=2:
                        # Include the REAL surrounding plaster, not just a sticker-like
                        # cutout. Dilate ~4cm and feather ~2cm at original photo scale.
                        halo=mask.filter(ImageFilter.MaxFilter(55)).filter(ImageFilter.GaussianBlur(12))
                        mask=Image.fromarray(np.maximum(np.asarray(mask),np.asarray(halo)))
                    box=mask.getbbox();width=round((box[2]-box[0])/detail.width*damage_size*64);height=round((box[3]-box[1])/detail.height*damage_size*64)
                    patch_chip=detail.crop(box).resize((width,height),Image.Resampling.BOX)
                    graded=np.asarray(patch_chip).astype(float)+([60,65,80] if revision>=2 else [35,38,38])
                    working.paste(Image.fromarray(np.clip(graded,0,255).astype('uint8')),chip['dest'],mask.crop(box).resize((width,height),Image.Resampling.NEAREST))
                process+=['Quiet painted photographic base + two traced REAL spall contours from rough_plaster_broken; 1.5m source scale -> 64px/m, local lower-left placements; masks/coordinates recorded in script']
                process += [f'Revision {revision}: base gain [53,43,28]; chip gain {([60,65,80] if revision>=2 else [35,38,38])}; '+('55px original-source mask expansion and 12px feather include actual adjacent plaster; final palette quantization' if revision>=2 else 'tight photographic contours')]
                damage['ai_generated']=False
                # Record a real photo_to_pixel baseline for the additional photo as well.
                convert(SimpleNamespace(input=ROOT/damage['path'],output=OUT/'baseline/plaster_damage_96.png',palette=PAL,width_m=1.5,height_m=1.5,density=64,dither='none',resample='box',source_id=damage['id'],source_url=damage['url'],license_url=damage['license_url'],crop=None,canvas_size=None,metadata=None))
            else:
                working=patch
                features='board width differences, knots, weathered grain' if key=='wood' else 'broad overlapping ceramic rows, undercuts and rounded lower edges'
                process+=[f'Rotate {rotation} degrees; no recolouring; photographic {features} retained; shared palette quantization only']
            final=quantize_exact(common.seam_correct(working,band=2),colors,'none')
            process+=['2px matching-edge colour repair followed by exact palette quantization; no AI or random noise']
            final.save(albedo)
        source['ai_generated']=False
        process+=['Closest/Point proof material; metric GLB validates actual 64px/m; final lit framebuffer is outside albedo palette check']
        t={'id':key,'albedo':rel(albedo),'model':rel(OUT/'proof'/f'{key}_metric.glb'),'physical_size_m':physical,'density_px_m':64,'source':source,'process':process,'ai_generated':False,'crop_ltrb':crop,'rotation_degrees':rotation,'actual_used_colors':len(final.getcolors(final.width*final.height))}
        if key=='plaster':t.update(additional_sources=[damage],damage_placements=placements)
        tiles.append(t)
        tiled=Image.new('RGB',(final.width*3,final.height*3))
        for y in range(3):
            for x in range(3):tiled.paste(final,(x*final.width,y*final.height))
        tiled.resize((tiled.width*2,tiled.height*2),Image.Resampling.NEAREST).save(OUT/'review'/f'{key}_repeat_3x3.png')
    write(OUT/'manifest.json',{'schema_version':1,'revision':revision,'palette':rel(PAL),'tiles':tiles})
    common.OUT=OUT;common.board(tiles)

def register():
    m=read(OUT/'manifest.json');path=ROOT/'art/provenance.csv'
    with path.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
    rows=[r for r in rows if not r['asset_id'].startswith('a1_kp2_')]
    for t in m['tiles']:
        for kind,p in [('source',t['source']['path']),('albedo',t['albedo']),('proof_quad',t['model'])]:
            if kind=='source' and any(r['path']==p and r['sha256']==sha256(ROOT/p) and r['source_url']==t['source']['url'] for r in rows):continue
            rows.append(dict(asset_id=f'a1_kp2_{t["id"]}_{kind}',path=p,source_url=t['source']['url'],license='CC0-1.0',accessed='2026-10-24',tool='photo_to_pixel.py; kp_textures_v2.py; kp_texture_preview.py',ai_generated='no',sha256=sha256(ROOT/p),notes='; '.join(t['process'])))
        for index,s in enumerate(t.get('additional_sources',[])):
            rows.append(dict(asset_id=f'a1_kp2_{t["id"]}_detail{index}',path=s['path'],source_url=s['url'],license='CC0-1.0',accessed='2026-10-24',tool='photo_to_pixel.py; kp_textures_v2.py',ai_generated='no',sha256=sha256(ROOT/s['path']),notes='Physically scaled photographic spall; traced source contours and final placements in manifest.damage_placements; '+ '; '.join(t['process'])))
    with path.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
    print('A1 second-study provenance registered; unchanged source paths reuse their existing unique rows')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--revision',type=int,default=1);p.add_argument('--provenance',action='store_true');a=p.parse_args()
    register() if a.provenance else make(a.revision)
