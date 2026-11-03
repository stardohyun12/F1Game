from pathlib import Path
import json
p = Path('tools/viewer/feedback/depot.jsonl')
ids = {'depot-20261028-211558-ot14', 'depot-20261028-211628-vhoh'}
lines = p.read_text(encoding='utf-8').splitlines(keepends=True)
found, out, before, after = set(), [], [], []
for line in lines:
    d = json.loads(line) if line.strip() else None
    if d and d.get('id') in ids:
        before.append(d.copy()); found.add(d['id'])
        d['status'] = 'done'; after.append(d.copy())
        out.append(json.dumps(d, ensure_ascii=False) + ('\n' if line.endswith('\n') else ''))
    else:
        out.append(line)
assert found == ids, found
for old, new in zip(before, after):
    assert {k:v for k,v in old.items() if k!='status'} == {k:v for k,v in new.items() if k!='status'}
Path('docs/qa/tmp/build/depot/feedback_0335.json').write_text(json.dumps(dict(before=before, after=after),ensure_ascii=False,indent=2),encoding='utf-8')
p.write_text(''.join(out),encoding='utf-8',newline='')
print('status done:',sorted(found))
