using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PitCrew.TownGen
{
    /// <summary>
    /// 곡선 큰길 마을 튜닝값 — 스펙 docs/superpowers/specs/2026-10-20-spline-town-design.md (D-111) §2·§10.
    /// "가설" 값은 TownGenConfig 에셋에서 코드 수정 없이 바꾼다.
    /// </summary>
    [Serializable]
    public class SplineTownSettings
    {
        // 땅·바다
        public float boundsXMeters = 480f, boundsZMeters = 400f;
        /// <summary>높이 배율. 가설 0.4 (0.3~0.5).</summary>
        public float heightScale = 0.3f;   // §2.9: 0.4 → 0.3 (도현이 D-110 에서 고른 높이)
        public float seaLevelMeters = 0.15f;
        public float seaCellMeters = 8f;
        /// <summary>입력 변형: "c" = 포지타노 조각을 SS163 이 들어오게 다시 자른 판(osm_to_network.py --variant c). "" = 1차 실험 입력.</summary>
        public string inputVariant = "c";

        // 선별 (§2.2)
        public float residentialMinLengthMeters = 60f;
        public float alleyMaxLengthMeters = 40f;
        public float isolatedDropLengthMeters = 80f;
        /// <summary>
        /// 큰길에 안 닿는 긴 주거 도로(≥ residentialMinLength)도 큰길 후보로 두고 고립 조각 규칙(≥80m 잇기)으로 묶는다.
        /// false = 스펙 §2.2 글자 그대로(닿아야 큰길, A — 기각). 기획 확정 B(true). 포지타노 조각엔 primary 가 없어서 false 면 비탈 큰길 0.
        /// </summary>
        public bool isolatedResidentialAsMain = true;
        /// <summary>잇기 길 찾기 격자 간격 m.</summary>
        public float connectorGridMeters = 4f;
        /// <summary>잇기 길 비용: 구간 길이 × (1 + 이 값 × 경사 상한 초과분). 클수록 완만하게 돌아간다.</summary>
        public float connectorGradePenalty = 120f;   // 400 시도 → M7 2557→2633·집 겹침 재발, 되돌림(BUG-008 진단)
        /// <summary>잇기 길이 다른 큰길에 이만큼 가까이 못 감(교차·겹침 방지) m.</summary>
        public float connectorClearanceMeters = 7f;

        // 곡선 (§2.3)
        public float resampleStepMeters = 2f;
        public int chaikinIterations = 2;
        /// <summary>큰길 최소 곡률 반경 m. 가설 12.</summary>
        public float mainMinRadiusMeters = 12f;
        /// <summary>헤어핀(방향 변화 ≥ hairpinTurnDeg) 최소 반경 m. 가설 8.</summary>
        public float hairpinMinRadiusMeters = 8f;
        public float hairpinTurnDeg = 120f;
        /// <summary>헤어핀 판정 창(호 길이) m.</summary>
        public float hairpinWindowMeters = 40f;
        /// <summary>굽이로 보는 이웃 3점 반경 상한(m) — 스펙 §2.3 "반경 &lt; 30m 가 끊김 없이 이어지는 구간".</summary>
        public float hairpinBendRadiusMeters = 30f;
        /// <summary>반경 펴기로 원래 OSM 선에서 벗어나도 되는 최대 거리 m. 가설 6.</summary>
        public float maxDeviationMeters = 6f;
        /// <summary>헤어핀 원호의 OSM 선 이탈 상한 m. §2.9: 10. R=8m 에서 이보다 벗어나는 급한 꺾임(약 127° 초과)은 두 굽이로 나눈다.</summary>
        public float hairpinMaxDeviationMeters = 10f;
        /// <summary>반경 펴기 = 필렛(꼭짓점에 원호). false 면 옛 방식(Chaikin + 이웃 평균) — 비교용.</summary>
        public bool useFillet = true;
        /// <summary>필렛 전 꺾은선 단순화 허용 오차 m (OSM 점 / 잇기 격자 경로).</summary>
        public float filletSimplifyMeters = 0.75f, connectorSimplifyMeters = 2.0f;

        // 종단 (§2.3)
        public float profileWindowMeters = 20f;
        /// <summary>큰길 종단 경사 상한. 가설 8%.</summary>
        public float mainMaxGrade = 0.08f;
        /// <summary>헤어핀 패드 경사 상한. 3%.</summary>
        public float hairpinMaxGrade = 0.05f;   // §2.9: 3 → 5% (헤어핀·교차점 패드)
        /// <summary>잇기 길·섞기 경계 길(생성 길) 종단 경사 상한. §2.9: 10% (실제 OSM 큰길은 mainMaxGrade 8% 유지).</summary>
        public float generatedMaxGrade = 0.10f;
        /// <summary>섞기 경계 띠(z) — 이 안의 큰길 점은 생성 길 경사 상한. 입력 heights 의 blend_z 와 같게.</summary>
        public float seamZMin = 80f, seamZMax = 120f;
        /// <summary>교차점(차수 ≥3)에서 이 호 길이 안은 패드 경사(hairpinMaxGrade). §2.9 "교차점 원판도 ≤5%".</summary>
        public float junctionPadMeters = 10f;

        // 단면 (§2.4)
        public float laneWidthMeters = 3f;
        public float sidewalkWidthMeters = 1f;
        public float curbHeightMeters = 0.15f;
        public float junctionDiscFactor = 0.75f;
        /// <summary>집 진입로 폭(m)과 계단으로 바꾸는 경사(높이/길이). 가설 — 그래픽 판정용.</summary>
        public float driveWidthMeters = 2.4f;
        /// <summary>이 길이보다 짧은 교차점↔교차점 길은 접어 한 교차점으로(m). 가설.</summary>
        public float junctionMergeMeters = 15f;
        /// <summary>패드(≤5%) 길이 = max(junctionPadMeters, 끊는 거리 + 이 여유)(m).</summary>
        public float junctionPadMarginMeters = 3f;
        /// <summary>막다른·지도 경계 끝에서 이 길이 안에 반경 미달이 남으면 끝을 그 점까지 잘라낸다(m).</summary>
        public float freeEndTrimMeters = 30f;
        /// <summary>잇기 길(OSM 에 없는 생성 길)은 반경 자리가 모자랄 때 꼭짓점을 빼서 펴는 이탈 상한을 넓힌다(m). OSM 이탈 상한(6m)은 실제 길에만.</summary>
        public float connectorStraightenDeviationMeters = 20f;
        public float driveStepGrade = 0.25f;
        /// <summary>진입로 입구 디딤판 길이(m) — 보도 기울기를 받아 진입로 높이로 잇는 판. 진입로 길이의 30% 를 넘지 않음.</summary>
        public float driveLandingMeters = 0.6f;
        /// <summary>진입로 최대 길이(m) — 넘으면 안 냄(구조 스펙 f_길 40m 와 같음).</summary>
        public float driveMaxMeters = 40f;
        /// <summary>교차점 판 평면에 맞춘 단면 옆 기울기를 0 으로 푸는 길이(m).</summary>
        public float bankRunMeters = 10f;
        public float dashLengthMeters = 3f, dashGapMeters = 3f, dashWidthMeters = 0.15f;

        // 깎기/메우기 (§2.5)
        public float shoulderMeters = 2f;
        public float blendMeters = 6f;
        /// <summary>이보다 높이차가 크면 옹벽. 가설 1.5m.</summary>
        public float wallThresholdMeters = 1.5f;
        /// <summary>옹벽을 어깨 끝에서 이만큼 바깥에 세우고 그 사이는 평평(보도 높이). 지형 격자(2m)가 수직 단차를 못 그려 삼각형이 길 위로 솟는 것 방지 — 격자 1.5칸.</summary>
        public float wallSetbackMeters = 0f;   // BUG-003 수정(지형 상한 고정)으로 묻힘을 막으므로 옹벽은 다시 어깨 끝에. 3 이던 평탄 띠가 M7 깊이·긴 옹벽 띠를 키웠음(BUG-008).
        /// <summary>길과 길이 이 높이차 넘게 서로 단면 영향권(어깨 끝 합) 안에 붙으면 "충돌" — 골목은 버리고 막다른 끝은 비켜날 때까지 자른다(BUG-003).</summary>
        public float conflictHeightMeters = 1.0f;
        /// <summary>깎기·메우기 최대 깊이(넘으면 경고). 가설 3m.</summary>
        public float maxCutFillMeters = 6f;   // §2.9: 3 → 6m (1.5m 넘는 쪽 석축). 넘으면 ① 6m 초과 점 ≤ 길 표본 1% 로 판정 ② 8m ③ 기획 반송
        /// <summary>지형 메시 간격 m.</summary>
        public float terrainStepMeters = 2f;
        /// <summary>어깨 띠 바깥 끝을 길 표본(2m) 하나당 이만큼 잘게 나눠 지형 면에 붙인다 — 지형 삼각형과의 현 오차(M1) 줄이기.</summary>
        public int shoulderSubdivisions = 4;   // 8(0.25m) 시도 → QA 경계 추출과 어긋나 M1 위반 584→23,564 로 폭증, 되돌림(BUG-004 진단)

        // 골목 (§2.7)
        public float alleyHalfWidthMeters = 1.5f;
        public float alleyShoulderMeters = 1f;
        public float alleyBlendMeters = 3f;
        /// <summary>골목 종단 경사 상한. 12%.</summary>
        public float alleyMaxGrade = 0.12f;
        /// <summary>골목 최소 반경 m. 6.</summary>
        public float alleyMinRadiusMeters = 6f;

        // 집 줄짓기 (§2.6)
        public float plotFrontageMinMeters = 8f, plotFrontageMaxMeters = 12f;
        public float plotGapMaxMeters = 2f;
        public float plotDepthMeters = 10f;
        /// <summary>집 앉히기(§11.1, 가설): 앞줄 큰길 집 바닥 = 보도 + 이 값(0.15~0.30) · 앉히는 법 경계(땅에 바로 / 벽 내림 / 반지하) · 상한(넘으면 비움).</summary>
        /// <summary>광장(§11.3, 가설): 크기 30×24 · 경사 ≤3% · 종탑 8×8×20 · 상점 6~10 · 자리 조건(경사 5%·바다 30%·해안 80m, 완화 7%·20%).</summary>
        public float plazaLength = 24f, plazaWidth = 18f, plazaMaxSiteGrade = 0.06f, plazaTerraceStepMeters = 2f, plazaMaxGrade = 0.03f, towerSize = 8f, towerHeight = 20f;
        public int shopsMin = 6, shopsMax = 10;
        /// <summary>큰길 옆(갈래 1) 광장 후보도 볼지 — §11.3 개정(2026-10-20 기획)으로 기본 켬.</summary>
        public bool plazaRoadsideFallback = true;
        /// <summary>광장(§11.3)을 만들지 — 구조 스펙 조각 2(SS163 만) 는 끔.</summary>
        public bool plazaEnabled = true;
        /// <summary>구조 스펙(D-113) 중심 영역: 광장 후보는 (centerSearchX, Z) 반경 centerSearchRadius 안에서만, 집은 고른 광장 중심 반경 houseRegionRadius 안에만(0 = 제한 없음).</summary>
        public float centerSearchX, centerSearchZ, centerSearchRadius = 0f, houseRegionRadius = 0f;
        /// <summary>구조 스펙 §3 광장 땅 조건: 교차점 반경 이 거리 안을 6m 간격·6방향으로 훑는다(0 = 끔). 켜면 판정은 바닥 자리 경사·바다 칸만(바다 보임·해안 거리는 순위에만).</summary>
        public float plazaJunctionSearchRadius = 0f;
        /// <summary>구조 스펙 §4.2 ⓐ: (alleyRelaxX, Z) 반경 alleyRelaxRadius 안에서만 골목 규칙 완화 — 주거·서비스·보행로 길이 ≥ alleyRelaxMinMeters 면 골목(40m 상한 없음),
        /// 큰길에 안 닿는 조각(≥ alleyRelaxAttachMinMeters)은 alleyRelaxAttachMaxMeters 안 가장 가까운 큰길 점에 직선으로 잇는다. 0 = 끔.</summary>
        public float alleyRelaxX, alleyRelaxZ, alleyRelaxRadius = 0f, alleyRelaxMinMeters = 10f, alleyRelaxAttachMinMeters = 20f, alleyRelaxAttachMaxMeters = 30f;
        /// <summary>둘째 완화 원(구조 스펙 §4 ④ 작은 마을). 0 = 끔.</summary>
        public float alleyRelax2X, alleyRelax2Z, alleyRelax2Radius = 0f;
        /// <summary>구조 스펙 §4.3: (noPromoteX, Z) 반경 noPromoteRadius 안(run 중점 기준) 주거 도로는 큰길로 올리지 않는다(→ 골목 후보). noPromoteKeepWayIds 는 예외(입구 길). 0 = 끔.</summary>
        public float noPromoteX, noPromoteZ, noPromoteRadius = 0f;
        [NonSerialized] public long[] noPromoteKeepWayIds = new long[0];
        /// <summary>구조 스펙 §4.3: 완화 원 안 골목이 큰길에 안 닿아도 이 점(공용 계단 끝)에서 alleyAnchorMaxMeters 안이면 골목으로 둔다(입구 = 계단). 받아들인 골목 끝도 다음 골목의 닻이 된다.</summary>
        [NonSerialized] public Vec2[] alleyAnchors = new Vec2[0];
        /// <summary>15a-2: 큰길 U자(60m 안 150°+, 다리 반폭 &lt; 큰길 반경)를 헤어핀 패드 원호로 바꾼다.</summary>
        [NonSerialized] public bool hairpinPads = false;
        /// <summary>15a-2: 패드 원호가 지나야 할 끝점(그 U자 안 15m 안에 있으면 그 점이 원호 끝). 상자 경계 잇기 중간점.</summary>
        [NonSerialized] public Vec2[] padTips = new Vec2[0];
        public float alleyAnchorMaxMeters = 8f;
        /// <summary>구조 조각 12(D-115): 첫 완화 원 안 골목(= 좁은 차로)은 다른 골목에도 잇는다 — 큰길에 안 닿는 OSM 토막도 후보로 두고, 받아들인 골목을 입구(host)로 삼아 여러 번 돈다. 큰길만 가로지름 판정. false = 예전 규칙.</summary>
        public bool alleyChain = false;
        /// <summary>구조 조각 13: 깎기/메우기 직전에 부른다 — 골목 가운데 선을 옮기고 점마다 반폭(SplineEdge.halfW)을 정한다. null = 안 함.</summary>
        [NonSerialized] public Action<SplineTownResult> alleyAdjust;
        public bool ChainAt(Vec2 p) => alleyChain && alleyRelaxRadius > 0 && SplineEdge.Dist(p, new Vec2(alleyRelaxX, alleyRelaxZ)) <= alleyRelaxRadius;
        public bool AlleyRelaxOn => alleyRelaxRadius > 0 || alleyRelax2Radius > 0;
        public bool InAlleyRelax(Vec2 p) => (alleyRelaxRadius > 0 && SplineEdge.Dist(p, new Vec2(alleyRelaxX, alleyRelaxZ)) <= alleyRelaxRadius) || (alleyRelax2Radius > 0 && SplineEdge.Dist(p, new Vec2(alleyRelax2X, alleyRelax2Z)) <= alleyRelax2Radius);
        public float frontFloorAboveSidewalk = 0.2f, seatGroundMeters = 0.3f, seatWallMeters = 1.2f, seatTerraceMaxMeters = 3.0f;
        /// <summary>집 몸체가 차지하는 깊이(필지 앞쪽) m — 건물 모델 깊이 ≤ 7m.</summary>
        public float houseDepthMeters = 7f;
        public float setbackMeters = 0.5f;
        /// <summary>큰길 집 앞 진입 공간 m — 어깨 끝(6m)에서 이만큼 더 물린다(BUG-007·그래픽 결함 2 "집 앞 공간"). 골목은 0.3.</summary>
        public float houseFrontGapMeters = 1.0f;
        /// <summary>기단 최대 높이 m. 넘으면 한 단 뒤로 물림. 가설 2.5.</summary>
        public float plinthMaxMeters = 2.5f;
        /// <summary>커브 안쪽 반경이 이보다 작으면 집 안 둠 m. 15.</summary>
        public float noHouseInnerRadiusMeters = 15f;
        /// <summary>집 수 상한. 가설 300.</summary>
        public int houseCap = 300;
        /// <summary>건물 모델 폭·깊이(m). 비어 있으면 정면 폭을 8~12m 에서 고른다. 채우면 모델을 먼저 고르고 필지 폭 = 모델 폭 + 틈(0~2m) — 붙은 줄집(§2.6).
        /// 씬 빌더가 기존 Codex 건물 프리팹에서 재서 채운다(설정 에셋엔 저장 안 함).</summary>
        [NonSerialized] public float[] houseModelWidths = new float[0];
        [NonSerialized] public float[] houseModelDepths = new float[0];
        /// <summary>모델 바닥 범위의 가운데(모델 로컬 x, z) — 기준점이 몸체 가운데가 아닌 모델이 있다(CornerShopB: z −2.9~+4.1). BUG-007.</summary>
        [NonSerialized] public string[] houseModelNames = new string[0];
        [NonSerialized] public float[] houseModelCenterX = new float[0];
        [NonSerialized] public float[] houseModelCenterZ = new float[0];

        public float HalfCarriageway => laneWidthMeters;                       // 2차로 6m 의 반
        public float HalfSection => laneWidthMeters + sidewalkWidthMeters;     // 보도 바깥 끝
        public float HalfShoulder => HalfSection + shoulderMeters;            // 어깨 바깥 끝 = 길 메시 바깥 끝
        public SplineTownSettings Clone() => (SplineTownSettings)MemberwiseClone();
    }

    /// <summary>OSM way 하나(땅 로컬 m). highway = 원래 OSM 태그.</summary>
    public class OsmWay
    {
        public string highway = "";
        public long wayId;
        public List<Vec2> points = new List<Vec2>();
    }

    public class SplineEdge
    {
        public int a, b;                 // 노드 번호
        public List<Vec2> pts = new List<Vec2>();
        public List<double> y = new List<double>();
        public List<bool> hairpin = new List<bool>();
        /// <summary>점마다 패드(교차점 근처) 여부 — 경사 상한 hairpinMaxGrade.</summary>
        public List<bool> pad = new List<bool>();
        public string highway = "";
        public bool connector;
        /// <summary>골목(§2.7) — 폭 3m·연석 없음·경사 12%·반경 6m.</summary>
        public bool alley;
        /// <summary>구조 조각 13: 골목(좁은 차로) 점마다 반폭(길 폭 = 건물 사이 틈). null = 설정의 alleyHalfWidthMeters.</summary>
        public List<double> halfW;
        public double AlleyHalf(int i, double def) => halfW != null && halfW.Count > 0 ? halfW[Math.Max(0, Math.Min(halfW.Count - 1, i))] : def;
        public double AlleyHalfMax(double def) => halfW != null && halfW.Count > 0 ? halfW.Max() : def;
        /// <summary>점마다 왼쪽/오른쪽에 옹벽이 섰나(집 배치가 옹벽 너머로 물림).</summary>
        public List<bool> wallL = new List<bool>(), wallR = new List<bool>();
        /// <summary>교차점 쪽 끝에서 보도·연석·어깨·차로·중앙선을 끊는 거리(중심선 따라, m). 그 안은 교차점 판(원판+모서리)이 덮는다. 0 = 끊지 않음.</summary>
        public double clipA, clipB;
        /// <summary>끊는 점에서 단면 옆 기울기(왼쪽으로 1m 갈 때 높이 변화). 판이 평면인 교차점 쪽만 0 이 아님 — 단면 끝을 판 평면에 맞추고 bankRunMeters 에 걸쳐 0 으로 푼다.</summary>
        public double bankA, bankB;
        public double Length => PolyLength(pts);
        public static double PolyLength(List<Vec2> p) { double l = 0; for (int i = 1; i < p.Count; i++) l += Dist(p[i - 1], p[i]); return l; }
        public static double Dist(Vec2 a, Vec2 b) => Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
    }

    /// <summary>지형 격자(행 0 = 남쪽) + 메시와 똑같은 삼각형 면 높이. 대각선 (0,0)→(1,1).</summary>
    public class TerrainGrid
    {
        public int nx, nz; public double step; public double[] h;
        public TerrainGrid(int nx, int nz, double step) { this.nx = nx; this.nz = nz; this.step = step; h = new double[nx * nz]; }
        public double Surface(double x, double z)
        {
            double fx = Math.Min(Math.Max(x / step, 0), nx - 1.000001), fz = Math.Min(Math.Max(z / step, 0), nz - 1.000001);
            int i = (int)fx, j = (int)fz; double tx = fx - i, tz = fz - j;
            double h00 = h[j * nx + i], h10 = h[j * nx + i + 1], h01 = h[(j + 1) * nx + i], h11 = h[(j + 1) * nx + i + 1];
            return tz >= tx ? h00 + tx * (h11 - h01) + tz * (h01 - h00) : h00 + tx * (h10 - h00) + tz * (h11 - h10);
        }
    }

    public class WallSegment { public Vec2 a, b; public double topA, topB, botA, botB; }

    /// <summary>집 필지 하나 — 집 몸체 사각형(가운데·방향·폭·깊이) + 기단.</summary>
    public class HousePlot
    {
        public Vec2 center; public double yawDeg;      // yaw = 집 정면이 길을 보는 방향(Unity Y 회전, +z=0°, +x=90°)
        public double width, depth;                    // 몸체(폭 = 정면 폭 − 여유)
        public double plinthY, terrainMin, terrainMax; // 기단 윗면 = 집 바닥
        public bool alley; public int row; public int hash;
        /// <summary>houseModelWidths 의 번호(-1 = 모델 정보 없음).</summary>
        public int model = -1;
        /// <summary>모델 기준점(배치 위치). center 는 몸체 사각형 가운데(겹침 판정용) — 기준점이 몸체 가운데가 아닌 모델 보정.</summary>
        public Vec2 pivot;
        /// <summary>붙은 길(edges 번호)·그 길의 점 번호·쪽(+1 왼쪽). 진입로 계산용.</summary>
        public int edge = -1, idx, side;
        /// <summary>진입로(길 가장자리 → 현관 앞): 시작 = 보도 바깥 끝(골목은 골목 가장자리), 끝 = 몸체 정면 가운데. 폭 driveW. 경사가 급하면 계단.</summary>
        public bool hasDrive, driveSteps; public Vec2 driveA, driveB; public double driveAY, driveBY, driveW;
        /// <summary>입구 디딤판 양 끝(왼쪽 = 길→집 방향 기준 왼쪽) 높이 = 그 자리 보도 높이, 디딤판 길이. 보도가 길 경사로 기울어 평평한 입구와 어긋나던 것(BUG-010).</summary>
        public double driveAYL, driveAYR, driveLanding;
        /// <summary>앉히는 법(§11.1): 0 땅에 바로(바닥 밑 지형 고름) · 1 벽을 땅까지 내림 · 2 반지하 층. seatBottom = 내린 벽·반지하 밑단 높이.</summary>
        public int seat; public double seatBottom;
        /// <summary>광장 상점(§11.3) — 정면이 광장, 진입로 없음.</summary>
        public bool shop;
        /// <summary>③ 해안도로 외딴집(구조 스펙 D-113).</summary>
        public bool scenic;
        public Vec2[] Corners()
        {
            double r = yawDeg * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx;
            double hw = width / 2, hd = depth / 2;
            return new[] {
                new Vec2(center.x + rx * hw + fx * hd, center.z + rz * hw + fz * hd), new Vec2(center.x - rx * hw + fx * hd, center.z - rz * hw + fz * hd),
                new Vec2(center.x - rx * hw - fx * hd, center.z - rz * hw - fz * hd), new Vec2(center.x + rx * hw - fx * hd, center.z + rz * hw - fz * hd) };
        }
    }

    /// <summary>교차점에 붙은 길 하나의 끝: 끊는 점(중심선) · 바깥 방향 · 왼쪽(반시계) 법선 · 높이.</summary>
    public class JunctionArm
    {
        public int edge; public bool atStart; public double clip, y;
        /// <summary>c = 끊는 점, u = 교차점→끊는 점 방향(각도 정렬용), t = 끊는 점에서 바깥 방향 접선, ccw = t 기준 왼쪽 법선.</summary>
        public Vec2 c, u, t, ccw;
        /// <summary>끊는 점 → 교차점까지 중심선 위 점과 그 점의 (바깥 기준 왼쪽) 법선. 모서리 선이 휜 길을 따라 교차점까지 돌아오게(4차).</summary>
        public List<Vec2> backP = new List<Vec2>(), backN = new List<Vec2>();
    }
    /// <summary>교차점 판: 붙은 길을 바깥 방향 각도(반시계) 순으로.</summary>
    public class JunctionLayout
    {
        public int node; public double y; public List<JunctionArm> arms = new List<JunctionArm>();
        /// <summary>판(부채꼴) 둘레 점과 높이 — 코어가 한 번 정하고 씬·지형 누르기가 같은 것을 쓴다. 가운데 = 노드(높이 y).</summary>
        public List<Vec2> ringP = new List<Vec2>(); public List<double> ringY = new List<double>();
    }

    /// <summary>마을 중심 광장(§11.3): 중심·방향(긴 변 방향 yaw)·크기·평면(높이 = y + gx·dx + gz·dz)·종탑·점수.</summary>
    public class PlazaInfo
    {
        public Vec2 c; public double yawDeg, length, width, y, gx, gz;
        public Vec2 towerC; public double towerYaw, towerSize, towerHeight;
        public int node, arms; public double slope30, seeSea, coastDist, score; public bool relaxed, roadside;
        public List<string> candidates = new List<string>();
        /// <summary>테라스 광장(§11.3 마지막 단계): tierAxis 쪽(내적 &gt; 0) 반은 위 단 평면(y2, gx2, gz2).</summary>
        public bool terraced; public Vec2 tierAxis; public double y2, gx2, gz2;
        public bool Upper(Vec2 p) => terraced && (p.x - c.x) * tierAxis.x + (p.z - c.z) * tierAxis.z > 0;
        public double Y(Vec2 p) => Upper(p) ? y2 + gx2 * (p.x - c.x) + gz2 * (p.z - c.z) : y + gx * (p.x - c.x) + gz * (p.z - c.z);
        public double YTier(Vec2 p, bool upper) => upper ? y2 + gx2 * (p.x - c.x) + gz2 * (p.z - c.z) : y + gx * (p.x - c.x) + gz * (p.z - c.z);
        public Vec2[] Corners(double grow = 0)
        {
            double r = yawDeg * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx;
            double hl = length / 2 + grow, hw = width / 2 + grow;
            return new[] { new Vec2(c.x + fx * hl + rx * hw, c.z + fz * hl + rz * hw), new Vec2(c.x + fx * hl - rx * hw, c.z + fz * hl - rz * hw),
                           new Vec2(c.x - fx * hl - rx * hw, c.z - fz * hl - rz * hw), new Vec2(c.x - fx * hl + rx * hw, c.z - fz * hl + rz * hw) };
        }
    }

    public class SplineTownResult
    {
        public PlazaInfo plaza;
        /// <summary>집 놓기 영역 중심(houseRegionRadius &gt; 0 일 때) — 광장 중심, 광장 없으면 centerSearch 점.</summary>
        public Vec2? houseRegionCenter;
        public List<Vec2> nodes = new List<Vec2>();
        /// <summary>조각 15a: 생성기가 만든 잇기 길(지도에 없는 길) 원래 꺾은선 — 다듬기·이어 붙이기 전. 이어 붙이면 SplineEdge.connector 가 이웃 실제 길까지 번지므로 길이는 이걸로 잰다.</summary>
        public List<List<Vec2>> connectorRaw = new List<List<Vec2>>();
        public List<Vec2> maxDevOrig, maxDevOut;   // 15a-2 진단: OSM 이탈 최대인 길의 다듬기 전·후 선
        public List<double> nodeY = new List<double>();
        public List<SplineEdge> edges = new List<SplineEdge>();
        public TerrainGrid terrainOriginal, terrain;
        public List<WallSegment> walls = new List<WallSegment>();
        public List<int> junctions = new List<int>();     // 노드 번호, 차수 ≥ 3
        public List<HousePlot> houses = new List<HousePlot>();
        public List<JunctionLayout> junctionLayouts = new List<JunctionLayout>();
        /// <summary>판 평면이 성립한 교차점(노드 → 기울기). 없으면 그 교차점 판은 원뿔꼴.</summary>
        public Dictionary<int, (double gx, double gz)> junctionPlane = new Dictionary<int, (double gx, double gz)>();
        public IEnumerable<SplineEdge> Mains => edges.Where(e => !e.alley);
        public IEnumerable<SplineEdge> Alleys => edges.Where(e => e.alley);
        public TownGrid sea;
        public TownReport report = new TownReport();
        public int NodeDegree(int n) => edges.Count(e => e.a == n) + edges.Count(e => e.b == n);
    }

    /// <summary>
    /// 곡선 큰길 1차(스펙 §2.1~2.5, §2.8 중 길 관련): OSM 차도 선별 → 곡선 그래프 → 다듬기 → 종단 → 깎기/메우기.
    /// 엔진 참조 없음. 결정적(반복 순서 = 입력 순서·노드 번호).
    /// </summary>
    public static class SplineTown
    {
        static readonly HashSet<string> MainTags = new HashSet<string> { "primary", "secondary", "tertiary", "primary_link", "secondary_link", "tertiary_link", "trunk", "trunk_link" };
        static readonly HashSet<string> ResTags = new HashSet<string> { "residential", "unclassified", "living_street" };
        static readonly HashSet<string> AlleyTags = new HashSet<string> { "service", "pedestrian" };

        public static SplineTownResult Build(List<OsmWay> ways, IHeightField hf, SplineTownSettings s)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var res = new SplineTownResult();
            var rep = res.report; rep.source = "osm-spline";
            IHeightField H = new FuncHeightField((x, z) => hf.HeightAt(x, z) * s.heightScale);

            // 바다 (D-110 같은 규칙)
            var seaSettings = new TownGenSettings { boundsXMeters = s.boundsXMeters, boundsZMeters = s.boundsZMeters, cellSizeMeters = s.seaCellMeters, heightScale = 1f, seaLevelMeters = s.seaLevelMeters };
            res.sea = TownBackend.SeaGrid(H, seaSettings);
            bool IsSea(Vec2 p)
            {
                int c = (int)Math.Floor(p.x / s.seaCellMeters), r = (int)Math.Floor(p.z / s.seaCellMeters);
                return res.sea.InBounds(c, r) && res.sea.sea[res.sea.Index(c, r)];
            }

            // ① 차도만 + 땅 안 + 바다 아닌 구간으로 자르기
            int stepsWays = 0; var runs = new List<OsmWay>();
            double alleyLen = 0; int alleyCand = 0, seaClipped = 0, relaxedAlleys = 0;
            var alleyRaw = new List<List<Vec2>>(); var alleyAnchored = new HashSet<List<Vec2>>(); int anchoredAlleys = 0;
            foreach (var w in ways)
            {
                if (w.highway == "steps") { stepsWays++; continue; }
                if (!MainTags.Contains(w.highway) && !ResTags.Contains(w.highway) && !AlleyTags.Contains(w.highway)) continue;
                var run = new List<Vec2>();
                foreach (var p in w.points)
                {
                    bool ok = p.x >= 0 && p.z >= 0 && p.x <= s.boundsXMeters && p.z <= s.boundsZMeters && !IsSea(p);
                    if (!ok && IsSea(p)) seaClipped++;
                    if (ok) run.Add(p);
                    else { if (run.Count >= 2) runs.Add(new OsmWay { highway = w.highway, wayId = w.wayId, points = run }); run = new List<Vec2>(); }
                }
                if (run.Count >= 2) runs.Add(new OsmWay { highway = w.highway, wayId = w.wayId, points = run });
            }

            // ② 노드: 끝점 + 둘 이상 way 가 공유하는 점 (같은 OSM 노드 → 같은 좌표)
            var keyCount = new Dictionary<(long, long), int>();
            (long, long) K(Vec2 p) => ((long)Math.Round(p.x * 100), (long)Math.Round(p.z * 100));
            foreach (var r in runs) foreach (var k in r.points.Select(K).Distinct()) keyCount[k] = keyCount.TryGetValue(k, out var n) ? n + 1 : 1;
            var nodeIndex = new Dictionary<(long, long), int>();
            int Node(Vec2 p)
            {
                var k = K(p);
                if (!nodeIndex.TryGetValue(k, out var id)) { id = res.nodes.Count; nodeIndex[k] = id; res.nodes.Add(p); }
                return id;
            }
            var rawEdges = new List<(SplineEdge e, int run)>();
            for (int ri = 0; ri < runs.Count; ri++)
            {
                var r = runs[ri];
                var cur = new List<Vec2> { r.points[0] };
                int start = Node(r.points[0]);
                for (int i = 1; i < r.points.Count; i++)
                {
                    cur.Add(r.points[i]);
                    bool isNode = i == r.points.Count - 1 || keyCount[K(r.points[i])] >= 2;
                    if (!isNode) continue;
                    int end = Node(r.points[i]);
                    if (cur.Count >= 2 && SplineEdge.PolyLength(cur) > 0.01)
                        rawEdges.Add((new SplineEdge { a = start, b = end, pts = cur, highway = r.highway }, ri));
                    cur = new List<Vec2> { r.points[i] }; start = end;
                }
            }

            // ③ 선별: 주요 도로 = 큰길. 주거 도로는 run 길이 ≥ 60m 이고 run 양 끝 중 하나가 큰길 노드면 큰길(반복).
            var isMain = rawEdges.Select(x => MainTags.Contains(x.e.highway)).ToList();
            var runLen = runs.Select(r => SplineEdge.PolyLength(r.points)).ToList();
            for (int pass = 0; pass < 32; pass++)
            {
                var mainNodes = new HashSet<int>();
                for (int i = 0; i < rawEdges.Count; i++) if (isMain[i]) { mainNodes.Add(rawEdges[i].e.a); mainNodes.Add(rawEdges[i].e.b); }
                bool changed = false;
                for (int ri = 0; ri < runs.Count; ri++)
                {
                    if (!ResTags.Contains(runs[ri].highway) || runLen[ri] < s.residentialMinLengthMeters) continue;
                    if (s.noPromoteRadius > 0 && SplineEdge.Dist(runs[ri].points[runs[ri].points.Count / 2], new Vec2(s.noPromoteX, s.noPromoteZ)) <= s.noPromoteRadius
                        && Array.IndexOf(s.noPromoteKeepWayIds, runs[ri].wayId) < 0) continue;
                    var idx = Enumerable.Range(0, rawEdges.Count).Where(i => rawEdges[i].run == ri).ToList();
                    if (idx.Count == 0 || isMain[idx[0]]) continue;
                    int endA = Node(runs[ri].points[0]), endB = Node(runs[ri].points[runs[ri].points.Count - 1]);
                    if (!s.isolatedResidentialAsMain && !mainNodes.Contains(endA) && !mainNodes.Contains(endB)) continue;
                    foreach (var i in idx) isMain[i] = true;
                    changed = true;
                }
                if (!changed) break;
            }
            {
                var mainNodes = new HashSet<int>();
                for (int i = 0; i < rawEdges.Count; i++) if (isMain[i]) { mainNodes.Add(rawEdges[i].e.a); mainNodes.Add(rawEdges[i].e.b); }
                var mainPts = new List<Vec2>(); for (int i = 0; i < rawEdges.Count; i++) if (isMain[i]) mainPts.AddRange(rawEdges[i].e.pts);
                var anchorPending = new List<int>();
                for (int ri = 0; ri < runs.Count; ri++)
                {
                    bool anyMain = Enumerable.Range(0, rawEdges.Count).Any(i => rawEdges[i].run == ri && isMain[i]);
                    if (anyMain || (!ResTags.Contains(runs[ri].highway) && !AlleyTags.Contains(runs[ri].highway))) continue;
                    // §4.2 ⓐ 중심 안 완화: 길이 ≥ 10m 면 골목, 40m 상한 없음, 안 닿는 조각은 30m 안 큰길 점에 잇기
                    if (s.AlleyRelaxOn)
                    {
                        var rp = runs[ri].points; var mid = rp[rp.Count / 2];
                        if (s.InAlleyRelax(mid) && runLen[ri] >= (s.ChainAt(mid) ? 4 : s.alleyRelaxMinMeters))
                        {
                            bool t0 = nodeIndex.TryGetValue(K(rp[0]), out var m0) && mainNodes.Contains(m0), t1 = nodeIndex.TryGetValue(K(rp[rp.Count - 1]), out var m1) && mainNodes.Contains(m1);
                            var apts2 = new List<Vec2>(rp);
                            if (!t0 && t1) apts2.Reverse();
                            else if (!t0 && !t1 && s.ChainAt(mid)) { }   // 조각 12: 다른 차로 토막에 이어질 수 있다 — 골목 단계에서 입구를 찾는다
                            else if (!t0 && !t1)
                            {
                                if (runLen[ri] < s.alleyRelaxAttachMinMeters || mainPts.Count == 0) { if (s.alleyAnchors.Length > 0) anchorPending.Add(ri); continue; }
                                var n0 = mainPts.OrderBy(q => SplineEdge.Dist(q, rp[0])).First(); var n1 = mainPts.OrderBy(q => SplineEdge.Dist(q, rp[rp.Count - 1])).First();
                                double d0 = SplineEdge.Dist(n0, rp[0]), d1 = SplineEdge.Dist(n1, rp[rp.Count - 1]);
                                if (Math.Min(d0, d1) > s.alleyRelaxAttachMaxMeters) { if (s.alleyAnchors.Length > 0) anchorPending.Add(ri); continue; }
                                if (d1 < d0) { apts2.Reverse(); n0 = n1; }
                                apts2.Insert(0, n0);
                            }
                            else if (!t0) continue;
                            alleyCand++; alleyLen += SplineEdge.PolyLength(apts2); relaxedAlleys++;
                            alleyRaw.Add(apts2);
                            continue;
                        }
                    }
                    if (!runs[ri].points.Any(p => nodeIndex.TryGetValue(K(p), out var n) && mainNodes.Contains(n))) continue;
                    alleyCand++; alleyLen += Math.Min(runLen[ri], s.alleyMaxLengthMeters);
                    var apts = runs[ri].points;
                    bool startTouch = nodeIndex.TryGetValue(K(apts[0]), out var an0) && mainNodes.Contains(an0);
                    bool endTouch = nodeIndex.TryGetValue(K(apts[apts.Count - 1]), out var an1) && mainNodes.Contains(an1);
                    if (!startTouch && endTouch) { apts = new List<Vec2>(apts); apts.Reverse(); startTouch = true; }
                    if (startTouch) alleyRaw.Add(TrimLength(apts, s.alleyMaxLengthMeters));
                }
                // §4.3 계단 닻: 큰길에 못 붙은 완화 원 골목 중 계단 끝(또는 이미 받아들인 닻 골목 끝)에서 alleyAnchorMaxMeters 안인 것 — 닻 쪽 끝을 시작으로
                var anchors = new List<Vec2>(s.alleyAnchors);
                for (bool grew = true; grew && anchorPending.Count > 0;)
                {
                    grew = false;
                    for (int k = anchorPending.Count - 1; k >= 0; k--)
                    {
                        var rp = runs[anchorPending[k]].points;
                        double d0 = anchors.Min(q => SplineEdge.Dist(q, rp[0])), d1 = anchors.Min(q => SplineEdge.Dist(q, rp[rp.Count - 1]));
                        if (Math.Min(d0, d1) > s.alleyAnchorMaxMeters) continue;
                        var ap2 = new List<Vec2>(rp); if (d1 < d0) ap2.Reverse();
                        alleyCand++; alleyLen += SplineEdge.PolyLength(ap2); relaxedAlleys++; anchoredAlleys++;
                        alleyRaw.Add(ap2); alleyAnchored.Add(ap2);
                        anchors.Add(rp[0]); anchors.Add(rp[rp.Count - 1]);
                        anchorPending.RemoveAt(k); grew = true;
                    }
                }
            }
            var edges = new List<SplineEdge>();
            for (int i = 0; i < rawEdges.Count; i++) if (isMain[i]) edges.Add(rawEdges[i].e);
            double selectedLen = edges.Sum(e => e.Length);

            // ④ 차수 2 노드에서 이어 붙여 한 곡선으로 (다듬기가 끊기지 않게)
            edges = MergeChains(edges, res.nodes.Count);

            // ⑤ 큰길 망 한 덩어리: 가장 긴 덩어리 + (≥ 80m 조각은 가장 가까운 점에 직선 연결 / 아니면 버림)
            double connectorLen = 0, droppedLen = 0; int connectors = 0, dropped = 0;
            for (int guard = 0; guard < 64; guard++)
            {
                var comp = Components(edges, res.nodes.Count, out var compLen);
                if (compLen.Count <= 1) break;
                int main = Enumerable.Range(0, compLen.Count).OrderByDescending(k => compLen[k]).First();
                int other = Enumerable.Range(0, compLen.Count).Where(k => k != main).OrderByDescending(k => compLen[k]).First();
                var otherEdges = edges.Where(e => comp[e.a] == other).ToList();
                if (compLen[other] < s.isolatedDropLengthMeters || !RouteConnect(edges, comp, other, main, res, IsSea, H, s, out double clen))
                {
                    droppedLen += compLen[other]; dropped++;
                    edges.RemoveAll(e => comp[e.a] == other);
                    continue;
                }
                connectorLen += clen; connectors++;
            }
            edges = MergeChains(edges, res.nodes.Count);
            // ⑤-b 붙어 있는 교차점(사이 길이 < junctionMergeMeters)은 한 교차점으로 — 매듭(10m 간격 교차점 셋)이 원판·반경을 망침(3차 M3)
            int dupes = 0, collapsed = 0;
            for (int pass = 0; pass < 8; pass++)
            {
                // 교차점을 합치면 같은 두 노드를 잇는 겹친 길이 새로 생길 수 있고, 그걸 빼면 다시 합칠 짧은 길이 생길 수 있다 → 안정될 때까지
                int c = CollapseShortLinks(edges, res, s.junctionMergeMeters), d = DropDuplicateEdges(edges, s.HalfCarriageway);
                collapsed += c; dupes += d;
                if (c + d == 0) break;
                edges = MergeChains(edges, res.nodes.Count);
            }

            // ⑥ 곡선 다듬기 (끝점 고정): 필렛(꼭짓점마다 반경 R 원호, §2.9) → 2m 재표본
            double maxDev = 0, connDev = 0; string maxDevAt = "";
            var fstat = new FilletStats(); var padSet = new HashSet<(long, long)>(); var padLog = new List<string>();
            var degPre = new int[res.nodes.Count];
            // 15a-2: 패드 점 가까이(0.75m)는 헤어핀 — 이완(EnforceRadius)이 12m 를 맞추려 패드 끝을 안으로 당기지 않게, 경사 상한도 패드 5%
            void MarkPads(List<bool> hf, List<Vec2> p0, List<Vec2> padPts) { if (padPts == null || padPts.Count == 0) return; for (int i = 0; i < p0.Count && i < hf.Count; i++) if (!hf[i] && padPts.Any(v => Math.Abs(v.x - p0[i].x) < 0.75 && Math.Abs(v.z - p0[i].z) < 0.75)) hf[i] = true; }
            void Smooth(SplineEdge e)
            {
                var orig = Resample(e.pts, s.resampleStepMeters);
                List<Vec2> p; List<Vec2> padPts = null;
                if (s.useFillet)
                {
                    var fin = s.hairpinPads && !e.connector && !e.alley ? HairpinPads(e.pts, s.hairpinMinRadiusMeters * 1.06, s.mainMinRadiusMeters + 0.5, s.maxDeviationMeters, s.padTips, padSet, padLog) : e.pts;
                    padPts = fin.Where(v => padSet.Contains(PadKey(v))).ToList();
                    p = Fillet(fin, s.mainMinRadiusMeters, s.hairpinMinRadiusMeters, e.connector ? s.connectorSimplifyMeters : s.filletSimplifyMeters, s, fstat, degPre[e.a] == 1 || OnBoundary(res.nodes[e.a], s), degPre[e.b] == 1 || OnBoundary(res.nodes[e.b], s), e.connector ? s.connectorStraightenDeviationMeters : s.maxDeviationMeters, padSet);
                    p = Resample(p, s.resampleStepMeters);
                    // 필렛이 못 편 자리(S자 겹침·끝 공간 부족)만 이웃 평균 이완으로 덧대기(이탈 상한 그대로). U자는 필렛이 이미 처리.
                    var hpTmp = HairpinFlags(p, s); MarkPads(hpTmp, p, padPts);
                    double devTmp = 0;
                    // 잇기 길(생성 길)은 OSM 이탈 상한 대신 잇기 상한으로 이완 — 지도 경계를 따라 도는 경로의 잔 굽이(3차 M3)
                    var sRelax = s; if (e.connector) { sRelax = s.Clone(); sRelax.maxDeviationMeters = s.connectorStraightenDeviationMeters; }
                    p = EnforceRadius(p, hpTmp, orig, sRelax, ref devTmp, padPts);
                    // 필렛 원호가 이탈 상한을 조금 넘긴 점(짧은 갈고리 굽이 — 15a-2 6.2m)은 원래 선 쪽으로 상한까지 당긴다. 패드 점은 그대로.
                    if (!e.connector) for (int i = 0; i < p.Count; i++)
                    {
                        double dv = DistToPoly(p[i], orig); if (dv <= s.maxDeviationMeters * 0.99) continue;
                        if (padPts != null && padPts.Any(v => Math.Abs(v.x - p[i].x) < 0.75 && Math.Abs(v.z - p[i].z) < 0.75)) continue;
                        Vec2 best = orig[0]; double bd = double.MaxValue;
                        for (int k = 0; k + 1 < orig.Count; k++) { double dd = DistToSeg(p[i], orig[k], orig[k + 1], out double t); if (dd < bd) { bd = dd; best = new Vec2(orig[k].x + (orig[k + 1].x - orig[k].x) * t, orig[k].z + (orig[k + 1].z - orig[k].z) * t); } }
                        double f = 1 - s.maxDeviationMeters * 0.99 / dv;
                        p[i] = new Vec2(p[i].x + (best.x - p[i].x) * f, p[i].z + (best.z - p[i].z) * f);
                    }
                    if (e.connector) foreach (var v in p) connDev = Math.Max(connDev, DistToPoly(v, orig));
                    else foreach (var v in p) { double dv = DistToPoly(v, orig); if (dv > maxDev) { maxDev = dv; maxDevAt = $"({v.x:0.0},{v.z:0.0}) {e.highway} 노드 {e.a}-{e.b}"; res.maxDevOrig = new List<Vec2>(e.pts); res.maxDevOut = new List<Vec2>(p); } }
                }
                else
                {
                    p = orig;
                    for (int k = 0; k < s.chaikinIterations; k++) p = Chaikin(p);
                    p = Resample(p, s.resampleStepMeters);
                    e.hairpin = HairpinFlags(p, s);
                    p = EnforceRadius(p, e.hairpin, orig, s, ref maxDev);
                }
                // 막다른 끝·지도 경계 끝 가까이(30m) 반경 미달이 남으면 그 점까지 끝을 잘라낸다(교차점이 아닌 끝만 — 3차 M3, PD 허용)
                bool freeA = degPre[e.a] == 1 || OnBoundary(res.nodes[e.a], s), freeB = degPre[e.b] == 1 || OnBoundary(res.nodes[e.b], s);
                if (freeA || freeB)
                {
                    var hfl = HairpinFlags(p, s);
                    double Need(int i) => hfl[i] ? s.hairpinMinRadiusMeters : s.mainMinRadiusMeters;
                    int cutA = 0, cutB = p.Count - 1; double acc = 0;
                    if (freeA && degPre[e.a] <= 1)
                        for (int i = 1; i + 1 < p.Count && acc <= s.freeEndTrimMeters; i++) { acc += SplineEdge.Dist(p[i - 1], p[i]); if (RadiusAt(p, i, 1) < Need(i) * 0.999) cutA = i + 1; }
                    acc = 0;
                    if (freeB && degPre[e.b] <= 1)
                        for (int i = p.Count - 2; i >= 1 && acc <= s.freeEndTrimMeters; i--) { acc += SplineEdge.Dist(p[i + 1], p[i]); if (RadiusAt(p, i, 1) < Need(i) * 0.999) cutB = Math.Min(cutB, i - 1); }
                    if ((cutA > 0 || cutB < p.Count - 1) && cutB - cutA >= 3)
                    {
                        fstat.endCut += (cutA > 0 ? 1 : 0) + (cutB < p.Count - 1 ? 1 : 0);
                        p = p.GetRange(cutA, cutB - cutA + 1);
                        res.nodes[e.a] = p[0]; res.nodes[e.b] = p[p.Count - 1];
                    }
                }
                e.pts = p;
                e.hairpin = HairpinFlags(p, s); MarkPads(e.hairpin, p, padPts);
                // 재표본으로 바뀐 패드 점을 다시 등록 — 겹친 길을 빼고 이어 붙인 길을 다시 다듬을 때 필렛이 패드를 깎지 않게(15a-2)
                if (padPts != null) for (int i = 0; i < p.Count; i++) if (e.hairpin[i] && padPts.Any(v => Math.Abs(v.x - p[i].x) < 0.75 && Math.Abs(v.z - p[i].z) < 0.75)) padSet.Add(PadKey(p[i]));
            }
            foreach (var e in edges) { degPre[e.a]++; degPre[e.b]++; }
            foreach (var e in edges) Smooth(e);
            // 다듬은 뒤 같은 두 노드를 잇는 겹친 길(원래 모양이 달라 다듬기 전엔 안 겹치던 것) → 하나 빼고, 이어 붙은 길만 다시 다듬는다
            {
                int d2 = DropDuplicateEdges(edges, s.HalfCarriageway);
                if (d2 > 0)
                {
                    dupes += d2;
                    var before = edges.ToDictionary(x => x, x => x.pts.Count);
                    edges = MergeChains(edges, res.nodes.Count);
                    Array.Clear(degPre, 0, degPre.Length); foreach (var e in edges) { degPre[e.a]++; degPre[e.b]++; }
                    foreach (var e in edges) if (!before.TryGetValue(e, out int c0) || c0 != e.pts.Count) Smooth(e);
                }
                rep.Metric("junction_merged", $"{collapsed} (교차점 사이 {s.junctionMergeMeters:0}m 미만 길을 접어 한 교차점으로) · 겹친 중복 길 뺌 {dupes}");
            }

            // ⑦ 종단: 노드 높이 = 지형, 선 높이 = 지형 이동평균(20m) → 끝 맞춤 → 경사 상한(앞뒤 반복)
            // 노드 높이: 지형에서 시작 → 막다른 끝(차수 1)은 고정 안 함 → 교차점끼리 경사 상한으로 닿을 수 있게 서로 당김(이완)
            res.nodeY = res.nodes.Select(n => H.HeightAt(n.x, n.z)).ToList();
            var degN = new int[res.nodes.Count]; foreach (var e in edges) { degN[e.a]++; degN[e.b]++; }
            // 교차점 판이 덮는 거리(끊는 거리)를 모양만으로 먼저 구해, 패드(≤5%)를 그 거리 + 여유까지 늘린다 — 판 둘레에 10% 구간이 섞이면 판이 꺾임(4차 BUG-005)
            res.edges = edges; res.junctions.Clear();
            for (int n = 0; n < res.nodes.Count; n++) if (degN[n] >= 3) res.junctions.Add(n);
            BuildJunctionLayouts(res, s);
            foreach (var e in edges)
            {
                var arcIn = new double[e.pts.Count];
                for (int i = 1; i < e.pts.Count; i++) arcIn[i] = arcIn[i - 1] + SplineEdge.Dist(e.pts[i - 1], e.pts[i]);
                double L = arcIn[arcIn.Length - 1];
                double padA = Math.Max(s.junctionPadMeters, e.clipA + s.junctionPadMarginMeters), padB = Math.Max(s.junctionPadMeters, e.clipB + s.junctionPadMarginMeters);
                e.pad = Enumerable.Range(0, e.pts.Count).Select(i => (degN[e.a] >= 3 && arcIn[i] <= padA) || (degN[e.b] >= 3 && L - arcIn[i] <= padB)).ToList();
            }
            res.junctions.Clear(); res.junctionLayouts.Clear();
            int relaxIters = RelaxNodeHeights(edges, res.nodeY, degN, s);
            foreach (var e in edges)
            {
                e.y = Profile(e, H, degN[e.a] >= 2 ? res.nodeY[e.a] : (double?)null, degN[e.b] >= 2 ? res.nodeY[e.b] : (double?)null, s);
                if (degN[e.a] < 2) res.nodeY[e.a] = e.y[0];
                if (degN[e.b] < 2) res.nodeY[e.b] = e.y[e.y.Count - 1];
            }
            // 교차점 판 평면: 붙은 길이 경사 상한 안에서 받아낼 수 있는 교차점만 평면, 아니면 원뿔꼴(4차 BUG-005)
            {
                res.edges = edges; res.junctions.Clear();
                for (int n = 0; n < res.nodes.Count; n++) if (degN[n] >= 3) res.junctions.Add(n);
                BuildJunctionLayouts(res, s);
                var (planes, cones) = TryJunctionPlanes(res, degN, s);
                res.junctions.Clear(); res.junctionLayouts.Clear();
                rep.Metric("junction_planes", $"평면 {planes} · 원뿔꼴 {cones} (붙은 길 경사 상한으로 평면 높이를 못 받는 교차점)");
            }
            rep.Metric("node_relax_iters", relaxIters.ToString(CultureInfo.InvariantCulture));
            if (s.hairpinPads) rep.Metric("hairpin_pads", $"{padLog.Count} · {string.Join(" · ", padLog.Distinct().Take(20))}");
            rep.Metric("osm_deviation_max_at", $"{maxDev:0.0}m {maxDevAt}");
            rep.Metric("connector_deviation", $"잇기 길(생성 길) 다듬기 전 선에서 최대 {connDev:0.0}m (상한 {s.connectorStraightenDeviationMeters} — OSM 이탈 상한 {s.maxDeviationMeters}m 는 실제 길에만)");
            rep.Metric("fillet", $"원호 {fstat.arcs} · 합침 {fstat.merged} · 두 굽이로 나눔 {fstat.split} · 막다른 끝 토막 자름 {fstat.trimmed} · 꼭짓점 빼서 폄 {fstat.straightened} · 막다른·경계 끝 반경 미달 잘라냄 {fstat.endCut} · 반경 줄임(S자·교차점 끝 공간 부족) {fstat.reduced} {string.Join(" ", fstat.reducedAt.Take(8))} · 펴지 못한 U자 {fstat.uturn}");
            res.edges = edges;
            for (int n = 0; n < res.nodes.Count; n++) if (res.NodeDegree(n) >= 3) res.junctions.Add(n);

            // ⑦-b 골목(§2.7): 큰길 위 시작점에 붙이고(높이 = 그 자리 큰길 높이), 다듬고, 경사 12% 로 종단
            int alleyKept = 0, alleyRejected = 0, alleyTight = 0;
            var rejAt = new List<string>(); void Rej(string why, List<Vec2> p) { if (p == null || p.Count == 0) { rejAt.Add(why); return; } var m = p[p.Count / 2]; rejAt.Add($"{why} ({m.x:0},{m.z:0}) {SplineEdge.PolyLength(p):0}m"); }
            var alleySettings = s.Clone(); alleySettings.mainMinRadiusMeters = s.alleyMinRadiusMeters; alleySettings.hairpinMinRadiusMeters = s.alleyMinRadiusMeters;
            var alleyQueue = alleyRaw.ToList(); int alleyPasses = s.alleyChain ? 16 : 1; bool chainAttach = false; int chainLinks = 0;
            for (int pass = 0; pass < alleyPasses && alleyQueue.Count > 0; pass++)
            {
            var alleyNext = new List<List<Vec2>>(); int keptBefore = alleyKept;
            foreach (var raw0 in alleyQueue)
            {
                var raw = raw0; bool chain = s.ChainAt(raw[raw.Count / 2]);
                if (raw.Count < 2 || SplineEdge.PolyLength(raw) < 6) { alleyRejected++; Rej("short", raw); continue; }
                if (alleyAnchored.Contains(raw0))
                {
                    // 계단 닻 골목: 큰길 입구 없음 — 시작 높이 = 그 자리 땅, 이후 같은 12% 종단
                    var bp = s.useFillet ? Resample(Fillet(raw, s.alleyMinRadiusMeters, s.alleyMinRadiusMeters, s.filletSimplifyMeters, s, new FilletStats()), s.resampleStepMeters) : Resample(raw, s.resampleStepMeters);
                    bool crossA = false;
                    for (int i = 0; i + 1 < bp.Count && !crossA; i++) crossA = CrossesAnyEdge(bp[i], bp[i + 1], res.edges);
                    if (crossA || SplineEdge.PolyLength(bp) < 6) { alleyRejected++; Rej(crossA ? "anchor_cross" : "anchor_short", raw); continue; }
                    { bool tight = false; for (int i = 1; i + 1 < bp.Count && !tight; i++) if (RadiusAt(bp, i, 1) < s.alleyMinRadiusMeters - 1e-6) tight = true; if (tight) { alleyRejected++; Rej("anchor_tight", raw); alleyTight++; continue; } }
                    double y0 = H.HeightAt(bp[0].x, bp[0].z);
                    var be = new SplineEdge { pts = bp, highway = "alley", alley = true, hairpin = Enumerable.Repeat(false, bp.Count).ToList() };
                    be.a = res.nodes.Count; res.nodes.Add(bp[0]); res.nodeY.Add(y0);
                    be.b = res.nodes.Count; res.nodes.Add(bp[bp.Count - 1]); res.nodeY.Add(0);
                    be.y = Profile(be, H, y0, null, s, s.alleyMaxGrade);
                    res.nodeY[be.b] = be.y[be.y.Count - 1];
                    res.edges.Add(be); alleyKept++;
                    continue;
                }
                SplineEdge host = null; int hostIdx = -1; double hd = double.MaxValue;
                foreach (var me in edges) for (int i = 0; i < me.pts.Count; i++) { double d = SplineEdge.Dist(me.pts[i], raw[0]); if (d < hd) { hd = d; host = me; hostIdx = i; } }
                if (chain)
                {
                    // 조각 12: 어느 끝이 이미 있는 길(큰길·받아들인 차로)에 붙나 — 가까운 끝을 시작으로
                    SplineEdge h2 = null; int i2 = -1; double d2 = double.MaxValue; var last = raw[raw.Count - 1];
                    foreach (var me in edges) for (int i = 0; i < me.pts.Count; i++) { double d = SplineEdge.Dist(me.pts[i], last); if (d < d2) { d2 = d; h2 = me; i2 = i; } }
                    if (d2 < hd) { raw = new List<Vec2>(raw); raw.Reverse(); host = h2; hostIdx = i2; hd = d2; }
                    if (host != null && hd > 8 && chainAttach && hd <= s.alleyRelaxAttachMaxMeters) { raw = new List<Vec2>(raw); raw.Insert(0, host.pts[hostIdx]); hd = 0; chainLinks++; }   // 더 이을 게 없을 때만: 가장 가까운 차도 점에 직선(§4.2 와 같은 30m)
                    if (host != null && hd > 8) { alleyNext.Add(raw0); continue; }   // 아직 이어질 길이 없음 — 다음 바퀴
                }
                if (host != null && hd > 3 && hd <= 8 && s.AlleyRelaxOn) { raw = new List<Vec2>(raw); raw.Insert(0, host.pts[hostIdx]); hd = 0; }   // 다듬기로 큰길이 몇 m 옮겨 간 자리 — 골목 시작을 큰길 점에 붙임(§4.2)
                if (host == null || hd > 3) { alleyRejected++; Rej(host == null ? "no_host" : $"far_from_main_{hd:0.0}m", raw); continue; }
                List<Vec2> ap;
                if (s.useFillet) ap = Resample(Fillet(raw, s.alleyMinRadiusMeters, s.alleyMinRadiusMeters, s.filletSimplifyMeters, s, new FilletStats()), s.resampleStepMeters);
                else
                {
                    ap = Resample(raw, s.resampleStepMeters);
                    ap = Chaikin(ap); ap = Resample(ap, s.resampleStepMeters);
                    double adev = 0;
                    var noHair = Enumerable.Repeat(false, ap.Count).ToList();
                    ap = EnforceRadius(ap, noHair, raw, alleySettings, ref adev);
                }
                bool cross = false;
                var crossSet = chain ? res.edges.Where(e => !e.alley).ToList() : res.edges;   // 조각 12: 차로끼리 만나는 건 교차로
                for (int i = 2; i + 1 < ap.Count && !cross; i++) cross = CrossesAnyEdge(ap[i], ap[i + 1], crossSet);
                if (cross) { alleyRejected++; Rej("crosses_road", raw); continue; }
                // 골목 입구 = 큰길 보도 바깥 끝(보도 높이). 중앙선에서 시작하면 밝은 돌이 차도 위에 깔린다(그래픽 결함, 2026-10-20 3차).
                ap = TrimToMouth(ap, host.pts, chain && host.alley ? s.alleyHalfWidthMeters : s.HalfSection);
                if (ap != null) ap = Resample(ap, s.resampleStepMeters);   // 잘린 첫 구간 길이를 고르게(짧은 첫 구간 = 3점 반경 왜곡)
                if (ap == null || SplineEdge.PolyLength(ap) < 6) { alleyRejected++; Rej("short_after_mouth", raw); continue; }
                // 반경 6m 를 못 지키는 골목(급하게 꺾인 OSM 보행로·직선으로 이은 첫 구간)은 버린다 — M3 골목 하한
                { bool tight = false; for (int i = 1; i + 1 < ap.Count && !tight; i++) if (RadiusAt(ap, i, 1) < s.alleyMinRadiusMeters - 1e-6) tight = true; if (tight) { alleyRejected++; Rej("radius_lt_min", raw); alleyTight++; continue; } }
                hd = double.MaxValue; for (int i = 0; i < host.pts.Count; i++) { double d = SplineEdge.Dist(host.pts[i], ap[0]); if (d < hd) { hd = d; hostIdx = i; } }
                double mouthY = host.y[hostIdx] + (chain && host.alley ? 0 : s.curbHeightMeters);
                var ae = new SplineEdge { pts = ap, highway = "alley", alley = true, hairpin = Enumerable.Repeat(false, ap.Count).ToList() };
                ae.a = res.nodes.Count; res.nodes.Add(ap[0]); res.nodeY.Add(mouthY);
                ae.b = res.nodes.Count; res.nodes.Add(ap[ap.Count - 1]); res.nodeY.Add(0);
                ae.y = Profile(ae, H, mouthY, null, s, s.alleyMaxGrade);
                res.nodeY[ae.b] = ae.y[ae.y.Count - 1];
                res.edges.Add(ae); alleyKept++;
            }
            alleyQueue = alleyNext;
            if (alleyKept == keptBefore) { if (s.alleyChain && !chainAttach) { chainAttach = true; continue; } break; }
            }
            foreach (var r0 in alleyQueue) { alleyRejected++; Rej("no_road_to_join", r0); }
            rep.Metric("alleys", $"{alleyKept}개 · {res.Alleys.Sum(a => a.Length):0}m (후보 {alleyRaw.Count} — 그중 §4.2 중심 완화 {relaxedAlleys}(계단 닻 {anchoredAlleys}), 버림 {alleyRejected}: 너무 짧음·큰길에서 떨어짐·가로지름 · 반경 6m 미달 {alleyTight})");
            rep.Metric("alley_rejected_at", string.Join(" · ", rejAt));
            if (s.alleyChain) rep.Metric("alley_chain_links", $"{chainLinks} (다른 차로·큰길에 안 닿는 차로 토막을 30m 안 차도 점에 직선으로 이은 수)");

            // ⑦-c 충돌 정리(BUG-003): 높이 다른 길끼리 단면이 겹치는 자리
            ResolveConflicts(res, s);

            // ⑦-d 교차점 판(원판 + 모서리 보도): 붙는 길을 어디서 끊을지
            BuildJunctionLayouts(res, s);

            // ⑧ 지형 + 깎기/메우기 + 옹벽 (조각 13: 그 전에 골목 폭 = 건물 사이 틈)
            s.alleyAdjust?.Invoke(res);
            CutFill(res, H, s, out double maxCut, out int overCut, out double wallLen, out int wallSides);

            // ⑧-a 마을 중심 광장 + 종탑 자리 + 상점(§11.3)
            if (s.plazaEnabled) PlacePlaza(res, H, s);

            // ⑧-b 집 줄짓기(§2.6)
            PlaceHouses(res, s);

            // ⑨ 보고 (스펙 §6 M1~M3·M5~M8 길 관련)
            rep.Metric("osm_runs", $"차도 run {runs.Count} (바다로 잘린 점 {seaClipped}, 계단 way {stepsWays} — 장식 후보, 1차 미사용)");
            rep.Metric("main_selected_m", $"{selectedLen:0} (선별 직후)");
            rep.Metric("main_final_m", $"{res.Mains.Sum(x => x.Length):0} · 곡선 {res.Mains.Count()}개 · 교차점(차수≥3) {res.junctions.Count}");
            rep.Metric("alley_candidates", $"{alleyCand}개 · 앞 {s.alleyMaxLengthMeters:0}m 기준 {alleyLen:0}m (2차에서 씀)");
            rep.Metric("connectors", $"{connectors}개 · {connectorLen:0}m");
            {
                // 기획 §2.2 C 기준(가설): 잇기 총길이 > 큰길 총길이 40% 또는 잇기 하나 > 150m → 포지타노 조각 다시 자르기
                double mainTotal = edges.Sum(x => x.Length), connTotal = edges.Where(x => x.connector).Sum(x => x.Length), connMax = edges.Where(x => x.connector).Select(x => x.Length).DefaultIfEmpty(0).Max();
                double ratio = mainTotal > 0 ? connTotal / mainTotal : 0;
                rep.Metric("connector_stats", $"잇기 총길이 {connTotal:0}m (다듬은 뒤) · 최장 {connMax:0}m · 큰길 대비 {ratio * 100:0}% (기획 판단: 40%·150m 기준 폐기, 보고만)");
                // 잇기 구간 목록 — 도현이 씬 뷰에서 "만든 길"이 실제처럼 읽히는지 볼 자리
                int ci = 0;
                foreach (var ce in edges.Where(x => x.connector).OrderByDescending(x => x.Length))
                {
                    var p0 = ce.pts[0]; var p1 = ce.pts[ce.pts.Count - 1];
                    rep.Metric($"connector_{++ci}", $"({p0.x:0},{p0.z:0}) → ({p1.x:0},{p1.z:0}) · {ce.Length:0}m · 높이 {H.HeightAt(p0.x, p0.z):0.0} → {H.HeightAt(p1.x, p1.z):0.0}m");
                }
            }
            rep.Metric("dropped_isolated", $"{dropped}개 · {droppedLen:0}m");
            Measure(res, H, s, maxDev);
            rep.Metric("cut_fill_max_m", $"{maxCut:0.00} (상한 {s.maxCutFillMeters}, 넘은 지형 점 {overCut})");
            rep.Metric("walls", $"{wallLen:0}m · 옹벽 구간 {wallSides} (기준 높이차 {s.wallThresholdMeters}m)");
            rep.Check("M7", overCut == 0, $"깎기·메우기 최대 {maxCut:0.00}m (≤ {s.maxCutFillMeters}), 넘은 점 {overCut} · 옹벽 {wallLen:0}m");
            rep.Metric("hash", Hash(res));
            sw.Stop();
            rep.Metric("build_ms", sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));
            return res;
        }

        // ------------------------------------------------------------------ 그래프
        /// <summary>같은 두 노드를 잇고 서로 tol 안에 겹치는 길(같은 구간이 두 번 들어온 데이터)은 하나만 남긴다 — 잇기 길·짧은 쪽을 뺀다.</summary>
        public static int DropDuplicateEdges(List<SplineEdge> edges, double tol)
        {
            int n = 0;
            for (int i = 0; i < edges.Count; i++)
                for (int j = edges.Count - 1; j > i; j--)
                {
                    var A = edges[i]; var B = edges[j];
                    if (A.a == A.b || !((A.a == B.a && A.b == B.b) || (A.a == B.b && A.b == B.a))) continue;
                    double d = 0;
                    foreach (var q in Resample(B.pts, 1)) d = Math.Max(d, DistToPoly(q, A.pts));
                    foreach (var q in Resample(A.pts, 1)) d = Math.Max(d, DistToPoly(q, B.pts));
                    if (d > tol) continue;
                    bool dropA = (A.connector && !B.connector) || (A.connector == B.connector && A.Length < B.Length);
                    if (dropA) { edges.RemoveAt(i); i--; n++; break; }
                    edges.RemoveAt(j); n++;
                }
            return n;
        }

        /// <summary>양 끝이 모두 교차점(차수 ≥ 3)이고 길이가 maxLen 미만인 길을 접어, 두 교차점을 그 길 가운데 한 점으로 합친다.</summary>
        public static int CollapseShortLinks(List<SplineEdge> edges, SplineTownResult res, double maxLen)
        {
            int count = 0;
            for (int guard = 0; guard < 64; guard++)
            {
                var deg = new int[res.nodes.Count]; foreach (var e in edges) { deg[e.a]++; deg[e.b]++; }
                var link = edges.Where(e => e.a != e.b && deg[e.a] >= 3 && deg[e.b] >= 3 && e.Length < maxLen).OrderBy(e => e.Length).FirstOrDefault();
                if (link == null) break;
                // 가운데 = 선 위 길이 절반 점
                double half = link.Length / 2, acc = 0; Vec2 mid = link.pts[0];
                for (int i = 1; i < link.pts.Count; i++)
                {
                    double d = SplineEdge.Dist(link.pts[i - 1], link.pts[i]);
                    if (acc + d >= half) { double t = d > 1e-9 ? (half - acc) / d : 0; mid = new Vec2(link.pts[i - 1].x + (link.pts[i].x - link.pts[i - 1].x) * t, link.pts[i - 1].z + (link.pts[i].z - link.pts[i - 1].z) * t); break; }
                    acc += d;
                }
                int keep = link.a, gone = link.b;
                edges.Remove(link);
                res.nodes[keep] = mid;
                foreach (var e in edges)
                {
                    if (e.a == gone) e.a = keep;
                    if (e.b == gone) e.b = keep;
                    if (e.a == keep) e.pts[0] = mid;
                    if (e.b == keep) e.pts[e.pts.Count - 1] = mid;
                }
                edges.RemoveAll(e => e.a == e.b && e.Length < 2 * maxLen);   // 접혀 생긴 짧은 고리
                count++;
            }
            return count;
        }

        static List<SplineEdge> MergeChains(List<SplineEdge> edges, int nodeCount)
        {
            var deg = new int[nodeCount];
            foreach (var e in edges) { deg[e.a]++; deg[e.b]++; }
            var used = new bool[edges.Count];
            var outList = new List<SplineEdge>();
            for (int i = 0; i < edges.Count; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                var e = Clone(edges[i]);
                // 뒤로(b 쪽) 이어 붙이기
                for (bool grew = true; grew;)
                {
                    grew = false;
                    if (deg[e.b] != 2 || e.b == e.a) break;
                    for (int j = 0; j < edges.Count; j++)
                    {
                        if (used[j]) continue;
                        var f = edges[j];
                        if (f.a == e.b) { e.pts.AddRange(f.pts.Skip(1)); e.b = f.b; e.connector |= f.connector; }
                        else if (f.b == e.b) { var rp = new List<Vec2>(f.pts); rp.Reverse(); e.pts.AddRange(rp.Skip(1)); e.b = f.a; e.connector |= f.connector; }
                        else continue;
                        used[j] = true; grew = true; break;
                    }
                }
                // 앞으로(a 쪽)
                for (bool grew = true; grew;)
                {
                    grew = false;
                    if (deg[e.a] != 2 || e.b == e.a) break;
                    for (int j = 0; j < edges.Count; j++)
                    {
                        if (used[j]) continue;
                        var f = edges[j];
                        if (f.b == e.a) { var np = new List<Vec2>(f.pts); np.AddRange(e.pts.Skip(1)); e.pts = np; e.a = f.a; e.connector |= f.connector; }
                        else if (f.a == e.a) { var rp = new List<Vec2>(f.pts); rp.Reverse(); rp.AddRange(e.pts.Skip(1)); e.pts = rp; e.a = f.b; e.connector |= f.connector; }
                        else continue;
                        used[j] = true; grew = true; break;
                    }
                }
                outList.Add(e);
            }
            return outList;
        }

        static SplineEdge Clone(SplineEdge e) => new SplineEdge { a = e.a, b = e.b, pts = new List<Vec2>(e.pts), highway = e.highway, connector = e.connector };

        static int[] Components(List<SplineEdge> edges, int nodeCount, out List<double> compLen)
        {
            var comp = Enumerable.Repeat(-1, nodeCount).ToArray();
            var adj = new List<int>[nodeCount];
            for (int i = 0; i < nodeCount; i++) adj[i] = new List<int>();
            foreach (var e in edges) { adj[e.a].Add(e.b); adj[e.b].Add(e.a); }
            int id = 0;
            var used = new HashSet<int>(); foreach (var e in edges) { used.Add(e.a); used.Add(e.b); }
            foreach (var start in used.OrderBy(x => x))
            {
                if (comp[start] >= 0) continue;
                var q = new Queue<int>(); q.Enqueue(start); comp[start] = id;
                while (q.Count > 0) { int u = q.Dequeue(); foreach (var v in adj[u]) if (comp[v] < 0) { comp[v] = id; q.Enqueue(v); } }
                id++;
            }
            compLen = Enumerable.Repeat(0.0, id).ToList();
            foreach (var e in edges) compLen[comp[e.a]] += e.Length;
            return comp;
        }

        /// <summary>
        /// 고립 조각 → 큰길 망 잇기(§2.2 "최소 경사로"): 격자(4m, 8방향) 다익스트라.
        /// 출발 = 조각 길 점 근처 칸들, 도착 = 큰길 망 점 근처 칸. 비용 = 길이 × (1 + 벌점 × 경사 상한 초과분).
        /// 바다 칸·다른 조각 길 근처(clearance)는 못 지남. 찾은 경로는 꺾은선 잇기 길이 되고, 뒤의 곡선 다듬기를 그대로 탄다.
        /// </summary>
        static bool RouteConnect(List<SplineEdge> edges, int[] comp, int other, int main, SplineTownResult res, Func<Vec2, bool> isSea, IHeightField H, SplineTownSettings s, out double len)
        {
            len = 0;
            double gs = s.connectorGridMeters;
            int gw = (int)Math.Floor(s.boundsXMeters / gs) + 1, gh = (int)Math.Floor(s.boundsZMeters / gs) + 1;
            Vec2 C(int i) => new Vec2((i % gw) * gs, (i / gw) * gs);
            var hgt = new double[gw * gh]; var blocked = new bool[gw * gh];
            for (int i = 0; i < hgt.Length; i++) { var p = C(i); hgt[i] = H.HeightAt(p.x, p.z); blocked[i] = isSea(p); }
            var src = new HashSet<int>(); var dst = new HashSet<int>();
            var near = new Dictionary<int, (int edge, int idx)>();
            void Mark(int ei, double radius, HashSet<int> set, bool block)
            {
                var e = edges[ei];
                for (int k = 0; k < e.pts.Count; k++)
                {
                    var p = e.pts[k];
                    int c0 = (int)Math.Floor((p.x - radius) / gs), c1 = (int)Math.Ceiling((p.x + radius) / gs);
                    int r0 = (int)Math.Floor((p.z - radius) / gs), r1 = (int)Math.Ceiling((p.z + radius) / gs);
                    for (int r = Math.Max(0, r0); r <= Math.Min(gh - 1, r1); r++)
                        for (int c = Math.Max(0, c0); c <= Math.Min(gw - 1, c1); c++)
                        {
                            int i = r * gw + c;
                            if (SplineEdge.Dist(C(i), p) > radius) continue;
                            if (block) { blocked[i] = true; continue; }
                            set.Add(i);
                            if (!near.TryGetValue(i, out var cur) || SplineEdge.Dist(C(i), p) < SplineEdge.Dist(C(i), edges[cur.edge].pts[cur.idx])) near[i] = (ei, k);
                        }
                }
            }
            for (int ei = 0; ei < edges.Count; ei++)
            {
                int ce = comp[edges[ei].a];
                if (ce == other) Mark(ei, gs * 0.75, src, false);
                else if (ce == main) Mark(ei, gs * 0.75, dst, false);
                else Mark(ei, s.connectorClearanceMeters, null, true);
            }
            // 큰길 망은 닿는 순간 도착이라 가로지를 수 없고, 조각 자기 길은 다중 출발이라 되돌아 지나가지 않는다 → 둘은 안 막는다.
            foreach (var i in src) blocked[i] = false;
            foreach (var i in dst) blocked[i] = false;
            if (src.Count == 0 || dst.Count == 0) return false;

            var dist = Enumerable.Repeat(double.PositiveInfinity, gw * gh).ToArray();
            var prev = Enumerable.Repeat(-1, gw * gh).ToArray();
            var pq = new SortedSet<(double, int)>();
            foreach (var i in src.OrderBy(x => x)) { dist[i] = 0; pq.Add((0, i)); }
            int goal = -1;
            int[] dc = { 1, -1, 0, 0, 1, 1, -1, -1 }, dr = { 0, 0, 1, -1, 1, -1, 1, -1 };
            while (pq.Count > 0)
            {
                var top = pq.Min; pq.Remove(top); int u = top.Item2;
                if (dst.Contains(u) && !src.Contains(u)) { goal = u; break; }
                int uc = u % gw, ur = u / gw;
                for (int k = 0; k < 8; k++)
                {
                    int vc = uc + dc[k], vr = ur + dr[k];
                    if (vc < 0 || vr < 0 || vc >= gw || vr >= gh) continue;
                    int v = vr * gw + vc;
                    if (blocked[v]) continue;
                    double l = k < 4 ? gs : gs * Math.Sqrt(2), g = Math.Abs(hgt[v] - hgt[u]) / l;
                    double nd = dist[u] + l * (1 + s.connectorGradePenalty * Math.Max(0, g - s.generatedMaxGrade));
                    if (nd < dist[v] - 1e-9) { if (!double.IsPositiveInfinity(dist[v])) pq.Remove((dist[v], v)); dist[v] = nd; prev[v] = u; pq.Add((nd, v)); }
                }
            }
            if (goal < 0) return false;
            var cells = new List<int>();
            for (int v = goal; v >= 0; v = prev[v]) cells.Add(v);
            cells.Reverse();                                   // 조각 쪽 → 큰길 쪽
            var startRef = near[cells[0]]; var endRef = near[goal];
            var startEdge = edges[startRef.edge]; var endEdge = edges[endRef.edge];
            var pts = new List<Vec2> { startEdge.pts[startRef.idx] };
            for (int k = 1; k + 1 < cells.Count; k++) pts.Add(C(cells[k]));
            pts.Add(endEdge.pts[endRef.idx]);
            int na = SplitAt(edges, edges.IndexOf(startEdge), startRef.idx, res);
            int nb = SplitAt(edges, edges.IndexOf(endEdge), endRef.idx, res);
            pts[0] = res.nodes[na]; pts[pts.Count - 1] = res.nodes[nb];
            edges.Add(new SplineEdge { a = na, b = nb, pts = pts, highway = "connector", connector = true }); res.connectorRaw.Add(new List<Vec2>(pts));
            len = SplineEdge.PolyLength(pts);
            return true;
        }

        /// <summary>조각의 점 중 큰길 망 점과 가장 가까운 쌍을 직선으로 잇는다(바다를 지나면 다음 후보). 큰길 쪽 선은 그 점에서 나눈다.</summary>
        static bool TryConnect(List<SplineEdge> edges, List<SplineEdge> other, int[] comp, int main, SplineTownResult res, Func<Vec2, bool> isSea, IHeightField H, SplineTownSettings s, out double len)
        {
            len = 0;
            var cands = new List<(double d, int oe, int oi, int me, int mi)>();
            var mainEdges = Enumerable.Range(0, edges.Count).Where(i => comp[edges[i].a] == main).ToList();
            for (int oe = 0; oe < other.Count; oe++)
                for (int oi = 0; oi < other[oe].pts.Count; oi += Math.Max(1, other[oe].pts.Count / 40))
                    foreach (var me in mainEdges)
                        for (int mi = 0; mi < edges[me].pts.Count; mi++)
                            cands.Add((SplineEdge.Dist(other[oe].pts[oi], edges[me].pts[mi]), oe, oi, me, mi));
            // 스펙 §2.2 "최소 경사로 잇는다": 높이차/거리 가 큰길 경사 상한 안인 쌍을 먼저(가까운 순), 없으면 경사가 가장 낮은 쌍
            double Grade((double d, int oe, int oi, int me, int mi) c)
            {
                var p0 = other[c.oe].pts[c.oi]; var q0 = edges[c.me].pts[c.mi];
                return c.d < 1e-6 ? 0 : Math.Abs(H.HeightAt(p0.x, p0.z) - H.HeightAt(q0.x, q0.z)) / c.d;
            }
            // 비용 = 거리 + 30 × (경사 상한을 넘는 높이차 m). 짧고 완만한 잇기를 고른다.
            double Cost((double d, int oe, int oi, int me, int mi) c) => c.d + 30 * Math.Max(0, Grade(c) - s.mainMaxGrade) * c.d;
            var ordered = cands.Where(c => c.d > 0.5).OrderBy(Cost).ThenBy(c => c.me).ThenBy(c => c.mi);
            foreach (var c in ordered.Take(400))
            {
                var p = other[c.oe].pts[c.oi]; var q = edges[c.me].pts[c.mi];
                if (CrossesAnyEdge(p, q, edges)) continue;   // 다른 큰길과 높이 다른 채 겹치는(교차점 없는) 잇기 금지
                bool crossesSea = false;
                int n = Math.Max(2, (int)(c.d / 2));
                for (int k = 0; k <= n && !crossesSea; k++) crossesSea = isSea(new Vec2(p.x + (q.x - p.x) * k / n, p.z + (q.z - p.z) * k / n));
                if (crossesSea) continue;
                int na = SplitAt(edges, edges.IndexOf(other[c.oe]), c.oi, res);
                int nb = SplitAt(edges, c.me, c.mi, res);
                edges.Add(new SplineEdge { a = na, b = nb, pts = new List<Vec2> { res.nodes[na], res.nodes[nb] }, highway = "connector", connector = true }); res.connectorRaw.Add(new List<Vec2> { res.nodes[na], res.nodes[nb] });
                len = c.d;
                return true;
            }
            return false;
        }

        /// <summary>선분 p-q 가 어떤 큰길 선분과 (양 끝 1m 밖에서) 교차하는가.</summary>
        static bool CrossesAnyEdge(Vec2 p, Vec2 q, List<SplineEdge> edges)
        {
            double len = SplineEdge.Dist(p, q);
            if (len < 2) return false;
            // 끝 1m 씩 뺀 안쪽 선분만 검사 (자기 끝점이 붙는 선과의 접촉은 허용)
            var a = new Vec2(p.x + (q.x - p.x) / len, p.z + (q.z - p.z) / len);
            var b = new Vec2(q.x - (q.x - p.x) / len, q.z - (q.z - p.z) / len);
            foreach (var e in edges)
                for (int i = 0; i + 1 < e.pts.Count; i++)
                    if (SegIntersect(a, b, e.pts[i], e.pts[i + 1])) return true;
            return false;
        }

        static bool SegIntersect(Vec2 p1, Vec2 p2, Vec2 p3, Vec2 p4)
        {
            double d1 = Cross(p3, p4, p1), d2 = Cross(p3, p4, p2), d3 = Cross(p1, p2, p3), d4 = Cross(p1, p2, p4);
            return ((d1 > 0) != (d2 > 0)) && ((d3 > 0) != (d4 > 0));
        }

        static double Cross(Vec2 a, Vec2 b, Vec2 c) => (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);

        /// <summary>edges[ei] 를 점 i 에서 나눠 그 자리를 노드로 만든다(끝점이면 그대로). 노드 번호 반환.</summary>
        static int SplitAt(List<SplineEdge> edges, int ei, int i, SplineTownResult res)
        {
            var e = edges[ei];
            if (i <= 0) return e.a;
            if (i >= e.pts.Count - 1) return e.b;
            int n = res.nodes.Count; res.nodes.Add(e.pts[i]);
            var tail = new SplineEdge { a = n, b = e.b, pts = e.pts.GetRange(i, e.pts.Count - i), highway = e.highway, connector = e.connector };
            e.pts = e.pts.GetRange(0, i + 1); e.b = n;
            edges.Add(tail);
            return n;
        }

        /// <summary>땅 경계에서 잘린 끝(길이 땅 밖으로 나감) — 교차점이 아니니 필렛 공간이 모자라면 토막을 잘라도 된다.</summary>
        static bool OnBoundary(Vec2 p, SplineTownSettings s) => p.x < 1 || p.z < 1 || p.x > s.boundsXMeters - 1 || p.z > s.boundsZMeters - 1;

        public class FilletStats { public int arcs, merged, split, reduced, uturn, trimmed, straightened, endCut; public List<string> reducedAt = new List<string>(); }

        /// <summary>
        /// 15a-2 헤어핀 패드: 60m 안에서 150° 넘게 되돌아가는 U자(지그재그 길·상자 경계 잇기) 중 다리 사이 반쪽 폭 &lt; 큰길 반경인 곳을
        /// 반경 padR 원호(끝 = U자 끝점, 또는 padTips 의 15m 안 점) + 양쪽 반경 transR 이음 원호로 바꾼다. 필렛은 이 점들을 그대로 둔다(pads).
        /// 필렛에 맡기면 꼭짓점 합침이 거의 평행한 두 다리의 먼 교점으로 끝을 밀어(48m) 이탈했다.
        /// </summary>
        public static List<Vec2> HairpinPads(List<Vec2> P, double padR0, double transR, double maxDev, Vec2[] tips, HashSet<(long, long)> pads, List<string> log)
        {
            if (P.Count < 3 || SplineEdge.PolyLength(P) < 20) return P;
            var q = Resample(P, 1.0); int n = q.Count;
            var arc = new double[n]; for (int k = 1; k < n; k++) arc[k] = arc[k - 1] + SplineEdge.Dist(q[k - 1], q[k]);
            var head = new double[n]; for (int k = 0; k + 1 < n; k++) head[k] = Math.Atan2(q[k + 1].z - q[k].z, q[k + 1].x - q[k].x); head[n - 1] = head[n - 2];
            var cum = new double[n]; for (int k = 1; k < n; k++) { double d = head[k] - head[k - 1]; while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI; cum[k] = cum[k - 1] + d; }
            var outP = new List<Vec2>(); double doneArc = -1; int last = 0;
            Vec2 Add(Vec2 a, Vec2 b, double t) => new Vec2(a.x + b.x * t, a.z + b.z * t);
            double Dot(Vec2 a, Vec2 b) => a.x * b.x + a.z * b.z;
            Vec2 Sub(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.z - b.z);
            var segs = new List<(double sA, double sB, List<Vec2> pts)>();
            for (int i = 0; i < n; i++)
            {
                if (arc[i] <= doneArc) continue;
                int j = -1;
                for (int k = i + 1; k < n && arc[k] - arc[i] <= 60; k++) if (Math.Abs(cum[k] - cum[i]) >= 150 * Math.PI / 180) { j = k; break; }
                if (j < 0) continue;
                var M = new Vec2((q[i].x + q[j].x) / 2, (q[i].z + q[j].z) / 2);
                int ti = i; double best = -1; for (int k = i; k <= j; k++) { double d = SplineEdge.Dist(q[k], M); if (d > best) { best = d; ti = k; } }
                var T = q[ti];
                if (tips != null) foreach (var tp in tips) if (Enumerable.Range(i, j - i + 1).Any(k => SplineEdge.Dist(q[k], tp) <= 15)) T = tp;
                double ul = SplineEdge.Dist(T, M); if (ul < 1e-6) continue;
                var u = new Vec2((T.x - M.x) / ul, (T.z - M.z) / ul); var w = new Vec2(-u.z, u.x);
                bool forced = tips != null && tips.Any(tp => SplineEdge.Dist(T, tp) < 1e-6);
                (List<Vec2> pts, double sA, double sB, double dev, double aMin, string info) TryPad(double padR, Vec2 u)
                {
                    var C = Add(T, u, -padR);
                    // 다리 점 찾기: 끝에서 뒤로(dir −1 = 들어오는 다리, +1 = 나가는 다리) 걸어 축 좌표 ≤ x 인 첫 점
                    (Vec2 p, double s, int idx) Leg(int dir, double x)
                    {
                        for (int k = ti; k >= 1 && k < n - 1; k += dir)
                        {
                            int k2 = k + dir; double x1 = Dot(Sub(q[k], C), u), x2 = Dot(Sub(q[k2], C), u);
                            if (x1 >= x && x2 < x) { double t = (x1 - x) / (x1 - x2); return (new Vec2(q[k].x + (q[k2].x - q[k].x) * t, q[k].z + (q[k2].z - q[k].z) * t), arc[k] + (arc[k2] - arc[k]) * t, k); }
                            if (Math.Abs(arc[k2] - arc[ti]) > 90) break;
                        }
                        return (new Vec2(0, 0), -1, -1);
                    }
                    var side = new List<(Vec2 L, double s, Vec2 Ct, Vec2 K, Vec2 d)>(); bool ok = true; double aMin = 1e9; var dbg = new List<string>();
                    // 다리 k 의 국소 방향(끝 쪽을 향함, ±3m 평균)
                    Vec2 LegDir(int k, int dir) { int k0 = Math.Max(0, k - 3), k1 = Math.Min(n - 1, k + 3); var d = dir < 0 ? Sub(q[k1], q[k0]) : Sub(q[k0], q[k1]); double l = Math.Sqrt(Dot(d, d)); return l < 1e-9 ? u : new Vec2(d.x / l, d.z / l); }
                    foreach (int dir in new[] { -1, 1 })
                    {
                        var lp = Leg(dir, -padR); if (lp.idx < 0) { ok = false; break; }
                        int kq = lp.idx; Vec2 L = default, Ct = default, K = default, dL = u; double a = 0, sL = 0; bool outward = true;
                        for (int it = 0; it < 6; it++)
                        {
                            // 다리 = q[kq] 를 지나는 직선(방향 d, 끝 쪽). 전환 원(transR)이 이 직선과 큰 원(C, padR)에 동시에 접하게.
                            var P0 = q[kq]; var d = LegDir(kq, dir); var pn = new Vec2(-d.z, d.x); if (Dot(Sub(C, P0), pn) < 0) pn = new Vec2(-pn.x, -pn.z);
                            a = Dot(Sub(C, P0), pn); double tc = Dot(Sub(C, P0), d), h, sg;
                            if (a < padR) { outward = true; h = (padR + transR) * (padR + transR) - (transR + a) * (transR + a); sg = -1; }
                            else { outward = false; h = (transR - padR) * (transR - padR) - (transR - a) * (transR - a); sg = 1; }
                            if (h < 0) { ok = false; break; }
                            double tt = tc - Math.Sqrt(h);
                            L = Add(P0, d, tt); Ct = Add(L, pn, sg * transR); dL = d;
                            sL = arc[kq] + (dir < 0 ? tt : -tt);
                            // 다음 반복: L 에 가장 가까운 다리 점
                            int kb = kq; double bd = 1e9; for (int k = ti; k >= 0 && k < n && Math.Abs(arc[k] - arc[ti]) <= 90; k += dir) { double dd = SplineEdge.Dist(q[k], L); if (dd < bd) { bd = dd; kb = k; } }
                            if (kb == kq) break; kq = kb;
                        }
                        if (!ok) break;
                        aMin = Math.Min(aMin, a);
                        var cc = Sub(Ct, C); double cl = Math.Sqrt(Dot(cc, cc)); if (cl < 1e-6) { ok = false; break; }
                        K = outward ? Add(C, cc, padR / cl) : Add(C, cc, -padR / cl);
                        side.Add((L, sL, Ct, K, dL)); dbg.Add($"{(outward ? "밖" : "안")}{a:0.0}");
                    }
                    if (!ok || side.Count < 2) return (null, 0, 0, 1e9, aMin, "다리 못 찾음");
                    // 접선을 이어 가며 원호를 그린다: 들어오는 다리 방향 → 전환 A → 큰 원 → 전환 B. 나가는 다리 방향과 12° 넘게 어긋나면 버림.
                    double Cr(Vec2 r0, Vec2 v0) => r0.x * v0.z - r0.z * v0.x;
                    int Sense(Vec2 c0, Vec2 p0, Vec2 dv) => Cr(Sub(p0, c0), dv) >= 0 ? 1 : -1;
                    Vec2 TanAt(Vec2 c0, Vec2 p0, int sg0) { var r0 = Sub(p0, c0); double l0 = Math.Sqrt(Dot(r0, r0)); return sg0 > 0 ? new Vec2(-r0.z / l0, r0.x / l0) : new Vec2(r0.z / l0, -r0.x / l0); }
                    List<Vec2> ArcPts(Vec2 c0, double r0, Vec2 p0, Vec2 p1, int sg0, out double sw)
                    {
                        double b0 = Math.Atan2(p0.z - c0.z, p0.x - c0.x), b1 = Math.Atan2(p1.z - c0.z, p1.x - c0.x); sw = b1 - b0;
                        while (sg0 > 0 && sw < 0) sw += 2 * Math.PI; while (sg0 > 0 && sw >= 2 * Math.PI) sw -= 2 * Math.PI;
                        while (sg0 < 0 && sw > 0) sw -= 2 * Math.PI; while (sg0 < 0 && sw <= -2 * Math.PI) sw += 2 * Math.PI;
                        var o = new List<Vec2>(); int stp = Math.Max(1, (int)Math.Ceiling(Math.Abs(sw) * r0));
                        for (int k = 0; k <= stp; k++) { double an = b0 + sw * k / stp; o.Add(new Vec2(c0.x + Math.Cos(an) * r0, c0.z + Math.Sin(an) * r0)); }
                        return o;
                    }
                    var A = side[0]; var B = side[1];
                    int sA = Sense(A.Ct, A.L, A.d); var trA = ArcPts(A.Ct, transR, A.L, A.K, sA, out double swA);
                    var tK = TanAt(A.Ct, A.K, sA);
                    int sC = Sense(C, A.K, tK); var big = ArcPts(C, padR, A.K, B.K, sC, out double sweep);
                    var tK2 = TanAt(C, B.K, sC);
                    int sB = Sense(B.Ct, B.K, tK2); var trB = ArcPts(B.Ct, transR, B.K, B.L, sB, out double swB);
                    var tL = TanAt(B.Ct, B.L, sB);
                    double miss = Math.Acos(Math.Max(-1, Math.Min(1, -Dot(tL, B.d)))) * 180 / Math.PI;
                    dbg.Add($"{swA * 180 / Math.PI:0}°/{swB * 180 / Math.PI:0}° 끝 어긋남 {miss:0}°");
                    var pts = new List<Vec2>(trA); pts.AddRange(big.Skip(1)); pts.AddRange(trB.Skip(1));
                    if (miss > 12 || Math.Abs(swA) > Math.PI || Math.Abs(swB) > Math.PI) return (null, 0, 0, 1e9, aMin, $"접선 안 맞음 {string.Join(",", dbg)}");
                    double dev = 0; var origSec = q.Where((_, k) => arc[k] >= side[0].s - 1 && arc[k] <= side[1].s + 1).ToList(); foreach (var v in pts) dev = Math.Max(dev, DistToPoly(v, origSec));
                    return (pts, side[0].s, side[1].s, dev, aMin, $"R{padR:0.0} 원호 {sweep * 180 / Math.PI:0}° {string.Join(",", dbg)}");
                }
                // 반경을 padR..padR+3.5 (0.5 간격) 시험해 원래 선에서 덜 벗어나는 패드를 고른다
                //  다리가 평행하지 않으면(모이는 U자) 축 u 를 끝점 둘레로 ±10·20·30° 돌려 본다
                var bestTry = TryPad(padR0, u);
                if (bestTry.pts != null && bestTry.aMin >= padR0 && !forced) continue;   // 다리 반폭이 패드 반경 이상 — 필렛(8m)이 그대로 들어간다
                string fail = bestTry.pts == null ? bestTry.info : "";
                // 강제 패드(상자 경계 잇기)는 끝점을 실제로 지나는 것(0.5m 안)을 먼저 — 비대칭 원호는 끝점을 비켜 간다(R6 중간점 1.2m)
                bool Better((List<Vec2> pts, double sA, double sB, double dev, double aMin, string info) a, (List<Vec2> pts, double sA, double sB, double dev, double aMin, string info) b)
                {
                    if (b.pts == null) return true;
                    if (forced) { bool ma = DistToPoly(T, a.pts) <= 0.5, mb = DistToPoly(T, b.pts) <= 0.5; if (ma != mb) return ma; }
                    return a.dev < b.dev;
                }
                foreach (double rot in new[] { 0.0, 10, -10, 20, -20, 30, -30 })
                for (double R = padR0 + (rot == 0 ? 0.5 : 0); R <= padR0 + 3.51; R += 0.5) { double ca = Math.Cos(rot * Math.PI / 180), sa = Math.Sin(rot * Math.PI / 180); var r1 = TryPad(R, new Vec2(u.x * ca - u.z * sa, u.x * sa + u.z * ca)); if (r1.pts == null) { if (fail == "") fail = r1.info; continue; } if (Better(r1, bestTry)) bestTry = r1; }
                if (bestTry.pts == null) { log?.Add($"({T.x:0.0},{T.z:0.0}) 패드 못 만듦 {fail}"); doneArc = arc[j]; continue; }
                bool keep = bestTry.dev <= maxDev || forced;
                log?.Add($"({T.x:0.0},{T.z:0.0}) 이탈 {bestTry.dev:0.0}m {bestTry.info}{(forced ? " 강제" : "")}{(keep ? "" : " 버림")}");
                if (!keep || bestTry.sA <= doneArc || bestTry.sA >= bestTry.sB) { doneArc = arc[j]; continue; }
                segs.Add((bestTry.sA, bestTry.sB, bestTry.pts)); doneArc = bestTry.sB;
            }
            if (segs.Count == 0) return P;
            // 원래 꼭짓점 + 패드 점 끼우기(원래 선 호 길이 기준)
            var parc = new double[P.Count]; for (int k = 1; k < P.Count; k++) parc[k] = parc[k - 1] + SplineEdge.Dist(P[k - 1], P[k]);
            int si = 0, kk = 0;
            while (kk < P.Count)
            {
                if (si < segs.Count && parc[kk] > segs[si].sA)
                {
                    foreach (var v in segs[si].pts) { outP.Add(v); pads.Add(PadKey(v)); }
                    while (kk < P.Count && parc[kk] <= segs[si].sB) kk++;
                    si++; continue;
                }
                outP.Add(P[kk]); kk++;
            }
            for (; si < segs.Count; si++) foreach (var v in segs[si].pts) { outP.Add(v); pads.Add(PadKey(v)); }
            return outP;
        }
        public static (long, long) PadKey(Vec2 v) => ((long)Math.Round(v.x * 1000), (long)Math.Round(v.z * 1000));

        /// <summary>Douglas–Peucker 단순화(끝점 유지).</summary>
        public static List<Vec2> Simplify(List<Vec2> p, double eps)
        {
            if (p.Count < 3) return new List<Vec2>(p);
            var keep = new bool[p.Count]; keep[0] = keep[p.Count - 1] = true;
            var stack = new Stack<(int, int)>(); stack.Push((0, p.Count - 1));
            while (stack.Count > 0)
            {
                var (a, b) = stack.Pop();
                double best = -1; int bi = -1;
                for (int i = a + 1; i < b; i++) { double d = DistToSeg(p[i], p[a], p[b], out _); if (d > best) { best = d; bi = i; } }
                if (best > eps) { keep[bi] = true; stack.Push((a, bi)); stack.Push((bi, b)); }
            }
            return p.Where((v, i) => keep[i]).ToList();
        }

        /// <summary>
        /// 필렛(§2.9): 꺾인 꼭짓점마다 반경 R 원호를 끼운다 — 최소 반경이 정의상 보장되고 결정적.
        ///  · R = 꺾임각 ≥ hairpinTurn(120°) 이면 hairpinR(8m), 아니면 baseR(12m).
        ///  · 이웃 두 원호의 접선 길이 합이 선분보다 길면(겹침) R 을 줄이지 않고 두 꼭짓점을 한 꼭짓점(앞뒤 선의 교점)으로 합친다(같은 방향 굽이만).
        ///  · R=hairpinR 에서 이탈이 hairpinMaxDeviation 을 넘는 급한 꺾임(약 127° 초과)은 꼭짓점을 둘로 나눠 두 굽이로.
        ///  · 그래도 안 되는 곳(S자 겹침·끝점(교차점 고정) 앞 공간 부족)은 들어가는 만큼 R 을 줄이고 센다. 거의 되돌아가는 U자(≥175°)는 그대로 두고 센다.
        /// 끝점은 고정. 결과는 직선 + 원호 꺾은선(원호는 약 1m 간격).
        /// </summary>
        public static List<Vec2> Fillet(List<Vec2> raw, double baseR, double hairpinR, double simplifyEps, SplineTownSettings s, FilletStats st, bool freeStart = false, bool freeEnd = false, double straightenDev = -1, HashSet<(long, long)> pads = null)
        {
            if (straightenDev < 0) straightenDev = s.maxDeviationMeters;
            // 15a-2: 헤어핀 패드 점은 그대로 두고, 그 사이 토막만 필렛(패드 끝점 = 고정 끝점)
            if (pads != null && pads.Count > 0 && raw.Any(q => pads.Contains(PadKey(q))))
            {
                var o2 = new List<Vec2>(); int i0 = 0;
                void Put(IEnumerable<Vec2> src) { foreach (var q in src) if (o2.Count == 0 || SplineEdge.Dist(o2[o2.Count - 1], q) > 0.05) o2.Add(q); }
                while (i0 < raw.Count)
                {
                    bool pd = pads.Contains(PadKey(raw[i0])); int j0 = i0;
                    while (j0 + 1 < raw.Count && pads.Contains(PadKey(raw[j0 + 1])) == pd) j0++;
                    if (pd) Put(raw.GetRange(i0, j0 - i0 + 1));
                    else
                    {
                        int a0 = Math.Max(0, i0 - 1), b0 = Math.Min(raw.Count - 1, j0 + 1);
                        var part = raw.GetRange(a0, b0 - a0 + 1);
                        Put(part.Count >= 3 ? Fillet(part, baseR, hairpinR, simplifyEps, s, st, freeStart && a0 == 0, freeEnd && b0 == raw.Count - 1, straightenDev) : part);
                    }
                    i0 = j0 + 1;
                }
                return o2;
            }
            var v = Simplify(raw, simplifyEps);
            if (v.Count < 3) return new List<Vec2>(raw);
            // 나눠서 생긴 두 꼭짓점 표시(같은 번호) — 다시 합치지 않는다(나눔↔합침 왕복 방지)
            var tag = Enumerable.Repeat(0, v.Count).ToList(); int nextTag = 1;
            double HairTurn = s.hairpinTurnDeg * Math.PI / 180;
            double splitTurn = 2 * Math.Acos(hairpinR / (hairpinR + s.hairpinMaxDeviationMeters));   // R(1/cos(θ/2) − 1) = dev
            (double, double) Dir(Vec2 a, Vec2 b) { double dx = b.x - a.x, dz = b.z - a.z, l = Math.Sqrt(dx * dx + dz * dz); return l < 1e-9 ? (0, 0) : (dx / l, dz / l); }
            double Turn(int k) { var (ax, az) = Dir(v[k - 1], v[k]); var (bx, bz) = Dir(v[k], v[k + 1]); double c = Math.Max(-1, Math.Min(1, ax * bx + az * bz)); return Math.Acos(c); }
            double Side(int k) { var (ax, az) = Dir(v[k - 1], v[k]); var (bx, bz) = Dir(v[k], v[k + 1]); return Math.Sign(ax * bz - az * bx); }
            // 2% 여유: 원호를 2m 로 다시 찍을 때 현 때문에 재는 반경이 R 보다 조금 작게 나온다
            double R(int k) => (Turn(k) >= HairTurn ? hairpinR : baseR) * 1.02;
            double Tan(int k) { double t = Turn(k); return t < 1e-6 ? 0 : R(k) * Math.Tan(t / 2); }

            for (int guard = 0; guard < 400; guard++)
            {
                bool changed = false;
                // 거의 일직선 꼭짓점 제거
                for (int k = 1; k + 1 < v.Count; k++) if (Turn(k) < 0.5 * Math.PI / 180) { v.RemoveAt(k); tag.RemoveAt(k); changed = true; break; }
                if (changed) continue;
                // 막다른 끝(교차점 아님) 바로 앞 굽이에 원호 자리가 모자라면 반경을 줄이지 말고 끝 토막을 잘라낸다
                if (freeStart && v.Count > 3 && Tan(1) > SplineEdge.Dist(v[0], v[1]) + 1e-6) { v.RemoveAt(0); tag.RemoveAt(0); st.trimmed++; continue; }
                if (freeEnd && v.Count > 3 && Tan(v.Count - 2) > SplineEdge.Dist(v[v.Count - 2], v[v.Count - 1]) + 1e-6) { v.RemoveAt(v.Count - 1); tag.RemoveAt(tag.Count - 1); st.trimmed++; continue; }
                // 급한 꺾임 → 두 굽이
                for (int k = 1; k + 1 < v.Count && !changed; k++)
                {
                    double t = Turn(k);
                    if (t <= splitTurn || t >= 175 * Math.PI / 180) continue;
                    var (ax, az) = Dir(v[k - 1], v[k]); var (bx, bz) = Dir(v[k], v[k + 1]);
                    double half = hairpinR * Math.Tan(t / 4), tt = half / Math.Cos(t / 2) * 1.05;
                    if (tt >= SplineEdge.Dist(v[k - 1], v[k]) * 0.95 || tt >= SplineEdge.Dist(v[k], v[k + 1]) * 0.95) continue;
                    var v1 = new Vec2(v[k].x - ax * tt, v[k].z - az * tt); var v2 = new Vec2(v[k].x + bx * tt, v[k].z + bz * tt);
                    v[k] = v1; v.Insert(k + 1, v2); tag[k] = nextTag; tag.Insert(k + 1, nextTag); nextTag++; st.split++; changed = true;
                }
                if (changed) continue;
                // 겹치는 이웃 원호 → 한 꼭짓점으로 합침
                for (int k = 1; k + 2 < v.Count && !changed; k++)
                {
                    double L = SplineEdge.Dist(v[k], v[k + 1]);
                    if (Tan(k) + Tan(k + 1) <= L + 1e-6) continue;
                    if (Side(k) != Side(k + 1)) continue;                     // S자는 못 합침
                    if (tag[k] != 0 && tag[k] == tag[k + 1]) continue;        // 방금 나눈 짝은 다시 합치지 않음
                    // 선 (v[k−1]→v[k]) 와 선 (v[k+1]→v[k+2]) 의 교점
                    var p1 = v[k - 1]; var p2 = v[k]; var q1 = v[k + 1]; var q2 = v[k + 2];
                    double d = (p2.x - p1.x) * (q2.z - q1.z) - (p2.z - p1.z) * (q2.x - q1.x);
                    if (Math.Abs(d) < 1e-9) continue;
                    double ta = ((q1.x - p1.x) * (q2.z - q1.z) - (q1.z - p1.z) * (q2.x - q1.x)) / d;
                    var x = new Vec2(p1.x + (p2.x - p1.x) * ta, p1.z + (p2.z - p1.z) * ta);
                    if (ta < 1 - 1e-9 || SplineEdge.Dist(x, v[k]) > 4 * L + 20) continue;   // 뒤쪽 교점·너무 먼 교점이면 안 합침
                    v[k] = x; v.RemoveAt(k + 1); tag[k] = 0; tag.RemoveAt(k + 1); st.merged++; changed = true;
                }
                if (changed) continue;
                // 원호 자리가 모자란 꼭짓점(S자·교차점 끝 가까이) → 반경을 줄이기 전에 꼭짓점 하나를 빼서 편다(원래 선 이탈 ≤ 상한일 때만)
                for (int k = 1; k + 1 < v.Count && !changed; k++)
                {
                    double roomA = SplineEdge.Dist(v[k - 1], v[k]) - (k - 1 >= 1 ? Tan(k - 1) : 0);
                    double roomB = SplineEdge.Dist(v[k], v[k + 1]) - (k + 1 < v.Count - 1 ? Tan(k + 1) : 0);
                    if (Tan(k) <= Math.Min(roomA, roomB) + 1e-6) continue;
                    var tries = new List<int> { k };
                    if (roomA < roomB && k - 1 >= 1) tries.Add(k - 1); else if (k + 1 < v.Count - 1) tries.Add(k + 1);
                    foreach (int r in tries.OrderBy(q => Turn(q)))
                    {
                        var cand = new List<Vec2>(v); cand.RemoveAt(r);
                        if (cand.Count < 2) continue;
                        double dev = 0; foreach (var q in raw) dev = Math.Max(dev, DistToPoly(q, cand));
                        foreach (var q in cand) dev = Math.Max(dev, DistToPoly(q, raw));
                        if (dev > straightenDev) continue;
                        if (cand.Any(q => q.x < s.HalfShoulder || q.z < s.HalfShoulder || q.x > s.boundsXMeters - s.HalfShoulder || q.z > s.boundsZMeters - s.HalfShoulder) && !v.Any(q => q.x < s.HalfShoulder || q.z < s.HalfShoulder || q.x > s.boundsXMeters - s.HalfShoulder || q.z > s.boundsZMeters - s.HalfShoulder)) continue;
                        v.RemoveAt(r); tag.RemoveAt(r); st.straightened++; changed = true; break;
                    }
                }
                if (!changed) break;
            }

            // 원호 만들기: 각 꼭짓점의 R 을 정하고(겹침이 남으면 줄임), 접점 사이를 원호로
            int n = v.Count;
            var rr = new double[n]; var tl = new double[n];
            for (int k = 1; k + 1 < n; k++) { rr[k] = R(k); tl[k] = Tan(k); }
            for (int k = 1; k + 1 < n; k++)
            {
                if (Turn(k) >= 175 * Math.PI / 180) { st.uturn++; rr[k] = 0; tl[k] = 0; continue; }
                double room = Math.Min(SplineEdge.Dist(v[k - 1], v[k]) - (k - 1 >= 1 ? tl[k - 1] : 0), SplineEdge.Dist(v[k], v[k + 1]) * (k + 1 < n - 1 ? 0.5 : 1.0));
                if (tl[k] > room && room > 0) { double f = room / tl[k]; rr[k] *= f; tl[k] = room; st.reduced++; st.reducedAt.Add($"({v[k].x:0},{v[k].z:0}) R{rr[k]:0.0}"); }
            }
            var o = new List<Vec2> { v[0] };
            for (int k = 1; k + 1 < n; k++)
            {
                if (tl[k] <= 1e-6) { o.Add(v[k]); continue; }
                var (ax, az) = Dir(v[k - 1], v[k]); var (bx, bz) = Dir(v[k], v[k + 1]);
                var pin = new Vec2(v[k].x - ax * tl[k], v[k].z - az * tl[k]);
                double side = Side(k);
                // 원 중심 = 접점에서 안쪽 법선 방향 R
                double nx = -az * side, nz = ax * side;
                var c = new Vec2(pin.x + nx * rr[k], pin.z + nz * rr[k]);
                double a0 = Math.Atan2(pin.z - c.z, pin.x - c.x), turn = Turn(k) * side;
                int steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(turn) * rr[k] / 1.0));
                for (int j = 0; j <= steps; j++)
                {
                    double a = a0 + turn * j / steps;
                    o.Add(new Vec2(c.x + Math.Cos(a) * rr[k], c.z + Math.Sin(a) * rr[k]));
                }
                st.arcs++;
            }
            o.Add(v[n - 1]);
            // 중복 점 제거
            var clean = new List<Vec2> { o[0] };
            foreach (var q in o.Skip(1)) if (SplineEdge.Dist(q, clean[clean.Count - 1]) > 0.05) clean.Add(q);
            return clean;
        }

        /// <summary>선분 (x0,z0)→(x1,z1) 이 지형 격자선(x·z = 칸 배수)과 대각선(x−z = 칸 배수)을 지나는 매개변수 t(0·1 포함, 정렬). 그 점마다 정점을 두면 선이 지형 삼각형 면에 정확히 놓인다(M1).</summary>
        public static List<double> GridCrossings(TerrainGrid g, double x0, double z0, double x1, double z1)
        {
            var ts = new List<double> { 0, 1 };
            void Lines(double f0, double f1)
            {
                if (Math.Abs(f1 - f0) < 1e-9) return;
                double lo = Math.Min(f0, f1) / g.step, hi = Math.Max(f0, f1) / g.step;
                for (double k = Math.Ceiling(lo); k <= Math.Floor(hi); k++)
                {
                    double t = (k * g.step - f0) / (f1 - f0);
                    if (t > 1e-4 && t < 1 - 1e-4) ts.Add(t);
                }
            }
            Lines(x0, x1); Lines(z0, z1); Lines(x0 - z0, x1 - z1);
            ts.Sort();
            for (int k = ts.Count - 1; k > 0; k--) if (ts[k] - ts[k - 1] < 1e-4) ts.RemoveAt(k);
            return ts;
        }

        /// <summary>교차점 원판 반경(§2.4: 넓은 쪽 폭 8m × 0.75).</summary>
        public static double JunctionRadius(SplineTownSettings s) => (s.HalfSection * 2) * s.junctionDiscFactor;
        /// <summary>교차점에서 붙는 길의 보도·연석·어깨·중앙선을 끊는 거리(중심선 따라) — 차로 가장자리가 원판 둘레에 닿는 곳.</summary>
        public static double JunctionClip(SplineTownSettings s) { double r = JunctionRadius(s), hc = s.HalfCarriageway; return Math.Sqrt(Math.Max(0, r * r - hc * hc)); }
        /// <summary>호 길이 위치의 단면 옆 기울기(왼쪽 +). 끊는 점 안쪽은 끊는 점 값, 밖은 bankRunMeters 에 걸쳐 0 으로.</summary>
        public static double BankAt(SplineEdge e, double arc, SplineTownSettings s)
        {
            // 짧은 길에서 한쪽 기울기가 반대쪽 끊는 점까지 남으면 그쪽 판과 정점이 어긋난다 → 두 끊는 점 사이 절반 안에서 0 으로
            double L = e.Length, run = Math.Max(0.1, Math.Min(s.bankRunMeters, (L - e.clipA - e.clipB) * 0.5)), b = 0;
            if (e.bankA != 0) b += e.bankA * Math.Max(0, Math.Min(1, 1 - (arc - e.clipA) / run));
            if (e.bankB != 0) b += e.bankB * Math.Max(0, Math.Min(1, 1 - (L - arc - e.clipB) / run));
            return b;
        }

        /// <summary>호 길이 위치의 중심선 점·높이·왼쪽 법선(구간 보간).</summary>
        public static (Vec2 p, double y, Vec2 n) Station(SplineEdge e, double arcPos)
        {
            double acc = 0;
            for (int i = 0; i + 1 < e.pts.Count; i++)
            {
                double d = SplineEdge.Dist(e.pts[i], e.pts[i + 1]);
                if (acc + d >= arcPos || i + 2 == e.pts.Count)
                {
                    double t = d > 1e-9 ? Math.Max(0, Math.Min(1, (arcPos - acc) / d)) : 0;
                    var (n0x, n0z) = Normal(e.pts, i); var (n1x, n1z) = Normal(e.pts, i + 1);
                    double nx = n0x + (n1x - n0x) * t, nz = n0z + (n1z - n0z) * t, nl = Math.Sqrt(nx * nx + nz * nz); if (nl > 1e-9) { nx /= nl; nz /= nl; }
                    double yy = e.y != null && e.y.Count == e.pts.Count ? e.y[i] + (e.y[i + 1] - e.y[i]) * t : 0;
                    return (new Vec2(e.pts[i].x + (e.pts[i + 1].x - e.pts[i].x) * t, e.pts[i].z + (e.pts[i + 1].z - e.pts[i].z) * t), yy, new Vec2(nx, nz));
                }
                acc += d;
            }
            return (e.pts[0], e.y != null && e.y.Count > 0 ? e.y[0] : 0, new Vec2(0, 0));
        }

        /// <summary>
        /// 교차점마다 붙는 큰길을 바깥 방향 각도순으로 모으고, 끊는 거리를 정한다:
        /// 기본 = 차로 가장자리가 원판 둘레에 닿는 곳(√(R²−반차로²)), 이웃 길과 사이각이 좁으면 두 길의 보도 바깥선이 갈라지는 곳까지 더 뒤로(+0.5m, 길 길이의 45%·15m 상한).
        /// </summary>
        static void BuildJunctionLayouts(SplineTownResult res, SplineTownSettings s)
        {
            res.junctionLayouts.Clear();
            double d0 = JunctionClip(s);
            foreach (var n in res.junctions)
            {
                var jl = new JunctionLayout { node = n, y = res.nodeY[n] };
                for (int ei = 0; ei < res.edges.Count; ei++)
                {
                    var e = res.edges[ei]; if (e.alley) continue;
                    foreach (bool atStart in new[] { true, false })
                    {
                        if ((atStart ? e.a : e.b) != n) continue;
                        jl.arms.Add(new JunctionArm { edge = ei, atStart = atStart, clip = d0 });
                    }
                }
                if (jl.arms.Count < 2) continue;
                foreach (var a in jl.arms) Locate(a);
                jl.arms = jl.arms.OrderBy(a => Math.Atan2(a.u.z, a.u.x)).ToList();
                for (int k = 0; k < jl.arms.Count; k++)
                {
                    var A = jl.arms[k]; var B = jl.arms[(k + 1) % jl.arms.Count];
                    double gap = Math.Atan2(B.u.z, B.u.x) - Math.Atan2(A.u.z, A.u.x); while (gap <= 0) gap += 2 * Math.PI;
                    if (gap >= Math.PI) continue;
                    double need = s.HalfSection / Math.Tan(gap / 2) + 0.5;
                    if (need > A.clip) { A.clip = need; }
                    if (need > B.clip) { B.clip = need; }
                }
                foreach (var a in jl.arms)
                {
                    var e = res.edges[a.edge]; a.clip = Math.Min(a.clip, Math.Max(d0, Math.Min(15, e.Length * 0.45)));
                    // 끊는 점을 그 너머 첫 중심선 점에 맞춘다 — 점 사이에서 끊으면 짧은 마지막 구간이 생겨 3점 반경이 작게 잡힘(측정·모양 모두)
                    double acc = 0;
                    for (int i = 1; i < e.pts.Count; i++)
                    {
                        int k = a.atStart ? i : e.pts.Count - 1 - i, kp = a.atStart ? i - 1 : e.pts.Count - i;
                        acc += SplineEdge.Dist(e.pts[kp], e.pts[k]);
                        if (acc >= a.clip - 1e-9) { if (acc <= e.Length * 0.5) a.clip = acc; break; }
                    }
                    Locate(a);
                }
                foreach (var a in jl.arms) { var e = res.edges[a.edge]; if (a.atStart) e.clipA = a.clip; else e.clipB = a.clip; }
                foreach (var a in jl.arms) { var e = res.edges[a.edge]; if (a.atStart) e.bankA = 0; else e.bankB = 0; }
                if (res.junctionPlane.TryGetValue(n, out var pg))
                    foreach (var a in jl.arms)
                    {
                        // 길 왼쪽 법선 = 바깥 기준 왼쪽 법선 × (시작 끝 +1, 끝 끝 −1)
                        double sg = a.atStart ? 1 : -1, nx = a.ccw.x * sg, nz = a.ccw.z * sg;
                        var e = res.edges[a.edge]; double bank = pg.gx * nx + pg.gz * nz;
                        if (a.atStart) e.bankA = bank; else e.bankB = bank;
                    }
                if (jl.arms.All(a => res.edges[a.edge].y != null && res.edges[a.edge].y.Count == res.edges[a.edge].pts.Count)) FinalizePlate(res, jl, s);
                res.junctionLayouts.Add(jl);

                void Locate(JunctionArm a)
                {
                    var e = res.edges[a.edge];
                    var st = Station(e, a.atStart ? a.clip : e.Length - a.clip);
                    a.c = st.p; a.y = e.y != null && e.y.Count == e.pts.Count ? st.y : 0;
                    double ux = st.p.x - res.nodes[n].x, uz = st.p.z - res.nodes[n].z, ul = Math.Sqrt(ux * ux + uz * uz);
                    a.u = ul > 1e-9 ? new Vec2(ux / ul, uz / ul) : new Vec2(1, 0);
                    // 끊는 선(차로 폭)은 길 법선 방향 — 굽은 길에서도 단면과 맞게. 바깥 방향 기준 왼쪽 = 시작 끝은 길 왼쪽, 끝 끝은 길 오른쪽.
                    double sg = a.atStart ? 1 : -1; a.ccw = new Vec2(st.n.x * sg, st.n.z * sg);
                    a.t = new Vec2(a.ccw.z, -a.ccw.x);
                    a.backP.Clear(); a.backN.Clear();
                    int nb = Math.Max(1, (int)Math.Ceiling(a.clip / 1.0));
                    for (int k = 1; k <= nb; k++)
                    {
                        double d = a.clip * (1 - (double)k / nb);
                        var sb = Station(e, a.atStart ? d : e.Length - d);
                        a.backP.Add(sb.p); a.backN.Add(new Vec2(sb.n.x * sg, sb.n.z * sg));
                    }
                }
            }
        }

        /// <summary>두 길 사이 모서리 곡선: A 의 반시계 쪽 오프셋 선과 B 의 시계 쪽 오프셋 선을 잇는 2차 베지어(두 선의 교점이 제어점, 교점이 끊는 점보다 바깥이면 직선).</summary>
        public static List<Vec2> CornerCurve(JunctionArm A, JunctionArm B, double off, int samples)
        {
            var P = new Vec2(A.c.x + A.ccw.x * off, A.c.z + A.ccw.z * off);
            var Q = new Vec2(B.c.x - B.ccw.x * off, B.c.z - B.ccw.z * off);
            // P + s(−tA) = Q + t(−tB)  (끊는 점의 실제 접선 — 휜 길에서 모서리가 차도를 가로지르지 않게)
            double ax = -A.t.x, az = -A.t.z, bx = -B.t.x, bz = -B.t.z;
            double den = ax * (-bz) - az * (-bx);
            Vec2 X = new Vec2((P.x + Q.x) / 2, (P.z + Q.z) / 2);
            if (Math.Abs(den) > 1e-6)
            {
                double rx = Q.x - P.x, rz = Q.z - P.z;
                double sP = (rx * (-bz) - rz * (-bx)) / den, tQ = (ax * rz - az * rx) / den;
                if (sP > 0 && tQ > 0) X = new Vec2(P.x + ax * sP, P.z + az * sP);
            }
            var r = new List<Vec2>();
            for (int k = 0; k <= samples; k++)
            {
                double t = (double)k / samples, m = 1 - t;
                r.Add(new Vec2(m * m * P.x + 2 * m * t * X.x + t * t * Q.x, m * m * P.z + 2 * m * t * X.z + t * t * Q.z));
            }
            return r;
        }

        /// <summary>사이각(A 바깥 방향 → B 바깥 방향, 반시계, 0~2π).</summary>
        public static double ArmGap(JunctionArm A, JunctionArm B)
        {
            double gap = Math.Atan2(B.u.z, B.u.x) - Math.Atan2(A.u.z, A.u.x); while (gap <= 1e-9) gap += 2 * Math.PI; return gap;
        }

        /// <summary>
        /// 교차점 판 모서리 선(오프셋 off): 사이각 &lt; 180° 는 베지어(오프셋 선 교점이 제어점), 그 밖은 A 오프셋 선을 따라 교차점 옆까지 →
        /// 교차점 둘레 반경 off 원호 → B 오프셋 선(뒤로 크게 도는 쪽이 교차점에 붙어 돈다). 점 수는 off 와 무관(오프셋끼리 띠를 잇게).
        /// </summary>
        public static List<Vec2> CornerPath(JunctionArm A, JunctionArm B, Vec2 node, double off, int N)
        {
            double gap = ArmGap(A, B);
            if (gap < Math.PI - 1e-3) return CornerCurve(A, B, off, N);
            // 뒤로 크게 도는 쪽: A 오프셋 선을 길 따라 교차점까지 → 교차점 둘레 원호 → B 오프셋 선을 길 따라 끊는 점까지
            var r = new List<Vec2> { new Vec2(A.c.x + A.ccw.x * off, A.c.z + A.ccw.z * off) };
            for (int k = 0; k < A.backP.Count; k++) r.Add(new Vec2(A.backP[k].x + A.backN[k].x * off, A.backP[k].z + A.backN[k].z * off));
            var nA = A.backN.Count > 0 ? A.backN[A.backN.Count - 1] : A.ccw; var nB = B.backN.Count > 0 ? B.backN[B.backN.Count - 1] : B.ccw;
            double a0 = Math.Atan2(nA.z, nA.x), a1 = Math.Atan2(-nB.z, -nB.x); while (a1 < a0) a1 += 2 * Math.PI;
            int arcN = Math.Max(2, N);
            for (int k = 1; k < arcN; k++) { double a = a0 + (a1 - a0) * k / arcN; r.Add(new Vec2(node.x + Math.Cos(a) * off, node.z + Math.Sin(a) * off)); }
            for (int k = B.backP.Count - 1; k >= 0; k--) r.Add(new Vec2(B.backP[k].x - B.backN[k].x * off, B.backP[k].z - B.backN[k].z * off));
            r.Add(new Vec2(B.c.x - B.ccw.x * off, B.c.z - B.ccw.z * off));
            // 겹친 점(오프셋이 0 근처에서 모임) 제거는 하지 않는다 — 오프셋끼리 점 수가 같아야 띠를 잇는다. 높이는 위치로 정하니 겹친 점은 같은 높이.
            return r;
        }

        /// <summary>
        /// 교차점마다(차례로) 판 평면을 시도: 노드 높이를 지나고 붙는 길 끊는 점 높이에 최소제곱으로 맞춘 기울기(크기 ≤ 패드 경사 × 0.98).
        /// 붙는 길의 끊는 거리(+0.5m) 안 점을 평면 높이로 고정하고, 고정점(노드 끝 포함)에서 경사 상한으로 퍼뜨린 위·아래 한계선 사이에 나머지를 끼운다.
        /// 한 곳이라도 한계선이 엇갈리면(경사 상한에 붙은 비탈 길) 그 교차점은 원래 높이로 되돌리고 원뿔꼴로 둔다.
        /// </summary>
        static (int planes, int cones) TryJunctionPlanes(SplineTownResult res, int[] degN, SplineTownSettings s)
        {
            res.junctionPlane.Clear();
            var fixedPts = new Dictionary<SplineEdge, bool[]>();
            bool[] Fx(SplineEdge e)
            {
                if (!fixedPts.TryGetValue(e, out var f))
                {
                    fixedPts[e] = f = new bool[e.pts.Count];
                    if (degN[e.a] >= 2) f[0] = true;
                    if (degN[e.b] >= 2) f[e.pts.Count - 1] = true;
                }
                return f;
            }
            bool Envelope(SplineEdge e, bool[] fx, bool apply)
            {
                int n = e.pts.Count; var U = new double[n]; var L = new double[n];
                for (int i = 0; i < n; i++) { U[i] = fx[i] ? e.y[i] : double.PositiveInfinity; L[i] = fx[i] ? e.y[i] : double.NegativeInfinity; }
                for (int i = 1; i < n; i++) { double g = GradeLimit(e, i, s) * 0.995 * SplineEdge.Dist(e.pts[i - 1], e.pts[i]); U[i] = Math.Min(U[i], U[i - 1] + g); L[i] = Math.Max(L[i], L[i - 1] - g); }
                for (int i = n - 2; i >= 0; i--) { double g = GradeLimit(e, i + 1, s) * 0.995 * SplineEdge.Dist(e.pts[i], e.pts[i + 1]); U[i] = Math.Min(U[i], U[i + 1] + g); L[i] = Math.Max(L[i], L[i + 1] - g); }
                for (int i = 0; i < n; i++) if (L[i] > U[i] + 1e-9) return false;
                if (apply) for (int i = 0; i < n; i++) if (!fx[i]) e.y[i] = Math.Min(Math.Max(e.y[i], L[i]), U[i]);
                return true;
            }
            int planes = 0, cones = 0;
            foreach (var jl in res.junctionLayouts)
            {
                var c = res.nodes[jl.node]; double cy = res.nodeY[jl.node];
                double sxx = 0, sxz = 0, szz = 0, sxy = 0, szy = 0;
                foreach (var a in jl.arms) { double dx = a.c.x - c.x, dz = a.c.z - c.z, dy = a.y - cy; sxx += dx * dx; sxz += dx * dz; szz += dz * dz; sxy += dx * dy; szy += dz * dy; }
                double det = sxx * szz - sxz * sxz, gx = 0, gz = 0;
                if (Math.Abs(det) > 1e-9) { gx = (sxy * szz - szy * sxz) / det; gz = (szy * sxx - sxy * sxz) / det; }
                double gm = Math.Sqrt(gx * gx + gz * gz), lim = s.hairpinMaxGrade * 0.98;
                if (gm > lim) { gx *= lim / gm; gz *= lim / gm; }
                // 시도: 저장 → 고정 → 한계선 검사
                var saved = jl.arms.Select(a => res.edges[a.edge]).Distinct().ToDictionary(e => e, e => (y: new List<double>(e.y), fx: (bool[])Fx(e).Clone()));
                foreach (var a in jl.arms)
                {
                    var e = res.edges[a.edge]; var fx = Fx(e); double acc = 0;
                    for (int k = 0; k < e.pts.Count; k++)
                    {
                        int i = a.atStart ? k : e.pts.Count - 1 - k;
                        if (k > 0) acc += SplineEdge.Dist(e.pts[i], e.pts[a.atStart ? i - 1 : i + 1]);
                        if (acc > a.clip + 0.5) break;
                        e.y[i] = cy + gx * (e.pts[i].x - c.x) + gz * (e.pts[i].z - c.z); fx[i] = true;
                    }
                }
                bool ok = saved.Keys.All(e => Envelope(e, fixedPts[e], false));
                if (ok) { foreach (var e in saved.Keys) Envelope(e, fixedPts[e], true); res.junctionPlane[jl.node] = (gx, gz); planes++; }
                else { foreach (var kv in saved) { kv.Key.y = kv.Value.y; fixedPts[kv.Key] = kv.Value.fx; } cones++; }
            }
            return (planes, cones);
        }

        /// <summary>판 둘레 = 길마다 (끊는 선 시계쪽 끝, 반시계쪽 끝) + 다음 길까지 모서리 선 안쪽 점들. 0.05m 안으로 붙은 점은 하나로(가는 삼각형 방지). 높이 = PlateHeight.</summary>
        static void FinalizePlate(SplineTownResult res, JunctionLayout jl, SplineTownSettings s)
        {
            var node = res.nodes[jl.node]; double hc = s.HalfCarriageway; int m = jl.arms.Count;
            var pts = new List<Vec2>(); var ys = new List<double>();
            for (int k = 0; k < m; k++)
            {
                var A = jl.arms[k]; var B = jl.arms[(k + 1) % m];
                // 끊는 선 두 끝 = 길 단면 높이(단면은 가로로 평평) — 판과 길 띠가 같은 정점 높이로 맞물림
                var pcw = new Vec2(A.c.x - A.ccw.x * hc, A.c.z - A.ccw.z * hc); var pccw = new Vec2(A.c.x + A.ccw.x * hc, A.c.z + A.ccw.z * hc);
                // 끊는 선 끝 = 길 띠 끝 정점과 같은 식(중심 높이 + 단면 옆 기울기 × 반차로) — 정점이 정확히 맞물림(M6 용접)
                var ea = res.edges[A.edge]; double sgA = A.atStart ? 1 : -1, bankEnd = A.atStart ? ea.bankA : ea.bankB;
                pts.Add(pcw); ys.Add(A.y + bankEnd * (-sgA * hc));
                pts.Add(pccw); ys.Add(A.y + bankEnd * (sgA * hc));
                var c0 = CornerPath(A, B, node, hc, CornerSamples(A, B)); var y0 = CornerY(res, jl, A, B, c0, hc);
                for (int q = 1; q + 1 < c0.Count; q++) { pts.Add(c0[q]); ys.Add(y0[q]); }
            }
            jl.ringP.Clear(); jl.ringY.Clear();
            for (int i = 0; i < pts.Count; i++)
            {
                var q = pts[i];
                if (jl.ringP.Count > 0 && SplineEdge.Dist(q, jl.ringP[jl.ringP.Count - 1]) < 0.05) continue;
                if (SplineEdge.Dist(q, node) < 0.05) continue;
                jl.ringP.Add(q); jl.ringY.Add(ys[i]);
            }
            while (jl.ringP.Count > 2 && SplineEdge.Dist(jl.ringP[0], jl.ringP[jl.ringP.Count - 1]) < 0.05) { jl.ringP.RemoveAt(jl.ringP.Count - 1); jl.ringY.RemoveAt(jl.ringY.Count - 1); }
        }

        /// <summary>
        /// 모서리 선 점 높이 = 판 평면 + (양 끝을 길 단면 높이에 맞추는 보정, 선 따라 직선 보간). 단면이 가로로 평평해 평면과 끊는 선 끝이 최대 반차로×기울기 어긋나는 것을 모서리 안에서 흡수.
        /// </summary>
        public static List<double> CornerY(SplineTownResult res, JunctionLayout jl, JunctionArm A, JunctionArm B, List<Vec2> path, double halfC)
        {
            var r = new List<double>(); int n = path.Count;
            bool plane = res.junctionPlane.ContainsKey(jl.node);
            double dA = plane ? 0 : A.y - PlateHeight(res, jl, path[0], halfC), dB = plane ? 0 : B.y - PlateHeight(res, jl, path[n - 1], halfC);
            double total = 0; var acc = new double[n];
            for (int i = 1; i < n; i++) { total += SplineEdge.Dist(path[i - 1], path[i]); acc[i] = total; }
            for (int i = 0; i < n; i++) { double t = total > 1e-9 ? acc[i] / total : (double)i / Math.Max(1, n - 1); r.Add(PlateHeight(res, jl, path[i], halfC) + dA + (dB - dA) * t); }
            return r;
        }

        /// <summary>판(가운데 + 둘레 부채꼴) 렌더 면의 높이. 판 밖이면 null.</summary>
        public static double? PlateSurfaceAt(SplineTownResult res, JunctionLayout jl, Vec2 p)
        {
            var c = res.nodes[jl.node]; int n = jl.ringP.Count;
            for (int k = 0; k < n; k++)
            {
                var a = jl.ringP[k]; var b = jl.ringP[(k + 1) % n];
                double den = (a.z - b.z) * (c.x - b.x) + (b.x - a.x) * (c.z - b.z);
                if (Math.Abs(den) < 1e-12) continue;
                double wc = ((a.z - b.z) * (p.x - b.x) + (b.x - a.x) * (p.z - b.z)) / den;
                double wa = ((b.z - c.z) * (p.x - b.x) + (c.x - b.x) * (p.z - b.z)) / den;
                double wb = 1 - wc - wa;
                if (wc >= -1e-9 && wa >= -1e-9 && wb >= -1e-9) return wc * jl.y + wa * jl.ringY[k] + wb * jl.ringY[(k + 1) % n];
            }
            return null;
        }

        /// <summary>
        /// 교차점 판 위 한 점의 높이(원뿔꼴): 교차점에서 그 점까지 거리 r × 방향별 기울기 s(θ). s(θ) 는 붙은 길마다
        /// "끊는 선 끝 높이 − 교차점 높이) / 끊는 선 끝까지 거리" 를 그 길 방향에 두고, 길 사이 방향은 각도로 직선 보간.
        /// 끊는 선 두 끝(길 단면, 가로 평평)은 같은 거리·같은 높이라 정확히 맞고, 판 면은 반지름 방향 기울기 ≈ 길의 패드 경사(≤5%).
        /// 3차의 "점 순서 보간"(81% 면)과 4차 시도 "한 평면"(비탈에서 길 높이와 양립 불가)을 대신한다.
        /// </summary>
        public static double PlateHeight(SplineTownResult res, JunctionLayout jl, Vec2 p, double halfC)
        {
            if (res.junctionPlane.TryGetValue(jl.node, out var pg)) { var pc = res.nodes[jl.node]; return res.nodeY[jl.node] + pg.gx * (p.x - pc.x) + pg.gz * (p.z - pc.z); }
            var c = res.nodes[jl.node]; double r = SplineEdge.Dist(p, c);
            if (r < 1e-9 || jl.arms.Count == 0) return jl.y;
            double th = Math.Atan2(p.z - c.z, p.x - c.x);
            // 길마다 (각도, 기울기) — 끊는 선 가운데 방향과 두 끝 방향 셋을 같은 기울기로
            var keys = new List<(double a, double g)>();
            foreach (var A in jl.arms)
            {
                foreach (int sg in new[] { -1, 0, 1 })
                {
                    var q = new Vec2(A.c.x + A.ccw.x * halfC * sg, A.c.z + A.ccw.z * halfC * sg);
                    double rq = SplineEdge.Dist(q, c); if (rq < 1e-9) continue;
                    keys.Add((Math.Atan2(q.z - c.z, q.x - c.x), (A.y - jl.y) / rq));
                }
            }
            keys.Sort((x, y) => x.a.CompareTo(y.a));
            // th 를 사이에 둔 두 키(원형)
            int n = keys.Count; int hi = keys.FindIndex(k => k.a >= th); if (hi < 0) hi = 0;
            int lo = (hi - 1 + n) % n;
            double a0 = keys[lo].a, a1 = keys[hi].a, t = th;
            if (a1 <= a0) { a1 += 2 * Math.PI; if (t < a0) t += 2 * Math.PI; }
            double w = a1 - a0 > 1e-9 ? (t - a0) / (a1 - a0) : 0;
            double g = keys[lo].g + (keys[hi].g - keys[lo].g) * Math.Max(0, Math.Min(1, w));
            return jl.y + g * r;
        }

        /// <summary>교차점 판 바깥 윤곽(오프셋 off) — 붙는 길 끊는 선 + 모서리 선을 반시계로.</summary>
        public static List<Vec2> PlateOutline(JunctionLayout jl, Vec2 node, double off)
        {
            var r = new List<Vec2>(); int m = jl.arms.Count;
            for (int k = 0; k < m; k++)
            {
                var A = jl.arms[k]; var B = jl.arms[(k + 1) % m];
                r.Add(new Vec2(A.c.x - A.ccw.x * off, A.c.z - A.ccw.z * off));
                r.AddRange(CornerPath(A, B, node, off, CornerSamples(A, B)));
            }
            return r;
        }
        public static int CornerSamples(JunctionArm A, JunctionArm B) => Math.Max(3, (int)Math.Ceiling(ArmGap(A, B) / (Math.PI / 12)));

        /// <summary>원판(둘레 + 지형 대각선) 안 붙는 길 높이 중 가장 낮은 값.</summary>
        public static double JunctionLowY(SplineTownResult res, int n, SplineTownSettings s)
        {
            double lo = res.nodeY[n], reach = JunctionRadius(s) + s.sidewalkWidthMeters + s.terrainStepMeters * 1.5;
            foreach (var e in res.edges)
                for (int i = 0; i < e.pts.Count; i++)
                    if ((e.a == n || e.b == n) && SplineEdge.Dist(e.pts[i], res.nodes[n]) <= reach) lo = Math.Min(lo, e.y[i]);
            return lo;
        }

        /// <summary>앞에서부터 큰길 중심선과의 거리가 처음 minDist 가 되는 점까지 잘라낸다(그 점을 새 시작점으로). 끝까지 못 벗어나면 null.</summary>
        public static List<Vec2> TrimToMouth(List<Vec2> p, List<Vec2> host, double minDist)
        {
            double prev = DistToPoly(p[0], host);
            if (prev >= minDist) return p;
            for (int k = 1; k < p.Count; k++)
            {
                double d = DistToPoly(p[k], host);
                if (d < minDist) { prev = d; continue; }
                // 구간 안에서 거리 = minDist 인 점을 이분으로
                double lo = 0, hi = 1;
                for (int it = 0; it < 30; it++)
                {
                    double m = (lo + hi) / 2;
                    var q = new Vec2(p[k - 1].x + (p[k].x - p[k - 1].x) * m, p[k - 1].z + (p[k].z - p[k - 1].z) * m);
                    if (DistToPoly(q, host) < minDist) lo = m; else hi = m;
                }
                var start = new Vec2(p[k - 1].x + (p[k].x - p[k - 1].x) * hi, p[k - 1].z + (p[k].z - p[k - 1].z) * hi);
                var r = new List<Vec2> { start };
                if (SplineEdge.Dist(start, p[k]) < 0.3) k++;
                for (; k < p.Count; k++) r.Add(p[k]);
                return r.Count >= 2 ? r : null;
            }
            return null;
        }

        static List<Vec2> TrimLength(List<Vec2> p, double maxLen)
        {
            var o = new List<Vec2> { p[0] }; double acc = 0;
            for (int i = 1; i < p.Count; i++)
            {
                double d = SplineEdge.Dist(p[i - 1], p[i]);
                if (acc + d >= maxLen) { double t = (maxLen - acc) / d; o.Add(new Vec2(p[i - 1].x + (p[i].x - p[i - 1].x) * t, p[i - 1].z + (p[i].z - p[i - 1].z) * t)); return o; }
                acc += d; o.Add(p[i]);
            }
            return o;
        }

        // ------------------------------------------------------------------ 곡선
        public static List<Vec2> Resample(List<Vec2> p, double step)
        {
            var outP = new List<Vec2> { p[0] };
            double total = SplineEdge.PolyLength(p);
            int n = Math.Max(1, (int)Math.Round(total / step));
            double each = total / n, acc = 0; int seg = 0; double segStart = 0;
            for (int k = 1; k < n; k++)
            {
                acc = k * each;
                while (seg < p.Count - 2 && segStart + SplineEdge.Dist(p[seg], p[seg + 1]) < acc) { segStart += SplineEdge.Dist(p[seg], p[seg + 1]); seg++; }
                double l = SplineEdge.Dist(p[seg], p[seg + 1]), t = l > 1e-9 ? (acc - segStart) / l : 0;
                outP.Add(new Vec2(p[seg].x + (p[seg + 1].x - p[seg].x) * t, p[seg].z + (p[seg + 1].z - p[seg].z) * t));
            }
            outP.Add(p[p.Count - 1]);
            return outP;
        }

        static List<Vec2> Chaikin(List<Vec2> p)
        {
            if (p.Count < 3) return p;
            var o = new List<Vec2> { p[0] };
            for (int i = 0; i + 1 < p.Count; i++)
            {
                var a = p[i]; var b = p[i + 1];
                o.Add(new Vec2(a.x * .75 + b.x * .25, a.z * .75 + b.z * .25));
                o.Add(new Vec2(a.x * .25 + b.x * .75, a.z * .25 + b.z * .75));
            }
            o.Add(p[p.Count - 1]);
            return o;
        }

        /// <summary>점 i 의 곡률 반경 — i±k 세 점의 외접원 반경(k = 4m 호에 해당). 직선이면 무한.</summary>
        public static double RadiusAt(List<Vec2> p, int i, int k = 2)
        {
            if (i - k < 0 || i + k >= p.Count) return double.PositiveInfinity;
            var a = p[i - k]; var b = p[i]; var c = p[i + k];
            double ab = SplineEdge.Dist(a, b), bc = SplineEdge.Dist(b, c), ca = SplineEdge.Dist(c, a);
            double cross = Math.Abs((b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x));
            if (cross < 1e-9) return double.PositiveInfinity;
            return ab * bc * ca / (2 * cross);
        }

        /// <summary>
        /// 헤어핀 판정(스펙 §2.3, 2026-10-20 개정): 굽이 = 이웃 3점 반경 &lt; hairpinBendRadiusMeters 가 끊김 없이 이어지는 구간
        /// (필렛이 나눈 두 원호처럼 같은 방향 굽이가 4m 이하 직선으로 떨어져 있으면 한 굽이). 굽이 안 부호 있는 방향 변화 합의 크기 ≥ hairpinTurnDeg 면 구간 전체가 헤어핀.
        /// 고정 길이 창으로 재던 3차 방식은 일반 굽이 점을 헤어핀으로 잘못 넣어 반경 12m 검사를 비켜 갔다(BUG-006 3점).
        /// </summary>
        static List<bool> HairpinFlags(List<Vec2> p, SplineTownSettings s)
        {
            int n = p.Count;
            var f = new List<bool>(new bool[n]);
            var curved = new bool[n]; var turn = new double[n];
            for (int i = 1; i + 1 < n; i++) { curved[i] = RadiusAt(p, i, 1) < s.hairpinBendRadiusMeters; turn[i] = TurnAngle(p[i - 1], p[i], p[i + 1]); }
            // 굽이 구간 모으기
            var runs = new List<(int a, int b, double sum)>();
            for (int i = 1; i + 1 < n; i++)
            {
                if (!curved[i]) continue;
                int j = i; double sum = 0;
                while (j + 1 < n && curved[j]) { sum += turn[j]; j++; }
                runs.Add((i, j - 1, sum)); i = j;
            }
            // 같은 방향 굽이가 2점(약 4m) 이하 직선으로 떨어져 있으면 합친다
            for (int k = runs.Count - 2; k >= 0; k--)
            {
                var r0 = runs[k]; var r1 = runs[k + 1];
                if (r1.a - r0.b <= 3 && Math.Sign(r0.sum) == Math.Sign(r1.sum))
                {
                    double gapTurn = 0; for (int i = r0.b + 1; i < r1.a; i++) gapTurn += turn[i];
                    runs[k] = (r0.a, r1.b, r0.sum + gapTurn + r1.sum); runs.RemoveAt(k + 1);
                }
            }
            foreach (var r in runs)
                if (Math.Abs(r.sum) * 180 / Math.PI >= s.hairpinTurnDeg)
                    for (int i = r.a; i <= r.b; i++) f[i] = true;
            // 패드 경계를 앞뒤 2점(약 4m) 넓힌다 — 경계 구간이 큰길 경사로 남던 것(BUG-005, QA 분류와 한 점 어긋남)
            var g = new List<bool>(f);
            for (int i = 0; i < f.Count; i++) if (f[i]) for (int k = Math.Max(0, i - 2); k <= Math.Min(f.Count - 1, i + 2); k++) g[k] = true;
            return g;
        }

        public static double TurnAngle(Vec2 a, Vec2 b, Vec2 c)
        {
            double a1 = Math.Atan2(b.z - a.z, b.x - a.x), a2 = Math.Atan2(c.z - b.z, c.x - b.x), d = a2 - a1;
            while (d > Math.PI) d -= 2 * Math.PI; while (d < -Math.PI) d += 2 * Math.PI;
            return d;
        }

        /// <summary>반경 미달 점과 주변을 이웃 평균 쪽으로 반복 이완(끝점 고정). 원래 선에서 maxDeviation 을 넘게는 안 민다.</summary>
        static List<Vec2> EnforceRadius(List<Vec2> p, List<bool> hairpin, List<Vec2> orig, SplineTownSettings s, ref double maxDev, List<Vec2> frozen = null)
        {
            // 헤어핀 패드 점(frozen)은 이완이 건드리지 않는다 — 이완은 원래 선 어디든 6m 안이면 옮기므로 U자 안쪽 다리 쪽으로 끝을 끌어당겼다(15a-2)
            bool Frozen(Vec2 v) => frozen != null && frozen.Any(f => Math.Abs(f.x - v.x) < 0.75 && Math.Abs(f.z - v.z) < 0.75);
            var q = new List<Vec2>(p);
            // 이완 → 2m 재표본 → 다시 검사(재표본이 만든 이웃 3점 꺾임까지) 최대 4회. 반경은 이웃 3점(2m)·5점(4m) 둘 다 본다.
            for (int round = 0; round < 4; round++)
            {
            bool roundAny = false;
            for (int it = 0; it < 300; it++)
            {
                bool any = false;
                var next = new List<Vec2>(q);
                for (int i = 1; i + 1 < q.Count; i++)
                {
                    double need = hairpin[Math.Min(i, hairpin.Count - 1)] ? s.hairpinMinRadiusMeters : s.mainMinRadiusMeters;
                    if (Math.Min(RadiusAt(q, i), RadiusAt(q, i, 1)) >= need * 1.01) continue;
                    roundAny = true;
                    any = true;
                    for (int j = Math.Max(1, i - 2); j <= Math.Min(q.Count - 2, i + 2); j++)
                    {
                        var m = new Vec2((q[j - 1].x + q[j + 1].x) / 2, (q[j - 1].z + q[j + 1].z) / 2);
                        var cand = new Vec2(q[j].x + (m.x - q[j].x) * 0.5, q[j].z + (m.z - q[j].z) * 0.5);
                        if (Frozen(q[j])) continue;
                        if (DistToPoly(cand, orig) <= s.maxDeviationMeters) next[j] = cand;
                    }
                }
                q = next;
                if (!any) break;
            }
            q = Resample(q, s.resampleStepMeters);
            if (!roundAny) break;
            }
            foreach (var v in q) maxDev = Math.Max(maxDev, DistToPoly(v, orig));
            return q;
        }

        public static double DistToPoly(Vec2 v, List<Vec2> poly)
        {
            double best = double.MaxValue;
            for (int i = 0; i + 1 < poly.Count; i++) best = Math.Min(best, DistToSeg(v, poly[i], poly[i + 1], out _));
            return best;
        }

        static double DistToSeg(Vec2 v, Vec2 a, Vec2 b, out double t)
        {
            double dx = b.x - a.x, dz = b.z - a.z, l2 = dx * dx + dz * dz;
            t = l2 > 1e-12 ? Math.Max(0, Math.Min(1, ((v.x - a.x) * dx + (v.z - a.z) * dz) / l2)) : 0;
            double px = a.x + dx * t - v.x, pz = a.z + dz * t - v.z;
            return Math.Sqrt(px * px + pz * pz);
        }

        // ------------------------------------------------------------------ 종단
        /// <summary>
        /// 구간 (i−1 → i) 종단 경사 상한 — 프로필·교차점 이완·측정이 모두 이 함수 하나를 쓴다(단계마다 다르면 모자란 높이가 한 구간에 몰림, BUG-005).
        /// 골목 12% · 패드(헤어핀·교차점 근처) 5% · 생성 길(잇기·섞기 경계 띠) 10% · 실제 OSM 큰길 8%. (§2.3·§2.9)
        /// </summary>
        public static double GradeLimit(SplineEdge e, int i, SplineTownSettings s)
        {
            if (e.alley) return s.alleyMaxGrade;
            bool Pad(int k) => k >= 0 && k < e.pts.Count && ((e.hairpin.Count > k && e.hairpin[k]) || (e.pad.Count > k && e.pad[k]));
            if (Pad(i) || Pad(i - 1)) return s.hairpinMaxGrade;
            bool Seam(int k) => k >= 0 && k < e.pts.Count && e.pts[k].z >= s.seamZMin && e.pts[k].z <= s.seamZMax;
            if (e.connector || Seam(i) || Seam(i - 1)) return s.generatedMaxGrade;
            return s.mainMaxGrade;
        }

        /// <summary>각 곡선의 경사 용량(Σ 상한×구간 길이)보다 양 끝 교차점 높이차가 크면 두 끝을 반씩 당긴다. 막다른 끝은 대상 아님.</summary>
        static int RelaxNodeHeights(List<SplineEdge> edges, List<double> nodeY, int[] deg, SplineTownSettings s)
        {
            // 용량 = 프로필이 실제로 쓰는 상한(× 0.995)과 같게 — 다르면 모자란 높이가 교차점 옆 한 구간에 몰린다(BUG-005)
            var cap = edges.Select(e => { double c = 0; for (int i = 1; i < e.pts.Count; i++) c += SplineEdge.Dist(e.pts[i - 1], e.pts[i]) * GradeLimit(e, i, s) * 0.995; return c * 0.999; }).ToList();
            for (int it = 0; it < 2000; it++)
            {
                bool any = false;
                for (int k = 0; k < edges.Count; k++)
                {
                    var e = edges[k];
                    if (deg[e.a] < 2 || deg[e.b] < 2 || e.a == e.b) continue;
                    double d = nodeY[e.b] - nodeY[e.a], excess = Math.Abs(d) - cap[k];
                    if (excess <= 1e-6) continue;
                    double move = excess / 2 * Math.Sign(d);
                    nodeY[e.a] += move; nodeY[e.b] -= move; any = true;
                }
                if (!any) return it;
            }
            return 2000;
        }

        internal static List<double> Profile(SplineEdge e, IHeightField H, double? pinA, double? pinB, SplineTownSettings s, double? gradeOverride = null)
        {
            int n = e.pts.Count;
            var sArc = new double[n];
            for (int i = 1; i < n; i++) sArc[i] = sArc[i - 1] + SplineEdge.Dist(e.pts[i - 1], e.pts[i]);
            double L = sArc[n - 1];
            var raw = e.pts.Select(p => H.HeightAt(p.x, p.z)).ToArray();
            var y = new double[n];
            double half = s.profileWindowMeters / 2;
            for (int i = 0; i < n; i++)
            {
                double sum = 0; int cnt = 0;
                for (int j = 0; j < n; j++) if (Math.Abs(sArc[j] - sArc[i]) <= half) { sum += raw[j]; cnt++; }
                y[i] = sum / cnt;
            }
            double e0 = pinA.HasValue ? pinA.Value - y[0] : 0, e1 = pinB.HasValue ? pinB.Value - y[n - 1] : 0;
            for (int i = 0; i < n; i++)
                y[i] += e0 * Math.Max(0, 1 - sArc[i] / s.profileWindowMeters) + e1 * Math.Max(0, 1 - (L - sArc[i]) / s.profileWindowMeters);
            if (pinA.HasValue) y[0] = pinA.Value;
            if (pinB.HasValue) y[n - 1] = pinB.Value;
            // 구간 (i−1 → i) 은 양 끝 중 하나라도 헤어핀이면 패드 경사
            double G(int i) => (gradeOverride ?? GradeLimit(e, i, s)) * 0.995; // 반올림·재표본으로 상한을 넘지 않게 0.5% 안쪽(BUG-005)
            // 고정 끝(교차점)은 그대로, 막다른 끝은 같이 움직인다
            int lo = pinA.HasValue ? 1 : 0, hi = pinB.HasValue ? n - 2 : n - 1;
            for (int pass = 0; pass < 24; pass++)
            {
                for (int i = Math.Max(1, lo); i <= hi; i++) { double d = sArc[i] - sArc[i - 1], g = G(i) * d; y[i] = Math.Min(Math.Max(y[i], y[i - 1] - g), y[i - 1] + g); }
                // 구간 (i → i+1) 의 상한은 G(i+1) — G(i) 로 자르면 상한이 바뀌는 경계(패드 5% ↔ 생성 길 10%)에서 큰 값이 패드로 샌다
                for (int i = Math.Min(n - 2, hi); i >= lo; i--) { double d = sArc[i + 1] - sArc[i], g = G(i + 1) * d; y[i] = Math.Min(Math.Max(y[i], y[i + 1] - g), y[i + 1] + g); }
            }
            return y.ToList();
        }

        // ------------------------------------------------------------------ 깎기/메우기
        struct RoadSeg
        {
            public Vec2 a, b; public double ya, yb; public bool wallL, wallR; public int edge, idx;
            public double halfSec, edgeD, blend, curb; public bool alley;
        }

        /// <summary>
        /// 지형 점마다 가장 가까운 길(길 가장자리까지 거리 기준 — 큰길이 골목보다 넓으니 자연히 우선)에 맞춘다:
        ///  d ≤ 단면 반폭(큰길 4m·골목 1.5m): 길 밑 — 길 높이 −0.10 (길 메시 아래 숨김)
        ///  d ≤ 어깨 끝(큰길 6m·골목 2.5m): 보도/길 높이로 깎기·메우기
        ///  그 밖 블렌드(큰길 6m·골목 3m): 원래 지형으로 부드럽게. 큰길 옹벽 쪽은 옹벽(어깨 끝 + 3m)까지 평평, 그 너머 원래 지형.
        /// 옹벽 = 큰길에서 옹벽 자리 원래 지형과 보도 높이 차가 wallThreshold 초과. 골목엔 옹벽 없음(§2.4 벽·담 바로).
        /// </summary>
        static void CutFill(SplineTownResult res, IHeightField H, SplineTownSettings s, out double maxCut, out int overCut, out double wallLen, out int wallSides)
        {
            int nx = (int)Math.Round(s.boundsXMeters / s.terrainStepMeters) + 1, nz = (int)Math.Round(s.boundsZMeters / s.terrainStepMeters) + 1;
            var orig = new TerrainGrid(nx, nz, s.terrainStepMeters);
            for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) orig.h[j * nx + i] = H.HeightAt(i * orig.step, j * orig.step);
            res.terrainOriginal = orig;
            var t = new TerrainGrid(nx, nz, s.terrainStepMeters); Array.Copy(orig.h, t.h, orig.h.Length);
            res.terrain = t;

            var segs = new List<RoadSeg>();
            wallLen = 0; wallSides = 0; int foldSkipped = 0;
            for (int ei = 0; ei < res.edges.Count; ei++)
            {
                var e = res.edges[ei];
                double halfSec = e.alley ? s.alleyHalfWidthMeters : s.HalfSection;
                double edgeD = e.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder;
                double blend = e.alley ? s.alleyBlendMeters : s.blendMeters;
                double curb = e.alley ? 0 : s.curbHeightMeters;
                var wallL = new bool[e.pts.Count]; var wallR = new bool[e.pts.Count];
                if (!e.alley)
                    for (int i = 0; i < e.pts.Count; i++)
                    {
                        var (nxv, nzv) = Normal(e.pts, i);
                        double top = e.y[i] + curb, wd = edgeD + s.wallSetbackMeters;
                        double gl = orig.Surface(e.pts[i].x + nxv * wd, e.pts[i].z + nzv * wd);
                        double gr = orig.Surface(e.pts[i].x - nxv * wd, e.pts[i].z - nzv * wd);
                        wallL[i] = Math.Abs(gl - top) > s.wallThresholdMeters;
                        wallR[i] = Math.Abs(gr - top) > s.wallThresholdMeters;
                    }
                e.wallL = wallL.ToList(); e.wallR = wallR.ToList();
                var arcE = new double[e.pts.Count]; for (int i = 1; i < e.pts.Count; i++) arcE[i] = arcE[i - 1] + SplineEdge.Dist(e.pts[i - 1], e.pts[i]);
                for (int i = 0; i + 1 < e.pts.Count; i++)
                {
                    // 단면이 옆으로 기운 곳(교차점 평면 쪽)은 낮은 쪽 끝 높이로 — 지형을 그 아래로 누른다
                    double ba = Math.Abs(BankAt(e, arcE[i], s)) * halfSec, bb = Math.Abs(BankAt(e, arcE[i + 1], s)) * halfSec;
                    double hsI = e.alley ? Math.Max(e.AlleyHalf(i, halfSec), e.AlleyHalf(i + 1, halfSec)) : halfSec, edI = e.alley ? hsI + s.alleyShoulderMeters : edgeD;
                    segs.Add(new RoadSeg { a = e.pts[i], b = e.pts[i + 1], ya = e.y[i] - ba, yb = e.y[i + 1] - bb, wallL = wallL[i] || wallL[i + 1], wallR = wallR[i] || wallR[i + 1], edge = ei, idx = i, halfSec = hsI, edgeD = edI, blend = blend, curb = curb, alley = e.alley });
                    if (e.alley) continue;
                    foreach (int side in new[] { 1, -1 })
                    {
                        bool w = side == 1 ? (wallL[i] && wallL[i + 1]) : (wallR[i] && wallR[i + 1]);
                        if (!w) continue;
                        var (n0x, n0z) = Normal(e.pts, i); var (n1x, n1z) = Normal(e.pts, i + 1);
                        double wd = edgeD + s.wallSetbackMeters;
                        var pa = new Vec2(e.pts[i].x + side * n0x * wd, e.pts[i].z + side * n0z * wd);
                        var pb = new Vec2(e.pts[i + 1].x + side * n1x * wd, e.pts[i + 1].z + side * n1z * wd);
                        // 급커브 안쪽에서 옹벽 선이 접히면(방향이 뒤집히면) 세우지 않는다 — 톱니 방지
                        double cdx = e.pts[i + 1].x - e.pts[i].x, cdz = e.pts[i + 1].z - e.pts[i].z;
                        if ((pb.x - pa.x) * cdx + (pb.z - pa.z) * cdz <= 0.25 * (cdx * cdx + cdz * cdz)) { foldSkipped++; continue; }
                        double ta = e.y[i] + curb, tb = e.y[i + 1] + curb;
                        double ga = orig.Surface(pa.x, pa.z), gb = orig.Surface(pb.x, pb.z);
                        res.walls.Add(new WallSegment { a = pa, b = pb, topA = Math.Max(ta, ga), topB = Math.Max(tb, gb), botA = Math.Min(ta, ga) - 0.3, botB = Math.Min(tb, gb) - 0.3 });
                        wallLen += SplineEdge.Dist(pa, pb);
                    }
                }
                for (int i = 0; i < e.pts.Count; i++) { if (wallL[i] && (i == 0 || !wallL[i - 1])) wallSides++; if (wallR[i] && (i == 0 || !wallR[i - 1])) wallSides++; }
            }
            res.report.Metric("wall_fold_skipped", $"{foldSkipped} (급커브 안쪽에서 옹벽 선이 접혀 안 세운 구간)");
            // 교차점 원판·모서리 둘레와 다른 길 단면 위를 지나는 옹벽 조각은 세우지 않는다(교차점에서 벽이 길을 가로지름 — 3차 그래픽)
            {
                double jr = JunctionRadius(s) + s.HalfShoulder - s.HalfCarriageway;
                int before = res.walls.Count;
                res.walls.RemoveAll(w =>
                {
                    var m = new Vec2((w.a.x + w.b.x) / 2, (w.a.z + w.b.z) / 2);
                    foreach (var n in res.junctions) if (SplineEdge.Dist(m, res.nodes[n]) <= jr || SplineEdge.Dist(w.a, res.nodes[n]) <= jr || SplineEdge.Dist(w.b, res.nodes[n]) <= jr) return true;
                    foreach (var e in res.edges)
                    {
                        double lim = (e.alley ? e.AlleyHalfMax(s.alleyHalfWidthMeters) + s.alleyShoulderMeters : s.HalfSection) + 0.2;
                        if (DistToPoly(m, e.pts) < lim || DistToPoly(w.a, e.pts) < lim || DistToPoly(w.b, e.pts) < lim) return true;
                    }
                    return false;
                });
                wallLen = res.walls.Sum(w => SplineEdge.Dist(w.a, w.b));
                res.report.Metric("wall_junction_clipped", $"{before - res.walls.Count} 조각 (교차점 둘레·다른 길 위)");
            }

            double hc = s.HalfShoulder + s.blendMeters;   // 공간 해시 칸 = 가장 넓은 영향 반경
            var hash = new Dictionary<(int, int), List<int>>();
            for (int k = 0; k < segs.Count; k++)
            {
                var sg = segs[k];
                int c0 = (int)Math.Floor(Math.Min(sg.a.x, sg.b.x) / hc) - 1, c1 = (int)Math.Floor(Math.Max(sg.a.x, sg.b.x) / hc) + 1;
                int r0 = (int)Math.Floor(Math.Min(sg.a.z, sg.b.z) / hc) - 1, r1 = (int)Math.Floor(Math.Max(sg.a.z, sg.b.z) / hc) + 1;
                for (int c = c0; c <= c1; c++) for (int r = r0; r <= r1; r++) { if (!hash.TryGetValue((c, r), out var l)) hash[(c, r)] = l = new List<int>(); l.Add(k); }
            }

            maxCut = 0; overCut = 0;
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    var v = new Vec2(i * t.step, j * t.step);
                    if (!hash.TryGetValue(((int)Math.Floor(v.x / hc), (int)Math.Floor(v.z / hc)), out var list)) continue;
                    double bestScore = double.MaxValue, bestD = 0, bestY = 0; bool bestWall = false; RoadSeg best = default; bool found = false;
                    foreach (var k in list)
                    {
                        var sg = segs[k];
                        double d = DistToSeg(v, sg.a, sg.b, out double tt);
                        if (d > sg.edgeD + Math.Max(sg.blend, s.wallSetbackMeters)) continue;
                        double score = d - sg.halfSec;                       // 길 가장자리까지 거리
                        if (score >= bestScore) continue;
                        bestScore = score; bestD = d; best = sg; found = true;
                        bestY = sg.ya + (sg.yb - sg.ya) * tt;
                        double cross = (sg.b.x - sg.a.x) * (v.z - sg.a.z) - (sg.b.z - sg.a.z) * (v.x - sg.a.x); // > 0 = 왼쪽
                        bestWall = cross > 0 ? sg.wallL : sg.wallR;
                    }
                    if (!found) continue;
                    int idx = j * nx + i;
                    double o = orig.h[idx], top = bestY + best.curb, nh;
                    // 길 밑을 단면 반폭보다 격자 0.75칸(어깨 폭 안에서) 더 넓게 낮춘다 — 어깨의 보도 높이 점이 삼각형을 타고 차로 가장자리 위로 올라오는 것 방지.
                    // 어깨 띠 메시가 그 자리를 덮는다(안쪽 보도 높이 → 바깥 지형 면).
                    double under = best.halfSec + Math.Min(s.terrainStepMeters * 0.75, (best.edgeD - best.halfSec) * 0.75);
                    if (bestD <= under) nh = bestY - 0.10;
                    else if (bestD <= best.edgeD) nh = top;
                    else if (bestWall) nh = bestD <= best.edgeD + s.wallSetbackMeters ? top : o;
                    else if (bestD <= best.edgeD + best.blend) { double w = (bestD - best.edgeD) / best.blend; w = w * w * (3 - 2 * w); nh = top + (o - top) * w; }
                    else continue;
                    t.h[idx] = nh;
                    if (bestD <= best.edgeD) { double c = Math.Abs(nh - o); maxCut = Math.Max(maxCut, c); if (c > s.maxCutFillMeters) overCut++; }
                }
            // 교차점 원판 밑은 길 높이 아래로(원판 둘레는 붙는 길 높이를 이어받아 기울어 있으니 그중 가장 낮은 높이)
            var junY = res.junctions.ToDictionary(n => n, n => JunctionLowY(res, n, s));
            foreach (var n in res.junctions)
            {
                double rad = JunctionRadius(s);
                for (int j = 0; j < nz; j++)
                    for (int i = 0; i < nx; i++)
                        if (SplineEdge.Dist(new Vec2(i * t.step, j * t.step), res.nodes[n]) <= rad) t.h[j * nx + i] = Math.Min(t.h[j * nx + i], junY[n] - 0.10);
            }
            // 교차점 판(원판 + 모서리 보도) 윤곽 안: 보도 바깥 + 1.5m 까지 판 높이 −0.10, 어깨 끝 + 대각선까지 보도 높이 이하
            var plates = res.junctionLayouts.Where(jl => jl.ringP.Count >= 3).ToList();

            // 지형 상한 고정(BUG-003 근본): "가장 가까운 길 하나"가 아니라 영향권 안 모든 길·교차점 원판에 대해 지형 ≤ 그 길 면.
            // 길 면에 닿는 지형 삼각형의 정점은 길 가장자리에서 최대 격자 대각선(2.83m)까지 → 그 범위 정점을 전부 누른다.
            //  단면 + 1.5m 안: 길 높이 −0.10 (차로·보도 아래로 숨김) / 그 밖 대각선까지: 보도(또는 골목) 높이 이하.
            int capped = 0;
            double diag = t.step * Math.Sqrt(2) + 0.05;
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    var v = new Vec2(i * t.step, j * t.step);
                    if (!hash.TryGetValue(((int)Math.Floor(v.x / hc), (int)Math.Floor(v.z / hc)), out var list)) continue;
                    int idx = j * nx + i; double h0 = t.h[idx], h = h0;
                    foreach (var k in list)
                    {
                        var sg = segs[k];
                        double d = DistToSeg(v, sg.a, sg.b, out double tt);
                        if (d > sg.halfSec + diag) continue;
                        double y = sg.ya + (sg.yb - sg.ya) * tt;
                        double inner = sg.halfSec + Math.Min(1.5, (sg.edgeD - sg.halfSec) * 0.75);
                        h = Math.Min(h, d <= inner ? y - 0.10 : y + sg.curb);
                    }
                    foreach (var n in res.junctions)
                    {
                        // 원판 + 모서리 보도(원판 밖 1m) 아래: 길 높이 −0.10 / 그 밖 대각선까지: 모서리 보도 높이 이하
                        double dj = SplineEdge.Dist(v, res.nodes[n]), rj = JunctionRadius(s);
                        if (dj <= rj + Math.Min(1.5, s.sidewalkWidthMeters + 0.5)) h = Math.Min(h, junY[n] - 0.10);
                        else if (dj <= rj + s.sidewalkWidthMeters + diag) h = Math.Min(h, junY[n] + s.curbHeightMeters);
                    }
                    if (h < h0 - 1e-9) { t.h[idx] = h; capped++; }
                }
            // 교차점 판(부채꼴: 가운데 → 차로 끝 윤곽) 안 또는 윤곽에서 보도+1.5m 안: 판 최저 높이 −0.10 / 어깨 끝 + 대각선 안: 보도 높이 이하.
            // 윤곽이 스스로 꼬여도(좁은 사이각) 맞도록 다각형 안팎 대신 부채꼴 삼각형 포함 + 윤곽까지 거리로 판정.
            double inBand = s.HalfSection - s.HalfCarriageway + 1.5, outBand = s.HalfShoulder - s.HalfCarriageway + diag;
            foreach (var jl in plates)
            {
                var ring = jl.ringP; var pc = res.nodes[jl.node];
                int i0 = Math.Max(0, (int)Math.Floor((ring.Min(q => q.x) - outBand) / t.step)), i1 = Math.Min(nx - 1, (int)Math.Ceiling((ring.Max(q => q.x) + outBand) / t.step));
                int j0 = Math.Max(0, (int)Math.Floor((ring.Min(q => q.z) - outBand) / t.step)), j1 = Math.Min(nz - 1, (int)Math.Ceiling((ring.Max(q => q.z) + outBand) / t.step));
                for (int j = j0; j <= j1; j++)
                    for (int i = i0; i <= i1; i++)
                    {
                        var v = new Vec2(i * t.step, j * t.step); int idx = j * nx + i;
                        var fan = PlateSurfaceAt(res, jl, v);
                        // 판 밖이면 가장 가까운 둘레 변의 높이(끝점 보간) 기준
                        double dr = double.MaxValue, ry = jl.y;
                        for (int k = 0; k < ring.Count; k++)
                        {
                            double d = DistToSeg(v, ring[k], ring[(k + 1) % ring.Count], out double tt);
                            if (d < dr) { dr = d; ry = jl.ringY[k] + (jl.ringY[(k + 1) % ring.Count] - jl.ringY[k]) * tt; }
                        }
                        double cap = fan.HasValue ? Math.Min(fan.Value, ry + 1e6) - 0.10 : dr <= inBand ? ry - 0.10 : dr <= outBand ? ry + s.curbHeightMeters : double.MaxValue;
                        if (t.h[idx] > cap + 1e-9) { t.h[idx] = cap; capped++; }
                    }
            }
            res.report.Metric("terrain_capped", $"{capped} 정점 (모든 길·원판 아래로 눌러 길 위 지형 0 보장)");
        }

        // ------------------------------------------------------------------ 충돌 정리 (BUG-003)
        /// <summary>
        /// 두 길의 한 점이 서로의 단면 영향권(어깨 끝 합) 안에 있고 높이차가 conflictHeight 를 넘으며 같은 교차점을 공유하지 않으면 충돌.
        ///  · 골목: 시작 6m(붙는 자리) 밖에서 큰길과 충돌하면 골목을 버린다.
        ///  · 큰길 막다른 끝(차수 1): 끝에서부터 충돌이 없어질 때까지 잘라낸다(10m 미만 남으면 그 곡선을 버림).
        ///  · 그 밖(양쪽 다 이어진 큰길끼리)은 고칠 수 없는 형상(굽이 다리가 너무 가까움)이라 개수·위치만 보고 — 지형 상한 고정이 지형은 막는다.
        /// </summary>
        static void ResolveConflicts(SplineTownResult res, SplineTownSettings s)
        {
            double Reach(SplineEdge e) => e.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder;
            bool Conflict(SplineEdge e, int i, SplineEdge other)
            {
                if (other == e) return false;
                double lim = Reach(e) + Reach(other);
                for (int j = 0; j < other.pts.Count; j++)
                {
                    if (SplineEdge.Dist(e.pts[i], other.pts[j]) >= lim) continue;
                    if (Math.Abs(e.y[i] - other.y[j]) <= s.conflictHeightMeters) continue;
                    // 같은 교차점 근처(공유 노드에서 lim 안)는 교차점이 받는다
                    bool shared = (e.a == other.a || e.a == other.b) && SplineEdge.Dist(e.pts[i], res.nodes[e.a]) < lim * 1.5
                               || (e.b == other.a || e.b == other.b) && SplineEdge.Dist(e.pts[i], res.nodes[e.b]) < lim * 1.5;
                    if (!shared) return true;
                }
                return false;
            }
            var deg = new Dictionary<int, int>();
            foreach (var e in res.Mains) { deg[e.a] = deg.TryGetValue(e.a, out var x) ? x + 1 : 1; deg[e.b] = deg.TryGetValue(e.b, out var y) ? y + 1 : 1; }

            // 골목
            int alleyDropped = 0;
            foreach (var al in res.Alleys.ToList())
            {
                double arc = 0; bool bad = false;
                for (int i = 1; i < al.pts.Count && !bad; i++)
                {
                    arc += SplineEdge.Dist(al.pts[i - 1], al.pts[i]);
                    if (arc < 6) continue;
                    foreach (var o in res.edges) if (o != al && Conflict(al, i, o)) { bad = true; break; }
                }
                if (bad) { res.edges.Remove(al); alleyDropped++; }
            }

            // 큰길 막다른 끝
            int trimmedCells = 0, droppedEdges = 0;
            foreach (var e in res.Mains.ToList())
            {
                foreach (bool fromStart in new[] { true, false })
                {
                    int tip = fromStart ? e.a : e.b;
                    if (!deg.TryGetValue(tip, out var dg) || dg != 1 || e.pts.Count < 2) continue;
                    int cut = 0;
                    while (cut < e.pts.Count)
                    {
                        int i = fromStart ? cut : e.pts.Count - 1 - cut;
                        if (!res.edges.Any(o => o != e && Conflict(e, i, o))) break;
                        cut++;
                    }
                    if (cut == 0) continue;
                    trimmedCells += cut;
                    int keep = e.pts.Count - cut;
                    if (keep < 2 || SplineEdge.PolyLength(fromStart ? e.pts.GetRange(cut, keep) : e.pts.GetRange(0, keep)) < 10) { res.edges.Remove(e); droppedEdges++; break; }
                    // 점마다 붙은 목록(pts·y·hairpin·pad)을 함께 잘라야 인덱스가 안 밀린다(pad 를 빼먹어 경사 상한이 어긋났던 것)
                    if (fromStart) { e.pts.RemoveRange(0, cut); e.y.RemoveRange(0, cut); e.hairpin.RemoveRange(0, cut); if (e.pad.Count >= cut) e.pad.RemoveRange(0, cut); res.nodes[e.a] = e.pts[0]; res.nodeY[e.a] = e.y[0]; }
                    else { e.pts.RemoveRange(keep, cut); e.y.RemoveRange(keep, cut); e.hairpin.RemoveRange(keep, cut); if (e.pad.Count >= keep + cut) e.pad.RemoveRange(keep, cut); res.nodes[e.b] = e.pts[keep - 1]; res.nodeY[e.b] = e.y[keep - 1]; }
                }
            }

            // 남은 충돌(양쪽 다 이어진 큰길끼리)
            int remaining = 0; var where = new List<string>();
            foreach (var e in res.Mains)
                for (int i = 0; i < e.pts.Count; i += 3)
                    if (res.Mains.Any(o => Conflict(e, i, o))) { remaining++; if (where.Count < 5) where.Add($"({e.pts[i].x:0},{e.pts[i].z:0})"); }
            res.report.Metric("conflicts", $"골목 버림 {alleyDropped} · 큰길 막다른 끝 자른 점 {trimmedCells}(곡선 버림 {droppedEdges}) · 남은 큰길끼리 충돌 표본 {remaining} {string.Join(" ", where)}");
        }

        // ------------------------------------------------------------------ 집 줄짓기 (§2.6)
        /// <summary>
        /// 큰길·골목 양옆으로 필지를 낸다. 정면 폭 8~12m(결정적 해시), 틈 0~2m, 집 몸체 깊이 7m.
        /// 앞선 = 큰길: 보도 끝 + 0.5m(옹벽 쪽은 옹벽 + 0.5m — 옹벽 위 단에 앉음) / 골목: 골목 가장자리(셋백 0).
        /// 기단 = 몸체 밑 지형 최고점(집이 묻히지 않게) — 최저점과 차가 2.5m 넘으면 필지를 한 단(10m) 뒤로 물리고, 그래도 넘으면 버림.
        /// 커브 안쪽 반경 15m 미만 · 바다 · 길/보도/옹벽/다른 집과 겹침 → 버림. 낮은 곳(항구)부터 채워 상한 300.
        /// </summary>
        static void PlaceHouses(SplineTownResult res, SplineTownSettings s)
        {
            var t = res.terrain;
            var cands = new List<HousePlot>();
            int skipCurve = 0, skipSteep = 0, skipSea = 0, skipRoad = 0, skipWall = 0, skipOverlap = 0, skipBounds = 0, row2 = 0;
            int hseed = 0;
            for (int ei = 0; ei < res.edges.Count; ei++)
            {
                var e = res.edges[ei];
                var arc = new double[e.pts.Count];
                for (int i = 1; i < e.pts.Count; i++) arc[i] = arc[i - 1] + SplineEdge.Dist(e.pts[i - 1], e.pts[i]);
                double L = arc[arc.Length - 1];
                foreach (int side in new[] { 1, -1 })
                {
                    double sPos = e.alley ? 1 : 4;
                    while (true)
                    {
                        int h = Math.Abs(unchecked((int)(e.pts[0].x * 73856093) ^ (int)(e.pts[0].z * 19349663) ^ (hseed++ * 83492791)));
                        double gap = (h / 1000 % 1000) / 1000.0 * s.plotGapMaxMeters;
                        int model = s.houseModelWidths != null && s.houseModelWidths.Length > 0 ? h % s.houseModelWidths.Length : -1;
                        double bodyW = model >= 0 ? s.houseModelWidths[model] : 0, bodyD = model >= 0 ? s.houseModelDepths[model] : s.houseDepthMeters;
                        double front = model >= 0 ? bodyW : s.plotFrontageMinMeters + (h % 1000) / 1000.0 * (s.plotFrontageMaxMeters - s.plotFrontageMinMeters);
                        double mid = sPos + front / 2;
                        if (mid + front / 2 > L - (e.alley ? 1 : 4)) break;
                        sPos += front + gap;
                        int i = Array.BinarySearch(arc, mid); if (i < 0) i = Math.Min(e.pts.Count - 1, Math.Max(0, ~i));
                        var (nxv, nzv) = Normal(e.pts, i);
                        if (nxv == 0 && nzv == 0) continue;
                        // 커브 안쪽?
                        double r = RadiusAt(e.pts, i);
                        if (r < s.noHouseInnerRadiusMeters && i > 0 && i + 1 < e.pts.Count)
                        {
                            double turn = TurnAngle(e.pts[i - 1], e.pts[i], e.pts[i + 1]); // > 0 = 왼쪽으로 굽음 = 왼쪽(side +1)이 안쪽
                            if ((turn > 0 && side == 1) || (turn < 0 && side == -1)) { skipCurve++; continue; }
                        }
                        bool walled = !e.alley && (side == 1 ? e.wallL[i] : e.wallR[i]);
                        double f0 = e.alley ? s.alleyHalfWidthMeters + 0.3 : s.HalfShoulder + (walled ? s.wallSetbackMeters : 0) + s.houseFrontGapMeters;
                        HousePlot plot = null;
                        for (int row = 0; row < 2 && plot == null; row++)
                        {
                            double f = f0 + row * s.plotDepthMeters;
                            var c = new Vec2(e.pts[i].x + side * nxv * (f + bodyD / 2), e.pts[i].z + side * nzv * (f + bodyD / 2));
                            double yaw = Math.Atan2(-side * nxv, -side * nzv) * 180 / Math.PI;   // 정면 = 길 쪽
                            var hp = new HousePlot { center = c, yawDeg = yaw, width = model >= 0 ? bodyW : front - 0.6, depth = bodyD, alley = e.alley, row = row, hash = h, model = model, edge = ei, idx = i, side = side };
                            {
                                // 기준점 = 몸체 가운데 − (오른쪽·앞 방향으로 본 모델 바닥 가운데 오프셋)
                                double cxm = model >= 0 && s.houseModelCenterX.Length > model ? s.houseModelCenterX[model] : 0;
                                double czm = model >= 0 && s.houseModelCenterZ.Length > model ? s.houseModelCenterZ[model] : 0;
                                double rr = yaw * Math.PI / 180, fx = Math.Sin(rr), fz = Math.Cos(rr), rx = fz, rz = -fx;
                                hp.pivot = new Vec2(c.x - (rx * cxm + fx * czm), c.z - (rz * cxm + fz * czm));
                            }
                            Footprint(hp, t, out double mn, out double mx, out bool outB);
                            if (outB) { skipBounds++; break; }
                            // 집 앉히기(§11.1): 앞줄 큰길 집 바닥 = 앞 보도 + frontFloorAboveSidewalk, 그 밖 = 바닥 밑 최고 지형.
                            // 바닥 − 최저 지형(내리막 쪽 드러나는 높이) ≤ 3m, 최고 지형 − 바닥(오르막 쪽 깎는 깊이) ≤ 3m 여야 앉힌다. 넘으면 한 단 뒤 / 비움.
                            double floorY = (row == 0 && !e.alley) ? e.y[i] + s.curbHeightMeters + s.frontFloorAboveSidewalk : mx;
                            if (floorY - mn > s.seatTerraceMaxMeters || mx - floorY > s.seatTerraceMaxMeters) { if (row == 0) continue; skipSteep++; break; }
                            hp.terrainMin = mn; hp.terrainMax = mx; hp.plinthY = floorY;
                            plot = hp; if (row == 1) row2++;
                        }
                        if (plot == null) continue;
                        if (s.houseRegionRadius > 0)
                        {
                            var hc0 = res.houseRegionCenter ?? new Vec2(s.centerSearchX, s.centerSearchZ);
                            if (SplineEdge.Dist(plot.center, hc0) > s.houseRegionRadius) continue;
                        }
                        if (FootInSea(plot, res, s)) { skipSea++; continue; }
                        cands.Add(plot);
                    }
                }
            }
            // 낮은 곳(항구)부터, 같은 높이면 결정적으로
            foreach (var hp in cands.OrderBy(x => x.plinthY).ThenBy(x => x.center.x).ThenBy(x => x.center.z))
            {
                if (res.houses.Count >= s.houseCap) break;
                var poly = Inflate(hp, 0.2);
                if (TooCloseToRoad(poly, res, s)) { skipRoad++; continue; }
                if (TouchesWall(poly, res)) { skipWall++; continue; }
                if (res.houses.Any(o => RectOverlap(poly, o.Corners()))) { skipOverlap++; continue; }
                res.houses.Add(hp);
            }
            SeatHouses(res, s);
            Driveways(res, s);
            var rep = res.report;
            rep.Metric("houses", $"{res.houses.Count}채 (상한 {s.houseCap}) · 후보 {cands.Count} · 큰길 {res.houses.Count(x => !x.alley)} / 골목 {res.houses.Count(x => x.alley)} · 뒤 단(계단식) {res.houses.Count(x => x.row == 1)}");
            rep.Metric("house_skips", $"커브 안쪽 {skipCurve} · 드러남·깎음 >{s.seatTerraceMaxMeters}m {skipSteep} · 바다 {skipSea} · 길·보도 겹침 {skipRoad} · 옹벽 겹침 {skipWall} · 집끼리 겹침 {skipOverlap} · 땅 밖 {skipBounds}");
            double maxPl = res.houses.Count == 0 ? 0 : res.houses.Max(x => x.plinthY - x.terrainMin);
            rep.Metric("plinth_max_m", $"{maxPl:0.00} (≤ {s.plinthMaxMeters})");
            // 광장 상점은 광장 변에 붙는 게 의도(셋백 0) — 길·보도 겹침과 광장 바닥 자체 겹침만 센다
            int roadOverlap = res.houses.Count(x => x.shop ? TooCloseToRoadNoPlaza(x.Corners(), res, s) || (res.plaza != null && RectOverlap(x.Corners(), res.plaza.Corners(-0.05))) : TooCloseToRoad(x.Corners(), res, s));
            int seaHouse = res.houses.Count(x => FootInSea(x, res, s));
            rep.Check("M4-overlap", roadOverlap == 0, $"길·보도와 겹친 집 {roadOverlap} / {res.houses.Count}");
            rep.Check("M5-house", seaHouse == 0, $"바다 위 집 {seaHouse}");
        }

        /// <summary>밀도 지도(구조 스펙 D-113 §4) 집 후보 — 필지 가운데·정면 방향(길 쪽)·모델·장면(0 중심 · 1 가장자리 …).</summary>
        public struct HouseCand { public Vec2 c; public double yaw; public int model, scene; }

        /// <summary>
        /// 밀도 지도 후보를 기존 규칙으로 걸러 놓는다(큰길 옆 줄짓기 대신, §4·§4.1):
        /// 땅 밖 · 바다 · 앉히기 한계(드러남·깎음 > seatTerraceMax) · 길·보도·광장 겹침 · 옹벽 · 다른 집과 겹침 · 상한 → 탈락 이유별로 센다.
        /// 바닥 = 큰길 어깨 가까운(앞줄) 집은 보도 + frontFloorAboveSidewalk, 그 밖은 바닥 밑 최고 지형. 붙는 길(진입로용)은 가장 가까운 큰길·골목.
        /// 다 놓은 뒤 앉히기·진입로를 다시 돈다(광장 상점 포함 — 같은 결과).
        /// </summary>
        public static Dictionary<string, int> PlaceDensityHouses(SplineTownResult res, SplineTownSettings s, List<HouseCand> cands, int cap)
        {
            var why = new Dictionary<string, int>(); void Skip(string k) => why[k] = (why.TryGetValue(k, out int v) ? v : 0) + 1;
            var t = res.terrain; int placed = 0;
            foreach (var cd in cands)
            {
                if (res.houses.Count >= cap) { Skip("cap"); continue; }
                int model = cd.model;
                double bodyW = s.houseModelWidths[model], bodyD = s.houseModelDepths[model];
                var hp = new HousePlot { center = cd.c, yawDeg = cd.yaw, width = bodyW, depth = bodyD, model = model, row = 0, hash = cd.c.GetHashCode() & 0x7fffffff };
                double rr = cd.yaw * Math.PI / 180, fx = Math.Sin(rr), fz = Math.Cos(rr), rx = fz, rz = -fx;
                double cxm = s.houseModelCenterX.Length > model ? s.houseModelCenterX[model] : 0, czm = s.houseModelCenterZ.Length > model ? s.houseModelCenterZ[model] : 0;
                hp.pivot = new Vec2(cd.c.x - (rx * cxm + fx * czm), cd.c.z - (rz * cxm + fz * czm));
                Footprint(hp, t, out double mn, out double mx, out bool outB);
                if (outB) { Skip("out_of_bounds"); continue; }
                if (FootInSea(hp, res, s)) { Skip("sea"); continue; }
                // 붙는 길(가장 가까운 중심선) — 앞줄이면 보도 높이 기준 바닥
                SplineEdge host = null; int hostIdx = 0; double hd = double.MaxValue;
                for (int ei = 0; ei < res.edges.Count; ei++) { var e = res.edges[ei]; for (int i = 0; i < e.pts.Count; i++) { double d = SplineEdge.Dist(e.pts[i], cd.c); if (d < hd) { hd = d; host = e; hostIdx = i; hp.edge = ei; hp.idx = i; } } }
                if (host != null)
                {
                    var (nx, nz) = Normal(host.pts, hostIdx);
                    hp.side = (cd.c.x - host.pts[hostIdx].x) * nx + (cd.c.z - host.pts[hostIdx].z) * nz >= 0 ? 1 : -1;
                    hp.alley = host.alley;
                }
                bool front = host != null && !host.alley && hd - bodyD / 2 <= s.HalfShoulder + s.houseFrontGapMeters + 1.5;
                double floorY = front ? host.y[hostIdx] + s.curbHeightMeters + s.frontFloorAboveSidewalk : mx;
                if (floorY - mn > s.seatTerraceMaxMeters || mx - floorY > s.seatTerraceMaxMeters) { Skip("seat_over_3m"); continue; }
                hp.terrainMin = mn; hp.terrainMax = mx; hp.plinthY = floorY;
                var poly = Inflate(hp, 0.2);
                if (TooCloseToRoad(poly, res, s)) { Skip("road_or_plaza"); continue; }
                if (TouchesWall(poly, res)) { Skip("retaining_wall"); continue; }
                if (res.houses.Any(o => RectOverlap(poly, o.Corners()))) { Skip("house_overlap"); continue; }
                res.houses.Add(hp); placed++;
            }
            SeatHouses(res, s);
            Driveways(res, s);
            why["placed"] = placed;
            return why;
        }

        static Vec2[] Inflate(HousePlot hp, double m)
        {
            var big = new HousePlot { center = hp.center, yawDeg = hp.yawDeg, width = hp.width + 2 * m, depth = hp.depth + 2 * m };
            return big.Corners();
        }

        static IEnumerable<Vec2> FootSamples(Vec2[] poly, double step = 1.0)
        {
            var a = poly[0]; var b = poly[1]; var d = poly[3];
            double w = SplineEdge.Dist(a, b), h = SplineEdge.Dist(a, d);
            int nw = Math.Max(1, (int)Math.Ceiling(w / step)), nh = Math.Max(1, (int)Math.Ceiling(h / step));
            for (int i = 0; i <= nw; i++)
                for (int j = 0; j <= nh; j++)
                {
                    double u = (double)i / nw, v = (double)j / nh;
                    yield return new Vec2(a.x + (b.x - a.x) * u + (d.x - a.x) * v, a.z + (b.z - a.z) * u + (d.z - a.z) * v);
                }
        }

        static void Footprint(HousePlot hp, TerrainGrid t, out double mn, out double mx, out bool outOfBounds)
        {
            mn = double.MaxValue; mx = double.MinValue; outOfBounds = false;
            double maxX = (t.nx - 1) * t.step, maxZ = (t.nz - 1) * t.step;
            foreach (var p in FootSamples(hp.Corners()))
            {
                if (p.x < 0 || p.z < 0 || p.x > maxX || p.z > maxZ) { outOfBounds = true; return; }
                double y = t.Surface(p.x, p.z); mn = Math.Min(mn, y); mx = Math.Max(mx, y);
            }
        }

        static bool FootInSea(HousePlot hp, SplineTownResult res, SplineTownSettings s)
        {
            foreach (var p in FootSamples(hp.Corners(), 2.0))
            {
                int c = (int)Math.Floor(p.x / s.seaCellMeters), r = (int)Math.Floor(p.z / s.seaCellMeters);
                if (res.sea.InBounds(c, r) && res.sea.sea[res.sea.Index(c, r)]) return true;
            }
            return false;
        }

        /// <summary>
        /// 집 몸체 사각형이 어떤 길의 단면(큰길: 어깨 끝 6m, 골목: 골목 가장자리 1.5m)이나 교차점 원판 안으로 들어오나.
        /// 표본점이 아니라 사각형 변 ↔ 길 중심선 선분의 최소 거리로 판정(BUG-007: 표본 사이로 빠지던 겹침).
        /// </summary>
        /// <summary>
        /// 집마다 길 가장자리(큰길 = 보도 바깥 끝, 골목 = 골목 가장자리)에서 현관(몸체 정면 가운데)까지 포장 진입로(그래픽 결함 2: 집이 가로에 속하게).
        /// 다른 집·다른 길 단면과 겹치면 안 낸다. 길이 대비 높이차가 driveStepGrade 넘으면 계단. 진입로가 지나는 옹벽 조각은 걷어 낸다(계단 입구).
        /// </summary>
        static void Driveways(SplineTownResult res, SplineTownSettings s)
        {
            int made = 0, steps = 0, blocked = 0, wallCut = 0, tooLong = 0;
            foreach (var hp in res.houses)
            {
                if (hp.edge < 0) continue;
                var e = res.edges[hp.edge];
                double r = hp.yawDeg * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r);
                var B = new Vec2(hp.center.x + fx * hp.depth / 2, hp.center.z + fz * hp.depth / 2);
                // 길 가장자리 위 시작점: 현관에서 길 중심선에 내린 발 + 가장자리 오프셋
                double best = double.MaxValue; int bi = hp.idx; double bt = 0;
                for (int i = Math.Max(0, hp.idx - 6); i + 1 < e.pts.Count && i <= hp.idx + 6; i++) { double d = DistToSeg(B, e.pts[i], e.pts[i + 1], out double tt); if (d < best) { best = d; bi = i; bt = tt; } }
                if (bi + 1 >= e.pts.Count) continue;
                var foot = new Vec2(e.pts[bi].x + (e.pts[bi + 1].x - e.pts[bi].x) * bt, e.pts[bi].z + (e.pts[bi + 1].z - e.pts[bi].z) * bt);
                double footY = e.y[bi] + (e.y[bi + 1] - e.y[bi]) * bt;
                double ux = B.x - foot.x, uz = B.z - foot.z, ul = Math.Sqrt(ux * ux + uz * uz); if (ul < 1e-6) continue; ux /= ul; uz /= ul;
                double edgeOff = e.alley ? s.alleyHalfWidthMeters : s.HalfSection;
                var A = new Vec2(foot.x + ux * edgeOff, foot.z + uz * edgeOff);
                double len = SplineEdge.Dist(A, B); if (len < 0.3) continue;
                if (len > s.driveMaxMeters) { tooLong++; continue; }   // 밀도 지도 집이 길에서 멀면 진입로 대신 공용 계단(§11.2, 다음 조각)
                double w = Math.Min(s.driveWidthMeters, hp.width - 1.0); if (w < 1.2) continue;
                // 진입로 사각형(겹침 판정) — 끝은 현관 앞 0.1m 덜
                double px = -uz, pz = ux;
                Vec2 Q(double along, double lat) => new Vec2(A.x + ux * along + px * lat, A.z + uz * along + pz * lat);
                var rect = new[] { Q(0.05, -w / 2), Q(0.05, w / 2), Q(len - 0.1, w / 2), Q(len - 0.1, -w / 2) };
                bool bad = res.houses.Any(o => o != hp && RectOverlap(rect, o.Corners()));
                if (!bad)
                    foreach (var o in res.edges)
                    {
                        if (o == e) continue;
                        double lim = o.alley ? s.alleyHalfWidthMeters : s.HalfSection;
                        for (int i = 0; i + 1 < o.pts.Count && !bad; i++)
                        {
                            var a = o.pts[i]; var b = o.pts[i + 1];
                            if (Math.Min(a.x, b.x) > Math.Max(A.x, B.x) + lim + w || Math.Max(a.x, b.x) < Math.Min(A.x, B.x) - lim - w || Math.Min(a.z, b.z) > Math.Max(A.z, B.z) + lim + w || Math.Max(a.z, b.z) < Math.Min(A.z, B.z) - lim - w) continue;
                            for (int k = 0; k < 4 && !bad; k++) if (SegSegDist(rect[k], rect[(k + 1) % 4], a, b) < lim) bad = true;
                        }
                        if (bad) break;
                    }
                if (bad) { blocked++; continue; }
                hp.hasDrive = true; hp.driveA = A; hp.driveB = B; hp.driveW = w;
                hp.driveAY = footY + (e.alley ? 0 : s.curbHeightMeters); hp.driveBY = hp.plinthY;
                hp.driveLanding = Math.Min(s.driveLandingMeters, len * 0.3);
                var arcD = new double[e.pts.Count]; for (int i = 1; i < e.pts.Count; i++) arcD[i] = arcD[i - 1] + SplineEdge.Dist(e.pts[i - 1], e.pts[i]);
                double RoadY(Vec2 q)
                {
                    double bd = double.MaxValue, by = footY;
                    for (int i = Math.Max(0, bi - 6); i + 1 < e.pts.Count && i <= bi + 6; i++)
                    {
                        double d = DistToSeg(q, e.pts[i], e.pts[i + 1], out double tt);
                        if (d < bd)
                        {
                            bd = d;
                            double lat = (e.pts[i + 1].x - e.pts[i].x) * (q.z - e.pts[i].z) - (e.pts[i + 1].z - e.pts[i].z) * (q.x - e.pts[i].x) > 0 ? d : -d;
                            by = e.y[i] + (e.y[i + 1] - e.y[i]) * tt + BankAt(e, arcD[i] + (arcD[i + 1] - arcD[i]) * tt, s) * lat;
                        }
                    }
                    return by + (e.alley ? 0 : s.curbHeightMeters);
                }
                hp.driveAYL = RoadY(new Vec2(A.x + px * w / 2, A.z + pz * w / 2)); hp.driveAYR = RoadY(new Vec2(A.x - px * w / 2, A.z - pz * w / 2));
                hp.driveAY = RoadY(A);   // 기운 단면(교차점 평면 쪽)이면 그 자리 보도 높이
                hp.driveSteps = Math.Abs(hp.driveBY - hp.driveAY) / len > s.driveStepGrade;
                if (hp.driveSteps) steps++;
                made++;
                int wb = res.walls.Count;
                res.walls.RemoveAll(wl => { for (int k = 0; k < 4; k++) if (SegIntersect(wl.a, wl.b, rect[k], rect[(k + 1) % 4])) return true; return PointInPoly(wl.a, rect); });
                wallCut += wb - res.walls.Count;
            }
            int capped = CapUnderDriveways(res, s);
            res.report.Metric("driveways", $"{made} / 집 {res.houses.Count} (밑 지형 누른 정점 {capped} · {s.driveMaxMeters:0}m 넘어 안 냄 {tooLong}) (계단 {steps} · 다른 집·길과 겹쳐 못 냄 {blocked} · 걷어 낸 옹벽 조각 {wallCut})");
        }

        /// <summary>
        /// 집 앉히기(§11.1): 바닥 밑(0.3m 여유) 지형 정점 중 바닥보다 높은 것은 바닥 −0.02 로 깎는다(오르막 쪽 묻힘 방지).
        /// 드러나는 높이(바닥 − 최저 지형) ≤ 0.3m 면 낮은 정점도 바닥 −0.02 로 돋워 땅에 바로 앉힌다(받침대 없음).
        /// 0.3~1.2m 는 벽을 땅까지 내림, 1.2~3m 는 반지하 층(씬에서 몸체와 같은 크기의 아래 덩어리, 따로 튀어나온 받침대 아님).
        /// </summary>
        static void SeatHouses(SplineTownResult res, SplineTownSettings s)
        {
            var t = res.terrain; int[] count = new int[3]; int cut = 0, fill = 0;
            double diag = t.step * Math.Sqrt(2) + 0.05;
            var plazaNo = res.plaza?.Corners(0.5);
            bool NearRoad(Vec2 v)
            {
                if (plazaNo != null && PointInPoly(v, plazaNo)) return true;   // 광장 바닥 밑은 돋우지 않는다
                foreach (var e in res.edges) { double reach = (e.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder) + diag; if (DistToPoly(v, e.pts) < reach) return true; }
                return false;
            }
            foreach (var hp in res.houses)
            {
                var body = Inflate(hp, 0.3);
                double exposed = hp.plinthY - hp.terrainMin;
                hp.seat = exposed <= s.seatGroundMeters ? 0 : exposed <= s.seatWallMeters ? 1 : 2;
                bool unfilled = false;
                int i0 = Math.Max(0, (int)Math.Floor(body.Min(q => q.x) / t.step)), i1 = Math.Min(t.nx - 1, (int)Math.Ceiling(body.Max(q => q.x) / t.step));
                int j0 = Math.Max(0, (int)Math.Floor(body.Min(q => q.z) / t.step)), j1 = Math.Min(t.nz - 1, (int)Math.Ceiling(body.Max(q => q.z) / t.step));
                double lowest = double.MaxValue;
                for (int j = j0; j <= j1; j++)
                    for (int i = i0; i <= i1; i++)
                    {
                        var v = new Vec2(i * t.step, j * t.step); if (!PointInPoly(v, body)) continue;
                        int idx = j * t.nx + i; double target = hp.plinthY - 0.02;
                        if (t.h[idx] > target) { t.h[idx] = target; cut++; }
                        else if (hp.seat == 0 && t.h[idx] < target - 0.03)
                        {
                            if (NearRoad(v)) unfilled = true;   // 길 단면 영향권 안은 돋우지 않는다(길 위로 지형이 솟음) → 벽 내림으로 틈을 가림
                            else { t.h[idx] = target; fill++; }
                        }
                        lowest = Math.Min(lowest, t.h[idx]);
                    }
                if (hp.seat == 0 && unfilled) hp.seat = 1;
                count[hp.seat]++;
                // 밑단 = 몸체 둘레 지형 최저 − 0.3 (벽·반지하가 땅에 묻혀 끝나게)
                double edgeMin = hp.terrainMin;
                foreach (var q in FootSamples(hp.Corners())) edgeMin = Math.Min(edgeMin, t.Surface(q.x, q.z));
                hp.seatBottom = Math.Min(edgeMin, lowest == double.MaxValue ? edgeMin : lowest) - 0.3;
            }
            res.report.Metric("house_seating", $"땅에 바로 {count[0]} · 벽 내림 {count[1]} · 반지하 {count[2]} (따로 튀어나온 받침대 0) · 바닥 밑 깎은 정점 {cut} · 돋운 정점 {fill}");
        }

        static Vec2[] TowerCorners(PlazaInfo pz, double grow)
        {
            double r = pz.towerYaw * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx, h = pz.towerSize / 2 + grow;
            return new[] { new Vec2(pz.towerC.x + fx * h + rx * h, pz.towerC.z + fz * h + rz * h), new Vec2(pz.towerC.x + fx * h - rx * h, pz.towerC.z + fz * h - rz * h),
                           new Vec2(pz.towerC.x - fx * h - rx * h, pz.towerC.z - fz * h - rz * h), new Vec2(pz.towerC.x - fx * h + rx * h, pz.towerC.z - fz * h + rz * h) };
        }

        /// <summary>
        /// 마을 중심(§11.3): 교차점(갈래 ≥3)마다 가장 넓게 벌어진 방향으로 길 옆에 광장 자리(30×24, 긴 변이 교차점을 봄)를 놓고
        /// 반경 30m 원 지형 경사 평균 · 바다 쪽 90° 부채꼴 광선 바다 보임 비율(150m, 지형만) · 해안 큰길(바다 60m 안 큰길 점)까지 거리 · 갈래 수로 점수(같은 무게).
        /// 조건(경사 ≤5%·바다 ≥30%·해안 ≤80m) 통과 1위, 없으면 7%·20% 로 한 번 완화. 광장 바닥은 경사 ≤3% 평면으로 고르고, 오르막 쪽 변에 종탑 자리, 나머지 세 변에 상점.
        /// </summary>
        static void PlacePlaza(SplineTownResult res, IHeightField H, SplineTownSettings s)
        {
            var t = res.terrain; var rep = res.report;
            bool IsSea(Vec2 q) { int cc = (int)Math.Floor(q.x / s.seaCellMeters), rr = (int)Math.Floor(q.z / s.seaCellMeters); return res.sea.InBounds(cc, rr) && res.sea.sea[res.sea.Index(cc, rr)]; }
            var seaPts = new List<Vec2>();
            for (int rr = 0; rr < res.sea.H; rr++) for (int cc = 0; cc < res.sea.W; cc++) if (res.sea.sea[res.sea.Index(cc, rr)]) seaPts.Add(new Vec2((cc + 0.5) * s.seaCellMeters, (rr + 0.5) * s.seaCellMeters));
            var coast = new List<Vec2>();
            foreach (var e in res.Mains) foreach (var q in e.pts) if (seaPts.Any(sp => SplineEdge.Dist(sp, q) < 60)) coast.Add(q);
            var cands = new List<PlazaInfo>();
            // 광장 바닥(2m 간격 표본)이 바다 칸 위로 나가는가 — 가장자리가 물가에 닿는 것은 허용(§11.3 개정)
            bool OverSea(PlazaInfo pz)
            {
                var cs = pz.Corners();
                for (double a = 0.02; a <= 0.98; a += 2 / pz.length) for (double b = 0.02; b <= 0.98; b += 2 / pz.width)
                    if (IsSea(new Vec2(cs[2].x + (cs[1].x - cs[2].x) * a + (cs[3].x - cs[2].x) * b, cs[2].z + (cs[1].z - cs[2].z) * a + (cs[3].z - cs[2].z) * b))) return true;
                return false;
            }
            foreach (var jl in res.junctionLayouts)
            {
                var node = res.nodes[jl.node];
                // 가장 넓은 사이각의 가운데 방향
                double bestGap = -1, bestDir = 0;
                var angs = jl.arms.Select(a => Math.Atan2(a.u.z, a.u.x)).OrderBy(a => a).ToList();
                for (int k = 0; k < angs.Count; k++) { double a0 = angs[k], a1 = k + 1 < angs.Count ? angs[k + 1] : angs[0] + 2 * Math.PI; if (a1 - a0 > bestGap) { bestGap = a1 - a0; bestDir = (a0 + a1) / 2; } }
                var dir = new Vec2(Math.Cos(bestDir), Math.Sin(bestDir));
                foreach (double back in new[] { 0.0, 6.0, 12.0 })
                {
                    double dist = s.HalfShoulder + s.plazaWidth / 2 + 1 + back;
                    var pz = new PlazaInfo { c = new Vec2(node.x + dir.x * dist, node.z + dir.z * dist), length = s.plazaLength, width = s.plazaWidth, node = jl.node, arms = jl.arms.Count };
                    pz.yawDeg = Math.Atan2(-dir.z, dir.x) * 180 / Math.PI;   // 긴 변 = dir 에 수직 (yaw: +z=0°, +x=90° → 긴 변 방향 = (sin, cos) ⟂ dir)
                    var cs = pz.Corners(1.0);
                    if (cs.Any(q => q.x < 2 || q.z < 2 || q.x > s.boundsXMeters - 2 || q.z > s.boundsZMeters - 2) || OverSea(pz)) continue;
                    bool onRoad = false;
                    foreach (var e in res.edges) { foreach (var q in e.pts) if (PointInPoly(q, cs)) { onRoad = true; break; } if (onRoad) break; for (int i = 0; i + 1 < e.pts.Count && !onRoad; i++) for (int k = 0; k < 4 && !onRoad; k++) if (SegSegDist(cs[k], cs[(k + 1) % 4], e.pts[i], e.pts[i + 1]) < (e.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder)) onRoad = true; }
                    if (onRoad) continue;
                    Score(pz);
                    cands.Add(pz);
                    break;   // 교차점마다 처음 들어가는 거리 하나
                }
            }
            void Score(PlazaInfo pz)
            {
                {
                    // 평지(§11.3 개정): 광장 바닥 자리 안 원래 지형 경사 평균(±1m 차분, 2m 간격)
                    double sum = 0; int n = 0; var rc = pz.Corners();
                    for (double ua = 0.02; ua <= 0.98; ua += 2 / pz.length) for (double vb = 0.02; vb <= 0.98; vb += 2 / pz.width)
                    {
                        double x = rc[2].x + (rc[1].x - rc[2].x) * ua + (rc[3].x - rc[2].x) * vb, z = rc[2].z + (rc[1].z - rc[2].z) * ua + (rc[3].z - rc[2].z) * vb;
                        double gxx = (H.HeightAt(x + 1, z) - H.HeightAt(x - 1, z)) / 2, gzz = (H.HeightAt(x, z + 1) - H.HeightAt(x, z - 1)) / 2;
                        sum += Math.Sqrt(gxx * gxx + gzz * gzz); n++;
                    }
                    pz.slope30 = n > 0 ? sum / n : 1;
                    // 바다 보임: 가장 가까운 바다 쪽 ±45°, 19 광선, 2m 걸음 150m, 눈높이 1.6m, 지형만
                    var nearSea = seaPts.OrderBy(q => SplineEdge.Dist(q, pz.c)).FirstOrDefault();
                    double eye = t.Surface(pz.c.x, pz.c.z) + 1.6, seaDir = Math.Atan2(nearSea.z - pz.c.z, nearSea.x - pz.c.x); int seen = 0;
                    for (int k = 0; k < 19; k++)
                    {
                        double a = seaDir + (k / 18.0 - 0.5) * Math.PI / 2;
                        for (double d = 2; d <= 150; d += 2)
                        {
                            var q = new Vec2(pz.c.x + Math.Cos(a) * d, pz.c.z + Math.Sin(a) * d);
                            if (IsSea(q)) { seen++; break; }
                            if (q.x < 0 || q.z < 0 || q.x > s.boundsXMeters || q.z > s.boundsZMeters) break;
                            if (t.Surface(q.x, q.z) > eye) break;
                        }
                    }
                    pz.seeSea = seen / 19.0;
                    pz.coastDist = coast.Count > 0 ? coast.Min(q => SplineEdge.Dist(q, pz.c)) : double.MaxValue;
                    pz.score = pz.seeSea + Math.Max(0, 1 - pz.slope30 / 0.05) + Math.Max(0, Math.Min(1, (pz.arms - 2) / 2.0));
                }
            }
            PlazaInfo pick = null;
            // 임시(기획 확인 전): 교차점 후보가 조건(완화 포함)을 못 넘으면 큰길 옆(갈래 1) 자리도 본다 — plazaRoadsideFallback 으로 끔
            if (s.plazaRoadsideFallback)
            {
                var road = new List<PlazaInfo>();
                foreach (var e in res.Mains)
                {
                    double acc = 0;
                    for (int k = 1; k < e.pts.Count; k++)
                    {
                        acc += SplineEdge.Dist(e.pts[k - 1], e.pts[k]); if (acc < 10) continue; acc = 0;
                        var (nx, nz) = Normal(e.pts, k);
                        foreach (int sg in new[] { 1, -1 })
                        {
                            double dist = s.HalfShoulder + s.plazaWidth / 2 + 1;
                            var pz = new PlazaInfo { c = new Vec2(e.pts[k].x + sg * nx * dist, e.pts[k].z + sg * nz * dist), length = s.plazaLength, width = s.plazaWidth, node = e.a, arms = 1, roadside = true };
                            pz.yawDeg = Math.Atan2(nz, -nx) * 180 / Math.PI;   // 긴 변 = 길 방향 (sin, cos) = 접선 (nz, −nx)
                            var cs = pz.Corners(1.0);
                            if (cs.Any(q => q.x < 2 || q.z < 2 || q.x > s.boundsXMeters - 2 || q.z > s.boundsZMeters - 2) || OverSea(pz)) continue;
                            bool onRoad = false;
                            foreach (var o in res.edges) { for (int i = 0; i + 1 < o.pts.Count && !onRoad; i++) for (int q = 0; q < 4 && !onRoad; q++) if (SegSegDist(cs[q], cs[(q + 1) % 4], o.pts[i], o.pts[i + 1]) < (o.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder)) onRoad = true; if (onRoad) break; }
                            if (onRoad) continue;
                            Score(pz); road.Add(pz);
                        }
                    }
                }
                cands.AddRange(road);
            }
            if (s.plazaJunctionSearchRadius > 0)
            {
                // §3(구조 스펙): 교차점 반경 안 자리 훑기 — 교차점마다 바닥 자리 경사가 가장 낮은 자리 하나를 후보로
                foreach (var jl in res.junctionLayouts)
                {
                    var node = res.nodes[jl.node];
                    if (s.centerSearchRadius > 0 && SplineEdge.Dist(node, new Vec2(s.centerSearchX, s.centerSearchZ)) > s.centerSearchRadius + s.plazaJunctionSearchRadius) continue;
                    PlazaInfo best = null;
                    for (double dx = -s.plazaJunctionSearchRadius; dx <= s.plazaJunctionSearchRadius; dx += 6)
                        for (double dz = -s.plazaJunctionSearchRadius; dz <= s.plazaJunctionSearchRadius; dz += 6)
                        {
                            if (dx * dx + dz * dz > s.plazaJunctionSearchRadius * s.plazaJunctionSearchRadius) continue;
                            foreach (double yaw in new[] { 0.0, 30, 60, 90, 120, 150 })
                            {
                                var pz = new PlazaInfo { c = new Vec2(node.x + dx, node.z + dz), length = s.plazaLength, width = s.plazaWidth, node = jl.node, arms = jl.arms.Count, yawDeg = yaw };
                                var cs = pz.Corners(1.0);
                                if (cs.Any(q => q.x < 2 || q.z < 2 || q.x > s.boundsXMeters - 2 || q.z > s.boundsZMeters - 2) || OverSea(pz)) continue;
                                bool onRoad = false;
                                foreach (var o in res.edges) { for (int i = 0; i + 1 < o.pts.Count && !onRoad; i++) { if (Math.Min(o.pts[i].x, o.pts[i + 1].x) > cs.Max(q => q.x) + 8 || Math.Max(o.pts[i].x, o.pts[i + 1].x) < cs.Min(q => q.x) - 8 || Math.Min(o.pts[i].z, o.pts[i + 1].z) > cs.Max(q => q.z) + 8 || Math.Max(o.pts[i].z, o.pts[i + 1].z) < cs.Min(q => q.z) - 8) continue; for (int q = 0; q < 4 && !onRoad; q++) if (SegSegDist(cs[q], cs[(q + 1) % 4], o.pts[i], o.pts[i + 1]) < (o.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder)) onRoad = true; if (!onRoad && PointInPoly(o.pts[i], cs)) onRoad = true; } if (onRoad) break; }
                                if (onRoad) continue;
                                // 큰길에 붙어 있어야(가장자리가 큰길 어깨 끝에서 4m 안) — 차로 들어갈 수 있는 광장(스펙 §11.3 "붙는 큰길은 광장 가장자리에서")
                                bool touches = false;
                                foreach (var o in res.edges) { if (o.alley) continue; for (int i = 0; i + 1 < o.pts.Count && !touches; i++) for (int q = 0; q < 4 && !touches; q++) if (SegSegDist(cs[q], cs[(q + 1) % 4], o.pts[i], o.pts[i + 1]) < s.HalfShoulder + 4) touches = true; if (touches) break; }
                                if (!touches) continue;
                                // 경사만 먼저(싸다) — 최저 자리만 바다 보임까지 잰다
                                double sum = 0; int n = 0; var rc = pz.Corners();
                                for (double ua = 0.02; ua <= 0.98; ua += 2 / pz.length) for (double vb = 0.02; vb <= 0.98; vb += 2 / pz.width)
                                {
                                    double x = rc[2].x + (rc[1].x - rc[2].x) * ua + (rc[3].x - rc[2].x) * vb, z = rc[2].z + (rc[1].z - rc[2].z) * ua + (rc[3].z - rc[2].z) * vb;
                                    double gxx = (H.HeightAt(x + 1, z) - H.HeightAt(x - 1, z)) / 2, gzz = (H.HeightAt(x, z + 1) - H.HeightAt(x, z - 1)) / 2;
                                    sum += Math.Sqrt(gxx * gxx + gzz * gzz); n++;
                                }
                                pz.slope30 = sum / Math.Max(1, n);
                                if (best == null || pz.slope30 < best.slope30) best = pz;
                            }
                        }
                    if (best != null) { double sl = best.slope30; Score(best); best.slope30 = sl; cands.Add(best); }
                }
            }
            if (s.centerSearchRadius > 0) cands.RemoveAll(c => SplineEdge.Dist(c.c, new Vec2(s.centerSearchX, s.centerSearchZ)) > s.centerSearchRadius);
            // §11.3 개정: 교차점·큰길 옆 후보 함께, 조건 = 바닥 자리 경사 ≤6% · 바다 보임 ≥30% · 해안 큰길 ≤80m, 점수 1위(갈래는 점수에만)
            pick = s.plazaJunctionSearchRadius > 0
                ? cands.Where(c => c.slope30 <= s.plazaMaxSiteGrade).OrderByDescending(c => c.score).ThenBy(c => c.c.x).FirstOrDefault()   // 구조 스펙 §3 판정
                : cands.Where(c => c.slope30 <= s.plazaMaxSiteGrade && c.seeSea >= 0.30 && c.coastDist <= 80).OrderByDescending(c => c.score).ThenBy(c => c.c.x).FirstOrDefault();
            foreach (var c in cands.OrderByDescending(c => c.score).Take(8)) rep.Metric($"plaza_cand_{cands.OrderByDescending(x => x.score).ToList().IndexOf(c) + 1}", $"({c.c.x:0},{c.c.z:0}) {(c.roadside ? "큰길 옆" : "교차점")} 갈래 {c.arms} · 바닥 자리 경사 {c.slope30 * 100:0.0}% · 바다 {c.seeSea * 100:0}% · 해안 {(c.coastDist > 1e6 ? -1 : c.coastDist):0}m · 점수 {c.score:0.00}");
            bool terrace = false;
            if (pick == null)
            {
                // §11.3: 그래도 0곳 → 바다 보임 100% 교차점 후보 중 바닥 자리 경사 최저에 테라스 광장(두 단)
                pick = cands.Where(c => !c.roadside && c.seeSea >= 0.999).OrderBy(c => c.slope30).ThenBy(c => c.c.x).FirstOrDefault();
                if (pick == null) { rep.Metric("plaza", $"자리 없음 (후보 {cands.Count}, 바다 보임 100% 교차점도 없음)"); return; }
                terrace = true;
            }
            res.plaza = pick;
            if (s.houseRegionRadius > 0) res.houseRegionCenter = pick.c;
            // 바닥 평면: 광장 안 지형에 최소제곱 맞춤 → 기울기 ≤ 3%
            {
                double sxx = 0, sxz = 0, szz = 0, sxy = 0, szy = 0, sy = 0; int n = 0; var cs = pick.Corners();
                for (double a = -0.5; a <= 0.5; a += 0.1) for (double b = -0.5; b <= 0.5; b += 0.1)
                {
                    var q = new Vec2(cs[2].x + (cs[1].x - cs[2].x) * (a + 0.5) + (cs[3].x - cs[2].x) * (b + 0.5), cs[2].z + (cs[1].z - cs[2].z) * (a + 0.5) + (cs[3].z - cs[2].z) * (b + 0.5));
                    double dx = q.x - pick.c.x, dz = q.z - pick.c.z, y = t.Surface(q.x, q.z);
                    sxx += dx * dx; sxz += dx * dz; szz += dz * dz; sxy += dx * y; szy += dz * y; sy += y; n++;
                }
                pick.y = sy / n;
                double det = sxx * szz - sxz * sxz, gx = 0, gz = 0;
                if (Math.Abs(det) > 1e-9) { double mx = sxy - pick.y * 0, mz = szy; gx = (mx * szz - mz * sxz) / det; gz = (mz * sxx - mx * sxz) / det; }
                double gm = Math.Sqrt(gx * gx + gz * gz); if (gm > s.plazaMaxGrade) { gx *= s.plazaMaxGrade / gm; gz *= s.plazaMaxGrade / gm; }
                pick.gx = gx; pick.gz = gz;
            }
            if (terrace)
            {
                // 지형 기울기(맞춤, 자르기 전) 방향에 더 가까운 광장 축을 단 축으로 — 그 축 가운데 선에서 두 단으로 나눈다
                double r = pick.yawDeg * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx;
                var cs = pick.Corners(); double gxr = 0, gzr = 0;
                { double sxx = 0, sxz = 0, szz = 0, sxy = 0, szy = 0, sy = 0; int n = 0;
                  for (double a = 0.05; a <= 0.95; a += 0.1) for (double b = 0.05; b <= 0.95; b += 0.1) { var q = new Vec2(cs[2].x + (cs[1].x - cs[2].x) * a + (cs[3].x - cs[2].x) * b, cs[2].z + (cs[1].z - cs[2].z) * a + (cs[3].z - cs[2].z) * b); double dx = q.x - pick.c.x, dz = q.z - pick.c.z, y = t.Surface(q.x, q.z); sxx += dx * dx; sxz += dx * dz; szz += dz * dz; sxy += dx * y; szy += dz * y; sy += y; n++; }
                  double det = sxx * szz - sxz * sxz; if (Math.Abs(det) > 1e-9) { gxr = (sxy * szz - szy * sxz) / det; gzr = (szy * sxx - sxy * sxz) / det; } }
                bool alongLength = Math.Abs(gxr * fx + gzr * fz) >= Math.Abs(gxr * rx + gzr * rz);
                var ax = alongLength ? new Vec2(fx, fz) : new Vec2(rx, rz);
                if (gxr * ax.x + gzr * ax.z < 0) ax = new Vec2(-ax.x, -ax.z);   // 위 단 = 오르막 쪽
                double half = (alongLength ? pick.length : pick.width) / 4;     // 각 단 가운데까지
                Vec2 Mid(double sgn) => new Vec2(pick.c.x + ax.x * half * sgn, pick.c.z + ax.z * half * sgn);
                double yLow = t.Surface(Mid(-1).x, Mid(-1).z), yUp = t.Surface(Mid(1).x, Mid(1).z);
                if (yUp - yLow > s.plazaTerraceStepMeters) { double mid = (yUp + yLow) / 2; yLow = mid - s.plazaTerraceStepMeters / 2; yUp = mid + s.plazaTerraceStepMeters / 2; }
                // 각 단 = 단 축에 수직 방향으로만 기울기 허용(≤3%), 단 축 방향은 평평
                var px = new Vec2(-ax.z, ax.x); double gp = Math.Max(-s.plazaMaxGrade, Math.Min(s.plazaMaxGrade, gxr * px.x + gzr * px.z));
                pick.terraced = true; pick.tierAxis = ax;
                pick.gx = gp * px.x; pick.gz = gp * px.z; pick.y = yLow - (pick.gx * (Mid(-1).x - pick.c.x) + pick.gz * (Mid(-1).z - pick.c.z));
                pick.gx2 = pick.gx; pick.gz2 = pick.gz; pick.y2 = yUp - (pick.gx2 * (Mid(1).x - pick.c.x) + pick.gz2 * (Mid(1).z - pick.c.z));
            }
            // 지형: 광장 안 = 평면 −0.03, 둘레 4m = 원래 지형으로 부드럽게
            int cut = 0;
            {
                var inner = pick.Corners(0.5); var outer = pick.Corners(4.5);
                int i0 = Math.Max(0, (int)Math.Floor(outer.Min(q => q.x) / t.step)), i1 = Math.Min(t.nx - 1, (int)Math.Ceiling(outer.Max(q => q.x) / t.step));
                int j0 = Math.Max(0, (int)Math.Floor(outer.Min(q => q.z) / t.step)), j1 = Math.Min(t.nz - 1, (int)Math.Ceiling(outer.Max(q => q.z) / t.step));
                for (int j = j0; j <= j1; j++)
                    for (int i = i0; i <= i1; i++)
                    {
                        var v = new Vec2(i * t.step, j * t.step); int idx = j * t.nx + i; double py = pick.Y(v) - 0.03;
                        // 테라스: 단 경계에서 격자 대각선 안 위 단 쪽 정점은 아래 단 높이로 — 경계를 넘는 지형 삼각형이 아래 단 바닥 위로 솟지 않게(석축이 가림)
                        if (pick.terraced && pick.Upper(v) && Math.Abs((v.x - pick.c.x) * pick.tierAxis.x + (v.z - pick.c.z) * pick.tierAxis.z) <= t.step * Math.Sqrt(2) + 0.05) py = pick.YTier(v, false) - 0.03;
                        if (PointInPoly(v, inner)) { t.h[idx] = py; cut++; continue; }
                        if (!PointInPoly(v, outer)) continue;
                        double d = 4; for (int k = 0; k < 4; k++) d = Math.Min(d, DistToSeg(v, inner[k], inner[(k + 1) % 4], out _));
                        double w = d / 4; w = w * w * (3 - 2 * w);
                        t.h[idx] = py + (t.h[idx] - py) * w; cut++;
                    }
            }
            // 종탑: 긴 변 두 쪽 중 평면이 높은 쪽(오르막) 가운데, 광장 안쪽 가장자리에 붙여 · 바다(광장 반대쪽)를 봄
            {
                double r = pick.yawDeg * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx;
                var sideA = new Vec2(pick.c.x + rx * (pick.width / 2 - s.towerSize / 2), pick.c.z + rz * (pick.width / 2 - s.towerSize / 2));
                var sideB = new Vec2(pick.c.x - rx * (pick.width / 2 - s.towerSize / 2), pick.c.z - rz * (pick.width / 2 - s.towerSize / 2));
                bool aUp = pick.Y(sideA) >= pick.Y(sideB);
                pick.towerC = aUp ? sideA : sideB; pick.towerSize = s.towerSize; pick.towerHeight = s.towerHeight;
                var face = aUp ? new Vec2(-rx, -rz) : new Vec2(rx, rz);   // 광장 가운데 쪽
                pick.towerYaw = Math.Atan2(face.x, face.z) * 180 / Math.PI;
            }
            // 상점: 종탑 쪽을 뺀 세 변 바깥에 붙여(셋백 0) 정면이 광장을 봄 · 모델 = CornerShop 계열, 이웃 같은 모델 금지
            int shops = 0, shopSkip = 0;
            var shopModels = new List<int>();
            if (s.houseModelNames != null) for (int k = 0; k < s.houseModelNames.Length; k++) if (s.houseModelNames[k].StartsWith("CornerShop")) shopModels.Add(k);
            if (shopModels.Count > 0)
            {
                double r = pick.yawDeg * Math.PI / 180, fx = Math.Sin(r), fz = Math.Cos(r), rx = fz, rz = -fx;
                var towerSide = (pick.towerC.x - pick.c.x) * rx + (pick.towerC.z - pick.c.z) * rz > 0 ? 1 : -1;
                // 변: (바깥 법선, 변 방향, 변 길이)
                var sides = new List<(Vec2 n, Vec2 dir, double len, double off)>
                {
                    (new Vec2(-rx * towerSide, -rz * towerSide), new Vec2(fx, fz), pick.length, pick.width / 2),
                    (new Vec2(fx, fz), new Vec2(rx, rz), pick.width, pick.length / 2),
                    (new Vec2(-fx, -fz), new Vec2(rx, rz), pick.width, pick.length / 2),
                };
                int last = -1;
                foreach (var sd in sides)
                {
                    double pos = -sd.len / 2;
                    while (shops < s.shopsMax)
                    {
                        int model = shopModels[(shops + (int)(pick.c.x * 7)) % shopModels.Count]; if (model == last && shopModels.Count > 1) model = shopModels[(shopModels.IndexOf(model) + 1) % shopModels.Count];
                        double w = s.houseModelWidths[model], d = s.houseModelDepths[model];
                        if (pos + w > sd.len / 2) break;
                        double mid = pos + w / 2; pos += w + 1.0;
                        var c = new Vec2(pick.c.x + sd.n.x * (sd.off + d / 2) + sd.dir.x * mid, pick.c.z + sd.n.z * (sd.off + d / 2) + sd.dir.z * mid);
                        double yaw = Math.Atan2(-sd.n.x, -sd.n.z) * 180 / Math.PI;   // 정면 = 광장 쪽
                        var hp = new HousePlot { center = c, yawDeg = yaw, width = w, depth = d, model = model, row = 0, hash = shops, shop = true };
                        double cxm = s.houseModelCenterX.Length > model ? s.houseModelCenterX[model] : 0, czm = s.houseModelCenterZ.Length > model ? s.houseModelCenterZ[model] : 0;
                        double rr2 = yaw * Math.PI / 180, ffx = Math.Sin(rr2), ffz = Math.Cos(rr2), rrx = ffz, rrz = -ffx;
                        hp.pivot = new Vec2(c.x - (rrx * cxm + ffx * czm), c.z - (rrz * cxm + ffz * czm));
                        Footprint(hp, t, out double mn, out double mx, out bool outB);
                        var frontMid = new Vec2(pick.c.x + sd.n.x * sd.off + sd.dir.x * mid, pick.c.z + sd.n.z * sd.off + sd.dir.z * mid);
                        double floorY = pick.Y(frontMid) + 0.15;
                        var poly = Inflate(hp, 0.2);
                        if (outB || floorY - mn > s.seatTerraceMaxMeters || mx - floorY > s.seatTerraceMaxMeters || FootInSea(hp, res, s) || TooCloseToRoadNoPlaza(poly, res, s) || TouchesWall(poly, res) || res.houses.Any(o => RectOverlap(poly, o.Corners()))) { shopSkip++; continue; }
                        hp.terrainMin = mn; hp.terrainMax = mx; hp.plinthY = floorY;
                        res.houses.Add(hp); shops++; last = model;
                    }
                }
            }
            rep.Metric("plaza", $"({pick.c.x:0},{pick.c.z:0}) {pick.length:0}×{pick.width:0}m · 교차점 갈래 {pick.arms} · 바닥 자리 경사 {pick.slope30 * 100:0.0}% · 바다 보임 {pick.seeSea * 100:0}%(지형만) · 해안 큰길 {pick.coastDist:0}m · 바닥 기울기 {Math.Sqrt(pick.gx * pick.gx + pick.gz * pick.gz) * 100:0.0}%{(pick.roadside ? " · 큰길 옆(갈래 1)" : "")}{(pick.terraced ? $" · 조건 통과 0곳 → 테라스 광장 두 단(높이차 {pick.y2 - pick.y:0.00}m 기준점 차)" : "")} · 종탑 자리 ({pick.towerC.x:0},{pick.towerC.z:0}) {pick.towerHeight:0}m · 상점 {shops}(못 놓음 {shopSkip}) · 고른 지형 정점 {cut} · 후보 {cands.Count}");
        }

        static bool TooCloseToRoadNoPlaza(Vec2[] poly, SplineTownResult res, SplineTownSettings s)
        {
            var keep = res.plaza; res.plaza = null;
            try { return TooCloseToRoad(poly, res, s); } finally { res.plaza = keep; }
        }

        /// <summary>진입로 면의 아래 덮개 높이(경사판 = 직선 보간, 계단 = 이웃 디딤 중 낮은 쪽). along 은 길 쪽 끝에서 잰 거리.</summary>
        public static double DriveLowY(HousePlot hp, double along, SplineTownSettings s, bool extrapolate = false)
        {
            double len = SplineEdge.Dist(hp.driveA, hp.driveB); if (len < 1e-6) return hp.driveAY;
            double ld = hp.driveLanding;
            if (along < ld) return Math.Min(hp.driveAY, Math.Min(hp.driveAYL, hp.driveAYR));
            double run = Math.Max(1e-6, len - ld);
            // extrapolate: 현관 너머로도 같은 기울기로 잇는다(지형 누르기 전용 — 격자 2m 사이 직선 지형이 마지막 단 위로 솟지 않게)
            double t = Math.Max(0, extrapolate ? (along - ld) / run : Math.Min(1, (along - ld) / run)), y = hp.driveAY + (hp.driveBY - hp.driveAY) * t;
            if (!hp.driveSteps) return y;
            int ns = DriveStepCount(hp);
            return y - Math.Abs(hp.driveBY - hp.driveAY) / ns;
        }
        public static int DriveStepCount(HousePlot hp) => Math.Max(2, (int)Math.Ceiling(Math.Abs(hp.driveBY - hp.driveAY) / 0.18));

        /// <summary>
        /// 진입로 밑 지형을 길과 같은 두 띠로 누른다: 옆으로 반폭 + 1.5m 안 = 진입로 아래 덮개 −0.10, 반폭 + 격자 대각선 안 = 덮개 + 0.15.
        /// 다른 집 몸체(0.3m 여유) 안 정점은 건드리지 않는다. 진입로 면을 지형에 맞춰 띄우지 않아도 되므로 양 끝 높이가 정확히 맞는다(BUG-009·010).
        /// </summary>
        static int CapUnderDriveways(SplineTownResult res, SplineTownSettings s)
        {
            var t = res.terrain; int capped = 0; double diag = t.step * Math.Sqrt(2) + 0.05;
            var bodies = res.houses.Select(h => Inflate(h, 0.3)).ToList();
            foreach (var hp in res.houses)
            {
                if (!hp.hasDrive) continue;
                var own = hp.Corners();
                var A = hp.driveA; var B = hp.driveB; double len = SplineEdge.Dist(A, B); if (len < 1e-6) continue;
                double ux = (B.x - A.x) / len, uz = (B.z - A.z) / len, hw = hp.driveW / 2;
                double reach = hw + diag;
                int i0 = Math.Max(0, (int)Math.Floor((Math.Min(A.x, B.x) - reach) / t.step)), i1 = Math.Min(t.nx - 1, (int)Math.Ceiling((Math.Max(A.x, B.x) + reach) / t.step));
                int j0 = Math.Max(0, (int)Math.Floor((Math.Min(A.z, B.z) - reach) / t.step)), j1 = Math.Min(t.nz - 1, (int)Math.Ceiling((Math.Max(A.z, B.z) + reach) / t.step));
                for (int j = j0; j <= j1; j++)
                    for (int i = i0; i <= i1; i++)
                    {
                        var v = new Vec2(i * t.step, j * t.step);
                        double along = (v.x - A.x) * ux + (v.z - A.z) * uz, lat = Math.Abs(-(v.x - A.x) * uz + (v.z - A.z) * ux);
                        if (along < -diag || along > len + diag || lat > reach) continue;
                        // 다른 집은 0.3m 여유까지, 자기 집은 실제 몸체만 피한다(현관 앞 0.3m 띠의 지형이 진입로를 뚫던 것, BUG-009)
                        bool blocked = PointInPoly(v, own);
                        // 자기 집 몸체 안 지형이 기단(현관 높이)보다 높으면 기단 아래로 — 기단은 표본점 최고 높이라 격자 정점이 조금 더 높을 수 있다
                        if (blocked)
                        {
                            // 자기 집 아래: 진입로 하한선(현관 너머 연장) − 0.10, 단 기단 상자 바닥(지형 최저 − 0.3) 위 / 기단 상자 없으면 바닥 − 0.02 까지만
                            double ownLimit = hp.plinthY - hp.terrainMin > 0.05 ? hp.terrainMin - 0.25 : hp.plinthY - 0.02;
                            double capOwn = Math.Max(Math.Min(hp.plinthY - 0.05, DriveLowY(hp, along, s, true) - 0.10), ownLimit);
                            int id0 = j * t.nx + i; if (t.h[id0] > capOwn + 1e-9) { t.h[id0] = capOwn; capped++; }
                            continue;
                        }
                        // 이웃 집(0.3m 여유) 아래 점은 그 집 기단 상자 바닥(지형 최저 − 0.3) 위까지만 누른다 — 기단이 틈을 가린다. 기단 상자가 없는 집은 바닥 높이까지만.
                        double floorOf = double.NegativeInfinity;
                        for (int hi2 = 0; hi2 < bodies.Count; hi2++) if (res.houses[hi2] != hp && PointInPoly(v, bodies[hi2])) { var oh = res.houses[hi2]; floorOf = Math.Max(floorOf, oh.plinthY - oh.terrainMin > 0.05 ? oh.terrainMin - 0.25 : oh.plinthY - 0.02); }
                        double low = DriveLowY(hp, along, s, true);
                        double cap = lat <= hw + 1.5 && along >= -1.5 && along <= len + 1.5 ? low - 0.10 : low + 0.15;
                        cap = Math.Max(cap, floorOf);
                        int idx = j * t.nx + i;
                        if (t.h[idx] > cap + 1e-9) { t.h[idx] = cap; capped++; }
                    }
            }
            return capped;
        }

        static bool TooCloseToRoad(Vec2[] poly, SplineTownResult res, SplineTownSettings s)
        {
            double minX = poly.Min(p => p.x), maxX = poly.Max(p => p.x), minZ = poly.Min(p => p.z), maxZ = poly.Max(p => p.z);
            foreach (var e in res.edges)
            {
                double lim = e.alley ? s.alleyHalfWidthMeters : s.HalfShoulder;
                for (int i = 0; i + 1 < e.pts.Count; i++)
                {
                    var a = e.pts[i]; var b = e.pts[i + 1];
                    if (Math.Max(a.x, b.x) < minX - lim || Math.Min(a.x, b.x) > maxX + lim || Math.Max(a.z, b.z) < minZ - lim || Math.Min(a.z, b.z) > maxZ + lim) continue;
                    if (PointInPoly(a, poly) || PointInPoly(b, poly)) return true;
                    for (int k = 0; k < 4; k++) if (SegSegDist(poly[k], poly[(k + 1) % 4], a, b) < lim) return true;
                }
            }
            if (res.plaza != null && RectOverlap(poly, res.plaza.Corners(1.0))) return true;
            if (res.plaza != null && RectOverlap(poly, TowerCorners(res.plaza, 0.5))) return true;
            double disc = JunctionRadius(s) + s.HalfShoulder - s.HalfCarriageway;   // 원판 + 모서리 보도·어깨
            foreach (var n in res.junctions)
            {
                var c = res.nodes[n];
                if (PointInPoly(c, poly)) return true;
                for (int k = 0; k < 4; k++) if (DistToSeg(c, poly[k], poly[(k + 1) % 4], out _) < disc) return true;
            }
            return false;
        }

        static double SegSegDist(Vec2 p1, Vec2 p2, Vec2 q1, Vec2 q2)
        {
            if (SegIntersect(p1, p2, q1, q2)) return 0;
            return Math.Min(Math.Min(DistToSeg(p1, q1, q2, out _), DistToSeg(p2, q1, q2, out _)), Math.Min(DistToSeg(q1, p1, p2, out _), DistToSeg(q2, p1, p2, out _)));
        }

        static bool TouchesWall(Vec2[] poly, SplineTownResult res)
        {
            foreach (var w in res.walls)
            {
                for (int k = 0; k < 4; k++) if (SegIntersect(w.a, w.b, poly[k], poly[(k + 1) % 4])) return true;
                if (PointInPoly(w.a, poly)) return true;
            }
            return false;
        }

        static bool PointInTri(Vec2 p, Vec2 a, Vec2 b, Vec2 c)
        {
            double d1 = (b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x), d2 = (c.x - b.x) * (p.z - b.z) - (c.z - b.z) * (p.x - b.x), d3 = (a.x - c.x) * (p.z - c.z) - (a.z - c.z) * (p.x - c.x);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        static bool PointInPolyList(Vec2 p, List<Vec2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if ((poly[i].z > p.z) != (poly[j].z > p.z) && p.x < (poly[j].x - poly[i].x) * (p.z - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x) inside = !inside;
            return inside;
        }

        static bool PointInPoly(Vec2 p, Vec2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if (((poly[i].z > p.z) != (poly[j].z > p.z)) && (p.x < (poly[j].x - poly[i].x) * (p.z - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x)) inside = !inside;
            return inside;
        }

        /// <summary>볼록 사각형 두 개 겹침(분리축).</summary>
        public static bool RectOverlap(Vec2[] a, Vec2[] b)
        {
            foreach (var poly in new[] { a, b })
                for (int i = 0; i < 4; i++)
                {
                    var p1 = poly[i]; var p2 = poly[(i + 1) % 4];
                    double ax = -(p2.z - p1.z), az = p2.x - p1.x;
                    double amin = double.MaxValue, amax = double.MinValue, bmin = double.MaxValue, bmax = double.MinValue;
                    foreach (var q in a) { double d = q.x * ax + q.z * az; amin = Math.Min(amin, d); amax = Math.Max(amax, d); }
                    foreach (var q in b) { double d = q.x * ax + q.z * az; bmin = Math.Min(bmin, d); bmax = Math.Max(bmax, d); }
                    if (amax <= bmin + 1e-6 || bmax <= amin + 1e-6) return false;
                }
            return true;
        }

        /// <summary>점 i 의 왼쪽 법선(단위). 왼쪽 = 진행 방향 기준.</summary>
        public static (double, double) Normal(List<Vec2> p, int i)
        {
            var a = p[Math.Max(0, i - 1)]; var b = p[Math.Min(p.Count - 1, i + 1)];
            double dx = b.x - a.x, dz = b.z - a.z, l = Math.Sqrt(dx * dx + dz * dz);
            if (l < 1e-9) return (0, 0);
            return (-dz / l, dx / l);
        }

        // ------------------------------------------------------------------ 측정
        static void Measure(SplineTownResult res, IHeightField H, SplineTownSettings s, double maxDev)
        {
            var rep = res.report; var t = res.terrain;
            // M2 종단 경사
            double maxG = 0, maxGHair = 0; int gradeOver = 0;
            // M3 반경
            double minR = double.MaxValue, minRHair = double.MaxValue; int radOver = 0; string minAt = ""; var radUnder = new List<string>(); var underPoly = new List<string>(); var underPolyAt = new List<Vec2>();
            // M1 길 바깥 끝(어깨 끝 6m)과 지형: 길 메시 바깥 정점은 지형 면에 붙이므로, 정점 사이 중간점의 현(弦) 오차를 잰다
            double m1 = 0; int m1Over = 0, m1Samples = 0, wallSkipped = 0;
            int buried = 0; double buriedMax = 0;
            int seaPts = 0;
            double maxGAlley = 0, minRAlley = double.MaxValue;
            foreach (var e in res.edges)
            {
                double halfC = e.alley ? s.alleyHalfWidthMeters : s.HalfCarriageway, edgeD = e.alley ? s.alleyHalfWidthMeters + s.alleyShoulderMeters : s.HalfShoulder;
                for (int i = 1; i < e.pts.Count; i++)
                {
                    double d = SplineEdge.Dist(e.pts[i - 1], e.pts[i]);
                    if (d < 1e-6) continue;
                    double g = Math.Abs(e.y[i] - e.y[i - 1]) / d;
                    double lim = GradeLimit(e, i, s);
                    if (e.alley) maxGAlley = Math.Max(maxGAlley, g); else if (e.hairpin[i]) maxGHair = Math.Max(maxGHair, g); else maxG = Math.Max(maxG, g);
                    if (g > lim + 1e-6) gradeOver++;
                }
                var arcM = new double[e.pts.Count]; for (int i = 1; i < e.pts.Count; i++) arcM[i] = arcM[i - 1] + SplineEdge.Dist(e.pts[i - 1], e.pts[i]);
                for (int i = 0; i < e.pts.Count; i++)
                {
                    double r = RadiusAt(e.pts, i), need = e.alley ? s.alleyMinRadiusMeters : (e.hairpin[i] ? s.hairpinMinRadiusMeters : s.mainMinRadiusMeters);
                    if (e.alley) minRAlley = Math.Min(minRAlley, r);
                    else if (e.hairpin[i]) minRHair = Math.Min(minRHair, r);
                    else if (r < minR) { minR = r; minAt = $"({e.pts[i].x:0},{e.pts[i].z:0})"; }
                    if (r < need - 1e-6 && !e.alley && underPoly.Count < 6 && !underPolyAt.Any(q0 => SplineEdge.Dist(q0, e.pts[i]) < 20)) { underPolyAt.Add(e.pts[i]); underPoly.Add(string.Join(" ", e.pts.Skip(Math.Max(0, i - 10)).Take(21).Select(q0 => $"{q0.x:0.0},{q0.z:0.0}"))); }
                    if (r < need - 1e-6) { radOver++; if (radUnder.Count < 40) radUnder.Add($"({e.pts[i].x:0.0},{e.pts[i].z:0.0}) R{r:0.0}<{need:0}{(e.alley ? " 골목" : e.hairpin[i] ? " 헤어핀" : " 큰길")}"); }
                    int c = (int)Math.Floor(e.pts[i].x / s.seaCellMeters), rr = (int)Math.Floor(e.pts[i].z / s.seaCellMeters);
                    if (res.sea.InBounds(c, rr) && res.sea.sea[res.sea.Index(c, rr)]) seaPts++;
                    // 묻힘: 길 단면 위(차로·보도) 지형이 길보다 위로 나오는지
                    var (nxv, nzv) = Normal(e.pts, i);
                    var offs = e.alley ? new[] { 0.0, 1.0, -1.0, 1.4, -1.4 } : new[] { 0.0, 1.5, -1.5, 2.9, -2.9, 3.5, -3.5 };
                    foreach (double off in offs)
                    {
                        double ty = t.Surface(e.pts[i].x + nxv * off, e.pts[i].z + nzv * off);
                        double ry = e.y[i] + BankAt(e, arcM[i], s) * off + (!e.alley && Math.Abs(off) > halfC ? s.curbHeightMeters : 0);
                        if (ty > ry + 0.01) { buried++; buriedMax = Math.Max(buriedMax, ty - ry); }
                    }
                }
                foreach (int side in new[] { 1, -1 })
                    for (int i = 0; i + 1 < e.pts.Count; i++)
                    {
                        var (n0x, n0z) = Normal(e.pts, i); var (n1x, n1z) = Normal(e.pts, i + 1);
                        double ax = e.pts[i].x + side * n0x * edgeD, az = e.pts[i].z + side * n0z * edgeD;
                        double bx = e.pts[i + 1].x + side * n1x * edgeD, bz = e.pts[i + 1].z + side * n1z * edgeD;
                        // 옹벽 구간도 빼지 않는다(QA 방식) — wallSkipped 는 0 으로 남음
                        // 씬 메시와 같은 분할: 바깥 선이 지형 격자선·대각선을 지나는 점마다 정점(지형 면에 붙음), 그 사이 중간점의 현 오차를 잰다
                        var ts = GridCrossings(t, ax, az, bx, bz);
                        for (int k = 0; k + 1 < ts.Count; k++)
                        {
                            double t0 = ts[k], t1 = ts[k + 1];
                            double x0 = ax + (bx - ax) * t0, z0 = az + (bz - az) * t0, x1 = ax + (bx - ax) * t1, z1 = az + (bz - az) * t1;
                            double y0 = t.Surface(x0, z0), y1 = t.Surface(x1, z1), ym = t.Surface((x0 + x1) / 2, (z0 + z1) / 2);
                            double err = Math.Abs((y0 + y1) / 2 - ym);
                            m1 = Math.Max(m1, err); m1Samples++; if (err > 0.05) m1Over++;
                        }
                    }
            }
            var comp = Components(res.Mains.ToList(), res.nodes.Count, out var compLen);
            rep.Check("M1", m1Over == 0, $"길 바깥 끝(어깨 6m) 정점은 지형 면에 붙임 · 정점 사이 현 오차 최대 {m1:0.000}m (≤0.05), 위반 {m1Over}/{m1Samples} · 옹벽 구간 제외 {wallSkipped}");
            rep.Check("M1-buried", buried == 0, $"길 단면 위로 지형이 솟은 점 {buried} (최대 {buriedMax:0.00}m)");
            rep.Check("M2", gradeOver == 0, $"종단 경사 최대 큰길 {maxG * 100:0.0}% (≤{s.mainMaxGrade * 100:0}) · 헤어핀 {maxGHair * 100:0.0}% (≤{s.hairpinMaxGrade * 100:0}) · 골목 {maxGAlley * 100:0.0}% (≤{s.alleyMaxGrade * 100:0}) · 초과 구간 {gradeOver}");
            rep.Metric("m3_under_at", string.Join(" · ", radUnder));
            rep.Metric("m3_under_poly", string.Join(" | ", underPoly));
            rep.Check("M3", radOver == 0, $"최소 반경 큰길 {Fmt(minR)}m {minAt} (≥{s.mainMinRadiusMeters}) · 헤어핀 {Fmt(minRHair)}m (≥{s.hairpinMinRadiusMeters}) · 골목 {Fmt(minRAlley)}m (≥{s.alleyMinRadiusMeters}) · 미달 점 {radOver} · OSM 선 최대 이탈 {maxDev:0.0}m (상한 {s.maxDeviationMeters})");
            rep.Check("M5", seaPts == 0, $"바다 칸 위 길 점 {seaPts}");
            rep.Check("M6", compLen.Count == 1, $"큰길 망 덩어리 {compLen.Count} (교차점·끝점 그래프 BFS)");
        }

        static string Fmt(double r) => double.IsInfinity(r) || r == double.MaxValue ? "∞" : r.ToString("0.0", CultureInfo.InvariantCulture);

        static bool OnWall(SplineTownResult res, double x, double z)
        {
            foreach (var w in res.walls)
            {
                double d = DistToSeg(new Vec2(x, z), w.a, w.b, out _);
                if (d < 0.6) return true;
            }
            return false;
        }

        static string Hash(SplineTownResult res)
        {
            var sb = new StringBuilder();
            foreach (var e in res.edges) { foreach (var p in e.pts) sb.Append(p.x.ToString("F2", CultureInfo.InvariantCulture)).Append(',').Append(p.z.ToString("F2", CultureInfo.InvariantCulture)).Append(';'); foreach (var y in e.y) sb.Append(y.ToString("F2", CultureInfo.InvariantCulture)).Append(';'); sb.Append('|'); }
            foreach (var h in res.terrain.h) sb.Append(h.ToString("F2", CultureInfo.InvariantCulture)).Append(';');
            foreach (var hp in res.houses) sb.Append(hp.center.x.ToString("F2", CultureInfo.InvariantCulture)).Append(',').Append(hp.center.z.ToString("F2", CultureInfo.InvariantCulture)).Append(',').Append(hp.model).Append(';');
            using (var sha = SHA1.Create())
            {
                var hb = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return string.Concat(hb.Take(6).Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
            }
        }
    }
}
