"""Prepare F1Studio review assets only, then emit guarded ucli exec scripts.

Default emits scripts without touching any Unity project.
--copy is for the authorized studio operator; it copies only into the task's
isolated Assets/Art/Review/env-first folder. It never contacts Unity itself.
"""
from pathlib import Path
import argparse, json, shutil

R=Path(__file__).resolve().parents[2];E=R/'art/export/env-first';V=R/'art/_review/2026-10-30-env-first'
D=R/'F1Studio/Assets/Art/Review/env-first';K=R/'art/export/kit-shared-v2'
manifest=json.loads((E/'manifest.json').read_text(encoding='utf-8'))
guard='''if (!Application.dataPath.Replace("\\\\", "/").EndsWith("/F1Studio/Assets")) return "FAIL: F1Studio only";
'''

def prepare():
    entries=[]
    for b in manifest['buildings']:
        px,pz=b['pivot_xz']
        # Coordinates follow the Blender exporter used by the existing house pipeline.
        # The capture code additionally verifies/corrects X/Z signs from footprint vertices.
        flat=', '.join(f'new Vector2({x-px:.6f}f, {z-pz:.6f}f)' for x,z in b['poly'])
        entries.append(f'("{b["name"]}", new Vector3({px-185:.6f}f, {b["y"]-25.27:.6f}f, {pz+61:.6f}f), new Vector2[] {{ {flat} }})')
    setup=guard+'''if (Application.isPlaying) return "FAIL: import in Edit mode";
const string D = "Assets/Art/Review/env-first";
var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SceneA7/Mats/SA7_house_shop.mat");
if (!template) return "FAIL: A7 reference material missing";
var log = new System.Text.StringBuilder();
foreach (var slot in new[] { "wall", "trim", "roof", "floor" }) {
  string tp = D + "/" + slot + ".png";
  var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
  if (!ti) return "FAIL: run env_stage.py --copy then editor refresh first";
  ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true;
  ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false;
  ti.textureCompression = TextureImporterCompression.Uncompressed;
  ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 512; ti.SaveAndReimport();
  string mp = D + "/Shared_" + slot + ".mat";
  var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
  if (!mat) { mat = new Material(template); AssetDatabase.CreateAsset(mat, mp); }
  else mat.CopyPropertiesFromMaterial(template);
  mat.name = "Shared_" + slot; mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
  if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mat.mainTexture);
  if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mat.mainTexture);
  if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
  if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
  if (mat.HasProperty("_Sat")) mat.SetFloat("_Sat", 1f);
  EditorUtility.SetDirty(mat); log.Append(slot + ": " + mat.shader.name + "; ");
}
AssetDatabase.SaveAssets();
return "OK review-only material setup: " + log;
'''
    (V/'studio_import.cs').write_text(setup,encoding='utf-8')
    capture=guard+'''if (!Application.isPlaying) return "FAIL: Play mode required; this script never saves a scene";
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Studio_Building") return "FAIL: open Studio_Building manually without discarding unsaved work";
if (GameObject.Find("ENV_FIRST_REVIEW")) return "FAIL: stop/restart Play before re-running";
const string D = "Assets/Art/Review/env-first";
var camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == "Cam_ST_road");
if (!camera || !camera.GetComponent<LowResRenderTest>()) return "FAIL: existing A7 720 camera missing";
Application.runInBackground = true;
var root = new GameObject("ENV_FIRST_REVIEW");
var log = new System.Text.StringBuilder();
System.Func<GameObject, Vector2[], Vector2> signs = (obj, poly) => {
  // Infer FBX handedness from the body floor ring. No blind mirrored placement.
  float best = float.MaxValue; Vector2 result = Vector2.one;
  var points = new System.Collections.Generic.List<Vector2>();
  foreach(var mf in obj.GetComponentsInChildren<MeshFilter>(true)){
    if(!mf.name.EndsWith("_BODY"))continue;
    foreach(var p0 in mf.sharedMesh.vertices){var p=obj.transform.InverseTransformPoint(mf.transform.TransformPoint(p0));
      if(Mathf.Abs(p.y)<.005f)points.Add(new Vector2(p.x,p.z));}
  }
  foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) {
    float err = 0f; int n = 0;
    foreach (var q in poly) {
      float dd=float.MaxValue;
      foreach(var p in points)dd=Mathf.Min(dd,(q-new Vector2(p.x*sx,p.y*sz)).sqrMagnitude);
      err+=dd;n++;
    }
    if (n > 0 && err/n < best) { best=err/n;result=new Vector2(sx,sz); }
  }
  if (best > .02f) throw new System.Exception("FBX footprint transform check failed " + obj.name + " error " + best);
  log.Append(obj.name + " signs " + result + " footprint RMS " + Mathf.Sqrt(best) + "\\n");return result;
};
var entries = new (string name, Vector3 pos, Vector2[] poly)[] {
'''+',\n'.join(entries)+'''
};
var rendererStates = new System.Collections.Generic.Dictionary<Renderer,bool>();
var camStates = new System.Collections.Generic.Dictionary<Camera,bool>();
var originalPosition=camera.transform.position;var originalRotation=camera.transform.rotation;
float originalFov=camera.fieldOfView;bool originalOrtho=camera.orthographic;float originalSize=camera.orthographicSize;
float originalNear=camera.nearClipPlane, originalFar=camera.farClipPlane;
var lr=camera.GetComponent<LowResRenderTest>();int oldHeight=lr.targetHeight;bool oldSmooth=lr.smooth;
RenderTexture dst=null;
try {
  foreach (var e in entries) {
    var asset=AssetDatabase.LoadAssetAtPath<GameObject>(D+"/"+e.name+".fbx");if (!asset) throw new System.Exception("missing " + e.name);
    var go=UnityEngine.Object.Instantiate(asset);go.name=e.name;go.transform.SetParent(root.transform,false);
    var sign=signs(go,e.poly);go.transform.localScale=new Vector3(sign.x,1,sign.y);go.transform.localPosition=e.pos;
    foreach (var ren in go.GetComponentsInChildren<Renderer>(true)) {
      var old=ren.sharedMaterials;var mm=new Material[old.Length];
      for (int i=0;i<old.Length;i++) {
        string nm=old[i] ? old[i].name.Replace(" (Instance)","") : "";
        string slot=new[]{"wall","trim","roof","floor"}.FirstOrDefault(s=>nm.Contains("Shared_"+s));
        if(slot==null)throw new System.Exception("unmapped material: " + nm);
        mm[i]=AssetDatabase.LoadAssetAtPath<Material>(D+"/Shared_"+slot+".mat");
      }
      ren.sharedMaterials=mm;
    }
  }
'''
    for key,p in manifest['landmarks'].items():
        asset='Assets/Art/Review/house_home/house_home.prefab' if key=='house_home' else f'Assets/Art/Library/Buildings/{key}/{key}.prefab'
        x,z=p['world'];capture+=f'''  {{ var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("{asset}");if(!prefab)throw new System.Exception("missing reference {key}");
    var go=UnityEngine.Object.Instantiate(prefab);go.name="REFERENCE_{key}";go.transform.SetParent(root.transform,false);
    go.transform.localPosition=new Vector3({x-185:.6f}f,{p['y']-25.27:.6f}f,{z+61:.6f}f);go.transform.localRotation=Quaternion.Euler(0,{p['yaw']:.6f}f,0);go.SetActive(true); }}
'''
    # Ground uses the same static triangles as the Blender reference; no map or scene asset edits.
    capture+='''  string dataPath=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../art/export/env-first/studio-context.json"));
  var data=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dataPath));
  foreach (var chunk in data["meshes"]) {
    var go=new GameObject((string)chunk["name"]);go.transform.SetParent(root.transform,false);
    var verts=((Newtonsoft.Json.Linq.JArray)chunk["vertices"]).Select(v=>new Vector3((float)v[0],(float)v[2],-(float)v[1])).ToArray();
    var uv=((Newtonsoft.Json.Linq.JArray)chunk["uv"]).Select(v=>new Vector2((float)v[0],(float)v[1])).ToArray();
    var ix=((Newtonsoft.Json.Linq.JArray)chunk["triangles"]).Select(v=>(int)v).ToArray();
    // Blender-to-Unity handedness conversion above reverses winding.
    for(int i=0;i<ix.Length;i+=3){int t=ix[i+1];ix[i+1]=ix[i+2];ix[i+2]=t;}
    var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.vertices=verts;mesh.uv=uv;mesh.triangles=ix;mesh.RecalculateNormals();mesh.RecalculateBounds();
    go.AddComponent<MeshFilter>().sharedMesh=mesh;
    var contextMat=AssetDatabase.LoadAssetAtPath<Material>(D+"/Shared_floor.mat");
    if(go.name.Contains("terrain")){
      contextMat=new Material(contextMat);contextMat.name="ENV_FIRST_CONTEXT_ONLY";
      contextMat.mainTexture=null;
      foreach(var slot in new[]{"_BaseMap","_MainTex"})if(contextMat.HasProperty(slot))contextMat.SetTexture(slot,null);
      foreach(var slot in new[]{"_Color","_BaseColor"})if(contextMat.HasProperty(slot))contextMat.SetColor(slot,new Color(.66f,.68f,.62f));
    }
    go.AddComponent<MeshRenderer>().sharedMaterial=contextMat;
  }
  foreach(var ren in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
    if(ren.transform.IsChildOf(root.transform))continue;
    rendererStates[ren]=ren.enabled;ren.enabled=false;
  }
  foreach(var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(cam.GetComponent<LowResRenderTest>()){
    camStates[cam]=cam.gameObject.activeSelf;cam.gameObject.SetActive(cam==camera);
  }
  var lt=UnityEngine.Object.FindFirstObjectByType<A7LightToggles>();if(!lt)throw new System.Exception("A7LightToggles missing");
  lt.painterly=false;lt.realtimeShadows=true;lt.Apply();
  lr.targetHeight=720;lr.smooth=false;camera.fieldOfView=64f;camera.nearClipPlane=.08f;camera.farClipPlane=600f;
  string outDir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../art/_review/2026-10-30-env-first/"));
  dst=new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
  var views=new(string name,Vector3 pos,Vector3 target,bool ortho,float size)[]{
    ("a7_cart_home",new Vector3(-8,2.8f,-19),new Vector3(16,4.8f,0),false,0),
    ("a7_cart_route",new Vector3(67,5.1f,11),new Vector3(88,8.73f,36),false,0),
    ("a7_top",new Vector3(42,180,-5),new Vector3(42,0,-5),true,44.15625f)};
  foreach(var v in views){
    camera.transform.position=v.pos;camera.transform.LookAt(v.target,v.ortho?Vector3.forward:Vector3.up);
    camera.orthographic=v.ortho;if(v.ortho)camera.orthographicSize=v.size;
    lr.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);camera.Render();lr.Present(dst);
    var prev=RenderTexture.active;RenderTexture.active=dst;var tx=new Texture2D(1280,720,TextureFormat.RGB24,false);
    tx.ReadPixels(new Rect(0,0,1280,720),0,0);tx.Apply();RenderTexture.active=prev;
    System.IO.File.WriteAllBytes(outDir+v.name+".png",tx.EncodeToPNG());UnityEngine.Object.Destroy(tx);
    log.Append(v.name+" 1280x720 A7 shadows=on painterly=off\\n");
  }
  System.IO.File.WriteAllText(outDir+"a7-evidence.txt",log.ToString());
  return "OK A7 review captured, no scene saved. " + log;
} finally {
  camera.transform.SetPositionAndRotation(originalPosition,originalRotation);camera.fieldOfView=originalFov;
  camera.nearClipPlane=originalNear;camera.farClipPlane=originalFar;
  camera.orthographic=originalOrtho;camera.orthographicSize=originalSize;lr.targetHeight=oldHeight;lr.smooth=oldSmooth;
  foreach(var p in rendererStates)if(p.Key)p.Key.enabled=p.Value;
  foreach(var p in camStates)if(p.Key)p.Key.gameObject.SetActive(p.Value);
  if(dst){dst.Release();UnityEngine.Object.Destroy(dst);}UnityEngine.Object.Destroy(root);
}
'''
    (V/'studio_capture.cs').write_text(capture,encoding='utf-8')
    print('PREPARED studio_import.cs / studio_capture.cs (not executed)')

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--copy',action='store_true',help='Authorized F1Studio operator only');args=ap.parse_args()
    prepare()
    if args.copy:
        assert D.resolve().is_relative_to((R/'F1Studio/Assets/Art/Review').resolve())
        D.mkdir(parents=True,exist_ok=True)
        for b in manifest['buildings']:shutil.copy2(E/(b['name']+'.fbx'),D/(b['name']+'.fbx'))
        for slot in ['wall','trim','roof','floor']:shutil.copy2(K/(slot+'.png'),D/(slot+'.png'))
        print('COPIED review assets only:',D)
