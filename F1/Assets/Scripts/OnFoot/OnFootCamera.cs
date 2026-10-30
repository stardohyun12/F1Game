using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 기능 1: 걷는 동안 Main Camera 를 움직이는 3인칭 카메라 — 낮게 뒤에서(어깨 높이를 2.8 m 뒤에서).
/// 마우스로 돌린다. 벽·천장에 걸리면 SphereCast 로 앞으로 당겨 온다(집 안 필수).
/// 같은 카메라라 LowResRenderTest(720 Nearest)는 그대로 먹는다. 카트를 타면 감독이 이걸 끄고 FollowCamera 를 켠다.
/// </summary>
public class OnFootCamera : MonoBehaviour
{
    public Transform target;
    [SerializeField] float pivotHeight = 1.45f;
    [SerializeField] float distance = 2.8f;
    [SerializeField] float minDistance = 0.35f;
    [SerializeField] float collisionRadius = 0.2f;
    [SerializeField] float defaultPitch = 10f;
    [SerializeField] float minPitch = -35f, maxPitch = 60f;
    [Tooltip("마우스 1 픽셀당 도.")]
    [SerializeField] float mouseSensitivity = 0.12f;
    [SerializeField] float fov = 60f;
    [SerializeField] float nearClip = 0.08f;
    [Tooltip("당겨 온 거리를 다시 늘릴 때 시간(초). 줄일 때는 즉시.")]
    [SerializeField] float distanceRecoverTime = 0.25f;

    [Header("검증 봇 입력(마우스 대신)")]
    public bool simActive;

    public float Yaw { get; set; }
    public float Pitch { get; set; }

    Camera cam;
    float curDist, distVel, savedFov, savedNear;

    void Awake() { cam = GetComponent<Camera>(); }

    void OnEnable()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam != null) { savedFov = cam.fieldOfView; savedNear = cam.nearClipPlane; cam.fieldOfView = fov; cam.nearClipPlane = nearClip; }
        Pitch = defaultPitch;
        if (target != null) Yaw = target.eulerAngles.y;
        curDist = distance;
        if (!simActive) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    void OnDisable()
    {
        if (cam != null) { cam.fieldOfView = savedFov; cam.nearClipPlane = savedNear; }
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    /// <summary>봇·연출용: 마우스를 이만큼 돌린 것과 같다.</summary>
    public void AddLook(float yawDeg, float pitchDeg) { Yaw += yawDeg; Pitch = Mathf.Clamp(Pitch + pitchDeg, minPitch, maxPitch); }

    void LateUpdate()
    {
        if (target == null) return;
        if (!simActive && Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 d = Mouse.current.delta.ReadValue() * mouseSensitivity;
            AddLook(d.x, -d.y);
        }
        if (!simActive && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

        Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * pivotHeight;
        Vector3 back = rot * Vector3.back;
        float want = distance;
        if (Physics.SphereCast(pivot, collisionRadius, back, out var hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            want = Mathf.Max(minDistance, hit.distance);
        if (want < curDist) { curDist = want; distVel = 0f; }
        else curDist = Mathf.SmoothDamp(curDist, want, ref distVel, distanceRecoverTime);
        transform.SetPositionAndRotation(pivot + back * curDist, rot);
        // 좁은 방·복도에서 카메라가 등 뒤로 바짝 붙으면 몸이 화면을 가린다 → 그동안만 몸을 안 그린다(그림자는 남김).
        bool hide = curDist < hideBodyDistance;
        if (bodyRenderers == null || bodyOwner != target) { bodyOwner = target; bodyRenderers = target.GetComponentsInChildren<Renderer>(true); }
        foreach (var r in bodyRenderers) if (r != null) r.shadowCastingMode = hide ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
    }

    [Tooltip("카메라가 이 거리(m)보다 가까우면 몸을 숨긴다.")]
    [SerializeField] float hideBodyDistance = 0.9f;
    Renderer[] bodyRenderers; Transform bodyOwner;
}
