// 1720: 프리셋별 직선 풀스로틀·코너 캡처 + FOV/롤/흔들림/프레임시간 측정. 결과는 SessionState "camfeel".
Application.runInBackground=true; Time.timeScale=1f;
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=c.GetComponent<Rigidbody>();
var fc=UnityEngine.Object.FindFirstObjectByType<FollowCamera>(); var cam=fc.GetComponent<Camera>();
var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var fMove=typeof(ArcadeCarController).GetField("moveInput",bf); var fBase=typeof(FollowCamera).GetField("basePos",bf);
var urp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset; float rs=urp.renderScale;
string dir="D:/Github/F1Game/docs/qa/evidence/camfeel/"; System.IO.Directory.CreateDirectory(dir);
string[] tags={"0-none","1-calm","2-mid","3-f1","2-mid+moodA","2-mid+moodB"};
string[] presetPaths={null,CameraFeelSwitcher.Calm,CameraFeelSwitcher.Mid,CameraFeelSwitcher.F1,CameraFeelSwitcher.Mid,CameraFeelSwitcher.Mid};
// 직선 95m + 끝 지점에서 오른쪽 20° 로 25m 빈 차선 찾기
Vector3[] dirs={Vector3.right,Vector3.left,Vector3.forward,Vector3.back}; Vector3 start=Vector3.zero, ldir=Vector3.zero;
for(int k=0;k<3000 && ldir==Vector3.zero;k++){ var d=dirs[k%4]; float a=(k*7919)%1000/1000f, b=(k*104729)%1000/1000f;
 var p=new Vector3(Mathf.Lerp(-5,180,a),0.55f,Mathf.Lerp(-5,130,b)); var turn=Quaternion.Euler(0,20,0)*d; var end=p+d*70f;
 if(!Physics.CheckSphere(p+Vector3.up*0.5f,1.0f) && !Physics.SphereCast(p,0.6f,d,out var h,95f) && !Physics.SphereCast(p+Vector3.up,0.9f,d,out var h2,95f) && !Physics.SphereCast(end,0.6f,turn,out var h3,25f) && Physics.Raycast(p+Vector3.up,Vector3.down,3f)) { start=new Vector3(p.x,1f,p.z); ldir=d; } }
if(ldir==Vector3.zero) return "no lane";
var log=new System.Text.StringBuilder("lane start="+start.ToString("F1")+" dir="+ldir+" screen="+cam.pixelWidth+"x"+cam.pixelHeight+" rs="+rs+"\n");
var one=new Texture2D(1,1,TextureFormat.RGBA32,false);
System.Func<string,double> Cap=(name)=>{ int W=960,H=540; int rw=Mathf.RoundToInt(W/rs), rh=Mathf.RoundToInt(H/rs); var prev=cam.targetTexture;
 var rt=new RenderTexture(rw,rh,24){filterMode=FilterMode.Point}; rt.Create(); cam.targetTexture=rt;
 for(int i=0;i<2;i++){ cam.Render(); RenderTexture.active=rt; one.ReadPixels(new Rect(0,0,1,1),0,0); RenderTexture.active=null; }
 var sw=System.Diagnostics.Stopwatch.StartNew(); for(int i=0;i<10;i++){ cam.Render(); RenderTexture.active=rt; one.ReadPixels(new Rect(0,0,1,1),0,0); one.Apply(); RenderTexture.active=null; } sw.Stop();
 cam.Render(); var small=new RenderTexture(W,H,0); Graphics.Blit(rt,small); RenderTexture.active=small; var tex=new Texture2D(W,H,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,W,H),0,0); tex.Apply(); RenderTexture.active=null;
 System.IO.File.WriteAllBytes(dir+name+".png",tex.EncodeToPNG()); cam.targetTexture=prev; rt.Release(); small.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(small); UnityEngine.Object.Destroy(tex);
 return sw.Elapsed.TotalMilliseconds/10.0; };
System.Func<string> Probe=()=>{ var bp=(Vector3)fBase.GetValue(fc); var dd=cam.transform.position-bp; var look=c.transform.position+Vector3.up*(fc.Preset?fc.Preset.lookHeight:1.05f);
 float mpp=2f*Vector3.Distance(bp,look)*Mathf.Tan(cam.fieldOfView*0.5f*Mathf.Deg2Rad)/(cam.pixelHeight*rs);
 return "spd="+(c.CurrentSpeedMps*3.6f).ToString("F1")+"/"+(c.MaxSpeedMps*3.6f).ToString("F1")+"kmh fov="+cam.fieldOfView.ToString("F1")+" roll="+fc.DebugRoll.ToString("F2")+" lat="+fc.DebugLatAccel.ToString("F1")+" steer="+c.SteerInput.ToString("F2")+" shakePx=("+(Vector3.Dot(dd,cam.transform.right)/mpp).ToString("F2")+","+(Vector3.Dot(dd,cam.transform.up)/mpp).ToString("F2")+") camH="+(cam.transform.position.y-c.transform.position.y).ToString("F2"); };
int run=-1, phase=0; float t0=0, dtSum=0; int dtN=0; float maxFov=0; var shakeSet=new System.Collections.Generic.HashSet<string>(); var turnStart=0f;
UnityEditor.EditorApplication.CallbackFunction cb=null;
cb=()=>{
 if(run<0 || phase==3){
  run++; if(run>=tags.Length){ UnityEditor.EditorApplication.update-=cb; fMove.SetValue(c,Vector2.zero); log.Append("final mood revert: "+MoodSwitcher.Revert()+"\n"); fc.Preset=UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(CameraFeelSwitcher.Mid); UnityEditor.SessionState.SetString("camfeel",log.ToString()); System.IO.File.WriteAllText(dir+"log.txt",log.ToString()); return; }
  if(tags[run].Contains("moodA")) log.Append("mood "+MoodSwitcher.Apply(MoodSwitcher.PathA)+"\n"); else if(tags[run].Contains("moodB")) log.Append("mood "+MoodSwitcher.Apply(MoodSwitcher.PathB)+"\n");
  fc.Preset=presetPaths[run]!=null?UnityEditor.AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(presetPaths[run]):null;
  rb.position=start; rb.rotation=Quaternion.LookRotation(ldir); rb.linearVelocity=Vector3.zero; rb.angularVelocity=Vector3.zero; Physics.SyncTransforms();
  fc.enabled=false; cam.transform.position=start-ldir*5f+Vector3.up*2.5f; fc.enabled=true;
  phase=0; t0=Time.time; dtSum=0; dtN=0; maxFov=0; shakeSet.Clear(); }
 float t=Time.time-t0; float traveled=Vector3.Dot(rb.position-start,ldir);
 if(phase==0){ fMove.SetValue(c,new Vector2(0,1)); if(t>1f){ dtSum+=Time.unscaledDeltaTime; dtN++; } maxFov=Mathf.Max(maxFov,cam.fieldOfView);
  if(c.CurrentSpeedMps>0.85f*c.MaxSpeedMps){ var bp=(Vector3)fBase.GetValue(fc); var dd=cam.transform.position-bp; if(dd.sqrMagnitude>1e-8f){ var look=c.transform.position+Vector3.up*(fc.Preset?fc.Preset.lookHeight:1.05f); float mpp=2f*Vector3.Distance(bp,look)*Mathf.Tan(cam.fieldOfView*0.5f*Mathf.Deg2Rad)/(cam.pixelHeight*rs); shakeSet.Add(Mathf.Round(Vector3.Dot(dd,cam.transform.right)/mpp*100f)/100f+"/"+Mathf.Round(Vector3.Dot(dd,cam.transform.up)/mpp*100f)/100f); } }
  if(traveled>=68f || t>8f){ double ms=Cap(tags[run]+"_straight"); log.Append(tags[run]+" STRAIGHT t="+t.ToString("F1")+" dist="+traveled.ToString("F0")+" "+Probe()+" maxFovSeen="+maxFov.ToString("F1")+" frame="+(dtSum/Mathf.Max(1,dtN)*1000f).ToString("F2")+"ms(n="+dtN+") camRender="+ms.ToString("F2")+"ms shakeOffsetsPx={"+string.Join(" ",shakeSet)+"}\n"); phase=1; turnStart=Time.time; } return; }
 if(phase==1){ fMove.SetValue(c,new Vector2(0.8f,1)); if(Time.time-turnStart>=0.7f){ double ms=Cap(tags[run]+"_corner"); log.Append(tags[run]+" CORNER "+Probe()+" camRender="+ms.ToString("F2")+"ms\n"); phase=3; fMove.SetValue(c,Vector2.zero); } }
};
UnityEditor.EditorApplication.update+=cb; return log.ToString()+"started";
