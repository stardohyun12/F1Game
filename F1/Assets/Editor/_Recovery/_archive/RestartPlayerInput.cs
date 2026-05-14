using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public static class RestartPlayerInput
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }
        var pi = car.GetComponent<PlayerInput>();
        if (pi == null) { Debug.LogError("no PlayerInput"); return; }

        // PlayerInput을 비활성→활성 토글 → OnEnable 재호출 → device auto-assign + default map 활성화
        pi.enabled = false;
        pi.enabled = true;
        Debug.Log($"[RestartPI] After toggle: inputIsActive={pi.inputIsActive}, devices={pi.devices.Count}, currentMap={pi.currentActionMap?.name ?? "NULL"}");

        // 그래도 안 되면 직접 action asset의 Player map enable
        if (pi.actions != null)
        {
            var playerMap = pi.actions.FindActionMap("Player");
            if (playerMap != null)
            {
                playerMap.Enable();
                Debug.Log($"[RestartPI] Player map manually enabled. enabled={playerMap.enabled}");
            }
        }

        // 디바이스 강제 페어
        if (Keyboard.current != null)
        {
            InputUser.PerformPairingWithDevice(Keyboard.current, pi.user);
            Debug.Log($"[RestartPI] Keyboard paired. user.valid={pi.user.valid}, devices={pi.user.pairedDevices.Count}");
        }
    }
}
