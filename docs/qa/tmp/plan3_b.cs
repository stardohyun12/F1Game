var w = PitCrewPlan.PlanInputPopup.Current;
if (w == null) return "no popup";
w.SetText("한글 테스트 가나다");
w.Repaint();
// 팝업 캡처(증거)
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var parent = typeof(UnityEditor.EditorWindow).GetField("m_Parent", bf).GetValue(w);
parent.GetType().GetMethod("RepaintImmediately", bf, null, System.Type.EmptyTypes, null)?.Invoke(parent, null);
float ppp = UnityEditor.EditorGUIUtility.pixelsPerPoint;
var pos = w.position; int W = (int)(pos.width * ppp), H = (int)(pos.height * ppp);
var rt = new UnityEngine.RenderTexture(W, H, 0, UnityEngine.RenderTextureFormat.ARGB32);
parent.GetType().GetMethod("GrabPixels", bf).Invoke(parent, new object[] { rt, new UnityEngine.Rect(0, 0, W, H) });
var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
var tex = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0); tex.Apply(); UnityEngine.RenderTexture.active = prev;
var px = tex.GetPixels(); var fl = new UnityEngine.Color[px.Length];
for (int y = 0; y < H; y++) System.Array.Copy(px, y * W, fl, (H - 1 - y) * W, W);
tex.SetPixels(fl); tex.Apply();
System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/plan-window/popup.png", tex.EncodeToPNG()); rt.Release();
var text = w.Text;
// 저장 경로: 팝업 Enter 와 같은 함수
PitCrewPlan.PlanMode.Submit(text);
var jl = PitCrewPlan.PlanIO.JsonlPath("Map_TownV6");
var bytes = System.IO.File.ReadAllBytes(jl);
var want = System.Text.Encoding.UTF8.GetBytes("\"comment\":\"한글 테스트 가나다\"");
int at = -1;
for (int i = 0; i + want.Length <= bytes.Length && at < 0; i++) { int k = 0; while (k < want.Length && bytes[i + k] == want[k]) k++; if (k == want.Length) at = i; }
var cs = PitCrewPlan.PlanIO.ReadComments("Map_TownV6"); var last = cs[cs.Count - 1];
return "text=[" + text + "] utf8 bytes found at=" + at + " readback=[" + last.comment + "] equal=" + (last.comment == "한글 테스트 가나다") + " id=" + last.id + " shot=" + last.screenshot + " popupClosed=" + (PitCrewPlan.PlanInputPopup.Current == null);
