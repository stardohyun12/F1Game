using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.Garage
{
    public interface IBoostSystem
    {
        bool IsUnlocked { get; }
        bool IsActive { get; }
        float Gauge01 { get; }
        void SetUnlocked(bool v);
        event Action<bool> OnActiveChanged;
    }

    public sealed class BoostSystem : MonoBehaviour, IBoostSystem
    {
        [SerializeField] private float boostDuration = 2.0f;
        [SerializeField] private float rechargeTime = 5.0f;
        [SerializeField] private float speedMul = 1.50f;
        [SerializeField] private bool unlocked = false;

        float _gauge = 1f;
        float _activeUntil = -1f;
        bool _active;

        public bool IsUnlocked => unlocked;
        public bool IsActive => _active;
        public float Gauge01 => _gauge;
        public float SpeedMul => speedMul;

        public event Action<bool> OnActiveChanged;

        public void SetUnlocked(bool v)
        {
            unlocked = v;
            Debug.Log($"[BoostSystem] Unlocked={v}");
        }

        void Update()
        {
            if (!unlocked) return;
            var rc = Services.Run as RunController;
            if (rc == null || rc.CurrentStage != RunStage.Driving) { SetActive(false); return; }

            if (_active && Time.time >= _activeUntil)
            {
                SetActive(false);
            }

            if (!_active && _gauge >= 1f && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _gauge = 0f;
                _activeUntil = Time.time + boostDuration;
                SetActive(true);
            }

            if (!_active && _gauge < 1f)
            {
                _gauge = Mathf.Min(1f, _gauge + Time.deltaTime / rechargeTime);
            }
        }

        void SetActive(bool v)
        {
            if (_active == v) return;
            _active = v;
            OnActiveChanged?.Invoke(v);
        }
    }
}
