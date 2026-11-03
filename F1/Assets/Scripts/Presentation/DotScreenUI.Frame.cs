using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: DotScreenUI 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class DotScreenUI
{
    // ── 매 프레임 ──
    void LateUpdate()
    {
        if (canvas == null) return;
        Fit();   // 걷기(near 0.05)·주행 카메라 near·FOV 가 다르다 — 항상 바로 앞, 화면 꽉
        HideAll();
        var vis = new System.Text.StringBuilder();
        var st = director != null && director.isActiveAndEnabled ? director.State : OnFootDirector.Mode.InCart;
        if (st != lastState) { if (st == OnFootDirector.Mode.InCart) cartAt = Time.unscaledTime; lastState = st; }

        if (st == OnFootDirector.Mode.InCart) { DriveUI(vis); if (hideLegacyHud) HideLegacy(); }
        else RestoreLegacy();

        if (director != null && st == OnFootDirector.Mode.OnFoot)
        {
            if (director.InTalk) TalkUI(vis);
            else
            {
                if (director.Held == null || director.Held.State != HeldPaper.Pose.Read) { Show(dot, Mathf.RoundToInt(W * 0.5f) - 1, Mathf.RoundToInt(H * 0.5f) - 1); vis.Append("dot "); }
                ChipUI(vis);
                CartMarkUI(vis);
                if (director.PickedJob != null && !director.OrderStarted)            // D-250: 칠판에서 맡은 주문 = 걷는 중에도 Tab 전표
                    SlipUI(director.PickedJob, director.FirstDrop, director.Player != null ? director.Player.transform.position : cam.transform.position, vis);
            }
        }
        else if (director != null && st == OnFootDirector.Mode.InCart && director.Prompt.Length > 0) ChipUI(vis);
        DestUI(DestTarget(st), vis);
        if (director != null && st == OnFootDirector.Mode.OnFoot) MoneyUI(vis);
        if (director != null && st == OnFootDirector.Mode.Diary) DiaryUI(vis); else MorningUI(vis);
        StyleLate();   // D-258 A/B 비교판(기본 Now = 아무것도 안 바꿈)
        Visible = vis.ToString().Trim();
    }

    /// <summary>D-250 ③: 상자를 건넨 순간부터(반응 말풍선이 떠 있는 동안 + 끝난 뒤 1.2초, 최소 MoneySec 초) — 받는 사람 손 오른쪽에서 위로 14px 떠오르고 마지막 0.5초에 옅어진다.</summary>
    void MoneyUI(System.Text.StringBuilder vis)
    {
        string s = director.MoneyText;
        if (string.IsNullOrEmpty(s)) { MoneyShown = ""; return; }
        if (moneyText.text != s)
        {
            moneyText.text = s;
            moneyBox.rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Pref(moneyText, s, 300)) + 8, 15);
        }
        float age = Time.unscaledTime - director.MoneyAt, dur = Mathf.Max(0.5f, director.MoneySec);
        var sz = moneyBox.rectTransform.sizeDelta;
        int x, y;
        if (ToScreen(director.MoneyAnchor, out var sp)) { x = Mathf.RoundToInt(sp.x) + 8; y = Mathf.RoundToInt(sp.y); }
        else { x = W / 2 + 20; y = H / 2 - 20; }
        y += Mathf.RoundToInt(14f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / (dur * 0.6f))));
        x = Mathf.Clamp(x, 2, W - (int)sz.x - 2); y = Mathf.Clamp(y, 2, H - (int)sz.y - 2);
        float a = Mathf.Clamp01((director.MoneyEnd - Time.unscaledTime) / 0.5f);
        moneyBox.color = new Color(1f, 1f, 1f, a); moneyText.color = new Color(Paper.r, Paper.g, Paper.b, a);
        Show(moneyBox, x, y);
        MoneyPx = new Vector2(x, y); MoneyShown = s;
        vis.Append("money ");
    }

    // 실제 도트 격자 = LowResRenderTest.RenderSize(360 줄). 카메라 RT 는 URP renderScale 0.5 를 감안해 그 2배(1280×720)로
    // 잡혀 있어서 cam.pixelWidth 로 맞추면 UI 가 절반 크기로 그려져 글자 줄이 빠졌다(이 판 보고서 §3).
    LowResRenderTest lowRes;
    int W => lowRes != null && lowRes.isActiveAndEnabled ? lowRes.RenderSize.x : cam.pixelWidth;
    int H => lowRes != null && lowRes.isActiveAndEnabled ? lowRes.RenderSize.y : cam.pixelHeight;

    void HideAll()
    {
        foreach (var g in new Graphic[] { dot, arrowImg, warnBox, chipBox, bubble, tail, nameTab, slip, hintText, destFlag, destDots, destEdge, destEdgeBox, destChip, distBox, moneyBox })
            if (g != null && g.gameObject.activeSelf) g.gameObject.SetActive(false);
        if (diaryRoot != null && diaryRoot.activeSelf) diaryRoot.SetActive(false);
    }

    float Pref(Text t, string s, float maxW)
    {
        var set = t.GetGenerationSettings(new Vector2(maxW, 0f));
        set.horizontalOverflow = HorizontalWrapMode.Overflow; set.richText = false;
        return gen.GetPreferredWidth(s, set) / Mathf.Max(0.01f, t.pixelsPerUnit);
    }

    // ── 기존 HUD 끄기(되돌릴 수 있게) ──
    float legacyScanAt = -99f;

    void HideLegacy()
    {
        if (Time.unscaledTime - legacyScanAt < 0.25f) return;   // 탑승 뒤 늦게 켜지는 HUD(DeliveryProgressHUD 등)도 잡는다
        legacyScanAt = Time.unscaledTime;
        foreach (var m in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (m == null || m.transform.IsChildOf(root)) continue;
            string n = m.GetType().Name;
            if (!(n.EndsWith("HUD") || LegacyExtra.Contains(n))) continue;
            var g = m.gameObject;
            var cv = g.GetComponent<Canvas>();
            if (!g.activeSelf || (cv != null && cv.isRootCanvas)) continue;   // 루트 캔버스(UIRoot)는 결과창 등도 담고 있어 통째로 끄지 않는다
            g.SetActive(false); hidden.Add(g);
        }
    }

    /// <summary>이름이 HUD 로 안 끝나지만 주행 화면에 뜨던 것.</summary>
    static readonly HashSet<string> LegacyExtra = new HashSet<string> { "DirectionArrow", "CargoView" };   // CargoView(TRUNK CAM) — 이름이 HUD 로 안 끝나 주행 중 다시 보였다(D-258 과제)

    void RestoreLegacy()
    {
        foreach (var g in hidden) if (g != null) g.SetActive(true);
        hidden.Clear();
    }
}
