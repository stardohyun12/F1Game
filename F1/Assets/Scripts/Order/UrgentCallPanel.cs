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
    Button acceptBtn;
    Action onAccept;

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

    /// <summary>전화 표시. limitSec = 수락부터 배달 완료까지 목표 시간(포장 포함).</summary>
    public void Show(OrderJob job, float limitSec, Action accept)
    {
        onAccept = accept;
        string dest = job.destinationLabels != null && job.destinationLabels.Count > 0
            ? string.Join(" → ", job.destinationLabels) : "-";
        string cargo = CargoTypePresets.DisplayName(job.cargoType);
        callerText.text = string.IsNullOrEmpty(job.callerName) ? $"{dest}에서 전화" : job.callerName;
        situationText.text = string.IsNullOrEmpty(job.urgentSituation) ? DefaultSituation(job.cargoType, dest) : job.urgentSituation;
        string bonus = job.timeBonus > 0 ? $"  (+ 시간 안이면 ₩{job.timeBonus:N0})" : "";
        detailText.text = $"배달지  {dest}\n화물  {cargo}\n보상  ₩{job.baseReward:N0}{bonus}";
        int s = Mathf.CeilToInt(limitSec);
        timeText.text = $"{s / 60}:{s % 60:D2} 안에!";
        transform.SetAsLastSibling();
        root.SetActive(true);
        AudioCueBus.Publish(AudioCueId.PackingConfirm);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    void OnAcceptClicked()
    {
        Hide();
        var cb = onAccept; onAccept = null;
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
        timeText = Label(card.transform, "", 40, FontStyles.Bold, TextAlignmentOptions.Left);
        timeText.color = new Color(1f, 0.82f, 0.25f, 1f);
        Place(timeText.rectTransform, new Vector2(-150, -400), new Vector2(380, 60));

        var btnGO = new GameObject("Accept", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(card.transform, false);
        var art = btnGO.GetComponent<RectTransform>();
        art.anchorMin = art.anchorMax = new Vector2(0.5f, 1f); art.pivot = new Vector2(0.5f, 1f);
        art.anchoredPosition = new Vector2(200, -395); art.sizeDelta = new Vector2(260, 70);
        btnGO.GetComponent<Image>().color = new Color(0.88f, 0.10f, 0.08f, 1f);
        var bl = Label(btnGO.transform, "바로 갈게요!", 28, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(bl.rectTransform);
        acceptBtn = btnGO.GetComponent<Button>();
        acceptBtn.onClick.AddListener(OnAcceptClicked);

        if (UnityEngine.EventSystems.EventSystem.current == null &&
            FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            Debug.LogWarning("[UrgentCallPanel] EventSystem 없음 — 버튼 클릭 불가");

        root.SetActive(false);
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
