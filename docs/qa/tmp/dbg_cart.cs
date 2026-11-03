var d=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(); string o="";
o+=DebugWindow.GoPlace(3)+"\n";
o+="1) "+DebugWindow.ToggleCart()+" state="+d.State+" car="+d.Car.transform.position.ToString("F1")+"\n";
o+="2) "+DebugWindow.ToggleCart()+" state="+d.State+" feet="+d.Player.transform.position.ToString("F1")+"\n";
o+="3) fish: "+DebugWindow.Fish()+" state="+d.State+"\n";
o+="4) cart from Seated: "+DebugWindow.ToggleCart()+" state="+d.State+"\n";
o+="5) place from InCart: "+DebugWindow.GoPlace(1).Substring(0,20)+" state="+d.State+" camMain="+(Camera.main!=null)+"\n";
return o;
