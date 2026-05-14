using UnityEngine;
using UnityEngine.InputSystem;

public static class FixPlayerInputActivate
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }
        var pi = car.GetComponent<PlayerInput>();
        if (pi == null) { Debug.LogError("no PlayerInput"); return; }

        pi.ActivateInput();
        Debug.Log($"[FixPI] ActivateInput called. inputIsActive={pi.inputIsActive}");

        if (pi.actions != null)
        {
            // Action map 명시 활성화
            try
            {
                pi.SwitchCurrentActionMap("Player");
                Debug.Log($"[FixPI] SwitchCurrentActionMap('Player') OK. current={pi.currentActionMap?.name}");
            }
            catch (System.Exception e) { Debug.LogError($"[FixPI] SwitchCurrentActionMap failed: {e.Message}"); }

            // 사용 가능한 action map 나열
            foreach (var map in pi.actions.actionMaps)
                Debug.Log($"[FixPI] Available map: '{map.name}' (enabled={map.enabled})");
        }

        // 키보드 + 마우스 강제 활성화
        if (Keyboard.current != null)
        {
            try
            {
                pi.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
                Debug.Log($"[FixPI] SwitchCurrentControlScheme OK. devices={pi.devices.Count}, scheme={pi.currentControlScheme}");
            }
            catch (System.Exception e) { Debug.LogError($"[FixPI] SwitchCurrentControlScheme failed: {e.Message}"); }
        }
        else Debug.LogError("[FixPI] Keyboard.current is null — Input System runtime not detecting keyboard.");
    }
}
