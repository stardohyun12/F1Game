using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 2124(D-150 TONE.md 2번 장면): Map_BayLoop 본선 서쪽 해안 내리막(main 중심선 1030~1130) = 「배달 끝내고 차고로 돌아오는 길」 기준 구간.
// 맵 전체 재빌드 없이 이 구간에만 루트 하나(BayLoop/ToneSlice_ReturnRoad)를 다시 만든다. 다시 돌리면 지우고 새로.
// 모양은 전부 임시(박스·삼각형) — 실제 아트는 나중에 Codex. 재질은 LofiSurface(흰 텍스처 × _Tint)라 Look 5 규칙을 그대로 탄다.
// 넣는 것(TONE 3~7): 바람 풀(ToneSway) · 비어 있는 쉼 자리(벤치+포장) · 멀리 사람 3(빨래 너는 사람·손 흔드는 사람·자전거) · 빨래 줄 · 창 불빛(ToneWindowGlow) · 소리 슬롯.
public static class BuildToneSlice
{
    const int I0 = 1030, I1 = 1130;
    const string Dir = "Assets/Art/ToneSlice";

    [MenuItem("PitCrew/Map/BayLoop 톤 구간 — 돌아오는 길 (D-150)")]
    public static void Menu() => Debug.Log(Build());

    static Material Mat(string name, Color c)
    {
        var path = $"{Dir}/TS_{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Art", "ToneSlice");
            m = new Material(Shader.Find("PitCrew/LofiPilot/Surface"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetTexture("_MainTex", Texture2D.whiteTexture);
        m.SetColor("_Tint", c);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Mesh cube;
    static GameObject Box(Transform p, string n, Vector3 localPos, Vector3 size, Material m, Quaternion? rot = null)
    {
        var g = new GameObject(n);
        g.transform.SetParent(p, false);
        g.transform.localPosition = localPos; g.transform.localRotation = rot ?? Quaternion.identity; g.transform.localScale = size;
        g.AddComponent<MeshFilter>().sharedMesh = cube;
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    static float Gnd(Vector3 v, Transform skip)
    {
        foreach (var h in Physics.RaycastAll(new Vector3(v.x, 300f, v.z), Vector3.down, 600f).OrderBy(h => h.distance))
            if (skip == null || !h.transform.IsChildOf(skip)) return h.point.y;
        return 0f;
    }

    // 풀 한 포기: 밑동이 피벗, 넓은 잎 5장(양면). 잎이 가늘면 저해상도에서 깜빡이므로(D-149) 밑변 0.22m 로 굵게.
    static Mesh Clump(int seed)
    {
        var path = $"{Dir}/TS_Clump{seed}.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        var r = new System.Random(seed);
        var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
        for (int i = 0; i < 5; i++)
        {
            float a = (float)(i / 5.0 * Mathf.PI * 2 + r.NextDouble()), hgt = .55f + (float)r.NextDouble() * .4f, lean = .12f + (float)r.NextDouble() * .2f;
            var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var s = Vector3.Cross(Vector3.up, d) * .11f;
            int b = v.Count;
            v.Add(-s + d * .05f); v.Add(s + d * .05f); v.Add(d * (lean + .05f) + Vector3.up * hgt);
            t.AddRange(new[] { b, b + 2, b + 1, b, b + 1, b + 2 });
        }
        mesh = new Mesh { name = "TS_Clump" + seed };
        mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.SetUVs(0, v.Select(x => new Vector2(.5f, .5f)).ToList());
        mesh.normals = v.Select(_ => Vector3.up).ToArray();   // 위를 향한 법선 → 잎 양면 밝기 같음(뒷면 까맣게 안 됨)
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    // PS1 느낌 임시 사람(1.7m): 다리·몸·머리 박스. arm=true 면 오른팔을 어깨 피벗 자식으로(ToneSway 로 흔듦).
    static Transform Person(Transform p, string n, Vector3 pos, Vector3 face, Material coat, Material legs, Material skin, Transform armParent = null)
    {
        var g = new GameObject(n).transform; g.SetParent(p, false);
        g.position = pos; face.y = 0; g.rotation = Quaternion.LookRotation(face.sqrMagnitude > .01f ? face : Vector3.forward);
        Box(g, "Legs", new Vector3(0, .42f, 0), new Vector3(.34f, .84f, .24f), legs);
        Box(g, "Body", new Vector3(0, 1.12f, 0), new Vector3(.46f, .6f, .3f), coat);
        Box(g, "Head", new Vector3(0, 1.56f, 0), new Vector3(.24f, .26f, .24f), skin);
        Box(g, "ArmL", new Vector3(-.3f, 1.1f, 0), new Vector3(.12f, .56f, .14f), coat);
        g.localScale = Vector3.one * 1.15f;   // 2144: 1.7→1.95m·굵게 — 30~60m 에서 1× 실루엣이 읽히게
        if (armParent != null)
        {
            var sh = new GameObject(n + "_Shoulder").transform; sh.SetParent(armParent, false);
            sh.position = g.TransformPoint(new Vector3(.3f, 1.36f, 0)); sh.localScale = Vector3.one * 1.15f; sh.rotation = g.rotation * Quaternion.Euler(0, 0, 150f);   // 팔을 머리 위로
            Box(sh, "ArmR", new Vector3(0, -.28f, 0), new Vector3(.12f, .56f, .14f), coat);
        }
        else Box(g, "ArmR", new Vector3(.3f, 1.1f, 0), new Vector3(.12f, .56f, .14f), coat);
        return g;
    }

    public static string Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Map_BayLoop") return "Map_BayLoop 을 열고 돌릴 것 (지금: " + scene.name + ")";
        var bay = GameObject.Find("BayLoop"); if (bay == null) return "BayLoop 루트 없음";
        var old = bay.transform.Find("ToneSlice_ReturnRoad"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("ToneSlice_ReturnRoad").transform; root.SetParent(bay.transform, false);
        cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var main = Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).Where(r => r.roadId == "main" && r.centerline != null).OrderByDescending(r => r.centerline.Length).First();
        var c = main.centerline; float hw = main.width * .5f;
        System.Func<int, Vector3> Fwd = i => { var f = c[Mathf.Min(i + 4, c.Length - 1)] - c[Mathf.Max(i - 4, 0)]; f.y = 0; return f.normalized; };
        System.Func<int, Vector3> Right = i => Vector3.Cross(Vector3.up, Fwd(i));
        // 바다 쪽 = 18m 옆 지면이 더 낮은 쪽
        int mid = (I0 + I1) / 2; var mp = c[mid];
        float sign = Gnd(mp + Right(mid) * 18f, root) < Gnd(mp - Right(mid) * 18f, root) ? 1f : -1f;
        System.Func<int, Vector3> Sea = i => Right(i) * sign;
        var log = new System.Text.StringBuilder($"seaSide={(sign > 0 ? "right" : "left")} ");

        // 색: 바랜 가을색 바탕, 진한 색은 사람이 만든 작은 것에만(TONE 5)
        // 풀: 빛 배수(오후 1.4)를 받으면 흰색으로 뜨므로 한 단 낮춤(1차 캡처에서 하얀 가시처럼 보였음)
        var straw = Mat("Straw", new Color(.62f, .54f, .36f)); var olive = Mat("Olive", new Color(.44f, .46f, .29f)); var rust = Mat("Rust", new Color(.58f, .40f, .26f));
        var wood = Mat("Wood", new Color(.55f, .40f, .28f)); var stone = Mat("Stone", new Color(.82f, .78f, .70f)); var iron = Mat("Iron", new Color(.25f, .27f, .29f));
        var coatA = Mat("CoatNavy", new Color(.24f, .30f, .45f)); var coatB = Mat("CoatMustard", new Color(.78f, .60f, .22f)); var coatC = Mat("CoatRed", new Color(.72f, .26f, .20f));
        // 2144(c): 배경 대비용 — 밝은 밭 앞은 짙은 색, 바다·하늘 앞은 흰 색(명도 차로 실루엣)
        var coatDark = Mat("CoatDeepNavy", new Color(.10f, .13f, .24f)); var coatWine = Mat("CoatWine", new Color(.42f, .10f, .09f)); var coatCream = Mat("CoatCream", new Color(.97f, .95f, .88f));
        var trousers = Mat("Trousers", new Color(.30f, .28f, .26f)); var trDark = Mat("TrousersDark", new Color(.12f, .11f, .10f)); var skin = Mat("Skin", new Color(.86f, .68f, .55f));
        var clothW = Mat("ClothWhite", new Color(.95f, .94f, .90f)); var clothR = Mat("ClothRed", new Color(.80f, .24f, .22f)); var clothB = Mat("ClothBlue", new Color(.30f, .48f, .74f)); var clothY = Mat("ClothYellow", new Color(.92f, .76f, .30f));
        var bikeRed = Mat("BikeRed", new Color(.75f, .22f, .18f)); var glass = Mat("WindowGlass", new Color(.30f, .34f, .40f));

        // ── 1) 바람 풀: 길 양옆 4~14m, 포기마다 피벗 = 밑동. 바다 쪽은 성글게(트인 시야).
        var grass = new GameObject("WindGrass").transform; grass.SetParent(root, false);
        var clumps = new[] { Clump(1), Clump(2), Clump(3) };
        var rnd = new System.Random(2124); int nGrass = 0;
        for (int i = I0; i <= I1; i += 2)
            for (int side = -1; side <= 1; side += 2)
            {
                bool seaSide = side * sign > 0; int n = seaSide ? 1 : 3;
                for (int k = 0; k < n; k++)
                {
                    if (rnd.NextDouble() < .25) continue;
                    var p = c[i] + Right(i) * side * (hw + 4f + (float)rnd.NextDouble() * 10f) + Fwd(i) * (float)(rnd.NextDouble() * 2 - 1);
                    p.y = Gnd(p, root) - .05f;
                    if (Mathf.Abs(p.y - c[i].y) > 6f) continue;   // 절벽 아래·지붕 위 제외
                    var g = new GameObject("Clump"); g.transform.SetParent(grass, false);
                    g.transform.position = p; g.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
                    float s = .8f + (float)rnd.NextDouble() * .7f; g.transform.localScale = new Vector3(s, s * (.8f + (float)rnd.NextDouble() * .5f), s);
                    g.AddComponent<MeshFilter>().sharedMesh = clumps[rnd.Next(3)];
                    double pick = rnd.NextDouble();
                    g.AddComponent<MeshRenderer>().sharedMaterial = pick < .55 ? straw : pick < .9 ? olive : rust;
                    nGrass++;
                }
            }
        var gs = grass.gameObject.AddComponent<ToneSway>(); gs.amplitude = 7f; gs.frequency = .45f; gs.waveLength = 18f; gs.axis = Vector3.forward;
        log.Append($"grass={nGrass} ");

        // ── 2) 비어 있는 쉼 자리: 1075 바다 쪽 어깨에 포장 판 + 벤치(바다를 봄) + 낮은 돌 경계. 아무도 없음.
        {
            int i = 1075; var f = Fwd(i); var s = Sea(i);
            var p = c[i] + s * (hw + 3.2f); p.y = Gnd(p, root);
            var rest = new GameObject("RestSpot_Empty").transform; rest.SetParent(root, false);
            rest.position = p; rest.rotation = Quaternion.LookRotation(s);
            Box(rest, "Pad", new Vector3(0, .04f, .4f), new Vector3(4.2f, .12f, 2.6f), stone);
            Box(rest, "Seat", new Vector3(0, .46f, .5f), new Vector3(1.8f, .08f, .45f), wood);
            Box(rest, "Back", new Vector3(0, .78f, .3f), new Vector3(1.8f, .4f, .07f), wood, Quaternion.Euler(-12f, 0, 0));
            Box(rest, "LegL", new Vector3(-.75f, .22f, .5f), new Vector3(.1f, .44f, .4f), iron);
            Box(rest, "LegR", new Vector3(.75f, .22f, .5f), new Vector3(.1f, .44f, .4f), iron);
            Box(rest, "Kerb", new Vector3(0, .2f, 1.75f), new Vector3(4.2f, .4f, .3f), stone);
            // ── 3a) 자전거 한 대(주인 없음): 쉼 자리 옆에 기대 둠. 바퀴는 납작 박스(원판 대용, 굵게).
            var bike = new GameObject("Bicycle_Leaning").transform; bike.SetParent(rest, false);
            bike.localPosition = new Vector3(2.6f, 0, .9f); bike.localRotation = Quaternion.Euler(0, 90f, 8f);
            Box(bike, "WheelF", new Vector3(0, .34f, .55f), new Vector3(.08f, .66f, .66f), iron, Quaternion.Euler(45f, 0, 0));
            Box(bike, "WheelB", new Vector3(0, .34f, -.55f), new Vector3(.08f, .66f, .66f), iron, Quaternion.Euler(45f, 0, 0));
            Box(bike, "Frame", new Vector3(0, .62f, 0), new Vector3(.08f, .1f, 1.0f), bikeRed);
            Box(bike, "Post", new Vector3(0, .78f, -.25f), new Vector3(.08f, .4f, .08f), bikeRed);
            Box(bike, "Bar", new Vector3(0, .95f, .45f), new Vector3(.5f, .07f, .07f), iron);
            Box(bike, "Basket", new Vector3(0, .9f, .62f), new Vector3(.32f, .22f, .26f), wood);
            log.Append($"rest@{p:F0} ");
        }

        // ── 4) 멀리 사람 3 — 모두 길에서 25m 넘게(가까이 마주침 없음, TONE 6)
        var swayArms = new GameObject("WavingArms").transform; swayArms.SetParent(root, false);
        var laundrySway = new GameObject("LaundryCloths").transform; laundrySway.SetParent(root, false);
        {
            // 4a) 빨래 너는 사람 + 빨래 줄: 육지 쪽 농가 근처(1060 에서 왼쪽 앞 50~60m)
            // 1차: 농가 옆에 뒀더니 1060 시점 차 뒤쪽이라 안 보였음 → 진행 방향 앞, 육지 쪽 밭 위(1080, 22m)로. 1060 에서 약 45m 앞 왼쪽.
            Vector3 lp = c[1095] - Sea(1095) * 12f;   // 2144: 1060 시점 35m 앞·12m 옆(1×에서 보이게)
            lp.y = Gnd(lp, root);
            var lf = Fwd(1070);
            var line = new GameObject("LaundryLine").transform; line.SetParent(root, false); line.position = lp; line.rotation = Quaternion.LookRotation(lf);
            Box(line, "PoleA", new Vector3(0, 1.0f, -2.4f), new Vector3(.1f, 2.0f, .1f), wood);
            Box(line, "PoleB", new Vector3(0, 1.0f, 2.4f), new Vector3(.1f, 2.0f, .1f), wood);
            Box(line, "Rope", new Vector3(0, 1.92f, 0), new Vector3(.06f, .06f, 4.8f), stone);   // 6cm: 더 가늘면 저해상도에서 끊겨 깜빡임
            var cloths = new[] { clothW, clothR, clothB, clothW, clothY };
            for (int k = 0; k < 5; k++)
            {
                var piv = new GameObject("Cloth" + k).transform; piv.SetParent(laundrySway, false);
                piv.position = line.TransformPoint(new Vector3(0, 1.9f, -1.8f + k * .9f)); piv.rotation = line.rotation;
                float h = k == 2 ? .9f : .65f;
                Box(piv, "Sheet", new Vector3(0, -h * .5f, 0), new Vector3(.04f, h, .7f), cloths[k]);
            }
            var ls = laundrySway.gameObject.AddComponent<ToneSway>(); ls.amplitude = 10f; ls.frequency = .5f; ls.waveLength = 6f; ls.axis = Vector3.forward;
            Person(root, "Person_HangingLaundry", line.TransformPoint(new Vector3(.7f, 0, -2.9f)) + Vector3.up * 0f, -Sea(1070), coatWine, trDark, skin);
            log.Append($"laundry@{lp:F0} d={(lp - c[1060]).magnitude:F0}m ");

            // 4b) 손 흔드는 사람: 바다 쪽 아래 둑길(1100, 바다 쪽 30m)에서 길을 올려다봄
            // 물에 빠지지 않게: 30m 부터 안쪽으로 줄이며 해면(0.5m) 위 땅을 찾음
            var wp = c[1120]; for (float d = 10f; d >= 6f; d -= 1f) { wp = c[1120] + Sea(1120) * d; wp.y = Gnd(wp, root); if (wp.y > .5f) break; }
            Person(root, "Person_Waving", wp, c[1100] - wp, coatCream, trDark, skin, swayArms);   // 2144: 바다 앞 = 흰 옷
            var ws = swayArms.gameObject.AddComponent<ToneSway>(); ws.amplitude = 28f; ws.frequency = .9f; ws.waveLength = 1f; ws.axis = Vector3.forward;
            log.Append($"waver@{wp:F0} ");

            // 4c) 자전거 타는 사람(정지 자세, 이동은 다음 과제): 내리막 끝 앞쪽 1125 육지 쪽 30m — 1060 에서 보면 멀리 앞
            var cp = c[1112] - Sea(1112) * 8f; cp.y = Gnd(cp, root);
            var rider = Person(root, "Person_Cyclist", cp + Vector3.up * .35f, Fwd(1125), coatDark, trDark, skin);
            var rb = new GameObject("Bike").transform; rb.SetParent(rider, false);
            Box(rb, "WheelF", new Vector3(0, -.01f, .55f), new Vector3(.08f, .66f, .66f), iron, Quaternion.Euler(45f, 0, 0));
            Box(rb, "WheelB", new Vector3(0, -.01f, -.55f), new Vector3(.08f, .66f, .66f), iron, Quaternion.Euler(45f, 0, 0));
            Box(rb, "Frame", new Vector3(0, .3f, 0), new Vector3(.08f, .1f, 1.0f), bikeRed);
            log.Append($"cyclist@{cp:F0} ");
        }

        // ── 5) 창 불빛: 구간에서 보이는 집 3채, 길 쪽 벽 가운데 높이에 창 2장. 오후엔 어두운 유리, 노을·초저녁엔 켜짐.
        int nWin = 0;
        foreach (var name in new[] { "BLD_P209_farmhouse", "BLD_P187_town_house", "BLD_P166_terrace_house" })
        {
            var b = GameObject.Find(name); if (b == null) continue;
            var col = b.GetComponent<BoxCollider>(); if (col == null) continue;
            var toRoad = c[1065] - b.transform.position; toRoad.y = 0;
            // 길 쪽 면 고르기(로컬 ±X/±Z 중 길 방향과 가장 가까운 것)
            Vector3[] ax = { b.transform.right, -b.transform.right, b.transform.forward, -b.transform.forward };
            int best = 0; for (int a = 1; a < 4; a++) if (Vector3.Dot(ax[a], toRoad) > Vector3.Dot(ax[best], toRoad)) best = a;
            var ls = Vector3.Scale(col.size, b.transform.lossyScale);
            float half = best < 2 ? ls.x * .5f : ls.z * .5f, width = best < 2 ? ls.z : ls.x;
            var center = b.transform.TransformPoint(col.center);
            var side = Vector3.Cross(Vector3.up, ax[best]);
            for (int w = 0; w < 2; w++)
            {
                var wp = center + ax[best] * (half + .06f) + side * (w == 0 ? -.22f : .22f) * width + Vector3.up * (ls.y * .05f);
                var g = Box(root, "WindowGlow_" + name + "_" + w, Vector3.zero, new Vector3(.9f, 1.1f, .05f), glass);
                g.transform.position = wp; g.transform.rotation = Quaternion.LookRotation(ax[best]);
                g.AddComponent<ToneWindowGlow>(); nWin++;
            }
        }
        log.Append($"windows={nWin} ");

        // ── 6) 소리 슬롯(클립 없음, 나중에 채움): 귀환 구간 전용. 매핑 표는 보고서.
        foreach (var (n, off, dist) in new[] { ("Audio_Return_Wind", 0f, 40f), ("Audio_Return_Waves", 22f, 60f), ("Audio_Return_FarScooter", -45f, 90f), ("Audio_Return_VoiceFragments", -30f, 35f) })
        {
            var g = new GameObject(n); g.transform.SetParent(root, false);
            var p = c[mid] + Sea(mid) * off; p.y = Gnd(p, root) + 1.5f; g.transform.position = p;
            var a = g.AddComponent<AudioSource>(); a.playOnAwake = false; a.loop = true; a.spatialBlend = 1f; a.maxDistance = dist; a.rolloffMode = AudioRolloffMode.Linear; a.volume = .6f;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        return "ToneSlice built: " + log;
    }
}
