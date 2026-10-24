// 북쪽 부두 턱: z=119.4 선을 따라 x 0.5m 간격 하향 레이 → 턱 높이 0.2 미만 구간(틈) 목록. 이어 차를 동쪽 벽 앞에 두고 캡처.
var sb=new System.Text.StringBuilder(); float minH=99; int gaps=0; float gs=-1;
for(float x=-8f;x<=182f;x+=0.5f){ float h=-99; if(Physics.Raycast(new Vector3(x,10f,119.4f),Vector3.down,out var hit,20f,~0,QueryTriggerInteraction.Ignore)) h=hit.point.y;
 minH=Mathf.Min(minH,h); bool gap=h<0.2f; if(gap&&gs<0) gs=x; if(!gap&&gs>=0){ sb.Append("gap x="+gs+".."+(x-0.5f)+"; "); gaps++; gs=-1; } }
if(gs>=0){ sb.Append("gap x="+gs+"..182; "); gaps++; }
var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=c.GetComponent<Rigidbody>();
rb.position=new Vector3(165f,0.3f,-0.2f); rb.rotation=Quaternion.LookRotation(Vector3.right); rb.linearVelocity=Vector3.zero; Physics.SyncTransforms();
return "minH="+minH+" gaps="+gaps+" "+sb;
