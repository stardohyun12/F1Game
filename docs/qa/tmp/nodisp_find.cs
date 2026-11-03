var sb=new System.Text.StringBuilder();
var B=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
foreach(var asm in new[]{typeof(UnityEditor.Editor).Assembly, typeof(Camera).Assembly})
foreach(var t in asm.GetTypes()) {
  foreach(var m in t.GetMethods(B)) { var n=m.Name.ToLower(); if((n.Contains("nocamera")||n.Contains("camerasrendering")||n.Contains("hascamera")||n.Contains("anycamera")||n.Contains("displayhascamera")||(n.Contains("camera")&&n.Contains("display")))) sb.AppendLine(t.FullName+"."+m.Name+" "+m.ReturnType.Name+"("+string.Join(",",System.Array.ConvertAll(m.GetParameters(),p=>p.ParameterType.Name))+")"); }
}
return sb.ToString();
