// 과제 0535: 새 정비소 fbx 의 UV0 은 -3..3(반복용), 아틀라스 UV 는 UV1(0..1) → Garage_Albedo 서브메시 정점만 UV0 ← UV1 인 사본 메시. 원본 fbx 그대로
var mf = GameObject.Find("PV_Garage_q2").GetComponent<MeshFilter>();
var src = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/PreviewCand/garage/garage_v5_combo.fbx").OfType<Mesh>().First();
var m = UnityEngine.Object.Instantiate(src); m.name = src.name + "_pvuv";
var u0 = new List<Vector2>(); var u1 = new List<Vector2>(); m.GetUVs(0, u0); m.GetUVs(1, u1);
var sb = new System.Text.StringBuilder();
var used = new HashSet<int>(m.GetTriangles(0)); int ch = 0;
for (int s = 0; s < m.subMeshCount; s++) { var vs = new HashSet<int>(m.GetTriangles(s)); int out0 = vs.Count(i => u0[i].x < 0 || u0[i].x > 1 || u0[i].y < 0 || u0[i].y > 1); sb.Append("sub" + s + " verts " + vs.Count + " uv0 out " + out0 + "; "); }
foreach (var i in used) { u0[i] = u1[i]; ch++; }
m.SetUVs(0, u0);
string p = "Assets/Art/PreviewCand/garage/garage_v5_combo_pvuv.asset"; AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(m, p);
mf.sharedMesh = m; EditorUtility.SetDirty(mf);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(mf.gameObject.scene);
return sb + " changed " + ch;
