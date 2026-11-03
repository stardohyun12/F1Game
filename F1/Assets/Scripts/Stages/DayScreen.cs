using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: OnFootDirector 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class OnFootDirector
{
    // ── UI 1판(D-240): DotScreenUI 가 읽는 것 ──
    /// <summary>지금 안내([E] 칩)가 붙을 세상 점 — 문짝 가운데·카트 손잡이·사장 머리·낚시 자리. 없으면 null.</summary>
    public Vector3? PromptAnchor { get; internal set; }
    /// <summary>[E] 칩을 물건 곁이 아니라 화면 아래쪽 자리에 둔다 — 칠판 글씨·앞 나무를 가리지 않게(20261102-2305 ②). 칠판·앉기.</summary>
    public bool PromptLow { get; private set; }
    // ── C-003 첫 배달 안내 글(그레이박스 대역 문구 — 도현 확인 필요) ──
    // 오프닝 안내(OpeningSequence)가 끝난 뒤 같은 자리·같은 모양으로 단계마다 "다음 할 일" 한 줄. 카트 안 = 화면 아래(위는 화살·거리·깃발 자리).
    /// <summary>QA: 지금 보이는 배달 안내 글("" = 숨김).</summary>
    public string DeliveryCaption { get; private set; } = "";
    Image dcapBox; Text dcapText; RectTransform dcapRoot;

    // ── 떠오르는 돈 글자(문 앞 건네기 +, 정비 −) — Stage_Door·정비가 ShowMoney 로 띄우고 DotScreenUI 가 그린다 ──
    /// <summary>떠오르는 돈 글자("" = 없음) · 시작 시각(unscaled) · 사라지는 시각 · 세상 자리(받는 사람 손·카트 위).
    /// 최소 moneySec 초, 반응 말풍선이 떠 있는 동안은 계속 + 대화 끝난 뒤 1.2초.</summary>
    public string MoneyText => moneyText != null && Time.unscaledTime < MoneyEnd ? moneyText : "";
    public float MoneyAt { get; private set; } = -99f;
    public float MoneyEnd { get; private set; } = -99f;
    public float MoneySec => moneySec;
    public Vector3 MoneyAnchor { get; private set; }
    string moneyText;

    internal void ShowMoney(string text, Vector3 at)
    {
        moneyText = text;
        MoneyAt = Time.unscaledTime;
        MoneyEnd = MoneyAt + moneySec;
        MoneyAnchor = at;
    }

    internal void ClearMoney() { moneyText = null; MoneyAt = -99f; MoneyEnd = -99f; }

    string DeliveryCaptionNow()
    {
        if (!DepotFlow || !OrderPicked || InTalk || Knocking) return "";
        if (State != Mode.OnFoot && State != Mode.InCart) return "";
        var job = drive.FirstJob != null ? drive.FirstJob : depot.PickedJob;
        string place = !string.IsNullOrEmpty(job.callerName) ? job.callerName : firstDrop != null ? firstDrop.Label : "배달지";
        string who = string.IsNullOrEmpty(job.recipientName) ? "" : " " + job.recipientName;
        if (HandedOver) return State == Mode.OnFoot && Time.unscaledTime - HandedAt < handedCaptionSec ? $"첫 배달 끝 +{HandoverMoney:N0}원 — 집에 가서 자자" : "";   // 돌아오는 길 = 화면 0(SPEC §6-9)
        if (Delivered) return CarryingBox ? $"상자 들고{who}네 문 두드리기" : $"다 왔다 — 내려서{who}에게";
        if (OrderStarted)
        {
            if (State == Mode.OnFoot) return $"카트 타고 {place}{who}네로";
            Vector3 d = firstDrop.transform.position - car.transform.position; d.y = 0f;
            return d.magnitude <= arriveM ? $"다 왔다 — E 내려서{who}에게" : $"{place}{who}네 {DeliveryArrow(d)} {Mathf.RoundToInt(d.magnitude / (d.magnitude >= 100f ? 10 : 5)) * (d.magnitude >= 100f ? 10 : 5)}m";
        }
        if (CarryingBox) return "상자를 카트에 싣자";
        if (packSpot != null && packSpot.Ready) return "포장 끝 — 상자 들고 카트로";
        return $"작업대에서 포장 → {place}{who}";
    }

    static readonly string[] CapArrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };
    /// <summary>카메라 앞 기준 8방향 화살(OpeningSequence 안내와 같은 방식).</summary>
    string DeliveryArrow(Vector3 to)
    {
        Vector3 f = cam.transform.forward; f.y = 0f;
        float ang = f.sqrMagnitude > 1e-4f && to.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(f, to, Vector3.up) : 0f;
        return CapArrows[((Mathf.RoundToInt(ang / 45f) % 8) + 8) % 8];
    }

    // ── D-287 5단계: 화면 View(Presentation/DayView) 적기 — 감독(실행 순서 0)이 DotScreenUI(200)의 LateUpdate 보다 먼저 ──
    void WriteView()
    {
        DayView.Present = true; DayView.Active = true;
        DayView.State = (DayState.Mode)(int)State;
        DayView.Prompt = Prompt; DayView.PromptLow = PromptLow; DayView.PromptAnchor = PromptAnchor;
        bool talk = InTalk;
        DayView.InTalk = talk;
        DayView.TalkWho = TalkWho; DayView.TalkText = TalkText; DayView.TalkLineAt = TalkLineAt;
        DayView.SpeakerTop = talk ? SpeakerTop : Vector3.zero;
        DayView.Held = Held; DayView.PickedJob = PickedJob; DayView.OrderStarted = OrderStarted;
        DayView.FirstDrop = firstDrop; DayView.WalkDest = WalkDest;
        DayView.Player = player != null ? player.transform : null;
        DayView.Car = car != null ? car.transform : null;
        bool mark = CartMark;
        DayView.CartMark = mark; DayView.CartMarkAnchor = mark ? CartMarkAnchor : Vector3.zero;
        DayView.MoneyText = MoneyText; DayView.MoneyAt = MoneyAt; DayView.MoneyEnd = MoneyEnd; DayView.MoneySec = MoneySec; DayView.MoneyAnchor = MoneyAnchor;
        DayView.DiaryCanClose = DiaryCanClose; DayView.MorningAt = MorningAt;
    }

    void LateUpdate()
    {
        WriteView();
        if (State == Mode.Starting) return;
        string s = DeliveryCaptionNow();
        DeliveryCaption = s;
        if (!EnsureDeliveryCaption()) return;
        bool on = s.Length > 0;
        if (dcapBox.gameObject.activeSelf != on) dcapBox.gameObject.SetActive(on);
        if (!on) return;
        if (dcapText.text != s) dcapText.text = s;
        // Galmuri11 12px: 한글·화살표 12, 나머지 6 픽셀로 어림(OpeningSequence 와 같음)
        int w = 8;
        foreach (char c in s) w += (c >= 0xAC00 && c <= 0xD7A3) || (c >= 0x2190 && c <= 0x21FF) ? 12 : 6;
        Vector2 sz = dcapRoot.sizeDelta;
        var r = dcapBox.rectTransform;
        r.sizeDelta = new Vector2(w, 15);
        r.anchoredPosition = new Vector2(Mathf.Round((sz.x - w) * 0.5f), State == Mode.InCart ? 20f : Mathf.Round(sz.y - 44f));
        r.SetAsLastSibling();
    }

    bool EnsureDeliveryCaption()
    {
        if (dcapText != null) return true;
        var canvas = cam != null ? cam.transform.Find("DotScreenUI_Canvas") : null;
        if (dotUI == null || canvas == null) return false;
        dcapRoot = (RectTransform)canvas;
        var go = new GameObject("DeliveryCaption", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(dcapRoot, false);
        dcapBox = go.GetComponent<Image>();
        dcapBox.color = UIPalette.Ink; dcapBox.raycastTarget = false;
        var r = dcapBox.rectTransform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        var tg = new GameObject("Text", typeof(RectTransform));
        tg.transform.SetParent(go.transform, false);
        var tr = (RectTransform)tg.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(0, 1); tr.offsetMax = Vector2.zero;
        dcapText = tg.AddComponent<Text>();
        dcapText.font = dotUI.BodyFont; dcapText.fontSize = 12; dcapText.color = UIPalette.Paper;
        dcapText.alignment = TextAnchor.MiddleCenter; dcapText.raycastTarget = false;
        dcapText.horizontalOverflow = HorizontalWrapMode.Overflow; dcapText.verticalOverflow = VerticalWrapMode.Overflow;
        tg.AddComponent<PixelSnapText>().root = dcapRoot;
        go.SetActive(false);
        return true;
    }

    // ── 화면 ──
    void CollectHud()
    {
        foreach (var m in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (m == null) continue;
            string n = m.GetType().Name;
            if (!(n.EndsWith("HUD") || n == "CargoWarningHUD")) continue;
            var c = m.GetComponentInParent<Canvas>(true);
            if (c != null) { c = c.rootCanvas; if (!hud.Contains(c)) hud.Add(c); }
        }
    }

    internal void SetHud(bool on) { foreach (var c in hud) if (c != null) c.enabled = on; }

    void BuildUI()
    {
        var anyTmp = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        var font = anyTmp != null && anyTmp.font != null ? anyTmp.font : TMP_Settings.defaultFontAsset;
        var go = new GameObject("OnFootUI", typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        var ui = go.GetComponent<Canvas>(); ui.renderMode = RenderMode.ScreenSpaceOverlay; ui.sortingOrder = 90;
        overlayUI = ui;
        var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        // 한 줄 안내: 반투명 띠 + 글(외곽선은 폰트 재질을 건드리므로 안 씀). 아트는 나중에 띠 스프라이트 슬롯에.
        var bg = new GameObject("Prompt", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var rt = (RectTransform)bg.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 150f); rt.sizeDelta = new Vector2(320f, 64f);
        var img = bg.GetComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0.45f); img.raycastTarget = false;
        var tgo = new GameObject("Text", typeof(RectTransform));
        tgo.transform.SetParent(bg.transform, false);
        var trt = (RectTransform)tgo.transform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        promptText = tgo.AddComponent<TextMeshProUGUI>();
        promptText.font = font; promptText.fontSize = 38; promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white; promptText.raycastTarget = false;
        promptRoot = bg;
        bg.SetActive(false);
        // 조준점: 화면 가운데 작은 점(걷는 동안만). 아트는 나중에 이 Image 의 스프라이트 슬롯에.
        dotRoot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
        dotRoot.transform.SetParent(go.transform, false);
        var drt = (RectTransform)dotRoot.transform;
        drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = Vector2.zero; drt.sizeDelta = new Vector2(8f, 8f);
        var dimg = dotRoot.GetComponent<Image>(); dimg.color = new Color(1f, 1f, 1f, 0.85f); dimg.raycastTarget = false;
        dotRoot.SetActive(false);
        // 사장 대사 자막(기능 2): 화면 아래 넓은 반투명 띠. 글만 — 연출은 나중.
        subRoot = new GameObject("Subtitle", typeof(RectTransform), typeof(Image));
        subRoot.transform.SetParent(go.transform, false);
        var srt = (RectTransform)subRoot.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f); srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 90f); srt.sizeDelta = new Vector2(1200f, 96f);
        var simg = subRoot.GetComponent<Image>(); simg.color = new Color(0f, 0f, 0f, 0.55f); simg.raycastTarget = false;
        var sgo = new GameObject("Text", typeof(RectTransform));
        sgo.transform.SetParent(subRoot.transform, false);
        var strt = (RectTransform)sgo.transform; strt.anchorMin = Vector2.zero; strt.anchorMax = Vector2.one; strt.offsetMin = new Vector2(32f, 0f); strt.offsetMax = new Vector2(-32f, 0f);
        subText = sgo.AddComponent<TextMeshProUGUI>();
        subText.font = font; subText.fontSize = 40; subText.alignment = TextAlignmentOptions.Center;
        subText.color = Color.white; subText.raycastTarget = false; subText.richText = true;
        subRoot.SetActive(false);
    }

    /// <summary>걷기 = 안내를 조준점 바로 아래에(보는 곳 옆에서 읽게) · 카트 = 화면 아래 띠.</summary>
    internal void PlacePrompt(bool nearDot)
    {
        if (promptRoot == null) return;
        var rt = (RectTransform)promptRoot.transform;
        if (nearDot) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0f, -40f); rt.sizeDelta = new Vector2(220f, 52f); promptText.fontSize = 30; }
        else { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 150f); rt.sizeDelta = new Vector2(320f, 64f); promptText.fontSize = 38; }
    }
}
