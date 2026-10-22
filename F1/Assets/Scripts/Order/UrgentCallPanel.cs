using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-118 — 긴급 전화. 런 시작 때 "어디서 무엇이 왜 급한지"를 보여 주고, 수락하는 순간부터 시계가 돈다
/// (포장 단계 포함). 씬 배선 없이 런타임에 UIRoot 아래 스스로 만든다. timeScale 0 에서도 동작(UGUI).
/// </summary>
public class UrgentCallPanel : MonoBehaviour
{
    static UrgentCallPanel _inst;

    GameObject root;
    TMP_Text callerText, situationText, detailText, timeText;
    Button acceptBtn, declineBtn;
    Action onAccept, onDecline;

    public static UrgentCallPanel Ensure()
    {
        if (_inst != null) return _inst;
        var go = new GameObject("UrgentCallPanel", typeof(RectTransform));
        go.transform.SetParent(UIRoot.Ensure(), false);
        _inst = go.AddComponent<UrgentCallPanel>();
        _inst.Build();
        return _inst;
    }

    public bool IsVisible => root != null && root.activeSelf;

    /// <summary>전화 표시. remainSec = 지금 남은 제한 시간(음수면 숨김).</summary>
    public void Show(OrderJob job, float remainSec, Action accept, Action decline)
    {
        onAccept = accept;
        onDecline = decline;
        string dest = job.destinationLabels != null && job.destinationLabels.Count > 0
            ? string.Join(" → ", job.destinationLabels) : "-";
        string cargo = CargoTypePresets.DisplayName(job.cargoType);
        callerText.text = string.IsNullOrEmpty(job.callerName) ? $"{dest}에서 전화" : job.callerName;
        situationText.text = string.IsNullOrEmpty(job.urgentSituation) ? DefaultSituation(job.cargoType, dest) : job.urgentSituation;
        float bonusMul = (Services.Run as RunController)?.UrgentRewardBonus ?? 0.5f;
        int urgentExtra = Mathf.RoundToInt(job.baseReward * bonusMul);
        detailText.text = $"배달지  {dest}\n화물  {cargo}\n보상  ₩{job.baseReward:N0}  <color=#FFD040>+ 긴급 ₩{urgentExtra:N0}</color>";
        if (remainSec >= 0f)
        {
            int s = Mathf.CeilToInt(remainSec);
            timeText.text = $"남은 시간 {s / 60}:{s % 60:D2}";
        }
        else timeText.text = "";
        transform.SetAsLastSibling();
        root.SetActive(true);
        AudioCueBus.Publish(AudioCueId.PackingConfirm);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    // 전화가 떠 있는 동안에도 제한 시간은 흐른다 — 남은 시간을 실시간으로.
    void Update()
    {
        if (!IsVisible || timeText == null) return;
        var rc = Services.Run as RunController;
        if (rc == null || !rc.ClockRunning) return;
        int s = Mathf.CeilToInt(Mathf.Max(0f, rc.TimeRemaining));
        timeText.text = $"남은 시간 {s / 60}:{s % 60:D2}";
    }

    void OnAcceptClicked()
    {
        Hide();
        var cb = onAccept; onAccept = null; onDecline = null;
        cb?.Invoke();
    }

    void OnDeclineClicked()
    {
        Hide();
        var cb = onDecline; onAccept = null; onDecline = null;
        cb?.Invoke();
    }

    static string DefaultSituation(CargoType t, string dest) => t switch
    {
        CargoType.Eggs => $"\"{dest} 주방인데요, 브런치 손님이 몰렸어요! 계란이 다 떨어졌어요!\"",
        CargoType.Glass => $"\"{dest}예요. 오늘 저녁 행사 와인잔이 깨졌어요. 하나도 깨지면 안 돼요!\"",
        CargoType.Fish => $"\"{dest}! 활어가 다 팔렸는데 단체 손님이 오고 있어요. 신선할 때 와 주세요!\"",
        CargoType.Cake => $"\"{dest}에서 결혼식이 곧 시작해요. 케이크가 아직 안 왔어요!\"",
        _ => $"\"{dest}예요. 급한 물건이 있는데 지금 바로 와 줄 수 있어요?\"",
    };

    // ---------------------------------------------------------------
    void Build()
    {
        var rt = (RectTransform)transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        root = new GameObject("Root", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f); // 뒤 화면 어둡게 + 클릭 막기

        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(root.transform, false);
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(760, 520);
        card.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.97f);

        var band = new GameObject("Band", typeof(RectTransform), typeof(Image));
        band.transform.SetParent(card.transform, false);
        var brt = band.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(0.5f, 1);
        brt.sizeDelta = new Vector2(0, 70); brt.anchoredPosition = Vector2.zero;
        band.GetComponent<Image>().color = new Color(0.88f, 0.10f, 0.08f, 1f);
        var head = Label(band.transform, "긴급 전화", 36, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(head.rectTransform);

        callerText = Label(card.transform, "", 26, FontStyles.Bold, TextAlignmentOptions.Left);
        Place(callerText.rectTransform, new Vector2(0, -95), new Vector2(680, 40));
        situationText = Label(card.transform, "", 28, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Place(situationText.rectTransform, new Vector2(0, -145), new Vector2(680, 120));
        detailText = Label(card.transform, "", 22, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        detailText.color = new Color(0.78f, 0.80f, 0.85f, 1f);
        Place(detailText.rectTransform, new Vector2(0, -275), new Vector2(680, 100));
        timeText = Label(card.transform, "", 30, FontStyles.Bold, TextAlignmentOptions.Left);
        timeText.color = new Color(1f, 0.82f, 0.25f, 1f);
        Place(timeText.rectTransform, new Vector2(-170, -405), new Vector2(340, 50));

        acceptBtn = MakeButton(card.transform, "Accept", "받을게요!", new Vector2(250, -395), new Vector2(200, 70),
            new Color(0.88f, 0.10f, 0.08f, 1f));
        acceptBtn.onClick.AddListener(OnAcceptClicked);
        declineBtn = MakeButton(card.transform, "Decline", "못 가요", new Vector2(60, -395), new Vector2(150, 70),
            new Color(0.25f, 0.27f, 0.32f, 1f));
        declineBtn.onClick.AddListener(OnDeclineClicked);

        if (UnityEngine.EventSystems.EventSystem.current == null &&
            FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            Debug.LogWarning("[UrgentCallPanel] EventSystem 없음 — 버튼 클릭 불가");

        root.SetActive(false);
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = pos; r.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        var t = Label(go.transform, label, 26, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        return go.GetComponent<Button>();
    }

    static TMP_Text Label(Transform parent, string text, float size, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        t.text = text; t.fontSize = size; t.fontStyle = style; t.alignment = align;
        t.color = Color.white;
        var theme = UITheme.Active;
        if (theme != null && theme.fontPrimary != null) t.font = theme.fontPrimary;
        return t;
    }

    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
