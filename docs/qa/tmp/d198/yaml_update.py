# first-delivery.yaml 을 D-198 배치·3차 고리로 갱신(스키마 유지, 줄 단위 치환)
import json, os
H = os.path.dirname(os.path.abspath(__file__)); R = os.path.abspath(os.path.join(H, "../../../.."))
P = os.path.join(R, "docs/design/tutorial/first-delivery.yaml")
L = open(P, encoding="utf8").read().split("\n")
route = json.load(open(os.path.join(H, "route_world.json")))
tot = json.load(open(os.path.join(H, "anchors.json")))["total_m"]
gx, gz = route[0]
def px(x, z): return [round(2 * x + 800, 1), round(-2 * z + 800, 1)]
out = []; i = 0; place = None
while i < len(L):
    s = L[i]
    if s.startswith("# 변환은 docs/qa/tmp/tv6_plan.py"):
        s = "# 변환은 docs/qa/tmp/tv6_plan.py 의 P(x,z) = ((x+400)*2, (400-z)*2) 그대로. 확인점: Garage_start(" + str(gx) + "," + str(gz) + ") → px(" + str(px(gx, gz)[0]) + ", " + str(px(gx, gz)[1]) + "), Q_Teahouse 배달점(181.4,5.7) → px(1162.8, 788.6)"
    if s.strip().startswith("- {name: Q2_Garage"):
        s = "    - {name: Garage_start, world: [" + str(gx) + ", " + str(gz) + "], px: " + str(px(gx, gz)) + "}   # D-198 새 정비소 앞 길"
    if s.strip().startswith("route_world:"):
        out.append("    route_world:                  # 고리 경로 (docs/qa/tmp/d198/loop_pts.txt 3점마다 1점, D-198 3차 " + str(round(tot)) + " m: 정비소→보관소(길46)→북쪽 돌아 상점가→찻집→해안→정비소, 귀환은 골목16 피함)")
        out += ["      - [" + str(x) + ", " + str(z) + "]" for x, z in route]
        i += 1
        while not L[i].strip().startswith("steps:"): i += 1
        continue
    if s.strip() == "- id: door": place = "door"
    elif s.strip() == "- id: depot" and "places:" not in "\n".join(out[-3:]): place = "depot_step"
    if s.strip().startswith("marker_world:") and place == "door": s = "        marker_world: [-117.7, -10.3]   # D-198 방 정면"; place = None
    if s.strip().startswith("marker_world:") and place == "depot_step": s = "        marker_world: [-45.0, -14.1]   # D-198 포장 지점(길46, TUT_PackTrigger)"; place = None
    out.append(s); i += 1
txt = "\n".join(out)
pl = txt.index("\nplaces:")
head, tail = txt[:pl], txt[pl:]
for pid, w in (("room", "[-117.7, -11.9]"), ("garage", "[-62.1, 5.7]"), ("depot", "[-43.9, -31.2]"), ("postoffice", "[-57.3, -31.5]")):
    a = tail.index("  - id: " + pid + "\n"); b = tail.index("    world:", a); e = tail.index("\n", b)
    tail = tail[:b] + "    world: " + w + "   # D-198 건물 가운데" + tail[e:]
tail = tail.replace("note: 광장 갈림길(68,-54) 옆. 첫 배달에선", "note: D-198 보관소 서쪽 옆(정비소 앞 마을). 첫 배달에선")
open(P, "w", encoding="utf8", newline="\n").write(head + tail)
print("route", len(route), "tot", tot)
