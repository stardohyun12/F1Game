using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupDeliverySystem
{
    [MenuItem("PitCrew/Setup Delivery System")]
    public static void Setup()
    {
        // 1. Clean up old delivery objects
        var oldRoot = GameObject.Find("DeliverySystem");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);
        var oldHud = GameObject.Find("DeliveryHUD");
        if (oldHud != null) Object.DestroyImmediate(oldHud);

        // 2. Build delivery root
        var root = new GameObject("DeliverySystem");
        Undo.RegisterCreatedObjectUndo(root, "Setup Delivery System");

        // 3. Create delivery points at TileMapAuto.DeliveryCells
        var pointsList = new List<DeliveryPoint>();
        foreach (var cell in TileMapAuto.DeliveryCells)
        {
            var pos = TileMapAuto.CellToWorld(cell.col, cell.row) + Vector3.up * 0.5f;
            var go = new GameObject(cell.label);
            go.transform.SetParent(root.transform);
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 3f;
            var dp = go.AddComponent<DeliveryPoint>();
            // bind happens in DeliveryManager.Start
            pointsList.Add(dp);

            // visual marker (cylinder pillar)
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Marker";
            marker.transform.SetParent(go.transform);
            marker.transform.localPosition = new Vector3(0, 1.5f, 0);
            marker.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
            var col2 = marker.GetComponent<Collider>();
            if (col2 != null) Object.DestroyImmediate(col2);
            var rend = marker.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.2f, 1f, 0.4f);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", new Color(0.4f, 1f, 0.5f) * 1.5f);
                    mat.EnableKeyword("_EMISSION");
                }
                rend.sharedMaterial = mat;
            }
        }

        // 4. Return point at TileMapAuto.StartCell
        var returnGo = new GameObject("ReturnZone");
        returnGo.transform.SetParent(root.transform);
        returnGo.transform.position = TileMapAuto.CellToWorld(TileMapAuto.StartCell.x, TileMapAuto.StartCell.y) + Vector3.up * 0.5f;
        var returnMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        returnMarker.name = "ReturnMarker";
        returnMarker.transform.SetParent(returnGo.transform);
        returnMarker.transform.localPosition = new Vector3(0, 0.1f, 0);
        returnMarker.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
        var rcol = returnMarker.GetComponent<Collider>();
        if (rcol != null) Object.DestroyImmediate(rcol);
        var rrend = returnMarker.GetComponent<Renderer>();
        if (rrend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 0.8f, 0.2f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.2f) * 1.2f);
                mat.EnableKeyword("_EMISSION");
            }
            rrend.sharedMaterial = mat;
        }

        // 5. Manager
        var mgrGo = new GameObject("DeliveryManager");
        mgrGo.transform.SetParent(root.transform);
        var mgr = mgrGo.AddComponent<DeliveryManager>();
        mgr.SetPoints(pointsList, returnGo.transform);

        // 6. HUD canvas
        var hudGo = new GameObject("DeliveryHUD");
        Undo.RegisterCreatedObjectUndo(hudGo, "Setup Delivery HUD");
        var canvas = hudGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var hud = hudGo.AddComponent<DeliveryHUD>();
        // hud finds manager+camera in Awake; explicit wiring via SerializedObject
        var so = new SerializedObject(hud);
        so.FindProperty("manager").objectReferenceValue = mgr;
        so.FindProperty("cam").objectReferenceValue = Camera.main;
        so.ApplyModifiedProperties();

        // 7. Move car to start cell, facing +X
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            var startPos = TileMapAuto.CellToWorld(TileMapAuto.StartCell.x, TileMapAuto.StartCell.y) + Vector3.up * 0.5f;
            car.transform.position = startPos;
            car.transform.rotation = Quaternion.Euler(0, TileMapAuto.StartYawDeg, 0);
            var rb = car.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"SetupDeliverySystem: {pointsList.Count} delivery points + return zone + HUD created.");
    }
}
