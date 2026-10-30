var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/Map_TownV6_Mats/TV6_Terrain.mat");
var t=m.GetTexture("_MainTex") as Texture2D;
string s="shader="+m.shader.name+" tint="+m.GetColor("_Tint")+" tex="+(t?t.name+" "+t.width+"x"+t.height+" readable="+t.isReadable:"null")+" st="+m.GetTextureScale("_MainTex")+" faceShade="+m.GetFloat("_FaceShade");
if(t!=null){var rt=RenderTexture.GetTemporary(t.width,t.height);Graphics.Blit(t,rt);var prev=RenderTexture.active;RenderTexture.active=rt;var cp=new Texture2D(t.width,t.height);cp.ReadPixels(new Rect(0,0,t.width,t.height),0,0);cp.Apply();RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);
 var px=cp.GetPixels();float r=0,g=0,b=0;foreach(var c in px){r+=c.r;g+=c.g;b+=c.b;}r/=px.Length;g/=px.Length;b/=px.Length;s+=" avg="+r.ToString("F3")+","+g.ToString("F3")+","+b.ToString("F3");UnityEngine.Object.DestroyImmediate(cp);}
var st=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("D:/x");
return s;
