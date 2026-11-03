// Play 미리보기 조정(저장 안 됨 — 확정값은 코드 기본값 + 에디트 모드 setval.cs)
var fg = UnityEngine.Object.FindFirstObjectByType<DuskForeground>();
fg.paint = new[] { new Color(0.36f, 0.33f, 0.30f), new Color(0.40f, 0.36f, 0.31f), new Color(0.33f, 0.32f, 0.31f), new Color(0.38f, 0.35f, 0.33f) };
fg.bucketAt = new Vector3(__BX__f, __BY__f, 0f);
fg.boxAt = new Vector3(__CX__f, __CY__f, 20f);
fg.Rebuild();
var look = SunsetLook.Find(); look.screenGlow = new Vector4(0.10f, 0.22f, 0.08f, 0.035f);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>();
spot.rodTip = new Vector3(__TX__f, __TY__f, __TZ__f);
return "ok";
