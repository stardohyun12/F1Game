using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-287 4단계: 포장 단계 — 보관소 작업대(D-251, Proto_PackBench 그대로) 서기·물러나기 · 상자 들기 · 출발 때 포장 결과 적용.
/// 감독이 만들고 부른다(MonoBehaviour 아님). 작업대 안(흔들기·점수)은 DepotPackSpot/PackBench 그대로 — 여기는 연결만.
/// 듣는 신호: PackOpened(물건 PackTable → 작업대 서기 = Pack) · BoxTaken(상자 듦, Stage_Door 다음) · OrderBegan(Stage_Drive → 포장 결과) · DayStarted.
/// 지도: docs/world/CODEMAP.md
/// </summary>
public class Stage_Packing : DayStage
{
    public Stage_Packing(OnFootDirector d) : base(d) { }
    public override OnFootDirector.Mode Mode => OnFootDirector.Mode.Pack;

    /// <summary>포장 시점(작업대 앞)으로 옮겨 가는 중/돌아오는 중.</summary>
    public bool Moving => move != null;
    /// <summary>작업대를 떠나려고 든 상자가 내려앉길 기다리는 중.</summary>
    bool leaving, simLeave;
    Coroutine move;
    /// <summary>D-294 2D 보드로 내려가고 올라오는 시간(초).</summary>
    const float BoardSec = 0.4f;
    Vector3 eyePos; Quaternion eyeRot;
    /// <summary>출발 때 정한 화물 상태 시작값(0~1, 포장 결과). 음수 = 아직.</summary>
    public float StartIntegrity { get; private set; } = -1f;

    public override void Plug()
    {
        base.Plug();
        Listen(Signal.PackOpened, _ => Enter());
        Listen(Signal.BoxTaken, _ => TakeBox());
        Listen(Signal.OrderBegan, _ => ApplyPackResult());
    }

    protected override void NewDay()
    {
        StartIntegrity = -1f;
        if (d.packSpot != null) d.packSpot.ResetForNextDay();
    }

    /// <summary>작업대 앞에 서기: 카메라가 칠판 보기와 같은 방식으로 작업대 눈 자리로 옮겨 간 뒤 PackBench 가 마우스·시선을 맡는다. 몸은 그 자리.</summary>
    public override void Enter()
    {
        var spot = d.packSpot;
        if (spot == null) return;
        var board = spot.Board2D;                                           // D-294: 2D 보드 스위치(use2D) 켜짐이면 윗면 시점
        if (board == null && spot.Eye == null) return;
        d.StopDestFocus();
        d.State = OnFootDirector.Mode.Pack; leaving = false;
        d.player.enabled = false;
        d.footCam.enabled = false;
        eyePos = d.cam.transform.position; eyeRot = d.cam.transform.rotation;
        d.SetPrompt(""); d.PromptAnchor = null; d.GazeTarget = null;
        if (d.dotRoot != null) d.dotRoot.SetActive(false);
        if (board != null)
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (board.GetComponent<PackBoardView>() == null) PackBoardView.Create(board, spot.BoardFont);
            board.CamPose(d.cam, out var bp, out var br);
            if (move != null) d.StopCoroutine(move);
            move = d.StartCoroutine(MoveCam(bp, br, BoardSec, () => board.SetOn(true, d.cam, spot.V3Table != null ? spot.V3Table.BoardParcel : null)));
            d.Mark($"작업대 2D 보드 눈 {bp.ToString("F2")} 위에서 내려봄");
            return;
        }
        var e = spot.Eye;
        if (move != null) d.StopCoroutine(move);
        move = d.StartCoroutine(MoveCam(e.position, e.rotation, d.boardFocusSec, () => spot.SetPacking(true, d.cam)));
        d.Mark($"작업대 서기 눈 {e.position.ToString("F2")} 내려다봄 {e.eulerAngles.x:F0}°");
    }

    /// <summary>봇: 작업대에서 물러나기(Q 와 같은 길).</summary>
    internal void SimLeave() { simLeave = true; }

    public override void Tick(bool press)
    {
        d.PromptAnchor = null;
        var kb = Keyboard.current;
        bool leave = simLeave || (kb != null && kb.qKey.wasPressedThisFrame);
        simLeave = false;
        if (move != null) return;                                           // 옮겨 가는 동안은 입력 안 받음
        var board = d.packSpot.Board2D;
        if (board != null) { if (board.TakeExit() || leave) Exit(); return; }   // D-294: Esc · 빈 자리 오른쪽 클릭(PackBoard) · 봇 SimLeave
        var bench = d.packSpot.Bench;
        if (leave && !leaving) { leaving = true; bench.ReleaseAll(); }
        if (leaving && bench.CanLeave) Exit();
    }

    public override void Exit()
    {
        var spot = d.packSpot;
        leaving = false;
        var board = spot.Board2D;
        if (board != null) board.SetOn(false, null, null);
        spot.SetPacking(false);                                             // PackBench.OnDisable 가 커서 잠금을 푼다 → 걷기 카메라가 다시 잠금
        if (move != null) d.StopCoroutine(move);
        move = d.StartCoroutine(MoveCam(eyePos, eyeRot, board != null ? BoardSec : d.boardFocusSec * 0.6f, () =>
        {
            d.player.enabled = true;
            d.EnterFoot();
            d.footCam.HoldLook = false;
            d.Mark(spot.Ready ? $"작업대에서 물러남 — 흔들어 본 상자 있음(시작 {spot.StartIntegrity01 * 100f:F0}%)" : "작업대에서 물러남");
        }));
    }

    /// <summary>개발 디버그 창: 작업대 화면에서 바로 빠져나온다(눈 자리 되돌림·몸 켬). 걷기 모드는 부른 쪽이 켠다.</summary>
    internal void Abort()
    {
        if (move != null) { d.StopCoroutine(move); move = null; }
        if (d.packSpot.Bench != null) d.packSpot.Bench.ReleaseAll();
        d.packSpot.SetPacking(false); leaving = false;
        if (d.packSpot.Board2D != null) d.packSpot.Board2D.SetOn(false, null, null);
        d.cam.transform.SetPositionAndRotation(eyePos, eyeRot);
        d.player.enabled = true;
    }

    IEnumerator MoveCam(Vector3 to, Quaternion toRot, float sec, System.Action done)
    {
        yield return d.MoveCam(to, toRot, sec, null);
        move = null;
        done?.Invoke();
    }

    /// <summary>걷기 시점에서 흔들어 본 상자를 든다 → 화면 아래 든 상자(HeldBox 대역, Stage_Door 가 BoxTaken 을 듣고 켬), 카트에서 [E] 싣고 타기.</summary>
    void TakeBox()
    {
        var spot = d.packSpot;
        spot.Take();
        d.Mark($"작업대 상자 들기 rattle={spot.Result.rattle:F2} → 화물 시작 {spot.StartIntegrity01 * 100f:F0}% 예정");
        d.FocusThing(d.car.transform.position + Vector3.up * 0.7f, "카트");   // C-003: 다음 = 카트에 싣기
    }

    /// <summary>출발 직후(BeginRun 이 화물을 새것으로 채운 뒤) 포장 결과를 화물 상태 시작값으로.</summary>
    void ApplyPackResult()
    {
        var spot = d.packSpot;
        if (spot == null || !spot.HasBench || !spot.Taken) return;
        var cargo = Services.Cargo as CargoBox ?? Object.FindFirstObjectByType<CargoBox>();
        if (cargo == null) { d.Mark("포장 결과 적용 실패: CargoBox 없음"); return; }
        cargo.SetStartIntegrity(spot.StartIntegrity01);
        StartIntegrity = cargo.IntegrityPercent;
        var r = spot.Result;
        d.Mark($"출발 화물 상태 {StartIntegrity * 100f:F0}% ← 포장 rattle={r.rattle:F2} upright={r.upright} escaped={r.escaped} gap={r.gap:F3}");
    }
}
