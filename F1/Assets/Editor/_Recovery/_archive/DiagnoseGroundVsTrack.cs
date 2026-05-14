using UnityEditor;
using UnityEngine;

public static class DiagnoseGroundVsTrack
{
    public static void Execute()
    {
        var ground = GameObject.Find("GroundField");
        var gBox = ground != null ? ground.GetComponent<BoxCollider>() : null;
        Debug.Log($"[Diag] GroundField pos={ground.transform.position}, boxCenter={gBox.center}, boxSize={gBox.size}");
        Debug.Log($"[Diag] GroundField BoxCollider TOP y = {gBox.bounds.max.y:F3}");

        // Renderer 실제 mesh 표면 (시각). raycast로 잡기 위해 임시 MeshCollider 부착? 대신 MeshFilter bounds + transform 추정.
        var mf = ground.GetComponentInChildren<MeshFilter>();
        if (mf != null)
        {
            var b = mf.sharedMesh.bounds;
            Debug.Log($"[Diag] GroundField mesh local bounds: min={b.min}, max={b.max}");
            var rend = ground.GetComponentInChildren<MeshRenderer>();
            if (rend != null) Debug.Log($"[Diag] GroundField renderer world bounds: min={rend.bounds.min}, max={rend.bounds.max}");
        }

        // 차 스폰 (48, _, 36) 위에서 raycast 5m 아래로
        var origins = new Vector3[]
        {
            new Vector3(48f, 5f, 36f),  // 스폰
            new Vector3(60f, 5f, 36f),  // 도로 옆
            new Vector3(48f, 5f, 50f),  // 다른 도로
            new Vector3(80f, 5f, 80f),  // 잔디 (도로 밖)
            new Vector3(150f, 5f, 150f) // 멀리 잔디
        };
        foreach (var o in origins)
        {
            if (Physics.Raycast(o, Vector3.down, out var hit, 20f))
                Debug.Log($"[Diag] Ray @({o.x},{o.z}) hit '{hit.collider.gameObject.name}' y={hit.point.y:F3} (dist={hit.distance:F3})");
            else
                Debug.Log($"[Diag] Ray @({o.x},{o.z}) NO HIT");
        }

        // 첫 도로 타일 mesh 실제 top (renderer bounds)
        var tile = GameObject.Find("Track/Tile_Straight_2_0_r0");
        if (tile != null)
        {
            var r = tile.GetComponent<MeshRenderer>();
            var mc = tile.GetComponent<MeshCollider>();
            Debug.Log($"[Diag] Tile_Straight_2 renderer top y={r.bounds.max.y:F3}, MeshCollider enabled={mc?.enabled}");
        }

        // Track 자식 중 MeshCollider 켜진 것 카운트
        var track = GameObject.Find("Track");
        int colOn = 0, colOff = 0;
        var cols = track.GetComponentsInChildren<MeshCollider>(true);
        foreach (var c in cols) { if (c.enabled) colOn++; else colOff++; }
        Debug.Log($"[Diag] Track tile MeshColliders: enabled={colOn}, disabled={colOff}");
    }
}
