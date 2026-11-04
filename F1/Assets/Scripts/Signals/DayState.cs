/// <summary>
/// D-287 2단계: 하루 상태 칠판. 쓰는 쪽은 단계(Stages — 지금은 OnFootDirector)뿐, 물건(Interactables)·화면은 읽기만 한다.
/// 물건이 「지금 E 가 먹나」를 정할 때 감독을 뒤지지 않고 여기를 본다. 순수 C#(UnityEngine 없음).
/// </summary>
public static class DayState
{
    public enum Mode { Starting, OnFoot, InCart, Seated, Board, Pack, Calendar, Diary }

    public static Mode Now = Mode.Starting;
    /// <summary>보관소 흐름(D-250) — 칠판이 있는 씬.</summary>
    public static bool DepotFlow;
    /// <summary>카트를 타려면 사장 대화가 먼저(예전 흐름).</summary>
    public static bool CartNeedsBossTalk;
    public static bool Talked, OrderPicked, OrderStarted, Delivered, CarryingBox, Knocking, HandedOver, InTalk;
    /// <summary>일기를 펼친 때(unscaled 초) · 덮을 수 있기까지 최소 초.</summary>
    public static float DiaryAt = -99f, DiaryMinSec = 0.8f;
    /// <summary>D-301 첫 포장 튜토리얼을 끝까지 봤다(상자를 들고 나갔다). 하루가 바뀌어도 안 지운다 — Reset 도 건드리지 않는다.
    /// 저장 파일이 아직 없어 프로세스 동안만 산다(하루 단위 저장이 생기면 이 한 값을 거기에 얹는다).</summary>
    public static bool PackTutorialSeen;

    public static void Reset()
    {
        Now = Mode.Starting;
        DepotFlow = CartNeedsBossTalk = false;
        Talked = OrderPicked = OrderStarted = Delivered = CarryingBox = Knocking = HandedOver = InTalk = false;
        DiaryAt = -99f; DiaryMinSec = 0.8f;
    }
}
