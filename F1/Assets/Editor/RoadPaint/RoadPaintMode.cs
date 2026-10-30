using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

// 과제 2204(D-215 길 그리기 도구 1판): 도현이 노선을 그리고, 나머지(폭·높이·갈림목·지형·벽)는 빌더 규칙이 만든다.
//  켜면 집·차고 120 m 안 원본 노선(빌더 입력)이 색 스플라인으로 뜬다. 편집 = Splines 기본 핸들. 새 길 = Splines 기본 Draw 도구.
//  스플라인 루트는 DontSave — 씬엔 안 써지고, 저장은 docs/map/roads-hand.json(바뀐 길·새 길·지운 길만). Enter = 전체 재빌드(RoadNetV2BuildAll + 집 블록 계단 땅).
[InitializeOnLoad]
public static class RoadPaintMode
{
    const string Key = "PitCrew.RoadPaint.on", MenuPath = "PitCrew/길 그리기", TownScene = "Assets/Scenes/Map_TownV6.unity";
    const string GroundMesh = "Assets/Scenes/Map_TownV6_Mats/Mesh_TownV6_Terrain_0433.asset";   // 빌더가 높이를 재는 원지형
    static readonly Vector3 House = new Vector3(185, 0, -61), Garage = new Vector3(234, 0, -54);
    const float LoadR = 120f, SnapR = 3f, PickPx = 12f, Grade = .11f, MainR = 10f;

    class Orig { public string kind; public float w; public Vector3[] knots; }
    class Cache { public long hash; public Vector3[] pts; public bool changed; public List<(Vector3 p, string msg)> bad = new List<(Vector3, string)>(); public List<Vector3> badRun = new List<Vector3>(); }
    static GameObject root, groundGo; static MeshCollider ground;
    static readonly Dictionary<string, Orig> orig = new Dictionary<string, Orig>();
    static HashSet<string> prevDeleted = new HashSet<string>();
    static HashSet<string> noProps = new HashSet<string>();   // 2300: 소품 끈 길 id → roads-hand.json noProps
    static HashSet<int> foreign = new HashSet<int>();   // 켜기 전부터 씬에 있던 스플라인(남의 것) — 입양 안 함
    static readonly Dictionary<SplineContainer, Cache> cache = new Dictionary<SplineContainer, Cache>();
    static string lastMsg = "";
    static int newSeq; static double nextScan;

    static RoadPaintMode()
    {
        EditorApplication.delayCall += () => { if (SessionState.GetBool(Key, false) && !EditorApplication.isPlayingOrWillChangePlaymode) On(); };
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.ExitingEditMode && root != null) { SaveJson(); Off(false); }
            if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key, false)) On();
        };
        AssemblyReloadEvents.beforeAssemblyReload += () => { if (root != null) { SaveJson(); Off(false); } };
    }

    [MenuItem(MenuPath)]
    static void Toggle() { if (root != null) { SaveJson(); Off(true); } else On(); }
    [MenuItem(MenuPath, true)]
    static bool ToggleValidate() { Menu.SetChecked(MenuPath, root != null); return !EditorApplication.isPlaying; }

    // ── 켜기·끄기 ──────────────────────────────────────────
    static void On()
    {
        if (root != null) return;
        if (EditorSceneManager.GetActiveScene().path != TownScene) { Debug.LogWarning("[RoadPaint] Map_TownV6 를 먼저 연다"); SessionState.SetBool(Key, false); return; }
        foreach (var g in Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name.StartsWith("RoadPaint") && g.transform.parent == null && g.hideFlags != HideFlags.None).ToList()) Object.DestroyImmediate(g);
        SessionState.SetBool(Key, true);
        foreign = new HashSet<int>(Object.FindObjectsByType<SplineContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(c => c.GetInstanceID()));
        var terr = GameObject.Find("TownV6_Gray")?.transform.Find("TownV6_Terrain");
        groundGo = new GameObject("RoadPaint_ground") { hideFlags = HideFlags.HideAndDontSave };
        if (terr != null) { groundGo.transform.SetPositionAndRotation(terr.position + GroundOff, terr.rotation); groundGo.transform.localScale = terr.lossyScale; }   // 10 km 밖 — 씬 레이캐스트(Draw 도구·빌더)에 안 걸리게
        ground = groundGo.AddComponent<MeshCollider>(); ground.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GroundMesh);
        root = new GameObject("RoadPaint (길 그리기 — 씬에 저장 안 됨)") { hideFlags = HideFlags.DontSave };
        Load();
        SceneView.duringSceneGui += Gui; EditorApplication.update += Tick; EditorSceneManager.sceneSaving += OnSaving; Undo.undoRedoPerformed += SceneView.RepaintAll;
        lastMsg = $"길 {root.transform.childCount}개";
        SceneView.RepaintAll();
    }

    static void Off(bool user)
    {
        SceneView.duringSceneGui -= Gui; EditorApplication.update -= Tick; EditorSceneManager.sceneSaving -= OnSaving; Undo.undoRedoPerformed -= SceneView.RepaintAll;
        if (root != null && Selection.activeTransform != null && Selection.activeTransform.IsChildOf(root.transform)) Selection.activeObject = null;
        if (ToolManager.activeContextType == typeof(SplineToolContext)) ToolManager.SetActiveContext<GameObjectToolContext>();
        if (root != null) Object.DestroyImmediate(root); if (groundGo != null) Object.DestroyImmediate(groundGo);
        root = null; cache.Clear(); orig.Clear();
        if (user) SessionState.SetBool(Key, false);
        SceneView.RepaintAll();
    }

    static void OnSaving(UnityEngine.SceneManagement.Scene s, string path) { if (root != null) SaveJson(); }

    // ── 불러오기: 원본 노선 + roads-hand.json ──────────────────
    static void Load()
    {
        var hand = RoadPaintData.Load(); var hBy = new Dictionary<string, RoadPaintHand>(); if (hand != null) foreach (var h in hand.roads) hBy[h.id] = h;
        prevDeleted = new HashSet<string>(hand?.deleted ?? new List<string>()); noProps = new HashSet<string>(hand?.noProps ?? new List<string>()); orig.Clear();
        foreach (var s in BuildBayLoop.RoadPaintSources())
        {
            if (orig.ContainsKey(s.id)) continue;
            if (!hBy.ContainsKey(s.id) && !s.pts.Any(q => Flat(q - House) < LoadR || Flat(q - Garage) < LoadR)) continue;
            var ks = s.walk ? s.pts : DP(s.pts, .25f);
            orig[s.id] = new Orig { kind = s.kind, w = s.w, knots = ks.ToArray() };
            if (prevDeleted.Contains(s.id)) continue;
            if (hBy.TryGetValue(s.id, out var h)) MakeFromHand(h); else Make(s.id, s.kind, s.w, ks.Select(q => new BezierKnot((float3)q)).ToList(), null);
        }
        if (hand != null) foreach (var h in hand.roads) if (!orig.ContainsKey(h.id)) MakeFromHand(h);
    }

    static SplineContainer Make(string id, string kind, float w, List<BezierKnot> ks, List<TangentMode> modes)
    {
        var go = new GameObject(Name(kind, w, id)) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(root.transform, false);
        var c = go.AddComponent<SplineContainer>(); var sp = c.Spline; sp.Clear();
        for (int i = 0; i < ks.Count; i++) sp.Add(ks[i], modes != null ? modes[i] : TangentMode.Linear);
        return c;
    }
    static void MakeFromHand(RoadPaintHand h)
    {
        var ks = new List<BezierKnot>(); var ms = new List<TangentMode>();
        for (int i = 0; i + 12 < h.k.Length; i += 13)
        {
            ks.Add(new BezierKnot(new float3(h.k[i], h.k[i + 1], h.k[i + 2]), new float3(h.k[i + 3], h.k[i + 4], h.k[i + 5]), new float3(h.k[i + 6], h.k[i + 7], h.k[i + 8]), new quaternion(h.k[i + 9], h.k[i + 10], h.k[i + 11], h.k[i + 12])));
            ms.Add(h.m != null && ms.Count < h.m.Length ? (TangentMode)h.m[ms.Count] : TangentMode.Linear);
        }
        if (ks.Count >= 2) Make(h.id, h.kind, h.w, ks, ms);
    }

    // 이름 = "종류|폭|id" — 종류 바꾸기가 Undo 되게 GameObject 이름에 둔다
    static string Name(string kind, float w, string id) => $"{kind}|{w:0.##}|{id}";
    static bool Parse(string n, out string kind, out float w, out string id)
    {
        var a = n.Split(new[] { '|' }, 3); kind = a[0]; id = a.Length > 2 ? a[2] : ""; w = 0;
        return a.Length == 3 && float.TryParse(a[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out w);
    }
    static IEnumerable<SplineContainer> Paints() { if (root == null) yield break; foreach (Transform t in root.transform) { var c = t.GetComponent<SplineContainer>(); if (c != null) yield return c; } }

    // ── 저장: 원본과 다른 길만 ─────────────────────────────────
    static void SaveJson()
    {
        if (root == null) return;
        var f = new RoadPaintFile(); var present = new HashSet<string>();
        foreach (var c in Paints())
        {
            if (!Parse(c.name, out var kind, out var w, out var id) || c.Spline.Count < 2) continue;
            present.Add(id);
            if (!Changed(c, kind, w, id)) continue;
            var sp = c.Spline; var M = (float4x4)c.transform.localToWorldMatrix; var k = new List<float>(); var m = new List<int>();
            for (int i = 0; i < sp.Count; i++)
            {
                var b = sp[i].Transform(M);
                k.AddRange(new[] { b.Position.x, b.Position.y, b.Position.z, b.TangentIn.x, b.TangentIn.y, b.TangentIn.z, b.TangentOut.x, b.TangentOut.y, b.TangentOut.z, b.Rotation.value.x, b.Rotation.value.y, b.Rotation.value.z, b.Rotation.value.w });
                m.Add((int)sp.GetTangentMode(i));
            }
            var p = Sample(c, 1f).Select(q => { var y = GroundY(q); return new Vector3(q.x, float.IsNaN(y) ? q.y : y, q.z); }).SelectMany(q => new[] { q.x, q.y, q.z }).ToArray();
            f.roads.Add(new RoadPaintHand { id = id, kind = kind, w = w, walk = RoadPaintData.IsWalk(kind), k = k.ToArray(), m = m.ToArray(), p = p });
        }
        foreach (var id in orig.Keys.Concat(prevDeleted).Distinct()) if (!present.Contains(id)) f.deleted.Add(id);
        f.noProps = noProps.Where(present.Contains).OrderBy(x => x, System.StringComparer.Ordinal).ToList();
        RoadPaintData.Save(f);
        lastMsg = $"저장: 바뀐 길 {f.roads.Count} · 지운 길 {f.deleted.Count}";
    }

    static bool Changed(SplineContainer c, string kind, float w, string id)
    {
        if (!orig.TryGetValue(id, out var o) || o.kind != kind || Mathf.Abs(o.w - w) > .01f || o.knots.Length != c.Spline.Count) return true;
        var M = c.transform.localToWorldMatrix;
        for (int i = 0; i < o.knots.Length; i++)
        {
            var b = c.Spline[i]; if ((M.MultiplyPoint3x4(b.Position) - o.knots[i]).sqrMagnitude > .02f * .02f) return true;
            if (math.lengthsq(b.TangentIn) > 1e-4f || math.lengthsq(b.TangentOut) > 1e-4f) return true;
        }
        return false;
    }

    // ── 곡선 표본(월드, 매듭을 꼭 지난다) ──────────────────────
    static List<Vector3> Sample(SplineContainer c, float step)
    {
        var sp = c.Spline; var M = c.transform.localToWorldMatrix; var r = new List<Vector3>(); int nc = sp.Closed ? sp.Count : sp.Count - 1;
        for (int i = 0; i < nc; i++)
        {
            var cv = sp.GetCurve(i); int n = Mathf.Max(1, Mathf.CeilToInt(CurveUtility.ApproximateLength(cv) / step));
            for (int s = 0; s < n; s++) r.Add(M.MultiplyPoint3x4(CurveUtility.EvaluatePosition(cv, s / (float)n)));
        }
        if (sp.Count > 0) r.Add(M.MultiplyPoint3x4(sp.Closed ? sp[0].Position : sp[sp.Count - 1].Position));
        return r;
    }
    static readonly Vector3 GroundOff = new Vector3(10000, 0, 0);
    static float GroundY(Vector3 q) => ground != null && ground.Raycast(new Ray(new Vector3(q.x, 400, q.z) + GroundOff, Vector3.down), out var h, 800) ? h.point.y : float.NaN;
    static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

    static List<Vector3> DP(List<Vector3> p, float eps)
    {
        if (p.Count < 3) return new List<Vector3>(p);
        var keep = new bool[p.Count]; keep[0] = keep[p.Count - 1] = true; var st = new Stack<(int, int)>(); st.Push((0, p.Count - 1));
        while (st.Count > 0)
        {
            var (a, b) = st.Pop(); float dm = 0; int im = -1;
            for (int i = a + 1; i < b; i++) { float d = HandleUtility.DistancePointLine(p[i], p[a], p[b]); if (d > dm) { dm = d; im = i; } }
            if (im >= 0 && dm > eps) { keep[im] = true; st.Push((a, im)); st.Push((im, b)); }
        }
        return p.Where((q, i) => keep[i]).ToList();
    }

    static Cache Get(SplineContainer c)
    {
        long h = c.name.GetHashCode() * 31L + c.Spline.Count; var M = c.transform.localToWorldMatrix;
        for (int i = 0; i < c.Spline.Count; i++) { var b = c.Spline[i]; h = h * 1000003L + (long)(math.dot(b.Position, new float3(1.3f, 7.1f, 13.7f)) * 1000) + (long)(math.dot(b.TangentOut - b.TangentIn, new float3(3.1f, 5.3f, 11.9f)) * 1000); }
        h = h * 31 + M.GetHashCode();
        if (cache.TryGetValue(c, out var k) && k.hash == h) return k;
        k = new Cache { hash = h, pts = Sample(c, 1f).ToArray() };
        if (Parse(c.name, out var kind, out var w, out var id)) { k.changed = Changed(c, kind, w, id); if (k.changed && !RoadPaintData.IsWalk(kind)) Check(k, kind, w); }
        cache[c] = k; return k;
    }

    // ── 규칙 검사(바뀐 차길만): 반경 R<10 · 경사 11% 초과 → 빨간 점 + 빌더가 고칠 값 ──
    static void Check(Cache k, string kind, float w)
    {
        var p = k.pts; int n = p.Length; if (n < 3) return;
        float minR = kind == "main" ? MainR : Mathf.Min(MainR, w * .5f + .25f);
        int worst = -1; float wr = 1e9f;
        for (int i = 0; i <= n; i++)
        {
            float r = 1e9f;
            if (i >= 3 && i < n - 3) r = Circum(p[i - 3], p[i], p[i + 3]);
            if (r < MainR) { k.badRun.Add(p[i]); if (r < wr) { wr = r; worst = i; } }
            else if (worst >= 0) { k.bad.Add((p[worst], wr < minR ? $"R {wr:0.0} m → 너무 급함(최소 {minR:0.0}) — 점을 벌려야" : $"R {wr:0.0} m → {MainR:0} m 로 펴짐")); worst = -1; wr = 1e9f; }
        }
        var h = p.Select(q => { var y = GroundY(q); return float.IsNaN(y) ? q.y : y; }).ToArray();
        var ds = new float[n]; for (int i = 1; i < n; i++) ds[i] = Flat(p[i] - p[i - 1]);
        // 11% 로 누른 높이 = 앞→뒤 누름과 뒤→앞 누름의 평균(둘 다 11% 이하라 평균도 11% 이하) → 깎기·돋우기가 한쪽으로 안 쏠린다
        float[] Clamp(bool fwdFirst)
        {
            var a = (float[])h.Clone();
            for (int pass = 0; pass < 2; pass++)
                if ((pass == 0) == fwdFirst) for (int i = 1; i < n; i++) a[i] = Mathf.Clamp(a[i], a[i - 1] - Grade * ds[i], a[i - 1] + Grade * ds[i]);
                else for (int i = n - 2; i >= 0; i--) a[i] = Mathf.Clamp(a[i], a[i + 1] - Grade * ds[i + 1], a[i + 1] + Grade * ds[i + 1]);
            return a;
        }
        var hA = Clamp(true); var hB = Clamp(false); var hc = hA.Select((v, i) => (v + hB[i]) * .5f).ToArray();
        int s0 = -1; float gMax = 0, cut = 0, fill = 0; int gAt = 0;
        for (int i = 0; i <= n; i++)
        {
            float g = 0; if (i >= 5 && i < n) { float d = 0; for (int j = i - 4; j <= i; j++) d += ds[j]; g = d > .5f ? Mathf.Abs(h[i] - h[i - 5]) / d : 0; }
            if (g > Grade + .005f) { if (s0 < 0) { s0 = i; gMax = 0; cut = 0; fill = 0; } k.badRun.Add(p[i]); if (g > gMax) { gMax = g; gAt = i; } }
            else if (s0 >= 0)
            {
                for (int j = Mathf.Max(0, s0 - 10); j < Mathf.Min(n, i + 10); j++) { cut = Mathf.Max(cut, h[j] - hc[j]); fill = Mathf.Max(fill, hc[j] - h[j]); }
                k.bad.Add((p[gAt], $"경사 {gMax * 100:0}% → 11% (깎기 {cut:0.0} m · 돋우기 {fill:0.0} m)")); s0 = -1;
            }
        }
    }
    static float Circum(Vector3 a, Vector3 b, Vector3 c)
    {
        var A = new Vector2(a.x, a.z); var B = new Vector2(b.x, b.z); var C = new Vector2(c.x, c.z);
        float cr = Mathf.Abs((B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x)); if (cr < 1e-4f) return 1e9f;
        return (B - A).magnitude * (C - B).magnitude * (A - C).magnitude / (2f * cr);
    }

    // ── 새 길 입양: Draw 도구가 씬 루트에 만든 스플라인을 다 그린 뒤(Enter) 가져온다 ──
    static bool Drawing => ToolManager.activeToolType != null && ToolManager.activeToolType.Name == "CreateSplineTool";
    // 고른 길의 점을 바로 끌 수 있게 Spline 도구 맥락으로 — 선택이 반영된 다음 틱에 바꾼다(같은 틱이면 "편집할 대상 없음" 예외)
    static GameObject ctxFor;
    static void SelectRoad(GameObject g) { Selection.activeGameObject = g; ctxFor = g; }
    static void Tick()
    {
        if (root == null) return;
        if (EditorSceneManager.GetActiveScene().path != TownScene) { Off(true); return; }
        if (ctxFor != null && Selection.activeGameObject == ctxFor)
        {
            try { if (ToolManager.activeContextType != typeof(SplineToolContext)) ToolManager.SetActiveContext<SplineToolContext>(); ctxFor = null; } catch (System.InvalidOperationException) { }
        }
        else if (ctxFor != null && Selection.activeGameObject != ctxFor) ctxFor = null;
        if (Drawing || EditorApplication.timeSinceStartup < nextScan) return;
        nextScan = EditorApplication.timeSinceStartup + .3;
        foreach (var c in Object.FindObjectsByType<SplineContainer>(FindObjectsSortMode.None))
        {
            if (foreign.Contains(c.GetInstanceID()) || c.transform.IsChildOf(root.transform)) continue;
            if (c.Spline == null || c.Spline.Count < 2) continue;
            Adopt(c);
        }
    }
    static void Adopt(SplineContainer c)
    {
        var M = (float4x4)c.transform.localToWorldMatrix; var sp = c.Spline;
        for (int i = 0; i < sp.Count; i++) sp.SetKnot(i, sp[i].Transform(M));
        c.transform.SetParent(root.transform, false); c.transform.localPosition = Vector3.zero; c.transform.localRotation = Quaternion.identity; c.transform.localScale = Vector3.one;
        c.gameObject.hideFlags = HideFlags.DontSave;
        string id = $"hand_{System.DateTime.Now:MMddHHmmss}_{newSeq++}";
        c.gameObject.name = Name("village", 4.5f, id);
        // 끝점이 다른 길 3 m 안이면 그 중심선 위로 옮긴다 → 빌더가 갈림목(T)으로 잇는다
        int snapped = 0;
        foreach (int e in new[] { 0, sp.Count - 1 })
        {
            Vector3 q = sp[e].Position, best = q; float bd = SnapR;
            foreach (var o in Paints()) { if (o == c) continue; foreach (var s in Get(o).pts) { float d = Flat(s - q); if (d < bd) { bd = d; best = s; } } }
            if (bd < SnapR) { var kk = sp[e]; kk.Position = best; sp.SetKnot(e, kk); snapped++; }
        }
        lastMsg = $"새 길 {id} · 갈림목 {snapped}";
        SelectRoad(c.gameObject);
        SceneView.RepaintAll();
    }

    // ── 씬 뷰 ──────────────────────────────────────────────
    static readonly (string label, string kind, float w)[] Kinds = { ("큰길 6", "main", 6f), ("마을길 4.5", "village", 4.5f), ("골목 3", "village", 3f), ("보도·계단 1.5", "alley", 1.5f) };
    static Color KindColor(string kind, float w) => RoadPaintData.IsWalk(kind) ? new Color(.45f, 1f, .45f) : kind == "main" ? new Color(1f, .5f, .1f) : w < 4f ? new Color(.3f, .85f, 1f) : new Color(1f, .9f, .2f);
    static SplineContainer Selected => root != null && Selection.activeTransform != null && Selection.activeTransform.parent == root.transform ? Selection.activeTransform.GetComponent<SplineContainer>() : null;

    static void Gui(SceneView sv)
    {
        if (root == null) return;
        var e = Event.current; var sel = Selected;
        if (e.type == EventType.Repaint)
        {
            int changed = 0;
            foreach (var c in Paints())
            {
                var k = Get(c); if (k.pts.Length < 2) continue; if (k.changed) changed++;
                Parse(c.name, out var kind, out var w, out _);
                if (c == sel) { Handles.color = Color.white; Handles.DrawAAPolyLine(9f, k.pts); }
                Handles.color = KindColor(kind, w); Handles.DrawAAPolyLine(k.changed ? 6f : 4f, k.pts);
                if (k.badRun.Count > 0)
                {
                    Handles.color = new Color(1f, .15f, .1f);
                    foreach (var q in k.badRun) Handles.DrawSolidDisc(q + Vector3.up * .3f, Vector3.up, HandleUtility.GetHandleSize(q) * .04f);
                    foreach (var (q, msg) in k.bad) { Handles.DrawSolidDisc(q + Vector3.up * .3f, Vector3.up, HandleUtility.GetHandleSize(q) * .1f); Handles.Label(q + Vector3.up * 2f, msg, Red); }
                }
            }
            Hint(Drawing ? "새 길: 빈 땅 클릭 = 점 · Enter = 끝 (끝점이 길 3 m 안이면 갈림목)"
                         : $"길 그리기 — 선 클릭 = 고르기 · 점 끌기 · Ctrl+클릭 = 점 더하기 · N = 새 길 · Enter = 적용(~2분) · 바뀐 길 {changed}   {lastMsg}");
        }
        if (sel != null && !Drawing) KindBar(sv, sel);

        if (e.type == EventType.KeyDown && GUIUtility.hotControl == 0 && !Drawing)
        {
            if (e.keyCode == KeyCode.N && !e.control && !e.alt) { Selection.activeObject = null; EditorSplineUtility.SetKnotPlacementTool(); lastMsg = ""; e.Use(); }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { e.Use(); EditorApplication.delayCall += () => Apply(); }
        }
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.control && !e.shift && GUIUtility.hotControl == 0 && !Drawing)
        {
            SplineContainer best = null; float bd = PickPx;
            foreach (var c in Paints())
            {
                var pts = Get(c).pts;
                for (int i = 0; i + 1 < pts.Length; i++)
                {
                    float d = HandleUtility.DistanceToLine(pts[i], pts[i + 1]); if (d < bd) { bd = d; best = c; }
                }
            }
            if (best != null && best != sel) { SelectRoad(best.gameObject); e.Use(); }
        }
    }

    static GUIStyle red, hintSt;
    static GUIStyle Red => red ??= new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(1f, .25f, .2f) }, fontSize = 12 };
    static void Hint(string s)
    {
        Handles.BeginGUI();
        hintSt ??= new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter, fontSize = 13 };
        var sz = hintSt.CalcSize(new GUIContent(s)); var r = new Rect((SceneView.lastActiveSceneView.position.width - sz.x) / 2 - 10, 8, sz.x + 20, sz.y + 8);
        EditorGUI.DrawRect(r, new Color(0, 0, 0, .75f)); GUI.Label(r, s, hintSt);
        Handles.EndGUI();
    }

    // 고른 길의 종류 단추 4개 + 지우기 (한글 입력 없음)
    static void KindBar(SceneView sv, SplineContainer c)
    {
        Parse(c.name, out var kind, out var w, out var id);
        Handles.BeginGUI();
        float bw = 96, W = bw * 6 + 26; var r = new Rect((sv.position.width - W) / 2, sv.position.height - 78, W, 54);
        EditorGUI.DrawRect(r, new Color(0, 0, 0, .75f));
        GUI.Label(new Rect(r.x + 8, r.y + 2, W - 16, 18), $"{id}  ·  {kind} {w:0.##} m  ·  {Get(c).pts.Length - 1} m", EditorStyles.whiteLabel);
        for (int i = 0; i < Kinds.Length; i++)
        {
            var (label, k, kw) = Kinds[i]; bool on = kind == k && Mathf.Abs(w - kw) < .01f;
            GUI.color = on ? Color.yellow : Color.white;
            if (GUI.Button(new Rect(r.x + 8 + i * (bw + 2), r.y + 24, bw, 24), label) && !on) { Undo.RecordObject(c.gameObject, "길 종류"); c.gameObject.name = Name(k, kw, id); SceneView.RepaintAll(); }
        }
        GUI.color = new Color(1f, .6f, .6f);
        if (GUI.Button(new Rect(r.x + 8 + 4 * (bw + 2) + 6, r.y + 24, bw - 6, 24), "이 길 지우기")) { Selection.activeObject = null; Undo.DestroyObjectImmediate(c.gameObject); SceneView.RepaintAll(); }
        // 2300: 이 길 소품 끄기/켜기 — 저장하고 길가 소품만 바로 다시(길·지형 재빌드 없음, 몇 초)
        bool off = noProps.Contains(id); GUI.color = off ? new Color(1f, .85f, .4f) : Color.white;
        if (GUI.Button(new Rect(r.x + 8 + 5 * (bw + 2) + 6, r.y + 24, bw, 24), off ? "소품 켜기" : "소품 끄기"))
        {
            if (off) noProps.Remove(id); else noProps.Add(id);
            EditorApplication.delayCall += () => { SaveJson(); lastMsg = RoadsideBuild.Run(true); SceneView.RepaintAll(); };
        }
        GUI.color = Color.white;
        Handles.EndGUI();
    }

    // ── 적용 = 전체 재빌드 ───────────────────────────────────
    public static string Apply()
    {
        if (root == null) return "길 그리기 꺼짐";
        SaveJson();
        var sw = System.Diagnostics.Stopwatch.StartNew(); string a, b, rs;
        try
        {
            EditorUtility.DisplayProgressBar("길 적용", "길·지형 다시 깔기 (약 2분)…", .3f);
            a = BuildBayLoop.RoadNetV2BuildAll(); var t1 = sw.Elapsed.TotalSeconds;
            EditorUtility.DisplayProgressBar("길 적용", "집 블록 계단 땅·벽…", .85f);
            b = TerraceBlock.Build(TerraceBlock.HouseCenter, 60); var t2 = sw.Elapsed.TotalSeconds;
            EditorUtility.DisplayProgressBar("길 적용", "길가 소품(바뀐 길만)…", .95f);
            rs = RoadsideBuild.Run(true);
            lastMsg = $"적용 끝 {sw.Elapsed.TotalSeconds:F0}s (길 {t1:F0}s · 집 블록 {t2 - t1:F0}s · 소품 {sw.Elapsed.TotalSeconds - t2:F0}s)";
        }
        finally { EditorUtility.ClearProgressBar(); }
        var log = lastMsg + "\n" + a + "\n" + b + "\n" + rs;
        System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Application.dataPath + "/../../docs/qa/tmp/roadpaint_last.txt"), log);
        Debug.Log("[RoadPaint] " + log); cache.Clear(); SceneView.RepaintAll();
        return log;
    }
    // QA 캡처: 씬 카메라 그림 위에 길 선(종류 색)·위반 점(빨강)을 GL 로 덧그린다 — 에디터 창이 뒤에 있어도 찍힌다
    public static string Capture(string path, Vector3 eye, Vector3 at, bool overlay, int W = 1600, int H = 900)
    {
        var go = new GameObject("rp_cap") { hideFlags = HideFlags.HideAndDontSave }; var cam = go.AddComponent<Camera>();
        cam.transform.position = eye; cam.transform.LookAt(at); cam.fieldOfView = 50; cam.nearClipPlane = .3f; cam.farClipPlane = 3000;
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        if (overlay && root != null)
        {
            var mat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave }; mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always); mat.SetPass(0);
            GL.PushMatrix(); GL.LoadProjectionMatrix(GL.GetGPUProjectionMatrix(cam.projectionMatrix, false)); GL.modelview = cam.worldToCameraMatrix;
            foreach (var c in Paints())
            {
                var k = Get(c); Parse(c.name, out var kind, out var w, out _);
                GL.Begin(GL.QUADS); GL.Color(KindColor(kind, w));
                float hw = k.changed ? .9f : .5f;
                for (int i = 0; i + 1 < k.pts.Length; i++)
                {
                    var a = k.pts[i] + Vector3.up * .5f; var b = k.pts[i + 1] + Vector3.up * .5f; var sd = Vector3.Cross(Vector3.up, b - a).normalized * hw;
                    GL.Vertex(a - sd); GL.Vertex(a + sd); GL.Vertex(b + sd); GL.Vertex(b - sd);
                }
                GL.End();
                GL.Begin(GL.QUADS); GL.Color(new Color(1f, .15f, .1f));
                foreach (var q in k.badRun) { var o = q + Vector3.up * .7f; GL.Vertex(o + new Vector3(-.6f, 0, -.6f)); GL.Vertex(o + new Vector3(-.6f, 0, .6f)); GL.Vertex(o + new Vector3(.6f, 0, .6f)); GL.Vertex(o + new Vector3(.6f, 0, -.6f)); }
                foreach (var (q, _) in k.bad) { var o = q + Vector3.up * .8f; GL.Vertex(o + new Vector3(-1.5f, 0, -1.5f)); GL.Vertex(o + new Vector3(-1.5f, 0, 1.5f)); GL.Vertex(o + new Vector3(1.5f, 0, 1.5f)); GL.Vertex(o + new Vector3(1.5f, 0, -1.5f)); }
                GL.End();
            }
            GL.PopMatrix(); Object.DestroyImmediate(mat);
        }
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)); System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
        return path;
    }
    public static string Violations() => string.Join("\n", Paints().Select(c => (c, k: Get(c))).Where(x => x.k.changed).Select(x => $"{x.c.name}: " + string.Join(" / ", x.k.bad.Select(b => b.msg))));
    public static bool IsOn => root != null;
    public static void SetOn(bool on) { if (on && root == null) On(); else if (!on && root != null) { SaveJson(); Off(true); } }
    public static void SaveNow() => SaveJson();
}
