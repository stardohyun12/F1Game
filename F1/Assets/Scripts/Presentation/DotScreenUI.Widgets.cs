using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// D-287: DotScreenUI 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
public partial class DotScreenUI
{
    Image Img(string name, Sprite s, Color c, int w, int h, bool sliced = false, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent != null ? parent : root, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; r.sizeDelta = new Vector2(w, h);
        var im = go.GetComponent<Image>(); im.sprite = s; im.color = c; im.raycastTarget = false;
        if (sliced && s != null) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 1f; }
        return im;
    }

    Text Txt(string name, Transform parent, Font f, int size, Color c, TextAnchor a)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.font = f != null ? f : font; t.fontSize = size; t.color = c; t.alignment = a; t.raycastTarget = false;
        t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = false;
        go.AddComponent<PixelSnapText>().root = root;
        return t;
    }

    static void Fill(RectTransform r, int l, int b, int rr, int tt)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -tt);
    }

    /// <summary>7×7 상자(모서리 1칸 깎은 계단, 테 1px) — 9분할 테두리 2.</summary>
    static Sprite MakeBox(Color fill, Color edge)
    {
        var t = new Texture2D(7, 7, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Box" };
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
            {
                bool cx = x == 0 || x == 6, cy = y == 0 || y == 6;
                t.SetPixel(x, y, cx && cy ? Color.clear : (cx || cy) ? edge : fill);
            }
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 7, 7), Vector2.zero, 100f, 0, SpriteMeshType.FullRect, new Vector4(2, 2, 2, 2));
    }

    /// <summary>말풍선 꼬리 7×4 — 맨 윗줄은 상자 밑 테를 덮는다(속은 종이색).</summary>
    static Sprite MakeTail(Color fill, Color edge)
    {
        var t = new Texture2D(7, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Tail" };
        string[] rows = { "...o...", "..ofo..", ".offfo.", "offfffo" };   // 아래(y=0) → 위(y=3)
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 7; x++)
            {
                char ch = rows[y][x];
                t.SetPixel(x, y, ch == 'o' ? edge : ch == 'f' ? fill : Color.clear);
            }
        t.SetPixel(0, 3, edge); t.SetPixel(6, 3, edge);
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 7, 4), Vector2.zero, 100f);
    }

    /// <summary>
    /// 목적지 깃발(테 1px 포함): 종이색 깃대 + 신호색 깃발(오른쪽 끝 제비꼬리). 깃대 아래 끝 = 그림 왼쪽 아래(x 2) — 점선이 여기서 이어진다.
    /// 깃발은 x 3..fx1, y fy0..fy1, 제비꼬리 홈은 x ≥ notchX 부터 yc 를 가운데로 한 칸씩 벌어진다. 가까운 쪽 13×18 · 먼 쪽 17×24 가 같은 모양의 큰 판이다.
    /// </summary>
    static Sprite MakeFlag(int w, int h, int fy0, int fy1, int fx1, int notchX, int yc)
    {
        var kind = new int[w, h];   // 0 빈칸, 1 깃대(종이), 2 깃발(신호)
        for (int y = 1; y < h - 1; y++) kind[2, y] = 1;
        for (int y = fy0; y <= fy1; y++)
            for (int x = 3; x <= fx1; x++)
                if (!(x >= notchX && Mathf.Abs(y - yc) <= x - notchX)) kind[x, y] = 2;   // 제비꼬리 홈
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotUI_Flag" + w };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                if (kind[x, y] == 1) c = Paper;
                else if (kind[x, y] == 2) c = Signal;
                else
                {
                    for (int dy = -1; dy <= 1 && c.a == 0f; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) continue;
                            int xx = x + dx, yy = y + dy;
                            if (xx >= 0 && xx < w && yy >= 0 && yy < h && kind[xx, yy] != 0) { c = Ink; break; }
                        }
                }
                t.SetPixel(x, y, c);
            }
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, w, h), Vector2.zero, 100f);
    }

    /// <summary>점선 한 칸 3×4: 종이 점 2줄(양옆 잉크 테) + 빈 2줄. 세로로 바둑처럼 이어 붙인다(Tiled).</summary>
    static Sprite MakeDots()
    {
        var t = new Texture2D(3, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, name = "DotUI_Dots" };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 3; x++)
                t.SetPixel(x, y, y >= 2 ? (x == 1 ? Paper : Ink) : Color.clear);
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 3, 4), Vector2.zero, 100f, 0, SpriteMeshType.FullRect);
    }

    /// <summary>방향 화살표를 픽셀 단위로 직접 찍는다(16방향). 회전한 그림을 늘리지 않으니 흐린 픽셀이 없다. 모양은 1판 그대로, arrowScale 배.</summary>
    void DrawArrow(int step) => DrawArrowInto(arrowTex, ArrowN, arrowOld ? 1f : ArrowScale, step);

    static void DrawArrowInto(Texture2D arrowTex, int n, float k, int step)
    {
        float c0 = (n - 1) * 0.5f;
        float a = step * (Mathf.PI * 2f / 16f);
        float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        var inside = new bool[n, n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float px = x - c0, py = y - c0;
                // 화면 점을 +a 만큼 돌려 화살표 공간(+y 가 앞)에서 판정 = 화살표는 시계 방향으로 a. step 양수 = 오른쪽. 1/k 로 줄여 판정 = 모양이 k 배.
                float ux = (px * ca - py * sa) / k, uy = (px * sa + py * ca) / k;
                bool head = uy >= -0.5f && uy <= 5.5f && Mathf.Abs(ux) <= (5.5f - uy) * 0.95f;
                bool stem = uy >= -5.5f && uy < -0.5f && Mathf.Abs(ux) <= 1.6f;
                inside[x, y] = head || stem;
            }
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Color c = Color.clear;
                if (inside[x, y]) c = Paper;
                else if ((x > 0 && inside[x - 1, y]) || (x < n - 1 && inside[x + 1, y]) || (y > 0 && inside[x, y - 1]) || (y < n - 1 && inside[x, y + 1])) c = Ink;
                arrowTex.SetPixel(x, y, c);
            }
        arrowTex.Apply(false);
    }
}
