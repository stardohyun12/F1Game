// 부두 턱 실제 주행: x 여러 곳에서 z=108 북향 풀스로틀 5초, 도달 최대 z. SessionState "qabound".
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=c.GetComponent<Rigidbody>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf);
float[] xs={20f,60f,83f,110f,150f}; int cur=-1; float t0=0, maxZ=0; var res=new System.Text.StringBuilder(); UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(cur>=0) maxZ=Mathf.Max(maxZ,rb.position.z);
 if(cur<0||Time.time-t0>5f){ if(cur>=0) res.Append("x="+xs[cur]+" maxZ="+maxZ.ToString("F1")+" endY="+rb.position.y.ToString("F2")+"; ");
  cur++; if(cur>=xs.Length){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); UnityEditor.SessionState.SetString("qabound","QUAY "+res); return; }
  rb.position=new Vector3(xs[cur],0.3f,108f); rb.rotation=Quaternion.LookRotation(Vector3.forward); rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; Physics.SyncTransforms(); maxZ=0; t0=Time.time; }
 fMove.SetValue(c,new Vector2(0f,1f)); };
Time.timeScale=1f; UnityEditor.EditorApplication.update+=cb; return "quay drive start";
