"""봇 cover 결과 요약(0506). 사용: python -X utf8 docs/qa/tmp/botsum.py a.txt b.txt ..."""
import sys, collections, statistics
for p in sys.argv[1:]:
    rows = [l.split('\t') for l in open(p, encoding='utf-8-sig').read().splitlines()[2:] if l.strip() and not l.startswith('#')]
    rows = [r for r in rows if len(r) >= 13]
    c = collections.Counter(r[4] for r in rows)
    az = [float(r[9]) for r in rows]; pk = [float(r[10]) for r in rows]; air = sum(int(r[11]) for r in rows)
    t = sum(float(r[5]) for r in rows if r[4] == 'ok')
    print(f'{p}: 판 {len(rows)} · ' + ' '.join(f'{k} {v}' for k, v in c.most_common()) +
          f' · 접지끊김 {air} · 수직가속 RMS 평균 {statistics.mean(az):.2f} 중앙 {statistics.median(az):.2f} 최대 {max(az):.2f} · 피크 중앙 {statistics.median(pk):.1f} 최대 {max(pk):.1f} · 성공판 시간 합 {t:.0f}s')
