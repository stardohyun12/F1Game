using UnityEditor;
using UnityEngine;

// 도로 아스팔트(검은 부분, y=0.17) ↔ BaseHorizon(y=0.16) 1cm 차이로 z-fight.
// BaseHorizon 5cm 더 내려 y=0.11. GroundField BoxCollider도 함께 내려 차 off-road float 방지.
// 차 box.bottom은 asphalt top(0.17)에 맞춰 재배치 (이전엔 sidewalk top 0.22에 있었음).
public static class FixRoadAsphaltZFight
{
    [MenuItem("PitCrew/Fix/Road Asphalt Z-Fight")]
    public static void Execute()
    {
        // 1. BaseHorizon — world y 0.16 → 0.11 (5cm 추가 ↓)
        var bh = GameObject.Find("BaseHorizon");
        if (bh != null)
        {
            var lp = bh.transform.localPosition;
            bh.transform.localPosition = new Vector3(lp.x, lp.y - 0.05f, lp.z);
            Debug.Log($"[ZF] BaseHorizon local.y {lp.y:F3} → {bh.transform.localPosition.y:F3} (world {bh.transform.position.y:F3})");
        }

        // 2. GroundField BoxCollider top — y 0.17 → 0.12 (BaseHorizon top 0.11 ↑ 1cm)
        //    center.y -0.50, size.y 1 → top = pos.y + center.y + size.y/2 = 0.17 - 0.5 + 0.5 = 0.17
        //    새 top 0.12: pos.y(0.17) + center.y + 0.5 = 0.12 → center.y = -0.55
        var gf = GameObject.Find("GroundField");
        var bc = gf.GetComponent<BoxCollider>();
        var c = bc.center;
        bc.center = new Vector3(c.x, c.y - 0.05f, c.z);
        Debug.Log($"[ZF] GroundField BoxCollider center.y {c.y:F3} → {bc.center.y:F3} (top now {bc.bounds.max.y:F3})");

        // 3. 차 — box.bottom = asphalt top(0.17). 현재 car y = 0.22로 sidewalk 위. y -= 0.05.
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car != null)
        {
            var p = car.transform.position;
            car.transform.position = new Vector3(p.x, p.y - 0.05f, p.z);
            var rb = car.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            Debug.Log($"[ZF] car y {p.y:F3} → {car.transform.position.y:F3} (box.bottom on asphalt)");
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[ZF] saved.");
    }
}
