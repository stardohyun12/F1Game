using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: DotScreenUI 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class DotScreenUI
{
    // ── D-252 목적지 표식 ──
    /// <summary>QA·봇이 읽는 것: 표식 상태(none / on = 문 위 / edge = 화면 가장자리 / near = '여기' 칩), 깃발 아래 끝 픽셀(RT), 거리(m).</summary>
    public string DestMode { get; private set; } = "none";
    public Vector2 DestPx { get; private set; }
    public float DestDistM { get; private set; } = -1f;
    /// <summary>비교 캡처용: 이 판 이전(표식·거리·전표 추가 없음) 화면.</summary>
    public bool SimDestOff { get; set; }
    /// <summary>비교 캡처용: 점선/빛기둥 바꾸기.</summary>
    public DestStyle Style { get => destStyle; set => destStyle = value; }

    /// <summary>지금 그릴 목적지: 운전 중 = DeliveryManager.CurrentTarget, 걷는 중 = 사장에게 들은 첫 배달지(탑승 전).</summary>
    DeliveryPoint DestTarget(OnFootDirector.Mode st)
    {
        if (SimDestOff) return null;
        if (st == OnFootDirector.Mode.InCart) { var mgr = Services.Delivery as DeliveryManager; return mgr != null && mgr.CurrentTarget != null ? mgr.CurrentTarget : PreOrderDest; }
        if (st == OnFootDirector.Mode.OnFoot && director != null && !director.InTalk) return PreOrderDest;
        return null;
    }

    /// <summary>주문 출발 전 갈 곳(D-250): 보관소 흐름이면 사장 말 뒤 = 택배 보관소, 칠판에서 맡은 뒤 = 찻집. 옛 흐름이면 찻집.</summary>
    DeliveryPoint PreOrderDest => director != null && director.isActiveAndEnabled && !director.OrderStarted ? director.WalkDest : null;

    Vector3 Ground(DeliveryPoint dp)
    {
        if (groundFor != dp)
        {
            groundFor = dp;
            Vector3 p = dp.transform.position;
            groundAt = Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var h, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.point : p;
        }
        return groundAt;
    }

    void DestUI(DeliveryPoint tgt, System.Text.StringBuilder vis)
    {
        DestMode = "none"; DestDistM = -1f;
        bool beamOn = false;
        // D-269: 전표를 읽는 동안(Read)만 목적지 깃발·칩이 종이 위에 겹치지 않게 잠깐 숨김. 내려놓거나 Up 이면 그대로.
        if (tgt != null && !(director != null && director.Held != null && director.Held.State == HeldPaper.Pose.Read))
        {
            Vector3 me = director != null && director.State == OnFootDirector.Mode.InCart && director.Car != null ? director.Car.transform.position
                       : director != null && director.Player != null ? director.Player.transform.position : cam.transform.position;
            Vector3 gnd = Ground(tgt), top = DestAnchorOf(tgt);
            float dist = Vector2.Distance(new Vector2(me.x, me.z), new Vector2(gnd.x, gnd.z));
            DestDistM = dist;
            ApplyFlagTier(dist);
            beamOn = destStyle != DestStyle.Dots && dist > destNearM * 0.5f;

            var s = cam.WorldToScreenPoint(top);
            Vector2 p = new Vector2(s.x * W / cam.pixelWidth, s.y * H / cam.pixelHeight);
            bool front = s.z > 0.05f;
            var fs = destFlag.rectTransform.sizeDelta;
            int fw = (int)fs.x, fh = (int)fs.y;
            // 위쪽 띠(화살표·거리·위험 '!')는 비워 둔다.
            float xMin = 4, xMax = W - 4 - fw, yMin = 4, yMax = H - 4 - ArrowN - 4 - fh;
            bool inside = front && p.x - 2 >= xMin && p.x - 2 <= xMax && p.y >= yMin && p.y <= yMax;

            if (inside && dist <= destNearM)
            {
                string who = NearLabel();
                destChipText.text = who;
                int lw = Mathf.CeilToInt(Pref(destChipText, who, 400));
                int w = 4 + lw + 4;
                destChip.rectTransform.sizeDelta = new Vector2(w, 15);
                var lr = destChipText.rectTransform; lr.anchoredPosition = new Vector2(4, 1); lr.sizeDelta = new Vector2(lw + 2, 13);
                int cx = Mathf.Clamp(Mathf.RoundToInt(p.x) - w / 2, 2, W - w - 2), cy = Mathf.Clamp(Mathf.RoundToInt(p.y), 2, H - 15 - 2);
                Show(destChip, cx, cy); vis.Append("destchip ");
                DotsDown(p, gnd, cx + w / 2, cy, vis);
                DestMode = "near"; DestPx = new Vector2(cx + w / 2, cy);
            }
            else if (inside)
            {
                int fx = Mathf.RoundToInt(p.x) - 2, fy = Mathf.RoundToInt(p.y);
                Show(destFlag, fx, fy); vis.Append("dest ");
                if (destStyle != DestStyle.Beam) DotsDown(p, gnd, fx + 2, fy, vis);
                DestMode = "on"; DestPx = new Vector2(fx + 2, fy);
            }
            else
            {
                // 화면 밖·카메라 뒤: 가운데에서 그 방향으로 뻗은 선이 테두리에 닿는 곳에 깃발 + 바깥쪽 화살.
                Vector2 c = new Vector2(W * 0.5f, H * 0.5f), d;
                if (front) d = p - c;
                else
                {
                    Vector3 f = cam.transform.forward; f.y = 0f; Vector3 to = top - cam.transform.position; to.y = 0f;
                    d = new Vector2(Mathf.Sign(Vector3.SignedAngle(f, to, Vector3.up)), -0.35f);
                }
                if (d.sqrMagnitude < 1e-4f) d = Vector2.down;
                float ex = (d.x > 0 ? xMax - EdgeN - 1 - c.x : c.x - xMin - EdgeN - 1) / Mathf.Max(1e-3f, Mathf.Abs(d.x));
                float ey = (d.y > 0 ? yMax - c.y : c.y - yMin) / Mathf.Max(1e-3f, Mathf.Abs(d.y));
                Vector2 q = c + d * Mathf.Min(ex, ey);
                int fx = Mathf.RoundToInt(Mathf.Clamp(q.x, xMin + EdgeN + 1, xMax - EdgeN - 1)), fy = Mathf.RoundToInt(Mathf.Clamp(q.y, yMin, yMax));
                Show(destFlag, fx, fy);
                int step = Mathf.RoundToInt(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg / 22.5f);   // 위 = 0, 오른쪽 = +4
                if (step != edgeStep) { edgeStep = step; DrawArrowInto(edgeTex, EdgeN, 0.8f, step); }
                Vector2 dn = d.normalized;
                int axp = Mathf.RoundToInt(fx + fw * 0.5f + dn.x * (fw * 0.5f + EdgeN * 0.5f + 1) - EdgeN * 0.5f);
                int ayp = Mathf.RoundToInt(fy + fh * 0.5f + dn.y * (fh * 0.5f + EdgeN * 0.5f + 1) - EdgeN * 0.5f);
                axp = Mathf.Clamp(axp, 1, W - EdgeN - 1); ayp = Mathf.Clamp(ayp, 1, H - EdgeN - 1);
                Show(destEdge, axp, ayp);
                int bx0 = Mathf.Min(fx, axp) - 2, by0 = Mathf.Min(fy, ayp) - 2;
                int bx1 = Mathf.Max(fx + fw, axp + EdgeN) + 2, by1 = Mathf.Max(fy + fh, ayp + EdgeN) + 2;
                destEdgeBox.rectTransform.sizeDelta = new Vector2(bx1 - bx0, by1 - by0);
                Show(destEdgeBox, bx0, by0);
                vis.Append("destedge ");
                DestMode = "edge"; DestPx = new Vector2(fx + 2, fy);
            }
        }
        Beam(tgt, beamOn);
        if (beamOn) vis.Append("beam ");
        int area = 0;
        foreach (var g in new Graphic[] { destFlag, destDots, destEdge, destEdgeBox, destChip, distBox })
            if (g.gameObject.activeSelf) area += Mathf.RoundToInt(g.rectTransform.sizeDelta.x * g.rectTransform.sizeDelta.y);
        DestAreaPct = area * 100f / (W * H);
    }

    /// <summary>먼 곳은 큰 깃발, 가까우면 원래 크기. 경계(FlagFarM)에서 FlagHysteresisM 만큼 여유를 둬 깜빡이지 않게 한다. 슬롯 스프라이트를 쓰면 정수 2배.</summary>
    void ApplyFlagTier(float dist)
    {
        if (!flagFar && dist > FlagFarM) flagFar = true;
        else if (flagFar && dist < FlagFarM - FlagHysteresisM) flagFar = false;
        bool far = flagFar && !SimFlagNear;
        var sp = far ? flagFarSp : flagNearSp;
        if (destFlag.sprite != sp) destFlag.sprite = sp;
        int k = far && flagSprite != null ? 2 : 1;
        destFlag.rectTransform.sizeDelta = new Vector2(sp.rect.width * k, sp.rect.height * k);
    }

    /// <summary>표식(깃발·점선·가장자리·칩·거리) 화면 면적 합(%, 겹침 포함 위쪽 어림) — 과제 기준 4% 이내.</summary>
    public float DestAreaPct { get; private set; }

    string NearLabel()
    {
        var rc = Services.Run as RunController;
        var job = rc != null ? rc.CurrentJob : null;
        string to = job != null && !string.IsNullOrEmpty(job.callerName) ? job.callerName : "받는 곳";
        return "여기 · " + to;
    }

    /// <summary>깃발 아래(fx,fy)에서 배달점 땅까지 도트 점선. 땅이 화면 밖·뒤면 화면 아래 끝까지.</summary>
    void DotsDown(Vector2 topPx, Vector3 gnd, int x, int y, System.Text.StringBuilder vis)
    {
        float gy = ToScreen(gnd, out var gp) ? gp.y : 0f;
        int len = Mathf.Clamp(y - Mathf.RoundToInt(gy), 0, y - 2);
        len -= len % 4;
        if (len < 4) return;
        destDots.rectTransform.sizeDelta = new Vector2(3, len);
        Show(destDots, x - 1, y - len); vis.Append("dots ");
    }

    /// <summary>은은한 빛기둥(세상 속, 건물에 가려진다): 배달점 땅에서 위로 beamHeight, 카메라 쪽으로 도는 판 하나. 위로 갈수록 옅다.</summary>
    void Beam(DeliveryPoint tgt, bool on)
    {
        if (!on || tgt == null) { if (beam != null && beam.activeSelf) beam.SetActive(false); return; }
        if (beam == null)
        {
            beam = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beam.name = "DotUI_DestBeam";
            Destroy(beam.GetComponent<Collider>());
            var tex = new Texture2D(1, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_BeamFade" };
            for (int y = 0; y < 32; y++) tex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Pow(1f - y / 31f, 1.6f)));
            tex.Apply(false, true);
            beamMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "DotUI_DestBeam" };
            beamMat.SetTexture("_BaseMap", tex);
            beamMat.SetFloat("_Surface", 1f); beamMat.SetFloat("_Blend", 0f);
            beamMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            beamMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            beamMat.SetInt("_ZWrite", 0);
            beamMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            beamMat.renderQueue = 3000;
            var mr = beam.GetComponent<MeshRenderer>();
            mr.sharedMaterial = beamMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }
        if (!beam.activeSelf) beam.SetActive(true);
        var l = UIPalette.Lemon;
        beamMat.SetColor("_BaseColor", new Color(l.r, l.g, l.b, beamAlpha));
        Vector3 g = Ground(tgt);
        beam.transform.position = g + Vector3.up * (beamHeight * 0.5f);
        Vector3 to = cam.transform.position - g; to.y = 0f;
        if (to.sqrMagnitude > 1e-4f) beam.transform.rotation = Quaternion.LookRotation(-to.normalized, Vector3.up);
        beam.transform.localScale = new Vector3(beamWidth, beamHeight, 1f);
    }

    /// <summary>대역 약도: 북쪽 위. 나(잉크 3×3)와 받는 곳(신호색 5×5)이 다 들어가게 줄이고, 사이를 점선으로.</summary>
    void DrawMap(Vector3 me, Vector3 dst)
    {
        var px = new Color32[MapN * MapN];
        Color32 bg = UIPalette.PaperDeep, ink = Ink, sig = Signal, pap = Paper;
        for (int i = 0; i < px.Length; i++) px[i] = bg;
        for (int i = 0; i < MapN; i++) { px[i] = ink; px[(MapN - 1) * MapN + i] = ink; px[i * MapN] = ink; px[i * MapN + MapN - 1] = ink; }
        Vector2 a = new Vector2(me.x, me.z), b = new Vector2(dst.x, dst.z), mid = (a + b) * 0.5f;
        float span = Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), 20f) * 0.5f;
        float k = (MapN * 0.5f - 7f) / span;
        Vector2Int P(Vector2 w) => new Vector2Int(Mathf.RoundToInt(MapN * 0.5f + (w.x - mid.x) * k), Mathf.RoundToInt(MapN * 0.5f + (w.y - mid.y) * k));
        void Put(int x, int y, Color32 c) { if (x > 0 && y > 0 && x < MapN - 1 && y < MapN - 1) px[y * MapN + x] = c; }
        var pa = P(a); var pb = P(b);
        int steps = Mathf.Max(1, Mathf.RoundToInt(Vector2Int.Distance(pa, pb)));
        for (int s = 0; s <= steps; s++) if (s % 3 == 0) { var q = Vector2.Lerp(pa, pb, s / (float)steps); Put(Mathf.RoundToInt(q.x), Mathf.RoundToInt(q.y), ink); }
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++) Put(pb.x + x, pb.y + y, (Mathf.Abs(x) == 2 || Mathf.Abs(y) == 2) ? ink : sig);
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) Put(pa.x + x, pa.y + y, ink);
        Put(pa.x, pa.y, pap);
        // 북쪽 표시: 위 가운데 잉크 삼각 3줄
        for (int r = 0; r < 3; r++) for (int x = -r; x <= r; x++) Put(MapN / 2 + x, MapN - 3 - r, ink);
        mapTex.SetPixels32(px); mapTex.Apply(false);
    }

    static void Show(Graphic g, int x, int y) { g.gameObject.SetActive(true); g.rectTransform.anchoredPosition = new Vector2(x, y); }

    bool ToScreen(Vector3 world, out Vector2 p)
    {
        var s = cam.WorldToScreenPoint(world);
        p = new Vector2(s.x * W / cam.pixelWidth, s.y * H / cam.pixelHeight);
        return s.z > 0.05f;
    }
}
