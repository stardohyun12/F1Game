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

        // RT 의 CPU 사본 — 칠할 때만 더러워진다(MarkDirty). 주행 중엔 칠하지 않으니 ReadPixels 는 사실상 한 번.
        Texture2D cpuTex;
        Color32[] cpuPixels;
        bool cpuDirty = true;

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
            cpuDirty = true;
        }

        /// <summary>WrapRT 에 칠한 뒤 호출 — 다음 조회 때 CPU 사본을 다시 읽는다.</summary>
        public void MarkDirty() => cpuDirty = true;

        void OnDestroy()
        {
            if (WrapRT != null) { WrapRT.Release(); WrapRT = null; }
            if (cpuTex != null) { Object.Destroy(cpuTex); cpuTex = null; }
        }

        Color32[] GetPixelsCached()
        {
            if (WrapRT == null) return null;
            if (!cpuDirty && cpuPixels != null) return cpuPixels;
            if (cpuTex == null) cpuTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = WrapRT;
            cpuTex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0, false);
            RenderTexture.active = prev;
            cpuPixels = cpuTex.GetPixels32();
            cpuDirty = false;
            return cpuPixels;
        }

        // 마지막 strip 그릴 때 누적된 비용 (재료 채널별)
        public float[] CostByChannel = new float[3];

        // 평균 커버 비율 (0~1) — UV 픽셀 비율이라 실제 보호와 다를 수 있다(UV 빈 공간·윗/아랫면 포함).
        // 포장 화면 표시는 면별 흡수(QuerySideAbsorption)를 쓴다.
        public float ComputeCoverage()
        {
            var px = GetPixelsCached();
            if (px == null || px.Length == 0) return 0f;
            int covered = 0;
            foreach (var p in px) if (p.r > 127 || p.g > 127 || p.b > 127) covered++;
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
    [Tooltip("면 조회 때 면 위에 뿌리는 ray 격자(3×3)의 퍼짐 — 면 반폭 대비 비율.")]
    [SerializeField] private float faceSampleSpread = 0.6f;

    WrapMaterialDef[] cachedMats;

    WrapMaterialDef[] LoadMats() => cachedMats ??= Resources.LoadAll<WrapMaterialDef>("WrapMaterials");

    MeshCollider FindCollider()
    {
        var col = targetRenderer != null ? targetRenderer.GetComponent<MeshCollider>() : null;
        if (col == null) col = GetComponentInChildren<MeshCollider>(true);
        return col;
    }

    /// <summary>
    /// 충격이 박스의 어떤 면에 떨어졌는지 — outwardImpactDir는 박스 중심에서 충격 벽 쪽 방향.
    /// 그 벽 바깥에서 안쪽으로 raycast해 충격 면의 UV를 얻음. 박스 크기 무관. (한 점 조회 — 호환용)
    /// </summary>
    public float QueryAbsorption(Vector3 worldHitPoint, Vector3 outwardImpactDir)
    {
        var col = FindCollider();
        if (col == null) return 0f;

        Vector3 dir = outwardImpactDir.normalized;
        var b = targetRenderer != null ? targetRenderer.bounds : col.bounds;
        float r = Mathf.Max(0.5f, b.extents.magnitude * 1.5f);

        // 트렁크에 실린 화물은 콜라이더가 꺼져 있다(물리 간섭 방지) — 조회 순간만 켠다.
        bool wasEnabled = col.enabled;
        if (!wasEnabled) { col.enabled = true; Physics.SyncTransforms(); }
        // 충격 벽 바깥에서 시작 → 박스 안쪽 방향으로 ray → 첫 hit = 충격 면.
        var origin = worldHitPoint + dir * r;
        bool ok = col.Raycast(new Ray(origin, -dir), out var hit, r * 2f);
        if (!wasEnabled) col.enabled = false;
        if (!ok) return 0f;

        return SampleAbsorptionAtUV(hit.textureCoord, out _);
    }

    /// <summary>
    /// 면 전체 흡수율 — worldDir 쪽 면에 3×3 ray 를 뿌려 맞은 점들의 흡수율 평균.
    /// 포장 화면의 면별 게이지와 주행 중 벽 충돌 손상이 같은 함수를 쓴다(보이는 값 = 실제 값).
    /// dominantChannel = 가장 많은 점을 덮은 재료 채널(0/1/2), 없으면 -1.
    /// </summary>
    public float QueryFaceAbsorption(Vector3 worldDir, out int dominantChannel)
    {
        dominantChannel = -1;
        var col = FindCollider();
        if (col == null || worldDir.sqrMagnitude < 1e-8f) return 0f;

        bool wasEnabled = col.enabled;
        if (!wasEnabled) { col.enabled = true; Physics.SyncTransforms(); }

        // 꺼진 콜라이더의 bounds 는 비어 있다 — 켠 뒤에 읽는다.
        var b = col.bounds;
        Vector3 e = b.extents;
        Vector3 d = worldDir.normalized;
        Vector3 u = Vector3.Cross(d, Vector3.up);
        if (u.sqrMagnitude < 1e-4f) u = Vector3.Cross(d, Vector3.right);
        u.Normalize();
        Vector3 v = Vector3.Cross(d, u).normalized;
        float eu = (Mathf.Abs(u.x) * e.x + Mathf.Abs(u.y) * e.y + Mathf.Abs(u.z) * e.z) * faceSampleSpread;
        float ev = (Mathf.Abs(v.x) * e.x + Mathf.Abs(v.y) * e.y + Mathf.Abs(v.z) * e.z) * faceSampleSpread;
        float r = Mathf.Max(0.5f, e.magnitude * 1.5f);

        int hits = 0;
        float sum = 0f;
        int c0 = 0, c1 = 0, c2 = 0;
        for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++)
        {
            Vector3 origin = b.center + d * r + u * (i * eu) + v * (j * ev);
            if (!col.Raycast(new Ray(origin, -d), out var hit, r * 2f)) continue;
            hits++;
            sum += SampleAbsorptionAtUV(hit.textureCoord, out int ch);
            if (ch == 0) c0++; else if (ch == 1) c1++; else if (ch == 2) c2++;
        }
        if (!wasEnabled) col.enabled = false;
        if (hits == 0) return 0f;

        if (c0 + c1 + c2 > 0)
        {
            // 같은 수면 비싼 재료 우선 (B > G > R)
            dominantChannel = 2;
            int best = c2;
            if (c1 > best) { best = c1; dominantChannel = 1; }
            if (c0 > best) { dominantChannel = 0; }
        }
        return sum / hits;
    }

    /// <summary>화물 로컬 방향(예: Vector3.forward = '앞' 표식 쪽) 면의 흡수율.</summary>
    public float QuerySideAbsorption(Vector3 localDir, out int dominantChannel)
        => QueryFaceAbsorption(transform.TransformDirection(localDir), out dominantChannel);

    float SampleAbsorptionAtUV(Vector2 uv, out int channel)
    {
        channel = -1;
        var px = GetPixelsCached();
        if (px == null) return 0f;

        // queryRadiusUV → 픽셀 반경. 0.05 × 256 ≈ 13 → 27×27 region.
        int r = Mathf.Max(1, Mathf.RoundToInt(queryRadiusUV * textureSize));
        int cx = Mathf.Clamp(Mathf.RoundToInt(uv.x * textureSize), r, textureSize - r - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(uv.y * textureSize), r, textureSize - r - 1);

        byte maxR = 0, maxG = 0, maxB = 0;
        for (int y = cy - r; y < cy + r; y++)
        {
            int row = y * textureSize;
            for (int x = cx - r; x < cx + r; x++)
            {
                var p = px[row + x];
                if (p.r > maxR) maxR = p.r;
                if (p.g > maxG) maxG = p.g;
                if (p.b > maxB) maxB = p.b;
            }
        }

        var mats = LoadMats();
        // 최상위 = B > G > R 우선
        if (maxB > 127) { channel = 2; return AbsOf(mats, 2); }
        if (maxG > 127) { channel = 1; return AbsOf(mats, 1); }
        if (maxR > 127) { channel = 0; return AbsOf(mats, 0); }
        return 0f;
    }

    float AbsOf(WrapMaterialDef[] mats, int channel)
    {
        foreach (var m in mats) if (m.channelIndex == channel) return m.absorption;
        return 0f;
    }

    /// <summary>재료 중 가장 높은 흡수율 (게이지 최대치).</summary>
    public float MaxAbsorption
    {
        get
        {
            float mx = 0f;
            foreach (var m in LoadMats()) if (m != null) mx = Mathf.Max(mx, m.absorption);
            return mx > 0f ? mx : 1f;
        }
    }
    }
}
