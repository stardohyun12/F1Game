// Play: 도트 UI 캔버스에 Galmuri11/9 를 크기별로 찍어 본다 → 한 장.
var cv = GameObject.Find("DotScreenUI_Canvas");
if (cv == null) return "no canvas";
var f11 = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
var f9 = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri9.ttf");
var old = GameObject.Find("FontTest"); if (old != null) UnityEngine.Object.Destroy(old);
var holder = new GameObject("FontTest", typeof(RectTransform)); holder.transform.SetParent(cv.transform, false);
var hr = (RectTransform)holder.transform; hr.anchorMin = hr.anchorMax = hr.pivot = Vector2.zero; hr.anchoredPosition = Vector2.zero;
int y = 340; var s = "";
foreach (var f in new[] { f11, f9 })
  for (int size = 9; size <= 18; size++) {
    var bg = new GameObject("bg", typeof(RectTransform), typeof(UnityEngine.UI.Image)); bg.transform.SetParent(holder.transform, false);
    var br = (RectTransform)bg.transform; br.anchorMin = br.anchorMax = br.pivot = Vector2.zero; br.anchoredPosition = new Vector2(4, y - size - 4); br.sizeDelta = new Vector2(300, size + 4);
    bg.GetComponent<UnityEngine.UI.Image>().color = new Color(0.9f, 0.85f, 0.75f);
    var go = new GameObject(f.name + "_" + size, typeof(RectTransform)); go.transform.SetParent(holder.transform, false);
    var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; r.anchoredPosition = new Vector2(6, y - size - 3); r.sizeDelta = new Vector2(400, size + 2);
    var t = go.AddComponent<UnityEngine.UI.Text>(); t.font = f; t.fontSize = size; t.color = Color.black; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
    go.AddComponent<PixelSnapText>().root = (RectTransform)cv.transform;
    t.text = size +" 왔냐. 오늘부터 배달이다 ABC"; t.alignment = TextAnchor.LowerLeft;
    y -= size + 5;
  }
f11.material.mainTexture.filterMode = FilterMode.Point; f9.material.mainTexture.filterMode = FilterMode.Point;
return PresentGrab.Begin("D:/Github/F1Game/docs/qa/tmp/uiv1/z", "fontsize", 1, 0f, 0.3f);
