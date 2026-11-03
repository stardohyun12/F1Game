// 에디트 모드: Galmuri11/9 래스터 모드를 Smooth 로(정수 em 에서 외곽선이 픽셀 격자 위 → 커버리지 0/255).
var s = "";
foreach (var p in new[] { "Assets/UI/Fonts/Galmuri/Galmuri11.ttf", "Assets/UI/Fonts/Galmuri/Galmuri9.ttf" }) {
  var imp = (UnityEditor.TrueTypeFontImporter)UnityEditor.AssetImporter.GetAtPath(p);
  imp.fontRenderingMode = FontRenderingMode.Smooth;
  imp.SaveAndReimport();
  s += p + " " + imp.fontRenderingMode + " asc=" + imp.ascentCalculationMode + "; ";
}
return s;
