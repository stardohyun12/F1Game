"""Metric packages, native 64px/m orthonormal face UV, shared CC0 atlas."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
kind=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'cake'
repo=Path(__file__).resolve().parents[3];root=repo/f'art/models/cargo_{kind}_v01'
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
if kind=='cake':
 block('cake_board',(0,0,.002),(.254,.254,.004))
 for sign in [-1,1]:
  block('front_back_paper',(0,sign*(W/2-.001),H/2),(W,.002,H))
  block('side_paper',(sign*(W/2-.001),0,H/2),(.002,W-.004,H))
 # Four real lid rails leave an opening. Thin film uses material alpha, not fake window image.
 for sign in [-1,1]:
  block('lid_rail',(sign*.111,0,H-.001),(.032,W,.002))
  block('lid_rail',(0,sign*.111,H-.001),(.190,.032,.002))
 film=mesh('clear_polypropylene_window',[(-.095,-.095,.126),(.095,-.095,.126),(.095,.095,.126),(-.095,.095,.126)],[(0,1,2,3)],'cream')
 glass=mat.copy();glass.name='window_film_alpha';glass.node_tree.nodes.get('Principled BSDF').inputs['Alpha'].default_value=.07;glass.surface_render_method='DITHERED';film.data.materials[0]=glass
 ring('sponge_cake',.103,.004,.070,'sponge');ring('cream_top',.104,.07,.082,'cream')
 for x,y in [(-.038,-.025),(.032,-.025),(0,.038)]:
  bpy.ops.mesh.primitive_cone_add(vertices=6,radius1=.012,radius2=.006,depth=.018,location=(x,y,.091));o=bpy.context.object;o.name='berry';o.data.materials.append(mat);o['region']='fruit';parts.append(o)
else:
 # Hartmann Eggtec Cargo S 280x280x44mm + top tray25mm. 30 molded lid pockets.
 block('lower_rim',(0,0,.024),(.280,.280,.040))
 block('lid_seam',(0,0,.045),(.280,.280,.002))
 for sign in [-1,1]:
  block('lid_edge',(sign*.137,0,.057),(.006,.280,.024))
  block('lid_edge',(0,sign*.137,.057),(.268,.006,.024))
 for row in range(5):
  for col in range(6):
   x=-.134+col*.268/6;y=-.134+row*.268/5;dx=.268/6;dy=.268/5
   outer=[(.15,0),(.85,0),(1,.15),(1,.85),(.85,1),(.15,1),(0,.85),(0,.15)]
   vs=[(x+u*dx,y+v*dy,.046) for u,v in outer]+[(x+dx*(.5+(u-.5)*.58),y+dy*(.5+(v-.5)*.58),.069) for u,v in outer]
   mesh('lid_pocket_%02d'%(row*6+col),vs,[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]+[tuple(range(8,16))])
   # Thirty rounded-off trapezoid feet carry the actual tray grid silhouette.
   fx=x+dx/2;fy=y+dy/2
   mesh('tray_foot',[(fx+a,fy+b,z) for z,r in [(0,.012),(.006,.019)] for a,b in [(-r,-r),(r,-r),(r,r),(-r,r)]],[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
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
   x,y={'cream':(1,34),'fruit':(23,34),'sponge':(45,34)}.get(special,rects[region][:2]);coords=[(x+px-mx,y+py-my) for px,py in pts]
  for li,(u,v) in zip(poly.loop_indices,coords):uv.data[li].uv=(u/64,1-v/64)
  poly.use_smooth=False
 bpy.context.view_layer.objects.active=o;t=o.modifiers.new('triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=t.name)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();asset=bpy.context.object;asset.name=f'cargo_{kind}_v01';sc.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
asset['density_px_m']=64;asset['source']='Manufacturer product dimensions; not personally measured';asset['AO']='Palette baked fold/contact bands; no generated image';tris=len(asset.data.polygons);assert tris<=2000,tris
bpy.ops.wm.save_as_mainfile(filepath=str(root/f'cargo_{kind}_v01.blend'));bpy.ops.export_scene.gltf(filepath=str(root/f'cargo_{kind}_v01.glb'),export_format='GLB',use_selection=True,export_yup=True,export_extras=True)
(root/'geometry.json').write_text(json.dumps({'tris':tris,'dimensions_blender_m':list(asset.dimensions),'dimensions_gltf_m':[W,H,W],'density':64,'component_count':len(parts)},indent=2))
print('DONE',kind,tris)
