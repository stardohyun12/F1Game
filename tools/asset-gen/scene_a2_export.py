import sys
from pathlib import Path
import bpy,math,json
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[2];O=R/'art/models/garage_scene_a2_kit';E=R/'art/export/scene-a2-kit';V=R/'art/_review/2026-10-27-scene-a2-kit';meta=json.loads((O/'manifest.json').read_text(encoding='utf-8'));checks=json.loads((V/'technical-check.json').read_text()) if '--sheds' in sys.argv else []
for m in meta:
 name=m['name']
 if '--sheds' in sys.argv and name not in ['background_building_04','background_building_05']:continue
 checks=[c for c in checks if c['name']!=name]
 bpy.ops.wm.open_mainfile(filepath=str(O/(name+'.blend')));sc=bpy.context.scene;ob=bpy.data.objects[name]
 mat=bpy.data.materials.new(name+'_unlit');mat.use_nodes=True;n=mat.node_tree.nodes;n.clear();L=mat.node_tree.links;tx=n.new('ShaderNodeTexImage');tx.image=bpy.data.images.load(str(E/(name+'.png')));tx.interpolation='Closest';em=n.new('ShaderNodeEmission');L.new(tx.outputs['Color'],em.inputs[0]);tr=n.new('ShaderNodeBsdfTransparent');mix=n.new('ShaderNodeMixShader');L.new(tx.outputs['Alpha'],mix.inputs[0]);
 if name in ['background_building_04','background_building_05'] and m['pack']=='chillydurango-shacks-sheds':
  geo=n.new('ShaderNodeNewGeometry');sub=n.new('ShaderNodeMath');sub.operation='SUBTRACT';sub.inputs[0].default_value=1;L.new(geo.outputs['Backfacing'],sub.inputs[1]);mul=n.new('ShaderNodeMath');mul.operation='MULTIPLY';L.new(sub.outputs[0],mul.inputs[0]);L.new(tx.outputs['Alpha'],mul.inputs[1]);L.new(mul.outputs[0],mix.inputs[0])
 L.new(tr.outputs[0],mix.inputs[1]);L.new(em.outputs[0],mix.inputs[2]);out=n.new('ShaderNodeOutputMaterial');L.new(mix.outputs[0],out.inputs[0]);ob.data.materials.clear();ob.data.materials.append(mat)
 for f in ob.data.polygons:f.material_index=0
 for uv in list(ob.data.uv_layers):
  if uv.name!='BakedUV':ob.data.uv_layers.remove(uv)
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 bs=n.new('ShaderNodeBsdfPrincipled');L.new(tx.outputs['Color'],bs.inputs['Base Color']);L.new(tx.outputs['Alpha'],bs.inputs['Alpha']);L.new(bs.outputs[0],out.inputs[0]);bpy.ops.export_scene.fbx(filepath=str(E/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False)
 L.new(mix.outputs[0],out.inputs[0]);n.remove(bs)
 for o in list(sc.objects):
  if o!=ob:bpy.data.objects.remove(o,do_unlink=True)
 # Preview all assets at identical 24 pixels/m, fixed elevation/azimuth.
 bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.type='ORTHO';cam.rotation_euler=Vector((.55,1,-.28)).to_track_quat('-Z','Y').to_euler();sc.camera=cam;bpy.context.view_layer.update()
 inv=cam.matrix_world.inverted();ps=[inv@(ob.matrix_world@Vector(p)) for p in ob.bound_box];minx=min(p.x for p in ps)-.5;maxx=max(p.x for p in ps)+.5;miny=min(p.y for p in ps)-.5;maxy=max(p.y for p in ps)+.5;w=maxx-minx;h=maxy-miny;W=math.ceil(w*24);H=math.ceil(h*24);cam.data.ortho_scale=W/24;cam.data.sensor_fit='HORIZONTAL';cam.location=cam.rotation_euler.to_matrix()@Vector(((minx+maxx)/2,(miny+maxy)/2,100));cam.data.clip_end=500
 sc.render.engine='CYCLES';sc.cycles.samples=1;sc.cycles.use_denoising=False;sc.cycles.pixel_filter_type='BOX';sc.cycles.filter_width=.01;sc.cycles.transparent_max_bounces=16;sc.render.resolution_x=W;sc.render.resolution_y=H;sc.render.resolution_percentage=100;sc.render.film_transparent=True;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.render.dither_intensity=0;sc.render.filepath=str(V/(name+'.png'));bpy.ops.render.render(write_still=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/(name+'_unlit.blend')))
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/(name+'.fbx')));meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=sum(len(f.vertices)-2 for o in meshes for f in o.data.polygons);bpy.context.view_layer.update();pts=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box];size=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)];size_ok=all(abs(a-b)<.005 for a,b in zip(sorted(size),sorted(m['size'])));checks.append(dict(name=name,triangles=tris,size_m=size,size_pass=size_ok,passed=size_ok and tris==m['tris'] and all(o.data.uv_layers for o in meshes),preview_px=[W,H],preview_pixels_per_m=24));print('EXPORT_DONE',name,flush=True)
 (V/'technical-check.json').write_text(json.dumps(checks,indent=2))
