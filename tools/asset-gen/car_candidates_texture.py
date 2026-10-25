"""D142 candidate atlases: measured CC0 photos, 32px/m, shared palette.

No AI, synthetic noise, Assets writes or source-image modifications.
"""
from pathlib import Path
import json
import argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from types import SimpleNamespace
from photo_to_pixel import convert, load_palette
import car_texture as old

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/models/car_candidates_v01'
WORK=OUT/'textures'
PAL=ROOT/'art/source/palette/pitcrew_d123_32.json'
FONT=ROOT/'art/_review/2026-10-23-d123/fonts/Galmuri11.ttf'
PHYSICAL=3.75

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--variant',choices=['A','C'])
    args=parser.parse_args()
    WORK.mkdir(parents=True,exist_ok=True)
    colors=load_palette(PAL)
    source_data=json.loads((ROOT/'art/source/car-v01/texture-contract.json').read_text('utf-8'))['sources']
    # Reuse physical photo construction while redirecting every output.
    old.OUT=WORK;old.PHYSICAL=PHYSICAL
    photo={}
    for rec in source_data:
        assert old.sha(ROOT/rec['path'])==rec['sha256']
        path,im=old.physical_photo(rec)
        convert(SimpleNamespace(input=path,output=WORK/'baseline'/(rec['id']+'.png'),palette=PAL,
            width_m=PHYSICAL,height_m=PHYSICAL,density=32,dither='none',resample='box',
            source_id=rec['id'],source_url=rec['url'],license_url=rec['license_url'],
            crop=None,canvas_size=None,metadata=None))
        low=im.resize((120,120),Image.Resampling.BOX)
        lum=np.dot(np.asarray(low,dtype=np.float64),[.299,.587,.114])
        # Keep only fine photographed surface variation. The earlier 3x3-11x11
        # band repeated broad chips on every centred UV patch and read as camouflage.
        photo[rec['id']]=lum-old.box_float(lum,1)
    names=['body','replacement_panel','wheel_light_metal','exposed_steel','rubber_sidewall',
           'rubber_tread','ochre_fabric','rear_fabric','red_livery','black_fabric',
           'number_sticker','repair_tape','cream_paint','dark_glass','underbody','rear_lamp']
    base=[[15,28],[26,5],[7,19],[8,7],[26,2],[26,2],[29,30],[29,30],
          [15,28],[26,2],[16,0],[8,7],[16,0],[2,26],[26,2],[15,28]]
    contract={'atlas_size_px':[512,512],'grid_xy':[4,4],'cell_size_px':128,'guard_px':4,
       'active_size_px':120,'active_physical_m':[3.75,3.75],'density_px_m':32,
       'slot_order':'top-left row-major PNG','palette':PAL.relative_to(ROOT).as_posix(),
       'palette_sha256':old.sha(PAL),'sources':source_data,'ai_generated':False,
       'number_decal':{'slot':10,'local_rect_ltrb':[58.88,60.8,69.12,67.2],'size_m':[.32,.20],
          'font':FONT.relative_to(ROOT).as_posix(),'font_size_px':7,'text':'7','density_px_m':32,
          'glyph_processing':'binary Galmuri glyph 4x7 compressed to4x5 by coverage-preserving row OR; ink local62..65,61..65'},
       'sampling':'Closest/Point; Unity import not executed',
       'material_skill_adaptation':'Shared master with material regions. PBR/noise/transmission rejected for project flat palette photo workflow.',
       'variants':{}}
    ledger=[]
    if args.variant:
        existing=json.loads((OUT/'texture-contract.json').read_text('utf-8'))
        contract['variants']={k:v for k,v in existing['variants'].items() if k!=args.variant}
        ledger=[r for r in json.loads((OUT/'texture-provenance-rows.json').read_text('utf-8')) if r['asset_id']!=f'car_candidate_{args.variant}_atlas']
    for variant in ([args.variant] if args.variant else ['A','C']):
        ladders=[x[:] for x in base]
        if variant=='C':ladders[0]=[19,25]
        atlas=Image.new('RGB',(512,512),colors[6]);tiles=[]
        for slot,name in enumerate(names):
            sid='rough_linen' if slot in [6,7,9,10,11] else ('rubberized_track' if slot in [4,5,13] else 'rusty_metal_02')
            lum=photo[sid]
            # R3: 4% residual surface variation on paint, rather than field-wide
            # weathering. Functional wear lives only at actual repair geometry.
            q=.04 if slot in [0,1,6,7,8,10,12,15] else .15
            cut=float(np.quantile(lum,q))
            indices=np.where(lum<=cut,ladders[slot][0],ladders[slot][1])
            patch=Image.fromarray(np.asarray(colors,dtype=np.uint8)[indices])
            # Only functional information is scripted, with exact binary font pixels.
            if slot==10:
                d=ImageDraw.Draw(patch);d.rectangle((54,56,65,63),fill=colors[25])
                mask=Image.new('1',(120,120));md=ImageDraw.Draw(mask)
                font=ImageFont.truetype(str(FONT),7)
                glyph=Image.new('1',(16,16));ImageDraw.Draw(glyph).text((0,0),'7',font=font,fill=1)
                glyph=glyph.crop(glyph.getbbox());ga=np.asarray(glyph)
                compressed=np.stack([ga[int(y*ga.shape[0]/5):max(int(np.ceil((y+1)*ga.shape[0]/5)),int(y*ga.shape[0]/5)+1)].any(axis=0) for y in range(5)])
                glyph=Image.fromarray(compressed)
                mask.paste(glyph,(58,57))
                patch.paste(colors[6],mask=mask)
            tile=Image.fromarray(np.pad(np.asarray(patch),((4,4),(4,4),(0,0)),mode='edge'))
            tilepath=WORK/f'{variant}_{slot:02d}_{name}.png';tile.save(tilepath)
            atlas.paste(tile,((slot%4)*128,(slot//4)*128))
            rgb,count=np.unique(np.asarray(patch).reshape(-1,3),axis=0,return_counts=True)
            assert len(rgb)>=2
            tiles.append({'slot':slot,'name':name,'source_id':sid,'palette_indices':ladders[slot],
                'photo_quantile':q,'used_colors':len(rgb),'dominant_fraction':float(max(count)/sum(count)),
                'process':'source physical dimensions -> 3.75m photo field -> BOX120 -> target photo luminance minus float3x3 mean -> low-contrast palette ladder; no random noise; coarse camouflage rejected',
                'path':tilepath.relative_to(ROOT).as_posix(),'sha256':old.sha(tilepath)})
        path=OUT/f'{variant}_Atlas.png';atlas.save(path)
        used=set(map(tuple,np.asarray(atlas).reshape(-1,3)));outside=used-set(colors)
        assert not outside and len(used)<=32
        contract['variants'][variant]={'path':path.relative_to(ROOT).as_posix(),'sha256':old.sha(path),
            'palette_outside':0,'used_colors':len(used),'power_of_two':True,'tiles':tiles}
        ledger.append({'asset_id':f'car_candidate_{variant}_atlas','path':path.relative_to(ROOT).as_posix(),
            'source_url':'; '.join(s['url'] for s in source_data),'license':'CC0-1.0; Galmuri OFL-1.1',
            'accessed':'2026-10-25','tool':'car_candidates_texture.py + photo_to_pixel.py','ai_generated':'아니오',
            'sha256':old.sha(path),'notes':'32px/m incl .32x.20m number7; physical photo resampling; palette ladder; binary Galmuri7 compressed4x5; 4px edge gutters.'})
    old.dump(OUT/'texture-contract.json',contract)
    old.dump(OUT/'texture-provenance-rows.json',ledger)
    old.dump(OUT/'texture-check.json',{'status':'PASS','palette_outside':0,'density_metadata':32,
        'actual_mesh_uv_check':'parent checker required','variants':{k:{x:v[x] for x in ['sha256','used_colors','power_of_two']} for k,v in contract['variants'].items()}})
    (OUT/'TEXTURE-SOURCES.md').write_text('# 후보 차 질감\n\nCC0 사진을 기존 저장본에서 재사용. 원본 치수·SHA·URL은 texture-contract.json.\n'
        '32px/m, 공용 32색 내 양자화. 번호도 32px/m이며 64 예외 없음.\n'
        'R3: 원본 명도−float 3×3 평균의 미세 사진 신호를 도장4%/금속고무15%로 축소. 마모를 전면에 추가하지 않음. tile2는 밝은 휠 금속(공용7/19). 절차적 잡음 없음.\n'
        '사진 자체는 자동차 표면 실측 사진이 아니며, 금속·고무·직물의 표면 변화만 전용.\n'
        'Materials 스킬에서 공용 마스터·재질 구획을 채택. PBR 반사·굴절 및 Noise 녹 마스크는 프로젝트 규칙 때문에 사용하지 않음.\n\n'
        +'\n'.join(f"- {s['id']}: {s['url']} | CC0: {s['license_url']} | {s['dimensions_mm']} mm | SHA {s['sha256']}" for s in source_data)
        +'\n\nGalmuri11: art/_review/2026-10-23-d123/fonts/LICENSE.txt (OFL-1.1).\n',encoding='utf-8')
    print(json.dumps({'status':'PASS','variants':list(contract['variants']),'density':32,'palette_outside':0}))

if __name__=='__main__':main()
