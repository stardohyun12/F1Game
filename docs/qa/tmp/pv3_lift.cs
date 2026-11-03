var t = PackTable2.I;
var p = t.receive.today[0];
var bx = t.WorkBox.Box;
if (t.Holding.ToString() == "Lifted") t.SimUse(t.WorkBox);
if (bx.IsClosed) { bx.Open(); bx.SnapLid(); }
p.Body.isKinematic = false;
p.transform.SetPositionAndRotation(bx.transform.TransformPoint(new UnityEngine.Vector3(0f, bx.InnerBottomY + 0.02f + p.transform.lossyScale.y * 0.5f, 0f)), bx.transform.rotation);
p.Body.position = p.transform.position; p.Body.linearVelocity = UnityEngine.Vector3.zero;
UnityEngine.Physics.SyncTransforms();
bx.TryClose(); bx.SnapLid();
t.SimUse(t.WorkBox);
var go = new UnityEngine.GameObject("LiftProbe");
var log = new System.Collections.Generic.List<string>();
var mb = go.AddComponent<PackV3Shots>();
mb.StartCoroutine(Probe());
System.Collections.IEnumerator Probe()
{
    for (int i = 0; i < 40; i++)
    {
        yield return new UnityEngine.WaitForFixedUpdate();
        var lp = bx.transform.InverseTransformPoint(p.transform.position);
        log.Add(i + " box=" + bx.transform.position.ToString("F2") + " plocal=" + lp.ToString("F3"));
    }
    System.IO.File.WriteAllLines("D:/Github/F1Game/docs/qa/tmp/lift.txt", log);
}
return "held=" + t.Holding + " closed=" + bx.IsClosed + " act=" + t.LastAct;
