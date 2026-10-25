using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 과제 0404 — 언덕 도착 장면 보강(갭 #9) + 육지 쪽 맵 끝(갭 #6). 모든 수치는 가설(보고서 programmer-20261026-0404).
///  1) 바람 풀: 꼭대기·언덕 찻집 둘레 풀밭에 풀잎 삼각형(BL_WindGrass — 색은 땅 아틀라스 텍셀 그대로). 구름(CloudDrift) + 땅·풀 구름 그림자.
///  2) 홀로 선 집: 본선 꼭대기에서 내려다보이는 앞쪽 풀밭(바다 쪽 가산점)에 farmhouse_01 한 채 + 낮은 돌담.
///  3) 맵 끝: 고갯길(O_mountain) 끝 = 공사 중 차단 + 고개 너머로 넘어가는 길 모양, 터널(V_tunnel) 끝 = 입구 틀 + 공사 중 차단,
///     육지 쪽 먼 능선 실루엣 2겹(안개에 점점 씻김).
/// Proto 의 WindGrass 셰이더·머티리얼은 쓰지 않는다(BL_ 사본·BayLoop 전용 셰이더).
/// </summary>
public static partial class BuildBayLoop
{
    const string GrassShader = "PitCrew/BayLoop/WindGrass";

    // ------------------------------------------------------------------ 모든 길(골목·계단 포함) 점 4m 칸 해시 — 풀잎·집 자리 거르기
    static Dictionary<(int, int), List<(Vector3 p, float half)>> allGrid;
    static void InitAllGrid()
    {
        allGrid = new Dictionary<(int, int), List<(Vector3, float)>>();
        foreach (var r in roads)
            for (int i = 0; i < r.pts.Count; i++)
            {
                var p = r.pts[i]; var k = (Mathf.FloorToInt(p.x / 4f), Mathf.FloorToInt(p.z / 4f));
                if (!allGrid.TryGetValue(k, out var l)) allGrid[k] = l = new List<(Vector3, float)>();
                l.Add((p, r.LaneHalf(i) + Mathf.Max(r.Walk(i, 1), r.Walk(i, -1))));
            }
    }
    static bool NearAnyRoad(float x, float z, float margin)
    {
        int cx = Mathf.FloorToInt(x / 4f), cz = Mathf.FloorToInt(z / 4f), reach = Mathf.CeilToInt((margin + 8f) / 4f);
        for (int a = cx - reach; a <= cx + reach; a++)
            for (int b = cz - reach; b <= cz + reach; b++)
            {
                if (!allGrid.TryGetValue((a, b), out var l)) continue;
                foreach (var (p, half) in l) { float dx = p.x - x, dz = p.z - z, r = half + margin; if (dx * dx + dz * dz < r * r) return true; }
            }
        return false;
    }
    static float SlopeDeg(float x, float z)
    {
        float gx = (H(x + 1f, z) - H(x - 1f, z)) * .5f, gz = (H(x, z + 1f) - H(x, z - 1f)) * .5f;
        return Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
    }
    /// <summary>풀밭 칸인가: 지형이 풀(포장 아님)·경사 &lt; maxDeg·길/건물/열린 땅(광장·부두·빈터)/밭 밖.</summary>
    static bool GrassSpot(float x, float z, float roadM, float bldM, float maxDeg, List<(Dictionary<string, object> g, List<Vector2> poly)> green, List<List<Vector2>> hard, List<Rect> fieldRects)
    {
        if (x <= X0 + 2f || x >= X1 - 2f || z <= Z0 + 2f || z >= Z1 - 2f) return false;
        float h = H(x, z); if (h < 1.0f) return false;
        if (SlopeDeg(x, z) > maxDeg) return false;
        if (NearAnyRoad(x, z, roadM) || NearBuilding(new Vector3(x, h, z), bldM)) return false;
        foreach (var poly in hard) if (InP(poly, x, z)) return false;
        foreach (var fr in fieldRects) if (fr.Contains(new Vector2(x, z))) return false;
        int pc = pavedCells, ic = islandCells;
        int k = GroundKind(x, z, h - .3f, h + .3f, green);
        pavedCells = pc; islandCells = ic;   // 지면 포장 집계(이미 로그됨)는 건드리지 않는다
        return k < 0;
    }

    static string Hilltop(Transform parent, Road main)
    {
        InitAllGrid();
        var sb = new List<string>();
        var og = OG();
        var green = og.Where(o => (string)o.g["kind"] == "field" || (string)o.g["kind"] == "meadow").ToList();
        var hard = og.Where(o => (string)o.g["kind"] != "field" && (string)o.g["kind"] != "meadow").Select(o => o.poly).ToList();
        var fieldRects = ((List<object>)J["fields"]).Cast<Dictionary<string, object>>().Select(f => { var r = (List<object>)f["rect"]; float x = F(r[0]), z = F(r[1]), w = F(r[2]), d = F(r[3]); return new Rect(x - 2f, z - 2f, w + 4f, d + 4f); }).ToList();

        // 꼭대기 = 언덕 고원 우회길(V_detour)에서 가장 높은 점(밭·농가가 있는 들판). 본선 최고점(-1,44,143)은 언덕 마을 골목 안이라 들판이 아니다.
        var dt = roads.FirstOrDefault(r => r.id == "V_detour") ?? main;
        int ci = 0; for (int i = 1; i < dt.pts.Count; i++) if (dt.pts[i].y > dt.pts[ci].y) ci = i;
        var crest = dt.pts[ci]; var cf = Fwd(dt.pts, ci, dt.closed);
        sb.Add($"꼭대기 {dt.id}#{ci}({crest.x:F0},{crest.y:F1},{crest.z:F0})");

        // ── 1) 바람 풀 ──
        var gm = GrassMat();
        if (gm == null) sb.Add("WindGrass 셰이더 없음");
        else
        {
            var centers = new List<(Vector3 c, float R, string name)> { (crest, 95f, "Crest") };
            var tea = GameObject.Find("TUT_TeaHouse_NPC");
            if (tea != null) centers.Add((tea.transform.position, 45f, "TeaHouse"));
            var gG = new GameObject("WindGrass").transform; gG.SetParent(parent);
            foreach (var (c, R, name) in centers)
            {
                var (bl, tl) = Meadow(gG, name, c, R, gm, green, hard, fieldRects);
                sb.Add($"풀잎 {name} {bl}장/{tl}타일");
            }
        }
        // 구름: Proto 와 같은 CloudDrift(그림자 오프셋 전역) + 납작한 흰 공 구름 9무리
        {
            var clouds = new GameObject("Clouds"); clouds.transform.SetParent(parent);
            var cd = clouds.AddComponent<CloudDrift>(); cd.areaCenter = new Vector3(0f, 0f, 40f); cd.areaHalf = 900f;
            var white = FogLikeGround(M("Cloud")); if (white.HasProperty("_FaceShade")) white.SetFloat("_FaceShade", .35f);
            var rng = new System.Random(404);
            for (int i = 0; i < 9; i++)
            {
                var cc = new Vector3(((float)rng.NextDouble() * 2f - 1f) * 600f, 150f + (float)rng.NextDouble() * 40f, 40f + ((float)rng.NextDouble() * 2f - 1f) * 600f);
                var cl = new GameObject("CloudCluster").transform; cl.SetParent(clouds.transform); cl.position = cc;
                int puffs = 3 + rng.Next(3);
                for (int k = 0; k < puffs; k++)
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Sphere); p.name = "BLOCKOUT_CloudPuff"; p.transform.SetParent(cl);
                    float sz = 28f + (float)rng.NextDouble() * 26f;
                    p.transform.position = cc + new Vector3(((float)rng.NextDouble() * 2f - 1f) * 40f, (float)rng.NextDouble() * 8f, ((float)rng.NextDouble() * 2f - 1f) * 25f);
                    p.transform.localScale = new Vector3(sz * 1.6f, sz * .45f, sz);
                    Object.DestroyImmediate(p.GetComponent<Collider>());
                    var rr = p.GetComponent<Renderer>(); rr.sharedMaterial = white; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rr.receiveShadows = false;
                }
            }
            sb.Add("구름 9무리(CloudDrift)");
        }

        // ── 2) 홀로 선 집 + 낮은 돌담 ──
        sb.Add(LoneHouse(parent, crest, green, hard, fieldRects));
        return "언덕(0404): " + string.Join(", ", sb);
    }

    /// <summary>새로 만든 팔레트 재질(BL_Cloud·BL_Ridge*)은 셰이더 기본 안개 30/105m 로 생긴다 → 땅 재질의 안개·환경색을 복사(먼 능선이 거리별로 다르게 씻기게).</summary>
    static Material FogLikeGround(Material m)
    {
        var t = AssetDatabase.LoadAssetAtPath<Material>(ImportBayLoopDelivery.TerrainMat);
        if (t == null || m == null || !m.HasProperty("_FogStart")) return m;
        foreach (var f in new[] { "_FogStart", "_FogEnd" }) m.SetFloat(f, t.GetFloat(f));
        foreach (var c in new[] { "_FogColor", "_Ambient" }) if (m.HasProperty(c)) m.SetColor(c, t.GetColor(c));
        EditorUtility.SetDirty(m); return m;
    }

    static Material GrassMat()
    {
        var sh = Shader.Find(GrassShader); if (sh == null) return null;
        string p = MatDir + "/BL_WindGrass.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        // 땅(M_BL_TerrainSlope)과 같은 아틀라스·풀 칸·텍셀 배율·흙 패치 → 풀잎 색 = 그 자리 땅 색
        var t = AssetDatabase.LoadAssetAtPath<Material>(ImportBayLoopDelivery.TerrainMat);
        if (t != null)
        {
            m.SetTexture("_MainTex", t.GetTexture("_MainTex"));
            foreach (var v in new[] { "_TileGrass" }) m.SetVector(v, t.GetVector(v));
            foreach (var f in new[] { "_WorldScale", "_SoilAmount", "_PatchScale", "_PatchThreshold", "_FaceShade", "_FogStart", "_FogEnd" }) m.SetFloat(f, t.GetFloat(f));
            foreach (var c in new[] { "_FogColor", "_Ambient" }) m.SetColor(c, t.GetColor(c));   // 셰이더 기본 안개 30/105m 그대로면 100m 밖 풀이 하늘색으로 뜬다(실측)
        }
        m.SetFloat("_TipLift", 1.08f); m.SetFloat("_Band", .08f);   // 1.14/.10 은 먼 풀밭이 땅보다 한 단 밝았다 — 가설
        EditorUtility.SetDirty(m); return m;
    }

    /// <summary>원 하나 안 풀밭에 풀잎(0.7m² 당 하나, 높이 0.45~0.8m). 30m 타일 메시, 정점 색 a = 흔들림(끝 1), uv = 뿌리 xz.</summary>
    static (int blades, int tiles) Meadow(Transform parent, string name, Vector3 c, float R, Material mat, List<(Dictionary<string, object> g, List<Vector2> poly)> green, List<List<Vector2>> hard, List<Rect> fieldRects)
    {
        const float tile = 30f; var rng = new System.Random(name.GetHashCode() & 0x7fffffff ^ 404); int blades = 0, tiles = 0;
        int nt = Mathf.CeilToInt(R * 2f / tile);
        for (int tz = 0; tz < nt; tz++)
            for (int tx = 0; tx < nt; tx++)
            {
                float x0 = c.x - R + tx * tile, z0 = c.z - R + tz * tile;
                var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>(); var uv = new List<Vector2>();
                int n = Mathf.RoundToInt(tile * tile / .7f);
                for (int i = 0; i < n; i++)
                {
                    float x = x0 + (float)rng.NextDouble() * tile, z = z0 + (float)rng.NextDouble() * tile;
                    float yaw = (float)rng.NextDouble() * Mathf.PI * 2f, ht = .45f + (float)rng.NextDouble() * .35f, sh = .92f + (float)rng.NextDouble() * .08f;
                    if (new Vector2(x - c.x, z - c.z).magnitude > R || !GrassSpot(x, z, 1.2f, 1.0f, 40f, green, hard, fieldRects)) continue;
                    var side = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw)) * .11f;
                    var bp = new Vector3(x, H(x, z) - .03f, z);
                    int b0 = v.Count;
                    v.Add(bp - side); v.Add(bp + side); v.Add(bp + Vector3.up * ht + side * .2f);
                    col.Add(new Color(sh, sh, sh, 0f)); col.Add(new Color(sh, sh, sh, 0f)); col.Add(new Color(sh, sh, sh, 1f));
                    var root = new Vector2(x, z); uv.Add(root); uv.Add(root); uv.Add(root);
                    tri.Add(b0); tri.Add(b0 + 2); tri.Add(b0 + 1); blades++;
                }
                if (v.Count == 0) continue;
                // 이름 = 메시 에셋 경로(MeshGo) → 타일마다 달라야 앞 타일 메시가 지워지지 않는다
                var go = MeshGo(parent, $"GrassTile_{name}_{tx}_{tz}", v, uv, new[] { tri }, new[] { mat }, false);
                var mesh = go.GetComponent<MeshFilter>().sharedMesh; mesh.SetColors(col);
                var nrm = new Vector3[v.Count]; for (int k = 0; k < nrm.Length; k++) nrm[k] = Vector3.up; mesh.normals = nrm;
                mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.one);   // 흔들림 0.3m 만큼 여유
                EditorUtility.SetDirty(mesh);
                var mr = go.GetComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tiles++;
            }
        return (blades, tiles);
    }

    /// <summary>꼭대기 둘레 40~170m(사방 10° 간격)에서 집 자리(반경 12m 안 길·건물 없음, 풀, 평평)를 찾는다.
    /// 꼭대기 눈높이에서 집 지붕까지 막힘 없어야 한다(콜라이더 Linecast). 점수 = 바다에 가까울수록 + 가까울수록.</summary>
    static string LoneHouse(Transform parent, Vector3 crest, List<(Dictionary<string, object> g, List<Vector2> poly)> green, List<List<Vector2>> hard, List<Rect> fieldRects)
    {
        Physics.SyncTransforms();
        var eye = crest + Vector3.up * 2.5f;
        float best = float.MaxValue; Vector3 bp = Vector3.zero;
        for (float d = 40f; d <= 170f; d += 6f)
            for (float a = 0f; a < 360f; a += 10f)
            {
                var q = crest + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)) * d; float h = H(q.x, q.z);
                if (!GrassSpot(q.x, q.z, 12f, 10f, 18f, green, hard, fieldRects)) continue;
                bool ok = true;
                foreach (var (ox, oz) in new[] { (6f, 6f), (-6f, 6f), (6f, -6f), (-6f, -6f) }) if (!GrassSpot(q.x + ox, q.z + oz, 4f, 3f, 24f, green, hard, fieldRects)) { ok = false; break; }
                if (!ok) continue;
                if (Physics.Linecast(eye, new Vector3(q.x, h + 5f, q.z))) continue;   // 건물·지형에 가려지면 탈락
                float score = Mathf.Clamp(LandDist(q.x, q.z), -20f, 150f) * .01f + d * .004f;
                if (score < best) { best = score; bp = q; }
            }
        if (best == float.MaxValue) return "홀로 선 집 자리 없음";
        var g = new GameObject("LoneHouse").transform; g.SetParent(parent);
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(BldDir + "/farmhouse_01.prefab") ?? AssetDatabase.LoadAssetAtPath<GameObject>(BldDir + "/detached_house_01.prefab");
        float y0 = float.MaxValue, y1 = float.MinValue;
        foreach (var (ox, oz) in new[] { (0f, 0f), (3.5f, 4f), (-3.5f, 4f), (3.5f, -4f), (-3.5f, -4f) }) { float h = H(bp.x + ox, bp.z + oz); y0 = Mathf.Min(y0, h); y1 = Mathf.Max(y1, h); }
        var face = Quaternion.LookRotation(Flat(crest - bp));   // 앞면이 꼭대기를 본다 — 넘어오며 정면으로 보이게
        if (pf != null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, g); go.name = "LoneHouse_" + pf.name;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var b = Bounds(go); var off = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            go.transform.SetPositionAndRotation(new Vector3(bp.x, y1 - .05f, bp.z) + face * off, face);
        }
        if (y1 - y0 > .1f) Box(g, "LoneHouse_Plinth", new Vector3(7.4f, y1 - y0 + .4f, 8.4f), new Vector3(bp.x, (y0 + y1) * .5f - .2f, bp.z), face, M("Stone"), true);
        // 낮은 돌담(0.7m): 마당 18×16m, 꼭대기 쪽 가운데 4m 는 문으로 비움. 2m 토막이 땅을 따라 오르내린다.
        int seg = 0; var fwd = face * Vector3.forward; var right = face * Vector3.right;
        var corners = new[] { (-9f, -6f), (9f, -6f), (9f, 10f), (-9f, 10f) };
        for (int e = 0; e < 4; e++)
        {
            var (ax, az) = corners[e]; var (bx, bz) = corners[(e + 1) % 4];
            float len = Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az)); int k = Mathf.CeilToInt(len / 2f);
            for (int s = 0; s < k; s++)
            {
                float t = (s + .5f) / k, lx = Mathf.Lerp(ax, bx, t), lz = Mathf.Lerp(az, bz, t);
                if (e == 2 && Mathf.Abs(lx) < 2f) continue;   // 문(꼭대기 쪽 = +z 변)
                var p = bp + right * lx + fwd * lz; float h = H(p.x, p.z);
                if (NearCar(p, 1f)) continue;
                Box(g, "StoneWall", new Vector3(.45f, .7f, len / k + .05f), new Vector3(p.x, h + .3f, p.z), Quaternion.LookRotation(right * (bx - ax) + fwd * (bz - az)), M("Stone"), true);
                seg++;
            }
        }
        return $"홀로 선 집 ({bp.x:F0},{y1:F1},{bp.z:F0}) 꼭대기에서 {Vector3.Distance(Flat2D(bp), Flat2D(crest)):F0}m, 돌담 {seg}토막";
    }
    static Vector3 Flat2D(Vector3 v) { v.y = 0; return v; }

    // ------------------------------------------------------------------ 3) 맵 끝(육지 쪽)
    static string MapEdge(Transform parent)
    {
        var sb = new List<string>();
        // 고갯길: 끝 40m 앞 공사 중 차단, 끝에서 길이 고개를 넘어 사라지는 모양(보기만)
        var mt = roads.FirstOrDefault(r => r.id == "O_mountain");
        if (mt != null)
        {
            Barricade(parent, mt, mt.len - 40f, "공사 중 · 고갯길 통제");
            sb.Add(PassOverCrest(parent, mt));
        }
        var tn = roads.FirstOrDefault(r => r.id == "V_tunnel");
        if (tn != null) { TunnelPortal(parent, tn); Barricade(parent, tn, tn.len - 7f, "공사 중 터널"); sb.Add("터널 입구+차단"); }
        sb.Add(FarRidges(parent));
        return "맵 끝(0404): " + string.Join(", ", sb);
    }

    /// <summary>길 s 지점에 가로 차단(줄무늬 판 2단 + 받침 + 표지판). INTENT_ 이름 = 길 검사에서 '의도된 막음'.</summary>
    static void Barricade(Transform parent, Road r, float s, string text)
    {
        At(r, s, out var c, out var f, out var rt); f = Flat(f); rt = new Vector3(f.z, 0, -f.x);
        var g = new GameObject("INTENT_Barricade_" + r.id).transform; g.SetParent(parent);
        float w = r.LaneHalf(Mathf.Clamp(Mathf.RoundToInt(s), 0, r.pts.Count - 1)) * 2f + 1.2f;
        var rot = Quaternion.LookRotation(f);
        int n = Mathf.CeilToInt(w / 1.2f);
        for (int i = 0; i < n; i++)
        {
            var p = c + rt * ((i - (n - 1) * .5f) * 1.2f);
            Box(g, "INTENT_BarBoardLow", new Vector3(1.15f, .22f, .08f), p + Vector3.up * .55f, rot, M(i % 2 == 0 ? "Orange" : "White"), true);
            Box(g, "INTENT_BarBoardHigh", new Vector3(1.15f, .22f, .08f), p + Vector3.up * .95f, rot, M(i % 2 == 0 ? "White" : "Orange"), true);
            Box(g, "INTENT_BarLeg", new Vector3(.08f, 1.1f, .5f), p + Vector3.up * .55f + rt * .5f, rot, M("Dark"), false);
        }
        // 표지판: 왼쪽 가장자리, 길 쪽을 본다
        var sp = c + rt * (-w * .5f - .4f) - f * 1.5f;
        Box(g, "SignPost", new Vector3(.1f, 2.2f, .1f), sp + Vector3.up * 1.1f, rot, M("Dark"), false);
        Box(g, "SignBoard", new Vector3(2.4f, .9f, .06f), sp + Vector3.up * 2.0f, rot, M("Yellow"), false);
        Label(g, text, sp + Vector3.up * 2.0f - f * .05f, Quaternion.LookRotation(f), .34f, new Color(.12f, .1f, .08f));
        // 차단 판 앞 원뿔 3개(낮은 박스로 임시 모양)
        for (int i = -1; i <= 1; i++) Box(g, "Cone", new Vector3(.35f, .6f, .35f), c - f * 2.5f + rt * (i * 1.6f) + Vector3.up * .3f, rot, M("Orange"), false);
    }

    /// <summary>고갯길 끝 너머: 길 방향으로 70m 오르는 고개 둔덕(먼 판보다 높은 곳만) + 그 위로 이어지는 길 띠(콜라이더 없음). 정점 너머는 둔덕 뒤라 안 보인다.</summary>
    static string PassOverCrest(Transform parent, Road r)
    {
        At(r, r.len, out var e, out var f, out _); f = Flat(f); var rt = new Vector3(f.z, 0, -f.x);
        float rise = 7f, top = 70f, back = 150f, halfW = 170f;
        float Ridge(float a, float l) { float u = a < top ? S(a / top) : 1f - S((a - top) / (back - top)); return e.y - .2f + rise * u * (1f - S(Mathf.Abs(l) / halfW)); }
        // 둔덕: 16m 격자, 먼 판보다 낮은 곳은 먼 판 높이(먼 판에 묻힘)
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        int na = Mathf.RoundToInt((back + 20f) / 8f) + 1, nl = Mathf.RoundToInt(halfW * 2f / 8f) + 1;
        for (int j = 0; j < na; j++)
            for (int i = 0; i < nl; i++)
            {
                float a = -20f + j * 8f, l = -halfW + i * 8f; var p = e + f * a + rt * l;
                float y = Mathf.Max(Ridge(Mathf.Max(0f, a), l), FarH(p.x, p.z) - .5f);
                v.Add(new Vector3(p.x, y, p.z)); uv.Add(new Vector2(p.x, p.z));
            }
        for (int j = 0; j + 1 < na; j++)
            for (int i = 0; i + 1 < nl; i++) { int a = j * nl + i, b = a + 1, c = a + nl, d = c + 1; tri.Add(a); tri.Add(b); tri.Add(c); tri.Add(b); tri.Add(d); tri.Add(c); }
        var tm = AssetDatabase.LoadAssetAtPath<Material>(ImportBayLoopDelivery.TerrainMat) ?? M("Ground");
        var mound = MeshGo(parent, "PassMound_" + r.id, v, uv, new[] { tri }, new[] { tm }, false);
        var mm = mound.GetComponent<MeshFilter>().sharedMesh; mm.RecalculateNormals();
        // 길 띠: 끝에서 정점(70m)까지 + 4m, 살짝 오른쪽으로 휜다(고개 너머 휘어 내려감)
        var rv = new List<Vector3>(); var ruv = new List<Vector2>(); var rtri = new List<int>(); float hw = r.width * .5f;
        for (float a = 0f; a <= top + 4f; a += 2f)
        {
            float bend = a * a * .0035f; var p = e + f * a + rt * bend;
            var fd = (f + rt * (a * .007f)).normalized; var side = new Vector3(fd.z, 0, -fd.x);
            float y = Ridge(a, bend) + .06f;
            rv.Add(new Vector3(p.x, y, p.z) - side * hw); rv.Add(new Vector3(p.x, y, p.z) + side * hw); ruv.Add(new Vector2(-hw, a)); ruv.Add(new Vector2(hw, a));
        }
        for (int k = 0; k + 3 < rv.Count; k += 2) { rtri.Add(k); rtri.Add(k + 2); rtri.Add(k + 1); rtri.Add(k + 1); rtri.Add(k + 2); rtri.Add(k + 3); }
        MeshGo(parent, "PassRoadVisual_" + r.id, rv, ruv, new[] { rtri }, new[] { M("Pavement") }, false);
        return $"고개 둔덕 +{rise}m @{top}m";
    }

    /// <summary>터널 끝: 바위 벽 앞 콘크리트 입구 틀(기둥 2 + 상인방) + 어두운 입구 면.</summary>
    static void TunnelPortal(Transform parent, Road r)
    {
        At(r, r.len, out var e, out var f, out _); f = Flat(f); var rt = new Vector3(f.z, 0, -f.x);
        var g = new GameObject("INTENT_TunnelPortal").transform; g.SetParent(parent);
        float hw = r.LaneHalf(r.pts.Count - 1) + .6f, ht = 5.2f; var rot = Quaternion.LookRotation(f);
        var at = e - f * .6f;
        Box(g, "INTENT_TunnelMouth", new Vector3(hw * 2f, ht, .3f), at + f * .4f + Vector3.up * ht * .5f, rot, M("Dark"), true);
        foreach (var sd in new[] { -1f, 1f }) Box(g, "INTENT_TunnelPier", new Vector3(1.2f, ht + 1.2f, 1.6f), at + rt * sd * (hw + .6f) + Vector3.up * (ht + 1.2f) * .5f, rot, M("Concrete"), true);
        Box(g, "INTENT_TunnelLintel", new Vector3(hw * 2f + 2.4f, 1.4f, 1.6f), at + Vector3.up * (ht + .7f), rot, M("Concrete"), false);
    }

    /// <summary>육지 쪽(동·남) 먼 능선 2겹 — 지도 중심(0,180)에서 반경 700·880m 세운 실루엣 판. 안개(무드 90~950m)에 71%·89% 씻긴다.</summary>
    static string FarRidges(Transform parent)
    {
        var c = new Vector2(0f, 180f); int n = 0;
        foreach (var (R, baseTop, amp, key) in new[] { (700f, 120f, 45f, "Ridge1"), (880f, 165f, 60f, "Ridge2") })
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            const float a0 = -150f, a1 = 55f, da = 1.5f;   // 동(0°)에서 남(−90°) 지나 남서까지 — 북·서는 바다
            for (float a = a0; a <= a1 + 1e-3f; a += da)
            {
                float th = a * Mathf.Deg2Rad, x = c.x + Mathf.Cos(th) * R, z = c.y + Mathf.Sin(th) * R;
                float top = baseTop + amp * (.55f * Mathf.Sin(th * 5f + R) + .3f * Mathf.Sin(th * 13f + 1.7f) + .15f * Mathf.Sin(th * 31f));
                top *= S((a - a0) / 25f) * S((a1 - a) / 25f) * .7f + .3f;   // 양 끝은 낮게(바다 쪽으로 사그라짐)
                v.Add(new Vector3(x, -30f, z)); v.Add(new Vector3(x, top, z)); uv.Add(new Vector2(a, 0)); uv.Add(new Vector2(a, 1));
            }
            for (int k = 0; k + 3 < v.Count; k += 2) { tri.Add(k); tri.Add(k + 1); tri.Add(k + 2); tri.Add(k + 1); tri.Add(k + 3); tri.Add(k + 2); tri.Add(k); tri.Add(k + 2); tri.Add(k + 1); tri.Add(k + 1); tri.Add(k + 2); tri.Add(k + 3); }
            var go = MeshGo(parent, "FarRidge_" + key, v, uv, new[] { tri }, new[] { FogLikeGround(M(key)) }, false);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh; var nrm = new Vector3[v.Count];
            for (int k = 0; k < v.Count; k++) { var d = new Vector3(c.x - v[k].x, 0, c.y - v[k].z).normalized; nrm[k] = (d + Vector3.up * .6f).normalized; }   // 지도 쪽을 보는 면
            mesh.normals = nrm;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            n++;
        }
        return $"먼 능선 {n}겹";
    }
}
