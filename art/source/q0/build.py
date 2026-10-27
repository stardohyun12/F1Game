import bpy,json,hashlib,shutil
from pathlib import Path
import numpy as np
R=Path('D:/Github/F1Game');W=R/'art/source/q0';E=R/'art/export/scene-albedo-kit';checks=[];spec={d['name']:d for d in json.loads((R/'art/source/photo-kit/manifest.json').read_text())}
def mat(path):
 m=bpy.data.materials.new(path.stem+'_albedo');m.use_nodes=True;n=m.node_tree.nodes;bs=n.get('Principled BSDF');bs.inputs['Roughness'].default_value=.85;bs.inputs['Metallic'].default_value=0;t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path));t.interpolation='Closest';m.node_tree.links.new(t.outputs[0],bs.inputs['Base Color']);return m
def sig(o):return hashlib.sha256(np.array([v.co[:] for v in o.data.vertices]).tobytes()+str([tuple(f.vertices) for f in o.data.polygons]).encode()+np.array(o.matrix_world).tobytes()).hexdigest()
for name in ['vending_G1','vending_G2','ac_G3']:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(R/'art/export/scene-a6-photo-kit'/(name+'.fbx')));o=next(o for o in bpy.context.scene.objects if o.type=='MESH');before=sig(o);w,dep,h=spec[name]['size'];fw,fh=(76,114) if name.startswith('vending') else (52,32)
 uv=o.data.uv_layers.active
 for f in o.data.polygons:
  normal=o.matrix_world.to_3x3()@f.normal;points=[o.matrix_world@o.data.vertices[i].co for i in f.vertices];front=normal.y<-.99 and max(p.z for p in points)-min(p.z for p in points)>.2;foot=max(p.z for p in points)<.051
  for li in f.loop_indices:
   p=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
   if front:uv.data[li].uv=((8+(p.x/w+.5)*fw)/256,1-(8+(1-(p.z-.05)/(h-.05))*fh)/256)
   else:
    axes=[i for i in range(3) if i!=max(range(3),key=lambda i:abs(normal[i]))];u=(p[axes[0]]*64)%30;v=(p[axes[1]]*64)%30;uv.data[li].uv=((208+u if foot else 128+u)/256,1-(8+v)/256)
 o.data.materials.clear();o.data.materials.append(mat(E/(name+'.png')))
 for f in o.data.polygons:f.material_index=0
 assert sig(o)==before;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.export_scene.fbx(filepath=str(E/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False)
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/(name+'.fbx')));n=next(o for o in bpy.context.scene.objects if o.type=='MESH');n.data.calc_loop_triangles();pts=[n.matrix_world@v.co for v in n.data.vertices];dims=[max(p[k] for p in pts)-min(p[k] for p in pts) for k in range(3)];assert max(abs(x-y) for x,y in zip(dims,spec[name]['size']))<1e-5
 checks.append(dict(name=name,geometry_unchanged_before_export=True,tris=len(n.data.loop_triangles),dimensions=dims,uv_changed=True,uv_layers=len(n.data.uv_layers),front_pixels=[fw,fh],front_texels_per_m=[fw/w,fh/(h-.05)],bake_passes=0))
 shutil.copy2(E/(name+'.png'),E/(name+'.fbm')/(name+'.png'))
# Unchanged postbox is included for review and pairing, not redesigned.
shutil.copy2(R/'art/export/scene-a6-photo-kit/postbox_G1.fbx',E/'postbox_G1.fbx');(E/'postbox_G1.fbm').mkdir(exist_ok=True);shutil.copy2(E/'postbox_G1.png',E/'postbox_G1.fbm/postbox_G1.png')
(W/'geometry-validation.json').write_text(json.dumps(checks,indent=2));print(checks)
