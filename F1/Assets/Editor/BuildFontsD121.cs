using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// D-121 — OFL 한글 폰트(Pretendard 본문 + Jua 제목체)로 TMP 폰트 에셋을 굽고 UITheme·TMP Settings 를 새 폰트로 바꾼다.
///
/// 만드는 것 (Assets/UI/Fonts/…):
///   Pretendard/Pretendard-Regular SDF.asset   정적(Static) — KS X 1001 한글 2,350자 + ASCII + 기호
///   Pretendard/Pretendard-Bold SDF.asset      정적 — 같은 문자 집합. Regular 의 weight table 700(Bold) 에 연결
///   Pretendard/Pretendard-Regular Dynamic SDF.asset  동적 — 위 집합 밖 글자(드문 한글·기호) 폴백
///   Jua/Jua-Regular SDF.asset                 정적 — 제목체. 폴백 = Pretendard Regular → Dynamic
/// 정적 아틀라스라 Play 때 에셋이 바뀌지 않는다(옛 KoreanFallback 동적 아틀라스의 커밋 오염 해소).
/// 동적 폴백만 집합 밖 글자를 만났을 때 바뀐다.
///
/// 바꾸는 것:
///   Resources/UITheme.asset   fontPrimary = Pretendard Regular, fontCondensed = Pretendard Bold, fontDisplay = Jua
///   TMP Settings              기본 폰트 = Pretendard Regular, 폴백 = [Pretendard Dynamic], 한글 어절 줄바꿈 ON
///   KenneyFuture / KenneyFutureNarrow SDF   폴백 표의 KoreanFallback(맑은 고딕) → Pretendard Regular
/// 맑은 고딕 에셋(Korean/malgun.ttf · Malgun SDF · KoreanFallback SDF)은 지우지 않는다 —
///   "PitCrew/UI/Report Malgun References (D-121)" 로 남은 참조를 확인한 뒤 사람이 지운다.
/// </summary>
public static class BuildFontsD121
{
    const string Root = "Assets/UI/Fonts";
    const string PretendardRegularTtf = Root + "/Pretendard/Pretendard-Regular.ttf";
    const string PretendardBoldTtf = Root + "/Pretendard/Pretendard-Bold.ttf";
    const string JuaTtf = Root + "/Jua/Jua-Regular.ttf";

    const string PretendardRegularSdf = Root + "/Pretendard/Pretendard-Regular SDF.asset";
    const string PretendardBoldSdf = Root + "/Pretendard/Pretendard-Bold SDF.asset";
    const string PretendardDynamicSdf = Root + "/Pretendard/Pretendard-Regular Dynamic SDF.asset";
    const string JuaSdf = Root + "/Jua/Jua-Regular SDF.asset";

    const string ThemePath = "Assets/Resources/UITheme.asset";
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    static readonly string[] KenneyFontPaths =
    {
        Root + "/KenneyFuture SDF.asset",
        Root + "/KenneyFutureNarrow SDF.asset",
    };
    static readonly string[] MalgunPaths =
    {
        Root + "/Korean/KoreanFallback SDF.asset",
        Root + "/Korean/Malgun SDF.asset",
        Root + "/Korean/malgun.ttf",
        Root + "/Korean/NotoSansKR-VF.ttf",
    };

    // 정적 아틀라스 설정 — 2,350 + 약 150자. 2048² 여러 장(multi-atlas).
    const int SamplingPointSize = 56;
    const int AtlasPadding = 6;
    const int AtlasSize = 2048;

    /// <summary>ASCII + 코드/데이터에서 실제 쓰는 기호 + 자주 쓰는 문장부호 + 호환 자모(ㄱ~ㅎ, ㅏ~ㅣ).</summary>
    static string ExtraCharacters()
    {
        var sb = new StringBuilder();
        for (char c = (char)0x20; c <= (char)0x7E; c++) sb.Append(c);
        sb.Append("₩¥°±²·×÷π—–−≈≥≤…‘’“”「」『』《》〈〉※★☆○●◎◇◆□■△▲▽▼▶◀←↑→↓↔①②③④⑤─▬✓✔♥%");
        for (char c = (char)0x3131; c <= (char)0x3163; c++) sb.Append(c);
        return sb.ToString();
    }

    static string Charset() => KsX1001Charset.Hangul2350 + ExtraCharacters();

    [MenuItem("PitCrew/UI/Build Fonts (D-121)")]
    public static void Build() => Run(rebuild: false);

    [MenuItem("PitCrew/UI/Rebuild Fonts (D-121, overwrite SDF)")]
    public static void Rebuild() => Run(rebuild: true);

    static void Run(bool rebuild)
    {
        var regTtf = LoadFont(PretendardRegularTtf);
        var boldTtf = LoadFont(PretendardBoldTtf);
        var juaTtf = LoadFont(JuaTtf);
        if (regTtf == null || boldTtf == null || juaTtf == null)
        {
            Debug.LogError("[D-121 Fonts] TTF 를 못 읽음 — Assets/UI/Fonts/Pretendard·Jua 확인(임포트 'Include Font Data' 켜짐 필요).");
            return;
        }

        string charset = Charset();
        var report = new StringBuilder("[D-121 Fonts]\n");

        var dynamicFa = CreateOrLoad(PretendardDynamicSdf, regTtf, null, rebuild, report);
        var regular = CreateOrLoad(PretendardRegularSdf, regTtf, charset, rebuild, report);
        var bold = CreateOrLoad(PretendardBoldSdf, boldTtf, charset, rebuild, report);
        var jua = CreateOrLoad(JuaSdf, juaTtf, charset, rebuild, report);
        if (regular == null || bold == null || jua == null || dynamicFa == null)
        {
            Debug.LogError(report.ToString());
            return;
        }

        // 폴백 · 굵기 연결
        SetFallbacks(regular, dynamicFa);
        SetFallbacks(bold, regular, dynamicFa);
        SetFallbacks(jua, regular, dynamicFa);
        var weights = regular.fontWeightTable;
        if (weights != null && weights.Length > 7)
        {
            weights[7].regularTypeface = bold;     // 700 = Bold → FontStyles.Bold 가 가짜 굵게 대신 Pretendard Bold 사용 (배열 제자리 수정 — setter 는 internal)
        }
        EditorUtility.SetDirty(regular);
        EditorUtility.SetDirty(bold);
        EditorUtility.SetDirty(jua);
        EditorUtility.SetDirty(dynamicFa);

        // UITheme
        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        if (theme != null)
        {
            Undo.RecordObject(theme, "D-121 UITheme fonts");
            theme.fontPrimary = regular;
            theme.fontCondensed = bold;
            theme.fontDisplay = jua;
            EditorUtility.SetDirty(theme);
            UITheme.InvalidateCache();
            report.AppendLine($"UITheme: fontPrimary={regular.name}, fontCondensed={bold.name}, fontDisplay={jua.name}");
        }
        else report.AppendLine($"!! UITheme 없음: {ThemePath}");

        // TMP Settings — 기본 폰트·폴백·한글 어절 줄바꿈
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings != null)
        {
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = regular;
            var fb = so.FindProperty("m_fallbackFontAssets");
            fb.ClearArray();
            fb.InsertArrayElementAtIndex(0);
            fb.GetArrayElementAtIndex(0).objectReferenceValue = dynamicFa;
            var hangul = so.FindProperty("m_UseModernHangulLineBreakingRules");
            if (hangul != null) hangul.boolValue = true;
            else report.AppendLine("!! m_UseModernHangulLineBreakingRules 프로퍼티 없음 — TMP 버전 확인");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            report.AppendLine("TMP Settings: 기본 폰트=Pretendard Regular, 폴백=[Pretendard Dynamic], Modern Hangul Line Breaking=ON");
        }
        else report.AppendLine($"!! TMP Settings 없음: {TmpSettingsPath}");

        // KenneyFuture 계열 — 한글 폴백을 맑은 고딕 → Pretendard
        foreach (var path in KenneyFontPaths)
        {
            var k = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (k == null) continue;
            var list = k.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
            list.RemoveAll(f => f == null || IsMalgun(f));
            if (!list.Contains(regular)) list.Insert(0, regular);
            k.fallbackFontAssetTable = list;
            EditorUtility.SetDirty(k);
            report.AppendLine($"{Path.GetFileName(path)}: 폴백 → Pretendard Regular");
        }

        AssetDatabase.SaveAssets();
        TMPro_EventManager.ON_TMP_SETTINGS_CHANGED();
        report.AppendLine("다음: 'PitCrew/UI/Report Malgun References (D-121)' 로 남은 맑은 고딕 참조 확인.");
        Debug.Log(report.ToString());
    }

    static Font LoadFont(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
        if (importer != null && !importer.includeFontData)
        {
            importer.includeFontData = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Font>(path);
    }

    /// <summary>charset == null → 동적 에셋. 아니면 그 문자를 구운 뒤 Static 으로 고정.</summary>
    static TMP_FontAsset CreateOrLoad(string path, Font font, string charset, bool rebuild, StringBuilder report)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null && !rebuild)
        {
            report.AppendLine($"유지: {path} (덮어쓰려면 Rebuild 메뉴)");
            return existing;
        }
        if (existing != null) AssetDatabase.DeleteAsset(path);     // GUID 바뀜 → 아래에서 UITheme/TMP Settings 다시 연결

        var fa = TMP_FontAsset.CreateFontAsset(font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
            AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
        if (fa == null)
        {
            report.AppendLine($"!! CreateFontAsset 실패: {font.name}");
            return null;
        }
        string assetName = Path.GetFileNameWithoutExtension(path);
        fa.name = assetName;
        AssetDatabase.CreateAsset(fa, path);

        // 첫 아틀라스·머티리얼을 하위 에셋으로(추가 아틀라스는 TMP 가 OnFontAssetTextureChanged 로 붙인다)
        fa.atlasTextures[0].name = assetName + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        var sdfShader = Shader.Find("TextMeshPro/Distance Field");
        if (sdfShader != null) fa.material.shader = sdfShader;
        fa.material.name = assetName + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);

        if (charset != null)
        {
            fa.TryAddCharacters(charset, out string missing);
            if (!string.IsNullOrEmpty(missing))
                report.AppendLine($"{assetName}: 폰트에 없는 글자 {missing.Length}개 → 폴백 사용: {missing}");

            // 혹시 하위 에셋으로 안 붙은 추가 아틀라스 보정
            foreach (var tex in fa.atlasTextures)
            {
                if (tex == null || AssetDatabase.Contains(tex)) continue;
                if (string.IsNullOrEmpty(tex.name)) tex.name = assetName + " Atlas+";
                AssetDatabase.AddObjectToAsset(tex, fa);
            }
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            report.AppendLine($"생성(정적): {path} — 글자 {fa.characterTable.Count}개, 아틀라스 {CountAtlases(fa)}장");
        }
        else
        {
            // clearDynamicDataOnBuild 는 이 TMP 버전에서 internal — 인스펙터 기본값에 맡긴다.
            report.AppendLine($"생성(동적 폴백): {path}");
        }
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    static int CountAtlases(TMP_FontAsset fa)
    {
        int n = 0;
        foreach (var t in fa.atlasTextures) if (t != null && t.width > 1) n++;
        return n;
    }

    static void SetFallbacks(TMP_FontAsset fa, params TMP_FontAsset[] fallbacks)
    {
        var list = new List<TMP_FontAsset>();
        foreach (var f in fallbacks) if (f != null && f != fa && !list.Contains(f)) list.Add(f);
        fa.fallbackFontAssetTable = list;
    }

    static bool IsMalgun(TMP_FontAsset f)
    {
        string p = AssetDatabase.GetAssetPath(f);
        foreach (var m in MalgunPaths) if (p == m) return true;
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// 맑은 고딕 계열(KoreanFallback SDF·Malgun SDF·malgun.ttf·NotoSansKR-VF)을 참조하는 에셋을 찾아 로그로.
    /// 지우지 않는다. 참조가 0 이 되면 Korean/ 폴더를 통째로 지워도 된다(빌드 차단 해제).
    /// </summary>
    [MenuItem("PitCrew/UI/Report Malgun References (D-121)")]
    public static void ReportMalgunReferences()
    {
        var guids = new Dictionary<string, string>();
        foreach (var p in MalgunPaths)
        {
            string g = AssetDatabase.AssetPathToGUID(p);
            if (!string.IsNullOrEmpty(g)) guids[g] = p;
        }
        var sb = new StringBuilder("[D-121 Malgun refs]\n");
        int hits = 0;
        string[] exts = { ".unity", ".prefab", ".asset", ".mat", ".controller" };
        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/")) continue;
            if (path.StartsWith(Root + "/Korean/")) continue;     // 맑은 고딕 자기들끼리
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (System.Array.IndexOf(exts, ext) < 0) continue;
            string text;
            try { text = File.ReadAllText(path); } catch { continue; }
            foreach (var kv in guids)
            {
                if (!text.Contains(kv.Key)) continue;
                sb.AppendLine($"  {path}  →  {Path.GetFileName(kv.Value)}");
                hits++;
            }
        }
        // 코드에 박힌 경로 문자열
        foreach (var cs in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
        {
            string t = File.ReadAllText(cs);
            if (cs.Replace('\\', '/').EndsWith("BuildFontsD121.cs")) continue;
            if (t.Contains("KoreanFallback SDF") || t.Contains("Malgun SDF") || t.Contains("malgun.ttf"))
            {
                sb.AppendLine($"  (code) {cs.Replace('\\', '/')}");
                hits++;
            }
        }
        sb.AppendLine(hits == 0
            ? "참조 0 — Assets/UI/Fonts/Korean/ 삭제 가능."
            : $"참조 {hits}곳 — 프리팹은 'PitCrew/UI/Rewire Prefab Fonts to Pretendard (D-121)', 코드는 경로 문자열 교체 후 다시 실행.");
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 프리팹(Assets/Prefabs/**) 안 TMP 텍스트의 맑은 고딕 폰트를 Pretendard Regular 로 바꾼다. 씬은 건드리지 않는다
    /// (씬 텍스트는 대부분 런타임 생성 + UITheme 폰트). 맑은 고딕 SDF 의 머티리얼 프리셋도 기본 머티리얼로 되돌린다.
    /// </summary>
    [MenuItem("PitCrew/UI/Rewire Prefab Fonts to Pretendard (D-121)")]
    public static void RewirePrefabFonts()
    {
        var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PretendardRegularSdf);
        if (regular == null) { Debug.LogError("[D-121] Pretendard Regular SDF 없음 — 먼저 Build Fonts."); return; }
        int changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            bool dirty = false;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.font == null || !IsMalgun(t.font)) continue;
                t.font = regular;
                t.fontSharedMaterial = regular.material;
                dirty = true; changed++;
            }
            if (dirty) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[D-121] 프리팹 TMP 폰트 교체 {changed}개 → Pretendard Regular");
    }
}
