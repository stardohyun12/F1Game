using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 기능 1b(D-219): 걷는 동안 Main Camera 를 움직이는 1인칭 카메라 — 발 위 1.6 m 눈높이, 마우스로 돌림(상하 ±80°).
/// 몸(캡슐)은 그리지 않고 그림자만 남긴다. 계단 단(0.165 m)마다 CharacterController 가 툭 올라가는 걸 높이만 짧게 늦춰 따라간다.
/// 시야각은 게임 기본 FOV(FollowCamera 정지 시 FOV) 그대로, near clip 0.05 — 문·벽에 붙어도 안 뚫린다.
/// 같은 카메라라 LowResRenderTest(720 Nearest)는 그대로 먹는다. 카트를 타면 감독이 이걸 끄고 FollowCamera(B)를 켠다.
/// </summary>
public class OnFootCamera : MonoBehaviour
{
    public Transform target;
    [SerializeField] float eyeHeight = 1.6f;
    [SerializeField] float minPitch = -80f, maxPitch = 80f;
    [Tooltip("마우스 1 픽셀당 도.")]
    [SerializeField] float mouseSensitivity = 0.12f;
    [SerializeField] float nearClip = 0.05f;
    [Tooltip("눈높이가 발 높이 변화를 따라가는 시간(초). 계단 덜컹임을 줄인다. 0 = 바로.")]
    [SerializeField] float heightSmoothTime = 0.08f;
    [Tooltip("늦춰 따라가는 높이가 실제 눈높이에서 벌어질 수 있는 최대(m). 이보다 크면 붙인다(순간이동·낙하).")]
    [SerializeField] float maxHeightLag = 0.25f;

    [Header("검증 봇 입력(마우스 대신)")]
    public bool simActive;

    public float Yaw { get; set; }
    public float Pitch { get; set; }
    /// <summary>게임 기본 FOV. 감독이 FollowCamera 정지 FOV 를 넣는다. 0 이면 켤 때 카메라 FOV 를 그대로 쓴다.</summary>
    public float Fov { get; set; }

    Camera cam;
    float eyeY, eyeVel, savedNear;
    bool hasEye;

    void Awake() { cam = GetComponent<Camera>(); }

    void OnEnable()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam != null) { savedNear = cam.nearClipPlane; cam.nearClipPlane = nearClip; if (Fov > 1f) cam.fieldOfView = Fov; }
        Pitch = 0f;
        if (target != null) Yaw = target.eulerAngles.y;
        hasEye = false;
        SetBodyVisible(false);
        if (!simActive) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    void OnDisable()
    {
        if (cam != null) cam.nearClipPlane = savedNear;
        SetBodyVisible(true);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    /// <summary>봇·연출용: 마우스를 이만큼 돌린 것과 같다.</summary>
    public void AddLook(float yawDeg, float pitchDeg) { Yaw += yawDeg; Pitch = Mathf.Clamp(Pitch + pitchDeg, minPitch, maxPitch); }

    /// <summary>봇·연출용: 이 점을 바라보게(상하 ±80° 안에서).</summary>
    public void LookAt(Vector3 world)
    {
        Vector3 d = world - EyeTarget();
        if (d.sqrMagnitude < 1e-6f) return;
        Yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        Pitch = Mathf.Clamp(-Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg, minPitch, maxPitch);
    }

    Vector3 EyeTarget() => target.position + Vector3.up * eyeHeight;

    void LateUpdate()
    {
        if (target == null) return;
        if (!simActive && Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 d = Mouse.current.delta.ReadValue() * mouseSensitivity;
            AddLook(d.x, -d.y);
        }
        if (!simActive && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        Pitch = Mathf.Clamp(Pitch, minPitch, maxPitch);

        // 수평은 발에 딱 붙이고 높이만 늦춘다 — 계단 단마다 0.165 m 씩 툭툭 올라가는 걸 부드럽게.
        Vector3 eye = EyeTarget();
        if (!hasEye || heightSmoothTime <= 0f || Mathf.Abs(eye.y - eyeY) > 1f) { eyeY = eye.y; eyeVel = 0f; hasEye = true; }
        else
        {
            eyeY = Mathf.SmoothDamp(eyeY, eye.y, ref eyeVel, heightSmoothTime);
            eyeY = Mathf.Clamp(eyeY, eye.y - maxHeightLag, eye.y + maxHeightLag);
        }
        transform.SetPositionAndRotation(new Vector3(eye.x, eyeY, eye.z), Quaternion.Euler(Pitch, Yaw, 0f));
    }

    // 1인칭 = 몸이 화면을 가린다 → 걷는 동안 몸은 그림자만 그린다.
    void SetBodyVisible(bool on)
    {
        if (target == null) return;
        foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            if (r != null) r.shadowCastingMode = on ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    }
}
