"""Blender 전용. 9529 원본 재현/후보 생성/저장 FBX 조사. Unity 사용 없음.
blender -b --factory-startup -P tools/art/bin/vest_9529.py -- before|after|library
출력은 art/_review/2026-10-30-house-vest/만. 기본 에셋은 덮어쓰지 않는다.
"""
import bpy, hashlib, json, os, sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
REVIEW = ROOT / 'art/_review/2026-10-30-house-vest'
MODE = sys.argv[sys.argv.index('--') + 1]
OUT = REVIEW / MODE
OUT.mkdir(parents=True, exist_ok=True)

def signature(ob):
    me = ob.data
    return hashlib.sha256(repr((
        [tuple(round(v, 6) for v in ob.matrix_world @ p.co) for p in me.vertices],
        [tuple(p.vertices) for p in me.polygons],
        [tuple(round(v, 7) for v in u.uv) for u in me.uv_layers.active.data]
    )).encode()).hexdigest()

def metrics(ob):
    me = ob.data
    faces = [[ob.matrix_world @ me.vertices[v].co for v in p.vertices] for p in me.polygons]
    keys = [tuple(sorted(tuple(round(v, 5) for v in p) for p in ps)) for ps in faces]
    dup = [(i, j) for i in range(len(keys)) for j in range(i + 1, len(keys)) if keys[i] == keys[j]]
    # Opposite faces are included; legacy fix_1846 excludes these in one island.
    gaps = []
    for i in range(0, 12, 2):
        a, b = faces[i:i + 2]
        n = (a[1] - a[0]).cross(a[2] - a[0]).normalized()
        gaps.append(abs(n.dot(b[0] - a[0])))
    uv_sha = hashlib.sha256(repr([tuple(round(v, 6) for v in u.uv)
        for u in me.uv_layers.active.data]).encode()).hexdigest()
    return dict(faces=len(faces), tris=sum(len(p)-2 for p in faces), uv_sha256=uv_sha,
                coincident_pairs=dup, panel_normal_gaps_m=gaps)

if MODE in ('library', 'survey'):
    result = {}
    paths = [('studio', 'F1Studio/Assets/Art/Library/Buildings/room/room.fbx'),
             ('game', 'F1/Assets/Art/Library/Buildings/room/room.fbx'),
             ('candidate', 'art/_review/2026-10-30-house-vest/after/vest.fbx')]
    if MODE == 'survey':
        paths = [(p.stem, p.relative_to(ROOT).as_posix()) for p in
                 (ROOT / 'F1/Assets/Art/Library/Buildings').glob('*/*.fbx')]
    for label, rel in paths:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(ROOT / rel))
        if MODE == 'library':
            result[label] = metrics(bpy.data.objects['VEST_01'])
        else:
            found = []
            for ob in bpy.context.scene.objects:
                if ob.type != 'MESH': continue
                buckets = {}
                for p in ob.data.polygons:
                    key = tuple(sorted(tuple(round(v, 5) for v in ob.matrix_world @ ob.data.vertices[i].co) for i in p.vertices))
                    buckets.setdefault(key, []).append(p)
                pairs = [(a.index,b.index) for ps in buckets.values() for i,a in enumerate(ps)
                         for b in ps[i+1:] if a.normal.dot(b.normal) < -.999]
                if pairs: found.append(dict(object=ob.name, reverse_coincident_pairs=len(pairs)))
            result[rel] = found
else:
    src = REVIEW / 'q1_room_before.py' if MODE == 'before' else ROOT / 'tools/asset-gen/q1_room.py'
    source = src.read_text(encoding='utf-8')
    # Execute the original source through mesh construction, with output paths isolated.
    source = source.split('# 0052: 창 → 발코니 문(유리 없음)')[0]
    source = source.replace("OUT = R + 'art/export/q1-room/'", 'OUT = ' + repr(OUT.as_posix() + '/'))
    source = source.replace("RV = R + 'art/_review/2026-10-28-q1-room/'", 'RV = ' + repr(OUT.as_posix() + '/'))
    exec(compile(source, str(src), 'exec'), {})
    vest = bpy.data.objects['VEST_01']
    result = metrics(vest)
    result['object_signatures'] = {o.name: signature(o) for o in bpy.context.scene.objects if o.type == 'MESH'}
    result['texture_sha256'] = hashlib.sha256((OUT / 'room.png').read_bytes()).hexdigest()
    bpy.ops.object.select_all(action='DESELECT')
    vest.select_set(True)
    bpy.context.view_layer.objects.active = vest
    bpy.ops.export_scene.fbx(filepath=str(OUT / 'vest.fbx'), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
        path_mode='STRIP', embed_textures=False, mesh_smooth_type='FACE', object_types={'MESH'})
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'room.blend'))

(OUT / 'geometry.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
print('VEST_9529', MODE, json.dumps({k:v for k,v in result.items() if k != 'object_signatures'}), flush=True)
sys.stdout.flush()
os._exit(0)
