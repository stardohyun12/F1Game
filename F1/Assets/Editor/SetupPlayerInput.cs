using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

public class SetupPlayerInput
{
    public static void Execute()
    {
        var go = GameObject.Find("Race");
        if (go == null) { Debug.LogError("Race not found"); return; }

        var pi = go.GetComponent<PlayerInput>();
        if (pi == null) { Debug.LogError("PlayerInput not found"); return; }

        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (asset == null) { Debug.LogError("InputActionAsset not found"); return; }

        pi.actions = asset;
        pi.defaultActionMap = "Player";
        pi.notificationBehavior = PlayerNotifications.SendMessages;

        EditorUtility.SetDirty(go);
        Debug.Log("PlayerInput configured: actions=InputSystem_Actions, map=Player, behavior=SendMessages");
    }
}
