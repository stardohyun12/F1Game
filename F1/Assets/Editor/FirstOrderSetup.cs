using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 기능 2(과제 20261031-0209): 정비소 사장(1.7 m 캡슐 대역) + 첫 주문 배달지(찻집)를 지금 열린 씬(Map_TownV6)에 설치한다. 씬은 저장하지 않는다(dirty 만).
/// - 기능 1 루트(OnFoot_HomeGarage)가 없으면 먼저 OnFootSetup 을 돌린다.
/// - 사장: 카트 자리 둘레 2.6 m 고리에서 고른다 — 땅이 있고, 몸이 안 끼고, 카트 앞길(±50°)이 아니고, 차고 마당 접근점에서 보이는 자리 중 차고 문에 가장 가까운 곳.
/// - 배달지: 씬에 이미 있는 찻집 DeliveryPoint(label "tea", BuildBayLoop.Gray 가 town-v6b.json stops.tea 앞 차길에 만든 DP_tea)를 그대로 쓴다.
///   없으면 OnFoot_TeaDrop 을 새로 만들어 TeaFallbackXZ 에서 가장 가까운 차길 중심선 점에 둔다.
/// 이미 있으면 재사용하고 자리만 다시 잡는다(중복 생성 X).
/// </summary>
public static class FirstOrderSetup
{
    const string RootName = "OnFoot_HomeGarage";
    const string BossName = "OnFoot_GarageBoss";
    const string DropName = "OnFoot_TeaDrop";
    // 차고 마당 접근점 = 기능 1 봇이 걸어오는 「차고 마당」(OnFootRouteBot). 차고 문 = placements.yaml garage.
    static readonly Vector2 ApproachXZ = new Vector2(229.4f, -57.6f);
    static readonly Vector2 GarageXZ = new Vector2(234.1f, -53.79f);
    // first-delivery.yaml 의 찻집 좌표(D-198 맵 기준). 현재 맵(town-v6b)에선 hill_field 자리라 씬에 DP_tea 가 없을 때만 쓴다.
    static readonly Vector2 TeaFallbackXZ = new Vector2(181.4f, 5.7f);

    [MenuItem("PitCrew/기능/정비소 첫 주문 설치")]
    public static void Install() => Debug.Log(Run());

    public static string Run()
    {
        var log = new StringBuilder("[FirstOrderSetup] ");
        var scene = EditorSceneManager.GetActiveScene();
        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        if (root == null) { log.Append("기능 1 루트 없음 → OnFootSetup 먼저: ").Append(OnFootSetup.Run()).Append(" | "); root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName); }
        if (root == null) return log.Append("OnFoot_HomeGarage 를 못 만듦 — 중단").ToString();
        var dir = root.GetComponent<OnFootDirector>();
        var cartSpot = root.transform.Find("OnFoot_CartSpot");
        if (dir == null || cartSpot == null) return log.Append("감독·카트 자리 없음 — 중단").ToString();
        Physics.SyncTransforms();

        // ── 사장 ──
        var boss = root.transform.Find(BossName);
        if (boss == null)
        {
            var go = new GameObject(BossName);
            Undo.RegisterCreatedObjectUndo(go, "FirstOrder setup");
            go.transform.SetParent(root.transform, false);
            boss = go.transform;
        }
        // 몸(콜라이더 있음 — 시선 레이가 맞아야 한다) + 코(방향 표시, 콜라이더 없음). 레이어 기본(0).
        Part(boss, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(0.6f, 0.85f, 0.6f), true);
        Part(boss, "Nose", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0.27f), new Vector3(0.2f, 0.1f, 0.12f), false);
        var bossCols = boss.GetComponentsInChildren<Collider>(true);
        foreach (var c in bossCols) c.enabled = false;            // 자리 고를 때 자기 몸에 안 걸리게
        Physics.SyncTransforms();

        Vector3 cp = cartSpot.position; Vector3 cfw = cartSpot.forward; cfw.y = 0f; cfw.Normalize();
        float apY = cp.y;
        if (Physics.Raycast(new Vector3(ApproachXZ.x, cp.y + 3f, ApproachXZ.y), Vector3.down, out var ah, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) apY = ah.point.y;
        Vector3 eye = new Vector3(ApproachXZ.x, apY + 1.6f, ApproachXZ.y);
        Vector3 bestP = Vector3.zero; float bestScore = float.MaxValue; int tried = 0, okN = 0;
        foreach (float r in new[] { 2.6f, 3.2f })
            for (int deg = 0; deg < 360; deg += 15)
            {
                tried++;
                Vector3 dirv = Quaternion.Euler(0f, deg, 0f) * cfw;
                if (Vector3.Angle(dirv, cfw) < 50f) continue;                                         // 카트 앞길은 비운다
                Vector3 p = cp + dirv * r;
                if (!Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out var gh, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(gh.point.y - cp.y) > 0.8f) continue;                                   // 마당 높이
                p = gh.point;
                if (Physics.CheckCapsule(p + Vector3.up * 0.35f, p + Vector3.up * 1.4f, 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (new Vector2(p.x - ApproachXZ.x, p.z - ApproachXZ.y).magnitude < 1.8f) continue;  // 접근 길을 막지 않는다
                Vector3 chest = p + Vector3.up * 1.3f;
                if (Physics.Linecast(eye, chest, out var lh, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && lh.collider.GetComponentInParent<ArcadeCarController>() == null) continue;
                okN++;
                float score = new Vector2(p.x - GarageXZ.x, p.z - GarageXZ.y).magnitude;               // 차고 문 옆에 서 있는 사장
                if (score < bestScore) { bestScore = score; bestP = p; }
            }
        foreach (var c in bossCols) c.enabled = true;
        if (bestScore == float.MaxValue) { bestP = cp - cartSpot.right * 2.6f; log.Append("(사장 자리 조건 맞는 곳 없음 — 카트 왼쪽 2.6 m) "); }
        Vector3 face = new Vector3(ApproachXZ.x, bestP.y, ApproachXZ.y) - bestP;
        Undo.RecordObject(boss, "FirstOrder boss");
        boss.SetPositionAndRotation(bestP, face.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(face.normalized) : Quaternion.identity);
        log.Append($"사장={bestP:F2} yaw={boss.eulerAngles.y:F0} (후보 {tried} 중 {okN} 통과, 차고 문까지 {bestScore:F1}m). ");

        // ── 배달지 ──
        DeliveryPoint drop = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(d => d.Label == "tea" || d.name == "DP_tea");
        if (drop != null)
        {
            log.Append($"배달지=씬의 {drop.name}(label {drop.Label}) {drop.transform.position:F1}, 차고에서 직선 {new Vector2(drop.transform.position.x - GarageXZ.x, drop.transform.position.z - GarageXZ.y).magnitude:F0}m. ");
            var own = root.transform.Find(DropName);
            if (own != null) { Undo.DestroyObjectImmediate(own.gameObject); log.Append("(예전 OnFoot_TeaDrop 지움) "); }
        }
        else
        {
            var t = root.transform.Find(DropName);
            if (t == null)
            {
                var go = new GameObject(DropName, typeof(SphereCollider));
                Undo.RegisterCreatedObjectUndo(go, "FirstOrder setup");
                go.transform.SetParent(root.transform, false);
                t = go.transform;
            }
            drop = t.GetComponent<DeliveryPoint>();
            if (drop == null) drop = Undo.AddComponent<DeliveryPoint>(t.gameObject);
            var dso = new SerializedObject(drop);
            dso.FindProperty("label").stringValue = "TeaHouse";
            dso.FindProperty("radius").floatValue = 5f;
            dso.ApplyModifiedProperties();
            Vector3 best = Vector3.zero; float bd = float.MaxValue; string rid = "-";
            foreach (var rs in Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None))
            {
                if (rs.centerline == null || rs.kind == "stairs" || (rs.kind != null && rs.kind.StartsWith("walk"))) continue;
                foreach (var p in rs.centerline)
                {
                    float d = (new Vector2(p.x, p.z) - TeaFallbackXZ).sqrMagnitude;
                    if (d < bd) { bd = d; best = p; rid = rs.roadId; }
                }
            }
            if (bd == float.MaxValue) return log.Append("찻집: 씬에 DP_tea 도 차길 중심선도 없음 — 중단").ToString();
            Undo.RecordObject(t, "FirstOrder drop");
            t.position = best + Vector3.up * 0.6f;
            log.Append($"배달지=새 {DropName} {t.position:F1} [{rid}] (yaml 좌표에서 {Mathf.Sqrt(bd):F1}m — 씬에 DP_tea 없음). ");
        }

        var so = new SerializedObject(dir);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("firstDrop").objectReferenceValue = drop;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        return log.ToString();
    }

    static void Part(Transform parent, string n, PrimitiveType type, Vector3 lp, Vector3 ls, bool keepCollider)
    {
        var c = parent.Find(n);
        if (c == null)
        {
            var go = GameObject.CreatePrimitive(type);
            Undo.RegisterCreatedObjectUndo(go, "FirstOrder setup");
            go.name = n;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            c = go.transform;
        }
        c.gameObject.layer = 0;
        c.localPosition = lp; c.localRotation = Quaternion.identity; c.localScale = ls;
    }
}
