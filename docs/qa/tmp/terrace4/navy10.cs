// 남색 판 10: 열린 씬 목록 + 모든 GameObject 컴포넌트 종류 수(렌더러 외에 그릴 수 있는 것 찾기)
var sb=new System.Text.StringBuilder();
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);sb.AppendLine("scene "+s.path+" loaded="+s.isLoaded);}
var kinds=new System.Collections.Generic.Dictionary<string,int>();
foreach(var c in Resources.FindObjectsOfTypeAll<Component>()){if(!c||UnityEditor.EditorUtility.IsPersistent(c))continue;var n=c.GetType().FullName;kinds.TryGetValue(n,out int k);kinds[n]=k+1;}
foreach(var kv in kinds.OrderBy(x=>x.Key))sb.AppendLine(kv.Value+" "+kv.Key);
return sb.ToString();
