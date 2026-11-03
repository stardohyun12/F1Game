// TV6_SeaDusk(씬 전용) 파 마루 선: _Crest = (마루 높이, 굵기, 보이는 거리 m, 섞음). 7판 값 (0.72, 1, 70, 0.45)
var m = SunsetLook.Find().seaMaterial; var o = m.GetVector("_Crest");
m.SetVector("_Crest", new Vector4(0.72f, 1f, __D__f, __W__f));
UnityEditor.EditorUtility.SetDirty(m);
return "crest " + o + " -> " + m.GetVector("_Crest");
