using UnityEngine;

/// <summary>
/// 과제 1145: Scene_A3_Town 의 토글 6개(과제 1124 R1~R3·M1~M3)를 인스펙터 한 곳에서 켜고 끈다. 에디트·Play 둘 다 바로 반영.
///  · R1 계단 명암 / R2 텍스처 질감 / R3 외곽선 = 재질 값 _Ramp / _R2 / _Outline (재질 에셋에 저장된다 — A3 캡처 스크립트와 같은 방식)
///  · M1 기복 / M2 가장자리 / M3 밀도 = 오브젝트 그룹 A3_M1 / A3_M2 / A3_M3 (M3 를 끄면 원래 길·담 A3_M3off 가 켜진다)
/// </summary>
[ExecuteAlways]
public sealed class SceneA3Toggles : MonoBehaviour
{
    [Header("렌더링 (재질 값)")]
    public bool R1_stepShade = true;
    public bool R2_texture = true;
    public bool R3_outline = true;
    [Header("메쉬 (오브젝트 그룹)")]
    public bool M1_relief = true;
    public bool M2_edges = true;
    public bool M3_density = true;
    [Header("부드러운 색감 D-171 (과제 1232 — A3 는 전부 끔 = 그대로)")]
    public bool S1_highlightRolloff = false;
    public bool S2_oneLight = false;
    public bool S3_softBorders = false;
    public bool T1_sunEdge = false;
    public bool T2_shadeAmbient = false;
    [Tooltip("SA3_* 재질 — 빌드 스크립트가 채운다")]
    public Material[] materials;

    int applied = -1;

    int Bits => (R1_stepShade ? 1 : 0) | (R2_texture ? 2 : 0) | (R3_outline ? 4 : 0) | (M1_relief ? 8 : 0) | (M2_edges ? 16 : 0) | (M3_density ? 32 : 0)
        | (S1_highlightRolloff ? 64 : 0) | (S2_oneLight ? 128 : 0) | (S3_softBorders ? 256 : 0) | (T1_sunEdge ? 512 : 0) | (T2_shadeAmbient ? 1024 : 0);

    void OnEnable() => applied = -1;

    void Update()
    {
        if (Bits != applied) Apply();
    }

    public void Apply()
    {
        applied = Bits;
        if (materials != null)
            foreach (var m in materials)
            {
                if (m == null || !m.HasProperty("_Ramp")) continue;
                m.SetFloat("_Ramp", R1_stepShade ? 1 : 0); m.SetFloat("_R2", R2_texture ? 1 : 0); m.SetFloat("_Outline", R3_outline ? 1 : 0);
                if (!m.HasProperty("_S1")) continue;
                m.SetFloat("_S1", S1_highlightRolloff ? 1 : 0); m.SetFloat("_S2", S2_oneLight ? 1 : 0); m.SetFloat("_S3", S3_softBorders ? 1 : 0);
                m.SetFloat("_T1", T1_sunEdge ? 1 : 0); m.SetFloat("_T2", T2_shadeAmbient ? 1 : 0);
            }
        Set("A3_M1", M1_relief); Set("A3_M2", M2_edges); Set("A3_M3", M3_density); Set("A3_M3off", !M3_density);
        Set("A3_M2/edge5", !M3_density); Set("A3_M2/edge4", M3_density);
    }

    void Set(string path, bool on)
    {
        var t = transform.Find(path);
        if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
    }
}
