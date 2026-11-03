"""0236: 3판(a4255090) ↔ 지금 작업본 지형 메시 정점 높이 비교. _typelessdata = 정점마다 위치3+법선3 float(24 B).
  python -X utf8 docs/qa/tmp/parcels4/terrain_diff.py [x0 x1 z0 z1]"""
import struct, subprocess, sys
P = "F1/Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_parcels.asset"
def verts(text):
    line = next(l for l in text.splitlines() if l.strip().startswith("_typelessdata:"))
    b = bytes.fromhex(line.split(":", 1)[1].strip())
    return [struct.unpack_from("<3f", b, i) for i in range(0, len(b), 24)]
old = verts(subprocess.run(["git", "show", "a4255090:" + P], capture_output=True, text=True, encoding="utf-8").stdout)
new = verts(open(P, encoding="utf-8").read())
print("verts", len(old), len(new))
box = [float(a) for a in sys.argv[1:5]] if len(sys.argv) >= 5 else None
diff = [(o, n) for o, n in zip(old, new) if abs(o[1] - n[1]) > .02 or abs(o[0] - n[0]) > .01 or abs(o[2] - n[2]) > .01]
print("changed verts", len(diff))
if box:
    sel = [(o, n) for o, n in diff if box[0] <= o[0] <= box[1] and box[2] <= o[2] <= box[3]]
    print("in box", len(sel))
    for o, n in sorted(sel, key=lambda t: -abs(t[0][1] - t[1][1]))[:25]:
        print(f"({o[0]:.1f},{o[2]:.1f}) y {o[1]:.2f} -> {n[1]:.2f}  d{n[1] - o[1]:+.2f}")
