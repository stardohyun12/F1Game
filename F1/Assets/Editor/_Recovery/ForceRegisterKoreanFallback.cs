using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// 한글 fallback 강제 등록 + UITheme.fontPrimary 직접 교체 (세션 16 D-054 후속, B1+B5).
///
/// B1 (fallback 등록):
///   SetupKoreanFallback이 메모리 객체엔 추가하지만 SaveAssets 누락 가능성 → 강제.
///
/// B5 (도현 #2-4 "아직도 한글 깨짐"):
///   fallback chain이 작동 안 하면 우회. UITheme.fontPrimary를 KoreanFallback SDF로 직접 교체.
///   Korean SDF (Malgun)는 Latin 글리프 포함이라 영어/한글 동시 표시 OK.
///   Kenney Future 스타일은 잃지만 가독성 우선.
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

            // (도현 #2-5 진단) null 항목 누적 정리 — 이전 Apply 실행 시 KoreanFallback 삭제→재생성으로
            // 기존 참조가 missing(null)으로 남아 fallback chain 끊김. 첫 항목이 null이면 이후 fallback 검색 안 함.
            int beforeSize = arr.arraySize;
            for (int i = arr.arraySize - 1; i >= 0; i--)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == null)
                {
                    arr.DeleteArrayElementAtIndex(i);
                }
            }
            int cleaned = beforeSize - arr.arraySize;
            if (cleaned > 0) Debug.Log($"[ForceFallback] {primary.name}: null fallback {cleaned}개 정리.");

            // 이미 (정리 후) 등록됐는지 확인
            bool already = false;
            for (int i = 0; i < arr.arraySize; i++)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == korean) { already = true; break; }
            }
            if (!already)
            {
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = korean;
                registered++;
                Debug.Log($"[ForceFallback] {primary.name}: Korean fallback 추가 (총 {arr.arraySize}개).");
            }
            else
            {
                Debug.Log($"[ForceFallback] {primary.name}: Korean fallback 이미 정상 등록됨 (총 {arr.arraySize}개).");
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(primary);
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

        // B5: UITheme.fontPrimary를 KoreanFallback SDF로 직접 교체 (도현 #2-4).
        // fallback chain 우회 — 어느 HUD든 무조건 한글 렌더.
        const string UIThemePath = "Assets/Resources/UITheme.asset";
        var uiTheme = AssetDatabase.LoadAssetAtPath<UITheme>(UIThemePath);
        if (uiTheme != null)
        {
            var soTheme = new SerializedObject(uiTheme);
            var fontProp = soTheme.FindProperty("fontPrimary");
            if (fontProp != null)
            {
                fontProp.objectReferenceValue = korean;
                soTheme.ApplyModifiedProperties();
                EditorUtility.SetDirty(uiTheme);
                Debug.Log($"[ForceFallback] (B5) UITheme.fontPrimary → KoreanFallback SDF 직접 교체.");
            }
        }
        else
        {
            Debug.LogWarning($"[ForceFallback] (B5) {UIThemePath} 없음 — fontPrimary 교체 건너뜀.");
        }
        AssetDatabase.SaveAssets();

        // TMP_Text 전부 강제 새로고침 (이미 렌더된 한글 □ 갱신)
        var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (var t in texts)
        {
            if (t == null) continue;
            // primary font 자체를 교체 — 즉시 반영 보장
            if (uiTheme != null && uiTheme.fontPrimary != null)
                t.font = uiTheme.fontPrimary;
            t.ForceMeshUpdate(true);
        }
        Debug.Log($"[ForceFallback] (3/3) TMP_Text {texts.Length}개 font 교체 + ForceMeshUpdate.");

        // Korean SDF 정상성 검증 (atlas texture sub-asset 여부)
        bool atlasOk = korean.atlasTextures != null
            && korean.atlasTextures.Length > 0
            && korean.atlasTextures[0] != null
            && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(korean.atlasTextures[0]));
        Debug.Log($"[ForceFallback] (검증) KoreanFallback atlas texture sub-asset: {(atlasOk ? "정상" : "깨짐 — Apply 재실행 필요")}");

        // 다이얼로그 — 명확화 (이미 등록 vs 신규 등록 구분)
        string statusLine = registered > 0
            ? $"✓ {registered}개 폰트에 Korean fallback 신규 등록."
            : "✓ Korean fallback 이미 정상 등록됨 (변경 X).";
        string atlasLine = atlasOk
            ? "✓ Korean atlas texture sub-asset 정상."
            : "⚠ Korean atlas texture 깨짐 — Apply 메뉴 재실행 필요.";

        EditorUtility.DisplayDialog("Force Korean Fallback",
            $"{statusLine}\n" +
            $"{atlasLine}\n" +
            "✓ UITheme.fontPrimary → KoreanFallback SDF 직접 교체.\n" +
            $"✓ 씬 TMP_Text {texts.Length}개 font 교체 + 재렌더.\n\n" +
            "다음 절차:\n" +
            "1. Console 빨간 에러 모두 Clear\n" +
            "2. ▶ Play\n" +
            "3. HUD/정산 한글 표시 확인",
            "확인");
    }
}
