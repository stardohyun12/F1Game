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

        ParticleSystem _exhaustL, _exhaustR;

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
            if (v) EnsureExhaust();
            if (_exhaustL != null) { if (v) _exhaustL.Play(); else _exhaustL.Stop(); }
            if (_exhaustR != null) { if (v) _exhaustR.Play(); else _exhaustR.Stop(); }
        }

        // Day 4 — 부스트 활성 시 차 뒤 양쪽 exhaust 파티클
        void EnsureExhaust()
        {
            if (_exhaustL != null) return;
            var car = Services.Car?.Transform;
            if (car == null) return;
            _exhaustL = MakeExhaust(car, new Vector3(-0.45f, 0.25f, -1.15f));
            _exhaustR = MakeExhaust(car, new Vector3(+0.45f, 0.25f, -1.15f));
        }

        ParticleSystem MakeExhaust(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("BoostExhaust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"));
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ps.main;
            main.startLifetime = 0.45f;
            main.startSpeed = 6.5f;
            main.startSize = 0.4f;
            main.startColor = new Color(1f, 0.65f, 0.15f, 0.95f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = 80f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.05f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 1f, 0.7f), 0f),
                        new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.5f),
                        new GradientColorKey(new Color(0.3f, 0.1f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f),
                        new GradientAlphaKey(0.5f, 0.5f),
                        new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 2.5f));

            ps.Stop();
            return ps;
        }
    }
}
