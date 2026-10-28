// 과제 0628(fb 4607 2회차): 남은 FAIL 점의 지형·길 중심선·길 콜라이더 높이(읽기만)
var sb = new System.Text.StringBuilder();
var pts = new[]{ new Vector2(286.5f,-61.3f), new Vector2(284.7f,-64.7f), new Vector2(285.4f,-66.3f), new Vector2(287.5f,-64.7f) };
var tmc = GameObject.Find("TownV6_Gray/TownV6_Terrain").GetComponent<MeshCollider>();
foreach (var P in pts) {
  sb.Append("@" + P + ": ");
  RaycastHit h; if (tmc.Raycast(new Ray(new Vector3(P.x,200,P.y), Vector3.down), out h, 400)) sb.Append("terr " + h.point.y.ToString("F2") + " ");
  foreach (var r in UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None)) { var c = r.centerline; if (c==null) continue; float bd=1e9f; float by=0; int bi=-1; float btr=0;
    for (int i=1;i<c.Length;i++){ var a=c[i-1]; var b=c[i]; var ab=b-a; ab.y=0; var ap=new Vector3(P.x,0,P.y)-new Vector3(a.x,0,a.z); float tr0=Vector3.Dot(ap,ab)/Mathf.Max(1e-6f,ab.sqrMagnitude); var q=Vector3.Lerp(a,b,Mathf.Clamp01(tr0)); float d=new Vector2(q.x-P.x,q.z-P.y).magnitude; if(d<bd){bd=d;by=q.y;bi=i;btr=tr0;} }
    if (bd - r.width*.5f < 4f) { sb.Append("| " + r.name + " w=" + r.width + " e=" + (bd-r.width*.5f).ToString("F2") + " y=" + by.ToString("F2") + " seg" + bi + "/" + (c.Length-1) + " t=" + btr.ToString("F2") + " ");
      var mc = r.GetComponent<MeshCollider>(); if (mc!=null && mc.Raycast(new Ray(new Vector3(P.x,200,P.y), Vector3.down), out h, 400)) sb.Append("col=" + h.point.y.ToString("F2") + " "); }
  }
  sb.Append("\n"); }
return sb.ToString();
