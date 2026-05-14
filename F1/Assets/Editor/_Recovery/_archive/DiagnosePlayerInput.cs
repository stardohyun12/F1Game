using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class DiagnosePlayerInput
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }

        var pi = car.GetComponent<PlayerInput>();
        if (pi == null) { Debug.LogError("[PI] no PlayerInput on Race"); return; }

        Debug.Log($"[PI] enabled={pi.enabled}, active={pi.gameObject.activeInHierarchy}");
        Debug.Log($"[PI] actions asset={pi.actions?.name ?? "NULL"}");
        Debug.Log($"[PI] currentActionMap={pi.currentActionMap?.name ?? "NULL"}");
        Debug.Log($"[PI] defaultActionMap='{pi.defaultActionMap}'");
        Debug.Log($"[PI] notificationBehavior={pi.notificationBehavior}");
        Debug.Log($"[PI] inputIsActive={pi.inputIsActive}, devices={pi.devices.Count}");
        foreach (var d in pi.devices) Debug.Log($"[PI] device: {d.displayName}");

        // ArcadeCarController.moveInput private — Reflection
        var moveField = typeof(ArcadeCarController).GetField("moveInput", BindingFlags.NonPublic | BindingFlags.Instance);
        if (moveField != null) Debug.Log($"[PI] car.moveInput = {moveField.GetValue(car)}");

        var smoothedField = typeof(ArcadeCarController).GetField("smoothedMove", BindingFlags.NonPublic | BindingFlags.Instance);
        if (smoothedField != null) Debug.Log($"[PI] car.smoothedMove = {smoothedField.GetValue(car)}");

        // 강제 throttle 입력 — 1초 동안 forward
        if (moveField != null)
        {
            moveField.SetValue(car, new Vector2(0, 1));
            Debug.Log("[PI] Force-set moveInput = (0, 1). Should accelerate next FixedUpdate.");
        }
    }
}
