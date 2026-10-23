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

    Camera cam;
    RenderTexture rt;
    GameObject canvasGO;
    RawImage image;
    int builtW, builtH;

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
        builtW = builtH = 0;
    }

    void LateUpdate()
    {
        int h = Mathf.Max(16, targetHeight);
        int w = Mathf.Max(16, Mathf.RoundToInt(h * (float)Screen.width / Mathf.Max(1, Screen.height)));
        if (w != RenderSize.x || h != RenderSize.y) Rebuild();
    }

    void Rebuild()
    {
        int h = Mathf.Max(16, targetHeight);
        int w = Mathf.Max(16, Mathf.RoundToInt(h * (float)Screen.width / Mathf.Max(1, Screen.height)));
        RenderSize = new Vector2Int(w, h);
        float scale = 1f;
        // PitCrew asmdef 는 URP 를 참조하지 않는다 → renderScale 은 리플렉션으로 읽는다.
        var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        var prop = rp != null ? rp.GetType().GetProperty("renderScale") : null;
        if (prop != null) scale = Mathf.Max(0.1f, (float)prop.GetValue(rp));
        int rw = Mathf.RoundToInt(w / scale), rh = Mathf.RoundToInt(h / scale);
        if (rt != null && rw == builtW && rh == builtH) return;
        if (cam != null) cam.targetTexture = null;
        if (rt != null) { rt.Release(); Destroy(rt); }
        rt = new RenderTexture(rw, rh, 24) { name = "LowResRenderTest_RT", filterMode = FilterMode.Point };
        rt.Create();
        builtW = rw; builtH = rh;
        cam.targetTexture = rt;
        if (image != null) image.texture = rt;
    }
}
