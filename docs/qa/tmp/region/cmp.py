# 0301: 두 지문 비교 — python -X utf8 cmp.py A B
import json, struct, sys, os
d = os.path.dirname(os.path.abspath(__file__))
A, B = sys.argv[1], sys.argv[2]
ja = json.load(open(f'{d}/{A}.json', encoding='utf8')); jb = json.load(open(f'{d}/{B}.json', encoding='utf8'))
def terr(tag):
    b = open(f'{d}/{tag}_terrain.bin', 'rb').read(); n = struct.unpack_from('<i', b, 0)[0]
    v = struct.unpack_from(f'<{3*n}f', b, 4); m = struct.unpack_from('<i', b, 4 + 12 * n)[0]
    t = struct.unpack_from(f'<{m}i', b, 8 + 12 * n); return n, v, t
na, va, ta = terr(A); nb, vb, tb = terr(B)
print(f'terrain {A} v{na} t{len(ta)//3} / {B} v{nb} t{len(tb)//3} · hash same={ja["terrain"]["h"]==jb["terrain"]["h"]}')
if na == nb and ta == tb:
    mx = 0; ch = 0; x0 = z0 = 1e9; x1 = z1 = -1e9
    for i in range(na):
        dd = max(abs(va[3*i+k] - vb[3*i+k]) for k in range(3))
        if dd > 0:
            ch += 1; mx = max(mx, dd); x0 = min(x0, va[3*i]); x1 = max(x1, va[3*i]); z0 = min(z0, va[3*i+2]); z1 = max(z1, va[3*i+2])
    print(f'  same topology · changed verts {ch} · max {mx*1000:.3f} mm' + (f' · box x[{x0:.0f},{x1:.0f}] z[{z0:.0f},{z1:.0f}]' if ch else ''))
else:
    print('  topology differs (vertex/triangle lists not equal)')
oa, ob = ja['objs'], jb['objs']
only_a = sorted(set(oa) - set(ob)); only_b = sorted(set(ob) - set(oa)); diff = sorted(k for k in oa if k in ob and oa[k] != ob[k])
print(f'objs {len(oa)} / {len(ob)} · only {A} {len(only_a)} · only {B} {len(only_b)} · hash differ {len(diff)}')
for k in (only_a[:5] + only_b[:5] + diff[:8]): print('  ', k)
