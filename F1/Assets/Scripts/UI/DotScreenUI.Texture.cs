using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// D-258(과제 20261102-2038): "UI 가 AI 티 — 게임 텍스처 방식과 안 맞음" 비교판. 기본값은 지금 그대로(Now), 디버그 창 버튼으로 바꾼다.
///   Now   — 1판: 단색 상자 + 1px 테.
///   World — A 세상 속 물건: 칠판 글씨는 칠판 면에 인쇄(빛·안개 받음), Tab 전표는 손에 든 종이(화면 아래에서 올라옴), 말풍선은 사장 머리 위 판.
///           셋 다 같은 사진 텍스처(Resources/LofiUI, CC0) 바탕을 작은 RT 에 찍고 LofiPilot/Print 셰이더(LofiSurface 와 같은 빛 계산)로 3D 판에 붙인다.
///   Paper — B 화면 UI 유지 + 게임 텍스처: 상자 바탕 = 같은 사진 텍스처를 1 텍셀 = 도트 1칸으로 깔고, 테두리 = 1px 선 대신 텍스처 밝기로 정한 찢긴 가장자리.
/// 두 안 다 키 안내 줄(칠판 "W/S 고르기…", 탑승 "Tab 전표")은 처음 1회만.
/// </summary>
public partial class DotScreenUI
{
    public enum UIStyle { Now, World, Paper }
    public static UIStyle UiStyle =UIStyle.Now;
    public static string StyleLabel(UIStyle s) => s == UIStyle.Now ? "지금" : s == UIStyle.World ? "A 세상 속" : "B 텍스처 화면";

    UIStyle appliedStyle = UIStyle.Now;
    bool styleInit;
    Sprite nowBox, nowDark, nowKey, nowTail, nowWarn;
    Sprite paperBox, woodBox, paperTail;
    Texture2D texPaper, texWood, texSlate;
    Material printMatBubble, printMatSlip, printMatBoard, frameMat, slateMat, lightRef;
    static bool slipHintShownOnce;

    /// <summary>QA: 지금 그린 안(Now/World/Paper)과 A 판이 떠 있는지.</summary>
    public string StyleState => $"{UiStyle} bubble3d={(bubbleQuad != null && bubbleQuad.activeSelf)} slip3d={(slipQuad != null && slipQuad.activeSelf)} board={boardStyle}";

    // ── 재료: 사진 텍스처를 읽을 수 있게 복사 + 세상(PCTex 5모드 9탭)과 비슷하게 1-2-1 로 미리 흐림 ──
    static Texture2D LoadSoft(string n)
    {
        var src = Resources.Load<Texture2D>("LofiUI/" + n);
        if (src == null) { Debug.LogWarning("[DotScreenUI] Resources/LofiUI/" + n + " 없음"); return null; }
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = RenderTexture.active;   // Blit 이 active 를 rt 로 바꾸므로 그 전에 잡는다(아니면 Release 경고 5건)
        Graphics.Blit(src, rt); RenderTexture.active = rt;
        var raw = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        raw.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); raw.Apply(false);
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        int w = raw.width, h = raw.height;
        var a = raw.GetPixels32(); var o = new Color32[a.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int r = 0, g = 0, b = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int k = (dx == 0 ? 2 : 1) * (dy == 0 ? 2 : 1);
                        var c = a[((y + dy + h) % h) * w + (x + dx + w) % w];
                        r += c.r * k; g += c.g * k; b += c.b * k;
                    }
                o[y * w + x] = new Color32((byte)(r / 16), (byte)(g / 16), (byte)(b / 16), 255);
            }
        raw.SetPixels32(o); raw.filterMode = FilterMode.Point; raw.wrapMode = TextureWrapMode.Repeat; raw.name = "LofiUI_" + n; raw.Apply(false);
        return raw;
    }

    static float Lum(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;

    /// <summary>
    /// 사진 텍스처 한 칸(s×s)을 잘라 9분할 Tiled 스프라이트로. 가장자리 깊이 = 그 자리 텍스처 밝기(테두리 따라 3칸 평균)로 0..depth 칸 — 같은 사진이 가장자리 모양도 정한다.
    /// 잘린 바로 안쪽 1칸은 12% 어둡게(종이 섬유·나무 마구리) — 빛·그림자를 굽는 것이 아니라 재질 끝의 색.
    /// </summary>
    static Sprite TexBox(Texture2D tex, int ox, int oy, int s, float depth, int border, float contrast)
    {
        var src = tex.GetPixels32(); int tw = tex.width;
        Color32 At(int x, int y) => src[((oy + y) % tex.height) * tw + (ox + x) % tw];
        // 깊이 기준 = 이 칸 밝기의 10%~90% (1차: 한 줄 최소·최대로 잡았더니 흐린 사진에선 거의 0칸 → 1px 테처럼 보였다)
        var all = new List<float>(s * s); float mean = 0f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) { float l = Lum(At(x, y)); all.Add(l); mean += l; }
        all.Sort(); mean /= all.Count;
        float lo = all[all.Count / 10], hi = all[all.Count * 9 / 10];
        float span = Mathf.Max(0.01f, hi - lo);
        var cut = new bool[s, s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(s - 1 - x, s - 1 - y));
                // 테두리 방향을 따라 3칸 평균 밝기 → 0..1
                bool horiz = Mathf.Min(y, s - 1 - y) <= Mathf.Min(x, s - 1 - x);
                float l = 0f;
                for (int k = -2; k <= 2; k++) l += horiz ? Lum(At(Mathf.Clamp(x + k, 0, s - 1), y)) : Lum(At(x, Mathf.Clamp(y + k, 0, s - 1)));
                float n = Mathf.Clamp01((l / 5f - lo) / span);
                int t = Mathf.RoundToInt(n * depth);
                bool corner = Mathf.Min(x, s - 1 - x) + Mathf.Min(y, s - 1 - y) < 2;   // 모서리 1칸 깎기
                cut[x, y] = d < t || corner;
            }
        var t2 = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "LofiUI_Box_" + tex.name };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                if (cut[x, y]) { t2.SetPixel(x, y, Color.clear); continue; }
                bool rim = (x == 0 || cut[x - 1, y]) || (x == s - 1 || cut[x + 1, y]) || (y == 0 || cut[x, y - 1]) || (y == s - 1 || cut[x, y + 1]);
                Color c = At(x, y);
                // 결 대비: 128px 원본이 7단계·진폭 0.12 라 1-2-1 흐림 뒤엔 단색처럼 보였다 → 평균 둘레로 contrast 배(색 비율 유지)
                float lc = (0.299f * c.r + 0.587f * c.g + 0.114f * c.b), k2 = lc > 1e-3f ? Mathf.Clamp(mean + (lc - mean) * contrast, 0f, 1f) / lc : 1f;
                c = new Color(c.r * k2, c.g * k2, c.b * k2, 1f);
                if (rim) c = new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f, 1f);
                t2.SetPixel(x, y, c);
            }
        t2.Apply(false, true);
        return Sprite.Create(t2, new Rect(0, 0, s, s), Vector2.zero, 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    /// <summary>B 말풍선 꼬리 — 1판과 같은 모양, 속은 종이 텍스처, 바깥 1칸만 재질 끝 색(어둡게 12%).</summary>
    static Sprite TexTail(Texture2D tex)
    {
        string[] rows = { "...o...", "..ofo..", ".offfo.", "offfffo" };
        var t = new Texture2D(7, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "LofiUI_Tail" };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 7; x++)
            {
                char ch = rows[y][x];
                Color c = tex.GetPixel(20 + x, 20 + y);
                t.SetPixel(x, y, ch == '.' ? Color.clear : ch == 'o' ? new Color(c.r * 0.88f, c.g * 0.88f, c.b * 0.88f, 1f) : c);
            }
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, 7, 4), Vector2.zero, 100f);
    }

    void EnsureStyleAssets()
    {
        if (styleInit) return;
        styleInit = true;
        nowBox = bubble.sprite; nowDark = chipBox.sprite; nowKey = chipKey.sprite; nowTail = tail.sprite; nowWarn = warnBox.sprite;
        texPaper = LoadSoft("cream"); texWood = LoadSoft("walnut"); texSlate = LoadSoft("slate");
        if (texPaper != null) { paperBox = TexBox(texPaper, 8, 8, 64, 4.5f, 7, 2.6f); paperTail = TexTail(texPaper); }
        if (texWood != null) woodBox = TexBox(texWood, 30, 40, 48, 1.6f, 4, 1.6f);
        // 세상과 같은 해·그늘·안개 값을 쓰려고 장면의 LofiSurface 머티리얼 하나를 기준으로 매 프레임 따라간다(시간대가 바뀌면 같이 바뀜).
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var m = r.sharedMaterial;
            if (m == null || m.shader == null || m.shader.name != "PitCrew/LofiPilot/Surface") continue;
            lightRef = m;
            if (r.name.ToLower().Contains("depot")) break;
        }
    }

    Material NewPrint(string n)
    {
        var sh = Resources.Load<Shader>("LofiUI/LofiPrint");
        if (sh == null) sh = Shader.Find("PitCrew/LofiPilot/Print");
        return sh != null ? new Material(sh) { name = n } : null;
    }

    Material NewSurface(string n, Texture2D tex, Vector2 tiling)
    {
        var sh = Shader.Find("PitCrew/LofiPilot/Surface");
        if (sh == null) return null;
        var m = new Material(sh) { name = n, mainTexture = tex, mainTextureScale = tiling };
        return m;
    }

    void SyncLight(Material m)
    {
        if (m == null || lightRef == null) return;
        m.SetColor("_Ambient", lightRef.GetColor("_Ambient"));
        m.SetColor("_FogColor", lightRef.GetColor("_FogColor"));
        m.SetFloat("_FogStart", lightRef.GetFloat("_FogStart"));
        m.SetFloat("_FogEnd", lightRef.GetFloat("_FogEnd"));
        m.SetFloat("_FaceShade", lightRef.GetFloat("_FaceShade"));
    }

    /// <summary>안을 바꿀 때 한 번: 상자 스프라이트(B·A 는 사진 종이·나무, Now 는 1판 단색) 바꿔 끼우기.</summary>
    void ApplyStyleSprites()
    {
        bool tex = UiStyle !=UIStyle.Now && paperBox != null && woodBox != null;
        void Set(Image im, Sprite s) { if (im == null) return; im.sprite = s; im.type = tex ? Image.Type.Tiled : Image.Type.Sliced; im.pixelsPerUnitMultiplier = 1f; }
        Set(bubble, tex ? paperBox : nowBox);
        Set(slip, tex ? paperBox : nowBox);
        Set(chipKey, tex ? paperBox : nowKey);
        foreach (var im in new[] { chipBox, nameTab, destChip, distBox, moneyBox }) Set(im, tex ? woodBox : nowDark);
        tail.sprite = tex && paperTail != null ? paperTail : nowTail;
        appliedStyle = UiStyle;
    }

    // ── A: 작은 RT 에 찍는 인쇄대(장면 먼 곳, 레이어 31, 직교 카메라) ──
    const int PrintLayer = 31;
    class PrintRig
    {
        public GameObject go; public Camera cam; public RectTransform canvasRt; public RenderTexture rt;
        public readonly List<(GameObject src, GameObject dst, RectTransform[] sr, RectTransform[] dr)> pairs = new();
        public Vector2Int size;   // 캔버스 단위(화면 도트 1칸). RT 는 size × ss
        public int ss = 2;
    }

    PrintRig MakeRig(string n, Vector3 at, int ss)
    {
        var r = new PrintRig { ss = ss };
        r.go = new GameObject("LofiPrint_" + n);
        r.go.transform.position = at;
        var cgo = new GameObject("Cam"); cgo.transform.SetParent(r.go.transform, false); cgo.transform.localPosition = new Vector3(0, 0, -10);
        r.cam = cgo.AddComponent<Camera>();
        r.cam.orthographic = true; r.cam.clearFlags = CameraClearFlags.SolidColor; r.cam.backgroundColor = new Color(0, 0, 0, 0);
        r.cam.cullingMask = 1 << PrintLayer; r.cam.nearClipPlane = 0.1f; r.cam.farClipPlane = 50f; r.cam.allowHDR = false; r.cam.allowMSAA = false;
        r.cam.enabled = false;
        var kgo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        kgo.layer = PrintLayer;
        kgo.transform.SetParent(r.go.transform, false);
        var cv = kgo.GetComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; cv.worldCamera = r.cam;
        r.canvasRt = (RectTransform)kgo.transform; r.canvasRt.pivot = new Vector2(0.5f, 0.5f);
        return r;
    }

    void RigSize(PrintRig r, int w, int h)
    {
        w = Mathf.Max(8, (w + 7) / 8 * 8); h = Mathf.Max(8, (h + 7) / 8 * 8);   // 8 단위로 — 글자 찍히는 동안 RT 를 매 프레임 다시 만들지 않게
        if (r.rt != null && r.size.x == w && r.size.y == h) return;
        if (r.rt != null) { r.cam.targetTexture = null; Destroy(r.rt); }   // Release() 즉시 호출은 렌더 중 active 경고 5건(21:05) → 프레임 끝 Destroy 에 맡김
        // ss 배로 찍는다: 1배·최근접이면 반 픽셀 어긋난 Galmuri 획이 빠졌다(21:00 실측). 셰이더가 선형으로 한 번 읽어 화면 도트로 모은다.
        r.rt = new RenderTexture(w * r.ss, h * r.ss, 16, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear, name = "LofiPrintRT" };
        r.size = new Vector2Int(w, h);
        r.cam.targetTexture = r.rt; r.cam.orthographicSize = h * 0.5f;
        r.canvasRt.sizeDelta = new Vector2(w, h);
    }

    /// <summary>화면 UI 요소(src)를 인쇄대에 복제해 두고 매 프레임 상태(글·색·자리·켜짐)를 옮긴다. offset = 화면 좌표 → RT 좌표.</summary>
    void RigCopy(PrintRig r, GameObject src, Vector2 offset)
    {
        int idx = r.pairs.FindIndex(p => p.src == src);
        if (idx < 0)
        {
            var dst = Instantiate(src, r.canvasRt, false);
            foreach (var t in dst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PrintLayer;
            foreach (var ps in dst.GetComponentsInChildren<PixelSnapText>(true)) ps.root = r.canvasRt;
            r.pairs.Add((src, dst, src.GetComponentsInChildren<RectTransform>(true), dst.GetComponentsInChildren<RectTransform>(true)));
            idx = r.pairs.Count - 1;
        }
        var pr = r.pairs[idx];
        for (int i = 0; i < pr.sr.Length && i < pr.dr.Length; i++)
        {
            RectTransform a = pr.sr[i], b = pr.dr[i];
            bool on = i == 0 || a.gameObject.activeSelf;
            if (b.gameObject.activeSelf != on) b.gameObject.SetActive(on);
            b.anchorMin = a.anchorMin; b.anchorMax = a.anchorMax; b.pivot = a.pivot; b.sizeDelta = a.sizeDelta;
            b.anchoredPosition = i == 0 ? a.anchoredPosition + offset : a.anchoredPosition;
            if (a.TryGetComponent<Text>(out var ta) && b.TryGetComponent<Text>(out var tb))
            { tb.supportRichText = ta.supportRichText; if (tb.text != ta.text) tb.text = ta.text; tb.color = ta.color; }
            else if (a.TryGetComponent<Image>(out var ia) && b.TryGetComponent<Image>(out var ib))
            { ib.sprite = ia.sprite; ib.type = ia.type; ib.color = ia.color; }
        }
    }

    void RigHideAll(PrintRig r) { foreach (var p in r.pairs) if (p.dst.activeSelf) p.dst.SetActive(false); }

    /// <summary>uv 사각형(RT 일부)을 쓰는 판 메시. viewerAtMinusZ = 보는 사람이 -z 쪽에 있음(오른쪽 = +x).</summary>
    static void QuadMesh(Mesh m, float w, float h, Rect uv, bool viewerAtMinusZ, Vector2 pivot)
    {
        float x0 = -w * pivot.x, x1 = w * (1 - pivot.x), y0 = -h * pivot.y, y1 = h * (1 - pivot.y);
        m.Clear();
        m.vertices = new[] { new Vector3(x0, y0, 0), new Vector3(x1, y0, 0), new Vector3(x0, y1, 0), new Vector3(x1, y1, 0) };
        float u0 = viewerAtMinusZ ? uv.xMin : uv.xMax, u1 = viewerAtMinusZ ? uv.xMax : uv.xMin;
        m.uv = new[] { new Vector2(u0, uv.yMin), new Vector2(u1, uv.yMin), new Vector2(u0, uv.yMax), new Vector2(u1, uv.yMax) };
        var n = viewerAtMinusZ ? Vector3.back : Vector3.forward;
        m.normals = new[] { n, n, n, n };
        m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        m.RecalculateBounds();
    }

    GameObject MakeQuad(string n, Material mat, out Mesh mesh)
    {
        var go = new GameObject(n, typeof(MeshFilter), typeof(MeshRenderer));
        mesh = new Mesh { name = n };
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = true;
        return go;
    }

    PrintRig rigBubble, rigSlip, rigBoard;
    GameObject bubbleQuad, slipQuad, boardQuad;
    Mesh bubbleMesh, slipMesh, boardMesh;
    float bubbleMpp; string bubbleFor;
    float slipUpAt = -99f; bool slipWasUp;

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
