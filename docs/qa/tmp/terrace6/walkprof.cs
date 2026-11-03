// terrace6: 집 계단 발치 근처 보도 단면 — x 192..204 0.5 m 마다 보도(Walkways) z 범위·가운데 높이, 그 사이 기울기
var wk=GameObject.Find("TownV6_Gray/TerraceBlock/Walkways").GetComponent<Collider>();var sb=new System.Text.StringBuilder();float prevY=float.NaN;
for(float x=192;x<=204.01f;x+=.5f){float z0=float.NaN,z1=float.NaN;for(float z=-74;z>=-82;z-=.05f){RaycastHit h;if(wk.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out h,1000)&&h.normal.y>.9f){if(float.IsNaN(z0))z0=z;z1=z;}else if(!float.IsNaN(z0)&&z<z0-.5f)break;}
 float zc=(z0+z1)/2;RaycastHit hc;float y=wk.Raycast(new Ray(new Vector3(x,500,zc),Vector3.down),out hc,1000)?hc.point.y:float.NaN;
 sb.Append($"x{x:F1} z{z0:F2}..{z1:F2} w{(z0-z1):F2} y{y:F3} dy/0.5m {(y-prevY)/.5f:F3}\n");prevY=y;}
return sb.ToString();
