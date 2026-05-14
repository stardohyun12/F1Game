using UnityEditor;
using UnityEngine;

public static class DiagnoseGrounded
{
    public static void Execute()
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }

        var t = car.transform.position;
        Debug.Log($"[Ground] Car pos={t}, layer={car.gameObject.layer}({LayerMask.LayerToName(car.gameObject.layer)})");

        // 인스펙터 groundLayer 값
        var so = new SerializedObject(car);
        var p = so.FindProperty("groundLayer");
        if (p != null) Debug.Log($"[Ground] groundLayer mask value = {p.intValue} (hex 0x{p.intValue:X8})");

        // Raycast 시뮬레이션
        var origin = t + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, 1.1f))
            Debug.Log($"[Ground] Raycast hit '{hit.collider.gameObject.name}' layer={hit.collider.gameObject.layer}({LayerMask.LayerToName(hit.collider.gameObject.layer)}) at dist={hit.distance:F3}");
        else
            Debug.LogError($"[Ground] Raycast NO HIT from {origin} down 1.1m");

        // GroundField 상태
        var ground = GameObject.Find("GroundField");
        if (ground != null)
        {
            var col = ground.GetComponent<Collider>();
            Debug.Log($"[Ground] GroundField active={ground.activeInHierarchy}, layer={ground.layer}({LayerMask.LayerToName(ground.layer)}), col={col?.GetType().Name}, enabled={col?.enabled}, bounds={col?.bounds}");
        }
    }
}
