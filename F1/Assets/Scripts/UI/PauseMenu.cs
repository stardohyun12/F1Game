using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Pause 메뉴. 세션 13 Kenney panelGloss + button sprite.
/// ESC 토글, Time.timeScale 0/1, Kenney click/back SFX.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class PauseMenu : MonoBehaviour
{
    RectTransform rt;
    CanvasGroup group;
    Image overlay;
    RectTransform panelRt;
    bool isOpen;

    void Awake()
    {
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        BuildIfNeeded();
        SetOpen(false);
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[PauseMenu] Canvas 밖.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        if (overlay == null)
        {
            var go = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            overlay = go.GetComponent<Image>();
            overlay.color = UIPalette.WithAlpha(UIPalette.Ink, 0.75f);
            overlay.raycastTarget = true;
            var r = overlay.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        if (panelRt == null)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var panelImg = go.GetComponent<Image>();
            UIPalette.PaperPanel(panelImg);     // D-121: 종이 패널
            panelImg.raycastTarget = true;
            panelRt = panelImg.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(520, 400);

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panelRt, false);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "일시 정지";
            title.alignment = TextAlignmentOptions.Center;
            title.fontSize = 60; title.fontStyle = FontStyles.Bold;
            UIPalette.Display(title, UIPalette.Ink);
            var tR = title.rectTransform;
            tR.anchorMin = new Vector2(0, 1); tR.anchorMax = new Vector2(1, 1); tR.pivot = new Vector2(0.5f, 1);
            tR.sizeDelta = new Vector2(0, 96); tR.anchoredPosition = new Vector2(0, -40);

            CreateButton("ResumeBtn", panelRt, "계속하기", new Vector2(0, 30), () => SetOpen(false), UIPalette.Terracotta, UIPalette.Paper);
            CreateButton("QuitBtn", panelRt, "나가기", new Vector2(0, -60), OnQuit, UIPalette.PaperDeep, UIPalette.Ink);
        }
    }

    void CreateButton(string name, RectTransform parent, string label, Vector2 anchoredPos, System.Action onClick,
        Color fill, Color textColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        UIPalette.PaperPanel(img, fill, slot: UISlot.Button);
        var btnRt = img.rectTransform;
        btnRt.anchorMin = new Vector2(0.5f, 0.5f); btnRt.anchorMax = new Vector2(0.5f, 0.5f); btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(320, 72);
        btnRt.anchoredPosition = anchoredPos;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        UIPalette.ButtonColors(btn, fill);     // D-121: 글로스 스프라이트 대신 평면 색 상태
        btn.onClick.AddListener(() => { UISounds.Click(); onClick?.Invoke(); });

        var lblGo = new GameObject("Label", typeof(RectTransform));
        lblGo.transform.SetParent(btnRt, false);
        var lbl = lblGo.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.fontSize = 32; lbl.fontStyle = FontStyles.Bold;
        UIPalette.Body(lbl, textColor);
        lbl.raycastTarget = false;
        var lR = lbl.rectTransform;
        lR.anchorMin = Vector2.zero; lR.anchorMax = Vector2.one;
        lR.offsetMin = Vector2.zero; lR.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetOpen(!isOpen);
    }

    void SetOpen(bool open)
    {
        isOpen = open;
        if (group != null)
        {
            group.alpha = open ? 1f : 0f;
            group.interactable = open;
            group.blocksRaycasts = open;
        }
        Time.timeScale = open ? 0f : 1f;
        if (open) UISounds.Back();
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
