using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    /// <summary>칠판 보기 시점으로 옮겨 가는 중/돌아오는 중(QA·캡처용).</summary>
    public bool BoardMoving => boardMove != null;
    OrderJob pickedJob;
    Coroutine boardMove;
    Vector3 boardEyePos; Quaternion boardEyeRot;
    bool simBoardClose; int simBoardMove;
    // 칠판 줄: 첫 주문 1줄(찻집·줄리아·달걀 한 판) + 대역 2줄 — 글은 도현이 정함.
    static readonly DepotOrderBoard.Line[] BoardLines =
    {
        new DepotOrderBoard.Line("달걀 한 판 → 언덕 위 찻집 (줄리아)", true),
        new DepotOrderBoard.Line("(주문 자리)", false),
        new DepotOrderBoard.Line("(주문 자리)", false),
    };
    // ── D-250: 주문 칠판 보기 ──
    /// <summary>칠판 보기 시점: 카메라가 boardFocusSec 동안 칠판 앞(DepotOrderBoard.ViewPose)으로 옮겨 가 정면으로 본다. 몸은 그 자리.</summary>
    void OpenBoard()
    {
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        State = Mode.Board;
        player.enabled = false;
        footCam.enabled = false;
        boardEyePos = cam.transform.position; boardEyeRot = cam.transform.rotation;
        orderBoard.SetHint(true);
        SetPrompt(""); PromptAnchor = null; GazeTarget = null;
        if (dotRoot != null) dotRoot.SetActive(false);
        orderBoard.ViewPose(cam.fieldOfView, cam.aspect, out var pos, out var rot);
        if (boardMove != null) StopCoroutine(boardMove);
        boardMove = StartCoroutine(MoveCam(pos, rot, boardFocusSec, null));
        Mark($"칠판 보기 시점 {pos.ToString("F2")} 칠판까지 {Vector3.Distance(pos, orderBoard.Center):F2}m fov={cam.fieldOfView:F0}");
    }

    void CloseBoard()
    {
        orderBoard.SetHint(false);
        if (boardMove != null) StopCoroutine(boardMove);
        boardMove = StartCoroutine(MoveCam(boardEyePos, boardEyeRot, boardFocusSec * 0.6f, () =>
        {
            player.enabled = true;
            EnterFoot();
            footCam.HoldLook = false;
            Mark("칠판 닫기");
            if (OrderPicked) FocusNext(0f);
        }));
    }

    /// <summary>봇: 칠판에서 W(-1)/S(+1) 한 번, 닫기(Esc).</summary>
    public void SimBoardMove(int d) { simBoardMove = d; }
    public void SimBoardClose() { simBoardClose = true; }

    void UpdateBoard(bool press)
    {
        PromptAnchor = null;
        var kb = Keyboard.current;
        int mv = simBoardMove; simBoardMove = 0;
        if (kb != null) { if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) mv = -1; if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) mv = 1; }
        bool close = simBoardClose || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame));
        simBoardClose = false;
        if (boardMove != null) return;                                      // 옮겨 가는 동안은 입력 안 받음
        if (mv != 0) orderBoard.Move(mv);
        if (close) { CloseBoard(); return; }
        if (!press) return;
        if (OrderPicked) { CloseBoard(); return; }                          // 이미 맡았으면 E = 닫기
        if (!orderBoard.IsOpenLine(orderBoard.Selected)) { Mark($"칠판 {orderBoard.Selected + 1}줄 = 대역(주문 자리) — 못 맡음"); return; }
        orderBoard.Pick();
        pickedJob = MakeFirstJob();
        if (packSpot != null) packSpot.ShowCargo();                        // D-251: 맡은 화물(달걀 한 판)이 작업대에 놓인다
        Mark($"주문 맡음: {pickedJob.displayName} → 전표·목적지 표식 {firstDrop?.Label}");
        StartCoroutine(CloseBoardAfter(0.6f));
    }

    IEnumerator CloseBoardAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        if (State == Mode.Board && boardMove == null) CloseBoard();
    }

    // ── D-269: 손에 든 종이(전표) ──
    /// <summary>지금 손에 든 전표(칠판에서 집은 쪽지). 없으면 null.</summary>
    public HeldPaper Held { get; private set; }
    Vector3 gazePoint;
    bool simTab;
    Mode heldPaperMode = Mode.OnFoot;
    /// <summary>봇: 이번 프레임에 Tab 을 누른 것과 같다.</summary>
    public void SimTab() { simTab = true; }
    /// <summary>디버그 창: HeldPaper.On 을 바꾼 뒤 칠판을 다시 쓴다(맡기 전에만).</summary>
    public void RefreshBoard() { if (orderBoard != null && !OrderPicked) orderBoard.SetLines(BoardLines); }

    void PickNote(HeldPaper note)
    {
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        orderBoard.PickNote(note);
        pickedJob = MakeFirstJob();
        if (packSpot != null) packSpot.ShowCargo();
        note.SetSlip(pickedJob);
        note.TakeToHand(cam, HeldPaper.Pose.Up);
        Held = note;
        SetPrompt(""); PromptAnchor = null;
        Mark($"쪽지 집음: {pickedJob.displayName} → 손에 든 전표 · 목적지 표식 {firstDrop?.Label}");
        FocusNext(HeldPaperLiftSec);
    }

    const float HeldPaperLiftSec = 0.6f;
    /// <summary>Tab: 걷기·카트에서 들기 → 읽기 → 내려놓기. 대화·상자 들기·작업대·칠판·달력·일기에선 내려놓는다.</summary>
    void UpdateHeldPaper()
    {
        bool tab = simTab || (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame);
        simTab = false;
        if (Held == null) return;
        if (State != heldPaperMode) { heldPaperMode = State; if (Held.State != HeldPaper.Pose.Down) Held.SetPose(HeldPaper.Pose.Down); }   // 타고 내릴 때 내려놓음
        bool free = (State == Mode.OnFoot && !InTalk && !Knocking && !CarryingBox && footCam != null && footCam.enabled) || State == Mode.InCart;
        if (!free) { if (Held.State != HeldPaper.Pose.Down) Held.SetPose(HeldPaper.Pose.Down); return; }
        if (tab) { Held.Cycle(); Mark($"전표 {Held.State}"); }
    }

    OrderJob MakeFirstJob()
    {
        var j = ScriptableObject.CreateInstance<OrderJob>();
        j.name = "TUT_FirstEggs"; j.displayName = "달걀 한 판 → 언덕 위 찻집"; j.cargoType = CargoType.Eggs;
        j.destinationLabels = new List<string> { firstDrop.Label }; j.timeLimitSec = 0f; j.baseReward = Rules.FirstOrderReward; j.timeBonus = 0;
        j.callerName = "언덕 위 찻집";
        j.recipientName = "줄리아";                                 // 사장 대사(BossLines)의 이름 — 도현 확인 필요(위 주석)
        return j;
    }
}
