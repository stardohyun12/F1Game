#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// D-248 디버그 창의 "장소 옮기기" 표 — 좌표는 전부 여기 한 곳. Map_TownV6 기준(월드 좌표). 높이는 이동할 때 레이로 땅에 맞추니
/// y 는 어림이어도 된다(레이는 y+1.5 에서 아래로). 지도가 바뀌어 어긋나면 이 표만 고친다.
/// (D-289: 아래 출처 줄은 표를 yaml 로 옮기기 전 기록이다. 표 자체는 world/world.yaml 의 places.)
/// 출처: 집 방 = OnFootSetup(OnFoot_Spawn_Room) · 정비소 = FirstOrderSetup(접근점 229.4,-57.6 에서 사장 쪽) · 찻집 = 씬 DP_tea ·
/// 낚시 = PitCrewSunsetMenu.SeatPos 뒤 1.1 m · 꺼짐 = docs/qa/tmp/sink1935/camsave.cs 의 Cam_FB_sink 가 보는 점(120,18) · 큰길/항구/집 앞 = 씬에서 레이로 잰 점.
/// </summary>
public static class DebugPlaces
{
    /// <summary>카트를 어디에 둘지.</summary>
    public enum CarRule
    {
        /// <summary>처음 시작 때 자리(차고 마당 OnFoot_CartSpot).</summary>
        CartSpot,
        /// <summary>서 있는 자리 옆 빈 곳을 찾아 놓는다. 못 찾으면 그대로.</summary>
        Near,
        /// <summary>카트 옆에 선다(카트는 그대로).</summary>
        BesideCar,
        /// <summary>정해 둔 자리(carAt, carYaw)에 놓는다 — 둘레 찾기가 건물 안을 고를 수 있는 곳(택배 보관소).</summary>
        Fixed,
    }

    public struct Place
    {
        public string name; public Vector3 feet; public float yaw; public CarRule car; public string note; public Vector3 carAt; public float carYaw;
        public Place(string name, Vector3 feet, float yaw, CarRule car, string note) { this.name = name; this.feet = feet; this.yaw = yaw; this.car = car; this.note = note; carAt = Vector3.zero; carYaw = 0f; }
        public Place(string name, Vector3 feet, float yaw, Vector3 carAt, float carYaw, string note) : this(name, feet, yaw, CarRule.Fixed, note) { this.carAt = carAt; this.carYaw = carYaw; }
    }

    /// <summary>D-250 택배 보관소 앞 카트 자리 — 셔터 앞 마을 길(v2_ridge_up_c_1, 273.1,-37.1) 위, 동쪽을 봄.</summary>
    public static readonly Vector3 DepotCart = OnFootDirector.DepotCartSpot;

    static Place[] all;

    /// <summary>D-289: 장소 표는 world/world.yaml 이 원천 → WorldPlaces.asset(gen.py). 여기는 읽어서 Place 로 바꿀 뿐이다. 에셋이 없으면 빈 표.</summary>
    public static Place[] All => all ??= Build();

    /// <summary>에셋을 다시 만든 뒤(gen.py) 부른다 — 캐시를 버리고 다시 읽는다.</summary>
    public static void Reload() { all = null; }

    static Place[] Build()
    {
        var src = WorldData.Places;
        if (src == null) { Debug.LogWarning("[DebugPlaces] WorldPlaces 에셋 없음 — python tools/world/gen.py"); return new Place[0]; }
        var list = new Place[src.spots.Count];
        for (int i = 0; i < list.Length; i++)
        {
            var s = src.spots[i];
            var rule = (CarRule)System.Enum.Parse(typeof(CarRule), s.car);
            list[i] = rule == CarRule.Fixed ? new Place(s.name, s.feet, s.yaw, s.carAt, s.carYaw, s.role) : new Place(s.name, s.feet, s.yaw, rule, s.role);
        }
        return list;
    }
}
#endif
