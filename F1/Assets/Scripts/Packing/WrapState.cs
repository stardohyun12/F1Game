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
    }
}
