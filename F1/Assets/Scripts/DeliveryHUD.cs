using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배달 목표 화살표 HUD (UGUI). 세션 13 전환.
/// 목표가 화면 안에 있으면 목표 위치에, 밖에 있으면 화면 가장자리에 clamp.
/// DeliveryProgressHUD가 카운트/라벨 담당 — 여기서는 순수 화살표만.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class DeliveryHUD : MonoBehaviour
{
    [SerializeField] DeliveryManager manager;
    [SerializeField] Camera cam;
    [SerializeField] float edgePadding = 80f;
    [SerializeField] float arrowSize = 72f;

    RectTransform rt;
    RectTransform arrowRt;
    Image arrowImg;

    void Awake()
    {
        if (manager == null) manager = FindFirstObjectByType<DeliveryManager>();
        if (cam == null) cam = Camera.main;
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        BuildIfNeeded();
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[DeliveryHUD] Canvas 밖. UIRoot 직속으로.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        // rt = 전체 화면 덮는 투명 컨테이너. 자식 Arrow가 이동.
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        if (arrowImg == null)
        {
            var go = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            arrowImg = go.GetComponent<Image>();
            arrowImg.raycastTarget = false;

            // Kenney UI pack의 arrow_basic_n — 북쪽 향한 화살표. 북쪽 기준(angle 0)에서 회전.
            var arrowSprite = LoadSprite("Assets/UI/kenney_ui-pack/PNG/Yellow/Default/arrow_basic_n.png");
            if (arrowSprite != null) arrowImg.sprite = arrowSprite;
            else arrowImg.color = UITheme.Active != null ? UITheme.Active.colorAccent : new Color(1f, 0.85f, 0.28f);

            arrowRt = arrowImg.rectTransform;
            arrowRt.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRt.pivot = new Vector2(0.5f, 0.5f);
            arrowRt.sizeDelta = new Vector2(arrowSize, arrowSize);
        }
    }

#if UNITY_EDITOR
    static Sprite LoadSprite(string path) => UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
    static Sprite LoadSprite(string path) => null;
#endif

    void Update()
    {
        if (manager == null || cam == null || arrowImg == null) return;

        Vector3 worldTarget = Vector3.zero;
        bool hasTarget = false;
        var theme = UITheme.Active;
        Color tint = theme != null ? theme.colorAccent : new Color(1f, 0.85f, 0.28f);

        if (manager.Finished)
        {
            arrowImg.enabled = false;
            return;
        }
        else if (manager.Returning)
        {
            tint = theme != null ? theme.colorWarn : new Color(1f, 0.8f, 0.3f);
            if (manager.ReturnTarget != null) { worldTarget = manager.ReturnTarget.position; hasTarget = true; }
        }
        else
        {
            var t = manager.CurrentTarget;
            if (t != null) { worldTarget = t.transform.position; hasTarget = true; }
        }

        arrowImg.enabled = hasTarget;
        if (!hasTarget) return;
        arrowImg.color = tint;

        // UGUI 화면 좌표 (bottom-left origin)
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
            angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f; // arrow_n이 북쪽 향이라 -90 offset
        }
        else
        {
            finalScreenPos = screenPos;
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        }

        // Screen → Canvas anchored position 변환
        // rt가 전체 화면 덮는 Canvas scaled space. arrow는 center 기준 anchored.
        var canvas = GetComponentInParent<Canvas>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, finalScreenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPos);
        arrowRt.anchoredPosition = localPos;
        arrowRt.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
