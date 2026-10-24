// 같은 프레임(시간 정지)에서 메인 카메라를 세로 H 로 렌더 → CPU 에서 Nearest 로 1920x1080 확대 → PNG. 해상도별 렌더 시간도 잰다.
Time.timeScale=0f;
var cam=Camera.main; var urp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset; float rs=urp.renderScale;
int OW=1920, OH=1080; var dir="D:/Github/F1Game/docs/qa/evidence/";
int[] hs={1080,540,360,270}; string[] tags={"native","960","640","480"};
var sb=new System.Text.StringBuilder(); sb.Append("renderScale="+rs+" filter="+urp.upscalingFilter+" camPos="+cam.transform.position.ToString("F1")+" ; ");
var outs=new Color32[4][];
var prevTarget=cam.targetTexture;
var one=new Texture2D(1,1,TextureFormat.RGBA32,false);
for(int k=0;k<4;k++){
 int h=hs[k], w=h*16/9; int rw=Mathf.RoundToInt(w/rs), rh=Mathf.RoundToInt(h/rs);
 var rt=new RenderTexture(rw,rh,24){filterMode=FilterMode.Point}; rt.Create();
 cam.targetTexture=rt;
 // 시간: 워밍업 3회, 그 뒤 20회 평균. 매회 1px 읽어 GPU 완료까지 기다림.
 for(int i=0;i<3;i++){ cam.Render(); RenderTexture.active=rt; one.ReadPixels(new Rect(0,0,1,1),0,0); RenderTexture.active=null; }
 var sw=System.Diagnostics.Stopwatch.StartNew();
 for(int i=0;i<20;i++){ cam.Render(); RenderTexture.active=rt; one.ReadPixels(new Rect(0,0,1,1),0,0); one.Apply(); RenderTexture.active=null; }
 sw.Stop(); double ms=sw.Elapsed.TotalMilliseconds/20.0;
 cam.Render();
 // RT 를 실제 렌더 해상도(w×h)로 Point 축소 블릿(rs<1 이면 RT 안은 이미 정수배 블록) → 읽기
 var small=new RenderTexture(w,h,0); var prevF=rt.filterMode; Graphics.Blit(rt,small);
 RenderTexture.active=small; var tex=new Texture2D(w,h,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,w,h),0,0); tex.Apply(); RenderTexture.active=null;
 var src=tex.GetPixels32(); var dst=new Color32[OW*OH];
 for(int y=0;y<OH;y++){ int sy=y*h/OH; for(int x=0;x<OW;x++){ dst[y*OW+x]=src[sy*w + x*w/OW]; } }
 outs[k]=dst;
 var big=new Texture2D(OW,OH,TextureFormat.RGB24,false); big.SetPixels32(dst); big.Apply();
 System.IO.File.WriteAllBytes(dir+"lowres-"+tags[k]+".png",big.EncodeToPNG());
 sb.Append(tags[k]+"("+w+"x"+h+", RT "+rw+"x"+rh+"): "+ms.ToString("F2")+"ms ; ");
 cam.targetTexture=prevTarget; rt.Release(); UnityEngine.Object.Destroy(rt); small.Release(); UnityEngine.Object.Destroy(small); UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(big);
}
// 2x2 비교: 각 칸 960x540 (원본 1920x1080 을 2칸마다 1칸 — 반으로 줄임). 좌상 native, 우상 960, 좌하 640, 우하 480. 칸 사이 4px 검은 선.
int CW=960, CH=540; var cmp=new Color32[OW*OH];
for(int k=0;k<4;k++){ int ox=(k%2)*CW, oy=(k<2)?CH:0; var s=outs[k];
 for(int y=0;y<CH;y++) for(int x=0;x<CW;x++){ bool line=(x<2&&k%2==1)||(x>=CW-2&&k%2==0)||(y<2&&k<2)||(y>=CH-2&&k>=2); cmp[(oy+y)*OW+ox+x]= line? new Color32(0,0,0,255) : s[(y*2)*OW + x*2]; } }
var ct=new Texture2D(OW,OH,TextureFormat.RGB24,false); ct.SetPixels32(cmp); ct.Apply(); System.IO.File.WriteAllBytes(dir+"lowres-compare.png",ct.EncodeToPNG()); UnityEngine.Object.Destroy(ct);
// 확대 크롭: 화면 중앙(차 근처) 240x135 를 8배 → 1920x1080, 4장 다 (픽셀 경계 눈으로 확인용)
for(int k=0;k<4;k++){ var s=outs[k]; var z=new Color32[OW*OH]; int cx=OW/2-120, cy=OH/2-100;
 for(int y=0;y<OH;y++) for(int x=0;x<OW;x++) z[y*OW+x]=s[(cy+y/8)*OW + cx+x/8];
 var zt=new Texture2D(OW,OH,TextureFormat.RGB24,false); zt.SetPixels32(z); zt.Apply(); System.IO.File.WriteAllBytes(dir+"lowres-"+tags[k]+"-zoom8.png",zt.EncodeToPNG()); UnityEngine.Object.Destroy(zt); }
UnityEngine.Object.Destroy(one);
return sb.ToString();
