// 글꼴 아틀라스에서 '가','왔' 를 크기 12 로 꺼내 ASCII 로 — 래스터가 깨졌나, 찍기(샘플링)가 깨졌나 가르기.
var f = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
int s = 12; string chars = "가왔배";
f.RequestCharactersInTexture(chars, s, FontStyle.Normal);
var src = f.material.mainTexture;
var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
Graphics.Blit(src, rt);
var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
var a0 = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); tex.Apply(); RenderTexture.active = a0;
RenderTexture.ReleaseTemporary(rt);
var sb = new System.Text.StringBuilder("atlas " + src.width + "x" + src.height + " fmt=" + ((Texture2D)src).format + "\n");
foreach (var ch in chars) {
  CharacterInfo ci; f.GetCharacterInfo(ch, out ci, s);
  sb.Append(ch + " uvBL=" + ci.uvBottomLeft.x * src.width + "," + ci.uvBottomLeft.y * src.height + " uvTR=" + ci.uvTopRight.x * src.width + "," + ci.uvTopRight.y * src.height + " flipped=" + (ci.uvTopLeft.x != ci.uvBottomLeft.x) + " w=" + ci.glyphWidth + " h=" + ci.glyphHeight + "\n");
  int x0 = Mathf.RoundToInt(Mathf.Min(ci.uvBottomLeft.x, ci.uvTopRight.x) * src.width), x1 = Mathf.RoundToInt(Mathf.Max(ci.uvBottomLeft.x, ci.uvTopRight.x) * src.width);
  int y0 = Mathf.RoundToInt(Mathf.Min(ci.uvBottomLeft.y, ci.uvTopRight.y) * src.height), y1 = Mathf.RoundToInt(Mathf.Max(ci.uvBottomLeft.y, ci.uvTopRight.y) * src.height);
  for (int y = y1 - 1; y >= y0; y--) {
    for (int x = x0; x < x1; x++) { float v = tex.GetPixel(x, y).a; sb.Append(v > 0.9f ? '#' : v > 0.1f ? '+' : '.'); }
    sb.Append('\n');
  }
}
UnityEngine.Object.Destroy(tex);
return sb.ToString();
