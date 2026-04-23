using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 현재 열린 씬에 UIRoot Canvas + 기본 HUD 6종 배치 (세션 13 확장).
/// 이미 존재하면 스킵. 모든 HUD는 Awake에서 자가구성 — 여기선 GameObject만 생성.
/// </summary>
public static class SetupUIScene
{
    [MenuItem("PitCrew/UI/Setup UI Scene (Current)")]
    public static void Execute()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var rootT = UIRoot.Ensure();
        Debug.Log("[UIScene] UIRoot ready: " + rootT.name);

        EnsureHUD<FuelHUD>("FuelHUD", rootT);
        EnsureHUD<GearHUD>("GearHUD", rootT);
        EnsureHUD<SpeedometerHUD>("SpeedometerHUD", rootT);
        EnsureHUD<DeliveryProgressHUD>("DeliveryProgressHUD", rootT);

        // DeliveryHUD는 SetupOrderSystem이 Canvas 컴포넌트를 붙여 만들었던 기존 GameObject가 있을 수 있음.
        // 그 기존 GameObject의 Canvas 제거 + UIRoot 하위로 이동 (RectTransform 교체).
        var existingDelivery = Object.FindFirstObjectByType<DeliveryHUD>();
        if (existingDelivery == null)
        {
            var go = new GameObject("DeliveryHUD", typeof(RectTransform));
            go.transform.SetParent(rootT, false);
            go.AddComponent<DeliveryHUD>();
            Debug.Log("[UIScene] Added DeliveryHUD");
        }
        else
        {
            // 기존 배치 재정돈 — 이전 버전은 루트 Canvas GameObject였음
            var oldCanvas = existingDelivery.GetComponent<Canvas>();
            if (oldCanvas != null)
            {
                // Canvas + GraphicRaycaster 제거. RectTransform으로 교체.
                var gr = existingDelivery.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (gr != null) Object.DestroyImmediate(gr);
                Object.DestroyImmediate(oldCanvas);

                // Transform → RectTransform 교체는 불가. GameObject 재생성.
                var name = existingDelivery.name;
                Object.DestroyImmediate(existingDelivery.gameObject);
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(rootT, false);
                go.AddComponent<DeliveryHUD>();
                Debug.Log("[UIScene] Reparented legacy DeliveryHUD under UIRoot");
            }
            else if (existingDelivery.transform.parent != rootT)
            {
                existingDelivery.transform.SetParent(rootT, false);
            }
        }

        EnsureHUD<PauseMenu>("PauseMenu", rootT);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[UIScene] Scene saved: " + scene.path);
    }

    static void EnsureHUD<T>(string name, Transform parent) where T : Component
    {
        if (Object.FindFirstObjectByType<T>() != null) return;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<T>();
        Debug.Log($"[UIScene] Added {typeof(T).Name}");
    }
}
