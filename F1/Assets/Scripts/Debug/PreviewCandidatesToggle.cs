using UnityEngine;

/// <summary>
/// 과제 0535 미리보기 씬(Preview_Candidates) 전용: 후보 에셋 있음/없음 한 번에 바꾸기.
/// show 켬 = candidates 켜고 replaced(기존 정비소·방을 품은 집 원본) 끔. 끔 = 반대(= Scene_A7_Light 그대로).
/// </summary>
[ExecuteAlways]
public sealed class PreviewCandidatesToggle : MonoBehaviour
{
    [Tooltip("후보 보이기 (끄면 원래 A7 장면)")] public bool show = true;
    public GameObject[] candidates;
    public GameObject[] replaced;

    void OnValidate() => Apply();
    void OnEnable() => Apply();

    public void Apply()
    {
        if (candidates != null) foreach (var g in candidates) if (g && g.activeSelf != show) g.SetActive(show);
        if (replaced != null) foreach (var g in replaced) if (g && g.activeSelf == show) g.SetActive(!show);
    }
}
