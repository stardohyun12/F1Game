using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 채널 3 — 잠금 zone 카드 리스트. OrderBoard와 같은 시점(런 사이 메뉴)에 보임.
/// 자금 충분한 카드만 클릭 가능. 클릭 시 ZoneUnlockSystem.TryUnlock → 카드 사라짐, OrderBoard 풀 확장.
///
/// 와이어링:
///   panelRoot       — 패널 root.
///   cardsContainer  — 카드들이 들어갈 Vertical Layout Group RectTransform.
///   cardPrefab      — ZoneUnlockCard 컴포넌트를 가진 prefab. 없으면 런타임에 간단 카드 자동 생성.
/// </summary>
public class ZoneUnlockPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform cardsContainer;
    [SerializeField] private GameObject cardPrefab;

    [Header("Polling")]
    [Tooltip("Wallet 변경 시 자동 갱신. false면 OrderBoard.Open과 같이 RefreshExternal 호출 필요.")]
    [SerializeField] private bool autoRefreshOnWalletChange = true;

    readonly List<GameObject> spawned = new List<GameObject>();
    UpgradeSystem walletRef;
    ZoneUnlockSystem unlockRef;

    void OnEnable()
    {
        TryHookEvents();
        Refresh();
    }

    void Start()
    {
        // OnEnable이 System.Awake 보다 먼저 도는 경우(같은 프레임 인스턴스화 등) 대비.
        TryHookEvents();
        Refresh();
    }

    void TryHookEvents()
    {
        if (unlockRef == null) unlockRef = ZoneUnlockSystem.Instance;
        if (walletRef == null) walletRef = UpgradeSystem.Instance;
        if (autoRefreshOnWalletChange && walletRef != null)
        {
            walletRef.OnWalletChanged -= OnWalletChanged;
            walletRef.OnWalletChanged += OnWalletChanged;
        }
        if (unlockRef != null)
        {
            unlockRef.OnZoneUnlocked -= OnZoneUnlocked;
            unlockRef.OnZoneUnlocked += OnZoneUnlocked;
        }
    }

    void OnDisable()
    {
        if (walletRef != null) walletRef.OnWalletChanged -= OnWalletChanged;
        if (unlockRef != null) unlockRef.OnZoneUnlocked -= OnZoneUnlocked;
    }

    void OnWalletChanged(int _) => Refresh();
    void OnZoneUnlocked(Zone _) => Refresh();

    public void RefreshExternal() => Refresh();

    void Refresh()
    {
        foreach (var g in spawned) if (g != null) Destroy(g);
        spawned.Clear();

        if (cardsContainer == null) return;
        if (unlockRef == null) unlockRef = ZoneUnlockSystem.Instance;
        if (unlockRef == null) return;

        var locked = unlockRef.GetLockedZones();
        if (panelRoot != null) panelRoot.SetActive(locked.Count > 0);

        foreach (var z in locked) SpawnCard(z);
    }

    void SpawnCard(Zone z)
    {
        GameObject go;
        if (cardPrefab != null)
        {
            go = Instantiate(cardPrefab, cardsContainer);
            var card = go.GetComponent<ZoneUnlockCard>();
            if (card != null) card.Bind(z, OnClickUnlock);
        }
        else
        {
            // 런타임 폴백 — prefab 없으면 간단 카드 생성.
            go = BuildFallbackCard(z);
        }
        spawned.Add(go);
    }

    GameObject BuildFallbackCard(Zone z)
    {
        var go = new GameObject($"ZoneCard_{z.id}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(cardsContainer, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(220, 60);
        var img = go.GetComponent<Image>();
        UIPalette.PaperPanel(img, slot: UISlot.Card);

        var label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(10, 4); lrt.offsetMax = new Vector2(-10, -4);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Left;
        UIPalette.Body(tmp, UIPalette.Ink);

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => OnClickUnlock(z));

        UpdateFallbackCardLabel(tmp, btn, z);
        return go;
    }

    static void UpdateFallbackCardLabel(TMP_Text tmp, Button btn, Zone z)
    {
        var wallet = UpgradeSystem.Instance;
        bool canAfford = wallet != null && wallet.Wallet >= z.unlockCost;
        tmp.text = $"<b>{z.displayName}</b>  잠금 해제 ₩{z.unlockCost:N0}\n<size=12><color={UIPalette.Hex(UIPalette.InkMuted)}>잡 {z.jobs.Count}개 추가</color></size>";
        if (btn != null) btn.interactable = canAfford;
        tmp.color = canAfford ? UIPalette.Ink : UIPalette.Stone;
    }

    void OnClickUnlock(Zone z)
    {
        if (unlockRef == null) unlockRef = ZoneUnlockSystem.Instance;
        if (unlockRef == null) return;
        if (!unlockRef.TryUnlock(z))
        {
            Debug.Log($"[ZoneUnlockPanel] {z.displayName} 해금 실패 (자금 부족 또는 이미 해금).");
        }
    }
}
