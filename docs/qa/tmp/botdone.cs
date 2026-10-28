var s = UnityEditor.SessionState.GetString("a4e.wall", "");
if (!s.Contains("km/h")) throw new System.Exception("not yet: " + s.Length);
return s;
