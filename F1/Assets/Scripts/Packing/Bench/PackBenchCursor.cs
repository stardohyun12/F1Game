using UnityEngine;
using UnityEngine.UI;

namespace PitCrew.PackBench
{
    /// <summary>
    /// 포장 작업대 화면 UI — 걷기(DotScreenUI)와 같은 문법만: 가운데 조준점 2×2(= 커서 하나) + [E] 칩을 그 상자 곁에.
    /// 단계 글자·진행 막대 없음(D-240 B 거부, DELIVERY-LOOP §UI "포장 = 화면 0, 커서 1개").
    /// DotScreenUI 의 캔버스 방식을 그대로 옮겼다(World Space 캔버스를 카메라 near+2cm 에, 1 단위 = RT 1 픽셀, PixelSnapText).
    /// 조준점은 손에 뭘 들었는지에 따라 모양만 바뀐다: 빈손 2×2 · 든 것/도구 4×4 테두리(가설 — 도현 판정).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class PackBenchCursor : MonoBehaviour
    {
        [SerializeField] PackBench bench;
        [SerializeField] Font font, smallFont;
        [SerializeField] int fontSize = 12, smallSize = 10;
        [Header("아트 슬롯(비면 코드 임시 모양)")]
        public Sprite tabSprite, keySprite;

        Camera cam; LowResRenderTest lowRes;
        RectTransform root;
        Image dot, ring, chipBox, chipKey;
        Text chipLabel, chipKeyText;
        TextGenerator gen;

        /// <summary>QA 가 읽는 것 — 지금 보이는 요소 이름들.</summary>
        public string Visible { get; private set; } = "";

        int W => lowRes != null && lowRes.isActiveAndEnabled ? lowRes.RenderSize.x : cam.pixelWidth;
        int H => lowRes != null && lowRes.isActiveAndEnabled ? lowRes.RenderSize.y : cam.pixelHeight;

        void Start()
        {
            if (bench == null) bench = PackBench.I;
            cam = bench != null && bench.cam != null ? bench.cam : Camera.main;
            if (cam == null) { enabled = false; return; }
            lowRes = cam.GetComponent<LowResRenderTest>();
            Build();
        }

        void Build()
        {
            var go = new GameObject("PackBenchCursor_Canvas", typeof(Canvas));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 32000;
            root = (RectTransform)go.transform;
            root.SetParent(cam.transform, false);
            root.pivot = new Vector2(0.5f, 0.5f);
            gen = new TextGenerator();
            foreach (var f in new[] { font, smallFont })
                if (f != null && f.material != null && f.material.mainTexture != null) f.material.mainTexture.filterMode = FilterMode.Point;

            var dark = tabSprite != null ? tabSprite : MakeBox(UIPalette.Ink, UIPalette.Ink);
            var key = keySprite != null ? keySprite : MakeBox(UIPalette.Paper, UIPalette.Ink);
            dot = Img("Dot", null, UIPalette.Paper, 2, 2);
            ring = Img("Ring", MakeRing(), Color.white, 6, 6);
            chipBox = Img("Chip", dark, Color.white, 40, 15, true);
            chipKey = Img("Key", key, Color.white, 11, 11, true, chipBox.transform);
            chipKey.rectTransform.anchoredPosition = new Vector2(2, 2);
            chipKeyText = Txt("Text", chipKey.transform, smallFont, smallSize, UIPalette.Ink, TextAnchor.MiddleCenter); chipKeyText.text = "E";
            var kr = chipKeyText.rectTransform; kr.anchorMin = Vector2.zero; kr.anchorMax = Vector2.one; kr.offsetMin = new Vector2(1, 0); kr.offsetMax = Vector2.zero;
            chipLabel = Txt("Label", chipBox.transform, font, fontSize, UIPalette.Paper, TextAnchor.MiddleLeft);
            chipLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void LateUpdate()
        {
            if (root == null) return;
            float d = cam.nearClipPlane + 0.02f;
            float fh = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            root.localPosition = new Vector3(0f, 0f, d); root.localRotation = Quaternion.identity;
            root.sizeDelta = new Vector2(W, H); root.localScale = Vector3.one * (fh / H);

            var vis = new System.Text.StringBuilder();
            bool busy = bench != null && bench.HandBusy;
            Set(dot, !busy, W / 2 - 1, H / 2 - 1); if (!busy) vis.Append("dot ");
            Set(ring, busy, W / 2 - 3, H / 2 - 3); if (busy) vis.Append("ring ");
            string p = bench != null ? bench.Prompt : "";
            chipBox.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(p)) { Chip(p, bench.PromptAnchor); vis.Append("chip "); }
            Visible = vis.ToString().Trim();
        }

        static void Set(Graphic g, bool on, int x, int y)
        {
            if (g.gameObject.activeSelf != on) g.gameObject.SetActive(on);
            if (on) g.rectTransform.anchoredPosition = new Vector2(x, y);
        }

        void Chip(string p, Vector3? anchor)
        {
            bool hasKey = p.StartsWith("E ");
            string label = hasKey ? p.Substring(2) : p;
            chipLabel.text = label;
            var set = chipLabel.GetGenerationSettings(new Vector2(400, 0f));
            set.horizontalOverflow = HorizontalWrapMode.Overflow; set.richText = false;
            int lw = Mathf.CeilToInt(gen.GetPreferredWidth(label, set) / Mathf.Max(0.01f, chipLabel.pixelsPerUnit));
            int keyW = hasKey ? 13 : 0;
            chipKey.gameObject.SetActive(hasKey);
            int w = 3 + keyW + lw + 4, h = 15;
            chipBox.rectTransform.sizeDelta = new Vector2(w, h);
            var lr = chipLabel.rectTransform; lr.anchoredPosition = new Vector2(3 + keyW, 1); lr.sizeDelta = new Vector2(lw + 2, 13);
            int x, y;
            var s = anchor is Vector3 a ? cam.WorldToScreenPoint(a) : Vector3.back;
            float sx = s.x * W / cam.pixelWidth, sy = s.y * H / cam.pixelHeight;
            if (s.z > 0.05f && sx > 0 && sx < W && sy > 0 && sy < H) { x = Mathf.RoundToInt(sx) + 6; y = Mathf.RoundToInt(sy) + 2; }
            else { x = W / 2 - w / 2; y = H / 2 - 10 - h; }
            x = Mathf.Clamp(x, 2, W - w - 2); y = Mathf.Clamp(y, 2, H - h - 2);
            chipBox.gameObject.SetActive(true);
            chipBox.rectTransform.anchoredPosition = new Vector2(x, y);
        }

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

        static Sprite MakeBox(Color fill, Color edge)
        {
            var t = new Texture2D(7, 7, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "PackUI_Box" };
            for (int y = 0; y < 7; y++)
                for (int x = 0; x < 7; x++)
                {
                    bool cx = x == 0 || x == 6, cy = y == 0 || y == 6;
                    t.SetPixel(x, y, cx && cy ? Color.clear : (cx || cy) ? edge : fill);
                }
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, 7, 7), Vector2.zero, 100f, 0, SpriteMeshType.FullRect, new Vector4(2, 2, 2, 2));
        }

        /// <summary>6×6 속 빈 네모(종이색 테 1px) — 손에 뭘 들었을 때의 조준점.</summary>
        static Sprite MakeRing()
        {
            var t = new Texture2D(6, 6, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "PackUI_Ring" };
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 6; x++)
                {
                    bool edge = x == 0 || x == 5 || y == 0 || y == 5, corner = (x == 0 || x == 5) && (y == 0 || y == 5);
                    t.SetPixel(x, y, edge && !corner ? UIPalette.Paper : Color.clear);
                }
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, 6, 6), Vector2.zero, 100f);
        }
    }
}
