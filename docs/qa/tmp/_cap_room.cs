// 360 프레임 측정 — after_room 을 sed 로 바꿔 씀. LowResRenderTest.Present 로 도현이 보는 화면 그대로.
var lrt = UnityEngine.Object.FindFirstObjectByType<LowResRenderTest>();
var sz = lrt.RenderSize;
var dst = RenderTexture.GetTemporary(sz.x, sz.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
lrt.Present(dst);
var prev = RenderTexture.active; RenderTexture.active = dst;
var tex = new Texture2D(sz.x, sz.y, TextureFormat.RGB24, false);
tex.ReadPixels(new Rect(0, 0, sz.x, sz.y), 0, 0); tex.Apply();
RenderTexture.active = prev; RenderTexture.ReleaseTemporary(dst);
System.IO.Directory.CreateDirectory("../docs/qa/evidence/art-indoor");
System.IO.File.WriteAllBytes("../docs/qa/evidence/art-indoor/after_room.png", tex.EncodeToPNG());
var px = tex.GetPixels32(); int W = sz.x, H = sz.y, N = px.Length;
var hist = new System.Collections.Generic.Dictionary<int,int>();
var lumH = new int[256];
for (int i = 0; i < N; i++) { int k = (px[i].r << 16) | (px[i].g << 8) | px[i].b; hist[k] = hist.TryGetValue(k, out var v) ? v + 1 : 1; lumH[(int)(0.299f*px[i].r+0.587f*px[i].g+0.114f*px[i].b)]++; }
var counts = new System.Collections.Generic.List<int>(hist.Values); counts.Sort(); counts.Reverse();
int acc = 0, c95 = 0; foreach (var c in counts) { acc += c; c95++; if (acc >= N * 0.95) break; }
var lc = new System.Collections.Generic.List<int>(lumH); lc.Sort(); lc.Reverse(); acc = 0; int l95 = 0; foreach (var c in lc) { acc += c; l95++; if (acc >= N * 0.95) break; }
// 같은 색 가로 줄 평균 길이(px) — 길면 색면(도트 얼룩), 짧으면 텍셀마다 색이 바뀜(사진 결)
long runs = 0; for (int y = 0; y < H; y++) { int r = 1; for (int x = 1; x < W; x++) { var a = px[y*W+x]; var b = px[y*W+x-1]; if (a.r != b.r || a.g != b.g || a.b != b.b) r++; } runs += r; }
float runLen = (float)N / runs;
// 디더 비율: 좌우(또는 위아래) 이웃이 서로 같고 가운데만 다른 픽셀(체커 교차)
int dith = 0, iso = 0;
System.Func<Color32, Color32, bool> eq = (a, b) => a.r == b.r && a.g == b.g && a.b == b.b;
System.Func<Color32, Color32, int> dl = (a, b) => System.Math.Abs((a.r*299+a.g*587+a.b*114) - (b.r*299+b.g*587+b.b*114)) / 1000;
for (int y = 1; y < H - 1; y++) for (int x = 1; x < W - 1; x++) {
  var c = px[y*W+x]; var l = px[y*W+x-1]; var r = px[y*W+x+1]; var u = px[(y+1)*W+x]; var d = px[(y-1)*W+x];
  if ((eq(l, r) && !eq(c, l)) || (eq(u, d) && !eq(c, u))) dith++;
  // 외톨이 점: 네 이웃과 밝기 차가 모두 3 이상이고 네 이웃끼리는 비슷(±6) — 사진 점무늬
  if (dl(c,l) >= 3 && dl(c,r) >= 3 && dl(c,u) >= 3 && dl(c,d) >= 3 && dl(l,r) <= 6 && dl(u,d) <= 6) iso++;
}
int inner = (W - 2) * (H - 2);
return "{\"name\":\"after_room\",\"w\":" + W + ",\"h\":" + H + ",\"colors\":" + hist.Count + ",\"colors95\":" + c95 + ",\"lum95\":" + l95 + ",\"runLen\":" + runLen.ToString("F2") + ",\"dither\":" + ((float)dith / inner).ToString("F3") + ",\"speckle\":" + ((float)iso / inner).ToString("F3") + "}";
