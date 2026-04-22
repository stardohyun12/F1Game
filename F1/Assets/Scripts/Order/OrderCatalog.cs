using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주문 풀. 시작 시 3개 후보 제공.
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Order Catalog", fileName = "OrderCatalog")]
public class OrderCatalog : ScriptableObject
{
    [Tooltip("시작 보드에 표시할 후보. 순서대로 슬롯 1/2/3.")]
    public List<OrderJob> jobs = new List<OrderJob>();
}
