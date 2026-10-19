from pathlib import Path
p=Path(__file__).parent
s=Path('art/source/street-furniture/capture-real-scale.cs.txt').read_text(encoding='utf-8-sig')
start=s.index(' // Local preview deformation:')
end=s.index(' var line=',start)
s=s[:start]+s[end:]
s=s.replace('floor+1.58f','floor+1.63f').replace('Person(-5.8f,.7f);Person(1.6f,.6f);','Person(.5f,.7f);')
s=s.replace('new UnityEngine.Vector3(-3.4f,.12f,.65f)','new UnityEngine.Vector3(2.5f,.12f,.65f)')
s=s.replace('new UnityEngine.Vector3(.7f,0,6.6f)','new UnityEngine.Vector3(-3f,0,5.2f)')
s=s.replace('cam.fieldOfView=48','cam.fieldOfView=50')
s=s.replace('D:/Github/F1Game/art/source/street-furniture/','D:/Github/F1Game/art/_compare/2026-10-19-scale/')
a=s.index(' SizeBuildings(1f);')
b=s.index('\n}finally{',a)
s=s[:a]+'''
 var report=new System.Text.StringBuilder();
 report.AppendLine("Controlled comparison scene; no game scene or assets saved. Source workflow: capture-real-scale.cs.txt; car stretching REMOVED.");
 var cr=car.GetComponentsInChildren<UnityEngine.Renderer>();var cb=cr[0].bounds;foreach(var r in cr)cb.Encapsulate(r.bounds);
 if(UnityEngine.Mathf.Abs(cb.size.x-3.785f)>.02f)throw new System.Exception("Expected 3.785m car; got "+cb.size);
 report.AppendLine("Car world bounds="+cb.size.ToString("F3")+"; human=1.750m; bench=1.8m; road=6m; sidewalks=2.2/1.8m.");
 var black=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit"));black.SetColor("_BaseColor",UnityEngine.Color.black);temporaryMaterials.Add(black);
 var subjects=structures.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Renderer>()).Concat(cr).Concat(bench.GetComponentsInChildren<UnityEngine.Renderer>()).Concat(scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Scale marker")).SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Renderer>())).ToArray();
 var originals=subjects.Select(r=>r.sharedMaterials).ToArray();
 foreach(float scale in new[]{.64f,1f}) {
  SizeBuildings(scale);string id=scale<1?"064":"100";
  foreach(var g in structures){var rr=g.GetComponentsInChildren<UnityEngine.Renderer>();var bb=rr[0].bounds;foreach(var r in rr)bb.Encapsulate(r.bounds);report.AppendLine(id+" "+g.name+" bounds="+bb.size.ToString("F3"));}
  for(int mode=0;mode<2;mode++) {
   for(int i=0;i<subjects.Length;i++)subjects[i].sharedMaterials=mode==0?originals[i]:originals[i].Select(_=>black).ToArray();
   string suffix=mode==0?"":"_silhouette";
   Shot("drive_"+id+suffix,car.transform.position+new UnityEngine.Vector3(-7,3.2f,0),car.transform.position+UnityEngine.Vector3.up*1.15f,false,7.5f);
   Shot("side_"+id+suffix,new UnityEngine.Vector3(0,5,23),new UnityEngine.Vector3(0,3,0),true,7.8f);
   Shot("overview_"+id+suffix,new UnityEngine.Vector3(13,18,23),new UnityEngine.Vector3(0,0,1),true,12f);
  }
 }
 report.AppendLine("Drive offset=(-7,3.2,0), yaw=90, lookHeight=1.15, FOV=50. Side eye=(0,5,23), target=(0,3,0), ortho=7.8. Output=1920x1080.");
 System.IO.File.WriteAllText("D:/Github/F1Game/art/_compare/2026-10-19-scale/validation.txt",report.ToString());
 return report.ToString();
'''+s[b:]
p.joinpath('capture.cs.txt').write_text(s,encoding='utf-8')
