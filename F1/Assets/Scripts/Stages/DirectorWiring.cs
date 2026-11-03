using UnityEngine;

// D-287 3단계: 감독의 씬 배선(인스펙터 필드)만 모은 조각. 단계 클래스는 MonoBehaviour 가 아니라 직렬화를 못 하므로
// 씬 값은 감독에 남고, 단계가 d.필드 로 읽는다. 이름·값은 예전 그대로(씬에 저장된 값이 이름으로 붙는다). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    [SerializeField] internal OnFootPlayer player;
    [SerializeField] internal Transform spawn;
    [SerializeField] internal Transform cartSpot;
    [Tooltip("1인칭 시선 레이 길이(m). 눈에서 이 거리 안에 문·카트가 맞으면 E.")]
    [SerializeField] internal float gazeReach = 2.0f;
    [Tooltip("이 속도(m/s) 밑이면 내릴 수 있다.")]
    [SerializeField] internal float exitMaxSpeed = 1.0f;
    [Tooltip("카트에서 내릴 때 차 옆 거리(m).")]
    [SerializeField] internal float exitSide = 1.7f;

    [Header("기능 2 — 정비소 사장 → 첫 주문(과제 20261031-0209)")]
    [Tooltip("정비소 사장 자리(1.7m 캡슐 대역). 비면 기능 1 처럼 바로 카트를 탈 수 있다.")]
    [SerializeField] internal Transform boss;
    [Tooltip("첫 주문 배달지(찻집 앞 길). 비면 탑승 때 주문 없이 자유 주행.")]
    [SerializeField] internal DeliveryPoint firstDrop;
    [Tooltip("사장에게 말 거는 시선 레이 길이(m). 사람은 문보다 멀리서 말을 건다.")]
    [SerializeField] internal float talkReach = 3.0f;

    [Header("D-250·251 — 택배 보관소에서 주문 받기(과제 20261102-1515)")]
    [Tooltip("보관소 안 주문 칠판. 비면 예전 기능 2(사장이 말로 찻집 주문, 탑승 = 출발).")]
    [SerializeField] internal DepotOrderBoard orderBoard;
    [Tooltip("포장 자리 대역(작업대 위 상자). 비면 칠판에서 고르는 것만으로 출발할 수 있다.")]
    [SerializeField] internal DepotPackSpot packSpot;
    [Tooltip("사장 대화 뒤 목적지 표식이 가리킬 곳(보관소 앞). 꺼진 DeliveryPoint — 표식 자리로만 쓴다.")]
    [SerializeField] internal DeliveryPoint depotDest;
    [Tooltip("칠판 보기 시점으로 옮겨 가는 시간(초).")]
    [SerializeField] internal float boardFocusSec = 0.5f;

    [Header("D-250 ③④ — 문 앞 건네기(과제 20261102-1543)")]
    [Tooltip("배달지 문 앞 대역(문짝·받는 사람). 비면 예전처럼 도착 = 끝(정산창).")]
    [SerializeField] internal DoorHandover handover;
    [Tooltip("배달지 표식에서 이 거리(m, 수평) 안에서 세우고 내리면 도착으로 친다(트리거를 못 밟았어도).")]
    [SerializeField] internal float arriveM = 15f;
    [Tooltip("돈 글자가 떠 있는 시간(초).")]
    [SerializeField] internal float moneySec = 2.6f;
    [Tooltip("두드린 뒤 문이 열리기까지(초).")]
    [SerializeField] internal float knockWaitSec = 0.9f;

    [Header("하루 끝 일기장·방 달력(과제 20261102-1951, DELIVERY-LOOP 5-5)")]
    [Tooltip("방 벽 달력 실물. 비면 달력 [E] 없음.")]
    [SerializeField] internal RoomCalendar calendar;
    [Tooltip("방 침대 [E] 자리. 비면 하루 끝 일기장 없음.")]
    [SerializeField] internal RoomBed bed;
    [Tooltip("일기를 펼친 뒤 이 초가 지나야 덮인다(실수로 바로 넘기지 않게).")]
    [SerializeField] internal float diaryMinSec = 0.8f;

    [Header("첫 과제 01-first-loop — 건넨 뒤 노을(SPEC §11-3 가설)")]
    [Tooltip("문 앞 건네기가 끝나면 SunsetLook.dusk 를 0 → 1 로 이 시간(초, 실시간)에 걸쳐 올린다. 0 = 안 바꿈.")]
    [SerializeField] internal float duskRampSec = 60f;
    [Tooltip("건넨 뒤 '첫 배달 끝' 글이 떠 있는 시간(초). 그 뒤 돌아오는 길은 화면 0(SPEC §6-9).")]
    [SerializeField] internal float handedCaptionSec = 6f;

    [Header("UI 1판(D-240) — 대화 시선")]
    [Tooltip("대화가 시작되면 카메라가 말하는 사람 쪽으로 도는 시간(초). 0 = 안 돌림.")]
    [SerializeField] internal float talkFocusSec = 0.5f;
    [Tooltip("화면 가운데에 둘 점 = 머리 꼭대기에서 이만큼 아래(m). 말풍선이 그 위에 들어갈 자리.")]
    [SerializeField] internal float talkFocusDrop = 0.05f;
    [Header("목적지 표식(D-252) — 대화 끝 시선")]
    [Tooltip("사장 대화가 끝나면 카메라가 첫 배달지 표식 쪽으로 도는 시간(초). 0 = 안 돌림(대화 시선과 같은 방식).")]
    [SerializeField] internal float destFocusSec = 0.5f;
    [Tooltip("표식 쪽을 본 채 멈춰 있는 시간(초) — 그 뒤 다시 플레이어 시선.")]
    [SerializeField] internal float destHoldSec = 0.8f;
}
