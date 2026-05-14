using UnityEditor;
using UnityEngine;

public static class DiagnoseGroundColliders
{
    public static void Execute()
    {
        // 1) Track 타일들의 콜라이더 확인
        var trackRoot = GameObject.Find("Track");
        if (trackRoot != null)
        {
            int total = 0, withCollider = 0;
            foreach (Transform child in trackRoot.transform)
            {
                total++;
                if (child.GetComponentInChildren<Collider>() != null) withCollider++;
            }
            Debug.Log($"[Ground] Track tiles: {total}, with Collider: {withCollider}");
        }
        else Debug.LogError("[Ground] Track root not found.");

        // 2) GroundField 확인
        var ground = GameObject.Find("GroundField");
        if (ground != null)
        {
            var col = ground.GetComponentInChildren<Collider>();
            Debug.Log($"[Ground] GroundField: collider={col?.GetType().Name ?? "NULL"}, " +
                $"pos={ground.transform.position}, scale={ground.transform.localScale}");
        }
        else Debug.LogWarning("[Ground] GroundField not found.");

        // 3) Buildings 콜라이더
        var buildings = GameObject.Find("Buildings");
        if (buildings != null)
        {
            int withCollider = 0, total = 0;
            foreach (Transform child in buildings.transform)
            {
                total++;
                if (child.GetComponentInChildren<Collider>() != null) withCollider++;
            }
            Debug.Log($"[Ground] Buildings: {total}, with Collider: {withCollider}");
        }

        // 4) Car 시작 위치 아래에 raycast
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            var origin = car.transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 100f))
                Debug.Log($"[Ground] Raycast from car: hit '{hit.collider.gameObject.name}' at distance {hit.distance:F2}");
            else
                Debug.LogError($"[Ground] Raycast from car ({origin}) DOWN 100m: NO HIT — 차 아래 아무 콜라이더 없음. 자유낙하 원인.");
        }
    }
}
