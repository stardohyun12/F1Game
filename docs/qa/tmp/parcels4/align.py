"""옆 필지 집끼리 방향 차 — parcels-home.json 만으로(3판·4판 같은 잣대). SideNb 근사: 필지 중심 12 m 안·앞 방향 30° 안·앞뒤 6 m 안·옆 2 m 넘게.
  python -X utf8 docs/qa/tmp/parcels4/align.py <json>..."""
import json, math, sys
for path in sys.argv[1:]:
    j = json.load(open(path, encoding="utf-8"))
    ps = [p for p in j["parcels"] if p.get("mass")]
    ang = []
    for i, p in enumerate(ps):
        fy = math.radians(p["frontYaw"]); f = (math.sin(fy), math.cos(fy))
        for q in ps[i + 1:]:
            dx, dz = q["center"][0] - p["center"][0], q["center"][1] - p["center"][1]
            if dx * dx + dz * dz > 144: continue
            d = abs((p["frontYaw"] - q["frontYaw"] + 180) % 360 - 180)
            if d >= 30: continue
            along = dx * f[0] + dz * f[1]; side = dx * f[1] - dz * f[0]
            if abs(along) >= 6 or abs(side) <= 2: continue
            ang.append(abs((p["mass"]["yaw"] - q["mass"]["yaw"] + 180) % 360 - 180))
    ang.sort(); n = len(ang)
    print(f"{path}: pairs {n} · median {ang[n // 2]:.1f}° · <5° {sum(a < 5 for a in ang) / n:.0%} · >15° {sum(a > 15 for a in ang)}")
