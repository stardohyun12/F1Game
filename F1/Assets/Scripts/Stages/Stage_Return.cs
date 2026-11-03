using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    [SerializeField] OnFootPlayer player;
    [SerializeField] Transform spawn;
    [SerializeField] Transform cartSpot;
    [Tooltip("1인칭 시선 레이 길이(m). 눈에서 이 거리 안에 문·카트가 맞으면 E.")]
    [SerializeField] float gazeReach = 2.0f;
    [Tooltip("이 속도(m/s) 밑이면 내릴 수 있다.")]
    [SerializeField] float exitMaxSpeed = 1.0f;
    [Tooltip("카트에서 내릴 때 차 옆 거리(m).")]
    [SerializeField] float exitSide = 1.7f;

    [Header("기능 2 — 정비소 사장 → 첫 주문(과제 20261031-0209)")]
    [Tooltip("정비소 사장 자리(1.7m 캡슐 대역). 비면 기능 1 처럼 바로 카트를 탈 수 있다.")]
    [SerializeField] Transform boss;
    [Tooltip("첫 주문 배달지(찻집 앞 길). 비면 탑승 때 주문 없이 자유 주행.")]
    [SerializeField] DeliveryPoint firstDrop;
    [Tooltip("사장에게 말 거는 시선 레이 길이(m). 사람은 문보다 멀리서 말을 건다.")]
    [SerializeField] float talkReach = 3.0f;

    [Header("D-250·251 — 택배 보관소에서 주문 받기(과제 20261102-1515)")]
    [Tooltip("보관소 안 주문 칠판. 비면 예전 기능 2(사장이 말로 찻집 주문, 탑승 = 출발).")]
    [SerializeField] DepotOrderBoard orderBoard;
    [Tooltip("포장 자리 대역(작업대 위 상자). 비면 칠판에서 고르는 것만으로 출발할 수 있다.")]
    [SerializeField] DepotPackSpot packSpot;
    [Tooltip("사장 대화 뒤 목적지 표식이 가리킬 곳(보관소 앞). 꺼진 DeliveryPoint — 표식 자리로만 쓴다.")]
    [SerializeField] DeliveryPoint depotDest;
    [Tooltip("칠판 보기 시점으로 옮겨 가는 시간(초).")]
    [SerializeField] float boardFocusSec = 0.5f;

    [Header("D-250 ③④ — 문 앞 건네기(과제 20261102-1543)")]
    [Tooltip("배달지 문 앞 대역(문짝·받는 사람). 비면 예전처럼 도착 = 끝(정산창).")]
    [SerializeField] DoorHandover handover;
    [Tooltip("배달지 표식에서 이 거리(m, 수평) 안에서 세우고 내리면 도착으로 친다(트리거를 못 밟았어도).")]
    [SerializeField] float arriveM = 15f;
    [Tooltip("돈 글자가 떠 있는 시간(초).")]
    [SerializeField] float moneySec = 2.6f;
    [Tooltip("두드린 뒤 문이 열리기까지(초).")]
    [SerializeField] float knockWaitSec = 0.9f;

    [Header("하루 끝 일기장·방 달력(과제 20261102-1951, DELIVERY-LOOP 5-5)")]
    [Tooltip("방 벽 달력 실물. 비면 달력 [E] 없음.")]
    [SerializeField] RoomCalendar calendar;
    [Tooltip("방 침대 [E] 자리. 비면 하루 끝 일기장 없음.")]
    [SerializeField] RoomBed bed;
    [Tooltip("일기를 펼친 뒤 이 초가 지나야 덮인다(실수로 바로 넘기지 않게).")]
    [SerializeField] float diaryMinSec = 0.8f;

    [Header("첫 과제 01-first-loop — 건넨 뒤 노을(SPEC §11-3 가설)")]
    [Tooltip("문 앞 건네기가 끝나면 SunsetLook.dusk 를 0 → 1 로 이 시간(초, 실시간)에 걸쳐 올린다. 0 = 안 바꿈.")]
    [SerializeField] float duskRampSec = 60f;
    [Tooltip("건넨 뒤 '첫 배달 끝' 글이 떠 있는 시간(초). 그 뒤 돌아오는 길은 화면 0(SPEC §6-9).")]
    [SerializeField] float handedCaptionSec = 6f;
    /// <summary>건네기가 끝난 때(unscaled) — 노을 시작.</summary>
    public float HandedAt { get; private set; } = -99f;
    Coroutine duskRamp;
    bool lookWasOn;

    /// <summary>SPEC §11-3 가설: 건넨 뒤 duskRampSec 동안 오후 → 노을. SunsetLook 이 꺼져 있었으면 dusk 0 으로 켠 뒤 올린다(켜는 순간 튀지 않게).</summary>
    IEnumerator DuskRamp()
    {
        var look = SunsetLook.Find();
        if (look == null) { Mark("노을 실패: SunsetLook 없음"); yield break; }
        float d0 = look.IsOn ? look.dusk : 0f;
        if (!look.IsOn) { look.dusk = 0f; look.SetOn(true); }
        Mark($"노을 시작 dusk {d0:F2} → 1 ({duskRampSec:F0}s)"); SignalBus.Raise(Signal.DuskStarted);
        for (float t = 0f; t < duskRampSec; t += Time.unscaledDeltaTime)
        {
            look.dusk = Mathf.Lerp(d0, 1f, t / duskRampSec);
            look.Apply();
            yield return null;
        }
        look.dusk = 1f; look.Apply();
        duskRamp = null;
        Mark("노을 1.0");
    }

    FishingSpot NearSpot()
    {
        foreach (var s in spots) if (s != null && s.CanSit(player.transform.position)) return s;
        return null;
    }

    /// <summary>D-227 낚시 자리에 앉기 — 몸·걷기 카메라·조준점·안내를 끄고 자리(FishingSpot)가 카메라를 잡는다.</summary>
    void Sit(FishingSpot s)
    {
        Mark("앉기 " + s.name);
        State = Mode.Seated; seatedAt = s;
        EngineAudio.OnFootMute = true;
        footCam.enabled = false;
        player.gameObject.SetActive(false);
        if (dotRoot != null) dotRoot.SetActive(false);
        SetPrompt("");
        GazeTarget = null;
        s.Sit(cam);
    }

    void StandUp()
    {
        var s = seatedAt; seatedAt = null;
        Mark("일어나기 " + (s != null ? s.name : "-"));
        if (s != null) s.Stand();
        player.gameObject.SetActive(true);
        if (s != null) player.Teleport(s.StandPoint(), s.SeatYaw);
        EnterFoot();
    }

    /// <summary>개발용(메뉴 PitCrew/노을/낚시 자리로 가기): 걷는 중이면 자리 바로 뒤로 옮긴다.</summary>
    public bool GoToSpot()
    {
        if (State != Mode.OnFoot || spots.Count == 0) return false;
        var s = spots[0];
        player.Teleport(s.StandPoint(), s.SeatYaw);
        footCam.Yaw = s.SeatYaw; footCam.Pitch = 0f;
        Mark("자리로 이동 " + s.name);
        return true;
    }
}
