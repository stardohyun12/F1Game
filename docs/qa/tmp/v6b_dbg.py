import sys, math, copy; sys.argv = ['x']; sys.path.insert(0, 'D:/Github/F1Game/tools/map')
import compose_osm_town as C
C.seam_offsets(); C.build_grid(); C.cut_pieces(); C.widen(); tip = C.outer_roads(); C.seams(); st = C.pick_stops(tip); C.add_shortcuts(st)
pos = {s['id']: s['pos'] for s in st}
C.CHAIN_GAIN = 1.0; C.FREE_RATIO = 1.0
base_roads = list(C.ROADS)
for src in ('garage', 'mart'):
    for pid in ('tomo', 'camo', 'posi'):
        for hmin in (0.0, 20.0, 40.0):
            C.ROADS[:] = list(base_roads); C.CHAIN_DBG.clear(); C.EDITS.clear()
            r = C.chain_shortcut(src, 'tea', pos, set(), 'd', free=(pid, hmin))
            cand = [x for x in C.CHAIN_DBG if x[0] == 'cand'][:3]
            print(src, pid, hmin, r and {k: r[k] for k in ('end', 'm', 'base', 'via', 'avgGrade')}, 'ncand', C.CHAIN_DBG[0][3], cand)
