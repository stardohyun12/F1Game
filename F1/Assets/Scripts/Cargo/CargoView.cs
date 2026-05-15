using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 트렁크 CCTV HUD. SimCam의 RenderTexture를 RawImage로 표시.
/// 자가구성 (FuelHUD 패턴). UIRoot 아래 자동 재부착.
///
/// 2026-05-15: 도현 피드백 — 헤더 드래그로 이동, 우하단 핸들로 리사이즈.
/// PlayerPrefs로 위치/크기 저장. SerializeField 제거 (직렬화 함정 회피).
/// </summary>
[DisallowMultipleComponent]
public class CargoView : MonoBehaviour
{
    [SerializeField] RenderTexture renderTexture;

    // 기본값 — PlayerPrefs에 저장된 값 없으면 사용
    static readonly Vector2 defaultAnchoredPos = new Vector2(-36, -220);
    static readonly Vector2 defaultSize = new Vector2(720, 500);
    static readonly Vector2 minSize = new Vector2(280, 200);
    static readonly Vector2 maxSize = new Vector2(1400, 900);

    const string PrefKey = "CargoView.Layout.v1";

    RectTransform rt;
    Image bg;
    RawImage view;
    TMP_Text label;
    CanvasGroup group;
    RectTransform headerHandle;
    RectTransform resizeHandle;

    void Awake()
    {
        EnsureCanvasParent();
        rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        TryLoadRT();
        BuildIfNeeded();
        LoadLayout();
    }

    void TryLoadRT()
    {
        if (renderTexture == null)
            renderTexture = Resources.Load<RenderTexture>("CargoRT");
    }

    void EnsureCanvasParent()
    {
        if (GetComponentInParent<Canvas>() != null) return;
        if (transform.parent == null) { transform.SetParent(UIRoot.Ensure(), false); return; }
        Debug.LogWarning("[CargoView] Canvas 밖에 있음.");
        enabled = false;
    }

    void BuildIfNeeded()
    {
        var theme = UITheme.Active;
        rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = defaultAnchoredPos;
        rt.sizeDelta = defaultSize;

        if (bg == null)
        {
            var go = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            bg = go.GetComponent<Image>();
            bg.raycastTarget = true; // raycastTarget=true 라야 헤더/핸들 이벤트 안 가려짐 (BG는 raycast off로 두는게 일반적인데 헤더가 BG보다 작으므로 BG는 raycast on 둬도 안전).
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
            if (renderTexture != null) view.texture = renderTexture;
            var r = view.rectTransform;
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1);
            r.offsetMin = new Vector2(10, 10); r.offsetMax = new Vector2(-10, -36);
        }

        if (label == null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Top;
            label.fontSize = 20; label.fontStyle = FontStyles.Bold;
            if (theme != null) { if (theme.fontPrimary != null) label.font = theme.fontPrimary; label.color = theme.colorTextMuted; }
            label.text = "TRUNK CAM";
            var r = label.rectTransform;
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 1);
            r.sizeDelta = new Vector2(0, 28);
            r.anchoredPosition = new Vector2(0, -6);
        }

        // 헤더 드래그 영역 — 상단 28px 띠. 투명 Image + DragHandler.
        if (headerHandle == null)
        {
            var go = new GameObject("HeaderHandle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.04f); // 거의 투명, raycast용
            img.raycastTarget = true;
            headerHandle = go.GetComponent<RectTransform>();
            headerHandle.anchorMin = new Vector2(0, 1); headerHandle.anchorMax = new Vector2(1, 1);
            headerHandle.pivot = new Vector2(0.5f, 1);
            headerHandle.sizeDelta = new Vector2(-32, 28); // 우상단 28x28은 close zone 대신 비움 (오버랩 회피)
            headerHandle.anchoredPosition = Vector2.zero;
            var drag = go.AddComponent<CargoViewDragHandle>();
            drag.target = this;
        }

        // 우하단 리사이즈 핸들 — 24x24
        if (resizeHandle == null)
        {
            var go = new GameObject("ResizeHandle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.35f);
            img.raycastTarget = true;
            resizeHandle = go.GetComponent<RectTransform>();
            resizeHandle.anchorMin = new Vector2(1, 0); resizeHandle.anchorMax = new Vector2(1, 0);
            resizeHandle.pivot = new Vector2(1, 0);
            resizeHandle.sizeDelta = new Vector2(24, 24);
            resizeHandle.anchoredPosition = Vector2.zero;
            var rs = go.AddComponent<CargoViewResizeHandle>();
            rs.target = this;
        }
    }

    void Update()
    {
        if (view == null) return;
        if (view.texture == null)
        {
            TryLoadRT();
            if (renderTexture != null) view.texture = renderTexture;
        }
        // 트렁크 CCTV는 런 중에만 표시 (주문/포장/정산 화면에선 숨김).
        if (group != null)
        {
            var run = Services.Run as RunController;
            group.alpha = (run != null && run.RunActive) ? 1f : 0f;
            // alpha=0이면 raycast도 꺼서 뒤 UI 가리지 않게
            group.blocksRaycasts = group.alpha > 0.5f;
            group.interactable = group.alpha > 0.5f;
        }
    }

    // ── Drag/Resize 외부 API ─────────────────────────────────────────
    public void ApplyDrag(Vector2 deltaScreen)
    {
        rt.anchoredPosition += deltaScreen;
        SaveLayout();
    }

    public void ApplyResize(Vector2 deltaScreen)
    {
        var s = rt.sizeDelta + new Vector2(deltaScreen.x, -deltaScreen.y);
        s.x = Mathf.Clamp(s.x, minSize.x, maxSize.x);
        s.y = Mathf.Clamp(s.y, minSize.y, maxSize.y);
        rt.sizeDelta = s;
        SaveLayout();
    }

    void SaveLayout()
    {
        var pos = rt.anchoredPosition;
        var size = rt.sizeDelta;
        PlayerPrefs.SetString(PrefKey, $"{pos.x},{pos.y},{size.x},{size.y}");
    }

    void LoadLayout()
    {
        var s = PlayerPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(s)) return;
        var parts = s.Split(',');
        if (parts.Length != 4) return;
        if (!float.TryParse(parts[0], out var px)) return;
        if (!float.TryParse(parts[1], out var py)) return;
        if (!float.TryParse(parts[2], out var sx)) return;
        if (!float.TryParse(parts[3], out var sy)) return;
        rt.anchoredPosition = new Vector2(px, py);
        rt.sizeDelta = new Vector2(
            Mathf.Clamp(sx, minSize.x, maxSize.x),
            Mathf.Clamp(sy, minSize.y, maxSize.y));
    }
}

/// <summary>헤더 드래그 → CargoView.ApplyDrag.</summary>
public class CargoViewDragHandle : MonoBehaviour, IDragHandler
{
    public CargoView target;
    public void OnDrag(PointerEventData e)
    {
        if (target != null) target.ApplyDrag(e.delta);
    }
}

/// <summary>우하단 핸들 드래그 → CargoView.ApplyResize.</summary>
public class CargoViewResizeHandle : MonoBehaviour, IDragHandler
{
    public CargoView target;
    public void OnDrag(PointerEventData e)
    {
        if (target != null) target.ApplyResize(e.delta);
    }
}
