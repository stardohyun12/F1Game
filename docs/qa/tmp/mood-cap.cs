// 1609: 운전 중 같은 프레임(시간 정지)에서 현재·A·B 무드를 960x540 렌더 → 각 1920x1080 Nearest PNG + 원화와 2x2 비교. 렌더 시간 측정.
Time.timeScale=0f;
var cam=Camera.main; var urp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset; float rs=urp.renderScale;
var dir="D:/Github/F1Game/docs/qa/evidence/"; int W=960,H=540,OW=1920,OH=1080;
string[] tags={"current","A","B"};
var sb=new System.Text.StringBuilder("renderScale="+rs+" filter="+urp.upscalingFilter+" cam="+cam.transform.position.ToString("F1")+"\n");
var cells=new Color32[4][];
// 원화: 비율 유지하며 가운데 16:9 로 잘라 960x540 nearest
var ref_=new Texture2D(2,2); ref_.LoadImage(System.IO.File.ReadAllBytes("D:/Github/F1Game/art/concept/lofi-styles-2026-10-13/02_warm_street.png"));
{ int rw=ref_.width, rh=ref_.height; int cw=rw, ch=rw*9/16; if(ch>rh){ ch=rh; cw=rh*16/9; } int ox=(rw-cw)/2, oy=(rh-ch)/2; var rp=ref_.GetPixels32(); var c=new Color32[W*H];
  for(int y=0;y<H;y++) for(int x=0;x<W;x++) c[y*W+x]=rp[(oy+y*ch/H)*rw + ox+x*cw/W]; cells[0]=c; sb.Append("ref "+rw+"x"+rh+"\n"); }
var one=new Texture2D(1,1,TextureFormat.RGBA32,false); var prev=cam.targetTexture;
for(int k=0;k<3;k++){
 string r = k==0? MoodSwitcher.Revert() : MoodSwitcher.Apply(k==1?MoodSwitcher.PathA:MoodSwitcher.PathB);
 int rw=Mathf.RoundToInt(W/rs), rh=Mathf.RoundToInt(H/rs);
 var rt=new RenderTexture(rw,rh,24){filterMode=FilterMode.Point}; rt.Create(); cam.targetTexture=rt;
 for(int i=0;i<3;i++){ cam.Render(); RenderTexture.active=rt; one.ReadPixels(new Rect(0,0,1,1),0,0); RenderTexture.active=null; }
 var sw=System.Diagnostics.Stopwatch.StartNew();
 for(int i=0;i<20;i++){ cam.Render(); RenderTexture.active=rt; one.ReadPixels(new Rect(0,0,1,1),0,0); one.Apply(); RenderTexture.active=null; }
 sw.Stop(); double ms=sw.Elapsed.TotalMilliseconds/20.0;
 cam.Render();
 var small=new RenderTexture(W,H,0); Graphics.Blit(rt,small);
 RenderTexture.active=small; var tex=new Texture2D(W,H,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,W,H),0,0); tex.Apply(); RenderTexture.active=null;
 var src=tex.GetPixels32(); cells[k+1]=src; var dst=new Color32[OW*OH];
 for(int y=0;y<OH;y++) for(int x=0;x<OW;x++) dst[y*OW+x]=src[(y/2)*W+x/2];
 var big=new Texture2D(OW,OH,TextureFormat.RGB24,false); big.SetPixels32(dst); big.Apply(); System.IO.File.WriteAllBytes(dir+"mood-"+tags[k]+".png",big.EncodeToPNG());
 sb.Append(tags[k]+" ["+r+"] "+ms.ToString("F2")+"ms fog="+RenderSettings.fog+" amb="+RenderSettings.ambientMode+"\n");
 cam.targetTexture=prev; rt.Release(); UnityEngine.Object.Destroy(rt); small.Release(); UnityEngine.Object.Destroy(small); UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(big);
}
// 2x2: 좌상 원화, 우상 현재, 좌하 A, 우하 B. 4px 검은 선.
var cmp=new Color32[OW*OH];
for(int k=0;k<4;k++){ int ox=(k%2)*W, oy=(k<2)?H:0; var s=cells[k];
 for(int y=0;y<H;y++) for(int x=0;x<W;x++){ bool line=(x<2&&k%2==1)||(x>=W-2&&k%2==0)||(y<2&&k<2)||(y>=H-2&&k>=2); cmp[(oy+y)*OW+ox+x]= line? new Color32(0,0,0,255) : s[y*W+x]; } }
var ct=new Texture2D(OW,OH,TextureFormat.RGB24,false); ct.SetPixels32(cmp); ct.Apply(); System.IO.File.WriteAllBytes(dir+"mood-compare.png",ct.EncodeToPNG());
sb.Append("final revert: "+MoodSwitcher.Revert());
return sb.ToString();
