import json, glob, os
os.chdir(os.path.join(os.path.dirname(__file__), "../../../tools/viewer/feedback"))
skip = ('id', 'time', 'status', 'kind', 'comment', 'camera', 'nodes', 'strokes', 'point', 'node', 'building', 'screenshot', 'scene', 'space', 'point_source')
for f in sorted(glob.glob('*.jsonl')):
    for l in open(f, encoding='utf8'):
        if not l.strip():
            continue
        d = json.loads(l)
        extra = {k: d[k] for k in d if k not in skip}
        print(f, d.get('id'), (d.get('time') or '')[:16], d.get('status'), d.get('kind'), str(extra)[:160], (d.get('comment') or '')[:40].replace('\n', ' '))
