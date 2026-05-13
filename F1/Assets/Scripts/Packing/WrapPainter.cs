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

        public float StripWidthUV { get => stripWidthUV; set => stripWidthUV = Mathf.Clamp(value, 0.01f, 0.25f); }
        public void SetMaterial(WrapMaterialDef def) { currentMat = def; }

        void Awake()
        {
            blitMat = new Material(Shader.Find("PitCrew/WrapStripBlit"));
            if (targetState != null) InitRT();
        }

        /// <summary>targetState가 직렬화로 연결되지 않은 경우(예: 런타임 AddComponent) 수동으로 호출.</summary>
        public void Init(WrapState state)
        {
            targetState = state;
            if (blitMat == null) blitMat = new Material(Shader.Find("PitCrew/WrapStripBlit"));
            InitRT();
        }

        void InitRT()
        {
            if (scratch != null) scratch.Release();
            scratch = new RenderTexture(targetState.Size, targetState.Size, 0, RenderTextureFormat.ARGB32);
            scratch.Create();
        }

        void OnDestroy() { if (scratch != null) scratch.Release(); if (blitMat != null) Destroy(blitMat); }

        public void BeginStroke(Vector2 screenPos)
        {
            if (TryRaycastUV(screenPos, out var uv)) { lastUV = uv; stroking = true; }
        }

        public void EndStroke() { stroking = false; }

        public void UpdateStroke(Vector2 screenPos)
        {
            if (!stroking || currentMat == null) return;
            if (!TryRaycastUV(screenPos, out var uv)) return;
            if ((uv - lastUV).sqrMagnitude < minSampleDistUV * minSampleDistUV) return;

            DrawStrip(lastUV, uv, currentMat);
            lastUV = uv;
        }

        bool TryRaycastUV(Vector2 screenPos, out Vector2 uv)
        {
            uv = default;
            var ray = packingCamera.ScreenPointToRay(screenPos);
            if (!targetCollider.Raycast(ray, out var hit, 100f)) return false;
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
        }
    }
}
