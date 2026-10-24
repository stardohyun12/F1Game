import bpy,math,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parent;fish='fish' in R.name;D=64
manifest=json.loads((R/'atlas_manifest.json').read_text());rects={r['face']:r['rect_xywh'] for r in manifest['faces']}
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system='METRIC'
mat=bpy.data.materials.new('authored_prop_palette_point');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=1;bs.inputs['Metallic'].default_value=0;bs.inputs['Specular IOR Level'].default_value=0
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/f'{R.name}_atlas_64.png'));tex.interpolation='Closest';tex.extension='EXTEND'
vc=mat.node_tree.nodes.new('ShaderNodeVertexColor');vc.layer_name='ContactAO'
mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
mat.node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);mat.node_tree.links.new(vc.outputs['Color'],mix.inputs[2]);mat.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
parts=[]
def block(name,loc,dim,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('silhouette_only_bevel','BEVEL');m.width=bevel;m.segments=1;bpy.ops.object.modifier_apply(modifier=m.name)
 parts.append(o);return o
if fish:
 # 400x300x95mm BEWI 042A. Open inspection state; real wall thickness.
 block('EPS_base',(0,0,.0075),(.4,.3,.015),.003)
 block('EPS_front',(0,-.14125,.055),(.4,.0175,.08),.002)
 block('EPS_back',(0,.14125,.055),(.4,.0175,.08),.002)
 block('EPS_left',(-.19125,0,.055),(.0175,.265,.08),.002)
 block('EPS_right',(.19125,0,.055),(.0175,.265,.08),.002)
 # Ice represented by packed angular slabs, not high-density sparkle.
 for i,(x,y,z) in enumerate([(-.11,-.055,.027),(.08,.04,.026),(-.07,.067,.025),(.115,-.055,.028)]):
  block('ice_'+str(i),(x,y,z),(.095,.065,.024),.008)
 # Single fish mesh: elongated body, tapered head, forked tail, dorsal silhouette.
 verts=[(-.14,0,.045),(-.09,-.026,.045),(.075,-.028,.045),(.13,0,.047),(.075,.028,.045),(-.09,.026,.045),(-.04,0,.072),(-.04,0,.031)]
 faces=[(0,1,6),(1,2,6),(2,3,6),(3,4,6),(4,5,6),(5,0,6),(0,7,1),(1,7,2),(2,7,3),(3,7,4),(4,7,5),(5,7,0)]
 me=bpy.data.meshes.new('fish_body');me.from_pydata(verts,[],faces);o=bpy.data.objects.new('fish',me);bpy.context.collection.objects.link(o);parts.append(o)
 me=bpy.data.meshes.new('tail');me.from_pydata([(-.14,0,.045),(-.18,-.027,.05),(-.168,0,.05),(-.18,.027,.05)],[],[(0,1,2),(0,2,3)]);o=bpy.data.objects.new('fish_tail',me);bpy.context.collection.objects.link(o);parts.append(o)
 # Actual lid retained as a separate mesh fitted under the tray for pack-preview.
 dims=[.4,.095,.3]
else:
 # Cristallo Bordeaux VIP750:75.2mm diameter,300mm height, neck27.5mm.
 N=12;rings=[(0,.0308),(.007,.0376),(.17,.0376),(.205,.033),(.23,.019),(.243,.01375),(.284,.01375),(.3,.01475)]
 verts=[(r*math.cos(2*math.pi*i/N),r*math.sin(2*math.pi*i/N),z) for z,r in rings for i in range(N)]
 faces=[]
 for j in range(len(rings)-1):
  for i in range(N):faces.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
 faces += [tuple(reversed(range(N))),tuple((len(rings)-1)*N+i for i in range(N))]
 me=bpy.data.meshes.new('bordeaux750');me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new('wine750',me);bpy.context.collection.objects.link(o);parts.append(o);dims=[.0752,.3,.0752]
records=[]
for o in parts:
 o.data.materials.append(mat)
 while o.data.uv_layers:o.data.uv_layers.remove(o.data.uv_layers[0])
 uv=o.data.uv_layers.new(name='UVMap')
 for poly in o.data.polygons:
  pts=[o.data.vertices[o.data.loops[li].vertex_index].co+o.location for li in poly.loop_indices];n=poly.normal
  axis=max(range(3),key=lambda a:abs(n[a]));region=('right' if n.x>0 else 'left') if axis==0 else (('back' if n.y>0 else 'front') if axis==1 else ('top' if n.z>0 else 'bottom'))
  x,y,w,h=rects[region]
  a=pts[0];edges=[pts[(i+1)%len(pts)]-p for i,p in enumerate(pts)]
  # Vertical side faces get horizontal first axis, preserving printed label orientation.
  if abs(n.z)<.95:u=Vector((0,0,1)).cross(n).normalized();v=-n.cross(u).normalized()
  else:u=Vector((1,0,0));u=(u-n*u.dot(n)).normalized();v=n.cross(u).normalized()
  coords=[((p-a).dot(u)*D,(p-a).dot(v)*D) for p in pts];minx=min(c[0] for c in coords);miny=min(c[1] for c in coords)
  if not fish and abs(n.z)<.95:
   center=sum(pts,Vector())/len(pts);width=max(c[0] for c in coords)-minx
   horizontal=center.x if region=='front' else (-center.x if region=='back' else (center.y if region=='right' else -center.y))
   x+=(horizontal+.0376)*D-width/2
   y+=(.3-max(p.z for p in pts))*D
  if o.name.startswith('ice'):x,y=1,44
  elif o.name.startswith('fish'):x,y=30,44
  elif not fish and min(p.z for p in pts)>=.243:x,y=48,1
  for li,(xx,yy) in zip(poly.loop_indices,coords):uv.data[li].uv=((x+xx-minx)/64,1-(y+yy-miny)/64)
  poly.use_smooth=False
  records.append({'object':o.name,'region':region})
 # Contact shading through vertex colour, exported into glTF.
 ca=o.data.color_attributes.new(name='ContactAO',type='FLOAT_COLOR',domain='CORNER')
 for poly in o.data.polygons:
  for li in poly.loop_indices:
   z=o.data.vertices[o.data.loops[li].vertex_index].co.z+o.location.z
   shade=.83 if z<.016 else 1.0
   ca.data[li].color=(shade,shade,shade,1)
 bpy.context.view_layer.objects.active=o
 mod=o.modifiers.new('triangulate','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=R.name
bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
o['density_px_m']=64;o['source_geometry']='Manufacturer dimension drawing; not personally measured'
bpy.ops.wm.save_as_mainfile(filepath=str(R/f'{R.name}.blend'))
bpy.ops.export_scene.gltf(filepath=str(R/f'{R.name}.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=True,export_extras=True,export_vertex_color='NAME',export_vertex_color_name='ContactAO')
(R/'geometry.json').write_text(json.dumps({'tris':len(o.data.polygons),'gltf_dimensions_m':dims,'density':64,'regions':records},indent=2))
print(R.name,len(o.data.polygons),'tris')
