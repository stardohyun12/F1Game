using UnityEditor;
using UnityEngine;

/// <summary>
/// 회귀 단언 라이브러리 — "이 상태면 이래야 한다"를 코드로 박아 자동 검증.
/// 정지 스크린샷이 못 잡는 비시각/크로스상태 버그(오디오 잔류·fog·피벗)를 잡는다.
/// 스크립트(scripts/regression-check.ps1)가 Play 중 비트별로 호출. 각 메서드는 "PASS/FAIL: detail" 반환.
///
/// 각 단언은 "우리가 실제로 겪은 버그"에 1:1 대응 (효과 증명됨):
///  - CheckRunInactiveSilent  → 2026-05-22 엔진음 런 후 잔류 (90edf70)
///  - CheckFog                → 2026-05-22 오버뷰 맵 흐릿 (90edf70)
///  - CheckCargoPivots        → 2026-05-21 화물 작업대 잘림/회전 궤도 (93f7f75)
/// </summary>
public static class HarnessChecks
{
    /// <summary>런 비활성(보드/정산/차고)인데 소리내는 AudioSource가 있으면 FAIL — 엔진음 잔류 검출.</summary>
    public static string CheckRunInactiveSilent()
    {
        bool runActive = Services.Run != null && Services.Run.RunActive;
        if (runActive) return "SKIP: 런 활성 상태 (이 단언은 비활성에서만)";
        foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            // 루프 + 재생 + 음소거 안 됨 = 잔류 소리(엔진/BGM 제외 — Music은 의도)
            if (s.isPlaying && !s.mute && s.loop && s.gameObject.name != "Music")
            {
                string clip = s.clip != null ? s.clip.name : "-";
                return $"FAIL: 런 비활성인데 '{s.gameObject.name}' 소리남 (clip={clip})";
            }
        }
        return "PASS: 런 비활성, 잔류 소리 없음";
    }

    /// <summary>fog가 기대값과 다르면 FAIL — 오버뷰 맵 흐릿 검출.</summary>
    public static string CheckFog(bool expected)
    {
        if (RenderSettings.fog != expected)
            return $"FAIL: fog={RenderSettings.fog} 기대={expected}";
        return $"PASS: fog={RenderSettings.fog}";
    }

    /// <summary>엔진 mute가 기대값과 다르면 FAIL.</summary>
    public static string CheckEngineMute(bool expectedMuted)
    {
        var e = Object.FindFirstObjectByType<EngineAudio>();
        if (e == null) return "SKIP: EngineAudio 없음";
        var s = e.GetComponent<AudioSource>();
        if (s == null) return "SKIP: AudioSource 없음";
        if (s.mute != expectedMuted)
            return $"FAIL: engineMute={s.mute} 기대={expectedMuted}";
        return $"PASS: engineMute={s.mute}";
    }

    /// <summary>모든 화물 프리팹의 피벗이 기하 중심에서 벗어나면 FAIL — 작업대 잘림/회전 궤도 검출.</summary>
    public static string CheckCargoPivots()
    {
        var cat = Resources.Load<CargoModelCatalog>("CargoModels/CargoModelCatalog");
        if (cat == null) return "SKIP: 카탈로그 없음";
        string fails = "";
        foreach (CargoType ct in System.Enum.GetValues(typeof(CargoType)))
        {
            if (!cat.TryGet(ct, out var prefab, out _)) continue;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = Vector3.zero;
            var r = inst.GetComponentInChildren<MeshRenderer>();
            float off = r != null ? (r.bounds.center - inst.transform.position).magnitude : 99f;
            Object.DestroyImmediate(inst);
            if (off > 0.1f) fails += $"{ct}(off={off:F2}) ";
        }
        return fails == "" ? "PASS: 모든 화물 피벗 중심" : $"FAIL: 피벗 어긋남 {fails}";
    }

    /// <summary>
    /// 화물 카탈로그의 각 메시 tri 수가 예산 안에 드는지 단언 — 고폴리 자산 침투 검출.
    /// Kenney 로우폴리 월드(박스 ~12 tris)에 Meshy 원본(2.7만~7.5만)이 안 깎인 채
    /// 들어오면 결이 깨지고 성능도 샌다. hero 화물도 ~3000이면 충분(CargoDecimator로 깎음).
    /// (2026-05-23: Eggs 53k/Glass 27k/Fish 75k/Cake 54k 발견 — 전부 미가공 Meshy)
    /// 가정: 화물 프리팹은 단일 mesh 구조(LOD 그룹 없음). 다중 LOD면 모든 LOD를 합산하므로
    ///       과대 카운트됨 — 그땐 lod0만 세도록 보강 필요. catalog/prefab null은 SKIP이며,
    ///       회귀 스크립트가 SKIP을 끝에 크게 집계하므로(조용히 안 넘김) 누락이 드러난다.
    /// </summary>
    public static string CheckCargoPolyBudget(int budgetTris = 3000)
    {
        var cat = Resources.Load<CargoModelCatalog>("CargoModels/CargoModelCatalog");
        if (cat == null) return "SKIP: 카탈로그 없음";
        string fails = "";
        foreach (CargoType ct in System.Enum.GetValues(typeof(CargoType)))
        {
            if (!cat.TryGet(ct, out var prefab, out _) || prefab == null) continue;
            int tris = 0;
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
            if (tris > budgetTris) fails += $"{ct}({tris}tris) ";
        }
        return fails == "" ? $"PASS: 모든 화물 ≤{budgetTris}tris" : $"FAIL: 예산({budgetTris}) 초과 — {fails}(CargoDecimator로 깎아 카탈로그에 먹일 것)";
    }

    /// <summary>
    /// OrderBoard의 SerializeField 와이어링(panelRoot/cardsContainer/cardPrefab + catalog/runController)이
    /// 전부 채워졌는지 단언 — 도현이 manual play로 "주문 UI 안 뜸"을 발견해야 알던 재발 버그를 자동 검출.
    /// (2026-05-15 D-073, 2026-05-24 세션31, 둘 다 wiring NULL 재발 → 도현 의존)
    /// cardPrefab엔 OrderCardUI 컴포넌트가 있어야 함도 확인.
    /// Play 불필요 — 저장된 씬 상태를 그대로 검사하므로 "런타임에만 고쳐지고 씬엔 NULL" 재발 루프가 드러난다.
    /// (FindObjectsInactive.Include: panelRoot 토글로 보드가 꺼져 있어도 OrderBoard 자체는 잡아야)
    /// </summary>
    public static string CheckOrderBoardWiring()
    {
        var ob = Object.FindFirstObjectByType<OrderBoard>(FindObjectsInactive.Include);
        if (ob == null) return "SKIP: OrderBoard 없음 (이 씬엔 주문 보드가 없음)";

        var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        string[] fields = { "panelRoot", "cardsContainer", "cardPrefab", "catalog", "runController" };
        string nulls = "";
        object cardPrefabVal = null;
        foreach (var f in fields)
        {
            var fi = typeof(OrderBoard).GetField(f, BF);
            if (fi == null) { nulls += $"{f}(필드없음) "; continue; }
            var v = fi.GetValue(ob);
            // UnityEngine.Object는 "fake null" 처리 위해 명시 캐스팅 비교
            bool isNull = v == null || (v is Object uo && uo == null);
            if (isNull) nulls += $"{f} ";
            if (f == "cardPrefab") cardPrefabVal = v;
        }

        if (nulls != "")
            return $"FAIL: OrderBoard 와이어링 NULL — {nulls.Trim()} (FixOrderBoardWiring + SaveScene 필요. 런타임만 고치면 재발)";

        var prefab = cardPrefabVal as GameObject;
        if (prefab != null && prefab.GetComponent<OrderCardUI>() == null)
            return "FAIL: cardPrefab에 OrderCardUI 컴포넌트 없음 (카드 데이터 바인딩 안 됨)";

        return "PASS: OrderBoard 와이어링 5필드 + cardPrefab.OrderCardUI 정상";
    }

    /// <summary>
    /// 풀 게임루프 시스템이 Services에 전부 등록됐는지 단언 — 씬 리빌드가 시스템 GameObject를
    /// 조용히 누락시키면(2026-05-25: 맵 리빌드로 8시스템 유실, 포장·차고 흐름 통째로 죽음)
    /// 에러 없이 단계가 빠진다. 도현이 manual play로 발견해야 알던 걸 자동 검출.
    /// Play 중 호출(GameContext.AutoFindMissing이 런타임에 씬 인스턴스로 등록하므로).
    /// </summary>
    public static string CheckCoreServicesPresent()
    {
        string missing = "";
        if (Services.Run == null) missing += "Run ";
        if (Services.Delivery == null) missing += "Delivery ";
        if (Services.Packing == null) missing += "Packing ";
        if (Services.Restore == null) missing += "Restore ";
        if (Services.Boost == null) missing += "Boost ";
        if (Services.TireWear == null) missing += "TireWear ";
        if (Services.Tires == null) missing += "Tires ";
        if (Services.Upgrade == null) missing += "Upgrade ";
        if (Services.Shake == null) missing += "Shake ";
        // BUG-002 (2026-10-19): ZoneUnlockSystem 은 Services 에 없고 자체 Instance 싱글톤.
        // ComplexTrack 에 한 번도 배선된 적이 없어 OrderBoard 가 OrderCatalog(A/B/C Box 3종) 폴백으로
        // 조용히 고정됐고 원물 화물·zone 해금이 플레이에서 전부 죽어 있었다. 씬 객체 + registry 둘 다 단언.
        // FindFirstObjectByType 이라 에디트 모드/Play 양쪽에서 동작.
        var zus = Object.FindFirstObjectByType<ZoneUnlockSystem>(FindObjectsInactive.Include);
        if (zus == null) missing += "ZoneUnlock ";
        else if (zus.Registry == null || zus.Registry.allZones == null || zus.Registry.allZones.Count == 0) missing += "ZoneUnlock.registry ";
        return missing == ""
            ? "PASS: 풀 루프 시스템 10개 전부 등록됨 (Services 9 + ZoneUnlockSystem/registry)"
            : $"FAIL: 시스템 누락(단계 죽음) — {missing}(씬에 GameObject 유실. WireGarageSceneObjects/WireCoreSingletons/Add PackingScreen/Setup Cargo View/W1 Wire Scene (Zones) 재실행 필요)";
    }

    /// <summary>
    /// 모든 OrderJob의 목적지 라벨이 씬 DeliveryPoint와 매칭되는지 단언 — 매칭 안 되면
    /// RoutePlanScreen이 마커를 0개 만들어 "경로 설정 안됨"이 됨. 도현이 manual play로
    /// 발견해야 알던 D-073 라벨 미스매치 재발(세션27·세션32)을 자동 검출.
    /// 보드에 실제 뜨는 주문(OrderCatalog)뿐 아니라 풀(t:OrderJob) 전체를 검사.
    /// Play 불필요 — 저장된 자산/씬 상태 검사.
    /// </summary>
    public static string CheckOrderDestinationsResolve()
    {
        var points = Object.FindObjectsByType<DeliveryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (points.Length == 0) return "SKIP: 씬에 DeliveryPoint 없음";
        // 런타임엔 DeliveryPoint.Label이 GameObject 이름으로 채워짐(Awake). 에디트 모드에선
        // 직렬화 label이 비어 있으므로 name으로 폴백 — 두 모드 모두에서 동작.
        var labels = new System.Collections.Generic.HashSet<string>();
        foreach (var p in points) labels.Add(string.IsNullOrEmpty(p.Label) ? p.name : p.Label);

        string bad = "";
        int chec_ = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:OrderJob"))
        {
            var job = AssetDatabase.LoadAssetAtPath<OrderJob>(AssetDatabase.GUIDToAssetPath(guid));
            if (job == null || job.destinationLabels == null || job.destinationLabels.Count == 0) continue;
            chec_++;
            var label = job.destinationLabels[0];
            if (!labels.Contains(label)) bad += $"{job.name}→'{label}' ";
        }
        return bad == ""
            ? $"PASS: 주문 {chec_}개 목적지 전부 씬 DeliveryPoint와 매칭"
            : $"FAIL: 목적지 미매칭(경로설정 불가) — {bad}(FixOrderDestinations 또는 라벨 정합 필요)";
    }

    /// <summary>
    /// 포장 wrap을 **실제 입력경로(BeginStroke→UpdateStroke)**로 구동해 3D 리본이 실제로
    /// 생기는지(geometry>0) 단언. 지름길(DrawStrip 등) 우회 금지 — false-green 종류 박멸.
    /// (2026-05-21 와인병 리본 false-green: 검증이 DrawStrip만 봐서 GREEN, 실제 경로는 0)
    /// 호출 전 PackingScreen.Show(잡)로 화물이 떠 있어야. 호출자가 Show.
    /// </summary>
    public static string CheckWrapRealPath()
    {
        var ps = Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>();
        if (ps == null) return "SKIP: PackingScreen 없음";
        var item = ps.ActiveItem;
        if (item == null) return "SKIP: ActiveItem 없음 (Show 먼저)";
        var painter = item.GetComponent<PitCrew.Packing.WrapPainter>();
        if (painter == null) return "SKIP: WrapPainter 없음";

        var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var cam = ps.GetType().GetField("packingCamera", BF)?.GetValue(ps) as Camera;
        var col = painter.GetType().GetField("targetCollider", BF)?.GetValue(painter) as Collider;
        if (cam == null || col == null) return "SKIP: 카메라/콜라이더 미배선";

        Physics.SyncTransforms();
        // 객체의 실제 화면 bounds 안에서 세로 스트로크 (모양 무관 hit 보장)
        var b = col.bounds; var c = b.center;
        Vector3 topW = c + new Vector3(0, b.extents.y * 0.7f, 0);
        Vector3 botW = c - new Vector3(0, b.extents.y * 0.7f, 0);
        Vector2 top = cam.WorldToScreenPoint(topW);
        Vector2 bot = cam.WorldToScreenPoint(botW);
        painter.BeginStroke(top);
        for (int i = 1; i <= 16; i++) painter.UpdateStroke(Vector2.Lerp(top, bot, i / 16f));
        painter.EndStroke();

        int verts = 0;
        foreach (Transform t in col.transform)
            if (t.name.Contains("Ribbon"))
            {
                var mf = t.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) verts += mf.sharedMesh.vertexCount;
            }
        return verts > 0
            ? $"PASS: 실제 입력경로 랩 생성됨 verts={verts}"
            : "FAIL: BeginStroke로 랩 geometry 0 — false-green 종류 (지름길은 통과해도 진짜 경로 깨짐)";
    }
}
