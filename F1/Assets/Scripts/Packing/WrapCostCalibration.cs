using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>
    /// 화물 종류별 '사람처럼 다 감기' 실측값. 메뉴 PitCrew/Calibrate Wrap Cost 가 채운다(Play 중).
    /// fullWrapPixels = 다 감을 때 DrawStrip 이 세는 픽셀 면적 합(겹침 포함). 재료와 무관하다
    /// (비용 길이 = 드래그 거리라서). PackingScreen 이 CostScale = 보상 ÷ fullWrapPixels 로 써서
    /// '다 감으면 보상 × 재료 costPerPixel' 이 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "PitCrew/Wrap Cost Calibration")]
    public class WrapCostCalibration : ScriptableObject
    {
        public const string ResourcePath = "WrapCostCalibration";

        [System.Serializable]
        public struct Entry
        {
            public CargoType cargoType;
            public float fullWrapPixels;
            [Tooltip("보정에 쓴 스트로크 수(참고)")] public int strokes;
        }

        public List<Entry> entries = new List<Entry>();

        static WrapCostCalibration cached;
        public static WrapCostCalibration Load()
        {
            if (cached == null) cached = Resources.Load<WrapCostCalibration>(ResourcePath);
            return cached;
        }

        public bool TryGet(CargoType t, out float pixels)
        {
            foreach (var e in entries)
                if (e.cargoType == t && e.fullWrapPixels > 0f) { pixels = e.fullWrapPixels; return true; }
            pixels = 0f;
            return false;
        }

        public void Set(CargoType t, float pixels, int strokes)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].cargoType == t) { entries[i] = new Entry { cargoType = t, fullWrapPixels = pixels, strokes = strokes }; return; }
            entries.Add(new Entry { cargoType = t, fullWrapPixels = pixels, strokes = strokes });
        }
    }
}
