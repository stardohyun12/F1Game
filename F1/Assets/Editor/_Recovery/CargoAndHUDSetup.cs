using UnityEditor;
using UnityEngine;

/// <summary>
/// 세션 14 D-040: Race 프리팹에 CargoBox 부착 + 현재 씬에 IntegrityHUD 배치.
/// 멱등. 이미 있으면 스킵.
/// </summary>
public static class CargoAndHUDSetup
{
    const string RacePrefabPath = "Assets/Prefabs/Race.prefab";

    [MenuItem("PitCrew/Cargo/Setup Cargo + IntegrityHUD")]
    public static void Execute()
    {
        // 1. Race prefab에 CargoBox 영구 부착 (씬 인스턴스에도 자동 반영)
        var editableRoot = PrefabUtility.LoadPrefabContents(RacePrefabPath);
        if (editableRoot == null)
        {
            Debug.LogError($"[CargoSetup] {RacePrefabPath} 없음. SetupRaceInScene 먼저 실행.");
            return;
        }

        bool cargoAdded = false;
        if (editableRoot.GetComponent<CargoBox>() == null)
        {
            editableRoot.AddComponent<CargoBox>();
            cargoAdded = true;
        }

        if (cargoAdded)
        {
            PrefabUtility.SaveAsPrefabAsset(editableRoot, RacePrefabPath);
            Debug.Log("[CargoSetup] Race prefab에 CargoBox 추가 완료.");
        }
        else
        {
            Debug.Log("[CargoSetup] Race prefab에 이미 CargoBox 있음. 스킵.");
        }
        PrefabUtility.UnloadPrefabContents(editableRoot);

        // 2. 현재 씬에 IntegrityHUD 배치 (SetupUIScene이 모든 HUD 점검 + 씬 저장)
        SetupUIScene.Execute();

        Debug.Log("[CargoSetup] 완료. Play 실행해서 CargoBox FixedUpdate + IntegrityHUD 렌더 확인.");
    }
}
