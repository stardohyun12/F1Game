#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 세션 16 후속(A): create_ui_element가 legacy UI.Text를 만든 것을 TextMeshProUGUI로 변환.
/// HeaderText/HelpText (씬, inactive 패널 안) + TitleText/TipText (prefab) 4개.
/// </summary>
public static class ConvertOrderBoardTextsToTMP
{
    public static void Execute()
    {
        // UIRoot는 active. OrderBoardPanel은 inactive — Transform.Find로 inactive 자식 찾기.
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot == null)
        {
            Debug.LogError("[Convert] UIRoot not found");
            return;
        }

        var panelT = uiRoot.transform.Find("OrderBoardPanel");
        if (panelT == null)
        {
            Debug.LogError("[Convert] OrderBoardPanel not found under UIRoot");
            return;
        }

        // 1. Scene texts
        var headerT = panelT.Find("HeaderText");
        var helpT = panelT.Find("HelpText");
        if (headerT != null) ReplaceLegacyText(headerT.gameObject, "주문 선택", 36, TextAlignmentOptions.Center, new Color(1, 1, 1, 1), false);
        else Debug.LogWarning("[Convert] HeaderText child not found");
        if (helpT != null) ReplaceLegacyText(helpT.gameObject, "카드에 마우스 올리면 맵에 배달지가 표시됨 · 클릭으로 출발", 20, TextAlignmentOptions.MidlineLeft, new Color(1, 0.95f, 0.6f, 1), false);
        else Debug.LogWarning("[Convert] HelpText child not found");

        // 2. Wire OrderBoard.headerText/helpText
        var runRoot = GameObject.Find("RunRoot");
        if (runRoot != null)
        {
            var board = runRoot.GetComponent<OrderBoard>();
            if (board != null)
            {
                var so = new SerializedObject(board);
                var headerProp = so.FindProperty("headerText");
                var helpProp = so.FindProperty("helpText");
                if (headerProp != null && headerT != null)
                    headerProp.objectReferenceValue = headerT.GetComponent<TextMeshProUGUI>();
                if (helpProp != null && helpT != null)
                    helpProp.objectReferenceValue = helpT.GetComponent<TextMeshProUGUI>();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(board);
                Debug.Log($"[Convert] OrderBoard.headerText={(headerT?.GetComponent<TextMeshProUGUI>()?.name ?? "null")}, helpText={(helpT?.GetComponent<TextMeshProUGUI>()?.name ?? "null")}");
            }
        }

        // 3. Prefab edit
        const string prefabPath = "Assets/Prefabs/UI/OrderCard.prefab";
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("[Convert] Prefab load failed.");
            return;
        }

        var titleT = prefabRoot.transform.Find("TitleText");
        var tipT = prefabRoot.transform.Find("TipText");
        if (titleT != null) ReplaceLegacyText(titleT.gameObject, "주문 제목", 30, TextAlignmentOptions.MidlineLeft, new Color(1, 0.97f, 0.85f, 1), false);
        if (tipT != null) ReplaceLegacyText(tipT.gameObject, "→ 운전 가이드\n→ 포장 가이드", 16, TextAlignmentOptions.TopLeft, new Color(1, 0.85f, 0.5f, 1), true);

        // Re-wire OrderCardUI references
        var cardUI = prefabRoot.GetComponent<OrderCardUI>();
        if (cardUI != null)
        {
            if (titleT != null) cardUI.titleText = titleT.GetComponent<TMP_Text>();
            if (tipT != null) cardUI.tipText = tipT.GetComponent<TMP_Text>();
            EditorUtility.SetDirty(cardUI);
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        // 4. Save scene
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Convert] 완료.");
    }

    static void ReplaceLegacyText(GameObject go, string text, float fontSize, TextAlignmentOptions align, Color color, bool wrap)
    {
        var legacy = go.GetComponent<Text>();
        if (legacy != null) Object.DestroyImmediate(legacy, true);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

        var theme = UITheme.Active;
        if (theme != null && theme.fontPrimary != null) tmp.font = theme.fontPrimary;

        EditorUtility.SetDirty(go);
    }
}
#endif
