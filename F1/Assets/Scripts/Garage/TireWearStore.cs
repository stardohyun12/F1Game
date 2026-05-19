using System;
using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.Garage
{
    /// <summary>
    /// 타이어 4종 영구 마모 + 펑크 확률 (D-091, Task 7).
    ///
    /// 설계:
    /// - 마모는 효과를 깎지 않음. 임계점(30% 이하) = 펑크 위험.
    /// - 펑크 발동 = 즉시 잡 실패 (RunController.EndRun(false)).
    /// - TireType: Soft / Medium / Hard 3종.
    /// - 수리 비용 = FullRepairCost * (1 - 현재 내구도).
    /// </summary>
    public interface ITireWearStore
    {
        /// <summary>0~1. 1=새 타이어, 0=완전 마모.</summary>
        float GetWear01(TireType type);
        bool IsPunctureRisk(TireType type);
        int RepairCost(TireType type);
        bool TryRepair(TireType type);
        void RecordJobUsage(TireType used, float distanceMeters, float peakG);
        bool RollPuncture(TireType current);
        event Action OnWearChanged;
        event Action OnPunctured;
    }

    public sealed class TireWearStore : MonoBehaviour, ITireWearStore
    {
        static readonly Dictionary<TireType, int> FullRepairCost = new Dictionary<TireType, int>
        {
            { TireType.Soft,   800 },
            { TireType.Medium, 500 },
            { TireType.Hard,   600 },
            { TireType.Wet,    500 },  // D-091: 안전 운전용
        };

        readonly Dictionary<TireType, float> _wear = new Dictionary<TireType, float>();

        public event Action OnWearChanged;
        public event Action OnPunctured;

        void Awake()
        {
            foreach (TireType t in Enum.GetValues(typeof(TireType)))
                _wear[t] = 1f;
        }

        public float GetWear01(TireType type) => _wear.TryGetValue(type, out var v) ? v : 1f;

        public bool IsPunctureRisk(TireType type) => GetWear01(type) <= 0.30f;

        public int RepairCost(TireType type)
        {
            float worn = 1f - GetWear01(type);
            return Mathf.CeilToInt(FullRepairCost[type] * worn);
        }

        public bool TryRepair(TireType type)
        {
            int cost = RepairCost(type);
            if (cost <= 0) return false;
            var up = Services.Upgrade;
            if (up == null || up.Wallet < cost) return false;
            up.Spend(cost);
            _wear[type] = 1f;
            OnWearChanged?.Invoke();
            return true;
        }

        /// <summary>잡 종료 시 RunController가 호출. 영구 마모 누적.</summary>
        public void RecordJobUsage(TireType used, float distanceMeters, float peakG)
        {
            float wearDelta = Mathf.Clamp(distanceMeters / 500f * 0.10f + peakG / 5f * 0.10f, 0.05f, 0.25f);
            _wear[used] = Mathf.Max(0f, _wear[used] - wearDelta);
            OnWearChanged?.Invoke();
            Debug.Log($"[TireWearStore] {used} wear: -{wearDelta:F2} → {_wear[used]:F2}");
        }

        /// <summary>
        /// 매 초 RunController가 호출. 30% 이하면 펑크 롤.
        /// per-second probability: 30% = 20%, 0% = 5%.
        /// </summary>
        public bool RollPuncture(TireType current)
        {
            float w = GetWear01(current);
            if (w > 0.30f) return false;
            // w: 0.30 → rate 0.05, w: 0 → rate 0.20
            float rate = Mathf.Lerp(0.20f, 0.05f, w / 0.30f);
            if (UnityEngine.Random.value < rate)
            {
                Debug.Log($"[TireWearStore] PUNCTURE on {current} (wear={w:F2}, p={rate:F2})");
                OnPunctured?.Invoke();
                return true;
            }
            return false;
        }
    }
}
