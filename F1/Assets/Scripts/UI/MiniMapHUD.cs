using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 HUD (W2 채널 8 — 지도/내비).
/// 좌상단에 top-down 미니맵 표시. 자가구성 (CargoView 패턴):
/// 런타임에 직교 카메라 + RenderTexture를 만들어 RawImage에 띄운다.
///
/// 카메라는 차(Services.Car)를 따라가며 수직으로 내려다봄 (정북향 고정 — 회전 안 함).
/// 월드를 그대로 비추므로 도로/건물/배달 마커 기둥/차가 자연스럽게 점으로 보임
/// (별도 dot 시스템 없이 — Kenney-only 규칙 + 단순성).
///
/// 런 중에만 표시 (RunActive 게이팅) — IntegrityHUD/CargoView와 일관 (세션 24).
/// </summary>
[DisallowMultipleComponent]
public class MiniMapHUD : MonoBehaviour
{
    [Header("Layout (좌상단 앵커)")]
    [SerializeField] Vector2 anchoredPos = new Vector2(36, -36);
    [SerializeField] Vector2 size = new Vector2(240, 240);

    [Header("Camera")]
    [Tooltip("미니맵 카메라가 차 위로 올라가는 높이 (m).")]
    [SerializeField] float camHeight = 60f;
    [Tooltip("직교 투영 크기 (절반 높이, m). 클수록 넓게 보임. 맵이 78x54m라 32면 64m 창.")]
    [SerializeField] float orthoSize = 34f;
    [Tooltip("RenderTexture 한 변 해상도 (px).")]
    [SerializeField] int rtResolution = 256;

    RectTransform rt;
    Image bg;
    RawImage view;
    CanvasGroup group;
    Camera mapCam;
    RenderTexture mapRT;
    Transform carT;

    void Awake()
    {
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        BuildIfNeeded();
        EnsureCamera();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[MiniMapHUD] Canvas 밖에 있음.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (bg == null)
        {
            var go = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            bg = go.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = theme != null ? theme.colorPanel : new Color(0.12f, 0.14f, 0.18f, 0.92f);
            if (theme?.panelDark != null) { bg.sprite = theme.panelDark; bg.type = Image.Type.Sliced; }
            var r = bg.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        if (view == null)
        {
            var go = new GameObject("View", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(transform, false);
            view = go.GetComponent<RawImage>();
            view.raycastTarget = false;
            var r = view.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1);
            r.offsetMin = new Vector2(8, 8); r.offsetMax = new Vector2(-8, -8);
        }
    }

    void EnsureCamera()
    {
        if (mapCam != null) return;

        mapRT = new RenderTexture(rtResolution, rtResolution, 16) { name = "MiniMapRT" };

        var camGO = new GameObject("MiniMapCamera");
        mapCam = camGO.AddComponent<Camera>();
        mapCam.orthographic = true;
        mapCam.orthographicSize = orthoSize;
        mapCam.clearFlags = CameraClearFlags.SolidColor;
        mapCam.backgroundColor = new Color(0.10f, 0.12f, 0.14f, 1f);
        mapCam.cullingMask = ~0;
        mapCam.depth = -10;                 // 메인보다 먼저. targetTexture 있어 화면엔 안 그림.
        mapCam.nearClipPlane = 1f;
        mapCam.farClipPlane = camHeight + 30f;
        mapCam.targetTexture = mapRT;
        camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 수직 하향, 정북향 고정

        if (view != null) view.texture = mapRT;
    }

    void LateUpdate()
    {
        // 런 중에만 표시 (주문/포장/정산 화면에선 숨김).
        var run = Services.Run as RunController;
        bool show = run != null && run.RunActive;
        if (group != null) group.alpha = show ? 1f : 0f;
        if (!show) return;

        if (carT == null)
        {
            var car = Services.Car;
            if (car != null) carT = car.Transform;
        }
        if (carT != null && mapCam != null)
        {
            Vector3 p = carT.position;
            mapCam.transform.position = new Vector3(p.x, p.y + camHeight, p.z);
        }
    }

    void OnDestroy()
    {
        // RT/카메라 수명 짝 맞춤 (Material 누수 방지 패턴).
        if (mapCam != null) Destroy(mapCam.gameObject);
        if (mapRT != null) { mapRT.Release(); Destroy(mapRT); }
    }
}
