using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 채널 8 — OrderBoard 카드 컨테이너를 우측 얇은 스트립으로 재배치 (맵 중심 레이아웃 A).
/// panelRoot 배경도 우측 스트립으로 축소 → 맵이 화면 대부분.
/// 멱등.
/// </summary>
public static class Channel8BoardLayoutSetup
{
    [MenuItem("PitCrew/Channel8/Board Layout — 카드 우측 스트립")]
    public static void Run()
    {
        var board = UnityEngine.Object.FindFirstObjectByType<OrderBoard>(FindObjectsInactive.Include);
        if (board == null) { Debug.LogError("[Channel8BoardLayoutSetup] OrderBoard 못 찾음"); return; }

        // 필드 reflection — cardsContainer + panelRoot
        var bt = typeof(OrderBoard);
        var ccF = bt.GetField("cardsContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var prF = bt.GetField("panelRoot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cc = ccF != null ? ccF.GetValue(board) as RectTransform : null;
        var pr = prF != null ? prF.GetValue(board) as GameObject : null;

        // panelRoot → 우측 320 폭 스트립
        if (pr != null)
        {
            var prt = pr.GetComponent<RectTransform>();
            if (prt != null)
            {
                prt.anchorMin = new Vector2(1, 0);
                prt.anchorMax = new Vector2(1, 1);
                prt.pivot = new Vector2(1, 0.5f);
                prt.anchoredPosition = new Vector2(0, 0);
                prt.sizeDelta = new Vector2(320, 0);
                EditorUtility.SetDirty(prt);
            }
            // 배경 살짝 반투명 — 맵이 비치게
            var img = pr.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color; c.a = 0.85f; img.color = c;
                EditorUtility.SetDirty(img);
            }
        }

        // cardsContainer → panelRoot 안에서 위아래 마진만 두고 채움
        if (cc != null)
        {
            cc.anchorMin = new Vector2(0, 0);
            cc.anchorMax = new Vector2(1, 1);
            cc.pivot = new Vector2(0.5f, 0.5f);
            cc.anchoredPosition = new Vector2(0, -20); // 위 header 공간 약간
            cc.sizeDelta = new Vector2(-20, -120); // 좌우 10 + 위 100 + 아래 20 마진
            EditorUtility.SetDirty(cc);

            // HorizontalLayoutGroup → VerticalLayoutGroup (우측 스트립이라 세로 스택)
            var hlg = cc.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) Object.DestroyImmediate(hlg);
            var vlg = cc.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = cc.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            EditorUtility.SetDirty(vlg);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[Channel8BoardLayoutSetup] OrderBoard 카드 우측 스트립 배치 완료.");
    }
}
