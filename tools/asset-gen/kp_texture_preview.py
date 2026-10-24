"""Blender-only A1 material coupons; no game scene or AI plate is used."""
import json
import math
from pathlib import Path
import struct
import sys
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/(sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'art/textures/2026-10-24-kp')
MANIFEST=json.loads((OUT/'manifest.json').read_text(encoding='utf-8'))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def material(tile,unlit=False):
    mat=bpy.data.materials.new(tile['id']+('_proof' if unlit else '_lit'))
    mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();links=mat.node_tree.links
    output=nodes.new('ShaderNodeOutputMaterial')
    tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/tile['albedo']),check_existing=True)
    tex.interpolation='Closest';tex.extension='REPEAT'
    if unlit:
        shader=nodes.new('ShaderNodeEmission');shader.inputs['Strength'].default_value=1
        links.new(tex.outputs['Color'],shader.inputs['Color'])
    else:
        shader=nodes.new('ShaderNodeBsdfPrincipled');shader.inputs['Roughness'].default_value=1
        shader.inputs['Metallic'].default_value=0;shader.inputs['Specular IOR Level'].default_value=0
        links.new(tex.outputs['Color'],shader.inputs['Base Color'])
    links.new(shader.outputs[0],output.inputs['Surface'])
    return mat

def quad(name,vertices,uv,mat):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],[(0,1,2),(0,2,3)]);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    layer=mesh.uv_layers.new()
    for face in mesh.polygons:
        for loop in face.loop_indices: layer.data[loop].uv=uv[mesh.loops[loop].vertex_index]
    obj.data.materials.append(mat)
    return obj

def point_sampler(path):
    # glTF exporter may choose a mip filter by default. Explicit no-mip proof.
    raw=path.read_bytes();n,kind=struct.unpack_from('<II',raw,12);g=json.loads(raw[20:20+n]);binary=raw[20+n:]
    for s in g.get('samplers',[]): s.update(magFilter=9728,minFilter=9728,wrapS=10497,wrapT=10497)
    # Blender 5 exports Emission as a lit emissive material. Normalize to the
    # intended glTF unlit base-colour material without touching the embedded PNG.
    for mat in g.get('materials',[]):
        tex=mat.pop('emissiveTexture')
        mat.pop('emissiveFactor',None)
        mat['pbrMetallicRoughness']={'baseColorTexture':tex,'baseColorFactor':[1,1,1,1],'metallicFactor':0,'roughnessFactor':1}
        mat['extensions']={'KHR_materials_unlit':{}}
    g['extensionsUsed']=list(dict.fromkeys(g.get('extensionsUsed',[])+['KHR_materials_unlit']))
    data=json.dumps(g,separators=(',',':')).encode();data+=b' '*((-len(data))%4)
    path.write_bytes(struct.pack('<III',0x46546C67,2,12+8+len(data)+len(binary))+struct.pack('<II',len(data),kind)+data+binary)

for tile in MANIFEST['tiles']:
    w,h=tile['physical_size_m']
    obj=quad(tile['id']+'_metric',[(0,0,0),(w,0,0),(w,h,0),(0,h,0)],[(0,0),(1,0),(1,1),(0,1)],material(tile,True))
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    path=ROOT/tile['model']
    bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_yup=True,export_materials='EXPORT')
    point_sampler(path);bpy.data.objects.remove(obj,do_unlink=True)

def coupon(tile,verts):
    w=(Vector(verts[1])-Vector(verts[0])).length;h=(Vector(verts[3])-Vector(verts[0])).length
    tw,th=tile['physical_size_m']
    return quad(tile['id'],verts,[(0,0),(w/tw,0),(w/tw,h/th),(0,h/th)],material(tile))

tiles={t['id']:t for t in MANIFEST['tiles']}
coupon(tiles['wood'],[(-3.2,1,0),(-.6,1,0),(-.6,1,2.6),(-3.2,1,2.6)])
coupon(tiles['plaster'],[(.6,1,0),(3.2,1,0),(3.2,1,2.6),(.6,1,2.6)])
coupon(tiles['stone'],[(-3.2,-3.2,.06),(-.6,-3.2,.06),(-.6,-.6,.06),(-3.2,-.6,.06)])
coupon(tiles['roof'],[(.6,-3.2,.08),(3.2,-3.2,.08),(3.2,-.9,1.4),(.6,-.9,1.4)])

# Small scale ticks on the ground, using geometry rather than painted texture.
mat=bpy.data.materials.new('Neutral support');mat.diffuse_color=(.22,.235,.225,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.04));bpy.context.object.data.materials.append(mat)
for x in [-3.2,-2.2,-1.2,.6,1.6,2.6]:
    bpy.ops.mesh.primitive_cube_add(size=1,location=(x,-3.55,0))
    obj=bpy.context.object;obj.scale=(.015,.14,.025);obj.data.materials.append(mat)

scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32
scene.cycles.use_denoising=False
scene.render.resolution_x=960;scene.render.resolution_y=540;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.world.color=(.55,.58,.62)
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.64,.69,.74,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
bpy.ops.object.light_add(type='AREA',location=(-3,-5,8));bpy.context.object.data.energy=750;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=7
bpy.ops.object.camera_add(location=(6.7,-10.5,7.1));cam=bpy.context.object;scene.camera=cam
target=Vector((0,-.8,1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=9.7
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.render.filepath=str(OUT/'review/material-stage-960.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'proof/material-stage.blend'))
bpy.ops.render.render(write_still=True)
# Second view exposes repeated seams at a shallow angle. Still not a motion test.
cam.location=(3.8,-7.5,3.4);cam.rotation_euler=(Vector((0,-.5,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=8.9
scene.render.filepath=str(OUT/'review/material-stage-oblique-960.png');bpy.ops.render.render(write_still=True)
print('A1 metric quads + two 960x540 static Blender views complete')
