#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// D-275 세계 모델 1층 — 「지금 세계 상태」 스냅숏. 에디트 모드에서 열린 씬(Map_TownV6)을 읽어 사람·LLM 이 읽는 마크다운을 돌려준다.
/// 쓰는 곳: scripts/world_snapshot.sh 가 `ucli-http.sh exec "return WorldSnapshot.Dump();"` 로 받아 docs/world/WORLD.md 에 쓴다.
/// 읽기 전용 — 씬을 바꾸지 않는다. Play 불필요. 내용: 장소(DebugPlaces) · 배달 지점(DP_*) · 상호작용 물건([E] 칩을 내는 컴포넌트) ·
/// 흐름 단계(OnFootDirector 의 enum) · 숨긴 대역 오브젝트 수.
/// </summary>
public static class WorldSnapshot
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    /// <summary>[E] 칩을 내는 컴포넌트들(OnFootDirector.Update 의 시선 분기 + 디렉터가 잇는 슬롯). 새 상호작용 물건을 만들면 여기 한 줄.</summary>
    static readonly string[] InteractableTypes = { "DoorHandover", "DepotOrderBoard", "DepotPackSpot", "FishingSpot", "RoomBed", "RoomCalendar", "PackBench", "DeliveryPoint", "ArcadeCarController" };
    /// <summary>대역(임시) 오브젝트로 치는 이름 조각 — WorldInvariants 와 같은 표.</summary>
    public static readonly string[] PlaceholderMarks = { "Placeholder", "placeholder", "대역", "_tmp", "tmp_", "TMP_", "Dummy" };

    static string V(Vector3 p) => string.Format(Inv, "({0:0.0}, {1:0.0}, {2:0.0})", p.x, p.y, p.z);

    public static string Dump()
    {
        var sb = new StringBuilder();
        var scene = SceneManager.GetActiveScene();
        sb.Append("# 세계 스냅숏 — ").Append(scene.name).Append('\n');
        sb.Append("에디터 ").Append(Application.unityVersion).Append(" · 에디트 모드 ").Append(Application.isPlaying ? "아님(Play 중!)" : "맞음")
          .Append(" · 씬 dirty=").Append(scene.isDirty).Append(" · 루트 오브젝트 ").Append(scene.rootCount).Append('\n');
        if (scene.name != "Map_TownV6") sb.Append("> ⚠ 열린 씬이 Map_TownV6 가 아니다 — 아래 내용은 이 씬 기준.\n");
        sb.Append('\n');
        try { Places(sb); } catch (Exception e) { sb.Append("(장소 오류: ").Append(e.Message).Append(")\n"); }
        try { DeliveryPoints(sb); } catch (Exception e) { sb.Append("(배달 지점 오류: ").Append(e.Message).Append(")\n"); }
        try { Interactables(sb); } catch (Exception e) { sb.Append("(상호작용 오류: ").Append(e.Message).Append(")\n"); }
        try { Flow(sb); } catch (Exception e) { sb.Append("(흐름 오류: ").Append(e.Message).Append(")\n"); }
        try { Placeholders(sb); } catch (Exception e) { sb.Append("(대역 오류: ").Append(e.Message).Append(")\n"); }
        try { Misc(sb); } catch (Exception e) { sb.Append("(기타 오류: ").Append(e.Message).Append(")\n"); }
        return sb.ToString();
    }

    /// <summary>DebugPlaces.All 전부 — 디버그 창 순간이동 자리. 땅 높이는 레이로 재서 표 y 와의 차이를 같이 적는다.</summary>
    static void Places(StringBuilder sb)
    {
        sb.Append("## 장소 (DebugPlaces.All — 디버그 창 순간이동 자리)\n");
        sb.Append("| 이름 | 발 좌표 | yaw | 카트 규칙 | 땅(레이) | 메모 |\n|---|---|---|---|---|---|\n");
        foreach (var p in DebugPlaces.All)
        {
            string ground = "-";
            if (p.feet != Vector3.zero)
                ground = Physics.Raycast(p.feet + Vector3.up * 1.5f, Vector3.down, out var h, 6.5f, ~0, QueryTriggerInteraction.Ignore)
                    ? string.Format(Inv, "y {0:0.00} ({1}) Δ{2:+0.00;-0.00}", h.point.y, h.collider.name, h.point.y - p.feet.y) : "⚠ 없음(6.5m)";
            string car = p.car == DebugPlaces.CarRule.Fixed ? "Fixed " + V(p.carAt) + string.Format(Inv, " yaw {0:0}", p.carYaw) : p.car.ToString();
            sb.Append("| ").Append(p.name).Append(" | ").Append(V(p.feet)).Append(" | ").Append(p.yaw.ToString("0", Inv)).Append(" | ").Append(car)
              .Append(" | ").Append(ground).Append(" | ").Append(p.note.Replace("|", "/")).Append(" |\n");
        }
        sb.Append("보관소 앞 카트 자리 DepotCart = ").Append(V(DebugPlaces.DepotCart)).Append("\n\n");
    }

    /// <summary>이름이 DP_ 로 시작하는 오브젝트 = 배달 지점. 위치·활성·DeliveryPoint 컴포넌트·문 앞 대역(DoorHandover) 유무·땅.</summary>
    static void DeliveryPoints(StringBuilder sb)
    {
        sb.Append("## 배달 지점 (DP_* — 목적지 깃발 앵커는 DotScreenUI.DestAnchorOf = 위치 + 3 m, 씬 오브젝트 아님)\n");
        sb.Append("| 이름 | 위치 | 활성 | DeliveryPoint | 라벨 | 문 앞 대역(DoorHandover ≤12 m) | 땅(레이) |\n|---|---|---|---|---|---|---|\n");
        var doors = UnityEngine.Object.FindObjectsByType<DoorHandover>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var list = new List<Transform>();
        foreach (var t in all) if (t.name.StartsWith("DP_", StringComparison.Ordinal)) list.Add(t);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        foreach (var t in list)
        {
            var dp = t.GetComponent<DeliveryPoint>();
            string label = dp != null ? (string.IsNullOrEmpty(dp.Label) ? "(비어 있음→이름)" : dp.Label) : "-";
            string door = "없음";
            foreach (var d in doors)
            {
                if (d == null) continue;
                var a = d.transform.position; var b = t.position; a.y = b.y = 0f;
                float m = Vector3.Distance(a, b);
                if (m <= 12f) { door = string.Format(Inv, "{0} {1:0.0} m{2}", d.name, m, d.gameObject.activeInHierarchy ? "" : "(꺼짐)"); break; }
            }
            string ground = Physics.Raycast(t.position + Vector3.up * 1.5f, Vector3.down, out var h, 6.5f, ~0, QueryTriggerInteraction.Ignore)
                ? string.Format(Inv, "y {0:0.00} Δ{1:+0.00;-0.00}", h.point.y, h.point.y - t.position.y) : "⚠ 없음";
            sb.Append("| ").Append(t.name).Append(" | ").Append(V(t.position)).Append(" | ").Append(t.gameObject.activeInHierarchy ? "켜짐" : "꺼짐")
              .Append(" | ").Append(dp != null ? "있음" : "⚠ 없음").Append(" | ").Append(label).Append(" | ").Append(door).Append(" | ").Append(ground).Append(" |\n");
        }
        if (list.Count == 0) sb.Append("| (DP_ 오브젝트 없음) |\n");
        sb.Append('\n');
    }

    /// <summary>[E] 칩을 내는 컴포넌트(InteractableTypes) 전부 — 이름·위치·활성. 사장(boss)은 OnFootDirector 의 비공개 슬롯이라 SerializedObject 로 읽는다.</summary>
    static void Interactables(StringBuilder sb)
    {
        sb.Append("## 상호작용 물건 ([E] 칩을 내는 컴포넌트)\n| 종류 | 오브젝트 | 위치 | 활성 |\n|---|---|---|---|\n");
        var want = new HashSet<string>(InteractableTypes);
        var rows = new List<string>();
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string tn = mb.GetType().Name;
            if (!want.Contains(tn)) continue;
            rows.Add("| " + tn + " | " + Path(mb.transform) + " | " + V(mb.transform.position) + " | " + (mb.gameObject.activeInHierarchy && mb.enabled ? "켜짐" : "꺼짐") + " |");
        }
        rows.Sort(StringComparer.Ordinal);
        foreach (var r in rows) sb.Append(r).Append('\n');
#if UNITY_EDITOR
        var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(FindObjectsInactive.Include);
        if (dir != null)
        {
            var so = new UnityEditor.SerializedObject(dir);
            foreach (var slot in new[] { "boss", "spawn", "cartSpot", "firstDrop", "orderBoard", "packSpot", "depotDest", "handover", "calendar", "bed" })
            {
                var prop = so.FindProperty(slot);
                var o = prop != null ? prop.objectReferenceValue : null;
                var tr = o as Transform ?? (o as Component)?.transform;
                sb.Append("| 디렉터 슬롯 ").Append(slot).Append(" | ").Append(o == null ? "(비어 있음)" : Path(tr)).Append(" | ").Append(tr != null ? V(tr.position) : "-")
                  .Append(" | ").Append(tr != null ? (tr.gameObject.activeInHierarchy ? "켜짐" : "꺼짐") : "-").Append(" |\n");
            }
        }
        else sb.Append("| ⚠ OnFootDirector 없음 | | | |\n");
#endif
        sb.Append('\n');
    }

    /// <summary>OnFootDirector 안의 enum 전부(Mode 등) — 흐름 단계 이름 열거. 리플렉션이라 enum 을 더해도 자동으로 잡힌다.</summary>
    static void Flow(StringBuilder sb)
    {
        sb.Append("## 흐름 단계 (OnFootDirector 의 enum)\n");
        foreach (var t in typeof(OnFootDirector).GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            if (t.IsEnum) sb.Append("- ").Append(t.Name).Append(": ").Append(string.Join(" → ", Enum.GetNames(t))).Append('\n');
        var dir = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(FindObjectsInactive.Include);
        if (dir != null) sb.Append("- 지금 State = ").Append(dir.State).Append(" (에디트 모드면 Starting)\n");
        sb.Append('\n');
    }

    /// <summary>이름에 Placeholder/대역/_tmp 등이 든 오브젝트 — 켜진 것은 세계에 남은 대역(불변 조건 위반 후보).</summary>
    static void Placeholders(StringBuilder sb)
    {
        int on = 0, off = 0; var onNames = new List<string>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsPlaceholderName(t.name)) continue;
            if (t.gameObject.activeInHierarchy) { on++; if (onNames.Count < 20) onNames.Add(Path(t) + " " + V(t.position)); } else off++;
        }
        sb.Append("## 대역 오브젝트 (이름에 ").Append(string.Join("/", PlaceholderMarks)).Append(")\n");
        sb.Append("- 숨긴(꺼진) 대역: ").Append(off).Append(" · 켜진 대역: ").Append(on).Append(on > 0 ? " ⚠" : "").Append('\n');
        foreach (var n in onNames) sb.Append("  - ").Append(n).Append('\n');
        var hold = GameObject.Find("_TUT_HOLD");
        var holdT = hold == null ? FindInactive("_TUT_HOLD") : hold.transform;
        if (holdT != null) sb.Append("- _TUT_HOLD(보류 튜토리얼 물건, D-201): 자식 ").Append(holdT.childCount).Append(" · ").Append(holdT.gameObject.activeInHierarchy ? "⚠ 켜짐" : "꺼짐").Append('\n');
        sb.Append('\n');
    }

    static void Misc(StringBuilder sb)
    {
        sb.Append("## 기타\n");
        int cams = 0; foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c.isActiveAndEnabled) cams++;
        sb.Append("- 켜진 카메라: ").Append(cams).Append('\n');
        var rails = GameObject.Find("PierRails");
        sb.Append("- 부두 난간 PierRails: ").Append(rails == null ? "⚠ 없음" : "자식 콜라이더 " + rails.GetComponentsInChildren<Collider>(true).Length + (rails.GetComponent<WalkOnlyRail>() != null ? " · WalkOnlyRail 있음" : " · ⚠ WalkOnlyRail 없음")).Append('\n');
        sb.Append("- HeldPaper(손 전표·칠판 쪽지): ").Append(UnityEngine.Object.FindObjectsByType<HeldPaper>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length).Append('\n');
        var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
        sb.Append("- NavMesh 정점: ").Append(tri.vertices != null ? tri.vertices.Length : 0).Append(tri.vertices == null || tri.vertices.Length == 0 ? " (없음 — 땅 검사는 레이캐스트로)" : "").Append('\n');
    }

    public static bool IsPlaceholderName(string n)
    {
        foreach (var m in PlaceholderMarks) if (n.IndexOf(m, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    static Transform FindInactive(string name)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (t.name == name) return t;
        return null;
    }

    /// <summary>계층 경로(부모/자식) — 같은 이름이 여럿일 때 구분.</summary>
    public static string Path(Transform t)
    {
        if (t == null) return "-";
        var s = t.name; int guard = 0;
        for (var p = t.parent; p != null && guard++ < 6; p = p.parent) s = p.name + "/" + s;
        return s;
    }
}
#endif
