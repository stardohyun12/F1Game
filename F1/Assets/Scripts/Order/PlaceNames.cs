using System.Collections.Generic;

/// <summary>
/// 배달지 표시명(한글). DeliveryPoint.Label / OrderJob.destinationLabels 는 데이터 키라 그대로 두고,
/// 화면에 보일 때만 이 함수로 바꾼다. 모르는 키는 원문 그대로(새 맵 라벨이 깨지지 않게).
/// </summary>
public static class PlaceNames
{
    static readonly Dictionary<string, string> Map = new Dictionary<string, string>
    {
        // ComplexTrack 배달지 11곳
        { "Mall", "쇼핑몰" },
        { "TeaHouse", "언덕 찻집" }, // D-142 튜토리얼 STOP_1
        { "CityHall", "시청" },
        { "Plaza", "광장" },
        { "Station", "기차역" },
        { "Warehouse", "창고" },
        { "Depot", "물류 센터" },
        { "FishMarket", "수산 시장" },
        { "PierEnd", "부두 끝" },
        { "CargoDock", "화물 부두" },
        { "FactoryA", "A 공장" },
        { "ContainerYard", "컨테이너 야적장" },
        // 프로토 해안 코스 (Proto_CoastCourse)
        { "AlleyEnd", "골목 끝 집" },
        { "CoastCafe", "해안 카페" },
        { "HairpinOrchard", "헤어핀 과수원" },
        { "HilltopBellTower", "언덕 종탑" },
        { "LoneHouse", "들판 외딴집" },
        // 다른 트랙/레이아웃 라벨
        { "CentralCross", "중앙 교차로" },
        { "DowntownEntry", "시내 입구" },
        { "EastHub", "동쪽 거점" },
        { "WestHub", "서쪽 거점" },
        { "FactoryNorth", "북쪽 공장" },
        { "LowerWest", "서쪽 아랫마을" },
        { "PlateauTop", "고원 꼭대기" },
        { "SuburbanGate", "교외 입구" },
        { "Delivery", "배달지" },
    };

    /// <summary>라벨 키 → 한글 표시명. null/빈 값은 "배달지".</summary>
    public static string Display(string label)
    {
        if (string.IsNullOrEmpty(label)) return "배달지";
        return Map.TryGetValue(label, out var ko) ? ko : label;
    }

    /// <summary>여러 라벨을 표시명으로 이어 붙임(기본 " → ").</summary>
    public static string Join(IList<string> labels, string separator = " → ")
    {
        if (labels == null || labels.Count == 0) return "-";
        var parts = new string[labels.Count];
        for (int i = 0; i < labels.Count; i++) parts[i] = Display(labels[i]);
        return string.Join(separator, parts);
    }
}
