using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-294 포장 2D 작업대 ①: 화면 = 작업대 윗면. 카메라가 작업대 위로 내려가면(0.4 s, 세상 안 — 노을빛 그대로) 윗면이 판이 되고,
/// 왼쪽 = 받은 물건(08 의 Parcel·꼬리표 재사용) · 가운데 = 빈 자리(slot) · 오른쪽 = 상자 소/중/대 + 롤(뽁뽁이·종이) + 테이프. 마우스로 집어 놓는다(월드 평면 끌기 — UI 캔버스 아님).
/// 상자를 가운데에 놓으면 열린다. 물건 넣기·롤·테이프·흔들기는 ②③.
/// 자리 틀: 원점 = 포장대 판 한가운데 윗면, 화면 위 = workSpot.forward, 화면 오른쪽 = workSpot.right. 좌표는 판 기준 m(u 오른쪽, v 위쪽) — 손으로 쓴 월드 좌표 없음.
/// 그리기는 Presentation/PackBoardView, 켜고 끄기는 Stage_Packing. 08 의 3D 손 흐름(PackTable2)은 꺼 두기만 하고 지우지 않는다.
/// </summary>
public class PackBoard : MonoBehaviour
{
    public const float HalfW = 1.0f, HalfH = 0.42f;
    public const float SlotU = -0.12f, SlotV = 0f, SlotW = 0.46f, SlotD = 0.40f;
    const float BoxScale = 0.6f;

    public readonly List<PackPiece> pieces = new List<PackPiece>();
    public bool On { get; private set; }
    public PackPiece Held { get; private set; }
    public PackPiece InSlot { get; private set; }
    public int OpenedCount { get; private set; }
    public Vector3 Origin { get; private set; }
    public Vector3 Right { get; private set; }
    public Vector3 Up { get; private set; }
    public float TopY => Origin.y;
    public Camera Cam { get; private set; }
    public bool ExitAsked { get; private set; }
    public string LastAct { get; private set; } = "";

    PackTable2 table;
    Vector2 grabOff;
    readonly List<(Renderer r, bool was)> hidden = new List<(Renderer, bool)>();

    /// <summary>판 자리 틀을 포장대에서 구한다(한 번). 조각은 world.yaml packing 에서.</summary>
    public void Build(PackTable2 t)
    {
        if (pieces.Count > 0) return;
        table = t;
        var ws = t.workSpot;
        Up = Vector3.ProjectOnPlane(ws.forward, Vector3.up).normalized;
        Right = Vector3.ProjectOnPlane(ws.right, Vector3.up).normalized;
        Origin = ws.position + Up * 0.2f;      // workSpot 은 판 한가운데에서 화면 아래쪽으로 0.2 m — 판 한가운데로

        var pk = WorldData.Orders != null ? WorldData.Orders.packing : null;
        var boxes = pk != null && pk.boxes.Count > 0 ? pk.boxes : null;
        float cu = 0.28f;
        if (boxes != null)
            foreach (var b in boxes) { cu = AddBox(cu, "상자 " + b.name, b.inner.x * BoxScale, b.inner.z * BoxScale); }
        else
            foreach (var (n, s) in new[] { ("소", 0.22f), ("중", 0.315f), ("대", 0.46f) }) cu = AddBox(cu, "상자 " + n, s * BoxScale, s * BoxScale);

        float ru = 0.30f;
        var fl = pk != null && pk.fillers.Count > 0 ? pk.fillers : null;
        if (fl != null) foreach (var f in fl) ru = AddRoll(ru, f.name + " 롤", f.name);
        else foreach (var n in new[] { "뽁뽁이", "종이" }) ru = AddRoll(ru, n + " 롤", n);
        Add(PackPieceKind.Tape, "테이프", ru + 0.05f, -0.2f, 0.09f, 0.09f);

        Add(PackPieceKind.Parcel, "물건", -0.72f, 0f, 0.30f, 0.30f).active = false;
    }

    float AddBox(float u, string n, float w, float d) { Add(PackPieceKind.Box, n, u + w * 0.5f, 0.2f, w, d); return u + w + 0.06f; }
    float AddRoll(float u, string n, string filler) { var p = Add(PackPieceKind.Roll, n, u + 0.11f, -0.2f, 0.22f, 0.11f); p.filler = filler; return u + 0.22f + 0.06f; }
    PackPiece Add(PackPieceKind k, string n, float u, float v, float w, float d)
    {
        var p = new PackPiece { kind = k, name = n, w = w, d = d, homeU = u, homeV = v };
        p.Home(); pieces.Add(p); return p;
    }

    public PackPiece Find(PackPieceKind k, int index = 0)
    {
        foreach (var p in pieces) if (p.kind == k && index-- == 0) return p;
        return null;
    }

    public int Count(PackPieceKind k) { int n = 0; foreach (var p in pieces) if (p.kind == k) n++; return n; }

    // ── 켜고 끄기 ──
    /// <summary>카메라가 판을 위에서 내려다보는 자리(판 전체가 화면에 들어오게).</summary>
    public void CamPose(Camera cam, out Vector3 pos, out Quaternion rot)
    {
        float tv = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float h = Mathf.Max((HalfW + 0.08f) / (tv * Mathf.Max(0.5f, cam.aspect)), (HalfH + 0.08f) / tv);
        pos = Origin + Vector3.up * h;
        rot = Quaternion.LookRotation(Vector3.down, Up);
    }

    public void SetOn(bool on, Camera cam, Parcel held)
    {
        if (on == On) return;
        On = on; Cam = cam; ExitAsked = false; Held = null;
        var pp = Find(PackPieceKind.Parcel);
        if (on)
        {
            if (pp != null) { pp.active = held != null; if (held != null && pp.parcel != held) { pp.parcel = held; pp.Home(); } }
            HideWorld(true);
            Act("2D 보드 켜짐");
        }
        else { HideWorld(false); Act("2D 보드 꺼짐"); }
    }

    /// <summary>보드가 켜진 동안 3D 상자·롤·물건은 보이지 않게(판 위 대역이 그 자리를 맡는다). 포장대 판·바닥은 그대로.</summary>
    void HideWorld(bool hide)
    {
        if (!hide) { foreach (var (r, was) in hidden) if (r != null) r.enabled = was; hidden.Clear(); return; }
        if (table == null || table.transform.parent == null) return;
        var own = table.GetComponent<Renderer>();                          // 포장대 판 자신은 그대로
        foreach (var r in table.transform.parent.GetComponentsInChildren<Renderer>(true))
        {
            if (r == own || !r.enabled) continue;
            var th = r.GetComponentInParent<PackThing>();
            bool thing = th != null && th != table;
            // 판 위로 솟은 것(뒤쪽 선반 단 Shelf_Riser 등)이 위에서 내려다보는 판을 가린다 — 판 자리 위에 겹치면 같이 숨김
            var rel = r.bounds.center - Origin;
            bool over = r.bounds.max.y > TopY + 0.01f && Mathf.Abs(Vector3.Dot(rel, Right)) < HalfW + 0.3f && Mathf.Abs(Vector3.Dot(rel, Up)) < HalfH + 0.3f;
            if (!thing && !over) continue;
            hidden.Add((r, r.enabled)); r.enabled = false;
        }
    }

    public bool TakeExit() { bool e = ExitAsked; ExitAsked = false; return e; }

    public void ResetAll()
    {
        Held = null; InSlot = null; OpenedCount = 0; ExitAsked = false;
        foreach (var p in pieces) { p.Home(); if (p.kind == PackPieceKind.Parcel) { p.active = false; p.parcel = null; } }
    }

    // ── 입력(사람 = Update, 봇 = Sim*) ──
    void Update()
    {
        if (!On || Cam == null) return;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        var ms = Mouse.current; var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) ExitAsked = true;
        if (ms == null || !PointerOnBoard(ms.position.ReadValue(), out float u, out float v)) return;
        if (ms.leftButton.wasPressedThisFrame) Begin(u, v);
        if (Held != null)
        {
            Move(u, v);
            if (ms.leftButton.wasReleasedThisFrame) End();
        }
        else if (ms.rightButton.wasPressedThisFrame && Pick(u, v) == null) { ExitAsked = true; Act("빈 자리 오른쪽 클릭 → 물러남"); }
    }

    /// <summary>화면 픽셀 → 판 좌표. 게임은 낮은 해상도 RT 를 꽉 채워 보여 주므로 뷰포트 비율로 쏜다.</summary>
    bool PointerOnBoard(Vector2 px, out float u, out float v)
    {
        u = v = 0f;
        var ray = Cam.ViewportPointToRay(new Vector3(px.x / Mathf.Max(1, Screen.width), px.y / Mathf.Max(1, Screen.height), 0f));
        var plane = new Plane(Vector3.up, new Vector3(0f, TopY, 0f));
        if (!plane.Raycast(ray, out float dist)) return false;
        var p = ray.GetPoint(dist) - Origin;
        u = Vector3.Dot(p, Right); v = Vector3.Dot(p, Up);
        return true;
    }

    public PackPiece Pick(float u, float v)
    {
        for (int i = pieces.Count - 1; i >= 0; i--) if (pieces[i].Contains(u, v)) return pieces[i];
        return null;
    }

    public void Begin(float u, float v)
    {
        var p = Pick(u, v);
        if (p == null) return;
        Held = p; p.lifted = true; grabOff = new Vector2(p.u - u, p.v - v);
        pieces.Remove(p); pieces.Add(p);                  // 맨 위로
        if (InSlot == p) { InSlot = null; p.open = false; }
        Act($"집음 {p.name}");
    }

    public void Move(float u, float v)
    {
        var p = Held; if (p == null) return;
        p.u = Mathf.Clamp(u + grabOff.x, -HalfW + p.w * 0.5f, HalfW - p.w * 0.5f);
        p.v = Mathf.Clamp(v + grabOff.y, -HalfH + p.d * 0.5f, HalfH - p.d * 0.5f);
    }

    public void End()
    {
        var p = Held; if (p == null) return;
        Held = null; p.lifted = false;
        bool inSlot = Mathf.Abs(p.u - SlotU) <= SlotW * 0.5f && Mathf.Abs(p.v - SlotV) <= SlotD * 0.5f;
        if (p.kind == PackPieceKind.Box && inSlot)
        {
            if (InSlot != null && InSlot != p) InSlot.Home();
            p.u = SlotU; p.v = SlotV; p.open = true; InSlot = p; OpenedCount++;
            Act($"{p.name} 가운데에 놓음 → 열림");
        }
        else Act($"놓음 {p.name} ({p.u:F2},{p.v:F2})");
    }

    /// <summary>봇: 사람과 같은 길(집기 → 끌기 → 놓기)로 조각 하나를 판 좌표 (u,v) 로.</summary>
    public bool SimDrag(PackPiece p, float toU, float toV)
    {
        if (p == null || !On || !p.active) return false;
        Begin(p.u, p.v);
        if (Held != p) return false;
        Move(toU, toV); End();
        return true;
    }

    void Act(string s) { LastAct = s; SignalBus.Mark("포장2D " + s); }
}
