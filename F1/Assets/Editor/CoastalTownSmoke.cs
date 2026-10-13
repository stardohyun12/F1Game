using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class CoastalTownSmoke
{
 const string Key="CoastalTownSmoke.Pending";
 static double since;
 static int phase;
 static string log="";
 static Vector3 start;
 static OrderJob testJob;
 static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/source/coastal-town"));
 static CoastalTownSmoke(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){since=EditorApplication.timeSinceStartup;phase=0;log="";}};}
 public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Start from edit mode");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 static void Input(ArcadeCarController car,Vector2 value)=>typeof(ArcadeCarController).GetField("moveInput",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(car,value);
 static void Place(ArcadeCarController car,Vector3 p,float yaw){var rb=car.GetComponent<Rigidbody>();rb.position=p;rb.rotation=Quaternion.Euler(0,yaw,0);rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;start=p;Input(car,new Vector2(0,.45f));}
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);log+="PASS "+label+"\n";}
 static void Sample(ArcadeCarController car,string name){Check(Vector3.Distance(start,car.transform.position)>1,name+" movement");float bottom=car.GetComponent<BoxCollider>().bounds.min.y;Check(bottom>-.06f&&bottom<.15f,name+" grounded bottom="+bottom);ScreenCapture.CaptureScreenshot(Output+"/"+name+".png");}
 static void Tick()
 {
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||since==0)return;
  double t=EditorApplication.timeSinceStartup-since;
  try{
   var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();var run=UnityEngine.Object.FindFirstObjectByType<RunController>();if(!car||!run)return;
   if(phase==0&&t>.8){run.BeginRun((OrderJob)null);Place(car,new Vector3(66,.25f,48),0);phase=1;}
   if(phase==1&&t>3.3){Sample(car,"01_ingame_neighborhood");Input(car,Vector2.zero);phase=2;}
   if(phase==2&&t>3.7){Place(car,new Vector3(0,.25f,12),0);phase=3;}
   if(phase==3&&t>6.2){Sample(car,"02_ingame_outer_road");Input(car,Vector2.zero);phase=4;}
   if(phase==4&&t>6.6){Place(car,new Vector3(60,.25f,96),0);phase=5;}
   if(phase==5&&t>9.1){Sample(car,"03_ingame_harbor");Input(car,Vector2.zero);phase=6;}
   if(phase==6&&t>9.5){
    testJob=ScriptableObject.CreateInstance<OrderJob>();testJob.displayName="Town delivery probe";testJob.destinationLabels.Add("FactoryA");
    // 이전 phase 의 BeginRun(null) 이 route 를 points[0] 으로 남겨 HasPlannedRoute=true → 라벨이 무시되던 버그.
    // 실제 게임 흐름(RoutePlanScreen)처럼 라벨로 경로를 명시한다.
    var mgr=UnityEngine.Object.FindFirstObjectByType<DeliveryManager>();var target=mgr?mgr.FindByLabel("FactoryA"):null;Check(target!=null,"FactoryA delivery point resolves by label");
    mgr.ActivateRoute(new System.Collections.Generic.List<DeliveryPoint>{target});run.BeginRun(testJob);
    Place(car,new Vector3(48,.25f,0),90);phase=7;
   }
   if(phase==7&&t>13){Check(run.CompletedCount>=1,"delivery trigger completes order after driving approach");ScreenCapture.CaptureScreenshot(Output+"/04_delivery_complete.png");phase=8;}
   if(phase==8&&t>14){File.WriteAllText(Output+"/play-verification.txt",log+"SUCCESS\n");SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
   if(t>25)throw new Exception("Timeout");
  }catch(Exception e){File.WriteAllText(Output+"/play-verification.txt",log+"FAIL "+e);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;Debug.LogError(e);}
 }
}
