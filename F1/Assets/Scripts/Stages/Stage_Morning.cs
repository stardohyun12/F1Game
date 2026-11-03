using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    public RoomCalendar Calendar => calendar;
    /// <summary>달력 보기 시점으로 옮겨 가는 중/돌아오는 중(QA).</summary>
    public bool CalendarMoving => State == Mode.Calendar && boardMove != null;
    /// <summary>SPEC §11-4: 옛 배달 지점(AlleyEnd·CoastCafe·HairpinOrchard·LoneHouse — DP_ 접두가 아닌 DeliveryPoint)은 Play 때 끈다.</summary>
    public static bool IsLegacyPoint(DeliveryPoint dp) => dp != null && !dp.name.StartsWith("DP_") && dp.transform.parent != null && dp.transform.parent.name == "DeliverySystem";
    /// <summary>SPEC §11-4: 옛 배달 지점 4곳을 끈다(DeliveryManager.Start 가 켠 뒤 — Start 는 한 프레임 늦게 돈다).</summary>
    void HideLegacyPoints()
    {
        int n = 0;
        foreach (var dp in FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None))
            if (IsLegacyPoint(dp)) { dp.gameObject.SetActive(false); n++; }
        if (n > 0) Mark($"옛 배달 지점 {n}곳 끔(SPEC §11-4)");
    }

    // ── 과제 20261102-1951: 방 달력 보기 · 하루 끝 일기장 · 다음 날 아침 ──
    /// <summary>달력 보기 시점: 칠판 보기와 같은 방식(카메라가 달력 앞 ViewPose 로). 몸은 그 자리.</summary>
    void OpenCalendar()
    {
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        State = Mode.Calendar;
        player.enabled = false;
        footCam.enabled = false;
        boardEyePos = cam.transform.position; boardEyeRot = cam.transform.rotation;
        calendar.SetHint(true);
        SetPrompt(""); PromptAnchor = null; GazeTarget = null;
        if (dotRoot != null) dotRoot.SetActive(false);
        calendar.ViewPose(cam.fieldOfView, cam.aspect, out var pos, out var rot);
        if (boardMove != null) StopCoroutine(boardMove);
        boardMove = StartCoroutine(MoveCam(pos, rot, boardFocusSec, null));
        Mark($"달력 보기 시점 {pos.ToString("F2")} 달력까지 {Vector3.Distance(pos, calendar.Center):F2}m — {calendar.Written}");
    }

    void CloseCalendar()
    {
        calendar.SetHint(false);
        if (boardMove != null) StopCoroutine(boardMove);
        boardMove = StartCoroutine(MoveCam(boardEyePos, boardEyeRot, boardFocusSec * 0.6f, () =>
        {
            player.enabled = true;
            EnterFoot();
            footCam.HoldLook = false;
            Mark("달력 닫기");
        }));
    }

    /// <summary>봇: 달력 닫기 · 일기 덮기.</summary>
    public void SimCalendarClose() { simCalClose = true; }
    void UpdateCalendar(bool press)
    {
        PromptAnchor = null;
        var kb = Keyboard.current;
        bool close = press || simCalClose || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame));
        simCalClose = false;
        if (boardMove != null) return;                                      // 옮겨 가는 동안은 입력 안 받음
        if (close) CloseCalendar();
    }
}
