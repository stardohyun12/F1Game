var o = new System.Text.StringBuilder();
o.Append("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " | fixedDt=" + Time.fixedDeltaTime + " | ");
foreach (var fc in UnityEngine.Object.FindObjectsByType<FollowCamera>(FindObjectsSortMode.None))
{
    var so = new UnityEditor.SerializedObject(fc); var it = so.GetIterator(); it.NextVisible(true);
    o.Append("FC " + fc.name + ": ");
    while (it.NextVisible(false))
    {
        string v = it.propertyType == UnityEditor.SerializedPropertyType.Float ? it.floatValue.ToString("0.###") : it.propertyType == UnityEditor.SerializedPropertyType.Vector3 ? it.vector3Value.ToString("F2") : it.propertyType == UnityEditor.SerializedPropertyType.ObjectReference ? (it.objectReferenceValue ? it.objectReferenceValue.name : "null") : it.propertyType == UnityEditor.SerializedPropertyType.Enum ? it.enumNames[it.enumValueIndex] : it.propertyType.ToString();
        o.Append(it.name + "=" + v + " ");
    }
    var cam = fc.GetComponent<Camera>(); if (cam) o.Append("fov=" + cam.fieldOfView + " near=" + cam.nearClipPlane + " ");
    if (fc.Preset != null)
    {
        var ps = new UnityEditor.SerializedObject(fc.Preset); var pi = ps.GetIterator(); pi.NextVisible(true);
        o.Append(" | PRESET " + UnityEditor.AssetDatabase.GetAssetPath(fc.Preset) + ": ");
        while (pi.NextVisible(false)) o.Append(pi.name + "=" + (pi.propertyType == UnityEditor.SerializedPropertyType.Float ? pi.floatValue.ToString("0.###") : pi.propertyType == UnityEditor.SerializedPropertyType.Integer ? pi.intValue.ToString() : pi.propertyType.ToString()) + " ");
    }
    o.Append(" || ");
}
foreach (var c in UnityEngine.Object.FindObjectsByType<ArcadeCarController>(FindObjectsSortMode.None))
{
    var rb = c.GetComponent<Rigidbody>(); o.Append("CAR " + c.name + " interp=" + (rb ? rb.interpolation.ToString() : "-") + " mass=" + (rb ? rb.mass : 0) + " | ");
}
foreach (var z in UnityEngine.Object.FindObjectsByType<CameraFramingZone>(FindObjectsSortMode.None)) o.Append("ZONE " + z.name + " ");
var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; var p = rp != null ? rp.GetType().GetProperty("renderScale") : null;
o.Append(" renderScale=" + (p != null ? p.GetValue(rp) : "?") + " vsync=" + QualitySettings.vSyncCount + " targetFps=" + Application.targetFrameRate);
foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)) { var n = mb.GetType().Name; if (n.Contains("Pixel") || n.Contains("Snap") || n.Contains("Shake") || n.Contains("Cam")) o.Append(" MB:" + n + "@" + mb.name); }
return o.ToString();
