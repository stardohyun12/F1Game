var sv = UnityEditor.SceneView.lastActiveSceneView;
var g = UnityEngine.GameObject.Find("TownV6_Gray/D198_Buildings/D198_garage");
UnityEditor.Selection.activeGameObject = g;
sv.LookAt(g.transform.position + UnityEngine.Vector3.up * 3, UnityEngine.Quaternion.Euler(25, 150, 0), 18f, false, true);
sv.Repaint();
return sv.camera.transform.position.ToString("F1");
