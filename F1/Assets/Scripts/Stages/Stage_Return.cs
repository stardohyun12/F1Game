using System.Collections;
using UnityEngine;

/// <summary>
/// D-287 3단계: 돌아오는 길 단계 — 건넨 뒤 노을 올리기 · 낚시 자리 앉기/서기. 감독이 만들고 부른다(MonoBehaviour 아님).
/// 듣는 신호: HandedOver(Stage_Door → 노을 시작) · SeatTaken(물건 Seat → 앉기 = Seated) · DayStarted(아침 = 오후 빛).
/// 쏘는 신호: DuskStarted.
/// 지도: docs/world/CODEMAP.md
/// </summary>
public class Stage_Return : DayStage
{
    public Stage_Return(OnFootDirector d) : base(d) { }
    public override OnFootDirector.Mode Mode => OnFootDirector.Mode.Seated;

    /// <summary>건네기가 끝난 때(unscaled) — 노을 시작.</summary>
    public float HandedAt { get; private set; } = -99f;
    /// <summary>Play 시작 때 SunsetLook 이 켜져 있었나(감독 Start 가 적는다) — 아침에 그 상태로 되돌린다.</summary>
    internal bool LookWasOn;
    /// <summary>지금 앉은 낚시 자리(없으면 null).</summary>
    internal FishingSpot SeatedAt;
    Coroutine duskRamp;

    public override void Plug()
    {
        base.Plug();
        Listen(Signal.SeatTaken, a => { if (a is FishingSpot sp) Sit(sp); });
        Listen(Signal.HandedOver, _ => StartDusk());
    }

    protected override void NewDay()
    {
        // 아침 = 오후 빛으로(노을은 건넨 뒤에만)
        if (duskRamp != null) { d.StopCoroutine(duskRamp); duskRamp = null; }
        HandedAt = -99f;
        var look = SunsetLook.Find();
        if (look != null && look.IsOn) { look.dusk = 0f; if (LookWasOn) look.Apply(); else look.SetOn(false); }
    }

    void StartDusk()
    {
        HandedAt = Time.unscaledTime;
        if (d.duskRampSec > 0f && duskRamp == null) duskRamp = d.StartCoroutine(DuskRamp());
    }

    /// <summary>SPEC §11-3 가설: 건넨 뒤 duskRampSec 동안 오후 → 노을. SunsetLook 이 꺼져 있었으면 dusk 0 으로 켠 뒤 올린다(켜는 순간 튀지 않게).</summary>
    IEnumerator DuskRamp()
    {
        float sec = d.duskRampSec;
        var look = SunsetLook.Find();
        if (look == null) { d.Mark("노을 실패: SunsetLook 없음"); yield break; }
        float d0 = look.IsOn ? look.dusk : 0f;
        if (!look.IsOn) { look.dusk = 0f; look.SetOn(true); }
        d.Mark($"노을 시작 dusk {d0:F2} → 1 ({sec:F0}s)"); SignalBus.Raise(Signal.DuskStarted);
        for (float t = 0f; t < sec; t += Time.unscaledDeltaTime)
        {
            look.dusk = Mathf.Lerp(d0, 1f, t / sec);
            look.Apply();
            yield return null;
        }
        look.dusk = 1f; look.Apply();
        duskRamp = null;
        d.Mark("노을 1.0");
    }

    /// <summary>D-227 낚시 자리에 앉기 — 몸·걷기 카메라·조준점·안내를 끄고 자리(FishingSpot)가 카메라를 잡는다.</summary>
    internal void Sit(FishingSpot s)
    {
        d.Mark("앉기 " + s.name);
        d.State = OnFootDirector.Mode.Seated; SeatedAt = s;
        EngineAudio.OnFootMute = true;
        d.footCam.enabled = false;
        d.player.gameObject.SetActive(false);
        if (d.dotRoot != null) d.dotRoot.SetActive(false);
        d.SetPrompt("");
        d.GazeTarget = null;
        s.Sit(d.cam);
    }

    /// <summary>앉아 있는 동안 화면에 아무것도 없음(D-227). E = 일어나기.</summary>
    public override void Tick(bool press)
    {
        d.PromptAnchor = null;
        if (press) Exit();
    }

    public override void Exit()
    {
        var s = SeatedAt; SeatedAt = null;
        d.Mark("일어나기 " + (s != null ? s.name : "-"));
        if (s != null) s.Stand();
        d.player.gameObject.SetActive(true);
        if (s != null) d.player.Teleport(s.StandPoint(), s.SeatYaw);
        d.EnterFoot();
    }

    /// <summary>개발용(메뉴 PitCrew/노을/낚시 자리로 가기): 걷는 중이면 자리 바로 뒤로 옮긴다.</summary>
    internal bool GoToSpot()
    {
        if (d.State != OnFootDirector.Mode.OnFoot || d.spots.Count == 0) return false;
        var s = d.spots[0];
        d.player.Teleport(s.StandPoint(), s.SeatYaw);
        d.footCam.Yaw = s.SeatYaw; d.footCam.Pitch = 0f;
        d.Mark("자리로 이동 " + s.name);
        return true;
    }
}
