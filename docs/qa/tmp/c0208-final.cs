// 기본 차 A, 운전자·가방 끔. C 는 Apply("C") 한 번 돌려 임포트·틴트 복구 뒤 A 로.
CarVisualSwitcher.ShowDriver = false; CarVisualSwitcher.ShowCargo = false;
CarVisualSwitcher.Apply("C");
var r = CarVisualSwitcher.Apply("A");
var sb = new System.Text.StringBuilder(r + "\n");
foreach (var w in new[] { "A", "C" })
{
  var t = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/BayLoop/Car/" + w + "_Atlas.png");
  var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/BayLoop/Car/Candidate_" + w + ".mat");
  sb.Append(w + " " + t.format + " " + t.filterMode + " mips=" + t.mipmapCount + " tint=" + m.GetColor("_Tint") + "\n");
}
sb.Append("scene dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
return sb.ToString();
