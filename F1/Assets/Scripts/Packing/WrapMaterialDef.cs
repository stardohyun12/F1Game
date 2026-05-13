using UnityEngine;

namespace PitCrew.Packing
{
    [CreateAssetMenu(menuName = "PitCrew/WrapMaterial", fileName = "WrapMaterial")]
    public sealed class WrapMaterialDef : ScriptableObject
    {
        [Tooltip("표시 이름 (한글)")]
        public string displayName = "Bubble";

        [Tooltip("RGB 채널 인덱스. 0=R(부블랩), 1=G(폼), 2=B(하드케이스)")]
        [Range(0, 2)] public int channelIndex = 0;

        [Tooltip("단위 면적(픽셀)당 비용. 띠 한 strip 그릴 때 픽셀 수 × 이 값.")]
        public float costPerPixel = 0.05f;

        [Tooltip("충격 흡수율 0~1. 1이면 완전 흡수.")]
        [Range(0f, 1f)] public float absorption = 0.25f;

        [Tooltip("시각 색상 (UI 스와치 + 표면 오버레이)")]
        public Color displayColor = new Color(0.42f, 0.72f, 0.91f, 0.85f);

        [Tooltip("입력 키 (1/2/3)")]
        public KeyCode hotkey = KeyCode.Alpha1;
    }
}
