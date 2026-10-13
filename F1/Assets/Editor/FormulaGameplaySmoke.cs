using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using PitCrew.Garage;

/// <summary>Opt-in play-mode integration probe. Purchases are confined to play mode.</summary>
[InitializeOnLoad]
public static class FormulaGameplaySmoke
{
 const string Key="FormulaGameplaySmoke.Pending";
 static double began;
 static int step;
 static Vector3 start;
 static Quaternion wheelStart;
 static float maxSpeed;
 static string log="";
 static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/source/coastal-formula"));
 static FormulaGameplaySmoke(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}
 public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Start in edit mode");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){began=EditorApplication.timeSinceStartup;step=0;log="";maxSpeed=0;}}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);log+="PASS "+message+"\n";}
 static void Input(ArcadeCarController car,Vector2 value)=>typeof(ArcadeCarController).GetField("moveInput",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(car,value);
 static void Tick()
 {
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isPaused||began==0)return;
  double t=EditorApplication.timeSinceStartup-began;
  try{
   var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();if(!car){if(t>20)throw new Exception("Player unavailable");return;}
   var rig=car.GetComponentInChildren<FormulaVehicleVisual>();if(!rig)throw new Exception("Gameplay visual missing");
   maxSpeed=Mathf.Max(maxSpeed,Mathf.Abs(car.CurrentSpeedMps));
   if(step==0&&t>.7){
    var run=UnityEngine.Object.FindFirstObjectByType<RunController>();run.BeginRun((OrderJob)null);
    start=car.transform.position;wheelStart=rig.wheels[0].transform.localRotation;
    Check(rig.parts.Where(p=>!p.driver).All(p=>p.renderer.sharedMaterial==rig.profile.worn),"new run starts worn");
    Check(rig.GetComponentsInChildren<Collider>().Length==0,"visual adds no collider");
    Input(car,new Vector2(0,.6f));step=1;
   }
   if(step==1&&t>2.7){
    Check(maxSpeed>.2f,"controller accelerates");Check(Vector3.Distance(start,car.transform.position)>.15f,"vehicle moves");
    Check(Quaternion.Angle(wheelStart,rig.wheels[0].transform.localRotation)>1,"wheel rotation follows travel");
    ScreenCapture.CaptureScreenshot(Output+"/10_ingame_worn.png");
    Input(car,new Vector2(.3f,.4f));step=2;
   }
   if(step==2&&t>3.7){
    Check(car.SteerInput>.1f,"steering input reaches visual rig");Input(car,Vector2.zero);
    var restore=UnityEngine.Object.FindFirstObjectByType<F1RestoreSystem>();var up=UpgradeSystem.Instance;
    up.AddReward(100000);var paint=restore.Catalog.parts.First(p=>p.id==F1PartId.Paint);
    Check(restore.TryRestore(paint),"paint purchase succeeds");
    Check(rig.parts.Where(p=>p.restoredBy==F1PartId.Paint&&!p.cargo&&!p.driver).All(p=>p.renderer.sharedMaterial==rig.profile.restored),"paint restores body");
    Check(rig.parts.Where(p=>p.restoredBy==F1PartId.Tires||p.cargo).All(p=>p.renderer.sharedMaterial==rig.profile.worn),"paint preserves tires and cargo");
    int money=up.Wallet;Check(!restore.TryRestore(paint)&&up.Wallet==money,"duplicate restoration does not charge");
    foreach(var p in restore.Catalog.parts.Where(p=>p.id!=F1PartId.Paint))Check(restore.TryRestore(p),"restore "+p.id);
    Check(rig.parts.Where(p=>!p.cargo).All(p=>p.renderer.sharedMaterial==rig.profile.restored),"all restored groups use clean material");
    var def=ScriptableObject.CreateInstance<UpgradeDef>();def.effect=UpgradeEffect.ReinforcedTrunk;def.cost=1;Check(up.TryPurchase(def),"reinforced trunk purchase");UnityEngine.Object.Destroy(def);
    Check(rig.parts.Where(p=>p.cargo).All(p=>p.renderer.sharedMaterial==rig.profile.serviced),"cargo upgrade changes bag only");
    rig.enabled=false;rig.enabled=true;
    Check(rig.parts.Where(p=>!p.cargo).All(p=>p.renderer.sharedMaterial==rig.profile.restored),"re-enable resynchronizes owned upgrades");
    step=3;
   }
   if(step==3&&t>4.7){ScreenCapture.CaptureScreenshot(Output+"/11_ingame_restored.png");log+="MaxSpeedMps="+maxSpeed+"\nDistance="+Vector3.Distance(start,car.transform.position)+"\n";step=4;}
   if(step==4&&t>6){File.WriteAllText(Output+"/gameplay-smoke.txt",log+"SUCCESS\n");SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
   if(t>25)throw new Exception("Probe timed out");
  }catch(Exception e){File.WriteAllText(Output+"/gameplay-smoke.txt",log+"FAIL "+e);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;Debug.LogError(e);}
 }
}
