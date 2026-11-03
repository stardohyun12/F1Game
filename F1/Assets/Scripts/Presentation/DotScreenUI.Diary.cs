using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 과제 20261102-1951(DELIVERY-LOOP §5-5): 하루 끝 일기장 풀스크린 — UI 1판 그대로(종이 크림 + 잉크 테 + Galmuri 도트 글꼴, 종이결 없음 D-242).
/// 오늘 날짜(방학 N일째) · 만난 사람 한 줄씩(대역 "(일기 자리 — 이름)") · 오늘 번 돈 합계. 닫으면 다음 날 아침(MorningUI: 종이색이 0.7초 걷힘).
/// 일기 실제 글은 도현이 정한다 — 지금은 대역.
/// </summary>
public partial class DotScreenUI
{
    const int DiaryMaxVisits = 4;
    GameObject diaryRoot;
    Image diaryDesk, diaryPage, diaryRule, diaryTotalBox;
    Text diaryTitle, diaryDayNo, diaryLabel, diaryTotal, diaryHint, diaryEmpty;
    readonly Text[] diaryWho = new Text[DiaryMaxVisits], diaryMeta = new Text[DiaryMaxVisits];

    /// <summary>QA·캡처가 읽는 것: 일기장에 지금 적혀 있는 글 전체.</summary>
    public string DiaryShown { get; private set; } = "";
    /// <summary>일기 페이지 픽셀 사각형(RT 좌표, 왼쪽 아래 기준) — 겹침 검사용.</summary>
    public RectInt DiaryPageRect { get; private set; }

    void BuildDiary()
    {
        diaryRoot = new GameObject("Diary", typeof(RectTransform));
        var rr = (RectTransform)diaryRoot.transform;
        rr.SetParent(root, false);
        rr.anchorMin = rr.anchorMax = rr.pivot = Vector2.zero; rr.sizeDelta = Vector2.zero;
        diaryDesk = Img("Desk", null, UIPalette.PaperDeep, 10, 10, false, rr);
        diaryPage = Img("Page", boxSprite != null ? boxSprite : MakeBox(Paper, Ink), Color.white, 10, 10, true, rr);
        diaryRule = Img("Rule", null, Ink, 10, 1, false, rr);
        diaryTotalBox = Img("TotalBox", MakeBox(UIPalette.Lemon, Ink), Color.white, 10, 10, true, rr);
        diaryTitle = Txt("Title", rr, font, fontSize, Ink, TextAnchor.UpperLeft);
        diaryDayNo = Txt("DayNo", rr, smallFont, smallSize, UIPalette.InkMuted, TextAnchor.UpperRight);
        diaryLabel = Txt("Label", rr, smallFont, smallSize, UIPalette.InkMuted, TextAnchor.UpperLeft);
        diaryEmpty = Txt("Empty", rr, font, fontSize, UIPalette.InkMuted, TextAnchor.UpperLeft);
        for (int i = 0; i < DiaryMaxVisits; i++)
        {
            diaryWho[i] = Txt("Who" + i, rr, font, fontSize, Ink, TextAnchor.UpperLeft);
            diaryMeta[i] = Txt("Meta" + i, rr, smallFont, smallSize, UIPalette.InkMuted, TextAnchor.UpperLeft);
        }
        diaryTotal = Txt("Total", rr, font, fontSize, Ink, TextAnchor.MiddleCenter);
        diaryHint = Txt("Hint", rr, smallFont, smallSize, UIPalette.InkMuted, TextAnchor.LowerRight);
        diaryRoot.SetActive(false);
    }

    static void PlaceG(Graphic g, int x, int y, int w, int h)
    {
        g.gameObject.SetActive(true);
        g.rectTransform.anchoredPosition = new Vector2(x, y);
        g.rectTransform.sizeDelta = new Vector2(w, h);
    }

    /// <summary>글자를 (x, 위쪽 y) 에 놓는다 — 한 줄 오버플로.</summary>
    static void PlaceT(Text t, string s, int x, int yTop, int w, int h)
    {
        t.gameObject.SetActive(true);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.text = s;
        t.rectTransform.sizeDelta = new Vector2(w, h);
        t.rectTransform.anchoredPosition = new Vector2(x, yTop - h);
    }

    void DiaryUI(System.Text.StringBuilder vis)
    {
        if (diaryRoot == null) return;
        foreach (Transform c in diaryRoot.transform) c.gameObject.SetActive(false);
        diaryRoot.SetActive(true);
        diaryDesk.color = UIPalette.PaperDeep;
        int pw = Mathf.Min(W - 40, 540), ph = H - 28;
        int px = (W - pw) / 2, py = (H - ph) / 2;
        DiaryPageRect = new RectInt(px, py, pw, ph);
        PlaceG(diaryDesk, 0, 0, W, H);
        PlaceG(diaryPage, px, py, pw, ph);
        int lx = px + 18, rx = px + pw - 18, top = py + ph - 14, inner = rx - lx;
        var sb = new System.Text.StringBuilder();

        string title = $"방학 {DayLog.Day}일째";
        PlaceT(diaryTitle, title, lx, top, inner, 14);
        PlaceT(diaryDayNo, $"{DayLog.Day} / {DayLog.TotalDays}", lx, top - 2, inner, 12);
        PlaceG(diaryRule, lx, top - 20, inner, 1);
        sb.Append(title);

        PlaceT(diaryLabel, "오늘 만난 사람", lx, top - 28, inner, 12);
        int y = top - 44;
        int n = Mathf.Min(DayLog.Visits, DiaryMaxVisits);
        for (int i = 0; i < n; i++)
        {
            var v = DayLog.Today[i];
            string line = $"{v.who}: (일기 자리 — {v.who})";                       // 대역 — 실제 글은 도현
            string meta = $"{v.from} · 화물 {v.tier} · +{v.money:N0}원";
            PlaceT(diaryWho[i], line, lx + 4, y, inner - 4, 14);
            PlaceT(diaryMeta[i], meta, lx + 4, y - 15, inner - 4, 12);
            sb.Append('\n').Append(line).Append('\n').Append(meta);
            y -= 36;
        }
        if (n == 0)
        {
            PlaceT(diaryEmpty, "(오늘은 아무도 못 만났다)", lx + 4, y, inner - 4, 14);
            sb.Append("\n(오늘은 아무도 못 만났다)");
        }

        string total = $"오늘 번 돈  +{DayLog.TodayMoney:N0}원";
        int tw = Mathf.CeilToInt(Pref(diaryTotal, total, 300)) + 24, tx = lx, ty = py + 30;
        PlaceG(diaryTotalBox, tx, ty, tw, 20);
        PlaceT(diaryTotal, total, tx, ty + 20, tw, 20);
        diaryTotal.rectTransform.anchoredPosition = new Vector2(tx, ty);
        sb.Append('\n').Append(total);

        bool canClose = DayView.Present && DayView.DiaryCanClose;
        if (canClose) PlaceT(diaryHint, "E 덮기 — 다음 날 아침", lx, py + 22, inner, 12);
        diaryHint.rectTransform.anchoredPosition = new Vector2(lx, py + 10);

        DiaryShown = sb.ToString();
        vis.Append("diary ");
    }

    /// <summary>일기를 덮은 직후: 종이색이 0.7초 동안 걷힌다(다음 날 아침).</summary>
    void MorningUI(System.Text.StringBuilder vis)
    {
        if (diaryRoot == null || !DayView.Present) return;
        float age = Time.unscaledTime - DayView.MorningAt;
        if (age < 0f || age > MorningSec) return;
        foreach (Transform c in diaryRoot.transform) c.gameObject.SetActive(false);
        diaryRoot.SetActive(true);
        var p = UIPalette.PaperDeep;
        diaryDesk.color = new Color(p.r, p.g, p.b, 1f - Mathf.SmoothStep(0f, 1f, age / MorningSec));
        PlaceG(diaryDesk, 0, 0, W, H);
        vis.Append("morning ");
    }

    const float MorningSec = 0.7f;
}
