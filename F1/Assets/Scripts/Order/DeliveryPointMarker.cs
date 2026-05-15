using UnityEngine;
using TMPro;

/// <summary>
/// 배달지 시각 마커 (런타임 가시). 기둥 + 상단 구체.
/// OrderBoard가 호버 시 색/투명도 제어.
/// 채널 8 (2026-05-15): RoutePlanScreen이 SetRouteOrder/ClearRouteOrder로 순번 배지 표시.
/// </summary>
[RequireComponent(typeof(DeliveryPoint))]
public class DeliveryPointMarker : MonoBehaviour
{
    [Header("Pillar")]
    [SerializeField] private float pillarHeight = 6f;
    [SerializeField] private float pillarRadius = 0.35f;

    [Header("Sphere")]
    [SerializeField] private float sphereRadius = 1.0f;

    [Header("Colors")]
    [SerializeField] private Color baseColor = new Color(0.4f, 1f, 0.5f);
    [SerializeField] private float dimAlpha = 0.25f;

    GameObject pillar;
    GameObject sphere;
    Material pillarMat;
    Material sphereMat;
    DeliveryPoint dp;
    bool useAutoState = true; // false면 OrderBoard가 외부 제어

    // 채널 8 — 경로 순번 배지 (RoutePlanScreen용)
    GameObject orderBadge;
    TextMeshPro orderBadgeText;

    /// <summary>채널 8 — OrderJob.destinationLabels와 매칭용. DeliveryPoint.Label 노출.</summary>
    public string Label => dp != null ? dp.Label : null;

    void Awake()
    {
        dp = GetComponent<DeliveryPoint>();
        BuildVisuals();
    }

    void Update()
    {
        // 런 중 상태 자동 반영 (OrderBoard가 close에서 SetExternalControl(false) 호출)
        if (!useAutoState || dp == null) return;

        if (dp.IsCompleted)
        {
            ApplyColor(new Color(0.4f, 0.4f, 0.4f), 0.4f);
        }
        else if (dp.IsActive)
        {
            // 현재 목표 — 밝게 펄스
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f);
            ApplyColor(baseColor, 1f, pulse);
        }
        else
        {
            // 대기 중 (이번 런에 포함됐지만 아직 차례 안 됨)
            ApplyColor(baseColor, 0.85f, 0.6f);
        }
    }

    /// <summary>OrderBoard 가 호버 중일 때 직접 제어. false 면 자동 상태 반영.</summary>
    public void SetExternalControl(bool external)
    {
        useAutoState = !external;
    }

    void BuildVisuals()
    {
        // Pillar
        pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "Pillar";
        pillar.transform.SetParent(transform, false);
        pillar.transform.localPosition = new Vector3(0f, pillarHeight * 0.5f, 0f);
        pillar.transform.localScale = new Vector3(pillarRadius * 2f, pillarHeight * 0.5f, pillarRadius * 2f);
        var pcol = pillar.GetComponent<Collider>();
        if (pcol != null) Destroy(pcol);
        var prend = pillar.GetComponent<Renderer>();
        pillarMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        prend.sharedMaterial = pillarMat;

        // Sphere
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Sphere";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = new Vector3(0f, pillarHeight + sphereRadius * 0.5f, 0f);
        sphere.transform.localScale = Vector3.one * sphereRadius * 2f;
        var scol = sphere.GetComponent<Collider>();
        if (scol != null) Destroy(scol);
        var srend = sphere.GetComponent<Renderer>();
        sphereMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        srend.sharedMaterial = sphereMat;

        ApplyColor(baseColor, 1f);
    }

    public void SetColor(Color c)
    {
        baseColor = c;
        ApplyColor(c, 1f);
    }

    public void SetDimmed(bool dim)
    {
        ApplyColor(baseColor, dim ? dimAlpha : 1f);
    }

    public void SetVisible(bool visible)
    {
        if (pillar != null) pillar.SetActive(visible);
        if (sphere != null) sphere.SetActive(visible);
    }

    void ApplyColor(Color c, float alpha, float emissionScale = 1.5f)
    {
        if (pillarMat == null || sphereMat == null) return;
        Color withAlpha = new Color(c.r, c.g, c.b, alpha);

        if (alpha < 1f)
        {
            SetTransparent(pillarMat); SetTransparent(sphereMat);
        }
        else
        {
            SetOpaque(pillarMat); SetOpaque(sphereMat);
        }

        pillarMat.color = withAlpha;
        sphereMat.color = withAlpha;
        if (pillarMat.HasProperty("_EmissionColor"))
        {
            pillarMat.SetColor("_EmissionColor", c * emissionScale);
            pillarMat.EnableKeyword("_EMISSION");
        }
        if (sphereMat.HasProperty("_EmissionColor"))
        {
            sphereMat.SetColor("_EmissionColor", c * emissionScale);
            sphereMat.EnableKeyword("_EMISSION");
        }
    }

    static void SetTransparent(Material m)
    {
        m.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
    }

    static void SetOpaque(Material m)
    {
        m.SetFloat("_Surface", 0);
        m.SetOverrideTag("RenderType", "Opaque");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = -1;
    }

    // ── 채널 8 — 경로 순번 배지 ─────────────────────────────────

    /// <summary>RoutePlanScreen이 플레이어가 정한 순서 번호를 표시.</summary>
    public void SetRouteOrder(int n)
    {
        EnsureOrderBadge();
        orderBadge.SetActive(true);
        orderBadgeText.text = n.ToString();
    }

    /// <summary>배지 숨김 (RoutePlan 닫힘 또는 재설정).</summary>
    public void ClearRouteOrder()
    {
        if (orderBadge != null) orderBadge.SetActive(false);
    }

    void EnsureOrderBadge()
    {
        if (orderBadge != null) return;
        orderBadge = new GameObject("OrderBadge");
        orderBadge.transform.SetParent(transform, false);
        orderBadge.transform.localPosition = new Vector3(0f, pillarHeight + sphereRadius * 2f + 1.2f, 0f);
        orderBadgeText = orderBadge.AddComponent<TextMeshPro>();
        orderBadgeText.fontSize = 8f;
        orderBadgeText.alignment = TextAlignmentOptions.Center;
        orderBadgeText.color = Color.white;
        orderBadgeText.fontStyle = FontStyles.Bold;
        // 작은 검정 배경 — 가독성
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "BadgeBG";
        bg.transform.SetParent(orderBadge.transform, false);
        bg.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
        bg.transform.localPosition = new Vector3(0f, 0f, 0.01f); // 텍스트 뒤
        var c = bg.GetComponent<Collider>();
        if (c != null) Destroy(c);
        var mr = bg.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = new Color(0f, 0f, 0f, 0.7f);
        // 투명 설정
        mat.SetFloat("_Surface", 1);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mr.sharedMaterial = mat;
    }

    void LateUpdate()
    {
        // 순번 배지를 메인 카메라 향해 빌보드
        if (orderBadge == null || !orderBadge.activeSelf) return;
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 toCam = orderBadge.transform.position - cam.transform.position;
        if (toCam.sqrMagnitude > 0.0001f)
            orderBadge.transform.rotation = Quaternion.LookRotation(toCam);
    }
}
