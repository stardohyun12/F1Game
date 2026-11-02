using System.IO;
using PitCrew.PackBench;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 포장 작업대 회색 박스 시제품 씬(D-250 ②, 과제 20261102-1108)을 새로 만든다 → Assets/Scenes/Proto_PackBench.unity.
/// 맵(Map_TownV6)에는 넣지 않는다. 머티리얼은 씬 전용 회색 사본(Assets/Scenes/Proto_PackBench_Mats/) — 공유 .mat 안 건드림.
/// 배치(세계 좌표, m): 눈 (0, 1.6, 0) +z 를 보고 48° 내려다봄 · 작업대 윗면 y 0.9, z 0.3~1.0, 폭 1.4 · 뒷벽 z 1.3
///   · 선반 두 단(y 1.15 / 1.6) 위 상자 S(−0.45)·M(0)·L(+0.5) · 작업 자리 (0.05, 0.9, 0.62)
///   · 왼쪽 앞 달걀 한 판, 왼쪽 뒤 신문지 묶음·라벨 묶음, 오른쪽 뽁뽁이 롤·테이프.
/// 상자 크기(속): S 0.32×0.10×0.32(완충재 들어갈 자리 없음) · M 0.315×0.15×0.315(맞는 상자) · L 0.50×0.30×0.50(헐렁) — 가설.
/// </summary>
public static class PackBenchBuilder
{
    public const string ScenePath = "Assets/Scenes/Proto_PackBench.unity";
    const string MatDir = "Assets/Scenes/Proto_PackBench_Mats";
    // 보관소(D-251)용: 아래 도우미들은 rig 기준 로컬 좌표로 놓는다(rig == null 이면 예전처럼 월드 = 시제품 씬).
    static Transform rig;
    static string matDir = MatDir, matPrefix = "PB_";
    static Vector3 P(Vector3 local) => rig != null ? rig.TransformPoint(local) : local;
    static Quaternion Q(Quaternion local) => rig != null ? rig.rotation * local : local;

    [MenuItem("PitCrew/Proto/Build PackBench (포장 작업대 회색 박스)")]
    static void Menu() => Debug.Log("[PackBenchBuilder] " + Build());

    public static string Build()
    {
        if (EditorApplication.isPlaying) return "Play 중 — 멈추고 다시";
        var cur = SceneManager.GetActiveScene();
        if (cur.isDirty && cur.path != ScenePath) return $"열린 씬 {cur.path} 에 저장 안 된 변경이 있음 — 중단(덮지 않음)";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        rig = null; matDir = MatDir; matPrefix = "PB_";
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Scenes", "Proto_PackBench_Mats");
        var mFloor = Mat("PB_Floor", 0.34f, 0.1f);
        var mWall = Mat("PB_Wall", 0.52f, 0.05f);
        var mBench = Mat("PB_Bench", 0.42f, 0.15f);
        var mShelf = Mat("PB_Shelf", 0.46f, 0.1f);
        var mBox = Mat("PB_Box", 0.60f, 0.05f);
        var mTray = Mat("PB_Tray", 0.70f, 0.05f);
        var mEgg = Mat("PB_Egg", 0.80f, 0.2f);
        var mPaper = Mat("PB_Paper", 0.78f, 0.0f);
        var mBubble = Mat("PB_Bubble", 0.66f, 0.6f);
        var mTape = Mat("PB_Tape", 0.50f, 0.5f);
        var mLabel = Mat("PB_Label", 0.88f, 0.1f);

        // ── 방 ──
        var room = new GameObject("Room").transform;
        Block("Floor", room, new Vector3(0f, -0.05f, 0.6f), new Vector3(4f, 0.1f, 3.6f), mFloor);
        Block("Wall_Back", room, new Vector3(0f, 1.4f, 1.35f), new Vector3(4f, 2.8f, 0.1f), mWall);
        Block("Wall_L", room, new Vector3(-1.4f, 1.4f, 0.4f), new Vector3(0.1f, 2.8f, 2.4f), mWall);
        Block("Wall_R", room, new Vector3(1.4f, 1.4f, 0.4f), new Vector3(0.1f, 2.8f, 2.4f), mWall);
        Block("Bench", room, new Vector3(0f, 0.45f, 0.65f), new Vector3(1.4f, 0.9f, 0.7f), mBench);
        Block("Shelf_Low", room, new Vector3(0f, 1.14f, 1.15f), new Vector3(1.6f, 0.02f, 0.3f), mShelf);
        Block("Shelf_High", room, new Vector3(0f, 1.59f, 1.15f), new Vector3(1.6f, 0.02f, 0.3f), mShelf);
        Block("Shelf_PostL", room, new Vector3(-0.79f, 1.1f, 1.15f), new Vector3(0.03f, 0.8f, 0.3f), mShelf);
        Block("Shelf_PostR", room, new Vector3(0.79f, 1.1f, 1.15f), new Vector3(0.03f, 0.8f, 0.3f), mShelf);

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(55f, 25f, 0f);
        var fill = new GameObject("Lamp").AddComponent<Light>();
        fill.type = LightType.Point; fill.range = 3f; fill.intensity = 0.8f; fill.shadows = LightShadows.None;
        fill.transform.position = new Vector3(0f, 2.3f, 0.5f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.44f);

        // ── 눈(카메라) ──
        var eye = new GameObject("Eye", typeof(Camera), typeof(AudioListener));
        eye.tag = "MainCamera";
        eye.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(48f, 0f, 0f));
        var cam = eye.GetComponent<Camera>();
        cam.fieldOfView = 60f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 30f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f, 0.2f, 0.21f);
        var low = eye.AddComponent<LowResRenderTest>();
        low.targetHeight = 360; low.smooth = false; low.msaa = 1;   // 걷기·주행과 같은 360 도트 설정

        // ── 상자 셋(선반) ──
        var shelf = new GameObject("ShelfSlots").transform;
        var boxes = new GameObject("Boxes").transform;
        var bS = MakeBox("Box_S", boxes, shelf, "S", new Vector3(0.32f, 0.10f, 0.32f), new Vector3(-0.45f, 1.15f, 1.15f), mBox, mTape, mLabel);
        var bM = MakeBox("Box_M", boxes, shelf, "M", new Vector3(0.315f, 0.15f, 0.315f), new Vector3(0f, 1.15f, 1.15f), mBox, mTape, mLabel);
        var bL = MakeBox("Box_L", boxes, shelf, "L", new Vector3(0.50f, 0.30f, 0.50f), new Vector3(0.5f, 1.15f, 1.15f), mBox, mTape, mLabel);

        var spot = new GameObject("BenchSpot").transform;
        spot.SetPositionAndRotation(new Vector3(0.05f, 0.9f, 0.62f), Quaternion.identity);

        // ── 재료 더미 ──
        var sup = new GameObject("Supplies").transform;
        Supply("Newspaper", sup, PrimitiveType.Cube, new Vector3(-0.55f, 0.93f, 0.75f), Quaternion.Euler(0f, 8f, 0f), new Vector3(0.30f, 0.06f, 0.22f), mPaper, PackSupplyKind.Newspaper);
        Supply("BubbleRoll", sup, PrimitiveType.Cylinder, new Vector3(0.55f, 0.98f, 0.55f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.16f, 0.16f, 0.16f), mBubble, PackSupplyKind.Bubble);
        Supply("Tape", sup, PrimitiveType.Cylinder, new Vector3(0.42f, 0.9125f, 0.95f), Quaternion.identity, new Vector3(0.10f, 0.0125f, 0.10f), mTape, PackSupplyKind.Tape);
        Supply("Labels", sup, PrimitiveType.Cube, new Vector3(-0.32f, 0.91f, 0.95f), Quaternion.Euler(0f, -6f, 0f), new Vector3(0.10f, 0.02f, 0.07f), mLabel, PackSupplyKind.Label);

        // ── 달걀 한 판(CargoTypePresets Eggs 대역 — 회색) ──
        MakeTray(new Vector3(-0.45f, 0.9f + 0.0325f + 0.002f, 0.45f), mTray, mEgg);

        // ── 손(컨트롤러) + 커서 ──
        var bench = eye.AddComponent<PackBench>();
        bench.cam = cam; bench.benchSpot = spot; bench.paperMat = mPaper; bench.bubbleMat = mBubble;
        var cur2 = new GameObject("Cursor").AddComponent<PackBenchCursor>();
        var so = new SerializedObject(cur2);
        so.FindProperty("bench").objectReferenceValue = bench;
        so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
        so.FindProperty("smallFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri9.ttf");
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        bool ok = EditorSceneManager.SaveScene(scene, ScenePath);
        return $"{(ok ? "저장" : "저장 실패")} {ScenePath} — 상자 {bS.sizeName}/{bM.sizeName}/{bL.sizeName}, 머티리얼 {MatDir}";
    }

    // ═══ D-251(과제 20261102-1651): 같은 작업대를 택배 보관소 카운터(COUNTER_01) 위에 ═══
    // 시제품 좌표를 그대로 rig 로컬로 쓴다 — rig 원점 = 시제품의 「발밑」(눈 아래 0.7+0.9 m), rig +z = 작업대 쪽.
    // 카운터 윗면 = 로컬 y 0.9(시제품 작업대 윗면)가 되게 rig 높이를 맞추고, 카운터 앞(손님 쪽) 가장자리에서 0.19 m 떨어져 선다.
    // 시제품의 방(바닥·벽)·작업대 블록·조명은 안 만든다(진짜 카운터·건물·조명 사용). 선반 두 단은 카운터 뒤(로컬 z 1.25)에 다리째.
    const string DepotScene = "Assets/Scenes/Map_TownV6.unity";
    const string DepotMatDir = "Assets/Scenes/Map_TownV6_Mats";
    const float DepotYawFallback = 85f;    // 건물 rotY 175 → 카운터 앞에서 보관소 안쪽(+x) 을 보는 방향

    [MenuItem("PitCrew/기능/보관소 포장 작업대 설치 (D-251)")]
    static void DepotMenu() => Debug.Log("[PackBenchBuilder] " + BuildDepot());

    public static string BuildDepot()
    {
        if (EditorApplication.isPlaying) return "Play 중 — 멈추고 다시";
        var scene = SceneManager.GetActiveScene();
        if (scene.path != DepotScene) return $"열린 씬이 {scene.path} — {DepotScene} 를 열고 다시";
        var psGo = GameObject.Find("OnFoot_Depot/PackSpot");
        var ps = psGo != null ? psGo.GetComponent<DepotPackSpot>() : null;
        if (ps == null) return "OnFoot_Depot/PackSpot(DepotPackSpot) 없음 — 택배 보관소 주문 설치 먼저";

        // 다시 돌리면 예전 작업대를 지우고 새로
        var old = ps.transform.Find("PackBench");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        foreach (var n in new[] { "Box", "Tape" }) { var t = ps.transform.Find(n); if (t != null) t.gameObject.SetActive(false); }   // 대역 상자(1515) 숨김
        Physics.SyncTransforms();

        // 카운터 찾기
        Collider counter = null;
        foreach (var c in Physics.OverlapBox(new Vector3(274.4f, 31.8f, -29.5f), new Vector3(1.5f, 1.2f, 1.5f)))
            if (c.name == "COUNTER_01") { counter = c; break; }
        if (counter == null) return "COUNTER_01 콜라이더 없음 — 중단";
        var cb = counter.bounds;
        if (!Physics.Raycast(new Vector3(cb.center.x, cb.max.y + 1f, cb.center.z), Vector3.down, out var topHit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return "카운터 윗면 레이 안 맞음 — 중단";
        float top = topHit.point.y;
        // 카운터 앞 방향: 카운터의 가로축 중 월드 +x(보관소 안쪽)에 가장 가까운 것
        float yaw = DepotYawFallback; string yawSrc = "기본값";
        var ct = counter.transform; float bestDot = 0.9f;
        foreach (var ax in new[] { ct.right, -ct.right, ct.forward, -ct.forward, ct.up, -ct.up })
        {
            var h = new Vector3(ax.x, 0f, ax.z); if (h.sqrMagnitude < 0.5f) continue; h.Normalize();
            if (h.x > bestDot) { bestDot = h.x; yaw = Mathf.Atan2(h.x, h.z) * Mathf.Rad2Deg; yawSrc = "카운터 축"; }
        }
        var f = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        var origin = new Vector3(cb.center.x, top - 0.9f, cb.center.z) - f * 0.65f;

        var rigGo = new GameObject("PackBench");
        rigGo.transform.SetParent(ps.transform, false);
        rigGo.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, yaw, 0f));
        rig = rigGo.transform; matDir = DepotMatDir; matPrefix = "DepotPB_";
        try
        {
            var mShelf = Mat("PB_Shelf", 0.46f, 0.1f);
            var mBox = Mat("PB_Box", 0.60f, 0.05f);
            var mTray = Mat("PB_Tray", 0.70f, 0.05f);
            var mEgg = Mat("PB_Egg", 0.80f, 0.2f);
            var mPaper = Mat("PB_Paper", 0.78f, 0.0f);
            var mBubble = Mat("PB_Bubble", 0.66f, 0.6f);
            var mTape = Mat("PB_Tape", 0.50f, 0.5f);
            var mLabel = Mat("PB_Label", 0.88f, 0.1f);

            // 작업면: 보이지 않는 얇은 판(윗면 = 로컬 y 0.9) — 걷기 시선이 [E] 를 잡는 자리이자 물건이 놓이는 면(카운터 윗면과 같은 높이)
            var benchTop = new GameObject("BenchTop").transform;
            benchTop.SetParent(rig, false);
            benchTop.localPosition = new Vector3(0f, 0.895f, 0.65f); benchTop.localRotation = Quaternion.identity;
            benchTop.gameObject.AddComponent<BoxCollider>().size = new Vector3(1.4f, 0.01f, 0.7f);

            // 작업 판(보이는 것): COUNTER_01 충돌 상자 윗면(top)이 그림 윗면보다 23cm 높고, 그 사이에 건물 메시 소품(통·병·초록 상자)이 줄지어 있다.
            // 물건이 공중에 뜬 것처럼 보이지 않게 카운터 위에 두꺼운 판을 깔아 소품을 덮는다(건물은 그대로). 판 = 소품 줄 전체(월드 z -28.70~-30.45) × 카운터 깊이.
            float visTop = top;
            {
                var cmf = counter.GetComponent<MeshFilter>();
                var probe = new GameObject("tmp_probe"); probe.layer = 31;
                probe.transform.SetPositionAndRotation(counter.transform.position, counter.transform.rotation); probe.transform.localScale = counter.transform.lossyScale;
                probe.AddComponent<MeshCollider>().sharedMesh = cmf.sharedMesh;
                Physics.SyncTransforms();
                if (Physics.Raycast(new Vector3(cb.center.x - 0.25f, top + 1f, cb.center.z), Vector3.down, out var vh, 2f, 1 << 31)) visTop = vh.point.y;
                Object.DestroyImmediate(probe);
            }
            if (top - visTop > 0.02f)
            {
                // 카운터와 같은 재질을 참조만 한다(수정 안 함) — 카운터가 두꺼워진 것처럼 보이게
                var cr = counter.GetComponent<Renderer>();
                var mBoard = cr != null && cr.sharedMaterial != null ? cr.sharedMaterial : Mat("PB_Bench", 0.42f, 0.15f);
                float th = top - visTop;
                var board = Block("WorkBoard", rig, new Vector3(0.025f, 0.9f - th * 0.5f, 0.645f), new Vector3(1.75f, th, 0.75f), mBoard);
                Object.DestroyImmediate(board.GetComponent<Collider>());
            }

            // 선반 두 단 + 다리(바닥까지) — 카운터 뒤
            float floorLocal = 0f;
            var postAt = P(new Vector3(0.79f, 0.5f, 1.25f));
            if (Physics.Raycast(postAt, Vector3.down, out var fl, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) floorLocal = rig.InverseTransformPoint(fl.point).y;
            var shelfRoot = new GameObject("Shelf").transform; shelfRoot.SetParent(rig, false);
            Block("Shelf_Low", shelfRoot, new Vector3(0f, 1.14f, 1.25f), new Vector3(1.6f, 0.02f, 0.3f), mShelf);
            Block("Shelf_High", shelfRoot, new Vector3(0f, 1.59f, 1.25f), new Vector3(1.6f, 0.02f, 0.3f), mShelf);
            float postH = 1.6f - floorLocal;
            Block("Shelf_PostL", shelfRoot, new Vector3(-0.79f, floorLocal + postH * 0.5f, 1.25f), new Vector3(0.03f, postH, 0.3f), mShelf);
            Block("Shelf_PostR", shelfRoot, new Vector3(0.79f, floorLocal + postH * 0.5f, 1.25f), new Vector3(0.03f, postH, 0.3f), mShelf);
            foreach (Transform t in shelfRoot) t.gameObject.isStatic = false;   // 씬 정적 배칭에 안 끼게(대역)

            var slots = new GameObject("ShelfSlots").transform; slots.SetParent(rig, false);
            var boxes = new GameObject("Boxes").transform; boxes.SetParent(rig, false);
            MakeBox("Box_S", boxes, slots, "S", new Vector3(0.32f, 0.10f, 0.32f), new Vector3(-0.45f, 1.15f, 1.25f), mBox, mTape, mLabel);
            MakeBox("Box_M", boxes, slots, "M", new Vector3(0.315f, 0.15f, 0.315f), new Vector3(0f, 1.15f, 1.25f), mBox, mTape, mLabel);
            MakeBox("Box_L", boxes, slots, "L", new Vector3(0.50f, 0.30f, 0.50f), new Vector3(0.5f, 1.15f, 1.25f), mBox, mTape, mLabel);

            var spot = new GameObject("BenchSpot").transform; spot.SetParent(rig, false);
            spot.SetPositionAndRotation(P(new Vector3(0.05f, 0.9f, 0.62f)), Q(Quaternion.identity));

            var sup = new GameObject("Supplies").transform; sup.SetParent(rig, false);
            // 카운터에 붙은 건물 소품(초록 상자 로컬 x 0.5~0.85 z 0.55~0.9, 파란 통 로컬 x -0.8 z 0.6)을 피해 신문·뽁뽁이만 proto 자리에서 옮김(top_counter.png 실측)
            Supply("Newspaper", sup, PrimitiveType.Cube, new Vector3(-0.50f, 0.93f, 0.75f), Quaternion.Euler(0f, 8f, 0f), new Vector3(0.30f, 0.06f, 0.22f), mPaper, PackSupplyKind.Newspaper);
            Supply("BubbleRoll", sup, PrimitiveType.Cylinder, new Vector3(0.45f, 0.98f, 0.38f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.16f, 0.16f, 0.16f), mBubble, PackSupplyKind.Bubble);
            Supply("Tape", sup, PrimitiveType.Cylinder, new Vector3(0.42f, 0.9125f, 0.95f), Quaternion.identity, new Vector3(0.10f, 0.0125f, 0.10f), mTape, PackSupplyKind.Tape);
            Supply("Labels", sup, PrimitiveType.Cube, new Vector3(-0.32f, 0.91f, 0.95f), Quaternion.Euler(0f, -6f, 0f), new Vector3(0.10f, 0.02f, 0.07f), mLabel, PackSupplyKind.Label);

            var tray = MakeTray(new Vector3(-0.45f, 0.9f + 0.0325f + 0.002f, 0.45f), mTray, mEgg);
            tray.SetActive(false);                                         // 칠판에서 주문을 맡으면 놓인다(DepotPackSpot.ShowCargo)

            // 눈(포장 시점) — 카메라는 걷기 카메라를 그대로 쓴다. PackBench 는 여기 붙고 꺼진 채(감독이 켠다).
            var eye = new GameObject("Eye").transform; eye.SetParent(rig, false);
            eye.SetPositionAndRotation(P(new Vector3(0f, 1.6f, 0f)), Q(Quaternion.Euler(48f, 0f, 0f)));
            var bench = eye.gameObject.AddComponent<PackBench>();
            bench.benchSpot = spot; bench.paperMat = mPaper; bench.bubbleMat = mBubble;
            bench.enabled = false;
            var cur = new GameObject("Cursor").AddComponent<PackBenchCursor>();
            cur.transform.SetParent(rig, false);
            var cso = new SerializedObject(cur);
            cso.FindProperty("bench").objectReferenceValue = bench;
            cso.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
            cso.FindProperty("smallFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri9.ttf");
            cso.ApplyModifiedPropertiesWithoutUndo();
            cur.enabled = false;

            var so = new SerializedObject(ps);
            so.FindProperty("bench").objectReferenceValue = bench;
            so.FindProperty("cursor").objectReferenceValue = cur;
            so.FindProperty("eye").objectReferenceValue = eye;
            so.FindProperty("cargo").objectReferenceValue = tray;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 확인: 선반·상자가 건물 콜라이더와 겹치나
            Physics.SyncTransforms();
            var overlaps = new System.Text.StringBuilder();
            foreach (Transform t in shelfRoot)
                foreach (var c in Physics.OverlapBox(t.position, Vector3.Scale(t.lossyScale, Vector3.one * 0.5f) * 0.98f, t.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (!c.transform.IsChildOf(rig)) overlaps.Append($"{t.name}×{c.name} ");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            bool ok = EditorSceneManager.SaveScene(scene);
            var eyeW = eye.position;
            return $"{(ok ? "저장" : "저장 실패")} {scene.path} — 카운터 {counter.name} 윗면 y {top:F2} 중심 {cb.center.ToString("F2")}, yaw {yaw:F1}({yawSrc}), rig {origin.ToString("F2")}, 눈 {eyeW.ToString("F2")}(바닥 위 {eyeW.y - (origin.y + floorLocal):F2} m), 바닥 로컬 y {floorLocal:F2}, 겹침 [{overlaps}]";
        }
        finally { rig = null; matDir = MatDir; matPrefix = "PB_"; }
    }

    static Material Mat(string name, float grey, float smooth)
    {
        if (matPrefix != "PB_" && name.StartsWith("PB_")) name = matPrefix + name.Substring(3);
        string p = $"{matDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        var c = new Color(grey, grey, grey * 0.98f);
        m.SetColor("_BaseColor", c); m.color = c;
        m.SetFloat("_Smoothness", smooth);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Transform Block(string name, Transform parent, Vector3 pos, Vector3 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.isStatic = true;
        g.transform.SetParent(parent, false);
        g.transform.SetPositionAndRotation(P(pos), Q(Quaternion.identity)); g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g.transform;
    }

    static PackBox MakeBox(string name, Transform parent, Transform slots, string size, Vector3 inner, Vector3 shelfPos, Material box, Material tape, Material label)
    {
        var slot = new GameObject("Slot_" + size).transform;
        slot.SetParent(slots, false);
        slot.SetPositionAndRotation(P(shelfPos), Q(Quaternion.identity));
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.SetPositionAndRotation(P(shelfPos), Q(Quaternion.identity));
        var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
        var b = g.AddComponent<PackBox>();
        b.inner = inner; b.sizeName = size; b.boxMat = box; b.tapeMat = tape; b.labelMat = label;
        b.shelfSlot = slot; b.startClosed = true;
        b.Build();
        return b;
    }

    static void Supply(string name, Transform parent, PrimitiveType type, Vector3 pos, Quaternion rot, Vector3 scale, Material m, PackSupplyKind kind)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.SetPositionAndRotation(P(pos), Q(rot)); g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (type == PrimitiveType.Cylinder)   // 기본 캡슐 콜라이더 대신 상자(롤이 굴러보이지 않게, 조준만 맞으면 됨)
        {
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(1f, 2f, 1f);
        }
        g.AddComponent<PackSupply>().kind = kind;
    }

    /// <summary>달걀 한 판: 충돌 상자 0.30×0.065×0.30(중심 = 피벗) + 보기용 판·달걀 5×6(콜라이더 없음).</summary>
    static GameObject MakeTray(Vector3 pos, Material tray, Material egg)
    {
        var g = new GameObject("EggTray");
        if (rig != null) g.transform.SetParent(rig, false);
        g.transform.SetPositionAndRotation(P(pos), Q(Quaternion.identity));
        var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(0.30f, 0.065f, 0.30f);
        var pm = new PhysicsMaterial("EggTray") { dynamicFriction = 0.5f, staticFriction = 0.6f, bounciness = 0.05f };
        string pmPath = $"{matDir}/{matPrefix}EggTray.physicMaterial";
        var saved = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
        if (saved == null) { AssetDatabase.CreateAsset(pm, pmPath); saved = pm; }
        bc.sharedMaterial = saved;
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(slab.GetComponent<Collider>());
        slab.name = "Slab"; slab.transform.SetParent(g.transform, false);
        slab.transform.localPosition = new Vector3(0f, -0.0175f, 0f); slab.transform.localScale = new Vector3(0.30f, 0.03f, 0.30f);
        slab.GetComponent<Renderer>().sharedMaterial = tray;
        for (int x = 0; x < 5; x++)
            for (int z = 0; z < 6; z++)
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(e.GetComponent<Collider>());
                e.name = $"Egg_{x}{z}"; e.transform.SetParent(g.transform, false);
                e.transform.localPosition = new Vector3((x - 2f) * 0.058f, 0.008f, (z - 2.5f) * 0.048f);
                e.transform.localScale = new Vector3(0.042f, 0.05f, 0.042f);
                e.GetComponent<Renderer>().sharedMaterial = egg;
            }
        var rb = g.AddComponent<Rigidbody>();
        rb.mass = 2.0f; rb.linearDamping = 0.1f; rb.angularDamping = 0.5f;   // 달걀 30개 × 60 g + 판
        PackBench.Setup(rb);
        g.AddComponent<PackItem>().kind = PackItemKind.Eggs;
        return g;
    }
}
