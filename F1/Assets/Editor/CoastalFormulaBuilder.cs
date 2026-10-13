using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CoastalFormulaBuilder
{
 const string Dir="Assets/Art/CoastalFormula";
 const int Layer=30;
 static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/source/coastal-formula"));
 static Bounds Bounds(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static void Layerize(GameObject g){foreach(var t in g.GetComponentsInChildren<Transform>())t.gameObject.layer=Layer;}
 static void Capture(Camera c,string name,int w=1200,int h=800){var rt=new RenderTexture(w,h,24);var old=RenderTexture.active;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);try{c.targetTexture=rt;c.aspect=(float)w/h;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Output+"/"+name+".png",tex.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
 static Material Mat(string name,Color color,Texture texture=null){var m=new Material(Shader.Find("PitCrew/LofiPilot/Surface"));m.name=name;m.SetColor("_Tint",color);m.SetColor("_Ambient",Color.white);m.SetFloat("_FaceShade",.8f);m.SetFloat("_FogStart",100);m.SetFloat("_FogEnd",200);if(texture)m.SetTexture("_MainTex",texture);var p=Dir+"/"+name+".mat";var ex=AssetDatabase.LoadAssetAtPath<Material>(p);if(ex){EditorUtility.CopySerialized(m,ex);UnityEngine.Object.DestroyImmediate(m);return ex;}AssetDatabase.CreateAsset(m,p);return m;}
 static void Aim(Camera c,Vector3 p,Vector3 t,float size){c.orthographic=true;c.orthographicSize=size;c.transform.position=p;c.transform.LookAt(t);}
 [MenuItem("PitCrew/Art/Coastal Formula/Build and Capture")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit play mode before asset build");
  Directory.CreateDirectory(Output);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var ti=(TextureImporter)AssetImporter.GetAtPath(Dir+"/FormulaAtlas.png");ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
  var material=Mat("FormulaShared",Color.white,AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/FormulaAtlas.png"));
  var old=SceneManager.GetActiveScene();bool dirty=old.isDirty;var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  try{
   var car=new GameObject("CoastalFormula");var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/CoastalFormula.fbx"));visual.transform.SetParent(car.transform,false);
   foreach(var r in car.GetComponentsInChildren<Renderer>())r.sharedMaterials=new[]{material};
   var wheels=car.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("wheel_")).ToArray();
   if(wheels.Length!=4)throw new Exception("Expected four independent wheels");
   // FBX import keeps wheel-centered mesh origins, checked independently in Unity.
   var checks=new System.Collections.Generic.List<string>();
   foreach(var w in wheels){var b=Bounds(w.gameObject);if(Vector3.Distance(w.position,b.center)>.005f)throw new Exception("Off-center wheel pivot "+w.name);if(Mathf.Abs(b.min.y)>.002f)throw new Exception("Wheel not on ground "+w.name+" "+b.min.y);if((w.name.EndsWith("left")&&w.position.x>=0)||(w.name.EndsWith("right")&&w.position.x<=0))throw new Exception("Wheel side reversed");checks.Add(w.name+" pivot="+w.localPosition+" bottom="+b.min.y);}
   var bag=car.GetComponentsInChildren<Transform>().Single(t=>t.name=="delivery_bag");var mount=new GameObject("CargoMount").transform;mount.SetParent(car.transform,false);mount.localPosition=new Vector3(0,.69f,-.92f);bag.SetParent(mount,true);
   var collision=car.AddComponent<BoxCollider>();collision.center=new Vector3(0,.43f,-.04f);collision.size=new Vector3(1.25f,.55f,2.7f);
   var prefab=PrefabUtility.SaveAsPrefabAsset(car,Dir+"/CoastalFormula.prefab");
   Layerize(car);
   var light=new GameObject("ReviewSun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.shadows=LightShadows.Soft;light.cullingMask=1<<Layer;light.transform.rotation=Quaternion.Euler(48,-35,0);RenderSettings.sun=light;
   var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="ReviewFloor";ground.transform.position=new Vector3(0,-.08f,0);ground.transform.localScale=new Vector3(20,.15f,20);ground.GetComponent<Renderer>().sharedMaterial=Mat("ReviewFloor",new Color(.55f,.59f,.55f));Layerize(ground);
   var cam=new GameObject("ReviewCamera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.58f,.68f,.69f);cam.cullingMask=1<<Layer;cam.nearClipPlane=.05f;cam.farClipPlane=150;cam.allowHDR=false;cam.allowMSAA=false;
   Aim(cam,new Vector3(4,2.8f,5.5f),new Vector3(0,.5f,0),2.25f);Capture(cam,"01_front");
   Aim(cam,new Vector3(-4,2.6f,-5.5f),new Vector3(0,.5f,0),2.25f);Capture(cam,"02_rear");
   Aim(cam,new Vector3(6,.65f,0),new Vector3(0,.65f,0),1.55f);Capture(cam,"03_side",1400,650);
   Aim(cam,new Vector3(0,7,0),Vector3.zero,2.3f);cam.transform.rotation=Quaternion.Euler(90,0,0);Capture(cam,"04_top",850,1100);
   // Check steering and rolling pivots visually, then restore.
   foreach(var w in wheels){var original=w.localRotation;w.Rotate(Vector3.right,65,Space.Self);if(w.name.Contains("front"))w.Rotate(Vector3.up,22,Space.World);checks.Add(w.name+" rotation-center="+Vector3.Distance(w.position,Bounds(w.gameObject).center));w.localRotation=original;}
   var previous=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/Generated/race_v2.fbx");
   if(previous){var comparison=(GameObject)PrefabUtility.InstantiatePrefab(previous);comparison.name="PreviousCar";comparison.transform.position=new Vector3(-2.3f,0,0);comparison.transform.localScale=Vector3.one*1.35f;Layerize(comparison);
    foreach(var r in comparison.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>Mat("Previous_"+m.name,m.HasProperty("_Color")?m.color:Color.gray)).ToArray();
    car.transform.position=new Vector3(2.2f,0,0);Aim(cam,new Vector3(6,5,9),new Vector3(0,.45f,0),3.4f);Capture(cam,"05_comparison",1500,850);UnityEngine.Object.DestroyImmediate(comparison);car.transform.position=Vector3.zero;}
   Aim(cam,new Vector3(-4,2.6f,-5.5f),new Vector3(0,.5f,0),2.25f);
   EditorSceneManager.SaveScene(scene,Dir+"/CoastalFormulaReview.unity");
   var tris=car.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);var bounds=Bounds(car);
   var errors=ShaderUtil.GetShaderMessages(material.shader).Where(m=>m.severity.ToString()=="Error").ToArray();if(errors.Length>0)throw new Exception("Shader errors");
   File.WriteAllText(Output+"/unity-verification.txt","Mesh triangles="+tris+"\nBounds="+bounds+"\nIndependent wheels="+wheels.Length+"\nShader errors="+errors.Length+"\nSource scene="+old.path+" dirty="+dirty+"\n"+string.Join("\n",checks));
  }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(old);}
  // Existing coastal art scene: temporary swap for camera comparison; never save it.
  var town=EditorSceneManager.OpenScene("Assets/Art/LofiPilot/LofiPilot.unity",OpenSceneMode.Additive);
  try{SceneManager.SetActiveScene(town);foreach(var root in town.GetRootGameObjects()){if(root.name=="ExistingFormula_ArtOnly"||root.name=="DeliveryBag_mount_visual")root.SetActive(false);}
   var car=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/CoastalFormula.prefab"),town);Layerize(car);
   var cam=town.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();cam.orthographic=false;cam.fieldOfView=50;cam.transform.position=new Vector3(0,2.35f,-5.5f);cam.transform.LookAt(new Vector3(0,.6f,1.2f));
   Capture(cam,"06_town_chase",960,540);Capture(cam,"07_town_chase_640",640,360);
  }finally{EditorSceneManager.CloseScene(town,true);SceneManager.SetActiveScene(old);}
  AssetDatabase.SaveAssets();Debug.Log("COASTAL_FORMULA_OK "+Output);
 }
}
