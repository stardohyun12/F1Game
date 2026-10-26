using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 2306 엽서 장소 01 — 비탈 스위치백 마을 블록아웃 (docs/design/PLACES.md §01, D-151·D-152).
// 자리: D_slope 안, 본선이 감싸 도는 동쪽 안쪽(x -33~51, z 172~222). 본선 x=-37 줄기(y 26)에서 갈라져 동쪽으로 지그재그로 오르는 막다른 차 길.
//  다리 5개(남북 방향, 28m) + 헤어핀 4 + 꼭대기 굽이(동쪽으로 꺾임) → 꼭대기 길(y≈48) 끝에 배달 집. 고도차 약 22m.
// 길은 지형 위에 뜬 옹벽 데크(포지타노처럼 층층) — 지형은 안 건드린다. 이 상자 안의 기존 생성 건물·소품·A_terrace 길은 지우고 손 배치.
// 다시 돌리면 지우고 새로(재빌드 BuildScene 끝에서도 호출). 모양은 전부 임시 박스, 재질 LofiSurface(Look 5 규칙 그대로).
public static partial class BuildBayLoop
{
    const string P01Dir = "Assets/Art/Place01";
    // 지울 상자: 본선(x=-37 줄기 동쪽 가장자리 -33.5)·본선 북쪽 줄기·남쪽 줄기를 침범하지 않는 안쪽
    static readonly Rect P01Box = new Rect(-33f, 172.5f, 85f, 47f);   // x -33..52, z 172.5..219.5
    const float P01W = 4f, P01Gr = .115f, P01GrPin = .05f;

    [MenuItem("PitCrew/Map/BayLoop 장소 01 — 비탈 스위치백 마을 (블록아웃)")]
    public static void Place01Menu() => Debug.Log(Place01Standalone());

    /// 열린 Map_BayLoop 에서 장소 01 만 다시 만든다(맵 재빌드 없이). 씬 저장까지.
    public static string Place01Standalone()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Map_BayLoop") return "Map_BayLoop 을 열고 돌릴 것 (지금: " + scene.name + ")";
        var bay = GameObject.Find("BayLoop"); if (bay == null) return "BayLoop 루트 없음";
        root = bay.transform;
        var old = root.Find("Place01"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var r = Place01(Group("Place01"));
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return r;
    }

    static bool InP01(Vector3 p) => P01Box.Contains(new Vector2(p.x, p.z));

    static Material P01Mat(string name, Color c)
    {
        var path = $"{P01Dir}/P01_{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            if (!AssetDatabase.IsValidFolder(P01Dir)) AssetDatabase.CreateFolder("Assets/Art", "Place01");
            m = new Material(Shader.Find("PitCrew/LofiPilot/Surface"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetTexture("_MainTex", Texture2D.whiteTexture); m.SetColor("_Tint", c); EditorUtility.SetDirty(m);
        return m;
    }

    static Mesh p01Cube;
    static GameObject P01Box3(Transform p, string n, Vector3 center, Vector3 size, Material m, float yaw = 0f, bool col = true)
    {
        var g = new GameObject(n); g.transform.SetParent(p, false);
        g.transform.position = center; g.transform.rotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = size;
        g.AddComponent<MeshFilter>().sharedMesh = p01Cube; g.AddComponent<MeshRenderer>().sharedMaterial = m;
        if (col) g.AddComponent<BoxCollider>();
        return g;
    }

    // 지형 높이(BayTerrain 만 — 지운 뒤라 건물에 안 걸림, 장소 01 자신도 무시)
    static float P01Gnd(float x, float z, Transform self)
    {
        foreach (var h in Physics.RaycastAll(new Vector3(x, 300f, z), Vector3.down, 600f).OrderBy(h => h.distance))
            if (!h.transform.IsChildOf(self) && (h.collider.name.Contains("Terrain") || h.collider.name.StartsWith("OG_"))) return h.point.y;
        return H(x, z);
    }

    static string Place01(Transform g)
    {
        p01Cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var log = new System.Text.StringBuilder("[장소01] ");

        // ── 0) 이 상자 안 기존 생성물 걷어내기(건물·앞 소품·계단 화분·가구·전봇대·A_terrace) ──
        int removed = 0;
        foreach (var grp in new[] { "Buildings", "FrontProps", "StairPlanters", "StreetFurniture", "Poles", "Features", "LivedIn", "Trees" })
        {
            var t = root.Find(grp); if (t == null) continue;
            foreach (var c in t.Cast<Transform>().ToList())
            {
                var rr = c.GetComponentInChildren<Renderer>(); var p = rr != null ? rr.bounds.center : c.position;
                if (InP01(p)) { Object.DestroyImmediate(c.gameObject); removed++; }
            }
        }
        foreach (var rs in Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
            if (rs.roadId == "A_terrace") { Object.DestroyImmediate(rs.gameObject); removed++; }
        Physics.SyncTransforms();
        log.Append($"걷어냄 {removed} · ");

        // ── 재질(임시 블록아웃 색: 회반죽 크림·연분홍·연황토·회청, 지붕 테라코타, 길 짙은 회색, 옹벽 돌회색) ──
        var road = P01Mat("Road", new Color(.42f, .41f, .40f));
        var wallStone = P01Mat("RetainStone", new Color(.62f, .60f, .55f));
        var parapet = P01Mat("Parapet", new Color(.80f, .78f, .72f));
        var walls = new[] { P01Mat("WallCream", new Color(.90f, .86f, .76f)), P01Mat("WallPink", new Color(.90f, .76f, .70f)), P01Mat("WallOchre", new Color(.88f, .76f, .56f)), P01Mat("WallBlueGrey", new Color(.70f, .76f, .80f)), P01Mat("WallWhite", new Color(.94f, .93f, .90f)) };
        var roof = P01Mat("RoofTerracotta", new Color(.70f, .42f, .32f));
        var dark = P01Mat("WindowDark", new Color(.20f, .22f, .26f));
        var door = P01Mat("DoorWood", new Color(.46f, .32f, .22f));
        var step = P01Mat("StairStone", new Color(.74f, .71f, .66f));
        var post = P01Mat("Bollard", new Color(.35f, .35f, .36f));
        var clothW = P01Mat("ClothWhite", new Color(.95f, .95f, .93f)); var clothB = P01Mat("ClothBlue", new Color(.50f, .64f, .84f)); var clothR = P01Mat("ClothRose", new Color(.88f, .58f, .58f));
        var rope = P01Mat("Rope", new Color(.30f, .30f, .30f));
        var delivery = P01Mat("DeliveryHouse", new Color(.96f, .80f, .46f));
        var bikeM = P01Mat("Bike", new Color(.20f, .40f, .62f));
        var wood = P01Mat("Wood", new Color(.52f, .38f, .26f));

        // ── 1) 차 길 중심선(1m 간격): 본선 → S자 진입 → 다리 5 + 헤어핀 4 → 꼭대기 굽이(동쪽) → 꼭대기 길 ──
        var pts = new List<Vector2>(); var grade = new List<float>();
        void Add(Vector2 p, float gr) { if (pts.Count > 0 && (p - pts[pts.Count - 1]).magnitude < .5f) return; pts.Add(p); grade.Add(gr); }
        void Line(Vector2 a, Vector2 b, float gr) { int n = Mathf.CeilToInt((b - a).magnitude); for (int i = 0; i <= n; i++) Add(Vector2.Lerp(a, b, i / (float)n), gr); }
        void Arc(Vector2 c, float r, float a0, float a1, float gr) { int n = Mathf.CeilToInt(Mathf.Abs(a1 - a0) * Mathf.Deg2Rad * r); for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad; Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, gr); } }
        // S자 진입(본선 중심 x=-37, z 166 → 다리0 x=-27, z 184). 3차 베지어.
        { Vector2 b0 = new(-37, 166), b1 = new(-37, 176), b2 = new(-27, 174), b3 = new(-27, 184); for (int i = 0; i <= 22; i++) { float t = i / 22f, u = 1 - t; Add(u * u * u * b0 + 3 * u * u * t * b1 + 3 * u * t * t * b2 + t * t * t * b3, i < 6 ? .02f : P01Gr); } }
        float[] legX = { -27, -14, -1, 12, 25 }; const float zS = 184, zN = 212, pinR = 6.5f;
        for (int k = 0; k < 5; k++)
        {
            bool north = k % 2 == 0; float x = legX[k];
            if (k < 4) Line(new Vector2(x, north ? zS : zN), new Vector2(x, north ? zN : zS), P01Gr);
            if (k < 4)
            {
                float cx = (x + legX[k + 1]) * .5f;
                if (north) Arc(new Vector2(cx, zN), pinR, 180, 0, P01GrPin);   // 북쪽 헤어핀: 서→동, 위로 볼록
                else Arc(new Vector2(cx, zS), pinR, 180, 360, P01GrPin);       // 남쪽 헤어핀: 아래로 볼록
            }
        }
        // 다리4: 북쪽으로 오르다 꼭대기 굽이(오른쪽 90°, 반지름 6) → 꼭대기 길 동쪽(z 208)
        Line(new Vector2(25, zS), new Vector2(25, 202), P01Gr);
        Arc(new Vector2(31, 202), 6f, 180, 90, P01GrPin);
        Line(new Vector2(31, 208), new Vector2(44, 208), 0f);
        // 높이 적분(시작 = 본선 높이)
        var main = Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
        // 본선 띠 안(중심에서 3.7m 이내)의 점은 본선 높이 그대로 — 본선에 턱을 만들지 않는다. 메시는 띠 밖부터.
        var c3 = new Vector3[pts.Count]; float y = 0f, maxGr = 0f; int entryN = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var mq = main.centerline.OrderBy(q => (new Vector2(q.x, q.z) - pts[i]).sqrMagnitude).First();
            if ((new Vector2(mq.x, mq.z) - pts[i]).magnitude < main.width * .5f + .2f) { y = mq.y; entryN = i + 1; }
            else { float d = (pts[i] - pts[i - 1]).magnitude; y += d * grade[i]; maxGr = Mathf.Max(maxGr, grade[i]); }
            c3[i] = new Vector3(pts[i].x, y, pts[i].y);
        }
        System.Func<int, Vector3> Fw = i => { var f = c3[Mathf.Min(i + 1, c3.Length - 1)] - c3[Mathf.Max(i - 1, 0)]; f.y = 0; return f.normalized; };
        System.Func<float, float, int> Near = (x, z) => { int bi = 0; float bd = 1e9f; for (int i = 0; i < c3.Length; i++) { float d = (c3[i].x - x) * (c3[i].x - x) + (c3[i].z - z) * (c3[i].z - z); if (d < bd) { bd = d; bi = i; } } return bi; };
        System.Func<int, float, float> DeckY = (k, z) => c3[Near(legX[k], z)].y;   // 다리 k 의 z 지점 데크 높이

        // ── 1b) 상자 밖이라도 차 길(반폭+1.5m)에 걸치는 기존 생성물은 걷어냄 — 진입부 P089 가 길을 막았던 것(주행 1차 6m 에서 멈춤) ──
        foreach (var grp in new[] { "Buildings", "FrontProps", "StairPlanters", "StreetFurniture", "Poles", "Features", "LivedIn", "Trees" })
        {
            var t = root.Find(grp); if (t == null) continue;
            foreach (var c in t.Cast<Transform>().ToList())
            {
                var rrs = c.GetComponentsInChildren<Renderer>(); if (rrs.Length == 0) continue;
                var b = rrs[0].bounds; foreach (var r2 in rrs) b.Encapsulate(r2.bounds);
                bool hit = false;
                foreach (var q in pts) { var d = b.SqrDistance(new Vector3(q.x, b.center.y, q.y)); if (d < (P01W * .5f + 1.5f) * (P01W * .5f + 1.5f)) { hit = true; break; } }
                if (hit) { Object.DestroyImmediate(c.gameObject); removed++; }
            }
        }
        Physics.SyncTransforms();

        // ── 2) 길 메시: 윗면 + 양옆 옹벽(지형 -1m 까지) — 한 메시, MeshCollider ──
        var gRoad = new GameObject("Road").transform; gRoad.SetParent(g, false);
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>(); float worstBelow = -99f;
            for (int i = 0; i < c3.Length; i++)
            {
                var rgt = Vector3.Cross(Vector3.up, Fw(i)) * (P01W * .5f);
                var L = c3[i] - rgt; var R = c3[i] + rgt;
                float gl = P01Gnd(L.x, L.z, g), gr = P01Gnd(R.x, R.z, g);
                if (i > entryN + 2) worstBelow = Mathf.Max(worstBelow, Mathf.Max(gl, gr) - c3[i].y);   // 지형이 데크 위로 솟은 정도(진입부 제외)
                v.Add(L); v.Add(R); v.Add(new Vector3(L.x, Mathf.Min(gl, c3[i].y) - 1f, L.z)); v.Add(new Vector3(R.x, Mathf.Min(gr, c3[i].y) - 1f, R.z));
                uv.Add(Vector2.one * .5f); uv.Add(Vector2.one * .5f); uv.Add(Vector2.one * .5f); uv.Add(Vector2.one * .5f);
                if (i <= entryN) continue;   // 본선 띠 안은 본선 메시가 맡음
                int a = (i - 1) * 4, b = i * 4;
                t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });             // 윗면
                t.AddRange(new[] { a, a + 2, b, b, a + 2, b + 2 });             // 왼 옹벽(바깥 향함)
                t.AddRange(new[] { a + 1, b + 1, a + 3, a + 3, b + 1, b + 3 }); // 오른 옹벽
            }
            var mesh = new Mesh { name = "P01_Road", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.SetUVs(0, uv); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var mp = $"{P01Dir}/P01_Road.asset"; if (!AssetDatabase.IsValidFolder(P01Dir)) AssetDatabase.CreateFolder("Assets/Art", "Place01");
            AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
            var go = new GameObject("P01_RoadDeck"); go.transform.SetParent(gRoad, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { road };
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            var rsf = go.AddComponent<RoadSurface>(); rsf.roadId = "P01_switchback"; rsf.width = P01W; rsf.closed = false; rsf.centerline = c3;
            log.Append($"차길 {c3.Length}m 폭{P01W} 높이 {c3[0].y:F1}→{c3[c3.Length - 1].y:F1}(+{c3[c3.Length - 1].y - c3[0].y:F1}m) 최대경사 {maxGr:P1} 지형돌출 {worstBelow:F2}m · ");
        }

        // ── 3) 집 줄: 다리 사이 9m 를 등 맞댄 두 줄(4.2m 깊이)로 — 벽이 길 끝에 바짝 ──
        var gB = new GameObject("Houses").transform; gB.SetParent(g, false);
        var rnd = new System.Random(2306);
        float topY = c3[c3.Length - 1].y;
        // 계단 골목(z 구간, x 구간) — 집 줄을 끊는다
        var stairs = new[] { (z: 198f, x0: -33f, x1: -1f), (z: 204f, x0: -1f, x1: 25f), (z: 193f, x0: 12f, x1: 25f) };
        const float stairW = 2.4f;
        int nHouse = 0, nWin = 0;
        // 줄: (x0, x1, 기준 다리 k, 앞면 쪽 +1=동 -1=서, z 시작, z 끝)
        var rows = new List<(float x0, float x1, int k, int face, float zA, float zB)>
        {
            (-33.0f, -29.2f, 0, +1, 189.5f, 211f),
            (-24.8f, -20.6f, 0, -1, 189.5f, 211f), (-20.4f, -16.2f, 1, +1, 189.5f, 211f),
            (-11.8f, -7.6f, 1, -1, 189.5f, 211f), (-7.4f, -3.2f, 2, +1, 189.5f, 211f),
            (1.2f, 5.4f, 2, -1, 189.5f, 211f), (5.6f, 9.8f, 3, +1, 189.5f, 211f),
            (14.2f, 18.4f, 3, -1, 189.5f, 211f), (18.6f, 22.8f, 4, +1, 189.5f, 200.5f),
            (27.2f, 32.0f, 4, -1, 189.5f, 199.5f),
        };
        foreach (var row in rows)
        {
            float z = row.zA;
            while (z < row.zB - 2.5f)
            {
                float len = 4.5f + (float)rnd.NextDouble() * 3f; float z1 = Mathf.Min(z + len, row.zB);
                // 계단 골목에 걸리면 거기서 자르고 골목 건너뜀
                bool cut = false;
                foreach (var s in stairs)
                {
                    float cx = (row.x0 + row.x1) * .5f; if (cx < s.x0 || cx > s.x1) continue;
                    float sA = s.z - stairW * .5f, sB = s.z + stairW * .5f;
                    if (z < sB && z1 > sA) { if (sA - z < 2.5f) { z = sB; cut = true; break; } z1 = sA; }
                }
                if (cut) continue;
                float zm = (z + z1) * .5f, baseY = row.k == 4 && zm > 201f ? topY : DeckY(row.k, zm);
                int floors = 2 + rnd.Next(3);
                // 꼭대기 시야: 아래 줄 지붕이 꼭대기 길 높이 -1m 를 넘지 않게(서쪽 다리 0~2 줄)
                while (floors > 2 && row.k <= 2 && baseY + floors * 3f > topY - 1f) floors--;
                float hgt = floors * 3f;
                float gnd = Mathf.Min(P01Gnd(row.x0, zm, g), P01Gnd(row.x1, zm, g)) - 1f;
                float yb = Mathf.Min(gnd, baseY - .5f), cxr = (row.x0 + row.x1) * .5f, w = row.x1 - row.x0;
                var h = P01Box3(gB, $"House_{nHouse}_F{floors}", new Vector3(cxr, (yb + baseY + hgt) * .5f, zm), new Vector3(w, baseY + hgt - yb, z1 - z - .15f), walls[rnd.Next(walls.Length)]);
                P01Box3(h.transform.parent, h.name + "_Roof", new Vector3(cxr, baseY + hgt + .15f, zm), new Vector3(w + .3f, .3f, z1 - z + .1f), roof, 0, false);
                // 앞면: 문 1 + 층마다 창 1~2 (길 쪽 면)
                float fx = row.face > 0 ? row.x1 + .03f : row.x0 - .03f;
                P01Box3(gB, h.name + "_Door", new Vector3(fx, baseY + 1.05f, zm - (z1 - z) * .25f), new Vector3(.08f, 2.1f, 1.0f), door, 0, false);
                for (int f = 0; f < floors; f++)
                {
                    int nw = f == 0 ? 1 : 1 + rnd.Next(2);
                    for (int wi = 0; wi < nw; wi++)
                    {
                        float wz = f == 0 ? zm + (z1 - z) * .2f : Mathf.Lerp(z + 1f, z1 - 1f, (wi + .5f) / nw);
                        P01Box3(gB, h.name + "_Win", new Vector3(fx, baseY + f * 3f + 1.6f, wz), new Vector3(.08f, 1.1f, .8f), dark, 0, false); nWin++;
                    }
                }
                nHouse++; z = z1 + (rnd.NextDouble() < .25 ? .3f : 0f);
            }
        }
        // 꼭대기 길 남쪽 집(2층, 길 쪽 = 북) + 배달 집(동쪽 끝)
        for (float x = 33f; x < 43f; x += 5f)
        {
            float gnd = P01Gnd(x + 2.3f, 203.5f, g) - 1f;
            P01Box3(gB, $"House_{nHouse++}_F2_Top", new Vector3(x + 2.3f, (gnd + topY + 6f) * .5f, 203.6f), new Vector3(4.6f, topY + 6f - gnd, 4.2f), walls[rnd.Next(walls.Length)]);
            P01Box3(gB, "TopRoof", new Vector3(x + 2.3f, topY + 6.15f, 203.6f), new Vector3(4.9f, .3f, 4.5f), roof, 0, false);
            P01Box3(gB, "TopWin", new Vector3(x + 2.3f, topY + 4.6f, 205.73f), new Vector3(.8f, 1.1f, .08f), dark, 0, false);
        }
        {
            float gnd = P01Gnd(47f, 205.5f, g) - 1f;
            var dh = P01Box3(g, "DeliveryHouse_Grandma", new Vector3(47.2f, (gnd + topY + 6.5f) * .5f, 205.5f), new Vector3(5.4f, topY + 6.5f - gnd, 9f), delivery);
            P01Box3(g, "DeliveryHouse_Roof", new Vector3(47.2f, topY + 6.7f, 205.5f), new Vector3(5.9f, .4f, 9.5f), roof, 0, false);
            P01Box3(g, "DeliveryHouse_Door", new Vector3(44.47f, topY + 1.05f, 207.5f), new Vector3(.08f, 2.1f, 1.1f), door, 0, false);
            P01Box3(g, "DeliveryHouse_Mailbox", new Vector3(44.2f, topY + 1.1f, 209.2f), new Vector3(.3f, .4f, .4f), P01Mat("MailboxRed", new Color(.80f, .22f, .16f)), 0, false);
            P01Box3(g, "DeliveryHouse_DropZone", new Vector3(42.5f, topY + .03f, 207.5f), new Vector3(2.2f, .04f, 2.2f), P01Mat("DropMark", new Color(.98f, .86f, .40f)), 0, false);
        }

        // ── 4) 굽이 바깥 낮은 담(차 추락 방지, 0.9m) + 바다 쪽 틈 ──
        //  북쪽 헤어핀(H0·H2): 바깥이 북쪽 = 본선 너머 바다 → 담만, 집 없음. 남쪽 헤어핀(H1·H3): z 184~189.5 는 모든 줄에 집 없음 = 서쪽으로 열린 바다 띠.
        var gW = new GameObject("Parapets").transform; gW.SetParent(g, false); int nPar = 0;
        //  꼭대기 굽이·꼭대기 길 북쪽도 낮은 담만 — 180° 열림(본선 너머 바다, 서쪽 아래 마을 지붕). 진입부(본선 위)는 담 없음.
        for (int i = entryN + 2; i < c3.Length; i += 2)
        {
            if (i <= 24) continue;   // 진입 S자(베지어 23점)는 z<184 라 헤어핀으로 오판됨 → 담이 길가에 서서 차가 부딪혔다(주행 2차 6m 멈춤)
            var p = c3[i]; bool onPin = p.z > zN + .5f || p.z < zS - .5f || (p.x > 25.5f && p.z > 201f);
            bool corridor = p.z < 189.5f && p.z > zS - .5f;
            if (!onPin && !corridor) continue;
            var rgt = Vector3.Cross(Vector3.up, Fw(i));
            foreach (int sd in new[] { -1, 1 })
            {
                var q = p + rgt * sd * (P01W * .5f + .15f);
                if (p.x > 25.5f && p.z > 205f && sd > 0) continue;   // 꼭대기 길 남쪽 = 집 앞
                P01Box3(gW, "Parapet", new Vector3(q.x, p.y + .25f, q.z), new Vector3(.3f, 1.3f, 2.05f), parapet, Mathf.Atan2(Fw(i).x, Fw(i).z) * Mathf.Rad2Deg); nPar++;
            }
        }

        // ── 5) 계단 골목 3: 다리 사이 집 틈으로 동쪽으로 오름. 차 길 가장자리에 말뚝 3개씩(사람만 지나감) ──
        var gS = new GameObject("StairAlleys").transform; gS.SetParent(g, false); int nStep = 0, nBol = 0;
        foreach (var s in stairs)
        {
            // 이 골목이 지나는 다리들
            var legs = Enumerable.Range(0, 5).Where(k => legX[k] > s.x0 - .1f && legX[k] < s.x1 + .1f).ToList();
            float prevX = s.x0, prevY = s.x0 < -30f ? main.centerline.OrderBy(q => (new Vector2(q.x, q.z) - new Vector2(-37, s.z)).sqrMagnitude).First().y : DeckY(legs[0], s.z);
            if (s.x0 >= -30f) { prevX = legX[legs[0]] + P01W * .5f; legs.RemoveAt(0); }
            else prevX = -33.4f;
            foreach (int k in legs.Concat(new[] { -1 }))
            {
                float nx = k >= 0 ? legX[k] - P01W * .5f : s.x1, ny = k >= 0 ? DeckY(k, s.z) : (s.x1 > 30f ? topY : prevY + 1.5f);
                float run = nx - prevX, rise = ny - prevY; if (run < .5f) { prevX = k >= 0 ? legX[k] + P01W * .5f : nx; prevY = ny; continue; }
                int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(rise) / .17f)); float tr = run / n;
                for (int j = 0; j < n; j++)
                {
                    float top = prevY + rise * (j + 1) / n, bot = Mathf.Min(prevY, ny) - .6f;
                    P01Box3(gS, "Step", new Vector3(prevX + tr * (j + .5f), (top + bot) * .5f, s.z), new Vector3(tr + .02f, top - bot, stairW), step); nStep++;
                }
                if (k >= 0)
                {
                    // 차 길 양 가장자리 말뚝 3개(0.7m 틈 = 사람만)
                    foreach (float ex in new[] { legX[k] - P01W * .5f - .15f, legX[k] + P01W * .5f + .15f })
                        for (int b = -1; b <= 1; b++) { P01Box3(gS, "Bollard", new Vector3(ex, ny + .45f, s.z + b * .85f), new Vector3(.22f, .9f, .22f), post); nBol++; }
                    prevX = legX[k] + P01W * .5f; prevY = ny;
                }
            }
        }

        // ── 6) 생활(세월): 길 건너 빨래줄(2층 높이) · 화분(Prop "Pots") · 자전거 · 의자 ──
        var gL = new GameObject("Lived").transform; gL.SetParent(g, false); int nLine = 0, nPot = 0;
        var clothMats = new[] { clothW, clothB, clothW, clothR };
        foreach (var (k, z) in new[] { (1, 205f), (2, 191f), (3, 208f), (0, 202f) })
        {
            float yy = DeckY(k, z) + 4.6f, x0 = legX[k] - P01W * .5f - .1f, x1 = legX[k] + P01W * .5f + .1f;
            P01Box3(gL, "LaundryRope", new Vector3(legX[k], yy, z), new Vector3(x1 - x0, .05f, .05f), rope, 0, false);
            for (int c = 0; c < 4; c++) P01Box3(gL, "Cloth", new Vector3(Mathf.Lerp(x0 + .5f, x1 - .5f, c / 3f), yy - .35f, z), new Vector3(.6f, .65f, .04f), clothMats[(c + k) % 4], 0, false);
            nLine++;
        }
        foreach (var (k, z, sd) in new[] { (0, 195f, 1), (1, 201f, -1), (2, 208f, 1), (3, 196f, -1), (4, 191f, 1), (2, 194f, -1), (1, 188.8f, 1) })
        {
            var pos = new Vector3(legX[k] + sd * (P01W * .5f - .35f), DeckY(k, z), z);
            if (Prop(gL, "Pots", pos, Quaternion.LookRotation(new Vector3(-sd, 0, 0))) != null) nPot++;
            else { P01Box3(gL, "Pots_Box", pos + Vector3.up * .25f, new Vector3(.5f, .5f, .9f), P01Mat("PotTerracotta", new Color(.72f, .44f, .32f)), 0, false); nPot++; }
        }
        {   // 자전거(박스, 다리2 서쪽 벽에 기댐)
            var bp = new Vector3(legX[2] - P01W * .5f + .3f, DeckY(2, 196f), 196f);
            var bk = new GameObject("Bicycle_Leaning").transform; bk.SetParent(gL, false); bk.position = bp; bk.rotation = Quaternion.Euler(0, 0, -8f);
            P01Box3(bk, "Frame", bp + new Vector3(0, .55f, 0), new Vector3(.06f, .06f, 1.0f), bikeM, 0, false);
            P01Box3(bk, "WheelF", bp + new Vector3(0, .34f, .55f), new Vector3(.05f, .66f, .66f), dark, 0, false);
            P01Box3(bk, "WheelR", bp + new Vector3(0, .34f, -.55f), new Vector3(.05f, .66f, .66f), dark, 0, false);
        }

        // ── 7) 사람 기척 3 (v1 모델, 자리만) + 꼭대기 쉼 자리 ──
        var gP = new GameObject("People").transform; gP.SetParent(g, false); int nPpl = 0;
        GameObject Ppl(string pf, string n, Vector3 pos, Vector3 face)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/BayLoop/People/{pf}.prefab");
            GameObject go;
            if (asset != null) go = (GameObject)PrefabUtility.InstantiatePrefab(asset, gP);
            else { go = new GameObject(); go.transform.SetParent(gP, false); P01Box3(go.transform, "Body", pos + Vector3.up * .85f, new Vector3(.45f, 1.7f, .3f), clothB, 0, false); }
            go.name = n; go.transform.position = pos; face.y = 0; go.transform.rotation = Quaternion.LookRotation(face); nPpl++;
            return go;
        }
        {   // 계단 오르는 사람: 골목 1 중간(다리0~1 사이)
            var s = stairs[0]; float sx = -21f; int si = 0; float bestD = 1e9f; Vector3 sp = default;
            foreach (Transform st in gS) { if (st.name != "Step" || Mathf.Abs(st.position.z - s.z) > .1f) continue; float d = Mathf.Abs(st.position.x - sx); if (d < bestD) { bestD = d; sp = st.position + Vector3.up * st.localScale.y * .5f; si++; } }
            Ppl("people_young_01", "Person_ClimbingStairs", sp, Vector3.right);
        }
        {   // 창가에서 빨래 너는 사람: 다리1 빨래줄 옆 2층 창 앞 발코니(박스)
            float z = 205f, yy = DeckY(1, z) + 3f; float bx = legX[1] + P01W * .5f + .1f;
            P01Box3(gL, "Balcony", new Vector3(bx - .5f, yy - .1f, z + 1.4f), new Vector3(1.0f, .2f, 1.6f), parapet);
            Ppl("people_young_02", "Person_HangingLaundry", new Vector3(bx - .55f, yy, z + 1.4f), Vector3.left);
        }
        {   // 문 앞 의자 어르신: 다리3 서쪽 줄 문 앞
            float z = 201.5f; var cp = new Vector3(legX[3] - P01W * .5f + .45f, DeckY(3, z), z);
            P01Box3(gL, "Chair_Seat", cp + Vector3.up * .45f, new Vector3(.5f, .08f, .5f), wood, 0, false);
            P01Box3(gL, "Chair_Back", cp + new Vector3(-.23f, .8f, 0), new Vector3(.06f, .7f, .5f), wood, 0, false);
            Ppl("people_elder_01", "Person_ElderOnChair", cp + Vector3.up * .05f, Vector3.right);
        }
        {   // 꼭대기 쉼 자리: 북쪽 담 앞 벤치, 마을·바다를 내려다봄
            var bp = new Vector3(37f, topY, 209.2f);
            if (Prop(g, "Bench", bp, Quaternion.LookRotation(Vector3.forward)) == null)
            { P01Box3(g, "RestBench_Seat", bp + Vector3.up * .45f, new Vector3(1.8f, .1f, .5f), wood, 0, false); }
            P01Box3(g, "RestSpot_Paving", new Vector3(37f, topY + .02f, 209f), new Vector3(4f, .04f, 2f), step, 0, false);
        }

        // ── 8) 소리 슬롯(빈 오브젝트 + 범위 상자, 아트·소리는 나중에) ──
        var gA = new GameObject("SoundSlots").transform; gA.SetParent(g, false);
        void Slot(string n, Vector3 c, Vector3 size) { var s = new GameObject("SND_" + n); s.transform.SetParent(gA, false); s.transform.position = c; var bc = s.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = size; }
        Slot("WallEcho_Legs0to4", new Vector3(-1f, DeckY(2, 200f) + 2f, 200f), new Vector3(56f, 18f, 26f));
        Slot("Radio_OpenWindow", new Vector3(legX[2] + 2.2f, DeckY(2, 207f) + 4.6f, 207f), new Vector3(6f, 6f, 6f));
        Slot("LaundryFlap", new Vector3(legX[1], DeckY(1, 205f) + 4.6f, 205f), new Vector3(8f, 6f, 8f));
        Slot("WindAndWaves_Top", new Vector3(38f, topY + 2f, 208f), new Vector3(18f, 10f, 10f));

        Physics.SyncTransforms();
        log.Append($"집 {nHouse} 창 {nWin} 담 {nPar} 계단 {nStep}단 말뚝 {nBol} 빨래줄 {nLine} 화분 {nPot} 사람 {nPpl} · 꼭대기 y={topY:F1}");
        return log.ToString();
    }

    static bool IsInsidePin(Vector3 q, float[] legX, float zS, float zN, float r)
    {
        for (int k = 0; k < 4; k++)
        {
            float cx = (legX[k] + legX[k + 1]) * .5f, cz = k % 2 == 0 ? zN : zS;
            if (new Vector2(q.x - cx, q.z - cz).magnitude < r) return true;
        }
        return false;
    }
}
