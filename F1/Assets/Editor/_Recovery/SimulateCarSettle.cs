using UnityEditor;
using UnityEngine;

// 에디트 모드에서 Physics.Simulate로 차를 떨어뜨려 안착 위치 확인.
// + serialized centerOfMassOffset 검증.
public static class SimulateCarSettle
{
    public static void Execute()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");

        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        var rb = car.GetComponent<Rigidbody>();
        var col = car.GetComponent<Collider>();

        // serialized 값 확인
        var so = new SerializedObject(car);
        var pCom = so.FindProperty("centerOfMassOffset");
        Debug.Log($"[Sim] serialized centerOfMassOffset = {pCom.vector3Value}");
        var pAng = so.FindProperty("angularDamping");
        Debug.Log($"[Sim] serialized angularDamping = {pAng.floatValue}");

        Debug.Log($"[Sim] BEFORE: pos={car.transform.position}, boxBottom.y={col.bounds.min.y:F3}");

        // 수동 simulation
        Physics.simulationMode = SimulationMode.Script;
        rb.centerOfMass = pCom.vector3Value;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        for (int i = 0; i < 120; i++) // 2초 simulate
            Physics.Simulate(1f / 60f);

        Debug.Log($"[Sim] AFTER 2s: pos={car.transform.position}, boxBottom.y={col.bounds.min.y:F3}, rot.euler={car.transform.eulerAngles}");
        Debug.Log($"[Sim] velocity={rb.linearVelocity}, angVel={rb.angularVelocity}");
        Physics.simulationMode = SimulationMode.FixedUpdate;
    }
}
