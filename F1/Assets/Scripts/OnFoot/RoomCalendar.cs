using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 과제 20261102-1951(DELIVERY-LOOP §5-5): 방 벽 달력 실물 — 세상 속 물건. 대역 판 + 도트 숫자 28칸(방학 28일).
/// 하루를 마친 날은 칸이 지워지고(X), 오늘 칸은 노란 판 + 잉크 테. 맨 아래 칸은 「축제까지 n일」 자리 — 글은 도현이 정한다(지금은 "(축제 자리)").
/// [E] 로 보면 OnFootDirector 가 카메라를 ViewPose 로 옮긴다(DepotOrderBoard 와 같은 방식).
/// 달력 면 = transform.forward 가 방 안쪽. 설치: RoomDaySetup. 아트(진짜 달력 그림)는 나중에 Sheet 자리.
/// </summary>
public class RoomCalendar : MonoBehaviour
{
    [Tooltip("숫자 글꼴(Galmuri9, 10px). 비면 DotScreenUI 의 작은 글꼴.")]
    [SerializeField] Font font;
    [Tooltip("제목 글꼴(Galmuri11, 12px). 비면 DotScreenUI 본문 글꼴.")]
    [SerializeField] Font titleFont;
    [Tooltip("달력 판 크기(m). 벽걸이 실물 비례(가로 46 × 세로 58 cm).")]
    [SerializeField] Vector2 size = new Vector2(0.46f, 0.58f);
    [Tooltip("보기 시점에서 달력 높이가 화면 세로의 이만큼.")]
    [SerializeField, Range(0.4f, 0.95f)] float viewFill = 0.8f;

    const float UnitsPerMeter = 300f;            // 캔버스 1 단위 ≈ 3.3 mm (두 자리 날짜가 칸에 들어가는 최소)
    const int Cols = 7, Rows = 4;

    Canvas canvas;
    Text titleText, hintText, festText;
    readonly Image[] cellBg = new Image[DayLog.TotalDays];
    readonly Text[] cellNum = new Text[DayLog.TotalDays];
    readonly Image[] crossA = new Image[DayLog.TotalDays];
    readonly Image[] crossB = new Image[DayLog.TotalDays];
    readonly Outline[] cellOutline = new Outline[DayLog.TotalDays];
    static readonly Color Past = new Color(0.55f, 0.52f, 0.46f, 1f);

    /// <summary>QA·캡처가 읽는 것: 지금 달력 상태 한 줄(오늘 · 지운 날 수 · 축제 칸 글).</summary>
    public string Written { get; private set; } = "";
    /// <summary>지금 지워진 칸 수(= 오늘 날짜 − 1).</summary>
    public int ErasedCount { get; private set; }
    public int TodayCell { get; private set; }
    public Vector3 Center => transform.position;
    /// <summary>"E 달력 보기" 칩 자리 — 달력 윗변 바로 위(칩이 숫자를 가리지 않게).</summary>
    public Vector3 PromptPoint => transform.position + Vector3.up * (size.y * 0.5f + 0.07f);

    void Awake() { Build(); Redraw(); }
    void OnEnable() { DayLog.Changed += Redraw; if (canvas != null) Redraw(); }
    void OnDisable() { DayLog.Changed -= Redraw; }

    public void SetHint(bool viewing) { if (hintText != null) hintText.gameObject.SetActive(viewing); }
    /// <summary>디오라마(과제 20261104-1559): 멀리서 볼 땐 28칸 글자판을 끄고 판(Frame·Sheet) 한 장만 남긴다. 다가가 볼 때 다시 켠다.</summary>
    public void SetDetail(bool on) { if (canvas != null && canvas.gameObject.activeSelf != on) canvas.gameObject.SetActive(on); }

    /// <summary>보기 시점: 달력 면 앞(법선 = transform.forward) 거리 d, 높이 = 달력 가운데. 세로 시야각에서 달력 높이가 viewFill.</summary>
    public void ViewPose(float vFovDeg, float aspect, out Vector3 pos, out Quaternion rot)
    {
        float t = Mathf.Tan(vFovDeg * 0.5f * Mathf.Deg2Rad);
        float dV = size.y / viewFill / (2f * t);
        float dH = size.x / 0.92f / (2f * t * Mathf.Max(0.5f, aspect));
        float d = Mathf.Max(dV, dH);
        pos = transform.position + transform.forward * d;
        rot = Quaternion.LookRotation(-transform.forward, Vector3.up);
    }

    void Build()
    {
        if (canvas != null) return;
        var ui = ScreenFonts.Find();
        if (font == null && ui != null) font = ui.SmallFont;
        if (titleFont == null && ui != null) titleFont = ui.BodyFont;
        if (titleFont == null) titleFont = font;

        // 대역 판: 잉크 테 + 종이색 면 (밋밋한 단색 — 종이결 없음, D-242). 면 = 로컬 z 0, 뒤(-z)로 두께.
        Block("Frame", new Vector3(size.x + 0.02f, size.y + 0.02f, 0.012f), new Vector3(0f, 0f, -0.007f), UIPalette.Ink);
        Block("Sheet", new Vector3(size.x, size.y, 0.012f), new Vector3(0f, 0f, -0.006f), UIPalette.Paper);

        var go = new GameObject("CalendarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.forward * 0.004f;
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);      // 캔버스 앞면 = -z → 달력 앞(+forward)에서 읽힌다
        float s = 1f / UnitsPerMeter;
        go.transform.localScale = new Vector3(s, s, s);
        canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 1f;       // 글자를 fontSize 픽셀 그대로 굽는다(Galmuri)
        float w = size.x * UnitsPerMeter, h = size.y * UnitsPerMeter;     // 138 × 174
        ((RectTransform)go.transform).sizeDelta = new Vector2(w, h);

        titleText = Label("Title", titleFont, 12, UIPalette.Ink, TextAnchor.MiddleCenter, new Vector2(0f, h * 0.5f - 11f), new Vector2(w - 8f, 14f));
        titleText.text = "방학";
        float cw = (w - 8f) / Cols, ch = (h - 64f) / Rows, gx0 = -w * 0.5f + 4f, gy0 = h * 0.5f - 22f;     // 격자 왼쪽 위(칸 높이 = 제목·축제 칸·안내 뺀 나머지)
        float diag = Mathf.Sqrt(cw * cw + ch * ch) * 0.9f, ang = Mathf.Atan2(ch, cw) * Mathf.Rad2Deg;
        for (int d = 0; d < DayLog.TotalDays; d++)
        {
            int c = d % Cols, r = d / Cols;
            var center = new Vector2(gx0 + cw * (c + 0.5f), gy0 - ch * (r + 0.5f));
            cellBg[d] = Rect("Cell" + (d + 1), center, new Vector2(cw - 2f, ch - 2f), UIPalette.PaperDeep);
            cellOutline[d] = cellBg[d].gameObject.AddComponent<Outline>();
            cellOutline[d].effectColor = UIPalette.Ink; cellOutline[d].effectDistance = new Vector2(1f, 1f); cellOutline[d].enabled = false;
            cellNum[d] = Label("Num" + (d + 1), font, 9, UIPalette.Ink, TextAnchor.MiddleCenter, center, new Vector2(cw, ch));
            cellNum[d].text = (d + 1).ToString();
            crossA[d] = Bar("XA" + (d + 1), center, diag, 1.3f, ang, UIPalette.Signal);
            crossB[d] = Bar("XB" + (d + 1), center, diag, 1.3f, -ang, UIPalette.Signal);
        }

        // 축제까지 n일 칸 — 비워 둔다(글은 도현이).
        float fy = gy0 - ch * Rows - 4f;
        Rect("FestFrame", new Vector2(0f, fy - 12f), new Vector2(w - 8f, 24f), UIPalette.Ink);
        Rect("FestBg", new Vector2(0f, fy - 12f), new Vector2(w - 10f, 22f), UIPalette.PaperDeep);
        festText = Label("Fest", font, 10, UIPalette.InkMuted, TextAnchor.MiddleCenter, new Vector2(0f, fy - 12f), new Vector2(w - 10f, 22f));
        festText.text = "(축제 자리)";

        hintText = Label("Hint", font, 10, UIPalette.InkMuted, TextAnchor.MiddleCenter, new Vector2(0f, -h * 0.5f + 6f), new Vector2(w - 8f, 12f));
        hintText.text = "E / Esc 닫기";
        hintText.gameObject.SetActive(false);
    }

    void Block(string n, Vector3 scale, Vector3 localPos, Color c)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n;
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(transform, false);
        g.transform.localPosition = localPos; g.transform.localScale = scale;
        var mr = g.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c);
        mr.SetPropertyBlock(mpb);
    }

    Image Rect(string n, Vector2 pos, Vector2 sz, Color c)
    {
        var g = new GameObject(n, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(canvas.transform, false);
        var r = (RectTransform)g.transform; r.anchoredPosition = pos; r.sizeDelta = sz;
        var im = g.GetComponent<Image>(); im.color = c; im.raycastTarget = false;
        return im;
    }

    Image Bar(string n, Vector2 pos, float len, float thick, float deg, Color c)
    {
        var im = Rect(n, pos, new Vector2(len, thick), c);
        im.rectTransform.localRotation = Quaternion.Euler(0f, 0f, deg);
        return im;
    }

    Text Label(string n, Font f, int px, Color c, TextAnchor a, Vector2 pos, Vector2 sz)
    {
        var g = new GameObject(n, typeof(RectTransform));
        g.transform.SetParent(canvas.transform, false);
        var r = (RectTransform)g.transform; r.anchoredPosition = pos; r.sizeDelta = sz;
        var t = g.AddComponent<Text>();
        t.font = f; t.fontSize = px; t.color = c; t.alignment = a; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void Redraw()
    {
        if (canvas == null) return;
        int today = Mathf.Clamp(DayLog.Day, 1, DayLog.TotalDays);
        int erased = today - 1;
        for (int d = 0; d < DayLog.TotalDays; d++)
        {
            bool past = d + 1 < today, isToday = d + 1 == today;
            cellBg[d].color = isToday ? UIPalette.Lemon : past ? Past : UIPalette.Paper;
            cellNum[d].color = isToday ? UIPalette.Ink : past ? UIPalette.InkMuted : UIPalette.Ink;
            crossA[d].gameObject.SetActive(past); crossB[d].gameObject.SetActive(past);
            cellOutline[d].enabled = isToday;
        }
        ErasedCount = erased; TodayCell = today;
        Written = $"방학 오늘 {today}/{DayLog.TotalDays}일 · 지운 칸 {erased}(1~{Mathf.Max(0, erased)}) · 축제 칸 \"{festText.text}\"";
    }
}
