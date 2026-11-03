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
        depot.Always();   // 손에 든 전표(Tab)
        SyncDay();
        if (moneyText != null && InTalk && handover != null && Speaker == handover.recipient) MoneyEnd = Mathf.Max(MoneyEnd, Time.unscaledTime + 1.2f);
        if (State == Mode.OnFoot)
        {
            if (InTalk) { PromptAnchor = null; UpdateTalk(press); return; }
            if (Knocking) { PromptAnchor = null; SetPrompt(""); return; }
            // 1인칭(D-219): 화면 가운데 점이 가리키는 물건만(D-287 2단계: 고르기는 Interactables/Gaze, 일은 물건이 쏜 신호를 듣는 쪽이).
            var it = gaze.Find();
            GazeTarget = gaze.Target;
            PromptAnchor = it?.Anchor;
            PromptLow = it != null && it.Low;
            SetPrompt(it?.Prompt ?? "");
            if (press && it != null && it.CanUse) it.Use();
        }
        else if (State == Mode.Seated)
        {
            PromptAnchor = null;
            if (press) StandUp();   // 앉아 있는 동안 화면에 아무것도 없음(D-227)
        }
        else if (State == Mode.Board) depot.Tick(press);
        else if (State == Mode.Pack) UpdatePack();
        else if (State == Mode.Calendar) UpdateCalendar(press);
        else if (State == Mode.Diary) dayEnd.Tick(press);
        else if (State == Mode.InCart)
        {
            bool slow = Mathf.Abs(car.CurrentSpeedMps) < exitMaxSpeed;
            PromptAnchor = car.transform.position + Vector3.up * 1.0f;
            SetPrompt(slow ? "E 내리기" : "");
            if (press && slow) { ArriveIfNear(); ExitCart(); }
        }
    }

    internal void SetPrompt(string s)
    {
        if (Prompt == s) return;
        Prompt = s;
        if (promptText != null) { promptText.text = s; promptRoot.SetActive(s.Length > 0); }
    }

    void OnDestroy()
    {
        EngineAudio.OnFootMute = false;   // 감독 없는 씬에선 엔진 소리 원래대로
        Unplug();
        depot?.Unplug(); dayEnd?.Unplug();
    }

    // ── 걷기 ↔ 카트 ──
    internal void EnterFoot()
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
