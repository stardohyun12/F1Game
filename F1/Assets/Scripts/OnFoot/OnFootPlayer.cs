using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 기능 1(과제 20261030-2243): 걷는 주인공. 모델 없음 — 1.7 m 캡슐 대역(D-149 사람 모델은 톤 합격 뒤).
/// CharacterController: stepOffset 0.3 → 계단 단 0.165·연석 0.12~0.156 은 오르고, 0.5 m 넘는 축대·담·울은 막힌다.
/// 이동 = 카메라 기준 WASD, Shift 달리기, Space 점프(D-248 ②). 검증 봇은 sim* 로 같은 통로에 값을 넣는다(키 대신).
/// 점프: 오르는 속도 jumpSpeed + 점프 중 전용 중력 jumpGravity → 높이 ≈0.45 m · 체공 ≈0.55 s (고1 실물). 땅을 떠난 뒤 coyoteTime 안에도 뛸 수 있고
/// 착지 jumpBuffer 전에 눌러도 착지 순간 뛴다. 공중에선 방향을 약하게만 바꾼다(airAccel). 대화(enabled=false)·앉기·카트(오브젝트 꺼짐)에선 Update 가 안 돌아 점프가 없다.
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

    [Header("점프 (D-248 ②)")]
    [Tooltip("뛰는 순간 위로 속도(m/s). 높이 = v²/(2·jumpGravity).")]
    [SerializeField] float jumpSpeed = 3.3f;
    [Tooltip("점프 중 중력(m/s²). 낙하·계단 중력(gravity)보다 약해 체공이 0.55 s 가 된다. 점프로 뜬 동안만 쓴다.")]
    [SerializeField] float jumpGravity = 12f;
    [Tooltip("땅을 막 떠난 뒤에도 이 시간(초) 안이면 뛸 수 있다.")]
    [SerializeField] float coyoteTime = 0.1f;
    [Tooltip("착지 이 시간(초) 전에 눌러도 닿는 순간 뛴다.")]
    [SerializeField] float jumpBuffer = 0.1f;
    [Tooltip("공중에서 가고 싶은 방향으로 속도가 바뀌는 최대 가속(m/s²). 땅에선 바로.")]
    [SerializeField] float airAccel = 4f;
    [Tooltip("이 시간(초)보다 짧게 떠 있다 닿은 건 착지로 안 친다(계단 한 단·연석).")]
    [SerializeField] float minLandAir = 0.12f;

    [Header("검증 봇 입력(키 대신)")]
    public bool simActive;
    public Vector2 simMove;
    public bool simRun;
    bool simJumpReq;
    /// <summary>봇: 이번 프레임에 Space 를 누른 것과 같다.</summary>
    public void SimJump() { simJumpReq = true; }

    public OnFootCamera Cam { get; set; }
    public CharacterController Controller => cc;
    public float Speed { get; private set; }
    /// <summary>발이 땅에서 떨어져 있다(점프·낙하 공통).</summary>
    public bool Airborne => !wasGrounded;
    /// <summary>점프로 뜬 중(착지하면 꺼진다).</summary>
    public bool Jumping => jumped;
    /// <summary>점프를 시작한 횟수 — 봇·QA 용.</summary>
    public int JumpCount { get; private set; }
    /// <summary>마지막 점프의 체공 시간(초, 뜬 프레임~닿은 프레임) — 봇·QA 용.</summary>
    public float LastAirTime { get; private set; }
    /// <summary>착지(minLandAir 이상 떠 있다 닿음). 인자 = 닿는 순간 아래로 속도(m/s, 양수). 카메라 눌림·착지음이 구독한다.</summary>
    public event System.Action<float> Landed;

    CharacterController cc;
    float vy;
    bool wasGrounded;
    bool jumped;
    float sinceGround, jumpBuf, airTime, jumpT0;
    Vector3 hv;

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

    bool ReadJump()
    {
        if (simJumpReq) { simJumpReq = false; return true; }
        if (simActive || OnFootCamera.UiCapture) return false;   // 디버그 창이 입력을 쓰는 동안은 안 뛴다
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    void OnEnable() { jumpBuf = 0f; simJumpReq = false; }

    bool ReadRun() => simActive ? simRun : (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        dt = Mathf.Min(dt, 0.05f);   // 렉(0.27 s 이상)이 뛰는 순간에 걸려도 위로 속도가 한 프레임에 다 깎여 점프가 사라지지 않게
        Vector2 m = ReadMove();
        float yaw = Cam != null ? Cam.Yaw : transform.eulerAngles.y;
        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
        Vector3 dir = q * new Vector3(m.x, 0f, m.y);
        float sp = ReadRun() ? runSpeed : walkSpeed;
        Vector3 h = dir * sp;
        // 땅에선 바로 따라가고, 공중에선 airAccel 만큼만 바꾼다(뛰는 순간의 속도가 이어진다).
        hv = wasGrounded ? h : Vector3.MoveTowards(hv, h, airAccel * dt);
        // 1인칭(D-219): 몸은 늘 보는 쪽을 향한다(옆걸음·뒷걸음이 되게). 카메라가 없으면 예전처럼 가는 쪽으로 돈다.
        if (Cam != null && Cam.isActiveAndEnabled) transform.rotation = q;
        else if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), turnSpeedDeg * dt);

        // 점프: 코요테(떠난 직후)·미리 누름(착지 직전) 허용, 점프로 뜬 동안엔 다시 못 뛴다.
        sinceGround = wasGrounded ? 0f : sinceGround + dt;
        jumpBuf = ReadJump() ? jumpBuffer : jumpBuf - dt;
        if (jumpBuf > 0f && !jumped && sinceGround <= coyoteTime)
        {
            vy = jumpSpeed; jumped = true; jumpBuf = 0f; JumpCount++; jumpT0 = Time.time;
            sinceGround = coyoteTime + 1f;
        }

        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy -= (jumped ? jumpGravity : gravity) * dt;
        float vyHit = vy;
        var flags = cc.Move((hv + Vector3.up * vy) * dt);
        bool grounded = (flags & CollisionFlags.Below) != 0;
        if ((flags & CollisionFlags.Above) != 0 && vy > 0f) vy = 0f;   // 머리 부딪힘 — 바로 떨어진다
        // 내리막 계단: 방금까지 땅이었는데 떴고 위로 뛰는 중이 아니면 아래로 붙인다.
        if (!grounded && wasGrounded && vy <= 0f && !jumped &&
            Physics.SphereCast(transform.position + cc.center, cc.radius * 0.9f, Vector3.down, out var hit,
                cc.height * 0.5f + groundSnap, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            float drop = hit.distance - (cc.height * 0.5f - cc.radius * 0.9f);
            if (drop > 0f && drop <= groundSnap) { cc.Move(Vector3.down * (drop + 0.01f)); grounded = true; vy = -2f; }
        }
        if (grounded && vy <= 0f)
        {
            if (!wasGrounded && airTime >= minLandAir) Landed?.Invoke(Mathf.Max(0f, -vyHit));
            if (jumped) LastAirTime = Time.time - jumpT0;
            jumped = false; airTime = 0f;
        }
        else if (!grounded) airTime += dt;
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
        vy = 0f; wasGrounded = false; jumped = false; airTime = 0f; sinceGround = 0f; jumpBuf = 0f; hv = Vector3.zero;
    }
}
