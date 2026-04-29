#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 세션 16 후속(A): OrderCard prefab 빌드 자동화.
/// Coplay execute_script로 한 번에 호출 → 6개 StatRow의 Label+Value 추가, OrderCardUI 와이어링,
/// prefab 저장, RunRoot의 OrderBoard 컴포넌트 references 와이어링까지.
/// 도현이 일어나서 검토 후 바로 Play 가능 상태.
/// </summary>
public static class BuildOrderCardPrefab
{
    public static void Execute()
    {
        var card = GameObject.Find("UIRoot/OrderBoardPanel/OrderCard");
        if (card == null)
        {
            Debug.LogError("[BuildOrderCardPrefab] OrderCard not found.");
            return;
        }

        // 6개 StatRow에 Label + Value 추가, 투명화.
        string[] defaultLabels = { "지점", "시간", "보상", "무게", "화물", "포장" };
        var statRows = new OrderCardStatRow[6];
        for (int i = 0; i < 6; i++)
        {
            var row = card.transform.Find($"StatRow{i}");
            if (row == null)
            {
                Debug.LogError($"[BuildOrderCardPrefab] StatRow{i} not found.");
                return;
            }
            var img = row.GetComponent<Image>();
            if (img != null) img.color = new Color(0f, 0f, 0f, 0f);

            // Label (좌측)
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(row, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0); labelRT.anchorMax = new Vector2(0, 1);
            labelRT.pivot = new Vector2(0, 0.5f);
            labelRT.anchoredPosition = new Vector2(0, 0);
            labelRT.sizeDelta = new Vector2(120, 0);
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.raycastTarget = false;
            labelTMP.text = defaultLabels[i];
            labelTMP.fontSize = 18;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            labelTMP.textWrappingMode = TextWrappingModes.NoWrap;

            // Value (우측)
            var valueGO = new GameObject("Value", typeof(RectTransform));
            valueGO.transform.SetParent(row, false);
            var valueRT = valueGO.GetComponent<RectTransform>();
            valueRT.anchorMin = new Vector2(0, 0); valueRT.anchorMax = new Vector2(1, 1);
            valueRT.pivot = new Vector2(1, 0.5f);
            valueRT.anchoredPosition = new Vector2(0, 0);
            valueRT.sizeDelta = new Vector2(-130, 0);
            var valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.raycastTarget = false;
            valueTMP.text = "—";
            valueTMP.fontSize = 22;
            valueTMP.fontStyle = FontStyles.Bold;
            valueTMP.alignment = TextAlignmentOptions.MidlineRight;
            valueTMP.color = new Color(1f, 0.92f, 0.40f, 1f);
            valueTMP.textWrappingMode = TextWrappingModes.NoWrap;

            // 테마 폰트
            var theme = UITheme.Active;
            if (theme != null && theme.fontPrimary != null)
            {
                labelTMP.font = theme.fontPrimary;
                valueTMP.font = theme.fontPrimary;
            }

            statRows[i] = new OrderCardStatRow { label = labelTMP, value = valueTMP };
        }

        // TitleText, TipText에도 테마 폰트 적용.
        ApplyThemeFontTo(card, "TitleText");
        ApplyThemeFontTo(card, "TipText");

        // OrderCardUI 컴포넌트 references 와이어링.
        var ui = card.GetComponent<OrderCardUI>();
        if (ui == null) ui = card.AddComponent<OrderCardUI>();
        ui.bg = card.GetComponent<Image>();
        var stripGO = card.transform.Find("ColorStrip");
        if (stripGO != null) ui.colorStrip = stripGO.GetComponent<Image>();
        var hoverGO = card.transform.Find("HoverStrip");
        if (hoverGO != null) ui.hoverStrip = hoverGO.GetComponent<Image>();
        var titleGO = card.transform.Find("TitleText");
        if (titleGO != null) ui.titleText = titleGO.GetComponent<TMP_Text>();
        var tipGO = card.transform.Find("TipText");
        if (tipGO != null) ui.tipText = tipGO.GetComponent<TMP_Text>();
        ui.statRows = statRows;

        // prefab으로 저장.
        const string prefabFolder = "Assets/Prefabs/UI";
        if (!Directory.Exists(prefabFolder))
            Directory.CreateDirectory(prefabFolder);
        const string prefabPath = "Assets/Prefabs/UI/OrderCard.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(card, prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[BuildOrderCardPrefab] Prefab save failed.");
            return;
        }
        Debug.Log($"[BuildOrderCardPrefab] Saved prefab → {prefabPath}");

        // 씬에서 임시 OrderCard 제거 (prefab 저장 완료).
        Object.DestroyImmediate(card);

        // RunRoot의 OrderBoard 컴포넌트 references 와이어링.
        var runRoot = GameObject.Find("RunRoot");
        if (runRoot != null)
        {
            var board = runRoot.GetComponent<OrderBoard>();
            if (board != null)
            {
                var so = new SerializedObject(board);
                var panelProp = so.FindProperty("panelRoot");
                var containerProp = so.FindProperty("cardsContainer");
                var prefabProp = so.FindProperty("cardPrefab");
                var headerProp = so.FindProperty("headerText");
                var helpProp = so.FindProperty("helpText");

                var panelObj = GameObject.Find("UIRoot/OrderBoardPanel");
                var containerObj = GameObject.Find("UIRoot/OrderBoardPanel/CardsContainer");
                var headerObj = GameObject.Find("UIRoot/OrderBoardPanel/HeaderText");
                var helpObj = GameObject.Find("UIRoot/OrderBoardPanel/HelpText");

                if (panelProp != null && panelObj != null) panelProp.objectReferenceValue = panelObj;
                if (containerProp != null && containerObj != null) containerProp.objectReferenceValue = containerObj.GetComponent<RectTransform>();
                if (prefabProp != null) prefabProp.objectReferenceValue = prefab;
                if (headerProp != null && headerObj != null) headerProp.objectReferenceValue = headerObj.GetComponent<TMP_Text>();
                if (helpProp != null && helpObj != null) helpProp.objectReferenceValue = helpObj.GetComponent<TMP_Text>();

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(board);
                Debug.Log("[BuildOrderCardPrefab] OrderBoard references 와이어링 완료.");
            }
        }

        // 패널은 시작 시 비활성 (OrderBoard.Awake가 토글하지만 시작 시 보이지 않게).
        var panel = GameObject.Find("UIRoot/OrderBoardPanel");
        if (panel != null) panel.SetActive(false);

        // 씬 저장.
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BuildOrderCardPrefab] 완료. Play 모드에서 검증 가능.");
    }

    static void ApplyThemeFontTo(GameObject parent, string childName)
    {
        var t = parent.transform.Find(childName);
        if (t == null) return;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp == null) return;
        var theme = UITheme.Active;
        if (theme != null && theme.fontPrimary != null) tmp.font = theme.fontPrimary;
    }
}
#endif
