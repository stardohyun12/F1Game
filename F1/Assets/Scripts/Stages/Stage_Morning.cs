using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-287 4단계: 아침 방 단계 — 방 달력 보기 · 옛 배달 지점 끄기. 감독이 만들고 부른다(MonoBehaviour 아님).
/// 듣는 신호: CalendarOpened(물건 RoomCalendar → 달력 보기 = Calendar).
/// 지도: docs/world/CODEMAP.md
/// </summary>
public class Stage_Morning : DayStage
{
    public Stage_Morning(OnFootDirector d) : base(d) { }
    public override OnFootDirector.Mode Mode => OnFootDirector.Mode.Calendar;

    /// <summary>달력 보기 시점으로 옮겨 가는 중/돌아오는 중(QA).</summary>
    public bool Moving => d.State == OnFootDirector.Mode.Calendar && d.boardMove != null;
    bool simClose;

    public override void Plug()
    {
        base.Plug();
        Listen(Signal.CalendarOpened, _ => Enter());
    }

    /// <summary>SPEC §11-4: 옛 배달 지점(AlleyEnd·CoastCafe·HairpinOrchard·LoneHouse — DP_ 접두가 아닌 DeliveryPoint)은 Play 때 끈다.</summary>
    public static bool IsLegacyPoint(DeliveryPoint dp) => dp != null && !dp.name.StartsWith("DP_") && dp.transform.parent != null && dp.transform.parent.name == "DeliverySystem";
    /// <summary>SPEC §11-4: 옛 배달 지점 4곳을 끈다(DeliveryManager.Start 가 켠 뒤 — Start 는 한 프레임 늦게 돈다).</summary>
    internal void HideLegacyPoints()
    {
        int n = 0;
        foreach (var dp in Object.FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None))
            if (IsLegacyPoint(dp)) { dp.gameObject.SetActive(false); n++; }
        if (n > 0) d.Mark($"옛 배달 지점 {n}곳 끔(SPEC §11-4)");
    }

    // ── 과제 20261102-1951: 방 달력 보기 ──
    /// <summary>달력 보기 시점: 칠판 보기와 같은 방식(카메라가 달력 앞 ViewPose 로). 몸은 그 자리.</summary>
    public override void Enter()
    {
        d.StopDestFocus();
        d.State = OnFootDirector.Mode.Calendar;
        d.player.enabled = false;
        d.footCam.enabled = false;
        d.boardEyePos = d.cam.transform.position; d.boardEyeRot = d.cam.transform.rotation;
        d.calendar.SetHint(true);
        d.SetPrompt(""); d.PromptAnchor = null; d.GazeTarget = null;
        if (d.dotRoot != null) d.dotRoot.SetActive(false);
        d.calendar.ViewPose(d.cam.fieldOfView, d.cam.aspect, out var pos, out var rot);
        if (d.boardMove != null) d.StopCoroutine(d.boardMove);
        d.boardMove = d.StartCoroutine(d.MoveCam(pos, rot, d.boardFocusSec, null));
        d.Mark($"달력 보기 시점 {pos.ToString("F2")} 달력까지 {Vector3.Distance(pos, d.calendar.Center):F2}m — {d.calendar.Written}");
    }

    public override void Exit()
    {
        d.calendar.SetHint(false);
        if (d.boardMove != null) d.StopCoroutine(d.boardMove);
        d.boardMove = d.StartCoroutine(d.MoveCam(d.boardEyePos, d.boardEyeRot, d.boardFocusSec * 0.6f, () =>
        {
            d.player.enabled = true;
            d.EnterFoot();
            d.footCam.HoldLook = false;
            d.Mark("달력 닫기");
        }));
    }

    /// <summary>봇: 달력 닫기.</summary>
    internal void SimClose() { simClose = true; }

    public override void Tick(bool press)
    {
        d.PromptAnchor = null;
        var kb = Keyboard.current;
        bool close = press || simClose || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame));
        simClose = false;
        if (d.boardMove != null) return;                                    // 옮겨 가는 동안은 입력 안 받음
        if (close) Exit();
    }
}
