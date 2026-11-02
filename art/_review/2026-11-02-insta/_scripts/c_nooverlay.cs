// 검증: overlay(역광 테 머티리얼) 없이 같은 장면을 찍어 3x3 블록이 100% 균일한지 — 비균일의 원인이 overlay 인지 확인.
var o = lr.overlay;
Save(Grab(360, 450, 3, null, null, 0f), "test_with_overlay.png");
lr.overlay = null;
Save(Grab(360, 450, 3, null, null, 0f), "test_no_overlay.png");
lr.overlay = o;
return "overlay=" + (o != null ? o.name + " shader=" + o.shader.name : "null");
