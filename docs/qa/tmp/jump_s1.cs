var sb = new System.Text.StringBuilder();
System.Func<float,float,string> G = (x,z) => { RaycastHit h; if (Physics.Raycast(new Vector3(x, 60f, z), Vector3.down, out h, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return h.point.y.ToString("F2") + "(" + h.collider.name + ")"; return "-"; };
sb.Append("stairs: "); for (int i = 0; i <= 8; i++) { float t = i / 8f; float x = Mathf.Lerp(196.3f, 197.0f, t), z = Mathf.Lerp(-68.6f, -76.9f, t); sb.Append(z.ToString("F1") + ":" + G(x, z) + " "); }
sb.Append("\nramp: "); foreach (var p in new[]{ new Vector2(233.5f,-48f), new Vector2(238.5f,-46.8f), new Vector2(243f,-51f), new Vector2(245.5f,-57f), new Vector2(244.5f,-64f)}) sb.Append(p + ":" + G(p.x, p.y) + " ");
sb.Append("\nflat: " + G(205f,-76.5f) + " " + G(210f,-75.6f));
var f = UnityEngine.Object.FindFirstObjectByType<FishingSpot>(); var sp = f.StandPoint();
sb.Append("\npier stand " + sp + " yaw " + f.SeatYaw + " ground " + G(sp.x, sp.z));
var fw = Quaternion.Euler(0, f.SeatYaw, 0) * Vector3.forward;
sb.Append("\npier along fwd: "); for (int i = -6; i <= 6; i++) { var q = sp + fw * i * 0.5f; sb.Append((i*0.5f) + ":" + G(q.x, q.z) + " "); }
return sb.ToString();
