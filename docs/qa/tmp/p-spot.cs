Application.runInBackground=true; Time.timeScale=1f;
var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=car.GetComponent<Rigidbody>();
var mi=typeof(ArcadeCarController).GetField("moveInput",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
float z=(float)UnityEditor.SessionState.GetInt("p.z10",1071)/10f; float x0=(float)UnityEditor.SessionState.GetInt("p.x0",195);
foreach(var b in UnityEngine.Object.FindObjectsByType<CargoBox>(FindObjectsSortMode.None)) b.Repair();
rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero;
car.transform.SetPositionAndRotation(new Vector3(x0,-2.2f,z),Quaternion.LookRotation(Vector3.right)); Physics.SyncTransforms();
float t0=Time.realtimeSinceStartup; string log="";float prev=100;
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ float n=Time.realtimeSinceStartup-t0; mi.SetValue(car,new Vector2(0,rb.linearVelocity.magnitude<14f?0.7f:0f));
 foreach(var b in UnityEngine.Object.FindObjectsByType<CargoBox>(FindObjectsSortMode.None)){ if(Mathf.Abs(b.IntegrityHP-prev)>=3f){ log+=prev.ToString("F0")+">"+b.IntegrityHP.ToString("F0")+"@x"+rb.position.x.ToString("F1")+" v"+(rb.linearVelocity.magnitude*3.6f).ToString("F0")+"; "; prev=b.IntegrityHP; } }
 if(n>9f){ UnityEditor.EditorApplication.update-=cb; mi.SetValue(car,Vector2.zero); UnityEditor.SessionState.SetString("p.spot","z="+z+" x0="+x0+" endx="+rb.position.x.ToString("F0")+" hp="+prev.ToString("F0")+" drops:"+log); } };
UnityEditor.EditorApplication.update+=cb; return "go z="+z;
