// Unity 가 Game 뷰 경고에 쓰는 판정(EditorGUIUtility.IsDisplayReferencedByCameras) 그대로 호출.
var m=typeof(UnityEditor.EditorGUIUtility).GetMethod("IsDisplayReferencedByCameras",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
bool d0=(bool)m.Invoke(null,new object[]{0});
var lr=UnityEngine.Object.FindFirstObjectByType<LowResRenderTest>();
var sb=new System.Text.StringBuilder();
foreach(var c in Camera.allCameras) sb.Append(c.name+"(tt="+(c.targetTexture!=null)+",mask="+c.cullingMask+") ");
return "display0Referenced="+d0+" warningShown="+(!d0)+" lowres="+(lr!=null&&lr.enabled)+" cams: "+sb+" frame="+Time.frameCount;
