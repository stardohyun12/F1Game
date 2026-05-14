using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DiagnoseRoadOverlap
{
    [MenuItem("PitCrew/Fix/Diagnose Road Overlap")]
    public static void Execute()
    {
        var rs = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        // 도로 근처 y 범위 (0.10~0.30) 의 모든 renderer
        var nearRoad = rs.Where(r => r.bounds.max.y >= 0.10f && r.bounds.max.y <= 0.30f)
                         .ToList();
        // top.y 빈도
        var topYBuckets = new Dictionary<float, int>();
        foreach (var r in nearRoad)
        {
            float t = Mathf.Round(r.bounds.max.y * 1000f) / 1000f;
            topYBuckets[t] = topYBuckets.GetValueOrDefault(t) + 1;
        }
        Debug.Log($"[RD] near-road renderers: {nearRoad.Count}");
        foreach (var kv in topYBuckets.OrderBy(k => k.Key))
            Debug.Log($"[RD]   top.y={kv.Key:F3} → {kv.Value} renderers");

        // base 근처도 (0.13~0.18) — BaseHorizon이랑 도로 측면 영역
        var baseRange = rs.Where(r => r.bounds.max.y >= 0.13f && r.bounds.max.y <= 0.18f).ToList();
        Debug.Log($"[RD] base-range (0.13~0.18): {baseRange.Count} renderers");
        foreach (var r in baseRange.Take(10))
            Debug.Log($"[RD]   '{r.gameObject.name}' bounds=[min={r.bounds.min}, max={r.bounds.max}]");

        // 첫 도로 tile 자세히
        var track = GameObject.Find("Track");
        if (track != null && track.transform.childCount > 0)
        {
            var t = track.transform.GetChild(0);
            var r = t.GetComponent<MeshRenderer>();
            Debug.Log($"[RD] first tile '{t.name}': pos.y={t.position.y:F3}, render bounds=[min={r.bounds.min}, max={r.bounds.max}], scale={t.localScale}");
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var b = mf.sharedMesh.bounds;
                Debug.Log($"[RD]   mesh local bounds: min={b.min}, max={b.max}, size={b.size}");
                Debug.Log($"[RD]   submesh count: {mf.sharedMesh.subMeshCount}");
            }
            // 자식 mesh (compound prefab일 경우)
            int childMeshes = 0;
            foreach (var c in t.GetComponentsInChildren<MeshRenderer>(true)) childMeshes++;
            Debug.Log($"[RD]   total MeshRenderers under tile: {childMeshes}");
        }

        // BaseHorizon detail
        var bh = GameObject.Find("BaseHorizon");
        if (bh != null)
        {
            var r = bh.GetComponent<MeshRenderer>();
            Debug.Log($"[RD] BaseHorizon: pos={bh.transform.position}, bounds=[min={r.bounds.min}, max={r.bounds.max}]");
        }
    }
}
