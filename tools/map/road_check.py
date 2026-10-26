"""차길 노면 검사(평면 json 만으로) — v6e 과제 0421.

  ① 겹친·나란한 차길: 두 중심선 간격 < (두 폭 합/2 + 2 m) 이 10 m 이상 이어지는 쌍.
  ② 높이 프로파일: 1 m 간격 경사 변화 |Δ경사|/m > 3 %/m (돌길 요철 0.4+ 는 6 %/m). 3 m 안에 이어진 위반 = 1곳.
     갈림목 통과(끝끼리 맞닿은 두 길이 거의 일직선 ≥150°)는 두 길을 이어서 잰다.

  python -X utf8 tools/map/road_check.py v6b v6e      → 전후 표 + 겹침 크롭 시트
"""
import json, math, os, sys
from collections import defaultdict

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'docs', 'map')
LIM, LIM_STONE, STONE = 0.03, 0.06, 0.4
GAP_EXTRA, RUN_MIN = 2.0, 10.0
KEY = lambda q: (round(q[0], 1), round(q[1], 1))


def resample(pts, step=1.0):
    """꺾은선 [(x,z,y)] 을 step 이하 간격으로(원래 점은 그대로 남김), y 는 직선 보간."""
    out = [tuple(pts[0])]
    for a, b in zip(pts, pts[1:]):
        d = math.hypot(b[0] - a[0], b[1] - a[1]); n = max(1, math.ceil(d / step - 1e-6))
        out += [tuple(a[k] + (b[k] - a[k]) * i / n for k in range(3)) for i in range(1, n + 1)]
    return out


def cls(r):
    return 'stone' if r['roughness'] >= STONE else r['kind']


def lim_of(r):
    return LIM_STONE if r['roughness'] >= STONE else LIM


def curv(p, i, n):
    h0 = math.hypot(i[0] - p[0], i[1] - p[1]); h1 = math.hypot(n[0] - i[0], n[1] - i[1])
    if h0 < 0.3 or h1 < 0.3: return 0.0
    return ((n[2] - i[2]) / h1 - (i[2] - p[2]) / h0) / ((h0 + h1) / 2)


def through_pairs(roads):
    """끝점이 맞닿은 길끼리 거의 일직선(≥150°)인 쌍 → [(ra, at_start_a, rb, at_start_b)]."""
    ends = defaultdict(list)
    for r in roads:
        P = r['pts']
        if len(P) < 2: continue
        ends[KEY(P[0])].append((r, True)); ends[KEY(P[-1])].append((r, False))
    out = []
    for k, es in ends.items():
        if len(es) < 2: continue
        def odir(e):
            r, s = e; P = r['pts']; a, b = (P[0], P[1]) if s else (P[-1], P[-2])
            for q in (P[2:6] if s else P[-3:-7:-1]):
                if math.hypot(b[0] - a[0], b[1] - a[1]) >= 3: break
                b = q
            L_ = math.hypot(b[0] - a[0], b[1] - a[1]) or 1
            return ((b[0] - a[0]) / L_, (b[1] - a[1]) / L_)
        used = set(); cand = []
        for i in range(len(es)):
            for j in range(i + 1, len(es)):
                if es[i][0] is es[j][0]: continue
                u, v = odir(es[i]), odir(es[j]); cs = u[0] * v[0] + u[1] * v[1]
                if cs <= math.cos(math.radians(150)): cand.append((cs, i, j))
        for cs, i, j in sorted(cand):
            if i in used or j in used: continue
            used |= {i, j}; out.append((*es[i], *es[j]))
    return out


def profile(roads):
    """위반 곳 목록 [(cls, x, z, |k|, road)] 과 길이(m) — roads: pts 는 1 m 표본 [(x,z,y)]."""
    bad, length = [], defaultdict(float)
    def scan(seq, r, tag):
        last = None
        for t in range(1, len(seq) - 1):
            k = abs(curv(seq[t - 1], seq[t], seq[t + 1]))
            if k > lim_of(r) + 1e-6:
                p = seq[t]
                if last and math.hypot(p[0] - last[0], p[1] - last[1]) <= 3:
                    if k > bad[-1][3]: bad[-1] = (cls(r), p[0], p[1], k, tag)
                else: bad.append((cls(r), p[0], p[1], k, tag))
                last = p
    for r in roads:
        P = r['pts']
        for a, b in zip(P, P[1:]): length[cls(r)] += math.hypot(b[0] - a[0], b[1] - a[1])
        scan(P, r, r['id'])
    for ra, sa, rb, sb in through_pairs(roads):     # 갈림목 통과: 앞 길 끝 4 m + 뒤 길 첫 4 m
        A = ra['pts'][:5][::-1] if sa else ra['pts'][-5:]
        B = rb['pts'][:5] if sb else rb['pts'][-5:][::-1]
        r = ra if lim_of(ra) <= lim_of(rb) else rb
        n0 = len(bad); scan(A + B[1:], r, f"{ra['id']}|{rb['id']}")
        bad[n0:] = [b for b in bad[n0:] if abs(b[1] - A[-1][0]) + abs(b[2] - A[-1][1]) < 2.5]   # 이음 자리만(길 안쪽은 위에서 셌다)
    return bad, length


def overlaps(roads, gap_extra=GAP_EXTRA, run_min=RUN_MIN, both=False):
    """나란히 겹친 차길 쌍: a 의 1 m 표본 중 b 와의 간격 < (wa+wb)/2+gap_extra 가 run_min m 이상 이어지는 구간.
    → [{a, b, i0, i1 (a 의 표본 번호), m, gapMin, dy(평균 |높이차|), dyMax, shared(두 길이 구간 끝에서 노드를 공유)}]"""
    def tdir(P, i):
        a, b = P[max(0, i - 2)], P[min(len(P) - 1, i + 2)]; L_ = math.hypot(b[0] - a[0], b[1] - a[1]) or 1
        return ((b[0] - a[0]) / L_, (b[1] - a[1]) / L_)
    grid = defaultdict(list)
    for r in roads:
        for j, q in enumerate(r['pts']): grid[(int(q[0] // 8), int(q[1] // 8))].append((r['id'], j, q, tdir(r['pts'], j)))
    byid = {r['id']: r for r in roads}
    keys = {r['id']: {KEY(q) for q in r['pts']} for r in roads}
    found = {}
    for a in roads:
        hits = defaultdict(dict)           # b → {i: (d, dy)}
        for i, q in enumerate(a['pts']):
            cx, cz = int(q[0] // 8), int(q[1] // 8); ta = tdir(a['pts'], i)
            for dx in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for bid, j, s, tb in grid.get((cx + dx, cz + dz), ()):
                        if bid == a['id']: continue
                        if abs(ta[0] * tb[0] + ta[1] * tb[1]) < 0.866: continue      # 나란한(30° 안) 곳만 — 갈림목에서 비스듬히 만나는 건 겹침이 아니다
                        thr = (a['width'] + byid[bid]['width']) / 2 + gap_extra
                        d = math.hypot(s[0] - q[0], s[1] - q[1])
                        if d < thr and (i not in hits[bid] or d < hits[bid][i][0]): hits[bid][i] = (d, abs(s[2] - q[2]))
        for bid in list(hits):             # 두 길이 공유한 갈림목 둘레(thr) 는 뺀다 — 끝끼리 일직선으로 이어진 곳이 겹침으로 잡히지 않게
            sh = [q for q in a['pts'] if KEY(q) in keys[bid]]
            thr = (a['width'] + byid[bid]['width']) / 2 + gap_extra
            for i in [i for i in hits[bid] if any(math.hypot(a['pts'][i][0] - s[0], a['pts'][i][1] - s[1]) < thr for s in sh)]: del hits[bid][i]
            if not hits[bid]: del hits[bid]
        for bid, hs in hits.items():
            idx = sorted(hs); runs, cur = [], [idx[0]]
            for i in idx[1:]:
                if i - cur[-1] <= 2: cur.append(i)
                else: runs.append(cur); cur = [i]
            runs.append(cur)
            for ru in runs:
                P = a['pts']; m = sum(math.hypot(P[k + 1][0] - P[k][0], P[k + 1][1] - P[k][1]) for k in range(ru[0], ru[-1]))
                if m < run_min: continue
                shared = bool(keys[a['id']] & keys[bid])
                dys = [hs[i][1] for i in ru if i in hs]
                rec = {'a': a['id'], 'b': bid, 'i0': ru[0], 'i1': ru[-1], 'm': round(m, 1), 'gapMin': round(min(hs[i][0] for i in ru if i in hs), 1),
                       'dy': round(sum(dys) / len(dys), 2), 'dyMax': round(max(dys), 2), 'shared': shared,
                       'at': [round(P[(ru[0] + ru[-1]) // 2][0], 1), round(P[(ru[0] + ru[-1]) // 2][1], 1)]}
                k = (a['id'], bid) if both else tuple(sorted((a['id'], bid)))
                if k not in found or rec['m'] > found[k]['m']: found[k] = rec
    return sorted(found.values(), key=lambda r: -r['m'] * (1 + r['dy']))


def load(tag):
    d = json.load(open(os.path.join(OUT, f'town-{tag}.json'), encoding='utf-8'))
    rs = [{'id': r['id'], 'kind': r['kind'], 'width': r['width'], 'roughness': r['roughness'], 'pts': resample(r['knots'])}
          for r in d['roads'] if r['kind'] in ('main', 'village')]
    return d, rs


def summary(rs):
    bad, length = profile(rs)
    ov = overlaps(rs)
    c = defaultdict(int); mx = defaultdict(float)
    for b in bad: c[b[0]] += 1; mx[b[0]] = max(mx[b[0]], b[3])
    gs = []                                  # 경사: 2 m 이상 떨어진 두 표본 사이(0.3 m 짜리 마디 하나로 재면 잡음)
    for r in rs:
        q = r['pts']; j = 0
        for i in range(len(q)):
            j = max(j, i)
            while j < len(q) - 1 and math.hypot(q[j][0] - q[i][0], q[j][1] - q[i][1]) < 2: j += 1
            d = math.hypot(q[j][0] - q[i][0], q[j][1] - q[i][1])
            if d >= 2: gs.append((abs(q[j][2] - q[i][2]) / d, r['id'], round(q[i][0]), round(q[i][1])))
    gs.sort(reverse=True)
    return {'bad': bad, 'count': dict(c), 'max': dict(mx), 'length': dict(length), 'overlaps': ov, 'gradeMax': gs[0][0], 'gradeTop': gs[:5],
            'gradeOver18': sum(1 for g in gs if g[0] > 0.18)}


def crop_sheet(tags, centers, path, box=90.0, scale=5):
    """겹침 자리마다 [전 | 후] — 차길을 실제 폭으로, 색 = 높이, 겹친 두 길 높이차 글자."""
    from PIL import Image, ImageDraw, ImageFont
    fnt = lambda s: ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', s)
    data = [load(t) for t in tags]
    W = int(box * scale); rows = []
    for c in centers:
        row = Image.new('RGB', (W * len(tags) + 10 * (len(tags) - 1), W + 26), (255, 255, 255))
        for k, (d, rs) in enumerate(data):
            im = Image.new('RGB', (W, W), (236, 232, 220)); dr = ImageDraw.Draw(im)
            x0, z0 = c['at'][0] - box / 2, c['at'][1] + box / 2
            P = lambda q: ((q[0] - x0) * scale, (z0 - q[1]) * scale)
            ys = [q[2] for r in rs for q in r['pts'] if abs(q[0] - c['at'][0]) < box / 2 and abs(q[1] - c['at'][1]) < box / 2]
            lo, hi = (min(ys), max(ys)) if ys else (0, 1)
            for r in d['roads']:
                if r['kind'] in ('alley', 'stairs'):
                    dr.line([P(q) for q in r['knots']], fill=(220, 90, 30) if r['kind'] == 'stairs' else (230, 150, 70), width=2)
            for r in sorted(rs, key=lambda r: r['width']):
                pts = r['pts']
                for a, b in zip(pts, pts[1:]):
                    if abs(a[0] - c['at'][0]) > box or abs(a[1] - c['at'][1]) > box: continue
                    t = (a[2] - lo) / max(0.5, hi - lo); col = (int(60 + 180 * t), int(80 + 60 * (1 - abs(t - .5) * 2)), int(220 - 170 * t))
                    dr.line([P(a), P(b)], fill=col, width=max(2, int(r['width'] * scale)))
                for a, b in zip(pts, pts[1:]):
                    if abs(a[0] - c['at'][0]) > box: continue
                    dr.line([P(a), P(b)], fill=(30, 30, 30), width=1)
            dr.text((4, W - 20), f'높이 {lo:.1f}(파랑)~{hi:.1f} m(빨강)', fill=(0, 0, 0), font=fnt(13))
            row.paste(im, (k * (W + 10), 26))
        dr = ImageDraw.Draw(row)
        dr.text((4, 3), f"{c['a']} ↔ {c['b']} · {c['m']:.0f} m 나란히 · 간격 {c['gapMin']} m · 높이차 평균 {c['dy']} m → {c.get('fix', '')}", fill=(0, 0, 0), font=fnt(15))
        rows.append(row)
    if not rows: return
    hd = Image.new('RGB', (rows[0].width, 30), (255, 255, 255))
    ImageDraw.Draw(hd).text((4, 4), '   |   '.join(f'town-{t}' for t in tags) + f'   ({box:.0f} m 사방, 1 m = {scale} px)', fill=(0, 0, 0), font=fnt(17))
    sh = Image.new('RGB', (rows[0].width, 30 + sum(r.height + 8 for r in rows)), (255, 255, 255)); sh.paste(hd, (0, 0)); y = 30
    for r in rows: sh.paste(r, (0, y)); y += r.height + 8
    sh.save(path)


def main():
    tags = sys.argv[1:] or ['v6b', 'v6e']
    res = {}
    for t in tags:
        _, rs = load(t); res[t] = summary(rs)
        s = res[t]
        print(t, 'profile places', s['count'], 'max %/m', {k: round(100 * v, 1) for k, v in s['max'].items()},
              'km', {k: round(v / 1000, 2) for k, v in s['length'].items()}, 'gradeMax', round(s['gradeMax'], 3), '>18%', s['gradeOver18'], 'top', [(round(g[0], 3), *g[1:]) for g in s['gradeTop']], 'overlaps', len(s['overlaps']))
    lines = ['# 차길 노면 검사 — ' + ' → '.join(tags), '',
             f'경사 변화 한도 {LIM * 100:.0f} %/m(돌길 {LIM_STONE * 100:.0f} %/m), 1 m 간격 중심선, 3 m 안 이어진 위반 = 1곳. 겹침 = 간격 < 두 폭 합/2 + {GAP_EXTRA:.0f} m 가 {RUN_MIN:.0f} m 이상.', '']
    for t in tags:
        s = res[t]
        lines.append(f"- **{t}**: 덜컹 곳 큰길 {s['count'].get('main', 0)} · 마을길 {s['count'].get('village', 0)} · 돌길 {s['count'].get('stone', 0)}"
                     f" / 최대 {', '.join(f'{k} {100 * v:.0f}' for k, v in sorted(s['max'].items()))} %/m / 겹친 쌍 {len(s['overlaps'])} / 경사 최대 {100 * s['gradeMax']:.1f} %")
    for t in tags:
        lines += ['', f'## {t} 겹친 쌍', '']
        for o in res[t]['overlaps']:
            lines.append(f"- {o['a']} ↔ {o['b']} @({o['at'][0]:.0f},{o['at'][1]:.0f}) {o['m']:.0f} m · 간격 최소 {o['gapMin']} m · 높이차 평균 {o['dy']} / 최대 {o['dyMax']} m · 노드 공유 {o['shared']}")
        lines += ['', f'## {t} 덜컹 상위 15', '']
        for b in sorted(res[t]['bad'], key=lambda b: -b[3])[:15]:
            lines.append(f'- {b[4]} ({b[1]:.0f},{b[2]:.0f}) {b[0]} {100 * b[3]:.1f} %/m')
    open(os.path.join(OUT, f'road-check-{"-".join(tags)}.md'), 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
    return res


if __name__ == '__main__':
    main()
