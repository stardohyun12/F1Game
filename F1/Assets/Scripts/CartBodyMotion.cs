using UnityEngine;

/// <summary>
/// D-257 차체·짐칸 출렁임 (보이는 것만 — 물리 차체는 그대로).
/// 차체(A6_KartVisual/KartBody, 없으면 옛 "body")를 바퀴 위에서 앞뒤·좌우로 기울이고, 턱에서 위아래로 출렁이게 한다.
/// 짐칸 상자(delivery_box + 끈)는 차체보다 한 박자 늦게, 더 크게 흔들린다 — 화물이 실려 있다는 게 보이게.
/// 스프링(진동수 Hz·감쇠비)이라 멈춘 뒤 한두 번 되흔들린다(Lerp 는 되흔들림이 없어 딱딱해 보였다).
/// ArcadeCarController.Awake 가 없으면 붙인다. 값은 차의 주행감 프리셋(DriveFeel)에서 — A 는 전부 0 = 끔.
/// </summary>
[DisallowMultipleComponent]
public class CartBodyMotion : MonoBehaviour
{
    ArcadeCarController car;
    Rigidbody rb;
    Transform body, box;
    Vector3 bodyRestPos, boxRestPos; Quaternion bodyRestRot, boxRestRot;   // 차 기준(body)·부모 기준(box)
    Vector3 pivotLocal;
    bool posed;

    Vector3 prevVel; Vector2 accLP; float prevVy, vyMean;
    // 스프링 상태: x = pitch, y = roll (도)
    Vector2 bodyAng, bodyAngVel, boxAng, boxAngVel;
    float heave, heaveVel;

    /// <summary>QA 측정: 지금·최대 차체 기울기(도)·짐칸 흔들림(도)·위아래(m). ResetPeaks 로 0.</summary>
    public float BodyPitch => bodyAng.x;
    public float BodyRoll => bodyAng.y;
    public float BoxSwayDeg => boxAng.magnitude;
    public float PeakBodyPitch { get; private set; }
    public float PeakBodyRoll { get; private set; }
    public float PeakBoxSway { get; private set; }
    public float PeakHeave { get; private set; }
    public void ResetPeaks() { PeakBodyPitch = PeakBodyRoll = PeakBoxSway = PeakHeave = 0f; }
    public bool HasBody => body != null;
    public bool HasBox => box != null;

    void Awake()
    {
        car = GetComponent<ArcadeCarController>();
        rb = GetComponent<Rigidbody>();
    }

    void Resolve()
    {
        var kb = transform.Find("A6_KartVisual/KartBody");
        body = kb != null && kb.gameObject.activeInHierarchy ? kb : null;
        if (body == null) { var b = transform.Find("body"); if (b != null && b.gameObject.activeInHierarchy) body = b; }
        box = null;
        if (body != null)
        {
            bodyRestPos = transform.InverseTransformPoint(body.position);
            bodyRestRot = Quaternion.Inverse(transform.rotation) * body.rotation;
            pivotLocal = new Vector3(0f, bodyRestPos.y + 0.5f, 0f);   // 차 기준 0.5(×0.7 스케일 ≈ 0.35m) — 무게중심 높이쯤에서 기운다
            box = MakeBoxPivot(body);
            if (box != null) { boxRestPos = box.localPosition; boxRestRot = box.localRotation; }
        }
        posed = false;
    }

    /// <summary>짐칸 상자와 끈을 한 덩어리로 묶는 회전축(상자 밑면 가운데)을 만든다. 런타임에만 — 씬에 저장되지 않는다.</summary>
    static Transform MakeBoxPivot(Transform root)
    {
        var existing = FindDeep(root, "CargoSwayPivot");
        if (existing != null) return existing;
        var db = FindDeep(root, "delivery_box");
        if (db == null) return null;
        var parent = db.parent;
        var r = db.GetComponent<Renderer>();
        Vector3 baseW = r != null ? new Vector3(r.bounds.center.x, r.bounds.min.y, r.bounds.center.z) : db.position;
        var piv = new GameObject("CargoSwayPivot").transform;
        piv.SetParent(parent, false);
        piv.position = baseW;
        piv.rotation = parent.rotation;
        var move = new System.Collections.Generic.List<Transform>();
        foreach (Transform c in parent) if (c.name.StartsWith("delivery_")) move.Add(c);
        foreach (var c in move) c.SetParent(piv, true);
        return piv;
    }

    static Transform FindDeep(Transform t, string n)
    {
        if (t.name == n) return t;
        foreach (Transform c in t) { var f = FindDeep(c, n); if (f != null) return f; }
        return null;
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        float dt = Time.fixedDeltaTime;
        Vector3 v = rb.linearVelocity;
        Vector3 a = (v - prevVel) / dt; prevVel = v;
        // 차 기준 앞뒤·좌우 가속(m/s²), 짧게 고른다(물리 한 칸 튐 제거)
        var raw = new Vector2(Vector3.Dot(a, transform.forward), Vector3.Dot(a, transform.right));
        if (raw.sqrMagnitude > 400f * 400f) raw = Vector2.zero;   // 순간이동(리스폰·주차) 튐
        accLP = Vector2.Lerp(accLP, raw, 1f - Mathf.Exp(-dt / 0.08f));
        // 턱: 위아래 속도의 갑작스런 변화(평균에서 벗어난 몫)
        float vy = v.y, dvy = vy - prevVy; prevVy = vy;
        vyMean = Mathf.Lerp(vyMean, dvy, 1f - Mathf.Exp(-dt / 0.3f));
        float jolt = Mathf.Abs(dvy) > 5f ? 0f : dvy - vyMean;

        var f = car != null ? car.Feel : default;
        if (f.bodyPitchPerG <= 0f) return;
        float load = car != null ? Mathf.Clamp01(car.CargoLoadKg / 45f) * f.weightInfluence : 0f;
        float gLong = accLP.x / 9.81f, gLat = accLP.y / 9.81f;

        // 차체: 가속 → 앞이 들림(−pitch), 오른쪽으로 돌면 바깥(왼쪽)으로 기움(+roll)
        float bodyAmp = 1f + 0.5f * load;
        var bodyTarget = new Vector2(Mathf.Clamp(-gLong * f.bodyPitchPerG * bodyAmp, -f.bodyMaxDeg, f.bodyMaxDeg),
                                     Mathf.Clamp(gLat * f.bodyRollPerG * bodyAmp, -f.bodyMaxDeg, f.bodyMaxDeg));
        Spring(ref bodyAng, ref bodyAngVel, bodyTarget, f.bodyFreq, f.bodyDampRatio, dt);
        float w = 2f * Mathf.PI * Mathf.Max(0.5f, f.bodyFreq * 1.4f);
        heaveVel += -jolt * f.bodyHeaveGain * w * (1f + load);
        float hAcc = -w * w * heave - 2f * f.bodyDampRatio * w * heaveVel;
        heaveVel += hAcc * dt; heave += heaveVel * dt;
        heave = Mathf.Clamp(heave, -0.08f, 0.08f);

        // 짐칸: 차체 기울기 + 자기 관성(무거울수록 크고 느리게)
        float boxAmp = 0.7f + 0.6f * load;
        float boxFreq = f.boxFreq * (1f - 0.25f * load);
        var boxTarget = new Vector2(Mathf.Clamp(-gLong * f.boxSwayPerG * boxAmp, -f.boxMaxDeg, f.boxMaxDeg),
                                    Mathf.Clamp(gLat * f.boxSwayPerG * boxAmp, -f.boxMaxDeg, f.boxMaxDeg));
        Spring(ref boxAng, ref boxAngVel, boxTarget, boxFreq, f.boxDampRatio, dt);
        boxAngVel.x += -jolt * 8f * boxAmp;   // 턱에서 상자가 톡 튄다

        PeakBodyPitch = Mathf.Max(PeakBodyPitch, Mathf.Abs(bodyAng.x));
        PeakBodyRoll = Mathf.Max(PeakBodyRoll, Mathf.Abs(bodyAng.y));
        PeakBoxSway = Mathf.Max(PeakBoxSway, boxAng.magnitude);
        PeakHeave = Mathf.Max(PeakHeave, Mathf.Abs(heave));
    }

    static void Spring(ref Vector2 x, ref Vector2 vel, Vector2 target, float hz, float zeta, float dt)
    {
        float w = 2f * Mathf.PI * Mathf.Max(0.1f, hz);
        Vector2 acc = w * w * (target - x) - 2f * zeta * w * vel;
        vel += acc * dt; x += vel * dt;
    }

    void LateUpdate()
    {
        if (body == null || !body.gameObject.activeInHierarchy) { Resolve(); if (body == null) return; }
        var f = car != null ? car.Feel : default;
        if (f.bodyPitchPerG <= 0f)
        {
            if (posed) Rest();   // A 로 바꾸면 제자리로
            return;
        }
        posed = true;
        var delta = Quaternion.Euler(bodyAng.x, 0f, bodyAng.y);
        Vector3 p = pivotLocal + delta * (bodyRestPos - pivotLocal) + Vector3.up * (heave / Mathf.Max(0.01f, transform.lossyScale.y));
        body.SetPositionAndRotation(transform.TransformPoint(p), transform.rotation * delta * bodyRestRot);
        if (box != null)   // 차 축 기준으로 돌린다(모델 안 부모가 뒤집혀 있어도 부호가 같게)
        {
            var boxD = Quaternion.Euler(boxAng.x, 0f, boxAng.y);
            box.rotation = transform.rotation * boxD * Quaternion.Inverse(transform.rotation) * box.parent.rotation * boxRestRot;
        }
    }

    void Rest()
    {
        posed = false;
        bodyAng = bodyAngVel = boxAng = boxAngVel = Vector2.zero; heave = heaveVel = 0f;
        body.SetPositionAndRotation(transform.TransformPoint(bodyRestPos), transform.rotation * bodyRestRot);
        if (box != null) { box.localPosition = boxRestPos; box.localRotation = boxRestRot; }
    }
}
