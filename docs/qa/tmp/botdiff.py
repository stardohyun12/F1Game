"""봇 cover 전후 실패 비교. 사용: python -X utf8 docs/qa/tmp/botdiff.py <전.txt> <후.txt>"""
import sys
def rd(p):
    rows = [l.split('\t') for l in open(p, encoding='utf-8-sig').read().splitlines()[2:] if l.strip()]
    return {(r[0], r[1]): r for r in rows}
a, b = rd(sys.argv[1]), rd(sys.argv[2])
fa = {k for k, r in a.items() if r[4] != 'ok'}; fb = {k for k, r in b.items() if r[4] != 'ok'}
print('before fail', len(fa), 'after fail', len(fb), 'new', len(fb - fa), 'fixed', len(fa - fb))
for k in sorted(fb):
    r = b[k]; print('NEW ' if k not in fa else 'OLD ', r[0], r[1], 'len', r[2], 'grade', r[3], r[4], 'at', r[7], r[8], r[12] if len(r) > 12 else '')
