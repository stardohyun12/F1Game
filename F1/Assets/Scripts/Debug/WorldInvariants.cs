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
    public static readonly HashSet<string> Soft = new HashSet<string> { "보관소 출구", "지형 감사" };

    static readonly (string name, Func<List<string>> run)[] Checks =
    {
        ("지형 감사", () => TerrainAudit.Lines()),   // 틈·뜬/묻힌 길·굴곡·흰 조각 — D-295, 처음엔 WARN
        ("지형 안쪽 열린 변 0", () => TerrainAudit.OpenEdgeLines()),   // D-295 버그: 쪼갠 삼각형 옆 T자 이음매가 벌어져 하늘이 비침
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
        ("실내 도트 방 상자(반폭 0.8~8 m, 방 소품 품음)", () => IndoorDotBox()),
        ("실내 디오라마(D-298): 방 잔소품 켜짐 0 · 침대·책상·의자·일기장 켜짐 · 단색 사본(_roomflat) 0 · 방 텍스처 Point", () => RoomDioramaProps()),
        ("건물 안 전부 디오라마(D-304): 건물마다 방 상자 ≥1 · 2층 방 있음 · 반폭 0.8~10 m", () => InteriorBoxes()),
        ("건물 안 고정 시점(D-306): 건물마다 컷 셰이더(정비소 포함) · 작은 방 고정 카메라 = 상자 밖 · 건물 안 Unlit = 유리로 다룸 · 천장 컷 바닥 위 ≤2.7m", () => InteriorFixedView()),
        ("건물 문 밖 1 m 는 디오라마가 아니다(과제 20261105-0453): 바깥 벽·문·셔터 1 m 밖 점이 어느 방 상자에도 안 듦", () => InteriorDoorOutside()),
        ("작은 방은 바닥 네 모서리가 화면 안(D-307 ⑧): 맞춘 고정 카메라로 투영한 바닥 모서리 4점 |NDC| ≤ 1", () => InteriorRoomInFrame()),
        ("디오라마 시점에서 캡슐 앞을 가리는 벽이 없다(과제 20261105-0535): 방마다 카메라 → 가슴(바닥+1.1m, 3×3) 레이의 벽이 다 컷에 잘림", () => InteriorCapsuleClear()),
        ("건물 안 방 바닥은 위에서 선다(과제 20261105-0535): 한 면 바닥(우체국 2층) → 고침 콜라이더 뒤 0", () => InteriorFloorStandable()),
        ("디오라마 창(D-299) = 유리문 아님(아래 끝 바닥 + 0.5 m 위) · 책상 2.5 m 안", () => RoomDioramaWindow()),
        ("디오라마 방 빛 시간대(과제 20261104-2033): 잘린 지붕 그림자 안 드리움 · 해 꺼짐 = 밤 · 바탕빛 낮 ≥ 밤×2.5 · 저녁 R > B×1.3", () => RoomDioramaTimeLight()),
        ("world.yaml 좌표 ≤0.5m", () => YamlVsScene()),
        ("포장대 선반 물건 5개·칩 0", () => PackV3Shelf()),
        ("포장 v3 물건·상자 연속 충돌(들면 안 빠짐)", () => PackV3Continuous()),
        ("들린 물건 Rigidbody 보간 꺼짐(걸을 때 따라옴)", () => HeldFollowsHand()),
        ("포장 2D 보드 조각(상자3·롤2·테이프·물건)·판 안·안 겹침", () => PackBoard2D()),
        ("포장 2D 규칙: 소 튕김·중 딱맞음(+뽁뽁이 90%)·대 헐거움·롤 가득·뜯은 조각 0", () => PackBoard2DRules()),
        ("포장 물리(D-296): 달걀 감싸기 2겹 + 중 상자 → 빈틈 있고 흔들림 있되 부딪힘 손상 ≤ 안 감쌌을 때의 40% · 대 상자 안 감쌈 >5cm", () => PackPhysicsShake()),
        ("포장 구긴 종이(D-296): 달걀 + 중 상자 + 4덩어리 → 흔들림 ≤ 종이 0일 때의 50% · 덩어리 늘면 틈 메움↑(≤0.85)", () => PackPaperShake()),
        ("포장 알갱이(D-296): 달걀 + 중 상자 + 알갱이 2줌 → 흔들림 ≤ 알갱이 0일 때의 50% · 같은 양이면 무거운 물건일수록 덜 붙듦 · 개수 늘면 메움↑(≤0.9)", () => PackBeadShake()),
        ("포장 보이고 들리게(D-300): 신호 7종(구김·툭·감쌈·삽·쏟기·밖쏟기·덜컹) 전부 · 합성음 출처 5줄 다 적힘(AI 그림·외부 음원 없음)", () => PackFxSignals()),
        ("포장 3D 작업대(D-301): 들어가는 물건(감싼 겹 포함)이 그림에서도 상자 안 벽 안쪽에 들어간다(벽 뚫고 삐져나옴 0)", () => PackBench3DFit()),
        ("포장 소리 풀(20261105): 꺼진 오브젝트에 매달린 풀은 켜진 host 로 옮겨짐 — 덜그럭이 조용하지 않게", () => PackSfxPoolActive()),
        ("포장 첫 튜토리얼(D-302): 단계마다 큰 글 한 줄 · 보드 상태마다 단계가 순서대로 바뀜(상자 없음→완충재→넣기→틈(선택)→테이프→닫힘=들고 나가기, 흔들기 단계 없음)", () => PackTutorialSteps()),
        ("포장 흔들기 선택(D-302): 안 흔든 상자(흔든 시간 0 < 1.5 s)는 표준 흔들기로 재고, 헐거운 상자면 그 값으로 시작값이 깎인다(흔들림 0 일 때보다 낮음)", () => PackNoShakeStart()),
    };

    static List<string> PackTutorialSteps()
    {
        var bad = new List<string>();
        int steps = Enum.GetValues(typeof(PackTutorial.Step)).Length;
        if (PackTutorial.Now.Length != steps || PackTutorial.Hint.Length != steps) bad.Add("단계 글 줄 수가 단계 수(" + steps + ")와 다름");
        foreach (var l in PackTutorial.Now) if (string.IsNullOrEmpty(l)) bad.Add("큰 글이 빈 단계가 있음");
        var box = new PackPiece { kind = PackPieceKind.Box, active = true };
        var egg = new PackPiece { kind = PackPieceKind.Parcel, active = true };
        void Is(PackTutorial.Step want, PackPiece b, PackPiece p, string what)
        {
            var got = PackTutorial.StepOf(b, p);
            if (got != want) bad.Add(what + ": 단계 " + got + " (기대 " + want + ")");
        }
        Is(PackTutorial.Step.PickBox, null, egg, "상자 없음");
        Is(PackTutorial.Step.Cushion, box, egg, "빈 상자 열림 · 완충재 아직");
        egg.wrap = 1;
        Is(PackTutorial.Step.PutParcel, box, egg, "물건 감쌈 → 넣기");
        egg.wrap = 0; box.bottom = PackFiller.Paper;
        Is(PackTutorial.Step.PutParcel, box, egg, "바닥 완충재 → 넣기(깔기도 통과)");
        box.bottom = PackFiller.None; box.content = egg;
        Is(PackTutorial.Step.Gap, box, egg, "물건 넣음 · 틈 아직(선택 단계)");
        box.top = PackFiller.Paper;
        Is(PackTutorial.Step.Tape, box, egg, "덮은 완충재 → 테이프");
        box.top = PackFiller.None; box.closed = true;
        Is(PackTutorial.Step.TakeOut, box, egg, "닫힘 → 흔들지 않아도 들고 나가기");   // D-302 흔들기는 선택: 단계가 흔든 적에 달려 있지 않다
        return bad;
    }

    /// <summary>
    /// 버그(20261105): 덜그럭·뽁뽁이·툭 소리 풀(PackBench.pool)이 꺼진 옛 작업대 Eye 에 만들어져 2D 보드에서 RattlePlayed 만 오르고 소리는 안 났다(isPlaying 0/282 프레임).
    /// 꺼진 host 에서 풀을 만든 뒤 켜진 host 로 EnsureSfxOn 하면 풀이 켜진 host 로 옮겨져야 한다. 임시 오브젝트는 지우고 풀·씬 dirty 를 되돌린다.
    /// </summary>
    static List<string> PackSfxPoolActive()
    {
        var bad = new List<string>();
        var T = typeof(PackBoard).Assembly.GetType("PitCrew.PackBench.PackBench");
        var f = T != null ? T.GetField("pool", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic) : null;
        if (f == null) { bad.Add("PackBench.pool 필드를 못 찾음"); return bad; }
        var ens = T.GetMethod("EnsureSfxOn", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
#if UNITY_EDITOR
        var ps = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();   // 임시 오브젝트는 미리보기 씬에 — 열린 씬을 dirty 로 안 만든다
#endif
        var saved = f.GetValue(null);
        GameObject a = null, b = null;
        try
        {
            f.SetValue(null, null);
            a = new GameObject("_sfxpool_off") { hideFlags = HideFlags.HideAndDontSave }; a.SetActive(false);
            b = new GameObject("_sfxpool_on") { hideFlags = HideFlags.HideAndDontSave };
#if UNITY_EDITOR
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(a, ps); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(b, ps);
#endif
            ens.Invoke(null, new object[] { a });
            ens.Invoke(null, new object[] { b });
            var pool = (AudioSource[])f.GetValue(null);
            if (pool == null || pool.Length == 0 || pool[0] == null) bad.Add("EnsureSfxOn 뒤 풀이 비었음");
            else if (!pool[0].gameObject.activeInHierarchy) bad.Add($"소리 풀이 꺼진 오브젝트 '{pool[0].gameObject.name}' 에 매달려 있음 — 소리가 안 난다 (켜진 host 로 옮겨야 함)");
        }
        finally
        {
            f.SetValue(null, null);
            if (a != null) UnityEngine.Object.DestroyImmediate(a);
            if (b != null) UnityEngine.Object.DestroyImmediate(b);
            f.SetValue(null, saved);
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(ps);
#endif
        }
        return bad;
    }

    /// <summary>
    /// D-300 포장 2D 보이고 들리게: 판이 보내는 신호(PackBoard.PackFx)가 7종 그대로이고(하나 빠지면 그 소리·모양이 조용히 사라진다), 소리 출처 표(PackBoardFx.SoundSources)가 비어 있지 않다.
    /// 씬 없이 돈다. 화면·소리 자체는 Play 에서 본다(그림 3장).
    /// </summary>
    static List<string> PackFxSignals()
    {
        var bad = new List<string>();
        int n = System.Enum.GetValues(typeof(PackBoard.PackFx)).Length;
        if (n != 7) bad.Add($"PackFx 신호가 {n}종 (7종이어야 함 — 그림/소리 연결이 어긋남)");
        var src = PackBoardFx.SoundSources;
        if (src == null || src.Length < 5) bad.Add($"소리 출처 표 {(src == null ? 0 : src.Length)}줄 (5줄 이상)");
        else foreach (var l in src) if (string.IsNullOrWhiteSpace(l)) bad.Add("소리 출처 줄이 비었음");
        return bad;
    }

    /// <summary>
    /// D-301 3D 작업대: 판지 벽 두께만큼 상자 안이 좁아, 달걀 한 판 + 뽁뽁이 2겹이 중 상자 벽을 뚫고 삐져나왔다(2026-11-05 Play 그림).
    /// world.yaml 상자 3종 × 감싼 겹 0~2 중 규칙상 들어가는 조합마다, PackBoardView.FitInside 를 거친 그림 겉이 안 벽 사이에 들어가고 너무 찌그러지지(80% 미만) 않아야 한다. 씬 없이 돈다.
    /// </summary>
    static List<string> PackBench3DFit()
    {
        var bad = new List<string>();
        var od = WorldData.Orders; var pk = od != null ? od.packing : null;
        if (pk == null || pk.boxes.Count == 0) { bad.Add("world.yaml packing.boxes 없음"); return bad; }
        var tr = od.TraitOf("달걀 한 판");
        var size = tr != null ? tr.size : new Vector3(0.30f, 0.07f, 0.30f);
        float S = PackBoardView.DrawScale, wall = 0.006f;
        foreach (var bx in pk.boxes)
            for (int wrap = 0; wrap <= 2; wrap++)
            {
                var ws = PackBoard.WrappedSize(size, wrap);
                if (ws.x > bx.inner.x || ws.z > bx.inner.z) continue;               // 규칙상 안 들어감 — 그림 문제 아님
                float ow = size.x * S + PackBoardView.WrapDraw * wrap, odp = size.z * S + PackBoardView.WrapDraw * wrap;
                float bw = bx.inner.x * S, bd = bx.inner.z * S;
                float s = PackBoardView.FitInside(ow, odp, bw, bd);
                if (ow * s > bw - 2f * wall + 1e-4f || odp * s > bd - 2f * wall + 1e-4f) bad.Add($"{bx.inner} 상자 + {wrap}겹: 그림 겉 {ow * s:F3}×{odp * s:F3} m > 안 벽 사이 {bw - 2f * wall:F3}×{bd - 2f * wall:F3}");
                if (s < 0.8f) bad.Add($"{bx.inner} 상자 + {wrap}겹: 그림을 {s:P0} 로 찌그러뜨려야 들어감 — 그림 축척·벽 두께가 규칙 크기와 어긋남");
            }
        return bad;
    }

    /// <summary>
    /// D-296 스티로폼 알갱이: 달걀 한 판 + 중 상자(안 감쌈)를 표준 흔들기(진폭 4 cm · 3 Hz)로 흔든 덜그럭 값이 알갱이 0줌일 때보다 2줌(= 2 × bead_scoop 개)일 때 절반 이하여야 한다.
    /// 같은 개수·같은 상자 틈이면 물건이 무거울수록(달걀 0.6 kg &gt; 생선 1.5 kg) 알갱이가 덜 붙든다(질량 차) · 개수가 늘수록 메움이 줄지 않고 BeadFillMax 를 넘지 않으며 bead_max 개에서도 그렇다. 씬 없이 돈다.
    /// </summary>
    static List<string> PackBeadShake()
    {
        var bad = new List<string>();
        var od = WorldData.Orders; var pk = od != null ? od.packing : null;
        var tr = od != null ? od.TraitOf("달걀 한 판") : null;
        if (tr == null) tr = new WorldOrders.Trait { cargo = "달걀 한 판", trait = "깨짐", size = new Vector3(0.30f, 0.07f, 0.30f), mass = 0.6f };
        bool yaml = pk != null && pk.boxes.Count >= 3;
        Vector3 mid = yaml ? pk.boxes[1].inner : new Vector3(0.315f, 0.15f, 0.315f), big = yaml ? pk.boxes[2].inner : new Vector3(0.46f, 0.24f, 0.46f);
        var mm = yaml ? new PackPhysics.Mat(pk.boxes[1].mass, pk.boxes[1].bounce, pk.boxes[1].friction) : new PackPhysics.Mat(0.12f, 0.3f, 0.4f);
        if (PackBoard.BeadScoop <= 0 || PackBoard.BeadMax < PackBoard.BeadScoop * 2 || PackBoard.BeadRadius <= 0f || PackBoard.BeadMass <= 0f || PackBoard.BeadHold <= 0f)
            bad.Add($"bead_* 숫자가 이상함 scoop={PackBoard.BeadScoop} max={PackBoard.BeadMax} r={PackBoard.BeadRadius} m={PackBoard.BeadMass} hold={PackBoard.BeadHold}");
        int two = PackBoard.BeadScoop * 2;
        float s0 = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None).MeasureStandard();
        var withBeads = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, 0, 0, two);
        float s2 = withBeads.MeasureStandard();
        if (s0 <= 0f) bad.Add($"알갱이 0 + 중 상자 + {tr.cargo}: 흔들림 {s0 * 100f:F2} cm — 비교할 기준이 없음");
        else if (s2 > s0 * 0.5f) bad.Add($"알갱이 2줌({two}개) 흔들림 {s2 * 100f:F2} cm > 알갱이 0 {s0 * 100f:F2} cm 의 50% (틈 메움 {withBeads.fill * 100f:F0}%)");
        float eggFill = PackBoard.BeadFill(mid, tr.size.x, tr.size.z, 0.6f, two), fishFill = PackBoard.BeadFill(mid, tr.size.x, tr.size.z, 1.5f, two);
        if (!(eggFill > fishFill)) bad.Add($"같은 알갱이 {two}개: 0.6 kg 물건 메움 {eggFill:F2} ≤ 1.5 kg 물건 {fishFill:F2} — 무거울수록 덜 붙들어야 함");
        float prev = 0f;
        for (int n = 1; n <= PackBoard.BeadMax; n += Mathf.Max(1, PackBoard.BeadScoop))
        {
            float f = PackBoard.BeadFill(big, tr.size.x, tr.size.z, tr.mass, n);
            if (f < prev || f > PackBoard.BeadFillMax + 1e-4f || f <= 0f) bad.Add($"대 상자 알갱이 {n}개 메움 {f:F3} (앞 {prev:F3}, 0 초과 {PackBoard.BeadFillMax:F2} 이하·줄지 않아야 함)");
            prev = f;
        }
        return bad;
    }

    /// <summary>
    /// D-296 구긴 종이: 달걀 한 판 + 중 상자(안 감쌈)를 표준 흔들기(진폭 4 cm · 3 Hz)로 흔든 덜그럭 값이 종이 0덩어리일 때보다 4덩어리일 때 절반 이하여야 한다(종이가 틈을 메우면 움직임↓).
    /// 덩어리가 늘수록 메운 비율(fill)이 줄지 않고 PaperFillMax 를 넘지 않으며, 대 상자에서도 4덩어리가 흔들림을 줄인다. 씬 없이 돈다.
    /// </summary>
    static List<string> PackPaperShake()
    {
        var bad = new List<string>();
        var od = WorldData.Orders; var pk = od != null ? od.packing : null;
        var tr = od != null ? od.TraitOf("달걀 한 판") : null;
        if (tr == null) tr = new WorldOrders.Trait { cargo = "달걀 한 판", trait = "깨짐", size = new Vector3(0.30f, 0.07f, 0.30f) };
        bool yaml = pk != null && pk.boxes.Count >= 3;
        Vector3 mid = yaml ? pk.boxes[1].inner : new Vector3(0.315f, 0.15f, 0.315f), big = yaml ? pk.boxes[2].inner : new Vector3(0.46f, 0.24f, 0.46f);
        var mm = yaml ? new PackPhysics.Mat(pk.boxes[1].mass, pk.boxes[1].bounce, pk.boxes[1].friction) : new PackPhysics.Mat(0.12f, 0.3f, 0.4f);
        var bm = yaml ? new PackPhysics.Mat(pk.boxes[2].mass, pk.boxes[2].bounce, pk.boxes[2].friction) : new PackPhysics.Mat(0.2f, 0.3f, 0.4f);
        if (PackBoard.PaperRadius <= 0f || PackBoard.PaperMass <= 0f || PackBoard.PaperFriction <= 0f) bad.Add($"paper_* 숫자가 없음 r={PackBoard.PaperRadius} m={PackBoard.PaperMass} μ={PackBoard.PaperFriction}");
        float s0 = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None).MeasureStandard();
        var four = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, 0, 4);
        float s4 = four.MeasureStandard();
        if (s0 <= 0f) bad.Add($"종이 0 + 중 상자 + {tr.cargo}: 흔들림 {s0 * 100f:F2} cm — 비교할 기준이 없음");
        else if (s4 > s0 * 0.5f) bad.Add($"종이 4덩어리 흔들림 {s4 * 100f:F2} cm > 종이 0 {s0 * 100f:F2} cm 의 50% (틈 메움 {four.fill * 100f:F0}%)");
        float prev = 0f;
        for (int n = 1; n <= PackBoard.PaperMax; n++)
        {
            float f = PackBoard.Setup(big, false, bm, tr, false, PackFiller.None, PackFiller.None, 0, n).fill;
            if (f < prev || f > PackBoard.PaperFillMax + 1e-4f || f <= 0f) bad.Add($"대 상자 종이 {n}덩어리 메움 {f:F2} (앞 {prev:F2}, 0 초과 {PackBoard.PaperFillMax:F2} 이하·줄지 않아야 함)");
            prev = f;
        }
        float b0 = PackBoard.Setup(big, false, bm, tr, false, PackFiller.None, PackFiller.None).MeasureStandard();
        float b4 = PackBoard.Setup(big, false, bm, tr, false, PackFiller.None, PackFiller.None, 0, 4).MeasureStandard();
        if (b0 > 0f && b4 >= b0) bad.Add($"대 상자 종이 4덩어리 흔들림 {b4 * 100f:F1} cm ≥ 종이 0 {b0 * 100f:F1} cm — 종이가 틈을 안 메움");
        return bad;
    }

    /// <summary>
    /// D-295 포장 2D ③: 상자 안 물리(PackPhysics)의 표준 흔들기(진폭 4 cm · 3 Hz, 1.5 s 합) — 사람이 흔드는 것과 같은 물리 길.
    /// 달걀 한 판을 뽁뽁이로 2겹 감싸면 물건이 커져(PackRules.WrapThick) 중 상자에 들어가되 빈틈이 조금 남는다(= 흔들림은 있다). 부딪힘 손상 = 시작값이 흔들림 때문에 깎인 만큼(바탕 손상 제외)은
    /// 안 감쌌을 때(같은 중 상자·같은 표준 흔들기)의 40% 이하여야 한다 겹 계수(WrapK)가 물리에 실제로 들어갔는지(1 미만)도 따로 본다.
    /// 대 상자 + 안 감쌈은 5 cm 초과(틈이 있는데 안 움직이면 물성·틈 계산이 어긋난 것). 씬 없이 돈다.
    /// </summary>
    static List<string> PackPhysicsShake()
    {
        var bad = new List<string>();
        var od = WorldData.Orders; var pk = od != null ? od.packing : null;
        var tr = od != null ? od.TraitOf("달걀 한 판") : null;
        if (tr == null) tr = new WorldOrders.Trait { cargo = "달걀 한 판", trait = "깨짐", size = new Vector3(0.30f, 0.07f, 0.30f) };
        bool yaml = pk != null && pk.boxes.Count >= 3;
        Vector3 mid = yaml ? pk.boxes[1].inner : new Vector3(0.315f, 0.15f, 0.315f), big = yaml ? pk.boxes[2].inner : new Vector3(0.46f, 0.24f, 0.46f);
        var mm = yaml ? new PackPhysics.Mat(pk.boxes[1].mass, pk.boxes[1].bounce, pk.boxes[1].friction) : new PackPhysics.Mat(0.12f, 0.3f, 0.4f);
        var bm = yaml ? new PackPhysics.Mat(pk.boxes[2].mass, pk.boxes[2].bounce, pk.boxes[2].friction) : new PackPhysics.Mat(0.2f, 0.3f, 0.4f);
        var wrapped = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None, PackRules.WrapMax);
        var bare = PackBoard.Setup(mid, false, mm, tr, false, PackFiller.None, PackFiller.None);
        float shW = wrapped.MeasureStandard(out int hitsW), shB = bare.MeasureStandard();
        float bigBare = PackBoard.Setup(big, false, bm, tr, false, PackFiller.None, PackFiller.None).MeasureStandard();
        var gap = wrapped.Limit;
        if (PackRules.WrapK(PackRules.WrapMax) >= 1f) bad.Add($"겹 계수 {PackRules.WrapK(PackRules.WrapMax):F2} ≥ 1 — 감싸도 부딪힘이 안 줄어든다 (wrap_damp {PackRules.WrapDamp:F2})");
        if (!Mathf.Approximately(wrapped.wrapK, PackRules.WrapK(PackRules.WrapMax))) bad.Add($"물리에 겹 계수가 안 들어감 wrapK {wrapped.wrapK:F2} ≠ {PackRules.WrapK(PackRules.WrapMax):F2}");
        if (FitCheck(mid, tr.size, PackRules.WrapMax) != PackFit.Snug) bad.Add($"{tr.cargo} {PackRules.WrapMax}겹 감쌈 → 중 상자 {FitCheck(mid, tr.size, PackRules.WrapMax)} (기대 Snug — 안 들어가면 wrap_thick {PackRules.WrapThick:F3} 이 너무 두꺼움)");
        if (gap.x <= 0f || gap.y <= 0f) bad.Add($"감싼 달걀 + 중 상자: 빈틈 없음(꽉 끼었다) 틈 {gap.x * 2000f:F1}×{gap.y * 2000f:F1} mm — 감싸도 흔들림은 있어야 한다");
        if (shW <= 0f || hitsW <= 0) bad.Add($"감싼 달걀 + 중 상자: 흔들림 {shW * 100f:F2} cm · 벽 부딪힘 {hitsW}번 — 빈틈이 있는데 안 움직임");
        float s0 = PackRules.StartFromShake(tr.trait, PackFiller.None, PackFiller.None, 0f);
        float dmgB = s0 - PackRules.StartFromShake(tr.trait, PackFiller.None, PackFiller.None, shB);
        float dmgW = s0 - PackRules.StartFromShake(tr.trait, PackFiller.None, PackFiller.None, shW, wrapped.wrapK);
        if (dmgB <= 0f) bad.Add($"안 감싼 달걀 + 중 상자: 부딪힘 손상 {dmgB * 100f:F2}%p ≤ 0 (흔들림 {shB * 100f:F1} cm) — 비교할 기준이 없음");
        else if (dmgW > dmgB * 0.40f) bad.Add($"부딪힘 손상 감싼 {dmgW * 100f:F2}%p > 안 감싼 {dmgB * 100f:F2}%p 의 40% (흔들림 감싼 {shW * 100f:F2} cm / 안 감싼 {shB * 100f:F1} cm)");
        if (bigBare <= 0.05f) bad.Add($"대 상자 + 안 감쌈 + {tr.cargo}: 흔들림 {bigBare * 100f:F1} cm (기대 > 5 cm) — 틈이 있는데 안 움직임");
        return bad;
    }

    /// <summary>D-302 흔들기는 선택: 안 흔든 닫힌 상자(ShakeTime 0 &lt; Window)는 PackBoard.TakeOut 이 표준 흔들기로 잰다. 대 상자 + 안 감쌈이면 그 값 &gt; 0 이고 시작값이 흔들림 0 일 때보다 낮아야 한다. 씬 없이 돈다.</summary>
    static List<string> PackNoShakeStart()
    {
        var bad = new List<string>();
        var od = WorldData.Orders; var pk = od != null ? od.packing : null;
        var tr = od != null ? od.TraitOf("달걀 한 판") : null;
        if (tr == null) tr = new WorldOrders.Trait { cargo = "달걀 한 판", trait = "깨짐", size = new Vector3(0.30f, 0.07f, 0.30f) };
        bool yaml = pk != null && pk.boxes.Count >= 3;
        Vector3 big = yaml ? pk.boxes[2].inner : new Vector3(0.46f, 0.24f, 0.46f);
        var bm = yaml ? new PackPhysics.Mat(pk.boxes[2].mass, pk.boxes[2].bounce, pk.boxes[2].friction) : new PackPhysics.Mat(0.2f, 0.3f, 0.4f);
        var ph = PackBoard.Setup(big, false, bm, tr, false, PackFiller.None, PackFiller.None);
        if (ph.ShakeTime >= PackPhysics.Window) bad.Add($"안 흔든 상자인데 흔든 시간 {ph.ShakeTime:F1}s ≥ {PackPhysics.Window:F1}s — 표준 흔들기 경로를 안 탐");
        float std = ph.MeasureStandard();
        float noShake = PackRules.StartFromShake(tr.trait, PackFiller.None, PackFiller.None, 0f), withStd = PackRules.StartFromShake(tr.trait, PackFiller.None, PackFiller.None, std);
        if (std <= 0f) bad.Add($"대 상자 + 안 감쌈 표준 흔들기 {std * 100f:F2} cm ≤ 0 — 안 흔든 상자의 시작값을 잴 수 없음");
        else if (withStd >= noShake) bad.Add($"표준 흔들기 시작값 {withStd * 100f:F1}% ≥ 흔들림 0 의 {noShake * 100f:F1}% — 헐거운 상자가 안 깎임");
        return bad;
    }

    static PackFit FitCheck(Vector3 inner, Vector3 sz, int wrap)
    {
        var w = PackBoard.WrappedSize(sz, wrap);
        return PackRules.FitOf(w.x, w.y, w.z, inner.x, inner.y, inner.z);
    }

    /// <summary>
    /// D-294 포장 2D ②③: 달걀 한 판이 소 상자엔 안 들어가고(TooSmall) 중엔 딱 맞고(Snug) 대엔 헐겁고(Loose), 중 + 뽁뽁이 위·아래면 시작값 90%(Core 규칙 그대로).
    /// 롤은 가득(left==full>0)으로 시작하고 뜯은 한 장(Sheet) 조각이 처음부터 남아 있지 않다. 상자 inner 가 바뀌어 규칙이 어긋나면 여기서 잡힌다.
    /// </summary>
    static List<string> PackBoard2DRules()
    {
        var bad = new List<string>();
        var sp = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>(FindObjectsInactive.Include);
        var t = UnityEngine.Object.FindFirstObjectByType<PackTable2>(FindObjectsInactive.Include);
        if (sp == null || t == null || !sp.Use2D) return bad;
        var go = new GameObject("PackBoard2DRules_check") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var b = go.AddComponent<PackBoard>();
            b.Build(t);
            var tr = WorldData.Orders != null ? WorldData.Orders.TraitOf("달걀 한 판") : null;
            Vector3 sz = tr != null ? tr.size : new Vector3(0.30f, 0.07f, 0.30f);
            string trait = tr != null ? tr.trait : "깨짐";
            PackFit[] want = { PackFit.TooSmall, PackFit.Snug, PackFit.Loose };
            for (int i = 0; i < 3; i++)
            {
                var bx = b.Find(PackPieceKind.Box, i);
                if (bx == null) { bad.Add($"상자 {i} 없음"); continue; }
                var fit = PackRules.FitOf(sz.x, sz.y, sz.z, bx.inner.x, bx.inner.y, bx.inner.z);
                if (fit != want[i]) bad.Add($"{bx.name}: 달걀 한 판 {fit} (기대 {want[i]})");
                if (i == 1)
                {
                    float s = PackRules.StartIntegrity(trait, fit, PackFiller.Bubble, PackFiller.Bubble, 0f);
                    if (Mathf.Abs(s - 0.90f) > 0.005f) bad.Add($"중 + 뽁뽁이 위·아래 시작값 {s * 100f:F1}% (기대 90%)");
                }
            }
            for (int i = 0; i < b.Count(PackPieceKind.Roll); i++)
            {
                var r = b.Find(PackPieceKind.Roll, i);
                if (r.full <= 0 || r.left != r.full) bad.Add($"롤 {r.name} 남은 {r.left}/{r.full} (가득 아님)");
            }
            if (b.Count(PackPieceKind.Sheet) != 0) bad.Add($"처음부터 뜯은 조각 {b.Count(PackPieceKind.Sheet)}개");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        return bad;
    }

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
    /// 과제 20261104-0501: 방(ROOM_2F) 안 A7 면은 A7ShadeGlobals 가 레이로 잡은 방 상자 안에서만 도트 문법(거른 텍스처·밝기 단·디더)을 쓴다.
    /// 집이 옮겨지거나 벽이 빠져 레이가 못 닿으면 상자가 없어져 조용히 옛 화면(사진 결·한 색 그늘)으로 돌아간다 — 여기서 잡는다.
    /// 상자는 방 소품 가운데를 품고, 반폭이 0.8~8 m(집 한 칸)여야 한다(벽을 지나쳐 바깥까지 먹으면 거리 풍경도 바뀐다).
    /// </summary>
    static List<string> IndoorDotBox()
    {
        var bad = new List<string>();
        var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
        if (g == null || !g.indoorDot) return bad;
        g.Apply();
        if (!g.IndoorReady) { bad.Add("방 상자 없음(" + g.roomName + ") — 방 소품 묶음 이름이나 벽 콜라이더 확인"); return bad; }
        Transform room = null;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == g.roomName && t.parent != null) { room = t; break; }
        var rs = room.GetComponentsInChildren<Renderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
        var c = g.IndoorBox.MultiplyPoint3x4(bb.center);
        if (Mathf.Max(Mathf.Abs(c.x), Mathf.Max(Mathf.Abs(c.y), Mathf.Abs(c.z))) > 1f) bad.Add("방 소품 가운데 " + V(bb.center) + " 가 방 상자 밖");
        var inv = g.IndoorBox.inverse;
        for (int a = 0; a < 3; a++)
        {
            var ax = Vector3.zero; ax[a] = 1f;
            float half = (inv.MultiplyPoint3x4(ax) - inv.MultiplyPoint3x4(Vector3.zero)).magnitude;
            if (half < 0.8f || half > 8f) bad.Add(string.Format(Inv, "방 상자 축{0} 반폭 {1:0.00} m — 0.8~8 m 밖(레이가 벽을 지나쳤거나 소품에 막힘)", a, half));
        }
        return bad;
    }

    /// <summary>
    /// D-304(과제 20261105-0359) 건물 안 전부 디오라마: A7ShadeGlobals.FindInteriors 가 찾은 건물 안마다 방 상자가 잡힌다.
    /// D198_Buildings 아래 켜진 건물마다 상자 ≥1 · 2층 방(이름 방) 있음 · 상자 반폭 0.8~10 m · 못 잡힌 곳은 이름과 이유.
    /// 건물이 옮겨지거나 바닥·벽 콜라이더가 빠지면 그 건물 안에서 조용히 1인칭으로 돌아간다 — 여기서 잡는다.
    /// </summary>
    static List<string> InteriorBoxes()
    {
        var bad = new List<string>();
        var list = A7ShadeGlobals.FindInteriors(null);
        if (!list.Exists(i => i.room != null)) bad.Add("2층 방(이름 방) 상자 없음");
        foreach (var it in list)
        {
            if (!it.ok) { bad.Add(it.name + ": 상자 못 잡음 — " + it.why); continue; }
            if (it.room != null) continue;   // 2층 방은 위 「실내 도트 방 상자」 검사가 본다
            float lo = Mathf.Min(it.half.x, Mathf.Min(it.half.y, it.half.z)), hi = Mathf.Max(it.half.x, Mathf.Max(it.half.y, it.half.z));
            if (lo < 0.8f || hi > 10f) bad.Add(string.Format(Inv, "{0}: 반폭 {1} — 0.8~10 m 밖", it.name, V(it.half)));
        }
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.name != A7ShadeGlobals.BuildingsRoot) continue;
            foreach (Transform b in t)
                if (b.gameObject.activeInHierarchy && !list.Exists(i => i.ok && i.building == b))
                    bad.Add(b.name + ": 건물 안 0 — 바닥(위 보는 면) 위 1 m 에서 천장·네 벽이 그 건물 콜라이더에 안 닿음");
        }
        return bad;
    }

    /// <summary>
    /// D-306(과제 20261105-0419) 건물 안 고정 시점. 고친 버그 셋을 다시 잡는다:
    /// ① 정비소(Library/A7LitPoint)에 컷이 없어 디오라마에서 빠짐 → 건물 안이 잡힌 건물마다 RoomDiorama.CanCut.
    /// ② 우체국 유리(URP Unlit, Q4_*_glass)가 GLASS_ 이름이 아니라 안 잘림 → 건물 안 Unlit 렌더러는 전부 RoomDiorama.IsGlass.
    /// ③ 고정 카메라가 상자 안이면 문 쪽 벽이 안 잘려 화면을 덮는다 → 작은 방 고정 자세가 상자 밖(수평 |q| > 1.05)·천장 위.
    /// </summary>
    static List<string> InteriorFixedView()
    {
        var bad = new List<string>();
        var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
        var doors = UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
        var rd = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>(FindObjectsInactive.Include);
        // 씬에 없으면(실행 중에만 붙는다) RoomDiorama 필드 기본값
        float pitch = rd != null ? rd.pitch : 50f, margin = rd != null ? rd.fitMargin : 0.05f, minCap = rd != null ? rd.minCapsule : 0.06f, capH = rd != null ? rd.capsuleHeight : 1.8f;
        DioramaLens(out float fov, out float aspect);
        var seen = new HashSet<Transform>();
        foreach (var it in list)
        {
            if (seen.Add(it.building))
            {
                if (!RoomDiorama.CanCut(it.building)) bad.Add(it.building.name + ": 컷 셰이더(" + string.Join(", ", RoomDiorama.CutShaders) + ") 재질 0 — 디오라마에서 빠진다");
                foreach (var r in it.building.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name.Contains("Unlit") && !RoomDiorama.IsGlass(r))
                        bad.Add(it.building.name + "/" + r.name + ": Unlit(" + r.sharedMaterial.name + ") 인데 유리로 안 잡힘 — 안 잘리고 남는다");
            }
            // 천장 컷이 바닥에서 2.7m 를 넘으면 높은 상자(정비소)의 지붕 처마가 안 잘리고 화면 절반을 덮는다
            float ceilY = RoomDiorama.CeilOf(it.box, rd != null ? rd.ceilCut : 0.9f, rd != null ? rd.ceilMetres : 2.6f);
            if ((ceilY + 1f) * it.half.y > 2.7f)
                bad.Add(string.Format(Inv, "{0}: 천장 컷 바닥 위 {1:F2}m — 2.7m 초과(지붕 처마가 남는다)", it.name, (ceilY + 1f) * it.half.y));
            float yaw = RoomDiorama.FixedYaw(it, list, doors, null);
            RoomDiorama.Fit(it.box.inverse, yaw, pitch, fov, aspect, margin, ceilY, capH, out Vector3 look, out float d, out float cap);
            if (cap < minCap) continue;   // 맞추면 사람이 너무 작은 넓은 곳은 플레이어 따라 움직인다
            Vector3 c = it.box.MultiplyPoint3x4(look - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * d);
            if (Mathf.Max(Mathf.Abs(c.x), Mathf.Abs(c.z)) <= 1.05f || c.y <= ceilY)
                bad.Add(string.Format(Inv, "{0}: 고정 카메라 상자 좌표 {1} — 상자 밖·천장 컷 위가 아님(문 쪽 벽이 안 잘린다)", it.name, V(c)));
        }
        return bad;
    }

    /// <summary>디오라마 카메라 시야각·화면비: 실행 중이면 그 카메라, 아니면 게임 기본 시야각(FollowCamera.BaseFov — 걷기도 같은 값) · 16:9.</summary>
    static void DioramaLens(out float fov, out float aspect)
    {
        var fc = UnityEngine.Object.FindFirstObjectByType<FollowCamera>(FindObjectsInactive.Include);
        var cam = Camera.main;
        fov = Application.isPlaying && cam != null ? cam.fieldOfView : fc != null ? fc.BaseFov : 60f;
        aspect = Application.isPlaying && cam != null ? cam.aspect : 16f / 9f;
    }

    /// <summary>
    /// D-307 ⑧(도현 11/05 「방안을 보여주는 카메라 각도도 방안이 다 보이게」): 작은 방(맞춘 자리에 고정되는 방)은 바닥 네 모서리가 화면 안.
    /// 옛 고정 거리 표(2층 방 5 m 등)로는 모서리가 화면 밖으로 잘렸다. RoomDiorama.Fit 이 준 자세로 따로 투영 행렬을 만들어 확인한다
    /// (맞춤 식이 틀려도 걸리게 — 식을 다시 쓰지 않음). 넓은 곳(사람 캡슐 < minCapsule)은 따라 움직이니 뺀다.
    /// </summary>
    static List<string> InteriorRoomInFrame()
    {
        var bad = new List<string>();
        var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
        var doors = UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
        var rd = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>(FindObjectsInactive.Include);
        float pitch = rd != null ? rd.pitch : 50f, margin = rd != null ? rd.fitMargin : 0.05f, minCap = rd != null ? rd.minCapsule : 0.06f, capH = rd != null ? rd.capsuleHeight : 1.8f;
        DioramaLens(out float fov, out float aspect);
        Matrix4x4 proj = Matrix4x4.Perspective(fov, aspect, 0.1f, 500f);
        int small = 0;
        foreach (var it in list)
        {
            float ceilY = RoomDiorama.CeilOf(it.box, rd != null ? rd.ceilCut : 0.9f, rd != null ? rd.ceilMetres : 2.6f);
            float yaw = RoomDiorama.FixedYaw(it, list, doors, null);
            RoomDiorama.Fit(it.box.inverse, yaw, pitch, fov, aspect, margin, ceilY, capH, out Vector3 look, out float d, out float cap);
            if (cap < minCap) continue;
            small++;
            var r = Quaternion.Euler(pitch, yaw, 0f);
            Matrix4x4 view = Matrix4x4.TRS(look - r * Vector3.forward * d, r, new Vector3(1f, 1f, -1f)).inverse;   // Unity 카메라 = -z 앞
            for (int k = 0; k < 4; k++)
            {
                Vector3 q = new Vector3((k & 1) == 0 ? -1f : 1f, -1f, (k & 2) == 0 ? -1f : 1f);
                Vector3 wp = it.box.inverse.MultiplyPoint3x4(q);
                Vector4 h = proj * (view * new Vector4(wp.x, wp.y, wp.z, 1f));
                if (h.w <= 0f || Mathf.Abs(h.x / h.w) > 1.001f || Mathf.Abs(h.y / h.w) > 1.001f)
                    bad.Add(string.Format(Inv, "{0}: 바닥 모서리 {1} 화면 좌표 ({2:F2},{3:F2}) w={4:F2} — 화면 밖(거리 {5:F1}m)", it.name, V(q), h.x / h.w, h.y / h.w, h.w, d));
            }
        }
        if (list.Count > 0 && small == 0) bad.Add("작은 방 0 — 모든 방이 넓은 곳으로 빠졌다(minCapsule·시야각 확인)");
        return bad;
    }

    /// <summary>
    /// 과제 20261105-0535 ②: 디오라마 시점에서 캡슐 앞을 가리는 벽이 없다. 방마다 맞춘 고정 카메라(RoomDiorama.Fit) → 사람 가슴(바닥 + 1.1 m, 상자 3×3 점)
    /// 레이에 걸린 그 건물 벽이 셰이더 컷(상자 밖 · 천장 위 · 카메라 쪽 컷선 RoomDiorama.CutLine 밖)에 안 잘리면 실패.
    /// 집 2층 복도(2F#2)는 문 쪽 벽 안쪽 면이 0.87 이라 옛 컷선(wallCut 0.95 하나)에 안 걸려 그 벽이 캡슐을 가렸다.
    /// </summary>
    static List<string> InteriorCapsuleClear()
    {
        var bad = new List<string>();
        var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok && RoomDiorama.CanCut(i.building));
        var doors = UnityEngine.Object.FindObjectsByType<DoorHinge>(FindObjectsSortMode.None);
        var rd = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>(FindObjectsInactive.Include);
        float pitch = rd != null ? rd.pitch : 50f, margin = rd != null ? rd.fitMargin : 0.05f, capH = rd != null ? rd.capsuleHeight : 1.8f, wallCut = rd != null ? rd.wallCut : 0.95f;
        DioramaLens(out float fov, out float aspect);
        int rays = 0;
        bool bf = Physics.queriesHitBackfaces;
        try
        {
            foreach (var it in list)
            {
                float ceilY = RoomDiorama.CeilOf(it.box, rd != null ? rd.ceilCut : 0.9f, rd != null ? rd.ceilMetres : 2.6f);
                float yaw = RoomDiorama.FixedYaw(it, list, doors, null);
                RoomDiorama.Fit(it.box.inverse, yaw, pitch, fov, aspect, margin, ceilY, capH, out Vector3 look, out float d, out _);
                Vector3 cp = look - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * d;
                float cut = RoomDiorama.CutLine(it.box, cp, A7ShadeGlobals.WallFaces(it), wallCut);
                for (int gx = -1; gx <= 1; gx++)
                    for (int gz = -1; gz <= 1; gz++)
                    {
                        // 바닥 = 상자 높이 가운데에서 아래로 그 건물 벽·바닥 메시(뒷면 포함 — 우체국 2층 한 면 바닥)
                        Physics.queriesHitBackfaces = true;
                        if (!A7ShadeGlobals.FirstWall(it.box.inverse.MultiplyPoint3x4(new Vector3(gx * 0.6f, 0f, gz * 0.6f)), Vector3.down, 8f, it.building, out var fl)) continue;
                        Vector3 chest = fl.point + Vector3.up * 1.1f, cb = it.box.MultiplyPoint3x4(chest);
                        if (it.box.MultiplyPoint3x4(fl.point).y < -1.05f || cb.y > ceilY) continue;
                        rays++;
                        string blk = A7ShadeGlobals.CapsuleBlocker(it, cp, chest, cut, ceilY);
                        if (blk != null) bad.Add(string.Format(Inv, "{0}: 카메라 {1} → 가슴 {2}(상자 {3}) 레이에 안 잘린 벽 {4} — 컷선 {5:F3}", it.name, V(cp), V(chest), cb.ToString("F2"), blk, cut));
                    }
            }
        }
        finally { Physics.queriesHitBackfaces = bf; }
        if (list.Count > 0 && rays == 0) bad.Add("잰 레이 0 — 바닥을 못 찾음");
        return bad;
    }

    /// <summary>
    /// 과제 20261105-0535 ③: 건물 안 방 바닥은 위에서 선다. 우체국 2층 바닥은 아래만 보는 한 면이라 순간이동 레이·캐릭터가 빠져 1층으로 떨어졌다.
    /// A7ShadeGlobals.PatchOneSidedFloors 를 붙인 뒤에는 「뒷면으로만 맞는 바닥」 방이 0 이어야 한다(에디트 모드면 붙였다 바로 지움).
    /// </summary>
    static List<string> InteriorFloorStandable()
    {
        var bad = new List<string>();
        var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
        int one = list.FindAll(i => A7ShadeGlobals.OneSidedFloor(i, out _)).Count;
        var made = A7ShadeGlobals.PatchOneSidedFloors(list);
        Physics.SyncTransforms();
        try
        {
            foreach (var it in list)
                if (A7ShadeGlobals.OneSidedFloor(it, out float y)) bad.Add(string.Format(Inv, "{0}: 바닥 y={1:F2} 이 아래만 보는 한 면 — 고침 콜라이더 뒤에도 위에서 못 섬(한 면 방 {2}개, 새로 붙임 {3})", it.name, y, one, made.Count));
        }
        finally
        {
            if (!Application.isPlaying) { foreach (var g in made) UnityEngine.Object.DestroyImmediate(g); Physics.SyncTransforms(); }
        }
        return bad;
    }

    /// <summary>
    /// 과제 20261105-0453 ①: 건물 문 밖 1 m 는 디오라마가 아니다. 정비소 벽(한 면짜리 메시)을 안에서 쏜 레이가 못 맞혀
    /// 상자가 왼쪽 벽 밖 3.9 m 까지 뻗었고, 밖에 서 있어도 디오라마로 바뀌었다.
    /// 건물 안 상자마다 바닥 위 1 m 에서 집 축 네 옆으로 벽(IsWall, 뒷면 포함)까지 쏘고, 그 벽 1 m 밖 · 문·셔터 양쪽 1 m 중
    /// 더 바깥에 그 건물 벽이 없는 점(= 건물 밖)이 어느 상자에도(≤1.02) 들거나, 방 사이 문턱 붙듦(RoomDiorama.Holds)에 걸리면 실패
    /// (집 1층 벽 밖 1 m 가 「같은 건물 다른 상자 1.3 안」이라 2층 방 디오라마가 남았다).
    /// </summary>
    static List<string> InteriorDoorOutside()
    {
        var bad = new List<string>();
        var list = A7ShadeGlobals.FindInteriors(null).FindAll(i => i.ok);
        bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
        try
        {
            bool WallHit(Transform bld, Vector3 o, Vector3 d, float far, out RaycastHit best)
            {
                best = default; bool got = false;
                foreach (var h in Physics.RaycastAll(o, d, far, ~0, QueryTriggerInteraction.Ignore))
                    if (h.transform.IsChildOf(bld) && A7ShadeGlobals.IsWall(h.collider) && (!got || h.distance < best.distance)) { best = h; got = true; }
                return got;
            }
            void Probe(Transform bld, Vector3 p0, Vector3 d, string what)
            {
                if (WallHit(bld, p0, d, 40f, out _)) return;   // 더 바깥에 그 건물 벽이 있다 = 옆 방(안 벽)
                Probe1(p0, what);
                // 2층 벽 밖은 허공 — 그 아래 땅에 선 사람(발 위 1 m = RoomDiorama 가 보는 점)도. 그 건물 차양·지붕 위는 못 서니 뺀다
                if (Physics.Raycast(p0, Vector3.down, out var g, 12f, ~0, QueryTriggerInteraction.Ignore) && g.distance > 0.5f && !g.transform.IsChildOf(bld)) Probe1(g.point + Vector3.up, what + "(아래 땅)");
            }
            void Probe1(Vector3 p, string what)
            {
                foreach (var o in list)
                {
                    var q = o.box.MultiplyPoint3x4(p);
                    if (Mathf.Max(Mathf.Abs(q.x), Mathf.Max(Mathf.Abs(q.y), Mathf.Abs(q.z))) <= 1.02f)
                    { bad.Add(string.Format(Inv, "{0} 밖 1 m {1} 가 {2} 상자 안(상자 좌표 {3}) — 밖에 서도 디오라마", what, V(p), o.name, V(q))); return; }
                }
                // 안에서 걸어 나올 때: 방 사이 문턱 붙듦(RoomDiorama.Holds)이 밖까지 지금 상자를 붙들면 안 된다
                for (int c = 0; c < list.Count; c++)
                    if (RoomDiorama.Holds(list, c, p)) { bad.Add(string.Format(Inv, "{0} 밖 1 m {1} 에서 {2} 상자를 문턱으로 붙듦 — 나와도 디오라마", what, V(p), list[c].name)); return; }
            }
            var seen = new HashSet<Transform>();
            foreach (var it in list)
            {
                var inv = it.box.inverse;
                Vector3 o = inv.MultiplyPoint3x4(new Vector3(0f, -1f, 0f)) + Vector3.up;
                foreach (var a in new[] { it.building.right, -it.building.right, it.building.forward, -it.building.forward })
                    if (WallHit(it.building, o, a, 40f, out var h)) Probe(it.building, h.point + a * 1f, a, it.name + " 벽");
                if (!seen.Add(it.building)) continue;
                foreach (var c in it.building.GetComponentsInChildren<Collider>())
                {
                    if (!c.enabled || c.isTrigger || !(c.name.StartsWith("DOOR") || c.name.StartsWith("SHUTTER"))) continue;
                    var b = c.bounds; var e = b.extents;
                    // 문짝의 얇은 축 = 문을 지나는 방향(집 축 x 또는 z 중 반폭이 작은 쪽)
                    Vector3 n = Mathf.Abs(Vector3.Dot(e, Abs(it.building.right))) < Mathf.Abs(Vector3.Dot(e, Abs(it.building.forward))) ? it.building.right : it.building.forward;
                    Vector3 p0 = new Vector3(b.center.x, b.min.y + 1f, b.center.z);
                    Probe(it.building, p0 + n * 1f, n, it.building.name + "/" + c.name);
                    Probe(it.building, p0 - n * 1f, -n, it.building.name + "/" + c.name);
                }
            }
        }
        finally { Physics.queriesHitBackfaces = bf; }
        return bad;
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    /// <summary>
    /// D-298 실내 디오라마(과제 20261104-1559): 기준 화면 07 = 잔소품 없음 · 원본 텍스처의 거친 결. 집 빌더나 17 의 RoomFlatBuild 를 다시 돌리면
    /// 잔소품이 되살아나고 단색 사본(_roomflat)이 다시 꽂힌다 — 그걸 잡는다. 남길 큰 덩어리(침대·책상·의자·일기장)가 같이 꺼져도 잡는다.
    /// </summary>
    static List<string> RoomDioramaProps()
    {
        var bad = new List<string>();
        var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
        if (g == null) return bad;
        Transform room = null;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == g.roomName && t.parent != null) { room = t; break; }
        if (room == null || !A7ShadeGlobals.TryRoomBox(room, out var box)) return bad;
        var keep = new HashSet<string>();
        foreach (var r in room.parent.GetComponentsInChildren<Renderer>(true))
        {
            var q = box.MultiplyPoint3x4(r.bounds.center);
            if (Mathf.Max(Mathf.Abs(q.x), Mathf.Max(Mathf.Abs(q.y), Mathf.Abs(q.z))) > 1.05f) continue;
            bool on = r.gameObject.activeInHierarchy;
            if (on && RoomDiorama.IsSmallProp(r.name)) bad.Add("잔소품 켜짐: " + r.name);
            foreach (var k in new[] { "BED_", "DESK_", "CHAIR_", "NOTEBOOK_" }) if (on && r.name.StartsWith(k)) keep.Add(k);
            if (!on) continue;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                if (m.name.EndsWith("_roomflat")) { bad.Add("단색 사본 재질: " + r.name + " ← " + m.name); continue; }
                if (m.shader != null && m.shader.name == "PitCrew/SceneA7/Lit" && m.GetTexture("_BaseMap") is Texture tx && tx.filterMode != FilterMode.Point) bad.Add("방 텍스처 Point 아님: " + r.name + " ← " + tx.name);
            }
        }
        foreach (var k in new[] { "BED_", "DESK_", "CHAIR_", "NOTEBOOK_" }) if (!keep.Contains(k)) bad.Add("남길 것 꺼짐/없음: " + k);
        if (bad.Count > 12) { int n = bad.Count; bad.RemoveRange(12, n - 12); bad.Add("… 외 " + (n - 12)); }
        return bad;
    }

    /// <summary>
    /// D-299(과제 20261104-1635): 19 의 창 고르기는 넓이만 봐서 바닥 가까이 내려오는 유리문(GLASS_17)을 창으로 골랐다.
    /// RoomDiorama.PickWindow 가 고른 창이 유리문이 아니고(아래 끝이 바닥 + DoorGlassFloor 위) 책상(DESK_*) 가까이 있는지 본다.
    /// </summary>
    static List<string> RoomDioramaWindow()
    {
        var bad = new List<string>();
        var g = UnityEngine.Object.FindFirstObjectByType<A7ShadeGlobals>();
        if (g == null) return bad;
        Transform room = null;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == g.roomName && t.parent != null) { room = t; break; }
        if (room == null || !A7ShadeGlobals.TryRoomBox(room, out var box)) return bad;
        if (!RoomDiorama.PickWindow(room, box, out var c, out var low)) { bad.Add("방 창(GLASS_*) 못 고름"); return bad; }
        if (low < RoomDiorama.DoorGlassFloor) bad.Add($"고른 창 아래 끝이 바닥 위 {low:F2} m — 유리문");
        Renderer desk = null;
        foreach (var r in room.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("DESK_")) { desk = r; break; }
        if (desk == null) bad.Add("책상 DESK_* 없음");
        else
        {
            var d = desk.bounds.center - c; d.y = 0f;
            if (d.magnitude > 2.5f) bad.Add($"고른 창이 책상에서 {d.magnitude:F1} m — 책상 창 아님");
        }
        return bad;
    }

    /// <summary>
    /// 과제 20261104-2033: 디오라마가 지붕·천장을 그림에서만 버리고 그림자 패스에선 그대로 그려서, 잘린 지붕 그림자가 방 전체를 덮었다
    /// → 낮·밤 방 밝기가 같았다(실측 전등 밖 L 0.158 / 0.158). A7 그림자 패스가 A7DioShadowCut 으로 지붕을 버리는지,
    /// 해가 꺼지면 밤 무게 1 인지, 시간대 바탕빛이 낮 ≥ 밤×2.5 · 저녁은 주황(R > B×1.3)인지 본다.
    /// </summary>
    static List<string> RoomDioramaTimeLight()
    {
        var bad = new List<string>();
        var sp = System.IO.Path.Combine(Application.dataPath, "Art/SceneA7/SceneA7Lit.shader");
        if (System.IO.File.Exists(sp))
        {
            var src = System.IO.File.ReadAllText(sp);
            if (!src.Contains("if (A7DioShadowCut(i.wp)) discard;")) bad.Add("SceneA7Lit 그림자 패스에 A7DioShadowCut 없음 — 잘린 지붕이 방에 그림자를 드리움");
        }
        RoomDiorama.TimeWeights(null, null, out _, out var nw);
        if (nw < 0.99f) bad.Add($"해 없음인데 밤 무게 {nw:F2} (기대 1)");
        // 디오라마는 Play 때 OnFootDirector 가 붙인다 — 없으면 숨긴 임시 오브젝트에 붙여 기본값을 본다(에디트 모드라 Awake 안 돎)
        var go = new GameObject("_inv_sun") { hideFlags = HideFlags.HideAndDontSave };
        var sun = go.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.35f;
        RoomDiorama.TimeWeights(null, sun, out var dw, out nw);
        var d = UnityEngine.Object.FindFirstObjectByType<RoomDiorama>(FindObjectsInactive.Include);
        if (d == null) d = go.AddComponent<RoomDiorama>();
        Color day = RoomDiorama.Mix(d.ambientDay, d.ambientDusk, d.ambientNight, 0, 0), night = RoomDiorama.Mix(d.ambientDay, d.ambientDusk, d.ambientNight, 0, 1);
        Color dusk = RoomDiorama.Mix(d.ambientDay, d.ambientDusk, d.ambientNight, 1, 0);
        UnityEngine.Object.DestroyImmediate(go);
        if (nw > 0.01f || dw > 0.01f) bad.Add($"해 켜짐(1.35)·노을 꺼짐인데 저녁 {dw:F2} · 밤 {nw:F2} (기대 0 · 0)");
        float Lum(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
        if (Lum(day) < 2.5f * Lum(night)) bad.Add($"바탕빛 낮 {Lum(day):F2} < 밤 {Lum(night):F2} × 2.5 — 낮밤 방이 같아 보임");
        if (dusk.r <= 1.3f * dusk.b) bad.Add($"저녁 바탕빛 R {dusk.r:F2} ≤ B {dusk.b:F2} × 1.3 — 주황 아님");
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
