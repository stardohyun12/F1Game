using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// D-287: DotScreenUI 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class DotScreenUI
{
    /// <summary>A 말풍선: 화면에 배치된 말풍선+이름표를 인쇄해 사장 머리 위 판(카메라를 향함)에 붙인다. 크기는 대사가 시작될 때의 거리에서 지금 크기와 같게 정하고 그 뒤로는 세상 크기로 고정.</summary>
    void WorldBubble()
    {
        if (!bubble.gameObject.activeSelf)
        {
            if (bubbleQuad != null && bubbleQuad.activeSelf) bubbleQuad.SetActive(false);
            if (rigBubble != null) { RigHideAll(rigBubble); rigBubble.cam.enabled = false; }
            bubbleFor = null;
            return;
        }
        if (rigBubble == null) { rigBubble = MakeRig("Bubble", new Vector3(0, -20000, 0), 2); printMatBubble = NewPrint("LofiPrint_Bubble"); bubbleQuad = MakeQuad("UI_A_Bubble", printMatBubble, out bubbleMesh); }
        var bs = bubble.rectTransform.sizeDelta; var ns = nameTab.rectTransform.sizeDelta;
        int w = Mathf.CeilToInt(bs.x), h = Mathf.CeilToInt(bs.y + ns.y - 1);
        RigSize(rigBubble, w, h);
        var b0 = bubble.rectTransform.anchoredPosition;
        RigCopy(rigBubble, bubble.gameObject, -b0);
        RigCopy(rigBubble, nameTab.gameObject, -b0);
        rigBubble.cam.enabled = true;
        printMatBubble.mainTexture = rigBubble.rt;
        SyncLight(printMatBubble);
        Vector3 head = director.SpeakerTop + Vector3.up * 0.03f;
        string key = director.TalkWho;
        if (bubbleFor != key)
        {
            bubbleFor = key;
            float dist = Mathf.Max(0.5f, Vector3.Distance(cam.transform.position, head));
            bubbleMpp = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / H;
        }
        QuadMesh(bubbleMesh, w * bubbleMpp, h * bubbleMpp, new Rect(0, 0, w / (float)rigBubble.size.x, h / (float)rigBubble.size.y), true, new Vector2(0.5f, 0f));
        if (!bubbleQuad.activeSelf) bubbleQuad.SetActive(true);
        bubbleQuad.transform.position = head + Vector3.up * 0.06f;
        Vector3 f = head - cam.transform.position;
        bubbleQuad.transform.rotation = Quaternion.LookRotation(f.sqrMagnitude > 1e-4f ? f.normalized : cam.transform.forward, Vector3.up);
        bubble.gameObject.SetActive(false); tail.gameObject.SetActive(false); nameTab.gameObject.SetActive(false);
    }

    /// <summary>A 전표: 손에 든 종이. Tab 을 누르면 0.18초 동안 화면 아래에서 올라오고, 위끝이 살짝 뒤로 젖혀진 채 카메라 앞에 있다(빛·그늘 받음).</summary>
    void WorldSlip()
    {
        bool up = slip.gameObject.activeSelf;
        if (up && !slipWasUp) slipUpAt = Time.unscaledTime;
        slipWasUp = up;
        if (!up)
        {
            if (slipQuad != null && slipQuad.activeSelf) slipQuad.SetActive(false);
            if (rigSlip != null) { RigHideAll(rigSlip); rigSlip.cam.enabled = false; }
            return;
        }
        if (rigSlip == null) { rigSlip = MakeRig("Slip", new Vector3(0, -20500, 0), 2); printMatSlip = NewPrint("LofiPrint_Slip"); slipQuad = MakeQuad("UI_A_Slip", printMatSlip, out slipMesh); }
        var ss = slip.rectTransform.sizeDelta;
        int w = Mathf.CeilToInt(ss.x), h = Mathf.CeilToInt(ss.y);
        RigSize(rigSlip, w, h);
        RigCopy(rigSlip, slip.gameObject, -slip.rectTransform.anchoredPosition);
        rigSlip.cam.enabled = true;
        printMatSlip.mainTexture = rigSlip.rt;
        SyncLight(printMatSlip);
        float d = Mathf.Max(cam.nearClipPlane + 0.06f, 0.32f);
        float mpp = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / H * 1.04f;   // 젖혀진 만큼 조금 크게(1.15 는 글자가 화면 밖으로)
        QuadMesh(slipMesh, w * mpp, h * mpp, new Rect(0, 0, w / (float)rigSlip.size.x, h / (float)rigSlip.size.y), true, new Vector2(0f, 0f));
        if (slipQuad.transform.parent != cam.transform) slipQuad.transform.SetParent(cam.transform, false);
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.unscaledTime - slipUpAt) / 0.18f));
        float halfH = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), halfW = halfH * cam.aspect;
        float x = -halfW + 16f * mpp, yTop = -halfH + 8f * mpp, yHide = -halfH - h * mpp - 0.02f;
        slipQuad.transform.localPosition = new Vector3(x, Mathf.Lerp(yHide, yTop, k), d);
        slipQuad.transform.localRotation = Quaternion.Euler(-8f, 0f, 2f);   // -14°·3° 는 왼쪽 끝이 화면 밖·글자 기울어 못 읽음(21:00)
        if (!slipQuad.activeSelf) slipQuad.SetActive(true);
        slip.gameObject.SetActive(false);
    }

    // ── 칠판 ──
    DepotOrderBoard board; UIStyle boardStyle = UIStyle.Now; Renderer boardFrame, boardSlate; Material frameOrig, slateOrig;

    /// <summary>
    /// 칠판: B = 판·틀에 사진 텍스처(같은 셰이더), 글씨는 지금처럼 칠판 위 캔버스(빛 안 받음).
    /// A = 같은 판·틀 + 글씨까지 칠판 면 RT 에 인쇄해 판과 같은 빛을 받음(캔버스 끔). 둘 다 키 안내 줄은 처음 1회.
    /// </summary>
    void BoardStyle()
    {
        if (board == null) { board = FindFirstObjectByType<DepotOrderBoard>(); if (board == null) return; }
        if (boardFrame == null)
        {
            var f = board.transform.Find("Frame"); var s = board.transform.Find("Slate");
            if (f != null) { boardFrame = f.GetComponent<Renderer>(); frameOrig = boardFrame.sharedMaterial; }
            if (s != null) { boardSlate = s.GetComponent<Renderer>(); slateOrig = boardSlate.sharedMaterial; }
        }
        DepotOrderBoard.HintOnce = UiStyle !=UIStyle.Now;
        if (UiStyle!= boardStyle)
        {
            boardStyle = UiStyle;
            bool tex = UiStyle !=UIStyle.Now;
            if (tex && frameMat == null) { frameMat = NewSurface("UI_Board_Frame", texWood, new Vector2(2f, 1f)); slateMat = NewSurface("UI_Board_Slate", texSlate, new Vector2(1.9f, 1.1f)); }
            if (boardFrame != null) boardFrame.sharedMaterial = tex && frameMat != null ? frameMat : frameOrig;
            if (boardSlate != null) boardSlate.sharedMaterial = tex && slateMat != null ? slateMat : slateOrig;
            if (board.ChalkCanvas != null) board.ChalkCanvas.enabled = UiStyle !=UIStyle.World;
            if (boardQuad != null) boardQuad.SetActive(false);
            if (rigBoard != null) rigBoard.cam.enabled = false;
        }
        SyncLight(frameMat); SyncLight(slateMat);
        if (UiStyle!= UIStyle.World || board.ChalkCanvas == null) return;

        // A: 칠판 캔버스 내용(제목·줄·안내)을 슬레이트 사진 바탕 위에 RT 로 찍어 판 바로 앞 판에 붙인다.
        var crt = (RectTransform)board.ChalkCanvas.transform;
        int w = Mathf.RoundToInt(crt.sizeDelta.x), h = Mathf.RoundToInt(crt.sizeDelta.y);
        if (rigBoard == null)
        {
            rigBoard = MakeRig("Board", new Vector3(0, -21000, 0), 3);
            RigSize(rigBoard, w, h);
            var bg = new GameObject("Slate", typeof(RectTransform), typeof(RawImage));
            bg.layer = PrintLayer; bg.transform.SetParent(rigBoard.canvasRt, false);
            var ri = bg.GetComponent<RawImage>(); ri.texture = texSlate; ri.raycastTarget = false;
            var br = (RectTransform)bg.transform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            ri.uvRect = new Rect(0, 0, rigBoard.size.x / (float)texSlate.width, rigBoard.size.y / (float)texSlate.height);   // 1 텍셀 = RT 1칸(칠판 120칸/m)
            printMatBoard = NewPrint("LofiPrint_Board");
            printMatBoard.SetVector("_Tint", new Vector4(1.35f, 1.35f, 1.35f, 1));   // SetVector = 선형 변환 없이 그대로 곱함   // 차고 안 그늘에서 분필이 흐려짐(21:05 d_03_a) — 가설 1.35
            boardQuad = MakeQuad("UI_A_BoardFace", printMatBoard, out boardMesh);
            boardQuad.transform.SetParent(board.transform, false);
            boardQuad.transform.localPosition = Vector3.forward * 0.003f; boardQuad.transform.localRotation = Quaternion.identity;
            float mw = w / DepotOrderBoard.UnitsPerMeter, mh = h / DepotOrderBoard.UnitsPerMeter;
            QuadMesh(boardMesh, mw, mh, new Rect(0, 0, w / (float)rigBoard.size.x, h / (float)rigBoard.size.y), false, new Vector2(0.5f, 0.5f));
        }
        foreach (Transform row in crt)
        {
            var off = new Vector2(w * 0.5f, h * 0.5f) - new Vector2(rigBoard.size.x * 0.5f, rigBoard.size.y * 0.5f);   // 칠판 캔버스(가운데 원점) → 인쇄대 캔버스(가운데 원점, 8 단위로 커진 만큼 왼쪽 아래 맞춤)
            RigCopy(rigBoard, row.gameObject, -off);
        }
        rigBoard.cam.enabled = true;
        printMatBoard.mainTexture = rigBoard.rt;
        SyncLight(printMatBoard);
        if (!boardQuad.activeSelf) boardQuad.SetActive(true);
    }

    /// <summary>LateUpdate 끝에서: 안 바뀜 반영 + A 판 그리기.</summary>
    void StyleLate()
    {
        EnsureStyleAssets();
        if (appliedStyle != UiStyle) ApplyStyleSprites();
        BoardStyle();
        if (UiStyle== UIStyle.World && director != null) { WorldBubble(); WorldSlip(); }
        else
        {
            if (bubbleQuad != null && bubbleQuad.activeSelf) { bubbleQuad.SetActive(false); rigBubble.cam.enabled = false; }
            if (slipQuad != null && slipQuad.activeSelf) { slipQuad.SetActive(false); rigSlip.cam.enabled = false; }
            bubbleFor = null; slipWasUp = false;
        }
    }

    /// <summary>탑승 안내 "Tab 전표": 지금 = 탑승마다 6초, A·B = 첫 탑승 1회만.</summary>
    bool SlipHintAllowed()
    {
        if (UiStyle== UIStyle.Now) return true;
        return !slipHintShownOnce || Time.unscaledTime - slipHintFirstAt < slipHintSec;
    }
    float slipHintFirstAt = -99f;
    void MarkSlipHint() { if (!slipHintShownOnce) { slipHintShownOnce = true; slipHintFirstAt = Time.unscaledTime; } }
}
