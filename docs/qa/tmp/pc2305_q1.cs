var sb=new System.Text.StringBuilder();
sb.AppendLine(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+" playing="+UnityEditor.EditorApplication.isPlaying);
var cols=Physics.OverlapBox(new Vector3(-9f,0f,-290f),new Vector3(14f,10f,30f));
foreach(var c in cols){ var b=c.bounds; sb.AppendLine(c.name+" | "+c.GetType().Name+" trig="+c.isTrigger+" c="+b.center.ToString("F1")+" s="+b.size.ToString("F1")+" parent="+(c.transform.parent!=null?c.transform.parent.name:"-")); }
return sb.ToString();
