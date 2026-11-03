using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    void Update()
    {
        if (State == Mode.Starting) return;
        if (overlayUI != null && overlayUI.enabled == UseDot) overlayUI.enabled = !UseDot;   // 도트 UI 가 켜져 있으면 예전 화면 안내·자막을 끈다
        PromptLow = false;
        TrackStep();
        bool press = InteractPressed();
        UpdateHeldBox();
        UpdateBedBox();
        UpdateHeldPaper();
        if (moneyText != null && InTalk && handover != null && Speaker == handover.recipient) MoneyEnd = Mathf.Max(MoneyEnd, Time.unscaledTime + 1.2f);
        if (State == Mode.OnFoot)
        {
            if (InTalk) { PromptAnchor = null; UpdateTalk(press); return; }
            if (Knocking) { PromptAnchor = null; SetPrompt(""); return; }
            // 1인칭(D-219): 화면 가운데 점이 가리키는 것만 — 시선 레이 2 m 에 처음 맞은 것이 문·카트일 때.
            Object hit = Gaze(out DoorHinge door);
            // 첫 한 판 메모 1: 상자를 들고 카트 2 m 안이면 어디를 보든 카트가 대상 — 시선 각도 때문에 칩이 안 뜨던 것을 없앤다.
            if (hit == null && DepotFlow && CarryingBox && !OrderStarted && NearCart(CartLoadReach)) hit = car;
            GazeTarget = hit != null ? hit.name : null;
            PromptAnchor = hit == null ? (Vector3?)null : hit == car ? car.transform.position + Vector3.up * 0.8f : hit == boss ? TopOf(boss)
                         : orderBoard != null && hit == orderBoard ? orderBoard.Center : packSpot != null && hit == packSpot ? packSpot.Top
                         : handover != null && hit == handover ? handover.DoorCenter
                         : calendar != null && hit == calendar ? calendar.PromptPoint : bed != null && hit == bed ? bed.Anchor : CenterOf(door);
            if (handover != null && hit == handover)
            {
                // D-250 ③: 상자를 들고 있을 때만 두드린다. 건네기 전·후엔 그냥 문.
                if (CarryingBox && !handover.Busy && !handover.IsOpen)
                {
                    SetPrompt("E 두드리기");
                    if (press) Knock();
                }
                else { SetPrompt(""); PromptAnchor = null; }
            }
            else if (boss != null && hit == boss)
            {
                SetPrompt("E 말하기");
                if (press) BeginTalk();
            }
            else if (orderBoard != null && hit == orderBoard && orderBoard.NotesOn)
            {
                // D-269: 칠판에 꽂힌 쪽지를 집으면 그 쪽지가 손에 든 전표가 된다. [E] 칩은 쪽지 곁.
                var note = orderBoard.NearestNote(gazePoint);
                if (note != null && !OrderPicked)
                {
                    PromptAnchor = DepotOrderBoard.NoteChipAnchor(note);
                    SetPrompt("E 쪽지 집기");
                    if (press) PickNote(note);
                }
                else { SetPrompt(""); PromptAnchor = null; }
            }
            else if (orderBoard != null && hit == orderBoard)
            {
                SetPrompt(OrderPicked ? "E 칠판 보기(맡음)" : "E 칠판 보기");
                PromptLow = true;
                if (press) OpenBoard();
            }
            else if (packSpot != null && hit == packSpot && packSpot.HasBench)
            {
                // D-251: 작업대 — 서서 싸고 흔들어 본 상자를 들고 나간다
                if (!OrderPicked) SetPrompt("칠판에서 주문 먼저");
                else if (packSpot.Taken) { SetPrompt(""); PromptAnchor = null; }
                else if (packSpot.Ready && !CarryingBox)
                {
                    SetPrompt("E 상자 들기");
                    if (press) TakePacked();
                }
                else
                {
                    SetPrompt(packSpot.NeedsShake ? "E 작업대(들어서 흔들어 보기)" : "E 포장하기");
                    if (press) EnterPack();
                }
            }
            else if (packSpot != null && hit == packSpot)
            {
                if (!OrderPicked) SetPrompt("칠판에서 주문 먼저");
                else if (!packSpot.Packed)
                {
                    SetPrompt("E 포장하기");
                    if (press) { packSpot.Pack(); Mark("포장됨(대역 — E 한 번)"); }
                }
                else SetPrompt("포장됨 — 카트로");
            }
            else if (calendar != null && hit == calendar)
            {
                SetPrompt("E 달력 보기");
                if (press) OpenCalendar();
            }
            else if (bed != null && hit == bed)
            {
                // 하루 끝: 오늘 배달(문 앞 건네기)을 마친 뒤에만 눕는다.
                if (HandedOver) { SetPrompt("E 자기 (일기)"); if (press) OpenDiary(); }
                else SetPrompt("오늘 배달 먼저");
            }
            else if ((hit == car || hit == null) && RepairSpot.Ready(player.transform.position, car))
            {
                // C-001: 정비 자리 안에 상한 카트와 같이 서 있으면 타기보다 고치기가 먼저
                PromptAnchor = car.transform.position + Vector3.up * 0.8f;
                SetPrompt("E 고치기");
                if (press) RepairCart();
            }
            else if (hit == car)
            {
                // 기능 2: 사장 말을 듣기 전엔 못 탄다. D-250: 주문을 맡았으면 포장해야 싣는다.
                if (CartNeedsBossTalk && !Talked) SetPrompt("사장님한테 먼저");
                else
                {
                    string chip = CartChip(DepotFlow, OrderPicked, Packed, OrderStarted);
                    SetPrompt(chip);
                    if (press && chip.StartsWith("E ")) EnterCart();
                }
            }
            else if (door != null)
            {
                SetPrompt(door.IsOpen ? "E 닫기" : "E 열기");
                if (press && !door.Moving) { door.Toggle(player.transform.position); Mark((door.IsOpen ? "열기 " : "닫기 ") + door.name); }
            }
            else if (NearSpot() is FishingSpot spot)
            {
                SetPrompt("E 앉기");
                PromptAnchor = spot.transform.position + Vector3.up * 0.6f;
                PromptLow = true;
                if (press) Sit(spot);
            }
            else SetPrompt("");
        }
        else if (State == Mode.Seated)
        {
            PromptAnchor = null;
            if (press) StandUp();   // 앉아 있는 동안 화면에 아무것도 없음(D-227)
        }
        else if (State == Mode.Board) UpdateBoard(press);
        else if (State == Mode.Pack) UpdatePack();
        else if (State == Mode.Calendar) UpdateCalendar(press);
        else if (State == Mode.Diary) UpdateDiary(press);
        else if (State == Mode.InCart)
        {
            bool slow = Mathf.Abs(car.CurrentSpeedMps) < exitMaxSpeed;
            PromptAnchor = car.transform.position + Vector3.up * 1.0f;
            SetPrompt(slow ? "E 내리기" : "");
            if (press && slow) { ArriveIfNear(); ExitCart(); }
        }
    }

    void SetPrompt(string s)
    {
        if (Prompt == s) return;
        Prompt = s;
        if (promptText != null) { promptText.text = s; promptRoot.SetActive(s.Length > 0); }
    }

    void OnDestroy() { EngineAudio.OnFootMute = false; }   // 감독 없는 씬에선 엔진 소리 원래대로

    // ── 걷기 ↔ 카트 ──
    void EnterFoot()
    {
        State = Mode.OnFoot;
        EngineAudio.OnFootMute = true;
        if (follow != null) follow.enabled = false;
        player.gameObject.SetActive(true);
        footCam.Yaw = player.transform.eulerAngles.y;
        footCam.enabled = true;
        if (dotRoot != null) dotRoot.SetActive(true);
        PlacePrompt(true);
        SetHud(false);
    }
}
