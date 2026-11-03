using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    /// <summary>사장 대화를 끝냈는지 — 이 뒤에만 카트를 탄다.</summary>
    public bool Talked { get; private set; }
    /// <summary>카트를 타려면 사장 대화가 먼저인가. 보관소 흐름(SPEC §11-7: 사장은 정비만)이면 아니다 — 불변 조건 「첫 배달 한 바퀴 배선」이 본다.</summary>
    public bool CartNeedsBossTalk => boss != null && !DepotFlow;
    /// <summary>아침 카트 자리 = 정비소 자리(cartSpot, SPEC §3-2 카트 자리). 불변 조건 「아침 카트 정비소」가 본다.</summary>
    public Vector3? MorningCartAt => cartSpot != null ? cartSpot.position : (Vector3?)null;

    void ParkMorningCart()
    {
        if (cartSpot != null) ParkCar(cartSpot.position, cartSpot.eulerAngles.y);
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
    (string who, string text)[] Lines => curLines ?? (DepotFlow ? DepotBossLines : BossLines);
    /// <summary>C-001: 정비 자리에서 E — 상처를 지우고 수리비 글자(−숫자)를 카트 위에 띄운다(건네기 돈 글자와 같은 자리 쓰기).</summary>
    void RepairCart()
    {
        int cost = RepairSpot.Repair(car);
        moneyText = $"−{cost:N0}원";
        MoneyAt = Time.unscaledTime;
        MoneyEnd = MoneyAt + moneySec;
        MoneyAnchor = car.transform.position + Vector3.up * 1.5f;   // 칩(0.8 m)과 안 겹치게 더 위
        SetPrompt(""); PromptAnchor = null;
        Mark($"수리 — 수리비 {moneyText} · 오늘 수리 합 {DayLog.TodayRepair}");
    }
}
