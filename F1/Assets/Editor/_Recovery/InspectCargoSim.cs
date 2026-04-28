using UnityEditor;
using UnityEngine;

public static class InspectCargoSim
{
    public static void Execute()
    {
        var root = GameObject.Find("CargoSimRoot");
        if (root == null) { Debug.LogWarning("[Inspect] CargoSimRoot 없음"); return; }
        var sim = root.GetComponent<CargoShakeSimulator>();
        if (sim == null) { Debug.LogWarning("[Inspect] CargoShakeSimulator 없음"); return; }

        var so = new SerializedObject(sim);
        string[] keys = {
            "enableRotation", "matchCarRotation",
            "degPerLongG", "degPerLatG", "maxRotDeg", "rotLerp",
            "staticFrictionG", "kineticFriction", "accelScale", "maxOffset", "wallBounce", "velocityDeadZone",
            "cargo", "boxTransform", "simCamRig"
        };
        foreach (var k in keys)
        {
            var p = so.FindProperty(k);
            if (p == null) { Debug.Log($"[Inspect] {k} = <missing>"); continue; }
            string val = p.propertyType switch
            {
                SerializedPropertyType.Boolean => p.boolValue.ToString(),
                SerializedPropertyType.Float => p.floatValue.ToString("F3"),
                SerializedPropertyType.Integer => p.intValue.ToString(),
                SerializedPropertyType.ObjectReference => p.objectReferenceValue == null ? "null" : p.objectReferenceValue.name,
                _ => p.propertyType.ToString()
            };
            Debug.Log($"[Inspect] {k} = {val}");
        }

        var box = root.transform.Find("Box");
        if (box != null)
        {
            Debug.Log($"[Inspect] Box localPos={box.localPosition} localRot={box.localEulerAngles}");
        }
        Debug.Log($"[Inspect] Root rot={root.transform.eulerAngles}");
    }
}
