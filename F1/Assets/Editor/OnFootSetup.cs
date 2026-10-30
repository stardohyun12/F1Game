using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 기능 1(과제 20261030-2243): 집-차고 구간을 지금 열린 씬(Map_TownV6)에 설치한다. 씬은 저장하지 않는다(dirty 만).
/// - 루트 OnFoot_HomeGarage(감독) 아래 캐릭터·시작점(주인공 방)·카트 자리. 이미 있으면 재사용하고 자리만 다시 잡는다(중복 생성 X).
/// - house_home 의 DOOR_* 마다 DoorHinge: 메시에서 경첩(피벗이 문 끝이면 피벗, 아니면 손잡이 반대 끝)을 찾고,
///   반쯤 열린 채 모델링된 문은 문틀(집 껍데기 콜라이더)에 맞는 각도를 찾아 닫힌 자세로 적는다.
/// </summary>
public static class OnFootSetup
{
    const string RootName = "OnFoot_HomeGarage";
    // 주인공 방 = house_home 좌표(2층 바닥 3.2 m, 방 가운데). 9차 집: 방 = 칸막이(x -0.65) ~ 동쪽 벽(x -4.15), ROOM_2F 가운데 x -2.4(library_import 0133).
    static readonly Vector3 SpawnHouseLocal = new Vector3(-2.4f, 3.25f, 1.35f);

    [MenuItem("PitCrew/기능/집-차고 구간 설치")]
    public static void Install() => Debug.Log(Run());

    public static string Run()
    {
        var log = new StringBuilder("[OnFootSetup] ");
        var scene = EditorSceneManager.GetActiveScene();
        Transform house = FindHouse();
        if (house == null) return log.Append("house_home 을 못 찾음 — 중단").ToString();

        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        bool fresh = root == null;
        if (fresh) { root = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(root, "OnFoot setup"); }
        var dir = Get<OnFootDirector>(root);

        // 시작점(주인공 방) — 방문 쪽을 본다
        var spawn = Child(root.transform, "OnFoot_Spawn_Room");
        Vector3 sp = house.TransformPoint(SpawnHouseLocal);
        if (Physics.Raycast(sp + Vector3.up * 0.5f, Vector3.down, out var sh, 1.5f)) sp.y = sh.point.y + 0.02f;
        else log.Append("(시작점 바닥 못 찾음) ");
        Transform roomDoor = house.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "DOOR_ROOM");
        float spYaw = house.eulerAngles.y;
        if (roomDoor != null) { var r = roomDoor.GetComponent<Renderer>(); Vector3 to = (r != null ? r.bounds.center : roomDoor.position) - sp; to.y = 0f; if (to.sqrMagnitude > 0.01f) spYaw = Quaternion.LookRotation(to).eulerAngles.y; }
        Undo.RecordObject(spawn, "OnFoot spawn");
        spawn.SetPositionAndRotation(sp, Quaternion.Euler(0f, spYaw, 0f));

        // 카트 자리(차고 앞 마당)
        var cart = Child(root.transform, "OnFoot_CartSpot");
        // 차고 마당 월드 좌표(placements.yaml garage [234.1,-53.79] yaw 85.15 기준으로 잰 값). 그룹 피벗이 원점일 수 있어 월드로 둔다.
        Vector3 cp = new Vector3(229.5f, 26.7f, -55.5f);
        float cy = 25f;
        if (Physics.Raycast(cp + Vector3.up * 2f, Vector3.down, out var ch, 4f)) cp.y = ch.point.y;
        else log.Append("(카트 자리 바닥 못 찾음) ");
        Undo.RecordObject(cart, "OnFoot cart");
        cart.SetPositionAndRotation(cp, Quaternion.Euler(0f, cy, 0f));

        // 캐릭터(1.7 m 캡슐 대역)
        var player = Child(root.transform, "OnFoot_Player");
        player.gameObject.layer = 2;   // Ignore Raycast — 카메라 당김·땅 찾기가 자기 몸에 안 걸리게
        var cc = Get<CharacterController>(player.gameObject);
        cc.height = 1.7f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.85f, 0f);
        cc.stepOffset = 0.3f; cc.slopeLimit = 50f; cc.skinWidth = 0.03f; cc.minMoveDistance = 0f;
        if (player.GetComponent<OnFootPlayer>() == null) Undo.AddComponent<OnFootPlayer>(player.gameObject);
        Visual(player, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(0.6f, 0.85f, 0.6f));
        Visual(player, "Nose", PrimitiveType.Cube, new Vector3(0f, 1.5f, 0.27f), new Vector3(0.2f, 0.1f, 0.12f));
        Undo.RecordObject(player, "OnFoot player");
        player.SetPositionAndRotation(sp, spawn.rotation);

        var so = new SerializedObject(dir);
        so.FindProperty("player").objectReferenceValue = player.GetComponent<OnFootPlayer>();
        so.FindProperty("spawn").objectReferenceValue = spawn;
        so.FindProperty("cartSpot").objectReferenceValue = cart;
        so.ApplyModifiedProperties();
        log.Append(fresh ? "새로 만듦. " : "있던 것 재사용. ");
        log.Append($"시작점={sp:F2} yaw={spYaw:F0} 카트={cp:F2} yaw={cy:F0}. ");

        // 문
        Physics.SyncTransforms();
        Collider shell = house.GetComponentsInChildren<MeshCollider>(true).FirstOrDefault(c => c.name == "HOUSE_shell")
                         ?? house.GetComponentsInChildren<MeshCollider>(true).FirstOrDefault(c => !c.name.StartsWith("DOOR_"));
        var knobs = house.GetComponentsInChildren<Transform>(true).Where(k => k.name.ToUpper().StartsWith("KNOB")).ToList();
        int n = 0;
        foreach (var t in house.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("DOOR_")) continue;
            var dh = Get<DoorHinge>(t.gameObject);
            log.Append(ConfigureDoor(dh, shell, knobs)).Append(' ');
            n++;
        }
        log.Append($"문 {n}개. ");

        // 난간: 자동 BoxCollider 가 ㄱ·ㄷ자 난간의 바깥 상자라 계단 통로를 통째로 막는다 → 메시 모양 콜라이더로 바꾼다.
        int rails = 0;
        foreach (var bc in house.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!bc.name.ToUpper().StartsWith("RAIL")) continue;
            var mf = bc.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var mc = bc.GetComponent<MeshCollider>();
            if (mc == null) mc = Undo.AddComponent<MeshCollider>(bc.gameObject);
            mc.sharedMesh = mf.sharedMesh; mc.convex = false;
            Undo.RecordObject(bc, "OnFoot rail");
            bc.enabled = false;
            rails++;
        }
        log.Append($"난간 콜라이더 메시로 {rails}개. ");

        var so2 = new SerializedObject(dir);
        so2.FindProperty("gazeReach").floatValue = 2.0f;   // 옛 이름 cartReach 는 D-219 에 gazeReach 로 바뀜 — 그대로 두면 NRE 로 MarkSceneDirty 전에 죽었다
        so2.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        return log.ToString();
    }

    static Transform FindHouse()
    {
        // 바깥 house_home(배치 루트) 중 DOOR_* 를 가진 것
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "house_home" && (t.parent == null || t.parent.name != "house_home") && t.GetComponentsInChildren<Transform>(true).Any(c => c.name.StartsWith("DOOR_")))
                return t;
        return null;
    }

    // GetComponent 의 빈 값은 Unity 가짜 null 이라 ?? 가 안 먹는다 — 명시 비교.
    static T Get<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    static Transform Child(Transform root, string n)
    {
        var c = root.Find(n);
        if (c != null) return c;
        var go = new GameObject(n);
        Undo.RegisterCreatedObjectUndo(go, "OnFoot setup");
        go.transform.SetParent(root, false);
        return go.transform;
    }

    static void Visual(Transform parent, string n, PrimitiveType type, Vector3 lp, Vector3 ls)
    {
        var c = parent.Find(n);
        if (c == null)
        {
            var go = GameObject.CreatePrimitive(type);
            Undo.RegisterCreatedObjectUndo(go, "OnFoot setup");
            go.name = n;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            c = go.transform;
        }
        c.gameObject.layer = 2;
        c.localPosition = lp; c.localRotation = Quaternion.identity; c.localScale = ls;
    }

    /// <summary>문 하나의 경첩·닫힌 자세를 메시에서 찾아 DoorHinge 에 적는다. 모두 부모(집) 좌표.</summary>
    static string ConfigureDoor(DoorHinge dh, Collider shell, List<Transform> knobs)
    {
        var door = dh.transform; var par = door.parent;
        var mf = door.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return door.name + ":메시없음";
        var m = Matrix4x4.TRS(door.localPosition, door.localRotation, door.localScale);
        var pts = mf.sharedMesh.vertices.Select(v => m.MultiplyPoint3x4(v)).ToArray();
        Vector3 up = par != null ? par.InverseTransformDirection(Vector3.up).normalized : Vector3.up;

        // 수평에서 가장 긴 축 = 문 폭 방향
        Vector3 a = Vector3.right; float L = 0f;
        for (int deg = 0; deg < 180; deg += 1)
        {
            var d = Quaternion.AngleAxis(deg, up) * Vector3.right;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in pts) { float s = Vector3.Dot(p, d); lo = Mathf.Min(lo, s); hi = Mathf.Max(hi, s); }
            if (hi - lo > L) { L = hi - lo; a = d; }
        }
        Vector3 nrm = Vector3.Cross(up, a).normalized;
        float aMin = pts.Min(p => Vector3.Dot(p, a)), aMax = pts.Max(p => Vector3.Dot(p, a));
        float yMin = pts.Min(p => Vector3.Dot(p, up)), yMax = pts.Max(p => Vector3.Dot(p, up));
        var core = pts.Where(p => { float s = Vector3.Dot(p, a); return s > aMin + 0.3f * L && s < aMax - 0.3f * L; }).ToArray();
        if (core.Length == 0) core = pts;
        float nLo = core.Min(p => Vector3.Dot(p, nrm)), nHi = core.Max(p => Vector3.Dot(p, nrm));
        float nC = (nLo + nHi) * 0.5f, halfT = (nHi - nLo) * 0.5f;

        float pa = Vector3.Dot(door.localPosition, a);
        // 따로 된 손잡이(KNOB_*): 문짝 가운데 0.7 m 안·같은 높이대
        Vector3 midW = par != null ? par.TransformPoint(a * ((aMin + aMax) * 0.5f) + nrm * nC + up * ((yMin + yMax) * 0.5f)) : Vector3.zero;
        Transform knobT = knobs.Select(k => { var r = k.GetComponent<Renderer>(); return (k, c: r != null ? r.bounds.center : k.position); })
            .Where(x => { var dv = x.c - midW; return Mathf.Abs(dv.y) < 1.2f && new Vector2(dv.x, dv.z).magnitude < 0.7f; })
            .OrderBy(x => (x.c - midW).sqrMagnitude).Select(x => x.k).FirstOrDefault();
        bool hingeAtMin; string how;
        if (knobT != null && par != null)
        {
            var kr = knobT.GetComponent<Renderer>();
            float ka = Vector3.Dot(par.InverseTransformPoint(kr != null ? kr.bounds.center : knobT.position), a);
            hingeAtMin = ka > (aMin + aMax) * 0.5f; how = "손잡이 " + knobT.name;
        }
        else if (pa - aMin < 0.15f * L) { hingeAtMin = true; how = "피벗"; }
        else if (aMax - pa < 0.15f * L) { hingeAtMin = false; how = "피벗"; }
        else
        {
            var knob = pts.Where(p => Mathf.Abs(Vector3.Dot(p, nrm) - nC) > halfT + 0.008f).ToArray();
            if (knob.Length > 0) { float ka = knob.Average(p => Vector3.Dot(p, a)); hingeAtMin = ka > (aMin + aMax) * 0.5f; how = "손잡이"; }
            else { hingeAtMin = true; how = "추정"; }
        }
        float endA = hingeAtMin ? aMin : aMax;
        Vector3 hinge = a * endA + nrm * nC + up * yMin;
        Vector3 leaf = hingeAtMin ? a : -a;
        Vector3 center = a * ((aMin + aMax) * 0.5f) + nrm * nC + up * ((yMin + yMax) * 0.5f);

        // 문틀 맞춤: 문 끝이 반대편 문설주에, 경첩 뒤가 벽에 닿는 각도(작은 각부터)
        // 1차: 먼 문설주 + 경첩 뒤 벽. 2차(경첩 쪽이 유리·틀이라 벽이 없는 문): 먼 문설주 면이 세 높이에서 같은 거리·마주 보는 면.
        float fit = 0f; bool found = false; string pass = "";
        if (shell != null && par != null)
        {
            Vector3 hW0 = par.TransformPoint(hinge);
            for (int stage = 1; stage <= 2 && !found; stage++)
                for (int k = 0; k <= 80 && !found; k++)
                {
                    float th = k == 0 ? 0f : (k % 2 == 1 ? 1 : -1) * ((k + 1) / 2) * 2.5f;
                    Vector3 dW = par.TransformDirection(Quaternion.AngleAxis(th, up) * leaf).normalized;
                    bool ok = true; float d0 = -1f;
                    foreach (float hy in stage == 1 ? new[] { 1.0f } : new[] { 0.5f, 1.0f, 1.8f })
                    {
                        Vector3 o = hW0 + Vector3.up * hy + dW * 0.06f;
                        if (!shell.Raycast(new Ray(o, dW), out var h1, L + 0.35f)) { ok = false; break; }
                        float dist = h1.distance + 0.06f;
                        if (dist < L - 0.1f || dist > L + 0.25f || (stage == 2 && Vector3.Dot(h1.normal, dW) > -0.7f)) { ok = false; break; }
                        if (d0 < 0f) d0 = dist; else if (Mathf.Abs(dist - d0) > 0.05f) { ok = false; break; }
                        if (stage == 1 && !shell.Raycast(new Ray(o, -dW), out _, 0.3f)) { ok = false; break; }
                    }
                    if (ok) { fit = th; found = true; pass = stage == 1 ? "" : "(문설주만)"; }
                }
        }
        Quaternion R = Quaternion.AngleAxis(fit, up);
        Undo.RecordObject(dh, "OnFoot door");
        dh.hingeLocal = hinge;
        dh.closedLocalPos = hinge + R * (door.localPosition - hinge);
        dh.closedLocalRot = R * door.localRotation;
        dh.centerLocal = hinge + R * (center - hinge);
        dh.leafDirLocal = R * leaf;
        dh.fitYaw = fit;
        dh.riders = knobT != null ? new[] { knobT } : new Transform[0];
        dh.configured = true;
        EditorUtility.SetDirty(dh);
        return $"{door.name}(폭{L:F2} 경첩={how} 맞춤={(found ? fit.ToString("F1") + "°" + pass : "없음")})";
    }
}
