# 지형 메시 .asset(텍스트 YAML)을 직접 읽어 가장자리 변(한 삼각형만 쓰는 변)을 센다 — 맵 둘레 4 m 안은 뺀다.
# 사용: python -X utf8 mesh_edges.py <asset> [<asset> ...]
import sys, struct, json
from collections import Counter

def load(path):
    ib = vd = None; fmt = 0
    with open(path, 'r', encoding='utf-8') as f:
        for l in f:
            s = l.strip()
            if s.startswith('m_IndexFormat:'): fmt = int(s.split(':')[1])
            elif s.startswith('m_IndexBuffer:'): ib = bytes.fromhex(s.split(':', 1)[1].strip())
            elif s.startswith('_typelessdata:'): vd = bytes.fromhex(s.split(':', 1)[1].strip()); break
    idx = struct.unpack('<%d%s' % (len(ib) // (4 if fmt else 2), 'I' if fmt else 'H'), ib)
    n = len(vd) // 24
    fl = struct.unpack('<%df' % (n * 6), vd)
    pos = [(fl[i * 6], fl[i * 6 + 1], fl[i * 6 + 2]) for i in range(n)]
    return idx, pos

def run(path):
    idx, pos = load(path)
    key = {}; weld = []
    for p in pos:
        k = (round(p[0] * 100), round(p[1] * 100), round(p[2] * 100))
        weld.append(key.setdefault(k, len(weld)))
    c = Counter(); degen = 0
    for t in range(0, len(idx), 3):
        a, b, cc = weld[idx[t]], weld[idx[t + 1]], weld[idx[t + 2]]
        if a == b or b == cc or a == cc: degen += 1; continue
        for u, v in ((a, b), (b, cc), (cc, a)):
            c[(u, v) if u < v else (v, u)] += 1
    xs = [p[0] for p in pos]; zs = [p[2] for p in pos]
    mnx, mxx, mnz, mxz = min(xs), max(xs), min(zs), max(zs)
    inner = []; over = 0
    for (u, v), n in c.items():
        if n > 2: over += 1
        if n != 1: continue
        a, b = pos[u], pos[v]; m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2)
        if m[0] < mnx + 4 or m[0] > mxx - 4 or m[2] < mnz + 4 or m[2] > mxz - 4: continue
        if m[1] < 0.3: continue
        inner.append(m)
    cells = Counter((int(m[0] // 12), int(m[2] // 12)) for m in inner)
    top = cells.most_common(12)
    return {'asset': path.split('/')[-1], 'verts': len(pos), 'tris': len(idx) // 3, 'degenerate': degen, 'edges': len(c),
            'boundary_inner': len(inner), 'nonmanifold': over, 'cells12m': len(cells),
            'top_cells': [((x * 12 + 6, z * 12 + 6), n) for (x, z), n in top]}

if __name__ == '__main__':
    for p in sys.argv[1:]:
        print(json.dumps(run(p), ensure_ascii=False))
        sys.stdout.flush()
