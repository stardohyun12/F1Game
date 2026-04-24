using UnityEditor;
using UnityEngine;

/// <summary>
/// Race prefab에 CarVisual 자식 wrapper 추가 + body/wheels 4개 reparent + CarVisualLean 부착.
/// 세션 14 리서치 TOP 3 (benmcinnes 패턴).
///
/// 변경 전 구조: Race/{body, wheel-*, SkidMark_*, Smoke_*}
/// 변경 후 구조: Race/CarVisual/{body, wheel-*}, Race/{SkidMark_*, Smoke_*, CarVisual}
///
/// SkidMark/Smoke는 Race에 그대로 (월드 기준 지면 흔적, rig 영향 X).
/// 멱등: 이미 CarVisual 하위로 옮겨져 있으면 스킵.
/// </summary>
public static class SetupCarVisualRig
{
    const string RacePrefabPath = "Assets/Prefabs/Race.prefab";

    static readonly string[] VisualChildren = new[]
    {
        "body",
        "wheel-back-left",
        "wheel-back-right",
        "wheel-front-left",
        "wheel-front-right",
    };

    [MenuItem("PitCrew/Car/Setup Visual Rig")]
    public static void Execute()
    {
        var edit = PrefabUtility.LoadPrefabContents(RacePrefabPath);
        if (edit == null) { Debug.LogError("[VisualRig] Race prefab missing."); return; }

        // 1. CarVisual wrapper
        Transform rigT = edit.transform.Find("CarVisual");
        if (rigT == null)
        {
            var rigGO = new GameObject("CarVisual");
            rigGO.transform.SetParent(edit.transform, false);
            rigGO.transform.localPosition = Vector3.zero;
            rigGO.transform.localRotation = Quaternion.identity;
            rigGO.transform.localScale = Vector3.one;
            rigT = rigGO.transform;
            Debug.Log("[VisualRig] CarVisual wrapper 생성.");
        }

        // 2. body + wheels reparent
        int moved = 0;
        foreach (var name in VisualChildren)
        {
            var child = edit.transform.Find(name);
            if (child == null) continue;                    // 이름 변경 or 부재
            if (child.parent == rigT) continue;             // 이미 이동됨
            if (child.parent != edit.transform) continue;   // 다른 곳에 있음 (건드리지 않음)

            Vector3 lp = child.localPosition;
            Quaternion lr = child.localRotation;
            Vector3 ls = child.localScale;
            child.SetParent(rigT, false);
            child.localPosition = lp;
            child.localRotation = lr;
            child.localScale = ls;
            moved++;
        }
        if (moved > 0) Debug.Log($"[VisualRig] {moved}개 자식 CarVisual로 reparent 완료.");

        // 3. CarVisualLean 부착
        if (rigT.GetComponent<CarVisualLean>() == null)
        {
            rigT.gameObject.AddComponent<CarVisualLean>();
            Debug.Log("[VisualRig] CarVisualLean 컴포넌트 부착.");
        }

        PrefabUtility.SaveAsPrefabAsset(edit, RacePrefabPath);
        PrefabUtility.UnloadPrefabContents(edit);
        Debug.Log("[VisualRig] Setup 완료.");
    }
}
