var sb = new System.Text.StringBuilder();
var t = UnityEngine.AI.NavMesh.CalculateTriangulation();
sb.AppendLine("navmesh verts=" + (t.vertices == null ? 0 : t.vertices.Length));
var from = new UnityEngine.Vector3(184.8f, 25.3f, -66.2f);
var to = new UnityEngine.Vector3(271.9f, 30.5f, -33.0f);
UnityEngine.AI.NavMeshHit h1, h2;
bool a = UnityEngine.AI.NavMesh.SamplePosition(from, out h1, 4f, UnityEngine.AI.NavMesh.AllAreas);
bool b = UnityEngine.AI.NavMesh.SamplePosition(to, out h2, 4f, UnityEngine.AI.NavMesh.AllAreas);
sb.AppendLine("sample from=" + a + " " + h1.position.ToString("F1") + " to=" + b + " " + h2.position.ToString("F1"));
if (a && b) {
  var p = new UnityEngine.AI.NavMeshPath();
  bool ok = UnityEngine.AI.NavMesh.CalculatePath(h1.position, h2.position, UnityEngine.AI.NavMesh.AllAreas, p);
  sb.AppendLine("path ok=" + ok + " status=" + p.status + " corners=" + p.corners.Length);
  float len = 0; for (int i = 1; i < p.corners.Length; i++) len += UnityEngine.Vector3.Distance(p.corners[i-1], p.corners[i]);
  sb.AppendLine("len=" + len.ToString("F0"));
  foreach (var c in p.corners) sb.AppendLine("  " + c.ToString("F1"));
}
return sb.ToString();
