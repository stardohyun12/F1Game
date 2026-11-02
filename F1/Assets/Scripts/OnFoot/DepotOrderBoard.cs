using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-250·251(과제 20261102-1515): 택배 보관소 안 주문 칠판 — 세상 속 물건. [E] 로 보면 OnFootDirector 가 카메라를 칠판 앞(ViewPose)으로 옮기고,
/// W/S 로 줄을 고르고 E 로 맡는다. 글씨는 Galmuri11 분필색, 칠판 면에 붙은 World Space 캔버스(1 단위 = 1/120 m → 보기 시점에서 2 화면 픽셀).
/// 칠판 판·틀은 회색 대역(DepotOrderSetup 이 만든다) — 아트 납품 아님. 줄 내용(주문 1 + 대역 2)은 OnFootDirector 가 넣는다.
/// </summary>
public class DepotOrderBoard : MonoBehaviour
{
    [Tooltip("분필 글씨(Galmuri11). 비면 DotScreenUI 본문 글꼴.")]
    [SerializeField] Font font;
    [Tooltip("칠판 면 크기(m) — 틀 안쪽.")]
    [SerializeField] Vector2 size = new Vector2(2.0f, 1.2f);
    [Tooltip("보기 시점: 칠판 높이가 화면 세로의 이만큼(0~1).")]
    [Range(0.4f, 0.95f)] [SerializeField] float viewFill = 0.8f;
    [SerializeField] Color chalk = new Color(0.93f, 0.92f, 0.84f);
    [SerializeField] Color chalkDim = new Color(0.93f, 0.92f, 0.84f, 0.45f);

    public const float UnitsPerMeter = 120f;

    public struct Line { public string text; public bool open; public Line(string t, bool o) { text = t; open = o; } }
    readonly List<Line> lines = new List<Line>();
    readonly List<Text> rows = new List<Text>();
    Text title, hint;
    Canvas canvas;

    public int Selected { get; private set; }
    public int Picked { get; private set; } = -1;
    public int Count => lines.Count;
    public bool IsOpenLine(int i) => i >= 0 && i < lines.Count && lines[i].open;
    /// <summary>QA·봇이 읽는 것 — 지금 칠판에 적힌 글 그대로(줄바꿈 구분).</summary>
    public string Written { get; private set; } = "";
    /// <summary>칠판 면 가운데(월드) — 시선 레이·[E] 칩 자리.</summary>
    public Vector3 Center => transform.position;

    public void SetLines(IList<Line> ls)
    {
        lines.Clear(); lines.AddRange(ls);
        Selected = 0; Picked = -1;
        Build();
        BuildNotes();
        Redraw();
    }

    // ── D-269: 핀 꽂힌 쪽지(오늘 주문 수만큼). HeldPaper.On 이면 분필 줄 대신 쪽지를 집는다. ──
    readonly List<HeldPaper> notes = new List<HeldPaper>();
    readonly List<int> noteLine = new List<int>();
    public bool NotesOn { get; private set; }
    /// <summary>아직 칠판에 꽂혀 있는 쪽지 수.</summary>
    public int NotesLeft { get { int n = 0; foreach (var p in notes) if (p != null && p.State == HeldPaper.Pose.Pinned) n++; return n; } }

    void BuildNotes()
    {
        for (int i = 0; i < notes.Count; i++)
            if (notes[i] != null && notes[i].State == HeldPaper.Pose.Pinned) Destroy(notes[i].gameObject);   // 손에 든 건 감독이 치운다
        notes.Clear(); noteLine.Clear();
        NotesOn = HeldPaper.On;
        foreach (var r in rows) r.gameObject.SetActive(!NotesOn);
        if (!NotesOn) return;
        var ui = FindFirstObjectByType<DotScreenUI>();
        Font small = ui != null ? ui.SmallFont : font;
        int k = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            if (!lines[i].open) continue;
            var n = HeldPaper.Create("OrderNote" + i, font, small);
            // 제목 아래 왼쪽부터 한 줄로, 조금씩 기울게(손으로 꽂은 것)
            n.PinTo(transform, new Vector3(-size.x * 0.5f + 0.32f + k * 0.2f, size.y * 0.5f - 0.32f, 0.006f));
            n.transform.localRotation *= Quaternion.Euler(0f, 0f, (k % 2 == 0 ? -4f : 3f));
            n.SetPreview(lines[i].text);
            notes.Add(n); noteLine.Add(i); k++;
        }
    }

    /// <summary>시선이 칠판 어디에 닿았을 때 그 점에서 가장 가까운, 아직 꽂힌 쪽지.</summary>
    public HeldPaper NearestNote(Vector3 point)
    {
        HeldPaper best = null; float bd = float.MaxValue;
        foreach (var p in notes)
        {
            if (p == null || p.State != HeldPaper.Pose.Pinned) continue;
            float d = (p.transform.position - point).sqrMagnitude;
            if (d < bd) { bd = d; best = p; }
        }
        return best;
    }

    /// <summary>쪽지를 집음 = 그 줄을 맡음(Picked·Written 은 예전과 같게 — QA 호환).</summary>
    public bool PickNote(HeldPaper n)
    {
        int i = notes.IndexOf(n);
        if (i < 0) return false;
        Selected = noteLine[i];
        return Pick();
    }

    /// <summary>[E] 칩 자리 — 쪽지 오른쪽 위 모서리 곁.</summary>
    public static Vector3 NoteChipAnchor(HeldPaper n) =>
        n.transform.TransformPoint(new Vector3(HeldPaper.WidthM * 0.5f + 0.01f, HeldPaper.HeightM * 0.35f, 0f));

    public void Move(int d)
    {
        if (lines.Count == 0) return;
        Selected = Mathf.Clamp(Selected + d, 0, lines.Count - 1);
        Redraw();
    }

    /// <summary>고른 줄을 맡았다고 칠판에 표시(✓). 대역 줄이면 false.</summary>
    public bool Pick()
    {
        if (!IsOpenLine(Selected) || Picked >= 0) return false;
        Picked = Selected;
        Redraw();
        return true;
    }

    /// <summary>칠판 글씨 캔버스(D-258 A안이 칠판 면에 인쇄할 때 읽는다).</summary>
    public Canvas ChalkCanvas => canvas;
    /// <summary>D-258 A/B: 키 안내 줄은 처음 볼 때 1회만(기본 false = 볼 때마다).</summary>
    public static bool HintOnce;
    static bool hintSeen;

    public void SetHint(bool viewing)
    {
        if (hint == null) return;
        if (NotesOn) viewing = false;
        if (viewing && HintOnce && hintSeen) viewing = false;
        if (viewing) hintSeen = true;
        hint.gameObject.SetActive(viewing);
    }

    /// <summary>
    /// 보기 시점: 칠판 면 앞(법선 = transform.forward) 거리 d, 높이 = 칠판 가운데. d 는 세로 시야각에서 칠판 높이가 viewFill 이 되게.
    /// 가로도 넘치면(좁은 화면) 가로 기준으로 더 물러난다.
    /// </summary>
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
        if (font == null) { var ui = FindFirstObjectByType<DotScreenUI>(); if (ui != null) font = ui.BodyFont; }
        var go = new GameObject("ChalkCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.forward * 0.004f;           // 면 바로 앞(겹침 깜박임 방지)
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);       // 캔버스 앞면 = -z → 칠판 앞(+forward)에서 읽힌다
        float s = 1f / UnitsPerMeter;
        go.transform.localScale = new Vector3(s, s, s);
        canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 1f;       // 글자를 fontSize 픽셀 그대로 굽는다(Galmuri 12px)
        var rt = (RectTransform)go.transform; rt.sizeDelta = size * UnitsPerMeter;
        float w = size.x * UnitsPerMeter, h = size.y * UnitsPerMeter;
        title = Row("Title", h * 0.5f - 10f, w);
        for (int i = 0; i < 3; i++) rows.Add(Row("Line" + i, h * 0.5f - 34f - i * 22f, w));
        hint = Row("Hint", -h * 0.5f + 16f, w);
        hint.color = chalkDim; hint.alignment = TextAnchor.MiddleCenter;
        hint.text = "W/S 고르기   E 맡기   Esc 닫기";
        title.alignment = TextAnchor.MiddleCenter;
        title.text = "오늘 주문";
        hint.gameObject.SetActive(false);
    }

    Text Row(string n, float y, float w)
    {
        var g = new GameObject(n, typeof(RectTransform));
        g.transform.SetParent(canvas.transform, false);
        var r = (RectTransform)g.transform; r.anchoredPosition = new Vector2(0f, y); r.sizeDelta = new Vector2(w - 16f, 18f);
        var t = g.AddComponent<Text>();
        t.font = font; t.fontSize = 12; t.color = chalk; t.alignment = TextAnchor.MiddleLeft; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void Redraw()
    {
        var sb = new System.Text.StringBuilder(title != null ? title.text : "");
        for (int i = 0; i < rows.Count; i++)
        {
            if (i >= lines.Count) { rows[i].text = ""; continue; }
            string mark = i == Picked ? "✓ " : i == Selected ? "▶ " : "   ";
            rows[i].text = mark + lines[i].text;
            rows[i].color = lines[i].open ? chalk : chalkDim;
            sb.Append('\n').Append(rows[i].text);
        }
        if (NotesOn) sb.Append("\n[쪽지 ").Append(NotesLeft).Append("장]");
        Written = sb.ToString();
    }
}
