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
        door.Always();    // 손에 든 상자·카트 짐칸 상자
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
        else if (State == Mode.Seated) ret.Tick(press);
        else if (State == Mode.Board) depot.Tick(press);
        else if (State == Mode.Pack) packing.Tick(press);
        else if (State == Mode.Calendar) morning.Tick(press);
        else if (State == Mode.Diary) dayEnd.Tick(press);
        else if (State == Mode.InCart) drive.Tick(press);
    }

    internal void SetPrompt(string s)
    {
        if (Prompt == s) return;
        Prompt = s;
        if (promptText != null) { promptText.text = s; promptRoot.SetActive(s.Length > 0); }
    }

    void OnDisable() { DayView.Active = false; }   // 화면: 감독이 꺼지면 예전처럼 주행 화면으로

    void OnDestroy()
    {
        DayView.Reset();
        EngineAudio.OnFootMute = false;   // 감독 없는 씬에선 엔진 소리 원래대로
        Unplug();
        foreach (var st in Stages) st?.Unplug();
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

    internal void SetCarControl(bool on)
    {
        car.SetMoveInput(Vector2.zero);
        if (carInput != null) carInput.enabled = on;
    }

    /// <summary>카트를 pos·yaw 에 세운다(땅 높이는 레이로). DestClarityShots 가 리플렉션으로 부른다(이름 그대로).</summary>
    internal void ParkCar(Vector3 pos, float yaw)
    {
        if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out var hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            pos.y = hit.point.y + 0.05f;
        var rot = Quaternion.Euler(0f, yaw, 0f);
        car.transform.SetPositionAndRotation(pos, rot);
        if (carRb != null) { carRb.position = pos; carRb.rotation = rot; carRb.linearVelocity = Vector3.zero; carRb.angularVelocity = Vector3.zero; }
        Physics.SyncTransforms();
    }
}
