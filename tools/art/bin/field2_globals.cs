var sg=UnityEngine.Object.FindObjectsByType<A7ShadeGlobals>(FindObjectsInactive.Include,FindObjectsSortMode.None);
string s="count="+sg.Length;
foreach(var g in sg)s+=" ["+g.name+" enabled="+g.enabled+" active="+g.gameObject.activeInHierarchy+"]";
s+=" shade="+Shader.GetGlobalColor("_A7Shade")+" T="+Shader.GetGlobalVector("_A7T")+" mid="+Shader.GetGlobalFloat("_A7Mid");
foreach(var go in Resources.FindObjectsOfTypeAll<GameObject>()){if(go.name!="A7ShadeGlobals"||!go.scene.IsValid())continue;s+=" go-comps=";foreach(var c in go.GetComponents<Component>())s+=(c==null?"MISSING":c.GetType().Name)+",";}
return s;
