using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Phase 1 통합 setup — 모든 신규 시스템을 1번 클릭으로 자동 연결 (세션 15 마무리).
///
/// 메뉴: PitCrew/Phase 1/Full Integration Setup
///
/// 실행 순서:
/// 1. GenerateExpandedOrders — 8개 주문 + OrderCatalogExpanded.asset
/// 2. GenerateUpgrades — 5개 업그레이드 + UpgradeCatalog.asset
/// 3. 씬에 UpgradeSystem GameObject 추가 (없으면)
/// 4. 씬에 AudioManager GameObject 추가 (없으면, AudioClip은 도현이 별도 할당)
/// 5. OrderBoard.catalog → OrderCatalogExpanded 자동 연결
/// 6. ResultPanel.upgradeCatalog → UpgradeCatalog 자동 연결
/// 7. SetupCargoView — 트렁크 4벽 + CCTV (재실행으로 새 카메라/벽 적용)
/// 8. FixStartYaw — 차량 방향 자동 정렬
///
/// 도현 작업 후: 컴파일 OK → 메뉴 클릭 → Play 즉시.
/// </summary>
public static class SetupPhase1Integration
{
    const string OrderCatalogPath = "Assets/Data/Orders/OrderCatalogExpanded.asset";
    const string UpgradeCatalogPath = "Assets/Data/Upgrades/UpgradeCatalog.asset";

    [MenuItem("PitCrew/Phase 1/Full Integration Setup")]
    public static void Execute()
    {
        Debug.Log("[Phase1] === Full Integration Setup 시작 ===");

        // 1. 8개 주문 생성
        GenerateExpandedOrders.Generate();
        Debug.Log("[Phase1] (1/8) 8개 주문 생성 완료.");

        // 2. 5개 업그레이드 생성
        GenerateUpgrades.Generate();
        Debug.Log("[Phase1] (2/8) 5개 업그레이드 생성 완료.");

        // 3. UpgradeSystem 씬에 추가
        EnsureSingletonGO<UpgradeSystem>("UpgradeSystem");
        Debug.Log("[Phase1] (3/8) UpgradeSystem 씬 추가.");

        // 4. AudioManager 씬에 추가
        EnsureSingletonGO<AudioManager>("AudioManager");
        Debug.Log("[Phase1] (4/8) AudioManager 씬 추가 (AudioClip은 별도 할당 필요).");

        // 4.5. TireSystem 씬에 추가 (Day 20 통합 — ArcadeCarController에서 자동 참조)
        EnsureSingletonGO<TireSystem>("TireSystem");
        Debug.Log("[Phase1] (4.5/8) TireSystem 씬 추가 — Medium 기본, ArcadeCarController가 자동 연결.");

        // 5. OrderBoard.catalog 자동 연결
        var orderCatalog = AssetDatabase.LoadAssetAtPath<OrderCatalog>(OrderCatalogPath);
        var orderBoard = Object.FindFirstObjectByType<OrderBoard>();
        if (orderBoard != null && orderCatalog != null)
        {
            var so = new SerializedObject(orderBoard);
            var prop = so.FindProperty("catalog");
            if (prop != null)
            {
                prop.objectReferenceValue = orderCatalog;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(orderBoard);
                Debug.Log("[Phase1] (5/8) OrderBoard.catalog → OrderCatalogExpanded 연결.");
            }
        }
        else
        {
            Debug.LogWarning("[Phase1] (5/8) OrderBoard 또는 catalog 없음 — 수동 연결 필요.");
        }

        // 6. ResultPanel.upgradeCatalog 자동 연결
        var upgradeCatalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradeCatalogPath);
        var resultPanel = Object.FindFirstObjectByType<ResultPanel>();
        if (resultPanel != null && upgradeCatalog != null)
        {
            var so = new SerializedObject(resultPanel);
            var prop = so.FindProperty("upgradeCatalog");
            if (prop != null)
            {
                prop.objectReferenceValue = upgradeCatalog;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(resultPanel);
                Debug.Log("[Phase1] (6/8) ResultPanel.upgradeCatalog → UpgradeCatalog 연결.");
            }
        }
        else
        {
            Debug.LogWarning("[Phase1] (6/8) ResultPanel 또는 catalog 없음 — 수동 연결 필요.");
        }

        // 7. CargoView (트렁크 4벽 + CCTV) 재실행
        SetupCargoView.Execute();
        Debug.Log("[Phase1] (7/8) CargoView 재실행 — 트렁크 4벽 + 카메라 + 박스.");

        // 8. 차량 방향 자동 정렬
        FixStartYaw.Execute();
        Debug.Log("[Phase1] (8/8) FixStartYaw — 차량 방향 도로 매칭.");

        // 씬 저장
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[Phase1] === Full Integration Setup 완료. Play 가능. ===");
        EditorUtility.DisplayDialog("Phase 1 Integration",
            "✓ 8개 주문 + 5개 업그레이드 + UpgradeSystem + AudioManager\n" +
            "✓ OrderBoard / ResultPanel 자동 연결\n" +
            "✓ 트렁크 4벽 + CCTV 카메라 재배치\n" +
            "✓ 차량 방향 자동 정렬\n\n" +
            "AudioClip은 AudioManager 인스펙터에서 수동 할당.",
            "확인");
    }

    static T EnsureSingletonGO<T>(string goName) where T : MonoBehaviour
    {
        var existing = Object.FindFirstObjectByType<T>();
        if (existing != null) return existing;

        var go = GameObject.Find(goName);
        if (go == null) go = new GameObject(goName);
        var comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }
}
