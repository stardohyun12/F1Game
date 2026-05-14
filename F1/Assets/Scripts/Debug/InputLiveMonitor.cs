using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputLiveMonitor : MonoBehaviour
{
    public float duration = 6f;
    private float t;
    private ArcadeCarController car;
    private InputAction moveAction;
    private FieldInfo moveInputField;
    private FieldInfo smoothedMoveField;
    private FieldInfo isGroundedField;
    private PlayerInput pi;
    private float logInterval = 0.4f;
    private float nextLog;

    void Start()
    {
        car = FindFirstObjectByType<ArcadeCarController>();
        pi = car?.GetComponent<PlayerInput>();
        if (pi != null) moveAction = pi.actions.FindAction("Move");
        moveInputField = typeof(ArcadeCarController).GetField("moveInput", BindingFlags.NonPublic | BindingFlags.Instance);
        smoothedMoveField = typeof(ArcadeCarController).GetField("smoothedMove", BindingFlags.NonPublic | BindingFlags.Instance);
        isGroundedField = typeof(ArcadeCarController).GetField("isGrounded", BindingFlags.NonPublic | BindingFlags.Instance);
        Debug.Log($"[Live] PI behavior={pi?.notificationBehavior}, map={pi?.currentActionMap?.name}, actions enabled={pi?.actions?.enabled}");
        Debug.Log($"[Live] Move action enabled={moveAction?.enabled}, type={moveAction?.type}");
        Debug.Log("[Live] Press W now for 6 sec.");
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= duration) { Destroy(gameObject); return; }
        if (Time.time < nextLog) return;
        nextLog = Time.time + logInterval;
        if (car == null) return;
        var mv = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        var carMv = moveInputField != null ? (Vector2)moveInputField.GetValue(car) : Vector2.zero;
        var carSm = smoothedMoveField != null ? (Vector2)smoothedMoveField.GetValue(car) : Vector2.zero;
        var grounded = isGroundedField != null ? (bool)isGroundedField.GetValue(car) : false;
        var rb = car.GetComponent<Rigidbody>();
        Debug.Log($"[Live] t={t:F1} | Action.Move={mv} car.moveInput={carMv} car.smoothed={carSm} grounded={grounded} vel={rb.linearVelocity.magnitude:F2}");
    }
}
