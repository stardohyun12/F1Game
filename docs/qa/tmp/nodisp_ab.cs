// A/B: 빈 카메라 있음/없음에서 같은 판정. 끝나면 되돌림.
var m=typeof(UnityEditor.EditorGUIUtility).GetMethod("IsDisplayReferencedByCameras",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
var dc=GameObject.Find("LowResRenderTest_DisplayCam").GetComponent<Camera>();
bool with=(bool)m.Invoke(null,new object[]{0});
dc.enabled=false; bool without=(bool)m.Invoke(null,new object[]{0});
dc.enabled=true; dc.cullingMask=0; bool again=(bool)m.Invoke(null,new object[]{0});
return "withDisplayCam="+with+" disabled="+without+" reenabled="+again;
