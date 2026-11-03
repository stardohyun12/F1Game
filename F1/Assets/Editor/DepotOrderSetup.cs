using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// D-250·251(과제 20261102-1515): 택배 보관소(Q3_Depot, D198_depot) 안에 주문 칠판·포장 자리 대역을 놓고, 감독(OnFootDirector)에 잇는다.
/// - 칠판: 서쪽 안벽(x≈267.07)에 붙은 2.0×1.2 m 판(회색 틀 + 짙은 판). 글씨는 DepotOrderBoard 가 Play 때 그린다.
/// - 포장 자리: 카운터(COUNTER_01) 윗면에 회색 상자 하나 + 테이프 띠(포장되면 켜짐).
/// - DP_depot: 보관소 앞 땅의 DeliveryPoint(label "택배 보관소") — 정비소 → 보관소 길 표식 전용, 배달 경로엔 안 들어감.
/// 건물·선반·탁자는 손대지 않는다(체크리스트 11). 다시 돌리면 자리만 다시 잡는다. 회색 대역 = 아트 납품 아님.
/// </summary>
public static class DepotOrderSetup
{
    const string RootName = "OnFoot_HomeGarage";
    const string DepotName = "OnFoot_Depot";
    const string MatDir = "Assets/Scenes/Map_TownV6_Mats";
    // 측정(보고서 programmer-20261102-1515 §1): 안바닥 y 31.03, 서쪽 안벽 x 267.07, 카운터 (274.4, -29.5), 셔터 앞 땅 (270, -34).
    static readonly Vector3 BoardAt = new Vector3(267.10f, 32.55f, -29.4f);
    static readonly Vector2 CounterXZ = new Vector2(274.4f, -29.5f);
    static readonly Vector2 DepotDropXZ = new Vector2(270.0f, -34.6f);
    const float RampX = 277.0f, RampRun = 1.9f;

    [MenuItem("PitCrew/기능/택배 보관소 주문 설치")]
    public static void Install() => Debug.Log(Run());

    public static string Run()
    {
        var log = new StringBuilder("[DepotOrderSetup] ");
        var scene = EditorSceneManager.GetActiveScene();
        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        var dir = root != null ? root.GetComponent<OnFootDirector>() : null;
        if (dir == null) return log.Append("OnFoot_HomeGarage/OnFootDirector 없음 — 정비소 첫 주문 설치 먼저").ToString();
        if (GameObject.Find("D198_depot") == null) log.Append("(경고: D198_depot 없음 — 좌표만 놓음) ");

        var depot = Child(root.transform, DepotName);
        depot.localPosition = Vector3.zero; depot.localRotation = Quaternion.identity; depot.localScale = Vector3.one;
        // 감독 루트가 스케일·회전이 없다고 가정하지 않는다 — 아래는 전부 월드 좌표로 놓는다.

        var slate = Mat("Depot_Proxy_Slate", new Color(0.13f, 0.16f, 0.14f));
        var grey = Mat("Depot_Proxy_Grey", new Color(0.55f, 0.55f, 0.53f));
        var tape = Mat("Depot_Proxy_Tape", new Color(0.72f, 0.58f, 0.36f));

        // ── 칠판 ──
        var board = Child(depot, "OrderBoard");
        // 서쪽 안벽은 건물(rotY 175)을 따라 5° 기울어 있다(실측: z -32 → -26 에 x 267.28 → 266.80). 두 점으로 벽면을 재서 붙인다 — 축 정렬로 두면 칠판 왼쪽이 벽에 묻힌다(1회차 캡처).
        foreach (var c in board.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        Physics.SyncTransforms();
        Vector3 boardAt = BoardAt; var boardRot = Quaternion.Euler(0f, 90f, 0f); string wall = "벽 안 맞음";
        if (Physics.Raycast(new Vector3(269f, BoardAt.y, BoardAt.z - 0.8f), Vector3.left, out var wa, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
         && Physics.Raycast(new Vector3(269f, BoardAt.y, BoardAt.z + 0.8f), Vector3.left, out var wb, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 t = (wb.point - wa.point); t.y = 0f; t.Normalize();
            Vector3 n = new Vector3(t.z, 0f, -t.x); if (n.x < 0f) n = -n;                      // 방 안쪽(+x 쪽)
            boardRot = Quaternion.LookRotation(n, Vector3.up);
            boardAt = (wa.point + wb.point) * 0.5f + n * 0.05f;
            wall = $"{wa.collider.name} 벽 기울기 {Vector3.SignedAngle(Vector3.right, n, Vector3.up):F1}°";
        }
        foreach (var c in board.GetComponentsInChildren<Collider>(true)) c.enabled = true;
        board.SetPositionAndRotation(boardAt, boardRot);                          // forward = 벽 법선(방 안쪽)
        Part(board, "Frame", PrimitiveType.Cube, new Vector3(0f, 0f, -0.025f), new Vector3(2.12f, 1.32f, 0.04f), true, grey);
        Part(board, "Slate", PrimitiveType.Cube, new Vector3(0f, 0f, -0.002f), new Vector3(2.0f, 1.2f, 0.004f), false, slate);
        var ob = board.GetComponent<DepotOrderBoard>();               // ?? 금지 — 에디터의 가짜 null
        if (ob == null) ob = Undo.AddComponent<DepotOrderBoard>(board.gameObject);
        var obso = new SerializedObject(ob);
        obso.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
        obso.ApplyModifiedProperties();
        log.Append($"칠판={boardAt:F2} yaw={boardRot.eulerAngles.y:F1} 뒤={wall}. ");

        // ── 포장 자리(카운터 위) ──
        var pack = Child(depot, "PackSpot");
        foreach (var c in pack.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        Physics.SyncTransforms();
        float top = 32.2f; string topOn = "추정";
        if (Physics.Raycast(new Vector3(CounterXZ.x, 34f, CounterXZ.y), Vector3.down, out var ch, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        { top = ch.point.y; topOn = ch.collider.name; }
        foreach (var c in pack.GetComponentsInChildren<Collider>(true)) c.enabled = true;
        pack.SetPositionAndRotation(new Vector3(CounterXZ.x, top, CounterXZ.y), Quaternion.Euler(0f, 90f, 0f));
        Part(pack, "Box", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.45f, 0.32f, 0.36f), true, grey);
        var mark = Part(pack, "Tape", PrimitiveType.Cube, new Vector3(0f, 0.323f, 0f), new Vector3(0.47f, 0.006f, 0.08f), false, tape);
        var ps = pack.GetComponent<DepotPackSpot>();
        if (ps == null) ps = Undo.AddComponent<DepotPackSpot>(pack.gameObject);
        var psso = new SerializedObject(ps);
        psso.FindProperty("packedMark").objectReferenceValue = mark.gameObject;
        psso.ApplyModifiedProperties();
        log.Append($"포장 상자=({CounterXZ.x:F1},{top:F2},{CounterXZ.y:F1}) 받침={topOn}. ");

        // ── 보관소 앞 표식 점 ──
        var dropT = Child(depot, "DP_depot");
        if (dropT.GetComponent<SphereCollider>() == null) Undo.AddComponent<SphereCollider>(dropT.gameObject);
        var dp = dropT.GetComponent<DeliveryPoint>();
        if (dp == null) dp = Undo.AddComponent<DeliveryPoint>(dropT.gameObject);
        var dso = new SerializedObject(dp);
        dso.FindProperty("label").stringValue = "택배 보관소";
        dso.FindProperty("radius").floatValue = 2f;
        dso.ApplyModifiedProperties();
        float gy = 30.9f;
        if (Physics.Raycast(new Vector3(DepotDropXZ.x, 34f, DepotDropXZ.y), Vector3.down, out var gh, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) gy = gh.point.y;
        dropT.position = new Vector3(DepotDropXZ.x, gy + 0.6f, DepotDropXZ.y);
        log.Append($"DP_depot={dropT.position:F1}. ");

        // ── 앞마당 경사로(대역) ──
        // 실측(Play, 이 과제): 보관소 앞마당 땅 y 30.9 가 셔터 앞 길(ridge_up_c_1)보다 0.8~1.7 m 높다 — 걷는 몸 계단 한도 0.3 m 라 못 올라감.
        // 길이 가장 높은(차이가 가장 작은) x≈277 에 회색 경사로 하나. 건물·땅은 안 고친다(체크리스트 11) — 맵 쪽 해결 전 임시.
        var ramp = Child(depot, "ApronRamp");
        foreach (var c in ramp.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        Physics.SyncTransforms();
        float rx = RampX, padY = float.NaN, edgeZ = -34.5f;
        for (float z = -33.5f; z > -37f; z -= 0.05f)
        {
            if (!Physics.Raycast(new Vector3(rx, 36f, z), Vector3.down, out var eh, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (float.IsNaN(padY)) padY = eh.point.y;
            if (eh.point.y < padY - 0.1f) { edgeZ = z + 0.05f; break; }
        }
        float botZ = edgeZ - RampRun, roadY = padY - 0.9f;
        if (Physics.Raycast(new Vector3(rx, 36f, botZ), Vector3.down, out var rh, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) roadY = rh.point.y;
        foreach (var c in ramp.GetComponentsInChildren<Collider>(true)) c.enabled = true;
        Vector3 rTop = new Vector3(rx, padY, edgeZ), rBot = new Vector3(rx, roadY, botZ);
        Vector3 along = rTop - rBot; float len = along.magnitude;
        var rot = Quaternion.LookRotation(along / len, Vector3.up);
        ramp.SetPositionAndRotation((rTop + rBot) * 0.5f + rot * Vector3.down * 0.1f, rot);
        Part(ramp, "Slab", PrimitiveType.Cube, Vector3.zero, new Vector3(2.0f, 0.2f, len + 0.3f), true, grey);
        log.Append($"경사로 x={rx:F1} 길 y={roadY:F2}(z {botZ:F2}) → 마당 y={padY:F2}(가장자리 z {edgeZ:F2}) 높이 {padY - roadY:F2}m 기울기 {Mathf.Atan2(padY - roadY, RampRun) * Mathf.Rad2Deg:F0}°. ");

        var so = new SerializedObject(dir);
        so.FindProperty("orderBoard").objectReferenceValue = ob;
        so.FindProperty("packSpot").objectReferenceValue = ps;
        so.FindProperty("depotDest").objectReferenceValue = dp;
        so.ApplyModifiedProperties();
        log.Append(PlaceRoadSign(depot));
        EditorSceneManager.MarkSceneDirty(scene);
        return log.Append("감독 연결 끝(orderBoard·packSpot·depotDest).").ToString();
    }

    // D-283 첫 한 판 메모 2: 집 앞에서 "보관소가 어디쯤"인지 알려 주는 세상 속 단서 — 걷는 길(집 → 동쪽 큰길) 가의 소포 표지판.
    // 화면 화살표 없음. 새 아트 아님: 보관소 건물 아이콘 아틀라스(M_depot_icons)의 소포 칸을 그대로 쓴다. 충돌체 없음(길을 안 막는다).
    static readonly Vector2 SignXZ = new Vector2(209.0f, -77.0f);
    static readonly Vector3 HomeFrontEye = new Vector3(184.8f, 27.6f, -66.2f);

    static string PlaceRoadSign(Transform depot)
    {
        var sign = Child(depot, "DepotRoadSign");
        foreach (var c in sign.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        Physics.SyncTransforms();
        float gy = 20f;
        if (Physics.Raycast(new Vector3(SignXZ.x, 60f, SignXZ.y), Vector3.down, out var gh, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) gy = gh.point.y;
        Vector3 toEye = HomeFrontEye - new Vector3(SignXZ.x, gy, SignXZ.y); toEye.y = 0f;
        sign.SetPositionAndRotation(new Vector3(SignXZ.x, gy, SignXZ.y), Quaternion.LookRotation(toEye.normalized, Vector3.up));   // forward = 집 앞 쪽(앞면)

        var post = Mat("Depot_Sign_Post", new Color(0.30f, 0.27f, 0.24f));
        var plate = Mat("Depot_Sign_Plate", new Color(0.86f, 0.80f, 0.64f));
        var icon = SignIconMat();
        Part(sign, "Post", PrimitiveType.Cube, new Vector3(0f, 1.6f, 0.06f), new Vector3(0.18f, 3.2f, 0.18f), false, post);
        Part(sign, "Plate", PrimitiveType.Cube, new Vector3(0f, 3.4f, 0f), new Vector3(2.6f, 2.6f, 0.1f), false, plate);
        var q = Part(sign, "Icon", PrimitiveType.Quad, new Vector3(0f, 3.4f, 0.056f), new Vector3(2.3f, 2.3f, 1f), false, icon);
        q.localRotation = Quaternion.Euler(0f, 180f, 0f);                         // 쿼드 앞면(−z 쪽) 이 표지판 앞(+z)을 본다
        // 걷는 방향(동쪽 큰길)을 가리키는 화살 막대 — 표지판 앞에서 보아 오른쪽(+x 가 오른쪽일 때)이 아니라 월드 +x 방향으로 눕힌다.
        // 표지판은 집 앞 쪽을 보고 길은 그 뒤쪽(시선 방향)으로 이어진다 — 옆 화살표는 방향이 틀려 보인다. 위(↑ = 앞으로) 화살.
        Part(sign, "Arrow", PrimitiveType.Cube, new Vector3(0f, 1.15f, 0.02f), new Vector3(0.28f, 0.9f, 0.1f), false, post);
        var tip = Part(sign, "ArrowTip", PrimitiveType.Cube, new Vector3(0f, 1.7f, 0.02f), new Vector3(0.62f, 0.62f, 0.1f), false, post);
        tip.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Physics.SyncTransforms();
        var vis = new System.Text.StringBuilder();
        foreach (var eye in new[] { HomeFrontEye, HomeFrontEye + new Vector3(3.2f, 0f, -0.8f) })
            vis.Append(Physics.Linecast(eye, sign.position + Vector3.up * 3.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? "가림 " : "보임 ");
        return $"표지판=({sign.position.x:F1},{sign.position.y:F1},{sign.position.z:F1}) 집 앞 시선 {vis}. ";
    }

    /// <summary>보관소 아틀라스(M_depot_icons)의 소포 칸(왼쪽 위 1/4)만 보이게 하는 씬 전용 사본 — 원본 재질은 안 건드린다.</summary>
    static Material SignIconMat()
    {
        const string path = MatDir + "/Depot_Sign_Icon.mat";
        var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Library/Buildings/depot/M_depot_icons.mat");
        var tex = src != null && src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : null;
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Scenes", "Map_TownV6_Mats");
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(m, path);
        }
        if (tex != null)
        {
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", new Vector2(0.5f, 0.5f));
            m.SetTextureOffset("_BaseMap", new Vector2(0f, 0.5f));
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Transform Child(Transform parent, string n)
    {
        var t = parent.Find(n);
        if (t != null) return t;
        var go = new GameObject(n);
        Undo.RegisterCreatedObjectUndo(go, "Depot setup");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Transform Part(Transform parent, string n, PrimitiveType type, Vector3 lp, Vector3 ls, bool keepCollider, Material m)
    {
        var c = parent.Find(n);
        if (c == null)
        {
            var go = GameObject.CreatePrimitive(type);
            Undo.RegisterCreatedObjectUndo(go, "Depot setup");
            go.name = n;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            c = go.transform;
        }
        c.gameObject.layer = 0;
        c.localPosition = lp; c.localRotation = Quaternion.identity; c.localScale = ls;
        if (m != null) c.GetComponent<Renderer>().sharedMaterial = m;
        return c;
    }

    /// <summary>씬 전용 대역 재질(공유 재질 수정 금지 — 교훈 2). 있으면 색만 맞춘다.</summary>
    static Material Mat(string n, Color col)
    {
        string path = $"{MatDir}/{n}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Scenes", "Map_TownV6_Mats");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        m.color = col;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(m);
        return m;
    }
}
