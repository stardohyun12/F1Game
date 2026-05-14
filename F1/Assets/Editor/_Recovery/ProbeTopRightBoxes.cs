using UnityEngine;
using UnityEditor;
using System.Text;

namespace PitCrew.RecoveryTools
{
    public static class ProbeTopRightBoxes
    {
        [MenuItem("PitCrew/Probe/Find Cardboard Boxes Top-Right")]
        public static void Run()
        {
            var mc = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (mc == null) { Debug.LogError("[Probe] no Main Camera"); return; }
            var all = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var sb = new StringBuilder();
            int hits = 0;
            foreach (var r in all)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var p = r.transform.position;
                if (p.y < -50f) continue;
                var vp = mc.WorldToViewportPoint(p);
                if (vp.z > 1f && vp.x > 0.65f && vp.y > 0.55f && vp.x < 1.1f && vp.y < 1.1f)
                {
                    sb.Append($"{GetPath(r.transform)}@{p.x:F1},{p.y:F1},{p.z:F1}\n");
                    hits++;
                    if (hits > 50) break;
                }
            }
            Debug.Log($"[Probe] hits={hits}\n{sb}");
        }

        static string GetPath(Transform t)
        {
            var s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }
    }
}
