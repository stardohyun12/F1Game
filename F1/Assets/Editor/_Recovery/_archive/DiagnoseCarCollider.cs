using UnityEngine;

public static class DiagnoseCarCollider
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }
        var rb = car.GetComponent<Rigidbody>();
        var box = car.GetComponent<BoxCollider>();

        Debug.Log($"[Col] BoxCollider center={box.center}, size={box.size}, isTrigger={box.isTrigger}");
        Debug.Log($"[Col] bounds={box.bounds} (min.y={box.bounds.min.y}, max.y={box.bounds.max.y})");
        Debug.Log($"[Col] PhysicMaterial={box.sharedMaterial?.name ?? "NULL"}");
        Debug.Log($"[Col] Car pos={car.transform.position}, rotation.eulerAngles={car.transform.eulerAngles}");

        // Lift the car 1m up to escape any penetration
        car.transform.position += Vector3.up * 1.0f;
        Debug.Log($"[Col] Lifted car +1m → new pos={car.transform.position}");

        // 직접 velocity 설정 (queued force가 아니라 즉시 반영)
        rb.linearVelocity = car.transform.forward * 10f;
        Debug.Log($"[Col] Set linearVelocity = {rb.linearVelocity}, magnitude={rb.linearVelocity.magnitude}");

        // 모든 자식 콜라이더 (자기 colliding) 검사
        var childCols = car.GetComponentsInChildren<Collider>();
        foreach (var c in childCols)
            Debug.Log($"[Col] Child collider: {c.gameObject.name} type={c.GetType().Name} isTrigger={c.isTrigger} bounds={c.bounds}");
    }
}
