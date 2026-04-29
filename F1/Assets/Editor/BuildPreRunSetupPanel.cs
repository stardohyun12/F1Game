#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 세션 16 후속(A): PreRunSetupPanel scene/prefab 일괄 빌드.
/// - UIRoot/PreRunSetupRoot 계층 신규 (Backdrop + Panel + 자식 모두)
/// - Assets/Prefabs/UI/PackingCard.prefab 신규
/// - RunRoot에 PreRunSetupPanel 컴포넌트 attach + references 와이어링
/// </summary>
public static class BuildPreRunSetupPanel
{
    public static void Execute()
    {
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot == null) { Debug.LogError("[Build] UIRoot 없음"); return; }

        var theme = UITheme.Active;
        var fontDefault = theme != null ? theme.fontPrimary : null;
        var colorPanel = theme != null ? theme.colorPanel : new Color(0.12f, 0.14f, 0.18f, 0.96f);

        // 기존 PreRunSetupRoot 있으면 제거 (재실행 안전).
        var existing = uiRoot.transform.Find("PreRunSetupRoot");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // -- Root --
        var root = new GameObject("PreRunSetupRoot", typeof(RectTransform));
        root.transform.SetParent(uiRoot.transform, false);
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;

        // Backdrop (어둡게)
        var backdrop = CreateImage(root.transform, "Backdrop", new Color(0, 0, 0, 0.65f));
        var bdRT = backdrop.rectTransform;
        bdRT.anchorMin = Vector2.zero; bdRT.anchorMax = Vector2.one;
        bdRT.offsetMin = Vector2.zero; bdRT.offsetMax = Vector2.zero;
        backdrop.raycastTarget = true; // 클릭 차단

        // Panel (1400×680 중앙)
        var panel = CreateImage(root.transform, "Panel", colorPanel);
        var panelRT = panel.rectTransform;
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(1400, 680);
        if (theme?.panelDark != null) { panel.sprite = theme.panelDark; panel.type = Image.Type.Sliced; }

        // Title
        var title = CreateText(panel.transform, "TitleText", "출발 준비",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -50), new Vector2(900, 60),
            48, FontStyles.Bold, TextAlignmentOptions.Center);

        // Cargo (좌상단)
        var cargoT = CreateText(panel.transform, "CargoText", "화물: -",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(60, -130), new Vector2(700, 44),
            36, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        // Cargo tip
        var cargoTip = CreateText(panel.transform, "CargoTipText", "",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(60, -180), new Vector2(900, 36),
            24, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
        if (theme != null) cargoTip.color = theme.colorTextMuted;

        // CardsParent (4 카드 가로)
        var cardsParentGO = new GameObject("CardsParent", typeof(RectTransform));
        cardsParentGO.transform.SetParent(panel.transform, false);
        var cpRT = cardsParentGO.GetComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0, 1);
        cpRT.anchorMax = new Vector2(1, 1);
        cpRT.pivot = new Vector2(0.5f, 1);
        cpRT.anchoredPosition = new Vector2(0, -240);
        cpRT.sizeDelta = new Vector2(-120, 320);     // 좌우 60px 패딩
        var hLayout = cardsParentGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 20;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // TotalCost (좌하단)
        var totalCost = CreateText(panel.transform, "TotalCostText", "—",
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(60, 50), new Vector2(700, 60),
            32, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        // Cancel button
        var cancelBtnGO = CreateButton(panel.transform, "CancelBtn", "취소",
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(800, 40), new Vector2(240, 80),
            36, new Color(0.40f, 0.40f, 0.45f), out var cancelText);

        // Confirm button
        var confirmBtnGO = CreateButton(panel.transform, "ConfirmBtn", "출발",
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1060, 40), new Vector2(320, 80),
            40, theme != null ? theme.colorAccent : new Color(0.95f, 0.65f, 0.15f), out var confirmText);

        // -- PackingCard prefab 빌드 (씬에 임시 GameObject로) --
        const string prefabFolder = "Assets/Prefabs/UI";
        if (!Directory.Exists(prefabFolder)) Directory.CreateDirectory(prefabFolder);
        const string prefabPath = "Assets/Prefabs/UI/PackingCard.prefab";

        var card = BuildPackingCardTemplate(uiRoot.transform);
        var prefab = PrefabUtility.SaveAsPrefabAsset(card, prefabPath);
        Object.DestroyImmediate(card);
        if (prefab == null) { Debug.LogError("[Build] PackingCard prefab 저장 실패"); return; }
        Debug.Log($"[Build] Saved {prefabPath}");

        // -- RunRoot에 PreRunSetupPanel 컴포넌트 attach --
        var runRoot = GameObject.Find("RunRoot");
        if (runRoot == null) { Debug.LogError("[Build] RunRoot 없음"); return; }
        var pre = runRoot.GetComponent<PreRunSetupPanel>();
        if (pre == null) pre = runRoot.AddComponent<PreRunSetupPanel>();

        // -- References 와이어링 --
        var so = new SerializedObject(pre);
        so.FindProperty("runController").objectReferenceValue = runRoot.GetComponent<RunController>();
        so.FindProperty("panelRoot").objectReferenceValue = root;     // PreRunSetupRoot 자체
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("cargoText").objectReferenceValue = cargoT;
        so.FindProperty("cargoTipText").objectReferenceValue = cargoTip;
        so.FindProperty("totalCostText").objectReferenceValue = totalCost;
        so.FindProperty("cardsParent").objectReferenceValue = cpRT;
        so.FindProperty("packingCardPrefab").objectReferenceValue = prefab;
        so.FindProperty("confirmBtn").objectReferenceValue = confirmBtnGO.GetComponent<Button>();
        so.FindProperty("confirmText").objectReferenceValue = confirmText;
        so.FindProperty("cancelBtn").objectReferenceValue = cancelBtnGO.GetComponent<Button>();
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(pre);

        // -- Root 비활성 (시작 시 안 보이게) --
        root.SetActive(false);

        // -- 저장 --
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Build] PreRunSetupPanel 빌드 완료.");
    }

    static GameObject BuildPackingCardTemplate(Transform tempParent)
    {
        // PackingCard 305×320 (Horizontal Layout 안에 들어갈 사이즈)
        var card = new GameObject("PackingCard", typeof(RectTransform));
        card.transform.SetParent(tempParent, false);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(305, 320);

        var bg = card.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.18f, 0.22f, 0.92f);
        var theme = UITheme.Active;
        if (theme?.panelDark != null) { bg.sprite = theme.panelDark; bg.type = Image.Type.Sliced; }

        var btn = card.AddComponent<Button>();
        btn.targetGraphic = bg;

        var ui = card.AddComponent<PackingCardUI>();

        // ColorStrip (top, 8px)
        var stripGO = new GameObject("ColorStrip", typeof(RectTransform));
        stripGO.transform.SetParent(card.transform, false);
        var sRT = stripGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 1);
        sRT.anchorMax = new Vector2(1, 1);
        sRT.pivot = new Vector2(0.5f, 1);
        sRT.anchoredPosition = Vector2.zero;
        sRT.sizeDelta = new Vector2(0, 8);
        var stripImg = stripGO.AddComponent<Image>();
        stripImg.color = new Color(0.55f, 0.85f, 1f);
        stripImg.raycastTarget = false;

        // Name (top, large)
        var nameT = CreateText(card.transform, "NameText", "포장 이름",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -22), new Vector2(0, 50),
            32, FontStyles.Bold, TextAlignmentOptions.Center);

        // Multiplier (bigger, accent color)
        var mulT = CreateText(card.transform, "MultiplierText", "×1.0",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -76), new Vector2(0, 50),
            40, FontStyles.Bold, TextAlignmentOptions.Center);
        if (theme != null) mulT.color = theme.colorAccent;

        // Desc (multiline, smaller)
        var descT = CreateText(card.transform, "DescText", "설명",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -140), new Vector2(-20, 120),
            20, FontStyles.Normal, TextAlignmentOptions.Top);
        descT.textWrappingMode = TextWrappingModes.Normal;
        descT.overflowMode = TextOverflowModes.Truncate;

        // Cost (bottom)
        var costT = CreateText(card.transform, "CostText", "—",
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 16), new Vector2(0, 36),
            26, FontStyles.Bold, TextAlignmentOptions.Center);

        // PackingCardUI references
        ui.bg = bg;
        ui.colorStrip = stripImg;
        ui.nameText = nameT;
        ui.multiplierText = mulT;
        ui.descText = descT;
        ui.costText = costT;
        ui.button = btn;

        return card;
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin == anchorMax ? anchorMin : (anchorMin + anchorMax) * 0.5f;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        var theme = UITheme.Active;
        if (theme != null)
        {
            if (theme.fontPrimary != null) t.font = theme.fontPrimary;
            t.color = theme.colorText;
        }
        return t;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        float fontSize, Color color, out TextMeshProUGUI labelText)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        var theme = UITheme.Active;
        if (theme?.panelBright != null) { img.sprite = theme.panelBright; img.type = Image.Type.Sliced; }
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        labelText = CreateText(go.transform, "Label", label,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, sizeDelta,
            fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        labelText.color = Color.white;

        return go;
    }
}
#endif
