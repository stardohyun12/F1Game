# 변 키 (a<<32)|b 를 .NET Dictionary<long,int> 에 넣을 때 해시가 얼마나 몰리는지 — long 기본 해시(a^b) vs 섞은 해시.
# 사용: python -X utf8 docs/qa/tmp/ta/hash_chain.py
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from mesh_edges import load

idx, pos = load('F1/Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_parcels.asset')
P = np.round(np.array(pos) * 100).astype(np.int64)
_, first, inv = np.unique(P, axis=0, return_index=True, return_inverse=True)
weld = first[inv.ravel()]   # C# Weld 와 같다: 같은 자리면 처음 나온 정점 번호
T = weld[np.array(idx, dtype=np.int64)].reshape(-1, 3)
E = np.concatenate([T[:, [0, 1]], T[:, [1, 2]], T[:, [2, 0]]])
E = E[E[:, 0] != E[:, 1]]; E.sort(axis=1)
U = np.unique(E, axis=0)
h_old = (U[:, 0] ^ U[:, 1]).astype(np.uint64)
key = ((U[:, 0] << 32) | U[:, 1]).astype(np.uint64)
x = key * np.uint64(0x9E3779B97F4A7C15)
h_new = (x >> np.uint64(32)) ^ (x & np.uint64(0xffffffff))
cap = 5082503   # 사전 용량 ≈ 삼각형 인덱스 수 이상 소수

def stat(h):
    _, c = np.unique(h % np.uint64(cap), return_counts=True)
    return len(c), int(c.max()), float((c.astype(np.float64) ** 2).sum() / 2)

print("verts", len(pos), "tris", len(idx) // 3, "unique edges", len(U))
print("old a^b : buckets %d, max chain %d, ~compares %.3g" % stat(h_old))
print("new mix : buckets %d, max chain %d, ~compares %.3g" % stat(h_new))
