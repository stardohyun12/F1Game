using UnityEngine;

[DisallowMultipleComponent]
public class DriftVfxController : MonoBehaviour
{
    [SerializeField] private ArcadeCarController car;
    [SerializeField] private TrailRenderer[] skidTrails;
    [SerializeField] private ParticleSystem[] smokeParticles;

    void Awake()
    {
        if (car == null)
            car = GetComponentInParent<ArcadeCarController>();
        if (car == null)
            car = GetComponent<ArcadeCarController>();
    }

    void Update()
    {
        if (car == null) return;
        bool drifting = car.IsDrifting;

        if (skidTrails != null)
        {
            for (int i = 0; i < skidTrails.Length; i++)
            {
                var t = skidTrails[i];
                if (t == null) continue;
                if (t.emitting != drifting) t.emitting = drifting;
            }
        }

        if (smokeParticles != null)
        {
            for (int i = 0; i < smokeParticles.Length; i++)
            {
                var ps = smokeParticles[i];
                if (ps == null) continue;
                var emission = ps.emission;
                if (emission.enabled != drifting) emission.enabled = drifting;
            }
        }
    }
}
