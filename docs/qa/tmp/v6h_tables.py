# 과제 0640 — 기준(커밋된 씬) vs v6h, Box·Wheels 각각 road_feel.py 표·히트맵 + 봇 실패 집계
import shutil, subprocess, collections, sys
T = 'docs/qa/tmp/'; V = T + 'v6h/'; OUT = 'docs/qa/evidence/townv6h-feel/'
for scene in ('base', 'v6h'):
    for mode in ('Box', 'Wheels'):
        shutil.copy(V + scene + '-road-feel.tsv', T + 'road-feel.tsv'); shutil.copy(V + scene + '-road-feel-roads.tsv', T + 'road-feel-roads.tsv')
        shutil.copy(V + f'{scene}-{mode}-bot.txt', T + 'townv6-bot-cover.txt'); shutil.copy(V + f'{scene}-{mode}-bot-feel.tsv', T + 'townv6-bot-cover-feel.tsv')
        r = subprocess.run([sys.executable, '-X', 'utf8', 'tools/qa/road_feel.py', f'{scene}_{mode}', OUT], capture_output=True, text=True, encoding='utf-8')
        print('==', scene, mode, r.returncode, r.stderr[-300:])
        print(open(OUT + f'table_{scene}_{mode}.md', encoding='utf-8').read())
def rd(p):
    rows = [l.split('\t') for l in open(p, encoding='utf-8-sig').read().splitlines()[2:] if l.strip()]
    return {(x[0], x[1]): x for x in rows}
for mode in ('Box', 'Wheels'):
    a, b = rd(V + f'base-{mode}-bot.txt'), rd(V + f'v6h-{mode}-bot.txt')
    fa = {k for k, x in a.items() if x[4] != 'ok'}; fb = {k for k, x in b.items() if x[4] != 'ok'}
    print(mode, 'runs', len(a), '->', len(b), 'fail', len(fa), '->', len(fb), 'new', len(fb - fa), 'fixed', len(fa - fb),
          'kinds', dict(collections.Counter(x[4] for x in a.values() if x[4] != 'ok')), '->', dict(collections.Counter(x[4] for x in b.values() if x[4] != 'ok')))
    for k in sorted(fb - fa)[:12]:
        x = b[k]; print('  NEW', x[0], x[1], 'grade', x[3], x[4], 'at', x[7], x[8], x[12] if len(x) > 12 else '')
