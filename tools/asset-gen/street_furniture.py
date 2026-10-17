"""Scoped, original street furniture. Does not regenerate the shared town kit.
blender -b -P tools/asset-gen/street_furniture.py -- --root D:/Github/F1Game
Units: metres; front -Y in Blender -> +Z in Unity. Deterministic PNG/FBX/blend.
"""
import bpy, math, random, json, argparse, sys, struct, zlib
from pathlib import Path
from mathutils import Vector

ap=argparse.ArgumentParser(); ap.add_argument('--root', required=True)
args=ap.parse_args(sys.argv[sys.argv.index('--')+1:]); root=Path(args.root)
out=root/'F1/Assets/Art/StreetFurniture'; out.mkdir(parents=True,exist_ok=True)
source=root/'art/source/street-furniture'; source.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def png(path,n,rows):
    def chunk(t,d): return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
    raw=b''.join(b'\0'+bytes(row) for row in rows)
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',n,n,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))

# Eight deliberately quiet material swatches. Long grain follows the plank,
# not random dirt sprayed across every surface. UV padding prevents bleed.
N=256; palette=[(166,120,72),(179,135,83),(156,112,68),(58,88,90),
                (67,107,111),(32,40,39),(178,181,165),(192,180,145)]
rows=[]
for y in range(N):
    row=[]
    for x in range(N):
        idx=(y//128)*4+x//64; c=palette[idx]; u=x%64; v=y%128
        delta=0
        if idx<3:
            delta=round(2*math.sin(v*.32)+math.sin(u*.07+v*.91))
            if v in (25,57,91) and 10<u<49: delta-=5
            if v<3 or v>124: delta+=6
            # Sparse elongated grain strokes, sun-bleached contact edges.
            if (u//11+v//4*7+idx*3)%19==0: delta-=4
            if v<8 and (u//7+idx)%3==0: delta+=10
        elif idx in (3,4):
            # Lower edge grime and restrained edge wear on painted metal.
            if v>116: delta-=4
            if u<2 or u>61: delta+=4
            if (u//13+v//9*3)%23==0: delta+=3
            if (v<7 or u<7) and (u//3+v//2)%7==0: delta+=12
        elif idx==7:
            # A quiet cream person-and-basket symbol on teal, not a square placeholder.
            c=palette[4]
            head=(u-25)**2+(v-37)**2<30
            torso=22<=u<=29 and 45<=v<=64
            legs=(20<=u<=24 and 62<=v<=87) or (28<=u<=32 and 62<=v<=87)
            arm=29<=u<=42 and abs(v-(u*.65+29))<3
            basket=(39<=u<=54 and 66<=v<=69) or (v>=69 and v<=88 and (abs(u-(40+(v-69)*.13))<1.5 or abs(u-(53-(v-69)*.13))<1.5)) or (44<=u<=50 and 87<=v<=90)
            if head or torso or legs or arm or basket:c=(208,200,168)
        row.extend(max(0,min(255,k+delta)) for k in c)
    rows.append(row)
png(out/'FurnitureAtlas.png',N,rows)

# Seamless 4m asphalt tile: small aggregate, no baked cracks, no painted camo.
# Periodic low-frequency components and fixed seed guarantee repeatability.
rng=random.Random(941); rows=[]
for y in range(256):
    row=[]
    for x in range(256):
        broad=1.1*math.sin(math.tau*x/256)+.7*math.cos(math.tau*(x+2*y)/256)
        grain=rng.choices([-7,-4,-1,0,1,4,7],[1,3,15,30,15,3,1])[0]
        aggregate=((x//2*73+y//2*157)^((x//2+y//2)*31))%101
        if aggregate<7:grain+=7
        elif aggregate>96:grain-=5
        row.extend(max(0,min(255,round(c+broad+grain))) for c in (116,117,113))
    rows.append(row)
png(out/'AsphaltAggregate.png',256,rows)

atlas=bpy.data.images.load(str(out/'FurnitureAtlas.png'))
mat=bpy.data.materials.new('Furniture'); mat.use_nodes=True
tex=mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=atlas; tex.interpolation='Closest'
mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
parts=[]

def block(name,center,size,tile,bevel=0,angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    obj=bpy.context.object; obj.name=name; obj.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new('Worn machined edge','BEVEL'); mod.width=min(bevel,min(size)*.22); mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    uv=obj.data.uv_layers.active or obj.data.uv_layers.new()
    for poly in obj.data.polygons:
        normal=poly.normal; axes=(0,1) if abs(normal.z)>.5 else ((0,2) if abs(normal.y)>.5 else (1,2))
        for li in poly.loop_indices:
            co=obj.data.vertices[obj.data.loops[li].vertex_index].co
            a=co[axes[0]]/size[axes[0]]+.5; b=co[axes[1]]/size[axes[1]]+.5
            if tile==7:b=1-b  # Upright icon on vertical faces (PNG rows run top-down).
            # PNG top-down vs Blender UV bottom-up.
            uv.data[li].uv=((tile%4*64+4+a*56)/256,1-(tile//4*128+4+b*120)/256)
    obj.rotation_euler.x=angle; obj.data.materials.append(mat); parts.append(obj)
    return obj

def beam(name,a,b,width,depth,tile):
    a,b=Vector(a),Vector(b)
    obj=block(name,(a+b)/2,(width,depth,(b-a).length),tile,.006)
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()

def bench():
    # 1.8m seat, seat top .45m; separated boards, inclined back, leg frames.
    for i in range(4):
        block('Seat slat %d'%i,(0,-.21+i*.135,.421),(1.8,.117,.058),i%3,.009)
    for i in range(3):
        z=.60+i*.115; y=.22+(z-.48)*.17
        block('Back slat %d'%i,(0,y,z),(1.8,.045,.094),(i+1)%3,.007,math.radians(-10))
    for x in (-.66,.66):
        beam('Front leg',(x,-.23,.027),(x,-.16,.39),.055,.055,3)
        beam('Back leg',(x,.26,.027),(x,.15,.39),.055,.055,3)
        beam('Back support',(x,.17,.34),(x,.285,.875),.046,.046,3)
        block('Seat bearer',(x,0,.376),(.065,.55,.046),3,.006)
        for y in (-.23,.26):block('Ground shoe',(x,y,.014),(.13,.11,.028),3,.008)
    beam('Underseat tie',(-.67,.16,.25),(.67,.16,.25),.04,.04,3)
    for x in (-.82,.82):
        beam('Arm front',(x,-.16,.45),(x,-.16,.625),.035,.035,3)
        beam('Arm rear',(x,.22,.45),(x,.22,.635),.035,.035,3)
        block('Arm rest',(x,.02,.647),(.068,.47,.04),3,.012)
    # Fasteners are real silhouettes at close range, no ubiquitous random dents.
    for x in (-.66,.66):
        for i in range(3):
            z=.60+i*.115; y=.22+(z-.48)*.17-.027
            block('Back bolt',(x,y,z),(.018,.009,.018),6,.003)

def bin_():
    # Original hooded municipal bin, .50 x .44 x .96m. Open recess is geometry.
    block('Recessed plinth',(0,0,.025),(.43,.38,.05),3,.012)
    block('Body',(0,0,.363),(.48,.42,.646),4,.015)
    block('Door seam',(0,-.213,.365),(.415,.008,.559),5,.008)
    block('Collection door',(0,-.221,.369),(.397,.013,.538),4,.011)
    for z in (.20,.55):block('Hinge',(-.208,-.237,z),(.028,.026,.055),3,.004)
    block('Key lock',(.153,-.234,.575),(.025,.016,.025),6,.004)
    block('Dark inner liner',(0,.065,.704),(.398,.273,.044),5,.007)
    block('Opening back',(0,.187,.79),(.464,.04,.23),4,.009)
    for x in (-.218,.218):block('Hood cheek',(x,0,.794),(.044,.42,.228),4,.008)
    block('Rain hood',(0,-.004,.925),(.50,.448,.054),4,.016)
    block('Front lip',(0,-.214,.703),(.443,.042,.031),6,.006)
    # A tiny cream waste pictogram, built from simple shapes (no logo).
    block('Waste pictogram',(0,-.233,.49),(.16,.004,.195),7)

manifest=[]
for name,fn in [('Bench',bench),('Bin',bin_)]:
    parts.clear(); fn(); bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name
    bpy.context.scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    obj.data.calc_loop_triangles(); triangles=len(obj.data.loop_triangles)
    assert triangles<2500 and all(p.area>1e-10 for p in obj.data.polygons), (name,triangles,min(p.area for p in obj.data.polygons))
    assert abs(min(v.co.z for v in obj.data.vertices))<1e-5
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='STRIP')
    manifest.append({'name':name,'triangles':triangles,'dimensions_blender':list(obj.dimensions),'origin':'ground centre','front':'Unity +Z'})
    obj.location.x=0 if name=='Bench' else 2
bpy.ops.wm.save_as_mainfile(filepath=str(source/'StreetFurniture.blend'))
(out/'manifest.json').write_text(json.dumps(manifest,indent=2))
print('STREET_FURNITURE_OK '+json.dumps(manifest))
