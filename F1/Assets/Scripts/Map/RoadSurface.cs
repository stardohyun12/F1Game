using UnityEngine;

/// <summary>
/// 길 메시에 붙는 노면 태그(BuildBayLoop 이 굽는다). roughness 0~1 = 화물 흔들림 가중치로 쓸 값(평면도 json, 가설).
/// 지금은 읽는 쪽이 없다 — 화물 흔들림 배선은 다음 과제.
/// </summary>
public class RoadSurface : MonoBehaviour
{
    public string roadId;
    public string kind;          // main / village / shortcut / alley / stairs
    [Range(0f, 1f)] public float roughness;
    public float width;
    [SerializeField] public Vector3[] centerline = new Vector3[0];   // 1m 간격, 자동 주행·검사용
    public bool closed;
    public float[] widths = new float[0];   // v4: centerline 점마다 차로 폭(widthProfile). 비면 width 하나
    // kind 에 walk_curb / walk_gutter / walk_white_line / walk_none(인도 띠), plaza / quay / lot(열린 땅)도 온다
}
