// 0338: 차·길·갈림목 판의 PhysicsMaterial(마찰) 비교 + 갈림목 판 전체 분포
string M(Collider c) { var m = c.sharedMaterial; return c == null ? "null" : $"{c.name}: {(m != null ? $"{m.name} s{m.staticFriction} d{m.dynamicFriction} {m.frictionCombine}" : "none(기본 0.6)")} {c.GetType().Name}"; }
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
string r = "car " + M(car.GetComponent<Collider>()) + "\n";
foreach (var n in new[] { "ROAD_V2e_tomo_118267060_15", "ROAD_V2j_14", "ROAD_V2j_200_l", "ROAD_V2j_203", "ROAD_V2e_tomo_118267044_13_1", "TownV6_Terrain" }) { var g = GameObject.Find(n); r += (g != null && g.GetComponent<Collider>() != null ? M(g.GetComponent<Collider>()) : n + " 없음") + "\n"; }
var groups = new Dictionary<string, int>();
foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)) if (c.name.StartsWith("ROAD_")) { var m = c.sharedMaterial; string k = (c.name.StartsWith("ROAD_V2j") ? "j " : "road ") + (m != null ? m.name + " s" + m.staticFriction + " " + m.frictionCombine : "none"); groups[k] = groups.TryGetValue(k, out var v) ? v + 1 : 1; }
foreach (var kv in groups) r += kv.Key + " x" + kv.Value + "\n";
return r;
