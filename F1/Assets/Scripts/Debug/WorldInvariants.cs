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
        ("상자 든 채 카트 칩", () => CartChipWithBox()),
        ("길 표지판 꺼짐", () => RoadSignOff()),
        ("아침 카트는 정비소 앞", () => MorningCartAtGarage()),
        ("B 후진 ≥ 전진의 50%", () => ReverseNotCrawling()),
        ("스크립트 Missing 0", () => NoMissingScripts()),
        ("world.yaml 좌표 ≤0.5m", () => YamlVsScene()),
        ("포장대 선반 물건 5개·칩 0", () => PackV3Shelf()),
        ("포장 v3 물건·상자 연속 충돌(들면 안 빠짐)", () => PackV3Continuous()),
        ("들린 물건 Rigidbody 보간 꺼짐(걸을 때 따라옴)", () => HeldFollowsHand()),
        ("포장 2D 보드 조각(상자3·롤2·테이프·물건)·판 안·안 겹침", () => PackBoard2D()),
    };

    /// <summary>
    /// D-294 포장 2D ①: use2D 가 켜져 있으면 보드 조각 = world.yaml 상자 수 + 롤 수 + 테이프 1 + 물건 1, 전부 판(±1.0×±0.42) 안, 서로·가운데 빈 자리와 안 겹침,
    /// 판 틀(위·오른쪽)은 수평·직각. 조각이 판 밖으로 나가거나 겹치면(= 끌어서 집을 수 없다) 여기서 잡힌다. 임시 PackBoard 를 만들어 보고 지운다.
    /// </summary>
    static List<string> PackBoard2D()
    {
        var bad = new List<string>();
        var sp = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>(FindObjectsInactive.Include);
        var t = UnityEngine.Object.FindFirstObjectByType<PackTable2>(FindObjectsInactive.Include);
        if (sp == null || t == null || !sp.Use2D) return bad;
        var go = new GameObject("PackBoard2D_check") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var b = go.AddComponent<PackBoard>();
            b.Build(t);
            var pk = WorldData.Orders != null ? WorldData.Orders.packing : null;
            int nBox = pk != null && pk.boxes.Count > 0 ? pk.boxes.Count : 3, nRoll = pk != null && pk.fillers.Count > 0 ? pk.fillers.Count : 2;
            if (b.Count(PackPieceKind.Box) != nBox || b.Count(PackPieceKind.Roll) != nRoll || b.Count(PackPieceKind.Tape) != 1 || b.Count(PackPieceKind.Parcel) != 1)
                bad.Add($"조각 상자{b.Count(PackPieceKind.Box)}/{nBox}·롤{b.Count(PackPieceKind.Roll)}/{nRoll}·테이프{b.Count(PackPieceKind.Tape)}/1·물건{b.Count(PackPieceKind.Parcel)}/1");
            if (Mathf.Abs(b.Up.y) > 1e-3f || Mathf.Abs(b.Right.y) > 1e-3f || Mathf.Abs(Vector3.Dot(b.Up, b.Right)) > 1e-3f) bad.Add($"판 틀이 수평·직각이 아님 up={V(b.Up)} right={V(b.Right)}");
            var ps = b.pieces;
            for (int i = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                if (Mathf.Abs(p.homeU) + p.w * 0.5f > PackBoard.HalfW || Mathf.Abs(p.homeV) + p.d * 0.5f > PackBoard.HalfH) bad.Add($"{p.name} 판 밖 ({p.homeU:F2},{p.homeV:F2})");
                if (Overlap(p.homeU, p.homeV, p.w, p.d, PackBoard.SlotU, PackBoard.SlotV, PackBoard.SlotW, PackBoard.SlotD)) bad.Add($"{p.name} 가운데 빈 자리와 겹침");
                for (int j = i + 1; j < ps.Count; j++)
                    if (Overlap(p.homeU, p.homeV, p.w, p.d, ps[j].homeU, ps[j].homeV, ps[j].w, ps[j].d)) bad.Add($"{p.name} ↔ {ps[j].name} 겹침");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        return bad;
    }

    static bool Overlap(float u1, float v1, float w1, float d1, float u2, float v2, float w2, float d2) =>
        Mathf.Abs(u1 - u2) < (w1 + w2) * 0.5f && Mathf.Abs(v1 - v2) < (d1 + d2) * 0.5f;

    /// <summary>
    /// D-290 버그: 상자를 들 때 물건이 바닥판을 뚫고 바닥으로 빠졌다(흔든 뒤 Ready 안 켜짐).
    /// 받는 곳 물건·포장대 상자 Rigidbody 는 Discrete 이면 안 된다.
    /// </summary>
    static List<string> PackV3Continuous()
    {
        var bad = new List<string>();
        var t = UnityEngine.Object.FindFirstObjectByType<PackTable2>(FindObjectsInactive.Include);
        if (t == null) return bad;
        var bodies = new List<Rigidbody>();
        if (t.boxes != null) foreach (var b in t.boxes) if (b != null) bodies.Add(b.GetComponent<Rigidbody>());
        if (t.receive != null && t.receive.today != null) foreach (var p in t.receive.today) if (p != null) bodies.Add(p.GetComponent<Rigidbody>());
        foreach (var rb in bodies)
            if (rb == null) bad.Add("Rigidbody 없음");
            else if (rb.collisionDetectionMode == CollisionDetectionMode.Discrete) bad.Add($"{rb.name} 충돌 감지 Discrete(들면 물건이 빠진다)");
        return bad;
    }

    /// <summary>
    /// D-293 버그: 손(카메라 자식)에 붙인 물건이 걸을 때 따라오지 않고 선반 자리에 남았다 — 보간(Interpolate) 켜진 Rigidbody 가 월드 위치를 되돌렸다.
    /// 받는 곳 물건·포장대 상자를 실제 집기 준비(FreezeForHand)에 넣어 보간이 꺼지는지 본다(되돌려 놓고). 걷기 실측은 DayLoopCheck.BeginWalk.
    /// </summary>
    static List<string> HeldFollowsHand()
    {
        var bad = new List<string>();
        var t = UnityEngine.Object.FindFirstObjectByType<PackTable2>(FindObjectsInactive.Include);
        if (t == null) return bad;
        var things = new List<Transform>();
        if (t.boxes != null) foreach (var b in t.boxes) if (b != null) things.Add(b.transform);
        if (t.receive != null && t.receive.today != null) foreach (var p in t.receive.today) if (p != null) things.Add(p.transform);
        var saved = new List<(Rigidbody rb, RigidbodyInterpolation interp)>();
        foreach (var x in things)
        {
            var body = x.GetComponent<Rigidbody>();
            bool kin = body != null && body.isKinematic;
            PackTable2.FreezeForHand(x, saved);
            if (body == null) bad.Add($"{x.name} Rigidbody 없음");
            else if (body.interpolation != RigidbodyInterpolation.None) bad.Add($"{x.name} 들 때 보간 {body.interpolation} 그대로(걸으면 따라오지 않는다)");
            foreach (var (rb, interp) in saved) if (rb != null) { rb.interpolation = interp; rb.detectCollisions = true; }
            if (body != null) body.isKinematic = kin;
        }
        return bad;
    }

    /// <summary>
    /// D-290 포장 v3(SPEC §6-4): 포장대 선반 = 상자 3(소·중·대) + 롤 2(뽁뽁이·종이), 전부 켜져 있고 시선이 맞을 콜라이더가 있고 작업면에서 2.2 m 안.
    /// 안내 0 — 어떤 물건을 봐도 칩 글이 비어 있다(「E 」 칩이 되살아나면 여기서 잡힌다). 받는 곳 선반에 오늘 물건이 1개 이상.
    /// </summary>
    static List<string> PackV3Shelf()
    {
        var bad = new List<string>();
        var ts = UnityEngine.Object.FindObjectsByType<PackTable2>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (ts.Length != 1) { bad.Add($"PackTable2 {ts.Length}개(1개여야) — PitCrew/World/Build Pack v3"); return bad; }
        var t = ts[0];
        var things = new List<PackThing>();
        if (t.boxes != null) foreach (var b in t.boxes) if (b != null) things.Add(b);
        if (t.rolls != null) foreach (var r in t.rolls) if (r != null) things.Add(r);
        int nb = t.boxes == null ? 0 : t.boxes.Length, nr = t.rolls == null ? 0 : t.rolls.Length;
        if (nb != 3 || nr != 2 || things.Count != 5) bad.Add($"포장대 선반 상자 {nb}·롤 {nr}(3·2 여야), 빈 칸 포함 {things.Count}/5");
        Vector3 ws = t.workSpot != null ? t.workSpot.position : t.transform.position;
        foreach (var x in things)
        {
            if (!x.gameObject.activeInHierarchy) bad.Add($"{x.name} 꺼짐");
            if (x.GetComponentInChildren<Collider>() == null) bad.Add($"{x.name} 콜라이더 없음(시선이 못 맞힘)");
            float d = Vector3.Distance(x.transform.position, ws);
            if (d > 2.2f) bad.Add($"{x.name} 작업면에서 {d:F2}m {V(x.transform.position)}");
            string p = t.PromptOf(x);
            if (!string.IsNullOrEmpty(p)) bad.Add($"{x.name} 칩 「{p}」(안내 0 이어야)");
        }
        string tp = t.PromptOf(t);
        if (!string.IsNullOrEmpty(tp)) bad.Add($"작업면 칩 「{tp}」");
        if (t.receive == null || t.receive.today == null || t.receive.today.Length == 0 || t.receive.today[0] == null) bad.Add("받는 곳 선반에 오늘 물건 없음");
        return bad;
    }

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

    /// <summary>
    /// 첫 한 판 메모 1(D-283): 상자를 든 채 카트 곁에 서면 「E 싣고 타기」 칩이 뜬다(상자 없이는 안 뜬다).
    /// 칩 글은 순수 함수(OnFootDirector.CartChip)라 Play 없이 보고, 카트 자리(보관소 앞)가 땅 위에 있고 작업대에서 걸어 들 거리(25 m) 안인지도 본다.
    /// </summary>
    static List<string> CartChipWithBox()
    {
        var bad = new List<string>();
        string withBox = OnFootDirector.CartChip(true, true, true, false);
        if (!withBox.StartsWith("E ") || !withBox.Contains("싣고")) bad.Add("상자를 든 채 카트 칩이 '" + withBox + "' — 'E 싣고 타기' 여야");
        string noBox = OnFootDirector.CartChip(true, true, false, false);
        if (noBox.StartsWith("E ")) bad.Add("주문을 맡고 상자 없이도 카트 칩 '" + noBox + "' 이 눌린다 — 상자부터 안내여야");
        var ds = UnityEngine.Object.FindObjectsByType<OnFootDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (ds.Length == 0 || !ds[0].DepotFlow) return bad;
        var d = ds[0];
        if (UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(FindObjectsInactive.Include) == null) bad.Add("씬에 ArcadeCarController 없음 — 싣고 탈 카트가 없다");
        var spot = OnFootDirector.DepotCartSpot;
        if (!GroundBelow(spot, 5f, out _)) bad.Add("아침 카트 자리 " + V(spot) + " — 아래 5 m 땅 없음");
        if (d.PackSpot != null)
        {
            float m = Vector2.Distance(new Vector2(spot.x, spot.z), new Vector2(d.PackSpot.transform.position.x, d.PackSpot.transform.position.z));
            if (m > 25f) bad.Add(string.Format(Inv, "아침 카트 자리가 작업대에서 {0:0} m — 상자 들고 25 m 안이어야", m));
        }
        return bad;
    }

    /// <summary>두 번째 한 판 메모 4(D-284): "표지판으로 방향을 알려 주는 게 분위기에 안 맞는다" — 보관소 길 표지판(DepotRoadSign)은 꺼져 있어야 한다. 지우지 않고 끈다.</summary>
    static List<string> RoadSignOff()
    {
        var bad = new List<string>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "DepotRoadSign" && t.gameObject.activeInHierarchy) bad.Add("DepotRoadSign 켜짐 " + V(t.position) + " — 강제 안내 표지판은 꺼 둔다(메모 4)");
        return bad;
    }

    /// <summary>두 번째 한 판 메모 5(D-284): 첫날 카트는 정비소(사장 곁)에서 받는다 — 아침 카트 자리가 사장에서 6 m 안, 보관소 앞(DepotCartSpot)이 아니어야.</summary>
    static List<string> MorningCartAtGarage()
    {
        var bad = new List<string>();
        var ds = UnityEngine.Object.FindObjectsByType<OnFootDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (ds.Length == 0 || ds[0].Boss == null) return bad;
        var at = ds[0].MorningCartAt;
        if (at == null) { bad.Add("cartSpot 비어 있음 — 아침 카트가 어디에도 없다"); return bad; }
        var boss = ds[0].Boss.position; var c = at.Value;
        float m = Vector2.Distance(new Vector2(c.x, c.z), new Vector2(boss.x, boss.z));
        if (m > 6f) bad.Add(string.Format(Inv, "아침 카트 {0} 가 사장({1})에서 {2:0.0} m — 정비소에서 받아야(6 m 안)", V(c), V(boss), m));
        var dep = OnFootDirector.DepotCartSpot;
        if (Vector2.Distance(new Vector2(c.x, c.z), new Vector2(dep.x, dep.z)) < 15f) bad.Add("아침 카트가 보관소 앞에 있다 — 첫날 카트는 정비소에서 시작");
        return bad;
    }

    /// <summary>
    /// D-289: world/world.yaml(→ WorldPlaces.asset) 의 장소 좌표와 씬이 0.5 m 안에서 같다. 비교 짝 = 배달 지점 ↔ 씬 DP_ 오브젝트 ·
    /// 「집 방」 ↔ OnFoot_Spawn_Room · Fixed 카트 자리 ↔ DebugPlaces.DepotCart(코드 상수). 어긋나면 양쪽 좌표를 같이 적는다 —
    /// 건물·오브젝트는 도현이 놓은 쪽이 기준이니 보통 yaml 을 고치고(gen.py), 씬 쪽이 실수로 밀렸으면 되돌린다.
    /// 에셋이 yaml 보다 낡았으면(해시 다름) 그것도 여기서 알린다.
    /// </summary>
    static List<string> YamlVsScene()
    {
        var bad = new List<string>(); const float Tol = 0.5f;
        var src = WorldData.Places;
        if (src == null) { bad.Add("WorldPlaces.asset 없음 — python tools/world/gen.py"); return bad; }
        string yamlPath = System.IO.Path.GetFullPath(Application.dataPath + "/../../world/world.yaml");
        if (System.IO.File.Exists(yamlPath))
        {
            string text = System.IO.File.ReadAllText(yamlPath, Encoding.UTF8).Replace("\r\n", "\n");
            var sb = new StringBuilder();
            using (var sha = System.Security.Cryptography.SHA1.Create()) foreach (var b in sha.ComputeHash(Encoding.UTF8.GetBytes(text))) sb.Append(b.ToString("x2"));
            if (sb.ToString() != src.sourceHash) bad.Add("WorldPlaces.asset 이 world.yaml 보다 낡음(해시 다름) — python tools/world/gen.py 로 다시 만들기");
        }
        var dps = new Dictionary<string, Transform>();
        foreach (var t in DpTransforms(true)) dps[t.name] = t;
        foreach (var d in src.deliveryPoints)
        {
            if (!dps.TryGetValue(d.name, out var t)) { bad.Add("yaml 배달 지점 " + d.name + " " + V(d.pos) + " — 씬에 켜진 " + d.name + " 없음"); continue; }
            float m = Vector3.Distance(d.pos, t.position);
            if (m > Tol) bad.Add(d.name + string.Format(Inv, " yaml {0} ↔ 씬 {1} ({2}) 차 {3:0.00} m > 0.5", V(d.pos), V(t.position), WorldSnapshot.Path(t), m));
        }
        foreach (var kv in dps)
            if (!src.deliveryPoints.Exists(x => x.name == kv.Key)) bad.Add("씬 " + kv.Key + " " + V(kv.Value.position) + " — yaml delivery_points 에 없음");
        var spawn = GameObject.Find("OnFoot_Spawn_Room");
        var room = src.spots.Find(s => s.name == "집 방");
        if (spawn != null && room != null && Vector3.Distance(room.feet, spawn.transform.position) > Tol)
            bad.Add(string.Format(Inv, "집 방 yaml {0} ↔ 씬 OnFoot_Spawn_Room {1} 차 {2:0.00} m > 0.5", V(room.feet), V(spawn.transform.position), Vector3.Distance(room.feet, spawn.transform.position)));
        foreach (var s in src.spots)
            if (s.car == "Fixed" && Vector3.Distance(s.carAt, DebugPlaces.DepotCart) > Tol)
                bad.Add(string.Format(Inv, "장소 '{0}' yaml 카트 자리 {1} ↔ 코드 DepotCart {2} 차 {3:0.00} m > 0.5", s.name, V(s.carAt), V(DebugPlaces.DepotCart), Vector3.Distance(s.carAt, DebugPlaces.DepotCart)));
        return bad;
    }

    /// <summary>두 번째 한 판 메모 3(D-284): 후진이 너무 느렸다(전진의 28%, 4초에 7 km/h). 기본 프리셋 B 의 후진 최고속·가속이 그 아래로 되돌아가지 않게.</summary>
    static List<string> ReverseNotCrawling()
    {
        var bad = new List<string>();
        var b = DriveFeel.B;
        if (b.reverseMaxRatio < 0.5f) bad.Add(string.Format(Inv, "B 후진 최고속 비율 {0:0.00} < 0.5 — 후진이 기어간다", b.reverseMaxRatio));
        if (b.reverseAccelMul < 1f) bad.Add(string.Format(Inv, "B 후진 가속 배율 {0:0.00} < 1 — 바닥 마찰에 먹혀 4초에 7 km/h 였다", b.reverseAccelMul));
        return bad;
    }

    /// <summary>
    /// D-287 6단계: 스크립트를 다른 어셈블리(asmdef)로 옮기면 씬이 「어셈블리::클래스」 이름으로 찾던 컴포넌트가 Missing 이 된다
    /// (실측: 작업대 재료 PackSupply — 파일 이름이 다른 클래스라 이름으로만 찾았다). 빈 컴포넌트 칸 = 조용히 기능이 빠진다.
    /// </summary>
    static List<string> NoMissingScripts()
    {
        var bad = new List<string>();
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var c in go.GetComponents<Component>())
                if (c == null) { if (bad.Count < 10) bad.Add("Missing 스크립트 " + WorldSnapshot.Path(go.transform) + " " + V(go.transform.position) + " — 클래스를 옮겼으면 파일 이름=클래스 이름으로"); else break; }
        return bad;
    }
}
#endif
