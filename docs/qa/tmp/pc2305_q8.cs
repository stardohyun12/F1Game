var sb=new System.Text.StringBuilder(); int n=0, near=0;
foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
 bool has=false; foreach(var m in r.sharedMaterials) if(m!=null&&(m.name=="TV6_Stairs")) has=true; if(!has) continue;
 n++; var b=r.bounds; string path=r.name; var t=r.transform.parent; int k=0; while(t!=null&&k<3){ path=t.name+"/"+path; t=t.parent; k++; }
 bool nr=Mathf.Abs(b.center.x-206)<80&&Mathf.Abs(b.center.z+168)<80; if(nr) near++;
 if(n<=12||nr) sb.AppendLine(path+" act="+r.gameObject.activeInHierarchy+" en="+r.enabled+" col="+(r.GetComponent<Collider>()!=null)+" c="+b.center.ToString("F0")+" s="+b.size.ToString("F0"));
}
sb.AppendLine("total="+n+" near206,-168="+near);
return sb.ToString();
