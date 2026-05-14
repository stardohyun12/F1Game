using UnityEditor;
using UnityEngine;

public static class DiagnoseCarFully
{
    public static void Execute()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");

        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("no car"); return; }

        Debug.Log($"[Car] name={car.name}, pos={car.transform.position}, rot={car.transform.eulerAngles}, scale={car.transform.lossyScale}");

        // 자식 전체 트리 (1단계)
        foreach (Transform t in car.transform)
            Debug.Log($"  └ child '{t.name}' localPos={t.localPosition}, worldPos={t.position}");

        // 모든 콜라이더
        var cols = car.GetComponentsInChildren<Collider>(true);
        Debug.Log($"[Car] Colliders={cols.Length}");
        foreach (var c in cols)
            Debug.Log($"  Col '{c.gameObject.name}' type={c.GetType().Name} enabled={c.enabled} isTrigger={c.isTrigger} bounds=[min={c.bounds.min}, max={c.bounds.max}, size={c.bounds.size}]");

        // 모든 MeshRenderer (비주얼)
        var rends = car.GetComponentsInChildren<MeshRenderer>(true);
        Debug.Log($"[Car] MeshRenderers={rends.Length}");
        foreach (var r in rends)
            Debug.Log($"  Mesh '{r.gameObject.name}' bounds=[min={r.bounds.min}, max={r.bounds.max}]");

        // Rigidbody
        var rb = car.GetComponent<Rigidbody>();
        if (rb != null)
            Debug.Log($"[Car] RB mass={rb.mass} drag={rb.linearDamping} angularDamping={rb.angularDamping} useGravity={rb.useGravity} isKinematic={rb.isKinematic} interpolation={rb.interpolation} collisionDetection={rb.collisionDetectionMode} centerOfMass(local)={rb.centerOfMass}");

        // PhysicMaterial
        if (cols.Length > 0)
        {
            var c = cols[0];
            Debug.Log($"[Car] PhysicsMaterial on '{c.name}': {(c.sharedMaterial != null ? c.sharedMaterial.name : "(none)")}");
        }
    }
}
