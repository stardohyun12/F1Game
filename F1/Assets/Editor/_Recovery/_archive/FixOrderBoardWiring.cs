using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FixOrderBoardWiring
{
    const string PrefabPath = "Assets/Prefabs/UI/OrderCard.prefab";

    public static void Execute()
    {
        var board = Object.FindFirstObjectByType<OrderBoard>();
        if (board == null)
        {
            Debug.LogError("[Wire] OrderBoard not found.");
            return;
        }

        var uiRoot = UIRoot.Ensure();

        // 기존 OrderBoardPanel 있으면 제거 (중복 방지).
        var oldPanel = uiRoot.Find("OrderBoardPanel");
        if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);

        // 패널 루트 (중앙 패널, 반투명 검정 BG).
        var panelGo = new GameObject("OrderBoardPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(uiRoot, false);
        var panelRT = panelGo.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(1100, 650);
        panelRT.anchoredPosition = Vector2.zero;
        var panelImg = panelGo.GetComponent<Image>();
        panelImg.color = new Color(0.05f, 0.06f, 0.10f, 0.94f);

        // 헤더 텍스트(선택, 시각용).
        var headerGo = new GameObject("Header",
            typeof(RectTransform), typeof(CanvasRenderer));
        headerGo.transform.SetParent(panelGo.transform, false);
        var headerRT = headerGo.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -10);
        headerRT.sizeDelta = new Vector2(0, 60);
        var headerText = headerGo.AddComponent<TMPro.TextMeshProUGUI>();
        headerText.text = "주문 선택";
        headerText.fontSize = 36;
        headerText.alignment = TMPro.TextAlignmentOptions.Center;
        headerText.color = Color.white;

        // 카드 컨테이너 — Horizontal Layout Group.
        var containerGo = new GameObject("CardsContainer", typeof(RectTransform));
        containerGo.transform.SetParent(panelGo.transform, false);
        var containerRT = containerGo.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.04f, 0.08f);
        containerRT.anchorMax = new Vector2(0.96f, 0.88f);
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;
        var hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // 도움말 텍스트(선택).
        var helpGo = new GameObject("Help",
            typeof(RectTransform), typeof(CanvasRenderer));
        helpGo.transform.SetParent(panelGo.transform, false);
        var helpRT = helpGo.GetComponent<RectTransform>();
        helpRT.anchorMin = new Vector2(0, 0);
        helpRT.anchorMax = new Vector2(1, 0);
        helpRT.pivot = new Vector2(0.5f, 0);
        helpRT.anchoredPosition = new Vector2(0, 8);
        helpRT.sizeDelta = new Vector2(0, 36);
        var helpText = helpGo.AddComponent<TMPro.TextMeshProUGUI>();
        helpText.text = "카드에 마우스 올리면 맵에 배달지 표시 · 클릭으로 출발";
        helpText.fontSize = 20;
        helpText.alignment = TMPro.TextAlignmentOptions.Center;
        helpText.color = new Color(0.8f, 0.8f, 0.85f);

        // OrderCard prefab 로드.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[Wire] Prefab not found: {PrefabPath}");
            return;
        }

        // OrderBoard 인스펙터 와이어링.
        var so = new SerializedObject(board);
        var pPanel = so.FindProperty("panelRoot");
        var pContainer = so.FindProperty("cardsContainer");
        var pPrefab = so.FindProperty("cardPrefab");
        var pHeader = so.FindProperty("headerText");
        var pHelp = so.FindProperty("helpText");

        if (pPanel != null) pPanel.objectReferenceValue = panelGo;
        if (pContainer != null) pContainer.objectReferenceValue = containerRT;
        if (pPrefab != null) pPrefab.objectReferenceValue = prefab;
        if (pHeader != null) pHeader.objectReferenceValue = headerText;
        if (pHelp != null) pHelp.objectReferenceValue = helpText;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(board);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Wire] OrderBoardPanel + CardsContainer 생성 + 와이어링 완료. Saved.");
    }
}
