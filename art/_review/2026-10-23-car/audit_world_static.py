"""Read-only world/atlas audit; writes only this review folder."""
from pathlib import Path
import ast, collections, hashlib, json, re
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
def read(p): return (ROOT/p).read_text(encoding='utf-8-sig')
def digest(p): return hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
tree=ast.parse(read('tools/asset-gen/lofi_pilot.py'))
pal=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='pal' for t in n.targets))
extensions=json.loads(read('art/source/hillhouse-surfaces/tiles.json'))['tiles']
used=set(range(len(pal)))|{t['index'] for t in extensions}
scene=read('F1/Assets/Scenes/Proto_CoastCourse.unity')
names=collections.Counter(re.findall(r'^  m_Name: (.+)$',scene,re.M))
materials=[]
for p in sorted((ROOT/'F1/Assets/Scenes/Proto_CoastCourse_Mats').glob('*.mat')):
    s=p.read_text(encoding='utf-8-sig')
    m=re.search(r'_MainTex:\s*\n\s*m_Texture: \{fileID: (\d+)(?:, guid: ([a-f0-9]+))?',s)
    guid=re.search(r'^guid: (\w+)',p.with_suffix('.mat.meta').read_text(),re.M).group(1)
    materials.append({'path':p.relative_to(ROOT).as_posix(),'main_texture_file_id':int(m.group(1)) if m else None,'main_texture_guid':m.group(2) if m else None,'serialized_scene_guid_mentions':scene.count(guid)})
atlas=Image.open(ROOT/'F1/Assets/Art/LofiPilot/LofiAtlas.png').convert('RGB')
colors={tuple(c) for c in json.loads(read('art/source/palette/pitcrew_d123_32.json'))['colors']}
tile_stats=[]
for idx in (13,42,59,60,61,62,63):
    x=(idx%8)*64;y=(7-idx//8)*64
    crop=atlas.crop((x,y,x+64,y+64));counts=collections.Counter(crop.getdata())
    tile_stats.append({'index':idx,'legacy_name':pal[idx][0] if idx<len(pal) else None,'color_count':len(counts),'most_common_fraction':counts.most_common(1)[0][1]/4096,'out_of_shared_palette_pixels':sum(n for c,n in counts.items() if c not in colors),'sample_colors':counts.most_common(8)})
out={'kind':'static audit, not Unity/runtime verification','screenshot':'docs/qa/evidence/proto-course-3-coast.png','screenshot_sha256':digest('docs/qa/evidence/proto-course-3-coast.png'),'scene_sha256':digest('F1/Assets/Scenes/Proto_CoastCourse.unity'),'legacy_atlas_sha256':digest('F1/Assets/Art/LofiPilot/LofiAtlas.png'),'legacy_tile_count':len(pal),'review_extension_indices':[t['index'] for t in extensions],'free_indices_in_512_grid':[i for i in range(64) if i not in used],'tile_stats':tile_stats,'materials':materials,'selected_scene_object_counts':{n:v for n,v in names.items() if n.startswith(('BLOCKOUT_','MeadowTile','HillTerrain','Guardrail','RailPost'))},'ground_projected_px_m':64*.93*.45,'worldscale_for_32_projected_px_m':32/(64*.93),'note':'WorldSurface chooses a dominant world projection; sloped triangle density needs per-triangle measurement.'}
(HERE/'world-audit-evidence.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'legacy':len(pal),'extension':[t['index'] for t in extensions],'free':out['free_indices_in_512_grid'],'ground_projected_px_m':out['ground_projected_px_m'],'flat_texture_null_materials':[Path(m['path']).stem for m in materials if m['main_texture_file_id']==0],'selected_counts':out['selected_scene_object_counts'],'tile_stats':tile_stats},ensure_ascii=False,indent=2))
