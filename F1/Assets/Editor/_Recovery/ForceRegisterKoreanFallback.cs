using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// 한글 fallback 강제 등록 (세션 16 D-054 후속, B1).
///
/// 진단: SetupKoreanFallback이 메모리 객체 fallbackFontAssetTable에는 추가하지만
/// EditorUtility.SetDirty + AssetDatabase.SaveAssets 호출이 누락되어 .asset 파일에 직렬화 안 됨.
/// → grep 결과 KenneyFuture SDF.asset의 fallbackFontAssetTable이 비어있음.
///
/// 처치: 직접 SerializedObject로 fallbackFontAssetTable 배열에 추가 + 명시적 SetDirty + SaveAssets.
/// 검증: 메뉴 실행 직후 fallback 개수 + 첫 항목 이름 출력.
///
/// 메뉴: PitCrew/UI/Force Register Korean Fallback
/// </summary>
public static class ForceRegisterKoreanFallback
{
    const string KoreanFontAssetPath = "Assets/UI/Fonts/Korean/KoreanFallback SDF.asset";
    static readonly string[] PrimaryFontPaths = new[]
    {
        "Assets/UI/Fonts/KenneyFuture SDF.asset",
        "Assets/UI/Fonts/KenneyFutureNarrow SDF.asset",
    };

    [MenuItem("PitCrew/UI/Force Register Korean Fallback")]
    public static void Execute()
    {
        Debug.Log("[ForceFallback] === 한글 fallback 강제 등록 시작 ===");

        var korean = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
        if (korean == null)
        {
            Debug.LogError($"[ForceFallback] {KoreanFontAssetPath} 없음. 먼저 PitCrew/UI/Setup Korean Fallback Font 실행 필요.");
            EditorUtility.DisplayDialog("Force Korean Fallback",
                "KoreanFallback SDF.asset이 없습니다.\n먼저 PitCrew/UI/Setup Korean Fallback Font 메뉴를 실행하세요.",
                "확인");
            return;
        }
        Debug.Log($"[ForceFallback] (1/3) Korean SDF 로드: {korean.name} (atlas {korean.atlasWidth}×{korean.atlasHeight}).");

        int registered = 0;
        foreach (var path in PrimaryFontPaths)
        {
            var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (primary == null) { Debug.LogWarning($"[ForceFallback] {path} 없음 — 건너뜀."); continue; }

            // 직렬화 강제 — SerializedObject로 fallbackFontAssetTable 배열에 추가.
            var so = new SerializedObject(primary);
            var arr = so.FindProperty("m_FallbackFontAssetTable");
            if (arr == null)
            {
                Debug.LogError($"[ForceFallback] {primary.name}의 m_FallbackFontAssetTable 직렬화 필드 못 찾음.");
                continue;
            }

            // 이미 등록됐는지 확인
            bool already = false;
            for (int i = 0; i < arr.arraySize; i++)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == korean) { already = true; break; }
            }
            if (already)
            {
                Debug.Log($"[ForceFallback] {primary.name}: 이미 등록됨 ({arr.arraySize}개) — 건너뜀.");
                continue;
            }

            arr.InsertArrayElementAtIndex(arr.arraySize);
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = korean;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(primary);
            registered++;
            Debug.Log($"[ForceFallback] {primary.name}: fallback 추가 (총 {arr.arraySize}개) — Korean SDF 등록 + SetDirty.");
        }

        // 명시적 저장 — 핵심
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ForceFallback] (2/3) AssetDatabase.SaveAssets — {registered}개 폰트에 fallback 영구 등록.");

        // 검증 — 다시 로드해서 fallback 카운트 확인
        foreach (var path in PrimaryFontPaths)
        {
            var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (primary == null) continue;
            int count = primary.fallbackFontAssetTable != null ? primary.fallbackFontAssetTable.Count : 0;
            string firstName = count > 0 && primary.fallbackFontAssetTable[0] != null
                ? primary.fallbackFontAssetTable[0].name
                : "<none>";
            Debug.Log($"[ForceFallback] (검증) {primary.name}.fallback = {count}개 (첫 항목: {firstName}).");
        }

        // TMP_Text 전부 강제 새로고침 (이미 렌더된 한글 □ 갱신)
        var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (var t in texts) { if (t != null) t.ForceMeshUpdate(true); }
        Debug.Log($"[ForceFallback] (3/3) TMP_Text {texts.Length}개 ForceMeshUpdate — 한글 즉시 재렌더.");

        EditorUtility.DisplayDialog("Force Korean Fallback",
            $"✓ {registered}개 폰트에 한글 fallback 강제 등록.\n" +
            "✓ AssetDatabase.SaveAssets — .asset 파일 영구 저장.\n" +
            $"✓ 씬 TMP_Text {texts.Length}개 강제 재렌더.\n\n" +
            "Play로 한글 표시 확인.",
            "확인");
    }
}
