using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 스폰 좌표 위/아래로 모든 collider 나열. 진짜 ground 어디 있는지 + 차 위/아래 stack 확인.
public static class DiagnoseSpawnStack
{
    public static void Execute()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OpenWorld_v2.unity");

        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        Vector3 p = car.transform.position;
        var carCols = new HashSet<Collider>(car.GetComponentsInChildren<Collider>(true));

        // 위로 raycast (차 아래 -> 위로 솟는 collider 찾기)
        Debug.Log($"[Stack] Car pos={p}");

        // 차 위/아래 ±10m 라인에서 모든 collider 찾기 (RaycastAll 양방향)
        for (float dir = -1f; dir <= 1f; dir += 2f)
        {
            Vector3 origin = p + Vector3.up * (dir > 0 ? -5f : 10f);
            Vector3 ray = Vector3.up * dir;
            var hits = Physics.RaycastAll(origin, ray, 20f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            string dirName = dir > 0 ? "UP from y=-5" : "DOWN from y=10";
            Debug.Log($"[Stack] Ray {dirName} at (48, _, 36) hits {hits.Length}:");
            foreach (var h in hits)
            {
                bool selfHit = carCols.Contains(h.collider);
                Debug.Log($"  - y={h.point.y:F3} '{h.collider.gameObject.name}' (type={h.collider.GetType().Name}, trig={h.collider.isTrigger}, self={selfHit})");
            }
        }
    }
}
