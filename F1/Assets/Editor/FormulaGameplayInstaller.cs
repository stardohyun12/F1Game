using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PitCrew.Garage;

public static class FormulaGameplayInstaller
{
 const string Dir="Assets/Art/CoastalFormula";
 static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/source/coastal-formula"));
 static Material Material(string state,string texture)
 {
  var path=Dir+"/Formula"+state+".mat";
  var ti=(TextureImporter)AssetImporter.GetAtPath(Dir+"/"+texture+".png");ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("PitCrew/LofiPilot/Surface"));AssetDatabase.CreateAsset(m,path);}
  m.SetColor("_Tint",Color.white);m.SetColor("_Ambient",Color.white);m.SetFloat("_FaceShade",.65f);m.SetFloat("_FogStart",100);m.SetFloat("_FogEnd",200);m.SetColor("_FogColor",new Color(.58f,.69f,.72f));m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/"+texture+".png"));EditorUtility.SetDirty(m);return m;
 }
 [MenuItem("PitCrew/Art/Coastal Formula/Install Gameplay Vehicle")]
 public static void Install()
 {
  if(EditorApplication.isPlaying)throw new Exception("Install only in edit mode");
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/ComplexTrack.unity")throw new Exception("Expected ComplexTrack scene");
  if(scene.isDirty)throw new Exception("Scene has unsaved edits; preserve them before installation");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var profile=AssetDatabase.LoadAssetAtPath<FormulaAppearanceProfile>(Dir+"/Appearance.asset");if(!profile){profile=ScriptableObject.CreateInstance<FormulaAppearanceProfile>();AssetDatabase.CreateAsset(profile,Dir+"/Appearance.asset");}
  profile.worn=Material("Worn","FormulaAtlasWorn");profile.serviced=Material("Serviced","FormulaAtlasServiced");profile.restored=Material("Restored","FormulaAtlas");EditorUtility.SetDirty(profile);
  var root=new GameObject("FormulaGameplayVisual");
  var imported=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/CoastalFormula.fbx"));
  PrefabUtility.UnpackPrefabInstance(imported,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);imported.transform.SetParent(root.transform,false);
  var chassis=new GameObject("ChassisLean").transform;chassis.SetParent(root.transform,false);
  var rig=root.AddComponent<FormulaVehicleVisual>();rig.profile=profile;rig.chassis=chassis;
  var renderers=imported.GetComponentsInChildren<MeshRenderer>();
  rig.parts=renderers.Select(r=>new FormulaVehicleVisual.Part{renderer=r,restoredBy=r.name.StartsWith("wheel_")?F1PartId.Tires:r.name=="engine"?F1PartId.Engine:r.name=="aero"?F1PartId.Aero:r.name=="suspension"?F1PartId.Suspension:F1PartId.Paint,cargo=r.name=="delivery_bag",driver=r.name=="driver"}).ToArray();
  rig.wheels=renderers.Where(r=>r.name.StartsWith("wheel_")).Select(r=>new FormulaVehicleVisual.Wheel{transform=r.transform,front=r.name.Contains("front"),radius=r.name.Contains("front")?.35f:.375f}).ToArray();
  foreach(var r in renderers){r.sharedMaterial=r.name=="driver"?profile.restored:profile.worn;r.transform.SetParent(r.name.StartsWith("wheel_")?root.transform:chassis,true);}
  UnityEngine.Object.DestroyImmediate(imported);
  var cargoMount=new GameObject("CargoMount").transform;cargoMount.SetParent(chassis,false);cargoMount.localPosition=new Vector3(0,.69f,-.92f);
  renderers.First(r=>r.name=="delivery_bag").transform.SetParent(cargoMount,true);
  int partCount=rig.parts.Length;
  PrefabUtility.SaveAsPrefabAsset(root,Dir+"/FormulaGameplayVisual.prefab");UnityEngine.Object.DestroyImmediate(root);
  var player=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();if(!player)throw new Exception("Player missing");
  var box=player.GetComponent<BoxCollider>();var rb=player.GetComponent<Rigidbody>();
  var prior=player.GetComponentInChildren<FormulaVehicleVisual>(true);if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);
  Directory.CreateDirectory(Output);var backup=Output+"/ComplexTrack.before-formula.unity";if(!File.Exists(backup))File.Copy(scene.path,backup);
  foreach(var t in player.GetComponentsInChildren<Transform>(true))if(t.name=="body"||t.name.StartsWith("wheel-")){var r=t.GetComponent<Renderer>();if(r)r.enabled=false;}
  var lean=player.GetComponent<CarVisualLean>();if(lean)lean.enabled=false;
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/FormulaGameplayVisual.prefab"),scene);instance.transform.SetParent(player.transform,false);
  // Fit the existing collider envelope, then place tire bottoms at its bottom plane.
  float scale=Mathf.Min(box.size.x/1.96f,box.size.z/3.785f);
  instance.transform.localScale=Vector3.one*scale;instance.transform.localPosition=new Vector3(box.center.x,box.center.y-box.size.y*.5f,box.center.z-.1375f*scale);
  foreach(var t in instance.GetComponentsInChildren<Transform>())t.gameObject.layer=player.gameObject.layer;
  if(instance.GetComponentsInChildren<Collider>().Length!=0||instance.GetComponentsInChildren<Rigidbody>().Length!=0)throw new Exception("Visual must not add physics bodies");
  foreach(var w in instance.GetComponent<FormulaVehicleVisual>().wheels){var b=w.transform.GetComponent<Renderer>().bounds;float bottom=player.transform.InverseTransformPoint(new Vector3(b.center.x,b.min.y,b.center.z)).y;if(Mathf.Abs(bottom-(box.center.y-box.size.y*.5f))>.003f)throw new Exception("Wheel bottom alignment failed");}
  File.WriteAllText(Output+"/gameplay-install.txt","Source="+scene.path+"\nVisualScale="+scale+"\nColliderCenter="+box.center+"\nColliderSize="+box.size+"\nMass="+rb.mass+"\nWheel bottoms align with existing collider. Existing physics unchanged.\nRenderers="+partCount);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
 public static void CaptureWear()
 {
  var old=SceneManager.GetActiveScene();var review=EditorSceneManager.OpenScene(Dir+"/CoastalFormulaReview.unity",OpenSceneMode.Additive);
  try{
   SceneManager.SetActiveScene(review);var oldCar=review.GetRootGameObjects().First(g=>g.name=="CoastalFormula");oldCar.SetActive(false);
   var car=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/FormulaGameplayVisual.prefab"),review);
   foreach(var t in car.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
   var cam=review.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();cam.transform.position=new Vector3(4,2.8f,5.5f);cam.transform.LookAt(new Vector3(0,.5f,0));cam.orthographic=true;cam.orthographicSize=2.25f;
   var method=typeof(CoastalFormulaBuilder).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
   method.Invoke(null,new object[]{cam,"08_worn",1200,800});
   foreach(var p in car.GetComponent<FormulaVehicleVisual>().parts)p.renderer.sharedMaterial=car.GetComponent<FormulaVehicleVisual>().profile.restored;
   method.Invoke(null,new object[]{cam,"09_restored",1200,800});
  }finally{EditorSceneManager.CloseScene(review,true);SceneManager.SetActiveScene(old);}
 }
}
