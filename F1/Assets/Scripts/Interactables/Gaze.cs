using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-287 2단계: 「화면 가운데 점이 무엇을 가리키나」 한 곳. 눈(카메라)에서 앞으로 레이를 쏴서 맞은 물건(IInteractable)을 돌려준다.
/// 감독은 Find() 의 결과로 E 칩(글·자리)을 띄우고, E 를 누르면 그 물건의 Use() 를 부를 뿐이다.
/// 규칙(예전 OnFootDirector.Gaze + Update 분기 그대로): 벽·문틀이 먼저 맞으면 아무것도 아니다 — 벽 너머 문은 안 잡힌다.
/// </summary>
public class Gaze
{
    // ── 배선(감독이 Start 에 채운다) ──
    public Transform player;
    public Camera cam;
    public ArcadeCarController car;
    public Transform boss;
    public DepotOrderBoard board;
    public DepotPackSpot packSpot;
    /// <summary>D-290 포장 v3(켜져 있으면 칠판·쪽지·옛 작업대 대신 — 받는 곳 선반·포장대 물건을 먼저 본다).</summary>
    public PackTable2 pack3;
    public DoorHandover handover;
    public RoomCalendar calendar;
    public RoomBed bed;
    public List<DoorHinge> doors;
    public List<FishingSpot> spots;
    public float gazeReach, talkReach;
    /// <summary>상자를 들고 카트 이 거리(m) 안이면 어디를 보든 카트(첫 한 판 메모 1).</summary>
    public float cartLoadReach = 2f;

    /// <summary>지금 레이가 맞은 것의 이름(없으면 null) — 봇·QA 용.</summary>
    public string Target { get; private set; }
    /// <summary>레이가 처음 맞은 점(쪽지 고르기용).</summary>
    public Vector3 Point { get; private set; }

    readonly RaycastHit[] hits = new RaycastHit[16];
    Note note; Board boardThing; PackTable pack; Cart cart; Repair repair; KnockDoor knock; Boss bossThing; WallCalendar cal; Bed bedThing; Door door; Seat seat;
    public readonly Diary Diary = new Diary();

    /// <summary>배선을 채운 뒤 한 번 — 물건을 만든다.</summary>
    public void Build()
    {
        note = new Note(); door = new Door(player); seat = new Seat();
        if (board != null) boardThing = new Board(board);
        if (packSpot != null) pack = new PackTable(packSpot);
        if (car != null) { cart = new Cart(car, packSpot); repair = new Repair(car); }
        if (handover != null) knock = new KnockDoor(handover);
        if (boss != null) bossThing = new Boss(boss);
        if (calendar != null) cal = new WallCalendar(calendar);
        if (bed != null) bedThing = new Bed(bed);
    }

    /// <summary>지금 시선이 가리키는 물건(없으면 null). 걷는 중 매 프레임 한 번.</summary>
    public IInteractable Find()
    {
        bool v3 = pack3 != null && pack3.On;
        if (v3 && !DayState.CarryingBox)
        {
            var p3 = pack3.Look(cam.transform, out var t3);
            if (p3 != null) { Target = t3; return p3; }
        }
        Object hit = Ray(out DoorHinge dh);
        if (hit == null && DayState.DepotFlow && DayState.CarryingBox && !DayState.OrderStarted && NearCart(car, player.position, cartLoadReach)) hit = car;
        Target = hit != null ? hit.name : null;

        if (handover != null && hit == handover) return knock;
        if (boss != null && hit == boss) return bossThing;
        if (v3 && (board != null && hit == board || packSpot != null && hit == packSpot)) return null;   // 칠판·옛 작업대 꺼짐(받는 곳 선반이 대신)
        if (board != null && hit == board)
        {
            if (!board.NotesOn) return boardThing;
            note.Target = board.NearestNote(Point);
            return note;
        }
        if (packSpot != null && hit == packSpot) return pack;
        if (calendar != null && hit == calendar) return cal;
        if (bed != null && hit == bed) return bedThing;
        if ((hit == car || hit == null) && RepairSpot.Ready(player.position, car)) return repair;
        if (hit == car && car != null) return cart;
        if (dh != null) { door.Target = dh; return door; }
        if (hit == null)
            foreach (var s in spots) if (s != null && s.CanSit(player.position)) { seat.Target = s; return seat; }
        return null;
    }

    /// <summary>레이 판정 — 맞은 물건(문이면 out 문). 순서가 곧 규칙이다.</summary>
    Object Ray(out DoorHinge door)
    {
        door = null;
        Transform eye = cam.transform;
        int n = Physics.RaycastNonAlloc(eye.position, eye.forward, hits, Mathf.Max(gazeReach, boss != null || board != null || handover != null ? talkReach : 0f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Collider first = null; float best = float.MaxValue; float packD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c == null || c.transform.IsChildOf(player)) continue;
            if (hits[i].distance < best) { best = hits[i].distance; first = c; Point = hits[i].point; }
            if (packSpot != null && c.transform.IsChildOf(packSpot.transform)) packD = Mathf.Min(packD, hits[i].distance);
        }
        // 방 달력(벽에 붙은 판)·침대(충돌체 없이 경계 상자) — 앞에 다른 것이 먼저 맞으면 못 잡는다.
        if (calendar != null && first != null && first.transform.IsChildOf(calendar.transform) && best <= talkReach) return calendar;
        if (bed != null && bed.Hit(new Ray(eye.position, eye.forward), Mathf.Min(best + 0.02f, talkReach), out _)) return bed;
        // 작업대 판(BenchTop)은 카운터 윗면과 같은 높이 — 2cm 안으로 비기면 작업대가 이긴다.
        if (packD <= best + 0.02f && packD <= gazeReach) return packSpot;
        // 사장(사람)은 talkReach 까지, 나머지(문·카트)는 예전처럼 gazeReach 까지.
        if (first != null && boss != null && first.transform.IsChildOf(boss) && best <= talkReach) return boss;
        // 칠판은 벽에 붙어 있어 한 발 물러서서 본다 — 사장처럼 talkReach 까지.
        if (first != null && board != null && first.transform.IsChildOf(board.transform) && best <= talkReach) return board;
        if (first != null && packSpot != null && first.transform.IsChildOf(packSpot.transform) && best <= gazeReach) return packSpot;
        // 문 앞 건네기: 문은 사람처럼 한 발 떨어져서 두드린다(talkReach).
        if (first != null && handover != null && first.transform.IsChildOf(handover.transform) && best <= talkReach) return handover;
        if (first != null && best > gazeReach) first = null;
        // 카트는 낮아서(충돌 0.44m) 서서 내려다보면 레이가 빗나간다 — 카트 중심이 3m 안(가장자리 약 2m)이고 대략 앞쪽이면 타기 허용(PD 10/31, 기능팀 보고 1c)
        if (car != null)
        {
            Vector3 to = car.transform.position - player.position; to.y = 0f;
            Vector3 fw = eye.forward; fw.y = 0f;
            if (to.magnitude < 3.0f && fw.sqrMagnitude > 1e-4f && Vector3.Dot(fw.normalized, to.normalized) > 0.5f
                && (first == null || first.GetComponentInParent<DoorHinge>() == null)) return car;
        }
        if (first == null) return null;
        var hitCar = first.GetComponentInParent<ArcadeCarController>();
        if (hitCar != null && hitCar == car) return car;
        var dh = first.GetComponentInParent<DoorHinge>();
        if (dh != null && doors.Contains(dh)) { door = dh; return dh; }
        foreach (var d in doors)
            if (d != null) foreach (var r in d.riders) if (r != null && first.transform.IsChildOf(r)) { door = d; return d; }
        return null;
    }

    // ── 칩 자리 도우미(감독의 대화 말풍선도 같이 쓴다) ──

    /// <summary>t 의 렌더러를 다 감싼 상자의 윗면 가운데 − t 위치. 렌더러가 없으면 1.8 m 위.</summary>
    public static Vector3 TopOffset(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return Vector3.up * 1.8f;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return new Vector3(b.center.x, b.max.y, b.center.z) - t.position;
    }

    /// <summary>문짝 가운데(문은 돌아가므로 매번 — 문 몇 개뿐).</summary>
    public static Vector3? CenterOf(DoorHinge d)
    {
        if (d == null) return null;
        var rs = d.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return d.transform.position;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    /// <summary>카트 몸 가장자리까지 수평 거리가 within 이내.</summary>
    public static bool NearCart(ArcadeCarController car, Vector3 p, float within)
    {
        if (car == null) return false;
        bool any = false; Bounds b = default;
        foreach (var c in car.GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger) continue;
            if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds);
        }
        if (!any) return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(car.transform.position.x, car.transform.position.z)) <= within + 1f;
        Vector3 q = b.ClosestPoint(new Vector3(p.x, b.center.y, p.z));
        return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(q.x, q.z)) <= within;
    }
}
