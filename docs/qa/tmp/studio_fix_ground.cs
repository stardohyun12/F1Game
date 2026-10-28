// artlive 1534: 스튜디오 땅 고침(씬 다시 안 만들고 사본만) — uv v 0.94→0.82(어두운 빈칸 피함), ST_ground _Sat 2.6→1
var me = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scenes/Studio_Building_Mats/ST_ground_mesh.asset");
var uv = me.uv; for (int i = 0; i < uv.Length; i++) if (uv[i].y > 0.5f) uv[i].y = 0.82f; me.uv = uv; EditorUtility.SetDirty(me);
var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Studio_Building_Mats/ST_ground.mat"); m.SetFloat("_Sat", 1f); EditorUtility.SetDirty(m);
AssetDatabase.SaveAssets();
return "ok uv " + string.Join(",", me.uv.Take(4)) + " sat " + m.GetFloat("_Sat");
