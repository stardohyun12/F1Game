using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// ESC로 토글하는 Pause 메뉴. Time.timeScale 0/1. Resume + Quit 버튼.
/// 풀스크린 반투명 overlay + 중앙 패널.
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
        Debug.LogWarning("[PauseMenu] Canvas 밖. UIRoot 직속으로.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;

        // 전체 화면 덮기
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        if (overlay == null)
        {
            var bgGo = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(transform, false);
            overlay = bgGo.GetComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.65f);
            overlay.raycastTarget = true; // 뒤쪽 UI 클릭 차단
            var bgRt = overlay.rectTransform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        }

        if (panelRt == null)
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(transform, false);
            var panelImg = panelGo.GetComponent<Image>();
            panelImg.color = theme != null ? new Color(0.12f, 0.12f, 0.13f, 0.98f) : new Color(0.12f, 0.12f, 0.13f, 0.98f);
            if (theme != null && theme.panelBackground != null) { panelImg.sprite = theme.panelBackground; panelImg.type = Image.Type.Sliced; }
            panelImg.raycastTarget = true;
            panelRt = panelImg.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480, 360);

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panelRt, false);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "PAUSED";
            title.alignment = TextAlignmentOptions.Center;
            title.fontSize = 56;
            title.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) title.font = theme.fontPrimary; title.color = theme.colorAccent; }
            var tRt = title.rectTransform;
            tRt.anchorMin = new Vector2(0, 1); tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.sizeDelta = new Vector2(0, 80);
            tRt.anchoredPosition = new Vector2(0, -32);

            // Resume button
            CreateButton("ResumeBtn", panelRt, "RESUME", new Vector2(0, 40), () => SetOpen(false));
            // Quit button
            CreateButton("QuitBtn", panelRt, "QUIT", new Vector2(0, -40), OnQuit);
        }
    }

    void CreateButton(string name, RectTransform parent, string label, Vector2 anchoredPos, System.Action onClick)
    {
        var theme = UITheme.Active;
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = theme != null ? new Color(0.25f, 0.25f, 0.28f, 1f) : new Color(0.25f, 0.25f, 0.28f, 1f);
        if (theme != null && theme.panelBackground != null) { img.sprite = theme.panelBackground; img.type = Image.Type.Sliced; }
        var btnRt = img.rectTransform;
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(280, 64);
        btnRt.anchoredPosition = anchoredPos;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => { UISounds.Click(); onClick?.Invoke(); });

        var lblGo = new GameObject("Label", typeof(RectTransform));
        lblGo.transform.SetParent(btnRt, false);
        var lbl = lblGo.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.fontSize = 28;
        lbl.fontStyle = FontStyles.Bold;
        if (theme != null) { if (theme.fontPrimary != null) lbl.font = theme.fontPrimary; lbl.color = theme.colorText; }
        lbl.raycastTarget = false;
        var lRt = lbl.rectTransform;
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(!isOpen);
        }
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
