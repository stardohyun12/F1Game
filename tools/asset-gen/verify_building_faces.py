"""Blender audit for the fifth-pass building-variety brief. No asset mutation.

blender -b -P tools/asset-gen/verify_building_faces.py -- --root D:/Github/F1Game \
    --baseline D:/Github/F1Game/files/building-variety-baseline
Baseline must be an unmodified lofi_pilot.py run into a separate --root,
with original.py, LofiAtlas.before.png and both protected C# files saved there.
"""
import argparse, ast, hashlib, json, sys
from pathlib import Path
import bpy

ap=argparse.ArgumentParser()
ap.add_argument('--root',required=True)
ap.add_argument('--baseline',required=True)
args=ap.parse_args(sys.argv[sys.argv.index('--')+1:])
root=Path(args.root);baseline=Path(args.baseline)
kit=root/'F1/Assets/Art/LofiPilot';old=baseline/'F1/Assets/Art/LofiPilot'
issues=[];checks=[]
def check(ok,label):
    checks.append({'check':label,'pass':bool(ok)})
    if not ok:issues.append(label)
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
source=root/'tools/asset-gen/lofi_pilot.py'
tree=ast.parse(source.read_text(encoding='utf-8'))
original=ast.parse((baseline/'original.py').read_text(encoding='utf-8'))
funcs=lambda t:{n.name:ast.dump(n) for n in t.body if isinstance(n,ast.FunctionDef)}
now_funcs=funcs(tree);old_funcs=funcs(original)
names=list(old_funcs);props=names[names.index('awning'):]
check(all(now_funcs[n]==old_funcs[n] for n in props),'All Awning--Boat function ASTs unchanged')
check((root/'F1/Assets/Editor/CoastalPropsBuilder.cs').read_bytes()==(baseline/'CoastalPropsBuilder.cs').read_bytes(),'CoastalPropsBuilder unchanged')
check((root/'F1/Assets/Editor/CoastalTownDensity.cs').read_bytes()==(baseline/'CoastalTownDensity.cs').read_bytes(),'CoastalTownDensity unchanged')
palette=lambda t:ast.literal_eval(next(n.value for n in t.body if isinstance(n,ast.Assign) and any(isinstance(x,ast.Name) and x.id=='pal' for x in n.targets)))
tile_count=len(palette(original))
check(palette(tree)[:tile_count]==palette(original),'Original palette entries/order preserved')
before=bpy.data.images.load(str(old/'LofiAtlas.png'));after=bpy.data.images.load(str(kit/'LofiAtlas.png'))
bp=list(before.pixels);np=list(after.pixels)
same=[]
for tile in range(tile_count):
    x=(tile%8)*64;y=(tile//8)*64
    same.append(all(bp[((y+j)*512+x)*4:((y+j)*512+x+64)*4]==np[((y+j)*512+x)*4:((y+j)*512+x+64)*4] for j in range(64)))
check(all(same),'All pixels in original atlas tiles identical to baseline')
check(digest(old/'LofiAtlas.png')==digest(baseline/'LofiAtlas.before.png'),'Original generator atlas reproduces pre-task atlas byte-for-byte')
unchanged=[]
for path in old.glob('*.mesh.json'):
    if path.name.startswith(('CoastalHouse','CornerShop','RepairGarage')):continue
    unchanged.append(path.stem.replace('.mesh',''))
    check(path.read_bytes()==(kit/path.name).read_bytes(),path.name+' geometry/UV unchanged')

for name in ('CoastalHouse','CoastalHouseB','CoastalHouseC'):
    check((old/(name+'.mesh.json')).read_bytes()==(kit/(name+'.mesh.json')).read_bytes(),name+' original house geometry/UV preserved')
catalog=json.loads((root/'art/source/coastal-town/building-catalog.json').read_text(encoding='utf-8'))
models=[]
for entry in catalog['buildings']:
    name,w,d=entry['name'],entry['w'],entry['d']
    data=json.loads((kit/(name+'.mesh.json')).read_text())
    vv=[(v['x'],v['y'],v['z']) for v in data['vertices']]
    projection=max(max(abs(x)-w/2,abs(z)-d/2) for x,y,z in vv)
    # Clip every triangle edge to y=1.9/2.5; endpoints alone miss crossing faces.
    band=[]
    for i in range(0,len(data['triangles']),3):
        tri=[vv[j] for j in data['triangles'][i:i+3]]
        band.extend(p for p in tri if 1.90001<=p[1]<=2.49999)
        for a,b in zip(tri,tri[1:]+tri[:1]):
            for height in (1.90001,2.49999):
                if min(a[1],b[1])<height<max(a[1],b[1]):
                    t=(height-a[1])/(b[1]-a[1]);band.append(tuple(a[k]+t*(b[k]-a[k]) for k in range(3)))
    band_projection=max(max(abs(x)-w/2,abs(z)-d/2) for x,y,z in band)
    check(projection<=entry['maxProjection']+.00001,name+' within catalog projection allowance')
    check(len(data['triangles'])//3<=4500,name+' <=4500 triangles')
    check(band_projection<=.00201,name+' mounted band flush (<=2 mm decal skin)')
    check(abs(min(y for x,y,z in vv))<.00001,name+' ground origin')
    check(len(data['uv'])==len(vv) and all(0<=u[k]<=1 for u in data['uv'] for k in ('x','y')),name+' valid atlas UVs')
    models.append({'name':name,'body':[w,d],'maxProjection':round(projection,5),'mountedBandProjection':round(band_projection,5),'triangles':len(data['triangles'])//3})
report={'baseline':'Unmodified generator, seed 413, same Blender; isolated output root only.',
        'unchangedNonBuildingMeshes':len(unchanged),'originalAtlasTilesIdentical':sum(same),
        'models':models,'checks':checks,'issues':issues}
path=root/'art/source/coastal-town/building-faces-geometry.json'
path.write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({'checks':len(checks),'issues':issues,'models':models}))
if issues:raise RuntimeError('Building audit failed: '+str(issues))
