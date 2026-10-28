# artlive 1846: 도현 뷰어 코멘트 요약 (id · node · split_from · center · comment · camera · delta)
import json, glob, os
for f in sorted(glob.glob('tools/viewer/feedback/*.jsonl')):
    for l in open(f, encoding='utf-8'):
        if not l.strip(): continue
        d = json.loads(l); x = d.get('node_extras') or {}
        td = d.get('transform_delta'); mv = d.get('moved_nodes') or {}
        print(os.path.basename(f), d['id'], d.get('status', ''), '|', d.get('comment'), '| node', d.get('node'), 'from', x.get('split_from'), 'c', x.get('center_blender_m'),
              '| mat', d.get('material'), 'tex', d.get('texture'), 'texel', d.get('texel'), '| pt', d.get('point_blender'),
              '| cam', (d.get('camera') or {}).get('pos'), (d.get('camera') or {}).get('target'), (d.get('camera') or {}).get('fov'),
              '| moved', {k: (v or {}).get('pos_blender') for k, v in mv.items()} if mv else '', '| nodes', [n.get('node') for n in d.get('nodes') or []])
