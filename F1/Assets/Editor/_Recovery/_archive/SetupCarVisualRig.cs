using UnityEditor;
using UnityEngine;

/// <summary>
/// Race prefab에 CarVisualLean 컴포넌트를 부착.
/// 세션 14 리서치 TOP 3. Nested prefab(Kenney car-kit) 구조 영향 없음.
///
/// 방식 변경: 초기 시도는 CarVisual wrapper로 reparent 했으나
/// "Setting the parent of a transform which resides in a Prefab instance"
/// 오류 발생 (nested prefab 자식 reparent 불가). 방식 전환 →
/// CarVisualLean을 Race 루트에 부착, body/wheel-* 각자의 localRotation 조작.
///
/// 기존 CarVisual wrapper(빈 GameObject)가 있으면 정리 삭제.
/// </summary>
public static class SetupCarVisualRig
{
    const string RacePrefabPath = "Assets/Prefabs/Race.prefab";

    [MenuItem("PitCrew/Car/Setup Visual Rig")]
    public static void Execute()
    {
        var edit = PrefabUtility.LoadPrefabContents(RacePrefabPath);
        if (edit == null) { Debug.LogError("[VisualRig] Race prefab missing."); return; }

        // 1. 이전 잘못된 시도의 잔재 CarVisual wrapper 제거
        var wrapper = edit.transform.Find("CarVisual");
        if (wrapper != null)
        {
            Object.DestroyImmediate(wrapper.gameObject);
            Debug.Log("[VisualRig] 이전 CarVisual wrapper 제거.");
        }

        // 2. CarVisualLean을 Race 루트에 부착 (멱등)
        if (edit.GetComponent<CarVisualLean>() == null)
        {
            edit.AddComponent<CarVisualLean>();
            Debug.Log("[VisualRig] CarVisualLean 루트 부착.");
        }
        else
        {
            Debug.Log("[VisualRig] CarVisualLean 이미 있음.");
        }

        PrefabUtility.SaveAsPrefabAsset(edit, RacePrefabPath);
        PrefabUtility.UnloadPrefabContents(edit);
        Debug.Log("[VisualRig] Setup 완료 — body + wheels가 개별 localRotation으로 기울어짐.");
    }
}
