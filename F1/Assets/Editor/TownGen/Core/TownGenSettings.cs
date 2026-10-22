using System;

namespace PitCrew.TownGen
{
    /// <summary>
    /// 마을 생성기 뒷단의 튜닝값 전부. 스펙 docs/superpowers/specs/2026-10-19-town-generator-experiment.md §3·§4·§12.
    /// "가설" 표시 값은 코드 수정 없이 TownGenConfig 에셋(인스펙터)에서 바꾼다.
    /// 실험 도중 바꾸면 두 앞단(OSM·roadgen)을 모두 다시 돌린다(§3).
    /// 엔진 참조 없음 — dotnet test 로 엔진 없이 검증된다.
    /// </summary>
    [Serializable]
    public class TownGenSettings
    {
        // ---- 땅·칸 ----
        /// <summary>땅 동서 길이 m. 가설 480 (도현 결정 대기 §12-2).</summary>
        public float boundsXMeters = 480f;
        /// <summary>땅 남북 길이 m. 가설 400 (도현 결정 대기 §12-2).</summary>
        public float boundsZMeters = 400f;
        /// <summary>칸 크기 m. 가설 8 (튜닝 7~9).</summary>
        public float cellSizeMeters = 8f;

        // ---- 차·차로 (뒷단 계산엔 안 쓰고 결과에 실어 E3 설치·카메라가 쓴다) ----
        /// <summary>실험 대역 차 전장 m. 가설 5.0 (도현 결정 대기 §12-1).</summary>
        public float carLengthMeters = 5.0f;
        /// <summary>차 폭 m. D-103 확정 1.90.</summary>
        public float carWidthMeters = 1.90f;
        /// <summary>휠베이스 m. D-103 확정 3.40.</summary>
        public float wheelbaseMeters = 3.40f;
        /// <summary>차로 폭 m. 가설 3.0.</summary>
        public float laneWidthMeters = 3.0f;

        // ---- 경사 (비율, 0.12 = 12%) ----
        /// <summary>도로 칸 사이 경사 상한. 가설 12% (D-100 ①).</summary>
        public float maxSlope = 0.12f;
        /// <summary>D-100 ② "언덕" 판정: 이 경사를 넘는 연결이 있으면 언덕 구간. 가설 5%.</summary>
        public float hillSlope = 0.05f;
        /// <summary>D-100 ② 평지 우회로의 모든 연결 경사 상한. 가설 2%.</summary>
        public float flatSlope = 0.02f;
        /// <summary>D-100 ② 우회로 길이 ≤ 언덕 구간 길이 × 이 값. 가설 2.</summary>
        public float detourMaxRatio = 2f;
        /// <summary>D-100 ② 가파른 연결 사이 평지 틈이 이 칸 수 이하면 한 언덕으로 묶는다(혹 꼭대기). 가설 2.</summary>
        public int hillMergeGapCells = 2;

        // ---- 정리·잇기 ----
        /// <summary>이보다 짧은(칸) 막다른 가지는 지운다. 2.</summary>
        public int minDeadEndCells = 2;
        /// <summary>footway 승격 최소 길이(칸). 2.</summary>
        public int footwayMinCells = 2;
        /// <summary>A* 경사 비용 계수 k: 비용 = 거리 × (1 + k·경사²). 가설 20.</summary>
        public float aStarSlopeK = 20f;
        /// <summary>잇기·정리 반복 상한(무한 루프 방지).</summary>
        public int maxRepairPasses = 4;

        // ---- 고도 ----
        /// <summary>고도 배율 — 연속 고도(DEM m)에 곱한다. 1.0 = 실제 높이. 두 앞단 공통. 낮출지는 디렉터 결정(§12-3).</summary>
        public float heightScale = 1.0f;
        /// <summary>해수면 m (배율 적용 뒤 고도 기준). 남쪽 변에서 이어진, 칸 최저 높이 ≤ 이 값인 칸 = 바다 → 도로 금지(D-110). 씬 바다 평면 높이와 같다.</summary>
        public float seaLevelMeters = 0.15f;

        // ---- 고도 단계 (heightMask 3단 — 구역 표시·랜드마크 판정용, 경사 판정엔 연속 고도) ----
        /// <summary>항구 단계 상한 m. 이하 = 단계 0. 가설 12.</summary>
        public float harborMaxHeightMeters = 12f;
        /// <summary>중턱/위 마을 경계 = 항구 상한 + (도로 최고 높이 − 항구 상한) × 이 값. 가설 0.5.</summary>
        public float upperTierFraction = 0.5f;

        // ---- 출발점 ----
        /// <summary>출발점은 남쪽 변(바다)에서 이 칸 수 이상 떨어진 큰길 칸. 2.</summary>
        public int startMinRowFromSea = 2;
        /// <summary>출발점 직접 지정(칸). -1 이면 자동. 지정 칸이 도로가 아니면 자동으로 돌아가고 경고.</summary>
        public int startCol = -1;
        public int startRow = -1;

        // ---- 배달지 (§4.4) ----
        /// <summary>배달지 사이 최소 간격(체비셰프 칸). 3.</summary>
        public int deliveryMinSpacingCells = 3;
        /// <summary>가까움 구간 하한 m. 가설 80.</summary>
        public float nearMinMeters = 80f;
        /// <summary>가까움/중간 경계 m. 가설 200.</summary>
        public float nearMaxMeters = 200f;
        /// <summary>중간/멂 경계 m. 가설 400.</summary>
        public float midMaxMeters = 400f;
        public int nearCount = 3;
        public int midCount = 4;
        public int farCount = 4;
        /// <summary>멂 고정 라벨 (보상 ₩1800).</summary>
        public string farFixedLabel = "Depot";
        /// <summary>중간 고정 라벨 (보상 ₩900).</summary>
        public string midFixedLabel = "FactoryA";
        /// <summary>가까움 고정 라벨 (보상 ₩500).</summary>
        public string nearFixedLabel = "ContainerYard";
        /// <summary>배달지 라벨 11개 — 지금 씬 DeliveryPoint 와 같은 이름(주문 데이터 재사용).</summary>
        public string[] deliveryLabels =
        {
            "CityHall", "Plaza", "Depot", "PierEnd", "CargoDock", "Warehouse",
            "FactoryA", "Station", "ContainerYard", "FishMarket", "Mall",
        };

        public int Columns => Math.Max(1, (int)Math.Ceiling(boundsXMeters / cellSizeMeters - 1e-6));
        public int Rows => Math.Max(1, (int)Math.Ceiling(boundsZMeters / cellSizeMeters - 1e-6));

        public TownGenSettings Clone() => (TownGenSettings)MemberwiseClone();
    }
}
