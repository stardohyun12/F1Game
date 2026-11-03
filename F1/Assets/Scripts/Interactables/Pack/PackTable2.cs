using System.Collections;
using System.Collections.Generic;
using PitCrew.PackBench;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>포장 v3 물건 공통(D-290): 시선 + E 하나. 칩 글은 없다(안내 0) — 물건 배치와 손이 순서다.</summary>
public abstract class PackThing : MonoBehaviour
{
    public abstract void Use(PackTable2 t);
}

/// <summary>
/// D-290 포장대(SPEC §6-4, 도현 묘사): 받는 곳에서 물건 집기 → 포장대 → 상자 고르기 → 롤에서 한 장 → 바닥에 → 물건 넣기 → 한 장 덮기
/// → 닫기(E) → 테이프(왼쪽 버튼 누른 채 이음선을 따라 시선) → 들기(E)·흔들기(왼쪽 버튼) → 내려놓기(E) → 상자 들고 카트(E, BoxTaken).
/// 걷기 시점 그대로(작업대 화면 전환 없음). 결과 = Core.PackRules(상자 크기·완충재 궁합) + 흔들기 덜그럭.
/// 옛 PackBench 는 DepotPackSpot.useV3 스위치로 꺼 둔다(지우지 않음).
/// </summary>
public class PackTable2 : PackThing
{
    public static PackTable2 I { get; private set; }
    /// <summary>지금 손에 든 것(손 그리기 Presentation/PackHands 가 읽는다). 없으면 null.</summary>
    public static Transform HeldNow { get; private set; }

    [Header("자리(빌더가 채운다)")]
    public Transform workSpot;
    public Transform parcelRest;
    public ReceiveShelf receive;
    public BoxPick[] boxes;
    public Roll[] rolls;
    [Header("첫날 직원 한 줄(말풍선 — 세상 속)")]
    public Transform clerk;
    public TextMeshPro clerkBubble;
    public string[] clerkLines = { "(대사 자리 — 먼저 물건)", "(대사 자리 — 상자 골라)", "(대사 자리 — 깔고, 넣고, 덮고)", "(대사 자리 — 닫고 테이프, 흔들어 봐)" };
    public float reach = 2.2f;

    public enum Hold { None, Parcel, Box, Sheet, Lifted }
    public Hold Holding { get; private set; }
    public BoxPick WorkBox { get; private set; }
    public Parcel InBox { get; private set; }
    public PackFiller Bottom { get; private set; }
    public PackFiller Top { get; private set; }
    public PackFit Fit { get; private set; } = PackFit.Snug;
    public bool Picked { get; private set; }
    public bool Ready { get; private set; }
    public bool Taken { get; private set; }
    public float StartIntegrity01 { get; private set; } = 1f;
    public PackBox.ShakeResult Result { get; private set; }
    public int ClerkStep { get; private set; }
    public string LastAct { get; private set; } = "";

    Parcel heldParcel; BoxPick heldBox; GameObject heldSheet; PackFiller heldKind; float heldThick;
    Parcel dropped; float droppedAt;
    readonly List<GameObject> made = new List<GameObject>();
    float stack;   // 바닥에 깐 두께 합
    Transform hand; Camera cam;
    float shakeT, simShake; bool simTape;
    int rattle0; Vector3 jitter;
    Material sheetBubble, sheetPaper;
    readonly Use3 use3 = new Use3();

    public bool On => isActiveAndEnabled;

    void Awake()
    {
        I = this;
        var w = WorldData.Orders; if (w != null) w.ApplyPacking();
    }
    void OnDestroy() { if (I == this) I = null; HeldNow = null; }

    void Start()
    {
        PackBench.EnsureSfxOn(gameObject);
        var src = boxes != null && boxes.Length > 0 ? boxes[0].Box.boxMat : null;
        if (src != null)
        {
            sheetBubble = new Material(src) { name = "v3_bubble(runtime)" }; Tint(sheetBubble, new Color(0.78f, 0.84f, 0.86f));
            sheetPaper = new Material(src) { name = "v3_paper(runtime)" }; Tint(sheetPaper, new Color(0.86f, 0.84f, 0.78f));
        }
    }

    static void Tint(Material m, Color c) { if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); if (m.HasProperty("_Color")) m.SetColor("_Color", c); }

    Transform Hand()
    {
        var c = Camera.main;
        if (hand != null && cam == c) return hand;
        cam = c;
        if (cam == null) return null;
        var t = cam.transform.Find("PackHand");
        if (t == null) { t = new GameObject("PackHand").transform; t.SetParent(cam.transform, false); t.localPosition = new Vector3(0.10f, -0.26f, 0.55f); }
        return hand = t;
    }

    // ── 시선 ──
    readonly RaycastHit[] hits = new RaycastHit[16];

    /// <summary>Gaze 가 매 프레임 부른다: 시선이 포장 물건에 닿았거나(또는 상자를 들고 있으면 늘) 칩 없는 E 대상을 준다.</summary>
    public IInteractable Look(Transform eye, out string target)
    {
        target = null;
        if (Holding == Hold.Lifted) { use3.thing = this; target = name; return use3; }
        var th = RayThing(eye, out _);
        if (th == null) return null;
        target = th.name; use3.thing = th; return use3;
    }

    PackThing RayThing(Transform eye, out RaycastHit best)
    {
        best = default;
        int n = Physics.RaycastNonAlloc(eye.position, eye.forward, hits, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bd = float.MaxValue; Collider first = null;
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c == null || (hand != null && c.transform.IsChildOf(hand))) continue;
            if (c.GetComponentInParent<CharacterController>() != null) continue;
            if (hits[i].distance < bd) { bd = hits[i].distance; first = c; best = hits[i]; }
        }
        if (first == null) return null;
        var th = first.GetComponentInParent<PackThing>();
        if (th == null || !th.transform.IsChildOf(transform.parent != null ? transform.parent : transform)) return null;
        return th;
    }

    /// <summary>불변 검사용: 이 물건을 볼 때 뜨는 칩 글(안내 0 → 늘 "").</summary>
    public string PromptOf(PackThing t) { var u = new Use3 { thing = t }; return u.Prompt; }

    sealed class Use3 : Interactable
    {
        public PackThing thing;
        public override string Prompt => "";
        public override bool CanUse => thing != null;
        public override Vector3? Anchor => null;
        public override void Use() { if (I != null && thing != null) thing.Use(I); }
    }

    // ── 물건별 E ──
    /// <summary>작업면(포장대 판) E: 든 것을 내려놓는다.</summary>
    public override void Use(PackTable2 t)
    {
        if (Holding == Hold.Lifted) { PutDown(); return; }
        if (Holding == Hold.Box) { PlaceBox(heldBox); return; }
        if (Holding == Hold.Parcel)   // 상자에 넣기는 상자를 보고 E(UseBox) — 판을 보면 판 위에 내려놓는다
        {
            SetDown(heldParcel.transform, parcelRest); heldParcel.OnShelf = false; Release(); Act("물건을 작업면에"); return;
        }
        if (Holding == Hold.Sheet && WorkBox != null) { LaySheet(); return; }
    }

    public void UseParcel(Parcel p)
    {
        if (Holding != Hold.None) { if (Holding == Hold.Parcel && heldParcel == p) return; if (WorkBox != null && Holding == Hold.Sheet) { LaySheet(); return; } Deny(); return; }
        if (p == InBox || p == dropped) { InBox = null; dropped = null; }
        Grab(p.transform); heldParcel = p; Holding = Hold.Parcel; p.Body.isKinematic = true;
        bool first = !Picked; p.OnShelf = false; Picked = true;
        Act($"물건 집음: {p.cargoName} → {p.recipient}·{p.destLabel}");
        if (first) SignalBus.Raise(Signal.NotePicked, "물건");   // 칠판·쪽지 대신 — 뒤 흐름(주문 맡음)은 그대로
        Sfx(0);
    }

    public void UseBox(BoxPick b)
    {
        if (b == WorkBox)
        {
            if (Holding == Hold.Lifted) { PutDown(); return; }
            if (Holding == Hold.Parcel) { DropInto(heldParcel); return; }
            if (Holding == Hold.Sheet) { LaySheet(); return; }
            if (Holding != Hold.None) { Deny(); return; }
            if (Ready && !Taken) { Taken = true; SignalBus.Raise(Signal.BoxTaken, "포장 상자"); Act("상자 들고 나감"); return; }
            if (!b.Box.IsClosed) { if (b.Box.TryClose()) { Act("뚜껑 닫음"); Sfx(3); } else { Act("뚜껑 안 닫힘(솟은 것)"); Deny(); } return; }
            Lift(); return;
        }
        if (Holding == Hold.Box) { Deny(); return; }
        if (Holding != Hold.None) { Deny(); return; }
        if (WorkBox != null)
        {
            if (WorkBox.Box.IsClosed || InBox != null || stack > 0f) { Deny(); return; }   // 짐 든 상자는 그대로
            WorkBox.BackToShelf(); WorkBox = null;
        }
        Grab(b.transform); heldBox = b; Holding = Hold.Box;
        Act($"상자 {b.Box.sizeName} 집음"); Sfx(0);
    }

    public void UseRoll(Roll r)
    {
        if (Holding != Hold.None) { if (Holding == Hold.Sheet) return; Deny(); return; }
        if (!r.Take()) { Deny(); return; }
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = r.kind == PackFiller.Bubble ? "v3_Sheet_Bubble" : "v3_Sheet_Paper";
        var mat = r.kind == PackFiller.Bubble ? sheetBubble : sheetPaper;
        if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
        g.GetComponent<Collider>().enabled = false;
        g.transform.localScale = new Vector3(0.34f, r.thick, 0.30f);
        made.Add(g);
        Grab(g.transform); heldSheet = g; heldKind = r.kind; heldThick = r.thick; Holding = Hold.Sheet;
        Act($"{(r.kind == PackFiller.Bubble ? "뽁뽁이" : "종이")} 한 장 당김(남은 {r.Left})"); Sfx(5);
    }

    // ── 동작 ──
    void Grab(Transform t)
    {
        var h = Hand(); if (h == null) return;
        foreach (var c in t.GetComponentsInChildren<Collider>()) c.enabled = false;
        t.SetParent(h, false);
        t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity;
        HeldNow = t;
    }

    void Release() { Holding = Hold.None; heldParcel = null; heldBox = null; heldSheet = null; HeldNow = null; }

    void SetDown(Transform t, Transform at)
    {
        t.SetParent(at, false); t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity;
        foreach (var c in t.GetComponentsInChildren<Collider>()) c.enabled = true;
        Physics.SyncTransforms();
    }

    void PlaceBox(BoxPick b)
    {
        if (WorkBox != null) { Deny(); return; }
        SetDown(b.transform, workSpot);
        var bx = b.Box; if (bx.IsClosed) { bx.Open(); bx.SnapLid(); }
        WorkBox = b; Release(); stack = 0f; Bottom = Top = PackFiller.None; Ready = false;
        var tr = Trait();
        Fit = PackRules.FitOf(tr.size.x, tr.size.y, tr.size.z, bx.inner.x, bx.inner.y, bx.inner.z);
        Act($"상자 {bx.sizeName} 작업면에 (안 {bx.inner.x:F3}×{bx.inner.y:F3}×{bx.inner.z:F3})"); Sfx(3);
    }

    /// <summary>물건을 상자 입구 위에서 놓는다 — 들어가는지는 물리가 정한다(작은 상자 = 테두리에 걸림).</summary>
    void DropInto(Parcel p)
    {
        var bx = WorkBox.Box;
        p.transform.SetParent(transform.parent, true);   // PackV3 아래에 둬야 시선이 다시 잡는다
        float half = p.transform.lossyScale.y * 0.5f;
        p.transform.SetPositionAndRotation(bx.transform.TransformPoint(new Vector3(0f, bx.RimY + 0.03f + half, 0f)), bx.transform.rotation);
        foreach (var c in p.GetComponentsInChildren<Collider>()) c.enabled = true;
        Physics.SyncTransforms();
        p.Body.isKinematic = false; p.Body.linearVelocity = Vector3.zero; p.Body.angularVelocity = Vector3.zero; p.Body.WakeUp();
        dropped = p; droppedAt = Time.time; InBox = null; Release();
        Act("물건을 상자에 놓음(떨어지는 중)");
    }

    void LaySheet()
    {
        var bx = WorkBox.Box;
        if (bx.IsClosed) { Deny(); return; }
        var s = heldSheet.transform;
        s.SetParent(bx.transform, false);
        float y; bool below = InBox == null && dropped == null;
        if (below) { y = bx.InnerBottomY + stack + heldThick * 0.5f; stack += heldThick; if (Bottom == PackFiller.None) Bottom = heldKind; }
        else
        {
            var pp = (InBox != null ? InBox : dropped).transform;
            y = bx.transform.InverseTransformPoint(pp.position).y + pp.lossyScale.y * 0.5f + heldThick * 0.5f + 0.002f;
            y = Mathf.Min(y, bx.RimY - heldThick * 0.5f);
            if (Top == PackFiller.None) Top = heldKind;
        }
        s.localPosition = new Vector3(0f, y, 0f); s.localRotation = Quaternion.identity;
        s.localScale = new Vector3(bx.inner.x - 0.004f, heldThick, bx.inner.z - 0.004f);
        s.GetComponent<Collider>().enabled = true;
        Physics.SyncTransforms();
        Release();
        Act($"{(heldKind == PackFiller.Bubble ? "뽁뽁이" : "종이")} {(below ? "바닥에" : "위에 덮음")}"); Sfx(5);
    }

    void Lift()
    {
        var bx = WorkBox.Box;
        var h = Hand(); if (h == null) return;
        var player = FindFirstObjectByType<CharacterController>();
        if (player != null) foreach (var a in WorkBox.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(a, player);
        if (InBox != null && player != null) foreach (var a in InBox.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(a, player);
        Holding = Hold.Lifted; HeldNow = bx.transform; shakeT = 0f;
        rattle0 = PackBench.RattlePlayed;
        bx.BeginShake();
        Act("상자 들어 올림"); Sfx(0);
    }

    void PutDown()
    {
        var bx = WorkBox.Box;
        // 안의 물건도 같이 옮긴다 — 상자만 순간이동하면 물건이 손 자리 허공에 남아 바닥으로 떨어진다(D-290 버그)
        Vector3 pl = default; Quaternion pr = default;
        if (InBox != null) { pl = bx.transform.InverseTransformPoint(InBox.transform.position); pr = Quaternion.Inverse(bx.transform.rotation) * InBox.transform.rotation; }
        var r = bx.EndShake();
        bx.Body.MovePosition(workSpot.position); bx.Body.MoveRotation(workSpot.rotation);
        bx.transform.SetPositionAndRotation(workSpot.position, workSpot.rotation);
        if (InBox != null)
        {
            InBox.transform.SetPositionAndRotation(bx.transform.TransformPoint(pl), bx.transform.rotation * pr);
            InBox.Body.position = InBox.transform.position; InBox.Body.rotation = InBox.transform.rotation;
            InBox.Body.linearVelocity = Vector3.zero; InBox.Body.angularVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
        Result = r;
        string trait = Trait().trait;
        bool inside = InBox != null && bx.InsideInner(InBox.transform.position, 0.01f);
        StartIntegrity01 = inside ? PackRules.StartIntegrity(trait, Fit, Bottom, Top, r.rattle) : PackRules.MinStart;
        Ready = r.closed && r.taped && inside;
        Holding = Hold.None; HeldNow = null;
        Act($"내려놓음 rattle={r.rattle:F2} hits={r.hits} fit={Fit} 바닥={Bottom} 덮기={Top} 테이프={r.taped} → 시작 {StartIntegrity01 * 100f:F0}% Ready={Ready}");
        Sfx(3);
    }

    // ── 매 프레임 ──
    void Update()
    {
        var kb = Keyboard.current; var ms = Mouse.current;
        // 떨어진 물건이 자리 잡았나
        if (dropped != null && Time.time - droppedAt > 0.5f && dropped.Body.IsSleeping() || dropped != null && Time.time - droppedAt > 1.5f)
        {
            var bx = WorkBox != null ? WorkBox.Box : null;
            if (bx != null && bx.InsideInner(dropped.transform.position, 0.0f)) { InBox = dropped; Act($"물건 들어감 (fit={Fit})"); Sfx(1); }
            else Act("물건 걸림 — 안 들어감");
            dropped = null;
        }
        if (WorkBox != null && Holding == Hold.None)
        {
            var bx = WorkBox.Box;
            // 다시 열기: 오른쪽 클릭으로 뚜껑을 본 채
            if (ms != null && ms.rightButton.wasPressedThisFrame && bx.IsClosed && Looking(WorkBox)) { bx.Open(); Ready = false; Act("뚜껑 다시 엶"); }
            // 테이프: 왼쪽 버튼을 누른 채 이음선을 따라 시선을 끈다
            if ((ms != null && ms.leftButton.isPressed || simTape) && bx.IsClosed)
            {
                var e = Hand() != null ? cam.transform : null;
                if (simTape) { bx.AddTape(0f); bx.AddTape(1f); simTape = false; Act($"테이프(봇) {bx.TapeCover:F2}"); }
                else if (e != null && RayThing(e, out var hit) == WorkBox && bx.SeamParam(hit.point, out float t)) { float c0 = bx.TapeCover; bx.AddTape(t); if (bx.TapeCover > c0 + 0.05f) Sfx(5); }
            }
        }
        UpdateClerk();
    }

    public const float MaxLiftStep = 0.04f;

    void FixedUpdate()
    {
        if (Holding != Hold.Lifted || WorkBox == null) return;
        var h = Hand(); if (h == null) return;
        var ms = Mouse.current;
        bool shake = simShake > 0f || (ms != null && ms.leftButton.isPressed);
        if (simShake > 0f) simShake -= Time.fixedDeltaTime;
        if (shake) shakeT += Time.fixedDeltaTime;
        Vector3 off = shake ? (h.up * Mathf.Sin(shakeT * Mathf.PI * 2f * 4.5f) * 0.07f + h.right * Mathf.Sin(shakeT * Mathf.PI * 2f * 2.9f) * 0.05f) : Vector3.zero;
        // 떨림: 덜그럭이 날 때마다 손이 살짝 튄다
        int rp = PackBench.RattlePlayed;
        if (rp != rattle0) { rattle0 = rp; jitter = Random.insideUnitSphere * 0.012f; }
        jitter = Vector3.Lerp(jitter, Vector3.zero, 0.3f);
        var body = WorkBox.Box.Body;
        var target = h.position + h.forward * 0.05f - h.up * 0.02f + off + jitter;
        // 한 스텝 최대 4 cm(50 Hz = 2 m/s) — 순간이동하면 안의 물건이 바닥판을 뚫고 빠진다(D-290 버그)
        body.MovePosition(Vector3.MoveTowards(body.position, target, MaxLiftStep));
        body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(h.forward, Vector3.up).normalized, Vector3.up), 0.3f));
    }

    bool Looking(PackThing t) { var h = Hand(); return h != null && RayThing(cam.transform, out _) == t; }

    void UpdateClerk()
    {
        if (clerkBubble == null) return;
        int step = !Picked ? 0 : WorkBox == null ? 1 : !(WorkBox.Box.IsClosed) ? 2 : !Ready && !Taken ? 3 : 4;
        ClerkStep = step;
        var c = Camera.main;
        bool show = DayLog.Day == 1 && step < clerkLines.Length && c != null && clerk != null && Vector3.Distance(c.transform.position, clerk.position) < 7f;
        if (clerkBubble.gameObject.activeSelf != show) clerkBubble.gameObject.SetActive(show);
        if (!show) return;
        if (clerkBubble.text != clerkLines[step]) clerkBubble.text = clerkLines[step];
        var f = clerkBubble.transform.position - c.transform.position; f.y = 0f;
        if (f.sqrMagnitude > 1e-4f) clerkBubble.transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
    }

    WorldOrders.Trait Trait()
    {
        string cargo = receive != null && receive.today != null && receive.today.Length > 0 ? receive.today[0].cargoName : "달걀 한 판";
        var w = WorldData.Orders; var t = w != null ? w.TraitOf(cargo) : null;
        return t ?? new WorldOrders.Trait { cargo = cargo, trait = "깨짐", size = new Vector3(0.30f, 0.07f, 0.30f) };
    }

    void Act(string s) { LastAct = s; SignalBus.Mark("포장v3 " + s); }
    void Deny() { Act("손이 안 맞음(그대로)"); Sfx(4); }
    void Sfx(int k) => PackBench.PlayCue(k);

    // ── 하루·바깥 ──
    /// <summary>카트에 실음(DepotPackSpot.Take) — 작업면 상자를 치운다.</summary>
    public void Take()
    {
        Taken = true;
        if (WorkBox != null) WorkBox.gameObject.SetActive(false);
        if (InBox != null) InBox.gameObject.SetActive(false);   // 물건은 상자 자식이 아니다(물리로 들어 있음) — 같이 치운다
    }

    public void ResetForNextDay()
    {
        if (Holding == Hold.Lifted && WorkBox != null) WorkBox.Box.EndShake();
        Release();
        foreach (var g in made) if (g != null) Destroy(g);
        made.Clear();
        if (WorkBox != null) { WorkBox.gameObject.SetActive(true); WorkBox.BackToShelf(); }
        foreach (var b in boxes) if (b != null && b.transform.parent != b.shelfParent) b.BackToShelf();
        foreach (var r in rolls) if (r != null) r.Refill();
        if (receive != null) receive.ResetForNextDay();
        WorkBox = null; InBox = null; dropped = null; stack = 0f; Bottom = Top = PackFiller.None;
        Picked = Ready = Taken = false; StartIntegrity01 = 1f; Result = default; Fit = PackFit.Snug;
    }

    // ── 봇·QA: 사람과 같은 길(Use) — 시선 대신 대상을 바로 ──
    public void SimUse(PackThing t) { if (t != null) t.Use(this); }
    public void SimTape() { simTape = true; }
    public void SimShake(float sec) { simShake = sec; }
}
