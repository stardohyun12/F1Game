var t = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } }).FirstOrDefault(x => x.Name == "BuildBayLoop");
return "compiling=" + UnityEditor.EditorApplication.isCompiling + " BuildBayLoop=" + (t != null) + " RoadSurface=" + (System.Type.GetType("RoadSurface, Assembly-CSharp") != null);
