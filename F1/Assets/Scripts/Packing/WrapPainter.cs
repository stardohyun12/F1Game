using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>
    /// 누른 채 끌면 표면에 실시간으로 ribbon 감김.
    /// 위치별 coverage grid가 페인트 횟수 누적 → 같은 자리 다시 감으면 lift 커져서 부풀어 오름.
    /// 적응 raycast 분할로 큐브뿐 아니라 볼록한 모양 전반에서 표면을 따라감.
    /// </summary>
    public sealed class WrapPainter : MonoBehaviour
    {
        [SerializeField] private Camera packingCamera;
        [SerializeField] private WrapState targetState;
        [SerializeField] private MeshCollider targetCollider;

        [Header("Painting")]
        [SerializeField] private float stripWidthUV = 0.06f;
        [SerializeField] private float baseLiftMeters = 0.06f;         // 첫 layer 솟음 6cm — cross-section 거의 반원
        [SerializeField] private float liftPerOverlapMeters = 0.025f;  // overlap 횟수당 +2.5cm
        [SerializeField] private float maxLiftMeters = 0.18f;
        [SerializeField] private float edgeNormalThresholdDeg = 45f;
        [SerializeField] private int maxSubdivideDepth = 1;

        Material blitMat;
        RenderTexture scratch;
        WrapMaterialDef currentMat;

        const int CovRes = 64;
        int[,] coverageGrid;

        bool stroking;
        WrapRibbon activeRibbon;
        float currentStrokeLift;
        Vector2 lastUV;
        Vector3 lastHitLocalPos;
        Vector3 lastHitLocalNormal;
        bool hasLastHit;

        Material[] ribbonMaterialsByChannel;

        const float k_RaycastRange = 100f;

        public float StripWidthUV { get => stripWidthUV; set => stripWidthUV = Mathf.Clamp(value, 0.01f, 0.25f); }
        public void SetMaterial(WrapMaterialDef def) { currentMat = def; }
        public System.Action<WrapState> OnStrokeEnded;

        void Awake()
        {
            blitMat = new Material(Shader.Find("PitCrew/WrapStripBlit"));
            if (targetState != null) InitRT();
        }

        public void Init(WrapState state, Camera cam = null, MeshCollider col = null)
        {
            targetState = state;
            if (cam != null) packingCamera = cam;
            if (col != null) targetCollider = col;
            if (blitMat == null) blitMat = new Material(Shader.Find("PitCrew/WrapStripBlit"));
            InitRT();
        }

        void InitRT()
        {
            if (scratch != null) scratch.Release();
            scratch = new RenderTexture(targetState.Size, targetState.Size, 0, RenderTextureFormat.ARGB32);
            scratch.Create();
            coverageGrid = new int[CovRes, CovRes];
        }

        void OnDestroy()
        {
            if (scratch != null) scratch.Release();
            if (blitMat != null) Destroy(blitMat);
            if (ribbonMaterialsByChannel != null)
                foreach (var m in ribbonMaterialsByChannel)
                    if (m != null) Destroy(m);
        }

        Material GetOrCreateRibbonMaterial(WrapMaterialDef def)
        {
            if (ribbonMaterialsByChannel == null) ribbonMaterialsByChannel = new Material[3];
            int idx = Mathf.Clamp(def.channelIndex, 0, 2);
            if (ribbonMaterialsByChannel[idx] == null)
            {
                var shader = Shader.Find("PitCrew/WrapRibbon");
                var mat = new Material(shader);
                var col = def.displayColor; col.a = 1.0f;
                mat.SetColor("_BaseColor", col);
                mat.SetColor("_RimColor", Color.white);
                mat.SetFloat("_RimPower", 3.0f);
                mat.SetFloat("_EdgeBoost", 0.4f);
                mat.SetFloat("_BaseAlpha", 0.45f);
                mat.SetFloat("_SpecPower", 90f);
                mat.SetFloat("_SpecStrength", 1.8f);
                ribbonMaterialsByChannel[idx] = mat;
            }
            return ribbonMaterialsByChannel[idx];
        }

        float ComputeLift(Vector2 uv)
        {
            if (coverageGrid == null) return baseLiftMeters;
            int x = Mathf.Clamp((int)(uv.x * CovRes), 0, CovRes - 1);
            int y = Mathf.Clamp((int)(uv.y * CovRes), 0, CovRes - 1);
            int c = coverageGrid[x, y];
            return Mathf.Min(maxLiftMeters, baseLiftMeters + c * liftPerOverlapMeters);
        }

        void BumpCoverage(Vector2 prev, Vector2 curr)
        {
            if (coverageGrid == null) return;
            int radius = Mathf.Max(1, Mathf.CeilToInt(stripWidthUV * CovRes * 0.5f));
            float dUV = (curr - prev).magnitude;
            int steps = Mathf.Max(2, Mathf.CeilToInt(dUV * CovRes * 2f));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                var uv = Vector2.Lerp(prev, curr, t);
                int cx = (int)(uv.x * CovRes);
                int cy = (int)(uv.y * CovRes);
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int y = cy + dy; if (y < 0 || y >= CovRes) continue;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int x = cx + dx; if (x < 0 || x >= CovRes) continue;
                        coverageGrid[x, y]++;
                    }
                }
            }
        }

        public void BeginStroke(Vector2 screenPos)
        {
            if (stroking) CancelStroke();
            if (currentMat == null) return;
            if (!TryRaycastUV(screenPos, out var uv, out var hit)) return;

            var ribbonGO = new GameObject($"Ribbon_{currentMat.displayName}");
            ribbonGO.transform.SetParent(targetCollider != null ? targetCollider.transform : transform, false);
            activeRibbon = ribbonGO.AddComponent<WrapRibbon>();
            activeRibbon.Init(GetOrCreateRibbonMaterial(currentMat), stripWidthUV * 4.0f);

            currentStrokeLift = ComputeLift(uv);
            activeRibbon.AddSample(hit.point, hit.normal, currentStrokeLift);

            var t = targetCollider.transform;
            lastUV = uv;
            lastHitLocalPos = t.InverseTransformPoint(hit.point);
            lastHitLocalNormal = t.InverseTransformDirection(hit.normal);
            hasLastHit = true;
            stroking = true;
            AudioCueBus.Publish(AudioCueId.PackingStrokeStart);
        }

        public void UpdateStroke(Vector2 screenPos)
        {
            if (!stroking || activeRibbon == null) return;
            if (!TryRaycastUV(screenPos, out var uv, out var hit))
            {
                hasLastHit = false;
                return;
            }

            var t = targetCollider.transform;

            if (hasLastHit)
            {
                // 큐브 회전(WASD) 반영 — 직전 hit을 현재 frame world/screen으로 재투영.
                Vector3 lastWorldNow = t.TransformPoint(lastHitLocalPos);
                Vector3 lastNormalWorldNow = t.TransformDirection(lastHitLocalNormal);
                Vector3 lastScreen3 = packingCamera.WorldToScreenPoint(lastWorldNow);
                Vector2 lastScreenNow = new Vector2(lastScreen3.x, lastScreen3.y);

                SubdivideAndAdd(lastScreenNow, screenPos,
                                lastWorldNow, lastNormalWorldNow, lastUV,
                                hit.point, hit.normal, uv, 0);
            }
            else
            {
                activeRibbon.AddSample(hit.point, hit.normal, currentStrokeLift);
            }

            DrawStrip(lastUV, uv, currentMat);
            BumpCoverage(lastUV, uv);

            lastUV = uv;
            lastHitLocalPos = t.InverseTransformPoint(hit.point);
            lastHitLocalNormal = t.InverseTransformDirection(hit.normal);
            hasLastHit = true;
        }

        void SubdivideAndAdd(Vector2 sA, Vector2 sB,
                             Vector3 pA, Vector3 nA, Vector2 uA,
                             Vector3 pB, Vector3 nB, Vector2 uB, int depth)
        {
            if (depth < maxSubdivideDepth && Vector3.Angle(nA, nB) > edgeNormalThresholdDeg)
            {
                var sM = (sA + sB) * 0.5f;
                if (TryRaycastUV(sM, out var uM, out var hM))
                {
                    SubdivideAndAdd(sA, sM, pA, nA, uA, hM.point, hM.normal, uM, depth + 1);
                    SubdivideAndAdd(sM, sB, hM.point, hM.normal, uM, pB, nB, uB, depth + 1);
                    return;
                }
            }
            activeRibbon.AddSample(pB, nB, currentStrokeLift);
        }

        public void EndStroke()
        {
            if (!stroking) return;
            OnStrokeEnded?.Invoke(targetState);
            AudioCueBus.Publish(AudioCueId.PackingStrokeEnd);
            stroking = false;
            activeRibbon = null;
            hasLastHit = false;
        }

        public void CancelStroke()
        {
            if (!stroking) return;
            if (activeRibbon != null && !activeRibbon.HasGeometry) Destroy(activeRibbon.gameObject);
            stroking = false;
            activeRibbon = null;
            hasLastHit = false;
        }

        bool TryRaycastUV(Vector2 screenPos, out Vector2 uv, out RaycastHit hit)
        {
            uv = default; hit = default;
            if (packingCamera == null || targetCollider == null) return false;
            var ray = packingCamera.ScreenPointToRay(screenPos);
            if (!targetCollider.Raycast(ray, out hit, k_RaycastRange)) return false;
            uv = hit.textureCoord;
            return true;
        }

        void DrawStrip(Vector2 prev, Vector2 curr, WrapMaterialDef mat)
        {
            if (targetState == null || targetState.WrapRT == null) return;
            var src = targetState.WrapRT;
            Vector4 mask = new Vector4(
                mat.channelIndex == 0 ? 1 : 0,
                mat.channelIndex == 1 ? 1 : 0,
                mat.channelIndex == 2 ? 1 : 0,
                0);

            blitMat.SetVector("_UVPrev", new Vector4(prev.x, prev.y, 0, 0));
            blitMat.SetVector("_UVCurr", new Vector4(curr.x, curr.y, 0, 0));
            blitMat.SetFloat("_Width", stripWidthUV);
            blitMat.SetVector("_ChannelMask", mask);

            Graphics.Blit(src, scratch, blitMat);
            Graphics.Blit(scratch, src);

            float lenUV = (curr - prev).magnitude;
            float pixelArea = lenUV * stripWidthUV * targetState.Size * targetState.Size;
            targetState.AccumulateCost(mat.channelIndex, pixelArea * mat.costPerPixel);
        }
    }
}
