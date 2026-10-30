if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets") || !Application.isPlaying) throw new System.Exception("Studio play required");
var root=GameObject.Find("FIELD_REVIEW_ONLY");if(!root)throw new System.Exception("review root missing");
var cam=root.transform.Find("FieldReviewCamera").GetComponent<Camera>();
var lr=cam.GetComponent<LowResRenderTest>();var lt=UnityEngine.Object.FindFirstObjectByType<A7LightToggles>();
lt.painterly=true;lt.Apply();lr.targetHeight=720;lr.integerScale=false;lr.smooth=true;lr.SendMessage("LateUpdate");
string outDir=System.IO.Path.GetFullPath(Application.dataPath+"/../../art/_review/2026-10-29-field_patch/");
var log=new System.Text.StringBuilder();
// Same Camera.Render -> LowResRenderTest.Present -> PNG path as studio_cap.cs.
// Fix the source RT at 16:9; the editor GameView itself can have any window aspect.
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var rtField=typeof(LowResRenderTest).GetField("rt",flags);var oldRT=(RenderTexture)rtField.GetValue(lr);
var src=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);src.antiAliasing=1;src.Create();rtField.SetValue(lr,src);cam.targetTexture=src;cam.aspect=16f/9;
var dst=new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);dst.Create();
foreach(string option in new[]{"A","B"}) {
  root.transform.Find("A_aligned_8x6").gameObject.SetActive(option=="A");root.transform.Find("B_curved_10x7").gameObject.SetActive(option=="B");
  foreach(string view in new[]{"high","eye"}) {
    if(view=="high"){cam.transform.position=new Vector3(7,30,-23);cam.transform.LookAt(new Vector3(0,1,0));cam.fieldOfView=40;}
    else {cam.transform.position=new Vector3(-2.8f,1.65f,-9.5f);cam.transform.LookAt(new Vector3(2.4f,.8f,.4f));cam.fieldOfView=64;}
    cam.Render();lr.Present(dst);var prev=RenderTexture.active;RenderTexture.active=dst;
    var tx=new Texture2D(1280,720,TextureFormat.RGB24,false);tx.ReadPixels(new Rect(0,0,1280,720),0,0);tx.Apply();RenderTexture.active=prev;
    string filename=option+"_"+view+".png";System.IO.File.WriteAllBytes(outDir+filename,tx.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tx);log.Append(filename+" ");
  }
}
cam.targetTexture=oldRT;rtField.SetValue(lr,oldRT);src.Release();dst.Release();UnityEngine.Object.DestroyImmediate(src);UnityEngine.Object.DestroyImmediate(dst);
return log+"| source=1280x720; A7+Kuwahara radius="+lt.painterlyRadius+"; present="+lr.presentMaterial.shader.name;
