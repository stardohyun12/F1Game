// SessionState c.mat = A|C, c.tint = "r,g,b", c.face = float → Candidate_X.mat 에 쓰고 c.tag 설정
string w = UnityEditor.SessionState.GetString("c.mat", "A");
var v = UnityEditor.SessionState.GetString("c.tint", "1,1,1").Split(',').Select(float.Parse).ToArray();
float face = UnityEditor.SessionState.GetFloat("c.face", 0.65f);
var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/BayLoop/Car/Candidate_" + w + ".mat");
m.SetColor("_Tint", new Color(v[0], v[1], v[2], 1f)); m.SetFloat("_FaceShade", face);
UnityEditor.EditorUtility.SetDirty(m); UnityEditor.AssetDatabase.SaveAssetIfDirty(m);
UnityEditor.SessionState.SetString("c.tag", w + "_t" + v[0].ToString("F2") + "_f" + face.ToString("F2"));
return "mat " + w + " tint=" + m.GetColor("_Tint") + " face=" + face;
