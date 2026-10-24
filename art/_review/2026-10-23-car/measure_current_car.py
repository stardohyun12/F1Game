"""Read-only measurement of the currently referenced Unity FBX and saved scene YAML.
Run: Blender -b --python art/_review/2026-10-23-car/measure_current_car.py
Writes only this review folder. Does not launch or modify Unity.
"""
import bpy, json, math, re, hashlib
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent

def read(p): return (ROOT/p).read_text(encoding='utf-8-sig')
def blocks(s):
    return {m.group(2):(int(m.group(1)),m.group(3)) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
def vec(s, key):
    x=re.search(r'^  '+re.escape(key)+r': \{([^}]+)\}',s,re.M)
    return {k:float(v) for k,v in re.findall(r'(\w+): ([\-\d.e+]+)',x.group(1))} if x else None
def modifications(s):
    return [{'target':int(m.group(1)),'path':m.group(2).strip("'"),'value':m.group(3).strip(),'reference':m.group(4)} for m in re.finditer(r'    - target: \{fileID: (-?\d+),[^\n]+\}\n      propertyPath: (.+)\n      value:([^\n]*)\n      objectReference: \{([^}]+)\}',s)]
def modvalue(ms,target,path):
    return next(float(m['value']) for m in ms if m['target']==target and m['path']==path)
def listvec(v): return [round(float(c),9) for c in v]
def bounds(coords):
    lo=[min(v[i] for v in coords) for i in range(3)]
    hi=[max(v[i] for v in coords) for i in range(3)]
    return {'min':listvec(lo),'max':listvec(hi),'dimensions':listvec([hi[i]-lo[i] for i in range(3)]),'center':listvec([(hi[i]+lo[i])/2 for i in range(3)])}
def measure():
    objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
    result={}
    allcoords=[]
    for o in objs:
        mesh=o.data; mesh.calc_loop_triangles()
        coords=[o.matrix_world@v.co for v in mesh.vertices]; allcoords.extend(coords)
        result[o.name]={'tris':len(mesh.loop_triangles),'world_origin_blender':listvec(o.matrix_world.translation),'local_position':listvec(o.location),'local_scale':listvec(o.scale),'local_rotation_euler':listvec(o.rotation_euler),'bounds_blender':bounds(coords),'uv_layer_count':len(mesh.uv_layers)}
    return {'objects':result,'total_tris':sum(o['tris'] for o in result.values()),'bounds_blender':bounds(allcoords)}

scene=blocks(read('F1/Assets/Scenes/Proto_CoastCourse.unity'))
prefab=blocks(read('F1/Assets/Art/CoastalFormula/FormulaGameplayVisual.prefab'))
race=blocks(read('F1/Assets/Prefabs/Race.prefab'))
visualmods=modifications(scene['444362461'][1]); racemods=modifications(scene['374189954'][1]); racedefault=modifications(race['8687216822286967902'][1])
visualscale=[modvalue(visualmods,2749762494709477545,'m_LocalScale.'+a) for a in 'xyz']
visualpos=[modvalue(visualmods,2749762494709477545,'m_LocalPosition.'+a) for a in 'xyz']
racescale=[modvalue(racedefault,-8679921383154817045,'m_LocalScale.'+a) for a in 'xyz']
racepos=[modvalue(racemods,9152872249828246453,'m_LocalPosition.'+a) for a in 'xyz']
net=[visualscale[i]*racescale[i] for i in range(3)]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'F1/Assets/Art/CoastalFormula/CoastalFormula.fbx'))
bpy.context.view_layer.update()
fbx=measure()
# Save an audit-only copy with authoritative FBX geometry and the active worn atlas.
for o in bpy.context.scene.objects:
    if o.type!='MESH': continue
    mat=bpy.data.materials.new(o.name+'_current_worn_audit'); mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF'); bs.inputs['Roughness'].default_value=0.9
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image=bpy.data.images.load(str(ROOT/'F1/Assets/Art/CoastalFormula'/('FormulaAtlas.png' if o.name=='driver' else 'FormulaAtlasWorn.png')),check_existing=True)
    tex.interpolation='Closest'; mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    o.data.materials.clear();o.data.materials.append(mat)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'current-car-source.blend'))

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(ROOT/'art/source/coastal-formula/CoastalFormula.glb'))
bpy.context.view_layer.update()
glb=measure()

go_names={int(k): re.search(r'^  m_Name: (.+)$',s,re.M).group(1) for k,(t,s) in prefab.items() if t==1}
transforms={}
for k,(t,s) in prefab.items():
    if t!=4: continue
    go=int(re.search(r'm_GameObject: \{fileID: (\d+)',s).group(1))
    name=go_names[go]
    transforms[name]={'file_id':int(k),'position_unity':vec(s,'m_LocalPosition'),'rotation_unity':vec(s,'m_LocalRotation'),'scale_unity':vec(s,'m_LocalScale'),'parent_file_id':int(re.search(r'm_Father: \{fileID: (\d+)',s).group(1))}

assetpaths=['F1/Assets/Art/CoastalFormula/CoastalFormula.fbx','art/source/coastal-formula/CoastalFormula.glb','F1/Assets/Scenes/Proto_CoastCourse.unity','F1/Assets/Art/CoastalFormula/FormulaGameplayVisual.prefab','F1/Assets/Prefabs/Race.prefab']
result={'units':'meters','coordinate_note':'Blender source = X width, Y depth, Z up; nose -Y. Unity prefab = +Z front, +Y up; Unity X = negative Blender X in the existing FBX/prefab conversion.','source_files':{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in assetpaths},'source_trace':{'scene':'F1/Assets/Scenes/Proto_CoastCourse.unity','visual_prefab':'F1/Assets/Art/CoastalFormula/FormulaGameplayVisual.prefab','visual_prefab_guid':'ee48b362edabaa245b556832a047788e','mesh_fbx':'F1/Assets/Art/CoastalFormula/CoastalFormula.fbx','mesh_fbx_guid':'c098ca4be9a433845a9dfe6306945ea0','physics_parent':'F1/Assets/Prefabs/Race.prefab','physics_parent_guid':'356a5e3b096c28641af798c7539095f8'},'scene_transform':{'visual_local_scale':visualscale,'visual_local_position_unity':visualpos,'race_inherited_scale':racescale,'combined_world_scale':net,'race_saved_position_unity':racepos,'visual_origin_offset_from_race_world_axes_unity':[visualpos[i]*racescale[i] for i in range(3)],'visual_saved_world_origin_unity':[racepos[i]+visualpos[i]*racescale[i] for i in range(3)],'runtime_note':'Saved scene only: runtime physics position, restoration state and render result not inspected.'},'unity_prefab_transforms':transforms,'fbx_import_measurement':fbx,'glb_import_measurement':glb,'wheel_rotation_contract':{'spin_axis_unity':[1,0,0],'steer_axis_unity':[0,1,0],'front_radius_source_m':0.35,'rear_radius_source_m':0.375,'source':'F1/Assets/Scripts/Garage/FormulaVehicleVisual.cs','names':['wheel_front_left','wheel_front_right','wheel_rear_left','wheel_rear_right']},'material_observation':{'not_untextured':True,'worn_texture':'F1/Assets/Art/CoastalFormula/FormulaAtlasWorn.png','restored_driver_texture':'F1/Assets/Art/CoastalFormula/FormulaAtlas.png','scene_material':'F1/Assets/Scenes/Proto_CoastCourse_Mats/FormulaWorn.mat','visual_diagnosis_note':'Texture is referenced in saved material; looking flat in screenshot does not prove absence of texture. Actual runtime material not verified.'},'execution':{'blender_version':bpy.app.version_string,'unity_editor_used':False,'files_under_F1_modified':False,'source_copy_blend':'art/_review/2026-10-23-car/current-car-source.blend'}}
(OUT/'current-car-measure.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'fbx':fbx['bounds_blender'],'glb':glb['bounds_blender'],'tris':fbx['total_tris'],'net_scale':net,'wheel_pivots':{k:v['position_unity'] for k,v in transforms.items() if k.startswith('wheel_')}},indent=2))
