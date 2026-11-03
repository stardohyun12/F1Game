var sb=new System.Text.StringBuilder();
// 보관소 앞벽 라인: z=-31.8 에서 -z 방향으로 1 m 높이 레이 — x 별로 몇 m 가면 막히나
for(float x=266f;x<=278f;x+=0.5f){
 var o=new Vector3(x,31.06f+0.9f,-31.8f);
 string res="free"; if(Physics.Raycast(o,Vector3.back,out var h,4f,~0,QueryTriggerInteraction.Ignore)){ string path=h.collider.name; var t=h.collider.transform.parent; int k=0; while(t!=null&&k<2){ path=t.name+"/"+path; t=t.parent; k++; } res=h.distance.ToString("F2")+" "+path; }
 sb.AppendLine("x="+x.ToString("F1")+" -> "+res);
}
var pl=UnityEngine.Object.FindFirstObjectByType<OnFootPlayer>(); var cc=pl.Controller; sb.AppendLine("cc r="+cc.radius+" h="+cc.height+" step="+cc.stepOffset+" skin="+cc.skinWidth);
return sb.ToString();
