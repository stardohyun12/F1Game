using UnityEngine;

/// <summary>
/// D-287 5단계: 화면 View — DotScreenUI 가 감독 대신 읽는 칠판(DayState 의 화면판). 화면은 여기만 본다(감독 직접 참조 0).
/// 쓰는 쪽 = 감독(Stages)뿐: 매 프레임 LateUpdate 첫 줄에서 적는다 — 감독(실행 순서 0)이 DotScreenUI(200)보다 먼저 돌아서
/// 화면이 읽는 값은 옮기기 전(감독 속성을 그 자리에서 읽던 때)과 같은 프레임 값이다. 이름은 옮기기 전 감독 공개 이름 그대로.
/// </summary>
public static class DayView
{
    /// <summary>감독이 씬에 있다(꺼져 있어도) · 켜져서 매 프레임 적고 있다.</summary>
    public static bool Present, Active;
    public static DayState.Mode State = DayState.Mode.Starting;

    // ── [E] 칩 ──
    public static string Prompt = "";
    public static bool PromptLow;
    public static Vector3? PromptAnchor;

    // ── 말풍선(대화 중일 때만 채운다) ──
    public static bool InTalk;
    public static string TalkWho = "", TalkText = "";
    public static float TalkLineAt;
    public static Vector3 SpeakerTop;

    // ── 전표·목적지 ──
    public static HeldPaper Held;
    public static OrderJob PickedJob;
    public static bool OrderStarted;
    public static DeliveryPoint FirstDrop, WalkDest;
    public static Transform Player, Car;

    // ── 카트 위 「!」 ──
    public static bool CartMark;
    public static Vector3 CartMarkAnchor;

    // ── 떠오르는 돈 글자 ──
    public static string MoneyText = "";
    public static float MoneyAt = -99f, MoneyEnd = -99f, MoneySec;
    public static Vector3 MoneyAnchor;

    // ── 일기·아침 ──
    public static bool DiaryCanClose;
    public static float MorningAt = -99f;

    /// <summary>감독이 없을 때 모습(감독 Awake·OnDestroy). 도메인 리로드가 꺼져 있어도 지난 Play 값이 안 남게.</summary>
    public static void Reset()
    {
        Present = Active = false; State = DayState.Mode.Starting;
        Prompt = ""; PromptLow = false; PromptAnchor = null;
        InTalk = false; TalkWho = TalkText = ""; TalkLineAt = 0f; SpeakerTop = Vector3.zero;
        Held = null; PickedJob = null; OrderStarted = false; FirstDrop = WalkDest = null; Player = Car = null;
        CartMark = false; CartMarkAnchor = Vector3.zero;
        MoneyText = ""; MoneyAt = MoneyEnd = -99f; MoneySec = 0f; MoneyAnchor = Vector3.zero;
        DiaryCanClose = false; MorningAt = -99f;
    }
}
