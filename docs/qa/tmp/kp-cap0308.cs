Application.runInBackground=true; Time.timeScale=1f;
var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BuildPostcardSlice")).First(x=>x!=null);
var frame=t.GetMethod("RouteFrame"); var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=car.GetComponent<Rigidbody>();
var qs=UnityEditor.SessionState.GetString("p.capq","20,60,150,300").Split(',').Select(float.Parse).ToArray(); int idx=-1; float t0=0; bool shot=false; string log="";
var wind=UnityEngine.Object.FindFirstObjectByType<WindAmbience>(); var wsrc=wind!=null?wind.GetComponent<AudioSource>():null;
var clouds=UnityEngine.Object.FindFirstObjectByType<CloudDrift>(); Vector3 c0=clouds!=null?clouds.transform.GetChild(0).position:Vector3.zero; float ct0=Time.time;
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{ if(!Application.isPlaying){UnityEditor.EditorApplication.update-=cb;return;}
 rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero;
 if(idx>=0&&!shot&&Time.time-t0>3f){ UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/p0308-q"+qs[idx]+".png"); shot=true; var cam=Camera.main; log+="q"+qs[idx]+" camY="+cam.transform.position.y.ToString("F1")+" carY="+rb.position.y.ToString("F1")+" windVol="+(wsrc!=null?wsrc.volume.ToString("F2"):"na")+"; "; }
 if(idx<0||(shot&&Time.time-t0>3.6f)){ idx++; shot=false; if(idx>=qs.Length){ UnityEditor.EditorApplication.update-=cb; Vector3 c1=clouds!=null?clouds.transform.GetChild(0).position:Vector3.zero; UnityEditor.SessionState.SetString("p.cap","DONE "+log+" cloudMove="+(c1-c0).ToString("F1")+" over "+(Time.time-ct0).ToString("F1")+"s windSrc="+(wsrc!=null)+" clouds="+(clouds!=null)); return; }
  var a=new object[]{0,qs[idx],null,null,null}; frame.Invoke(null,a); var pos=(Vector3)a[2]; var fw=(Vector3)a[3];
  car.transform.SetPositionAndRotation(pos+Vector3.up*0.6f,Quaternion.LookRotation(new Vector3(fw.x,0,fw.z))); Physics.SyncTransforms(); t0=Time.time; }
};
UnityEditor.EditorApplication.update+=cb; return "cap start wind="+(wsrc!=null)+" clouds="+(clouds!=null);
