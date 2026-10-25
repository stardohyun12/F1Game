Application.runInBackground=true;
float t0=Time.time; string log=""; float prev=100; var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=car.GetComponent<Rigidbody>();
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(!Application.isPlaying){UnityEditor.EditorApplication.update-=cb;return;}
 foreach(var b in UnityEngine.Object.FindObjectsByType<CargoBox>(FindObjectsSortMode.None)){ if(Mathf.Abs(b.IntegrityHP-prev)>=3f){ log+="t"+(Time.time-t0).ToString("F0")+" "+prev.ToString("F0")+">"+b.IntegrityHP.ToString("F0")+"@("+rb.position.x.ToString("F0")+","+rb.position.z.ToString("F0")+")v"+(rb.linearVelocity.magnitude*3.6f).ToString("F0")+"; "; prev=b.IntegrityHP; } }
 var r=Services.Run as RunController; if(r.CurrentStage.ToString()=="Result") { UnityEditor.EditorApplication.update-=cb; }
 UnityEditor.SessionState.SetString("p.hp","final="+prev.ToString("F0")+" drops: "+log); };
UnityEditor.EditorApplication.update+=cb; return "hp watch on";
