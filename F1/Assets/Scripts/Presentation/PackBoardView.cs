using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// D-294 포장 2D ①: PackBoard 를 작업대 윗면 위에 그린다 — 월드 안의 납작한 회색·갈색 대역(아트 아님, 월드 질감 셰이더는 뒤 과제).
/// 판 바탕·가운데 빈 자리 테두리·조각(상자는 열리면 속이 보임)·글자(조각 이름, 물건은 꼬리표 글). 보드가 켜진 동안만 보인다. 조각 자료는 건드리지 않는다.
/// </summary>
public class PackBoardView : MonoBehaviour
{
    PackBoard board;
    Transform root;
    readonly Dictionary<PackPiece, Transform> body = new Dictionary<PackPiece, Transform>();
    readonly Dictionary<PackPiece, Transform> inner = new Dictionary<PackPiece, Transform>();
    readonly Dictionary<PackPiece, TextMeshPro> label = new Dictionary<PackPiece, TextMeshPro>();
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
        paperC = new Color(0.86f, 0.84f, 0.78f), tapeC = new Color(0.85f, 0.75f, 0.20f);

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

    Transform Slab(string n, Transform parent, Color c)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
        foreach (var p in board.pieces)
        {
            var c = p.kind == PackPieceKind.Parcel ? parcelC : p.kind == PackPieceKind.Box ? boxC : p.kind == PackPieceKind.Tape ? tapeC : p.filler == "종이" ? paperC : bubbleC;
            var b = Slab(p.name, root, c); b.localScale = new Vector3(p.w, 0.03f, p.d); body[p] = b;
            if (p.kind == PackPieceKind.Box)
            {
                var i = Slab(p.name + " 속", b, insideC);
                i.localScale = new Vector3(0.82f, 1.2f, 0.82f); i.localPosition = new Vector3(0f, 0.02f, 0f); i.gameObject.SetActive(false); inner[p] = i;
            }
            var t = Text(p.name + " 글", root, 0.5f); label[p] = t;   // 조각(납작하게 눌린 부모)이 아니라 root 아래 — 글자가 일그러지지 않게
        }
    }

    void Place(Transform t, float u, float v, float h)
    {
        t.SetPositionAndRotation(board.Origin + board.Right * u + board.Up * v + Vector3.up * h, flat);
    }

    void LateUpdate()
    {
        bool on = board != null && board.On;
        if (root == null) { if (!on) return; BuildAll(); }
        if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on);
        if (!on) return;
        foreach (var p in board.pieces)
        {
            var b = body[p];
            if (b.gameObject.activeSelf != p.active) { b.gameObject.SetActive(p.active); label[p].gameObject.SetActive(p.active); }
            if (!p.active) continue;
            float h = p.kind == PackPieceKind.Box ? 0.05f : 0.03f;
            float lift = p.lifted ? 0.07f : 0f;
            b.localScale = new Vector3(p.w, h, p.d);
            Place(b, p.u, p.v, 0.012f + h * 0.5f + lift);
            if (inner.TryGetValue(p, out var i)) i.gameObject.SetActive(p.open);
            var t = label[p];
            t.transform.SetPositionAndRotation(b.position + Vector3.up * (h * 0.5f + 0.003f), Quaternion.LookRotation(Vector3.down, board.Up));
            string s = p.kind == PackPieceKind.Parcel && p.parcel != null ? p.parcel.cargoName + "\n" + p.parcel.recipient + "·" + p.parcel.destLabel : p.name;
            if (t.text != s) t.text = s;
        }
    }
}
