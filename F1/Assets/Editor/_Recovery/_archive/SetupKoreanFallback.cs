using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

/// <summary>
/// 한글 폰트 fallback 자동 설정 (세션 16, 도현 "한글 깨짐" 피드백).
///
/// 동작
/// 1. Windows에 NotoSansKR-VF.ttf (Open Font License) 또는 malgun.ttf 존재 확인
/// 2. C:/Windows/Fonts/{name}.ttf → F1/Assets/UI/Fonts/Korean/ 복사 (라이선스 안전한 NotoSansKR 우선)
/// 3. Dynamic SDF Font Asset 생성 (atlas 4096 — 한글 글리프 다수 수용)
/// 4. KenneyFuture SDF + KenneyFutureNarrow SDF의 fallbackFontAssetTable에 추가
/// 5. 결과: 영어=Kenney Future 유지, 한글=Noto/Malgun fallback 자동
///
/// 라이선스 노트
/// - NotoSansKR-VF.ttf = Open Font License (배포 OK)
/// - malgun.ttf = Microsoft 권한 (배포 X, 개인 빌드만)
/// 빌드 배포 시 NotoSansKR 우선 사용. 없으면 malgun으로 폴백 (Windows 사용자만 동작).
///
/// 메뉴: PitCrew/UI/Setup Korean Fallback Font
/// </summary>
public static class SetupKoreanFallback
{
    const string KoreanFontDir = "Assets/UI/Fonts/Korean";
    const string KoreanFontAssetPath = "Assets/UI/Fonts/Korean/KoreanFallback SDF.asset";
    const string KenneyFuturePath = "Assets/UI/Fonts/KenneyFuture SDF.asset";
    const string KenneyFutureNarrowPath = "Assets/UI/Fonts/KenneyFutureNarrow SDF.asset";

    [MenuItem("PitCrew/UI/Setup Korean Fallback Font")]
    public static void Execute()
    {
        Debug.Log("[KoreanFallback] === 한글 fallback 설정 시작 ===");

        // 1. Windows 한글 폰트 찾기 — malgun.ttf 우선 (Variable Font 호환성 문제 회피, 도현 #2-3).
        string windowsFontsDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts);
        string srcPath = null;
        string srcName = null;
        if (File.Exists(Path.Combine(windowsFontsDir, "malgun.ttf")))
        {
            srcPath = Path.Combine(windowsFontsDir, "malgun.ttf");
            srcName = "malgun.ttf";
            Debug.Log("[KoreanFallback] (1/4) malgun.ttf 사용 — 정적 TTF, TMP dynamic atlas 안정. 배포 전 OFL 폰트 교체 필요.");
        }
        else if (File.Exists(Path.Combine(windowsFontsDir, "NotoSansKR-VF.ttf")))
        {
            srcPath = Path.Combine(windowsFontsDir, "NotoSansKR-VF.ttf");
            srcName = "NotoSansKR-VF.ttf";
            Debug.LogWarning("[KoreanFallback] (1/4) NotoSansKR-VF (Variable Font) 사용 — TMP 호환 불안정 가능.");
        }
        else
        {
            Debug.LogError("[KoreanFallback] (1/4) 한글 TTF 없음.");
            return;
        }

        // 1.5. 기존 KoreanFallback SDF 강제 삭제 (도현 #2-3 — 재생성으로 stale atlas 제거).
        var existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
        if (existingAsset != null)
        {
            AssetDatabase.DeleteAsset(KoreanFontAssetPath);
            Debug.Log("[KoreanFallback] (1.5/4) 기존 KoreanFallback SDF 삭제 — 재생성 강제.");
        }

        // 2. 프로젝트로 복사 (TMP는 Library에 있는 OS 폰트 직접 못 읽음 — Assets로 import 필요)
        Directory.CreateDirectory(KoreanFontDir);
        string destPath = Path.Combine(KoreanFontDir, srcName).Replace('\\', '/');
        if (!File.Exists(destPath))
        {
            File.Copy(srcPath, destPath);
            AssetDatabase.ImportAsset(destPath);
            Debug.Log($"[KoreanFallback] (2/4) {srcName} → {destPath} 복사 + import.");
        }
        else
        {
            Debug.Log($"[KoreanFallback] (2/4) {destPath} 이미 존재 — 재사용.");
        }
        AssetDatabase.Refresh();

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(destPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[KoreanFallback] (2/4) Font import 실패: {destPath}");
            return;
        }

        // 3. Dynamic SDF Font Asset 생성 (atlas 4096 — 한글 다수 글리프)
        TMP_FontAsset koreanAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
        if (koreanAsset == null)
        {
            koreanAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,                                // sampling point size
                9,                                 // atlas padding
                GlyphRenderMode.SDFAA,             // SDF anti-aliased
                4096, 4096,                        // atlas width/height (한글 풍부)
                AtlasPopulationMode.Dynamic,       // 사용된 글리프만 atlas에 추가
                true                               // enableMultiAtlasSupport (atlas overflow 시 자동 추가)
            );
            koreanAsset.name = "KoreanFallback SDF";
            AssetDatabase.CreateAsset(koreanAsset, KoreanFontAssetPath);

            // 도현 #2-6 핵심 fix: atlas texture + material을 sub-asset으로 저장.
            // CreateFontAsset 결과의 m_AtlasTextures / m_Material은 메모리 객체. 명시적 AddObjectToAsset
            // 없이 SaveAssets만 부르면 .asset 파일에 dangling 참조 → Console "MissingReferenceException".
            if (koreanAsset.atlasTextures != null)
            {
                for (int i = 0; i < koreanAsset.atlasTextures.Length; i++)
                {
                    var tex = koreanAsset.atlasTextures[i];
                    if (tex != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                    {
                        tex.name = $"AtlasTexture_{i}";
                        AssetDatabase.AddObjectToAsset(tex, koreanAsset);
                    }
                }
            }
            if (koreanAsset.material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(koreanAsset.material)))
            {
                koreanAsset.material.name = "Material";
                AssetDatabase.AddObjectToAsset(koreanAsset.material, koreanAsset);
            }
            EditorUtility.SetDirty(koreanAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(KoreanFontAssetPath);
            Debug.Log("[KoreanFallback] (3a) atlas texture + material을 sub-asset으로 추가 (MissingReferenceException 처치).");
            Debug.Log("[KoreanFallback] (3/4) Dynamic SDF Font Asset 생성 — atlas 4096, multi-atlas on.");
        }
        else
        {
            Debug.Log("[KoreanFallback] (3/4) KoreanFallback SDF 이미 존재.");
        }

        // 4. Kenney 폰트 2종에 fallback 추가
        AddFallback(KenneyFuturePath, koreanAsset);
        AddFallback(KenneyFutureNarrowPath, koreanAsset);
        Debug.Log("[KoreanFallback] (4/4) Kenney Future / Narrow SDF에 fallback 등록 완료.");

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Korean Fallback",
            $"✓ {srcName} fallback 등록 완료.\n" +
            "✓ Dynamic SDF: 사용된 글리프만 atlas에 추가 (메모리 절약).\n\n" +
            (srcName.StartsWith("malgun")
                ? "⚠ malgun.ttf 사용 중 — 빌드 배포 전 NotoSansKR로 교체 필요."
                : "✓ OFL 폰트 — 배포 안전."),
            "확인");
    }

    static void AddFallback(string fontAssetPath, TMP_FontAsset fallback)
    {
        var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (primary == null)
        {
            Debug.LogWarning($"[KoreanFallback] {fontAssetPath} 없음 — 건너뜀.");
            return;
        }
        if (primary.fallbackFontAssetTable == null)
            primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
        if (!primary.fallbackFontAssetTable.Contains(fallback))
        {
            primary.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(primary);
        }
    }
}
