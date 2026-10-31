using UnityEngine;

/// <summary>
/// D-233 노을 6판: 바다에 해안 나무·땅·부두가 거꾸로 비치게 하는 평면 반사.
///  · 보는 카메라를 해수면(planeY)에 대칭시킨 자리에 반사 카메라를 두고, 물 밑은 경사 near 평면으로 잘라 반쪽 해상도 RT 에 그린다.
///    (대칭 카메라를 회전 행렬로만 만들면 그림이 좌우로 뒤집힘 → 셰이더가 화면 uv.x 를 1-x 로 읽는다. GL.invertCulling 불필요)
///  · 그리는 동안 바다 판(hide)은 끈다. 하늘은 스카이박스 그대로 → 구름·해 반사까지 같이 들어감.
///  · 결과는 전역 _PCSeaRefl(텍스처)·_PCSeaReflOn(x = 1 켬). DuskSea 가 하늘 반사(skyR) 자리에 섞는다.
///  · Play: 메인 카메라 기준으로 LateUpdate 맨 끝에 한 번. 편집 모드 캡처는 RenderFor(cam) 을 직접 부른다.
/// </summary>
[ExecuteAlways, DefaultExecutionOrder(10000)]
public sealed class SeaReflection : MonoBehaviour
{
    public Renderer[] hide;
    [Tooltip("해수면 높이(m). 비우면(NaN 아님 0) hide[0] 위치 y")]
    public float planeY = 0f;
    public bool planeFromHide = true;
    [Tooltip("보는 카메라 픽셀 대비 RT 크기 나눗수")]
    public int downscale = 2;
    public float farClip = 600f;
    public LayerMask cullMask = ~0;
    [Tooltip("경사 near 평면을 해수면보다 이만큼 아래로(물가가 잘려 뜨는 것 방지)")]
    public float clipOffset = 0.05f;

    Camera rc;
    RenderTexture rt;
    bool fresh;
    static readonly int TexId = Shader.PropertyToID("_PCSeaRefl"), OnId = Shader.PropertyToID("_PCSeaReflOn");

    public static SeaReflection Find() => FindFirstObjectByType<SeaReflection>();

    void OnDisable()
    {
        Shader.SetGlobalVector(OnId, Vector4.zero);
        if (rc != null) DestroyImmediate(rc.gameObject);
        if (rt != null) { rt.Release(); DestroyImmediate(rt); }
        rc = null; rt = null;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        var cam = Camera.main;
        if (cam != null) RenderFor(cam);
    }

    float PlaneY() => planeFromHide && hide != null && hide.Length > 0 && hide[0] != null ? hide[0].transform.position.y : planeY;

    public void RenderFor(Camera view)
    {
        // 바다 판이 전부 켜져 있을 때만(SeaNear 는 노을일 때만 켜짐 → 낮엔 반사 카메라 안 돎)
        bool allOn = hide != null && hide.Length > 0;
        if (hide != null) foreach (var r in hide) if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) allOn = false;
        if (!allOn || view == null) { Shader.SetGlobalVector(OnId, Vector4.zero); return; }

        int w = Mathf.Max(16, view.pixelWidth / Mathf.Max(1, downscale)), h = Mathf.Max(16, view.pixelHeight / Mathf.Max(1, downscale));
        if (rt == null || rt.width != w || rt.height != h)
        {
            if (rt != null) { rt.Release(); DestroyImmediate(rt); }
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { name = "SeaRefl", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        }
        if (rc == null)
        {
            var go = new GameObject("SeaReflCam") { hideFlags = HideFlags.HideAndDontSave };
            rc = go.AddComponent<Camera>(); rc.enabled = false; fresh = true;
            // PitCrew 어셈블리는 URP 를 참조하지 않음 → 카메라 추가 데이터는 이름으로(그림자 끔 = 반사 비용 줄임, 후처리 끔)
            var udT = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (udT != null)
            {
                var ud = go.AddComponent(udT);
                udT.GetProperty("renderShadows")?.SetValue(ud, false);
                udT.GetProperty("renderPostProcessing")?.SetValue(ud, false);
            }
        }

        float y = PlaneY();
        Vector3 p = view.transform.position, f = view.transform.forward, u = view.transform.up;
        rc.CopyFrom(view);
        rc.enabled = false;
        rc.cullingMask = cullMask;
        rc.farClipPlane = Mathf.Min(view.farClipPlane, farClip);
        // 6판: 하늘은 셰이더가 파 노멀로 직접 계산(DuskSkyColor) → RT 는 투명 바탕에 물체만(a = 1). 스카이박스로 덮으면 파 덩어리 색이 평평해짐(실측)
        rc.clearFlags = CameraClearFlags.SolidColor;
        rc.backgroundColor = new Color(0, 0, 0, 0);
        rc.targetTexture = rt;
        rc.transform.SetPositionAndRotation(new Vector3(p.x, 2f * y - p.y, p.z), Quaternion.LookRotation(new Vector3(f.x, -f.y, f.z), new Vector3(u.x, -u.y, u.z)));
        rc.ResetProjectionMatrix();
        // 경사 near 평면 = 해수면(물 밑은 안 그림). 카메라 공간 평면
        var w2c = rc.worldToCameraMatrix;
        Vector3 cp = w2c.MultiplyPoint(new Vector3(0, y - clipOffset, 0)), cn = w2c.MultiplyVector(Vector3.up).normalized;
        rc.projectionMatrix = rc.CalculateObliqueMatrix(new Vector4(cn.x, cn.y, cn.z, -Vector3.Dot(cp, cn)));

        var was = new bool[hide.Length];
        for (int i = 0; i < hide.Length; i++) if (hide[i] != null) { was[i] = hide[i].forceRenderingOff; hide[i].forceRenderingOff = true; }
        rc.Render();
        if (fresh) { rc.Render(); fresh = false; }   // 새로 만든 카메라의 첫 그림은 물체가 빠짐(6판 편집 모드 실측) → 한 번 더
        for (int i = 0; i < hide.Length; i++) if (hide[i] != null) hide[i].forceRenderingOff = was[i];
        rc.targetTexture = null;

        Shader.SetGlobalTexture(TexId, rt);
        Shader.SetGlobalVector(OnId, new Vector4(1, 0, 0, 0));
    }
}
