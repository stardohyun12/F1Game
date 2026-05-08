using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보유 업그레이드 트래킹 + 효과 조회 (Day 21).
/// 정산 화면(ResultPanel)에서 구매 → IsOwned 등록 → 다음 런부터 적용.
///
/// 효과 적용 패턴 (각 시스템에서 체크):
/// - CargoBox.Awake: SuspensionUpgrade → longWarnG/latWarnG +0.1
/// - CargoBox.Awake: ReinforcedTrunk → maxHP +20
/// - CargoBox.Awake: SmoothSteering → spinWarnDegPerSec +20
/// - OrderCatalog: ChilledHold → 냉동 주문 추가 (Phase 2)
/// - RoutePlannerPanel: ShortcutMap → isShortcut 엣지 visual
///
/// MVP는 owned set만 관리. 자금 시스템(Wallet)은 RunController.LastReward 누적으로 별도 추가.
/// </summary>
[DisallowMultipleComponent]
public class UpgradeSystem : MonoBehaviour, IUpgradeSystem
{
    /// <summary>씬 어디서나 접근 가능 (싱글톤). null 가능.</summary>
    public static UpgradeSystem Instance { get; private set; }

    [Tooltip("이번 세션 영속화 — 정산에서 누적. Phase 2에 PlayerPrefs 저장.")]
    [SerializeField] private List<UpgradeEffect> ownedList = new List<UpgradeEffect>();

    [Tooltip("현재 보유 자금. 정산 보상 누적, 업그레이드 구매로 차감.")]
    [SerializeField] private int wallet = 0;

    public IReadOnlyList<UpgradeEffect> Owned => ownedList;
    public int Wallet => wallet;

    public event Action<UpgradeEffect> OnUpgradePurchased;
    public event Action<int> OnWalletChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsOwned(UpgradeEffect effect)
    {
        for (int i = 0; i < ownedList.Count; i++)
            if (ownedList[i] == effect) return true;
        return false;
    }

    /// <summary>정산 후 보상 추가.</summary>
    public void AddReward(int amount)
    {
        if (amount <= 0) return;
        wallet += amount;
        OnWalletChanged?.Invoke(wallet);
    }

    /// <summary>업그레이드 구매. 자금 부족/이미 보유면 false.</summary>
    public bool TryPurchase(UpgradeDef def)
    {
        if (def == null) return false;
        if (IsOwned(def.effect)) return false;
        if (wallet < def.cost) return false;
        wallet -= def.cost;
        ownedList.Add(def.effect);
        OnWalletChanged?.Invoke(wallet);
        OnUpgradePurchased?.Invoke(def.effect);
        return true;
    }

    /// <summary>편의: 효과 보유 시 modifier 값 반환 (없으면 0).</summary>
    public float GetModifier(UpgradeEffect effect, float ownedAmount)
    {
        return IsOwned(effect) ? ownedAmount : 0f;
    }
}
