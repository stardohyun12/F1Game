using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 채널 3 (맵 해금) — 지역 단위. 잡 풀을 묶고 해금 자금 임계값을 가짐.
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Zone", fileName = "Zone_New")]
public class Zone : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("내부 식별자 (영문 권장). 저장/비교에 사용.")]
    public string id = "downtown";
    public string displayName = "다운타운";

    [Header("Unlock")]
    [Tooltip("해금 비용 (자금). 0이면 시작부터 해금.")]
    public int unlockCost = 0;

    [Tooltip("OrderCard zone 배지/카드 strip 색. 카드 시각 그루핑용.")]
    public Color tintColor = new Color(0.6f, 0.7f, 1f);

    [Header("Jobs")]
    [Tooltip("이 지역에서 뜨는 주문 풀.")]
    public List<OrderJob> jobs = new List<OrderJob>();
}
