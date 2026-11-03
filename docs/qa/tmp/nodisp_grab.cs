// Game 뷰 창 자체를 화면에서 읽는다(IMGUI 경고 상자 포함). 라벨은 첫 줄.
string label="LABEL";
var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
UnityEditor.EditorWindow w=null; foreach(var o in Resources.FindObjectsOfTypeAll(t)) w=(UnityEditor.EditorWindow)o;
w.Repaint();
float ppp=UnityEditor.EditorGUIUtility.pixelsPerPoint;
var r=w.position; int x=(int)(r.x*ppp), y=(int)((r.y+22)*ppp), W=(int)(r.width*ppp), H=(int)((r.height-22)*ppp);
var cols=UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(new Vector2(x,y),W,H);
var tex=new Texture2D(W,H,TextureFormat.RGBA32,false); tex.SetPixels(cols); tex.Apply();
System.IO.Directory.CreateDirectory("../docs/qa/evidence/no-display-fix");
System.IO.File.WriteAllBytes("../docs/qa/evidence/no-display-fix/"+label+".png", tex.EncodeToPNG());
UnityEngine.Object.Destroy(tex);
return "ppp="+ppp+" rect="+x+","+y+","+W+","+H+" frame="+Time.frameCount;
