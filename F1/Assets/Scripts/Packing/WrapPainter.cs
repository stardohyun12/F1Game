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

        // 메시 평균 "UV 1 당 월드 길이". 비용·이음새 판정을 UV 배치와 무관하게 하려고 (AI 메시는 UV 섬이 잘게 쪼개져 있음).
        float worldPerUV = 1f;
        // 같은 값을 콜라이더 로컬 단위로 — 리본(콜라이더 자식, 로컬 폭)과 칠하는 띠 폭을 맞출 때 쓴다.
        float localPerUV = 1f;
        [Tooltip("UV 이동이 월드 이동으로 예상한 값의 몇 배를 넘으면 UV 이음새를 건넌 것으로 본다.")]
        [SerializeField] private float seamJumpRatio = 3f;

        [Header("Sound (포장 감기 틱)")]
        [Tooltip("리본이 이 길이(m)만큼 풀릴 때마다 틱 1번 (가설 — 플레이 후 조정).")]
        [SerializeField] private float tickSpacingMeters = 0.06f;
        float tickAccum;

        const float k_RaycastRange = 100f;

        public float StripWidthUV { get => stripWidthUV; set => stripWidthUV = Mathf.Clamp(value, 0.01f, 0.25f); }
        /// <summary>리본 폭(로컬 m) — BeginStroke 의 WrapRibbon 폭과 같은 식.</summary>
        public float RibbonWidthMeters => stripWidthUV * 4.0f;
        public MeshCollider TargetCollider => targetCollider;
        /// <summary>WrapRT 에 칠하는 띠 폭(UV) — 눈에 보이는 리본 폭과 같은 표면 넓이를 칠한다(보이는 포장 = 보호).</summary>
        float PaintWidthUV => Mathf.Clamp(RibbonWidthMeters / Mathf.Max(1e-4f, localPerUV), 0.005f, 0.5f);
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
            worldPerUV = ComputeWorldPerUV(targetCollider, true, out _);
            localPerUV = ComputeWorldPerUV(targetCollider, false, out _);
        }

        /// <summary>sqrt(표면적 / UV 면적). useScale=false 면 콜라이더 로컬 단위. 실패하면 1.</summary>
        static float ComputeWorldPerUV(MeshCollider col, bool useScale, out float worldArea)
        {
            worldArea = 0f;
            if (col == null || col.sharedMesh == null) return 1f;
            var m = col.sharedMesh;
            if (!m.isReadable) return 1f; // Read/Write 꺼진 임포트 메시 — 접근 시 에러 스팸. (화물 FBX 는 임포트에서 켜 둔다)
            var v = m.vertices; var uv = m.uv; var t = m.triangles;
            if (uv == null || uv.Length != v.Length || t.Length == 0) return 1f;
            var sc = useScale ? col.transform.lossyScale : Vector3.one;
            double wa = 0, ua = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                var a = Vector3.Scale(v[t[i]], sc); var b = Vector3.Scale(v[t[i + 1]], sc); var c = Vector3.Scale(v[t[i + 2]], sc);
                wa += Vector3.Cross(b - a, c - a).magnitude * 0.5;
                var p = uv[t[i]]; var q = uv[t[i + 1]]; var r = uv[t[i + 2]];
                ua += Mathf.Abs((q.x - p.x) * (r.y - p.y) - (r.x - p.x) * (q.y - p.y)) * 0.5;
            }
            worldArea = (float)wa;
            if (ua < 1e-6 || wa < 1e-6) return 1f;
            return (float)System.Math.Sqrt(wa / ua);
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
            tickAccum = 0f;
            AudioCueBus.Publish(AudioCueId.PackingStrokeStart);
        }

        /// <summary>
        /// 감는 소리 — 리본이 tickSpacingMeters 만큼 풀릴 때마다 짧은 틱. 속도가 빠를수록 촘촘·높게.
        /// 포장 중엔 timeScale 0 → unscaledDeltaTime 으로 속도를 잰다. 한 프레임에 1번만.
        /// </summary>
        void EmitStrokeTick(float dragDist)
        {
            if (dragDist <= 0f || currentMat == null) return;
            tickAccum += dragDist;
            if (tickAccum < tickSpacingMeters) return;
            tickAccum %= Mathf.Max(1e-4f, tickSpacingMeters);
            float speed = dragDist / Mathf.Max(1e-3f, Time.unscaledDeltaTime); // m/s
            PackingSfx.PlayTick(currentMat.channelIndex, Mathf.InverseLerp(0.2f, 3f, speed));
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
            float worldDist = 0f;
            float dragDist = 0f; // 화면 드래그를 물건 깊이의 평면으로 옮긴 거리 = 리본 길이(비용 기준)

            if (hasLastHit)
            {
                // 큐브 회전(WASD) 반영 — 직전 hit을 현재 frame world/screen으로 재투영.
                Vector3 lastWorldNow = t.TransformPoint(lastHitLocalPos);
                worldDist = Vector3.Distance(lastWorldNow, hit.point);
                Vector3 lastNormalWorldNow = t.TransformDirection(lastHitLocalNormal);
                Vector3 lastScreen3 = packingCamera.WorldToScreenPoint(lastWorldNow);
                Vector2 lastScreenNow = new Vector2(lastScreen3.x, lastScreen3.y);
                float depth = packingCamera.WorldToScreenPoint(hit.point).z;
                dragDist = Vector3.Distance(
                    packingCamera.ScreenToWorldPoint(new Vector3(lastScreenNow.x, lastScreenNow.y, depth)),
                    packingCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth)));

                SubdivideAndAdd(lastScreenNow, screenPos,
                                lastWorldNow, lastNormalWorldNow, lastUV,
                                hit.point, hit.normal, uv, 0);
            }
            else
            {
                activeRibbon.AddSample(hit.point, hit.normal, currentStrokeLift);
            }

            // 이음새 판정: 표면에선 조금 움직였는데 UV 가 크게 뛰면 다른 UV 섬으로 넘어간 것 —
            // 그 사이를 선으로 칠하면 텍스처 엉뚱한 곳이 칠해지고 비용이 폭증한다(케이크 윗면 버그).
            float expectedUV = worldDist / Mathf.Max(1e-4f, worldPerUV);
            bool seam = !hasLastHit || (lastUV - uv).magnitude > expectedUV * seamJumpRatio + 0.02f;
            Vector2 from = seam ? uv : lastUV;
            DrawStrip(from, uv, currentMat, dragDist);
            BumpCoverage(from, uv);
            EmitStrokeTick(dragDist);

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

        /// <param name="costLenUV">비용 길이 — 화면 드래그 거리(물건 깊이 평면, m). 리본은 월드 폭이 일정하므로 끈 길이 = 쓴 포장지.
        /// 기본 큐브(UV 1 = 1m)에선 예전 식(UV 이동 거리)과 같고, UV 섬 배치·메시 울퉁불퉁함과 무관하다.</param>
        void DrawStrip(Vector2 prev, Vector2 curr, WrapMaterialDef mat, float costLenUV)
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
            blitMat.SetFloat("_Width", PaintWidthUV);
            blitMat.SetVector("_ChannelMask", mask);

            Graphics.Blit(src, scratch, blitMat);
            Graphics.Blit(scratch, src);
            targetState.MarkDirty(); // 면별 게이지·흡수 조회가 새 픽셀을 읽게

            float pixelArea = costLenUV * stripWidthUV * targetState.Size * targetState.Size;
            targetState.AccumulateCost(mat.channelIndex, pixelArea * mat.costPerPixel * targetState.CostScale);
        }
    }
}
