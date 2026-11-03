using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-287 2단계: 보관소 단계 — 칠판(쪽지 집기·칠판 보기)과 손에 든 전표. 감독이 만들고 부른다(MonoBehaviour 아님).
/// 듣는 신호: BoardOpened(→ 칠판 보기 = Enter) · NotePicked(쪽지를 손에) · DayStarted(맡은 주문·전표 버리고 칠판 새로).
/// 지도: docs/world/CODEMAP.md
/// </summary>
public class Stage_Depot : DayStage
{
    public Stage_Depot(OnFootDirector d) : base(d) { }
    public override OnFootDirector.Mode Mode => OnFootDirector.Mode.Board;

    /// <summary>맡은 주문(없으면 null).</summary>
    internal OrderJob PickedJob;
    /// <summary>지금 손에 든 전표(칠판에서 집은 쪽지). 없으면 null.</summary>
    public HeldPaper Held { get; private set; }
    bool simBoardClose; int simBoardMove; bool simTab;
    OnFootDirector.Mode heldPaperMode = OnFootDirector.Mode.OnFoot;
    const float HeldPaperLiftSec = 0.6f;

    // 칠판 줄: 첫 주문 1줄(찻집·줄리아·달걀 한 판) + 대역 2줄 — 글은 도현이 정함.
    internal static readonly DepotOrderBoard.Line[] BoardLines =
    {
        new DepotOrderBoard.Line("달걀 한 판 → 언덕 위 찻집 (줄리아)", true),
        new DepotOrderBoard.Line("(주문 자리)", false),
        new DepotOrderBoard.Line("(주문 자리)", false),
    };

    public override void Plug()
    {
        base.Plug();
        Listen(Signal.BoardOpened, _ => Enter());
        Listen(Signal.NotePicked, a => { if (a is HeldPaper n) PickNote(n); });
    }

    protected override void NewDay()
    {
        PickedJob = null;
        if (Held != null) { Object.Destroy(Held.gameObject); Held = null; }   // D-269: 어제 전표는 버리고 칠판에 새 쪽지
        if (d.orderBoard != null) d.orderBoard.SetLines(BoardLines);
    }

    /// <summary>봇: 칠판에서 W(-1)/S(+1) 한 번, 닫기(Esc), Tab.</summary>
    public void SimBoardMove(int m) { simBoardMove = m; }
    public void SimBoardClose() { simBoardClose = true; }
    public void SimTab() { simTab = true; }
    /// <summary>디버그 창: HeldPaper.On 을 바꾼 뒤 칠판을 다시 쓴다(맡기 전에만).</summary>
    public void RefreshBoard() { if (d.orderBoard != null && PickedJob == null) d.orderBoard.SetLines(BoardLines); }

    // ── D-250: 주문 칠판 보기 ──
    /// <summary>칠판 보기 시점: 카메라가 boardFocusSec 동안 칠판 앞(DepotOrderBoard.ViewPose)으로 옮겨 가 정면으로 본다. 몸은 그 자리.</summary>
    public override void Enter()
    {
        d.StopDestFocus();
        d.State = OnFootDirector.Mode.Board;
        d.player.enabled = false;
        d.footCam.enabled = false;
        var cam = d.cam;
        d.boardEyePos = cam.transform.position; d.boardEyeRot = cam.transform.rotation;
        d.orderBoard.SetHint(true);
        d.SetPrompt(""); d.PromptAnchor = null; d.GazeTarget = null;
        if (d.dotRoot != null) d.dotRoot.SetActive(false);
        d.orderBoard.ViewPose(cam.fieldOfView, cam.aspect, out var pos, out var rot);
        if (d.boardMove != null) d.StopCoroutine(d.boardMove);
        d.boardMove = d.StartCoroutine(d.MoveCam(pos, rot, d.boardFocusSec, null));
        d.Mark($"칠판 보기 시점 {pos.ToString("F2")} 칠판까지 {Vector3.Distance(pos, d.orderBoard.Center):F2}m fov={cam.fieldOfView:F0}");
    }

    public override void Exit()
    {
        d.orderBoard.SetHint(false);
        if (d.boardMove != null) d.StopCoroutine(d.boardMove);
        d.boardMove = d.StartCoroutine(d.MoveCam(d.boardEyePos, d.boardEyeRot, d.boardFocusSec * 0.6f, () =>
        {
            d.player.enabled = true;
            d.EnterFoot();
            d.footCam.HoldLook = false;
            d.Mark("칠판 닫기");
            if (PickedJob != null) d.FocusNext(0f);
        }));
    }

    public override void Tick(bool press)
    {
        d.PromptAnchor = null;
        var kb = Keyboard.current;
        int mv = simBoardMove; simBoardMove = 0;
        if (kb != null) { if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) mv = -1; if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) mv = 1; }
        bool close = simBoardClose || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame));
        simBoardClose = false;
        if (d.boardMove != null) return;                                      // 옮겨 가는 동안은 입력 안 받음
        var board = d.orderBoard;
        if (mv != 0) board.Move(mv);
        if (close) { Exit(); return; }
        if (!press) return;
        if (PickedJob != null) { Exit(); return; }                          // 이미 맡았으면 E = 닫기
        if (!board.IsOpenLine(board.Selected)) { d.Mark($"칠판 {board.Selected + 1}줄 = 대역(주문 자리) — 못 맡음"); return; }
        board.Pick();
        PickedJob = MakeFirstJob();
        if (d.packSpot != null) d.packSpot.ShowCargo();                        // D-251: 맡은 화물(달걀 한 판)이 작업대에 놓인다
        d.Mark($"주문 맡음: {PickedJob.displayName} → 전표·목적지 표식 {d.firstDrop?.Label}");
        d.StartCoroutine(CloseAfter(0.6f));
    }

    IEnumerator CloseAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        if (d.State == OnFootDirector.Mode.Board && d.boardMove == null) Exit();
    }

    // ── D-269: 손에 든 종이(전표) ──
    void PickNote(HeldPaper note)
    {
        d.StopDestFocus();
        d.orderBoard.PickNote(note);
        PickedJob = MakeFirstJob();
        if (d.packSpot != null) d.packSpot.ShowCargo();
        note.SetSlip(PickedJob);
        note.TakeToHand(d.cam, HeldPaper.Pose.Up);
        Held = note;
        d.SetPrompt(""); d.PromptAnchor = null;
        d.Mark($"쪽지 집음: {PickedJob.displayName} → 손에 든 전표 · 목적지 표식 {d.firstDrop?.Label}");
        d.FocusNext(HeldPaperLiftSec);
    }

    /// <summary>Tab: 걷기·카트에서 들기 → 읽기 → 내려놓기. 대화·상자 들기·작업대·칠판·달력·일기에선 내려놓는다.</summary>
    public override void Always()
    {
        bool tab = simTab || (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame);
        simTab = false;
        if (Held == null) return;
        var st = d.State;
        if (st != heldPaperMode) { heldPaperMode = st; if (Held.State != HeldPaper.Pose.Down) Held.SetPose(HeldPaper.Pose.Down); }   // 타고 내릴 때 내려놓음
        bool free = (st == OnFootDirector.Mode.OnFoot && !d.InTalk && !d.Knocking && !d.CarryingBox && d.footCam != null && d.footCam.enabled) || st == OnFootDirector.Mode.InCart;
        if (!free) { if (Held.State != HeldPaper.Pose.Down) Held.SetPose(HeldPaper.Pose.Down); return; }
        if (tab) { Held.Cycle(); d.Mark($"전표 {Held.State}"); }
    }

    internal OrderJob MakeFirstJob()
    {
        var j = ScriptableObject.CreateInstance<OrderJob>();
        j.name = "TUT_FirstEggs"; j.displayName = "달걀 한 판 → 언덕 위 찻집"; j.cargoType = CargoType.Eggs;
        j.destinationLabels = new List<string> { d.firstDrop.Label }; j.timeLimitSec = 0f; j.baseReward = Rules.FirstOrderReward; j.timeBonus = 0;
        j.callerName = "언덕 위 찻집";
        j.recipientName = "줄리아";                                 // 사장 대사(BossLines)의 이름 — 도현 확인 필요(위 주석)
        return j;
    }
}
