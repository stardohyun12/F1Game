using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: DotScreenUI 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class DotScreenUI
{
    void DriveUI(System.Text.StringBuilder vis)
    {
        // 목적지 방향: 차 카메라 앞(수평) 기준으로 목적지가 몇 도 옆인지 → 16방향 화살표.
        var mgr = Services.Delivery as DeliveryManager;
        var tgt = mgr != null && mgr.CurrentTarget != null ? mgr.CurrentTarget : PreOrderDest;   // D-250: 정비소 → 보관소 자유 주행 중엔 보관소
        if (tgt != null)
        {
            Vector3 f = cam.transform.forward; f.y = 0f;
            Vector3 d = tgt.transform.position - cam.transform.position; d.y = 0f;
            if (f.sqrMagnitude > 1e-4f && d.sqrMagnitude > 1e-4f)
            {
                float ang = Vector3.SignedAngle(f, d, Vector3.up);
                int step = Mathf.RoundToInt(ang / 22.5f);
                if (step != arrowStep) { arrowStep = step; DrawArrow(step); }
                Show(arrowImg, W / 2 - ArrowN / 2, arrowOld ? H - 6 - 15 - 4 : H - 4 - ArrowN); vis.Append("arrow ");   // 1판 중심(위에서 14줄) → 새 중심(16줄): 화살 끝이 화면 위에서 ~7줄로 같다
            }
            // D-252 거리: 화살표 왼쪽(오른쪽은 위험 '!' 자리). 100 m 넘으면 10 m, 그 안은 5 m 단위 — 숫자가 매 프레임 떨지 않게.
            if (showDistance && !SimDestOff)
            {
                Vector3 me = director != null && director.Car != null ? director.Car.transform.position : cam.transform.position;
                Vector3 g = Ground(tgt);
                float dm = Vector2.Distance(new Vector2(me.x, me.z), new Vector2(g.x, g.z));
                int unit = dm >= 100f ? 10 : 5;
                string s = (Mathf.RoundToInt(dm / unit) * unit) + "m";
                distText.text = s;
                int bw = Mathf.CeilToInt(Pref(distText, s, 100)) + 7;
                distBox.rectTransform.sizeDelta = new Vector2(bw, 13);
                Show(distBox, W / 2 - ArrowN / 2 - 2 - bw, H - 4 - ArrowN / 2 - 7); vis.Append("dist ");
            }
        }
        // 화물 위험 "!": CargoWarningHUD 와 같은 4 소스(급제동·코너·스핀·경사) 중 큰 것.
        var cargo = Services.Cargo as CargoBox;
        if (cargo != null && tgt != null)
        {
            float r = Mathf.Max(Mathf.Max(cargo.CurrentLongG / 0.9f, cargo.CurrentLatG / 1.0f), Mathf.Max(cargo.CurrentSpinDegPerSec / 100f, cargo.CurrentTiltDeg / 10f));
            bool blinkOff = r >= warnCrit && Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f;
            if (r >= warnShow && !blinkOff) { Show(warnBox, W / 2 + 7 + 4, H - 6 - 14); vis.Append("warn "); }
        }
        // 전표: Tab 누르는 동안. 탑승 직후 잠깐 안내.
        var rc = Services.Run as RunController;
        var job = rc != null ? rc.CurrentJob : null;
        Vector3 at = director != null && director.Car != null ? director.Car.transform.position : cam.transform.position;
        if (!SlipUI(job, mgr != null ? mgr.CurrentTarget : null, at, vis) && job != null && Time.unscaledTime - cartAt < slipHintSec && SlipHintAllowed()) { Show(hintText, 8, 8); MarkSlipHint(); vis.Append("hint "); }
    }

    /// <summary>
    /// 전표(Tab 누르는 동안). 운전 중 = 지금 주문, 걷는 중(D-250) = 칠판에서 맡은 주문(출발 전). 그렸으면 true.
    /// </summary>
    bool SlipUI(OrderJob job, DeliveryPoint tgt, Vector3 me, System.Text.StringBuilder vis)
    {
        // D-269: 손에 든 전표(HeldPaper)가 있으면 화면 상자 전표는 끈다(스위치 HeldPaper.On = false 로 되돌림). "Tab 전표" 안내는 그대로.
        if (HeldPaper.On && director != null && director.Held != null) return false;
        bool tab = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        if (job != null && (tab || SimSlip))
        {
            slipBody.text = job.displayName;
            if (SimDestOff || tgt == null)
            {
                foreach (var g in new Graphic[] { slipTo, slipAddr, slipMap }) g.gameObject.SetActive(false);
                slipTitle.rectTransform.anchoredPosition = new Vector2(5, 22); slipBody.rectTransform.anchoredPosition = new Vector2(5, 6);
                int bw = Mathf.Clamp(Mathf.CeilToInt(Pref(slipBody, job.displayName, 400)) + 11, 60, 300);
                slip.rectTransform.sizeDelta = new Vector2(bw, 34);
            }
            else
            {
                // D-252: 받는 곳·받는 사람 / 주소 / 대역 약도(오른쪽). 높이 = 위 5 + 제목 11 + 줄 3×13 + 아래 5 = 60 → 약도 52 와 맞춘다.
                string to = "받는 곳  " + (string.IsNullOrEmpty(job.callerName) ? tgt.Label : job.callerName) + (string.IsNullOrEmpty(job.recipientName) ? "" : " · " + job.recipientName);
                string addr = "주소  " + (string.IsNullOrEmpty(job.address) ? "○○길(이름 미정)" : job.address);
                slipTo.text = to; slipAddr.text = addr;
                int tw = Mathf.CeilToInt(Mathf.Max(Pref(slipBody, job.displayName, 400), Mathf.Max(Pref(slipTo, to, 400), Pref(slipAddr, addr, 400))));
                int h = 62;
                slipTitle.rectTransform.anchoredPosition = new Vector2(5, h - 16);
                slipBody.rectTransform.anchoredPosition = new Vector2(5, h - 31);
                slipTo.rectTransform.anchoredPosition = new Vector2(5, h - 45); slipTo.rectTransform.sizeDelta = new Vector2(400, 14);
                slipAddr.rectTransform.anchoredPosition = new Vector2(5, h - 59); slipAddr.rectTransform.sizeDelta = new Vector2(400, 14);
                int bw = Mathf.Clamp(tw + 11 + 4 + MapN + 5, 120, 420);
                slip.rectTransform.sizeDelta = new Vector2(bw, h);
                slipMap.rectTransform.anchoredPosition = new Vector2(bw - 5 - MapN, 5);
                DrawMap(me, Ground(tgt));
                foreach (var g in new Graphic[] { slipTo, slipAddr, slipMap }) g.gameObject.SetActive(true);
                vis.Append("map ");
            }
            Show(slip, 8, 8); vis.Append("slip ");
            return true;
        }
        return false;
    }

    /// <summary>봇·캡처용: Tab 을 누른 것과 같다.</summary>
    public bool SimSlip { get; set; }
    /// <summary>비교 캡처용: 1판 크기(모양 1배)로 같은 화면에 그린다.</summary>
    public bool SimArrowV1 { get => arrowOld; set { arrowOld = value; arrowStep = int.MinValue; } }

    void ChipUI(System.Text.StringBuilder vis)
    {
        string p = director.Prompt;
        if (string.IsNullOrEmpty(p)) return;
        if (director.Held != null && director.Held.State == HeldPaper.Pose.Read) return;   // D-269: 전표를 읽는 동안 [E] 칩이 종이 위에 겹치지 않게
        bool hasKey = p.StartsWith("E ");
        string label = hasKey ? p.Substring(2) : p;
        chipLabel.text = label;
        int lw = Mathf.CeilToInt(Pref(chipLabel, label, 400));
        int keyW = hasKey ? 13 : 0;
        chipKey.gameObject.SetActive(hasKey);
        int w = 3 + keyW + lw + 4, h = 15;
        chipBox.rectTransform.sizeDelta = new Vector2(w, h);
        var lr = chipLabel.rectTransform; lr.anchoredPosition = new Vector2(3 + keyW, 1); lr.sizeDelta = new Vector2(lw + 2, 13);
        // 그 물건 곁(오른쪽 위)에. 화면 밖·뒤면 조준점 바로 아래.
        int x, y;
        if (director.PromptLow) { x = W / 2 - w / 2; y = Mathf.RoundToInt(H * 0.28f); }   // 칠판·앉기: 글씨·나무 위가 아니라 아래쪽 빈 자리
        else if (director.PromptAnchor is Vector3 a && ToScreen(a, out var sp) && sp.x > 0 && sp.x < W && sp.y > 0 && sp.y < H)
        { x = Mathf.RoundToInt(sp.x) + 6; y = Mathf.RoundToInt(sp.y) + 2; }
        else { x = W / 2 - w / 2; y = H / 2 - 10 - h; }
        x = Mathf.Clamp(x, 2, W - w - 2); y = Mathf.Clamp(y, 2, H - h - 2);
        Show(chipBox, x, y); vis.Append("chip ");
    }

    /// <summary>첫 한 판 메모 3: 상자를 들면 카트 위에 「!」 — 화면 밖이면 카트가 있는 쪽 가장자리(방향 표시). 싣고 출발하면 사라진다.</summary>
    void CartMarkUI(System.Text.StringBuilder vis)
    {
        if (!director.CartMark) return;
        int x, y;
        if (ToScreen(director.CartMarkAnchor, out var sp) && sp.x > 6 && sp.x < W - 18 && sp.y > 4 && sp.y < H - 18)
        { x = Mathf.RoundToInt(sp.x) - 5; y = Mathf.RoundToInt(sp.y); }
        else
        {
            Vector3 f = cam.transform.forward; f.y = 0f;
            Vector3 d = director.CartMarkAnchor - cam.transform.position; d.y = 0f;
            float ang = f.sqrMagnitude > 1e-4f && d.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(f, d, Vector3.up) : 0f;
            x = ang >= 0f ? W - 16 : 5; y = H / 2 - 7;
        }
        Show(warnBox, x, y); vis.Append("mark ");
    }

    void TalkUI(System.Text.StringBuilder vis)
    {
        string full = director.TalkText;
        if (full != bubbleLine)
        {
            bubbleLine = full;
            int oneLine = Mathf.CeilToInt(Pref(bubbleText, full, 2000));
            int tw = Mathf.Min(oneLine, bubbleMaxW - 10);
            bubbleText.rectTransform.sizeDelta = new Vector2(tw + 1, 13);
            bubbleText.text = full;
            int th = Mathf.CeilToInt(bubbleText.preferredHeight);
            bubble.rectTransform.sizeDelta = new Vector2(tw + 10, th + 8);
            bubbleText.rectTransform.sizeDelta = new Vector2(tw + 1, th);
            bubbleText.rectTransform.anchoredPosition = new Vector2(5, 4);
            nameText.text = director.TalkWho;
            nameTab.rectTransform.sizeDelta = new Vector2(Mathf.CeilToInt(Pref(nameText, director.TalkWho, 200)) + 8, 13);
        }
        // 한 글자씩: 안 나온 글자는 투명으로 그려 줄바꿈 자리를 미리 잡는다(글이 나오며 상자가 흔들리지 않게).
        int n = Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - director.TalkLineAt) * typeCps), 0, full.Length);
        bubbleText.supportRichText = true;
        bubbleText.text = n >= full.Length ? full : full.Substring(0, n) + "<color=#00000000>" + full.Substring(n) + "</color>";

        var sz = bubble.rectTransform.sizeDelta;
        int bw = (int)sz.x, bh = (int)sz.y;
        Vector3 head = director.SpeakerTop + Vector3.up * 0.03f;   // 0.12 는 대화 거리 2m 에서 머리 위 ~20px 떠 보였다
        int ax, ay;
        if (ToScreen(head, out var sp)) { ax = Mathf.RoundToInt(sp.x); ay = Mathf.RoundToInt(sp.y); }
        else { ax = W / 2; ay = H / 2; }
        // 꼬리 끝 = 머리 위. 화면 밖이면 가장자리 안으로(시선 이동 0.5초 동안만 생긴다).
        int bx = Mathf.Clamp(ax - bw / 2, 2, W - bw - 2);
        int by = Mathf.Clamp(ay + 3, 2, H - bh - 13 - 2);
        int tx = Mathf.Clamp(ax - 3, bx + 3, bx + bw - 10);
        Show(bubble, bx, by);
        Show(tail, tx, by - 3);
        Show(nameTab, bx + 4, by + bh - 1);
        vis.Append("bubble ");
    }
}
