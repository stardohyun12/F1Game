using UnityEngine;

/// <summary>
/// 트렁크 내 화물 물리 시뮬 — 채널 5 (다물건). 단일 박스 → N개 아이템.
///
/// 핵심 모델: N개(1~3) 화물 큐브가 트렁크 **바닥**에 놓여있고, 각자 슬라이드.
/// 작용 힘 2종류 (모든 아이템 공통):
///   1. 관성력 (차 가속/제동/코너 시)
///   2. 사면 중력 (차체 기울기 시) — D-044
/// 추가:
///   - 아이템 간 충돌 (2D AABB 밀어내기) — 잘 배치하면 서로 안 부딪힘
///   - 아이템별 벽 충돌 손상 — 각 아이템 자기 WrapState로 흡수 (각자 따로 포장)
///   - 공유 CargoBox HP 풀 (흡수만 아이템별 차등)
///
/// 동작:
/// - 차 평지 정지 → 아이템 가만히
/// - 차 가속/제동/코너/사면 → 관성·사면 중력으로 미끄러짐
/// - 미끄러진 아이템이 트렁크 벽 or 다른 아이템에 부딪힘 → 손상
///
/// 초기 배치는 그리드 자동 (임시 — Plan 2에서 플레이어 드래그 배치로 대체).
/// </summary>
[DisallowMultipleComponent]
public class CargoShakeSimulator : MonoBehaviour, ICargoShakeSimulator
{
    [SerializeField] CargoBox cargo;

    [Header("Trunk Visual Match (D-048 — yaw만 매칭)")]
    [Tooltip("트렁크 yaw만 차량 매칭. pitch/roll 무시 → 트렁크 visual 항상 평면.")]
    [SerializeField] bool matchCarRotation = true;
    [Tooltip("CargoSimCamRig 자동 추적 (trunk yaw 따라가서 시점 일관 유지).")]
    [SerializeField] Transform simCamRig;

    [Header("Trunk Bounds")]
    [Tooltip("트렁크 내벽 기준 아이템 중심 이동 한계 (CargoSimRoot local, m). 벽 ±1.0.")]
    [SerializeField] float trunkHalfExtent = 1.0f;
    [Tooltip("마운트 시 아이템 균일 스케일 (m). 3개가 트렁크에 들어가는 크기.")]
    [SerializeField] float itemScale = 0.5f;

    [Header("Position Physics (패턴 C+ — 자동차 안 화물 느낌)")]
    // D-046 (도현 세션 15): "시뮬레이션 느낌" 진단. 마찰 강화 + 반응 줄임.
    [Tooltip("정지 마찰 임계 G. 낮을수록 작은 G에도 미끄러짐.")]
    [SerializeField] float staticFrictionG = 0.08f;     // 0.15 너무 정적 → 0.08 일반 가속/제동에서 슬라이드
    [Tooltip("운동 마찰 감쇠율 (per sec). 낮을수록 오래 미끄러짐.")]
    [SerializeField] float kineticFriction = 1.0f;       // 1.5 → 1.0 (적당한 감쇠)
    [Tooltip("G 1단위를 m/s²로 변환. 높을수록 아이템이 강하게 밀려나감.")]
    [SerializeField] float accelScale = 1.5f;            // 1.0 → 1.5 (가속 반응 살림)
    [Tooltip("벽 충돌 시 반발 (0=멈춤, 0.5=절반 반발). 0.05 = 거의 멈춤.")]
    [SerializeField] float wallBounce = 0.05f;
    [Tooltip("velocity 이하이면 정지로 간주 (snap to 0).")]
    [SerializeField] float velocityDeadZone = 0.025f;    // 0.05 → 0.025 (정상 운전 진동 감지)

    [Header("Box Wall Impact → CargoBox Damage (세션 20)")]
    [Tooltip("trunk 벽 박을 때 아이템 속도(m/s)당 데미지. 충돌이 진짜 손상 인과.")]
    [SerializeField] float boxWallDamageMul = 8f;
    [Tooltip("이 속도(m/s) 이하 벽 충돌은 무시 (미세 진동 흡수).")]
    [SerializeField] float boxWallSpeedThreshold = 0.15f;

    // 트렁크에 마운트된 화물 1개의 slide 상태.
    class TrunkItem
    {
        public Transform t;
        public Vector3 velLocal;                       // CargoSimRoot local frame 속도
        public PitCrew.Packing.WrapState wrap;         // 이 아이템의 포장 상태 (손상 흡수용). null 가능.
        public float halfExtent;                       // XZ 충돌·clamp용 반경 (m)
    }

    readonly System.Collections.Generic.List<TrunkItem> items = new();

    // 아이템이 트렁크 바닥에 놓이는 local Y (placeholder Box localPos.y와 동일).
    const float kFloorY = 0.33f;

    // 그리드 자동 배치 슬롯 X (Plan 2에서 플레이어 드래그로 대체).
    static readonly float[] kSlotX = { -0.5f, 0f, 0.5f };

    bool placeholderHidden;
    float graceUntil = -1f;

    /// <summary>런 진입 직후 N초간 화물 손상 무효 (진입 순간 자동 손상 방지).</summary>
    public void StartGracePeriod(float seconds = 1.0f)
    {
        graceUntil = Time.time + seconds;
    }

    void Awake()
    {
        if (cargo == null) cargo = Services.Cargo as CargoBox;
    }

    // =========================================================
    //  마운트 / 청소
    // =========================================================
    /// <summary>
    /// 포장 큐브 1개를 트렁크에 마운트. 채널 5 Plan 2:
    /// 큐브가 이미 CargoSimRoot(=this.transform)의 자식이면 PackingScreen 배치 단계에서
    /// 플레이어가 끌어다 놓은 것 → 그 위치 유지. 아니면 자동 그리드 (폴백).
    /// </summary>
    public void MountWrappedItem(GameObject cube)
    {
        if (cube == null) return;
        EnsurePlaceholderHidden();

        // 배치 단계에서 플레이어가 트렁크에 직접 놓았으면 이미 자식이다.
        bool alreadyPlaced = cube.transform.parent == transform;
        Vector3 placedLocal = cube.transform.localPosition;

        cube.transform.SetParent(transform, false);    // CargoSimRoot 직속 (이미 자식이면 localPos 유지)
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = Vector3.one * itemScale;

        // Day 5 (2026-05-19): CargoType별 색 적용 (placeholder visual).
        // 후속: Kenney prop pack 자산으로 mesh swap.
        if (cargo != null)
        {
            var mr = cube.GetComponent<MeshRenderer>() ?? cube.GetComponentInChildren<MeshRenderer>();
            if (mr != null && mr.material != null)
            {
                mr.material.color = CargoTypePresets.BoxColor(cargo.Type);
            }
        }

        var item = new TrunkItem
        {
            t = cube.transform,
            velLocal = Vector3.zero,
            wrap = cube.GetComponentInChildren<PitCrew.Packing.WrapState>(),
            halfExtent = itemScale * 0.5f,
        };
        items.Add(item);

        if (alreadyPlaced)
        {
            // 플레이어가 놓은 위치 유지 — 바닥 Y만 보정.
            cube.transform.localPosition = new Vector3(placedLocal.x, kFloorY, placedLocal.z);
        }
        else
        {
            // 폴백 자동 그리드 — 추가된 순서대로 슬롯 (배치 단계 없이 들어온 경우).
            int slot = items.Count - 1;
            float sx = slot < kSlotX.Length ? kSlotX[slot] : 0f;
            cube.transform.localPosition = new Vector3(sx, kFloorY, 0f);
        }
    }

    public void ClearMountedItems()
    {
        foreach (var it in items) if (it.t != null) Destroy(it.t.gameObject);
        items.Clear();
    }

    // placeholder Box는 1회만 숨김 (포장 큐브가 그 자리 대신).
    void EnsurePlaceholderHidden()
    {
        if (placeholderHidden) return;
        var box = transform.Find("Box");
        if (box != null) { var mr = box.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false; }
        placeholderHidden = true;
    }

    // ── 배치 단계(PackingScreen Plan 2)용 노출 ────────────────────────
    /// <summary>배치 단계 진입 시 placeholder Box 미리 숨김.</summary>
    public void HidePlaceholder() => EnsurePlaceholderHidden();
    /// <summary>아이템 마운트 시 균일 스케일 (m).</summary>
    public float ItemScale => itemScale;
    /// <summary>트렁크 내벽 기준 아이템 중심 이동 한계 (local, m).</summary>
    public float TrunkHalfExtent => trunkHalfExtent;
    /// <summary>아이템이 놓이는 트렁크 바닥 local Y.</summary>
    public float FloorY => kFloorY;

    // =========================================================
    //  물리 — 아이템별 slide + 벽 충돌 + 아이템 간 충돌
    // =========================================================
    void FixedUpdate()
    {
        if (cargo == null) { cargo = Services.Cargo as CargoBox; if (cargo == null) return; }
        if (items.Count == 0) return;

        float dt = Time.fixedDeltaTime;

        // 두 힘은 모든 아이템 공통 (차 G + 사면 중력) — 1회 계산.
        // (1) 관성력: local x = lateral(우+), local z = longitudinal(앞+).
        Vector3 inertialAccel = new Vector3(-cargo.SignedLatG, 0f, -cargo.SignedLongG) * accelScale;
        // (2) 사면 중력 (D-044): 차체 기울면 중력의 차 local x/z 성분이 아이템을 밀어냄.
        Vector3 localGravity = cargo.transform.InverseTransformDirection(Physics.gravity);
        Vector3 slopeAccel = new Vector3(localGravity.x, 0f, localGravity.z) / 9.81f * accelScale;
        Vector3 totalAccel = inertialAccel + slopeAccel;
        float accelMag = Mathf.Sqrt(totalAccel.x * totalAccel.x + totalAccel.z * totalAccel.z);
        float dampFactor = Mathf.Max(0f, 1f - kineticFriction * dt);

        foreach (var it in items)
        {
            if (it.t == null) continue;

            // 정지 마찰: accel 크기가 임계 미만이면 push 없음.
            if (accelMag >= staticFrictionG * accelScale) it.velLocal += totalAccel * dt;

            // 운동 마찰: 항상 감쇠.
            it.velLocal.x *= dampFactor;
            it.velLocal.z *= dampFactor;
            if (it.velLocal.sqrMagnitude < velocityDeadZone * velocityDeadZone) it.velLocal = Vector3.zero;

            // 위치 적분.
            Vector3 p = it.t.localPosition + it.velLocal * dt;
            float lim = trunkHalfExtent - it.halfExtent;

            // 벽 충돌 (절대 트렁크 AABB) + 손상 전파.
            if (Mathf.Abs(p.x) > lim)
            {
                float impactSpeed = Mathf.Abs(it.velLocal.x);
                p.x = Mathf.Sign(p.x) * lim;
                it.velLocal.x = -it.velLocal.x * wallBounce;
                TryApplyBoxImpactDamage(it, impactSpeed, new Vector3(Mathf.Sign(p.x), 0f, 0f));
            }
            if (Mathf.Abs(p.z) > lim)
            {
                float impactSpeed = Mathf.Abs(it.velLocal.z);
                p.z = Mathf.Sign(p.z) * lim;
                it.velLocal.z = -it.velLocal.z * wallBounce;
                TryApplyBoxImpactDamage(it, impactSpeed, new Vector3(0f, 0f, Mathf.Sign(p.z)));
            }
            p.y = kFloorY; // 바닥 고정
            it.t.localPosition = p;
        }

        ResolveItemCollisions();
    }

    /// <summary>아이템 간 충돌 — XZ 평면 AABB 겹침 검사 + 밀어내기 (MTV).</summary>
    void ResolveItemCollisions()
    {
        for (int i = 0; i < items.Count; i++)
        for (int j = i + 1; j < items.Count; j++)
        {
            var a = items[i]; var b = items[j];
            if (a.t == null || b.t == null) continue;

            Vector3 pa = a.t.localPosition, pb = b.t.localPosition;
            float minDist = a.halfExtent + b.halfExtent;
            float dx = pb.x - pa.x, dz = pb.z - pa.z;
            float overlapX = minDist - Mathf.Abs(dx);
            float overlapZ = minDist - Mathf.Abs(dz);
            if (overlapX <= 0f || overlapZ <= 0f) continue; // 안 겹침

            // 더 얕게 겹친 축으로 분리 (AABB MTV) — 각 아이템 절반씩.
            if (overlapX < overlapZ)
            {
                float push = (dx >= 0f ? 1f : -1f) * overlapX * 0.5f;
                pa.x -= push; pb.x += push;
                float avg = (a.velLocal.x + b.velLocal.x) * 0.5f;
                a.velLocal.x = avg * 0.5f; b.velLocal.x = avg * 0.5f; // 충돌 감쇠
            }
            else
            {
                float push = (dz >= 0f ? 1f : -1f) * overlapZ * 0.5f;
                pa.z -= push; pb.z += push;
                float avg = (a.velLocal.z + b.velLocal.z) * 0.5f;
                a.velLocal.z = avg * 0.5f; b.velLocal.z = avg * 0.5f;
            }
            a.t.localPosition = pa; b.t.localPosition = pb;
        }
    }

    void TryApplyBoxImpactDamage(TrunkItem it, float speed, Vector3 impactDirLocal)
    {
        if (Time.time < graceUntil) return; // grace period — 런 진입 직후 손상 무시
        if (cargo == null || speed < boxWallSpeedThreshold) return;
        float raw = (speed - boxWallSpeedThreshold) * boxWallDamageMul;
        if (raw <= 0f) return;

        // 충격 흡수율 — 이 아이템 자기 WrapState로 계산 (각자 따로 포장).
        float absorption = 0f;
        if (it.wrap != null && it.t != null)
        {
            Vector3 worldHit = it.t.position;
            Vector3 worldDir = it.t.TransformDirection(impactDirLocal).normalized;
            absorption = it.wrap.QueryAbsorption(worldHit, worldDir);
        }
        float dmg = raw * (1f - absorption);
        if (absorption > 0.01f)
            Debug.Log($"[CargoShake] 충격 흡수 {absorption * 100f:F0}% (raw {raw:F1} → {dmg:F1})");
        if (dmg > 0.01f) cargo.ApplyExternalDamage(dmg, flashSpeedKmh: speed * 3.6f);
    }

    // =========================================================
    //  트렁크 visual yaw 매칭 + SimCamRig 추적 (D-048)
    // =========================================================
    void LateUpdate()
    {
        if (!matchCarRotation || cargo == null) return;

        float carYaw = cargo.transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, carYaw, 0f);

        // SimCamRig도 yaw만 차량 매칭. SerializeField 비어있으면 1회 fallback.
        if (simCamRig == null)
        {
            var rigGO = GameObject.Find("CargoSimCamRig");
            if (rigGO != null)
            {
                simCamRig = rigGO.transform;
                Debug.LogWarning("[CargoShakeSimulator] simCamRig을 GameObject.Find로 fallback 설정. SetupCargoView 메뉴 실행 권장.");
            }
        }
        if (simCamRig != null)
        {
            Vector3 trunkPos = transform.position;
            Vector3 offsetLocal = new Vector3(0f, 2.5f, -0.8f); // SetupCargoView default offset
            simCamRig.position = trunkPos + Quaternion.Euler(0f, carYaw, 0f) * offsetLocal;
            simCamRig.rotation = Quaternion.Euler(65f, carYaw, 0f);
        }
    }

    /// <summary>
    /// 화물 색상 변경 (D-043). RunController.BeginRun에서 화물 타입에 따라 자동 호출.
    /// 마운트된 모든 아이템의 base material을 색칠.
    /// </summary>
    public void SetBoxColor(Color c)
    {
        foreach (var it in items)
        {
            if (it.t == null) continue;
            var mr = it.t.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = c;
        }
    }
}
