using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    /// <summary>대화 끝 시선 이동 중(QA·캡처용).</summary>
    public bool DestFocusing => destFocus != null;
    Coroutine talkFocus, destFocus;
    // 칠판·달력이 같이 쓰는 카메라 핸들(옮겨 가는 중이면 not null)과 돌아올 눈 자리.
    internal Coroutine boardMove;
    internal Vector3 boardEyePos; internal Quaternion boardEyeRot;

    /// <summary>목적지·물건 쪽 시선 돌리기를 멈춘다(다른 화면을 열 때).</summary>
    internal void StopDestFocus() { if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; } }

    internal IEnumerator MoveCam(Vector3 to, Quaternion toRot, float sec, System.Action done)
    {
        Vector3 p0 = cam.transform.position; Quaternion r0 = cam.transform.rotation;
        for (float t = 0f; t < sec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / sec);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(p0, to, k), Quaternion.Slerp(r0, toRot, k));
            yield return null;
        }
        cam.transform.SetPositionAndRotation(to, toRot);
        boardMove = null;
        done?.Invoke();
    }

    // ── C-003: 첫 배달 이음새 — 한 단계를 마치면 다음 할 곳 쪽으로 시선이 한 번 돈다(D-252 목적지 시선과 같은 방식) ──
    /// <summary>주문을 맡은 뒤: 아직 안 쌌으면 작업대(가서 [E] 포장하기), 쌌으면 예전처럼 목적지 표식.</summary>
    internal void FocusNext(float delay)
    {
        if (destFocusSec <= 0f) return;
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        if (DepotFlow && packSpot != null && !Packed) destFocus = StartCoroutine(FocusAfter(delay, packSpot.Top - Vector3.up * 0.15f, "작업대"));
        else if (WalkDest != null && UseDot) destFocus = StartCoroutine(FocusDestAfter(delay));
    }

    /// <summary>D-252: 사장 대화 끝 — 목적지 표식(WalkDest) 쪽으로 시선 한 번.</summary>
    internal void FocusWalkDest()
    {
        if (destFocusSec > 0f && WalkDest != null && UseDot) destFocus = StartCoroutine(FocusDest(WalkDest));
    }

    /// <summary>가까운 물건(작업대·카트·문) 쪽으로 — 위아래도 끝까지 따라간다.</summary>
    internal void FocusThing(Vector3 at, string what)
    {
        if (destFocusSec <= 0f) return;
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        destFocus = StartCoroutine(FocusAfter(0f, at, what));
    }

    IEnumerator FocusAfter(float sec, Vector3 at, string what)
    {
        if (sec > 0f) yield return new WaitForSecondsRealtime(sec);
        if (State == Mode.OnFoot && !InTalk) yield return FocusLook(at, 1f, what);
        destFocus = null;
    }

    IEnumerator FocusDestAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        if (State == Mode.OnFoot && WalkDest != null) yield return FocusDest(WalkDest);
    }

    /// <summary>
    /// D-252: 목적지 표식이 처음 뜨는 순간(사장이 "언덕 위 찻집"이라고 말한 뒤) 시선이 한 번 그쪽으로 — FocusSpeaker 와 같은 방식(destFocusSec 동안 SmoothStep).
    /// 멀리(수백 m) 있어도 표식은 도트 UI 라 건물에 안 가린다. 위아래는 반만 따라간다(먼 점을 정확히 보면 하늘·땅만 보여서).
    /// </summary>
    IEnumerator FocusDest(DeliveryPoint target) => FocusLook(DotScreenUI.DestAnchorOf(target), 0.5f, "목적지");

    /// <summary>FocusDest 와 같은 시선 돌리기 — 점 at 쪽으로. pitchK = 위아래를 얼마나 따라가나(먼 표식 0.5, 가까운 물건 1).</summary>
    IEnumerator FocusLook(Vector3 at, float pitchK, string what)
    {
        footCam.HoldLook = true;
        float y0 = footCam.Yaw, p0 = footCam.Pitch;
        footCam.LookAt(at);
        float dy = Mathf.DeltaAngle(y0, footCam.Yaw), p1 = Mathf.Lerp(p0, footCam.Pitch, pitchK);
        Mark($"{what} 시선 {dy:F1}° 옆 / {p1 - p0:F1}° 위아래, {destFocusSec:F2}s + {destHoldSec:F2}s");
        for (float t = 0f; t < destFocusSec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / destFocusSec);
            footCam.Yaw = y0 + dy * k; footCam.Pitch = Mathf.Lerp(p0, p1, k);
            yield return null;
        }
        footCam.Yaw = y0 + dy; footCam.Pitch = p1;
        yield return new WaitForSecondsRealtime(destHoldSec);
        footCam.HoldLook = false;
        destFocus = null;
    }
}
