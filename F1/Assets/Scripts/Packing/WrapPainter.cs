using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>마우스 클릭 누름 동안 화물 표면에 랩 띠를 그림. 카메라와 WrapState 참조.</summary>
    public sealed class WrapPainter : MonoBehaviour
    {
        [SerializeField] private Camera packingCamera;
        [SerializeField] private WrapState targetState;
        [SerializeField] private MeshCollider targetCollider;

        [Header("Painting")]
        [SerializeField] private float stripWidthUV = 0.06f;
        [SerializeField] private float minSampleDistUV = 0.005f;

        Material blitMat;
        RenderTexture scratch;
        WrapMaterialDef currentMat;
        bool stroking;
        Vector2 lastUV;

        // 3D ribbon
        WrapRibbon currentRibbon;
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

        /// <summary>targetState가 직렬화로 연결되지 않은 경우(예: 런타임 AddComponent) 수동으로 호출.</summary>
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
        }

        void OnDestroy()
        {
            if (scratch != null) scratch.Release();
            if (blitMat != null) Destroy(blitMat);
            if (ribbonMaterialsByChannel != null)
            {
                foreach (var m in ribbonMaterialsByChannel)
                    if (m != null) Destroy(m);
            }
        }

        Material GetOrCreateRibbonMaterial(WrapMaterialDef def)
        {
            if (ribbonMaterialsByChannel == null) ribbonMaterialsByChannel = new Material[3];
            int idx = Mathf.Clamp(def.channelIndex, 0, 2);
            if (ribbonMaterialsByChannel[idx] == null)
            {
                var shader = Shader.Find("PitCrew/WrapRibbon");
                var mat = new Material(shader);
                var col = def.displayColor;
                col.a = 0.7f;
                mat.SetColor("_BaseColor", col);
                ribbonMaterialsByChannel[idx] = mat;
            }
            return ribbonMaterialsByChannel[idx];
        }

        public void BeginStroke(Vector2 screenPos)
        {
            if (stroking) EndStroke();  // 이전 stroke 암묵적 종료
            if (currentMat == null) return;
            if (!TryRaycastUV(screenPos, out var uv, out var hit)) return;

            lastUV = uv;
            stroking = true;
            AudioCueBus.Publish(AudioCueId.PackingStrokeStart);

            // 3D 리본 GameObject를 큐브의 자식으로 생성
            var ribbonGO = new GameObject($"Ribbon_{currentMat.displayName}");
            ribbonGO.transform.SetParent(targetCollider != null ? targetCollider.transform : transform, false);
            currentRibbon = ribbonGO.AddComponent<WrapRibbon>();
            currentRibbon.Init(GetOrCreateRibbonMaterial(currentMat), stripWidthUV * 1.5f);
            currentRibbon.AddSample(hit.point, hit.normal);
        }

        public void EndStroke()
        {
            if (stroking) { OnStrokeEnded?.Invoke(targetState); AudioCueBus.Publish(AudioCueId.PackingStrokeEnd); }
            stroking = false;
            currentRibbon = null; // ribbon GameObject는 cargo 자식으로 유지
        }

        public void UpdateStroke(Vector2 screenPos)
        {
            if (!stroking || currentMat == null) return;
            if (!TryRaycastUV(screenPos, out var uv, out var hit)) return;
            if ((uv - lastUV).sqrMagnitude < minSampleDistUV * minSampleDistUV) return;

            DrawStrip(lastUV, uv, currentMat);
            lastUV = uv;

            if (currentRibbon != null) currentRibbon.AddSample(hit.point, hit.normal);
        }

        bool TryRaycastUV(Vector2 screenPos, out Vector2 uv) => TryRaycastUV(screenPos, out uv, out _);

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

            // 비용 추정: 띠 길이(UV 거리) × 폭 × 텍스처 면적 × 단가
            float lenUV = (curr - prev).magnitude;
            float pixelArea = lenUV * stripWidthUV * targetState.Size * targetState.Size;
            targetState.AccumulateCost(mat.channelIndex, pixelArea * mat.costPerPixel);
        }
    }
}
