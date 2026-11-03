var t = PackTable2.I;
var p = t.receive.today[0];
var bx = t.WorkBox.Box;
if (bx.IsClosed) { bx.Open(); bx.SnapLid(); }
t.SimUse(p);
var s1 = "held=" + t.Holding;
t.SimUse(t.WorkBox);
var lp = bx.transform.InverseTransformPoint(p.transform.position);
return s1 + " after drop local=" + lp.ToString("F3") + " rbpos=" + bx.transform.InverseTransformPoint(p.Body.position).ToString("F3") + " kin=" + p.Body.isKinematic + " act=" + t.LastAct + " closed=" + bx.IsClosed;
