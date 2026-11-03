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
    IEnumerator MoveCam(Vector3 to, Quaternion toRot, float sec, System.Action done)
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
    void FocusNext(float delay)
    {
        if (destFocusSec <= 0f) return;
        if (destFocus != null) { StopCoroutine(destFocus); destFocus = null; footCam.HoldLook = false; }
        if (DepotFlow && packSpot != null && !Packed) destFocus = StartCoroutine(FocusAfter(delay, packSpot.Top - Vector3.up * 0.15f, "작업대"));
        else if (WalkDest != null && UseDot) destFocus = StartCoroutine(FocusDestAfter(delay));
    }

    /// <summary>가까운 물건(작업대·카트·문) 쪽으로 — 위아래도 끝까지 따라간다.</summary>
    void FocusThing(Vector3 at, string what)
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

    /// <summary>
    /// 눈(카메라)에서 앞으로 gazeReach 만큼 레이. 내 몸을 뺀 첫 번째 맞은 것이 문짝(또는 문과 같이 도는 손잡이)이면 그 문, 차 몸통이면 차.
    /// 벽·문틀이 먼저 맞으면 아무것도 아니다 — 벽 너머 문은 안 잡힌다.
    /// </summary>
    Object Gaze(out DoorHinge door)
    {
        door = null;
        Transform eye = cam.transform;
        int n = Physics.RaycastNonAlloc(eye.position, eye.forward, gazeHits, Mathf.Max(gazeReach, boss != null || orderBoard != null || handover != null ? talkReach : 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Collider first = null; float best = float.MaxValue; float packD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var c = gazeHits[i].collider;
            if (c == null || c.transform.IsChildOf(player.transform)) continue;
            if (gazeHits[i].distance < best) { best = gazeHits[i].distance; first = c; gazePoint = gazeHits[i].point; }
            if (packSpot != null && c.transform.IsChildOf(packSpot.transform)) packD = Mathf.Min(packD, gazeHits[i].distance);
        }
        // 방 달력(벽에 붙은 판)·침대(충돌체 없이 경계 상자) — 앞에 다른 것이 먼저 맞으면 못 잡는다.
        if (calendar != null && first != null && first.transform.IsChildOf(calendar.transform) && best <= talkReach) return calendar;
        if (bed != null && bed.Hit(new Ray(eye.position, eye.forward), Mathf.Min(best + 0.02f, talkReach), out _)) return bed;
        // 작업대 판(BenchTop)은 카운터 윗면과 같은 높이 — 2cm 안으로 비기면 작업대가 이긴다.
        if (packD <= best + 0.02f && packD <= gazeReach) return packSpot;
        // 사장(사람)은 talkReach 까지, 나머지(문·카트)는 예전처럼 gazeReach 까지.
        if (first != null && boss != null && first.transform.IsChildOf(boss) && best <= talkReach) return boss;
        // 칠판은 벽에 붙어 있어 한 발 물러서서 본다 — 사장처럼 talkReach 까지.
        if (first != null && orderBoard != null && first.transform.IsChildOf(orderBoard.transform) && best <= talkReach) return orderBoard;
        if (first != null && packSpot != null && first.transform.IsChildOf(packSpot.transform) && best <= gazeReach) return packSpot;
        // 문 앞 건네기: 문은 사람처럼 한 발 떨어져서 두드린다(talkReach).
        if (first != null && handover != null && first.transform.IsChildOf(handover.transform) && best <= talkReach) return handover;
        if (first != null && best > gazeReach) first = null;
        // 카트는 낮아서(충돌 0.44m) 서서 내려다보면 레이가 빗나간다 — 카트 중심이 3m 안(가장자리 약 2m)이고 대략 앞쪽이면 타기 허용(PD 10/31, 기능팀 보고 1c)
        if (car != null)
        {
            Vector3 to = car.transform.position - player.transform.position; to.y = 0f;
            Vector3 fw = eye.forward; fw.y = 0f;
            if (to.magnitude < 3.0f && fw.sqrMagnitude > 1e-4f && Vector3.Dot(fw.normalized, to.normalized) > 0.5f
                && (first == null || first.GetComponentInParent<DoorHinge>() == null)) return car;
        }
        if (first == null) return null;
        var hitCar = first.GetComponentInParent<ArcadeCarController>();
        if (hitCar != null && hitCar == car) return car;
        var dh = first.GetComponentInParent<DoorHinge>();
        if (dh != null && doors.Contains(dh)) { door = dh; return dh; }
        foreach (var d in doors)
            if (d != null) foreach (var r in d.riders) if (r != null && first.transform.IsChildOf(r)) { door = d; return d; }
        return null;
    }
}
