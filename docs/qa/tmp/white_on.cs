// 조명 단 수 측정: 방 렌더러의 텍스처를 흰색으로(MaterialPropertyBlock, 에셋 안 바뀜) → 빛만 남은 화면
var root = GameObject.Find("TownV6_Gray/D198_Buildings/house_home");
int n = 0;
foreach (var r in root.GetComponentsInChildren<Renderer>()) {
  var mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(mpb);
  if (true) mpb.SetTexture("_BaseMap", Texture2D.whiteTexture); else mpb.Clear();
  r.SetPropertyBlock(mpb); n++;
}
return "renderers " + n;
