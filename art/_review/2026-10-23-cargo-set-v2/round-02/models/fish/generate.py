"""Real-scale fish shipping tray, opened for packing inspection. Blender bpy."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parent;D=64
manifest=json.loads((R/'atlas_manifest.json').read_text());rects={r['face']:r['rect_xywh'] for r in manifest['faces']}
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC'
mat=bpy.data.materials.new('palette_point_opaque');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1;bs.inputs['Metallic'].default_value=0;bs.inputs['Specular IOR Level'].default_value=0
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/f'{R.name}_atlas_64.png'));tex.interpolation='Closest';tex.extension='EXTEND'
vc=mat.node_tree.nodes.new('ShaderNodeVertexColor');vc.layer_name='ContactAO'
mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
mat.node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);mat.node_tree.links.new(vc.outputs['Color'],mix.inputs[2]);mat.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
parts=[]
def mesh(name,verts,faces):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);parts.append(o);return o
def block(name,loc,dim,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('one_step_bevel','BEVEL');m.width=bevel;m.segments=1;bpy.ops.object.modifier_apply(modifier=m.name)
 parts.append(o);return o
block('EPS_base',(0,0,.0075),(.4,.3,.015),.003)
block('EPS_front',(0,-.14125,.055),(.4,.0175,.08),.002)
block('EPS_back',(0,.14125,.045),(.4,.0175,.06),.002)
block('EPS_left',(-.19125,0,.055),(.0175,.265,.08),.002)
block('EPS_right',(.19125,0,.055),(.0175,.265,.08),.002)
ice_data=[(-.052,-.072,.057,.050,.036,.011,5),(.035,-.073,.056,.039,.037,.013,6),(.113,-.058,.055,.047,.032,.014,5),(-.025,.068,.057,.049,.035,.010,5),(.093,.062,.055,.045,.034,.013,5)]
for j,(x,y,z,w,h,t,n) in enumerate(ice_data):
 angle=j*.41;v=[]
 for layer in [0,1]:
  for i in range(n):
   a=angle+2*math.pi*i/n;r=1-.13*((i+j)%3)
   v.append((x+math.cos(a)*w*.5*r,y+math.sin(a)*h*.5*r,z+layer*t-(.004 if layer and i%2 else 0)))
 faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 mesh(f'ice_{j}',v,faces)
N=8;rings=[(-.135,.008,.006),(-.10,.022,.015),(-.035,.037,.021),(.055,.035,.020),(.105,.027,.018),(.145,.008,.008)]
v=[(x,math.cos(2*math.pi*i/N)*ry,.073+math.sin(2*math.pi*i/N)*rz) for x,ry,rz in rings for i in range(N)]
f=[]
for j in range(len(rings)-1):
 for i in range(N):f.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
f += [tuple(reversed(range(N))),tuple((len(rings)-1)*N+i for i in range(N))]
mesh('fish_body',v,f)
mesh('fish_tail',[(-.133,0,.077),(-.179,-.049,.087),(-.149,0,.083),(-.179,.049,.087),(-.134,0,.075)],[(0,1,2),(0,2,3),(4,2,1),(4,3,2),(0,4,1),(0,3,4)])
mesh('fish_fin',[(-.025,-.024,.085),(.020,-.052,.088),(.039,-.025,.084),(.020,-.050,.086)],[(0,1,2),(0,3,1),(1,3,2),(0,2,3)])
for sign in [-1,1]:
 cx,cy,cz=.112,sign*.021,.082
 ring=[(cx,cy,cz)]+[(cx+math.cos(a*math.pi/4)*.006,cy+sign*.0005,cz+math.sin(a*math.pi/4)*.006) for a in range(8)]
 mesh('eye_ring_'+str(sign),ring,[(0,i+1,(i+1)%8+1) for i in range(8)])
 eye=[(cx,cy,cz)]+[(cx+math.cos(a*math.pi/4)*.004,cy+sign*.001,cz+math.sin(a*math.pi/4)*.004) for a in range(8)]
 mesh('eye_'+str(sign),eye,[(0,i+1,(i+1)%8+1) for i in range(8)])
 mesh('gill_'+str(sign),[(.086,sign*.027,.079),(.090,sign*.028,.079),(.079,sign*.024,.092),(.075,sign*.023,.092)],[(0,1,2),(0,2,3)])
for j,(x0,x1,y) in enumerate([(.032,.138,.112),(-.04,.075,-.116)]):
 mesh('liner_fold_'+str(j),[(x0,y,.061),(x1,y,.068),(x1-.017,y-.012,.086),(x0+.012,y-.008,.084)],[(0,1,2),(0,2,3)])
records=[]
for o in parts:
 # A hollow wall has two equal-area opposite faces. Subdivide only the inside
 # face so checker representatives remain the independently unwrapped outside.
 # This prevents interior label repetition without merging exterior UV regions.
 if o.name.startswith('EPS'):
  old=o.data;verts=[tuple(v.co) for v in old.vertices];faces=[]
  for poly in old.polygons:
   center=poly.center+o.location
   if abs(poly.normal.z)<.95 and center.x*poly.normal.x+center.y*poly.normal.y<0:
    ci=len(verts);verts.append(tuple(poly.center));ids=list(poly.vertices)
    faces.extend((ids[i],ids[(i+1)%len(ids)],ci) for i in range(len(ids)))
   else:faces.append(tuple(poly.vertices))
  fresh=bpy.data.meshes.new(o.name+'_exterior_and_inner_fan');fresh.from_pydata(verts,[],faces);fresh.update();o.data=fresh
 o.data.materials.append(mat)
 while o.data.uv_layers:o.data.uv_layers.remove(o.data.uv_layers[0])
 uv=o.data.uv_layers.new(name='UVMap')
 for poly in o.data.polygons:
  pts=[o.data.vertices[o.data.loops[li].vertex_index].co+o.location for li in poly.loop_indices];n=poly.normal;axis=max(range(3),key=lambda a:abs(n[a]))
  region=('right' if n.x>0 else 'left') if axis==0 else (('back' if n.y>0 else 'front') if axis==1 else ('top' if n.z>0 else 'bottom'));x,y,w,h=rects[region]
  if abs(n.z)<.95:u=Vector((0,0,1)).cross(n).normalized();v=-n.cross(u).normalized()
  else:u=Vector((1,0,0));u=(u-n*u.dot(n)).normalized();v=n.cross(u).normalized()
  if o.name.startswith('fish_body') and abs(n.x)<.99:
   u=Vector((1,0,0));u=(u-n*u.dot(n)).normalized();v=n.cross(u).normalized()
  a=pts[0];coords=[((p-a).dot(u)*D,(p-a).dot(v)*D) for p in pts];minx=min(c[0] for c in coords);miny=min(c[1] for c in coords);width=max(c[0] for c in coords)-minx;height=max(c[1] for c in coords)-miny
  if o.name.startswith('ice'):x,y=1+(int(o.name.split('_')[-1])*3)%13,43+(int(o.name.split('_')[-1])*2)%8
  elif o.name.startswith('fish_body'):
   center=sum(pts,Vector())/len(pts);x=31+(center.x+.18)*D-width/2;y=(44 if n.z>.4 else 48 if n.z>-.2 else 51)-height/2
  elif o.name.startswith('fish_tail'):x,y=58,43
  elif o.name.startswith('fish'):x,y=39,59
  elif o.name.startswith('eye_ring'):x,y=48,60
  elif o.name.startswith(('eye','gill')):x,y=30,60
  elif o.name.startswith('liner'):x,y=48,59
  elif o.name.startswith('EPS'):
   center=sum(pts,Vector())/len(pts)
   if abs(n.z)<.95 and center.x*n.x+center.y*n.y<0:x,y=30,54
  for li,(xx,yy) in zip(poly.loop_indices,coords):uv.data[li].uv=((x+xx-minx)/64,1-(y+yy-miny)/64)
  poly.use_smooth=False;records.append({'object':o.name,'region':region,'uv_origin_pixels':[x,y],'normal':list(n)})
 ca=o.data.color_attributes.new(name='ContactAO',type='FLOAT_COLOR',domain='CORNER')
 for poly in o.data.polygons:
  for li in poly.loop_indices:
   z=o.data.vertices[o.data.loops[li].vertex_index].co.z+o.location.z;shade=.83 if z<.016 else 1.0;ca.data[li].color=(shade,shade,shade,1)
 bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('triangulate','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=R.name
bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o['density_px_m']=64;o['source_geometry']='Manufacturer dimensions, not personally measured; open inspection.'
bpy.ops.wm.save_as_mainfile(filepath=str(R/f'{R.name}.blend'))
bpy.ops.export_scene.gltf(filepath=str(R/f'{R.name}.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_extras=True,export_vertex_color='NAME',export_vertex_color_name='ContactAO')
(R/'geometry.json').write_text(json.dumps({'tris':len(o.data.polygons),'revision':'PD1720 pass2','gltf_dimensions_m':[.4,.095,.3],'density':64,'shape_change':'Manufacturer outer envelope retained; back wall lowered from95 to75mm for open-inspection readability, side/front rim retain95mm. Not an exact BEWI manufactured wall replica.','inner_wall_topology':'Inside-facing wall polygons centre-fanned; exterior original two triangles retained as largest representative. Inner UV at [30,54], no printed label/arrows. No density/checker threshold change.','regions':records},indent=2))
print(R.name,len(o.data.polygons),'tris')
