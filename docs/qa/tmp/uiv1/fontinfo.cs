var f11 = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
var f9 = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri9.ttf");
var sb = new System.Text.StringBuilder();
foreach (var f in new[] { f11, f9 }) {
  sb.Append(f.name + " fontSize=" + f.fontSize + " lineH=" + f.lineHeight + " asc=" + f.ascent + "\n");
  foreach (int s in new[] { 10, 11, 12, 13, 14, 15, 16, 20, 24 }) {
    f.RequestCharactersInTexture("가A", s, FontStyle.Normal);
    CharacterInfo a, g; f.GetCharacterInfo('가', out g, s); f.GetCharacterInfo('A', out a, s);
    sb.Append(" s" + s + " 가 adv=" + g.advance + " x" + g.minX + ".." + g.maxX + " y" + g.minY + ".." + g.maxY + " | A adv=" + a.advance + " x" + a.minX + ".." + a.maxX + " y" + a.minY + ".." + a.maxY + "\n");
  }
}
return sb.ToString();
