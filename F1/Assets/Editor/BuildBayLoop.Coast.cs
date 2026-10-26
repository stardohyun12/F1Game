using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 과제 1514 (D-145) — 해안 구조물: 그림판 색칠 같던 해안 비탈을 "사람이 깎은 길"(D-133) 논리의 실제 해안도로처럼.
/// 전부 빌더 안 절차적 메시(새 FBX 없음). 재질은 4묶음 BL_Rock(+RockDark·WetRock)·BL_Concrete·BL_Rail·BL_Foam — 그림체가 바뀌면 이것만 바꾼다.
///  1) 물가: 해면(0m) 등고선을 따라 갯바위 무리(반쯤 잠김) + 흰 거품 띠 + 젖은 어두운 띠 → 바다·땅 칼선을 덮는다.
///  2) 바다 쪽 길 아래: 높이차 크면 계단진 석축 옹벽(지형에 붙인 띠), 작으면 사석(돌 무더기). 부두 겉면이 비탈에 걸쳤던 칸도 석축 호안.
///  3) 방파제: 양옆 물속에 테트라포드(4다리) 무더기.
///  4) 산 쪽 깎은 면: 층 무늬 바위 노출(납작한 판 2색) + 발치 짧은 석축 + 배수로 + 높은 면 낙석 방지망.
///  5) 길가: 해안 구간 반사봉(난간 없는 곳), 전망 쉼터 2곳. 난간 종류 섞기(가드레일·석조)는 Bands() 에서.
/// 모든 수치는 가설(보고서 programmer-20261026-1514).
/// </summary>
public static partial class BuildBayLoop
{
    class Batch { public List<Vector3> v = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>(); }

    /// <summary>재질 종류별·80m 칸별로 메시를 묶는다(6만 정점 넘으면 새 메시).</summary>
    class Chunks
    {
        readonly Transform parent; readonly Dictionary<string, (Material m, bool col)> kinds;
        readonly Dictionary<(string, int, int), Batch> open = new Dictionary<(string, int, int), Batch>();
        int serial; public int meshes, tris;
        public Chunks(Transform p, Dictionary<string, (Material, bool)> k) { parent = p; kinds = k; }
        public Batch Get(string kind, Vector3 p)
        {
            var k = (kind, Mathf.FloorToInt(p.x / 80f), Mathf.FloorToInt(p.z / 80f));
            if (!open.TryGetValue(k, out var b) || b.v.Count > 60000) { if (b != null) Emit(k, b); open[k] = b = new Batch(); }
            return b;
        }
        void Emit((string kind, int x, int z) k, Batch b)
        {
            if (b.v.Count == 0) return;
            var (m, col) = kinds[k.kind];
            MeshGo(parent, $"Coast_{k.kind}_{k.x}_{k.z}_{serial++}", b.v, b.uv, new[] { b.t }, new[] { m }, col);
            meshes++; tris += b.t.Count / 3;
        }
        public void Flush() { foreach (var kv in open) Emit(kv.Key, kv.Value); open.Clear(); }
    }

    /// <summary>삼각형 하나(면마다 정점 따로 → 각진 면 음영). inside 쪽을 향하면 뒤집는다.
    /// 지형에 붙인 면은 inside 를 멀리(아래 50~100m) 둔다 — 1m 아래로 두니 절벽(거의 수직)에서 삼각형 중점 치우침이 이겨 절반이 뒤집혀 안 보였다(1514 2차 캡처).</summary>
    static void Tri(Batch b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 inside)
    {
        var n = Vector3.Cross(p1 - p0, p2 - p0);
        if (Vector3.Dot(n, (p0 + p1 + p2) / 3f - inside) < 0) { var t = p1; p1 = p2; p2 = t; }
        int i = b.v.Count; b.v.Add(p0); b.v.Add(p1); b.v.Add(p2);
        b.uv.Add(new Vector2(p0.x + p0.y, p0.z)); b.uv.Add(new Vector2(p1.x + p1.y, p1.z)); b.uv.Add(new Vector2(p2.x + p2.y, p2.z));
        b.t.Add(i); b.t.Add(i + 1); b.t.Add(i + 2);
    }
    static void Quad(Batch b, Vector3 a, Vector3 c, Vector3 d, Vector3 e, Vector3 inside) { Tri(b, a, c, d, inside); Tri(b, c, e, d, inside); }

    static readonly Vector3[] IcoV = MakeIco();
    static readonly int[] IcoF = { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
    static Vector3[] MakeIco()
    {
        float t = (1f + Mathf.Sqrt(5f)) * .5f;
        return new[] { new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0), new Vector3(0,-1,t), new Vector3(0,1,t),
                       new Vector3(0,-1,-t), new Vector3(0,1,-t), new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1) }.Select(v => v.normalized).ToArray();
    }
    static float Rf(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    /// <summary>바위: 정이십면체 12점을 점마다 0.7~1.2 반지름으로 흔들고, 축마다 늘리고, 임의로 돌린다 — 같은 모양이 안 나온다. size = 가장 긴 지름.</summary>
    static void Rock(Batch b, System.Random r, Vector3 c, float size, float flat = 1f)
    {
        var sc = new Vector3(1f, Rf(r, .45f, .8f) * flat, Rf(r, .6f, 1f)) * (size * .5f);
        var rot = Quaternion.Euler(Rf(r, -18f, 18f), Rf(r, 0f, 360f), Rf(r, -18f, 18f));
        var p = new Vector3[12];
        for (int k = 0; k < 12; k++) { var v = IcoV[k] * Rf(r, .7f, 1.2f); p[k] = c + rot * Vector3.Scale(v, sc); }
        for (int f = 0; f < IcoF.Length; f += 3) Tri(b, p[IcoF[f]], p[IcoF[f + 1]], p[IcoF[f + 2]], c);
    }

    /// <summary>상자(판·담): 중심·크기·회전.</summary>
    static void BoxM(Batch b, Vector3 c, Vector3 size, Quaternion rot)
    {
        var h = size * .5f; var p = new Vector3[8];
        for (int k = 0; k < 8; k++) p[k] = c + rot * new Vector3((k & 1) == 0 ? -h.x : h.x, (k & 2) == 0 ? -h.y : h.y, (k & 4) == 0 ? -h.z : h.z);
        int[][] faces = { new[] { 0, 1, 2, 3 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 4, 5 }, new[] { 2, 3, 6, 7 }, new[] { 0, 2, 4, 6 }, new[] { 1, 3, 5, 7 } };
        foreach (var f in faces) Quad(b, p[f[0]], p[f[1]], p[f[2]], p[f[3]], c);
    }

    /// <summary>테트라포드: 정사면체 꼭짓점 4방향으로 뻗은 육각 원뿔대 다리 4개(가운데서 겹침). s = 다리 끝까지 길이.</summary>
    static void Tetrapod(Batch b, System.Random r, Vector3 c, float s)
    {
        var rot = Quaternion.Euler(Rf(r, 0f, 360f), Rf(r, 0f, 360f), Rf(r, 0f, 360f));
        foreach (var d0 in new[] { new Vector3(1, 1, 1), new Vector3(1, -1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1) })
        {
            var a = rot * d0.normalized; var u = Vector3.Cross(a, Mathf.Abs(a.y) < .9f ? Vector3.up : Vector3.right).normalized; var w = Vector3.Cross(a, u);
            Vector3 b0 = c - a * (s * .12f), b1 = c + a * s; float r0 = s * .36f, r1 = s * .2f;
            var ring0 = new Vector3[6]; var ring1 = new Vector3[6];
            for (int k = 0; k < 6; k++) { float ang = k * Mathf.PI / 3f; var o = u * Mathf.Cos(ang) + w * Mathf.Sin(ang); ring0[k] = b0 + o * r0; ring1[k] = b1 + o * r1; }
            for (int k = 0; k < 6; k++)
            {
                int k1 = (k + 1) % 6; var mid = (ring0[k] + ring1[k1]) * .5f; var ax = b0 + a * Vector3.Dot(mid - b0, a);
                Quad(b, ring0[k], ring0[k1], ring1[k], ring1[k1], ax);
                Tri(b, b1, ring1[k], ring1[k1], c);   // 끝 마개
            }
        }
    }

    /// <summary>OpenGroundMeshes 가 비탈이라 뺀 부두·광장 칸(과제 1514: 2m 칸이 5m 비탈에 걸쳐 +2cm 로 깔려 큰 삼각 톱니로 보였다).</summary>
    static readonly List<(float x, float z)> ogSteep = new List<(float, float)>();

    /// <summary>석축 겉면 높이: 지형 +0.25m 에 1.1m 마다 0.18m 턱(돌 줄눈 층) — 지형에 붙인 띠라 땅 밑으로 안 들어간다.</summary>
    static float Masonry(float x, float z) { float h = H(x, z); return h + 0.25f + (Mathf.Repeat(h, 1.1f) < .22f ? .18f : 0f); }

    static string Coast(Transform parent, Road main)
    {
        if (allGrid == null) InitAllGrid();
        var rnd = new System.Random(1514);
        var sb = new List<string>();
        foreach (var k in new[] { "Rock", "RockDark", "WetRock", "Foam", "Rail" }) FogLikeGround(M(k));
        var foamM = M("Foam"); if (foamM.HasProperty("_FaceShade")) { foamM.SetFloat("_FaceShade", 0f); EditorUtility.SetDirty(foamM); }
        var kinds = new Dictionary<string, (Material, bool)>
        {
            ["Rock"] = (M("Rock"), false), ["RockCol"] = (M("Rock"), true), ["RockDark"] = (M("RockDark"), false), ["Wet"] = (M("WetRock"), false),
            ["Foam"] = (foamM, false), ["Masonry"] = (M("Concrete"), false), ["Wall"] = (M("Concrete"), true), ["Tetra"] = (M("Concrete"), false),
            ["Net"] = (M("Dark"), false), ["Gutter"] = (M("Gutter"), false),
        };
        var ch = new Chunks(parent, kinds);
        var beach = OG().Where(o => (string)o.g["kind"] == "beach").Select(o => o.poly).ToList();
        System.Func<float, float, bool> InBeach = (x, z) => beach.Any(pl => InP(pl, x, z));
        System.Func<float, float, bool> Inside = (x, z) => x > X0 + 3f && x < X1 - 3f && z > Z0 + 3f && z < Z1 - 3f;

        // ── 1) 물가: 지형 2m 격자에서 해면(0) 건너는 점 ──
        int shoreN = 0, shoreRocks = 0;
        var up = Vector3.up;
        for (float x = X0; x < X1 - Step; x += Step)
            for (float z = Z0; z < Z1 - Step; z += Step)
            {
                float h0 = H(x, z);
                foreach (var (dx, dz) in new[] { (Step, 0f), (0f, Step) })
                {
                    float h1 = H(x + dx, z + dz); if ((h0 > 0f) == (h1 > 0f)) continue;
                    float tt = h0 / (h0 - h1); float cx = x + dx * tt, cz = z + dz * tt;
                    if (!Inside(cx, cz)) continue;
                    var g = new Vector3(H(cx + 1f, cz) - H(cx - 1f, cz), 0f, H(cx, cz + 1f) - H(cx, cz - 1f));
                    if (g.sqrMagnitude < 1e-5f) continue;
                    var land = g.normalized; var tan = new Vector3(land.z, 0f, -land.x); var c0 = new Vector3(cx, 0f, cz);
                    shoreN++;
                    bool sand = InBeach(cx, cz) || InBeach(cx + land.x * 3f, cz + land.z * 3f);
                    bool road = NearAnyRoad(cx, cz, 0.6f);
                    // 거품: 해면 +6cm 너덜한 띠(바다 쪽 0.4m) + 가끔 바깥 둘째 줄
                    {
                        var fb = ch.Get("Foam", c0);
                        for (int line = 0; line < (rnd.NextDouble() < .4 ? 2 : 1); line++)
                        {
                            float off = line == 0 ? Rf(rnd, .1f, .6f) : Rf(rnd, 1.4f, 2.4f), len = Rf(rnd, 1.6f, 2.8f), wd = line == 0 ? Rf(rnd, .5f, 1.1f) : Rf(rnd, .2f, .45f);
                            var m = c0 - land * off + up * 0.09f + tan * Rf(rnd, -.4f, .4f);
                            Vector3 a = m - tan * len * .5f, e = m + tan * len * .5f;
                            var q0 = a + land * wd * Rf(rnd, .3f, .6f); var q1 = e + land * wd * Rf(rnd, .3f, .6f);
                            var q2 = a - land * wd * Rf(rnd, .2f, .5f); var q3 = e - land * wd * Rf(rnd, .2f, .5f);
                            Quad(fb, q0, q1, q2, q3, m - up);
                        }
                    }
                    // 젖은 띠: 등고선에서 육지 쪽으로 땅이 해면 +0.6m 될 때까지(0.3~1.6m)를 4x3 격자로 지형 +5cm 에 붙임
                    // (네 귀만 붙인 판은 절벽에서 땅을 뚫고 조각으로 보였다 — 1차 캡처)
                    if (!road)
                    {
                        float dep = 0.3f; while (dep < 1.6f && H(cx + land.x * dep, cz + land.z * dep) < 0.6f) dep += 0.1f;
                        float len = Rf(rnd, 2.0f, 2.6f); var wb = ch.Get("Wet", c0); var gv = new Vector3[5, 4];
                        for (int a = 0; a < 5; a++) for (int c = 0; c < 4; c++) { var q = c0 + tan * (len * (a / 4f - .5f)) + land * (dep * c / 3f); q.y = Mathf.Max(H(q.x, q.z), 0f) + 0.05f; gv[a, c] = q; }
                        for (int a = 0; a < 4; a++) for (int c = 0; c < 3; c++) Quad(wb, gv[a, c], gv[a + 1, c], gv[a, c + 1], gv[a + 1, c + 1], (gv[a, c] + gv[a + 1, c + 1]) * .5f - up * 100f);
                    }
                    // 갯바위: 모래 해변·길 밑은 빼고. 크기 0.4~4m(작은 게 많게), 반쯤 잠김. 물가 바위는 콜라이더 없음(차가 리스폰선 y-3 위 바위에 얹혀 갇히지 않게 — 가설)
                    if (!sand && !NearAnyRoad(cx, cz, 1.5f) && !NearBuilding(c0, 0.5f))
                    {
                        int cnt = rnd.NextDouble() < .75 ? 1 + (rnd.NextDouble() < .45 ? 1 : 0) : 0;
                        for (int k = 0; k < cnt; k++)
                        {
                            float u = (float)rnd.NextDouble(); float size = k == 0 ? .5f + 3.5f * Mathf.Pow(u, 1.6f) : Rf(rnd, .4f, 1.2f);
                            var pc = c0 - land * Rf(rnd, -.6f, 2.2f) + tan * Rf(rnd, -1.1f, 1.1f);
                            float hy = H(pc.x, pc.z); pc.y = Mathf.Max(hy, -0.6f * size) - size * Rf(rnd, .05f, .25f);
                            Rock(ch.Get("Rock", pc), rnd, pc, size); shoreRocks++;
                        }
                    }
                }
            }
        sb.Add($"물가 등고선점 {shoreN}, 갯바위 {shoreRocks}");

        // ── 2)·4)·5) 차길 양옆 ──
        int revCols = 0, riprap = 0, strata = 0, wallM = 0, netCols = 0, posts = 0, refl = 0;
        var gR = new GameObject("CoastRoadside").transform; gR.SetParent(parent);
        var viewCand = new List<(float score, Road r, int i, int side)>();
        foreach (var r in roads.Where(q => q.Car && !q.overWater))
        {
            var P = r.pts; int n = P.Count;
            foreach (int side in new[] { 1, -1 })
            {
                Vector3[] prevRev = null, prevNet = null; int prevNetI = -99;
                for (int i = 0; i < n; i += 3)
                {
                    var p = P[i]; var f = Fwd(P, i, r.closed); var nrm = -side * new Vector3(f.z, 0, -f.x);
                    float wk = r.Walk(i, side), lh = r.LaneHalf(i); var o = p + nrm * (lh + wk); o.y = p.y;
                    float h4 = H(o.x + nrm.x * 4f, o.z + nrm.z * 4f), h3 = H(o.x + nrm.x * 3f, o.z + nrm.z * 3f);
                    var far = o + nrm * 25f; bool seaward = h4 < p.y - 2.5f && (H(far.x, far.z) < .5f || LandDist(far.x, far.z) < 30f);
                    Vector3[] rev = null, net = null;
                    if (seaward)
                    {
                        if (r.coast != null && r.coast[i] && i % 9 == 0) viewCand.Add((p.y - H(o.x + nrm.x * 6f, o.z + nrm.z * 6f), r, i, side));
                        if (h4 < p.y - 3.5f)
                        {
                            // 옹벽: 인도 바깥 0.6m 부터 0.6m 간격 22점, 물 밑(-0.8)에서 멈춤
                            rev = new Vector3[22]; bool ok = true;
                            for (int k = 0; k < 22 && ok; k++)
                            {
                                var q = o + nrm * (0.6f + k * 0.6f);
                                if (k < 12 && (NearAnyRoad(q.x, q.z, 0.3f) && k > 0 || NearBuilding(q, 0.3f))) { ok = false; break; }
                                q.y = Mathf.Max(Masonry(q.x, q.z), -0.8f); if (k == 0) q.y = Mathf.Min(q.y, p.y - 0.3f);
                                rev[k] = q;
                            }
                            if (!ok) rev = null;
                        }
                        else
                        {
                            // 사석: 비탈 1.5~9m 에 돌 무더기. 1m 넘는 돌만 콜라이더, 길·인도에서 1.5m+반지름 떨어뜨림
                            for (float d = 1.5f; d < 9.5f; d += Rf(rnd, 1.0f, 1.8f))
                                for (int k = 0; k < 1 + rnd.Next(2); k++)
                                {
                                    var q = o + nrm * d + f * Rf(rnd, -1.5f, 1.5f); float hq = H(q.x, q.z); if (hq < 0.2f) break;
                                    float size = Rf(rnd, .7f, 2.4f);
                                    if (NearAnyRoad(q.x, q.z, 1.5f + size * .5f) || NearBuilding(q, .5f)) continue;
                                    q.y = hq - size * .15f;
                                    Rock(ch.Get(size >= 1f ? "RockCol" : "Rock", q), rnd, q, size); riprap++;
                                }
                        }
                    }
                    else if (h3 > p.y + 2.0f)
                    {
                        var rot = Quaternion.LookRotation(f);
                        // 과제 1943: 층무늬 바위판 뺌 — 깨진 판자로 읽혔다(도현 1940-4·5). 깎은 면은 지형 셰이더가 바위 칸으로 칠한다(경사 텍스처 B, BakeSlope).
                        // 발치 짧은 석축(콜라이더 있음 — 담이니까) + 배수로
                        float h15 = H(o.x + nrm.x * 1.5f, o.z + nrm.z * 1.5f);
                        var wc = o + nrm * 1.05f;
                        if (h15 > p.y + 1.0f && !NearAnyRoad(wc.x, wc.z, 0.2f) && !NearBuilding(wc, .3f))
                        {
                            float wh = Mathf.Min(1.5f, h15 - p.y + .3f); wc.y = p.y - .2f + wh * .5f;
                            BoxM(ch.Get("Wall", wc), wc, new Vector3(.5f, wh, 3.1f), rot); wallM += 3;
                            var gc = o + nrm * .5f; gc.y = Mathf.Max(H(gc.x, gc.z), p.y - .1f) + .03f;
                            var gb = ch.Get("Gutter", gc);
                            Quad(gb, gc - f * 1.55f - nrm * .25f, gc + f * 1.55f - nrm * .25f, gc - f * 1.55f + nrm * .25f, gc + f * 1.55f + nrm * .25f, gc - up);
                        }
                        // 과제 1943: 낙석 방지망(어두운 띠·기둥) 뺌 — 지형 +0.35 띠가 검은 덩어리로 보였다(도현 1940-4).
                    }
                    // 반사봉: 해안 구간·인도 있는 쪽·난간 없는 곳(바깥 1.5m 땅이 노면 −2m 위), 12m 마다
                    if (r.coast != null && r.coast[i] && i % 12 == 0 && wk >= .3f && H(o.x + nrm.x * 1.5f, o.z + nrm.z * 1.5f) > p.y - 2f)
                    {
                        var rp = o - nrm * .2f; rp.y = p.y + .08f;
                        Box(gR, "ReflectorPost", new Vector3(.08f, .9f, .08f), rp + up * .45f, Quaternion.LookRotation(f), M("White"), false);
                        Box(gR, "Reflector", new Vector3(.1f, .14f, .1f), rp + up * .8f, Quaternion.LookRotation(f), M("Orange"), false);
                        refl++;
                    }
                    if (rev != null && prevRev != null)
                    {
                        var mb = ch.Get("Masonry", rev[0]);
                        // 도로 방향 단면 간격(~6m)을 0.6m 로 다시 쪼개 지형에 붙인다 — 직선으로 이으면 사이에서 솟은 지형이 큰 삼각 톱니로 뚫고 나왔다(과제 1514 zoom_bw)
                        int m = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(rev[0], prevRev[0]) / 0.6f), 1, 20);
                        for (int k = 0; k < 21; k++)
                            for (int t = 0; t < m; t++)
                            {
                                Vector3 L(Vector3 a0, Vector3 b0, float f) { var q = Vector3.Lerp(a0, b0, f); q.y = Mathf.Max(Masonry(q.x, q.z), -0.8f); if (k == 0 && a0 == prevRev[0]) q.y = Mathf.Min(q.y, Mathf.Lerp(prevRev[0].y, rev[0].y, f)); return q; }
                                float f0 = t / (float)m, f1 = (t + 1) / (float)m;
                                Vector3 a1 = L(prevRev[k], rev[k], f0), a2 = L(prevRev[k], rev[k], f1), b1 = L(prevRev[k + 1], rev[k + 1], f0), b2 = L(prevRev[k + 1], rev[k + 1], f1);
                                var mid = (a1 + b2) * .5f; Quad(mb, a1, a2, b1, b2, mid - (up + nrm) * 50f);
                            }
                        revCols++;
                    }
                    if (net != null && prevNet != null && i - prevNetI == 3)
                    {
                        var nb = ch.Get("Net", net[0]);
                        for (int k = 0; k < 6; k++) { var mid = (net[k] + prevNet[k + 1]) * .5f; Quad(nb, prevNet[k], net[k], prevNet[k + 1], net[k + 1], mid + (nrm - up) * 50f); }
                        netCols++;
                    }
                    prevRev = rev; prevNet = net; if (net != null) prevNetI = i;
                }
            }
        }
        sb.Add($"옹벽 {revCols * 3}m, 사석 {riprap}, 층무늬판 {strata}, 발치석축 {wallM}m, 낙석망 {netCols * 3}m·기둥 {posts}, 반사봉 {refl}");

        // ── 2b) 부두·광장 비탈 칸 → 석축 호안(0.5m 격자로 지형에 붙임) ──
        int hz = 0;
        foreach (var (x, z) in ogSteep)
        {
            if (NearAnyRoad(x + 1f, z + 1f, 0.8f) || NearBuilding(new Vector3(x + 1f, 0, z + 1f), 0.3f)) continue;
            hz++;
            var mb = ch.Get("Masonry", new Vector3(x, 0, z)); var v = new Vector3[5, 5];
            for (int a = 0; a < 5; a++) for (int c = 0; c < 5; c++) { float qx = x + a * .5f, qz = z + c * .5f; v[a, c] = new Vector3(qx, Mathf.Max(Masonry(qx, qz), -0.8f), qz); }
            for (int a = 0; a < 4; a++) for (int c = 0; c < 4; c++) Quad(mb, v[a, c], v[a + 1, c], v[a, c + 1], v[a + 1, c + 1], (v[a, c] + v[a + 1, c + 1]) * .5f - up * 100f);
        }
        sb.Add($"부두 비탈·가장자리 호안 {hz}/{ogSteep.Count}칸");

        // ── 3) 방파제 테트라포드 ──
        int tetra = 0;
        var bw = roads.FirstOrDefault(q => q.id == "O_breakwater");
        if (bw != null)
            for (int i = 0; i < bw.pts.Count; i += 2)
            {
                var p = bw.pts[i]; var f = Fwd(bw.pts, i, bw.closed);
                foreach (int side in new[] { 1, -1 })
                {
                    var nrm = -side * new Vector3(f.z, 0, -f.x); float edge = bw.LaneHalf(i) + bw.Walk(i, side);
                    int pile = 1 + rnd.Next(3);
                    for (int k = 0; k < pile; k++)
                    {
                        float s = Rf(rnd, .8f, 1.15f); var q = p + nrm * (edge + 1.3f + k * 1.5f + Rf(rnd, 0f, 1.2f)) + f * Rf(rnd, -.9f, .9f);
                        if (H(q.x, q.z) > -0.3f || NearAnyRoad(q.x, q.z, 0.8f)) continue;
                        q.y = Rf(rnd, -.9f, .35f) - k * .35f;
                        Tetrapod(ch.Get("Tetra", q), rnd, q, s * 1.1f); tetra++;
                    }
                }
            }
        sb.Add($"테트라포드 {tetra}");

        // ── 5) 전망 쉼터 2곳: 해안 구간 바다 쪽 높이차가 큰 곳, 서로 150m 이상 ──
        int views = 0; var picked = new List<Vector3>();
        foreach (var (score, r, i, side) in viewCand.OrderByDescending(c => c.score))
        {
            if (views >= 2 || score < 4f) break;
            var p = r.pts[i]; if (picked.Any(q => Vector3.Distance(q, p) < 150f)) continue;
            var f = Fwd(r.pts, i, r.closed); var nrm = -side * new Vector3(f.z, 0, -f.x); var rot = Quaternion.LookRotation(f);
            var c = p + nrm * (r.LaneHalf(i) + r.Walk(i, side) + 1.7f);
            bool blocked = false;
            foreach (var dd in new[] { -2.5f, 0f, 2.5f }) foreach (var dn in new[] { 0f, 1.5f }) { var q = c + f * dd + nrm * dn; if (NearBuilding(q, .5f) || roads.Any(o => o.Car && o != r && DistToRoad(o, q) < o.LaneHalf(0) + 2f)) blocked = true; }
            if (blocked) continue;
            float top = p.y + .1f, lo = Mathf.Min(H(c.x, c.z), Mathf.Min(H(c.x + nrm.x * 1.6f, c.z + nrm.z * 1.6f), H(c.x - nrm.x * 1.6f, c.z - nrm.z * 1.6f))) - .4f;
            var g = new GameObject($"Viewpoint_{views}").transform; g.SetParent(parent); g.position = c;
            Box(g, "Pad", new Vector3(3.2f, top - lo, 6f), new Vector3(c.x, (top + lo) * .5f, c.z), rot, M("Concrete"), true);
            // 바깥 3면 난간(보이는 석조 + 안 보이는 막이)
            foreach (var (pos, size) in new[] { (c + nrm * 1.5f, new Vector3(.3f, .8f, 6f)), (c + f * 2.9f + nrm * .1f, new Vector3(2.9f, .8f, .3f)), (c - f * 2.9f + nrm * .1f, new Vector3(2.9f, .8f, .3f)) })
            {
                var pp = pos; pp.y = top + .4f; Box(g, "Parapet", size, pp, rot, M("Concrete"), false);
                var guard = Box(g, "INTENT_Guard", new Vector3(size.x, 1.4f, size.z), pp + up * .3f, rot, M("Dark"), true); Object.DestroyImmediate(guard.GetComponent<MeshRenderer>());
            }
            var bc = c + nrm * .7f; bc.y = top;
            Box(g, "BenchSeat", new Vector3(.45f, .08f, 1.8f), bc + up * .45f, rot, M("Wood"), false);
            Box(g, "BenchLegs", new Vector3(.35f, .45f, 1.5f), bc + up * .22f, rot, M("Dark"), false);
            var sc = c - nrm * .9f + f * 2.4f; sc.y = top;
            Box(g, "SignPost", new Vector3(.08f, 1.6f, .08f), sc + up * .8f, rot, M("Dark"), false);
            Box(g, "SignBoard", new Vector3(.05f, .5f, .9f), sc + up * 1.5f, rot, M("MartSign"), false);
            picked.Add(p); views++;
            sb.Add($"전망쉼터 {r.id}#{i}({c.x:F0},{top:F1},{c.z:F0}) 높이차 {score:F1}m");
        }

        ch.Flush();
        return $"해안(1514): {string.Join(", ", sb)}, 메시 {ch.meshes}개 삼각형 {ch.tris}";
    }

    static float DistToRoad(Road r, Vector3 q)
    {
        float best = float.MaxValue; foreach (var p in r.pts) { float d = (p.x - q.x) * (p.x - q.x) + (p.z - q.z) * (p.z - q.z); if (d < best) best = d; }
        return Mathf.Sqrt(best);
    }
}
