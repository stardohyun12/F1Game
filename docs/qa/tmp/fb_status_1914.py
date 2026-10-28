# artlive 1914: 코멘트 jsonl 의 status/fix 갱신. python -X utf8 docs/qa/tmp/fb_status_1914.py <id> <status> <fix>
import sys, json, glob
cid, st, fix = sys.argv[1:4]
for f in glob.glob('tools/viewer/feedback/*.jsonl'):
    rows = [json.loads(l) for l in open(f, encoding='utf-8') if l.strip()]
    hit = False
    for r in rows:
        if r['id'] == cid: r['status'] = st; r['fix'] = fix; hit = True
    if hit:
        open(f, 'w', encoding='utf-8').write(''.join(json.dumps(r, ensure_ascii=False) + '\n' for r in rows)); print(f, cid, st)
