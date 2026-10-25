var sb = new System.Text.StringBuilder();
foreach (var w in new[]{"A","C"}) {
  var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/BayLoop/Car/" + w + ".fbx");
  foreach (var o in objs) { var m = o as Mesh; if (m == null) continue;
    var uv = new List<Vector2>(); m.GetUVs(0, uv); var uv1 = new List<Vector2>(); m.GetUVs(1, uv1);
    float mnx=9,mny=9,mxx=-9,mxy=-9; foreach (var u in uv){mnx=Mathf.Min(mnx,u.x);mny=Mathf.Min(mny,u.y);mxx=Mathf.Max(mxx,u.x);mxy=Mathf.Max(mxy,u.y);}
    var cells = new HashSet<string>(); foreach (var u in uv) cells.Add(((int)(u.x*4)).ToString()+((int)(u.y*4)));
    sb.Append(w+"/"+m.name+" v"+m.vertexCount+" uv0="+uv.Count+" uv1="+uv1.Count+" x["+mnx.ToString("F3")+","+mxx.ToString("F3")+"] y["+mny.ToString("F3")+","+mxy.ToString("F3")+"] cells="+string.Join(",",cells)+" col="+m.colors.Length+"\n");
  }
}
var t = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/BayLoop/Car/A_Atlas.png");
sb.Append("tex "+t.width+"x"+t.height+" fmt="+t.format+" filter="+t.filterMode+" mips="+t.mipmapCount);
return sb.ToString();
