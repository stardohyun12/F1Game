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
        // 2111(D-149): 스키드 자국이 alignment=View 라 카메라 쪽으로 세워진 띠 → 높은 카메라에서 도로 위 검은 톱니 얼룩으로 보였다.
        // 바닥에 눕힌다: TransformZ 정렬 + 띠 면이 위를 보게(로컬 -90° X), 도로와 z 싸움 안 나게 3cm 띄움, 끝을 투명하게.
        if (skidTrails != null)
            foreach (var t in skidTrails)
            {
                if (t == null) continue;
                t.alignment = LineAlignment.TransformZ;
                t.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                var lp = t.transform.localPosition; if (lp.y < 0.08f) t.transform.localPosition = new Vector3(lp.x, 0.08f, lp.z);
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(t.startColor, 0f), new GradientColorKey(t.endColor, 1f) },
                          new[] { new GradientAlphaKey(t.startColor.a * .7f, 0f), new GradientAlphaKey(0f, 1f) });
                t.colorGradient = g;
            }
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
