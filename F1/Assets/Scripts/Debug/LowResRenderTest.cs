using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 저해상도 렌더 시험 (2026-10-23, PD 과제 1527 — 방식 B). 기본은 꺼져 있다.
/// 켜면 이 카메라를 세로 targetHeight 픽셀 RenderTexture(Point 필터)로 그리고, 맨 아래 순서의 오버레이 캔버스에 꽉 채워 올린다
/// = 화면에 Nearest 로 확대. 게임 UI(ScreenSpaceOverlay 캔버스)는 그 위에 원 해상도로 그대로 그려진다.
/// URP 의 Render Scale 은 RenderTexture 카메라에도 곱해지므로, RT 를 (targetHeight ÷ renderScale) 로 잡아 실제 렌더 해상도가 targetHeight 가 되게 한다.
/// 팔레트·디더 같은 색 후처리는 하지 않는다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LowResRenderTest : MonoBehaviour
{
    [Tooltip("실제 렌더 세로 해상도(270 / 360 / 540). 가로는 화면 비율.")]
    public int targetHeight = 270;

    [Tooltip("켜면 화면 = RT × 정수배(k = 화면 세로 ÷ targetHeight 반올림). RT 를 화면÷k 로 잡고 이미지를 k배 크기로 가운데 둔다 → 픽셀이 고르게 Nearest(넘치는 건 k−1px 이하 잘림).")]
    public bool integerScale;

    [Tooltip("5c(D-149): 켜면 RT 를 바이리니어로 읽고 presentMaterial(PitCrew/Look/Present — 부드러운 확대·약한 블룸·옅은 헤이즈)로 화면에 올린다. 끄면 Nearest.")]
    public bool smooth;
    public Material presentMaterial;
    [Tooltip("RT 멀티샘플(1/2/4/8). 저해상도 가장자리 계단이 움직일 때 기어가는 것(크롤링)을 줄인다.")]
    public int msaa = 1;
    /// <summary>D-236 Nearest(smooth 꺼짐)일 때 화면에 올리는 머티리얼(없으면 그냥 Nearest). SunsetLook 이 노을일 때만 역광 테 머티리얼을 꽂는다.</summary>
    [System.NonSerialized] public Material overlay;

    Camera cam;
    RenderTexture rt;
    GameObject canvasGO;
    RawImage image;
    int builtW, builtH, builtAA;
    Material builtMat;

    Material WantMat => smooth ? presentMaterial : overlay;

    /// <summary>지금 실제로 그리는 해상도(가로×세로).</summary>
    public Vector2Int RenderSize { get; private set; }

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        canvasGO = new GameObject("LowResRenderTest_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -32000; // 게임 UI 보다 먼저(아래에) 그린다
        var imgGO = new GameObject("Image", typeof(RectTransform));
        imgGO.transform.SetParent(canvasGO.transform, false);
        var r = imgGO.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        image = imgGO.AddComponent<RawImage>();
        image.raycastTarget = false;
        Rebuild();
    }

    void OnDisable()
    {
        if (cam != null) cam.targetTexture = null;
        if (canvasGO != null) Destroy(canvasGO);
        if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        builtW = builtH = builtAA = 0;
    }

    void LateUpdate()
    {
        Size(out int w, out int h, out _);
        bool wantSmooth = smooth && presentMaterial != null;
        if (w != RenderSize.x || h != RenderSize.y || Mathf.ClosestPowerOfTwo(Mathf.Clamp(msaa, 1, 8)) != builtAA || (image != null && (image.material == presentMaterial) != wantSmooth)) Rebuild();
        else if (image != null && builtMat != WantMat) { builtMat = WantMat; image.material = builtMat; }
    }

    /// <summary>지금 화면에 올라가는 것과 같은 방법으로 RT 를 dst 에 확대한다(캡처·깜빡임 측정용).</summary>
    public void Present(RenderTexture dst)
    {
        if (rt == null) return;
        var f = rt.filterMode;
        if (smooth && presentMaterial != null) { rt.filterMode = FilterMode.Bilinear; Graphics.Blit(rt, dst, presentMaterial); }
        else { rt.filterMode = FilterMode.Point; if (overlay != null) Graphics.Blit(rt, dst, overlay); else Graphics.Blit(rt, dst); }
        rt.filterMode = f;
    }

    void Size(out int w, out int h, out int k)
    {
        int sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
        // 과제 0246-4: 1080 화면에 720 이면 반올림 k=2 → 540 으로 떨어진다. 화면이 목표의 정수배일 때만 정수배, 아니면 목표 높이 그대로(부드러운 확대).
        if (integerScale && sh % Mathf.Max(16, targetHeight) == 0)
        {
            k = Mathf.Max(1, Mathf.RoundToInt(sh / (float)Mathf.Max(16, targetHeight)));
            w = Mathf.Max(16, Mathf.CeilToInt(sw / (float)k)); h = Mathf.Max(16, Mathf.CeilToInt(sh / (float)k));
            return;
        }
        k = 0;
        h = Mathf.Max(16, targetHeight);
        w = Mathf.Max(16, Mathf.RoundToInt(h * (float)sw / sh));
    }

    void Rebuild()
    {
        Size(out int w, out int h, out int k);
        RenderSize = new Vector2Int(w, h);
        if (image != null)
        {
            var r = image.rectTransform;
            if (k > 0) { r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = new Vector2(w * k, h * k); r.anchoredPosition = Vector2.zero; }
            else { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        }
        float scale = 1f;
        // PitCrew asmdef 는 URP 를 참조하지 않는다 → renderScale 은 리플렉션으로 읽는다.
        var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        var prop = rp != null ? rp.GetType().GetProperty("renderScale") : null;
        if (prop != null) scale = Mathf.Max(0.1f, (float)prop.GetValue(rp));
        int rw = Mathf.RoundToInt(w / scale), rh = Mathf.RoundToInt(h / scale);
        int aa = Mathf.ClosestPowerOfTwo(Mathf.Clamp(msaa, 1, 8));
        builtMat = WantMat;
        if (image != null) image.material = builtMat;
        if (rt != null && rw == builtW && rh == builtH && aa == builtAA) { rt.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point; return; }
        if (cam != null) cam.targetTexture = null;
        if (rt != null) { rt.Release(); Destroy(rt); }
        rt = new RenderTexture(rw, rh, 24) { name = "LowResRenderTest_RT", filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point, antiAliasing = aa };
        rt.Create();
        builtW = rw; builtH = rh; builtAA = aa;
        Debug.Log($"[LowResRenderTest] RT {rw}×{rh} (렌더 {w}×{h}, 화면 {Screen.width}×{Screen.height}, k={k}, smooth={smooth}, filter={rt.filterMode})");
        cam.targetTexture = rt;
        if (image != null) image.texture = rt;
    }
}
