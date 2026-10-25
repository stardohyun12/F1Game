"""D-123 UI PNG contract checks. Called by check_asset.py --ui-manifest.

UI has fixed legacy NPOT dimensions; GLB/POT/metre checks are not applicable.
Provenance checks validate recorded lineage, not automatic AI-image detection.
"""
from pathlib import Path
import argparse, csv, hashlib, json
import numpy as np
from PIL import Image
from photo_to_pixel import load_palette

ROOT = Path(__file__).resolve().parents[2]
CONTRACT = {
    'paperPanel': ('UITheme', (256,256), (16,24,16,16), 'Tiled'),
    'paperCard': ('UITheme', (256,320), (16,24,16,40), 'Tiled'),
    'slipHeader': ('UITheme', (256,64), (16,16,16,16), 'Tiled'),
    'stampRing': ('UITheme', (256,256), (0,0,0,0), 'Simple'),
    'buttonPlate': ('UITheme', (256,80), (16,16,16,16), 'Sliced'),
    'enamelPlate': ('UITheme', (256,128), (20,20,20,20), 'Sliced'),
    'gaugeFill': ('UITheme', (128,24), (8,8,8,8), 'Sliced'),
    'stampSprite': ('PackingScreen', (300,300), (0,0,0,0), 'Simple'),
    'gaugeTrackSprite': ('PackingScreen', (256,32), (12,12,12,12), 'Sliced'),
    'gaugeFillSprite': ('PackingScreen', (128,24), (8,8,8,8), 'Sliced'),
}

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def inside(path, parent):
    try:
        Path(path).resolve().relative_to(Path(parent).resolve())
        return True
    except ValueError:
        return False

def inspect_sprite(s, root, palette, rows, sprite_root):
    name = s['slot']; owner, size, border, mode = CONTRACT[name]
    path = (root / s['path']).resolve()
    if not inside(path, sprite_root):
        raise ValueError('Sprite outside manifest sprite_root: ' + str(path))
    with Image.open(path) as im:
        fmt, original_mode, actual_size = im.format, im.mode, im.size
        px = np.asarray(im.convert('RGBA'))
    digest = sha(path)
    visible = px[:,:,3] > 0
    used = {tuple(map(int,c)) for c in px[:,:,:3][visible]}
    extra = sorted(used - palette)
    repeated = np.repeat(np.repeat(px[::4,::4],4,axis=0),4,axis=1)
    matching = [r for r in rows if r['path'] == s['path']]
    registered = len(matching)==1 and matching[0]['sha256']==digest and matching[0]['ai_generated']=='no' and all(matching[0].get(k) for k in ['source_url','license','tool'])
    checks = {
        'png_rgba': fmt=='PNG' and original_mode=='RGBA',
        'existing_size_preserved': actual_size==size and tuple(s['size'])==size,
        'existing_border_preserved': tuple(s['border_LBRT'])==border,
        'owner_matches': s['owner']==owner,
        'alpha_binary': set(np.unique(px[:,:,3]).tolist()) <= {0,255},
        'nonempty_visible_content': bool(np.any(visible)),
        'palette_outside_zero': not extra,
        'every_4px_cell_uniform': repeated.shape==px.shape and np.array_equal(repeated,px),
        'manifest_sha_matches_bytes': s['sha256']==digest,
        'provenance_registered_non_ai': bool(registered) and s.get('ai_generated') is False,
        'mode_matches_contract': s['mode']==mode,
        'full_color_white_tint_declared': s['tint'].startswith('Color.white'),
        'logical_2px_contract': s.get('export_cell_px')==4 and s.get('native_display_cell_px')==2 and s.get('source_to_logical')==0.5,
    }
    return {'slot':name,'path':s['path'],'pass':all(checks.values()),'checks':checks,'size':list(actual_size),'border_LBRT':list(border),'mode':mode,'used_colors':len(used),'outside_colors':extra,'sha256':digest}

def inspect_manifest(manifest_path, palette_path, root=ROOT):
    root=Path(root).resolve()
    manifest=json.loads(Path(manifest_path).read_text('utf-8'))
    palette={tuple(c) for c in load_palette(palette_path)}
    with (root/'art/provenance.csv').open(encoding='utf-8-sig',newline='') as f:
        rows=list(csv.DictReader(f))
    specs=manifest['sprites']
    names=[s['slot'] for s in specs]
    checks={
        'schema':manifest.get('schema')=='pitcrew-ui-sprites-v1',
        'ten_unique_expected_slots':len(names)==10 and set(names)==set(CONTRACT),
        'palette_32':len(palette)==32,
        'palette_matches_manifest':Path(palette_path).resolve()==(root/manifest['palette']).resolve(),
    }
    sprite_root=(root/manifest['sprite_root']).resolve()
    checks['review_only_sprite_root']=inside(sprite_root,root/'art/_review')
    checks['exactly_ten_pngs_on_disk']={p.stem for p in sprite_root.glob('*.png')}==set(CONTRACT)
    sources=json.loads((root/manifest['sources_manifest']).read_text('utf-8'))
    photos=[s for s in sources if s.get('license')=='CC0-1.0']
    checks['photographic_input_hash_matches']=bool(photos) and all(sha(root/s['original'])==s['sha256'] for s in photos)
    checks['photographic_lineage_recorded']=bool(photos) and all(s.get('technique')=='Surface Photogrammetry' and s.get('source_url','').startswith('https://ambientcg.com/') and any('photo_to_pixel' in step for step in s.get('pipeline',[])) for s in photos)
    results=[]
    for s in specs:
        try:
            results.append(inspect_sprite(s,root,palette,rows,sprite_root))
        except Exception as exc:
            results.append({'slot':s.get('slot'),'pass':False,'error':str(exc)})
    checks['all_sprite_checks']=all(r['pass'] for r in results)
    return {'pass':all(checks.values()),'scope':'10 review UI sprites only. Actual PNG pixels, immutable D-123 slot sizes/borders, SHA-256 + declared photographic/script provenance. Does not infer AI origin from pixels, inspect a release build, or test Unity import. NPOT slot sizes are intentional; all legacy GLB checks remain separate.','checks':checks,'sprite_count':len(results),'sprites':results}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--ui-manifest',required=True);p.add_argument('--palette',required=True);p.add_argument('--out',required=True)
    args=p.parse_args()
    try:
        result=inspect_manifest(args.ui_manifest,args.palette)
    except Exception as exc:
        result={'pass':False,'error':str(exc)}
    target=Path(args.out);target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'pass':result['pass'],'sprite_count':result.get('sprite_count'),'checks':result.get('checks'),'error':result.get('error')},ensure_ascii=False))
    return 0 if result['pass'] else 1
