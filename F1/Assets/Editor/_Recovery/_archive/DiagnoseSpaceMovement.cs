using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class DiagnoseSpaceMovement
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }

        var rb = car.GetComponent<Rigidbody>();
        var pi = car.GetComponent<PlayerInput>();

        Debug.Log($"[Diag] pos={car.transform.position}, vel={rb.linearVelocity}, mass={rb.mass}");

        // groundLayer mask
        var so = new SerializedObject(car);
        var gl = so.FindProperty("groundLayer");
        Debug.Log($"[Diag] groundLayer mask = {gl.intValue} (0xFFFFFFFF = ~0 default)");

        // isGrounded raycast simulation
        Vector3 origin = car.transform.position + Vector3.up * 0.5f;
        bool hit = Physics.Raycast(origin, Vector3.down, 1.1f, gl.intValue, QueryTriggerInteraction.Ignore);
        Debug.Log($"[Diag] Raycast grounded (with mask) = {hit}");
        if (Physics.Raycast(origin, Vector3.down, out var h, 1.1f, gl.intValue))
            Debug.Log($"[Diag] hit '{h.collider.name}' layer={h.collider.gameObject.layer}({LayerMask.LayerToName(h.collider.gameObject.layer)})");

        // private isGrounded field
        var gFld = typeof(ArcadeCarController).GetField("isGrounded", BindingFlags.NonPublic | BindingFlags.Instance);
        if (gFld != null) Debug.Log($"[Diag] car.isGrounded (cached) = {gFld.GetValue(car)}");

        // moveInput / smoothedMove
        var mFld = typeof(ArcadeCarController).GetField("moveInput", BindingFlags.NonPublic | BindingFlags.Instance);
        var sFld = typeof(ArcadeCarController).GetField("smoothedMove", BindingFlags.NonPublic | BindingFlags.Instance);
        Debug.Log($"[Diag] moveInput={mFld?.GetValue(car)}, smoothedMove={sFld?.GetValue(car)}");

        // PlayerInput state
        if (pi != null)
        {
            Debug.Log($"[Diag] PI active={pi.inputIsActive}, map={pi.currentActionMap?.name}, devices={pi.devices.Count}");
            var moveAction = pi.actions.FindAction("Move");
            var hbAction = pi.actions.FindAction("Handbrake");
            Debug.Log($"[Diag] Move action: enabled={moveAction?.enabled}, value={moveAction?.ReadValue<Vector2>()}");
            Debug.Log($"[Diag] Handbrake action: enabled={hbAction?.enabled}, pressed={hbAction?.IsPressed()}");
        }
    }
}
