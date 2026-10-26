using UnityEngine;

/// <summary>
/// 장면 A(Scene_A_Garage) 4판 전환 (과제 20261027-0429). 같은 씬 안에 V0~V3 뿌리 4개를 두고 하나만 켠다.
/// 인스펙터에서 variant / cut 을 바꾸거나 컴포넌트 우클릭 메뉴(V0~V3, 게임 컷↔소개 컷)로 고른다(Play 중에도 된다).
/// </summary>
[ExecuteAlways]
public class SceneAVariant : MonoBehaviour
{
    public static readonly string[] Names = { "V0 v5 그대로", "V1 포토배싱+EDC 조명", "V2 팔레트+계단 명암", "V3 V1+면 변화" };

    [Range(0, 3)] public int variant;
    [Tooltip("0 = 게임 컷(카트 뒤), 1 = 소개 컷")]
    [Range(0, 1)] public int cut;
    public GameObject[] roots = new GameObject[4];
    public Camera[] cams = new Camera[2];
    public Color[] sky = new Color[4];
    [Tooltip("V0 만 켜는 해(실시간 그림자). V1~V3 는 셰이더가 직접 칠한다.")]
    public Light sun;

    void OnValidate() => Apply();
    void OnEnable() => Apply();

    [ContextMenu("V0")] void M0() { variant = 0; Apply(); }
    [ContextMenu("V1")] void M1() { variant = 1; Apply(); }
    [ContextMenu("V2")] void M2() { variant = 2; Apply(); }
    [ContextMenu("V3")] void M3() { variant = 3; Apply(); }
    [ContextMenu("게임 컷 ↔ 소개 컷")] void MC() { cut = 1 - cut; Apply(); }

    public void Apply()
    {
        for (int i = 0; i < roots.Length; i++) if (roots[i] != null) roots[i].SetActive(i == variant);
        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] == null) continue;
            cams[i].gameObject.SetActive(i == cut);
            if (variant < sky.Length) cams[i].backgroundColor = sky[variant];
        }
        if (sun != null) sun.enabled = variant == 0;
    }
}
