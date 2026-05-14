using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 채널 3 — ZoneUnlockPanel의 카드 prefab root. prefab 없을 땐 패널이 폴백 카드를 만든다.
/// </summary>
public class ZoneUnlockCard : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private Button button;

    [Header("Affordable Colors")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f);

    Zone zone;
    Action<Zone> onClick;
    UpgradeSystem walletRef;

    public void Bind(Zone z, Action<Zone> onClick)
    {
        zone = z;
        this.onClick = onClick;
        if (titleText != null) titleText.text = z.displayName;
        if (costText != null) costText.text = $"₩{z.unlockCost:N0}";
        if (subText != null) subText.text = $"잡 {z.jobs.Count}개 추가";
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(zone));
        }
        RefreshAffordable();
    }

    void OnEnable()
    {
        walletRef = UpgradeSystem.Instance;
        if (walletRef != null) walletRef.OnWalletChanged += OnWalletChanged;
        RefreshAffordable();
    }

    void OnDisable()
    {
        if (walletRef != null) walletRef.OnWalletChanged -= OnWalletChanged;
    }

    void OnWalletChanged(int _) => RefreshAffordable();

    void RefreshAffordable()
    {
        if (zone == null) return;
        var w = walletRef != null ? walletRef : UpgradeSystem.Instance;
        bool ok = w != null && w.Wallet >= zone.unlockCost;
        if (button != null) button.interactable = ok;
        if (titleText != null) titleText.color = ok ? affordableColor : lockedColor;
        if (costText != null) costText.color = ok ? affordableColor : lockedColor;
    }
}
