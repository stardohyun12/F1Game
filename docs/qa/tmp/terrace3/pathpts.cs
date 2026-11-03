var mf=GameObject.Find("TownV6_Gray/TerraceBlock/DirtPath_house_field");if(!mf){var tb=GameObject.Find("TownV6_Gray/TerraceBlock");var s="children:";foreach(Transform c in tb.transform)s+=" "+c.name;return s;}
var m=mf.GetComponent<MeshFilter>().sharedMesh;var b=mf.GetComponent<MeshRenderer>().bounds;var vs=m.vertices;
var sb=new System.Text.StringBuilder("bounds "+b.min.ToString("F1")+" "+b.max.ToString("F1")+" n="+vs.Length+" | ");for(int i=0;i<vs.Length;i+=Mathf.Max(1,vs.Length/16)){var p=mf.transform.TransformPoint(vs[i]);sb.Append(p.ToString("F1")+" ");}
return sb.ToString();
