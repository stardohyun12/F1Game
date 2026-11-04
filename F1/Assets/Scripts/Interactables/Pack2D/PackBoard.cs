using System.Collections.Generic;
using PitCrew.PackBench;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-294 포장 2D 작업대 ①: 화면 = 작업대 윗면. 카메라가 작업대 위로 내려가면(0.4 s, 세상 안 — 노을빛 그대로) 윗면이 판이 되고,
/// 왼쪽 = 받은 물건(08 의 Parcel·꼬리표 재사용) · 가운데 = 빈 자리(slot) · 오른쪽 = 상자 소/중/대 + 롤(뽁뽁이·종이) + 테이프. 마우스로 집어 놓는다(월드 평면 끌기 — UI 캔버스 아님).
/// 상자를 가운데에 놓으면 열린다. ②③: 롤을 끌면 한 장 뜯겨 나와 종이는 상자 바닥(첫 장)/물건 위(덮기)에 놓이고, 뽁뽁이는 물건 위에 놓으면 물건을 감싸고(D-296, 최대 2겹), 물건을 열린 상자에 끌면 들어간다(PackRules.FitOf — 작으면 튕김),
/// 테이프를 상자 위로 가로질러 끌면 뚜껑이 닫히고 줄 1개, 닫은 상자를 집고 흔들면 안의 물건이 PackPhysics 로 실제 움직이고(틈이 크면 많이 · 완충재가 틈을 메우면 거의 안) 부딪힐 때 소리, 상자를 판 밖(아래)으로 끌면 들고 나감(BoxTaken) → 1.5 s 흔들림 합 → PackRules.StartFromShake 가 화물 시작값.
/// D-295: 놓는 순간부터 물리 — 조각은 떨어져 자리 잡고, 물건은 손의 속도로 상자 안에서 미끄러지며, 작은 상자엔 벽에 걸려 튕겨 나온다.
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
    /// <summary>들고 나간 상자의 화물 시작값(0~1). 음수 = 아직.</summary>
    public float LastStart { get; private set; } = -1f;
    public PackBox.ShakeResult LastResult { get; private set; }
    public int TapeCount => InSlot != null ? InSlot.strips.Count : 0;

    PackTable2 table;
    Vector2 grabOff;
    readonly List<Vector2> path = new List<Vector2>();   // 테이프를 끈 궤적
    float rawU, rawV;                                      // 마우스가 판 위에서 가리킨 자리(조각은 판 안으로 잘림)
    bool pendingTake, simHold;
    PackItem sfxItem;
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
            foreach (var b in boxes) { cu = AddBox(cu, "상자 " + b.name, b.inner.x * BoxScale, b.inner.z * BoxScale, b.inner); }
        else
            foreach (var (n, s, y) in new[] { ("소", 0.22f, 0.12f), ("중", 0.315f, 0.15f), ("대", 0.46f, 0.24f) }) cu = AddBox(cu, "상자 " + n, s * BoxScale, s * BoxScale, new Vector3(s, y, s));

        float ru = 0.30f;
        var fl = pk != null && pk.fillers.Count > 0 ? pk.fillers : null;
        if (fl != null) foreach (var f in fl) ru = AddRoll(ru, f.name + " 롤", f.name, KindOf(f.kind, f.name), f.roll);
        else foreach (var n in new[] { "뽁뽁이", "종이" }) ru = AddRoll(ru, n + " 롤", n, KindOf(null, n), 12);
        Add(PackPieceKind.Tape, "테이프", ru + 0.05f, -0.2f, 0.09f, 0.09f);

        Add(PackPieceKind.Parcel, "물건", -0.72f, 0f, 0.30f, 0.30f).active = false;
    }

    float AddBox(float u, string n, float w, float d, Vector3 inner) { var p = Add(PackPieceKind.Box, n, u + w * 0.5f, 0.2f, w, d); p.inner = inner; return u + w + 0.06f; }
    float AddRoll(float u, string n, string filler, PackFiller kind, int full)
    {
        var p = Add(PackPieceKind.Roll, n, u + 0.11f, -0.2f, 0.22f, 0.11f);
        p.filler = filler; p.fillerKind = kind; p.full = p.left = full > 0 ? full : 12;
        return u + 0.22f + 0.06f;
    }
    PackPiece Add(PackPieceKind k, string n, float u, float v, float w, float d)
    {
        var p = new PackPiece { kind = k, name = n, w = w, d = d, homeU = u, homeV = v, homeW = w, homeD = d };
        p.Home(); pieces.Add(p); return p;
    }

    static PackFiller KindOf(string kind, string name)
    {
        if (kind == "Bubble" || name == "뽁뽁이") return PackFiller.Bubble;
        if (kind == "Paper" || name == "종이") return PackFiller.Paper;
        return PackFiller.None;
    }

    /// <summary>들어 있는 물건의 성질·크기(world.yaml cargo_traits) — PackTable2.Trait 와 같은 값.</summary>
    WorldOrders.Trait TraitOfParcel(PackPiece pp)
    {
        string cargo = pp != null && pp.parcel != null ? pp.parcel.cargoName : "달걀 한 판";
        var w = WorldData.Orders; var t = w != null ? w.TraitOf(cargo) : null;
        return t ?? new WorldOrders.Trait { cargo = cargo, trait = "깨짐", size = new Vector3(0.30f, 0.07f, 0.30f) };
    }

    /// <summary>D-296 감싼 물건의 실제 크기: 겹마다 세 변 모두 양쪽으로 WrapThick.</summary>
    public static Vector3 WrappedSize(Vector3 size, int wrap)
    {
        return new Vector3(PackRules.Wrapped(size.x, wrap), PackRules.Wrapped(size.y, wrap), PackRules.Wrapped(size.z, wrap));
    }

    /// <summary>물건 조각의 판 위 발자국을 실제 크기(감싼 것 포함) × 상자와 같은 축척으로. 돌린 상태(rot90)는 그대로. 집 자리(home) 크기는 안 감싼 그대로.</summary>
    void FitFootprint(PackPiece pp)
    {
        var sz = WrappedSize(TraitOfParcel(pp).size, pp.wrap);
        float fw = sz.x * BoxScale, fd = sz.z * BoxScale;
        pp.w = pp.rot90 ? fd : fw; pp.d = pp.rot90 ? fw : fd;
    }

    // ── D-295 물성: world.yaml packing (mass·bounce·friction) → PackPhysics ──
    /// <summary>놓는 순간 떨어지기 시작하는 높이(m) · 벽에 걸린 물건이 떠 있는 높이.</summary>
    public const float DropZ = 0.07f, RimZ = 0.05f;
    const float ThrowMax = 1.5f;

    // ── D-296 구긴 종이: world.yaml packing paper_* ──
    /// <summary>한 상자에 놓을 수 있는 구긴 종이 덩어리 수 · 덩어리가 메울 수 있는 틈 비율의 위(1 이면 물건이 꽉 끼어 소리가 아예 안 난다 — 종이는 그 정도는 아니다).</summary>
    public const int PaperMax = 6;
    public const float PaperFillMax = 0.85f;
    static WorldOrders.Packing Pk => WorldData.Orders != null ? WorldData.Orders.packing : null;
    public static float PaperRadius => Pk != null ? Pk.paperRadius : 0.04f;
    public static float PaperFriction => Pk != null ? Pk.paperFriction : 0.9f;
    public static float PaperMass => Pk != null ? Pk.paperMass : 0.01f;

    static PackPhysics.Mat BoxMat(string pieceName)
    {
        var pk = WorldData.Orders != null ? WorldData.Orders.packing : null;
        if (pk != null) foreach (var b in pk.boxes) if ("상자 " + b.name == pieceName) return new PackPhysics.Mat(b.mass, b.bounce, b.friction);
        return new PackPhysics.Mat(0.12f, 0.30f, 0.40f);
    }

    /// <summary>완충재 한 장의 물성과 두께(m). yaml fillers 에서 종류로 찾고, 없으면 기본값.</summary>
    public static PackPhysics.Mat FillerMat(PackFiller k, out float thick)
    {
        var pk = WorldData.Orders != null ? WorldData.Orders.packing : null;
        if (pk != null) foreach (var f in pk.fillers) if (KindOf(f.kind, f.name) == k) { thick = f.thick; return new PackPhysics.Mat(f.mass, f.bounce, f.friction); }
        thick = k == PackFiller.Paper ? 0.01f : 0.02f;
        return k == PackFiller.Paper ? new PackPhysics.Mat(0.01f, 0.30f, 0.60f) : new PackPhysics.Mat(0.02f, 0.10f, 0.90f);
    }

    /// <summary>
    /// 상자 안 물건의 물리 설정. 상자 틀에서 본 안쪽(눕힌 방향 + 돌림) · 물건 발자국 · 틈을 메우는 완충재 두께 합 · 마찰(깔린 면과 평균) ·
    /// 벽 반발 √(물건×상자) × 놓인 완충재 반발 유지율의 곱. 불변 검사도 이 함수로 같은 물리를 만든다.
    /// </summary>
    public static PackPhysics Setup(Vector3 inner, bool boxRot, PackPhysics.Mat boxMat, WorldOrders.Trait cargo, bool parcelRot, PackFiller bottom, PackFiller top, int wrap = 0, int wads = 0)
    {
        float sx = PackRules.Wrapped(cargo.size.x, wrap), sz = PackRules.Wrapped(cargo.size.z, wrap);   // D-296: 감싼 만큼 물건이 커서 틈이 준다(메우기 효과 pad 와는 별개)
        var ph = new PackPhysics
        {
            inner = boxRot ? new Vector2(inner.z, inner.x) : new Vector2(inner.x, inner.z),
            part = parcelRot ? new Vector2(sz, sx) : new Vector2(sx, sz),
            mass = cargo.mass,
            wrapK = PackRules.WrapK(wrap),
        };
        float surf = boxMat.friction, e = Mathf.Sqrt(Mathf.Max(0f, cargo.bounce * boxMat.bounce)), pad = 0f;
        foreach (var f in new[] { bottom, top })
        {
            if (f == PackFiller.None) continue;
            var fm = FillerMat(f, out float th);
            pad += th; e *= fm.bounce;
            if (f == bottom) surf = fm.friction;                              // 물건은 바닥 완충재 위에 앉는다
        }
        ph.pad = pad; ph.e = e * ph.wrapK; ph.mu = (cargo.friction + surf) * 0.5f;
        if (wads > 0)                                                         // D-296: 구긴 종이가 틈(상자 바닥 − 물건 발자국)의 얼마를 메우나 → 움직일 폭·반발↓, 마찰↑
        {
            float gapArea = Mathf.Max(1e-4f, inner.x * inner.z - sx * sz), r = PaperRadius;
            ph.fill = Mathf.Min(PaperFillMax, wads * Mathf.PI * r * r / gapArea);
            ph.e *= Mathf.Lerp(1f, FillerMat(PackFiller.Paper, out _).bounce, ph.fill);
            ph.mu = Mathf.Lerp(ph.mu, Mathf.Max(ph.mu, PaperFriction), ph.fill);
        }
        return ph;
    }

    PackPhysics NewPhys(PackPiece box, PackPiece pc)
    {
        return Setup(box.inner, box.rot90, BoxMat(box.name), TraitOfParcel(pc), pc.rot90, box.bottom, box.top, pc.wrap, box.wads.Count);
    }

    /// <summary>완충재가 더해졌다 → 같은 상자의 물리 설정만 새로(자리·속도·측정은 그대로).</summary>
    void RefreshPhys(PackPiece box)
    {
        if (box.phys == null || box.content == null) return;
        var n = NewPhys(box, box.content);
        box.phys.pad = n.pad; box.phys.e = n.e; box.phys.mu = n.mu; box.phys.fill = n.fill;
        var l = box.phys.Limit;
        box.phys.pos = new Vector2(Mathf.Clamp(box.phys.pos.x, -l.x, l.x), Mathf.Clamp(box.phys.pos.y, -l.y, l.y));
    }

    void PlaceContent(PackPiece box)
    {
        if (box.content == null) return;
        var o = box.phys != null ? box.phys.pos : Vector2.zero;
        box.content.u = box.u + o.x; box.content.v = box.v + o.y;
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
            if (pp != null)
            {
                pp.active = held != null && !Gone(pp);
                if (held != null && pp.parcel != held)
                {
                    pp.parcel = held;
                    var sz = TraitOfParcel(pp).size;                          // 판 위 발자국 = 실제 크기 × 상자와 같은 축척 → 상자 안에 들어가는지가 눈에 보인다
                    pp.homeW = sz.x * BoxScale; pp.homeD = sz.z * BoxScale; pp.Home();
                }
            }
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

    static bool Gone(PackPiece p) => p.holder != null && p.holder.kind == PackPieceKind.Box && !p.holder.active;

    public bool TakeExit() { bool e = ExitAsked; ExitAsked = false; return e; }

    public void ResetAll()
    {
        Held = null; InSlot = null; OpenedCount = 0; ExitAsked = false; pendingTake = false; simHold = false; LastStart = -1f; path.Clear();
        pieces.RemoveAll(p => p.kind == PackPieceKind.Sheet);
        foreach (var p in pieces)
        {
            p.Home(); p.active = true; p.left = p.full;
            if (p.kind == PackPieceKind.Parcel) { p.active = false; p.parcel = null; }
        }
    }

    // ── 입력(사람 = Update, 봇 = Sim*) ──
    void Update()
    {
        if (!On || Cam == null) return;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        var ms = Mouse.current; var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) ExitAsked = true;
        Tick(Mathf.Min(Time.unscaledDeltaTime, 0.05f));                      // 물리는 마우스와 상관없이 매 프레임
        if (simHold) return;                                                 // 봇이 쥐고 있는 동안은 마우스를 안 본다
        if (ms == null || !PointerOnBoard(ms.position.ReadValue(), out float u, out float v)) return;
        if (ms.leftButton.wasPressedThisFrame) Begin(u, v);
        if (Held != null)
        {
            if (kb != null && kb.rKey.wasPressedThisFrame) Rotate();
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
        if (p.kind == PackPieceKind.Roll)                                    // 롤: 집는 게 아니라 한 장 뜯겨 나온다
        {
            if (p.left <= 0) { Deny($"{p.name} 다 씀"); return; }
            p.left--;
            var roll = p;
            bool paper = roll.fillerKind == PackFiller.Paper;                 // D-296: 종이는 집는 순간 구겨진다(둥근 덩어리, 크기는 paper_radius × 상자와 같은 축척)
            float dm = PaperRadius * 2f * BoxScale;
            p = Add(PackPieceKind.Sheet, roll.filler + (paper ? " 구긴 덩어리" : " 한 장"), u, v, paper ? dm : 0.20f, paper ? dm : 0.18f);
            p.filler = roll.filler; p.fillerKind = roll.fillerKind; p.crumple = paper;
            PackBench.PlayCue(5);
            Act($"{p.filler} 한 장 뜯음{(paper ? " → 집는 순간 구겨짐" : "")}(롤에 {roll.left} 남음)");
        }
        else if (p.kind == PackPieceKind.Box && p.content != null && !p.closed) { Deny($"{p.name} 안에 물건이 있음(먼저 물건을 꺼낸다)"); return; }
        else if (p.kind == PackPieceKind.Box && !p.closed && p.Stuffed) { p.bottom = p.top = PackFiller.None; p.dropBottom = p.dropTop = default; p.wads.Clear(); Act($"{p.name} 바닥 완충재·구긴 종이 걷어냄"); }
        else if (p.kind == PackPieceKind.Parcel && p.holder != null) { p.holder.content = null; p.holder.top = PackFiller.None; p.holder.dropTop = default; p.holder.phys = null; p.holder = null; Act("물건을 상자에서 꺼냄(덮은 완충재는 버려짐)"); }
        Held = p; p.lifted = true; p.drop = default; p.hand = Vector2.zero; p.lastPos = new Vector2(p.u, p.v); grabOff = new Vector2(p.u - u, p.v - v);
        rawU = u; rawV = v;
        if (p.kind == PackPieceKind.Tape) { path.Clear(); path.Add(new Vector2(p.u, p.v)); }
        pieces.Remove(p); pieces.Add(p);                  // 맨 위로
        if (p.content != null) { pieces.Remove(p.content); pieces.Add(p.content); }
        if (InSlot == p) { InSlot = null; p.open = false; }
        if (p.kind != PackPieceKind.Sheet) { Act($"집음 {p.name}"); if (p.kind != PackPieceKind.Tape) PackBench.PlayCue(0); }
    }

    public void Move(float u, float v)
    {
        var p = Held; if (p == null) return;
        rawU = u; rawV = v;
        p.u = Mathf.Clamp(u + grabOff.x, -HalfW + p.w * 0.5f, HalfW - p.w * 0.5f);
        p.v = Mathf.Clamp(v + grabOff.y, -HalfH + p.d * 0.5f, HalfH - p.d * 0.5f);
        PlaceContent(p);
        if (p.kind == PackPieceKind.Tape && (path.Count == 0 || (path[path.Count - 1] - new Vector2(p.u, p.v)).sqrMagnitude > 0.0001f)) path.Add(new Vector2(p.u, p.v));
    }

    /// <summary>R: 든 조각을 90° 돌린다(상자·물건 발자국 w·d 가 바뀜).</summary>
    public void Rotate()
    {
        var p = Held; if (p == null || p.kind == PackPieceKind.Roll || p.kind == PackPieceKind.Tape) return;
        p.Rotate();
        Move(rawU, rawV);
        Act($"R 돌림 {p.name} {(p.rot90 ? 90 : 0)}°"); PackBench.PlayCue(0);
    }

    bool InSlotRect(PackPiece p) => Mathf.Abs(p.u - SlotU) <= SlotW * 0.5f && Mathf.Abs(p.v - SlotV) <= SlotD * 0.5f;

    public void End()
    {
        var p = Held; if (p == null) return;
        Held = null; p.lifted = false; p.jit = Vector2.zero;
        if (p.kind != PackPieceKind.Roll) p.drop.Drop(DropZ, DropBounce(p));          // 놓는 순간부터 떨어진다
        switch (p.kind)
        {
            case PackPieceKind.Box: EndBox(p); break;
            case PackPieceKind.Parcel: EndParcel(p); break;
            case PackPieceKind.Sheet: EndSheet(p); break;
            case PackPieceKind.Tape: EndTape(p); break;
            default: Act($"놓음 {p.name} ({p.u:F2},{p.v:F2})"); break;
        }
    }

    void EndBox(PackPiece p)
    {
        if (p.closed && p.Taped && p.content != null && rawV < -HalfH - 0.05f) { TakeOut(p); return; }   // 판 밖(아래)으로 = 들고 나감
        if (InSlotRect(p))
        {
            if (InSlot != null && InSlot != p) InSlot.Home();
            p.u = SlotU; p.v = SlotV; p.open = !p.closed; InSlot = p;
            PlaceContent(p);
            if (!p.closed) OpenedCount++;
            Act($"{p.name} 가운데에 놓음 → {(p.closed ? "닫힌 채" : "열림")}");
        }
        else Act($"놓음 {p.name} ({p.u:F2},{p.v:F2})");
    }

    void EndParcel(PackPiece p)
    {
        var box = InSlot;
        if (box == null || box.closed || box.content != null || !InSlotRect(p)) { Act($"놓음 {p.name} ({p.u:F2},{p.v:F2})"); return; }
        var sz = WrappedSize(TraitOfParcel(p).size, p.wrap);
        var inn = box.inner;
        float px = p.rot90 ? sz.z : sz.x, pz = p.rot90 ? sz.x : sz.z;
        float bx = box.rot90 ? inn.z : inn.x, bz = box.rot90 ? inn.x : inn.z;
        var fit = PackRules.FitOf(px, sz.y, pz, bx, inn.y, bz);
        if (fit == PackFit.TooSmall)                                          // 벽에 걸린다: 상자 가장자리에 얹혀 잠깐 떠 있다가 튕겨 나온다(Tick)
        {
            p.u = box.u; p.v = box.v; p.drop = default; p.perch = 0.45f; PackBench.PlayCue(4);
            Act($"물건이 {box.name} 에 안 들어감(물건 {px:F2}×{pz:F2} > 안 {bx:F2}×{bz:F2}) → 벽에 걸림");
            return;
        }
        box.content = p; p.holder = box; box.fit = fit;
        var rel = new Vector2(p.u - box.u, p.v - box.v);
        box.phys = NewPhys(box, p);                                           // 놓은 자리·손의 속도 그대로 상자 안에서 미끄러진다
        box.phys.Reset(rel, Vector2.ClampMagnitude(p.hand, ThrowMax));
        PlaceContent(box);
        pieces.Remove(p); pieces.Insert(pieces.IndexOf(box) + 1, p);
        PackBench.PlayCue(1);
        Act($"물건{(p.wrap > 0 ? $"(뽁뽁이 {p.wrap}겹)" : "")}이 {box.name} 에 들어감 fit={fit}{(fit == PackFit.Loose ? "(헐거움)" : "")} 틈 {box.phys.Limit.x * 2f * 100f:F1}×{box.phys.Limit.y * 2f * 100f:F1} cm 던진 속도 {box.phys.vel.magnitude:F2} m/s");
    }

    void EndSheet(PackPiece p)
    {
        pieces.Remove(p);
        if (p.fillerKind == PackFiller.Bubble) { WrapWith(p); return; }          // D-296: 뽁뽁이는 깔지 않는다 — 물건을 감싼다
        var box = InSlot;
        if (p.crumple) { PutWad(p, box); return; }                               // D-296: 구긴 종이는 깔지도 덮지도 않는다 — 틈에 들어간다
        if (box == null || box.closed || !InSlotRect(p)) { Act($"{p.filler} 한 장 놓친 자리에 버려짐"); return; }
        float e = FillerMat(p.fillerKind, out _).bounce;
        if (box.content == null) { if (box.bottom == PackFiller.None) { box.bottom = p.fillerKind; box.dropBottom.Drop(DropZ, e); } Act($"{p.filler} 바닥에 깔림"); }
        else { if (box.top == PackFiller.None) { box.top = p.fillerKind; box.dropTop.Drop(DropZ, e); RefreshPhys(box); } Act($"{p.filler} 물건 위에 덮임"); }
        PackBench.PlayCue(5);
    }

    /// <summary>
    /// D-296 구긴 종이를 열린 상자(가운데에 놓인) 위에서 놓으면 떨어져 틈에 자리 잡는다. 상자 밖이면 버려지고, 한 상자 PaperMax 덩어리가 넘으면 그것도 버려진다.
    /// 물건 위에 놓아도 감싸지 않는다 — 물건 가장자리 밖 틈으로 밀려난다. 놓인 덩어리 수가 물리의 fill(틈 메운 비율)을 바꾼다.
    /// </summary>
    void PutWad(PackPiece sheet, PackPiece box)
    {
        if (box == null || box.closed || Mathf.Abs(sheet.u - box.u) > box.w * 0.5f || Mathf.Abs(sheet.v - box.v) > box.d * 0.5f) { Act("구긴 종이 상자 밖에 놓여 버려짐"); return; }
        if (box.wads.Count >= PaperMax) { Deny($"구긴 종이 {box.name} 에 이미 {PaperMax}덩어리 — 더 못 넣음(버려짐)"); return; }
        float rad = PaperRadius * BoxScale;
        var off = new Vector2(sheet.u - box.u, sheet.v - box.v);
        if (box.content != null)
        {
            var co = new Vector2(box.content.u - box.u, box.content.v - box.v);
            float hx = box.content.w * 0.5f, hz = box.content.d * 0.5f;
            var rel = off - co;
            if (Mathf.Abs(rel.x) < hx && Mathf.Abs(rel.y) < hz)                  // 물건 위 → 가까운 가장자리 밖(틈)으로
            {
                if (hx - Mathf.Abs(rel.x) < hz - Mathf.Abs(rel.y)) off.x = co.x + (rel.x >= 0f ? 1f : -1f) * (hx + rad * 0.3f);
                else off.y = co.y + (rel.y >= 0f ? 1f : -1f) * (hz + rad * 0.3f);
            }
        }
        float mx = box.w * 0.5f - rad * 0.3f, mz = box.d * 0.5f - rad * 0.3f;
        for (int pass = 0; pass < 8; pass++)                                      // 먼저 놓인 덩어리와 겹치지 않게 조금씩 비킨다
        {
            off = new Vector2(Mathf.Clamp(off.x, -mx, mx), Mathf.Clamp(off.y, -mz, mz));
            bool moved = false;
            foreach (var w in box.wads)
            {
                var d = off - w.off;
                if (d.magnitude >= rad * 1.2f) continue;
                off = w.off + (d.sqrMagnitude > 1e-6f ? d.normalized : Vector2.right) * rad * 1.2f; moved = true;
            }
            if (!moved) break;
        }
        var wad = new PackPiece.Wad { off = off };
        wad.fall.Drop(DropZ, 0.15f);                                              // 가볍고 푹신해서 거의 안 튄다
        box.wads.Add(wad);
        RefreshPhys(box);
        PackBench.PlayCue(5);
        float fill = box.phys != null ? box.phys.fill : 0f;
        Act($"구긴 종이가 {box.name} 틈에 자리 잡음 {box.wads.Count}/{PaperMax}덩어리(종이 {box.wads.Count * PaperMass * 1000f:F0} g{(box.phys != null ? $" · 틈 {fill * 100f:F0}% 메움" : "")})");
    }

    /// <summary>
    /// D-296 뜯은 뽁뽁이 한 장을 물건 위에 놓으면 물건을 감싼다(최대 PackRules.WrapMax 겹). 물건 밖에 놓은 장·상자 안 물건 위의 장은 버려진다.
    /// 감싸면 물건이 겹마다 양쪽 WrapThick 씩 커지고(발자국·상자 맞춤·물리 모두), 겹 계수가 부딪힘 세기·반발·손상을 줄인다. 틈을 메우는 효과(pad)는 없다.
    /// </summary>
    void WrapWith(PackPiece sheet)
    {
        var pc = Find(PackPieceKind.Parcel);
        bool over = pc != null && pc.active && pc.perch <= 0f && Mathf.Abs(sheet.u - pc.u) <= pc.w * 0.5f + 0.04f && Mathf.Abs(sheet.v - pc.v) <= pc.d * 0.5f + 0.04f;
        if (!over) { Deny($"{sheet.filler} 한 장 물건 밖에 놓여 버려짐(물건 위로 끌어다 놓아야 감싼다)"); return; }
        if (pc.holder != null) { Deny($"{sheet.filler} 상자 안 물건은 못 감쌈(꺼내서 감싼다)"); return; }
        if (pc.wrap >= PackRules.WrapMax) { Deny($"{sheet.filler} 이미 {pc.wrap}겹 — 더 못 감쌈"); return; }
        pc.wrap++; FitFootprint(pc);
        pc.drop.Drop(DropZ, DropBounce(pc));
        PackBench.PlayCue(5);
        Act($"{sheet.filler} 물건을 감쌈 {pc.wrap}겹(크기 +{(PackRules.Wrapped(1f, pc.wrap) - 1f) * 100f:F1} cm · 겹 계수 {PackRules.WrapK(pc.wrap):F2})");
    }

    void EndTape(PackPiece p)
    {
        var box = InSlot;
        p.Home();
        if (box == null || path.Count < 2) { Act("테이프 놓음(상자 없음)"); path.Clear(); return; }
        float cu = box.u, cv = box.v, hw = box.w * 0.5f, hd = box.d * 0.5f;
        float minU = float.MaxValue, maxU = float.MinValue, minV = float.MaxValue, maxV = float.MinValue, sumU = 0f, sumV = 0f; int n = 0;
        foreach (var q in path)
        {
            minU = Mathf.Min(minU, q.x); maxU = Mathf.Max(maxU, q.x); minV = Mathf.Min(minV, q.y); maxV = Mathf.Max(maxV, q.y);
            if (Mathf.Abs(q.x - cu) <= hw && Mathf.Abs(q.y - cv) <= hd) { sumU += q.x; sumV += q.y; n++; }
        }
        path.Clear();
        bool horiz = minU < cu - hw && maxU > cu + hw, vert = minV < cv - hd && maxV > cv + hd;
        if (n == 0 || (!horiz && !vert)) { Act("테이프가 상자를 가로지르지 못함"); return; }
        bool h = horiz;                                                       // 둘 다면 가로
        var strip = new PackStrip { horiz = h, du = h ? 0f : sumU / n - cu, dv = h ? sumV / n - cv : 0f, len = h ? box.w : box.d };
        strip.du = Mathf.Clamp(strip.du, -hw * 0.6f, hw * 0.6f); strip.dv = Mathf.Clamp(strip.dv, -hd * 0.6f, hd * 0.6f);
        bool wasOpen = !box.closed;
        box.closed = true; box.open = false; box.strips.Add(strip);
        if (wasOpen && box.phys != null) { box.phys.ResetMeasure(); box.rattle = 0f; box.shakeSecs = 0f; }   // 닫은 순간부터 잰다
        PackBench.PlayCue(wasOpen ? 3 : 5);
        Act($"테이프 {(h ? "가로" : "세로")} 줄 {box.strips.Count}개{(wasOpen ? " — 뚜껑 닫힘" : "")}");
    }

    // ── 물리 한 걸음(매 프레임) · 흔들기: 닫은 상자를 집은 채 마우스가 오가면 상자 가속 → 안의 물건이 실제로 움직인다 ──
    static float DropBounce(PackPiece p)
    {
        if (p.kind == PackPieceKind.Box) return BoxMat(p.name).bounce;
        if (p.kind == PackPieceKind.Parcel && p.parcel != null) { var t = WorldData.Orders != null ? WorldData.Orders.TraitOf(p.parcel.cargoName) : null; if (t != null) return t.bounce; }
        return 0.3f;
    }

    /// <summary>떨어지는 것·걸린 것·상자 안 물건을 dt 만큼. 상자를 쥐고 있으면 손 속도의 변화 = 상자 가속(m/s²)을 안의 물리에 먹인다.</summary>
    public void Tick(float dt)
    {
        if (dt <= 0f) return;
        foreach (var p in pieces.ToArray())
        {
            if (p.kind != PackPieceKind.Roll && p.kind != PackPieceKind.Tape) p.drop.Step(dt);
            p.dropBottom.Step(dt); p.dropTop.Step(dt);
            foreach (var w in p.wads) w.fall.Step(dt);
            if (p.crumple && p.crumpleT < 1f) p.crumpleT = Mathf.Min(1f, p.crumpleT + dt / 0.12f);
            var pos = new Vector2(p.u, p.v);
            Vector2 acc = Vector2.zero;
            if (Held == p)
            {
                var v = (pos - p.lastPos) / dt;
                acc = Vector2.ClampMagnitude((v - p.hand) / dt, 300f);
                p.hand = v;
            }
            p.lastPos = pos;
            if (p.perch > 0f)
            {
                p.perch -= dt;
                if (p.perch <= 0f) { p.perch = 0f; p.Home(); PackBench.PlayCue(4); Act("벽에 걸린 물건이 튕겨 나옴"); }
            }
            if (p.kind != PackPieceKind.Box || p.phys == null) continue;
            p.phys.Advance(dt, acc);
            PlaceContent(p);
            if (p.closed) { p.rattle = p.phys.PeakWindow; p.shakeSecs = p.phys.ShakeTime; }
            p.jit = p.closed ? Vector2.ClampMagnitude(p.phys.vel * 0.03f, 0.02f) : Vector2.zero;
            float im = p.phys.TakeImpact();
            if (im > 0.02f) Rattle(im);
        }
    }

    void Rattle(float impact)
    {
        if (sfxItem == null) { var g = new GameObject("pack2d_sfx") { hideFlags = HideFlags.HideAndDontSave }; sfxItem = g.AddComponent<PackItem>(); sfxItem.kind = PackItemKind.Eggs; }
        PackBench.PlayRattle(sfxItem, Mathf.Clamp01(impact / 0.5f));
        RattleBeats++;
    }

    /// <summary>QA: 이 보드가 낸 덜그럭 소리 수.</summary>
    public int RattleBeats { get; private set; }

    /// <summary>QA: 떨어지는 조각·걸린 물건이 아직 있나(없으면 다 자리 잡음).</summary>
    public bool Settling
    {
        get
        {
            foreach (var p in pieces)
            {
                if (p.drop.Active || p.dropBottom.Active || p.dropTop.Active || p.perch > 0f) return true;
                foreach (var w in p.wads) if (w.fall.Active) return true;
            }
            return false;
        }
    }

    /// <summary>마지막으로 들고 나간 상자의 흔들림 측정(m)과 어떻게 쟀는지.</summary>
    public float LastShake { get; private set; }
    public string LastShakeHow { get; private set; } = "";

    /// <summary>상자를 판 밖(아래)으로 끌었다: 흔들림 측정 → 화물 시작값을 Core 로 구해 포장대(PackTable2)에 넘기고, 판에서 물러난 뒤 BoxTaken 을 올린다.</summary>
    void TakeOut(PackPiece box)
    {
        var parcel = box.content;
        var tr = TraitOfParcel(parcel);
        float shake; string how;
        if (box.phys == null) { shake = 0f; how = "물리 없음"; }
        else if (box.shakeSecs >= PackPhysics.Window) { shake = box.rattle; how = $"직접 흔든 {box.shakeSecs:F1}s 중 1.5s 합"; }
        else { shake = box.phys.MeasureStandard(); how = $"흔든 시간 {box.shakeSecs:F1}s < 1.5s → 표준 흔들기(진폭 {PackPhysics.StdAmp * 100f:F0}cm·{PackPhysics.StdHz:F0}Hz)로 잼"; }
        float wrapK = box.phys != null ? box.phys.wrapK : 1f;
        var top = box.top == PackFiller.None && box.wads.Count > 0 ? PackFiller.Paper : box.top;   // D-296: 구긴 종이가 있으면 종이 궁합(깨짐 0.2)도 한 장 몫
        float start = PackRules.StartFromShake(tr.trait, box.bottom, top, shake, wrapK);
        var lim = box.phys != null ? box.phys.Limit : Vector2.zero;
        var r = new PackBox.ShakeResult
        {
            box = box.name, seconds = box.shakeSecs, rattle = shake, closed = true, taped = true, upright = true, locks = "", faces = "",
            gap = Mathf.Max(lim.x, lim.y) * 2f,
        };
        LastStart = start; LastResult = r; LastShake = shake; LastShakeHow = how;
        if (table != null) table.Accept2D(start, r, box.bottom, box.top, box.fit);
        Act($"상자 들고 나감 → 화물 시작 {start * 100f:F0}% (흔들림 {shake * 100f:F1} cm / {how}; 상자 {box.name} 바닥={box.bottom} 덮기={box.top} 감쌈 {parcel.wrap}겹(계수 {wrapK:F2}) 테이프 {box.strips.Count}줄)");
        box.active = false; parcel.active = false; InSlot = null;
        pendingTake = true; ExitAsked = true;
    }

    /// <summary>상자를 판 밖으로 들고 나갔다 → Stage_Packing 이 걷기 시점으로 돌아온 뒤 BoxTaken 을 올린다(Interactables 는 Stages 를 못 본다).</summary>
    public bool PendingTake => pendingTake;
    public void ConsumeTake() { pendingTake = false; }

    // ── 봇: 사람과 같은 길(집기 → 끌기 → 놓기). 롤을 집으면 뜯긴 한 장이 손에 온다 ──
    /// <summary>조각 하나를 판 좌표 (u,v) 로. 롤이면 한 장을 뜯어 그 자리로.</summary>
    public bool SimDrag(PackPiece p, float toU, float toV)
    {
        if (p == null || !On || !p.active) return false;
        Begin(p.u, p.v);
        if (Held == null) return false;
        Move(toU, toV); End();
        return true;
    }

    /// <summary>봇: SimDrag 인데 놓는 순간 손 속도(m/s)가 handV 였던 것처럼 — 던져 넣기.</summary>
    public bool SimDragThrow(PackPiece p, float toU, float toV, Vector2 handV)
    {
        if (p == null || !On || !p.active) return false;
        Begin(p.u, p.v);
        if (Held != p) return false;
        Move(toU, toV); p.hand = handV; End();
        return true;
    }

    /// <summary>봇: 테이프를 궤적(판 좌표 점들)대로 끌어 놓는다.</summary>
    public bool SimDragPath(PackPiece p, params Vector2[] pts)
    {
        if (p == null || !On || !p.active || pts.Length == 0) return false;
        Begin(p.u, p.v);
        if (Held != p) return false;
        foreach (var q in pts) Move(q.x, q.y);
        End();
        return true;
    }

    /// <summary>봇: 상자를 쥐고(마우스는 안 본다) — 이어서 SimShakeTick 을 매 프레임 — SimRelease 로 놓는다.</summary>
    public bool SimGrab(PackPiece p)
    {
        if (p == null || !On || !p.active) return false;
        Begin(p.u, p.v);
        if (Held != p) return false;
        simHold = true; return true;
    }

    /// <summary>봇: 쥔 상자를 좌우로 흔든다(한 프레임분). 사람이 마우스를 빠르게 오가는 것과 같은 길(Move → 다음 Tick 이 가속을 읽는다).</summary>
    public void SimShakeTick(float dt, float time)
    {
        if (Held == null) return;
        Move(SlotU + Mathf.Sin(time * 28f) * 0.12f, SlotV + Mathf.Cos(time * 19f) * 0.03f);   // 물리는 Update 의 Tick 이 돈다
    }

    public bool SimRelease(float toU, float toV)
    {
        if (Held == null) return false;
        simHold = false; Move(toU, toV); End();
        return true;
    }

    public void SimRotate() { Rotate(); }

    /// <summary>QA: 보드를 켠 채 처음 모습으로(상자·롤·테이프 제자리, 물건은 다시 선반 위에). 시험 한 판을 치우고 다음 판을 하려고.</summary>
    public void DebugRestart()
    {
        var parcel = Find(PackPieceKind.Parcel)?.parcel;
        ResetAll();
        var pp = Find(PackPieceKind.Parcel);
        if (pp != null && parcel != null) { pp.parcel = parcel; pp.active = true; }
        RattleBeats = 0; Act("QA 다시 시작");
    }

    void Deny(string s) { PackBench.PlayCue(4); Act(s); }
    void Act(string s) { LastAct = s; SignalBus.Mark("포장2D " + s); }
}
