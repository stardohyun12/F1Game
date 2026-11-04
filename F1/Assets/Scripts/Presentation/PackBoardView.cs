using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// D-294 포장 2D: PackBoard 를 작업대 윗면 위에 그린다 — 월드 안의 납작한 회색·갈색 대역(아트 아님, 월드 질감 셰이더는 뒤 과제).
/// 판 바탕·가운데 빈 자리 테두리·조각(상자는 열리면 속이 보임, 바닥/덮은 완충재·들어간 물건, 닫히면 뚜껑+테이프 줄)·글자(조각 이름, 물건은 꼬리표 글).
/// 뜯긴 한 장(Sheet)은 생기고 사라지므로 조각은 처음 볼 때 만든다. 보드가 켜진 동안만 보인다. 조각 자료는 건드리지 않는다.
/// </summary>
public class PackBoardView : MonoBehaviour
{
    PackBoard board;
    Transform root;
    readonly Dictionary<PackPiece, Transform> body = new Dictionary<PackPiece, Transform>();
    readonly Dictionary<PackPiece, Transform> inner = new Dictionary<PackPiece, Transform>();
    readonly Dictionary<PackPiece, TextMeshPro> label = new Dictionary<PackPiece, TextMeshPro>();
    sealed class BoxX { public Transform bottom, top, lid; public readonly List<Transform> strips = new List<Transform>(), wads = new List<Transform>(); }
    readonly Dictionary<PackPiece, BoxX> extras = new Dictionary<PackPiece, BoxX>();
    readonly Dictionary<PackPiece, Transform> shell = new Dictionary<PackPiece, Transform>();   // D-296 물건을 감싼 뽁뽁이 층(대역: 물건보다 넓고 밝은 테두리)
    readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
    TMP_FontAsset font;
    Quaternion flat;

    public static PackBoardView Create(PackBoard b, TMP_FontAsset f)
    {
        var v = b.gameObject.AddComponent<PackBoardView>();
        v.board = b; v.font = f;
        return v;
    }

    // 노을빛(어둡다)을 받아도 읽히게 밝게 — 아트 아님, 대역
    static readonly Color matC = new Color(0.50f, 0.52f, 0.51f), slotC = new Color(0.86f, 0.88f, 0.82f), parcelC = new Color(0.78f, 0.55f, 0.32f),
        boxC = new Color(0.86f, 0.70f, 0.45f), insideC = new Color(0.20f, 0.15f, 0.10f), bubbleC = new Color(0.78f, 0.84f, 0.86f),
        paperC = new Color(0.86f, 0.84f, 0.78f), tapeC = new Color(0.85f, 0.75f, 0.20f), boxLidC = new Color(0.93f, 0.78f, 0.52f);

    Material Mat(Color c)
    {
        if (mats.TryGetValue(c, out var m)) return m;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(sh) { name = "pack2d(runtime)" };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return mats[c] = m;
    }

    Transform Slab(string n, Transform parent, Color c, PrimitiveType shape = PrimitiveType.Cube)
    {
        var g = GameObject.CreatePrimitive(shape);
        g.name = n;
        Destroy(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().sharedMaterial = Mat(c);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    TextMeshPro Text(string n, Transform parent, float size)
    {
        var g = new GameObject(n);
        g.transform.SetParent(parent, false);
        var t = g.AddComponent<TextMeshPro>();
        if (font != null) t.font = font;
        t.fontSize = size;                                                // 월드 글자: 0.5 ≈ 글자 높이 4.5 cm
        t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.color = new Color(0.98f, 0.95f, 0.88f);
        t.rectTransform.sizeDelta = new Vector2(0.5f, 0.2f);
        return t;
    }

    Transform back, slotFrame, slotIn;

    void BuildAll()
    {
        root = new GameObject("PackBoard2D_View").transform;
        root.SetParent(transform, false);
        flat = Quaternion.LookRotation(board.Up, Vector3.up);
        back = Slab("판 바탕", root, matC);
        Place(back, 0f, 0f, 0.002f);
        back.localScale = new Vector3(PackBoard.HalfW * 2f, 0.004f, PackBoard.HalfH * 2f);
        slotFrame = Slab("빈 자리 테두리", root, slotC);
        slotFrame.localScale = new Vector3(PackBoard.SlotW + 0.02f, 0.004f, PackBoard.SlotD + 0.02f);
        slotIn = Slab("빈 자리", root, matC);
        slotIn.localScale = new Vector3(PackBoard.SlotW, 0.004f, PackBoard.SlotD);
        Place(slotFrame, PackBoard.SlotU, PackBoard.SlotV, 0.0085f); Place(slotIn, PackBoard.SlotU, PackBoard.SlotV, 0.0095f);
    }

    static Color FillerColor(PackFiller f) => f == PackFiller.Paper ? paperC : bubbleC;

    /// <summary>조각 하나의 몸·글자(·상자는 속과 덮개)를 처음 볼 때 만든다.</summary>
    void Ensure(PackPiece p)
    {
        if (body.ContainsKey(p)) return;
        var c = p.kind == PackPieceKind.Parcel ? parcelC : p.kind == PackPieceKind.Box ? boxC : p.kind == PackPieceKind.Tape ? tapeC : FillerColor(p.fillerKind);
        var b = Slab(p.name, root, c, p.crumple ? PrimitiveType.Cylinder : PrimitiveType.Cube); b.localScale = new Vector3(p.w, 0.03f, p.d); body[p] = b;   // 구긴 종이 = 원통(둥근 덩어리 대역)
        if (p.kind == PackPieceKind.Box)
        {
            var i = Slab(p.name + " 속", b, insideC);
            i.localScale = new Vector3(0.82f, 1.2f, 0.82f); i.localPosition = new Vector3(0f, 0.02f, 0f); i.gameObject.SetActive(false); inner[p] = i;
            extras[p] = new BoxX
            {
                bottom = Slab(p.name + " 바닥 완충재", root, bubbleC), top = Slab(p.name + " 덮은 완충재", root, bubbleC), lid = Slab(p.name + " 뚜껑", root, boxLidC),
            };
        }
        if (p.kind == PackPieceKind.Parcel) { var sh = Slab(p.name + " 감싼 뽁뽁이", root, bubbleC); sh.gameObject.SetActive(false); shell[p] = sh; }
        var t = Text(p.name + " 글", root, 0.5f); label[p] = t;   // 조각(납작하게 눌린 부모)이 아니라 root 아래 — 글자가 일그러지지 않게
    }

    void Drop(PackPiece p)
    {
        if (body.TryGetValue(p, out var b) && b != null) Destroy(b.gameObject);
        if (label.TryGetValue(p, out var t) && t != null) Destroy(t.gameObject);
        if (extras.TryGetValue(p, out var x)) { Destroy(x.bottom.gameObject); Destroy(x.top.gameObject); Destroy(x.lid.gameObject); foreach (var s in x.strips) Destroy(s.gameObject); foreach (var s in x.wads) Destroy(s.gameObject); }
        if (shell.TryGetValue(p, out var sh) && sh != null) Destroy(sh.gameObject);
        body.Remove(p); label.Remove(p); inner.Remove(p); extras.Remove(p); shell.Remove(p);
    }

    void Place(Transform t, float u, float v, float h)
    {
        t.SetPositionAndRotation(board.Origin + board.Right * u + board.Up * v + Vector3.up * h, flat);
    }

    // 높이(m, 판 윗면 기준): 상자 속 윗면 0.068 · 완충재 한 장 0.012 · 물건 0.03
    const float BoxTop = 0.068f, SheetH = 0.012f, ParcelH = 0.03f;
    /// <summary>감싼 겹 하나가 물건 테두리를 넓히는 폭(m, 그림용 과장 — 실제 크기 증가는 PackRules.WrapThick).</summary>
    const float WrapRim = 0.03f;
    static readonly Color wrap2C = new Color(0.94f, 0.98f, 1.0f);
    /// <summary>구긴 종이 덩어리 높이(m, 눌린 뒤) · 이웃 덩어리와 구분되게 살짝 다른 종이색.</summary>
    const float WadH = 0.03f;
    static readonly Color wadC = new Color(0.78f, 0.75f, 0.68f);

    static void SetAct(Transform t, bool on) { if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on); }

    void LateUpdate()
    {
        bool on = board != null && board.On;
        if (root == null) { if (!on) return; BuildAll(); }
        if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on);
        if (!on) return;
        var pcs = board.pieces;
        for (int n = 0; n < pcs.Count; n++) Ensure(pcs[n]);
        if (body.Count != pcs.Count)                                     // 뜯어 쓴 한 장은 사라진다
        {
            var gone = new List<PackPiece>();
            foreach (var k in body.Keys) if (!pcs.Contains(k)) gone.Add(k);
            foreach (var k in gone) Drop(k);
        }
        foreach (var p in pcs)
        {
            var b = body[p];
            bool show = p.active && !(p.holder != null && (p.holder.closed || !p.holder.active));
            if (b.gameObject.activeSelf != show) { b.gameObject.SetActive(show); label[p].gameObject.SetActive(show); }
            if (!p.active && extras.TryGetValue(p, out var off)) { SetAct(off.bottom, false); SetAct(off.top, false); SetAct(off.lid, false); foreach (var st in off.strips) SetAct(st, false); foreach (var wd in off.wads) SetAct(wd, false); }
            if (!show) { if (shell.TryGetValue(p, out var sh0)) SetAct(sh0, false); continue; }
            float h = p.kind == PackPieceKind.Box ? 0.05f : p.crumple ? Mathf.Lerp(0.012f, WadH + 0.01f, p.crumpleT) : 0.03f;
            float lift = p.lifted ? 0.07f : 0f;
            float pu = p.u + p.jit.x, pv = p.v + p.jit.y;
            float z = p.perch > 0f ? PackBoard.RimZ : p.drop.z;                // D-295: 놓은 뒤 떨어지는 높이 · 벽에 걸려 떠 있는 높이
            float baseY = 0.012f + h * 0.5f + lift + z;
            if (p.holder != null) baseY = BoxTop + (p.holder.bottom != PackFiller.None ? SheetH : 0f) + ParcelH * 0.5f + (p.holder.lifted ? 0.07f : 0f) + p.holder.drop.z + z;
            float dd = p.d;
            if (p.kind == PackPieceKind.Roll) dd = p.d * Mathf.Lerp(0.35f, 1f, (float)p.left / Mathf.Max(1, p.full));   // 롤이 가늘어진다
            if (p.crumple) b.localScale = new Vector3(Mathf.Lerp(0.20f, p.w, p.crumpleT), h * 0.5f, Mathf.Lerp(0.18f, p.d, p.crumpleT));   // 평평한 장 → 둥근 덩어리(원통 높이는 2배라 절반)
            else b.localScale = new Vector3(p.w, h, dd);
            Place(b, pu, pv, baseY);
            float topY = baseY + h * 0.5f;
            if (shell.TryGetValue(p, out var sx))                              // D-296: 감싼 겹이 있으면 물건 둘레에 밝고 넓은 테두리(겹마다 더 두껍고 더 밝게)
            {
                SetAct(sx, p.wrap > 0);
                if (p.wrap > 0)
                {
                    float sh = h - 0.004f;
                    sx.localScale = new Vector3(p.w + WrapRim * p.wrap, sh, p.d + WrapRim * p.wrap);
                    sx.GetComponent<Renderer>().sharedMaterial = Mat(p.wrap >= 2 ? wrap2C : bubbleC);
                    Place(sx, pu, pv, baseY - h * 0.5f + sh * 0.5f);
                }
            }
            if (inner.TryGetValue(p, out var i)) i.gameObject.SetActive(p.open);
            if (extras.TryGetValue(p, out var x)) topY = DrawBox(p, x, pu, pv, lift + z);
            var t = label[p];
            bool hideLabel = p.crumple || (p.kind == PackPieceKind.Box && p.Stuffed && !p.closed);
            SetAct(t.transform, !hideLabel);
            t.transform.SetPositionAndRotation(board.Origin + board.Right * pu + board.Up * pv + Vector3.up * (topY + 0.004f), Quaternion.LookRotation(Vector3.down, board.Up));
            string s = p.kind == PackPieceKind.Parcel && p.parcel != null ? p.parcel.cargoName + "\n" + p.parcel.recipient + "·" + p.parcel.destLabel : p.name;
            if (t.text != s) t.text = s;
        }
    }

    /// <summary>상자의 바닥/덮은 완충재·뚜껑·테이프 줄을 그리고, 글자를 얹을 높이를 돌려준다.</summary>
    float DrawBox(PackPiece p, BoxX x, float pu, float pv, float lift)
    {
        float y = BoxTop + lift;
        bool hasBottom = p.bottom != PackFiller.None, hasTop = p.top != PackFiller.None;
        SetAct(x.bottom, hasBottom);
        if (hasBottom)
        {
            x.bottom.GetComponent<Renderer>().sharedMaterial = Mat(FillerColor(p.bottom));
            x.bottom.localScale = new Vector3(p.w * 0.82f, SheetH, p.d * 0.82f);
            Place(x.bottom, pu, pv, y + p.dropBottom.z + SheetH * 0.5f); y += SheetH;
        }
        if (p.content != null) y += ParcelH;
        SetAct(x.top, hasTop);
        if (hasTop)
        {
            x.top.GetComponent<Renderer>().sharedMaterial = Mat(FillerColor(p.top));
            x.top.localScale = new Vector3(p.w * 0.82f, SheetH, p.d * 0.82f);
            Place(x.top, pu, pv, y + p.dropTop.z + SheetH * 0.5f); y += SheetH;
        }
        SetAct(x.lid, p.closed);
        if (p.closed)
        {
            x.lid.localScale = new Vector3(p.w, SheetH, p.d);
            Place(x.lid, pu, pv, y + SheetH * 0.5f); y += SheetH;
        }
        while (x.strips.Count < p.strips.Count) x.strips.Add(Slab(p.name + " 테이프 줄", root, tapeC));
        for (int k = 0; k < x.strips.Count; k++)
        {
            bool has = k < p.strips.Count && p.closed;
            SetAct(x.strips[k], has);
            if (!has) continue;
            var st = p.strips[k];
            x.strips[k].localScale = st.horiz ? new Vector3(st.len, 0.004f, 0.03f) : new Vector3(0.03f, 0.004f, st.len);
            Place(x.strips[k], pu + st.du, pv + st.dv, y + 0.002f);
        }
        if (p.closed && p.strips.Count > 0) y += 0.004f;
        // D-296 틈에 자리 잡은 구긴 종이(뚜껑이 덮이면 안 보인다). 놓인 만큼 눌려 납작·넓게
        while (x.wads.Count < p.wads.Count) x.wads.Add(Slab(p.name + " 구긴 종이", root, paperC, PrimitiveType.Cylinder));
        float rad = PackBoard.PaperRadius * 0.6f;
        for (int k = 0; k < x.wads.Count; k++)
        {
            bool has = k < p.wads.Count && !p.closed;
            SetAct(x.wads[k], has);
            if (!has) continue;
            var wd = p.wads[k];
            x.wads[k].GetComponent<Renderer>().sharedMaterial = Mat(k % 2 == 0 ? paperC : wadC);
            x.wads[k].localScale = new Vector3(rad * 2.2f, WadH * 0.5f, rad * 2.2f);
            Place(x.wads[k], pu + wd.off.x, pv + wd.off.y, BoxTop + lift + wd.fall.z + WadH * 0.5f + 0.002f);
        }
        return y;
    }
}
