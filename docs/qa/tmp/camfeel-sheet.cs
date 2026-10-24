// 1720: 프리셋 캡처 6행×2열(직선|코너) 시트 + 맵 끝 옹벽 캡처(에디트 모드, 메인 카메라 사본).
string dir="D:/Github/F1Game/docs/qa/evidence/camfeel/"; int W=960,H=540;
string[] tags={"0-none","1-calm","2-mid","3-f1","2-mid+moodA","2-mid+moodB"};
var sheet=new Texture2D(W*2,H*tags.Length,TextureFormat.RGB24,false); var px=new Color32[W*2*H*tags.Length];
for(int r=0;r<tags.Length;r++) for(int k=0;k<2;k++){ var t=new Texture2D(2,2); t.LoadImage(System.IO.File.ReadAllBytes(dir+tags[r]+(k==0?"_straight":"_corner")+".png")); var s=t.GetPixels32();
 int oy=(tags.Length-1-r)*H, ox=k*W; for(int y=0;y<H;y++) for(int x=0;x<W;x++){ bool line=x<2||y<2; px[(oy+y)*W*2+ox+x]=line?new Color32(0,0,0,255):s[y*W+x]; } UnityEngine.Object.DestroyImmediate(t); }
sheet.SetPixels32(px); sheet.Apply(); System.IO.File.WriteAllBytes(dir+"sheet.png",sheet.EncodeToPNG());
// 맵 끝: 남쪽 옹벽을 안쪽에서 비스듬히 + 동쪽 옹벽
var main=Camera.main; var go=new GameObject("tmpcap"); var cam=go.AddComponent<Camera>(); cam.CopyFrom(main); cam.fieldOfView=80f;
string res="";
Vector3[] pos={new Vector3(60,2.5f,6),new Vector3(165,2.5f,40)}; Vector3[] look={new Vector3(85,0.8f,-9),new Vector3(183,0.8f,60)};
for(int i=0;i<2;i++){ go.transform.position=pos[i]; go.transform.LookAt(look[i]);
 var rt=new RenderTexture(W*2,H*2,24){filterMode=FilterMode.Point}; rt.Create(); cam.targetTexture=rt; cam.Render();
 var small=new RenderTexture(W,H,0); Graphics.Blit(rt,small); RenderTexture.active=small; var tex=new Texture2D(W,H,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,W,H),0,0); tex.Apply(); RenderTexture.active=null;
 System.IO.File.WriteAllBytes(dir+"boundary-"+i+".png",tex.EncodeToPNG()); cam.targetTexture=null; rt.Release(); small.Release(); res+="boundary-"+i+" ok; "; }
UnityEngine.Object.DestroyImmediate(go);
return "sheet ok; "+res;
