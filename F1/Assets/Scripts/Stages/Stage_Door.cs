using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    // ── 문 앞 건네기 상태(QA·봇·DotScreenUI 가 읽음) ──
    public DoorHandover Handover => handover;
    /// <summary>카트에서 상자를 내려 들고 있다(화면 아래 상자).</summary>
    public bool CarryingBox { get; private set; }
    /// <summary>문을 두드렸다 ~ 받는 사람이 나올 때까지(입력 막음).</summary>
    public bool Knocking { get; private set; }
    public bool HandedOver { get; private set; }
    /// <summary>건넬 때 화물 상태 3단(0 멀쩡 · 1 조금 상함 · 2 깨짐, -1 = 아직).</summary>
    public int HandoverTierIdx { get; private set; } = -1;
    public int HandoverMoney { get; private set; }
    /// <summary>떠오르는 돈 글자("" = 없음) · 시작 시각(unscaled) · 사라지는 시각 · 세상 자리(받는 사람 손).
    /// 최소 moneySec 초, 반응 말풍선이 떠 있는 동안은 계속 + 대화 끝난 뒤 1.2초.</summary>
    public string MoneyText => moneyText != null && Time.unscaledTime < MoneyEnd ? moneyText : "";
    public float MoneyAt { get; private set; } = -99f;
    public float MoneyEnd { get; private set; } = -99f;
    public float MoneySec => moneySec;
    public Vector3 MoneyAnchor { get; private set; }
    string moneyText;
    GameObject heldBox;

    /// <summary>카트 짐칸에서 상자를 내려 든다 — 짐칸 상자는 숨기고 화면 아래 상자(대역)를 켠다.</summary>
    void BeginCarry()
    {
        CarryingBox = true;
        BedBox = false;
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        if (cargo != null && car != null && cargo.transform != car.transform)
            foreach (var r in cargo.GetComponentsInChildren<Renderer>()) r.enabled = false;
        Mark($"상자 내림(들고 걷기) 화물={(cargo != null ? (cargo.IntegrityPercent * 100f).ToString("F0") + "%" : "?")}");
    }

    /// <summary>걷는 동안만 손에 든 상자를 보인다(카메라는 주행과 같이 쓰므로 카트·대화 시점에선 끈다).</summary>
    void UpdateHeldBox()
    {
        if (heldBox == null) return;
        bool on = CarryingBox && State == Mode.OnFoot && footCam != null && footCam.enabled;
        if (heldBox.activeSelf != on) heldBox.SetActive(on);
    }

    /// <summary>C-003: 보관소 흐름에서 카트 짐칸 상자(카트 모델의 delivery_box·끈)는 실었을 때만 보인다 — 빈 카트로 와서, 실으면 생기고, 내려 들면 사라진다.</summary>
    public bool BedBox { get; private set; }
    Renderer[] bedRs; int bedShown = -1;
    void UpdateBedBox()
    {
        if (!DepotFlow || car == null) return;
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

    void BuildHeldBox()
    {
        // 회색 대역 상자: 카메라 자식, 화면 아래 가운데 조금 오른쪽. 아트는 나중에 이 자리(HeldBox)에 모델로.
        heldBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        heldBox.name = "HeldBox";
        Destroy(heldBox.GetComponent<Collider>());
        heldBox.transform.SetParent(cam.transform, false);
        heldBox.transform.localPosition = new Vector3(0.06f, -0.34f, 0.62f);
        heldBox.transform.localRotation = Quaternion.Euler(14f, -4f, 0f);
        heldBox.transform.localScale = new Vector3(0.42f, 0.24f, 0.32f);
        var mr = heldBox.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", new Color(0.58f, 0.57f, 0.55f)); mpb.SetColor("_Color", new Color(0.58f, 0.57f, 0.55f));
        mr.SetPropertyBlock(mpb);
        heldBox.SetActive(false);
    }

    void Knock()
    {
        Knocking = true;
        player.enabled = false;
        SetPrompt(""); PromptAnchor = null;
        var cargo = Services.Cargo as CargoBox ?? FindFirstObjectByType<CargoBox>();
        float pct = cargo != null ? cargo.IntegrityPercent : 1f;
        HandoverTierIdx = HandoverTier.Of(pct);
        handover.PlayKnock();
        Mark($"똑똑 — 문 {handover.name} 화물 {pct * 100f:F0}% → {HandoverTier.Name[HandoverTierIdx]}");
        StartCoroutine(KnockSeq(pct));
    }

    IEnumerator KnockSeq(float pct)
    {
        yield return new WaitForSecondsRealtime(knockWaitSec);
        yield return handover.Open(player.transform.position);
        Knocking = false;
        string who = handover.recipientName;
        var lines = new[] { (who, HandoverTier.Greeting), (who, HandoverTier.Reaction[HandoverTierIdx]) };
        Mark($"{who} 나옴 {handover.recipient.position.ToString("F1")}");
        BeginTalk(lines, handover.recipient, line => { if (line == 1) GiveBox(pct); }, EndHandover);
    }

    /// <summary>둘째 줄(반응)로 넘어갈 때 상자를 건넨다 — 상자가 받는 사람 손으로 옮겨 가고, 돈 글자가 떠오른다.</summary>
    void GiveBox(float pct)
    {
        CarryingBox = false;
        if (heldBox != null) StartCoroutine(MoveBoxToHands());
        int money = rc != null ? rc.PayHandover(HandoverTier.Pay[HandoverTierIdx], pct) : 0;
        HandoverMoney = money;
        moneyText = $"+{money:N0}원";
        MoneyAt = Time.unscaledTime;
        MoneyEnd = MoneyAt + moneySec;
        MoneyAnchor = handover.HandsPoint;
        Mark($"상자 건넴 → {HandoverTier.Name[HandoverTierIdx]} 반응 '{HandoverTier.Reaction[HandoverTierIdx]}' 돈 {moneyText}(×{HandoverTier.Pay[HandoverTierIdx]:F1}) 정산창={ResultPanel.ShowCount}회");
    }

    IEnumerator MoveBoxToHands()
    {
        // 카메라 자식에서 떼어 세상에서 받는 사람 손 앞으로(0.35초), 그 뒤 받는 사람 자식으로.
        var go = heldBox; heldBox = null; heldBoxGiven = go;       // UpdateHeldBox 가 더는 안 건드린다
        var t = go.transform;
        go.SetActive(true);
        t.SetParent(null, true);
        Vector3 p0 = t.position; Quaternion r0 = t.rotation;
        var rcp = handover.recipient;
        for (float k = 0f; k < 0.35f; k += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float s = Mathf.SmoothStep(0f, 1f, k / 0.35f);
            t.SetPositionAndRotation(Vector3.Lerp(p0, handover.HandsPoint, s), Quaternion.Slerp(r0, rcp.rotation, s));
            yield return null;
        }
        t.SetPositionAndRotation(handover.HandsPoint, rcp.rotation);
        t.SetParent(rcp, true);
    }
    GameObject heldBoxGiven;

    void EndHandover()
    {
        HandedOver = true;
        string metWho = firstJob != null && !string.IsNullOrEmpty(firstJob.recipientName) ? firstJob.recipientName : handover.recipientName;
        DayLog.Record(metWho, firstJob != null ? firstJob.callerName : "", HandoverTier.Name[HandoverTierIdx], HandoverMoney);
        Mark($"건네기 끝 — {HandoverTier.Name[HandoverTierIdx]} 돈 {HandoverMoney} 정산창 {ResultPanel.ShowCount}회");
        StartCoroutine(CloseDoorLater());
        HandedAt = Time.unscaledTime;
        if (duskRampSec > 0f && duskRamp == null) duskRamp = StartCoroutine(DuskRamp());
    }

    IEnumerator CloseDoorLater()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        yield return handover.Close();
        if (heldBoxGiven != null) heldBoxGiven.SetActive(false);
    }
}
