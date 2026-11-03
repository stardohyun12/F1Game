using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    // ── D-251 보관소 포장 작업대 ──
    /// <summary>포장 시점(작업대 앞)으로 옮겨 가는 중/돌아오는 중.</summary>
    public bool PackMoving => packMove != null;
    /// <summary>작업대를 떠나려고 든 상자가 내려앉길 기다리는 중.</summary>
    bool packLeaving, simPackLeave;
    Coroutine packMove;
    Vector3 packEyePos; Quaternion packEyeRot;
    /// <summary>출발 때 정한 화물 상태 시작값(0~1, 포장 결과). 음수 = 아직.</summary>
    public float StartIntegrity { get; private set; } = -1f;

    // ── D-251: 보관소 포장 작업대(Proto_PackBench 그대로) ──
    /// <summary>작업대 앞에 서기: 카메라가 칠판 보기와 같은 방식으로 작업대 눈 자리로 옮겨 간 뒤 PackBench 가 마우스·시선을 맡는다. 몸은 그 자리.</summary>
    void EnterPack()
    {
        if (packSpot == null || packSpot.Eye == null) return;
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        State = Mode.Pack; packLeaving = false;
        player.enabled = false;
        footCam.enabled = false;
        packEyePos = cam.transform.position; packEyeRot = cam.transform.rotation;
        SetPrompt(""); PromptAnchor = null; GazeTarget = null;
        if (dotRoot != null) dotRoot.SetActive(false);
        var e = packSpot.Eye;
        if (packMove != null) StopCoroutine(packMove);
        packMove = StartCoroutine(MovePackCam(e.position, e.rotation, boardFocusSec, () => packSpot.SetPacking(true, cam)));
        Mark($"작업대 서기 눈 {e.position.ToString("F2")} 내려다봄 {e.eulerAngles.x:F0}°");
    }

    /// <summary>봇: 작업대에서 물러나기(Q 와 같은 길).</summary>
    public void SimPackLeave() { simPackLeave = true; }

    void UpdatePack()
    {
        PromptAnchor = null;
        var kb = Keyboard.current;
        bool leave = simPackLeave || (kb != null && kb.qKey.wasPressedThisFrame);
        simPackLeave = false;
        if (packMove != null) return;                                       // 옮겨 가는 동안은 입력 안 받음
        var bench = packSpot.Bench;
        if (leave && !packLeaving) { packLeaving = true; bench.ReleaseAll(); }
        if (packLeaving && bench.CanLeave) LeavePack();
    }

    void LeavePack()
    {
        packLeaving = false;
        packSpot.SetPacking(false);                                         // PackBench.OnDisable 가 커서 잠금을 푼다 → 걷기 카메라가 다시 잠금
        if (packMove != null) StopCoroutine(packMove);
        packMove = StartCoroutine(MovePackCam(packEyePos, packEyeRot, boardFocusSec * 0.6f, () =>
        {
            player.enabled = true;
            EnterFoot();
            footCam.HoldLook = false;
            Mark(packSpot.Ready ? $"작업대에서 물러남 — 흔들어 본 상자 있음(시작 {packSpot.StartIntegrity01 * 100f:F0}%)" : "작업대에서 물러남");
        }));
    }

    IEnumerator MovePackCam(Vector3 to, Quaternion toRot, float sec, System.Action done)
    {
        yield return MoveCam(to, toRot, sec, null);
        packMove = null;
        done?.Invoke();
    }

    /// <summary>걷기 시점에서 흔들어 본 상자를 든다 → 화면 아래 든 상자(HeldBox 대역), 카트에서 [E] 싣고 타기.</summary>
    void TakePacked()
    {
        packSpot.Take();
        CarryingBox = true;
        Mark($"작업대 상자 들기 rattle={packSpot.Result.rattle:F2} → 화물 시작 {packSpot.StartIntegrity01 * 100f:F0}% 예정");
        FocusThing(car.transform.position + Vector3.up * 0.7f, "카트");   // C-003: 다음 = 카트에 싣기
    }

    /// <summary>출발 직후(BeginRun 이 화물을 새것으로 채운 뒤) 포장 결과를 화물 상태 시작값으로.</summary>
    void ApplyPackResult()
    {
        if (packSpot == null || !packSpot.HasBench || !packSpot.Taken) return;
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        if (cargo == null) { Mark("포장 결과 적용 실패: CargoBox 없음"); return; }
        cargo.SetStartIntegrity(packSpot.StartIntegrity01);
        StartIntegrity = cargo.IntegrityPercent;
        var r = packSpot.Result;
        Mark($"출발 화물 상태 {StartIntegrity * 100f:F0}% ← 포장 rattle={r.rattle:F2} upright={r.upright} escaped={r.escaped} gap={r.gap:F3}");
    }
}
