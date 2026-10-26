using System.IO;
using UnityEditor;
using UnityEngine;

// 과제 1548 (D-145): 그림체 3안 전환. 셰이더 전역 _PCLook 만 바꾼다 — 머티리얼·텍스처 원본은 손대지 않으므로 0 Current 가 곧 되돌리기.
// 고른 안은 EditorPrefs 에 남겨 도메인 리로드(Play 진입)에도 이어진다. 빌드는 전역 기본값 0 = 지금 판.
// 과제 1943 (D-146): 4 Retro-Clean = 머티리얼 _MainTex 를 같은 폴더의 <이름>_retro.png 로 바꾼다(tools/asset-gen/retro_clean.py 가 만든 사본).
//  원본 텍스처 파일은 그대로 — 다른 안을 고르면 _retro 를 뗀 원래 경로로 되돌린다. 머티리얼 에셋은 바뀐 참조로 저장된다.
public static class PitCrewLookMenu
{
    const string Key = "PitCrew.Look";
    const string Root = "PitCrew/Look/";
    static readonly string[] Names = { "0 Current", "1 Clean", "2 Painterly", "3 Pixel-Soft", "4 Retro-Clean" };

    [MenuItem(Root + "0 Current", false, 0)] static void L0() => Set(0);
    [MenuItem(Root + "1 Clean", false, 1)] static void L1() => Set(1);
    [MenuItem(Root + "2 Painterly", false, 2)] static void L2() => Set(2);
    [MenuItem(Root + "3 Pixel-Soft", false, 3)] static void L3() => Set(3);
    [MenuItem(Root + "4 Retro-Clean", false, 4)] static void L4() => Set(4);

    public static int Current => EditorPrefs.GetInt(Key, 0);

    public static void Set(int mode)
    {
        mode = Mathf.Clamp(mode, 0, Names.Length - 1);
        EditorPrefs.SetInt(Key, mode);
        int n = SwapRetro(mode == 4);
        Apply();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Debug.Log("[PitCrew Look] " + Names[mode] + " (텍스처 교체 " + n + ")");
    }

    /// <summary>on: _MainTex 를 *_retro.png 로, off: 원래 경로로. 바꾼 머티리얼 수.</summary>
    public static int SwapRetro(bool on)
    {
        int n = 0;
        foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art", "Assets/Scenes" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m == null || !m.HasProperty("_MainTex")) continue;
            var t = m.GetTexture("_MainTex"); if (t == null) continue;
            string p = AssetDatabase.GetAssetPath(t); if (!p.EndsWith(".png")) continue;
            bool isRetro = p.EndsWith("_retro.png");
            string want = on ? (isRetro ? p : p.Substring(0, p.Length - 4) + "_retro.png") : (isRetro ? p.Substring(0, p.Length - 10) + ".png" : p);
            if (want == p || !File.Exists(want)) continue;
            if (on) MatchImport(p, want);
            var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(want); if (nt == null) continue;
            m.SetTexture("_MainTex", nt); EditorUtility.SetDirty(m); n++;
        }
        AssetDatabase.SaveAssets();
        return n;
    }

    // 사본도 원본과 같은 가져오기 설정(Point·밉맵 없음·압축 없음 등) — 안 그러면 Bilinear·밉맵으로 큰 텍셀이 뭉개진다
    static void MatchImport(string src, string dst)
    {
        var a = AssetImporter.GetAtPath(src) as TextureImporter; var b = AssetImporter.GetAtPath(dst) as TextureImporter;
        if (a == null || b == null) return;
        if (b.filterMode == a.filterMode && b.mipmapEnabled == a.mipmapEnabled && b.textureCompression == a.textureCompression && b.maxTextureSize == a.maxTextureSize) return;
        b.textureType = a.textureType; b.filterMode = a.filterMode; b.mipmapEnabled = a.mipmapEnabled; b.textureCompression = a.textureCompression;
        b.maxTextureSize = a.maxTextureSize; b.sRGBTexture = a.sRGBTexture; b.wrapMode = a.wrapMode; b.alphaIsTransparency = a.alphaIsTransparency; b.npotScale = a.npotScale;
        b.SaveAndReimport();
    }

    [InitializeOnLoadMethod]
    static void Apply()
    {
        int m = Current;
        Shader.SetGlobalFloat("_PCLook", m);
        for (int i = 0; i < Names.Length; i++) Menu.SetChecked(Root + Names[i], i == m);
    }
}
