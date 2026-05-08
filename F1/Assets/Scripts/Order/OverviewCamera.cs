using UnityEngine;

/// <summary>
/// 오버뷰 ↔ 팔로우 카메라 전환.
/// 시작 시 / 정산 후 = 오버뷰 (탑다운).
/// 런 중 = 팔로우 (FollowCamera 활성).
/// </summary>
[RequireComponent(typeof(Camera))]
public class OverviewCamera : MonoBehaviour, IOverviewCamera
{
    [Header("Overview Pose")]
    [SerializeField] private Vector3 overviewCenter = new Vector3(36f, 0f, 24f); // 13x9 cell=6m → ~78x54m, 중심
    [SerializeField] private float overviewHeight = 95f;
    [SerializeField] private float overviewPitchDeg = 75f; // 75° = 거의 직하, 약간 기울임
    [SerializeField] private float overviewFov = 55f;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.8f;

    Camera cam;
    FollowCamera follow;
    Vector3 followStartPos;
    Quaternion followStartRot;
    float followStartFov;
    bool transitioning;
    float t;
    bool toOverview;
    System.Action onComplete;

    public bool IsInOverview { get; private set; } = true; // 시작은 오버뷰

    void Awake()
    {
        cam = GetComponent<Camera>();
        follow = GetComponent<FollowCamera>();
        if (follow != null) follow.enabled = false;

        // 즉시 오버뷰 포즈
        ApplyOverviewPose();
    }

    void ApplyOverviewPose()
    {
        ComputeOverviewPose(out var pos, out var rot);
        transform.position = pos;
        transform.rotation = rot;
        cam.fieldOfView = overviewFov;
    }

    void ComputeOverviewPose(out Vector3 pos, out Quaternion rot)
    {
        // pitch가 X-rotation = 카메라가 아래를 본다 (90이면 완전 직하).
        // horizontal back offset = height / tan(pitch)
        float pitchRad = overviewPitchDeg * Mathf.Deg2Rad;
        float horizDist = overviewHeight / Mathf.Max(0.01f, Mathf.Tan(pitchRad));
        // 카메라는 center에서 위로 height, Z축 -horizDist (살짝 뒤). LookAt(center)로 회전 자동.
        pos = overviewCenter + new Vector3(0f, overviewHeight, -horizDist);
        rot = Quaternion.LookRotation(overviewCenter - pos, Vector3.up);
    }

    void LateUpdate()
    {
        if (!transitioning) return;

        t += Time.unscaledDeltaTime / transitionDuration;
        float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        if (toOverview)
        {
            ComputeOverviewPose(out var targetPos, out var targetRot);
            transform.position = Vector3.Lerp(followStartPos, targetPos, u);
            transform.rotation = Quaternion.Slerp(followStartRot, targetRot, u);
            cam.fieldOfView = Mathf.Lerp(followStartFov, overviewFov, u);
        }
        else
        {
            // 오버뷰에서 팔로우로 — 팔로우 카메라가 즉시 따라잡도록 마지막에 활성화
            // (트랜지션 중엔 카메라가 그대로, 끝나면 follow.enabled = true)
        }

        if (t >= 1f)
        {
            transitioning = false;
            if (toOverview)
            {
                IsInOverview = true;
            }
            else
            {
                IsInOverview = false;
                if (follow != null) follow.enabled = true;
            }
            onComplete?.Invoke();
            onComplete = null;
        }
    }

    public void GoToOverview(System.Action onDone = null)
    {
        if (follow != null) follow.enabled = false;
        followStartPos = transform.position;
        followStartRot = transform.rotation;
        followStartFov = cam.fieldOfView;
        toOverview = true;
        t = 0f;
        transitioning = true;
        onComplete = onDone;
    }

    public void GoToFollow(System.Action onDone = null)
    {
        // 즉시 follow로 (팔로우 카메라가 자체 smoothdamp로 따라옴)
        if (follow != null) follow.enabled = true;
        IsInOverview = false;
        toOverview = false;
        transitioning = false; // 짧게 처리
        onDone?.Invoke();
    }
}
