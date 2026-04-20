using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ArcadeCarController : MonoBehaviour
{
    [Header("Performance")]
    [SerializeField] private float maxSpeedKmh = 60f;
    [SerializeField] private float accelTime = 2.5f;
    [SerializeField] private float brakeDecel = 12f;
    [SerializeField] private float reverseMaxKmh = 20f;
    [SerializeField] private float coastDrag = 0.8f;

    [Header("Steering")]
    [SerializeField] private float maxTurnDegPerSec = 110f;
    [SerializeField] private AnimationCurve turnResponseBySpeed =
        AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
    [SerializeField] private float minSpeedForSteer = 0.5f;

    [Header("Grip")]
    [SerializeField] private float sideGripDamping = 6f;
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.3f, 0f);

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer = ~0;

    private Rigidbody rb;
    private Vector2 moveInput;
    private float maxSpeedMps;
    private float reverseMaxMps;
    private float accelForce;
    private bool isGrounded;

    public float CurrentSpeedMps => rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;
    public float MaxSpeedMps => maxSpeedMps;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
        RecalculateForces();
    }

    void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        RecalculateForces();
    }

    void RecalculateForces()
    {
        maxSpeedMps = maxSpeedKmh / 3.6f;
        reverseMaxMps = reverseMaxKmh / 3.6f;
        if (rb != null)
            accelForce = rb.mass * maxSpeedMps / Mathf.Max(0.1f, accelTime);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        isGrounded = true;

        Vector3 v = rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(v, transform.forward);
        float sideSpeed = Vector3.Dot(v, transform.right);

        ApplyThrottle(forwardSpeed);
        ApplySteering(forwardSpeed);
        ApplySideGrip(sideSpeed);
    }

    void ApplyThrottle(float forwardSpeed)
    {
        if (!isGrounded) return;
        float t = moveInput.y;

        if (t > 0.01f)
        {
            if (forwardSpeed < maxSpeedMps)
                rb.AddForce(transform.forward * (accelForce * t), ForceMode.Force);
        }
        else if (t < -0.01f)
        {
            if (forwardSpeed > 0.2f)
                rb.AddForce(-transform.forward * (brakeDecel * rb.mass * -t), ForceMode.Force);
            else if (forwardSpeed > -reverseMaxMps)
                rb.AddForce(transform.forward * (accelForce * t * 0.5f), ForceMode.Force);
        }
        else
        {
            Vector3 horiz = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            rb.AddForce(-horiz * coastDrag, ForceMode.Acceleration);
        }
    }

    void ApplySteering(float forwardSpeed)
    {
        float speedMag = Mathf.Abs(forwardSpeed);
        if (speedMag < minSpeedForSteer) return;

        float speedNorm = Mathf.Clamp01(speedMag / maxSpeedMps);
        float response = turnResponseBySpeed.Evaluate(speedNorm);
        float yaw = maxTurnDegPerSec * moveInput.x * response * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
    }

    void ApplySideGrip(float sideSpeed)
    {
        if (!isGrounded) return;
        rb.AddForce(-transform.right * (sideSpeed * sideGripDamping), ForceMode.Acceleration);
    }
}
