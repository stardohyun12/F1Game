#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// D-248 디버그 창의 "장소 옮기기" 표 — 좌표는 전부 여기 한 곳. Map_TownV6 기준(월드 좌표). 높이는 이동할 때 레이로 땅에 맞추니
/// y 는 어림이어도 된다(레이는 y+1.5 에서 아래로). 지도가 바뀌어 어긋나면 이 표만 고친다.
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
    }

    public struct Place
    {
        public string name; public Vector3 feet; public float yaw; public CarRule car; public string note;
        public Place(string name, Vector3 feet, float yaw, CarRule car, string note) { this.name = name; this.feet = feet; this.yaw = yaw; this.car = car; this.note = note; }
    }

    public static readonly Place[] All =
    {
        new Place("집 방",            new Vector3(187.1f, 28.5f, -62.7f), 41f,  CarRule.CartSpot, "2층 방 가운데, 문 쪽을 봄"),
        new Place("집 앞",            new Vector3(184.1f, 25.4f, -66.5f), 10f,  CarRule.Near,     "1층 현관(DOOR_01 184.5,-63.9) 밖 2.6 m, 집을 봄"),
        new Place("정비소(사장 앞)",   new Vector3(231.1f, 26.7f, -56.4f), 53f,  CarRule.CartSpot, "사장(232.7,-55.2) 1.9 m 앞 — E 말하기가 떠야 함"),
        new Place("카트 옆",          Vector3.zero,                       0f,   CarRule.BesideCar, "지금 카트가 있는 곳, 내릴 때 서는 자리 — E 타기가 떠야 함"),
        new Place("첫 배달 찻집",      new Vector3(93.8f, 4.0f, -282.0f),  180f, CarRule.Near,     "DP_tea (93.8,-288.0) 표식 6 m 북쪽 도로에서 표식을 봄"),
        new Place("노을 낚시 자리",    new Vector3(-9.0f, 1.3f, -289.9f),  180f, CarRule.Near,     "손가락 부두 남쪽 끝 자리 뒤 1.1 m — E 앉기가 떠야 함"),
        new Place("항구",             new Vector3(272.2f, 1.3f, -234.7f), 180f, CarRule.Near,     "동쪽 부두 갑판(Pier 272,-235), 바다를 봄"),
        new Place("언덕 꺼짐(sink_1031)", new Vector3(110.0f, 18.7f, 30.0f), 140f, CarRule.Near,   "Cam_FB_sink 가 보는 점(120,19,18)을 내려다보는 자리"),
        new Place("큰길",             new Vector3(175.2f, 18.0f, -75.2f), 90f,  CarRule.Near,     "main 도로 중심선 점(175,-75)"),
        // D-248 ③ 환경음 확인용 — 지붕·벽 판정이 갈리는 자리
        new Place("정비소 안",         new Vector3(216.0f, 26.7f, -55.5f), 90f,  CarRule.CartSpot, "차고 가운데(벽까지 5~8 m) — 바람·파도가 먹먹해야 함"),
        new Place("언덕 꼭대기",       new Vector3(244.0f, 64.8f, 118.0f), 200f, CarRule.Near,     "마을 둘레 지형 최고점(y 64.8, 레이 격자 4 m) — 바람이 가장 큼"),
    };
}
#endif
