using UnityEngine;

public static class DiagnoseCarMesh
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }
        Debug.Log($"[Mesh] Car pos={car.transform.position}, rotation={car.transform.eulerAngles}");

        var box = car.GetComponent<BoxCollider>();
        Debug.Log($"[Mesh] BoxCollider local center={box.center}, size={box.size}");
        Debug.Log($"[Mesh] BoxCollider world bounds={box.bounds} (min.y={box.bounds.min.y:F3}, max.y={box.bounds.max.y:F3})");

        var renderers = car.GetComponentsInChildren<Renderer>();
        Debug.Log($"[Mesh] Found {renderers.Length} renderers.");
        foreach (var r in renderers)
        {
            Debug.Log($"[Mesh] Renderer '{r.gameObject.name}' bounds={r.bounds} (min.y={r.bounds.min.y:F3}, max.y={r.bounds.max.y:F3})");
        }

        // Track 표면 높이 확인
        if (Physics.Raycast(car.transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 2f))
        {
            Debug.Log($"[Mesh] Track surface at y={hit.point.y:F3} (under car). Box collider bottom={box.bounds.min.y:F3}. Gap={box.bounds.min.y - hit.point.y:F3}");
        }
    }
}
