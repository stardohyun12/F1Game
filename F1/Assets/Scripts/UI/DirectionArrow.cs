using UnityEngine;

/// <summary>
/// 월드 방향 화살표 (W2 채널 8 — 지도/내비).
/// 차 위에 떠서 현재 배달 목표(또는 복귀 지점) 방향을 수평으로 가리킨다.
/// 자가구성 — UITheme.arrowNorth (Kenney 화살표) 스프라이트를 월드 SpriteRenderer로 띄움.
///
/// 미니맵(전체 위치 파악) + 화면 가장자리 화살표(DeliveryHUD, 화면 밖 목표)와 역할 분담:
/// 이건 "지금 어느 쪽으로 핸들" — 차 바로 위에서 3D로 방향 제시.
///
/// 런 중 + 목표 있을 때만 표시.
/// </summary>
[DisallowMultipleComponent]
public class DirectionArrow : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("차 위로 띄우는 높이 (m).")]
    [SerializeField] float heightAboveCar = 2.6f;
    [Tooltip("화살표 스프라이트 크기 (m).")]
    [SerializeField] float arrowSize = 1.8f;

    // 스프라이트 자식 회전 — 코드 하드코드 (씬에 baked 안 되게 → 재컴파일만으로 조정 가능).
    // arrow_basic_n은 +Y가 화살표 끝. 부모는 목표 방향으로 yaw.
    //  - X=-90: 완전 수평 (바닥에 누움) → 추격 카메라가 edge-on으로 봄 (안 보임).
    //  - X=-130: 40° 뒤로 젖혀 앞면이 추격 카메라를 향함 → 잘 보임 + 끝이 살짝 위로.
    //  - Y=180: 스프라이트 끝(+Y)이 부모 +Z(목표)를 향하도록 보정.
    static readonly Quaternion k_SpriteLocalRot = Quaternion.Euler(-130f, 180f, 0f);

    Transform carT;
    DeliveryManager delivery;
    SpriteRenderer sr;

    void Awake()
    {
        BuildIfNeeded();
    }

    void BuildIfNeeded()
    {
        if (sr != null) return;
        var go = new GameObject("ArrowSprite");
        go.transform.SetParent(transform, false);
        go.transform.localRotation = k_SpriteLocalRot;
        go.transform.localScale = Vector3.one * arrowSize;
        sr = go.AddComponent<SpriteRenderer>();

        var theme = UITheme.Active;
        if (theme != null && theme.arrowNorth != null)
        {
            sr.sprite = theme.arrowNorth;
            sr.color = UIPalette.Lemon;
        }
        else
        {
            sr.color = UIPalette.Lemon; // 폴백 레몬
        }
    }

    void LateUpdate()
    {
        if (carT == null) { var c = Services.Car; if (c != null) carT = c.Transform; }
        if (delivery == null) delivery = Services.Delivery as DeliveryManager;

        var run = Services.Run as RunController;
        bool runActive = run != null && run.RunActive;

        // 목표 위치 결정 (DeliveryHUD와 동일 우선순위: 복귀 > 현재 목표).
        Vector3 targetPos = Vector3.zero;
        bool hasTarget = false;
        if (runActive && delivery != null)
        {
            if (delivery.Returning)
            {
                if (delivery.ReturnTarget != null) { targetPos = delivery.ReturnTarget.position; hasTarget = true; }
            }
            else if (!delivery.Finished)
            {
                var t = delivery.CurrentTarget;
                if (t != null) { targetPos = t.transform.position; hasTarget = true; }
            }
        }

        bool show = runActive && hasTarget && carT != null;
        if (sr != null && sr.enabled != show) sr.enabled = show;
        if (!show) return;

        // 차 위에 떠 있기
        transform.position = carT.position + Vector3.up * heightAboveCar;

        // 목표 방향으로 수평 yaw
        Vector3 dir = targetPos - carT.position;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // 거리 시각 피드백 — 멀면 노랑, 가까우면(< 15m) 녹색으로 보간 + 약한 펄스.
        if (sr != null)
        {
            float k = Mathf.Clamp01((15f - dist) / 15f); // 0..1 (가까울수록 1)
            var far = UIPalette.Lemon;     // D-121 토큰: 멀면 레몬, 가까우면 올리브
            var near = UIPalette.Olive;
            var c = Color.Lerp(far, near, k);
            // 가까우면 펄스
            if (k > 0f) c *= 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f);
            c.a = 1f;
            sr.color = c;
        }
    }
}
