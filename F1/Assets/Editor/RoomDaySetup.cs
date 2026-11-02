using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 과제 20261102-1951(DELIVERY-LOOP 5-5): 방 달력(RoomCalendar)·침대 자리(RoomBed)를 Map_TownV6 에 놓고 OnFootDirector 에 연결한다.
/// 스폰(OnFoot_Spawn_Room)에서 가장 가까운 CALENDAR_01·BED_01 을 쓴다 — 방이 둘이다(집 2층 · 옛 튜토리얼 방).
/// 다시 돌려도 같은 결과(기존 Room_Calendar·Room_BedSpot 을 지우고 새로 놓는다). 에디트 모드 전용.
/// 달력 판은 기존 CALENDAR_01 메시(벽걸이 모델) 앞 1.4 cm 에 겹쳐 놓는다 — 기존 메시는 건드리지 않는다(판이 더 커서 가린다).
/// </summary>
public static class RoomDaySetup
{
    [MenuItem("PitCrew/기능/하루 끝 일기장·방 달력 설치")]
    public static void InstallMenu() { Debug.Log("[RoomDaySetup] " + Install(true)); }

    public static string Install(bool save)
    {
        if (Application.isPlaying) return "플레이 중 — 끄고 다시";
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Map_TownV6") return "Map_TownV6 이 열려 있지 않음: " + scene.name;
        var spawn = GameObject.Find("OnFoot_Spawn_Room");
        var dir = Object.FindFirstObjectByType<OnFootDirector>(FindObjectsInactive.Include);
        if (spawn == null || dir == null) return $"스폰({spawn != null}) 또는 감독({dir != null}) 없음";
        var cal0 = Nearest("CALENDAR_01", spawn.transform.position);
        var bed0 = Nearest("BED_01", spawn.transform.position);
        if (cal0 == null || bed0 == null) return $"CALENDAR_01({cal0 != null}) BED_01({bed0 != null}) 없음";
        var parent = dir.transform;

        foreach (var n in new[] { "Room_Calendar", "Room_BedSpot" })
        {
            var old = parent.Find(n);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        // ── 달력: 벽 법선 = 집의 네 방향 중 스폰 쪽을 향하는 것 ──
        var cr = cal0.GetComponentInChildren<Renderer>(true);
        Vector3 c = cr != null ? cr.bounds.center : cal0.position;
        Transform house = cal0;
        for (var t = cal0; t != null; t = t.parent) if (t.name == "house_home") { house = t; break; }
        Quaternion yawOnly = Quaternion.Euler(0f, house.eulerAngles.y, 0f);
        Vector3 toSpawn = spawn.transform.position - c; toSpawn.y = 0f; toSpawn.Normalize();
        Vector3 normal = Vector3.forward; float bestDot = -2f;
        foreach (var a in new[] { yawOnly * Vector3.forward, yawOnly * Vector3.back, yawOnly * Vector3.right, yawOnly * Vector3.left })
        {
            float d = Vector3.Dot(a, toSpawn);
            if (d > bestDot) { bestDot = d; normal = a; }
        }
        Physics.SyncTransforms();
        Vector3 surface = c;                                            // 레이로 못 찾으면 그대로(경고)
        bool hitSurface = false;
        var hits = Physics.RaycastAll(c + normal * 0.5f, -normal, 1.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        foreach (var h in hits) if (h.distance < nearest) { nearest = h.distance; surface = h.point; hitSurface = true; }
        var goCal = new GameObject("Room_Calendar");
        goCal.transform.SetParent(parent, false);
        goCal.transform.SetPositionAndRotation(surface + normal * 0.014f, Quaternion.LookRotation(normal, Vector3.up));
        goCal.transform.position = new Vector3(goCal.transform.position.x, c.y, goCal.transform.position.z);
        var calendar = goCal.AddComponent<RoomCalendar>();
        var box = goCal.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0f, -0.01f); box.size = new Vector3(0.46f, 0.58f, 0.03f);

        // ── 침대: BED_01 렌더러 경계 상자 ──
        var br = bed0.GetComponentInChildren<Renderer>(true);
        var goBed = new GameObject("Room_BedSpot");
        goBed.transform.SetParent(parent, false);
        var bed = goBed.AddComponent<RoomBed>();
        if (br != null) { bed.Set(br.bounds.center, br.bounds.size); goBed.transform.position = br.bounds.center; }

        // ── 연결 ──
        var so = new SerializedObject(dir);
        so.FindProperty("calendar").objectReferenceValue = calendar;
        so.FindProperty("bed").objectReferenceValue = bed;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bed);
        EditorSceneManager.MarkSceneDirty(scene);
        if (save) EditorSceneManager.SaveScene(scene);
        return $"달력 {goCal.transform.position:F2} 법선 {normal:F2} 면 {(hitSurface ? "레이" : "못 찾음")}({nearest:F3}m) · 침대 {(br != null ? br.bounds.center.ToString("F2") + " " + br.bounds.size.ToString("F2") : "렌더러 없음")} · 연결 완료 저장={save}";
    }

    static Transform Nearest(string name, Vector3 from)
    {
        Transform best = null; float bd = float.MaxValue;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != name) continue;
            float d = Vector3.Distance(t.position, from);
            if (d < bd) { bd = d; best = t; }
        }
        return best;
    }
}
