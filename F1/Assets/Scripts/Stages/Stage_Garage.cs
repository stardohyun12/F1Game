using UnityEngine;

/// <summary>
/// D-287 4단계: 정비소 단계 — 사장 대화(카트 열쇠) · 정비(수리) · 아침 카트 자리. 감독이 만들고 부른다(MonoBehaviour 아님).
/// 듣는 신호: TalkAsked(물건 Boss → 사장 대화) · RepairAsked(물건 RepairSpot → 수리) · DayStarted(아침 카트 자리로).
/// 쏘는 신호: KeyReceived.
/// 지도: docs/world/CODEMAP.md
/// </summary>
public class Stage_Garage : DayStage
{
    public Stage_Garage(OnFootDirector d) : base(d) { }
    public override OnFootDirector.Mode Mode => OnFootDirector.Mode.OnFoot;

    /// <summary>사장 대화를 끝냈는지 — 이 뒤에만 카트를 탄다. 다음 날에도 안 되돌린다.</summary>
    public bool Talked { get; internal set; }
    /// <summary>카트를 타려면 사장 대화가 먼저인가. 보관소 흐름(SPEC §11-7: 사장은 정비만)이면 아니다 — 불변 조건 「첫 배달 한 바퀴 배선」이 본다.</summary>
    public bool CartNeedsBossTalk => d.boss != null && !d.DepotFlow;
    /// <summary>아침 카트 자리 = 정비소 자리(cartSpot, SPEC §3-2 카트 자리). 불변 조건 「아침 카트 정비소」가 본다.</summary>
    public Vector3? MorningCartAt => d.cartSpot != null ? d.cartSpot.position : (Vector3?)null;

    public override void Plug()
    {
        base.Plug();
        Listen(Signal.TalkAsked, _ => BeginBossTalk());
        Listen(Signal.RepairAsked, _ => RepairCart());
    }

    /// <summary>카트는 아침 자리(정비소)로.</summary>
    protected override void NewDay() => ParkMorningCart();

    internal void ParkMorningCart()
    {
        if (d.cartSpot != null) d.ParkCar(d.cartSpot.position, d.cartSpot.eulerAngles.y);
    }

    // 대사: docs/design/tutorial/first-delivery.yaml 의 garage 장면 1·2줄 그대로. 3줄(첫 주문 안내)은 설계에 사장 대사가 없어
    // 보관소 직원 대사("첫 주문. 달걀 한 판, 언덕 위 찻집.")를 사장 입으로 옮기고 줄리아 이름을 붙였다 — 도현 확인 필요.
    static readonly (string who, string text)[] BossLines =
    {
        ("사장", "왔냐. 오늘부터 배달이다. 대회는 그 다음."),
        ("사장", "뒤에 짐상자 달아 놨어. 깨지는 건 네 책임."),
        ("사장", "첫 주문. 달걀 한 판, 언덕 위 찻집 줄리아네."),
    };
    // D-250 보관소 흐름의 사장 대사 — 대역(도현이 정함). 정비소 = 카트 점검, 주문은 택배 보관소 칠판에서.
    // C-002 오프닝: 2줄에서 카트 열쇠를 건넨다(대역 문구 — 도현 확인 필요).
    // D-284(SPEC §11-7): 사장은 정비·카트 얘기만 — 주문 얘기는 보관소 칠판이 한다.
    static readonly (string who, string text)[] DepotBossLines =
    {
        ("사장", "왔냐. 방학 첫날이지? 카트는 내가 봐 놨다."),
        ("사장", "자, 열쇠. 뒤 짐상자 깨지는 건 네 책임이다."),
        ("사장", "상하면 가져와. 고쳐 줄 테니."),
    };

    /// <summary>사장 대화 시작(말풍선·시선 이동은 감독 대화 서비스가).</summary>
    internal void BeginBossTalk() => d.BeginTalk(d.DepotFlow ? DepotBossLines : BossLines, null, null, EndBossTalk);

    /// <summary>사장 대화 끝: 열쇠를 받고 → 목적지(보관소 앞 또는 찻집) 쪽으로 시선 한 번.</summary>
    void EndBossTalk()
    {
        Talked = true;
        d.Mark("사장 대화 끝" + (d.DepotFlow ? " → 보관소로" : ""));
        if (d.opening != null) { d.opening.OnTalkEnd(); d.Mark("카트 열쇠 받음"); SignalBus.Raise(Signal.KeyReceived); }
        d.FocusWalkDest();
    }

    /// <summary>C-001: 정비 자리에서 E — 상처를 지우고 수리비 글자(−숫자)를 카트 위에 띄운다(건네기 돈 글자와 같은 자리 쓰기).</summary>
    void RepairCart()
    {
        int cost = RepairSpot.Repair(d.car);
        string text = $"−{cost:N0}원";
        d.ShowMoney(text, d.car.transform.position + Vector3.up * 1.5f);   // 칩(0.8 m)과 안 겹치게 더 위
        d.SetPrompt(""); d.PromptAnchor = null;
        d.Mark($"수리 — 수리비 {text} · 오늘 수리 합 {DayLog.TodayRepair}");
    }
}
