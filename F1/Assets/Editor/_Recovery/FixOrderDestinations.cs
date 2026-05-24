using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 주문 목적지 라벨 정합 — OrderJob.destinationLabels를 현재 씬 DeliveryPoint 라벨에 맞춤.
///
/// 배경(세션32 진단): 주문 카탈로그는 R1 맵 설계(`docs/.../map-expansion-r1-design.md`)의
/// 배달지 라벨(FactoryGateA, MainStation, CentralPlaza, CargoShip, OfficeCornerN/S 등)으로
/// 쓰였는데, R1 맵은 씬에 적용된 적이 없다. 현재 씬은 더 단순한 라벨 세트
/// (FactoryA, Station, Plaza, CargoDock, Depot, CityHall, Mall 등)라서, 8개 주문의 목적지가
/// 어느 DeliveryPoint와도 매칭되지 않아 RoutePlanScreen이 마커를 0개 만들고 경로 설정이 불가했다.
/// (도현 manual play "경로 설정 안됨" — D-073 라벨 미스매치 재발)
///
/// 이 스크립트는 stale R1 라벨 → 현재 씬 라벨로 결정론적 remap. 멱등(이미 맞으면 skip).
/// 미래에 R1 맵을 실제 적용하면 이 매핑은 폐기.
/// 메뉴: PitCrew/Fix/Fix Order Destinations
/// </summary>
public static class FixOrderDestinations
{
    // stale(R1 설계) → 현재 씬 DeliveryPoint.Label
    static readonly Dictionary<string, string> Remap = new()
    {
        { "FactoryGateA", "FactoryA" },
        { "FactoryGateB", "Depot" },
        { "CentralPlaza", "Plaza" },
        { "MainStation", "Station" },
        { "CargoShip", "CargoDock" },
        { "OfficeCornerN", "CityHall" },
        { "OfficeCornerS", "Mall" },
        { "MotorDepot", "Depot" },
    };

    [MenuItem("PitCrew/Fix/Fix Order Destinations")]
    public static void Execute()
    {
        int changed = 0, orders = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:OrderJob"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var job = AssetDatabase.LoadAssetAtPath<OrderJob>(path);
            if (job == null || job.destinationLabels == null) continue;
            orders++;
            bool dirty = false;
            for (int i = 0; i < job.destinationLabels.Count; i++)
            {
                if (Remap.TryGetValue(job.destinationLabels[i], out var fixedLabel))
                {
                    job.destinationLabels[i] = fixedLabel;
                    dirty = true;
                }
            }
            if (dirty) { EditorUtility.SetDirty(job); changed++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[FixOrderDest] ✓ 주문 {orders}개 검사, {changed}개 목적지 라벨 정정 (stale R1 → 씬 라벨).");
    }
}
