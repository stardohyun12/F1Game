"""Read exported FBX afresh: geometry signatures and projected roof/window evidence."""
import bpy, json, hashlib, sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'art/export/env-first';V=R/'art/_review/2026-10-30-env-first'
if '--v2' in sys.argv:E=R/'art/export/env-first-v2';V=R/'art/_review/2026-10-30-env-first-v2'
elif '--baseline-repro' in sys.argv:
    base=R/'art/_review/2026-10-30-env-first-v2/_baseline';E=base/'export';V=base/'review'
manifest=json.loads((E/'manifest.json').read_text(encoding='utf-8'));out=[]
for b in manifest['buildings']:
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(E/(b['name']+'.fbx')))
    canonical=[];roof=[];glass=[];allp=[];faces=set();duplicate=0;degenerate=0;tri_count=0;surfaces=[];objects=[]
    for ob in sorted(bpy.context.scene.objects,key=lambda o:o.name):
        if ob.type!='MESH':continue
        me=ob.data;me.calc_loop_triangles();M=ob.matrix_world
        points=[M@v.co for v in me.vertices];allp+=points;tr=[]
        for tri in me.loop_triangles:
            p=[points[i] for i in tri.vertices];ar=(p[1]-p[0]).cross(p[2]-p[0]).length/2
            if ar<1e-8:degenerate+=1
            verts=[tuple(round(float(x),5) for x in q) for q in p]
            uv=[tuple(round(float(x),7) for x in me.uv_layers.active.data[li].uv) for li in tri.loops]
            key=tuple(sorted(verts));duplicate+=key in faces;faces.add(key)
            tr.append((verts,uv,tri.material_index));tri_count+=1
            if '--v2' in sys.argv:surfaces.append(dict(object=ob.name,vertices=[tuple(float(x) for x in q) for q in p],uv=uv,material=me.materials[tri.material_index].name))
            if ob.name.endswith('_ROOF') and abs((p[1]-p[0]).cross(p[2]-p[0]).z)>1e-7:
                roof.append([[float(q.x),float(-q.y)] for q in p])
        if ob.name.endswith('_GLASS'):
            for poly in me.polygons:
                glass.append([[float(points[i].x),float(-points[i].y),float(points[i].z)] for i in poly.vertices])
        canonical.append((ob.name,tr))
        objects.append(dict(name=ob.name,bounds=[[min(float(p[k]) for p in points) for k in range(3)],[max(float(p[k]) for p in points) for k in range(3)]]))
    out.append(dict(name=b['name'],triangles=tri_count,degenerate_triangles=degenerate,exact_duplicate_triangles=duplicate,
                    bottom=min(p.z for p in allp),mesh_uv_sha256=hashlib.sha256(json.dumps(canonical,sort_keys=True).encode()).hexdigest(),
                    roof_projected_triangles=roof,glass_faces=glass,objects=objects,surfaces=surfaces))
(V/'fbx-geometry.json').write_text(json.dumps(out,separators=(',',':')),encoding='utf-8')
print('READBACK',[(x['name'],x['triangles'],x['degenerate_triangles'],x['exact_duplicate_triangles']) for x in out])
