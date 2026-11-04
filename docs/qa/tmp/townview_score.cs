// 과제 26 ②: 마을 풍경 자리 후보 점수 (Play 중 exec). 레이 격자 96x54 로 건물/바다/하늘/지형 비율을 잰다.
var root = GameObject.Find("TownV6_Gray").transform;
var terr = root.Find("TownV6_Terrain").GetComponent<Collider>();
var sbo = new System.Text.StringBuilder();
var wallsT = root.Find("Buildings/Walls_0");
var roofsT = root.Find("Buildings/Roofs_0");
if (roofsT != null && roofsT.GetComponent<Collider>() == null && roofsT.GetComponent<MeshFilter>() != null)
{
    var mc = roofsT.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = roofsT.GetComponent<MeshFilter>().sharedMesh;
    sbo.Append("roofs MeshCollider added (runtime only)\n");
}
Physics.SyncTransforms();

// ── 마을 중심: 건물 정점 평균을 (210,-55) 에서 반경 130 m 로 4번 밀어 맞춘다 ──
float cx = 210f, cz = -55f; int nv = 0;
var wmf = wallsT != null ? wallsT.GetComponent<MeshFilter>() : null;
if (wmf != null && wmf.sharedMesh != null && wmf.sharedMesh.isReadable)
{
    var vs = wmf.sharedMesh.vertices; var m = wallsT.localToWorldMatrix;
    for (int it = 0; it < 4; it++)
    {
        double sx = 0, sz = 0; int n = 0;
        for (int i = 0; i < vs.Length; i += 3)
        {
            var w = m.MultiplyPoint3x4(vs[i]);
            float dx = w.x - cx, dz = w.z - cz;
            if (dx * dx + dz * dz < 130f * 130f) { sx += w.x; sz += w.z; n++; }
        }
        if (n > 0) { cx = (float)(sx / n); cz = (float)(sz / n); nv = n; }
    }
    sbo.Append("center from Walls_0 verts n=" + nv + "\n");
}
else sbo.Append("Walls_0 mesh not readable - center fallback\n");
float cy = 20f;
{ RaycastHit ch; if (terr.Raycast(new Ray(new Vector3(cx, 500f, cz), Vector3.down), out ch, 1000f)) cy = ch.point.y; }
var target = new Vector3(cx, cy + 6f, cz);
sbo.Append("TOWN_CENTER " + cx.ToString("F1") + "," + cy.ToString("F1") + "," + cz.ToString("F1") + "\n");

// ── 종류: 1 건물 · 2 지형 · 0 그 밖 ──
var cache = new System.Collections.Generic.Dictionary<Collider, int>();
System.Func<Collider, int> kind = c =>
{
    int k;
    if (cache.TryGetValue(c, out k)) return k;
    k = 0;
    if (c == terr) k = 2;
    else
    {
        var sp = new System.Text.StringBuilder();
        for (var t = c.transform; t != null; t = t.parent) sp.Insert(0, t.name + "/");
        string p = sp.ToString();
        if (p.Contains("/Buildings/") || p.Contains("D198_Buildings") || p.Contains("OnFoot_Depot")) k = 1;
    }
    cache[c] = k; return k;
};

float vfov = 60f; if (Camera.main != null) vfov = Camera.main.fieldOfView;
float asp = 16f / 9f;
float tv = Mathf.Tan(vfov * 0.5f * Mathf.Deg2Rad), th = tv * asp;

System.Func<Vector3, float, int, float[]> view = (eye, yaw, W) =>
{
    int H = W * 9 / 16;
    var rot = Quaternion.Euler(0f, yaw, 0f);
    Vector3 fw = rot * Vector3.forward, rt = rot * Vector3.right;
    int nB = 0, nS = 0, nK = 0, nT = 0, nO = 0; var colB = new bool[W]; var colN = new int[W];
    for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float u = ((x + 0.5f) / W * 2f - 1f) * th, v = (1f - (y + 0.5f) / H * 2f) * tv;
            Vector3 d = (fw + rt * u + Vector3.up * v).normalized;
            RaycastHit h; bool hit = Physics.Raycast(eye, d, out h, 3000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float tSea = d.y < -1e-4f ? (0f - eye.y) / d.y : float.PositiveInfinity;
            if (!hit || h.distance > tSea)
            {
                if (tSea < 3000f && eye.y > 0f) nS++; else nK++;
                continue;
            }
            int k = kind(h.collider);
            if (k == 1) { nB++; colN[x]++; } else if (k == 2) nT++; else nO++;
        }
    int cols = 0; for (int x = 0; x < W; x++) if (colN[x] >= 2) cols++;
    float tot = W * H;
    return new float[] { nB / tot, nS / tot, nK / tot, nT / tot, cols / (float)W, nO / tot };
};

System.Func<Vector3, bool> blockedByTerrain = eye =>
{
    Vector3 d = target - eye; float dist = d.magnitude;
    RaycastHit h;
    return Physics.Raycast(eye, d / dist, out h, Mathf.Max(0f, dist - 8f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && kind(h.collider) == 2;
};

// ── 후보 ──
var cands = new System.Collections.Generic.List<object[]>();   // name, feet
System.Action<string, Vector3> addGround = (name, p) =>
{
    RaycastHit h;
    if (!Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out h, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
    cands.Add(new object[] { name, h.point + Vector3.up * 0.05f });
};
var all = DebugPlaces.All;
for (int i = 0; i < all.Length; i++)
{
    if (all[i].car == DebugPlaces.CarRule.BesideCar) continue;
    addGround("place:" + all[i].name, all[i].feet);
}
foreach (var dp in UnityEngine.Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
    if (dp.name.StartsWith("DP_")) addGround("dp:" + dp.name, dp.transform.position);

// 지형 격자: (a) 5x4 구역마다 가장 높은 점 20개(hi:) (b) 20 m 격자 걸을 수 있는 모든 땅(g:) — 거친 레이로 먼저 거르고 위 40개만 촘촘히 다시 잰다
float x0 = -300f, x1 = 360f, z0 = -340f, z1 = 380f;
var best = new Vector3[5, 4]; var have = new bool[5, 4];
for (float gx = x0; gx <= x1; gx += 20f)
    for (float gz = z0; gz <= z1; gz += 20f)
    {
        RaycastHit h;
        if (!terr.Raycast(new Ray(new Vector3(gx, 500f, gz), Vector3.down), out h, 1000f)) continue;
        if (h.point.y < 2f) continue;
        if (Vector3.Angle(h.normal, Vector3.up) > 28f) continue;
        if (Physics.CheckSphere(h.point + Vector3.up * 1.0f, 0.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
        cands.Add(new object[] { "g:" + gx + "," + gz, h.point + Vector3.up * 0.05f });
        int bx = Mathf.Clamp((int)((gx - x0) / (x1 - x0) * 5f), 0, 4), bz = Mathf.Clamp((int)((gz - z0) / (z1 - z0) * 4f), 0, 3);
        if (!have[bx, bz] || h.point.y > best[bx, bz].y) { best[bx, bz] = h.point; have[bx, bz] = true; }
    }
for (int a = 0; a < 5; a++) for (int b = 0; b < 4; b++) if (have[a, b]) cands.Add(new object[] { "hi:" + a + "," + b, best[a, b] + Vector3.up * 0.05f });

// ── 점수 ──
System.Func<float[], bool, float> scoreOf = (s, blocked) =>
{
    float b = s[0], sea = s[1];
    float bs = b >= 0.10f && b <= 0.40f ? 1f : (b < 0.10f ? b / 0.10f : Mathf.Clamp01(1f - (b - 0.40f) / 0.40f));
    float ss = Mathf.Clamp01(sea / 0.10f), ks = Mathf.Clamp01(s[2] / 0.20f);
    bool all = bs >= 1f && ss >= 1f && ks >= 1f && !blocked;
    return 25f * bs + 25f * ss + 25f * ks + (blocked ? 0f : 25f) + 5f * s[4] + (all ? 20f : 0f);
};
var rows = new System.Collections.Generic.List<object[]>();   // score, name, feet, yaw, stats, blocked
foreach (var c in cands)
{
    string name = (string)c[0]; Vector3 feet = (Vector3)c[1];
    Vector3 eye = feet + Vector3.up * 1.7f;
    Vector3 to = new Vector3(cx - feet.x, 0f, cz - feet.z);
    if (to.magnitude < 25f) continue;
    float yaw0 = Mathf.Round(Quaternion.LookRotation(to).eulerAngles.y);
    if (Physics.CheckSphere(eye, 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
    bool blocked = blockedByTerrain(eye);
    float bestSc = -1f, bestYaw = yaw0; float[] bestS = null;
    for (int off = -35; off <= 35; off += 10)   // 중심이 화면 안(가로 시야 ±42°)에 남는 범위에서 방향을 돌려 본다
    {
        float[] s = view(eye, yaw0 + off, 32);
        float sc = scoreOf(s, blocked);
        if (sc > bestSc) { bestSc = sc; bestYaw = yaw0 + off; bestS = s; }
    }
    rows.Add(new object[] { bestSc, name, feet, ((bestYaw % 360f) + 360f) % 360f, bestS, blocked, to.magnitude });
}
rows.Sort((p, q) => ((float)q[0]).CompareTo((float)p[0]));
int coarse = rows.Count;
// 위 40개(+ 장소 표/hi: 는 전부)를 96x54 로 다시 잰다
var fine = new System.Collections.Generic.List<object[]>();
for (int i = 0; i < rows.Count; i++)
{
    var r = rows[i]; string nm = (string)r[1];
    if (i >= 40 && nm.StartsWith("g:")) continue;
    Vector3 feet = (Vector3)r[2]; Vector3 eye = feet + Vector3.up * 1.7f;
    float[] s = view(eye, (float)r[3], 96);
    bool blocked = (bool)r[5];
    fine.Add(new object[] { scoreOf(s, blocked), nm, feet, r[3], s, blocked, r[6] });
}
rows = fine;
rows.Sort((p, q) => ((float)q[0]).CompareTo((float)p[0]));
var json = new System.Text.StringBuilder("[\n");
int rank = 0;
foreach (var r in rows)
{
    var f = (Vector3)r[2]; var s = (float[])r[4];
    string line = string.Format("{{\"rank\":{0},\"name\":\"{1}\",\"score\":{2:F1},\"x\":{3:F1},\"y\":{4:F1},\"z\":{5:F1},\"yaw\":{6:F0},\"building\":{7:F3},\"sea\":{8:F3},\"sky\":{9:F3},\"terrain\":{10:F3},\"spread\":{11:F2},\"blocked\":{12}}}",
        ++rank, r[1], (float)r[0], f.x, f.y, f.z, (float)r[3], s[0], s[1], s[2], s[3], s[4], ((bool)r[5]) ? "true" : "false");
    json.Append(line).Append(rank < rows.Count ? ",\n" : "\n");
    if (rank <= 14) sbo.Append(line).Append("\n");
}
json.Append("]\n");
System.IO.Directory.CreateDirectory("docs/qa/evidence/debug-view");
System.IO.File.WriteAllText("docs/qa/evidence/debug-view/scores.json", json.ToString());
sbo.Append("coarse=" + coarse + " cands=" + cands.Count + " fine=" + rows.Count + " vfov=" + vfov + "\n");
return sbo.ToString();
