using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-287 3단계: 문 앞 단계 — 상자 들고 다니기·문 두드리기·건네기. 감독이 만들고 부른다(MonoBehaviour 아님).
/// 듣는 신호: BoxTaken(작업대 상자 듦) · BoxLoaded(카트에 실음) · CartLeft(내림 → 도착했으면 상자 들기) · DoorKnocked(문 두드리기) · DayStarted.
/// 쏘는 신호: HandedOver(건네기 끝 → Stage_Return 이 노을).
/// 지도: docs/world/CODEMAP.md
/// </summary>
public class Stage_Door : DayStage
{
    public Stage_Door(OnFootDirector d) : base(d) { }
    public override OnFootDirector.Mode Mode => OnFootDirector.Mode.OnFoot;

    /// <summary>상자를 들고 있다(화면 아래 상자).</summary>
    public bool CarryingBox { get; private set; }
    /// <summary>문을 두드렸다 ~ 받는 사람이 나올 때까지(입력 막음).</summary>
    public bool Knocking { get; private set; }
    public bool HandedOver { get; private set; }
    /// <summary>건넬 때 화물 상태 3단(0 멀쩡 · 1 조금 상함 · 2 깨짐, -1 = 아직).</summary>
    public int TierIdx { get; private set; } = -1;
    public int Money { get; private set; }
    /// <summary>C-003: 보관소 흐름에서 카트 짐칸 상자(카트 모델의 delivery_box·끈)는 실었을 때만 보인다 — 빈 카트로 와서, 실으면 생기고, 내려 들면 사라진다.</summary>
    public bool BedBox { get; private set; }
    GameObject heldBox, heldBoxGiven;
    Renderer[] bedRs; int bedShown = -1;

    static CargoBox Cargo => Services.Cargo as CargoBox ?? Object.FindFirstObjectByType<CargoBox>();

    public override void Plug()
    {
        base.Plug();
        Listen(Signal.BoxTaken, _ => CarryingBox = true);
        Listen(Signal.BoxLoaded, _ => { CarryingBox = false; BedBox = true; });
        Listen(Signal.CartLeft, _ => AfterCart());
        Listen(Signal.DoorKnocked, _ => Knock());
    }

    protected override void NewDay()
    {
        CarryingBox = false; Knocking = false; HandedOver = false; BedBox = false;
        TierIdx = -1; Money = 0;
        // 손에 든 상자: 받는 사람에게 건넨 것은 치우고 새로 만든다(MoveBoxToHands 가 heldBox 를 비웠다)
        if (heldBoxGiven != null) { Object.Destroy(heldBoxGiven); heldBoxGiven = null; }
        if (heldBox == null) BuildHeldBox();
        // 화물(카트 짐칸) — BeginCarry 가 꺼 둔 모양을 되살린다
        var cargo = Cargo;
        if (cargo != null && d.car != null && cargo.transform != d.car.transform)
            foreach (var r in cargo.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
    }

    /// <summary>모드와 상관없이 매 프레임: 손에 든 상자·카트 짐칸 상자 보이기.</summary>
    public override void Always() { UpdateHeldBox(); UpdateBedBox(); }

    void AfterCart()
    {
        var h = d.handover;
        if (d.Delivered && h != null && !HandedOver && !CarryingBox) { BeginCarry(); d.FocusThing(h.DoorCenter, "배달지 문"); }
    }

    /// <summary>카트 짐칸에서 상자를 내려 든다 — 짐칸 상자는 숨기고 화면 아래 상자(대역)를 켠다.</summary>
    internal void BeginCarry()
    {
        CarryingBox = true;
        BedBox = false;
        var cargo = Cargo;
        if (cargo != null && d.car != null && cargo.transform != d.car.transform)
            foreach (var r in cargo.GetComponentsInChildren<Renderer>()) r.enabled = false;
        d.Mark($"상자 내림(들고 걷기) 화물={(cargo != null ? (cargo.IntegrityPercent * 100f).ToString("F0") + "%" : "?")}");
    }

    /// <summary>걷는 동안만 손에 든 상자를 보인다(카메라는 주행과 같이 쓰므로 카트·대화 시점에선 끈다).</summary>
    void UpdateHeldBox()
    {
        if (heldBox == null) return;
        bool on = CarryingBox && d.State == OnFootDirector.Mode.OnFoot && d.footCam != null && d.footCam.enabled;
        if (heldBox.activeSelf != on) heldBox.SetActive(on);
    }

    void UpdateBedBox()
    {
        var car = d.car;
        if (!d.DepotFlow || car == null) return;
        if (bedRs == null || bedRs.Length == 0 || bedRs[0] == null)   // 카트 모델은 감독보다 늦게 붙거나 바뀐다 → 못 찾았으면 다시
        {
            var l = new List<Renderer>();
            foreach (var r in car.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("delivery_box") || r.name.StartsWith("delivery_strap")) l.Add(r);
            bedRs = l.ToArray();
            bedShown = -1;
            if (bedRs.Length == 0) return;
        }
        int want = BedBox ? 1 : 0;
        if (want == bedShown) return;
        bedShown = want;
        foreach (var r in bedRs) if (r != null) r.enabled = BedBox;
    }

    /// <summary>손에 든 상자의 크기(m, 카메라 기준 가로·높이·깊이) — 예전 회색 대역 큐브 크기 그대로.</summary>
    public static readonly Vector3 HeldSize = new Vector3(0.42f, 0.24f, 0.32f);

    /// <summary>손에 든 상자: 카메라 자식, 화면 아래 가운데 조금 오른쪽. 모양 = 보관소 선반 택배 상자(PARCEL_*) 메시·재질 그대로(없으면 회색 큐브). 감독 Start 에서 한 번.</summary>
    internal void BuildHeldBox()
    {
        heldBox = new GameObject("HeldBox");
        heldBox.transform.SetParent(d.cam.transform, false);
        heldBox.transform.localPosition = new Vector3(0.06f, -0.34f, 0.62f);
        heldBox.transform.localRotation = Quaternion.Euler(14f, -4f, 0f);
        var src = FindShelfParcel(out _);
        if (src != null) AddParcelModel(heldBox.transform, src);
        else AddGreyCube(heldBox.transform);
        foreach (var r in heldBox.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        heldBox.SetActive(false);
    }

    /// <summary>선반 택배 상자(보관소 건물 q3_depot 아래 PARCEL_*) 중 HeldSize 와 비례(가로:높이:깊이)가 가장 가까운 것. 못 찾으면 null. ratioErr = 가장 가까운 것의 비례 오차(로그 합, 0 = 같음).</summary>
    public static MeshFilter FindShelfParcel(out float ratioErr)
    {
        MeshFilter best = null; ratioErr = float.MaxValue;
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!mf.name.StartsWith("PARCEL_") || mf.sharedMesh == null || mf.transform.parent == null || mf.transform.parent.name != "q3_depot") continue;
            if (mf.GetComponent<MeshRenderer>() == null) continue;
            var s = ParcelBounds(mf).size;
            if (s.x < 0.01f || s.y < 0.01f || s.z < 0.01f) continue;
            float e = Mathf.Abs(Mathf.Log(s.x / s.y) - Mathf.Log(HeldSize.x / HeldSize.y)) + Mathf.Abs(Mathf.Log(s.z / s.y) - Mathf.Log(HeldSize.z / HeldSize.y));
            if (e < ratioErr) { ratioErr = e; best = mf; }
        }
        return best;
    }

    /// <summary>선반 상자 메시의 크기·중심 — 건물 안 자기 회전(fbx 위쪽 축 보정)·배율만 적용한 부모 기준 값(건물 방향은 안 탄다).</summary>
    static Bounds ParcelBounds(MeshFilter mf)
    {
        var m = Matrix4x4.TRS(Vector3.zero, mf.transform.localRotation, mf.transform.localScale);
        var mb = mf.sharedMesh.bounds;
        var b = new Bounds(m.MultiplyPoint3x4(mb.center), Vector3.zero);
        for (int k = 0; k < 8; k++)
            b.Encapsulate(m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1f : 1f, (k & 2) == 0 ? -1f : 1f, (k & 4) == 0 ? -1f : 1f))));
        return b;
    }

    /// <summary>선반 상자 메시·재질을 holder 자식으로 복사한다(재질은 공유 그대로 읽기만). 크기 = HeldSize 와 같은 부피, 중심이 holder 원점.</summary>
    static void AddParcelModel(Transform holder, MeshFilter src)
    {
        var b = ParcelBounds(src);
        float k = Mathf.Pow(HeldSize.x * HeldSize.y * HeldSize.z / (b.size.x * b.size.y * b.size.z), 1f / 3f);
        var go = new GameObject("Parcel");
        go.transform.SetParent(holder, false);
        go.transform.localRotation = src.transform.localRotation;
        go.transform.localScale = src.transform.localScale * k;
        go.transform.localPosition = -b.center * k;
        go.AddComponent<MeshFilter>().sharedMesh = src.sharedMesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = src.GetComponent<MeshRenderer>().sharedMaterials;
    }

    /// <summary>선반 상자를 못 찾았을 때만 — 예전 회색 큐브.</summary>
    static void AddGreyCube(Transform holder)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "GreyCube";
        Object.Destroy(cube.GetComponent<Collider>());
        cube.transform.SetParent(holder, false);
        cube.transform.localScale = HeldSize;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", new Color(0.58f, 0.57f, 0.55f)); mpb.SetColor("_Color", new Color(0.58f, 0.57f, 0.55f));
        cube.GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
    }

    internal void Knock()
    {
        var h = d.handover;
        Knocking = true;
        d.player.enabled = false;
        d.SetPrompt(""); d.PromptAnchor = null;
        var cargo = Cargo;
        float pct = cargo != null ? cargo.IntegrityPercent : 1f;
        TierIdx = HandoverTier.Of(pct);
        h.PlayKnock();
        d.Mark($"똑똑 — 문 {h.name} 화물 {pct * 100f:F0}% → {HandoverTier.Name[TierIdx]}");
        d.StartCoroutine(KnockSeq(pct));
    }

    IEnumerator KnockSeq(float pct)
    {
        var h = d.handover;
        yield return new WaitForSecondsRealtime(d.knockWaitSec);
        yield return h.Open(d.player.transform.position);
        Knocking = false;
        string who = h.recipientName;
        var lines = new[] { (who, HandoverTier.Greeting), (who, HandoverTier.Reaction[TierIdx]) };
        d.Mark($"{who} 나옴 {h.recipient.position.ToString("F1")}");
        d.BeginTalk(lines, h.recipient, line => { if (line == 1) GiveBox(pct); }, EndHandover);
    }

    /// <summary>둘째 줄(반응)로 넘어갈 때 상자를 건넨다 — 상자가 받는 사람 손으로 옮겨 가고, 돈 글자가 떠오른다.</summary>
    void GiveBox(float pct)
    {
        CarryingBox = false;
        if (heldBox != null) d.StartCoroutine(MoveBoxToHands());
        int money = d.rc != null ? d.rc.PayHandover(HandoverTier.Pay[TierIdx], pct) : 0;
        Money = money;
        string text = $"+{money:N0}원";
        d.ShowMoney(text, d.handover.HandsPoint);
        d.Mark($"상자 건넴 → {HandoverTier.Name[TierIdx]} 반응 '{HandoverTier.Reaction[TierIdx]}' 돈 {text}(×{HandoverTier.Pay[TierIdx]:F1}) 정산창={ResultPanel.ShowCount}회");
    }

    IEnumerator MoveBoxToHands()
    {
        // 카메라 자식에서 떼어 세상에서 받는 사람 손 앞으로(0.35초), 그 뒤 받는 사람 자식으로.
        var go = heldBox; heldBox = null; heldBoxGiven = go;       // UpdateHeldBox 가 더는 안 건드린다
        var t = go.transform;
        var h = d.handover;
        go.SetActive(true);
        t.SetParent(null, true);
        Vector3 p0 = t.position; Quaternion r0 = t.rotation;
        var rcp = h.recipient;
        for (float k = 0f; k < 0.35f; k += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float s = Mathf.SmoothStep(0f, 1f, k / 0.35f);
            t.SetPositionAndRotation(Vector3.Lerp(p0, h.HandsPoint, s), Quaternion.Slerp(r0, rcp.rotation, s));
            yield return null;
        }
        t.SetPositionAndRotation(h.HandsPoint, rcp.rotation);
        t.SetParent(rcp, true);
    }

    void EndHandover()
    {
        HandedOver = true;
        var job = d.drive.FirstJob;
        string metWho = job != null && !string.IsNullOrEmpty(job.recipientName) ? job.recipientName : d.handover.recipientName;
        DayLog.Record(metWho, job != null ? job.callerName : "", HandoverTier.Name[TierIdx], Money);
        d.Mark($"건네기 끝 — {HandoverTier.Name[TierIdx]} 돈 {Money} 정산창 {ResultPanel.ShowCount}회");
        d.StartCoroutine(CloseDoorLater());
        SignalBus.Raise(Signal.HandedOver);   // → Stage_Return: 노을 시작
    }

    IEnumerator CloseDoorLater()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        yield return d.handover.Close();
        if (heldBoxGiven != null) heldBoxGiven.SetActive(false);
    }
}
