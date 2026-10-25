// SessionState c.use = A|C, c.drv/c.cargo bool → 적용 후 c.tag 설정
CarVisualSwitcher.ShowDriver = UnityEditor.SessionState.GetBool("c.drv", false);
CarVisualSwitcher.ShowCargo = UnityEditor.SessionState.GetBool("c.cargo", false);
string w = UnityEditor.SessionState.GetString("c.use", "A");
var m1 = CarVisualSwitcher.Apply(w);
UnityEditor.SessionState.SetString("c.tag", w + "_after" + (CarVisualSwitcher.ShowDriver ? "_driver" : "") + (CarVisualSwitcher.ShowCargo ? "_cargo" : ""));
return m1;
