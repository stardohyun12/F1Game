using UnityEngine;

// D-287 3단계: 바깥(봇·화면·디버그·불변 검사)이 쓰던 감독의 공개 이름을 그대로 두고, 값은 단계 클래스에서 가져다 준다.
// 여기엔 몸통이 없다 — 일은 Stage_*.cs 가 한다. 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    // ── Stage_Drive ──
    public bool OrderStarted => drive.OrderStarted;
    public bool Delivered => drive.Delivered;
    /// <summary>
    /// 디버그 순간이동용 보관소 앞 길 카트 자리(DebugPlaces.DepotCart). 두 번째 한 판 메모(D-284): 아침 카트는 여기 서 있지 않다 —
    /// 첫날 카트는 정비소(cartSpot)에서 받아 타고 보관소로 간다(SPEC §1 거점은 몸으로 배운다). 보관소에서 상자를 싣는 논리는 카트가 어디 서 있든 같다(NearCart).
    /// 좌표는 DebugPlaces.DepotCart 가 이 값을 가져다 쓴다(디버그 코드는 정식 빌드에 없어 값은 여기).
    /// </summary>
    public static readonly Vector3 DepotCartSpot = new Vector3(273.5f, 29.6f, -37.4f);
    public const float DepotCartYaw = 90f;
    /// <summary>상자를 든 채 카트에서 이만큼(m, 카트 몸 가장자리까지 수평) 안이면 시선이 카트를 안 가리켜도 [E 싣고 타기] 칩이 뜬다.</summary>
    public const float CartLoadReach = 2f;
    /// <summary>카트 칩 글(순수 함수 — WorldInvariants 「상자 든 채 카트 칩」이 부른다). "E " 로 시작하면 누를 수 있다.</summary>
    public static string CartChip(bool depotFlow, bool orderPicked, bool packed, bool orderStarted)
        => OrderRules.CartChip(depotFlow, orderPicked, packed, orderStarted);   // D-287: 몸통은 Core/OrderRules
    /// <summary>상자를 들었고 아직 싣기 전 — 카트 위 「!」(DotScreenUI)가 켜진다.</summary>
    public bool CartMark => State == Mode.OnFoot && DepotFlow && !OrderStarted && CarryingBox && car != null;
    public Vector3 CartMarkAnchor => car.transform.position + Vector3.up * 1.5f;
    /// <summary>사장 대화가 끝나 첫 배달지를 들었다 — DotScreenUI 가 걷는 동안에도 목적지 표식을 그린다(탑승 전까지).</summary>
    public bool DestKnown => WalkDest != null;
    /// <summary>
    /// 주문 출발 전 목적지 표식이 가리킬 곳. 예전 흐름 = 사장 대화 뒤 찻집.
    /// 보관소 흐름(D-250) = 사장 대화 뒤 보관소 앞(depotDest) → 칠판에서 주문을 맡은 뒤 찻집.
    /// </summary>
    public DeliveryPoint WalkDest
    {
        get
        {
            if (firstDrop == null || Delivered) return null;
            // 첫 과제 01-first-loop(SPEC §11-7): 보관소 흐름은 사장 대화가 없어도 칠판 쪽지를 집으면 찻집을 가리킨다.
            if (DepotFlow && OrderPicked) return firstDrop;
            if (!Talked) return null;   // C-002(도현 D-277 B): 걷는 구간엔 방향 표시 없음
            return DepotFlow ? depotDest : firstDrop;
        }
    }
    /// <summary>DestClarityShots 가 리플렉션으로 부른다(이름 그대로).</summary>
    void EnterCart() => drive.Enter();

    // ── Stage_Door ──
    public DoorHandover Handover => handover;
    public bool CarryingBox => door.CarryingBox;
    public bool Knocking => door.Knocking;
    public bool HandedOver => door.HandedOver;
    public int HandoverTierIdx => door.TierIdx;
    public int HandoverMoney => door.Money;
    public bool BedBox => door.BedBox;

    // ── Stage_Return ──
    public float HandedAt => ret.HandedAt;
    /// <summary>개발용(메뉴 PitCrew/노을/낚시 자리로 가기): 걷는 중이면 자리 바로 뒤로 옮긴다.</summary>
    public bool GoToSpot() => ret.GoToSpot();

    // ── Stage_Garage (4단계) ──
    // 에디트 모드(불변 검사 「첫 배달 한 바퀴 배선」·「아침 카트는 정비소 앞」)에선 Awake 가 안 돌아 garage 가 없다 → 그때만 여기서 만든다.
    Stage_Garage Garage => garage ??= new Stage_Garage(this);
    public bool Talked => Garage.Talked;
    public bool CartNeedsBossTalk => Garage.CartNeedsBossTalk;
    public Vector3? MorningCartAt => Garage.MorningCartAt;

    // ── Stage_Morning ──
    public RoomCalendar Calendar => calendar;
    public bool CalendarMoving => morning.Moving;
    public void SimCalendarClose() => morning.SimClose();
    public static bool IsLegacyPoint(DeliveryPoint dp) => Stage_Morning.IsLegacyPoint(dp);

    // ── Stage_Packing ──
    public bool PackMoving => packing.Moving;
    public float StartIntegrity => packing.StartIntegrity;
    public void SimPackLeave() => packing.SimLeave();
    /// <summary>DayLoopCheck 가 리플렉션으로 부른다(이름 그대로).</summary>
    void EnterPack() => packing.Enter();
    /// <summary>DayLoopCheck 가 리플렉션으로 부른다(이름 그대로) — 물건 PackTable 과 같은 신호를 쏜다.</summary>
    void TakePacked() => SignalBus.Raise(Signal.BoxTaken);
}
