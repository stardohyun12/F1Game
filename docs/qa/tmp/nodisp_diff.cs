// SessionState "nd" 의 LABEL: LABEL-A.png vs LABEL-B.png 픽셀 차이(채널차 > 2/255 인 픽셀 %) + 차이 영역 bbox.
string label = UnityEditor.SessionState.GetString("nd", "x|x").Split('|')[1];
string dir = "../docs/qa/evidence/no-display-fix/";
var a = new Texture2D(2, 2); var b = new Texture2D(2, 2);
a.LoadImage(System.IO.File.ReadAllBytes(dir + label + "-A.png")); b.LoadImage(System.IO.File.ReadAllBytes(dir + label + "-B.png"));
if (a.width != b.width || a.height != b.height) return "size mismatch";
var pa = a.GetPixels32(); var pb = b.GetPixels32(); int diff = 0; int maxd = 0; int x0 = 99999, y0 = 99999, x1 = -1, y1 = -1; int W = a.width;
for (int i = 0; i < pa.Length; i++) { int d = Mathf.Max(Mathf.Abs(pa[i].r - pb[i].r), Mathf.Abs(pa[i].g - pb[i].g), Mathf.Abs(pa[i].b - pb[i].b)); if (d > 2) { diff++; int x = i % W, y = i / W; if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; } if (d > maxd) maxd = d; }
return label + " " + a.width + "x" + a.height + " diffPixels=" + diff + " (" + (100.0 * diff / pa.Length).ToString("F3") + "%) maxChannelDiff=" + maxd + " bbox=(" + x0 + "," + y0 + ")-(" + x1 + "," + y1 + ")";
