// 남색 판 8: c2 자리에서 (a) 전부 켬 (b) 렌더러 전부 끔 (c) 렌더러 끔+스카이박스 없음 세 장 저장
var go=new GameObject("_navy_cam"){hideFlags=HideFlags.HideAndDontSave};
var all=new System.Collections.Generic.List<Renderer>();foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.enabled&&r.gameObject.activeInHierarchy)all.Add(r);
var sky=RenderSettings.skybox;string o="D:/Github/F1Game/docs/qa/tmp/terrace4/";var sb=new System.Text.StringBuilder();
try{
var cam=go.AddComponent<Camera>();cam.CopyFrom(UnityEditor.SceneView.lastActiveSceneView.camera);cam.enabled=false;cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=2000;
var pos=new Vector3(188.00f,19.20f,-82.50f);var tgt=new Vector3(195.35f,23.49f,-70.68f);go.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(tgt-pos));
PitCrewPlan.PlanIO.RenderPng(cam,o+"navy_a_all.png",640,360,null);
foreach(var r in all)r.forceRenderingOff=true;PitCrewPlan.PlanIO.RenderPng(cam,o+"navy_b_norend.png",640,360,null);
RenderSettings.skybox=null;PitCrewPlan.PlanIO.RenderPng(cam,o+"navy_c_nosky.png",640,360,null);
sb.AppendLine("cam clear="+cam.clearFlags+" bg="+cam.backgroundColor);
}finally{RenderSettings.skybox=sky;foreach(var r in all)r.forceRenderingOff=false;UnityEngine.Object.DestroyImmediate(go);}
sb.AppendLine("dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
return sb.ToString();
