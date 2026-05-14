using UnityEditor;
using UnityEngine;

public static class CheckGroundPos
{
    public static void Execute()
    {
        var ground = GameObject.Find("GroundField");
        if (ground == null) { Debug.LogError("[Check] no GroundField"); return; }
        var t = ground.transform;
        Debug.Log($"[Check] parent={(t.parent != null ? t.parent.name : "null")}, local={t.localPosition}, world={t.position}");
        var box = ground.GetComponent<BoxCollider>();
        Debug.Log($"[Check] box bounds={box.bounds}, max.y={box.bounds.max.y:F4}");
    }
}
