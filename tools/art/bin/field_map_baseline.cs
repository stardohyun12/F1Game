var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new System.Exception("Scene has pre-existing unsaved edits; stop");
var sv=UnityEditor.SceneView.lastActiveSceneView;var src=sv!=null?sv.camera:Camera.main;
if(!src)throw new System.Exception("No source camera");
var go=new GameObject("_FieldMapBaseline"){hideFlags=HideFlags.HideAndDontSave};
try {var cam=go.AddComponent<Camera>();cam.CopyFrom(src);cam.enabled=false;cam.fieldOfView=60;
var pos=new Vector3(196.881f,53.714f,-81.658f);var tgt=new Vector3(197.27f,-95.469f,39.031f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
bool ok=PitCrewPlan.PlanIO.RenderPng(cam,"D:/Github/F1Game/art/_review/2026-10-29-field_patch/map_baseline.png",1280,611,null);
return "captured="+ok+" dirty="+scene.isDirty+" camera="+src.name;
}finally {UnityEngine.Object.DestroyImmediate(go);}
