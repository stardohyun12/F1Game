"""Real bpy-only comparison + street asset arrangement, never Unity evidence."""
from pathlib import Path
import sys,json,bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
ROOT=b.ROOT;REV=ROOT/'art/_review/2026-10-25-bayloop-italy';SRC=ROOT/'art/models/bayloop_italy_v01'
m=json.loads((SRC/'manifest.json').read_text());gates=json.loads((REV/'gate-summary.json').read_text())
def imported(path,x=0,y=0,z=0):
 before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(path));objs=[o for o in bpy.data.objects if o not in before]
 for ob in objs:
  if ob.parent is None:ob.location+=Vector((x,y,z))
 return objs
# Re-render archived machiya at the exact current shop A camera/light, no rescaling.
bpy.ops.wm.read_factory_settings(use_empty=True);sc=b.stage();sc.cycles.samples=16
imported(REV/'iterations/pre-d137/bayloop_buildings_v01/shop_house_a.glb')
a=next(a for a in m['assets'] if a['id']=='shop_house_a');h=a['dimensions_m'][1]
b.render(sc,REV/'machiya_same_camera.png',(13,-22,12),(0,3,h*.46),max(20,h*2.65,a['dimensions_m'][0]*1.8))
# A three-front street study. All imported assets preserve metre scale and materials.
bpy.ops.wm.read_factory_settings(use_empty=True);sc=b.stage();sc.cycles.samples=24
placements=[]
for name,x in [('shop_house_a',-6.4),('shop_house_b',0),('shop_house_c',6.7)]:
 if gates.get(name,{}).get('score',0)<7:continue
 imported(SRC/(name+'.glb'),x);placements.append(dict(id=name,xyz_blender=[x,0,0]))
props=json.loads((ROOT/'art/models/bayloop_italy_props_v01/manifest.json').read_text())['assets']
locations={'vending_machine':(-3.4,-.15,0),'planter_row':(8.1,-.3,0),'bicycle':(4.1,-.5,0),'lemon_tree_pot':(-8.7,-.5,0),'mailbox':(-4.0,-.08,1.1),'meter_box':(9.25,-.05,.9),'ac_unit':(9.3,-.05,2.2)}
for a in props:
 if a['id'] not in locations or gates.get(a['id'],{}).get('score',0)<7:continue
 xyz=locations[a['id']];imported(ROOT/a['model'],*xyz);placements.append(dict(id=a['id'],xyz_blender=list(xyz)))
b.render(sc,REV/'street-study-960.png',(15,-32,15),(0,2,4.6),30)
(REV/'street-study-placement.json').write_text(json.dumps({'placements':placements,'scope':'Real Blender asset arrangement only; not game capture, no collision/route proof; actual metre scale'},indent=2),encoding='utf8')
print('CONTEXT_DONE')
