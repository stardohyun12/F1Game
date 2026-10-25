"""D141 measured street review scene; real exported geometry, no AI plate."""
from pathlib import Path
import sys,json,math
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
ROOT=b.ROOT;O=ROOT/'art/models/bayloop_props2_v01';R=ROOT/'art/_review/2026-10-25-bayloop-props2'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC'
tm=json.loads((O/'texture-manifest.json').read_text());b.TEX=O/'Props2PhotoAtlas.png';b.TILES.update(tm['aliases']);mat=b.material();mat.name='BayLoop_Props2_Photo32'
b.TEX=O/'Props2PavingAtlas.png';roadmat=b.material();roadmat.name='BayLoop_Props2_Paving32';b.TILES['roadbase']=12
records=[]
def surface(name,x,y,z,w,d,h,tile):
 b.V.clear();b.F.clear();b.UV.clear();b.PARTS.clear();b.PART=name;b.box(x,y,z,w,d,h,tile);b.mesh(name,roadmat if tile=='roadbase' else mat)
surface('REVIEW_road',0,-1.5,-.1,35,6,.1,'roadbase')
surface('REVIEW_paved_frontage',0,5,-.1,35,7,.22,'stone')
surface('REVIEW_far_side_paving',0,-5.4,-.1,35,1.8,.22,'stone')
# Low kerb interrupted at a garage/side passage rather than a continuous barrier.
for x,w in [(-10,12),(7,15)]:surface('REVIEW_kerb',x,1.5,.0,w,.15,.12,'trim')
def place(path,x,y,z,yaw=0,kind='prop'):
 before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(path));obs=[o for o in bpy.data.objects if o not in before];roots=[o for o in obs if o.parent not in obs]
 for o in roots:o.location=(x,y,z);o.rotation_euler[2]+=yaw
 records.append(dict(path=path.relative_to(ROOT).as_posix(),position_blender_m=[x,y,z],yaw_degrees=math.degrees(yaw),kind=kind))
for name,x,y,angle in [('shop_house_01',-8,4.6,.025),('town_house_04',-.5,5.15,-.02),('terrace_house_01',8,6.2,0)]:place(ROOT/'art/models/bayloop_variety_v01'/(name+'.glb'),x,y,.12,angle,'existing_passed_building')
layout=[('parked_car_01',-5,-.1,0,math.pi/2),('parked_car_03',4,-3.4,0,-math.pi/2),('three_wheeler_01',7,.3,0,math.pi/2),('scooter_01',-9.8,3.35,.12,.3),('scooter_02',5.3,4.1,.12,-.35),('shop_display_01',-7.4,3.9,.12,0),('cafe_set_01',-2.6,3.25,.12,.1),('parasol_01',-3.65,3.8,.12,0),('traffic_mirror_01',10.4,2.0,.12,-.35),('street_sign_02',-12.4,2.05,.12,0),('street_sign_03',4.3,1.85,.12,0),('road_patch_01',.2,-1.6,.002,.13),('road_patch_02',-10,-3.1,.002,.7),('crosswalk_paint_01',10,-1.5,.002,math.pi/2),('mailbox_cluster_01',-.5,5.0,1.22,0),('laundry_line_01',8,6.0,3.5,0)]
for name,x,y,z,a in layout:
 path=O/(name+'.glb')
 if not path.exists():raise RuntimeError('Missing required context prop '+name)
 place(path,x,y,z,a)
sc=b.stage();sc.cycles.samples=24
# Correctly placed kerb and objects are independent of the review-only backdrop.
b.render(sc,R/'street-960.png',(16,-26,16),(0,2.5,3.2),34)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'street-review.blend'))
(R/'street-layout.json').write_text(json.dumps(dict(render='street-960.png',resolution=[960,540],units='metres',road_width_m=6,paved_frontage_width_m=7,kerb_height_m=.12,placements=records,unity_scene=False,limitations='Static Blender arrangement, not game navigation or collision proof; no actual traffic-sign standard certification'),indent=2),encoding='utf8')
print('CONTEXT_DONE',len(records))
