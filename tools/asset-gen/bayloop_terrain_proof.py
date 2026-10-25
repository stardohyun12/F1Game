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


print('Terrain metric proof quads exported; no stage/render executed')
