using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// C-002 오프닝(그레이박스): 방에서 깨어남 → 집을 나섬 → 정비소 사장 대화(카트 열쇠) → 카트 탑승.
/// OnFootDirector 가 시작 때 같은 오브젝트에 붙이고 단계를 알려 준다. 이 컴포넌트는 단계 값과
/// 안내 글만 맡는다 — 대화·탑승·표식 그리기는 감독·DotScreenUI 그대로. 걷는 구간엔 방향 표시가 없다(도현 D-277 B):
/// 집을 나서는 순간 시선이 정비소 건물 쪽으로 돌아 건물이 화면에 들어온다.
/// QA: <c>Opening.Step</c> 이 Room → Front → Talk → Key → Cart 순으로 바뀐다.
/// </summary>
public class OpeningSequence : MonoBehaviour
{
    public enum Stage { None, Room, Front, Talk, Key, Cart }

    [Tooltip("깨어나서 침대를 내려다보는 시간(초) — 그 뒤 달력으로 고개를 돌린다.")]
    [SerializeField] float bedHoldSec = 0.9f;
    [Tooltip("침대에서 방 물건(달력)으로 시선이 옮겨 가는 시간(초).")]
    [SerializeField] float wakeSec = 1.4f;
    [Tooltip("카트에 탄 뒤 '택배 보관소로' 안내 글을 보이는 시간(초).")]
    [SerializeField] float cartCaptionSec = 6f;
    [Tooltip("집을 나설 때 시선이 정비소 건물 쪽으로 도는 시간(초). 0 = 안 돌림.")]
    [SerializeField] float frontTurnSec = 0.8f;
    [Tooltip("집을 나선 자리에서 이만큼(m, 수평) 걸어 나오면 시선을 돌린다 — 현관 처마 밑에선 집 벽이 정비소를 반쯤 가린다.")]
    [SerializeField] float frontWalkM = 2.8f;

    public static OpeningSequence Current { get; private set; }
    public Stage Step { get; private set; } = Stage.None;
    public bool HasKey { get; private set; }
    /// <summary>단계가 바뀐 기록(QA 한 줄).</summary>
    public string Trail { get; private set; } = "";

    OnFootDirector dir;
    Renderer garage;          // 정비소 건물(씬 GARAGE_shell) — 집 앞에서 시선을 돌릴 목표
    bool frontTurned;
    Vector3 frontAt;
    /// <summary>QA: 집 앞 시선 돌림이 일어났나.</summary>
    public bool FrontTurned => frontTurned;
    float spawnY, stepAt;
    // 그레이박스 안내 글(대역) — 도트 UI 캔버스 위쪽 가운데. 단계마다 어디로 가는지 한 줄.
    Image capBox;
    Text capText;
    RectTransform capRoot;
    /// <summary>QA: 지금 보이는 안내 글("" = 숨김).</summary>
    public string Caption { get; private set; } = "";

    void OnDestroy()
    {
        if (Current == this) Current = null;
        if (capBox != null) Destroy(capBox.gameObject);
    }

    /// <summary>새 게임 시작(감독 Start 끝). 방에서 눈을 뜬다.</summary>
    public void Begin(OnFootDirector d)
    {
        dir = d; Current = this;
        if (garage == null) { var g = GameObject.Find("GARAGE_shell"); if (g != null) garage = g.GetComponent<Renderer>(); }
        HasKey = false; Trail = ""; Step = Stage.None; frontTurned = false;
        spawnY = d.Player.transform.position.y;
        Set(Stage.Room);
        StopAllCoroutines();
        StartCoroutine(Wake());
    }

    public void OnTalkBegin() { if (Step == Stage.Front || Step == Stage.Room) Set(Stage.Talk); }
    public void OnTalkEnd() { if (Step == Stage.Talk) { HasKey = true; Set(Stage.Key); } }
    public void OnCartEnter() { if (Step == Stage.Key) Set(Stage.Cart); }

    void Update()
    {
        if (dir == null || dir.State != OnFootDirector.Mode.OnFoot) return;
        // 집을 나와 몇 걸음(처마 밖 마당) 걸으면 정비소 쪽으로 고개를 돌린다 — 표식 없이 건물이 화면 가운데.
        if (Step == Stage.Front && !frontTurned && !dir.InTalk && frontTurnSec > 0f && garage != null)
        {
            Vector3 d = dir.Player.transform.position - frontAt; d.y = 0f;
            if (d.magnitude >= frontWalkM) { frontTurned = true; StartCoroutine(LookToGarage(GarageAim)); }
        }
        if (Step != Stage.Room) return;
        // 집을 나섰다 = 2층에서 내려왔고 머리 위가 하늘(지붕·천장 없음).
        Vector3 p = dir.Player.transform.position;
        if (p.y < spawnY - 1.5f && !Physics.Raycast(p + Vector3.up * 1.9f, Vector3.up, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Set(Stage.Front);
            frontAt = p;
        }
    }

    /// <summary>시선 목표: 정비소 서쪽 아치(집 쪽에서 집들 사이로 보이는 면) — 건물 폭 30% 지점, 지붕 아래 1.5 m.</summary>
    Vector3 GarageAim
    {
        get { Bounds b = garage.bounds; return new Vector3(Mathf.Lerp(b.min.x, b.max.x, 0.3f), b.max.y - 1.5f, b.center.z); }
    }

    /// <summary>정비소가 보이는 자리에 섰다: 그쪽으로 고개를 돌린다 — 표식 없이 건물이 화면 가운데 들어온다. 그 뒤엔 마우스 그대로.</summary>
    IEnumerator LookToGarage(Vector3 aim)
    {
        var cam = dir.Player.Cam;
        if (cam == null) yield break;
        float y0 = cam.Yaw, p0 = cam.Pitch;
        cam.LookAt(aim);
        float y1 = cam.Yaw, p1 = cam.Pitch;
        cam.HoldLook = true;
        for (float t = 0f; t < frontTurnSec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            if (Step != Stage.Front || dir.State != OnFootDirector.Mode.OnFoot) break;
            float k = Mathf.SmoothStep(0f, 1f, t / frontTurnSec);
            cam.Yaw = Mathf.LerpAngle(y0, y1, k); cam.Pitch = Mathf.Lerp(p0, p1, k);
            yield return null;
        }
        if (Step == Stage.Front) { cam.Yaw = y1; cam.Pitch = p1; }
        cam.HoldLook = false;
        Debug.Log($"[Opening] 집 앞 → 정비소 쪽 시선 yaw {y1:F0}");
    }

    void LateUpdate()
    {
        string s = CaptionNow();
        Caption = s;
        if (!EnsureCaption()) return;
        bool on = s.Length > 0;
        if (capBox.gameObject.activeSelf != on) capBox.gameObject.SetActive(on);
        if (!on) return;
        if (capText.text != s) capText.text = s;
        // Galmuri11 12px: 한글·화살표 12, 나머지 6 픽셀로 어림(그레이박스)
        int w = 8;
        foreach (char c in s) w += (c >= 0xAC00 && c <= 0xD7A3) || (c >= 0x2190 && c <= 0x21FF) ? 12 : 6;
        Vector2 sz = capRoot.sizeDelta;
        var r = capBox.rectTransform;
        r.sizeDelta = new Vector2(w, 15);
        // 카트 안에선 화면 아래 — 위쪽은 화살·거리·목적지 깃발 자리(위에 두면 보관소 깃발 머리를 가렸다).
        bool low = dir.State == OnFootDirector.Mode.InCart;
        r.anchoredPosition = new Vector2(Mathf.Round((sz.x - w) * 0.5f), low ? 20f : Mathf.Round(sz.y - 44f));
        r.SetAsLastSibling();
    }

    /// <summary>단계별 안내(대역 문구 — 도현 확인 필요). 대화·달력·일기 화면에선 숨긴다.</summary>
    string CaptionNow()
    {
        if (dir == null || !dir.isActiveAndEnabled) return "";
        var st = dir.State;
        if (st == OnFootDirector.Mode.InCart)
            return Step == Stage.Cart && Time.unscaledTime - stepAt < cartCaptionSec ? "열쇠로 출발 · " + Toward(dir.WalkDest, "택배 보관소") : "";
        if (st != OnFootDirector.Mode.OnFoot || dir.InTalk) return "";
        switch (Step)
        {
            case Stage.Room: return "방학 1일째 아침 — 집을 나서자";
            case Stage.Front: return "정비소로 — 사장님이 카트를 봐 놨다";
            case Stage.Key: return "카트 열쇠를 받았다 — 카트에 타자";
            default: return "";
        }
    }

    static readonly string[] Arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };

    /// <summary>안내 글의 방향 한 토막: "정비소 ↗ 45m" — 화살은 지금 카메라 앞 기준 8방향(깃발이 화면 밖이어도 어디인지 글로 남는다).</summary>
    string Toward(DeliveryPoint dp, string fallback)
    {
        var cam = Camera.main;
        if (dp == null || cam == null) return fallback + "로";
        Vector3 me = dir.State == OnFootDirector.Mode.InCart && dir.Car != null ? dir.Car.transform.position : dir.Player.transform.position;
        Vector3 to = dp.transform.position - me; to.y = 0f;
        Vector3 f = cam.transform.forward; f.y = 0f;
        float ang = f.sqrMagnitude > 1e-4f && to.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(f, to, Vector3.up) : 0f;
        int k = ((Mathf.RoundToInt(ang / 45f) % 8) + 8) % 8;
        float m = to.magnitude;
        int unit = m >= 100f ? 10 : 5;
        string name = string.IsNullOrEmpty(dp.Label) ? fallback : dp.Label;
        return name + " " + Arrows[k] + " " + Mathf.RoundToInt(m / unit) * unit + "m";
    }

    bool EnsureCaption()
    {
        if (capText != null) return true;
        var cam = Camera.main;
        var ui = FindFirstObjectByType<DotScreenUI>();
        var canvas = cam != null ? cam.transform.Find("DotScreenUI_Canvas") : null;
        if (ui == null || canvas == null) return false;
        capRoot = (RectTransform)canvas;
        var go = new GameObject("OpeningCaption", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(capRoot, false);
        capBox = go.GetComponent<Image>();
        capBox.color = UIPalette.Ink; capBox.raycastTarget = false;
        var r = capBox.rectTransform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        var tg = new GameObject("Text", typeof(RectTransform));
        tg.transform.SetParent(go.transform, false);
        var tr = (RectTransform)tg.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(0, 1); tr.offsetMax = Vector2.zero;
        capText = tg.AddComponent<Text>();
        capText.font = ui.BodyFont; capText.fontSize = 12; capText.color = UIPalette.Paper;
        capText.alignment = TextAnchor.MiddleCenter; capText.raycastTarget = false;
        capText.horizontalOverflow = HorizontalWrapMode.Overflow; capText.verticalOverflow = VerticalWrapMode.Overflow;
        tg.AddComponent<PixelSnapText>().root = capRoot;
        go.SetActive(false);
        return true;
    }

    void Set(Stage s)
    {
        if (Step == s) return;
        Step = s; stepAt = Time.unscaledTime;
        Trail += (Trail.Length > 0 ? "→" : "") + s;
        Debug.Log($"[Opening] {s} ({Trail})");
    }

    /// <summary>눈을 뜬다: 방금 일어난 침대를 내려다보다가 방 달력 쪽으로 고개를 돌린다 — 침대 → 방 안 → 달력 [E] 순으로 보인다.</summary>
    IEnumerator Wake()
    {
        var cam = dir.Player.Cam;
        if (cam == null) yield break;
        cam.HoldLook = true;
        yield return null;                       // 순간이동한 몸·눈 자리가 한 프레임 뒤에 잡힌다 — 그 뒤에 방향을 잰다
        // 달력 방향은 눈이 수평일 때 먼저 잰다 — 침대를 내려다본 자세(눈 자리가 앞·아래로 옮겨 감)에서 재면 조준점이 달력 아래로 빠져 [E] 가 안 떴다.
        // 달력 판 가운데(충돌체). PromptPoint([E] 칩 자리)는 판 위라 시선 레이가 벽에 맞는다.
        if (dir.Calendar != null) cam.LookAt(dir.Calendar.transform.position);
        float y1 = cam.Yaw, p1 = cam.Pitch;
        // 침대 머리맡 쪽(침대 자리에서 0.3 m 서쪽) — 이불·베개가 화면 가운데.
        if (dir.Bed != null) cam.LookAt(dir.Bed.transform.position + new Vector3(-0.3f, 0f, 0f));
        float y0 = cam.Yaw, p0 = cam.Pitch;
        for (float t = 0f; t < bedHoldSec; t += Time.unscaledDeltaTime) { cam.Yaw = y0; cam.Pitch = p0; yield return null; }
        for (float t = 0f; t < wakeSec; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float k = Mathf.SmoothStep(0f, 1f, t / wakeSec);
            cam.Yaw = Mathf.LerpAngle(y0, y1, k); cam.Pitch = Mathf.Lerp(p0, p1, k);
            yield return null;
        }
        cam.Yaw = y1; cam.Pitch = p1;
        cam.HoldLook = false;
    }
}

/// <summary>QA 짧은 이름: <c>Opening.Step</c>.</summary>
public static class Opening
{
    public static OpeningSequence.Stage Step => OpeningSequence.Current != null ? OpeningSequence.Current.Step : OpeningSequence.Stage.None;
    public static string Trail => OpeningSequence.Current != null ? OpeningSequence.Current.Trail : "";
}
