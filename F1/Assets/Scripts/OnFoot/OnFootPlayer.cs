using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 기능 1(과제 20261030-2243): 걷는 주인공. 모델 없음 — 1.7 m 캡슐 대역(D-149 사람 모델은 톤 합격 뒤).
/// CharacterController: stepOffset 0.3 → 계단 단 0.165·연석 0.12~0.156 은 오르고, 0.5 m 넘는 축대·담·울은 막힌다.
/// 이동 = 카메라 기준 WASD, Shift 달리기. 검증 봇은 sim* 로 같은 통로에 값을 넣는다(키 대신).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OnFootPlayer : MonoBehaviour
{
    [SerializeField] float walkSpeed = 3.0f;
    [SerializeField] float runSpeed = 5.0f;
    [SerializeField] float turnSpeedDeg = 720f;
    [SerializeField] float gravity = 20f;
    [Tooltip("내리막 계단에서 발이 떠 통통 튀지 않게 땅으로 붙여 주는 최대 거리(m).")]
    [SerializeField] float groundSnap = 0.35f;

    [Header("검증 봇 입력(키 대신)")]
    public bool simActive;
    public Vector2 simMove;
    public bool simRun;

    public OnFootCamera Cam { get; set; }
    public CharacterController Controller => cc;
    public float Speed { get; private set; }

    CharacterController cc;
    float vy;
    bool wasGrounded;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    Vector2 ReadMove()
    {
        if (simActive) return Vector2.ClampMagnitude(simMove, 1f);
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        Vector2 m = Vector2.zero;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) m.y += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) m.y -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) m.x += 1f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) m.x -= 1f;
        return Vector2.ClampMagnitude(m, 1f);
    }

    bool ReadRun() => simActive ? simRun : (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        Vector2 m = ReadMove();
        float yaw = Cam != null ? Cam.Yaw : transform.eulerAngles.y;
        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
        Vector3 dir = q * new Vector3(m.x, 0f, m.y);
        float sp = ReadRun() ? runSpeed : walkSpeed;
        Vector3 h = dir * sp;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), turnSpeedDeg * dt);

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy -= gravity * dt;
        var flags = cc.Move((h + Vector3.up * vy) * dt);
        bool grounded = (flags & CollisionFlags.Below) != 0;
        // 내리막 계단: 방금까지 땅이었는데 떴고 위로 뛰는 중이 아니면 아래로 붙인다.
        if (!grounded && wasGrounded && vy <= 0f &&
            Physics.SphereCast(transform.position + cc.center, cc.radius * 0.9f, Vector3.down, out var hit,
                cc.height * 0.5f + groundSnap, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            float drop = hit.distance - (cc.height * 0.5f - cc.radius * 0.9f);
            if (drop > 0f && drop <= groundSnap) { cc.Move(Vector3.down * (drop + 0.01f)); grounded = true; vy = -2f; }
        }
        wasGrounded = grounded;
        Speed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
    }

    /// <summary>순간 이동(카트에서 내릴 때·시작점). CharacterController 를 껐다 켜야 자리가 먹는다.</summary>
    public void Teleport(Vector3 pos, float yawDeg)
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yawDeg, 0f));
        cc.enabled = true;
        vy = 0f; wasGrounded = false;
    }
}
