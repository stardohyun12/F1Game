"""Metric packages, native 64px/m orthonormal face UV, shared CC0 atlas."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
kind=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'cake'
repo=Path(__file__).resolve().parents[3];root=repo/f'art/models/cargo_{kind}_v02'
W,H=(.254,.127) if kind=='cake' else (.280,.069)
manifest=json.loads((root/'atlas_manifest.json').read_text());rects={r['face']:r['rect_xywh'] for r in manifest['faces']}
bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.unit_settings.system='METRIC'
mat=bpy.data.materials.new('CC0_paper_point');mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=1;p.inputs['Specular IOR Level'].default_value=0
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/f'cargo_{kind}_atlas_64.png'));tex.interpolation='Closest';tex.extension='EXTEND';mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
parts=[]
def mesh(name,vs,fs,region=None):
 m=bpy.data.meshes.new(name);m.from_pydata(vs,[],fs);m.update();o=bpy.data.objects.new(name,m);sc.collection.objects.link(o);m.materials.append(mat);o['region']=region or '';parts.append(o);return o
def block(name,loc,dim,region=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat);o['region']=region or '';parts.append(o);return o
def ring(name,r,z0,z1,region='cream',n=16):
 vs=[(r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),z) for z in [z0,z1] for i in range(n)]
 fs=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,vs,fs,region)
def organic(name,cx,cy,profiles,region,n=8,cap=True):
 # Several curved profile rings and a point crown: no flat polygonal pillar cap.
 vs=[(cx+r*math.cos(i*2*math.pi/n),cy+r*math.sin(i*2*math.pi/n),z) for z,r in profiles[:-1] for i in range(n)]
 vs.append((cx,cy,profiles[-1][0]));k=len(profiles)-1
 fs=([tuple(range(n-1,-1,-1))] if cap else [])+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(k-1) for i in range(n)]+[((k-1)*n+i,(k-1)*n+(i+1)%n,k*n) for i in range(n)]
 return mesh(name,vs,fs,region)
if kind=='cake':
 block('cake_board',(0,0,.002),(.254,.254,.004))
 block('back_paper',(0,W/2-.001,H/2),(W,.002,H))
 block('front_low_paper',(0,-W/2+.001,.027),(W,.002,.054))
 for sign in [-1,1]:
  block('side_paper',(sign*(W/2-.001),0,H/2),(.002,W-.004,H))
  block('front_window_pillar',(sign*.113,-W/2+.001,.0905),(.028,.002,.073))
 block('front_window_top',(0,-W/2+.001,.122),(W,.002,.010))
 # Four real lid rails leave an opening. Thin film uses material alpha, not fake window image.
 for sign in [-1,1]:
  block('lid_rail',(sign*.111,0,H-.001),(.032,W,.002))
  block('lid_rail',(0,sign*.111,H-.001),(.190,.032,.002))
 # Optically clear film omitted from opaque raster; narrow edge glints denote the closed sheet.
 # No alpha dither layer means no subpixel speckle in the common low-res render.
 block('top_film_edge_glint',(-.092,0,.125),(.002,.162,.0005),'glint')
 block('front_film_edge_glint',(-.094,-.12675,.084),(.001,.0005,.052),'glint')
 ring('sponge_cake',.103,.004,.044,'sponge')
 ring('cream_middle',.104,.044,.066,'cream')
 ring('sponge_upper',.103,.066,.076,'sponge')
 ring('cream_top',.104,.076,.089,'cream')
 for i,(x,y) in enumerate([(-.043,-.025),(.040,-.025),(0,.041)]):
  organic('strawberry_%d'%i,x,y,[(.089,.010),(.097,.016),(.112,.012),(.121,0)],'fruit')
  # A small green calyx supplies a recognisable strawberry cue at close packing scale.
  block('strawberry_leaf',(x,y,.119),(.018,.005,.001),'leaf')
  block('strawberry_leaf',(x,y,.119),(.005,.016,.001),'leaf')
else:
 # Manufacturer outer envelope retained; new slotted protective pulp lid reveals real egg crowns.
 block('tray_base',(0,0,.008),(.272,.272,.016))
 for sign in [-1,1]:
  block('tray_side',(sign*.135,0,.032),(.010,.280,.048))
  block('tray_front_back',(0,sign*.135,.028),(.260,.010,.040))
  block('lid_side',(sign*.135,0,.065),(.010,.280,.008))
  block('lid_front_back',(0,sign*.134,.065),(.260,.012,.008))
 # Four thin bridges make five long vents/windows while the protective lid stays closed.
 for row in range(4):block('lid_bridge',(0,-.075+row*.050,.064),(.260,.004,.006))
 for row in range(5):
  for col in range(6):
   x=-.1075+col*.043;y=-.100+row*.050
   organic('egg_%02d'%(row*6+col),x,y,[(.016,.011),(.035,.019),(.053,.0155),(.0635,.0085),(.0675,0)],'cream',cap=False)
for o in parts:
 while o.data.uv_layers:o.data.uv_layers.remove(o.data.uv_layers[0])
 uv=o.data.uv_layers.new(name='UVMap')
 for poly in o.data.polygons:
  n=poly.normal;axis=max(range(3),key=lambda i:abs(n[i]));region=('right' if n.x>0 else 'left') if axis==0 else (('back' if n.y>0 else 'front') if axis==1 else ('top' if n.z>0 else 'bottom'))
  vs=[o.data.vertices[o.data.loops[i].vertex_index].co+o.location for i in poly.loop_indices]
  special=o.get('region','')
  if abs(n[axis])>.999 and not special:
   x,y,_,_=rects[region];coords=[]
   for p in vs:
    a,b=(p.x+W/2,H-p.z) if region=='front' else ((W/2-p.x,H-p.z) if region=='back' else ((p.y+W/2,H-p.z) if region=='right' else ((W/2-p.y,H-p.z) if region=='left' else ((p.x+W/2,W/2-p.y) if region=='top' else (p.x+W/2,p.y+W/2)))))
    coords.append((x+a*64,y+b*64))
  else:
   a=vs[0];edges=[vs[(i+1)%len(vs)]-p for i,p in enumerate(vs)];u=max(edges,key=lambda e:e.length).normalized();v=n.cross(u).normalized();pts=[((p-a).dot(u)*64,(p-a).dot(v)*64) for p in vs];mx=min(p[0] for p in pts);my=min(p[1] for p in pts)
   x,y={'cream':(1,34),'fruit':(23,34),'sponge':(45,34),'leaf':(1,56),'glint':(23,56)}.get(special,rects[region][:2]);coords=[(x+px-mx,y+py-my) for px,py in pts]
  for li,(u,v) in zip(poly.loop_indices,coords):uv.data[li].uv=(u/64,1-v/64)
  poly.use_smooth=False
 bpy.context.view_layer.objects.active=o;t=o.modifiers.new('triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=t.name)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();asset=bpy.context.object;asset.name=f'cargo_{kind}_v02';sc.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
asset['density_px_m']=64;asset['source']='Manufacturer product dimensions; not personally measured';asset['AO']='Palette baked fold/contact bands; no generated image';tris=len(asset.data.polygons);assert tris<=2000,tris
bpy.ops.wm.save_as_mainfile(filepath=str(root/f'cargo_{kind}_v02.blend'));bpy.ops.export_scene.gltf(filepath=str(root/f'cargo_{kind}_v02.glb'),export_format='GLB',use_selection=True,export_yup=True,export_extras=True)
(root/'geometry.json').write_text(json.dumps({'tris':tris,'dimensions_blender_m':list(asset.dimensions),'dimensions_gltf_m':[W,H,W],'density':64,'component_count':len(parts)},indent=2))
print('DONE',kind,tris)
