using UnityEngine;

/// <summary>
/// 한 런의 작업 정의. ScriptableObject.
/// GDD: GDD/core-loop.md (결정 1: 주문 선택), D-030 K4 연계.
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Order Job", fileName = "Order_New")]
public class OrderJob : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "주문";
    [Tooltip("UI 카드/맵 마커 색상.")]
    public Color color = Color.green;

    [Header("Items")]
    [Tooltip("이 잡에서 포장할 물건 수 (1~3)")]
    [Range(1, 3)] public int itemCount = 1;

    [Header("Difficulty")]
    [Tooltip("배달 목적지 수. 현 디자인: 1 (D-070 결정).")]
    public int pointCount = 1;

    [Tooltip("시간제한(초). 0이면 무제한.")]
    public float timeLimitSec = 0f;

    [Header("Reward")]
    public int baseReward = 500;
    [Tooltip("시간 내 완료 시 추가 보상. timeLimit=0이면 무시.")]
    public int timeBonus = 0;

    [Header("Display Only (MVP)")]
    [Tooltip("0=가벼움, 1=보통, 2=무거움. MVP는 표시만.")]
    [Range(0, 2)] public int weightTier = 0;

    public string WeightLabel => weightTier switch
    {
        0 => "가벼움",
        1 => "보통",
        _ => "무거움"
    };

    [Header("Cargo (Day 11)")]
    [Tooltip("이 주문에 실리는 화물 타입. RunController.BeginRun에서 CargoBox에 자동 적용.")]
    public CargoType cargoType = CargoType.Box;

}
