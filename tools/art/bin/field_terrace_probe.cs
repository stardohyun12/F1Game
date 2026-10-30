// read-only: terrain height grid around (198,-57)
var terrain=new System.Collections.Generic.List<Collider>();
foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.enabled && c.gameObject.activeInHierarchy && (c is TerrainCollider || c.name.Contains("Terrain")))terrain.Add(c);
float G(float x,float z){float best=float.NaN;var ray=new Ray(new Vector3(x,500,z),Vector3.down);foreach(var c in terrain){RaycastHit h;if(c.Raycast(ray,out h,1000)&&(float.IsNaN(best)||h.point.y>best))best=h.point.y;}return best;}
var sb=new System.Text.StringBuilder();sb.AppendLine("colliders="+terrain.Count);
for(float z=-65;z<=-49;z+=2){for(float x=190;x<=206;x+=2)sb.Append(G(x,z).ToString("F2")+" ");sb.AppendLine();}
float gx=(G(199.5f,-57)-G(196.5f,-57))/3f,gz=(G(198,-55.5f)-G(198,-58.5f))/3f;sb.AppendLine("grad x="+gx+" z="+gz);
return sb.ToString();
