"""Five shared texture slots only. No newly painted per-building texture."""
from pathlib import Path
import bpy,math,json
from mathutils import Vector
R=Path('D:/Github/F1Game');W=R/'art/source/q3-depot';E=R/'art/export/q3-depot';V=R/'art/_review/2026-10-28-q3-depot'
E.mkdir(parents=True,exist_ok=True);V.mkdir(parents=True,exist_ok=True)
import shutil,hashlib,csv
for filename in ['wall.png','trim.png','floor.png','roof.png','decals.png','palette.gpl','layout.json','provenance.csv']:
 shutil.copy2(R/'art/export/kit-shared-v2'/filename,E/filename)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
layout=json.loads((E/'layout.json').read_text());mats={};slots={};uvstats=[]
for slot in ['wall','trim','floor','roof','decals']:
 m=bpy.data.materials.new('Shared_'+slot);m.use_nodes=True;n=m.node_tree.nodes;b=n.get('Principled BSDF');b.inputs['Roughness'].default_value=.9
 t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(E/(slot+'.png')));t.interpolation='Closest';m.node_tree.links.new(t.outputs['Color'],b.inputs['Base Color'])
 if slot=='decals':m.node_tree.links.new(t.outputs['Alpha'],b.inputs['Alpha']);m.surface_render_method='DITHERED'
 mats[slot]=m
 for c in layout[slot]:slots[c['name']]=(slot,c['rect_px'])

def face(name,pts,cell,uv_m=None):
 slot,rect=slots[cell];x,y,w,h=rect
 if uv_m is None:
  a,b,c,d=[Vector(p) for p in pts];uu=(b-a).length;vv=(d-a).length;uv_m=[(0,0),(uu,0),(uu,vv),(0,vv)]
 if slot=='decals':
  uv=[((x+.5+u*(w-1))/512,1-(y+h-.5-v*(h-1))/512) for u,v in uv_m]
 else:
  assert max(u for u,v in uv_m)*32<w+.01 and max(v for u,v in uv_m)*32<h+.01,(name,cell,uv_m)
  uv=[((x+.01+u*32)/512,1-(y+h-.01-v*32)/512) for u,v in uv_m]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(pts,[],[(0,1,2,3)]);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);mesh.materials.append(mats[slot]);layer=mesh.uv_layers.new(name='UVMap')
 for i,v in enumerate(uv):layer.data[i].uv=v
 if slot!='decals':
  edges=[]
  for i,j in [(0,1),(0,3)]:
   length=(Vector(pts[j])-Vector(pts[i])).length
   if length:edges.append((Vector(uv[j])-Vector(uv[i])).length*512/length)
  uvstats.append(dict(object=name,cell=cell,texel_per_m=edges))
 return o

def box(name,center,size,cell):
 x,y,z=center;a,b,c=[v/2 for v in size];lo=[x-a,y-b,z-c];hi=[x+a,y+b,z+c]
 faces=[[(lo[0],lo[1],lo[2]),(hi[0],lo[1],lo[2]),(hi[0],lo[1],hi[2]),(lo[0],lo[1],hi[2])],
 [(hi[0],hi[1],lo[2]),(lo[0],hi[1],lo[2]),(lo[0],hi[1],hi[2]),(hi[0],hi[1],hi[2])],
 [(hi[0],lo[1],lo[2]),(hi[0],hi[1],lo[2]),(hi[0],hi[1],hi[2]),(hi[0],lo[1],hi[2])],
 [(lo[0],hi[1],lo[2]),(lo[0],lo[1],lo[2]),(lo[0],lo[1],hi[2]),(lo[0],hi[1],hi[2])],
 [(lo[0],lo[1],hi[2]),(hi[0],lo[1],hi[2]),(hi[0],hi[1],hi[2]),(lo[0],hi[1],hi[2])],
 [(lo[0],hi[1],lo[2]),(hi[0],hi[1],lo[2]),(hi[0],lo[1],lo[2]),(lo[0],lo[1],lo[2])]]
 out=[]
 for i,p in enumerate(faces):
  # Long frame axes map along U; do not stretch a 16px band over 7 metres.
  a,b,c,d=[Vector(v) for v in p];uu=(b-a).length;vv=(d-a).length;_,r=slots[cell]
  if vv*32>r[3] and uu*32<=r[3]:p=[p[1],p[2],p[3],p[0]]
  out.append(face(name+'_'+str(i),p,cell))
 bpy.ops.object.select_all(action='DESELECT')
 for o in out:o.select_set(True)
 bpy.context.view_layer.objects.active=out[0];bpy.ops.object.join();joined=bpy.context.object
 import bmesh
 bm=bmesh.new();bm.from_mesh(joined.data);bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=.000001);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(joined.data);bm.free();joined.select_set(False)
 return [joined]

