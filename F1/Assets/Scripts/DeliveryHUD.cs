using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배달 화살표 HUD. 세션 13 — UITheme의 arrowNorth sprite 사용.
/// 화면 안이면 타겟 위치, 밖이면 가장자리 clamp + 회전.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class DeliveryHUD : MonoBehaviour
{
    [SerializeField] DeliveryManager manager;
    [SerializeField] Camera cam;
    [SerializeField] float edgePadding = 80f;
    [SerializeField] float arrowSize = 80f;

    RectTransform rt;
    RectTransform arrowRt;
    Image arrowImg;
    Canvas cachedCanvas;  // Phase 6 perf(B1): Update의 GetComponentInParent 매 프레임 호출 → Awake 1회 캐시.

    void Awake()
    {
        // Phase 1: Services 경유. SerializeField 비어있으면 Services에서.
        if (manager == null) manager = Services.Delivery as DeliveryManager;
        if (cam == null) cam = Camera.main;
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        cachedCanvas = GetComponentInParent<Canvas>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[DeliveryHUD] Canvas 밖.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        var theme = UITheme.Active;
        if (arrowImg == null)
        {
            var go = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            arrowImg = go.GetComponent<Image>();
            arrowImg.raycastTarget = false;
            arrowImg.color = Color.white;
            if (theme?.arrowNorth != null) arrowImg.sprite = theme.arrowNorth;
            arrowRt = arrowImg.rectTransform;
            arrowRt.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRt.pivot = new Vector2(0.5f, 0.5f);
            arrowRt.sizeDelta = new Vector2(arrowSize, arrowSize);
        }
    }

    void Update()
    {
        if (manager == null || cam == null || arrowImg == null) return;

        Vector3 worldTarget = Vector3.zero;
        bool hasTarget = false;

        if (manager.Finished)
        {
            arrowImg.enabled = false;
            return;
        }
        else if (manager.Returning)
        {
            if (manager.ReturnTarget != null) { worldTarget = manager.ReturnTarget.position; hasTarget = true; }
        }
        else
        {
            var t = manager.CurrentTarget;
            if (t != null) { worldTarget = t.transform.position; hasTarget = true; }
        }

        arrowImg.enabled = hasTarget;
        if (!hasTarget) return;

        Vector3 sp = cam.WorldToScreenPoint(worldTarget);
        bool behind = sp.z < 0;
        if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 screenPos = new Vector2(sp.x, sp.y);
        bool offScreen = behind || sp.x < 0 || sp.x > Screen.width || sp.y < 0 || sp.y > Screen.height;

        Vector2 dir = (screenPos - center);
        Vector2 finalScreenPos;
        float angle;

        if (offScreen)
        {
            Vector2 d = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.up;
            float maxX = Screen.width * 0.5f - edgePadding;
            float maxY = Screen.height * 0.5f - edgePadding;
            float scale = Mathf.Min(maxX / Mathf.Max(0.001f, Mathf.Abs(d.x)), maxY / Mathf.Max(0.001f, Mathf.Abs(d.y)));
            finalScreenPos = center + d * scale;
            angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
        }
        else
        {
            finalScreenPos = screenPos;
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        }

        var canvas = cachedCanvas;
        if (canvas == null) return;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, finalScreenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPos);
        arrowRt.anchoredPosition = localPos;
        arrowRt.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
