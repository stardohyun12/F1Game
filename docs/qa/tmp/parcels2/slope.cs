// 필지 자리 경사: 중심 둘레 8×8 m 를 1 m 간격으로 재서 평면 기울기(%)
System.Func<float,float,float> gy = (x,z) => { var hs = UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(x,400,z), UnityEngine.Vector3.down, 800); float b = float.NaN; foreach (var h in hs) { var rn = h.collider.transform.root.name; if (rn=="Roadside"||rn=="Parcels_Plan"||rn=="OnFoot_HomeGarage") continue; if (float.IsNaN(b)||h.point.y>b) b=h.point.y; } return b; };
var pts = new float[,] { {167f,-58.4f}, {167.65f,-49.55f}, {164.58f,-43.04f}, {157.58f,-38.08f} };
string r = "";
for (int i = 0; i < pts.GetLength(0); i++)
{
  float cx = pts[i,0], cz = pts[i,1]; double sx=0,sz=0,sy=0,sxx=0,szz=0,sxz=0,sxy=0,szy=0; int n=0; float mn=1e9f, mx=-1e9f;
  for (float x=-4; x<=4; x+=1) for (float z=-4; z<=4; z+=1) { float y = gy(cx+x, cz+z); if (float.IsNaN(y)) continue; n++; sx+=x; sz+=z; sy+=y; sxx+=x*x; szz+=z*z; sxz+=x*z; sxy+=x*y; szy+=z*y; mn=System.Math.Min(mn,y); mx=System.Math.Max(mx,y); }
  double mx_=sx/n, mz=sz/n, my=sy/n, cxx=sxx/n-mx_*mx_, czz=szz/n-mz*mz, cxz=sxz/n-mx_*mz, cxy=sxy/n-mx_*my, czy=szy/n-mz*my, det=cxx*czz-cxz*cxz;
  double gx=(cxy*czz-czy*cxz)/det, gz=(czy*cxx-cxy*cxz)/det;
  r += string.Format("({0},{1}) slope {2:F0}% d8m {3:F1}m | ", cx, cz, 100*System.Math.Sqrt(gx*gx+gz*gz), mx-mn);
}
return r;
