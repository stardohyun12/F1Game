var t=System.Type.GetType("LowResRenderTest, PitCrew"); if(t==null) return "type missing (not compiled yet)";
var cam=Camera.main; var c=cam.GetComponent(t) as Behaviour; if(c==null) c=cam.gameObject.AddComponent(t) as Behaviour;
c.enabled=false; UnityEditor.EditorUtility.SetDirty(cam.gameObject);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cam.gameObject.scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(cam.gameObject.scene);
return "added to "+cam.name+" enabled="+c.enabled+" scene="+cam.gameObject.scene.name;
