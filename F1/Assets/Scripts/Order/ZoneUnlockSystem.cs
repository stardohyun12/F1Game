using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 채널 3 — zone 해금 상태 + 자금 차감 흐름. UpgradeSystem.Spend로 자금 차감.
///
/// 사용:
/// - OrderBoard: GetAvailableJobs()로 unlocked zone 잡 풀 결합.
/// - ZoneUnlockPanel: locked zone 카드 + TryUnlock(zone).
/// </summary>
[DisallowMultipleComponent]
public class ZoneUnlockSystem : MonoBehaviour
{
    public static ZoneUnlockSystem Instance { get; private set; }

    [SerializeField] private ZoneRegistry registry;
    [SerializeField] private UpgradeSystem wallet;

    [Tooltip("true면 wallet이 unlockCost 도달 시 자동 해금 (자금 차감 X). UI 없이 진척감 유지용.")]
    [SerializeField] private bool autoUnlockOnAffordable = true;

    readonly HashSet<string> unlockedIds = new HashSet<string>();

    public event Action<Zone> OnZoneUnlocked;

    public ZoneRegistry Registry => registry;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (wallet == null) wallet = UpgradeSystem.Instance;

        // 시작부터 해금된 zone 등록 (unlockCost==0).
        if (registry != null)
        {
            for (int i = 0; i < registry.allZones.Count; i++)
            {
                var z = registry.allZones[i];
                if (z == null) continue;
                if (z.unlockCost <= 0) unlockedIds.Add(z.id);
            }
        }

        if (autoUnlockOnAffordable && wallet != null)
        {
            wallet.OnWalletChanged += OnWalletChanged_AutoUnlock;
            // 시작 시 이미 충분할 수도.
            CheckAutoUnlock(wallet.Wallet);
        }
    }

    void OnDestroy()
    {
        if (wallet != null) wallet.OnWalletChanged -= OnWalletChanged_AutoUnlock;
        if (Instance == this) Instance = null;
    }

    void OnWalletChanged_AutoUnlock(int newWallet) => CheckAutoUnlock(newWallet);

    void CheckAutoUnlock(int currentWallet)
    {
        if (registry == null) return;
        for (int i = 0; i < registry.allZones.Count; i++)
        {
            var z = registry.allZones[i];
            if (z == null || unlockedIds.Contains(z.id)) continue;
            if (currentWallet >= z.unlockCost)
            {
                unlockedIds.Add(z.id);
                OnZoneUnlocked?.Invoke(z);
                AudioCueBus.Publish(AudioCueId.UpgradePurchased);
                Debug.Log($"[ZoneUnlock] 자동 해금: {z.displayName} (자금 ₩{currentWallet:N0} ≥ ₩{z.unlockCost:N0})");
            }
        }
    }

    public bool IsUnlocked(Zone z) => z != null && unlockedIds.Contains(z.id);
    public bool IsUnlocked(string id) => !string.IsNullOrEmpty(id) && unlockedIds.Contains(id);

    /// <summary>자금 충분하면 차감 후 unlocked 집합에 추가. 이미 해금이면 false.</summary>
    public bool TryUnlock(Zone z)
    {
        if (z == null) return false;
        if (unlockedIds.Contains(z.id)) return false;
        if (wallet == null) wallet = UpgradeSystem.Instance;
        if (wallet == null) { Debug.LogWarning("[ZoneUnlock] UpgradeSystem 없음 — 무료 해금 처리."); }
        else if (wallet.Wallet < z.unlockCost) return false;
        else wallet.Spend(z.unlockCost);

        unlockedIds.Add(z.id);
        OnZoneUnlocked?.Invoke(z);
        AudioCueBus.Publish(AudioCueId.UpgradePurchased);
        return true;
    }

    /// <summary>잡이 속한 zone 찾기 (배지 표시용). 없으면 null.</summary>
    public Zone GetZoneOf(OrderJob job)
    {
        if (job == null || registry == null) return null;
        for (int i = 0; i < registry.allZones.Count; i++)
        {
            var z = registry.allZones[i];
            if (z == null) continue;
            for (int j = 0; j < z.jobs.Count; j++)
                if (z.jobs[j] == job) return z;
        }
        return null;
    }

    /// <summary>현재 unlocked zone들의 잡을 하나로 합쳐 반환. 같은 잡이 여러 zone에 있으면 한 번만 (BUG-002 후속:
    /// Harbor·Industrial 잡이 Downtown 과 겹쳐 해금 뒤 보드에 같은 카드 2장이 떴음). 순서는 registry·zone 순, 처음 나온 자리 유지.</summary>
    public List<OrderJob> GetAvailableJobs()
    {
        var result = new List<OrderJob>();
        if (registry == null) return result;
        var seen = new HashSet<OrderJob>();
        for (int i = 0; i < registry.allZones.Count; i++)
        {
            var z = registry.allZones[i];
            if (z == null || !unlockedIds.Contains(z.id)) continue;
            for (int j = 0; j < z.jobs.Count; j++)
                if (z.jobs[j] != null && seen.Add(z.jobs[j])) result.Add(z.jobs[j]);
        }
        return result;
    }

    /// <summary>아직 해금되지 않은 zone들. ZoneUnlockPanel용.</summary>
    public List<Zone> GetLockedZones()
    {
        var result = new List<Zone>();
        if (registry == null) return result;
        for (int i = 0; i < registry.allZones.Count; i++)
        {
            var z = registry.allZones[i];
            if (z == null) continue;
            if (!unlockedIds.Contains(z.id)) result.Add(z);
        }
        return result;
    }
}
