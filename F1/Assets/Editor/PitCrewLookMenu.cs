using UnityEditor;
using UnityEngine;

// 과제 1548 (D-145): 그림체 3안 전환. 셰이더 전역 _PCLook 만 바꾼다 — 머티리얼·텍스처 원본은 손대지 않으므로 0 Current 가 곧 되돌리기.
// 고른 안은 EditorPrefs 에 남겨 도메인 리로드(Play 진입)에도 이어진다. 빌드는 전역 기본값 0 = 지금 판.
public static class PitCrewLookMenu
{
    const string Key = "PitCrew.Look";
    const string Root = "PitCrew/Look/";
    static readonly string[] Names = { "0 Current", "1 Clean", "2 Painterly", "3 Pixel-Soft" };

    [MenuItem(Root + "0 Current", false, 0)] static void L0() => Set(0);
    [MenuItem(Root + "1 Clean", false, 1)] static void L1() => Set(1);
    [MenuItem(Root + "2 Painterly", false, 2)] static void L2() => Set(2);
    [MenuItem(Root + "3 Pixel-Soft", false, 3)] static void L3() => Set(3);

    public static int Current => EditorPrefs.GetInt(Key, 0);

    public static void Set(int mode)
    {
        mode = Mathf.Clamp(mode, 0, 3);
        EditorPrefs.SetInt(Key, mode);
        Apply();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Debug.Log("[PitCrew Look] " + Names[mode]);
    }

    [InitializeOnLoadMethod]
    static void Apply()
    {
        int m = Current;
        Shader.SetGlobalFloat("_PCLook", m);
        for (int i = 0; i < Names.Length; i++) Menu.SetChecked(Root + Names[i], i == m);
    }
}
