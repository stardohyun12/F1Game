using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>화물 prefab에 부착. wrap 상태를 RenderTexture에 보관.</summary>
    [DisallowMultipleComponent]
    public sealed class WrapState : MonoBehaviour
    {
        [SerializeField] private int textureSize = 256;
        [SerializeField] private Renderer targetRenderer;

        /// <summary>wrap 상태 텍스처. OnDestroy 후 null. 외부 접근 전 null 체크 필수.</summary>
        public RenderTexture WrapRT { get; private set; }
        public int Size => textureSize;
        public Renderer TargetRenderer => targetRenderer;

        void Awake()
        {
            WrapRT = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
            WrapRT.wrapMode = TextureWrapMode.Clamp;
            WrapRT.filterMode = FilterMode.Bilinear;
            WrapRT.Create();
            ClearAll();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        }

        public void ClearAll()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = WrapRT;
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            RenderTexture.active = prev;
        }

        void OnDestroy()
        {
            if (WrapRT != null) { WrapRT.Release(); WrapRT = null; }
        }

        // 마지막 strip 그릴 때 누적된 비용 (재료 채널별)
        public float[] CostByChannel = new float[3];

        // 평균 커버 비율 (0~1). 픽셀 비싸므로 정해진 주기에만 호출.
        public float ComputeCoverage()
        {
            var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = WrapRT;
            tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            RenderTexture.active = prev;
            int covered = 0;
            var px = tex.GetPixels();
            foreach (var p in px) if (p.r > 0.5f || p.g > 0.5f || p.b > 0.5f) covered++;
            Object.Destroy(tex);
            return (float)covered / px.Length;
        }

        public void AccumulateCost(int channelIndex, float amount)
        {
            if (channelIndex < 0 || channelIndex > 2) return;
            CostByChannel[channelIndex] += amount;
        }

        public float TotalCost => CostByChannel[0] + CostByChannel[1] + CostByChannel[2];

    // ── Damage Query ──────────────────────────────────────────────────────────

    [Header("Damage Query")]
    [SerializeField] private float queryRadiusUV = 0.05f;

    WrapMaterialDef[] cachedMats;

    WrapMaterialDef[] LoadMats() => cachedMats ??= Resources.LoadAll<WrapMaterialDef>("WrapMaterials");

    /// <summary>충격 점 world position 받아 흡수율 0~1 반환.</summary>
    public float QueryAbsorption(Vector3 worldHitPoint, Vector3 incomingDir)
    {
        var col = targetRenderer != null ? targetRenderer.GetComponent<MeshCollider>() : null;
        if (col == null) col = GetComponentInChildren<MeshCollider>();
        if (col == null) return 0f;

        // 충격 방향의 반대 방향에서 raycast해 표면 UV 추출
        var origin = worldHitPoint - incomingDir.normalized * 0.5f;
        if (!col.Raycast(new Ray(origin, incomingDir.normalized), out var hit, 1f)) return 0f;

        var uv = hit.textureCoord;
        return SampleAbsorptionAtUV(uv);
    }

    float SampleAbsorptionAtUV(Vector2 uv)
    {
        // queryRadiusUV → 픽셀 반경. 0.05 × 256 ≈ 13 → 27×27 region.
        int r = Mathf.Max(1, Mathf.RoundToInt(queryRadiusUV * textureSize));
        int size = r * 2;
        int x = Mathf.Clamp(Mathf.RoundToInt(uv.x * textureSize), r, textureSize - r - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(uv.y * textureSize), r, textureSize - r - 1);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var prev = RenderTexture.active;
        RenderTexture.active = WrapRT;
        tex.ReadPixels(new Rect(x - r, y - r, size, size), 0, 0);
        RenderTexture.active = prev;

        var pixels = tex.GetPixels();
        float maxR = 0, maxG = 0, maxB = 0;
        foreach (var p in pixels) { maxR = Mathf.Max(maxR, p.r); maxG = Mathf.Max(maxG, p.g); maxB = Mathf.Max(maxB, p.b); }
        Object.Destroy(tex);

        var mats = LoadMats();
        // 최상위 = B > G > R 우선
        if (maxB > 0.5f) return AbsOf(mats, 2);
        if (maxG > 0.5f) return AbsOf(mats, 1);
        if (maxR > 0.5f) return AbsOf(mats, 0);
        return 0f;
    }

    float AbsOf(WrapMaterialDef[] mats, int channel)
    {
        foreach (var m in mats) if (m.channelIndex == channel) return m.absorption;
        return 0f;
    }
    }
}
