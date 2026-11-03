#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// D-275 세계 모델 1층 — 불변 조건 검사. 과제가 끝날 때 하네스(scripts/world_check.sh → team_queue.sh)가 에디트 모드에서
/// `return WorldInvariants.Check();` 로 부른다. 결과: "OK n/n" 또는 "FAIL k/n" + 위반 줄(조건 · 대상 · 좌표 · 고치는 힌트).
/// 읽기 전용(씬을 바꾸지 않음). 콘솔 에러 0 은 tools/evidence/bundle.sh 가 보니 여기서 안 센다.
/// 조건을 더하려면 아래 Checks 배열에 (이름, 함수) 한 줄 — 함수는 위반 목록을 돌려준다(빈 목록 = 통과). docs/world/README.md.
/// </summary>
public static class WorldInvariants
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static string V(Vector3 p) => string.Format(Inv, "({0:0.0}, {1:0.0}, {2:0.0})", p.x, p.y, p.z);

    /// <summary>켜진 채로 있어도 되는 대역 이름(정확히 일치). 의도된 대역을 여기 적는다.</summary>
    public static readonly string[] PlaceholderAllow = { };

    /// <summary>아직 실제 씬에서 한 번도 통과를 못 본 조건 — 위반해도 FAIL 이 아니라 WARN(과제 보류 안 함). 한 번 OK 를 보면 여기서 뺀다.</summary>
    public static readonly HashSet<string> Soft = new HashSet<string> { "보관소 출구" };

    static readonly (string name, Func<List<string>> run)[] Checks =
    {
        ("DP 땅 2m 안", () => DpGround()),
        ("장소 땅 5m 안", () => PlacesGround()),
        ("부두 난간", () => PierRails()),
        ("칠판 1개", () => OneBoard()),
        ("손 전표 ≤1", () => HeldPapers()),
        ("대역 켜짐 0", () => NoActivePlaceholders()),
        ("카메라 ≥1", () => Cameras()),
        ("DP 컴포넌트·트리거", () => DpComponents()),
        ("보관소 출구", () => DepotExit()),
        ("디렉터 1개", () => OneDirector()),
        ("첫 배달 한 바퀴 배선", () => FirstLoopWiring()),
    };

    public static string Check()
    {
        var sb = new StringBuilder(); int pass = 0, n = Checks.Length;
        var fails = new List<string>(); var warns = new List<string>();
        foreach (var c in Checks)
        {
            List<string> bad;
            try { bad = c.run() ?? new List<string>(); }
            catch (Exception e) { bad = new List<string> { "검사 자체 오류: " + e.GetType().Name + " " + e.Message }; }
            if (bad.Count == 0) { pass++; continue; }
            if (Soft.Contains(c.name)) { pass++; foreach (var b in bad) warns.Add("- WARN [" + c.name + "] " + b); continue; }
            foreach (var b in bad) fails.Add("- [" + c.name + "] " + b);
        }
        sb.Append(fails.Count == 0 ? "OK " : "FAIL ").Append(pass).Append('/').Append(n).Append('\n');
        foreach (var f in fails) sb.Append(f).Append('\n');
        foreach (var w in warns) sb.Append(w).Append('\n');
        return sb.ToString().TrimEnd();
    }

    // ───────── 도우미 ─────────
    static bool HasNavMesh() { var t = UnityEngine.AI.NavMesh.CalculateTriangulation(); return t.vertices != null && t.vertices.Length > 0; }
    static bool GroundBelow(Vector3 p, float down, out float y)
    {
        y = 0f;
        if (!Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out var h, down + 1.5f, ~0, QueryTriggerInteraction.Ignore)) return false;
        y = h.point.y; return true;
    }
    static List<Transform> DpTransforms(bool activeOnly)
    {
        var l = new List<Transform>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("DP_", StringComparison.Ordinal) && (!activeOnly || t.gameObject.activeInHierarchy)) l.Add(t);
        return l;
    }
    static DebugPlaces.Place? FindPlace(string name) { foreach (var p in DebugPlaces.All) if (p.name == name) return p; return null; }

    // ───────── 조건 ─────────
    /// <summary>모든 켜진 DP_ 배달 지점: NavMesh 가 있으면 SamplePosition 2 m, 없으면 아래 레이 5 m 로 땅이 있다(카트가 설 수 있는 자리).</summary>
    static List<string> DpGround()
    {
        var bad = new List<string>(); bool nav = HasNavMesh();
        foreach (var t in DpTransforms(true))
        {
            bool ok = nav ? UnityEngine.AI.NavMesh.SamplePosition(t.position, out _, 2f, UnityEngine.AI.NavMesh.AllAreas) : GroundBelow(t.position, 5f, out _);
            if (!ok) bad.Add(t.name + " " + V(t.position) + (nav ? " — NavMesh 2 m 안 없음" : " — 아래 5 m 에 콜라이더 없음(허공/바다 위)"));
        }
        return bad;
    }

    /// <summary>DebugPlaces 장소 전부(카트 옆 제외)와 Fixed 카트 자리: 발 아래 5 m 안 콜라이더. 디버그 창 순간이동이 허공에 떨어지지 않게.</summary>
    static List<string> PlacesGround()
    {
        var bad = new List<string>();
        foreach (var p in DebugPlaces.All)
        {
            if (p.feet == Vector3.zero) continue;
            if (!GroundBelow(p.feet, 5f, out float y)) bad.Add("장소 '" + p.name + "' " + V(p.feet) + " — 아래 5 m 땅 없음");
            else if (Mathf.Abs(y - p.feet.y) > 2.5f) bad.Add("장소 '" + p.name + "' " + V(p.feet) + string.Format(Inv, " — 표 y 와 땅 y {0:0.0} 차 {1:0.0} m(표 좌표 낡음?)", y, y - p.feet.y));
            if (p.car == DebugPlaces.CarRule.Fixed && !GroundBelow(p.carAt, 5f, out _)) bad.Add("장소 '" + p.name + "' 카트 자리 " + V(p.carAt) + " — 아래 5 m 땅 없음");
        }
        return bad;
    }

    /// <summary>부두(WalkOnlyRail 이 있는 PierRails) 끝 난간: 콜라이더가 있고, 부두 장소(낚시 자리·항구)에서 보는 방향 8 m 안에 난간 상자가 걸린다.
    /// 콜라이더 enabled 는 안 본다(Play 중 WalkOnlyRail 이 켜고 끔) — 월드 Bounds 로 선을 긋는다.</summary>
    static List<string> PierRails()
    {
        var bad = new List<string>();
        var rails = UnityEngine.Object.FindObjectsByType<WalkOnlyRail>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (rails.Length == 0) { bad.Add("WalkOnlyRail 오브젝트 없음 — 메뉴 F1/부두 난간 만들기"); return bad; }
        var boxes = new List<Bounds>();
        foreach (var r in rails) foreach (var c in r.GetComponentsInChildren<BoxCollider>(true)) boxes.Add(c.bounds);
        if (boxes.Count == 0) { bad.Add("PierRails 에 자식 BoxCollider 0 — 메뉴 F1/부두 난간 만들기"); return bad; }
        foreach (var name in new[] { "노을 낚시 자리", "항구" })
        {
            var p = FindPlace(name); if (p == null) continue;
            var ray = new Ray(p.Value.feet + Vector3.up * 0.6f, Quaternion.Euler(0f, p.Value.yaw, 0f) * Vector3.forward);
            bool hit = false;
            foreach (var b in boxes) { if (b.IntersectRay(ray, out float d) && d <= 8f) { hit = true; break; } }
            if (!hit) bad.Add("장소 '" + name + "' " + V(p.Value.feet) + string.Format(Inv, " yaw {0:0} 앞 8 m 안에 난간 없음(부두 끝 추락)", p.Value.yaw));
        }
        return bad;
    }

    static List<string> OneBoard()
    {
        var bad = new List<string>();
        var bs = UnityEngine.Object.FindObjectsByType<DepotOrderBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (bs.Length != 1) { bad.Add("DepotOrderBoard " + bs.Length + "개(1개여야)"); foreach (var b in bs) bad.Add("  " + WorldSnapshot.Path(b.transform) + " " + V(b.transform.position)); }
        return bad;
    }

    /// <summary>손에 든 전표(HeldPaper 중 칠판에 꽂힌 Pinned 가 아닌 것) ≤ 1 — 둘 이상이면 이전 과제가 지우지 않은 것.</summary>
    static List<string> HeldPapers()
    {
        var bad = new List<string>(); var held = new List<HeldPaper>();
        foreach (var h in UnityEngine.Object.FindObjectsByType<HeldPaper>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (h.State != HeldPaper.Pose.Pinned) held.Add(h);
        if (held.Count > 1) foreach (var h in held) bad.Add("HeldPaper " + WorldSnapshot.Path(h.transform) + " " + V(h.transform.position) + " (" + h.State + ") — 손 전표 " + held.Count + "개");
        return bad;
    }

    /// <summary>이름에 Placeholder/대역/_tmp/TMP_/Dummy 가 든 오브젝트가 켜진 채 남아 있지 않다(PlaceholderAllow 제외).</summary>
    static List<string> NoActivePlaceholders()
    {
        var bad = new List<string>(); var allow = new HashSet<string>(PlaceholderAllow);
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (WorldSnapshot.IsPlaceholderName(t.name) && t.gameObject.activeInHierarchy && !allow.Contains(t.name))
                bad.Add("대역 켜짐 " + WorldSnapshot.Path(t) + " " + V(t.position) + " — 끄거나 지우거나 PlaceholderAllow 에");
        return bad;
    }

    static List<string> Cameras()
    {
        int n = 0; foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c.isActiveAndEnabled) n++;
        return n >= 1 ? new List<string>() : new List<string> { "켜진 카메라 0" };
    }

    /// <summary>DP_ 오브젝트마다 DeliveryPoint + SphereCollider(트리거) — 없으면 도착 판정도, 깃발(DestAnchorOf(dp))도 못 그린다.</summary>
    static List<string> DpComponents()
    {
        var bad = new List<string>();
        foreach (var t in DpTransforms(false))
        {
            if (t.GetComponent<DeliveryPoint>() == null) bad.Add(t.name + " " + V(t.position) + " — DeliveryPoint 없음(깃발 앵커·도착 판정 불가)");
            else if (t.GetComponent<SphereCollider>() == null) bad.Add(t.name + " " + V(t.position) + " — SphereCollider(트리거) 없음");
        }
        return bad;
    }

    /// <summary>택배 보관소 출구: 칠판 앞 → 보관소 앞 마당(DebugPlaces) 사이를 가슴 높이(0.9 m)·무릎 높이(0.3 m)로 선을 그어 막는 콜라이더가 없다(트리거 제외).</summary>
    static List<string> DepotExit()
    {
        var bad = new List<string>();
        var a = FindPlace("택배 보관소 칠판"); var b = FindPlace("택배 보관소 앞");
        if (a == null || b == null) return bad;
        foreach (float h in new[] { 0.3f, 0.9f })
        {
            var from = a.Value.feet + Vector3.up * h; var to = b.Value.feet + Vector3.up * h;
            if (Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore))
                bad.Add(string.Format(Inv, "높이 {0:0.0} m 선 {1}→{2} 을 '{3}' 이 막음 {4}", h, V(from), V(to), WorldSnapshot.Path(hit.collider.transform), V(hit.point)));
        }
        return bad;
    }

    static List<string> OneDirector()
    {
        int n = UnityEngine.Object.FindObjectsByType<OnFootDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        return n == 1 ? new List<string>() : new List<string> { "OnFootDirector " + n + "개(1개여야)" };
    }

    /// <summary>
    /// 첫 과제 01-first-loop(SPEC §6·§11): 방 → 칠판 → 작업대 → 찻집 문 → 노을 → 침대가 한 줄로 이어지는 배선.
    /// 감독에 칠판·작업대·문 앞·첫 배달지·침대가 꽂혀 있고, 카트 타기가 사장 대화에 묶이지 않으며(§11-7), 노을 장치(SunsetLook)가 있다(§11-3).
    /// 사장 게이트가 되살아나거나 노을이 빠지면 Play 를 안 해도 여기서 잡힌다.
    /// </summary>
    static List<string> FirstLoopWiring()
    {
        var bad = new List<string>();
        var ds = UnityEngine.Object.FindObjectsByType<OnFootDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (ds.Length == 0) return bad;   // 감독 없는 씬은 대상 아님
        var d = ds[0];
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(d);
        foreach (var f in new[] { "orderBoard", "packSpot", "handover", "firstDrop", "bed", "spawn", "cartSpot" })
        {
            var p = so.FindProperty(f);
            if (p == null || p.objectReferenceValue == null) bad.Add("OnFootDirector." + f + " 비어 있음 — 한 바퀴가 끊긴다");
        }
#endif
        if (d.DepotFlow && d.CartNeedsBossTalk) bad.Add("보관소 흐름인데 카트가 사장 대화를 기다린다(SPEC §11-7 위반)");
        if (UnityEngine.Object.FindFirstObjectByType<SunsetLook>(FindObjectsInactive.Include) == null) bad.Add("SunsetLook 없음 — 건넨 뒤 노을이 안 온다(SPEC §11-3)");
        return bad;
    }
}
#endif
