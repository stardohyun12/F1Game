using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class RecreatePlayerInput
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }
        var go = car.gameObject;

        // 기존 PlayerInput의 actions asset 보존
        var oldPi = go.GetComponent<PlayerInput>();
        InputActionAsset actionAsset = null;
        if (oldPi != null)
        {
            actionAsset = oldPi.actions;
            Debug.Log($"[Recreate] Preserved actions asset: {actionAsset?.name}");
            Object.DestroyImmediate(oldPi);
            Debug.Log("[Recreate] Old PlayerInput destroyed.");
        }

        // 새 PlayerInput 부착
        var newPi = go.AddComponent<PlayerInput>();
        newPi.actions = actionAsset;
        newPi.defaultActionMap = "Player";
        newPi.notificationBehavior = PlayerNotifications.SendMessages;
        Debug.Log($"[Recreate] New PlayerInput added. inputIsActive={newPi.inputIsActive}, currentMap={newPi.currentActionMap?.name ?? "NULL"}, devices={newPi.devices.Count}");

        EditorUtility.SetDirty(go);
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Recreate] Scene saved. Need Play stop + start for full re-init.");
    }
}
